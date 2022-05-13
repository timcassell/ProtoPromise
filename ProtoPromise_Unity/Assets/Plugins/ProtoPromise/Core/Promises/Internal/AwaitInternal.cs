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

#pragma warning disable IDE0034 // Simplify 'default' expression

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Proto.Promises.Async.CompilerServices;

namespace Proto.Promises
{
    partial class Internal
    {
        partial class PromiseRefBase
        {
            [MethodImpl(InlineOption)]
            internal TResult GetResult<TResult>(short promiseId)
            {
                ValidateId(promiseId, this, 2);
                TResult result = GetResult<TResult>();
                MaybeDispose();
                return result;
            }

#if NET_LEGACY
            [MethodImpl(MethodImplOptions.NoInlining)]
            internal void Throw(Promise.State state, short promiseId)
            {
                ValidateId(promiseId, this, 2);
                if (state == Promise.State.Canceled)
                {
                    MaybeDispose();
                    throw CanceledExceptionInternal.GetOrCreate();
                }
                if (state == Promise.State.Rejected)
                {
                    SuppressRejection = true;
                    var exception = _rejectContainer.UnsafeAs<IRejectValueContainer>().GetException();
                    MaybeDispose();
                    throw exception;
                }
                throw new InvalidOperationException("PromiseAwaiter.GetResult() is only valid when the promise is completed.", GetFormattedStacktrace(2));
            }
#else
            [MethodImpl(MethodImplOptions.NoInlining)]
            internal System.Runtime.ExceptionServices.ExceptionDispatchInfo GetExceptionDispatchInfo(Promise.State state, short promiseId)
            {
                ValidateId(promiseId, this, 2);
                if (state == Promise.State.Canceled)
                {
                    MaybeDispose();
                    throw CanceledExceptionInternal.GetOrCreate();
                }
                if (state == Promise.State.Rejected)
                {
                    SuppressRejection = true;
                    var exceptionDispatchInfo = _rejectContainer.UnsafeAs<IRejectValueContainer>().GetExceptionDispatchInfo();
                    MaybeDispose();
                    return exceptionDispatchInfo;
                }
                throw new InvalidOperationException("PromiseAwaiter.GetResult() is only valid when the promise is completed.", GetFormattedStacktrace(2));
            }
#endif

            [MethodImpl(InlineOption)]
            internal void OnCompleted(Action continuation, short promiseId)
            {
                HookupNewWaiter(promiseId, AwaiterRef.GetOrCreate(continuation));
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode]
#endif
            private sealed class AwaiterRef : HandleablePromiseBase, ITraceable
            {
#if PROMISE_DEBUG
                CausalityTrace ITraceable.Trace { get; set; }
#endif

                private Action _continuation;

                private AwaiterRef() { }

                [MethodImpl(InlineOption)]
                internal static AwaiterRef GetOrCreate(Action continuation)
                {
                    var awaiter = ObjectPool<HandleablePromiseBase>.TryTake<AwaiterRef>()
                        ?? new AwaiterRef();
                    awaiter._continuation = continuation;
                    SetCreatedStacktrace(awaiter, 3);
                    return awaiter;
                }

                [MethodImpl(InlineOption)]
                private void Dispose()
                {
                    _continuation = null;
                    ObjectPool<HandleablePromiseBase>.MaybeRepool(this);
                }

                private void Invoke()
                {
                    ThrowIfInPool(this);
                    var callback = _continuation;
#if PROMISE_DEBUG
                    SetCurrentInvoker(this);
#else
                    Dispose();
#endif
                    try
                    {
                        callback.Invoke();
                    }
                    catch (Exception e)
                    {
                        // This should never hit if the `await` keyword is used, but a user manually subscribing to OnCompleted could throw.
                        ReportRejection(e, this);
                    }
#if PROMISE_DEBUG
                    ClearCurrentInvoker();
                    Dispose();
#endif
                }

                internal override void Handle(ref PromiseRefBase handler, out HandleablePromiseBase nextHandler, ref ExecutionScheduler executionScheduler)
                {
                    nextHandler = null;
                    Invoke();
                }

#if PROMISE_PROGRESS
                internal override PromiseRefBase SetProgress(ref Fixed32 progress, ref ushort depth, ref ExecutionScheduler executionScheduler)
                {
                    return null;
                }
#endif
            }
        }

