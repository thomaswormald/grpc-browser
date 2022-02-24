using System.Threading.Tasks;

namespace GrpcBrowser.Configuration.RequestInterceptors
{
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
}
