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
#pragma warning disable IDE0019 // Use pattern matching
#pragma warning disable IDE0018 // Inline variable declaration
#pragma warning disable IDE0034 // Simplify 'default' expression
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

#if !PROMISE_PROGRESS

#if UNITY_UNITY_5_5_OR_NEWER
        internal const string ProgressDisabledMessage = "Progress is disabled. Progress will not be reported. Remove PROTO_PROMISE_PROGRESS_DISABLE from your scripting compilation symbols to enable progress.";
#else
        internal const string ProgressDisabledMessage = "Progress is disabled. Progress will not be reported. Use a version of the library compiled with progress enabled for progress reports.";
#endif

#else // !PROMISE_PROGRESS

        private static readonly SendOrPostCallback s_synchronizationContextProgressCallback = ProgressFromContext;
        private static readonly WaitCallback s_threadPoolProgressCallback = ProgressFromContext;

        private static void ScheduleForProgress(HandleablePromiseBase progressable, SynchronizationContext context)
        {
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
            if (context == null)
            {
                throw new InvalidOperationException("context cannot be null");
            }
#endif
            if (context == BackgroundSynchronizationContextSentinel.s_instance)
            {
                ThreadPool.QueueUserWorkItem(s_threadPoolProgressCallback, progressable);
            }
            else
            {
                context.Post(s_synchronizationContextProgressCallback, progressable);
            }
        }

        private static void ProgressFromContext(object state)
        {
            // In case this is executed from a background thread, catch the exception and report it instead of crashing the app.
            try
            {
                state.UnsafeAs<HandleablePromiseBase>().InvokeProgressFromContext();
            }
            catch (Exception e)
            {
                // This should never happen.
                ReportRejection(e, state as ITraceable);
            }
        }

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

        [MethodImpl(InlineOption)]
        private static double Lerp(double a, double b, double t)
        {
            return a + (b - a) * t;
        }

