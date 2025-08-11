﻿#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;

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
                    throw Promise.CancelException();
                }
                if (state == Promise.State.Rejected)
                {
                    SuppressRejection = true;
                    var exceptionDispatchInfo = RejectContainer.GetExceptionDispatchInfo();
                    MaybeDispose();
                    return exceptionDispatchInfo;
                }
                throw new InvalidOperationException("PromiseAwaiter.GetResult() is only valid when the promise is completed.", GetFormattedStacktrace(2));
            }

            [MethodImpl(InlineOption)]
            internal void OnCompleted(Action continuation, short promiseId)
                => HookupNewWaiter(promiseId, AwaiterContinuer.GetOrCreate(continuation));

            [MethodImpl(InlineOption)]
            internal void OnCompleted(Action continuation, SynchronizationContext context, short promiseId)
                => HookupNewWaiter(promiseId, ConfiguredAsyncGenericContinuer.GetOrCreate(continuation, context));

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
                var resultContainer = new Promise.ResultContainer(RejectContainer, State);
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
                    var resultContainer = new Promise<TResult>.ResultContainer(_result, RejectContainer, State);
                    SuppressRejection = true;
                    MaybeDispose();
                    return resultContainer;
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private sealed class AwaiterContinuer : HandleablePromiseBase, ITraceable
            {
#if PROMISE_DEBUG
                CausalityTrace ITraceable.Trace { get; set; }
#endif
                private Action _continuation;

                private AwaiterContinuer() { }

                [MethodImpl(InlineOption)]
                private static AwaiterContinuer GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<AwaiterContinuer>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new AwaiterContinuer()
                        : obj.UnsafeAs<AwaiterContinuer>();
                }

                [MethodImpl(InlineOption)]
                internal static AwaiterContinuer GetOrCreate(Action continuation)
                {
                    var continuer = GetOrCreate();
                    continuer._next = null;
                    continuer._continuation = continuation;
                    SetCreatedStacktrace(continuer, 3);
                    return continuer;
                }

                [MethodImpl(InlineOption)]
                private void Dispose()
                {
                    _continuation = null;
                    ObjectPool.MaybeRepool(this);
                }

                internal override void Handle(PromiseRefBase handler, Promise.State state)
                {
                    ThrowIfInPool(this);
                    handler.SetCompletionState(state);
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
            }
        }

        internal interface IPromiseAwareAwaiter
        {
            void AwaitOnCompletedInternal(PromiseRefBase asyncPromiseRef);
        }

#if !NETCOREAPP
        // Override AwaitOnCompleted implementation to prevent boxing in Unity.
#if UNITY_2021_2_OR_NEWER || NETSTANDARD2_1_OR_GREATER // Even though CIL has always had function pointers, Unity did not properly support the CIL instructions until C# 9/ .Net Standard 2.1 support was added.
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal unsafe static class AwaitOverrider<TAwaiter> where TAwaiter : INotifyCompletion
        {
            internal static delegate*<ref TAwaiter, PromiseRefBase, Action, void> s_awaitFunc = &DefaultAwaitOnCompleted;

            [MethodImpl(InlineOption)]
            private static void DefaultAwaitOnCompleted(ref TAwaiter awaiter, PromiseRefBase asyncPromiseRef, Action continuation)
                => awaiter.OnCompleted(continuation);

            [MethodImpl(InlineOption)]
            internal static void AwaitOnCompleted(ref TAwaiter awaiter, PromiseRefBase asyncPromiseRef, Action continuation)
                // We call the function without a branch. If the awaiter is not a known awaiter type, the default function will be called.
                => s_awaitFunc(ref awaiter, asyncPromiseRef, continuation);
        }
        
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal unsafe static class CriticalAwaitOverrider<TAwaiter> where TAwaiter : ICriticalNotifyCompletion
        {
            internal static delegate*<ref TAwaiter, PromiseRefBase, Action, void> s_awaitFunc = &DefaultAwaitOnCompleted;

            [MethodImpl(InlineOption)]
            private static void DefaultAwaitOnCompleted(ref TAwaiter awaiter, PromiseRefBase asyncPromiseRef, Action continuation)
                => awaiter.UnsafeOnCompleted(continuation);

            [MethodImpl(InlineOption)]
            internal static void AwaitOnCompleted(ref TAwaiter awaiter, PromiseRefBase asyncPromiseRef, Action continuation)
                // We call the function without a branch. If the awaiter is not a known awaiter type, the default function will be called.
                => s_awaitFunc(ref awaiter, asyncPromiseRef, continuation);
        }
        
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal unsafe static class AwaitOverriderImpl<TAwaiter> where TAwaiter : struct, ICriticalNotifyCompletion, IPromiseAwareAwaiter
        {
            [MethodImpl(InlineOption)]
            internal static void Create()
            {
                // This is called multiple times in IL2CPP, but we don't need a null check since the function pointer doesn't allocate.
                AwaitOverrider<TAwaiter>.s_awaitFunc = &AwaitOnCompletedOverride;
                CriticalAwaitOverrider<TAwaiter>.s_awaitFunc = &AwaitOnCompletedOverride;
            }

            [MethodImpl(InlineOption)]
            private static void AwaitOnCompletedOverride(ref TAwaiter awaiter, PromiseRefBase asyncPromiseRef, Action continuation)
                => awaiter.AwaitOnCompletedInternal(asyncPromiseRef);
        }
#else // UNITY_2021_2_OR_NEWER || NETSTANDARD2_1_OR_GREATER
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal abstract class AwaitOverrider<TAwaiter> where TAwaiter : INotifyCompletion
        {
            internal static AwaitOverrider<TAwaiter> s_awaitOverrider;

            [MethodImpl(InlineOption)]
            internal static void AwaitOnCompleted(ref TAwaiter awaiter, PromiseRefBase asyncPromiseRef, Action continuation)
            {
                if (s_awaitOverrider != null)
                {
                    s_awaitOverrider.AwaitOnCompletedVirt(ref awaiter, asyncPromiseRef);
                }
                else
                {
                    awaiter.OnCompleted(continuation);
                }
            }

            internal abstract void AwaitOnCompletedVirt(ref TAwaiter awaiter, PromiseRefBase asyncPromiseRef);
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal static class CriticalAwaitOverrider<TAwaiter> where TAwaiter : ICriticalNotifyCompletion
        {
            [MethodImpl(InlineOption)]
            internal static void AwaitOnCompleted(ref TAwaiter awaiter, PromiseRefBase asyncPromiseRef, Action continuation)
            {
                if (AwaitOverrider<TAwaiter>.s_awaitOverrider != null)
                {
                    AwaitOverrider<TAwaiter>.s_awaitOverrider.AwaitOnCompletedVirt(ref awaiter, asyncPromiseRef);
                }
                else
                {
                    awaiter.UnsafeOnCompleted(continuation);
                }
            }
        }
        
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal sealed class AwaitOverriderImpl<TAwaiter> : AwaitOverrider<TAwaiter> where TAwaiter : struct, ICriticalNotifyCompletion, IPromiseAwareAwaiter
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
            internal override void AwaitOnCompletedVirt(ref TAwaiter awaiter, PromiseRefBase asyncPromiseRef)
            {
                awaiter.AwaitOnCompletedInternal(asyncPromiseRef);
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