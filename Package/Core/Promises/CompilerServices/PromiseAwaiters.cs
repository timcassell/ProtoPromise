#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Proto.Promises.CompilerServices;

#pragma warning disable IDE0090 // Use 'new(...)'

namespace Proto.Promises
{
    partial struct Promise
    {
        /// <summary>Gets an awaiter for this <see cref="Promise"/>.</summary>
        /// <remarks>This method is intended for compiler use rather than use directly in code.</remarks>
        /// <returns>The awaiter.</returns>
        [MethodImpl(Internal.InlineOption), EditorBrowsable(EditorBrowsableState.Never)]
        public PromiseAwaiterVoid GetAwaiter()
        {
            ValidateOperation(1);
            return new PromiseAwaiterVoid(this);
        }

        /// <summary>Gets an awaiter for this <see cref="Promise"/> that suppresses throws and returns a <see cref="ResultContainer"/> instead.</summary>
        /// <returns>The awaiter.</returns>
        /// <remarks> Use as `var resultContainer = await promise.AwaitNoThrow();`</remarks>
        [MethodImpl(Internal.InlineOption)]
        public PromiseNoThrowAwaiterVoid AwaitNoThrow()
        {
            ValidateOperation(1);
            return new PromiseNoThrowAwaiterVoid(this);
        }
    }

    partial struct Promise<T>
    {
        /// <summary>Gets an awaiter for this <see cref="Promise{T}"/>.</summary>
        /// <remarks>This method is intended for compiler use rather than use directly in code.</remarks>
        /// <returns>The awaiter.</returns>
        [MethodImpl(Internal.InlineOption), EditorBrowsable(EditorBrowsableState.Never)]
        public PromiseAwaiter<T> GetAwaiter()
        {
            ValidateOperation(1);
            return new PromiseAwaiter<T>(this);
        }

        /// <summary>Gets an awaiter for this <see cref="Promise{T}"/> that suppresses throws and returns a <see cref="ResultContainer"/> instead.</summary>
        /// <returns>The awaiter.</returns>
        /// <remarks>Use as `var resultContainer = await promise.AwaitNoThrow();`</remarks>
        [MethodImpl(Internal.InlineOption)]
        public PromiseNoThrowAwaiter<T> AwaitNoThrow()
        {
            ValidateOperation(1);
            return new PromiseNoThrowAwaiter<T>(this);
        }
    }

    namespace CompilerServices
    {
#if !NETCOREAPP
        partial struct PromiseAwaiterVoid
        {
            // Fix for IL2CPP not invoking the static constructor.
#if ENABLE_IL2CPP
            [MethodImpl(Internal.InlineOption)]
            static partial void CreateOverride()
#else
            static PromiseAwaiterVoid()
#endif
                => Internal.AwaitOverriderImpl<PromiseAwaiterVoid>.Create();
        }

        partial struct PromiseAwaiter<T>
        {
            // Fix for IL2CPP not invoking the static constructor.
#if ENABLE_IL2CPP
            [MethodImpl(Internal.InlineOption)]
            static partial void CreateOverride()
#else
            static PromiseAwaiter()
#endif
                => Internal.AwaitOverriderImpl<PromiseAwaiter<T>>.Create();
        }

        partial struct PromiseNoThrowAwaiterVoid
        {
            // Fix for IL2CPP not invoking the static constructor.
#if ENABLE_IL2CPP
            [MethodImpl(Internal.InlineOption)]
            static partial void CreateOverride()
#else
            static PromiseNoThrowAwaiterVoid()
#endif
                => Internal.AwaitOverriderImpl<PromiseNoThrowAwaiterVoid>.Create();
        }

        partial struct PromiseNoThrowAwaiter<T>
        {
            // Fix for IL2CPP not invoking the static constructor.
#if ENABLE_IL2CPP
            [MethodImpl(Internal.InlineOption)]
            static partial void CreateOverride()
#else
            static PromiseNoThrowAwaiter()
#endif
                => Internal.AwaitOverriderImpl<PromiseNoThrowAwaiter<T>>.Create();
        }
#endif // !NETCOREAPP

