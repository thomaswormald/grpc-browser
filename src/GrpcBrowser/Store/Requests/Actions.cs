using GrpcBrowser.Store.Services;
using System;

namespace GrpcBrowser.Store.Requests
{
    public record BaseAction(GrpcService Service, GrpcOperation Operation, GrpcRequestId RequestId, GrpcRequestHeaders Headers);

    public record CallUnaryOperation(GrpcService Service, GrpcOperation Operation, string RequestParameterJson, GrpcRequestId RequestId, GrpcRequestHeaders Headers, DateTimeOffset Timestamp) : BaseAction(Service, Operation, RequestId, Headers);
    public record UnaryResponseReceived(object RequestBody, CallUnaryOperation RequestAction, GrpcResponse Response);

    public record CallServerStreamingOperation(GrpcService Service, GrpcOperation Operation, string RequestParameterJson, GrpcRequestId RequestId, GrpcRequestHeaders Headers, DateTimeOffset Timestamp) : BaseAction(Service, Operation, RequestId, Headers);
    public record ServerStreamingResponseReceived(object RequestBody, CallServerStreamingOperation RequestAction, GrpcResponse Response);
    public record StopServerStreamingConnection(GrpcRequestId RequestId);
    public record ServerStreamingConnectionStopped(GrpcRequestId RequestId);

    public record CallClientStreamingOperation(GrpcService Service, GrpcOperation Operation, string FirstRequestParameterJson, GrpcRequestId RequestId, GrpcRequestHeaders Headers, DateTimeOffset Timestamp) : BaseAction(Service, Operation, RequestId, Headers);
    public record SendMessageToConnectedClientStreamingOperation(GrpcRequestId RequestId, GrpcService Service, GrpcOperation Operation, string RequestParameterJson, DateTimeOffset Timestamp);
    public record MessageSentToClientStreamingOperation(GrpcRequest Request);
    public record StopClientStreamingOperation(GrpcRequestId RequestId, GrpcService Service, GrpcOperation Operation);
    public record ClientStreamingResponseReceived(GrpcResponse Response);

    public record OpenDuplexConnection(GrpcService Service, GrpcOperation Operation, GrpcRequestId RequestId, GrpcRequestHeaders Headers) : BaseAction(Service, Operation, RequestId, Headers);
    public record DuplexConnectionOpened(GrpcRequestId RequestId);
    public record SendMessageToConnectedDuplexOperation(GrpcRequestId RequestId, GrpcService Service, GrpcOperation Operation, string RequestParameterJson, DateTimeOffset Timestamp);
    public record MessageSentToDuplexOperation(GrpcRequest Request);
    public record StopDuplexOperation(GrpcRequestId RequestId, GrpcService Service, GrpcOperation Operation);
    public record DuplexResponseReceived(GrpcResponse Response);
    public record DuplexConnectionStopped(GrpcRequestId RequestId);
}
