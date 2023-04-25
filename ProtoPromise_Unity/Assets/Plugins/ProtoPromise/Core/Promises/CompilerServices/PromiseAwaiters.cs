#if UNITY_5_5 || NET_2_0 || NET_2_0_SUBSET
#define NET_LEGACY
#endif

#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif
#if !PROTO_PROMISE_PROGRESS_DISABLE
#define PROMISE_PROGRESS
#else
#undef PROMISE_PROGRESS
#endif

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Proto.Promises.Async.CompilerServices;

namespace Proto.Promises
{
    partial struct Promise
    {
        /// <summary>Gets an awaiter for this <see cref="Promise"/>.</summary>
        /// <remarks>This method is intended for compiler user rather than use directly in code.</remarks>
        /// <returns>The awaiter.</returns>
        [MethodImpl(Internal.InlineOption)]
        public PromiseAwaiterVoid GetAwaiter()
        {
            ValidateOperation(1);
            return new PromiseAwaiterVoid(this);
        }

        /// <summary>
        /// Gets an awaiter for this <see cref="Promise"/> that supports reporting progress to the async <see cref="Promise"/> or <see cref="Promise{T}"/> function.
        /// The progress reported will be lerped from <paramref name="minProgress"/> to <paramref name="maxProgress"/>. Both values must be between 0 and 1 inclusive.
        /// </summary>
        /// <remarks> Use as `await promise.AwaitWithProgress(minProgress, maxProgress);`</remarks>
        /// <returns>The awaiter.</returns>
        public PromiseProgressAwaiterVoid AwaitWithProgress(float minProgress, float maxProgress)
        {
            ValidateOperation(1);
            Internal.ValidateProgressValue(minProgress, "minProgress", 1);
            Internal.ValidateProgressValue(maxProgress, "maxProgress", 1);
            return new PromiseProgressAwaiterVoid(this, minProgress, maxProgress);
        }

        /// <summary>
        /// Gets an awaiter for this <see cref="Promise"/> that supports reporting progress to the async <see cref="Promise"/> or <see cref="Promise{T}"/> function.
        /// The progress reported will be lerped from its current progress to <paramref name="maxProgress"/>. <paramref name="maxProgress"/> must be between 0 and 1 inclusive.
        /// </summary>
        /// <remarks> Use as `await promise.AwaitWithProgress(minProgress, maxProgress);`</remarks>
        /// <returns>The awaiter.</returns>
        public PromiseProgressAwaiterVoid AwaitWithProgress(float maxProgress)
        {
            ValidateOperation(1);
            Internal.ValidateProgressValue(maxProgress, "maxProgress", 1);
            return new PromiseProgressAwaiterVoid(this, float.NaN, maxProgress);
        }
    }

    partial struct Promise<T>
    {
        /// <summary>Gets an awaiter for this <see cref="Promise{T}"/>.</summary>
        /// <remarks>This method is intended for compiler user rather than use directly in code.</remarks>
        /// <returns>The awaiter.</returns>
        [MethodImpl(Internal.InlineOption)]
        public PromiseAwaiter<T> GetAwaiter()
        {
            ValidateOperation(1);
            return new PromiseAwaiter<T>(this);
        }

        /// <summary>
        /// Gets an awaiter for this <see cref="Promise{T}"/> that supports reporting progress to the async <see cref="Promise"/> or <see cref="Promise{T}"/> function.
        /// The progress reported will be lerped from <paramref name="minProgress"/> to <paramref name="maxProgress"/>. Both values must be between 0 and 1 inclusive.
        /// </summary>
        /// <remarks>
        /// If the previously awaited promise did not complete successfully, minProgress will be set to the previous <paramref name="maxProgress"/> instead of current.
        /// <para/>Use as `await promise.AwaitWithProgress(maxProgress);`
        /// </remarks>
        /// <returns>The awaiter.</returns>
        public PromiseProgressAwaiter<T> AwaitWithProgress(float minProgress, float maxProgress)
        {
            ValidateOperation(1);
            Internal.ValidateProgressValue(minProgress, "minProgress", 1);
            Internal.ValidateProgressValue(maxProgress, "maxProgress", 1);
            return new PromiseProgressAwaiter<T>(this, minProgress, maxProgress);
        }

