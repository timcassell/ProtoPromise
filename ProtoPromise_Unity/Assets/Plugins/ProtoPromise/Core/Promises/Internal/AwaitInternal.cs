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
#pragma warning disable IDE0044 // Add readonly modifier

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;

namespace Proto.Promises
{
    partial class Internal
    {
        partial class PromiseRefBase
        {
            [MethodImpl(InlineOption)]
            internal void GetResultForAwaiterVoid(short promiseId)
            {
                ValidateId(promiseId, this, 2);
                MaybeDispose();
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            internal ExceptionDispatchInfo GetExceptionDispatchInfo(Promise.State state, short promiseId)
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
                    var exceptionDispatchInfo = _rejectContainerOrPreviousOrLink.UnsafeAs<IRejectContainer>().GetExceptionDispatchInfo();
                    MaybeDispose();
                    return exceptionDispatchInfo;
                }
                throw new InvalidOperationException("PromiseAwaiter.GetResult() is only valid when the promise is completed.", GetFormattedStacktrace(2));
            }

            [MethodImpl(InlineOption)]
            internal void OnCompleted(Action continuation, short promiseId)
            {
                HookupNewWaiter(promiseId, AwaiterRef<DelegateVoidVoid>.GetOrCreate(new DelegateVoidVoid(continuation)));
            }

            [MethodImpl(InlineOption)]
            internal Promise.ResultContainer GetResultContainerAndMaybeDispose(short promiseId)
            {
                if (promiseId != Id | State == Promise.State.Pending)
                {
                    ValidateId(promiseId, this, 2);
                    throw new InvalidOperationException("PromiseAwaiter.GetResult() is only valid when the promise is completed.", GetFormattedStacktrace(2));
                }
                return GetResultContainerAndMaybeDispose();
            }

            [MethodImpl(InlineOption)]
            internal Promise.ResultContainer GetResultContainerAndMaybeDispose()
            {
                var resultContainer = new Promise.ResultContainer(_rejectContainerOrPreviousOrLink, State);
                SuppressRejection = true;
                MaybeDispose();
                return resultContainer;
            }

            partial class PromiseRef<TResult>
            {
                [MethodImpl(InlineOption)]
                internal TResult GetResultForAwaiter(short promiseId)
                {
                    ValidateId(promiseId, this, 2);
                    return GetResultAndMaybeDispose();
                }

                [MethodImpl(InlineOption)]
                internal TResult GetResultAndMaybeDispose()
                {
                    TResult result = _result;
                    MaybeDispose();
                    return result;
                }

                [MethodImpl(InlineOption)]
                new internal Promise<TResult>.ResultContainer GetResultContainerAndMaybeDispose(short promiseId)
                {
                    if (promiseId != Id | State == Promise.State.Pending)
                    {
                        ValidateId(promiseId, this, 2);
                        throw new InvalidOperationException("PromiseAwaiter.GetResult() is only valid when the promise is completed.", GetFormattedStacktrace(2));
                    }
                    return GetResultContainerAndMaybeDispose();
                }

                [MethodImpl(InlineOption)]
                new internal Promise<TResult>.ResultContainer GetResultContainerAndMaybeDispose()
                {
                    var resultContainer = new Promise<TResult>.ResultContainer(_result, _rejectContainerOrPreviousOrLink, State);
                    SuppressRejection = true;
                    MaybeDispose();
                    return resultContainer;
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private sealed class AwaiterRef<TContinuer> : HandleablePromiseBase, ITraceable
                where TContinuer : IAction
            {
#if PROMISE_DEBUG
                CausalityTrace ITraceable.Trace { get; set; }
#endif
                private TContinuer _continuer;

                private AwaiterRef() { }

                [MethodImpl(InlineOption)]
                private static AwaiterRef<TContinuer> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<AwaiterRef<TContinuer>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new AwaiterRef<TContinuer>()
                        : obj.UnsafeAs<AwaiterRef<TContinuer>>();
                }

                [MethodImpl(InlineOption)]
                internal static AwaiterRef<TContinuer> GetOrCreate(TContinuer continuer)
                {
                    var awaiter = GetOrCreate();
                    awaiter._continuer = continuer;
                    SetCreatedStacktrace(awaiter, 3);
                    return awaiter;
                }

                [MethodImpl(InlineOption)]
                private void Dispose()
                {
                    _continuer = default(TContinuer);
                    ObjectPool.MaybeRepool(this);
                }

                private void Invoke()
                {
                    ThrowIfInPool(this);
                    var callback = _continuer;
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

                internal override void Handle(PromiseRefBase handler, object rejectContainer, Promise.State state)
                {
                    ThrowIfInPool(this);
                    handler.SetCompletionState(rejectContainer, state);
                    Invoke();
                }
            }
        }

