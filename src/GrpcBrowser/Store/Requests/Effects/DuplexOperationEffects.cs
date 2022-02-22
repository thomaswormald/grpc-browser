using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Fluxor;
using Grpc.Core;
using Grpc.Net.Client;
using GrpcBrowser.Infrastructure;
using GrpcBrowser.Store.Services;
using ProtoBuf.Grpc;

namespace GrpcBrowser.Store.Requests.Effects
{
    public class DuplexOperationEffects
    {
        private readonly GrpcChannelUrlProvider _channelUrlProvider;
        record OpenProtoDuplexConnection(object DuplexResponse, CancellationTokenSource CancellationTokenSource);
        private ImmutableDictionary<GrpcRequestId, OpenProtoDuplexConnection> _openProtoConnections = ImmutableDictionary<GrpcRequestId, OpenProtoDuplexConnection>.Empty;

        record OpenCodeFirstDuplexConnection(Subject<object> RequestStream, CancellationTokenSource CancellationTokenSource);
        private ImmutableDictionary<GrpcRequestId, OpenCodeFirstDuplexConnection> _openCodeFirstConnections = ImmutableDictionary<GrpcRequestId, OpenCodeFirstDuplexConnection>.Empty;

        public DuplexOperationEffects(GrpcChannelUrlProvider channelUrlProvider)
        {
            _channelUrlProvider = channelUrlProvider;
        }

        private async Task WriteMessageToProtoFirstOperation(GrpcRequestId requestId, GrpcOperation operation, object request, DateTimeOffset timestamp, Type requestType, IDispatcher dispatcher)
        {
            if (_openProtoConnections.TryGetValue(requestId, out var clientStreamingCall))
            {
                var requestStream = clientStreamingCall.DuplexResponse.GetType().GetProperty(nameof(AsyncDuplexStreamingCall<object, object>.RequestStream))?.GetValue(clientStreamingCall.DuplexResponse);

                var writeAsync = requestStream.GetType().GetMethod(nameof(IAsyncStreamWriter<object>.WriteAsync), new[] { operation.RequestType });

                await (Task)writeAsync.Invoke(requestStream, new[] { request });

                dispatcher.Dispatch(new MessageSentToDuplexOperation(new GrpcRequest(timestamp, requestId, requestType, request)));
            }
        }

        private async Task InvokeProtoFileService(GrpcChannel channel, OpenDuplexConnection action, CallOptions callOptions, IDispatcher dispatcher, CancellationTokenSource cts)
        {
            var client = channel.GetProtoFileGrpcServiceClient(action.Service.ServiceType.Name);
            var method = client.GetType().GetMethod(action.Operation.Name, new[] { typeof(CallOptions) });
            var result = method?.Invoke(client, new object[] { callOptions });

            dispatcher.Dispatch(new DuplexConnectionOpened(action.RequestId));

            var responseStream = result.GetType().GetProperty(nameof(AsyncDuplexStreamingCall<object, object>.ResponseStream))?.GetValue(result);
            var moveNext = responseStream.GetType().GetMethod(nameof(AsyncDuplexStreamingCall<object, object>.ResponseStream.MoveNext));
            
            _openProtoConnections = _openProtoConnections.SetItem(action.RequestId, new OpenProtoDuplexConnection(result, cts));

            var endOfStream = false;
            try
            {
                do
                {
                    var moveNextTask = (Task)moveNext.Invoke(responseStream, new object[] { cts.Token });

                    await moveNextTask;

                    var success = (bool)moveNextTask.GetType().GetProperty("Result").GetValue(moveNextTask);
                    endOfStream = !success;

                    if (!endOfStream)
                    {
                        var current = responseStream.GetType().GetProperty("Current")?.GetValue(responseStream);
                        dispatcher.Dispatch(new DuplexResponseReceived(new GrpcResponse(DateTimeOffset.Now, action.RequestId, action.Operation.ResponseType, current)));
                    }

                } while (!endOfStream && !cts.IsCancellationRequested);
            }
            catch (OperationCanceledException) { /* Don't care, this will throw when the cancellation token is activated */ }
            finally
            {
                dispatcher.Dispatch(new DuplexConnectionStopped(action.RequestId));
            }
        }

        private async Task WriteMessageToCodeFirstOperation(GrpcRequestId requestId, object request, DateTimeOffset timestamp, Type requestType, IDispatcher dispatcher)
        {
            if (_openCodeFirstConnections.TryGetValue(requestId, out var openConnection))
            {
                openConnection.RequestStream.OnNext(request);

                dispatcher.Dispatch(new MessageSentToDuplexOperation(new GrpcRequest(timestamp, requestId, requestType, request)));
            }
        }

