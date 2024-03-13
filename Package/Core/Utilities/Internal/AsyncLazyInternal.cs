#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

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
        volatile private LazyFieldsBase _lazyFields;
        private T _result;

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        private abstract class LazyFieldsBase
        {
            internal Internal.PromiseRefBase.LazyPromise<T> _lazyPromise;

            internal bool IsStarted
            {
                [MethodImpl(Internal.InlineOption)]
                get => _lazyPromise != null;
            }

            internal abstract Promise<T> GetOrStartPromise(AsyncLazy<T> owner, ProgressToken progressToken);
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        private sealed class LazyFieldsNoProgress : LazyFieldsBase
        {
            internal Func<Promise<T>> _factory;

            internal bool IsComplete
            {
                [MethodImpl(Internal.InlineOption)]
                get => _factory == null;
            }

            internal LazyFieldsNoProgress(Func<Promise<T>> factory)
            {
                _factory = factory;
            }

            internal override Promise<T> GetOrStartPromise(AsyncLazy<T> owner, ProgressToken progressToken)
                => LazyPromiseNoProgress.GetOrStartPromise(owner, this);
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        private sealed class LazyFieldsWithProgress : LazyFieldsBase
        {
            internal Func<ProgressToken, Promise<T>> _factory;

            internal bool IsComplete
            {
                [MethodImpl(Internal.InlineOption)]
                get => _factory == null;
            }

            internal LazyFieldsWithProgress(Func<ProgressToken, Promise<T>> factory)
            {
                _factory = factory;
            }

            internal override Promise<T> GetOrStartPromise(AsyncLazy<T> owner, ProgressToken progressToken)
                => LazyWithProgressPromise.GetOrStartPromise(owner, this, progressToken);
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        private sealed class LazyPromiseNoProgress : Internal.PromiseRefBase.LazyPromise<T>
        {
            private AsyncLazy<T> _owner;
            internal PromiseMultiAwait<T> _preservedPromise;

            [MethodImpl(Internal.InlineOption)]
            private static LazyPromiseNoProgress GetOrCreate()
            {
                var obj = Internal.ObjectPool.TryTakeOrInvalid<LazyPromiseNoProgress>();
                return obj == InvalidAwaitSentinel.s_instance
                    ? new LazyPromiseNoProgress()
                    : obj.UnsafeAs<LazyPromiseNoProgress>();
            }

            [MethodImpl(Internal.InlineOption)]
            private static LazyPromiseNoProgress GetOrCreate(AsyncLazy<T> owner)
            {
                var promise = GetOrCreate();
                promise._owner = owner;
                promise.Reset();
                return promise;
            }

            internal override void MaybeDispose()
            {
                Dispose();
                Internal.ObjectPool.MaybeRepool(this);
            }

            internal static Promise<T> GetOrStartPromise(AsyncLazy<T> owner, LazyFieldsNoProgress lazyFields)
            {
                LazyPromiseNoProgress lazyPromise;
                PromiseMultiAwait<T> preservedPromise;
                lock (lazyFields)
                {
                    if (lazyFields.IsComplete)
                    {
                        return Promise<T>.Resolved(owner._result);
                    }

                    if (lazyFields.IsStarted)
                    {
                        return GetDuplicate(lazyFields._lazyPromise.UnsafeAs<LazyPromiseNoProgress>()._preservedPromise);
                    }

                    lazyPromise = GetOrCreate(owner);
                    lazyFields._lazyPromise = lazyPromise;
                    // Same thing as Promise.Preserve(), but more direct.
                    lazyPromise._preservedPromise = preservedPromise = PromiseMultiAwait<T>.GetOrCreateAndHookup(lazyPromise, lazyPromise.Id);
                    // Exit the lock before invoking the factory.
                }
                var promise = GetDuplicate(preservedPromise);
                lazyPromise.Start(lazyFields._factory);
                return promise;
            }

            internal override void Handle(Internal.PromiseRefBase handler, Promise.State state)
            {
                handler.SetCompletionState(state);
                _result = handler.GetResult<T>();
                _rejectContainer = handler._rejectContainer;
                handler.SuppressRejection = true;
                handler.MaybeDispose();
                OnComplete(state);
            }

            protected override void OnComplete(Promise.State state)
            {
                var lazyFields = _owner._lazyFields;
                PromiseMultiAwait<T> preservedPromise;
                if (state != Promises.Promise.State.Resolved)
                {
                    lock (lazyFields)
                    {
                        // Reset the state so that the factory will be ran again the next time the Promise is accessed.
                        preservedPromise = _preservedPromise;
                        _preservedPromise = null;
                        lazyFields._lazyPromise = null;
                    }

                    preservedPromise.Forget(preservedPromise.Id);
                    HandleNextInternal(state);
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
                    lazyFields.UnsafeAs<LazyFieldsNoProgress>()._factory = null;
                }

                preservedPromise.Forget(preservedPromise.Id);
                HandleNextInternal(state);
            }
        } // class LazyPromiseNoProgress

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        private sealed class LazyWithProgressPromise : Internal.PromiseRefBase.LazyPromise<T>
        {
            private AsyncLazy<T> _owner;
            internal PromiseMultiAwait<T> _preservedPromise;
            internal Internal.ProgressMultiHandler _progressHandler;

            [MethodImpl(Internal.InlineOption)]
            private static LazyWithProgressPromise GetOrCreate()
            {
                var obj = Internal.ObjectPool.TryTakeOrInvalid<LazyWithProgressPromise>();
                return obj == InvalidAwaitSentinel.s_instance
                    ? new LazyWithProgressPromise()
                    : obj.UnsafeAs<LazyWithProgressPromise>();
            }

            [MethodImpl(Internal.InlineOption)]
            private static LazyWithProgressPromise GetOrCreate(AsyncLazy<T> owner)
            {
                var promise = GetOrCreate();
                promise._owner = owner;
                promise.Reset();
                return promise;
            }

            internal override void MaybeDispose()
            {
                Dispose();
                Internal.ObjectPool.MaybeRepool(this);
            }

            internal static Promise<T> GetOrStartPromise(AsyncLazy<T> owner, LazyFieldsWithProgress lazyFields, ProgressToken progressToken)
            {
                LazyWithProgressPromise lazyPromise;
                PromiseMultiAwait<T> preservedPromise;
                lock (lazyFields)
                {
                    if (lazyFields.IsComplete)
                    {
                        // Exit lock before reporting progress.
                        goto Complete;
                    }

                    if (lazyFields.IsStarted)
                    {
                        var castedPromise = lazyFields._lazyPromise.UnsafeAs<LazyWithProgressPromise>();
                        castedPromise._progressHandler.Add(progressToken, castedPromise._progressHandler.Id);
                        return GetDuplicate(castedPromise._preservedPromise);
                    }

                    lazyPromise = GetOrCreate(owner);
                    lazyFields._lazyPromise = lazyPromise;
                    // Same thing as Progress.NewMultiHandler(), but more direct.
                    lazyPromise._progressHandler = Internal.ProgressMultiHandler.GetOrCreate();
                    // Same thing as Promise.Preserve(), but more direct.
                    lazyPromise._preservedPromise = preservedPromise = PromiseMultiAwait<T>.GetOrCreateAndHookup(lazyPromise, lazyPromise.Id);
                    // Exit the lock before invoking the factory.
                }
                var promise = GetDuplicate(preservedPromise);
                lazyPromise._progressHandler.Add(progressToken, lazyPromise._progressHandler.Id);
                lazyPromise.Start(lazyFields._factory, new ProgressToken(lazyPromise._progressHandler, lazyPromise._progressHandler.Id, 0d, 1d));
                return promise;

            Complete:
                progressToken.Report(1d);
                return Promise<T>.Resolved(owner._result);
            }

            internal override void Handle(Internal.PromiseRefBase handler, Promise.State state)
            {
                handler.SetCompletionState(state);
                _result = handler.GetResult<T>();
                _rejectContainer = handler._rejectContainer;
                handler.SuppressRejection = true;
                handler.MaybeDispose();
                OnComplete(state);
            }

            protected override void OnComplete(Promise.State state)
            {
                var lazyFields = _owner._lazyFields;
                PromiseMultiAwait<T> preservedPromise;
                Internal.ProgressMultiHandler progressHandler;
                if (state != Promises.Promise.State.Resolved)
                {
                    lock (lazyFields)
                    {
                        // Reset the state so that the factory will be ran again the next time GetResultAsync is called.
                        preservedPromise = _preservedPromise;
                        _preservedPromise = null;
                        progressHandler = _progressHandler;
                        _progressHandler = null;
                        lazyFields._lazyPromise = null;
                    }

                    progressHandler.Dispose(progressHandler.Id);
                    preservedPromise.Forget(preservedPromise.Id);
                    HandleNextInternal(state);
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
                    _progressHandler = null;
                    lazyFields.UnsafeAs<LazyFieldsWithProgress>()._factory = null;
                }

                progressHandler.Report(1d, progressHandler.Id);
                progressHandler.Dispose(progressHandler.Id);
                preservedPromise.Forget(preservedPromise.Id);
                HandleNextInternal(state);
            }
        } // class LazyWithProgressPromise
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
                protected static Promise<TResult> GetDuplicate(PromiseMultiAwait<TResult> preservedPromise)
                {
                    // Same thing as Promise.Duplicate(), but more direct.
                    var p = preservedPromise;
                    var duplicate = p.GetDuplicateT(p.Id);
                    return new Promise<TResult>(duplicate, duplicate.Id);
                }

                [MethodImpl(InlineOption)]
                protected void Start(Func<Promise<TResult>> factory)
                {
                    SetCurrentInvoker(this);
                    try
                    {
                        var promise = factory.Invoke();
                        WaitFor(promise._ref, promise._result, promise._id, null, new CompleteHandler(this));
                    }
                    catch (OperationCanceledException)
                    {
                        OnComplete(Promise.State.Canceled);
                    }
                    catch (Exception e)
                    {
                        _rejectContainer = CreateRejectContainer(e, int.MinValue, null, this);
                        OnComplete(Promise.State.Rejected);
                    }
                    ClearCurrentInvoker();
                }

                [MethodImpl(InlineOption)]
                protected void Start(Func<ProgressToken, Promise<TResult>> factory, ProgressToken progressToken)
                {
                    SetCurrentInvoker(this);
                    try
                    {
                        var promise = factory.Invoke(progressToken);
                        WaitFor(promise._ref, promise._result, promise._id, null, new CompleteHandler(this));
                    }
                    catch (OperationCanceledException)
                    {
                        OnComplete(Promise.State.Canceled);
                    }
                    catch (Exception e)
                    {
                        _rejectContainer = CreateRejectContainer(e, int.MinValue, null, this);
                        OnComplete(Promise.State.Rejected);
                    }
                    ClearCurrentInvoker();
                }

                protected abstract void OnComplete(Promise.State state);

#if !PROTO_PROMISE_DEVELOPER_MODE
                [DebuggerNonUserCode, StackTraceHidden]
#endif
                private readonly struct CompleteHandler : IWaitForCompleteHandler
                {
                    private readonly LazyPromise<TResult> _owner;

                    [MethodImpl(InlineOption)]
                    internal CompleteHandler(LazyPromise<TResult> owner)
                    {
                        _owner = owner;
                    }

                    [MethodImpl(InlineOption)]
                    void IWaitForCompleteHandler.HandleHookup(PromiseRefBase handler)
                    {
                        var state = handler.State;
                        _owner._result = handler.GetResult<TResult>();
                        _owner._rejectContainer = handler._rejectContainer;
                        _owner.SuppressRejection = true;
                        handler.MaybeDispose();
                        _owner.OnComplete(state);
                    }

                    [MethodImpl(InlineOption)]
                    void IWaitForCompleteHandler.HandleNull()
                        => _owner.OnComplete(Promise.State.Resolved);
                }
            }
        } // class PromiseRefBase
    } // class Internal
}