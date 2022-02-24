using System;
using System.Collections.Generic;
using System.ServiceModel;
using ProtoBuf.Grpc.Configuration;

namespace GrpcBrowser.Configuration
{
    internal static class ConfiguredGrpcServices
    {
        internal static List<Type> ProtoGrpcClients = new List<Type>();
        internal static List<Type> CodeFirstGrpcServiceInterfaces = new List<Type>();
        internal static List<IRequestInterceptor> ProtoRequestInterceptors = new List<IRequestInterceptor>();
    }

    public static class GrpcBrowser
    {
        public static void AddProtoFirstService<TClient>() where TClient : Grpc.Core.ClientBase<TClient>
        {
            ConfiguredGrpcServices.ProtoGrpcClients.Add(typeof(TClient));
        }

        public static void AddCodeFirstService<TServiceInterface>()
        {
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

            ConfiguredGrpcServices.CodeFirstGrpcServiceInterfaces.Add(typeof(TServiceInterface));
        }
    }
}
