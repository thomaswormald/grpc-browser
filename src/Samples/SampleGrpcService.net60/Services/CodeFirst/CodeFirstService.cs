using ProtoBuf.Grpc;
using System.Reactive.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace SampleGrpcService.net60.Services.CodeFirst;

[DataContract]
public class SampleCodeFirstRequest
{
    [DataMember(Order = 1)]
    public string Content { get; set; }
}

[DataContract]
public class SampleCodeFirstReply
{
    [DataMember(Order = 1)]
    public string Content { get; set; }
}

/// <summary>
/// This is a sample service that demonstrates all types of gRPC operations from a code-first gRPC service
/// </summary>
[ServiceContract]
public interface ICodeFirstGreeterService
{
    /// <summary>
    /// A Unary Void operation takes a single request, and does not return a response
    /// </summary>
    [OperationContract]
    Task UnaryVoidOperation(SampleCodeFirstRequest request, CallContext context = default);

    /// <summary>
    /// A Unary operation takes a single request, and returns a single response
    /// </summary>
    /// <param name="request"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    [OperationContract]
    Task<SampleCodeFirstReply> UnaryOperation(SampleCodeFirstRequest request, CallContext context = default);

    /// <summary>
    /// A Server Streaming operation takes a single request, and returns a stream of zero or more responses
    /// </summary>
    [OperationContract]
    IAsyncEnumerable<SampleCodeFirstReply> ServerStreamingOperation(SampleCodeFirstRequest request, CallContext context = default);

    /// <summary>
    /// A Client Streaming operation takes a stream of one or more requests, and returns a single response when the request stream is closed
    /// </summary>
    [OperationContract]
    Task<SampleCodeFirstReply> ClientStreamingOperation(IAsyncEnumerable<SampleCodeFirstRequest> request, CallContext context = default);

    /// <summary>
    /// A Duplex operation take a stream of zero or more requests, and returns a stream of zero or more responses
    /// </summary>
    [OperationContract]
    IAsyncEnumerable<SampleCodeFirstReply> DuplexStreamingOperation(IAsyncEnumerable<SampleCodeFirstRequest> request, CallContext context = default);
}

public class CodeFirstGreeterService : ICodeFirstGreeterService
{
    public Task UnaryVoidOperation(SampleCodeFirstRequest request, CallContext context = default)
    {
        return Task.CompletedTask;
    }

    public Task<SampleCodeFirstReply> UnaryOperation(SampleCodeFirstRequest request, CallContext context = default)
    {
        return Task.FromResult(new SampleCodeFirstReply { Content = $"Your request content was '{request.Content}'" });
    }

    public IAsyncEnumerable<SampleCodeFirstReply> ServerStreamingOperation(SampleCodeFirstRequest request, CallContext context = default)
    {
        return Observable.Interval(TimeSpan.FromSeconds(1))
            .Select(i => new SampleCodeFirstReply { Content = $"Streaming message #{i}. Your request content was '{request.Content}'" })
            .ToAsyncEnumerable();
    }

    public async Task<SampleCodeFirstReply> ClientStreamingOperation(IAsyncEnumerable<SampleCodeFirstRequest> request, CallContext context = default)
    {
        var messageCount = 0;
        var contentBuilder = new StringBuilder();

        await foreach (var message in request)
        {
            messageCount++;
            contentBuilder.AppendLine(message.Content);
        }

        return new SampleCodeFirstReply()
        {
            Content = $"You sent {messageCount} messages. The content of these messages was:\n{contentBuilder}"
        };
    }

    public IAsyncEnumerable<SampleCodeFirstReply> DuplexStreamingOperation(IAsyncEnumerable<SampleCodeFirstRequest> request, CallContext context = default)
    {
        return Observable.Create<SampleCodeFirstReply>(async obs =>
        {
            var messageCount = 0;
            await foreach (var message in request)
            {
                messageCount++;
                obs.OnNext(new SampleCodeFirstReply() { Content = $"You have sent {messageCount} requests so far. The most recent request content was {message.Content}" });
            }
        }).ToAsyncEnumerable();
    }
}