        internal interface IPromiseAwaiter
        {
            void AwaitOnCompletedInternal(PromiseRefBase asyncPromiseRef, ref PromiseRefBase.AsyncPromiseFields asyncFields);
        }

#if !NETCOREAPP
        // Override AwaitOnCompleted implementation to prevent boxing in Unity.
#if UNITY_2021_2_OR_NEWER || NETSTANDARD2_1_OR_GREATER // Even though CIL has always had function pointers, Unity did not properly support the CIL instructions until C# 9/ .Net Standard 2.1 support was added.
        internal unsafe abstract class AwaitOverrider<T> where T : INotifyCompletion
        {
#pragma warning disable IDE0044 // Add readonly modifier
            private static delegate*<ref T, PromiseRefBase, ref PromiseRefBase.AsyncPromiseFields, void> s_awaitOverrider;
#pragma warning restore IDE0044 // Add readonly modifier

            [MethodImpl(InlineOption)]
            internal static bool IsOverridden()
            {
                return s_awaitOverrider != null;
            }

            [MethodImpl(InlineOption)]
            internal static void Create<TAwaiter>() where TAwaiter : struct, T, ICriticalNotifyCompletion, IPromiseAwaiter
            {
                AwaitOverriderImpl<TAwaiter>.Create();
            }

            [MethodImpl(InlineOption)]
            internal static void AwaitOnCompletedInternal(ref T awaiter, PromiseRefBase asyncPromiseRef, ref PromiseRefBase.AsyncPromiseFields asyncFields)
            {
                s_awaitOverrider(ref awaiter, asyncPromiseRef, ref asyncFields);
            }

            private sealed class AwaitOverriderImpl<TAwaiter> : AwaitOverrider<TAwaiter> where TAwaiter : struct, T, ICriticalNotifyCompletion, IPromiseAwaiter
            {
                [MethodImpl(InlineOption)]
                internal static void Create()
                {
                    // This is called multiple times in IL2CPP, but we don't need a null check since the function pointer doesn't allocate.
                    s_awaitOverrider = &AwaitOnCompletedVirt;
                }

                [MethodImpl(InlineOption)]
                private static void AwaitOnCompletedVirt(ref TAwaiter awaiter, PromiseRefBase asyncPromiseRef, ref PromiseRefBase.AsyncPromiseFields asyncFields)
                {
                    awaiter.AwaitOnCompletedInternal(asyncPromiseRef, ref asyncFields);
                }
            }
        }
#else // UNITY_2021_2_OR_NEWER || NETSTANDARD2_1_OR_GREATER
        internal abstract class AwaitOverrider<T> where T : INotifyCompletion
        {
            private static AwaitOverrider<T> s_awaitOverrider;

            [MethodImpl(InlineOption)]
            internal static bool IsOverridden()
            {
                return s_awaitOverrider != null;
            }

            [MethodImpl(InlineOption)]
            internal static void Create<TAwaiter>() where TAwaiter : struct, T, ICriticalNotifyCompletion, IPromiseAwaiter
            {
                AwaitOverriderImpl<TAwaiter>.Create();
            }

            [MethodImpl(InlineOption)]
            internal static void AwaitOnCompletedInternal(ref T awaiter, PromiseRefBase asyncPromiseRef, ref PromiseRefBase.AsyncPromiseFields asyncFields)
            {
                s_awaitOverrider.AwaitOnCompletedVirt(ref awaiter, asyncPromiseRef, ref asyncFields);
            }

            protected abstract void AwaitOnCompletedVirt(ref T awaiter, PromiseRefBase asyncPromiseRef, ref PromiseRefBase.AsyncPromiseFields asyncFields);

            private sealed class AwaitOverriderImpl<TAwaiter> : AwaitOverrider<TAwaiter> where TAwaiter : struct, T, ICriticalNotifyCompletion, IPromiseAwaiter
            {
                [MethodImpl(InlineOption)]
                internal static void Create()
                {
#if ENABLE_IL2CPP // This is called multiple times in IL2CPP, so check for null.
                    if (s_awaitOverrider == null)
#endif
                    {
                        s_awaitOverrider = new AwaitOverriderImpl<TAwaiter>();
                    }
                }

                [MethodImpl(InlineOption)]
                protected override void AwaitOnCompletedVirt(ref TAwaiter awaiter, PromiseRefBase asyncPromiseRef, ref PromiseRefBase.AsyncPromiseFields asyncFields)
                {
                    awaiter.AwaitOnCompletedInternal(asyncPromiseRef, ref asyncFields);
                }
            }
        }
#endif // UNITY_2021_2_OR_NEWER || NETSTANDARD2_1_OR_GREATER
#endif // !NETCOREAPP

        [MethodImpl(InlineOption)]
        internal static void ValidateId(short promiseId, PromiseRefBase _ref, int skipFrames)
        {
            if (promiseId != _ref.Id)
            {
                ThrowInvalidAwait(skipFrames + 1);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowInvalidAwait(int skipFrames)
        {
            throw new InvalidOperationException("Cannot await a forgotten promise or a non-preserved promise more than once.", GetFormattedStacktrace(skipFrames + 1));
        }
    } // class Internal
} // namespace Proto.Promises