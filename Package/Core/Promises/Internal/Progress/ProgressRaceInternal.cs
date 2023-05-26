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

#pragma warning disable IDE0018 // Inline variable declaration

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Proto.Promises
{
    partial class Internal
    {
        partial class PromiseRefBase
        {
            partial class RacePromise<TResult>
            {
                internal sealed override PromiseRefBase AddProgressWaiter(short promiseId, out HandleablePromiseBase previousWaiter, ref ProgressHookupValues progressHookupValues)
                {
                    var promiseSingleAwait = AddWaiterImpl(promiseId, progressHookupValues.ProgressListener, out previousWaiter);
                    if (previousWaiter == PendingAwaitSentinel.s_instance)
                    {
                        SetProgressValuesAndGetPrevious(ref progressHookupValues);
                    }
                    return promiseSingleAwait;
                }

                new private void SetProgressValuesAndGetPrevious(ref ProgressHookupValues progressHookupValues)
                {
                    ThrowIfInPool(this);
                    // Retain this until we've hooked up progress to the passthroughs. This is necessary because we take the passthroughs, then put back the ones that are unable to be hooked up.
                    InterlockedAddWithUnsignedOverflowCheck(ref _retainCounter, 1);
                    progressHookupValues.RegisterHandler(this);
                    ProgressRacer.MaybeHookup(this, _passThroughs.TakeAndClear(), ref progressHookupValues);
                }

                internal override sealed bool TryHookupProgressListenerAndGetPrevious(ref ProgressHookupValues progressHookupValues)
                {
                    if (CompareExchangeWaiter(progressHookupValues.ProgressListener, progressHookupValues._expectedWaiter) != progressHookupValues._expectedWaiter)
                    {
                        progressHookupValues._previous = null;
                        return false;
                    }
                    SetProgressValuesAndGetPrevious(ref progressHookupValues);
                    return true;
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private sealed partial class ProgressRacer : ProgressPassThrough
            {
                private ProgressRacer() { }

#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                ~ProgressRacer()
                {
                    try
                    {
                        if (!_disposed)
                        {
                            // For debugging. This should never happen.
                            string message = "A RaceProgressPassThrough was garbage collected without it being released."
                                + " _targetRacePromise: " + _targetRacePromise + ", _currentProgress: " + _currentProgress
                                ;
                            ReportRejection(new UnreleasedObjectException(message), _targetRacePromise);
                        }
                    }
                    catch (Exception e)
                    {
                        // This should never happen.
                        ReportRejection(e, _targetRacePromise);
                    }
                }
#endif

                [MethodImpl(InlineOption)]
                private static ProgressRacer GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<ProgressRacer>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new ProgressRacer()
                        : obj.UnsafeAs<ProgressRacer>();
                }

                private static ProgressRacer GetOrCreate(PromiseRefBase targetRacePromise, ValueLinkedStack<PromisePassThrough> passThroughs)
                {
                    var racer = GetOrCreate();
                    racer._next = null;
                    racer._targetRacePromise = targetRacePromise;
                    racer._passThroughs = passThroughs;
                    racer._currentProgress = 0f;
                    racer._retainCounter = 1; // We have 1 retain during hookup in case of promise completions on other threads.
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                    racer._disposed = false;
#endif
                    return racer;
                }

                private void Dispose()
                {
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                    _disposed = true;
#endif
                    // Don't nullify target, if it is accessed after this is disposed, the checks on it will ensure nothing happens.
                    ObjectPool.MaybeRepool(this);
                }

                internal static void MaybeHookup(PromiseRefBase targetRacePromise, ValueLinkedStack<PromisePassThrough> passThroughs, ref ProgressHookupValues progressHookupValues)
                {
                    progressHookupValues._previous = null;
                    ushort depth = targetRacePromise.Depth;
                    progressHookupValues.SetMinAndMaxFromLocalProgress(0u, depth + 1u);
                    if (passThroughs.IsEmpty)
                    {
                        progressHookupValues._currentProgress = depth;
                        targetRacePromise.MaybeDispose();
                        return;
                    }

                    var racer = GetOrCreate(targetRacePromise, passThroughs);
                    progressHookupValues._currentProgress = 0d;

                    progressHookupValues.AddPassthrough(racer);
                }

                internal override void HookupToRoots(ref ProgressHookupValues progressHookupValues)
                {
                    ThrowIfInPool(this);

                    var returnPassthroughs = new ValueLinkedStack<PromisePassThrough>();
                    int afterHookedUpReleaseCount = -1; // This was created with 1 retain, so we must release it 1 extra.
                    while (_passThroughs.IsNotEmpty)
                    {
                        var passthrough = _passThroughs.Pop();
                        InterlockedAddWithUnsignedOverflowCheck(ref _retainCounter, 1);
                        if (!RaceProgressPassThrough.TryHookup(this, passthrough, ref progressHookupValues))
                        {
                            unchecked
                            {
                                --afterHookedUpReleaseCount;
                            }
                            returnPassthroughs.Push(passthrough);
                            continue;
                        }
                        // We replaced the passthrough with the progress passthrough, so it is no longer needed.
                        passthrough.Dispose();
                    }

                    if (returnPassthroughs.IsNotEmpty)
                    {
                        _targetRacePromise.UnsafeAs<IMultiHandleablePromise>().ReturnPassthroughs(returnPassthroughs);
                    }
                    _targetRacePromise.MaybeDispose();
                    if (InterlockedAddWithUnsignedOverflowCheck(ref _retainCounter, afterHookedUpReleaseCount) == 0)
                    {
                        Dispose();
                    }
                }

                new internal void ReportProgress(ref ProgressReportValues progressReportValues)
                {
                    ThrowIfInPool(this);

                    double current;
                    do
                    {
                        current = _currentProgress;
                        if (progressReportValues._progress <= current)
                        {
                            Monitor.Exit(progressReportValues._lockedObject);
                            progressReportValues._progressListener = null;
                            return;
                        }
                    } while (Interlocked.CompareExchange(ref _currentProgress, progressReportValues._progress, current) != current);

                    progressReportValues._progressListener = _targetRacePromise._next;
                    progressReportValues._reporter = this;
                }

                [MethodImpl(InlineOption)]
                new internal void Handle(PromiseRefBase handler, object rejectContainer, Promise.State state, int index)
                {
                    ThrowIfInPool(this);

                    if (InterlockedAddWithUnsignedOverflowCheck(ref _retainCounter, -1) == 0)
                    {
                        Dispose();
                    }
                    _targetRacePromise.Handle(handler, rejectContainer, state, index);
                }
            } // ProgressRacer

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private sealed partial class RaceProgressPassThrough : ProgressPassThrough
            {
                private RaceProgressPassThrough() { }

#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                ~RaceProgressPassThrough()
                {
                    try
                    {
                        if (!_disposed)
                        {
                            // For debugging. This should never happen.
                            string message = "A RaceProgressPassThrough was garbage collected without it being released."
                                + ", _target: " + _target + ", _index: " + _index
                                + ", _currentReporter: " + _progressFields._currentReporter + ", _current: " + _progressFields._current
                                + ", _min: " + _progressFields._min + ", _max: " + _progressFields._max
                                ;
                            ReportRejection(new UnreleasedObjectException(message), null);
                        }
                    }
                    catch (Exception e)
                    {
                        // This should never happen.
                        ReportRejection(e, null);
                    }
                }
#endif

                [MethodImpl(InlineOption)]
                private static RaceProgressPassThrough GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<RaceProgressPassThrough>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new RaceProgressPassThrough()
                        : obj.UnsafeAs<RaceProgressPassThrough>();
                }

                private static RaceProgressPassThrough GetOrCreate(ProgressRacer target, int index)
                {
                    var passThrough = GetOrCreate();
                    passThrough._next = null;
                    passThrough._target = target;
                    passThrough._index = index;
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                    passThrough._disposed = false;
#endif
                    return passThrough;
                }

                internal void Dispose()
                {
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                    _disposed = true;
#endif
                    _target = null;
                    ObjectPool.MaybeRepool(this);
                }

                private void MaybeDispose(int retainAddCount)
                {
                    if (InterlockedAddWithUnsignedOverflowCheck(ref _progressFields._retainCounter, retainAddCount) == 0)
                    {
                        Dispose();
                    }
                }

                internal static bool TryHookup(ProgressRacer target, PromisePassThrough oldPassThrough, ref ProgressHookupValues progressHookupValues)
                {
                    return GetOrCreate(target, oldPassThrough.Index).TryListenForProgressOnRoots(oldPassThrough, ref progressHookupValues);
                }

                private bool TryListenForProgressOnRoots(PromisePassThrough oldPassThrough, ref ProgressHookupValues progressHookupValues)
                {
                    progressHookupValues._retainCounter = 0;
                    progressHookupValues.ProgressListener = this;
                    progressHookupValues._expectedWaiter = oldPassThrough;
                    progressHookupValues.SetMinMaxAndDivisorFromDepth(0d, 1d, oldPassThrough.Depth);
                    progressHookupValues._currentProgress = oldPassThrough.Depth;
                    uint passthroughCount = progressHookupValues._pendingPassthroughCount;

                    // The lock is held until progress is hooked up to all the roots.
                    Monitor.Enter(this);
                    if (!oldPassThrough.Owner.TryHookupProgressListenerAndGetPrevious(ref progressHookupValues))
                    {
                        Monitor.Exit(this);
                        Dispose();
                        return false;
                    }

                    while (progressHookupValues._previous != null)
                    {
                        progressHookupValues._previous.TryHookupProgressListenerAndGetPrevious(ref progressHookupValues);
                    }
                    progressHookupValues.SetListenerFields(ref _progressFields);
                    if (passthroughCount == progressHookupValues._pendingPassthroughCount)
                    {
                        // The passthrough has finished hooking up to all of its roots.
                        PropagateProgress();
                    }
                    else
                    {
                        // The passthrough had further root branches, so we need to continue to hold the lock until all of those branches are hooked up.
                        // The caller will release the lock when all branches are hooked up.
                        progressHookupValues._lockedPassthroughs.Push(this);
                    }
                    return true;
                }

                internal override void MaybeHookupProgressToAwaited(PromiseRefBase current, PromiseRefBase awaited, ref ProgressRange userProgressRange, ref ProgressRange listenerProgressRange)
                {
                    if (awaited == null)
                    {
                        // The awaited promise is already complete, do nothing.
                        return;
                    }

                    Monitor.Enter(this);
                    // In case of promise completion on another thread,
                    // make sure this is still hooked up to current, and another registered promise has not broken the chain.
                    if (current._next != this | _progressFields._registeredPromisesHead != current)
                    {
                        Monitor.Exit(this);
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
                        Monitor.Exit(this);
                        return;
                    }

                    progressHookupValues.ListenForProgressOnRoots(ref _progressFields);
                    _hookingUp = false;

                    PropagateProgress();
                }

                private void PropagateProgress()
                {
                    // Report progress to propagate up to the PromiseProgress listener.
                    var reportProgress = Lerp(_progressFields._min, _progressFields._max, _progressFields._current);
                    var progressReportValues = new ProgressReportValues(null, _target, this, reportProgress);
                    _target.ReportProgress(ref progressReportValues);
                    progressReportValues.ReportProgressToAllListeners();
                }

                internal override bool TryReportProgress(PromiseRefBase reporter, double progress, int deferredId, ref DeferredIdAndProgress idAndProgress)
                {
                    // Manually enter the lock so the next listener can enter its lock before unlocking this.
                    // This is necessary for race conditions so a progress report won't get ahead of another on a separate thread.
                    Monitor.Enter(this);

                    // Another thread could have resolved and repooled this before this thread entered the lock,
                    // so check to make sure the deferred is still valid.
                    if (deferredId != idAndProgress._id)
                    {
                        Monitor.Exit(this);
                        return false;
                    }

                    var progressReportValues = new ProgressReportValues(null, reporter, this, progress);
                    MaybeReportProgressImpl(ref progressReportValues);
                    progressReportValues.ReportProgressToAllListeners();
                    return true;
                }

                internal override void MaybeReportProgress(ref ProgressReportValues progressReportValues)
                {
                    // Manually enter this lock before exiting previous lock.
                    Monitor.Enter(this);
                    Monitor.Exit(progressReportValues._lockedObject);

                    if (_hookingUp)
                    {
                        // Just set the current progress. This will be scheduled for invoke higher in the call stack.
                        _progressFields._current = (float) progressReportValues._progress;
                        Monitor.Exit(this);
                        progressReportValues._progressListener = null;
                        return;
                    }

                    progressReportValues._lockedObject = this;
                    MaybeReportProgressImpl(ref progressReportValues);
                }

                private void MaybeReportProgressImpl(ref ProgressReportValues progressReportValues)
                {
                    float castedProgress = (float) progressReportValues._progress;
                    if (_progressFields._currentReporter != progressReportValues._reporter)
                    {
                        Monitor.Exit(this);
                        progressReportValues._progressListener = null;
                        return;
                    }
                    _progressFields._current = castedProgress; // We don't actually need to store the current, but it could be useful for debugging.
                    progressReportValues._progress = Lerp(_progressFields._min, _progressFields._max, progressReportValues._progress);
                    _target.ReportProgress(ref progressReportValues);
                }

                internal override void Handle(PromiseRefBase handler, object rejectContainer, Promise.State state)
                {
                    ThrowIfInPool(this);

                    // We lock on this to resolve race condition with progress hookup and progress report.
                    Monitor.Enter(this);
                    HandleablePromiseBase target;
                    int negativeDetachedCount;
                    bool shouldComplete = _progressFields.UnregisterHandlerAndGetShouldComplete(handler, this, out target, out negativeDetachedCount);

                    if (!shouldComplete)
                    {
                        Monitor.Exit(this);
                        MaybeDispose(negativeDetachedCount);
                        target.Handle(handler, rejectContainer, state);
                        return;
                    }

                    Monitor.Exit(this);
                    handler.SetCompletionState(rejectContainer, state);
                    var racer = _target;
                    MaybeDispose(negativeDetachedCount);
                    racer.Handle(handler, rejectContainer, state, _index);
                }
            } // RaceProgressPassThrough
        } // PromiseRefBase
    } // Internal
}

#endif // PROMISE_PROGRESS