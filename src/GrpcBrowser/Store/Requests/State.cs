using System.Collections.Immutable;
using Fluxor;
using GrpcBrowser.Store.Services;

namespace GrpcBrowser.Store.Requests
{
    public record ServerStreamingConnectionState(bool Connected, ImmutableList<GrpcResponse> Responses);
    public record ClientStreamingConnectionState(bool Connected, GrpcResponse? Response);
    public record DuplexConnectionState(bool Connected, ImmutableList<GrpcResponse> Responses);

    public record RequestState(
        ImmutableDictionary<GrpcRequestId, GrpcResponse?> UnaryRequests,
        ImmutableDictionary<GrpcRequestId, ServerStreamingConnectionState> ServerStreamingRequests,
        ImmutableDictionary<GrpcRequestId, ClientStreamingConnectionState> ClientStreamingRequests,
        ImmutableDictionary<GrpcRequestId, DuplexConnectionState> DuplexRequests);

    public class Feature : Feature<RequestState>
    {
        public override string GetName() => "Requests";

        protected override RequestState GetInitialState() => 
            new RequestState(
                ImmutableDictionary<GrpcRequestId, GrpcResponse?>.Empty,
                ImmutableDictionary<GrpcRequestId, ServerStreamingConnectionState>.Empty,
                ImmutableDictionary<GrpcRequestId, ClientStreamingConnectionState>.Empty, 
                ImmutableDictionary<GrpcRequestId, DuplexConnectionState>.Empty);
    }
}
