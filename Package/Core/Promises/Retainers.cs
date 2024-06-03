#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
# endif

using Proto.Promises.CompilerServices;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Proto.Promises
{
    partial struct Promise
    {
        /// <summary>
        /// Retains this <see cref="Promise"/> so that it can be awaited multiple times.
        /// </summary>
        /// <returns>A <see cref="Retainer"/> that contains the retained <see cref="Promise"/>.</returns>
        public Retainer GetRetainer()
        {
            ValidateOperation(1);
            var promiseRef = _ref;
            if (promiseRef == null)
            {
                return default;
            }
#if !PROMISE_DEBUG
            // In RELEASE mode, we can return a default retainer if this is already resolved.
            // In DEBUG mode, we always want to create a backing reference for validation purposes.
            if (promiseRef.State == State.Resolved)
            {
                promiseRef.MaybeMarkAwaitedAndDispose(_id);
                return default;
            }
#endif
            return new Retainer(Internal.PromiseRefBase.PromiseRetainer<Internal.VoidResult>.GetOrCreateAndHookup(promiseRef, _id));
        }

        /// <summary>
        /// Retains a <see cref="Promise"/> so that it may be awaited multiple times.
        /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        public readonly struct Retainer : IDisposable
        {
            private readonly Promise<Internal.VoidResult>.Retainer _impl;

            [MethodImpl(Internal.InlineOption)]
            internal Retainer(Internal.PromiseRefBase.PromiseRetainer<Internal.VoidResult> retainerRef)
            {
                _impl = new Promise<Internal.VoidResult>.Retainer(retainerRef, retainerRef.Id);
            }

            /// <summary>
            /// Returns a <see cref="Promise"/> that adopts the state of the retained <see cref="Promise"/>.
            /// </summary>
            public Promise WaitAsync()
                => _impl.WaitAsync();

            /// <summary>
            /// Asynchronous infrastructure support. This method permits instances of <see cref="Retainer"/> to be awaited.
            /// </summary>
            [MethodImpl(Internal.InlineOption), EditorBrowsable(EditorBrowsableState.Never)]
            public PromiseAwaiterVoid GetAwaiter()
                => WaitAsync().GetAwaiter();

            /// <summary>
            /// Releases the retained promise.
            /// </summary>
            public void Dispose()
                => _impl.Dispose();
        }
    }

    partial struct Promise<T>
    {
        /// <summary>
        /// Retains this <see cref="Promise{T}"/> so that it can be awaited multiple times.
        /// </summary>
        /// <returns>A <see cref="Retainer"/> that contains the retained <see cref="Promise{T}"/>.</returns>
        public Retainer GetRetainer()
        {
            ValidateOperation(1);
            var promiseRef = _ref;
            if (promiseRef == null)
            {
                return new Retainer(_result);
            }
#if !PROMISE_DEBUG
            // In RELEASE mode, we can return a retainer with only the result if this is already resolved.
            // In DEBUG mode, we always want to create a backing reference for validation purposes.
            if (promiseRef.State == Promise.State.Resolved)
            {
                var retainer = new Retainer(promiseRef._result);
                promiseRef.MaybeMarkAwaitedAndDispose(_id);
                return retainer;
            }
#endif
            var retainerRef = Internal.PromiseRefBase.PromiseRetainer<T>.GetOrCreateAndHookup(promiseRef, _id);
            return new Retainer(retainerRef, retainerRef.Id);
        }

        /// <summary>
        /// Retains a <see cref="Promise{T}"/> so that it may be awaited multiple times.
        /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        public readonly struct Retainer : IDisposable
        {
            private readonly Internal.PromiseRefBase.PromiseRetainer<T> _ref;
            private readonly T _result;
            private readonly short _id;

            [MethodImpl(Internal.InlineOption)]
            internal Retainer(Internal.PromiseRefBase.PromiseRetainer<T> retainerRef, short retainerId)
            {
                _ref = retainerRef;
                _result = default;
                _id = retainerId;
            }

            [MethodImpl(Internal.InlineOption)]
            internal Retainer(in T result)
            {
                _ref = default;
                _result = result;
                _id = 0;
            }

            /// <summary>
            /// Returns a <see cref="Promise{T}"/> that adopts the state of the retained <see cref="Promise{T}"/>.
            /// </summary>
            public Promise<T> WaitAsync()
            {
                var promiseRef = _ref;
                return promiseRef == null
                    ? Promise.Resolved(_result)
                    : promiseRef.WaitAsync(_id);
            }

            /// <summary>
            /// Asynchronous infrastructure support. This method permits instances of <see cref="Retainer"/> to be awaited.
            /// </summary>
            [MethodImpl(Internal.InlineOption), EditorBrowsable(EditorBrowsableState.Never)]
            public PromiseAwaiter<T> GetAwaiter()
                => WaitAsync().GetAwaiter();

            /// <summary>
            /// Releases the retained promise.
            /// </summary>
            public void Dispose()
                => _ref?.Dispose(_id);
        }
    }
}