        /// <summary>
        /// Gets an awaiter for this <see cref="Promise{T}"/> that supports reporting progress to the async <see cref="Promise"/> or <see cref="Promise{T}"/> function.
        /// The progress reported will be lerped from its current progress to <paramref name="maxProgress"/>. <paramref name="maxProgress"/> must be between 0 and 1 inclusive.
        /// </summary>
        /// <remarks>
        /// If the previously awaited promise did not complete successfully, minProgress will be set to the previous <paramref name="maxProgress"/> instead of current.
        /// <para/>Use as `await promise.AwaitWithProgress(maxProgress);`
        /// </remarks>
        /// <returns>The awaiter.</returns>
        public PromiseProgressAwaiter<T> AwaitWithProgress(float maxProgress)
        {
            ValidateOperation(1);
            Internal.ValidateProgressValue(maxProgress, "maxProgress", 1);
            return new PromiseProgressAwaiter<T>(this, float.NaN, maxProgress);
        }
    }

    namespace Async.CompilerServices
    {
#if !NETCOREAPP
        partial struct PromiseAwaiterVoid
        {
            // Fix for IL2CPP not invoking the static constructor.
#if ENABLE_IL2CPP
            [MethodImpl(Internal.InlineOption)]
            static partial void CreateOverride()
            {
                Internal.AwaitOverrider<PromiseAwaiterVoid>.Create<PromiseAwaiterVoid>();
            }
#else
            static PromiseAwaiterVoid()
            {
                Internal.AwaitOverrider<PromiseAwaiterVoid>.Create<PromiseAwaiterVoid>();
            }
#endif
        }

        partial struct PromiseAwaiter<T>
        {
            // Fix for IL2CPP not invoking the static constructor.
#if ENABLE_IL2CPP
            [MethodImpl(Internal.InlineOption)]
            static partial void CreateOverride()
            {
                Internal.AwaitOverrider<PromiseAwaiter<T>>.Create<PromiseAwaiter<T>>();
            }
#else
            static PromiseAwaiter()
            {
                Internal.AwaitOverrider<PromiseAwaiter<T>>.Create<PromiseAwaiter<T>>();
            }
#endif
        }

        partial struct PromiseProgressAwaiterVoid
        {
            // Fix for IL2CPP not invoking the static constructor.
#if ENABLE_IL2CPP
            [MethodImpl(Internal.InlineOption)]
            static partial void CreateOverride()
            {
                Internal.AwaitOverrider<PromiseProgressAwaiterVoid>.Create<PromiseProgressAwaiterVoid>();
            }
#else
            static PromiseProgressAwaiterVoid()
            {
                Internal.AwaitOverrider<PromiseProgressAwaiterVoid>.Create<PromiseProgressAwaiterVoid>();
            }
#endif
        }

        partial struct PromiseProgressAwaiter<T>
        {
            // Fix for IL2CPP not invoking the static constructor.
#if ENABLE_IL2CPP
            [MethodImpl(Internal.InlineOption)]
            static partial void CreateOverride()
            {
                Internal.AwaitOverrider<PromiseProgressAwaiter<T>>.Create<PromiseProgressAwaiter<T>>();
            }
#else
            static PromiseProgressAwaiter()
            {
                Internal.AwaitOverrider<PromiseProgressAwaiter<T>>.Create<PromiseProgressAwaiter<T>>();
            }
#endif
        }
#endif // !NETCOREAPP

        /// <summary>
        /// Provides an awaiter for awaiting a <see cref="Promise"/>.
        /// </summary>
        /// <remarks>This type is intended for compiler user rather than use directly in code.</remarks>
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        public
#if CSHARP_7_3_OR_NEWER
            readonly
#endif
            partial struct PromiseAwaiterVoid : ICriticalNotifyCompletion, Internal.IPromiseAwaiter
        {
            private readonly Promise _promise;

            /// <summary>
            /// Internal use.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            internal PromiseAwaiterVoid(
#if CSHARP_7_3_OR_NEWER
                in
#endif
                Promise promise)
            {
                _promise = promise;
                CreateOverride();
            }

            static partial void CreateOverride();

            /// <summary>Gets whether the <see cref="Promise"/> being awaited is completed.</summary>
            /// <remarks>This property is intended for compiler user rather than use directly in code.</remarks>
            /// <exception cref="InvalidOperationException">The <see cref="Promise"/> has already been awaited or forgotten.</exception>
            public bool IsCompleted
            {
                [MethodImpl(Internal.InlineOption)]
                get
                {
                    var _ref = _promise._ref;
                    return _ref == null || _ref.GetIsCompleted(_promise._id);
                }
            }

