using System.Collections.Immutable;
using Fluxor;
using GrpcBrowser.Store.Services;

namespace GrpcBrowser.Store.Requests
{
    public record UnaryRequestState(object Request, CallUnaryOperation RequestAction, GrpcResponse Response);
    public record ServerStreamingConnectionState(object Request, bool Connected, CallServerStreamingOperation RequestAction, ImmutableList<GrpcResponse> Responses);
    public record ClientStreamingConnectionState(bool Connected, GrpcRequestHeaders Headers, ImmutableList<GrpcRequest> Requests, GrpcResponse? Response);
    public record DuplexConnectionState(bool Connected, GrpcRequestHeaders Headers, ImmutableList<GrpcRequest> Requests, ImmutableList<GrpcResponse> Responses);

    public record RequestState(
        ImmutableDictionary<GrpcRequestId, UnaryRequestState?> UnaryRequests,
        ImmutableDictionary<GrpcRequestId, ServerStreamingConnectionState> ServerStreamingRequests,
        ImmutableDictionary<GrpcRequestId, ClientStreamingConnectionState> ClientStreamingRequests,
        ImmutableDictionary<GrpcRequestId, DuplexConnectionState> DuplexRequests);

    public class Feature : Feature<RequestState>
    {
        public override string GetName() => "Requests";

        protected override RequestState GetInitialState() => 
            new RequestState(
                ImmutableDictionary<GrpcRequestId, UnaryRequestState?>.Empty,
                ImmutableDictionary<GrpcRequestId, ServerStreamingConnectionState>.Empty,
                ImmutableDictionary<GrpcRequestId, ClientStreamingConnectionState>.Empty, 
                ImmutableDictionary<GrpcRequestId, DuplexConnectionState>.Empty);
    }
}
