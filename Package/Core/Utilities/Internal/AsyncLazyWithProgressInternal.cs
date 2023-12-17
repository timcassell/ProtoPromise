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

#pragma warning disable IDE0018 // Inline variable declaration
#pragma warning disable IDE0034 // Simplify 'default' expression

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Proto.Promises
{
    partial class AsyncLazyWithProgress<T>
    {
        // Must be volatile to prevent out-of-order memory read/write with the result.
        // This is set to null when we have successfully obtained the result, so we will have zero lock contention on future accesses,
        // and we release all resources that are no longer needed for lazy initialization.
        volatile private LazyFields _lazyFields;
        private T _result;

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        private sealed class LazyFields
        {
            internal Func<ProgressToken, Promise<T>> _factory;
            internal LazyWithProgressPromise _lazyPromise;

            internal bool IsStarted
            {
                [MethodImpl(Internal.InlineOption)]
                get { return _lazyPromise != null; }
            }

            internal bool IsComplete
            {
                [MethodImpl(Internal.InlineOption)]
                get { return _factory == null; }
            }

            internal LazyFields(Func<ProgressToken, Promise<T>> factory)
            {
                _factory = factory;
            }

            [MethodImpl(Internal.InlineOption)]
            internal Promise<T> GetOrStartPromise(AsyncLazyWithProgress<T> owner, ProgressToken progressToken)
            {
                return LazyWithProgressPromise.GetOrStartPromise(owner, this, progressToken);
            }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        private sealed class LazyWithProgressPromise : Internal.PromiseRefBase.LazyWithProgressPromise<T>
        {
            private AsyncLazyWithProgress<T> _owner;
            internal PromiseMultiAwait<T> _preservedPromise;
            internal Progress.MultiHandler _progressHandler;

            [MethodImpl(Internal.InlineOption)]
            private static LazyWithProgressPromise GetOrCreate()
            {
                var obj = Internal.ObjectPool.TryTakeOrInvalid<LazyWithProgressPromise>();
                return obj == InvalidAwaitSentinel.s_instance
                    ? new LazyWithProgressPromise()
                    : obj.UnsafeAs<LazyWithProgressPromise>();
            }

            [MethodImpl(Internal.InlineOption)]
            private static LazyWithProgressPromise GetOrCreate(AsyncLazyWithProgress<T> owner)
            {
                var promise = GetOrCreate();
                promise._owner = owner;
                promise.Reset(0);
                return promise;
            }

            internal override void MaybeDispose()
            {
                Dispose();
                Internal.ObjectPool.MaybeRepool(this);
            }

            private static Promise<T> GetDuplicate(PromiseMultiAwait<T> preservedPromise)
            {
                // Same thing as Promise.Duplicate(), but more direct.
                var p = preservedPromise;
                var duplicate = p.GetDuplicateT(p.Id, 0);
                return new Promise<T>(duplicate, duplicate.Id, 0);
            }

            internal static Promise<T> GetOrStartPromise(AsyncLazyWithProgress<T> owner, LazyFields lazyFields, ProgressToken progressToken)
            {
                LazyWithProgressPromise lazyPromise;
                PromiseMultiAwait<T> preservedPromise;
                lock (lazyFields)
                {
                    if (lazyFields.IsComplete)
                    {
                        return Promise<T>.Resolved(owner._result);
                    }

                    if (lazyFields.IsStarted)
                    {
                        lazyFields._lazyPromise._progressHandler.Add(progressToken);
                        return GetDuplicate(lazyFields._lazyPromise._preservedPromise);
                    }

                    lazyPromise = GetOrCreate(owner);
                    lazyFields._lazyPromise = lazyPromise;
                    lazyPromise._progressHandler = Progress.NewMultiHandler();
                    // Same thing as Promise.Preserve(), but more direct.
                    lazyPromise._preservedPromise = preservedPromise = PromiseMultiAwait<T>.GetOrCreate(0);
                    lazyPromise.HookupNewPromise(lazyPromise.Id, preservedPromise);
                    // Exit the lock before invoking the factory.
                }
                var promise = GetDuplicate(preservedPromise);
                lazyPromise._progressHandler.Add(progressToken);
                lazyPromise.Start(lazyFields._factory, lazyPromise._progressHandler.Token);
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
                var lazyFields = _owner._lazyFields;
                PromiseMultiAwait<T> preservedPromise;
                Progress.MultiHandler progressHandler;
                if (state != Promise.State.Resolved)
                {
                    lock (lazyFields)
                    {
                        // Reset the state so that the factory will be ran again the next time GetResultAsync is called.
                        preservedPromise = _preservedPromise;
                        _preservedPromise = null;
                        progressHandler = _progressHandler;
                        _progressHandler = default(Progress.MultiHandler);
                        lazyFields._lazyPromise = null;
                    }

                    progressHandler.Dispose();
                    preservedPromise.Forget(preservedPromise.Id);
                    HandleNextInternal(rejectContainer, state);
                    return;
                }

                // Release resources only when we have obtained the result successfully.
                _owner._result = _result;
                // This is a volatile write, so we don't need a full memory barrier to prevent the result write from moving after it.
                _owner._lazyFields = null;

                lock (lazyFields)
                {
                    preservedPromise = _preservedPromise;
                    _preservedPromise = null;
                    progressHandler = _progressHandler;
                    _progressHandler = default(Progress.MultiHandler);
                    lazyFields._factory = null;
                }

                progressHandler.Token.Report(1d);
                progressHandler.Dispose();
                preservedPromise.Forget(preservedPromise.Id);
                HandleNextInternal(rejectContainer, state);
            }
        } // class LazyWithProgressPromise
    } // class AsyncLazyWithProgress<T>

    partial class Internal
    {
        partial class PromiseRefBase
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal abstract class LazyWithProgressPromise<TResult> : PromiseWaitPromise<TResult>
            {
                [MethodImpl(InlineOption)]
                protected void Start(Func<ProgressToken, Promise<TResult>> factory, ProgressToken progressToken)
                {
                    SetCurrentInvoker(this);
                    try
                    {
                        WaitFor_Lazy(factory.Invoke(progressToken));
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

                // This is the same logic as the normal WaitFor, except this will call OnComplete if necessary, instead of HandleNextInternal.
                private void WaitFor_Lazy(Promise<TResult> other)
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
                private void VerifyAndHandleSelf_Lazy(PromiseRefBase other, PromiseRefBase promiseSingleAwait)
                {
                    if (!VerifyWaiter(promiseSingleAwait))
                    {
                        throw new InvalidReturnException("Cannot await or forget a forgotten promise or a non-preserved promise more than once.", string.Empty);
                    }

                    other.WaitUntilStateIsNotPending();
                    _result = other.GetResult<TResult>();
                    var rejectContainer = other._rejectContainerOrPreviousOrLink;
                    var state = other.State;
                    other.MaybeDispose();
                    OnComplete(rejectContainer, state);
                }
            }
        } // class PromiseRefBase
    } // class Internal
}