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

#pragma warning disable 0420 // A reference to a volatile field will not be treated as volatile

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Proto.Promises
{
    partial class Internal
    {
        partial class PromiseRefBase
        {
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
            } // DeferredPromiseBase

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
        } // PromiseRefBase
    } // Internal
}

#endif // PROMISE_PROGRESS