        internal interface IPromiseAwaiter
        {
            void AwaitOnCompletedInternal(PromiseRefBase asyncPromiseRef);
        }

#if !NET5_0_OR_GREATER
        // Override AwaitOnCompleted implementation to prevent boxing in Unity.
#if UNITY_2021_2_OR_NEWER || !UNITY_5_5_OR_NEWER // C# 9 added in 2021.2. We can also use this in non-Unity library since CIL has supported function pointers forever.
        internal unsafe abstract class AwaitOverrider<T> where T : INotifyCompletion
        {
#pragma warning disable IDE0044 // Add readonly modifier
            private static delegate*<ref T, PromiseRefBase, void> _awaitOverrider;
#pragma warning restore IDE0044 // Add readonly modifier

            [MethodImpl(InlineOption)]
            internal static bool IsOverridden()
            {
                return _awaitOverrider != null;
            }

            [MethodImpl(InlineOption)]
            internal static void Create<TAwaiter>() where TAwaiter : struct, T, ICriticalNotifyCompletion, IPromiseAwaiter
            {
                AwaitOverriderImpl<TAwaiter>.Create();
            }

            [MethodImpl(InlineOption)]
            internal static void AwaitOnCompletedInternal(ref T awaiter, PromiseRefBase asyncPromiseRef)
            {
                _awaitOverrider(ref awaiter, asyncPromiseRef);
            }

            private sealed class AwaitOverriderImpl<TAwaiter> : AwaitOverrider<TAwaiter> where TAwaiter : struct, T, ICriticalNotifyCompletion, IPromiseAwaiter
            {
                [MethodImpl(InlineOption)]
                internal static void Create()
                {
                    // This is called multiple times in IL2CPP, but we don't need a null check since the function pointer doesn't allocate.
                    _awaitOverrider = &AwaitOnCompletedVirt;
                }

                [MethodImpl(InlineOption)]
                private static void AwaitOnCompletedVirt(ref TAwaiter awaiter, PromiseRefBase asyncPromiseRef)
                {
                    awaiter.AwaitOnCompletedInternal(asyncPromiseRef);
                }
            }
        }
#else // UNITY_2021_2_OR_NEWER || !UNITY_5_5_OR_NEWER
        internal abstract class AwaitOverrider<T> where T : INotifyCompletion
        {
            private static AwaitOverrider<T> _awaitOverrider;

            [MethodImpl(InlineOption)]
            internal static bool IsOverridden()
            {
                return _awaitOverrider != null;
            }

            [MethodImpl(InlineOption)]
            internal static void Create<TAwaiter>() where TAwaiter : struct, T, ICriticalNotifyCompletion, IPromiseAwaiter
            {
                AwaitOverriderImpl<TAwaiter>.Create();
            }

            [MethodImpl(InlineOption)]
            internal static void AwaitOnCompletedInternal(ref T awaiter, PromiseRefBase asyncPromiseRef)
            {
                _awaitOverrider.AwaitOnCompletedVirt(ref awaiter, asyncPromiseRef);
            }

            protected abstract void AwaitOnCompletedVirt(ref T awaiter, PromiseRefBase asyncPromiseRef);

            private sealed class AwaitOverriderImpl<TAwaiter> : AwaitOverrider<TAwaiter> where TAwaiter : struct, T, ICriticalNotifyCompletion, IPromiseAwaiter
            {
                [MethodImpl(InlineOption)]
                internal static void Create()
                {
#if ENABLE_IL2CPP // This is called multiple times in IL2CPP, so check for null.
                    if (_awaitOverrider == null)
#endif
                    {
                        _awaitOverrider = new AwaitOverriderImpl<TAwaiter>();
                    }
                }

                [MethodImpl(InlineOption)]
                protected override void AwaitOnCompletedVirt(ref TAwaiter awaiter, PromiseRefBase asyncPromiseRef)
                {
                    awaiter.AwaitOnCompletedInternal(asyncPromiseRef);
                }
            }
        }
#endif // UNITY_2021_2_OR_NEWER || !UNITY_5_5_OR_NEWER
#endif // !NET5_0_OR_GREATER

