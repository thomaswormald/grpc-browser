using System;
using System.Collections.Immutable;

namespace GrpcBrowser.Store.Services
{
    public record GrpcRequestId(Guid Value);

    public record GrpcResponse(DateTimeOffset TimeStamp, GrpcRequestId RequestId, Type ResponseType, object Response);

    public enum GrpcOperationType { Unary, ServerStreaming, ClientStreaming, Duplex }

    public record GrpcOperation(string Name, Type RequestType, Type ResponseType, GrpcOperationType Type);

    public record CodeFirstGrpcService(string Name, ImmutableDictionary<string, GrpcOperation> Operations);

    public record ProtoFirstGrpcService(string Name, ImmutableDictionary<string, GrpcOperation> Operations);
}
