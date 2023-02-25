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

namespace Proto.Promises
{
    partial class AsyncLazy<T>
    {
        // Must be volatile to prevent out-of-order memory read/write with the result.
        // This is set to null when we have successfully obtained the result, so we will have zero lock contention on future accesses,
        // and we release all resources that are no longer needed for lazy initialization.
        volatile private LazyPromise _lazyPromise;
        private T _result;

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        private sealed class LazyPromise : Internal.PromiseRefBase.LazyPromise<T>
        {
            private readonly AsyncLazy<T> _owner;
            private readonly Func<Promise<T>> _factory;
            private PromiseMultiAwait<T> _preservedPromise;
            private bool _isComplete;
            internal bool _isStarted;

            internal LazyPromise(AsyncLazy<T> owner, Func<Promise<T>> factory)
            {
                // We don't use object pooling for this type.
                _owner = owner;
                _factory = factory;
                // Suppress finalize to prevent errant errors reported if this is never started.
                GC.SuppressFinalize(this);
            }

            internal override void MaybeDispose()
            {
                // Do nothing, we don't use object pooling for this type. This is because another thread could try to read from it after we repooled it.
            }

            private static Promise<T> GetDuplicate(PromiseMultiAwait<T> preservedPromise)
            {
                // Same thing as Promise.Duplicate(), but more direct.
                var p = preservedPromise;
                var duplicate = p.GetDuplicateT(p.Id, 0);
                return new Promise<T>(duplicate, duplicate.Id, 0);
            }

            internal Promise<T> GetOrStartPromise()
            {
                PromiseMultiAwait<T> preservedPromise;
                lock (this)
                {
                    if (_isComplete)
                    {
                        return Promise<T>.Resolved(_result);
                    }

                    if (_isStarted)
                    {
                        return GetDuplicate(_preservedPromise);
                    }

                    _isStarted = true;
                    // Same thing as Promise.Preserve(), but more direct.
                    _preservedPromise = preservedPromise = PromiseMultiAwait<T>.GetOrCreate(0);
                    Reset(0);
                    HookupNewPromise(Id, _preservedPromise);
                    // Exit the lock before invoking the factory.
                }
                var promise = GetDuplicate(preservedPromise);
                Start(_factory);
                return promise;
            }

            internal override void Handle(Internal.PromiseRefBase handler, object rejectContainer, Promise.State state)
            {
                handler.SetCompletionState(rejectContainer, state);
                _result = handler.GetResult<T>();
                handler.MaybeDispose();
                OnComplete(rejectContainer, state);
            }

            protected override void OnComplete(object rejectContainer, Promise.State state)
            {
                PromiseMultiAwait<T> preservedPromise;
                if (state != Promises.Promise.State.Resolved)
                {
                    lock (this)
                    {
                        // Reset the state so that the factory will be ran again the next time the Promise is accessed.
                        preservedPromise = _preservedPromise;
                        _preservedPromise = null;
                        _isStarted = false;
                    }

                    ForgetPreserved(preservedPromise);
                    HandleNextInternal(rejectContainer, state);
                    return;
                }

                // Release resources only when we have obtained the result successfully.
                _owner._result = _result;
                // This is a volatile write, so we don't need a full memory barrier to prevent the result write from moving after it.
                _owner._lazyPromise = null;

                lock (this)
                {
                    preservedPromise = _preservedPromise;
                    _preservedPromise = null;
                    _isComplete = true;
                }

                ForgetPreserved(preservedPromise);
                HandleNextInternal(rejectContainer, state);
            }
        } // class LazyPromise
    } // class AsyncLazy<T>

    partial class Internal
    {
        partial class PromiseRefBase
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal abstract class LazyPromise<TResult> : PromiseWaitPromise<TResult>
            {
                [MethodImpl(InlineOption)]
                protected static void ForgetPreserved(PromiseMultiAwait<TResult> preservedPromise)
                {
                    preservedPromise.OnForget(preservedPromise.Id);
                }

                [MethodImpl(InlineOption)]
                protected void Start(Func<Promise<TResult>> factory)
                {
                    SetCurrentInvoker(this);
                    try
                    {
                        WaitFor(factory.Invoke());
                    }
                    catch (OperationCanceledException)
                    {
                        OnComplete(null, Promise.State.Canceled);
                    }
                    catch (Exception e)
                    {
                        var rejectContainer = CreateRejectContainer(e, int.MinValue, null, this);
                        OnComplete(rejectContainer, Promise.State.Rejected);
                    }
                    ClearCurrentInvoker();
                }

                protected abstract void OnComplete(object rejectContainer, Promise.State state);

                private void WaitFor(Promise<TResult> other)
                {
                    ValidateReturn(other);
                    if (other._ref != null)
                    {
                        SetSecondPreviousAndWaitFor_Lazy(other._ref, other._id, null);
                        return;
                    }
                    _result = other._result;
#if PROMISE_PROGRESS
                    SetSecondPreviousAndMaybeHookupProgress_Protected(null, null);
#endif
                    OnComplete(null, Promise.State.Resolved);
                }

                private void SetSecondPreviousAndWaitFor_Lazy(PromiseRefBase secondPrevious, short id, PromiseRefBase handler)
                {
                    HandleablePromiseBase previousWaiter;
                    PromiseRefBase promiseSingleAwait = secondPrevious.AddWaiter(id, this, out previousWaiter);
#if PROMISE_PROGRESS
                    SetSecondPreviousAndMaybeHookupProgress_Protected(secondPrevious, handler);
#endif
                    if (previousWaiter != PendingAwaitSentinel.s_instance)
                    {
                        VerifyAndHandleSelf_Lazy(secondPrevious, promiseSingleAwait);
                    }
                }

                // This is rare, only happens when the promise already completed (usually an already completed promise is not backed by a reference), or if a promise is incorrectly awaited twice.
                [MethodImpl(MethodImplOptions.NoInlining)]
                protected void VerifyAndHandleSelf_Lazy(PromiseRefBase other, PromiseRefBase promiseSingleAwait)
                {
                    if (!VerifyWaiter(promiseSingleAwait))
                    {
                        throw new InvalidReturnException("Cannot await or forget a forgotten promise or a non-preserved promise more than once.", string.Empty);
                    }

                    other.WaitUntilStateIsNotPending();
                    _result = other.GetResult<TResult>();
                    OnComplete(other._rejectContainerOrPreviousOrLink, other.State);
                }
            }
        } // class PromiseRefBase
    } // class Internal
}