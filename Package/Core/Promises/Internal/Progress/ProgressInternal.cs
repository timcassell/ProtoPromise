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

#pragma warning disable IDE0016 // Use 'throw' expression
#pragma warning disable IDE0018 // Inline variable declaration
#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable IDE0074 // Use compound assignment
#pragma warning disable IDE0090 // Use 'new(...)'
#pragma warning disable IDE0270 // Use coalesce expression
#pragma warning disable CA1507 // Use nameof to express symbol names
#pragma warning disable 0420 // A reference to a volatile field will not be treated as volatile

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Proto.Promises
{
    partial class Internal
    {
        internal const ushort NegativeOneDepth = ushort.MaxValue; // Same as (ushort) -1, but compiler complains that it needs unchecked context.

#if PROMISE_PROGRESS
        internal const string ProgressObsoleteMessage = "This API is obsolete and will be removed in a future version. Please use new Progress(Token) APIs.";
#else // PROMISE_PROGRESS

#if UNITY_5_5_OR_NEWER
        internal const string ProgressObsoleteMessage = "This API is obsolete and will be removed in a future version. Please use new Progress(Token) APIs.\nProgress is disabled. Progress will not be reported. Remove PROTO_PROMISE_PROGRESS_DISABLE from your scripting compilation symbols to enable progress.";
#else
        internal const string ProgressObsoleteMessage = "This API is obsolete and will be removed in a future version. Please use new Progress(Token) APIs.\nProgress is disabled. Progress will not be reported. Use a version of the library compiled with progress enabled for progress reports.";
#endif

#endif // !PROMISE_PROGRESS

#if PROMISE_PROGRESS

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal abstract partial class ProgressPassThrough : HandleablePromiseBase, ILinked<ProgressPassThrough>
        {
            ProgressPassThrough ILinked<ProgressPassThrough>.Next
            {
                [MethodImpl(InlineOption)]
                get { return _next.UnsafeAs<ProgressPassThrough>(); }
                [MethodImpl(InlineOption)]
                set { _next = value; }
            }

            internal virtual void ExitLock()
            {
                Monitor.Exit(this);
            }

            internal virtual void HookupToRoots(ref ProgressHookupValues progressHookupValues) { throw new System.InvalidOperationException(); }
            // This is only overridden by ProgressMultiAwait.
            internal virtual void ReportProgress(ref ProgressReportValues progressReportValues) { throw new System.InvalidOperationException(); }
        }

        internal
#if CSHARP_7_3_OR_NEWER
            ref // Don't allow on the heap.
#endif
            struct ProgressReportValues
        {
            internal HandleablePromiseBase _progressListener;
            internal HandleablePromiseBase _reporter;
            internal object _lockedObject;
            internal double _progress;
            internal ValueLinkedStack<ProgressPassThrough> _passthroughs;

            internal ProgressReportValues(HandleablePromiseBase progressListener, HandleablePromiseBase reporter, object lockedObject, double progress)
            {
                _progressListener = progressListener;
                _reporter = reporter;
                _lockedObject = lockedObject;
                _progress = progress;
                _passthroughs = new ValueLinkedStack<ProgressPassThrough>();
            }

            internal void ReportProgressToSingularListeners()
            {
                while (_progressListener != null)
                {
                    _progressListener.MaybeReportProgress(ref this);
                }
            }

            internal void ReportProgressToAllListeners()
            {
                while (true)
                {
                    ReportProgressToSingularListeners();
                    if (_passthroughs.IsEmpty)
                    {
                        break;
                    }
                    _passthroughs.Pop().ReportProgress(ref this);
                }
            }
        }

        internal
#if CSHARP_7_3_OR_NEWER
            ref // Don't allow on the heap.
#endif
            struct ProgressHookupValues
        {
            private HandleablePromiseBase _registeredPromisesHead;
            private HandleablePromiseBase _currentReporter;
            private HandleablePromiseBase _progressListener;
            internal HandleablePromiseBase _expectedWaiter;
            internal PromiseRefBase _previous;
            // Even though progress is reported with single float, we use double for better precision when calculating the normalized progress.
            internal double _min;
            internal double _max;
            // 1 / (depth of progress + 1), since it's faster to multiply the reciprocal than divide.
            private double _divisorReciprocal;
            internal double _currentProgress;
            private ValueLinkedStack<ProgressPassThrough> _pendingPassthroughs;
            internal uint _pendingPassthroughCount;
            // Passthrough listeners are locked when they are created, and the lock is held while they're being hooked up to their roots,
            // so we have to store them until all roots are hooked up to release the locks.
            internal ValueLinkedStack<ProgressPassThrough> _lockedPassthroughs;
            internal int _retainCounter;

            internal HandleablePromiseBase ProgressListener
            {
                get { return _progressListener; }
                set
                {
                    _progressListener = value;
                    _registeredPromisesHead = value;
                }
            }

            internal HandleablePromiseBase CurrentReporter
            {
                set { _currentReporter = value; }
            }

            internal ProgressHookupValues(HandleablePromiseBase progressListener, HandleablePromiseBase expectedWaiter, ushort depth, double min, double max, HandleablePromiseBase registeredPromisesHead)
            {
                _registeredPromisesHead = registeredPromisesHead;
                _currentReporter = null;
                _progressListener = progressListener;
                _expectedWaiter = expectedWaiter;
                _previous = null;
                _min = min;
                _max = max;
                _divisorReciprocal = 1d / (depth + 1u);
                _currentProgress = depth;
                _pendingPassthroughs = new ValueLinkedStack<ProgressPassThrough>();
                _pendingPassthroughCount = 0;
                _lockedPassthroughs = new ValueLinkedStack<ProgressPassThrough>();
                _retainCounter = 0;
            }

            internal void AddPassthrough(ProgressPassThrough progressPassthrough)
            {
                CurrentReporter = progressPassthrough;
                ++_pendingPassthroughCount;
                _pendingPassthroughs.Push(progressPassthrough);
            }

            internal ProgressPassThrough TakePassthrough()
            {
                --_pendingPassthroughCount;
                return _pendingPassthroughs.Pop();
            }

            internal void SetMinMaxAndDivisorReciprocal(double min, double max, double divisorReciprocal)
            {
                SetMinAndMax(min, max);
                _divisorReciprocal = divisorReciprocal;
            }

            internal void SetMinMaxAndDivisorFromDepth(double min, double max, ushort depth)
            {
                SetMinAndMax(min, max);
                SetDivisorFromDepth(depth);
            }

            internal void SetDivisorFromDepth(ushort depth)
            {
                _divisorReciprocal = 1d / (depth + 1u);
            }

            internal void SetMinAndMaxFromDepth(uint depth)
            {
                SetMinAndMaxFromLocalProgress(depth, depth + 1u);
            }

            private void SetMinAndMax(double min, double max)
            {
                _min = min;
                _max = max;
            }

            internal void SetMinAndMaxFromLocalProgress(double min, double max)
            {
                SetMinAndMax(
                    GetLerpedProgressFromLocalProgress(min),
                    GetLerpedProgressFromLocalProgress(max));
            }

            internal double GetLerpedProgressFromLocalProgress(double localProgress)
            {
                // localProgress is deferred's progress (0-1), or depth.
                return Lerp(_min, _max, localProgress * _divisorReciprocal);
            }

            internal void SetListenerFields(ref PromiseRefBase.ProgressListenerFields fields)
            {
                fields._current = (float) (_currentProgress * _divisorReciprocal);
                fields._min = (float) _min;
                fields._max = (float) _max;

                // There may already be some initial retains, so we add instead of overwrite.
                InterlockedAddWithUnsignedOverflowCheck(ref fields._retainCounter, _retainCounter);

                // Don't overwrite _unregisteredPromises.
                fields._registeredPromisesHead = _registeredPromisesHead;
                fields._currentReporter = _currentReporter;

                _registeredPromisesHead = null;
                _currentReporter = null;
            }

            internal void RegisterHandler(PromiseRefBase handler)
            {
                // Interlocked exchange instead of simple write to resolve race condition with await promise.
                InterlockedExchange(ref handler._rejectContainerOrPreviousOrLink, _registeredPromisesHead);
                _registeredPromisesHead = handler;
                IncrementRetainCounter();
            }

            internal void IncrementRetainCounter()
            {
                // int is treated as uint, we just use int because Interlocked does not support uint on old runtimes.
                unchecked
                {
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                    uint current = (uint) _retainCounter;
                    checked
                    {
                        ++current;
                    }
                    _retainCounter = (int) current;
#else
                    ++_retainCounter;
#endif
                }
            }

            internal void ListenForProgressOnRoots(ref PromiseRefBase.ProgressListenerFields progressFields)
            {
                // Users can forceAsync for very long promise chains to prevent stack overflow,
                // but we have to prevent stack overflow while iterating over the entire promise tree to hook up progress.
                // This algorithm allows the stack to unwind after visiting each promise, so we won't overflow, no matter how long the promise chain is.

                while (_previous != null)
                {
                    _previous.TryHookupProgressListenerAndGetPrevious(ref this);
                }
                SetListenerFields(ref progressFields);

                while (_pendingPassthroughCount > 0)
                {
                    TakePassthrough().HookupToRoots(ref this);
                }

                // Release the lock on all branched passthrough listeners.
                while (_lockedPassthroughs.IsNotEmpty)
                {
                    _lockedPassthroughs.Pop().ExitLock();
                }
            }
        }

        partial class PromiseRefBase
        {
            internal virtual PromiseRefBase AddProgressWaiter(short promiseId, out HandleablePromiseBase previousWaiter, ref ProgressHookupValues progressHookupValues) { throw new System.InvalidOperationException(); }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal partial struct ProgressRange
            {
                [MethodImpl(InlineOption)]
                internal ProgressRange(float min, float max)
                {
                    _min = min;
                    _max = max;
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal partial struct ProgressListenerFields
            {
                private static readonly Stack<Dictionary<PromiseRefBase, HandleablePromiseBase>> s_unregisteredDictPool = new Stack<Dictionary<PromiseRefBase, HandleablePromiseBase>>();

                // Detached count is negative because we use it to decrement the listener's retain counter via Interlocked.Add.
                internal bool UnregisterHandlerAndGetShouldComplete(PromiseRefBase handler, HandleablePromiseBase progressListener, out HandleablePromiseBase target, out int negativeDetachedCount)
                {
                    // The lock is already held when this is called.

                    // The progress listener is attached as the tail element in the linked-list,
                    // but we don't remove it since we only check if it's linked from the handler,
                    // and we use it to stop iterating while we're detaching handlers (this is cheaper than adding an extra branch to remove it).
                    bool shouldComplete = handler._rejectContainerOrPreviousOrLink == progressListener;
                    
                    if (_registeredPromisesHead == handler)
                    {
                        // Common case, the handler was the first element.
                        
                        // Null the current reporter to stop any further progress reports from that reporter.
                        _currentReporter = null;
                        _registeredPromisesHead = _registeredPromisesHead.UnsafeAs<PromiseRefBase>()._rejectContainerOrPreviousOrLink.UnsafeAs<HandleablePromiseBase>();
                        target = _registeredPromisesHead;
                        negativeDetachedCount = -1;
                        return shouldComplete;
                    }

                    // We have to special-case check for the singleton canceled promise.
                    // This is checked after the common case, so it shouldn't affect execution time in most cases.
                    if (handler is ICanceledPromiseSentinel)
                    {
                        target = null;
                        negativeDetachedCount = 0;
                        return true;
                    }

                    // Check for the rare case of the handler was already unregistered on another thread.
                    if (_unregisteredPromises != null && TryRemoveHandlerFromUnregisteredDict(handler, out target))
                    {
                        negativeDetachedCount = -1;
                        return false;
                    }

                    // We only null the current reporter if the handler was not already detached.
                    // This stops any further progress reports from that reporter.
                    _currentReporter = null;

                    // Uncommon case, the handler was canceled from a CancelationToken and broke the promise chain,
                    // so we iterate over the chain to unregister the handlers and try to restore the old waiters.
                    UnregisterHandlers(handler, progressListener, out target, out negativeDetachedCount);
                    return shouldComplete;
                }

                [MethodImpl(MethodImplOptions.NoInlining)]
                private void UnregisterHandlers(PromiseRefBase handler, HandleablePromiseBase progressListener, out HandleablePromiseBase target, out int negativeDetachedCount)
                {
                    int detachCounter = 0;
                    // The progress listener is the tail, so we check for it instead of null.
                    while (_registeredPromisesHead != progressListener)
                    {
                        var current = _registeredPromisesHead.UnsafeAs<PromiseRefBase>();
                        var next = current._rejectContainerOrPreviousOrLink.UnsafeAs<HandleablePromiseBase>();
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                        // progressListener should be the tail, we should never get null.
                        if (next == null)
                        {
                            throw new System.InvalidOperationException("next is null in UnregisterHandlers. current: " + current + ", progressListener: " + progressListener);
                        }
#endif
                        _registeredPromisesHead = next;
                        if (current == handler)
                        {
                            // Found the handler.
                            target = next;
                            negativeDetachedCount = detachCounter - 1;
                            return;
                        }
                        if (current.TryRestoreWaiter(next, progressListener))
                        {
                            --detachCounter;
                            continue;
                        }
                        // Very rare, this will only happen if the promise was completed on another thread while this was running.
                        AddDetachedHandler(current, next);
                    }
                    throw new ArgumentException("Handler { " + handler + " } not found registered on progress listener { " + progressListener + " }.", "handler");
                }

                [MethodImpl(MethodImplOptions.NoInlining)]
                private bool TryRemoveHandlerFromUnregisteredDict(PromiseRefBase handler, out HandleablePromiseBase target)
                {
                    bool removed = _unregisteredPromises.Remove(handler, out target);
                    if (_unregisteredPromises.Count == 0)
                    {
                        // We remove the dictionary if there are no more elements in it to not unnecessarily slow down future operations.
                        var dict = _unregisteredPromises;
                        _unregisteredPromises = null;
                        lock (s_unregisteredDictPool)
                        {
                            s_unregisteredDictPool.Push(dict);
                        }
                    }
                    return removed;
                }

                [MethodImpl(MethodImplOptions.NoInlining)]
                private void AddDetachedHandler(PromiseRefBase handler, HandleablePromiseBase target)
                {
                    // We lazy initialize the dictionary since this is a rare occurrence.
                    if (_unregisteredPromises == null)
                    {
                        // Try to get from the pool, otherwise create.
                        Dictionary<PromiseRefBase, HandleablePromiseBase> dict;
                        lock (s_unregisteredDictPool)
                        {
                            dict = s_unregisteredDictPool.Count > 0 ? s_unregisteredDictPool.Pop() : null;
                        }
                        _unregisteredPromises = dict ?? new Dictionary<PromiseRefBase, HandleablePromiseBase>();
                    }
                    _unregisteredPromises.Add(handler, target);
                }
            }

            internal virtual bool TryRestoreWaiter(HandleablePromiseBase waiter, HandleablePromiseBase expected)
            {
                return CompareExchangeWaiter(waiter, expected) == expected;
            }

            private void SetProgressValuesAndGetPrevious(ref ProgressHookupValues progressHookupValues)
            {
                ThrowIfInPool(this);
                progressHookupValues._currentProgress = Depth;
                progressHookupValues._expectedWaiter = this;
                progressHookupValues._previous = _rejectContainerOrPreviousOrLink as PromiseRefBase;
                progressHookupValues.RegisterHandler(this);
            }

            internal virtual bool TryHookupProgressListenerAndGetPrevious(ref ProgressHookupValues progressHookupValues)
            {
                // Promises that aren't normalizing progress technically don't need to hook up the listener,
                // but we still do it anyway so that the linked-list of registered promises can just use the links to get the old waiter (this implementation saves allocations).
                if (CompareExchangeWaiter(progressHookupValues.ProgressListener, progressHookupValues._expectedWaiter) != progressHookupValues._expectedWaiter)
                {
                    progressHookupValues._previous = null;
                    return false;
                }
                SetProgressValuesAndGetPrevious(ref progressHookupValues);
                return true;
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal sealed partial class PromiseProgress<TResult, TProgress> : PromiseSingleAwait<TResult>, ICancelable
                where TProgress : IProgress<float>
            {
                internal bool IsInvoking1
                {
                    [MethodImpl(InlineOption)]
                    get { return _previousState != Promise.State.Pending; }
                }

                private bool IsCanceled
                {
                    [MethodImpl(InlineOption)]
                    get { return _canceled; }
                    [MethodImpl(InlineOption)]
                    set { _canceled = value; }
                }

                private PromiseProgress() { }

                [MethodImpl(InlineOption)]
                new private void Reset(ushort depth)
                {
                    base.Reset(depth);
                    // _retainCounter is necessary to make sure the promise is disposed after the cancelation has invoked or unregistered,
                    // and the next awaited promise has been handled, and this is not invoking progress.
                    _progressFields._retainCounter = 2;
                }

                [MethodImpl(InlineOption)]
                private static PromiseProgress<TResult, TProgress> GetOrCreate()
                {
                    var obj = ObjectPool.TryTakeOrInvalid<PromiseProgress<TResult, TProgress>>();
                    return obj == InvalidAwaitSentinel.s_instance
                        ? new PromiseProgress<TResult, TProgress>()
                        : obj.UnsafeAs<PromiseProgress<TResult, TProgress>>();
                }

                internal static PromiseProgress<TResult, TProgress> GetOrCreate(TProgress progress, ushort depth, bool isSynchronous, SynchronizationContext synchronizationContext, bool forceAsync)
                {
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                    if (!isSynchronous && synchronizationContext == null)
                    {
                        throw new InvalidOperationException("synchronizationContext cannot be null");
                    }
#endif
                    var promise = GetOrCreate();
                    promise.Reset(depth);
                    promise._progress = progress;
                    promise.IsCanceled = false;
                    promise._isSynchronous = isSynchronous;
                    promise._previousState = Promise.State.Pending;
                    promise._synchronizationContext = synchronizationContext;
                    promise._forceAsync = forceAsync;
                    return promise;
                }

                internal static PromiseProgress<TResult, TProgress> GetOrCreateFromResolved(TProgress progress, TResult result, ushort depth, SynchronizationContext synchronizationContext, bool forceAsync, CancelationToken cancelationToken)
                {
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                    if (synchronizationContext == null)
                    {
                        throw new System.InvalidOperationException("synchronizationContext cannot be null");
                    }
#endif
                    var promise = GetOrCreate();
                    promise.Reset(depth);
                    promise._progress = progress;
                    promise.IsCanceled = false;
                    promise._isSynchronous = false;
                    promise._previousState = Promise.State.Resolved;
                    promise._synchronizationContext = synchronizationContext;
                    promise._result = result;
                    promise._forceAsync = forceAsync;
                    cancelationToken.TryRegister(promise, out promise._cancelationRegistration); // Very important, must register after promise is fully setup.
                    return promise;
                }

                [MethodImpl(InlineOption)]
                private bool ShouldInvokeSynchronous()
                {
                    return _isSynchronous | (!_forceAsync & _synchronizationContext == ts_currentContext);
                }

                internal override void MaybeDispose()
                {
                    MaybeDispose(-1);
                }

                private void MaybeDispose(int retainAddCount)
                {
                    if (InterlockedAddWithUnsignedOverflowCheck(ref _progressFields._retainCounter, retainAddCount) == 0)
                    {
                        Dispose();
                        _progress = default(TProgress);
                        _synchronizationContext = null;
                        _cancelationRegistration = default(CancelationRegistration);
                        _previousRejectContainer = null;
                        ObjectPool.MaybeRepool(this);
                    }
                }

                internal void HookupProgress(PromiseRefBase current, short promiseId, CancelationToken cancelationToken)
                {
#if PROMISE_DEBUG
                    _previous = current;
#endif
                    _rejectContainerOrPreviousOrLink = current;
                    cancelationToken.TryRegister(this, out _cancelationRegistration); // Very important, must register after promise is fully setup (previous is already assigned).

                    double reportProgress;
                    PromiseRefBase promiseSingleAwait;
                    lock (this)
                    {
                        _hookingUp = true;
                        try
                        {
                            HandleablePromiseBase previousWaiter;
                            _progressFields._registeredPromisesHead = this;
                            var progressHookupValues = new ProgressHookupValues(this, current, Depth, 0d, 1d, this);
                            promiseSingleAwait = current.AddProgressWaiter(promiseId, out previousWaiter, ref progressHookupValues);
                            if (previousWaiter == PendingAwaitSentinel.s_instance)
                            {
                                progressHookupValues.ListenForProgressOnRoots(ref _progressFields);

                                if (ShouldInvokeSynchronous())
                                {
                                    reportProgress = Lerp(_progressFields._min, _progressFields._max, _progressFields._current);
                                    // Retain and exit the lock before invoking so we're not holding the lock while user code runs.
                                    InterlockedAddWithUnsignedOverflowCheck(ref _progressFields._retainCounter, 1);
                                    goto InvokeProgressSynchronous;
                                }

                                ScheduleProgress();
                                return;
                            }
                        }
                        finally
                        {
                            _hookingUp = false;
                        }
                    }

                    VerifyAwaitAndHandle(current, promiseSingleAwait);
                    return;

                InvokeProgressSynchronous:
                    if (!IsInvoking1 & !IsCanceled & !_cancelationRegistration.Token.IsCancelationRequested)
                    {
                        InvokeAndCatchProgress((float) reportProgress);
                    }
                    MaybeDispose();
                }

                // This is rare, only happens when the promise already completed (usually an already completed promise is not backed by a reference), or if a promise is incorrectly awaited twice.
                [MethodImpl(MethodImplOptions.NoInlining)]
                private void VerifyAwaitAndHandle(PromiseRefBase current, PromiseRefBase promiseSingleAwait)
                {
                    if (!VerifyWaiter(promiseSingleAwait))
                    {
                        // We're already throwing InvalidOperationException here, so we don't want to also add exceptions from its finalizer.
                        Discard(this);
                        throw new InvalidOperationException("Cannot await or forget a forgotten promise or a non-preserved promise more than once.", GetFormattedStacktrace(3));
                    }

                    current.WaitUntilStateIsNotPending();
                    // Call HandleCompletion instead of Handle so we don't have to worry about unregistering promises that were never registered.
                    HandleCompletion(current, current._rejectContainerOrPreviousOrLink, current.State);
                }

                private void InvokeAndCatchProgress(float value)
                {
                    SetCurrentInvoker(this);
                    try
                    {
                        _progress.Report(value);
                    }
                    catch (Exception e)
                    {
                        ReportRejection(e, this);
                    }
                    ClearCurrentInvoker();
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
                    var reporter = _progressFields._currentReporter;
                    _hookingUp = false;

                    var reportMin = _progressFields._min;
                    var reportMax = _progressFields._max;
                    var reportT = _progressFields._current;

                    if (!ShouldInvokeSynchronous())
                    {
                        MaybeScheduleProgress();

                        // Report progress to next PromiseProgress listeners.
                        var progress = Lerp(reportMin, reportMax, reportT);
                        var progressReportValues = new ProgressReportValues(_next, this, this, progress);
                        progressReportValues.ReportProgressToAllListeners();
                        return;
                    }

                    // Retain and exit the lock before invoking so we're not holding the lock while user code runs.
                    InterlockedAddWithUnsignedOverflowCheck(ref _progressFields._retainCounter, 1);
                    Monitor.Exit(this);

                    if (!IsInvoking1 & !IsCanceled & !_cancelationRegistration.Token.IsCancelationRequested)
                    {
                        var reportProgress = (float) Lerp(reportMin, reportMax, reportT);
                        InvokeAndCatchProgress(reportProgress);
                    }

                    Monitor.Enter(this);
                    // Because we exited the lock and re-entered, some values may have changed on another thread (or even on the same thread from user code).
                    // We must make sure the values are still the same before continuing.
                    if (current._next != this | _progressFields._currentReporter != reporter
                        | _progressFields._current != reportT
                        | IsInvoking1 | IsCanceled | _cancelationRegistration.Token.IsCancelationRequested)
                    {
                        Monitor.Exit(this);
                        MaybeDispose();
                    }
                    else
                    {
                        // Report progress to next PromiseProgress listeners.
                        var progress = Lerp(reportMin, reportMax, reportT);
                        var progressReportValues = new ProgressReportValues(_next, this, this, progress);
                        MaybeDispose();
                        progressReportValues.ReportProgressToAllListeners();
                    }
                }

                internal override PromiseRefBase AddProgressWaiter(short promiseId, out HandleablePromiseBase previousWaiter, ref ProgressHookupValues progressHookupValues)
                {
                    var promiseSingleAwait = AddWaiter(promiseId, progressHookupValues.ProgressListener, out previousWaiter);
                    if (previousWaiter == PendingAwaitSentinel.s_instance)
                    {
                        lock (this)
                        {
                            ThrowIfInPool(this);
                            SetProgressValuesAndGetPrevious(ref progressHookupValues);
                        }
                    }
                    return promiseSingleAwait;
                }

                new private void SetProgressValuesAndGetPrevious(ref ProgressHookupValues progressHookupValues)
                {
                    progressHookupValues._previous = null;
                    progressHookupValues.SetMinAndMaxFromLocalProgress(0u, Depth + 1u);
                    progressHookupValues._currentProgress = Lerp(_progressFields._min, _progressFields._max, _progressFields._current);
                    progressHookupValues.CurrentReporter = this;
                    progressHookupValues.RegisterHandler(this);
                }

                internal override bool TryHookupProgressListenerAndGetPrevious(ref ProgressHookupValues progressHookupValues)
                {
                    lock (this)
                    {
                        ThrowIfInPool(this);
                        if (CompareExchangeWaiter(progressHookupValues.ProgressListener, progressHookupValues._expectedWaiter) != progressHookupValues._expectedWaiter)
                        {
                            progressHookupValues._previous = null;
                            return false;
                        }
                        SetProgressValuesAndGetPrevious(ref progressHookupValues);
                        return true;
                    }
                }

                private void MaybeScheduleProgress()
                {
                    if (!_isProgressScheduled)
                    {
                        ScheduleProgress();
                    }
                }

                private void ScheduleProgress()
                {
                    _isProgressScheduled = true;
                    InterlockedAddWithUnsignedOverflowCheck(ref _progressFields._retainCounter, 1);
                    ScheduleContextCallback(_synchronizationContext, this,
                        obj => obj.UnsafeAs<PromiseProgress<TResult, TProgress>>().InvokeProgressFromContext(),
                        obj => obj.UnsafeAs<PromiseProgress<TResult, TProgress>>().InvokeProgressFromContext()
                    );
                }

                private void InvokeProgressFromContext()
                {
                    float min, max, t;
                    lock (this)
                    {
                        ThrowIfInPool(this);

                        min = _progressFields._min;
                        max = _progressFields._max;
                        t = _progressFields._current;
                        _isProgressScheduled = false;
                        // Exit the lock before invoking so we're not holding the lock while user code runs.
                    }

                    var currentContext = ts_currentContext;
                    ts_currentContext = _synchronizationContext;

                    float value = (float) Lerp(min, max, t);
                    if (!IsInvoking1 & !IsCanceled & !_cancelationRegistration.Token.IsCancelationRequested)
                    {
                        InvokeAndCatchProgress(value);
                    }
                    MaybeDispose();

                    ts_currentContext = currentContext;
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
                    var reporter = progressReportValues._reporter;
                    progressReportValues._reporter = this;
                    float castedProgress = (float) progressReportValues._progress;
                    // Ignore progress 1, that will be reported when this is complete.
                    if (castedProgress >= 1f | _progressFields._currentReporter != reporter
                        | _progressFields._current == castedProgress
                        | IsInvoking1 | IsCanceled | _cancelationRegistration.Token.IsCancelationRequested)
                    {
                        Monitor.Exit(this);
                        progressReportValues._progressListener = null;
                        return;
                    }

                    // Only check if this is in the pool after we verified the current reporter.
                    ThrowIfInPool(this);

                    _progressFields._current = castedProgress;
                    progressReportValues._progress = Lerp(_progressFields._min, _progressFields._max, progressReportValues._progress);

                    if (!ShouldInvokeSynchronous())
                    {
                        MaybeScheduleProgress();

                        progressReportValues._progressListener = _next;
                        return;
                    }

                    // Retain and exit the lock before invoking so we're not holding the lock while user code runs.
                    InterlockedAddWithUnsignedOverflowCheck(ref _progressFields._retainCounter, 1);
                    Monitor.Exit(this);

                    InvokeAndCatchProgress((float) progressReportValues._progress);

                    Monitor.Enter(this);
                    // Because we exited the lock and re-entered, some values may have changed on another thread (or even on the same thread from user code).
                    // We must make sure the values are still the same before continuing.
                    if (_progressFields._currentReporter != reporter
                        | _progressFields._current != castedProgress
                        | IsInvoking1 | IsCanceled | _cancelationRegistration.Token.IsCancelationRequested)
                    {
                        Monitor.Exit(this);
                        progressReportValues._progressListener = null;
                        MaybeDispose();
                        return;
                    }
                    progressReportValues._progressListener = _next;
                    MaybeDispose();
                }

                internal override void Handle(PromiseRefBase handler, object rejectContainer, Promise.State state)
                {
                    ThrowIfInPool(this);

                    // We lock on this to resolve race condition with progress hookup and progress report.
                    bool shouldComplete;
                    HandleablePromiseBase target;
                    int negativeDetachedCount;
                    lock (this)
                    {
                        shouldComplete = _progressFields.UnregisterHandlerAndGetShouldComplete(handler, this, out target, out negativeDetachedCount);
                    }

                    if (!shouldComplete)
                    {
                        MaybeDispose(negativeDetachedCount);
                        target.Handle(handler, rejectContainer, state);
                        return;
                    }

                    // Release the amount of unregistered promises without checking the return (because we know we aren't fully released at this point).
                    InterlockedAddWithUnsignedOverflowCheck(ref _progressFields._retainCounter, negativeDetachedCount);
                    HandleCompletion(handler, rejectContainer, state);
                }

                private void HandleCompletion(PromiseRefBase handler, object rejectContainer, Promise.State state)
                {
                    handler.SetCompletionState(rejectContainer, state);
                    handler.SuppressRejection = true;
                    _result = handler.GetResult<TResult>();
                    handler.MaybeDispose();
                    _previousRejectContainer = rejectContainer;
                    _previousState = state;

                    if (ShouldInvokeSynchronous())
                    {
                        Invoke1(state);
                        return;
                    }

                    ScheduleCompletionOnContext();
                }

                internal void ScheduleCompletionOnContext()
                {
                    ScheduleContextCallback(_synchronizationContext, this,
                        obj => obj.UnsafeAs<PromiseProgress<TResult, TProgress>>().HandleFromContext(),
                        obj => obj.UnsafeAs<PromiseProgress<TResult, TProgress>>().HandleFromContext()
                    );
                }

                private void HandleFromContext()
                {
                    ThrowIfInPool(this);
                    var currentContext = ts_currentContext;
                    ts_currentContext = _synchronizationContext;

                    Invoke1(_previousState);

                    ts_currentContext = currentContext;
                }

                private void Invoke1(Promise.State state)
                {
                    if (TryUnregisterAndIsNotCanceling(ref _cancelationRegistration) & !IsCanceled)
                    {
                        if (state == Promise.State.Resolved)
                        {
                            InvokeAndCatchProgress(1f);
                        }
                        // Release since Cancel() will not be invoked.
                        InterlockedAddWithUnsignedOverflowCheck(ref _progressFields._retainCounter, -1);
                    }

                    // Leave pending until this is awaited.
                    if (ReadNextWaiterAndMaybeSetCompleted() == PendingAwaitSentinel.s_instance)
                    {
                        return;
                    }

                    HandleNextAlreadyAwaited(_previousRejectContainer, _previousState);
                }

                void ICancelable.Cancel()
                {
                    ThrowIfInPool(this);
                    IsCanceled = true;
                    MaybeDispose();
                }

                internal override PromiseRefBase AddWaiter(short promiseId, HandleablePromiseBase waiter, out HandleablePromiseBase previousWaiter)
                {
                    if (promiseId != Id)
                    {
                        previousWaiter = InvalidAwaitSentinel.s_instance;
                        return InvalidAwaitSentinel.s_instance;
                    }
                    ThrowIfInPool(this);
                    WasAwaitedOrForgotten = true;

                    var previous = CompareExchangeWaiter(waiter, PendingAwaitSentinel.s_instance);
                    if (previous != PendingAwaitSentinel.s_instance)
                    {
                        return VerifyAndHandleWaiter(waiter, out previousWaiter);
                    }
                    previousWaiter = PendingAwaitSentinel.s_instance;
                    return null; // It doesn't matter what we return since previousWaiter is set to PendingAwaitSentinel.s_instance.
                }

                // This is rare, only happens when the promise already completed (usually an already completed promise is not backed by a reference), or if a promise is incorrectly awaited twice.
                [MethodImpl(MethodImplOptions.NoInlining)]
                private PromiseRefBase VerifyAndHandleWaiter(HandleablePromiseBase waiter, out HandleablePromiseBase previousWaiter)
                {
                    // We do the verification process here instead of in the caller, because we need to handle continuations on the synchronization context.
                    bool shouldContinueSynchronous = ShouldInvokeSynchronous();
                    var setWaiter = shouldContinueSynchronous ? InvalidAwaitSentinel.s_instance : waiter;
                    if (CompareExchangeWaiter(setWaiter, PromiseCompletionSentinel.s_instance) != PromiseCompletionSentinel.s_instance)
                    {
                        previousWaiter = InvalidAwaitSentinel.s_instance;
                        return InvalidAwaitSentinel.s_instance;
                    }

                    if (shouldContinueSynchronous)
                    {
                        SetCompletionState(_previousRejectContainer, _previousState);
                        previousWaiter = waiter;
                        return null;
                    }

                    // This was configured to execute progress on a SynchronizationContext or the ThreadPool, force the waiter to execute on the same context for consistency.
                    ScheduleContextCallback(_synchronizationContext, this,
                        obj => obj.UnsafeAs<PromiseProgress<TResult, TProgress>>().HandleNextFromContext(),
                        obj => obj.UnsafeAs<PromiseProgress<TResult, TProgress>>().HandleNextFromContext()
                    );
                    previousWaiter = PendingAwaitSentinel.s_instance;
                    return null; // It doesn't matter what we return since previousWaiter is set to PendingAwaitSentinel.s_instance.
                }

                private void HandleNextFromContext()
                {
                    ThrowIfInPool(this);
                    var currentContext = ts_currentContext;
                    ts_currentContext = _synchronizationContext;

                    // We don't need to synchronize access here because this is only called when the waiter is added after Invoke1 has completed and this has been awaited, so there are no race conditions.
                    HandleNextInternal(_previousRejectContainer, _previousState);

                    ts_currentContext = currentContext;
                }

                internal override bool GetIsCompleted(short promiseId)
                {
                    ValidateId(promiseId, this, 2);
                    ThrowIfInPool(this);
                    // Make sure the continuation happens on the synchronization context.
                    if (ShouldInvokeSynchronous()
                        && CompareExchangeWaiter(InvalidAwaitSentinel.s_instance, PromiseCompletionSentinel.s_instance) == PromiseCompletionSentinel.s_instance)
                    {
                        WasAwaitedOrForgotten = true;
                        SetCompletionState(_previousRejectContainer, _previousState);
                        return true;
                    }
                    return false;
                }
            } // PromiseProgress<TProgress>
        } // PromiseRefBase
#endif // !PROMISE_PROGRESS
    } // Internal
}