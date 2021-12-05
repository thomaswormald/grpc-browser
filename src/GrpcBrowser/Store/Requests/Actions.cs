using GrpcBrowser.Store.Services;

namespace GrpcBrowser.Store.Requests
{
    public record CallUnaryOperation(GrpcService Service, GrpcOperation Operation, string RequestParameterJson, GrpcRequestId RequestId, GrpcRequestHeaders Headers);
    public record UnaryResponseReceived(GrpcResponse Response);

    public record CallServerStreamingOperation(GrpcService Service, GrpcOperation Operation, string RequestParameterJson, GrpcRequestId RequestId, GrpcRequestHeaders Headers);
    public record ServerStreamingResponseReceived(GrpcResponse Response);
    public record StopServerStreamingConnection(GrpcRequestId RequestId);
    public record ServerStreamingConnectionStopped(GrpcRequestId RequestId);

    public record CallClientStreamingOperation(GrpcService Service, GrpcOperation Operation, string FirstRequestParameterJson, GrpcRequestId RequestId, GrpcRequestHeaders Headers);
    public record SendMessageToConnectedClientStreamingOperation(GrpcRequestId RequestId, GrpcService Service, GrpcOperation Operation, string RequestParameterJson);
    public record MessageSentToClientStreamingOperation(GrpcRequestId RequestId);
    public record StopClientStreamingOperation(GrpcRequestId RequestId, GrpcService Service, GrpcOperation Operation);
    public record ClientStreamingResponseReceived(GrpcResponse Response);

    public record OpenDuplexConnection(GrpcService Service, GrpcOperation Operation, GrpcRequestId RequestId, GrpcRequestHeaders Headers);
    public record DuplexConnectionOpened(GrpcRequestId RequestId);
    public record SendMessageToConnectedDuplexOperation(GrpcRequestId RequestId, GrpcService Service, GrpcOperation Operation, string RequestParameterJson);
    public record MessageSentToDuplexOperation(GrpcRequestId RequestId);
    public record StopDuplexOperation(GrpcRequestId RequestId, GrpcService Service, GrpcOperation Operation);
    public record DuplexResponseReceived(GrpcResponse Response);
    public record DuplexConnectionStopped(GrpcRequestId RequestId);
}
