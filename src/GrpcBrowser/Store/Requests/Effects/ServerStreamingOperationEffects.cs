using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fluxor;
using Grpc.Core;
using Grpc.Net.Client;
using GrpcBrowser.Infrastructure;
using GrpcBrowser.Store.Services;
using Microsoft.AspNetCore.Components;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Client;

namespace GrpcBrowser.Store.Requests.Effects
{
    public class ServerStreamingOperationEffects
    {
        private readonly GrpcChannelUrlProvider _urlProvider;

        public ServerStreamingOperationEffects(GrpcChannelUrlProvider urlProvider)
        {
            _urlProvider = urlProvider;
        }
        private ImmutableDictionary<GrpcRequestId, CancellationTokenSource> _cancellationTokens = ImmutableDictionary<GrpcRequestId, CancellationTokenSource>.Empty;

        private static async Task InvokeCodeFirstService(GrpcChannel channel, CallServerStreamingOperation action, object requestParameter, CallOptions callOptions, IDispatcher dispatcher)
        {
            var client = channel.GetCodeFirstGrpcServiceClient(action.Service.Name);
            var context = new CallContext(callOptions);

            try
            {
                var result = client.ServerStreamingAsync(requestParameter, action.Operation.Name, action.Operation.RequestType, action.Operation.ResponseType, context);

                await foreach (var message in result)
                {
                    dispatcher.Dispatch(new ServerStreamingResponseReceived(new GrpcResponse(DateTimeOffset.Now,
                        action.RequestId, action.Operation.ResponseType, message)));
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error connecting to server streaming operation");
            }
            finally
            {
                dispatcher.Dispatch(new ServerStreamingConnectionStopped(action.RequestId));
            }
        }

        private async Task InvokeProtoFileService(GrpcChannel channel, CallServerStreamingOperation action, object requestParameter, CallOptions callOptions, IDispatcher dispatcher)
        {
            var client = channel.GetProtoFileGrpcServiceClient(action.Service.Name);
            var result = client.InvokeGrpcOperation(action.Operation, requestParameter, callOptions);

            var responseStream = result.GetType().GetProperty("ResponseStream")?.GetValue(result);
            var moveNext = responseStream.GetType().GetMethod("MoveNext");


            var endOfStream = false;
            try
            {
                do
                {
                    var moveNextTask = (Task)moveNext.Invoke(responseStream,
                        new object[] { _cancellationTokens[action.RequestId].Token });

                    await moveNextTask;

                    var success = (bool)moveNextTask.GetType().GetProperty("Result").GetValue(moveNextTask);
                    endOfStream = !success;

                    if (!endOfStream)
                    {
                        var current = responseStream.GetType().GetProperty("Current")?.GetValue(responseStream);
                        dispatcher.Dispatch(new ServerStreamingResponseReceived(new GrpcResponse(DateTimeOffset.Now,
                            action.RequestId, action.Operation.ResponseType, current)));
                    }

                } while (!endOfStream && !_cancellationTokens[action.RequestId].IsCancellationRequested);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error connecting to server streaming operation"); 
            }
            finally
            {
                dispatcher.Dispatch(new ServerStreamingConnectionStopped(action.RequestId));
            }

        }

        [EffectMethod]
        public async Task Handle(CallServerStreamingOperation action, IDispatcher dispatcher)
        {
            var channel = GrpcChannel.ForAddress(_urlProvider.BaseUrl);

            var requestParameter = GrpcUtils.GetRequestParameter(action.RequestParameterJson, action.Operation.RequestType);

            var cts = new CancellationTokenSource();
            _cancellationTokens = _cancellationTokens.Add(action.RequestId, cts);
            var callOptions = GrpcUtils.GetCallOptions(action.Headers, cts.Token);

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
        public async Task Handle(StopServerStreamingConnection action, IDispatcher dispatcher)
        {
            if (_cancellationTokens.TryGetValue(action.RequestId, out var cts))
            {
                cts.Cancel();
                _cancellationTokens = _cancellationTokens.Remove(action.RequestId);
            }
        }
    }
}
