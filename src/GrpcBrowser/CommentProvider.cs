using GrpcBrowser.Store.Services;
using System;
using Namotion.Reflection;

using System.Reflection;

namespace GrpcBrowser
{
    public static class CommentProvider
    {
        public static string GetServiceDescription(GrpcService service)
        {
            if (service.ImplementationType == GrpcServiceImplementationType.CodeFirst)
            {
                return service.ServiceType.GetXmlDocsSummary();
            }
            else if (service.ImplementationType == GrpcServiceImplementationType.ProtoFile)
            {
                // The comment for the service gets put on the parent class of the client
                return service.ServiceType.DeclaringType.GetXmlDocsSummary();
            }
            else
            {
                return string.Empty;
            }
        }

        public static string GetOperationDescription(GrpcOperation operation, GrpcService service)
        {
            MethodInfo? methodInfo = null;

            if (service.ImplementationType == GrpcServiceImplementationType.CodeFirst)
            {
                try
                {
                    methodInfo = service.ServiceType.GetMethod(operation.Name);
                }
                catch (AmbiguousMatchException)
                {
                    // ToDo handle this when multiple methods with the same name exist
                }
            }
            else if (service.ImplementationType == GrpcServiceImplementationType.ProtoFile)
            {
                methodInfo =
                    service.ServiceType.GetMethod(operation.Name, new Type[] { typeof(Grpc.Core.CallOptions) })
                    ?? service.ServiceType.GetMethod(operation.Name, new Type[] { operation.RequestType, typeof(Grpc.Core.CallOptions) });
            }    

            if (methodInfo is null)
            {
                return string.Empty;
            }

            var docs = methodInfo.GetXmlDocsSummary();

            return docs;
        }
    }
}
