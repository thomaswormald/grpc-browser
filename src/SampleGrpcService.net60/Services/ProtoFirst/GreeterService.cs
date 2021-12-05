using Grpc.Core;

namespace SampleGrpcService.net60.Services.ProtoFirst;

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