            /// <summary>Ends the await on the completed <see cref="Promise"/>.</summary>
            /// <remarks>This property is intended for compiler user rather than use directly in code.</remarks>
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
            /// <remarks>This property is intended for compiler user rather than use directly in code.</remarks>
            /// <exception cref="InvalidOperationException">The <see cref="Promise"/> has already been awaited or forgotten.</exception>
            [MethodImpl(Internal.InlineOption)]
            public void OnCompleted(Action continuation)
            {
                ValidateArgument(continuation, "continuation", 1);
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
            /// <remarks>This property is intended for compiler user rather than use directly in code.</remarks>
            /// <exception cref="InvalidOperationException">The <see cref="Promise"/> has already been awaited or forgotten.</exception>
            [MethodImpl(Internal.InlineOption)]
            public void UnsafeOnCompleted(Action continuation)
            {
                OnCompleted(continuation);
            }

            [MethodImpl(Internal.InlineOption)]
            void Internal.IPromiseAwaiter.AwaitOnCompletedInternal(Internal.PromiseRefBase asyncPromiseRef, ref Internal.PromiseRefBase.AsyncPromiseFields asyncFields)
            {
                asyncPromiseRef.HookupAwaiter(_promise._ref, _promise._id);
            }

            static partial void ValidateArgument<TArg>(TArg arg, string argName, int skipFrames);
#if PROMISE_DEBUG
            static partial void ValidateArgument<TArg>(TArg arg, string argName, int skipFrames)
            {
                Internal.ValidateArgument(arg, argName, skipFrames + 1);
            }
#endif
        } // struct PromiseAwaiterVoid

        /// <summary>
        /// Provides an awaiter for awaiting a <see cref="Promise{T}"/>.
        /// </summary>
        /// <remarks>This type is intended for compiler user rather than use directly in code.</remarks>
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        public
#if CSHARP_7_3_OR_NEWER
            readonly
#endif
            partial struct PromiseAwaiter<T> : ICriticalNotifyCompletion, Internal.IPromiseAwaiter
        {
            private readonly Promise<T> _promise;

            /// <summary>
            /// Internal use.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            internal PromiseAwaiter(
#if CSHARP_7_3_OR_NEWER
                in
#endif
                Promise<T> promise)
            {
                _promise = promise;
                CreateOverride();
            }

            static partial void CreateOverride();

            /// <summary>Gets whether the <see cref="Promise{T}"/> being awaited is completed.</summary>
            /// <remarks>This property is intended for compiler user rather than use directly in code.</remarks>
            /// <exception cref="InvalidOperationException">The <see cref="Promise{T}"/> has already been awaited or forgotten.</exception>
            public bool IsCompleted
            {
                [MethodImpl(Internal.InlineOption)]
                get
                {
                    var _ref = _promise._ref;
                    return _ref == null || _ref.GetIsCompleted(_promise._id);
                }
            }

            /// <summary>Ends the await on the completed <see cref="Promise{T}"/>.</summary>
            /// <returns>The result of the completed <see cref="Promise{T}"/></returns>
            /// <remarks>This property is intended for compiler user rather than use directly in code.</remarks>
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
            /// <remarks>This property is intended for compiler user rather than use directly in code.</remarks>
            /// <exception cref="InvalidOperationException">The <see cref="Promise{T}"/> has already been awaited or forgotten.</exception>
            [MethodImpl(Internal.InlineOption)]
            public void OnCompleted(Action continuation)
            {
                ValidateArgument(continuation, "continuation", 1);
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
            /// <remarks>This property is intended for compiler user rather than use directly in code.</remarks>
            /// <exception cref="InvalidOperationException">The <see cref="Promise{T}"/> has already been awaited or forgotten.</exception>
            [MethodImpl(Internal.InlineOption)]
            public void UnsafeOnCompleted(Action continuation)
            {
                OnCompleted(continuation);
            }

            [MethodImpl(Internal.InlineOption)]
            void Internal.IPromiseAwaiter.AwaitOnCompletedInternal(Internal.PromiseRefBase asyncPromiseRef, ref Internal.PromiseRefBase.AsyncPromiseFields asyncFields)
            {
                asyncPromiseRef.HookupAwaiter(_promise._ref, _promise._id);
            }

            static partial void ValidateArgument<TArg>(TArg arg, string argName, int skipFrames);
#if PROMISE_DEBUG
            static partial void ValidateArgument<TArg>(TArg arg, string argName, int skipFrames)
            {
                Internal.ValidateArgument(arg, argName, skipFrames + 1);
            }
#endif
        } // struct PromiseAwaiter<T>

