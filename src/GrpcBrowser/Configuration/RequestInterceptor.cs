using System.Collections.Generic;
using System.Threading.Tasks;

namespace GrpcBrowser.Configuration
{
    public class GrpcRequest
    {
        internal GrpcRequest(string serviceName, string operationName, Dictionary<string, string> headers)
        {
            ServiceName = serviceName;
            OperationName = operationName;
            Headers = headers;
        }

        public GrpcRequest(GrpcRequest existing, Dictionary<string, string> updatedHeaders)
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
        Task<GrpcRequest> BeforeExecution(GrpcRequest request);
    }
}
