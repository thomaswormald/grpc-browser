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
using GrpcBrowser.Store.Requests;
using GrpcBrowser.Store.Services;
using ProtoBuf.Grpc;

namespace GrpcBrowser.Store.Requests.Effects
{
    public class ClientStreamingOperationEffects
    {
        private readonly GrpcChannelUrlProvider _channelUrlProvider;
        // Stores the AsyncClientStreamingCall<TRequest, TResponse> for open client streaming connections
        // Only used for proto-first service connections
        private ImmutableDictionary<GrpcRequestId, object> _openProtoFirstStreamingRequests = ImmutableDictionary<GrpcRequestId, object>.Empty;

        record CodeFirstOpenClientStreamingConnection(Subject<object> RequestStream, Task ResultTask);
        private ImmutableDictionary<GrpcRequestId, CodeFirstOpenClientStreamingConnection> _openCodeFirstStreamingRequests = ImmutableDictionary<GrpcRequestId, CodeFirstOpenClientStreamingConnection>.Empty;

        public ClientStreamingOperationEffects(GrpcChannelUrlProvider channelUrlProvider)
        {
            _channelUrlProvider = channelUrlProvider;
        }

        private async Task WriteMessageToCodeFirstOperation(GrpcRequestId requestId, object request, IDispatcher dispatcher)
        {
            if (_openCodeFirstStreamingRequests.TryGetValue(requestId, out var openConnection))
            {
                openConnection.RequestStream.OnNext(request);

                dispatcher.Dispatch(new MessageSentToClientStreamingOperation(requestId));
            }
        }

        private async Task InvokeCodeFirstService(GrpcChannel channel, CallClientStreamingOperation action, object requestParameter, CallOptions callOptions, IDispatcher dispatcher)
        {
            var client = channel.GetCodeFirstGrpcServiceClient(action.Service.Name);
            var context = new CallContext(callOptions);

            var subject = new Subject<object>();

            var resultTask = client.ClientStreamingAsync(subject.ToAsyncEnumerable(), action.Operation.Name, action.Operation.RequestType, action.Operation.ResponseType, context);

            _openCodeFirstStreamingRequests = _openCodeFirstStreamingRequests.SetItem(action.RequestId, new CodeFirstOpenClientStreamingConnection(subject, resultTask));

            await WriteMessageToCodeFirstOperation(action.RequestId, requestParameter, dispatcher);
        }

        private async Task WriteMessageToProtoFirstOperation(GrpcRequestId requestId, GrpcOperation operation, object request, IDispatcher dispatcher)
        {
            if (_openProtoFirstStreamingRequests.TryGetValue(requestId, out var clientStreamingCall))
            {
                var requestStream = clientStreamingCall.GetType().GetProperty(nameof(AsyncClientStreamingCall<object, object>.RequestStream))?.GetValue(clientStreamingCall);

                var writeAsync = requestStream.GetType().GetMethod(nameof(IAsyncStreamWriter<object>.WriteAsync), new[] { operation.RequestType });

                await (Task)writeAsync.Invoke(requestStream, new[] { request });

                dispatcher.Dispatch(new MessageSentToClientStreamingOperation(requestId));
            }
        }

        private async Task InvokeProtoFileService(GrpcChannel channel, CallClientStreamingOperation action, object requestParameter, CallOptions callOptions, IDispatcher dispatcher)
        {
            var client = channel.GetProtoFileGrpcServiceClient(action.Service.Name);

            var method = client.GetType().GetMethod(action.Operation.Name, new[] { typeof(CallOptions) });

            var result = method?.Invoke(client, new object[] { callOptions });

            _openProtoFirstStreamingRequests = _openProtoFirstStreamingRequests.SetItem(action.RequestId, result);

            await WriteMessageToProtoFirstOperation(action.RequestId, action.Operation, requestParameter, dispatcher);
        }