        [MethodImpl(InlineOption)]
        internal static void ValidateNullId(short promiseId, int skipFrames)
        {
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
            if (promiseId != ValidIdFromApi)
            {
                ThrowInvalidAwait(skipFrames + 1);
            }
#endif
        }

        internal static void ValidateId(short promiseId, PromiseRefBase _ref, int skipFrames)
        {
            if (promiseId != _ref.Id)
            {
                ThrowInvalidAwait(skipFrames + 1);
            }
        }

        internal static bool GetIsCompleted(Promise promise, int skipFrames)
        {
            if (promise._target._ref == null)
            {
                if (promise._target.Id != ValidIdFromApi)
                {
                    ThrowInvalidAwait(skipFrames + 1);
                }
                return true;
            }
            return promise._target._ref.GetIsCompleted(promise._target.Id);
        }

        private static void ThrowInvalidAwait(int skipFrames)
        {
            throw new InvalidOperationException("Cannot await a forgotten promise or a non-preserved promise more than once.", GetFormattedStacktrace(skipFrames + 1));
        }
    }

    namespace Async.CompilerServices
    {
#if !NET5_0_OR_GREATER
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
#endif // !NET5_0_OR_GREATER

        /// <summary>
        /// Used to support the await keyword.
        /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode]
#endif
        public
#if CSHARP_7_3_OR_NEWER
            readonly
#endif
            partial struct PromiseAwaiterVoid : ICriticalNotifyCompletion, Internal.IPromiseAwaiter
        {
            private readonly PromiseAwaiter<Internal.VoidResult> _awaiter;

            /// <summary>
            /// Internal use.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            internal PromiseAwaiterVoid(Promise promise)
            {
                _awaiter = new PromiseAwaiter<Internal.VoidResult>(promise._target);
                CreateOverride();
            }

            static partial void CreateOverride();

            public bool IsCompleted
            {
                [MethodImpl(Internal.InlineOption)]
                get
                {
                    return _awaiter.IsCompleted;
                }
            }

            [MethodImpl(Internal.InlineOption)]
            public void GetResult()
            {
                var promise = _awaiter._promise;
                var _ref = promise._ref;
                if (_ref == null)
                {
                    Internal.ValidateNullId(promise.Id, 1);
                    return;
                }
                var state = _ref.State;
                if (state == Promise.State.Resolved)
                {
                    _ref.GetResult<Internal.VoidResult>(promise.Id);
                    return;
                }
#if NET_LEGACY
                _ref.Throw(state, promise.Id);
#else
                _ref.GetExceptionDispatchInfo(state, promise.Id).Throw();
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            public void OnCompleted(Action continuation)
            {
                _awaiter.OnCompleted(continuation);
            }

            [MethodImpl(Internal.InlineOption)]
            public void UnsafeOnCompleted(Action continuation)
            {
                _awaiter.UnsafeOnCompleted(continuation);
            }

            [MethodImpl(Internal.InlineOption)]
            void Internal.IPromiseAwaiter.AwaitOnCompletedInternal(Internal.PromiseRefBase asyncPromiseRef)
            {
                asyncPromiseRef.HookupAwaiter(_awaiter._promise._ref, _awaiter._promise.Id);
            }
        } // struct PromiseAwaiterVoid

        /// <summary>
        /// Used to support the await keyword.
        /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode]
#endif
        public
#if CSHARP_7_3_OR_NEWER
            readonly
#endif
            partial struct PromiseAwaiter<T> : ICriticalNotifyCompletion, Internal.IPromiseAwaiter
        {
            internal readonly Promise<T> _promise;

            /// <summary>
            /// Internal use.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            internal PromiseAwaiter(Promise<T> promise)
            {
                // TODO: check for null and use a sentinel PromiseRef so the other calls don't need to check for null.
                // It may be better to have the sentinel on the promise itself, rather than the awaiters, so none of the APIs need to check for null.
                _promise = promise;
                CreateOverride();
            }

            static partial void CreateOverride();

            public bool IsCompleted
            {
                [MethodImpl(Internal.InlineOption)]
                get
                {
                    return Internal.GetIsCompleted(_promise, 1);
                }
            }