        /// <summary>
        /// Provides an awaiter for awaiting a <see cref="Promise"/>.
        /// </summary>
        /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        public readonly partial struct PromiseAwaiterVoid : ICriticalNotifyCompletion, Internal.IPromiseAwaiter
        {
            private readonly Promise _promise;

            [MethodImpl(Internal.InlineOption)]
            internal PromiseAwaiterVoid(in Promise promise)
            {
                _promise = promise;
                CreateOverride();
            }

            static partial void CreateOverride();

            /// <summary>Gets whether the <see cref="Promise"/> being awaited is completed.</summary>
            /// <remarks>This property is intended for compiler use rather than use directly in code.</remarks>
            /// <exception cref="InvalidOperationException">The <see cref="Promise"/> has already been awaited or forgotten.</exception>
            public bool IsCompleted
            {
                [MethodImpl(Internal.InlineOption)]
                get => _promise._ref?.GetIsCompleted(_promise._id) != false;
            }

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

            /// <summary>Schedules the continuation onto the <see cref="Promise"/> associated with this <see cref="PromiseAwaiterVoid"/>.</summary>
            /// <param name="continuation">The action to invoke when the await operation completes.</param>
            /// <remarks>This property is intended for compiler use rather than use directly in code.</remarks>
            /// <exception cref="InvalidOperationException">The <see cref="Promise"/> has already been awaited or forgotten.</exception>
            [MethodImpl(Internal.InlineOption)]
            public void OnCompleted(Action continuation)
            {
                ValidateArgument(continuation, nameof(continuation), 1);
                var _ref = _promise._ref;
                if (_ref == null)
                {
                    continuation();
                    return;
                }
                _ref.OnCompleted(continuation, _promise._id);
            }

            /// <summary>Schedules the continuation onto the <see cref="Promise"/> associated with this <see cref="PromiseAwaiterVoid"/>.</summary>
            /// <param name="continuation">The action to invoke when the await operation completes.</param>
            /// <remarks>This property is intended for compiler use rather than use directly in code.</remarks>
            /// <exception cref="InvalidOperationException">The <see cref="Promise"/> has already been awaited or forgotten.</exception>
            [MethodImpl(Internal.InlineOption)]
            public void UnsafeOnCompleted(Action continuation)
            {
                OnCompleted(continuation);
            }

            [MethodImpl(Internal.InlineOption)]
            void Internal.IPromiseAwaiter.AwaitOnCompletedInternal(Internal.PromiseRefBase asyncPromiseRef)
                => asyncPromiseRef.HookupAwaiter(_promise._ref, _promise._id);

            static partial void ValidateArgument<TArg>(TArg arg, string argName, int skipFrames);
#if PROMISE_DEBUG
            static partial void ValidateArgument<TArg>(TArg arg, string argName, int skipFrames)
                => Internal.ValidateArgument(arg, argName, skipFrames + 1);
#endif
        } // struct PromiseAwaiterVoid

        /// <summary>
        /// Provides an awaiter for awaiting a <see cref="Promise{T}"/>.
        /// </summary>
        /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        public readonly partial struct PromiseAwaiter<T> : ICriticalNotifyCompletion, Internal.IPromiseAwaiter
        {
            private readonly Promise<T> _promise;

            [MethodImpl(Internal.InlineOption)]
            internal PromiseAwaiter(in Promise<T> promise)
            {
                _promise = promise;
                CreateOverride();
            }

            static partial void CreateOverride();

            /// <summary>Gets whether the <see cref="Promise{T}"/> being awaited is completed.</summary>
            /// <remarks>This property is intended for compiler use rather than use directly in code.</remarks>
            /// <exception cref="InvalidOperationException">The <see cref="Promise{T}"/> has already been awaited or forgotten.</exception>
            public bool IsCompleted
            {
                [MethodImpl(Internal.InlineOption)]
                get => _promise._ref?.GetIsCompleted(_promise._id) != false;
            }

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

            /// <summary>Schedules the continuation onto the <see cref="Promise{T}"/> associated with this <see cref="PromiseAwaiter{T}"/>.</summary>
            /// <param name="continuation">The action to invoke when the await operation completes.</param>
            /// <remarks>This property is intended for compiler use rather than use directly in code.</remarks>
            /// <exception cref="InvalidOperationException">The <see cref="Promise{T}"/> has already been awaited or forgotten.</exception>
            [MethodImpl(Internal.InlineOption)]
            public void OnCompleted(Action continuation)
            {
                ValidateArgument(continuation, nameof(continuation), 1);
                var _ref = _promise._ref;
                if (_ref == null)
                {
                    continuation();
                    return;
                }
                _ref.OnCompleted(continuation, _promise._id);
            }

            /// <summary>Schedules the continuation onto the <see cref="Promise{T}"/> associated with this <see cref="PromiseAwaiter{T}"/>.</summary>
            /// <param name="continuation">The action to invoke when the await operation completes.</param>
            /// <remarks>This property is intended for compiler use rather than use directly in code.</remarks>
            /// <exception cref="InvalidOperationException">The <see cref="Promise{T}"/> has already been awaited or forgotten.</exception>
            [MethodImpl(Internal.InlineOption)]
            public void UnsafeOnCompleted(Action continuation)
                => OnCompleted(continuation);

            [MethodImpl(Internal.InlineOption)]
            void Internal.IPromiseAwaiter.AwaitOnCompletedInternal(Internal.PromiseRefBase asyncPromiseRef)
                => asyncPromiseRef.HookupAwaiter(_promise._ref, _promise._id);

            static partial void ValidateArgument<TArg>(TArg arg, string argName, int skipFrames);
#if PROMISE_DEBUG
            static partial void ValidateArgument<TArg>(TArg arg, string argName, int skipFrames)
                => Internal.ValidateArgument(arg, argName, skipFrames + 1);
#endif
        } // struct PromiseAwaiter<T>

