using System.Threading.Tasks;

namespace GrpcBrowser.Configuration.RequestInterceptors.Implementations
{
    public class SetHeaderInterceptor : IRequestInterceptor
    {
        private readonly string _headerName;
        private readonly string _headerValue;

        public SetHeaderInterceptor(string headerName, string headerValue, string desciption = null)
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