        private async Task StopOpenProtoFirstOperation(GrpcRequestId requestId, GrpcOperation operation, IDispatcher dispatcher)
        {
            if (_openProtoFirstStreamingRequests.TryGetValue(requestId, out var clientStreamingCall))
            {
                var requestStream = clientStreamingCall.GetType()
                    .GetProperty(nameof(AsyncClientStreamingCall<object, object>.RequestStream))?.GetValue(clientStreamingCall);

                var completeAsync = requestStream.GetType().GetMethod(nameof(IClientStreamWriter<object>.CompleteAsync));

                await (Task)completeAsync.Invoke(requestStream, null);

                var responseTask = (Task)clientStreamingCall.GetType().GetProperty(nameof(AsyncClientStreamingCall<object, object>.ResponseAsync))?.GetValue(clientStreamingCall);

                await responseTask;

                var response = responseTask.GetType().GetProperty("Result").GetValue(responseTask);

                dispatcher.Dispatch(new ClientStreamingResponseReceived(new GrpcResponse(DateTimeOffset.Now, requestId, operation.ResponseType, response)));

                _openProtoFirstStreamingRequests = _openProtoFirstStreamingRequests.Remove(requestId);
            }
        }

        private async Task StopOpenCodeFirstOperation(GrpcRequestId requestId, GrpcOperation operation, IDispatcher dispatcher)
        {
            if (_openCodeFirstStreamingRequests.TryGetValue(requestId, out var clientStreamingCall))
            {
                clientStreamingCall.RequestStream.OnCompleted();

                await clientStreamingCall.ResultTask;

                var response = clientStreamingCall.ResultTask.GetType().GetProperty("Result").GetValue(clientStreamingCall.ResultTask);

                dispatcher.Dispatch(new ClientStreamingResponseReceived(new GrpcResponse(DateTimeOffset.Now, requestId, operation.ResponseType, response)));
            }
        }

        [EffectMethod]
        public async Task Handle(CallClientStreamingOperation action, IDispatcher dispatcher)
        {
            var channel = GrpcChannel.ForAddress(_channelUrlProvider.BaseUrl);

            var callOptions = GrpcUtils.GetCallOptions(action.Headers, CancellationToken.None);

            var requestParameter = GrpcUtils.GetRequestParameter(action.FirstRequestParameterJson, action.Operation.RequestType);

            if (action.Service.ImplementationType == GrpcServiceImplementationType.CodeFirst)
            {
                await InvokeCodeFirstService(channel, action, requestParameter, callOptions, dispatcher);
            }
            else if (action.Service.ImplementationType == GrpcServiceImplementationType.ProtoFile)
            {
                await InvokeProtoFileService(channel, action, requestParameter, callOptions, dispatcher);
            }
        }

        [EffectMethod]
        public async Task Handle(SendMessageToConnectedClientStreamingOperation action, IDispatcher dispatcher)
        {
            var requestParameter = GrpcUtils.GetRequestParameter(action.RequestParameterJson, action.Operation.RequestType);

            if (action.Service.ImplementationType == GrpcServiceImplementationType.ProtoFile)
            {
                await WriteMessageToProtoFirstOperation(action.RequestId, action.Operation, requestParameter, dispatcher);
            }
            else if (action.Service.ImplementationType == GrpcServiceImplementationType.CodeFirst)
            {
                await WriteMessageToCodeFirstOperation(action.RequestId, requestParameter, dispatcher);
            }
        }

        [EffectMethod]
        public async Task Handle(StopClientStreamingOperation action, IDispatcher dispatcher)
        {
            if (action.Service.ImplementationType == GrpcServiceImplementationType.ProtoFile)
            {
                await StopOpenProtoFirstOperation(action.RequestId, action.Operation, dispatcher);
            }
            else if (action.Service.ImplementationType == GrpcServiceImplementationType.CodeFirst)
            {
                await StopOpenCodeFirstOperation(action.RequestId, action.Operation, dispatcher);
            }
        }

    }
}
