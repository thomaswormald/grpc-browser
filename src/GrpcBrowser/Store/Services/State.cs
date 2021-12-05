using Fluxor;
using System.Collections.Immutable;

namespace GrpcBrowser.Store.Services
{
    public record ServicesState(ImmutableDictionary<string, CodeFirstGrpcService> CodeFirstServices, ImmutableDictionary<string, ProtoFirstGrpcService> ProtoFirstServices);

    public class Feature : Feature<ServicesState>
    {
        public override string GetName() => "Services";

        protected override ServicesState GetInitialState() => new ServicesState(ImmutableDictionary<string, CodeFirstGrpcService>.Empty, ImmutableDictionary<string, ProtoFirstGrpcService>.Empty);
    }
}
