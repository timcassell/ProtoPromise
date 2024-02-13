using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

// AsyncMethodBuilderAttribute
#pragma warning disable 0436 // Type conflicts with imported type

namespace Proto.Promises.Linq
{
    /// <summary>
    /// Type used to create an <see cref="AsyncEnumerable{T}"/> with <see cref="AsyncEnumerable{T}.Create(Func{AsyncStreamWriter{T}, CancelationToken, AsyncIteratorMethod})"/>.
    /// </summary>
    /// <remarks>This type should not be used directly, but rather only as the return type for async iterator functions.
    /// Use the <see langword="async"/> keyword to enable the compiler to generate the async iterator state machine.</remarks>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    [AsyncMethodBuilder(typeof(CompilerServices.AsyncIteratorMethodBuilder))]
    public readonly struct AsyncIteratorMethod
    {
        internal readonly Promise _promise;

        [MethodImpl(Internal.InlineOption)]
        internal AsyncIteratorMethod(Promise promise)
            => _promise = promise;
    }
}