            [MethodImpl(Internal.InlineOption)]
            public T GetResult()
            {
                var promise = _promise;
                var _ref = promise._ref;
                if (_ref == null)
                {
                    Internal.ValidateNullId(promise.Id, 1);
                    return promise.Result;
                }
                var state = _ref.State;
                if (state == Promise.State.Resolved)
                {
                    return _ref.GetResult<T>(promise.Id);
                }
#if NET_LEGACY
                _ref.Throw(state, promise.Id);
#else
                _ref.GetExceptionDispatchInfo(state, promise.Id).Throw();
#endif
                throw new Exception(); // This will never be reached, but the compiler needs help understanding that.
            }

            [MethodImpl(Internal.InlineOption)]
            public void OnCompleted(Action continuation)
            {
                ValidateArgument(continuation, "continuation", 1);
                var promise = _promise;
                if (promise._ref == null)
                {
                    Internal.ValidateNullId(promise.Id, 1);
                    continuation();
                    return;
                }
                promise._ref.OnCompleted(continuation, promise.Id);
            }

            [MethodImpl(Internal.InlineOption)]
            public void UnsafeOnCompleted(Action continuation)
            {
                OnCompleted(continuation);
            }

            [MethodImpl(Internal.InlineOption)]
            void Internal.IPromiseAwaiter.AwaitOnCompletedInternal(Internal.PromiseRefBase asyncPromiseRef)
            {
                asyncPromiseRef.HookupAwaiter(_promise._ref, _promise.Id);
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
        /// Used to support the await keyword.
        /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode]
#endif
        public
#if CSHARP_7_3_OR_NEWER
            readonly
#endif
            partial struct PromiseProgressAwaiterVoid : ICriticalNotifyCompletion, Internal.IPromiseAwaiter
        {
            private readonly PromiseProgressAwaiter<Internal.VoidResult> _awaiter;

            /// <summary>
            /// Internal use.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            internal PromiseProgressAwaiterVoid(Promise promise, float minProgress, float maxProgress)
            {
                _awaiter = new PromiseProgressAwaiter<Internal.VoidResult>(promise._target, minProgress, maxProgress);
                CreateOverride();
            }

            static partial void CreateOverride();

            [MethodImpl(Internal.InlineOption)]
            public PromiseProgressAwaiterVoid GetAwaiter()
            {
                return this;
            }

            public bool IsCompleted
            {
                [MethodImpl(Internal.InlineOption)]
                get
                {
                    return _awaiter.IsCompleted;
                }
            }

            [MethodImpl(Internal.InlineOption)]
            public void GetResult()
            {
                var promise = _awaiter._promise;
                var _ref = promise._ref;
                if (_ref == null)
                {
                    Internal.ValidateNullId(promise.Id, 1);
                    return;
                }
                var state = _ref.State;
                if (state == Promise.State.Resolved)
                {
                    _ref.GetResult<Internal.VoidResult>(promise.Id);
                    return;
                }
#if NET_LEGACY
                _ref.Throw(state, promise.Id);
#else
                _ref.GetExceptionDispatchInfo(state, promise.Id).Throw();
#endif
            }

            [MethodImpl(Internal.InlineOption)]
            public void OnCompleted(Action continuation)
            {
                _awaiter.OnCompleted(continuation);
            }

            [MethodImpl(Internal.InlineOption)]
            public void UnsafeOnCompleted(Action continuation)
            {
                _awaiter.UnsafeOnCompleted(continuation);
            }

            [MethodImpl(Internal.InlineOption)]
            void Internal.IPromiseAwaiter.AwaitOnCompletedInternal(Internal.PromiseRefBase asyncPromiseRef)
            {
                asyncPromiseRef.HookupAwaiterWithProgress(_awaiter._promise._ref, _awaiter._promise.Id, _awaiter._promise.Depth, _awaiter._minProgress, _awaiter._maxProgress);
            }
        } // struct PromiseAwaiterVoid

        /// <summary>
        /// Used to support the await keyword.
        /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode]
#endif
        public
#if CSHARP_7_3_OR_NEWER
            readonly
#endif
            partial struct PromiseProgressAwaiter<T> : ICriticalNotifyCompletion, Internal.IPromiseAwaiter
        {
            internal readonly Promise<T> _promise;
            internal readonly float _minProgress;
            internal readonly float _maxProgress;

