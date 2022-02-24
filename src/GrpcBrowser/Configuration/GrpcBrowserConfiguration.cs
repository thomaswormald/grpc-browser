using GrpcBrowser.Configuration.RequestInterceptors;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace GrpcBrowser.Configuration
{
    public class GrpcBrowserOptions
    {
        public List<IRequestInterceptor> GlobalRequestInterceptors { get; set; } = new List<IRequestInterceptor>();
    }

    public record ConfiguredGrpcClient(Type Type, ImmutableList<IRequestInterceptor> Interceptors);

    public class GrpcServiceOptions
    {
        public List<IRequestInterceptor> Interceptors { get; set; } = new List<IRequestInterceptor>();
    }

    internal class GrpcBrowserConfiguration
    {
        internal static ImmutableDictionary<string, ConfiguredGrpcClient> ProtoGrpcClients = ImmutableDictionary<string, ConfiguredGrpcClient>.Empty;
        internal static ImmutableDictionary<string, ConfiguredGrpcClient> CodeFirstGrpcServiceInterfaces = ImmutableDictionary<string, ConfiguredGrpcClient>.Empty;
        internal static ImmutableList<IRequestInterceptor> GlobalRequestInterceptors = ImmutableList<IRequestInterceptor>.Empty;
    }
}