        private async Task InvokeCodeFirstService(GrpcChannel channel, OpenDuplexConnection action, CallOptions callOptions, IDispatcher dispatcher, CancellationTokenSource cts)
        {
            var client = channel.GetCodeFirstGrpcServiceClient(action.Service.ServiceType.Name);
            var context = new CallContext(callOptions);

            var requestStreamSubject = new Subject<object>();

            try
            {
                var result = client.DuplexAsync(requestStreamSubject.ToAsyncEnumerable(), action.Operation.Name, action.Operation.RequestType, action.Operation.ResponseType, context);

                _openCodeFirstConnections = _openCodeFirstConnections.SetItem(action.RequestId, new OpenCodeFirstDuplexConnection(requestStreamSubject, cts));
                dispatcher.Dispatch(new DuplexConnectionOpened(action.RequestId));

                await foreach (var message in result)
                {
                    dispatcher.Dispatch(new DuplexResponseReceived(new GrpcResponse(DateTimeOffset.Now, action.RequestId, action.Operation.ResponseType, message)));
                }
            }
            catch (OperationCanceledException) { /* Don't care, this will throw when the cancellation token is activated */ }
            finally
            {
                dispatcher.Dispatch(new DuplexConnectionStopped(action.RequestId));
            }
        }

        [EffectMethod]
        public async Task Handle(OpenDuplexConnection action, IDispatcher dispatcher)
        {
            var channel = GrpcChannel.ForAddress(_channelUrlProvider.BaseUrl);

            var cts = new CancellationTokenSource();
            var callOptions = GrpcUtils.GetCallOptions(action.Headers, cts.Token);

            try
            {
                if (action.Service.ImplementationType == GrpcServiceImplementationType.CodeFirst)
                {
                    await InvokeCodeFirstService(channel, action, callOptions, dispatcher, cts);
                }
                else if (action.Service.ImplementationType == GrpcServiceImplementationType.ProtoFile)
                {
                    await InvokeProtoFileService(channel, action, callOptions, dispatcher, cts);
                }
            }
            catch (Exception ex)
            {
                dispatcher.Dispatch(new DuplexResponseReceived(new GrpcResponse(DateTimeOffset.Now, action.RequestId, ex.GetType(), ex)));
            }
        }

        [EffectMethod]
        public async Task Handle(SendMessageToConnectedDuplexOperation action, IDispatcher dispatcher)
        {
            var requestParameter = GrpcUtils.GetRequestParameter(action.RequestParameterJson, action.Operation.RequestType);

            try
            {
                if (action.Service.ImplementationType == GrpcServiceImplementationType.CodeFirst)
                {
                    await WriteMessageToCodeFirstOperation(action.RequestId, requestParameter, action.Timestamp, action.Operation.RequestType, dispatcher);
                }
                else if (action.Service.ImplementationType == GrpcServiceImplementationType.ProtoFile)
                {
                    await WriteMessageToProtoFirstOperation(action.RequestId, action.Operation, requestParameter, action.Timestamp, action.Operation.RequestType, dispatcher);
                }
            }
            catch (Exception ex)
            {
                dispatcher.Dispatch(new DuplexResponseReceived(new GrpcResponse(DateTimeOffset.Now, action.RequestId, ex.GetType(), ex)));
                dispatcher.Dispatch(new DuplexConnectionStopped(action.RequestId));
            }
        }

        [EffectMethod]
        public async Task Handle(StopDuplexOperation action, IDispatcher dispatcher)
        {
            if (action.Service.ImplementationType == GrpcServiceImplementationType.ProtoFile)
            {
                if (_openProtoConnections.TryGetValue(action.RequestId, out var openConnection))
                {
                    var requestStream = openConnection.DuplexResponse.GetType().GetProperty(nameof(AsyncDuplexStreamingCall<object, object>.RequestStream))?.GetValue(openConnection.DuplexResponse);

                    var completeAsync = requestStream.GetType().GetMethod(nameof(IClientStreamWriter<object>.CompleteAsync));

                    await (Task)completeAsync.Invoke(requestStream, null);

                    openConnection.CancellationTokenSource.Cancel();
                    _openProtoConnections = _openProtoConnections.Remove(action.RequestId);
                }
            }
            else if (action.Service.ImplementationType == GrpcServiceImplementationType.CodeFirst)
            {
                if (_openCodeFirstConnections.TryGetValue(action.RequestId, out var openConnection))
                {
                    openConnection.RequestStream.OnCompleted();

                    // There appears to be a race condition in Grpc.Net.Client which makes this delay necessary: https://issueexplorer.com/issue/grpc/grpc-dotnet/1394
                    await Task.Delay(500);

                    openConnection.CancellationTokenSource.Cancel();
                    _openCodeFirstConnections = _openCodeFirstConnections.Remove(action.RequestId);
                }
            }

            dispatcher.Dispatch(new DuplexConnectionStopped(action.RequestId));
        }
    }
}