            /// <summary>
            /// Internal use.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            internal PromiseProgressAwaiter(Promise<T> promise, float minProgress, float maxProgress)
            {
                _promise = promise;
                _minProgress = minProgress;
                _maxProgress = maxProgress;
                CreateOverride();
            }

            static partial void CreateOverride();

            [MethodImpl(Internal.InlineOption)]
            public PromiseProgressAwaiter<T> GetAwaiter()
            {
                return this;
            }

            public bool IsCompleted
            {
                [MethodImpl(Internal.InlineOption)]
                get
                {
                    return Internal.GetIsCompleted(_promise, 1);
                }
            }

            [MethodImpl(Internal.InlineOption)]
            public T GetResult()
            {
                var promise = _promise;
                var _ref = promise._ref;
                if (_ref == null)
                {
                    Internal.ValidateNullId(promise.Id, 1);
                    return promise.Result;
                }
                var state = _ref.State;
                if (state == Promise.State.Resolved)
                {
                    return _ref.GetResult<T>(promise.Id);
                }
#if NET_LEGACY
                _ref.Throw(state, promise.Id);
#else
                _ref.GetExceptionDispatchInfo(state, promise.Id).Throw();
#endif
                throw new Exception(); // This will never be reached, but the compiler needs help understanding that.
            }

            [MethodImpl(Internal.InlineOption)]
            public void OnCompleted(Action continuation)
            {
                ValidateArgument(continuation, "continuation", 1);
                var promise = _promise;
                if (promise._ref == null)
                {
                    Internal.ValidateNullId(promise.Id, 1);
                    continuation();
                    return;
                }
                promise._ref.OnCompleted(continuation, promise.Id);
            }

            [MethodImpl(Internal.InlineOption)]
            public void UnsafeOnCompleted(Action continuation)
            {
                OnCompleted(continuation);
            }

            [MethodImpl(Internal.InlineOption)]
            void Internal.IPromiseAwaiter.AwaitOnCompletedInternal(Internal.PromiseRefBase asyncPromiseRef)
            {
                asyncPromiseRef.HookupAwaiterWithProgress(_promise._ref, _promise.Id, _promise.Depth, _minProgress, _maxProgress);
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

    partial struct Promise
    {
        /// <summary>
        /// Used to support the await keyword.
        /// </summary>
        [MethodImpl(Internal.InlineOption)]
        public PromiseAwaiterVoid GetAwaiter()
        {
            ValidateOperation(1);
            return new PromiseAwaiterVoid(this);
        }

        /// <summary>
        /// Used to support reporting progress to the async Promise function. The progress reported will be scaled from minProgress to maxProgress. Both values must be between 0 and 1 inclusive.
        /// <para/> Use as `await promise.AwaitWithprogress(min, max);`
        /// </summary>
        public PromiseProgressAwaiterVoid AwaitWithProgress(float minProgress, float maxProgress)
        {
            ValidateOperation(1);
            Internal.ValidateProgressValue(minProgress, "minProgress", 1);
            Internal.ValidateProgressValue(maxProgress, "maxProgress", 1);
            return new PromiseProgressAwaiterVoid(this, minProgress, maxProgress);
        }
    }

    partial struct Promise<T>
    {
        /// <summary>
        /// Used to support the await keyword.
        /// </summary>
        [MethodImpl(Internal.InlineOption)]
        public PromiseAwaiter<T> GetAwaiter()
        {
            ValidateOperation(1);
            return new PromiseAwaiter<T>(this);
        }

        /// <summary>
        /// Used to support reporting progress to the async Promise function. The progress reported will be lerped from <paramref name="minProgress"/> to <paramref name="maxProgress"/>. Both values must be between 0 and 1 inclusive.
        /// <para/> Use as `await promise.AwaitWithProgress(min, max);`
        /// </summary>
        public PromiseProgressAwaiter<T> AwaitWithProgress(float minProgress, float maxProgress)
        {
            ValidateOperation(1);
            Internal.ValidateProgressValue(minProgress, "minProgress", 1);
            Internal.ValidateProgressValue(maxProgress, "maxProgress", 1);
            return new PromiseProgressAwaiter<T>(this, minProgress, maxProgress);
        }
    }
}