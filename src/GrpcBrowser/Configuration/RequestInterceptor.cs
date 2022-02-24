using GrpcBrowser.Store.Requests;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace GrpcBrowser.Configuration
{
    public class InterceptedGrpcRequest
    {
        internal InterceptedGrpcRequest(string serviceName, string operationName, IDictionary<string, string> headers)
        {
            ServiceName = serviceName;
            OperationName = operationName;
            Headers = new Dictionary<string, string>(headers);
        }

        public InterceptedGrpcRequest(InterceptedGrpcRequest existing, Dictionary<string, string> updatedHeaders)
        {
            ServiceName = existing.ServiceName;
            OperationName = existing.OperationName;
            Headers = updatedHeaders;
        }

        public string ServiceName { get; }
        public string OperationName { get; }
        public Dictionary<string, string> Headers { get; set; }
    }

    public interface IRequestInterceptor
    {
        /// <summary>
        /// Called just before the operation is executed. To modify the request, return an updated version of the request parameter
        /// </summary>
        Task<InterceptedGrpcRequest> BeforeExecution(InterceptedGrpcRequest request);

        /// <summary>
        /// A description that will be displayed to the user
        /// </summary>
        string Description { get; }
    }
        
    internal static class RequestInterceptorHelper
    {
        internal static async Task<T> ApplyAllInterceptors<T>(this T request) where T : BaseAction
        {
            var grpcRequest = new InterceptedGrpcRequest(request.Service.ServiceType.Name, request.Operation.Name, request.Headers.Values);

            foreach (var interceptor in ConfiguredGrpcServices.GlobalRequestInterceptors)
            {
                grpcRequest = await interceptor.BeforeExecution(grpcRequest);
            }

            ImmutableList<IRequestInterceptor> serviceRequestInterceptors = ImmutableList<IRequestInterceptor>.Empty;

            if (request.Service.ImplementationType == Store.Services.GrpcServiceImplementationType.CodeFirst)
            {
                serviceRequestInterceptors = 
                    ConfiguredGrpcServices.CodeFirstGrpcServiceInterfaces.TryGetValue(request.Service.ServiceType.FullName, out var configuration) 
                    ? configuration.Interceptors 
                    : serviceRequestInterceptors;
            }
            else if (request.Service.ImplementationType == Store.Services.GrpcServiceImplementationType.ProtoFile)
            {
                serviceRequestInterceptors =
                    ConfiguredGrpcServices.ProtoGrpcClients.TryGetValue(request.Service.ServiceType.FullName, out var configuration)
                    ? configuration.Interceptors
                    : serviceRequestInterceptors;
            }

            foreach (var interceptor in serviceRequestInterceptors)
            {
                grpcRequest = await interceptor.BeforeExecution(grpcRequest);
            }

            return request with
            {
                Headers = request.Headers with { Values = request.Headers.Values.AddRange(grpcRequest.Headers) }
            };
        }
    }

    public class AddHeaderInterceptor : IRequestInterceptor
    {
        private readonly string _headerName;
        private readonly string _headerValue;

        public AddHeaderInterceptor(string headerName, string headerValue, string desciption = null)
        {
            _headerName = headerName;
            _headerValue = headerValue;
            Description = desciption ?? $"Add a header '{headerName}': '{headerValue}'";
        }

        public string Description { get; set; }

        public Task<InterceptedGrpcRequest> BeforeExecution(InterceptedGrpcRequest request)
        {
            request.Headers.Add(_headerName, _headerValue);

            return Task.FromResult(request);
        }
    }
}
