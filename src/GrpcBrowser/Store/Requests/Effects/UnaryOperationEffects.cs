using System;
using System.Threading;
using System.Threading.Tasks;
using Fluxor;
using Grpc.Core;
using Grpc.Net.Client;
using GrpcBrowser.Infrastructure;
using GrpcBrowser.Store.Services;
using Microsoft.Extensions.DependencyInjection;
using ProtoBuf.Grpc;

namespace GrpcBrowser.Store.Requests.Effects
{
    public class UnaryOperationEffects
    {
        private readonly GrpcChannelUrlProvider _urlProvider;
        private readonly IServiceProvider _serviceProvider;

        public UnaryOperationEffects(GrpcChannelUrlProvider urlProvider, IServiceProvider serviceProvider)
        {
            _urlProvider = urlProvider;
            _serviceProvider = serviceProvider;
        }

        private static async Task InvokeCodeFirstService(GrpcChannel channel, CallUnaryOperation action, object requestParameter, CallOptions callOptions, IDispatcher dispatcher)
        {
            var client = channel.GetCodeFirstGrpcServiceClient(action.Service.ServiceType.Name);
            var context = new CallContext(callOptions);

            var result = await client.UnaryAsync(requestParameter, action.Operation.Name, action.Operation.RequestType, action.Operation.ResponseType, context);

            dispatcher.Dispatch(new UnaryResponseReceived(requestParameter, action, new GrpcResponse(DateTimeOffset.Now, action.RequestId, action.Operation.ResponseType, result)));
        }

        private static async Task InvokeProtoFileService(GrpcChannel channel, CallUnaryOperation action, object requestParameter, CallOptions callOptions, IDispatcher dispatcher)
        {
            var client = channel.GetProtoFileGrpcServiceClient(action.Service.ServiceType.Name);
            var result = client.InvokeGrpcOperation(action.Operation, requestParameter, callOptions);
            
            if (result.GetType().Name == "AsyncUnaryCall`1")
            {
                var resultTask = (Task)result.GetType().GetProperty("ResponseAsync").GetValue(result);

                await resultTask;
                var resultProperty = resultTask.GetType().GetProperty("Result");
                result = resultProperty.GetValue(resultTask);
            }

            dispatcher.Dispatch(new UnaryResponseReceived(requestParameter, action, new GrpcResponse(DateTimeOffset.Now, action.RequestId, action.Operation.ResponseType, result)));
        }

        [EffectMethod]
        public async Task Handle(CallUnaryOperation action, IDispatcher dispatcher)
        {
            try
            {
                var client = _serviceProvider.GetRequiredService(action.Service.ServiceType);
            }
            catch (InvalidOperationException ex)
            {
                // No registered service
            }

            var channel = GrpcChannel.ForAddress(_urlProvider.BaseUrl);

            var requestParameter = GrpcUtils.GetRequestParameter(action.RequestParameterJson, action.Operation.RequestType);

            var callOptions = GrpcUtils.GetCallOptions(action.Headers, CancellationToken.None);

            try
            {
                if (action.Service.ImplementationType == GrpcServiceImplementationType.CodeFirst)
                {
                    await InvokeCodeFirstService(channel, action, requestParameter, callOptions, dispatcher);
                }
                else if (action.Service.ImplementationType == GrpcServiceImplementationType.ProtoFile)
                {
                    await InvokeProtoFileService(channel, action, requestParameter, callOptions, dispatcher);
                }
            }
            catch (Exception ex)
            {
                dispatcher.Dispatch(new UnaryResponseReceived(requestParameter, action, new GrpcResponse(DateTimeOffset.Now, action.RequestId, ex.GetType(), ex)));
            }
        }
    }
}
