#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0016 // Use 'throw' expression

using Proto.Promises.Collections;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Proto.Promises
{
    partial class Internal
    {
        internal abstract partial class PromiseRefBase
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal sealed partial class PromiseRetainer<TResult> : PromiseRef<TResult>
            {
                private PromiseRetainer() { }

                ~PromiseRetainer()
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
                new private void Reset()
                {
                    _retainCounter = 2; // 1 for dispose, 1 for completion.
                    base.Reset();
                }

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
                    promise.Reset();
                    previous.HookupNewPromise(id, promise);
                    // We create the temp collection after we hook up in case the operation is invalid.
                    promise._nextBranches = new TempCollectionBuilder<HandleablePromiseBase>(0);
                    return promise;
                }

                [MethodImpl(InlineOption)]
                private void Retain()
                    => InterlockedAddWithUnsignedOverflowCheck(ref _retainCounter, 1);

                internal override void MaybeDispose()
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
                    MaybeReportUnhandledRejection(_rejectContainer, State);
                    Dispose();
                    ObjectPool.MaybeRepool(this);
                }

                internal void Dispose(short promiseId)
                {
                    lock (this)
                    {
                        if (promiseId != Id | WasAwaitedOrForgotten)
                        {
                            throw new ObjectDisposedException("The promise retainer was already disposed.", GetFormattedStacktrace(2));
                        }
                        WasAwaitedOrForgotten = true;
                    }
                    MaybeDispose();
                }

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
                        if (promiseId != Id | WasAwaitedOrForgotten)
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
                    _rejectContainer = handler._rejectContainer;
                    handler.SuppressRejection = true;
                    _result = handler.GetResult<TResult>();
                    SetCompletionState(state);
                    handler.MaybeDispose();

                    TempCollectionBuilder<HandleablePromiseBase> branches;
                    lock (this)
                    {
                        branches = _nextBranches;
                    }
                    for (int i = 0, max = branches._count; i < max; ++i)
                    {
                        branches[i].Handle(this, state);
                    }
                    MaybeDispose();
                }

                internal Promise<TResult> WaitAsync(short promiseId)
                {
                    lock (this)
                    {
                        if (!GetIsValid(promiseId))
                        {
                            throw new ObjectDisposedException("The promise retainer was already disposed.", GetFormattedStacktrace(2));
                        }

                        Retain();
                    }
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
        } // class PromiseRefBase
    } // class Internal
}