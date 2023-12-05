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

#if PROMISE_PROGRESS

#pragma warning disable IDE0019 // Use pattern matching
#pragma warning disable IDE0018 // Inline variable declaration
#pragma warning disable IDE0034 // Simplify 'default' expression

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Proto.Promises
{
    partial class Internal
    {
        partial class PromiseRefBase
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private sealed partial class IndividualPromisePassThrough<TResult> : PromiseRef<TResult>
            {
                // This type is used to hook up each waiter in PromiseMultiAwait to its progress listener.
                // This is necessary because the _rejectContainerOrPreviousOrLink field is used to hook up the registered promises chain,
                // and it would not be possible to do that for multiple progress listeners with a single promise object. So we have to create dummy objects to register multiple.

                private IndividualPromisePassThrough()
                {
                    // Don't let the base finalizer report an error.
                    WasAwaitedOrForgotten = true;
                }

                [MethodImpl(InlineOption)]
                private static IndividualPromisePassThrough<TResult> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<IndividualPromisePassThrough<TResult>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new IndividualPromisePassThrough<TResult>()
                        : obj.UnsafeAs<IndividualPromisePassThrough<TResult>>();
                }

                internal static IndividualPromisePassThrough<TResult> GetOrCreateAndRegister(PromiseMultiAwait<TResult> owner, ref ProgressHookupValues progressHookupValues)
                {
                    var passthrough = GetOrCreate();
                    passthrough._owner = owner;
                    passthrough._next = progressHookupValues.ProgressListener;
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                    passthrough._disposed = false;
#endif
                    progressHookupValues.RegisterHandler(passthrough);
                    return passthrough;
                }

#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                ~IndividualPromisePassThrough()
                {
                    if (!_disposed)
                    {
                        // For debugging. This should never happen.
                        string message = "A IndividualPromisePassThrough was garbage collected without it being released.";
                        ReportRejection(new UnreleasedObjectException(message), _owner);
                    }
                }
#endif

                internal override void MaybeDispose()
                {
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                    _disposed = true;
#endif
                    Dispose();
                    _owner = null;
                    ObjectPool.MaybeRepool(this);
                }

                internal override void Handle(PromiseRefBase handler, object rejectContainer, Promise.State state)
                {
                    _result = handler.GetResult<TResult>();
                    // Don't bother to call handler.SetCompletionState, since we know the handler is the owner which is PromiseMultiAwait that already set its completion state.
                    _next.Handle(this, rejectContainer, state);
                    if (SuppressRejection)
                    {
                        handler.SuppressRejection = true;
                    }
                    handler.MaybeDispose();
                }

                internal override bool TryRestoreWaiter(HandleablePromiseBase waiter, HandleablePromiseBase expected)
                {
                    if (_owner.TryRestoreWaiter(waiter, this))
                    {
                        State = Promise.State.Resolved;
                        MaybeDispose();
                        return true;
                    }
                    return false;
                }

                internal override void MaybeReportProgress(ref ProgressReportValues progressReportValues)
                {
                    _next.MaybeReportProgress(ref progressReportValues);
                }

                internal override PromiseRefBase AddProgressWaiter(short promiseId, out HandleablePromiseBase previousWaiter, ref ProgressHookupValues progressHookupValues) { throw new System.InvalidOperationException(); }
                internal override void Forget(short promiseId) { throw new System.InvalidOperationException(); }
                internal override PromiseRefBase AddWaiter(short promiseId, HandleablePromiseBase waiter, out HandleablePromiseBase previousWaiter) { throw new System.InvalidOperationException(); }
                internal override PromiseRefBase GetDuplicate(short promiseId, ushort depth) { throw new System.InvalidOperationException(); }
                internal override PromiseRef<TResult> GetDuplicateT(short promiseId, ushort depth) { throw new System.InvalidOperationException(); }
                internal override bool GetIsCompleted(short promiseId) { throw new System.InvalidOperationException(); }
                internal override bool GetIsValid(short promiseId) { throw new System.InvalidOperationException(); }
                internal override void MaybeMarkAwaitedAndDispose(short promiseId) { throw new System.InvalidOperationException(); }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private sealed partial class ProgressMultiAwait<TResult> : ProgressPassThrough
            {
                [MethodImpl(InlineOption)]
                private static ProgressMultiAwait<TResult> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<ProgressMultiAwait<TResult>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new ProgressMultiAwait<TResult>()
                        : obj.UnsafeAs<ProgressMultiAwait<TResult>>();
                }

                private static ProgressMultiAwait<TResult> GetOrCreate(PromiseMultiAwait<TResult> owner)
                {
                    var passthrough = GetOrCreate();
                    passthrough._next = null;
                    passthrough._owner = owner;
                    // Retain the owner so we can continue to lock on it until this is disposed.
                    owner.Retain();
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                    passthrough._disposed = false;
#endif
                    return passthrough;
                }

#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                ~ProgressMultiAwait()
                {
                    if (!_disposed)
                    {
                        // For debugging. This should never happen.
                        string message = "A ProgressMultiAwait was garbage collected without it being released."
                            + ", _currentReporter: " + _progressFields._currentReporter + ", _current: " + _progressFields._current
                            + ", _min: " + _progressFields._min + ", _max: " + _progressFields._max
                            ;
                        ReportRejection(new UnreleasedObjectException(message), _owner);
                    }
                }
#endif

                private void Dispose()
                {
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                    _disposed = true;
#endif
                    _owner.MaybeDispose();
                    _progressListeners.Clear();
                    // We don't nullify _owner here, since it is used to lock. If the lock is taken after this has already been disposed, the further checks will ensure no harm is done.
                    ObjectPool.MaybeRepool(this);
                }

                private void MaybeDispose(int retainAddCount)
                {
                    if (InterlockedAddWithUnsignedOverflowCheck(ref _progressFields._retainCounter, retainAddCount) == 0)
                    {
                        Dispose();
                    }
                }

                internal static void Hookup(PromiseMultiAwait<TResult> owner, ref ProgressHookupValues progressHookupValues)
                {
                    progressHookupValues._previous = null;
                    var passthrough = owner._next as ProgressMultiAwait<TResult>;
                    if (passthrough != null)
                    {
                        passthrough._progressListeners.Add(progressHookupValues.ProgressListener);
                        progressHookupValues.CurrentReporter = passthrough;
                        progressHookupValues._currentProgress = Lerp(passthrough._progressFields._min, passthrough._progressFields._max, passthrough._progressFields._current);
                        return;
                    }

                    passthrough = GetOrCreate(owner);
                    // The lock is held until progress is hooked up to all the roots.
                    // We lock on owner instead of passthrough to resolve race conditions with further progress listeners being added while this is being hooked up.
                    Monitor.Enter(owner);

                    passthrough._progressListeners.Add(progressHookupValues.ProgressListener);
                    progressHookupValues._currentProgress = owner.Depth;
                    progressHookupValues.AddPassthrough(passthrough);
                }

                internal override void HookupToRoots(ref ProgressHookupValues progressHookupValues)
                {
                    progressHookupValues._retainCounter = 0;
                    progressHookupValues.ProgressListener = this;
                    progressHookupValues._expectedWaiter = _owner;
                    progressHookupValues._currentProgress = _owner.Depth;
                    progressHookupValues.SetMinMaxAndDivisorFromDepth(0d, 1d, _owner.Depth);

                    var previous = _owner._rejectContainerOrPreviousOrLink as PromiseRefBase;
                    if (previous == null || !previous.TryHookupProgressListenerAndGetPrevious(ref progressHookupValues))
                    {
                        ExitLock();
                        Dispose();
                        return;
                    }

                    _owner._next = this;
                    var passthroughCount = progressHookupValues._pendingPassthroughCount;
                    while (progressHookupValues._previous != null)
                    {
                        progressHookupValues._previous.TryHookupProgressListenerAndGetPrevious(ref progressHookupValues);
                    }

                    progressHookupValues.SetListenerFields(ref _progressFields);
                    if (passthroughCount != progressHookupValues._pendingPassthroughCount)
                    {
                        // The passthrough had further root branches, so we need to continue to hold the lock until all of those branches are hooked up.
                        // The caller will release the lock when all branches are hooked up.
                        progressHookupValues._lockedPassthroughs.Push(this);
                        return;
                    }
                    // The passthrough has finished hooking up to all of its roots.
                    PropagateProgress();
                }

                internal override void MaybeHookupProgressToAwaited(PromiseRefBase current, PromiseRefBase awaited, ref ProgressRange userProgressRange, ref ProgressRange listenerProgressRange)
                {
                    if (awaited == null)
                    {
                        // The awaited promise is already complete, do nothing.
                        return;
                    }

                    Monitor.Enter(_owner);
                    // In case of promise completion on another thread,
                    // make sure this is still hooked up to current, and another registered promise has not broken the chain.
                    if (current._next != this | _progressFields._registeredPromisesHead != current)
                    {
                        ExitLock();
                        return;
                    }
                    // We only check this is not in the pool after we verified the promise is still registered, otherwise it is valid for this to be in the pool.
                    ThrowIfInPool(this);

                    _hookingUp = true;
                    double min = Lerp(listenerProgressRange._min, listenerProgressRange._max, userProgressRange._min);
                    double max = Lerp(listenerProgressRange._min, listenerProgressRange._max, userProgressRange._max);
                    var progressHookupValues = new ProgressHookupValues(this, current, awaited.Depth, min, max, _progressFields._registeredPromisesHead);
                    if (!awaited.TryHookupProgressListenerAndGetPrevious(ref progressHookupValues))
                    {
                        // The awaited promise is already complete, or this was already registered to it on another thread, do nothing else.
                        _hookingUp = false;
                        ExitLock();
                        return;
                    }

                    progressHookupValues.ListenForProgressOnRoots(ref _progressFields);
                    _hookingUp = false;

                    PropagateProgress();
                }

                private void PropagateProgress()
                {
                    // Report progress to propagate up to the PromiseProgress listener.
                    var progressReportValues = new ProgressReportValues(null, this, _owner, 0d);
                    ReportProgress(ref progressReportValues);
                    progressReportValues.ReportProgressToAllListeners();
                }

                internal override void ExitLock()
                {
                    Monitor.Exit(_owner);
                }

                internal override void Handle(PromiseRefBase handler, object rejectContainer, Promise.State state)
                {
                    // We lock on owner to resolve race condition with progress hookup and progress report.
                    Monitor.Enter(_owner);
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                    try
                    {
                        ThrowIfInPool(this);
                    }
                    catch
                    {
                        ExitLock();
                        throw;
                    }
#endif
                    HandleablePromiseBase target;
                    int negativeDetachedCount;
                    if (_progressFields.UnregisterHandlerAndGetShouldComplete(handler, this, out target, out negativeDetachedCount))
                    {
                        HandleOwner(handler, rejectContainer, state, negativeDetachedCount);
                        return;
                    }

                    ExitLock();
                    MaybeDispose(negativeDetachedCount);
                    target.Handle(handler, rejectContainer, state);
                }

                private void HandleOwner(PromiseRefBase handler, object rejectContainer, Promise.State state, int negativeDetachedCount)
                {
                    ThrowIfInPool(_owner);
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                    if (_owner.State != Promise.State.Pending)
                    {
                        throw new System.InvalidOperationException("ProgressMultiAwait: Cannot handle owner more than once.");
                    }
#endif
                    handler.SetCompletionState(rejectContainer, state);
                    handler.SuppressRejection = true;
                    _owner._result = handler.GetResult<TResult>();
                    handler.MaybeDispose();
                    _owner.SetCompletionState(rejectContainer, state);

                    var branches = _owner._nextBranches;
                    // Remove the branches so progress won't try to hook up.
                    _owner._nextBranches = default(ValueList<HandleablePromiseBase>);
                    // Detach this.
                    _owner._next = null;
                    ExitLock();

                    for (int i = 0, max = branches.Count; i < max; ++i)
                    {
                        _owner.Retain(); // Retain since Handle will call MaybeDispose indiscriminately.
                        branches[i].Handle(_owner, rejectContainer, state);
                    }
                    branches.Clear();
                    _owner._nextBranches = branches;
                    _owner.MaybeDispose();

                    MaybeDispose(negativeDetachedCount);
                }

                internal override bool TryReportProgress(PromiseRefBase reporter, double progress, int deferredId, ref DeferredIdAndProgress idAndProgress)
                {
                    // Manually enter the lock so the next listener can enter its lock before unlocking this.
                    // This is necessary for race conditions so a progress report won't get ahead of another on a separate thread.
                    Monitor.Enter(_owner);

                    // Another thread could have resolved and repooled this before this thread entered the lock,
                    // so check to make sure the deferred is still valid.
                    if (deferredId != idAndProgress._id)
                    {
                        Monitor.Exit(_owner);
                        return false;
                    }

                    var progressReportValues = new ProgressReportValues(null, reporter, _owner, progress);
                    MaybeReportProgressImpl(ref progressReportValues);
                    progressReportValues.ReportProgressToAllListeners();
                    return true;
                }

                internal override void MaybeReportProgress(ref ProgressReportValues progressReportValues)
                {
                    // Manually enter this lock before exiting previous lock.
                    Monitor.Enter(_owner);
                    Monitor.Exit(progressReportValues._lockedObject);

                    if (_hookingUp)
                    {
                        // Just set the current progress. This will be scheduled for invoke higher in the call stack.
                        _progressFields._current = (float) progressReportValues._progress;
                        Monitor.Exit(_owner);
                        progressReportValues._progressListener = null;
                        return;
                    }

                    progressReportValues._lockedObject = _owner;
                    MaybeReportProgressImpl(ref progressReportValues);
                }

                private void MaybeReportProgressImpl(ref ProgressReportValues progressReportValues)
                {
                    progressReportValues._progressListener = null;

                    if (_progressFields._currentReporter != progressReportValues._reporter)
                    {
                        ExitLock();
                        return;
                    }
                    // We only check this is not in the pool after we verified the reporter, otherwise it is valid for this to be in the pool.
                    ThrowIfInPool(this);

                    progressReportValues._reporter = this;
                    _progressFields._current = (float) progressReportValues._progress;
                    progressReportValues._passthroughs.Push(this);
                }

                internal override void ReportProgress(ref ProgressReportValues progressReportValues)
                {
                    double normalizedProgress = Lerp(_progressFields._min, _progressFields._max, _progressFields._current);
                    for (int i = 0, max = _progressListeners.Count; i < max; ++i)
                    {
                        progressReportValues._reporter = this;
                        progressReportValues._progress = normalizedProgress;
                        progressReportValues._lockedObject = _owner;
                        // We have to hold the lock until all branches have been reported.
                        // We enter the lock again for each listener because each one exits the lock indiscriminately.
                        Monitor.Enter(_owner);
                        _progressListeners[i].MaybeReportProgress(ref progressReportValues);
                        progressReportValues.ReportProgressToSingularListeners();
                    }
                    ExitLock();
                }
            }

            partial class PromiseMultiAwait<TResult>
            {
                internal override PromiseRefBase AddProgressWaiter(short promiseId, out HandleablePromiseBase previousWaiter, ref ProgressHookupValues progressHookupValues)
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
                            var passthrough = IndividualPromisePassThrough<TResult>.GetOrCreateAndRegister(this, ref progressHookupValues);
                            _nextBranches.Add(passthrough);
                            ProgressMultiAwait<TResult>.Hookup(this, ref progressHookupValues);
                            previousWaiter = PendingAwaitSentinel.s_instance;
                            return null;
                        }
                        Retain(); // Retain since Handle will be called higher in the stack which will call MaybeDispose indiscriminately.
                    }
                    previousWaiter = progressHookupValues.ProgressListener;
                    return null;
                }

                internal override bool TryHookupProgressListenerAndGetPrevious(ref ProgressHookupValues progressHookupValues)
                {
                    lock (this)
                    {
                        for (int i = 0, max = _nextBranches.Count; i < max; ++i)
                        {
                            if (_nextBranches[i] == progressHookupValues._expectedWaiter)
                            {
                                _nextBranches[i] = IndividualPromisePassThrough<TResult>.GetOrCreateAndRegister(this, ref progressHookupValues);
                                ProgressMultiAwait<TResult>.Hookup(this, ref progressHookupValues);
                                return true;
                            }
                        }
                    }
                    return false;
                }

                internal override bool TryRestoreWaiter(HandleablePromiseBase waiter, HandleablePromiseBase expected)
                {
                    lock (this)
                    {
                        for (int i = 0, max = _nextBranches.Count; i < max; ++i)
                        {
                            if (_nextBranches[i] == expected)
                            {
                                _nextBranches[i] = waiter;
                                return true;
                            }
                        }
                    }
                    return false;
                }
            } // PromiseMultiAwait
        } // PromiseRefBase
    } // Internal
}

#endif // PROMISE_PROGRESS