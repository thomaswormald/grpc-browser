using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Net.Client;
using GrpcBrowser.Configuration;
using GrpcBrowser.Store.Services;
using Newtonsoft.Json;
using ProtoBuf.Grpc.Client;

namespace GrpcBrowser.Store.Requests.Effects
{
    internal static class GrpcUtils
    {
        internal static object? GetRequestParameter(string requestParamJson, Type requestType) => JsonConvert.DeserializeObject(requestParamJson, requestType);

        internal static CallOptions GetCallOptions(GrpcRequestHeaders requestHeaders, CancellationToken cancellationToken)
        {
            var headers = new Metadata();

            foreach (var header in requestHeaders.Values)
            {
                headers.Add(header.Key, header.Value);
            }

            return new CallOptions(headers, cancellationToken: cancellationToken);
        }

        internal static GrpcClient GetCodeFirstGrpcServiceClient(this GrpcChannel channel, string serviceName)
        {
            var clientType = ConfiguredGrpcServices.CodeFirstGrpcServiceInterfaces.Single(c => c.Name == serviceName);

            return channel.CreateGrpcService(clientType);
        }

        internal static object? GetProtoFileGrpcServiceClient(this GrpcChannel channel, string serviceName)
        {
            var clientType = ConfiguredGrpcServices.ProtoGrpcClients.Single(c => c.Name == serviceName);
            return Activator.CreateInstance(clientType, channel);
        }

        internal static object? InvokeGrpcOperation(this object client, GrpcOperation operation, object requestParameter, CallOptions options)
        {
            var method = client.GetType().GetMethod(operation.Name, new[] { operation.RequestType, typeof(CallOptions) });

            return method?.Invoke(client, new[] { requestParameter, options });
        }
    }
}
