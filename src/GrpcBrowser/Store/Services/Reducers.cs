using Fluxor;
using System.Collections.Immutable;

namespace GrpcBrowser.Store.Services
{
    public static class Reducers
    {
        [ReducerMethod]
        public static ServicesState Reduce(ServicesState state, SetServices action) =>
            state with { Services = action.Services.ToImmutableDictionary(c => c.ServiceType.Name, c => c) };
    }
}
