#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Proto.Promises.Async.CompilerServices
{
    /// <summary>
    /// Provides an awaiter for switching to a context.
    /// </summary>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public readonly partial struct PromiseSwitchToContextAwaiter : ICriticalNotifyCompletion
    {
        private readonly SynchronizationContext _context;
        private readonly bool _forceAsync;

        /// <summary>
        /// Internal use.
        /// </summary>
        [MethodImpl(Internal.InlineOption)]
        internal PromiseSwitchToContextAwaiter(SynchronizationContext context, bool forceAsync)
        {
            _context = context;
            _forceAsync = forceAsync;
        }

        /// <summary>Gets the awaiter for this.</summary>
        /// <remarks>This method is intended for compiler use rather than use directly in code.</remarks>
        /// <returns>this</returns>
        [MethodImpl(Internal.InlineOption)]
        public PromiseSwitchToContextAwaiter GetAwaiter()
        {
            return this;
        }

        /// <summary>Gets whether the <see cref="Promise"/> being awaited is completed.</summary>
        /// <remarks>This property is intended for compiler use rather than use directly in code.</remarks>
        public bool IsCompleted
        {
            [MethodImpl(Internal.InlineOption)]
            get
            {
                return !_forceAsync & _context == Promise.Manager.ThreadStaticSynchronizationContext;
            }
        }

        /// <summary>Ends the await on the context.</summary>
        /// <remarks>This property is intended for compiler use rather than use directly in code.</remarks>
        [MethodImpl(Internal.InlineOption)]
        public void GetResult()
        {
            // Do nothing.
        }

        /// <summary>Schedules the continuation onto the context.</summary>
        /// <param name="continuation">The action to invoke when the await operation completes.</param>
        /// <remarks>This property is intended for compiler use rather than use directly in code.</remarks>
        [MethodImpl(Internal.InlineOption)]
        public void OnCompleted(Action continuation)
        {
            ValidateArgument(continuation, "continuation", 1);
            var context = _context;
            if (context == null)
            {
                ThreadPool.QueueUserWorkItem(state => state.UnsafeAs<Action>().Invoke(), continuation);
            }
            else
            {
                context.Post(state => state.UnsafeAs<Action>().Invoke(), continuation);
            }
        }

        /// <summary>Schedules the continuation onto the context.</summary>
        /// <param name="continuation">The action to invoke when the await operation completes.</param>
        /// <remarks>This property is intended for compiler use rather than use directly in code.</remarks>
        [MethodImpl(Internal.InlineOption)]
        public void UnsafeOnCompleted(Action continuation)
        {
            OnCompleted(continuation);
        }

        static partial void ValidateArgument<TArg>(TArg arg, string argName, int skipFrames);
#if PROMISE_DEBUG
        static partial void ValidateArgument<TArg>(TArg arg, string argName, int skipFrames)
        {
            Internal.ValidateArgument(arg, argName, skipFrames + 1);
        }
#endif
    } // struct PromiseSwitchToContextAwaiter
} // namespace Proto.Promises.Async.CompilerServices