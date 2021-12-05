using ProtoBuf.Grpc;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Threading.Tasks;

namespace SampleGrpcService.net50.Services.CodeFirst
{
    [DataContract]
    public class HelloReply
    {
        [DataMember(Order = 1)]
        public string Message { get; set; }
    }

    [DataContract]
    public class HelloRequest
    {
        [DataMember(Order = 1)]
        public string Name { get; set; }
    }

    [ServiceContract]
    public interface ICodeFirstGreeterService
    {
        [OperationContract]
        Task<HelloReply> SayHelloAsync(HelloRequest request,
            CallContext context = default);
    }

    public class CodeFirstGreeterService : ICodeFirstGreeterService
    {
        public Task<HelloReply> SayHelloAsync(HelloRequest request, CallContext context = default)
        {
            return Task.FromResult(
                    new HelloReply
                    {
                        Message = $"Hello {request.Name}"
                    });
        }
    }
}
