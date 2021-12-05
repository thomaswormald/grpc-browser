using Grpc.Core;
using System.Threading.Tasks;

namespace SampleGrpcService.netcore31.Services.ProtoFirst
{
    public class ProtoFirstGreeterService : ProtoFirstGreeter.ProtoFirstGreeterBase
    {
        public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
        {
            return Task.FromResult(new HelloReply
            {
                Message = "Hello " + request.Name
            });
        }
    }
}