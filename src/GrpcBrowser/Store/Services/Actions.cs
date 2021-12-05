using System.Collections.Immutable;

namespace GrpcBrowser.Store.Services
{
    public record SetServices(ImmutableList<GrpcService> Services);
}
