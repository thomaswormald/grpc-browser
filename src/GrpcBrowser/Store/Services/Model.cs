﻿using System;
using System.Collections.Immutable;

namespace GrpcBrowser.Store.Services
{
    public record GrpcRequestId(Guid Value);

    public record GrpcResponse(DateTimeOffset TimeStamp, GrpcRequestId RequestId, Type ResponseType, object Response);

    public enum GrpcOperationType { Unary, ServerStreaming, ClientStreaming, Duplex }

    public record GrpcOperation(string Name, Type RequestType, Type ResponseType, GrpcOperationType Type);

    public enum GrpcServiceImplementationType { CodeFirst, ProtoFile }

    public record GrpcService(string Name, ImmutableDictionary<string, GrpcOperation> Endpoints, GrpcServiceImplementationType ImplementationType); 
    
    public record GrpcRequestHeaders(ImmutableDictionary<string, string> Values);
}
