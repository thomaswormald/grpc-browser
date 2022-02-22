using System;
using System.Collections.Immutable;

namespace GrpcBrowser.Store.Services
{
    public record GrpcRequestId(Guid Value);

    public record GrpcRequest(DateTimeOffset TimeStamp, GrpcRequestId RequestId, Type RequestType, object RequestBody);

    public record GrpcResponse(DateTimeOffset TimeStamp, GrpcRequestId RequestId, Type ResponseType, object ResponseBody);

    public enum GrpcOperationType { Unary, ServerStreaming, ClientStreaming, Duplex }

    public record GrpcOperation(string Name, Type RequestType, Type ResponseType, GrpcOperationType Type);

    public enum GrpcServiceImplementationType { CodeFirst, ProtoFile }

    public record GrpcService(Type ServiceType, ImmutableDictionary<string, GrpcOperation> Endpoints, GrpcServiceImplementationType ImplementationType); 
    
    public record GrpcRequestHeaders(ImmutableDictionary<string, string> Values);
    
    public record GrpcResponseHeaders(ImmutableDictionary<string, string> Values);
}