        /// <summary>
        /// Provides an awaiter for awaiting a <see cref="Promise"/>, without throwing.
        /// </summary>
        /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        public readonly partial struct PromiseNoThrowAwaiterVoid : ICriticalNotifyCompletion, Internal.IPromiseAwaiter
        {
            private readonly Promise _promise;

            [MethodImpl(Internal.InlineOption)]
            internal PromiseNoThrowAwaiterVoid(in Promise promise)
            {
                _promise = promise;
                CreateOverride();
            }

            static partial void CreateOverride();

            /// <summary>Gets the awaiter for this.</summary>
            /// <remarks>This method is intended for compiler use rather than use directly in code.</remarks>
            /// <returns>this</returns>
            [MethodImpl(Internal.InlineOption)]
            public PromiseNoThrowAwaiterVoid GetAwaiter()
                => this;

            /// <summary>Gets whether the <see cref="Promise"/> being awaited is completed.</summary>
            /// <remarks>This property is intended for compiler use rather than use directly in code.</remarks>
            /// <exception cref="InvalidOperationException">The <see cref="Promise"/> has already been awaited or forgotten.</exception>
            public bool IsCompleted
            {
                [MethodImpl(Internal.InlineOption)]
                get => _promise._ref?.GetIsCompleted(_promise._id) != false;
            }

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

            /// <summary>Schedules the continuation onto the <see cref="Promise"/> associated with this <see cref="PromiseAwaiterVoid"/>.</summary>
            /// <param name="continuation">The action to invoke when the await operation completes.</param>
            /// <remarks>This property is intended for compiler use rather than use directly in code.</remarks>
            /// <exception cref="InvalidOperationException">The <see cref="Promise"/> has already been awaited or forgotten.</exception>
            [MethodImpl(Internal.InlineOption)]
            public void OnCompleted(Action continuation)
            {
                ValidateArgument(continuation, nameof(continuation), 1);
                var _ref = _promise._ref;
                if (_ref == null)
                {
                    continuation();
                    return;
                }
                _ref.OnCompleted(continuation, _promise._id);
            }

            /// <summary>Schedules the continuation onto the <see cref="Promise"/> associated with this <see cref="PromiseAwaiterVoid"/>.</summary>
            /// <param name="continuation">The action to invoke when the await operation completes.</param>
            /// <remarks>This property is intended for compiler use rather than use directly in code.</remarks>
            /// <exception cref="InvalidOperationException">The <see cref="Promise"/> has already been awaited or forgotten.</exception>
            [MethodImpl(Internal.InlineOption)]
            public void UnsafeOnCompleted(Action continuation)
                => OnCompleted(continuation);

            [MethodImpl(Internal.InlineOption)]
            void Internal.IPromiseAwaiter.AwaitOnCompletedInternal(Internal.PromiseRefBase asyncPromiseRef)
                => asyncPromiseRef.HookupAwaiter(_promise._ref, _promise._id);

            /// <summary>
            /// Configure the await.
            /// Returns a <see cref="ConfiguredPromiseNoThrowAwaiterVoid"/> that configures the continuation behavior of the await according to the provided <paramref name="continuationOptions"/>.
            /// </summary>
            /// <param name="continuationOptions">The options used to configure the execution behavior of the async continuation.</param>
            [MethodImpl(Internal.InlineOption)]
            public ConfiguredPromiseNoThrowAwaiterVoid ConfigureAwait(ContinuationOptions continuationOptions)
                => new ConfiguredPromiseNoThrowAwaiterVoid(_promise, continuationOptions);

            static partial void ValidateArgument<TArg>(TArg arg, string argName, int skipFrames);
#if PROMISE_DEBUG
            static partial void ValidateArgument<TArg>(TArg arg, string argName, int skipFrames)
                => Internal.ValidateArgument(arg, argName, skipFrames + 1);
#endif
        } // struct PromiseNoThrowAwaiterVoid

