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
                ClientStreamingRequests = state.ClientStreamingRequests.ContainsKey(action.Request.RequestId) 
                    ? state.ClientStreamingRequests.SetItem(action.Request.RequestId, state.ClientStreamingRequests[action.Request.RequestId] with { Connected = true, Requests = state.ClientStreamingRequests[action.Request.RequestId].Requests.Add(action.Request) })
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
        public static RequestState Reduce(RequestState state, DuplexConnectionOpened action) =>
            state with
            {
                DuplexRequests = state.DuplexRequests.SetItem(action.RequestId, new DuplexConnectionState(true, ImmutableList<GrpcResponse>.Empty))
            };

        [ReducerMethod]
        public static RequestState Reduce(RequestState state, DuplexConnectionStopped action) =>
            state with
            {
                DuplexRequests = state.DuplexRequests.SetItem(action.RequestId, state.DuplexRequests[action.RequestId] with { Connected = false })
            };

        [ReducerMethod]
        public static RequestState Reduce(RequestState state, MessageSentToDuplexOperation action)
        {
            if (state.DuplexRequests.TryGetValue(action.RequestId, out var existing))
            {
                return state with
                {
                    DuplexRequests = state.DuplexRequests.SetItem(action.RequestId, existing with { Connected = true })
                };
            }

            return state with
            {
                DuplexRequests = state.DuplexRequests.SetItem(action.RequestId,
                    new DuplexConnectionState(true, ImmutableList<GrpcResponse>.Empty))
            };
        }

        [ReducerMethod]
        public static RequestState Reduce(RequestState state, DuplexResponseReceived action)
        {
            var existingResponses =
                state.DuplexRequests.TryGetValue(action.Response.RequestId, out var existing)
                    ? existing
                    : new DuplexConnectionState(false, ImmutableList<GrpcResponse>.Empty);

            var updated = existingResponses with { Responses = existingResponses.Responses.Add(action.Response) };

            return state with
            {
                DuplexRequests = state.DuplexRequests.SetItem(action.Response.RequestId, updated)
            };
        }

    }
}
