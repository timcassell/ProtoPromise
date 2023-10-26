using System;

#pragma warning disable IDE0090 // Use 'new(...)'

namespace Proto.Promises.Async.CompilerServices
{
#if NET47_OR_GREATER || NETSTANDARD2_1_OR_GREATER || NETCOREAPP || UNITY_2021_2_OR_NEWER
    internal sealed class AsyncEnumerableDisposedException : Exception
    {
        // We can use a singleton instance since we never care about the stack trace.
        internal static readonly AsyncEnumerableDisposedException s_instance = new AsyncEnumerableDisposedException();

        private AsyncEnumerableDisposedException() : base("This is a special exception used for async enumerables. It should never be caught by user code!") { }
    }
#endif
}