        /// <summary>
        /// Provides an awaiter for awaiting a <see cref="Promise"/> and reporting its progress to the associated async <see cref="Promise"/> or <see cref="Promise{T}"/>.
        /// </summary>
        /// <remarks>This type is intended for compiler user rather than use directly in code.</remarks>
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        public
#if CSHARP_7_3_OR_NEWER
            readonly
#endif
            partial struct PromiseProgressAwaiterVoid : ICriticalNotifyCompletion, Internal.IPromiseAwaiter
        {
            private readonly Promise _promise;
            private readonly float _minProgress;
            private readonly float _maxProgress;

            /// <summary>
            /// Internal use.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            internal PromiseProgressAwaiterVoid(
#if CSHARP_7_3_OR_NEWER
                in
#endif
                Promise promise, float minProgress, float maxProgress)
            {
                _promise = promise;
                _minProgress = minProgress;
                _maxProgress = maxProgress;
                CreateOverride();
            }

            static partial void CreateOverride();

            /// <summary>Gets the awaiter for this.</summary>
            /// <remarks>This method is intended for compiler user rather than use directly in code.</remarks>
            /// <returns>this</returns>
            [MethodImpl(Internal.InlineOption)]
            public PromiseProgressAwaiterVoid GetAwaiter()
            {
                return this;
            }

            /// <summary>Gets whether the <see cref="Promise"/> being awaited is completed.</summary>
            /// <remarks>This property is intended for compiler user rather than use directly in code.</remarks>
            /// <exception cref="InvalidOperationException">The <see cref="Promise"/> has already been awaited or forgotten.</exception>
            public bool IsCompleted
            {
                [MethodImpl(Internal.InlineOption)]
                get
                {
                    var _ref = _promise._ref;
                    return _ref == null || _ref.GetIsCompleted(_promise._id);
                }
            }

            /// <summary>Ends the await on the completed <see cref="Promise"/>.</summary>
            /// <remarks>This property is intended for compiler user rather than use directly in code.</remarks>
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

            /// <summary>Schedules the continuation onto the <see cref="Promise"/> associated with this <see cref="PromiseProgressAwaiterVoid"/>.</summary>
            /// <param name="continuation">The action to invoke when the await operation completes.</param>
            /// <remarks>This property is intended for compiler user rather than use directly in code.</remarks>
            /// <exception cref="InvalidOperationException">The <see cref="Promise"/> has already been awaited or forgotten.</exception>
            [MethodImpl(Internal.InlineOption)]
            public void OnCompleted(Action continuation)
            {
                ValidateArgument(continuation, "continuation", 1);
                var _ref = _promise._ref;
                if (_ref == null)
                {
                    continuation();
                    return;
                }
                _promise._ref.OnCompleted(continuation, _promise._id);
            }

            /// <summary>Schedules the continuation onto the <see cref="Promise"/> associated with this <see cref="PromiseProgressAwaiterVoid"/>.</summary>
            /// <param name="continuation">The action to invoke when the await operation completes.</param>
            /// <remarks>This property is intended for compiler user rather than use directly in code.</remarks>
            /// <exception cref="InvalidOperationException">The <see cref="Promise"/> has already been awaited or forgotten.</exception>
            [MethodImpl(Internal.InlineOption)]
            public void UnsafeOnCompleted(Action continuation)
            {
                OnCompleted(continuation);
            }

            [MethodImpl(Internal.InlineOption)]
            void Internal.IPromiseAwaiter.AwaitOnCompletedInternal(Internal.PromiseRefBase asyncPromiseRef, ref Internal.PromiseRefBase.AsyncPromiseFields asyncFields)
            {
                asyncPromiseRef.HookupAwaiterWithProgress(_promise._ref, _promise._id, _promise.Depth, _minProgress, _maxProgress, ref asyncFields);
            }

            static partial void ValidateArgument<TArg>(TArg arg, string argName, int skipFrames);
#if PROMISE_DEBUG
            static partial void ValidateArgument<TArg>(TArg arg, string argName, int skipFrames)
            {
                Internal.ValidateArgument(arg, argName, skipFrames + 1);
            }
#endif
        } // struct PromiseAwaiterVoid

