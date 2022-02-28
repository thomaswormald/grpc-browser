using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace GrpcBrowser.Configuration
{
    public class GrpcBrowserOptions
    {
    }

    public record ConfiguredGrpcClient(Type Type);

    public class GrpcServiceOptions
    {
    }

    internal class GrpcBrowserConfiguration
    {
        internal static ImmutableDictionary<string, ConfiguredGrpcClient> ProtoGrpcClients = ImmutableDictionary<string, ConfiguredGrpcClient>.Empty;
        internal static ImmutableDictionary<string, ConfiguredGrpcClient> CodeFirstGrpcServiceInterfaces = ImmutableDictionary<string, ConfiguredGrpcClient>.Empty;
    }
}
