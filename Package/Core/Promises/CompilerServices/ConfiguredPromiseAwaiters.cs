#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Proto.Promises.CompilerServices
{
#if !NETCOREAPP
    partial struct ConfiguredPromiseAwaiterVoid
    {
        // Fix for IL2CPP not invoking the static constructor.
#if ENABLE_IL2CPP
        [MethodImpl(Internal.InlineOption)]
        static partial void CreateOverride()
#else
        static ConfiguredPromiseAwaiterVoid()
#endif
            => Internal.AwaitOverriderImpl<ConfiguredPromiseAwaiterVoid>.Create();
    }

    partial struct ConfiguredPromiseAwaiter<T>
    {
        // Fix for IL2CPP not invoking the static constructor.
#if ENABLE_IL2CPP
        [MethodImpl(Internal.InlineOption)]
        static partial void CreateOverride()
#else
        static ConfiguredPromiseAwaiter()
#endif
            => Internal.AwaitOverriderImpl<ConfiguredPromiseAwaiter<T>>.Create();
    }

    partial struct ConfiguredPromiseNoThrowAwaiterVoid
    {
        // Fix for IL2CPP not invoking the static constructor.
#if ENABLE_IL2CPP
        [MethodImpl(Internal.InlineOption)]
        static partial void CreateOverride()
#else
        static ConfiguredPromiseNoThrowAwaiterVoid()
#endif
            => Internal.AwaitOverriderImpl<ConfiguredPromiseNoThrowAwaiterVoid>.Create();
    }

    partial struct ConfiguredPromiseNoThrowAwaiter<T>
    {
        // Fix for IL2CPP not invoking the static constructor.
#if ENABLE_IL2CPP
        [MethodImpl(Internal.InlineOption)]
        static partial void CreateOverride()
#else
        static ConfiguredPromiseNoThrowAwaiter()
#endif
            => Internal.AwaitOverriderImpl<ConfiguredPromiseNoThrowAwaiter<T>>.Create();
    }
#endif // !NETCOREAPP

    /// <summary>
    /// Provides an awaiter type that enables configured awaits on a <see cref="Promise"/>.
    /// </summary>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public readonly partial struct ConfiguredPromiseAwaiterVoid : ICriticalNotifyCompletion, Internal.IPromiseAwaiter
    {
        private readonly Promise _promise;
        private readonly ContinuationOptions _continuationOptions;

        [MethodImpl(Internal.InlineOption)]
        internal ConfiguredPromiseAwaiterVoid(Promise promise, ContinuationOptions continuationOptions)
        {
            _promise = promise;
            _continuationOptions = continuationOptions;
            CreateOverride();
        }

        static partial void CreateOverride();

        /// <summary>Gets the awaiter for this.</summary>
        /// <remarks>This method is intended for compiler use rather than use directly in code.</remarks>
        /// <returns>this</returns>
        [MethodImpl(Internal.InlineOption)]
        public ConfiguredPromiseAwaiterVoid GetAwaiter()
            => this;

        /// <summary>Gets whether the <see cref="Promise"/> being awaited is completed and this is configured to continue immediately.</summary>
        /// <remarks>This property is intended for compiler use rather than use directly in code.</remarks>
        /// <exception cref="InvalidOperationException">The <see cref="Promise"/> has already been awaited or forgotten.</exception>
        public bool IsCompleted
            => _continuationOptions.GetShouldContinueImmediately()
            && _promise._ref?.GetIsCompleted(_promise._id) != false;

        /// <summary>Ends the await on the completed <see cref="Promise"/>.</summary>
        /// <remarks>This property is intended for compiler use rather than use directly in code.</remarks>
        /// <exception cref="InvalidOperationException">The <see cref="Promise"/> has already been awaited or forgotten, or it has not yet completed.</exception>
        [MethodImpl(Internal.InlineOption)]
        public void GetResult()
        {
            var _ref = _promise._ref;
            if (_ref == null)
            {
                return;
            }
            var state = _ref.State;
            if (state == Promise.State.Resolved)
            {
                _ref.GetResultForAwaiterVoid(_promise._id);
                return;
            }
            _ref.GetExceptionDispatchInfo(state, _promise._id).Throw();
        }

        /// <summary>Schedules the configured continuation onto the <see cref="Promise"/> associated with this <see cref="ConfiguredPromiseAwaiterVoid"/>.</summary>
        /// <param name="continuation">The action to invoke when the await operation completes.</param>
        /// <remarks>This property is intended for compiler use rather than use directly in code.</remarks>
        /// <exception cref="InvalidOperationException">The <see cref="Promise"/> has already been awaited or forgotten.</exception>
        [MethodImpl(Internal.InlineOption)]
        public void OnCompleted(Action continuation)
        {
            ValidateArgument(continuation, nameof(continuation), 1);
            var _ref = _promise._ref;
            if (_ref != null)
            {
                _ref.OnCompleted(continuation, _continuationOptions.GetContinuationContext(), _promise._id);
                return;
            }
            Internal.ScheduleContextCallback(_continuationOptions.GetContinuationContext(), continuation,
                obj => obj.UnsafeAs<Action>().Invoke(),
                obj => obj.UnsafeAs<Action>().Invoke()
            );
        }

        /// <summary>Schedules the configured continuation onto the <see cref="Promise"/> associated with this <see cref="ConfiguredPromiseAwaiterVoid"/>.</summary>
        /// <param name="continuation">The action to invoke when the await operation completes.</param>
        /// <remarks>This property is intended for compiler use rather than use directly in code.</remarks>
        /// <exception cref="InvalidOperationException">The <see cref="Promise"/> has already been awaited or forgotten.</exception>
        [MethodImpl(Internal.InlineOption)]
        public void UnsafeOnCompleted(Action continuation)
            => OnCompleted(continuation);

        [MethodImpl(Internal.InlineOption)]
        void Internal.IPromiseAwaiter.AwaitOnCompletedInternal(Internal.PromiseRefBase asyncPromiseRef)
            => Internal.PromiseRefBase.ConfiguredAsyncPromiseContinuer.ConfigureAsyncContinuation(asyncPromiseRef, _continuationOptions.GetContinuationContext(), _promise._ref, _promise._id);

        static partial void ValidateArgument<TArg>(TArg arg, string argName, int skipFrames);
#if PROMISE_DEBUG
        static partial void ValidateArgument<TArg>(TArg arg, string argName, int skipFrames)
            => Internal.ValidateArgument(arg, argName, skipFrames + 1);
#endif
    } // struct ConfiguredPromiseAwaiterVoid

    /// <summary>
    /// Provides an awaiter type that enables configured awaits on a <see cref="Promise{T}"/>.
    /// </summary>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public readonly partial struct ConfiguredPromiseAwaiter<T> : ICriticalNotifyCompletion, Internal.IPromiseAwaiter
    {
        private readonly Promise<T> _promise;
        private readonly ContinuationOptions _continuationOptions;

        [MethodImpl(Internal.InlineOption)]
        internal ConfiguredPromiseAwaiter(in Promise<T> promise, ContinuationOptions continuationOptions)
        {
            _promise = promise;
            _continuationOptions = continuationOptions;
            CreateOverride();
        }

        static partial void CreateOverride();

        /// <summary>Gets the awaiter for this.</summary>
        /// <remarks>This method is intended for compiler use rather than use directly in code.</remarks>
        /// <returns>this</returns>
        [MethodImpl(Internal.InlineOption)]
        public ConfiguredPromiseAwaiter<T> GetAwaiter()
            => this;

        /// <summary>Gets whether the <see cref="Promise{T}"/> being awaited is completed and this is configured to continue immediately.</summary>
        /// <remarks>This property is intended for compiler use rather than use directly in code.</remarks>
        /// <exception cref="InvalidOperationException">The <see cref="Promise{T}"/> has already been awaited or forgotten.</exception>
        public bool IsCompleted
            => _continuationOptions.GetShouldContinueImmediately()
            && _promise._ref?.GetIsCompleted(_promise._id) != false;

        /// <summary>Ends the await on the completed <see cref="Promise{T}"/>.</summary>
        /// <returns>The result of the completed <see cref="Promise{T}"/></returns>
        /// <remarks>This property is intended for compiler use rather than use directly in code.</remarks>
        /// <exception cref="InvalidOperationException">The <see cref="Promise{T}"/> has already been awaited or forgotten, or it has not yet completed.</exception>
        [MethodImpl(Internal.InlineOption)]
        public T GetResult()
        {
            var _ref = _promise._ref;
            if (_ref == null)
            {
                return _promise._result;
            }
            var state = _ref.State;
            if (state == Promise.State.Resolved)
            {
                return _ref.GetResultForAwaiter(_promise._id);
            }
            _ref.GetExceptionDispatchInfo(state, _promise._id).Throw();
            throw null; // This will never be reached, but the compiler needs help understanding that.
        }

        /// <summary>Schedules the configured continuation onto the <see cref="Promise{T}"/> associated with this <see cref="ConfiguredPromiseAwaiter{T}"/>.</summary>
        /// <param name="continuation">The action to invoke when the await operation completes.</param>
        /// <remarks>This property is intended for compiler use rather than use directly in code.</remarks>
        /// <exception cref="InvalidOperationException">The <see cref="Promise{T}"/> has already been awaited or forgotten.</exception>
        [MethodImpl(Internal.InlineOption)]
        public void OnCompleted(Action continuation)
        {
            ValidateArgument(continuation, nameof(continuation), 1);
            var _ref = _promise._ref;
            if (_ref != null)
            {
                _ref.OnCompleted(continuation, _continuationOptions.GetContinuationContext(), _promise._id);
                return;
            }
            Internal.ScheduleContextCallback(_continuationOptions.GetContinuationContext(), continuation,
                obj => obj.UnsafeAs<Action>().Invoke(),
                obj => obj.UnsafeAs<Action>().Invoke()
            );
        }

        /// <summary>Schedules the configured continuation onto the <see cref="Promise{T}"/> associated with this <see cref="ConfiguredPromiseAwaiter{T}"/>.</summary>
        /// <param name="continuation">The action to invoke when the await operation completes.</param>
        /// <remarks>This property is intended for compiler use rather than use directly in code.</remarks>
        /// <exception cref="InvalidOperationException">The <see cref="Promise{T}"/> has already been awaited or forgotten.</exception>
        [MethodImpl(Internal.InlineOption)]
        public void UnsafeOnCompleted(Action continuation)
            => OnCompleted(continuation);

        [MethodImpl(Internal.InlineOption)]
        void Internal.IPromiseAwaiter.AwaitOnCompletedInternal(Internal.PromiseRefBase asyncPromiseRef)
            => Internal.PromiseRefBase.ConfiguredAsyncPromiseContinuer.ConfigureAsyncContinuation(asyncPromiseRef, _continuationOptions.GetContinuationContext(), _promise._ref, _promise._id);

        static partial void ValidateArgument<TArg>(TArg arg, string argName, int skipFrames);
#if PROMISE_DEBUG
        static partial void ValidateArgument<TArg>(TArg arg, string argName, int skipFrames)
            => Internal.ValidateArgument(arg, argName, skipFrames + 1);
#endif
    } // struct ConfiguredPromiseAwaiter<T>

    /// <summary>
    /// Provides an awaiter type that enables configured awaits on a <see cref="Promise"/>, without throwing.
    /// </summary>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public readonly partial struct ConfiguredPromiseNoThrowAwaiterVoid : ICriticalNotifyCompletion, Internal.IPromiseAwaiter
    {
        private readonly Promise _promise;
        private readonly ContinuationOptions _continuationOptions;

        [MethodImpl(Internal.InlineOption)]
        internal ConfiguredPromiseNoThrowAwaiterVoid(in Promise promise, ContinuationOptions continuationOptions)
        {
            _promise = promise;
            _continuationOptions = continuationOptions;
            CreateOverride();
        }

        static partial void CreateOverride();

        /// <summary>Gets the awaiter for this.</summary>
        /// <remarks>This method is intended for compiler use rather than use directly in code.</remarks>
        /// <returns>this</returns>
        [MethodImpl(Internal.InlineOption)]
        public ConfiguredPromiseNoThrowAwaiterVoid GetAwaiter()
            => this;

        /// <summary>Gets whether the <see cref="Promise"/> being awaited is completed and this is configured to continue immediately.</summary>
        /// <remarks>This property is intended for compiler use rather than use directly in code.</remarks>
        /// <exception cref="InvalidOperationException">The <see cref="Promise"/> has already been awaited or forgotten.</exception>
        public bool IsCompleted
            => _continuationOptions.GetShouldContinueImmediately()
            && _promise._ref?.GetIsCompleted(_promise._id) != false;

        /// <summary>Ends the await on the completed <see cref="Promise"/>.</summary>
        /// <returns>A <see cref="Promise.ResultContainer"/> that wraps the completion state and reason of the <see cref="Promise"/>.</returns>
        /// <remarks>This property is intended for compiler use rather than use directly in code.</remarks>
        /// <exception cref="InvalidOperationException">The <see cref="Promise"/> has already been awaited or forgotten, or it has not yet completed.</exception>
        [MethodImpl(Internal.InlineOption)]
        public Promise.ResultContainer GetResult()
        {
            var _ref = _promise._ref;
            return _ref == null
                ? new Promise.ResultContainer(null, Promise.State.Resolved)
                : _ref.GetResultContainerAndMaybeDispose(_promise._id);
        }

        /// <summary>Schedules the configured continuation onto the <see cref="Promise"/> associated with this <see cref="ConfiguredPromiseNoThrowAwaiterVoid"/>.</summary>
        /// <param name="continuation">The action to invoke when the await operation completes.</param>
        /// <remarks>This property is intended for compiler use rather than use directly in code.</remarks>
        /// <exception cref="InvalidOperationException">The <see cref="Promise"/> has already been awaited or forgotten.</exception>
        [MethodImpl(Internal.InlineOption)]
        public void OnCompleted(Action continuation)
        {
            ValidateArgument(continuation, nameof(continuation), 1);
            var _ref = _promise._ref;
            if (_ref != null)
            {
                _ref.OnCompleted(continuation, _continuationOptions.GetContinuationContext(), _promise._id);
                return;
            }
            Internal.ScheduleContextCallback(_continuationOptions.GetContinuationContext(), continuation,
                obj => obj.UnsafeAs<Action>().Invoke(),
                obj => obj.UnsafeAs<Action>().Invoke()
            );
        }

        /// <summary>Schedules the configured continuation onto the <see cref="Promise"/> associated with this <see cref="ConfiguredPromiseNoThrowAwaiterVoid"/>.</summary>
        /// <param name="continuation">The action to invoke when the await operation completes.</param>
        /// <remarks>This property is intended for compiler use rather than use directly in code.</remarks>
        /// <exception cref="InvalidOperationException">The <see cref="Promise"/> has already been awaited or forgotten.</exception>
        [MethodImpl(Internal.InlineOption)]
        public void UnsafeOnCompleted(Action continuation)
            => OnCompleted(continuation);

        [MethodImpl(Internal.InlineOption)]
        void Internal.IPromiseAwaiter.AwaitOnCompletedInternal(Internal.PromiseRefBase asyncPromiseRef)
            => Internal.PromiseRefBase.ConfiguredAsyncPromiseContinuer.ConfigureAsyncContinuation(asyncPromiseRef, _continuationOptions.GetContinuationContext(), _promise._ref, _promise._id);

        static partial void ValidateArgument<TArg>(TArg arg, string argName, int skipFrames);
#if PROMISE_DEBUG
        static partial void ValidateArgument<TArg>(TArg arg, string argName, int skipFrames)
            => Internal.ValidateArgument(arg, argName, skipFrames + 1);
#endif
    } // struct ConfiguredPromiseNoThrowAwaiterVoid

    /// <summary>
    /// Provides an awaiter type that enables configured awaits on a <see cref="Promise{T}"/>, without throwing.
    /// </summary>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public readonly partial struct ConfiguredPromiseNoThrowAwaiter<T> : ICriticalNotifyCompletion, Internal.IPromiseAwaiter
    {
        private readonly Promise<T> _promise;
        private readonly ContinuationOptions _continuationOptions;

        [MethodImpl(Internal.InlineOption)]
        internal ConfiguredPromiseNoThrowAwaiter(in Promise<T> promise, ContinuationOptions continuationOptions)
        {
            _promise = promise;
            _continuationOptions = continuationOptions;
            CreateOverride();
        }

        static partial void CreateOverride();

        /// <summary>Gets the awaiter for this.</summary>
        /// <remarks>This method is intended for compiler use rather than use directly in code.</remarks>
        /// <returns>this</returns>
        [MethodImpl(Internal.InlineOption)]
        public ConfiguredPromiseNoThrowAwaiter<T> GetAwaiter()
            => this;

        /// <summary>Gets whether the <see cref="Promise{T}"/> being awaited is completed and this is configured to continue immediately.</summary>
        /// <remarks>This property is intended for compiler use rather than use directly in code.</remarks>
        /// <exception cref="InvalidOperationException">The <see cref="Promise{T}"/> has already been awaited or forgotten.</exception>
        public bool IsCompleted
            => _continuationOptions.GetShouldContinueImmediately()
            && _promise._ref?.GetIsCompleted(_promise._id) != false;

        /// <summary>Ends the await on the completed <see cref="Promise{T}"/>.</summary>
        /// <returns>A <see cref="Promise{T}.ResultContainer"/> that wraps the completion state and result or reason of the <see cref="Promise{T}"/>.</returns>
        /// <remarks>This property is intended for compiler use rather than use directly in code.</remarks>
        /// <exception cref="InvalidOperationException">The <see cref="Promise{T}"/> has already been awaited or forgotten, or it has not yet completed.</exception>
        [MethodImpl(Internal.InlineOption)]
        public Promise<T>.ResultContainer GetResult()
        {
            var _ref = _promise._ref;
            return _ref == null
                ? new Promise<T>.ResultContainer(_promise._result, null, Promise.State.Resolved)
                : _ref.GetResultContainerAndMaybeDispose(_promise._id);
        }

        /// <summary>Schedules the configured continuation onto the <see cref="Promise{T}"/> associated with this <see cref="ConfiguredPromiseNoThrowAwaiter{T}"/>.</summary>
        /// <param name="continuation">The action to invoke when the await operation completes.</param>
        /// <remarks>This property is intended for compiler use rather than use directly in code.</remarks>
        /// <exception cref="InvalidOperationException">The <see cref="Promise{T}"/> has already been awaited or forgotten.</exception>
        [MethodImpl(Internal.InlineOption)]
        public void OnCompleted(Action continuation)
        {
            ValidateArgument(continuation, nameof(continuation), 1);
            var _ref = _promise._ref;
            if (_ref != null)
            {
                _ref.OnCompleted(continuation, _continuationOptions.GetContinuationContext(), _promise._id);
                return;
            }
            Internal.ScheduleContextCallback(_continuationOptions.GetContinuationContext(), continuation,
                obj => obj.UnsafeAs<Action>().Invoke(),
                obj => obj.UnsafeAs<Action>().Invoke()
            );
        }

        /// <summary>Schedules the configured continuation onto the <see cref="Promise{T}"/> associated with this <see cref="ConfiguredPromiseNoThrowAwaiter{T}"/>.</summary>
        /// <param name="continuation">The action to invoke when the await operation completes.</param>
        /// <remarks>This property is intended for compiler use rather than use directly in code.</remarks>
        /// <exception cref="InvalidOperationException">The <see cref="Promise{T}"/> has already been awaited or forgotten.</exception>
        [MethodImpl(Internal.InlineOption)]
        public void UnsafeOnCompleted(Action continuation)
            => OnCompleted(continuation);

        [MethodImpl(Internal.InlineOption)]
        void Internal.IPromiseAwaiter.AwaitOnCompletedInternal(Internal.PromiseRefBase asyncPromiseRef)
            => Internal.PromiseRefBase.ConfiguredAsyncPromiseContinuer.ConfigureAsyncContinuation(asyncPromiseRef, _continuationOptions.GetContinuationContext(), _promise._ref, _promise._id);

        static partial void ValidateArgument<TArg>(TArg arg, string argName, int skipFrames);
#if PROMISE_DEBUG
        static partial void ValidateArgument<TArg>(TArg arg, string argName, int skipFrames)
            => Internal.ValidateArgument(arg, argName, skipFrames + 1);
#endif
    } // struct ConfiguredPromiseNoThrowAwaiter<T>
} // namespace Proto.Promises.CompilerServices