        /// <summary>
        /// Provides an awaiter for awaiting a <see cref="Promise{T}"/>, without throwing.
        /// </summary>
        /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        public readonly partial struct PromiseNoThrowAwaiter<T> : ICriticalNotifyCompletion, Internal.IPromiseAwaiter
        {
            private readonly Promise<T> _promise;

            [MethodImpl(Internal.InlineOption)]
            internal PromiseNoThrowAwaiter(in Promise<T> promise)
            {
                _promise = promise;
                CreateOverride();
            }

            static partial void CreateOverride();

            /// <summary>Gets the awaiter for this.</summary>
            /// <remarks>This method is intended for compiler use rather than use directly in code.</remarks>
            /// <returns>this</returns>
            [MethodImpl(Internal.InlineOption)]
            public PromiseNoThrowAwaiter<T> GetAwaiter()
                => this;

            /// <summary>Gets whether the <see cref="Promise{T}"/> being awaited is completed.</summary>
            /// <remarks>This property is intended for compiler use rather than use directly in code.</remarks>
            /// <exception cref="InvalidOperationException">The <see cref="Promise{T}"/> has already been awaited or forgotten.</exception>
            public bool IsCompleted
            {
                [MethodImpl(Internal.InlineOption)]
                get => _promise._ref?.GetIsCompleted(_promise._id) != false;
            }

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

            /// <summary>Schedules the continuation onto the <see cref="Promise{T}"/> associated with this <see cref="PromiseAwaiter{T}"/>.</summary>
            /// <param name="continuation">The action to invoke when the await operation completes.</param>
            /// <remarks>This property is intended for compiler use rather than use directly in code.</remarks>
            /// <exception cref="InvalidOperationException">The <see cref="Promise{T}"/> has already been awaited or forgotten.</exception>
            [MethodImpl(Internal.InlineOption)]
            public void OnCompleted(Action continuation)
            {
                ValidateArgument(continuation, nameof(continuation), 1);
                var _ref = _promise._ref;
                if (_ref == null)
                {
                    continuation();
                    return;
                }
                _ref.OnCompleted(continuation, _promise._id);
            }

            /// <summary>Schedules the continuation onto the <see cref="Promise{T}"/> associated with this <see cref="PromiseAwaiter{T}"/>.</summary>
            /// <param name="continuation">The action to invoke when the await operation completes.</param>
            /// <remarks>This property is intended for compiler use rather than use directly in code.</remarks>
            /// <exception cref="InvalidOperationException">The <see cref="Promise{T}"/> has already been awaited or forgotten.</exception>
            [MethodImpl(Internal.InlineOption)]
            public void UnsafeOnCompleted(Action continuation)
                => OnCompleted(continuation);

            [MethodImpl(Internal.InlineOption)]
            void Internal.IPromiseAwaiter.AwaitOnCompletedInternal(Internal.PromiseRefBase asyncPromiseRef)
                => asyncPromiseRef.HookupAwaiter(_promise._ref, _promise._id);

            /// <summary>
            /// Configure the await.
            /// Returns a <see cref="ConfiguredPromiseNoThrowAwaiter{T}"/> that configures the continuation behavior of the await according to the provided <paramref name="continuationOptions"/>.
            /// </summary>
            /// <param name="continuationOptions">The options used to configure the execution behavior of the async continuation.</param>
            [MethodImpl(Internal.InlineOption)]
            public ConfiguredPromiseNoThrowAwaiter<T> ConfigureAwait(ContinuationOptions continuationOptions)
                => new ConfiguredPromiseNoThrowAwaiter<T>(_promise, continuationOptions);

            static partial void ValidateArgument<TArg>(TArg arg, string argName, int skipFrames);
#if PROMISE_DEBUG
            static partial void ValidateArgument<TArg>(TArg arg, string argName, int skipFrames)
                => Internal.ValidateArgument(arg, argName, skipFrames + 1);
#endif
        } // struct PromiseNoThrowAwaiter<T>
    } // namespace CompilerServices
}