        /// <summary>
        /// Provides an awaiter for awaiting a <see cref="Promise{T}"/> and reporting its progress to the associated async <see cref="Promise"/> or <see cref="Promise{T}"/>.
        /// </summary>
        /// <remarks>This type is intended for compiler user rather than use directly in code.</remarks>
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        public
#if CSHARP_7_3_OR_NEWER
            readonly
#endif
            partial struct PromiseProgressAwaiter<T> : ICriticalNotifyCompletion, Internal.IPromiseAwaiter
        {
            private readonly Promise<T> _promise;
            private readonly float _minProgress;
            private readonly float _maxProgress;

            /// <summary>
            /// Internal use.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            internal PromiseProgressAwaiter(
#if CSHARP_7_3_OR_NEWER
                in
#endif
                Promise<T> promise, float minProgress, float maxProgress)
            {
                _promise = promise;
                _minProgress = minProgress;
                _maxProgress = maxProgress;
                CreateOverride();
            }

            static partial void CreateOverride();

            /// <summary>Gets the awaiter for this.</summary>
            /// <remarks>This method is intended for compiler user rather than use directly in code.</remarks>
            /// <returns>this</returns>
            [MethodImpl(Internal.InlineOption)]
            public PromiseProgressAwaiter<T> GetAwaiter()
            {
                return this;
            }

            /// <summary>Gets whether the <see cref="Promise{T}"/> being awaited is completed.</summary>
            /// <remarks>This property is intended for compiler user rather than use directly in code.</remarks>
            /// <exception cref="InvalidOperationException">The <see cref="Promise{T}"/> has already been awaited or forgotten.</exception>
            public bool IsCompleted
            {
                [MethodImpl(Internal.InlineOption)]
                get
                {
                    var _ref = _promise._ref;
                    return _ref == null || _ref.GetIsCompleted(_promise._id);
                }
            }

            /// <summary>Ends the await on the completed <see cref="Promise{T}"/>.</summary>
            /// <returns>The result of the completed <see cref="Promise{T}"/></returns>
            /// <remarks>This property is intended for compiler user rather than use directly in code.</remarks>
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

            /// <summary>Schedules the continuation onto the <see cref="Promise{T}"/> associated with this <see cref="PromiseProgressAwaiter{T}"/>.</summary>
            /// <param name="continuation">The action to invoke when the await operation completes.</param>
            /// <remarks>This property is intended for compiler user rather than use directly in code.</remarks>
            /// <exception cref="InvalidOperationException">The <see cref="Promise{T}"/> has already been awaited or forgotten.</exception>
            [MethodImpl(Internal.InlineOption)]
            public void OnCompleted(Action continuation)
            {
                ValidateArgument(continuation, "continuation", 1);
                var _ref = _promise._ref;
                if (_ref == null)
                {
                    continuation();
                    return;
                }
                _promise._ref.OnCompleted(continuation, _promise._id);
            }

            /// <summary>Schedules the continuation onto the <see cref="Promise{T}"/> associated with this <see cref="PromiseProgressAwaiter{T}"/>.</summary>
            /// <param name="continuation">The action to invoke when the await operation completes.</param>
            /// <remarks>This property is intended for compiler user rather than use directly in code.</remarks>
            /// <exception cref="InvalidOperationException">The <see cref="Promise{T}"/> has already been awaited or forgotten.</exception>
            [MethodImpl(Internal.InlineOption)]
            public void UnsafeOnCompleted(Action continuation)
            {
                OnCompleted(continuation);
            }

            [MethodImpl(Internal.InlineOption)]
            void Internal.IPromiseAwaiter.AwaitOnCompletedInternal(Internal.PromiseRefBase asyncPromiseRef, ref Internal.PromiseRefBase.AsyncPromiseFields asyncFields)
            {
                asyncPromiseRef.HookupAwaiterWithProgress(_promise._ref, _promise._id, _promise.Depth, _minProgress, _maxProgress, ref asyncFields);
            }

            static partial void ValidateArgument<TArg>(TArg arg, string argName, int skipFrames);
#if PROMISE_DEBUG
            static partial void ValidateArgument<TArg>(TArg arg, string argName, int skipFrames)
            {
                Internal.ValidateArgument(arg, argName, skipFrames + 1);
            }
#endif
        } // struct PromiseAwaiter<T>
    } // namespace Async.CompilerServices
}