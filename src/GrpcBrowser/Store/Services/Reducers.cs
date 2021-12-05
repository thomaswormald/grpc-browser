using Fluxor;
using System.Collections.Immutable;

namespace GrpcBrowser.Store.Services
{
    public static class Reducers
    {
        [ReducerMethod]
        public static ServicesState Reduce(ServicesState state, SetCodeFirstServices action) =>
            state with { CodeFirstServices = action.Services.ToImmutableDictionary(c => c.Name, c => c) };

        [ReducerMethod]
        public static ServicesState Reduce(ServicesState state, SetProtoFirstServices action) =>
            state with { ProtoFirstServices = action.Services.ToImmutableDictionary(c => c.Name, c => c) };
    }
}
