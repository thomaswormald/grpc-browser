using Grpc.Core;
using System;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reactive.Threading.Tasks;

namespace SampleGrpcService.net50.Services.ProtoFirst
{
    public class ProtoFirstSampleService : ProtoFirstGreeter.ProtoFirstGreeterBase
    {
        public override Task<SampleProtoFirstReply> UnaryOperation(SampleProtoFirstRequest request, ServerCallContext context)
        {
            return Task.FromResult(new SampleProtoFirstReply { Content = $"Your request content was '{request.Content}'" });
        }

        public override Task ServerStreamingOperation(SampleProtoFirstRequest request, IServerStreamWriter<SampleProtoFirstReply> responseStream, ServerCallContext context)
        {
            return Observable.Interval(TimeSpan.FromSeconds(1))
                .Select(i => new SampleProtoFirstReply { Content = $"Streaming message #{i}. Your request content was '{request.Content}'" })
                .Do(reply => responseStream.WriteAsync(reply))
                .ToTask();
        }

        public override async Task<SampleProtoFirstReply> ClientStreamingOperation(IAsyncStreamReader<SampleProtoFirstRequest> requestStream, ServerCallContext context)
        {
            var messageCount = 0;
            var contentBuilder = new StringBuilder();

            await foreach (var message in requestStream.ReadAllAsync())
            {
                messageCount++;
                contentBuilder.AppendLine(message.Content);
            }

            return new SampleProtoFirstReply()
            {
                Content = $"You sent {messageCount} messages. The content of these messages was:\n{contentBuilder}"
            };
        }

        public override async Task DuplexStreamingOperation(IAsyncStreamReader<SampleProtoFirstRequest> requestStream, IServerStreamWriter<SampleProtoFirstReply> responseStream, ServerCallContext context)
        {
            var messageCount = 0;
            await foreach (var message in requestStream.ReadAllAsync())
            {
                messageCount++;
                await responseStream.WriteAsync(new SampleProtoFirstReply() { Content = $"You have sent {messageCount} requests so far. The most recent request content was {message.Content}" });
            }
                
        }
    }
}