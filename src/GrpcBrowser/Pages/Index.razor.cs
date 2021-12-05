using Fluxor;
using Grpc.Core;
using GrpcBrowser.Configuration;
using GrpcBrowser.Store.Services;
using Microsoft.AspNetCore.Components;
using ProtoBuf.Grpc;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;

namespace GrpcBrowser.Pages
{
    public partial class Index
    {
        [Inject] public IDispatcher? Dispatcher { get; set; }
        [Inject] public IState<ServicesState>? ServicesState { get; set; }

        record GrpcOperationMethod(MethodInfo Method, GrpcOperationType OperationType, Type RequestMessageType, Type ResponseMessageType);

        private static GrpcOperationMethod DetermineOperationTypeFromProtoFirstSeviceMethod(MethodInfo method)
        {
            // nameof(Generic<Param1, Param2>) does not add the back-tick and number of generic arguments to the name,
            // unlike typeof(Generic<Param1, Param2>).Name. But typeof is not a constant and can not be used in the below
            // switch statement
            const string duplexStreamingReturnTypeName = nameof(AsyncDuplexStreamingCall<object, object>) + "`2";
            const string clientStreamingReturnTypeName = nameof(AsyncClientStreamingCall<object, object>) + "`2";
            const string serverStreamingReturnTypeName = nameof(AsyncServerStreamingCall<object>) + "`1";

            var type = method.ReturnType.Name switch
            {
                duplexStreamingReturnTypeName => GrpcOperationType.Duplex,
                clientStreamingReturnTypeName => GrpcOperationType.ClientStreaming,
                serverStreamingReturnTypeName => GrpcOperationType.ServerStreaming,
                _ => GrpcOperationType.Unary,
            };

            var (requestMessageType, responseMessageType) = type switch
            {
                GrpcOperationType.Duplex => (method.ReturnType.GetGenericArguments()[0], method.ReturnType.GetGenericArguments()[1]),
                GrpcOperationType.ClientStreaming => (method.ReturnType.GetGenericArguments()[0], method.ReturnType.GetGenericArguments()[1]),
                GrpcOperationType.ServerStreaming => (method.GetParameters()[0].ParameterType, method.ReturnType.GetGenericArguments()[0]),
                GrpcOperationType.Unary => (method.GetParameters()[0].ParameterType, method.ReturnType.IsGenericType ? method.ReturnType.GetGenericArguments()[0] : method.ReturnType),
            };

            return new GrpcOperationMethod(method, type, requestMessageType, responseMessageType);
        }

        private static GrpcOperationMethod DetermineOperationFromCodeFirstService(MethodInfo method)
        {
            var returnsStream = method.ReturnType.Name == typeof(IAsyncEnumerable<object>).Name;
            var streamParameter = method.GetParameters().Any(p => p.ParameterType.Name == typeof(IAsyncEnumerable<object>).Name);

            var type = (returnsStream, streamParameter) switch
            {
                (true, true) => GrpcOperationType.Duplex,
                (true, false) => GrpcOperationType.ServerStreaming,
                (false, true) => GrpcOperationType.ClientStreaming,
                (false, false) => GrpcOperationType.Unary
            };

            var (requestMessageType, responseMessageType) = type switch
            {
                GrpcOperationType.Duplex => (method.GetParameters()[0].ParameterType.GetGenericArguments()[0], method.ReturnType.GetGenericArguments()[0]),
                GrpcOperationType.ClientStreaming => (method.GetParameters()[0].ParameterType.GetGenericArguments()[0], method.ReturnType.GetGenericArguments()[0]),
                GrpcOperationType.ServerStreaming => (method.GetParameters()[0].ParameterType, method.ReturnType.GetGenericArguments()[0]),
                GrpcOperationType.Unary => (method.GetParameters()[0].ParameterType, method.ReturnType.IsGenericType ? method.ReturnType.GetGenericArguments()[0] : method.ReturnType),
            };

            return new GrpcOperationMethod(method, type, requestMessageType, responseMessageType);
        }

        private ProtoFirstGrpcService ProtoFirstServiceToGrpcService(Type serviceType)
        {
            var methods =
                serviceType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                    .Where(m => !m.IsConstructor)
                    .Where(m => m.GetParameters().Length <= 2)
                    .Where(m => m.GetParameters()[0].ParameterType == typeof(CallOptions) ||
                                (m.GetParameters().Length == 2 && m.GetParameters()[1].ParameterType == typeof(CallOptions)))
                    .Select(DetermineOperationTypeFromProtoFirstSeviceMethod)
                    .GroupBy(method => method.Method.Name)
                    .Select(method => method.First())
                    .Select(operationMethod => new GrpcOperation(operationMethod.Method.Name, operationMethod.RequestMessageType, operationMethod.ResponseMessageType, operationMethod.OperationType))
                    .ToImmutableDictionary(k => k.Name, v => v);

            return new ProtoFirstGrpcService(serviceType.Name, methods);
        }

        private CodeFirstGrpcService CodeFirstServiceToGrpcService(Type serviceType)
        {
            var methods =
                serviceType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                    .Where(m => !m.IsConstructor)
                    .Where(m => m.GetParameters().Length == 2)
                    .Where(m => m.GetParameters()[1].ParameterType == typeof(CallContext))
                    .Select(DetermineOperationFromCodeFirstService)
                    .GroupBy(method => method.Method.Name)
                    .Select(method => method.First())
                    .Select(operationMethod => new GrpcOperation(operationMethod.Method.Name, operationMethod.RequestMessageType, operationMethod.ResponseMessageType, operationMethod.OperationType))
                    .ToImmutableDictionary(k => k.Name, v => v);

            return new CodeFirstGrpcService(serviceType.Name, methods);
        }

        protected override void OnParametersSet()
        {
            var protoFileGrpcServices = ConfiguredGrpcServices.ProtoGrpcClients.Select(ProtoFirstServiceToGrpcService).ToImmutableList();
            Dispatcher?.Dispatch(new SetProtoFirstServices(protoFileGrpcServices));

            var codeFirstGrpcServices = ConfiguredGrpcServices.CodeFirstGrpcServiceInterfaces.Select(CodeFirstServiceToGrpcService).ToImmutableList();
            Dispatcher?.Dispatch(new SetCodeFirstServices(codeFirstGrpcServices));

            base.OnParametersSet();
        }
    }
}
