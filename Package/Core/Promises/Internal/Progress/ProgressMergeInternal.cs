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
#pragma warning disable 0420 // A reference to a volatile field will not be treated as volatile

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
            // Even though interfaces are slower than abstract classes, it doesn't matter for this case since this will rarely be called,
            // and it's cleaner to just add the interface for this specific case, rather than adding more clutter to the base class.
            internal interface IMultiHandleablePromise
            {
                void ReturnPassthroughs(ValueLinkedStack<PromisePassThrough> passThroughs);
            }

            partial class MultiHandleablePromiseBase<TResult> : IMultiHandleablePromise
            {
                void IMultiHandleablePromise.ReturnPassthroughs(ValueLinkedStack<PromisePassThrough> passThroughs)
                {
                    _passThroughs = passThroughs;
                }
            }

            partial class MergePromise<TResult>
            {
                protected void SetProgressValuesAndGetPrevious(ref ProgressHookupValues progressHookupValues, bool reportUnresolved)
                {
                    ThrowIfInPool(this);
                    // Retain this until we've hooked up progress to the passthroughs. This is necessary because we take the passthroughs, then put back the ones that are unable to be hooked up.
                    InterlockedAddWithUnsignedOverflowCheck(ref _retainCounter, 1);
                    progressHookupValues.RegisterHandler(this);
                    ProgressMerger.MaybeHookup(this, _completeProgress, _passThroughs.TakeAndClear(), ref progressHookupValues, reportUnresolved);
                }

                internal override PromiseRefBase AddProgressWaiter(short promiseId, out HandleablePromiseBase previousWaiter, ref ProgressHookupValues progressHookupValues)
                {
                    var promiseSingleAwait = AddWaiterImpl(promiseId, progressHookupValues.ProgressListener, out previousWaiter);
                    if (previousWaiter == PendingAwaitSentinel.s_instance)
                    {
                        SetProgressValuesAndGetPrevious(ref progressHookupValues, false);
                    }
                    return promiseSingleAwait;
                }

                internal override bool TryHookupProgressListenerAndGetPrevious(ref ProgressHookupValues progressHookupValues)
                {
                    if (CompareExchangeWaiter(progressHookupValues.ProgressListener, progressHookupValues._expectedWaiter) != progressHookupValues._expectedWaiter)
                    {
                        progressHookupValues._previous = null;
                        return false;
                    }
                    SetProgressValuesAndGetPrevious(ref progressHookupValues, false);
                    return true;
                }
            }

            partial class MergeSettledPromise<TResult>
            {
                internal override PromiseRefBase AddProgressWaiter(short promiseId, out HandleablePromiseBase previousWaiter, ref ProgressHookupValues progressHookupValues)
                {
                    var promiseSingleAwait = AddWaiterImpl(promiseId, progressHookupValues.ProgressListener, out previousWaiter);
                    if (previousWaiter == PendingAwaitSentinel.s_instance)
                    {
                        SetProgressValuesAndGetPrevious(ref progressHookupValues, true);
                    }
                    return promiseSingleAwait;
                }

                internal override bool TryHookupProgressListenerAndGetPrevious(ref ProgressHookupValues progressHookupValues)
                {
                    if (CompareExchangeWaiter(progressHookupValues.ProgressListener, progressHookupValues._expectedWaiter) != progressHookupValues._expectedWaiter)
                    {
                        progressHookupValues._previous = null;
                        return false;
                    }
                    SetProgressValuesAndGetPrevious(ref progressHookupValues, true);
                    return true;
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private sealed partial class ProgressMerger : ProgressPassThrough
            {
                private ProgressMerger()
                {
                    Track();
                }

                partial void Track();

                [MethodImpl(InlineOption)]
                private static ProgressMerger GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<ProgressMerger>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new ProgressMerger()
                        : obj.UnsafeAs<ProgressMerger>();
                }

                private static ProgressMerger GetOrCreate(PromiseRefBase targetMergePromise, ulong completedProgress, ulong expectedProgress, ValueLinkedStack<PromisePassThrough> passThroughs, bool reportUnresolved)
                {
                    var merger = GetOrCreate();
                    merger._next = null;
                    merger._targetMergePromise = targetMergePromise;
                    merger._passThroughs = passThroughs;
                    merger._currentProgress = completedProgress;
                    merger._divisorReciprocal = 1d / expectedProgress;
                    merger._reportUnresolved = reportUnresolved;
#if PROTO_PROMISE_DEVELOPER_MODE
                    merger._disposed = false;
#endif
                    return merger;
                }

                private void Dispose()
                {
#if PROTO_PROMISE_DEVELOPER_MODE
                    _disposed = true;
#endif
                    // Don't nullify target, if it is accessed after this is disposed, the checks on it will ensure nothing happens.
                    ObjectPool.MaybeRepool(this);
                }

                internal static void MaybeHookup(PromiseRefBase targetMergePromise, ulong completedProgress, ValueLinkedStack<PromisePassThrough> passThroughs, ref ProgressHookupValues progressHookupValues, bool reportUnresolved)
                {
                    progressHookupValues._previous = null;
                    ushort depth = targetMergePromise.Depth;
                    progressHookupValues.SetMinAndMaxFromLocalProgress(0u, depth + 1u);
                    if (passThroughs.IsEmpty)
                    {
                        progressHookupValues._currentProgress = depth;
                        targetMergePromise.MaybeDispose();
                        return;
                    }

                    ulong expectedProgress = completedProgress;
                    foreach (var pt in passThroughs)
                    {
                        expectedProgress += pt.Depth + 1u;
                    }
                    var merger = GetOrCreate(targetMergePromise, completedProgress, expectedProgress, passThroughs, reportUnresolved);
                    progressHookupValues._currentProgress = completedProgress * merger._divisorReciprocal;

                    progressHookupValues.AddPassthrough(merger);
                }

                internal override void HookupToRoots(ref ProgressHookupValues progressHookupValues)
                {
                    ThrowIfInPool(this);

                    var returnPassthroughs = new ValueLinkedStack<PromisePassThrough>();
                    int retainCount = 0;
                    while (_passThroughs.IsNotEmpty)
                    {
                        var passthrough = _passThroughs.Pop();
                        if (!MergeProgressPassThrough.TryHookup(this, passthrough, ref progressHookupValues))
                        {
                            AddProgress(passthrough.Depth + 1u);
                            returnPassthroughs.Push(passthrough);
                            continue;
                        }
                        unchecked
                        {
                            ++retainCount;
                        }
                        // We replaced the passthrough with the progress passthrough, so it is no longer needed.
                        passthrough.Dispose();
                    }
                    _retainCounter = retainCount;

                    if (returnPassthroughs.IsNotEmpty)
                    {
                        _targetMergePromise.UnsafeAs<IMultiHandleablePromise>().ReturnPassthroughs(returnPassthroughs);
                    }
                    _targetMergePromise.MaybeDispose();
                    if (retainCount == 0)
                    {
                        Dispose();
                    }
                }

                private double AddProgress(double value)
                {
                    // There is no Interlocked.Add function for double, so we have to do it in a CompareExchange loop.
                    double current;
                    double newValue;
                    do
                    {
                        // There is no Volatile.Read in old runtime, so we have to place a memory barrier instead to make sure we read a fresh value.
                        Thread.MemoryBarrier();
                        current = _currentProgress;
                        newValue = current + value;
                    } while (Interlocked.CompareExchange(ref _currentProgress, newValue, current) != current);
                    return newValue;
                }

                internal void UpdateProgress(float oldProgress, ref ProgressReportValues progressReportValues)
                {
                    ThrowIfInPool(this);

                    double dif = progressReportValues._progress - oldProgress;
                    // Multiply by the divisor reciprocal to normalize the progress.
                    progressReportValues._progress = AddProgress(dif) * _divisorReciprocal;
                    progressReportValues._progressListener = _targetMergePromise._next;
                    progressReportValues._reporter = this;
                }

                [MethodImpl(InlineOption)]
                internal void Handle(float oldProgress, float maxProgress, PromiseRefBase handler, object rejectContainer, Promise.State state, int index, object lockedObject)
                {
                    ThrowIfInPool(this);

                    if (_retainCounter == 0)
                    {
                        WaitForHookup();
                    }

                    // Update progress before decrementing retains to fix race condition with other threads.
                    var progressReportValues = new ProgressReportValues(null, this, lockedObject, maxProgress);
                    UpdateProgress(oldProgress, ref progressReportValues);

                    // We only report the progress if the handler was not the last completed, and the state is resolved (or the merge type is All/MergeSettled).
                    // We check the more common case first.
                    bool isComplete = InterlockedAddWithUnsignedOverflowCheck(ref _retainCounter, -1) == 0;
                    if (!isComplete & (_reportUnresolved | state == Promise.State.Resolved))
                    {
                        progressReportValues.ReportProgressToAllListeners();
                    }
                    else
                    {
                        Monitor.Exit(lockedObject);
                        if (isComplete)
                        {
                            Dispose();
                        }
                    }
                    _targetMergePromise.Handle(handler, rejectContainer, state, index);
                }

                [MethodImpl(MethodImplOptions.NoInlining)]
                private void WaitForHookup()
                {
                    var spinner = new SpinWait();
                    while (_retainCounter == 0)
                    {
                        spinner.SpinOnce();
                    }
                }
            } // ProgressMerger

#if PROTO_PROMISE_DEVELOPER_MODE
            partial class ProgressMerger : IFinalizable
            {
                WeakNode IFinalizable.Tracker { get; set; }

                partial void Track()
                {
                    TrackFinalizable(this);
                }

                ~ProgressMerger()
                {
                    try
                    {
                        UntrackFinalizable(this);
                        if (!_disposed)
                        {
                            // For debugging. This should never happen.
                            string message = "A MergeProgressPassThrough was garbage collected without it being released."
                                + " _targetMergePromise: " + _targetMergePromise + ", _currentProgress: " + _currentProgress + ", _divisorReciprocal: " + _divisorReciprocal
                                ;
                            ReportRejection(new UnreleasedObjectException(message), _targetMergePromise);
                        }
                    }
                    catch (Exception e)
                    {
                        // This should never happen.
                        ReportRejection(e, _targetMergePromise);
                    }
                }
            }
#endif

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private sealed partial class MergeProgressPassThrough : ProgressPassThrough
            {
                private MergeProgressPassThrough()
                {
                    Track();
                }

                partial void Track();

                [MethodImpl(InlineOption)]
                private static MergeProgressPassThrough GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<MergeProgressPassThrough>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new MergeProgressPassThrough()
                        : obj.UnsafeAs<MergeProgressPassThrough>();
                }

                private static MergeProgressPassThrough GetOrCreate(ProgressMerger target, int index)
                {
                    var passThrough = GetOrCreate();
                    passThrough._next = null;
                    passThrough._target = target;
                    passThrough._currentProgress = 0f;
                    passThrough._index = index;
#if PROTO_PROMISE_DEVELOPER_MODE
                    passThrough._disposed = false;
#endif
                    return passThrough;
                }

                internal void Dispose()
                {
#if PROTO_PROMISE_DEVELOPER_MODE
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

                internal static bool TryHookup(ProgressMerger target, PromisePassThrough oldPassThrough, ref ProgressHookupValues progressHookupValues)
                {
                    return GetOrCreate(target, oldPassThrough.Index).TryListenForProgressOnRoots(oldPassThrough, ref progressHookupValues);
                }

                private bool TryListenForProgressOnRoots(PromisePassThrough oldPassThrough, ref ProgressHookupValues progressHookupValues)
                {
                    progressHookupValues._retainCounter = 0;
                    progressHookupValues.ProgressListener = this;
                    progressHookupValues._expectedWaiter = oldPassThrough;
                    // Instead of lerping the progress from 0 to 1 like the base listener, we lerp from 0 to depth + 1,
                    // then that value gets added to the merger value before it is scaled back down to 0 to 1.
                    progressHookupValues.SetMinMaxAndDivisorReciprocal(0d, 1d, 1d);
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
                    var progressHookupValues = new ProgressHookupValues(this, current, 0, min, max, _progressFields._registeredPromisesHead);
                    if (awaited == null || !awaited.TryHookupProgressListenerAndGetPrevious(ref progressHookupValues))
                    {
                        // The awaited promise is already complete, or this was already registered to it on another thread, do nothing else.
                        progressHookupValues.SetListenerFields(ref _progressFields);
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
                    float oldProgress = _currentProgress;
                    _currentProgress = (float) Lerp(_progressFields._min, _progressFields._max, _progressFields._current);
                    var progressReportValues = new ProgressReportValues(null, _target, this, _currentProgress);
                    _target.UpdateProgress(oldProgress, ref progressReportValues);
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
                    if (_progressFields._currentReporter != progressReportValues._reporter)
                    {
                        Monitor.Exit(this);
                        progressReportValues._progressListener = null;
                        return;
                    }
                    float oldProgress = _currentProgress;
                    _progressFields._current = (float) progressReportValues._progress;
                    float newProgress = (float) Lerp(_progressFields._min, _progressFields._max, _progressFields._current);
                    _currentProgress = newProgress;
                    progressReportValues._progress = newProgress;
                    _target.UpdateProgress(oldProgress, ref progressReportValues);
                }

                internal override void Handle(PromiseRefBase handler, object rejectContainer, Promise.State state)
                {
                    ThrowIfInPool(this);

                    // We lock to resolve race condition with progress hookup and progress report.
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

                    handler.SetCompletionState(rejectContainer, state);
                    // We continue to hold the lock until the target has been handled.
                    _target.Handle(_currentProgress, _progressFields._max, handler, rejectContainer, state, _index, this);
                    MaybeDispose(negativeDetachedCount);
                }
            } // MergeProgressPassThrough

#if PROTO_PROMISE_DEVELOPER_MODE
            partial class MergeProgressPassThrough : ProgressPassThrough, IFinalizable
            {
                WeakNode IFinalizable.Tracker { get; set; }

                partial void Track()
                {
                    TrackFinalizable(this);
                }

                ~MergeProgressPassThrough()
                {
                    try
                    {
                        UntrackFinalizable(this);
                        if (!_disposed)
                        {
                            // For debugging. This should never happen.
                            string message = "A MergeProgressPassThrough was garbage collected without it being released."
                                + ", _target: " + _target
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
            }
#endif

            partial class PromisePassThrough
            {
                internal ushort Depth
                {
                    [MethodImpl(InlineOption)]
                    get { return _depth; }
                }
            }
        } // PromiseRefBase
    } // Internal
}

#endif // PROMISE_PROGRESS