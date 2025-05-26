using System;
using System.Runtime.CompilerServices;

#pragma warning disable IDE0090 // Use 'new(...)'

namespace Proto.Promises.CompilerServices
{
    internal sealed class AsyncEnumerableDisposedException : Exception
    {
        // Unity runtimes have a bug where stack traces are continually appended to the exception, causing a memory leak and runtime slowdowns.
        // To avoid the issue, we only use a singleton in runtimes where the bug is not present.
#if PROMISE_DEBUG || !NETCOREAPP || UNITY_2018_3_OR_NEWER
        [MethodImpl(Internal.InlineOption)]
        internal static AsyncEnumerableDisposedException GetOrCreate() => new AsyncEnumerableDisposedException();
        
        [MethodImpl(Internal.InlineOption)]
        internal static bool Is(Exception exception) => exception is AsyncEnumerableDisposedException;
#else
        // We can use a singleton instance since we never care about the stack trace.
        private static readonly AsyncEnumerableDisposedException s_instance = new AsyncEnumerableDisposedException();

        [MethodImpl(Internal.InlineOption)]
        internal static AsyncEnumerableDisposedException GetOrCreate() => s_instance;

        [MethodImpl(Internal.InlineOption)]
        internal static bool Is(Exception exception) => exception == s_instance;
#endif

        private AsyncEnumerableDisposedException() : base("This is a special exception used for async enumerables. It should never be caught by user code!") { }

    }
}