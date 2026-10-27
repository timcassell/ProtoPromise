#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Proto.Promises.CompilerServices
{
#if !NETCOREAPP
    partial struct PromiseSwitchToContextAwaiter
    {
        // Fix for IL2CPP not invoking the static constructor.
#if ENABLE_IL2CPP
        [MethodImpl(Internal.InlineOption)]
        static partial void CreateOverride()
#else
        static PromiseSwitchToContextAwaiter()
#endif
            => Internal.AwaitOverriderImpl<PromiseSwitchToContextAwaiter>.Create();
    }
#endif

    /// <summary>
    /// Provides an awaiter for switching to a context.
    /// </summary>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public readonly partial struct PromiseSwitchToContextAwaiter : ICriticalNotifyCompletion, Internal.IPromiseAwareAwaiter
    {
        private readonly SynchronizationContext _context;
        private readonly bool _forceAsync;

        [MethodImpl(Internal.InlineOption)]
        internal PromiseSwitchToContextAwaiter(SynchronizationContext context, bool forceAsync)
        {
            _context = context;
            _forceAsync = forceAsync;
            CreateOverride();
        }

        static partial void CreateOverride();

        /// <summary>Gets the awaiter for this.</summary>
        /// <remarks>This method is intended for compiler use rather than use directly in code.</remarks>
        /// <returns>this</returns>
        [MethodImpl(Internal.InlineOption)]
        public PromiseSwitchToContextAwaiter GetAwaiter()
            => this;

        /// <summary>Gets whether the <see cref="Promise"/> being awaited is completed.</summary>
        /// <remarks>This property is intended for compiler use rather than use directly in code.</remarks>
        public bool IsCompleted
        {
            [MethodImpl(Internal.InlineOption)]
            get
            {
                if (_forceAsync)
                {
                    return false;
                }
                var context = _context;
                return context is null ? Thread.CurrentThread.IsThreadPoolThread : context == Promise.Manager.ThreadStaticSynchronizationContext;
            }
        }

        /// <summary>Ends the await on the context.</summary>
        /// <remarks>This property is intended for compiler use rather than use directly in code.</remarks>
        [MethodImpl(Internal.InlineOption)]
        // Do nothing.
        public void GetResult() { }

        /// <summary>Schedules the continuation onto the context.</summary>
        /// <param name="continuation">The action to invoke when the await operation completes.</param>
        /// <remarks>This property is intended for compiler use rather than use directly in code.</remarks>
        [MethodImpl(Internal.InlineOption)]
        public void OnCompleted(Action continuation)
        {
            ValidateArgument(continuation, nameof(continuation));
            // It's scheduled where the awaiting function awaits, so it doesn't capture where it was scheduled, which is where it awaited.
            // Only async Promise functions capture that (see Promise.Config.AwaitTracesEnabled).
            Internal.ScheduleContextCallbackDirect(_context ?? Internal.BackgroundSynchronizationContextSentinel.s_instance, continuation,
                state => state.UnsafeAs<Action>().Invoke(),
                state => state.UnsafeAs<Action>().Invoke());
        }

        /// <summary>Schedules the continuation onto the context.</summary>
        /// <param name="continuation">The action to invoke when the await operation completes.</param>
        /// <remarks>This property is intended for compiler use rather than use directly in code.</remarks>
        [MethodImpl(Internal.InlineOption)]
        public void UnsafeOnCompleted(Action continuation)
            => OnCompleted(continuation);

        // Async promises don't capture where they were scheduled either (see AsyncPromiseRef.ContinueOnContext).
        [MethodImpl(Internal.InlineOption)]
        void Internal.IPromiseAwareAwaiter.AwaitOnCompletedInternal(Internal.PromiseRefBase asyncPromiseRef)
            => asyncPromiseRef.SwitchToContext(_context ?? Internal.BackgroundSynchronizationContextSentinel.s_instance);

        static partial void ValidateArgument<TArg>(TArg arg, string argName);
#if PROMISE_DEBUG
        static partial void ValidateArgument<TArg>(TArg arg, string argName)
            => Internal.ValidateArgument(arg, argName);
#endif
    } // struct PromiseSwitchToContextAwaiter
} // namespace Proto.Promises.CompilerServices