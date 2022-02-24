using System.Collections.Generic;

namespace GrpcBrowser.Configuration.RequestInterceptors
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
}
