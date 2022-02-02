using ProtoBuf.Grpc.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Internal;

namespace GrpcBrowser.Infrastructure
{
    public static class GrpcClientHelpers
    {
        // This class is required because the GrpcClient methods that we need are generic, but we don't have the types available to call them

        public static Task<object> UnaryAsync(this GrpcClient client, object request, string operationName, Type requestType, Type responseType, CallContext callContext = default)
        {
            var methodInfo =
                typeof(GrpcClient).GetMethods()
                    .Where(method => method.Name == nameof(GrpcClient.UnaryAsync))
                    .Single(method => method.GetParameters()[1].ParameterType == typeof(string));

            if (responseType == typeof(Task))
            {
                // Handle case of Task with no return type
                responseType = typeof(Empty);
            }

            var genericMethodInfo = methodInfo.MakeGenericMethod(requestType, responseType);

            return genericMethodInfo.InvokeAsync(client, request, operationName, callContext);
        }

        public static IAsyncEnumerable<object> ServerStreamingAsync(this GrpcClient client, object request, string operationName, Type requestType, Type responseType, CallContext callContext = default)
        {
            var methodInfo =
                typeof(GrpcClient).GetMethods()
                    .Where(method => method.Name == nameof(GrpcClient.ServerStreamingAsync))
                    .Single(method => method.GetParameters()[1].ParameterType == typeof(string));

            var genericMethodInfo = methodInfo.MakeGenericMethod(requestType, responseType);

            var result = genericMethodInfo.Invoke(client, new[] { request, operationName, callContext });

            return (IAsyncEnumerable<object>)result;
        }

        public static Task ClientStreamingAsync(this GrpcClient client, IAsyncEnumerable<object> requestStream, string operationName, Type requestType, Type responseType, CallContext callContext = default)
        {
            var methodInfo =
                typeof(GrpcClient).GetMethods()
                    .Where(method => method.Name == nameof(GrpcClient.ClientStreamingAsync))
                    .Single(method => method.GetParameters()[1].ParameterType == typeof(string));

            var genericMethodInfo = methodInfo.MakeGenericMethod(requestType, responseType);

            var castingEnumerableType = typeof(CastingAsyncEnumerable<>).MakeGenericType(requestType);

            var requestStreamCorrectGeneric = Activator.CreateInstance(castingEnumerableType, requestStream, callContext.CancellationToken);

            return (Task)genericMethodInfo.Invoke(client, new object[] { requestStreamCorrectGeneric, operationName, callContext });
        }

        public static IAsyncEnumerable<object> DuplexAsync(this GrpcClient client, IAsyncEnumerable<object> requestStream, string operationName, Type requestType, Type responseType, CallContext callContext = default)
        {
            var methodInfo =
                typeof(GrpcClient).GetMethods()
                    .Where(method => method.Name == nameof(GrpcClient.DuplexStreamingAsync))
                    .Single(method => method.GetParameters()[1].ParameterType == typeof(string));

            var genericMethodInfo = methodInfo.MakeGenericMethod(requestType, responseType);

            var castingEnumerableType = typeof(CastingAsyncEnumerable<>).MakeGenericType(requestType);

            var requestStreamCorrectGeneric = Activator.CreateInstance(castingEnumerableType, requestStream, callContext.CancellationToken);

            return (IAsyncEnumerable<object>)genericMethodInfo.Invoke(client, new object[] { requestStreamCorrectGeneric, operationName, callContext });
        }

        private static async Task<object> InvokeAsync(this MethodInfo @this, object obj, params object[] parameters)
        {
            var task = (Task)@this.Invoke(obj, parameters);
            await task.ConfigureAwait(false);
            var resultProperty = task.GetType().GetProperty("Result");
            return resultProperty.GetValue(task);
        }
    }

    class CastingAsyncEnumerable<T> : IAsyncEnumerable<T>
    {
        private readonly IAsyncEnumerable<object> _inner;
        private readonly IAsyncEnumerator<T> _enumerator;

        public CastingAsyncEnumerable(IAsyncEnumerable<object> inner, CancellationToken cancellationToken)
        {
            _inner = inner;
            _enumerator = new CastingAsyncEnumerator<T>(_inner.GetAsyncEnumerator(cancellationToken));
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = new CancellationToken()) => _enumerator;
    }

    class CastingAsyncEnumerator<T> : IAsyncEnumerator<T>
    {
        private readonly IAsyncEnumerator<object> _inner;

        public CastingAsyncEnumerator(IAsyncEnumerator<object> inner)
        {
            _inner = inner;
        }

        public ValueTask DisposeAsync() => _inner.DisposeAsync();

        public ValueTask<bool> MoveNextAsync() => _inner.MoveNextAsync();

        public T Current => (T)_inner.Current;
    }
}
