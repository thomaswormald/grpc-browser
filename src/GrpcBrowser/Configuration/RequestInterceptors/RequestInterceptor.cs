using GrpcBrowser.Store.Requests;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace GrpcBrowser.Configuration.RequestInterceptors
{
    internal static class RequestInterceptorHelper
    {
        internal static async Task<T> ApplyAllInterceptors<T>(this T request) where T : BaseAction
        {
            var grpcRequest = new InterceptedGrpcRequest(request.Service.ServiceType.Name, request.Operation.Name, request.Headers.Values);

            foreach (var interceptor in GrpcBrowserConfiguration.GlobalRequestInterceptors)
            {
                grpcRequest = await interceptor.BeforeExecution(grpcRequest);
            }

            ImmutableList<IRequestInterceptor> serviceRequestInterceptors = ImmutableList<IRequestInterceptor>.Empty;

            if (request.Service.ImplementationType == Store.Services.GrpcServiceImplementationType.CodeFirst)
            {
                serviceRequestInterceptors =
                    GrpcBrowserConfiguration.CodeFirstGrpcServiceInterfaces.TryGetValue(request.Service.ServiceType.FullName, out var configuration) 
                    ? configuration.Interceptors 
                    : serviceRequestInterceptors;
            }
            else if (request.Service.ImplementationType == Store.Services.GrpcServiceImplementationType.ProtoFile)
            {
                serviceRequestInterceptors =
                    GrpcBrowserConfiguration.ProtoGrpcClients.TryGetValue(request.Service.ServiceType.FullName, out var configuration)
                    ? configuration.Interceptors
                    : serviceRequestInterceptors;
            }

            foreach (var interceptor in serviceRequestInterceptors)
            {
                grpcRequest = await interceptor.BeforeExecution(grpcRequest);
            }

            return request with
            {
                Headers = request.Headers with { Values = request.Headers.Values.SetItems(grpcRequest.Headers) }
            };
        }
    }

}
