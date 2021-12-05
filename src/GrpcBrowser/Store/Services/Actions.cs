using System.Collections.Immutable;

namespace GrpcBrowser.Store.Services
{
    public record SetCodeFirstServices(ImmutableList<CodeFirstGrpcService> Services);
    public record SetProtoFirstServices(ImmutableList<ProtoFirstGrpcService> Services);
}