#endif // !PROMISE_PROGRESS

        partial class PromiseRefBase
        {
#if PROMISE_PROGRESS
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
                // Detached count is negative because we use it to decrement the listener's retain counter via Interlocked.Add.
                internal bool UnregisterHandlerAndGetShouldComplete(PromiseRefBase handler, HandleablePromiseBase progressListener, out HandleablePromiseBase target, out int negativeDetachedCount)
                {
                    // The lock is already held when this is called.

                    if (_unregisteredPromises != null && _unregisteredPromises.Remove(handler, out target))
                    {
                        negativeDetachedCount = -1;
                        return false;
                    }

                    // We only null the current reporter if the handler was not already detached.
                    // This stops any further progress reports from that reporter.
                    _currentReporter = null;

                    // The progress listener is attached as the tail element in the linked-list,
                    // but we don't remove it since we only check if it's linked from the handler,
                    // and we use it to stop iterating while we're detaching handlers (this is cheaper than adding an extra branch to remove it).
                    bool shouldComplete = handler._rejectContainerOrPreviousOrLink == progressListener;
                    
                    if (_registeredPromisesHead == handler)
                    {
                        // Common case, the handler was the first element.
                        _registeredPromisesHead = _registeredPromisesHead.UnsafeAs<PromiseRefBase>()._rejectContainerOrPreviousOrLink.UnsafeAs<HandleablePromiseBase>();
                        target = _registeredPromisesHead;
                        negativeDetachedCount = -1;
                        return shouldComplete;
                    }

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
                private void AddDetachedHandler(PromiseRefBase handler, HandleablePromiseBase target)
                {
                    // We lazy initialize the dictionary since this is a rare occurrence.
                    if (_unregisteredPromises == null)
                    {
                        _unregisteredPromises = new Dictionary<PromiseRefBase, HandleablePromiseBase>();
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
                private static readonly WaitCallback s_threadPoolCallback = ExecuteFromContext;
                private static readonly SendOrPostCallback s_synchronizationContextCallback = ExecuteFromContext;

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

                internal static PromiseProgress<TResult, TProgress> GetOrCreate(TProgress progress, ushort depth, bool isSynchronous, SynchronizationContext synchronizationContext, bool forceAsync)
                {
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                    if (!isSynchronous && synchronizationContext == null)
                    {
                        throw new InvalidOperationException("synchronizationContext cannot be null");
                    }
#endif
                    var promise = ObjectPool.TryTake<PromiseProgress<TResult, TProgress>>()
                        ?? new PromiseProgress<TResult, TProgress>();
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
                    var promise = ObjectPool.TryTake<PromiseProgress<TResult, TProgress>>()
                        ?? new PromiseProgress<TResult, TProgress>();
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

                    TProgress callback;
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
                                    callback = _progress;
                                    reportProgress = Lerp(_progressFields._min, _progressFields._max, _progressFields._current);
                                    // Exit the lock before invoking so we're not holding the lock while user code runs.
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
                        CallbackHelperVoid.InvokeAndCatchProgress(callback, (float) reportProgress, this);
                    }
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

                internal override void MaybeHookupProgressToAwaited(PromiseRefBase current, PromiseRefBase awaited, ref ProgressRange userProgressRange, ref ProgressRange listenerProgressRange)
                {
                    if (awaited == null)
                    {
                        // The awaited promise is already complete, do nothing.
                        return;
                    }

                    TProgress callback;
                    float reportMin, reportMax, reportT;
                    HandleablePromiseBase reporter;
                    lock (this)
                    {
                        // In case of promise completion on another thread,
                        // make sure this is still hooked up to current, and another registered promise has not broken the chain.
                        if (current._next != this | _progressFields._registeredPromisesHead != current)
                        {
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
                            return;
                        }

                        progressHookupValues.ListenForProgressOnRoots(ref _progressFields);
                        reporter = _progressFields._currentReporter;
                        _hookingUp = false;

                        reportMin = _progressFields._min;
                        reportMax = _progressFields._max;
                        reportT = _progressFields._current;

                        if (!ShouldInvokeSynchronous())
                        {
                            MaybeScheduleProgress();

                            goto PropagateProgress;
                        }

                        callback = _progress;
                        // Exit the lock before invoking so we're not holding the lock while user code runs.
                    }

                    if (!IsInvoking1 & !IsCanceled & !_cancelationRegistration.Token.IsCancelationRequested)
                    {
                        var reportProgress = (float) Lerp(reportMin, reportMax, reportT);
                        CallbackHelperVoid.InvokeAndCatchProgress(callback, reportProgress, this);
                    }

                PropagateProgress:

                    Monitor.Enter(this);
                    // Because we exited the lock and re-entered, some values may have changed on another thread (or even on the same thread from user code).
                    // We must make sure the values are still the same before continuing.
                    if (current._next != this | _progressFields._currentReporter != reporter
                        | _progressFields._current != reportT
                        | IsInvoking1 | IsCanceled | _cancelationRegistration.Token.IsCancelationRequested)
                    {
                        Monitor.Exit(this);
                        return;
                    }

                    // Report progress to next PromiseProgress listeners.
                    var progress = Lerp(reportMin, reportMax, reportT);
                    var progressReportValues = new ProgressReportValues(_next, this, this, progress);
                    progressReportValues.ReportProgressToAllListeners();
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

                internal override void InvokeProgressFromContext()
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
                        CallbackHelperVoid.InvokeAndCatchProgress(_progress, value, this);
                    }
                    MaybeDispose();

                    ts_currentContext = currentContext;
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
                    ScheduleForProgress(this, _synchronizationContext);
                }

                internal override void MaybeReportProgress(PromiseRefBase reporter, double progress)
                {
                    // Manually enter the lock so the next listener can enter its lock before unlocking this.
                    // This is necessary for race conditions so a progress report won't get ahead of another on a separate thread.
                    Monitor.Enter(this);
                    var progressReportValues = new ProgressReportValues(null, reporter, this, progress);
                    MaybeReportProgressImpl(ref progressReportValues);
                    progressReportValues.ReportProgressToAllListeners();
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
                    ThrowIfInPool(this);

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

                    _progressFields._current = castedProgress;
                    progressReportValues._progress = Lerp(_progressFields._min, _progressFields._max, progressReportValues._progress);

                    if (!ShouldInvokeSynchronous())
                    {
                        MaybeScheduleProgress();

                        progressReportValues._progressListener = _next;
                        return;
                    }

                    TProgress callback = _progress;
                    // Exit the lock before invoking so we're not holding the lock while user code runs.
                    Monitor.Exit(this);

                    CallbackHelperVoid.InvokeAndCatchProgress(callback, (float) progressReportValues._progress, this);

                    Monitor.Enter(this);
                    // Because we exited the lock and re-entered, some values may have changed on another thread (or even on the same thread from user code).
                    // We must make sure the values are still the same before continuing.
                    if (_progressFields._currentReporter != reporter
                        | _progressFields._current != castedProgress
                        | IsInvoking1 | IsCanceled | _cancelationRegistration.Token.IsCancelationRequested)
                    {
                        Monitor.Exit(this);
                        progressReportValues._progressListener = null;
                        return;
                    }
                    progressReportValues._progressListener = _next;
                }

                internal override void HandleFromContext()
                {
                    ThrowIfInPool(this);
                    var currentContext = ts_currentContext;
                    ts_currentContext = _synchronizationContext;

                    Invoke1(_previousState);

                    ts_currentContext = currentContext;
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

                    ScheduleForHandle(this, _synchronizationContext);
                }

                private void Invoke1(Promise.State state)
                {
                    if (TryUnregisterAndIsNotCanceling(ref _cancelationRegistration) & !IsCanceled)
                    {
                        if (state == Promise.State.Resolved)
                        {
                            CallbackHelperVoid.InvokeAndCatchProgress(_progress, 1f, this);
                        }
                        // Release since Cancel() will not be invoked.
                        InterlockedAddWithUnsignedOverflowCheck(ref _progressFields._retainCounter, -1);
                    }

                    HandleNextInternal(_previousRejectContainer, _previousState);
                }

                void ICancelable.Cancel()
                {
                    ThrowIfInPool(this);
                    IsCanceled = true;
                    MaybeDispose();
                }

                internal override PromiseRefBase AddWaiter(short promiseId, HandleablePromiseBase waiter, out HandleablePromiseBase previousWaiter)
                {
                    if (ShouldInvokeSynchronous())
                    {
                        return AddWaiterImpl(promiseId, waiter, out previousWaiter);
                    }

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
                    return this; // It doesn't matter what we return since previousWaiter is set to PendingAwaitSentinel.s_instance.
                }

                // This is rare, only happens when the promise already completed (usually an already completed promise is not backed by a reference), or if a promise is incorrectly awaited twice.
                [MethodImpl(MethodImplOptions.NoInlining)]
                private PromiseRefBase VerifyAndHandleWaiter(HandleablePromiseBase waiter, out HandleablePromiseBase previousWaiter)
                {
                    // We do the verification process here instead of in the caller, because we need to handle continuations on the synchronization context.
                    if (CompareExchangeWaiter(waiter, PromiseCompletionSentinel.s_instance) != PromiseCompletionSentinel.s_instance)
                    {
                        previousWaiter = InvalidAwaitSentinel.s_instance;
                        return InvalidAwaitSentinel.s_instance;
                    }

                    // If this was configured to execute progress on a SynchronizationContext or the ThreadPool, force the waiter to execute on the same context for consistency.
                    if (_synchronizationContext == null)
                    {
                        // If there is no context, send it to the ThreadPool.
                        ThreadPool.QueueUserWorkItem(s_threadPoolCallback, this);
                    }
                    else
                    {
                        _synchronizationContext.Post(s_synchronizationContextCallback, this);
                    }
                    previousWaiter = PendingAwaitSentinel.s_instance;
                    return null; // It doesn't matter what we return since previousWaiter is set to PendingAwaitSentinel.s_instance.
                }

                private static void ExecuteFromContext(object state)
                {
                    // In case this is executed from a background thread, catch the exception and report it instead of crashing the app.
                    try
                    {
                        // This handles the waiter that was added after this was already complete.
                        var _this = state.UnsafeAs<PromiseProgress<TResult, TProgress>>();
                        ThrowIfInPool(_this);
                        // We don't need to synchronize access here because this is only called when the waiter is added after Invoke1 has completed, so there are no race conditions.
                        _this.HandleNext(_this._next, _this._previousRejectContainer, _this._previousState);
                    }
                    catch (Exception e)
                    {
                        // This should never happen.
                        ReportRejection(e, state as ITraceable);
                    }
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
                        State = _previousState;
                        return true;
                    }
                    return false;
                }
            } // PromiseProgress<TProgress>

            partial class PromiseSingleAwait<TResult>
            {
                internal override PromiseRefBase AddProgressWaiter(short promiseId, out HandleablePromiseBase previousWaiter, ref ProgressHookupValues progressHookupValues)
                {
                    var promiseSingleAwait = AddWaiter(promiseId, progressHookupValues.ProgressListener, out previousWaiter);
                    if (previousWaiter == PendingAwaitSentinel.s_instance)
                    {
                        SetProgressValuesAndGetPrevious(ref progressHookupValues);
                    }
                    return promiseSingleAwait;
                }
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private sealed partial class IndividualPromisePassThrough<TResult> : PromiseRef<TResult>
            {
                // This type is used to hook up each waiter in PromiseMultiAwait to its progress listener.
                // This is necessary because the _rejectContainerOrPreviousOrLink field is used to hook up the registered promises chain,
                // and it would not be possible to do that for multiple progress listeners with a single promise object. So we have to create dummy objects to register multiple.

                internal static IndividualPromisePassThrough<TResult> GetOrCreateAndRegister(PromiseMultiAwait<TResult> owner, ref ProgressHookupValues progressHookupValues)
                {
                    var passthrough = ObjectPool.TryTake<IndividualPromisePassThrough<TResult>>()
                        ?? new IndividualPromisePassThrough<TResult>();
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
                    if (_owner.TryRestoreWaiter(waiter, expected))
                    {
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
                protected override void OnForget(short promiseId) { throw new System.InvalidOperationException(); }
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
                private static ProgressMultiAwait<TResult> GetOrCreate(PromiseMultiAwait<TResult> owner)
                {
                    var passthrough = ObjectPool.TryTake<ProgressMultiAwait<TResult>>()
                        ?? new ProgressMultiAwait<TResult>();
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

                internal override void MaybeReportProgress(PromiseRefBase reporter, double progress)
                {
                    // Manually enter the lock so the next listener can enter its lock before unlocking this.
                    // This is necessary for race conditions so a progress report won't get ahead of another on a separate thread.
                    Monitor.Enter(_owner);
                    var progressReportValues = new ProgressReportValues(null, reporter, _owner, progress);
                    MaybeReportProgressImpl(ref progressReportValues);
                    progressReportValues.ReportProgressToAllListeners();
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
                        Monitor.Exit(this);
                        progressReportValues._progressListener = null;
                        return;
                    }

                    progressReportValues._lockedObject = this;
                    MaybeReportProgressImpl(ref progressReportValues);
                }

                private void MaybeReportProgressImpl(ref ProgressReportValues progressReportValues)
                {
                    ThrowIfInPool(this);
                    progressReportValues._progressListener = null;

                    if (_progressFields._currentReporter != progressReportValues._reporter)
                    {
                        ExitLock();
                        return;
                    }

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
                                goto DisposePassthrough;
                            }
                        }
                    }
                    return false;

                DisposePassthrough:
                    expected.UnsafeAs<IndividualPromisePassThrough<TResult>>().MaybeDispose();
                    return true;
                }
            } // PromiseMultiAwait

            partial struct DeferredIdAndProgress
            {
                internal bool TrySetProgress(int deferredId, float progress)
                {
                    Thread.MemoryBarrier(); // Make sure we're reading fresh values and other instructions are executed before this.
                    DeferredIdAndProgress comparand = this;
                    comparand._id = deferredId;
                    DeferredIdAndProgress newValue = comparand;
                    newValue._currentProgress = progress;
                    comparand._interlocker = Interlocked.CompareExchange(ref _interlocker, newValue._interlocker, comparand._interlocker);
                    // We could run this in a loop if it failed due to the progress changing and the id remaining the same,
                    // but it's fine if another thread set progress concurrently. In that case, we just need to know if the call was valid.
                    return comparand._id == deferredId;
                }
            }

            partial class DeferredPromiseBase<TResult>
            {
                [MethodImpl(InlineOption)]
                new protected void Reset()
                {
                    base.Reset();
                    _idAndProgress._currentProgress = 0f;
                }

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
                    progressHookupValues._currentProgress = _idAndProgress._currentProgress;
                    progressHookupValues.SetMinAndMaxFromDepth(0u);
                    progressHookupValues._expectedWaiter = this;
                    progressHookupValues._previous = null;
                    progressHookupValues.CurrentReporter = this;
                    progressHookupValues.RegisterHandler(this);
                }

                internal sealed override bool TryHookupProgressListenerAndGetPrevious(ref ProgressHookupValues progressHookupValues)
                {
                    if (CompareExchangeWaiter(progressHookupValues.ProgressListener, progressHookupValues._expectedWaiter) != progressHookupValues._expectedWaiter)
                    {
                        progressHookupValues._previous = null;
                        return false;
                    }
                    SetProgressValuesAndGetPrevious(ref progressHookupValues);
                    return true;
                }

                [MethodImpl(InlineOption)]
                public bool TryReportProgress(int deferredId, float progress)
                {
                    // 1.0 is a valid value, but we don't report it here, as that will be reported automatically when the promise is resolved.
                    // We also protect against other values outside of the 0 to 1 range.
                    bool shouldReportProgress = progress >= 0 & progress < 1f;
                    if (!shouldReportProgress)
                    {
                        return deferredId == DeferredId;
                    }

                    // It is possible this is called concurrently on another thread after this object has been repooled.
                    // User code really shouldn't use this in that manner, which the deferredId protects against accidental usage.
                    // But in case that does happen (like in unit tests for stress testing), calling MaybeReportProgress will be a no-op.

                    if (!_idAndProgress.TrySetProgress(deferredId, progress))
                    {
                        return false;
                    }

                    Thread.MemoryBarrier(); // Make sure we read _next after we write progress to handle race condition of a progress listener being hooked up on another thread.
                    _next.MaybeReportProgress(this, progress);
                    return true;
                }
            }

            partial class PromiseWaitPromise<TResult>
            {
                [MethodImpl(InlineOption)]
                new protected void Reset(ushort depth)
                {
                    base.Reset(depth);
                    _waitState = WaitState.First;
                }

                [MethodImpl(InlineOption)]
                partial void SetSecondPreviousAndMaybeHookupProgress(PromiseRefBase secondPrevious, PromiseRefBase handler)
                {
                    SetSecondPrevious(secondPrevious, handler);
                    var listenerProgressRange = new ProgressRange(0f, 1f);
                    _next.MaybeHookupProgressToAwaited(this, secondPrevious, ref _progressRange, ref listenerProgressRange);
                }

                [MethodImpl(InlineOption)]
                private void SetSecondPrevious(PromiseRefBase secondPrevious, PromiseRefBase handler)
                {
                    // These are volatile writes, so their write order will not be changed.
                    // This looks superfluous, but is necessary for progress hookup on another thread.
                    _waitState = WaitState.SettingSecond;
#if PROMISE_DEBUG
                    _previous = secondPrevious;
#endif
                    // Resolve race condition with progress hookup.
                    // Only set _rejectContainerOrPreviousOrLink if it's the same as the handler,
                    // otherwise it could break the registered promise chain.
                    Interlocked.CompareExchange(ref _rejectContainerOrPreviousOrLink, secondPrevious, handler);
                    _waitState = WaitState.Second;
                }

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
                    uint depth = Depth;
                    progressHookupValues._currentProgress = depth;
                    progressHookupValues._expectedWaiter = this;
                    var previous = _rejectContainerOrPreviousOrLink;
                    // We read wait state after previous. These are both volatile reads, so we don't need a full memory barrier.
                    if (_waitState == WaitState.First)
                    {
                        _progressRange._min = (float) progressHookupValues.GetLerpedProgressFromLocalProgress(depth);
                        _progressRange._max = (float) progressHookupValues.GetLerpedProgressFromLocalProgress(depth + 1u);
                        progressHookupValues._previous = previous as PromiseRefBase;
                        progressHookupValues.RegisterHandler(this);
                        return;
                    }

                    progressHookupValues.SetMinAndMaxFromDepth(depth);
                    WaitForSecondPreviousAssignment();
                    // We read _rejectContainerOrPreviousOrLink again instead of using the cached previous, as another thread may have changed it.
                    // This is a volatile read, so we don't need a memory barrier.
                    var prev = _rejectContainerOrPreviousOrLink as PromiseRefBase;
                    if (prev != null)
                    {
                        progressHookupValues.SetDivisorFromDepth(prev.Depth);
                    }
                    progressHookupValues._previous = prev;
                    progressHookupValues.RegisterHandler(this);
                }

                [MethodImpl(InlineOption)]
                private void WaitForSecondPreviousAssignment()
                {
                    if (_waitState == WaitState.SettingSecond)
                    {
                        // Very rare, this should almost never happen.
                        WaitForSecondPreviousAssignmentCore();
                    }
                }

                [MethodImpl(MethodImplOptions.NoInlining)]
                private void WaitForSecondPreviousAssignmentCore()
                {
                    var spinner = new SpinWait();
                    while (_waitState == WaitState.SettingSecond)
                    {
                        spinner.SpinOnce();
                    }
                }

                internal override bool TryHookupProgressListenerAndGetPrevious(ref ProgressHookupValues progressHookupValues)
                {
                    if (CompareExchangeWaiter(progressHookupValues.ProgressListener, progressHookupValues._expectedWaiter) != progressHookupValues._expectedWaiter)
                    {
                        progressHookupValues._previous = null;
                        return false;
                    }
                    SetProgressValuesAndGetPrevious(ref progressHookupValues);
                    return true;
                }
            } // PromiseWaitPromise

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
                    ProgressMerger.MaybeHookup(this, _completeProgress, _passThroughs.TakeAndClear(), ref progressHookupValues);
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
            private sealed partial class ProgressMerger : ProgressPassThrough
            {
                private ProgressMerger() { }

#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                ~ProgressMerger()
                {
                    try
                    {
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
#endif

                private static ProgressMerger GetOrCreate(PromiseRefBase targetMergePromise, ulong completedProgress, ulong expectedProgress, ValueLinkedStack<PromisePassThrough> passThroughs)
                {
                    var merger = ObjectPool.TryTake<ProgressMerger>()
                        ?? new ProgressMerger();
                    merger._targetMergePromise = targetMergePromise;
                    merger._passThroughs = passThroughs;
                    merger._currentProgress = completedProgress;
                    merger._divisorReciprocal = 1d / expectedProgress;
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                    merger._disposed = false;
#endif
                    return merger;
                }

                private void Dispose()
                {
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                    _disposed = true;
#endif
                    // Don't nullify target, if it is accessed after this is disposed, the checks on it will ensure nothing happens.
                    ObjectPool.MaybeRepool(this);
                }

                internal static void MaybeHookup(PromiseRefBase targetMergePromise, ulong completedProgress, ValueLinkedStack<PromisePassThrough> passThroughs, ref ProgressHookupValues progressHookupValues)
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
                    var merger = GetOrCreate(targetMergePromise, completedProgress, expectedProgress, passThroughs);
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
                        current = _currentProgress;
                        newValue = current + value;
                    } while (Interlocked.CompareExchange(ref _currentProgress, newValue, current) != current);
                    return newValue;
                }

                internal void ReportProgress(float oldProgress, ref ProgressReportValues progressReportValues)
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

                    // We only report the progress if the handler was not the last completed.
                    if (InterlockedAddWithUnsignedOverflowCheck(ref _retainCounter, -1) == 0)
                    {
                        Monitor.Exit(lockedObject);
                        Dispose();
                    }
                    else
                    {
                        var progressReportValues = new ProgressReportValues(null, this, lockedObject, maxProgress);
                        ReportProgress(oldProgress, ref progressReportValues);
                        progressReportValues.ReportProgressToAllListeners();
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

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private sealed partial class MergeProgressPassThrough : ProgressPassThrough
            {
                private MergeProgressPassThrough() { }

#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                ~MergeProgressPassThrough()
                {
                    try
                    {
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
#endif

                private static MergeProgressPassThrough GetOrCreate(ProgressMerger target, int index)
                {
                    var passThrough = ObjectPool.TryTake<MergeProgressPassThrough>()
                        ?? new MergeProgressPassThrough();
                    passThrough._target = target;
                    passThrough._currentProgress = 0f;
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
                    _target.ReportProgress(oldProgress, ref progressReportValues);
                    progressReportValues.ReportProgressToAllListeners();
                }

                internal override void MaybeReportProgress(PromiseRefBase reporter, double progress)
                {
                    // Manually enter the lock so the next listener can enter its lock before unlocking this.
                    // This is necessary for race conditions so a progress report won't get ahead of another on a separate thread.
                    Monitor.Enter(this);
                    var progressReportValues = new ProgressReportValues(null, reporter, this, progress);
                    MaybeReportProgressImpl(ref progressReportValues);
                    progressReportValues.ReportProgressToAllListeners();
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
                    _target.ReportProgress(oldProgress, ref progressReportValues);
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

                private static ProgressRacer GetOrCreate(PromiseRefBase targetRacePromise, ValueLinkedStack<PromisePassThrough> passThroughs)
                {
                    var racer = ObjectPool.TryTake<ProgressRacer>()
                        ?? new ProgressRacer();
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

                private static RaceProgressPassThrough GetOrCreate(ProgressRacer target, int index)
                {
                    var passThrough = ObjectPool.TryTake<RaceProgressPassThrough>()
                        ?? new RaceProgressPassThrough();
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

                internal override void MaybeReportProgress(PromiseRefBase reporter, double progress)
                {
                    // Manually enter the lock so the next listener can enter its lock before unlocking this.
                    // This is necessary for race conditions so a progress report won't get ahead of another on a separate thread.
                    Monitor.Enter(this);
                    var progressReportValues = new ProgressReportValues(null, reporter, this, progress);
                    MaybeReportProgressImpl(ref progressReportValues);
                    progressReportValues.ReportProgressToAllListeners();
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

            partial class PromisePassThrough
            {
                internal ushort Depth
                {
                    [MethodImpl(InlineOption)]
                    get { return _depth; }
                }
            } // PromisePassThrough

            partial class AsyncPromiseRef<TResult>
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
                    _fields._listenerProgressRange = new ProgressRange(
                        (float) progressHookupValues._min,
                        (float) progressHookupValues._max
                    );

                    // Read previous before read range to resolve race condition with .AwaitWithProgress.
                    // This is a volatile read, so we don't need a full memory barrier.
                    progressHookupValues._previous = _rejectContainerOrPreviousOrLink as PromiseRefBase;
                    progressHookupValues._expectedWaiter = this;
                    progressHookupValues.SetMinAndMaxFromLocalProgress(_fields._userProgressRange._min, _fields._userProgressRange._max);
                    if (progressHookupValues._previous != null)
                    {
                        progressHookupValues.SetDivisorFromDepth(progressHookupValues._previous.Depth);
                    }
                    progressHookupValues.RegisterHandler(this);
                }

                internal sealed override bool TryHookupProgressListenerAndGetPrevious(ref ProgressHookupValues progressHookupValues)
                {
                    if (CompareExchangeWaiter(progressHookupValues.ProgressListener, progressHookupValues._expectedWaiter) != progressHookupValues._expectedWaiter)
                    {
                        progressHookupValues._previous = null;
                        return false;
                    }
                    SetProgressValuesAndGetPrevious(ref progressHookupValues);
                    return true;
                }

                [MethodImpl(InlineOption)]
                new private void Reset()
                {
                    _fields._userProgressRange._min = 0f;
                    _fields._userProgressRange._max = 0f;
                    base.Reset();
                }

                [MethodImpl(InlineOption)]
                partial void SetAwaitedComplete(PromiseRefBase handler)
                {
                    // Resolve race condition with progress hookup.
                    // Only nullify _rejectContainerOrPreviousOrLink if it's the same as the handler,
                    // otherwise it could break the registered promise chain.
                    Interlocked.CompareExchange(ref _rejectContainerOrPreviousOrLink, null, handler);
                }
            } // AsyncPromiseRef

            [MethodImpl(InlineOption)]
            private void SetPreviousAndMaybeHookupAsyncProgress(PromiseRefBase waiter, float minProgress, float maxProgress, ref AsyncPromiseFields asyncFields)
            {
#if PROMISE_DEBUG
                _previous = waiter;
#endif
                if (float.IsNaN(minProgress))
                {
                    // We have to set min to previous max instead of current, since we can't know what the current would be if the previous awaited promise was rejected or canceled, and a progress listener was never hooked up.
                    // Technically, we could hook up a progress listener just to tell this what its current progress would be, but that would be extremely inefficient for the common case where it doesn't matter.
                    minProgress = asyncFields._userProgressRange._max;
                }
                // Write range before write previous to resolve race condition with progress listener hooking up to this.
                asyncFields._userProgressRange._min = minProgress;
                asyncFields._userProgressRange._max = maxProgress;

                // Resolve race condition with progress hookup.
                // Only write _rejectContainerOrPreviousOrLink if it's null,
                // otherwise it could break the registered promise chain.
                Interlocked.CompareExchange(ref _rejectContainerOrPreviousOrLink, waiter, null);

                _next.MaybeHookupProgressToAwaited(this, waiter, ref asyncFields._userProgressRange, ref asyncFields._listenerProgressRange);
            }
#endif // PROMISE_PROGRESS
        } // PromiseRefBase
    } // Internal
}