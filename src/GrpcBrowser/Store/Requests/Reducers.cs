using System.Collections.Immutable;
using Fluxor;
using GrpcBrowser.Store.Services;

namespace GrpcBrowser.Store.Requests
{
    public static class Reducers
    {
        [ReducerMethod]
        public static RequestState Reduce(RequestState state, UnaryResponseReceived action) => state with {UnaryRequests = state.UnaryRequests.SetItem(action.Response.RequestId, new UnaryRequestState(action.RequestBody, action.RequestAction, action.Response))};

        [ReducerMethod]
        public static RequestState Reduce(RequestState state, ServerStreamingResponseReceived action)
        {
            var existingResponses =
                state.ServerStreamingRequests.TryGetValue(action.Response.RequestId, out var existing)
                    ? existing
                    : new ServerStreamingConnectionState(action.RequestBody, false, action.RequestAction, ImmutableList<GrpcResponse>.Empty);

            var updated = existingResponses with { Responses = existingResponses.Responses.Add(action.Response), Connected = true };

            return state with
            {
                ServerStreamingRequests = state.ServerStreamingRequests.SetItem(action.Response.RequestId, updated)
            };
        }

        [ReducerMethod]
        public static RequestState Reduce(RequestState state, ServerStreamingConnectionStopped action) =>
            state with
            {
                ServerStreamingRequests = state.ServerStreamingRequests.SetItem(action.RequestId,
                    state.ServerStreamingRequests[action.RequestId] with { Connected = false })
            };

        [ReducerMethod]
        public static RequestState Reduce(RequestState state, CallClientStreamingOperation action) =>
            state with
            {
                ClientStreamingRequests = state.ClientStreamingRequests.SetItem(action.RequestId, new ClientStreamingConnectionState(false, action.Headers, ImmutableList<GrpcRequest>.Empty, null))
            };

        [ReducerMethod]
        public static RequestState Reduce(RequestState state, MessageSentToClientStreamingOperation action) =>
            state with
            {
                ClientStreamingRequests = state.ClientStreamingRequests.TryGetValue(action.Request.RequestId, out var existing) 
                    ? state.ClientStreamingRequests.SetItem(action.Request.RequestId, existing with { Connected = true, Requests = existing.Requests.Add(action.Request) })
                    : state.ClientStreamingRequests.SetItem(action.Request.RequestId, new ClientStreamingConnectionState(true, new GrpcRequestHeaders(ImmutableDictionary<string, string>.Empty), ImmutableList<GrpcRequest>.Empty.Add(action.Request), null))
            };

        [ReducerMethod]
        public static RequestState Reduce(RequestState state, ClientStreamingResponseReceived action) =>
            state with
            {
                ClientStreamingRequests = state.ClientStreamingRequests.SetItem(action.Response.RequestId,
                    state.ClientStreamingRequests[action.Response.RequestId] with
                    {
                        Connected = false, Response = action.Response
                    })
            };

        [ReducerMethod]
        public static RequestState Reduce(RequestState state, OpenDuplexConnection action) =>
            state with
            {
                DuplexRequests =
                    state.DuplexRequests.TryGetValue(action.RequestId, out var existing)
                        ? state.DuplexRequests.SetItem(action.RequestId, existing with { Headers = action.Headers })
                        : state.DuplexRequests.SetItem(action.RequestId, new DuplexConnectionState(false, action.Headers, ImmutableList<GrpcRequest>.Empty, ImmutableList<GrpcResponse>.Empty))
            };

        [ReducerMethod]
        public static RequestState Reduce(RequestState state, DuplexConnectionOpened action) =>
            state with
            {
                DuplexRequests = 
                    state.DuplexRequests.TryGetValue(action.RequestId, out var existing)
                        ? state.DuplexRequests.SetItem(action.RequestId, existing with { Connected = true })
                        : state.DuplexRequests.SetItem(action.RequestId, new DuplexConnectionState(false, new GrpcRequestHeaders(ImmutableDictionary<string, string>.Empty), ImmutableList<GrpcRequest>.Empty, ImmutableList<GrpcResponse>.Empty))
            };

        [ReducerMethod]
        public static RequestState Reduce(RequestState state, DuplexConnectionStopped action) =>
            state with
            {
                DuplexRequests = state.DuplexRequests.SetItem(action.RequestId, state.DuplexRequests[action.RequestId] with { Connected = false })
            };

        [ReducerMethod]
        public static RequestState Reduce(RequestState state, MessageSentToDuplexOperation action) =>
            state with
            {
                DuplexRequests =
                    state.DuplexRequests.TryGetValue(action.Request.RequestId, out var existing)
                        ? state.DuplexRequests.SetItem(action.Request.RequestId, existing with { Connected = true, Requests = existing.Requests.Add(action.Request) })
                        : state.DuplexRequests.SetItem(action.Request.RequestId, new DuplexConnectionState(true, new GrpcRequestHeaders(ImmutableDictionary<string, string>.Empty), ImmutableList<GrpcRequest>.Empty.Add(action.Request), ImmutableList<GrpcResponse>.Empty))
            };

        [ReducerMethod]
        public static RequestState Reduce(RequestState state, DuplexResponseReceived action) =>
            state with
            {
                DuplexRequests =
                    state.DuplexRequests.TryGetValue(action.Response.RequestId, out var existing)
                        ? state.DuplexRequests.SetItem(action.Response.RequestId, existing with { Connected = true, Responses = existing.Responses.Add(action.Response) })
                        : state.DuplexRequests.SetItem(action.Response.RequestId, new DuplexConnectionState(true, new GrpcRequestHeaders(ImmutableDictionary<string, string>.Empty), ImmutableList<GrpcRequest>.Empty, ImmutableList<GrpcResponse>.Empty.Add(action.Response)))
            };

    }
}
