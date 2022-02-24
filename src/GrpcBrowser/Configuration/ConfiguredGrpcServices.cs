using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ServiceModel;
using ProtoBuf.Grpc.Configuration;

namespace GrpcBrowser.Configuration
{
    public record ConfiguredGrpcClient(Type Type, ImmutableList<IRequestInterceptor> Interceptors);
    public record CodeFirstGrpcServiceConfiguration(Type Type);
    public record ProtoFirstGrpcServiceConfiguration(Type Type);

    public class GrpcServiceOptions
    {
        public List<IRequestInterceptor> Interceptors { get; set; } = new List<IRequestInterceptor>();
    }

    internal static class ConfiguredGrpcServices
    {
        internal static ImmutableDictionary<string, ConfiguredGrpcClient> ProtoGrpcClients = ImmutableDictionary<string, ConfiguredGrpcClient>.Empty;
        internal static ImmutableDictionary<string, ConfiguredGrpcClient> CodeFirstGrpcServiceInterfaces = ImmutableDictionary<string, ConfiguredGrpcClient>.Empty;
        internal static ImmutableList<IRequestInterceptor> GlobalRequestInterceptors = ImmutableList<IRequestInterceptor>.Empty;
    }

    public static class GrpcBrowser
    {
        public static ProtoFirstGrpcServiceConfiguration AddProtoFirstService<TClient>(GrpcServiceOptions options = null) where TClient : Grpc.Core.ClientBase<TClient>
        {
            if (ConfiguredGrpcServices.ProtoGrpcClients.ContainsKey(typeof(TClient).FullName))
            {
                throw new ArgumentException($"A proto-first service with the type {typeof(TClient).Name} has already been configured");
            }

            ConfiguredGrpcServices.ProtoGrpcClients = ConfiguredGrpcServices.ProtoGrpcClients.Add(typeof(TClient).FullName, new ConfiguredGrpcClient(typeof(TClient), options.Interceptors.ToImmutableList()));

            return new ProtoFirstGrpcServiceConfiguration(typeof(TClient));
        }

        public static CodeFirstGrpcServiceConfiguration AddCodeFirstService<TServiceInterface>(GrpcServiceOptions options = null)
        {
            if (ConfiguredGrpcServices.CodeFirstGrpcServiceInterfaces.ContainsKey(typeof(TServiceInterface).FullName))
            {
                throw new ArgumentException($"A code-first service with interface {typeof(TServiceInterface).Name} has already been configured");
            }

            if (!typeof(TServiceInterface).IsInterface)
            {
                throw new ArgumentException(
                    $"To add your code-first service to GrpcBrowser, you must provide your GRPC service's interface. '{typeof(TServiceInterface).Name}' is not an interface");
            }

            if (!typeof(TServiceInterface).IsDefined(typeof(ServiceAttribute), false) &&
                !typeof(TServiceInterface).IsDefined(typeof(ServiceContractAttribute), false))
            {
                throw new ArgumentException(
                    $"To add your code-first service to GrpcBrowser, you must provide your GRPC service's interface. '{typeof(TServiceInterface).Name}' does not have the attribute '{nameof(ServiceAttribute)}' or '{nameof(ServiceContractAttribute)}', which is required for code-first GRPC services");
            }

            ConfiguredGrpcServices.CodeFirstGrpcServiceInterfaces = ConfiguredGrpcServices.CodeFirstGrpcServiceInterfaces.Add(typeof(TServiceInterface).FullName, new ConfiguredGrpcClient(typeof(TServiceInterface), options.Interceptors.ToImmutableList()));

            return new CodeFirstGrpcServiceConfiguration(typeof(TServiceInterface));
        }
    }
}
