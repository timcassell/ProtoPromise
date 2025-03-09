#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using Proto.Promises.Collections;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Proto.Promises
{
    partial class Internal
    {
        partial class PromiseRefBase
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal abstract partial class RetainedPromiseBase<TResult> : PromiseRef<TResult>
            {
                ~RetainedPromiseBase()
                {
                    if (!WasAwaitedOrForgotten)
                    {
                        WasAwaitedOrForgotten = true; // Stop base finalizer from adding an extra exception.
                        string message = "A retained Promise's resources were garbage collected without it being disposed.";
                        ReportRejection(new UnreleasedObjectException(message), this);
                    }
                    else if (_retainCounter != 0 & State != Promise.State.Pending)
                    {
                        string message = "A Promise from a retainer was not awaited or forgotten.";
                        ReportRejection(new UnreleasedObjectException(message), this);
                    }
                }

                [MethodImpl(InlineOption)]
                new protected void Reset()
                {
                    _retainCounter = 2; // 1 for dispose, 1 for completion.
                    base.Reset();
                }

                [MethodImpl(InlineOption)]
                protected void ResetForInternalUse()
                {
                    _retainCounter = 1; // 1 for completion/dispose.
                    _nextBranches = new TempCollectionBuilder<HandleablePromiseBase>(0);
                    base.Reset();
                }

                [MethodImpl(InlineOption)]
                protected void Hookup(PromiseRefBase previous, short id)
                    => previous.HookupNewPromise(id, this);

                [MethodImpl(InlineOption)]
                protected void ResetAndHookup(PromiseRefBase previous, short id)
                {
                    Reset();
                    Hookup(previous, id);
                    // We create the temp collection after we hook up in case the operation is invalid.
                    _nextBranches = new TempCollectionBuilder<HandleablePromiseBase>(0);
                }

                [MethodImpl(InlineOption)]
                private void Retain()
                    => InterlockedAddWithUnsignedOverflowCheck(ref _retainCounter, 1);

                internal void Dispose(short promiseId)
                {
                    lock (this)
                    {
                        if (promiseId != Id | WasAwaitedOrForgotten)
                        {
                            throw new ObjectDisposedException(nameof(Promise.Retainer), "The promise retainer was already disposed.");
                        }
                        ThrowIfInPool(this);

                        WasAwaitedOrForgotten = true;
                    }
                    MaybeDispose();
                }

                // Same as Dispose, but skips validation.
                [MethodImpl(InlineOption)]
                protected void DisposeUnsafe()
                {
                    ThrowIfInPool(this);
                    WasAwaitedOrForgotten = true;
                    MaybeDispose();
                }

                internal sealed override void MaybeDispose()
                {
                    if (InterlockedAddWithUnsignedOverflowCheck(ref _retainCounter, -1) != 0)
                    {
                        return;
                    }
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                    if (!WasAwaitedOrForgotten)
                    {
                        throw new System.InvalidOperationException("PromiseRetainer was MaybeDisposed completely without being properly disposed.");
                    }
#endif
                    _nextBranches.Dispose();
                    // Rejection maybe wasn't caught.
                    // We handle this directly here because we don't add the PromiseForgetSentinel to this type when it is disposed.
                    MaybeReportUnhandledRejection(State);
                    Dispose();
                    MaybeRepool();
                }

                protected abstract void MaybeRepool();

                internal override bool GetIsCompleted(short promiseId)
                {
                    ValidateId(promiseId, this, 2);
                    ThrowIfInPool(this);
                    return State != Promise.State.Pending;
                }

                internal override PromiseRefBase GetDuplicate(short promiseId)
                {
                    ValidateId(promiseId, this, 2);
                    ThrowIfInPool(this);
                    return this;
                }

                internal override PromiseRef<TResult> GetDuplicateT(short promiseId)
                {
                    ValidateId(promiseId, this, 2);
                    ThrowIfInPool(this);
                    return this;
                }

                [MethodImpl(InlineOption)]
                internal override bool GetIsValid(short promiseId)
                    => promiseId == Id;

                internal override void Forget(short promiseId)
                    => MaybeMarkAwaitedAndDispose(promiseId);

                internal override void MaybeMarkAwaitedAndDispose(short promiseId)
                {
                    ValidateId(promiseId, this, 2);
                    ThrowIfInPool(this);
                    MaybeDispose();
                }

                internal override PromiseRefBase AddWaiter(short promiseId, HandleablePromiseBase waiter, out HandleablePromiseBase previousWaiter)
                {
                    lock (this)
                    {
                        if (promiseId != Id)
                        {
                            previousWaiter = InvalidAwaitSentinel.s_instance;
                            return InvalidAwaitSentinel.s_instance;
                        }
                        ThrowIfInPool(this);

                        if (State == Promise.State.Pending)
                        {
                            _nextBranches.Add(waiter);
                            previousWaiter = PendingAwaitSentinel.s_instance;
                            return null;
                        }
                    }
                    previousWaiter = waiter;
                    return null;
                }

                internal override void Handle(PromiseRefBase handler, Promise.State state)
                {
                    ThrowIfInPool(this);
                    handler.SetCompletionState(state);
                    RejectContainer = handler.RejectContainer;
                    handler.SuppressRejection = true;
                    _result = handler.GetResult<TResult>();
                    SetCompletionState(state);
                    handler.MaybeDispose();

                    HandleBranches(state);
                    MaybeDispose();
                }

                protected void HandleBranches(Promise.State state)
                {
                    TempCollectionBuilder<HandleablePromiseBase> branches;
                    lock (this)
                    {
                        branches = _nextBranches;
                    }
                    for (int i = 0, max = branches._count; i < max; ++i)
                    {
                        branches[i].Handle(this, state);
                    }
                }

                internal Promise<TResult> WaitAsync(short promiseId)
                {
                    lock (this)
                    {
                        if (!GetIsValid(promiseId))
                        {
                            throw new ObjectDisposedException(nameof(Promise.Retainer), "The promise retainer was already disposed.");
                        }
                        ThrowIfInPool(this);

                        Retain();
                    }
                    return GetWaitAsync(promiseId);
                }

                // Same as WaitAsync, but skips validation.
                [MethodImpl(InlineOption)]
                internal Promise<TResult> WaitAsyncUnsafe()
                {
                    ThrowIfInPool(this);
                    Retain();
                    return GetWaitAsync(Id);
                }

                [MethodImpl(InlineOption)]
                private Promise<TResult> GetWaitAsync(short promiseId)
                {
#if PROMISE_DEBUG
                    // In DEBUG mode, we return a duplicate so that its usage will be validated properly.
                    var duplicatePromise = PromiseDuplicate<TResult>.GetOrCreate();
                    HookupNewPromise(promiseId, duplicatePromise);
                    return new Promise<TResult>(duplicatePromise, duplicatePromise.Id);
#else
                    // In RELEASE mode, we just return this for efficiency.
                    return new Promise<TResult>(this, promiseId);
#endif
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal sealed partial class PromiseRetainer<TResult> : RetainedPromiseBase<TResult>
            {
                private PromiseRetainer() { }

                [MethodImpl(InlineOption)]
                private static PromiseRetainer<TResult> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<PromiseRetainer<TResult>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new PromiseRetainer<TResult>()
                        : obj.UnsafeAs<PromiseRetainer<TResult>>();
                }

                [MethodImpl(InlineOption)]
                internal static PromiseRetainer<TResult> GetOrCreateAndHookup(PromiseRefBase previous, short id)
                {
                    var promise = GetOrCreate();
                    promise.ResetAndHookup(previous, id);
                    return promise;
                }

                protected override void MaybeRepool()
                    => ObjectPool.MaybeRepool(this);
            }
        } // class PromiseRefBase
    } // class Internal
}