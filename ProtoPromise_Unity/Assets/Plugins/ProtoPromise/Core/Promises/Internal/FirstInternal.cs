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

#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable 0420 // A reference to a volatile field will not be treated as volatile

using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Proto.Promises
{
    partial class Internal
    {
        partial class PromiseRef
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal sealed partial class FirstPromise : MultiHandleablePromiseBase
            {
                private FirstPromise() { }

                protected override void Dispose()
                {
                    base.Dispose();
#if PROMISE_DEBUG
                    lock (_locker)
                    {
                        while (_passThroughs.IsNotEmpty)
                        {
                            _passThroughs.Pop().Release();
                        }
                    }
#endif
                    ObjectPool<HandleablePromiseBase>.MaybeRepool(this);
                }

                internal static FirstPromise GetOrCreate(ValueLinkedStack<PromisePassThrough> promisePassThroughs, uint pendingAwaits, ushort depth)
                {
                    var promise = ObjectPool<HandleablePromiseBase>.TryTake<FirstPromise>()
                        ?? new FirstPromise();

                    checked
                    {
                        // Extra retain for handle.
                        ++pendingAwaits;
                    }
                    unchecked
                    {
                        promise._firstSmallFields._waitCount = (int) pendingAwaits;
                    }
                    promise.Reset(depth);

                    while (promisePassThroughs.IsNotEmpty)
                    {
                        var passThrough = promisePassThroughs.Pop();
#if PROMISE_DEBUG
                        passThrough.Retain();
                        lock (promise._locker)
                        {
                            promise._passThroughs.Push(passThrough);
                        }
#endif
                        passThrough.SetTargetAndAddToOwner(promise);
                        if (promise._valueOrPrevious != null)
                        {
                            // This was completed potentially before all passthroughs were hooked up. Release all remaining passthroughs.
                            int addCount = 0;
                            while (promisePassThroughs.IsNotEmpty)
                            {
                                var p = promisePassThroughs.Pop();
                                p.Owner.MaybeDispose();
                                p.Release();
                                --addCount;
                            }
                            if (addCount != 0 && InterlockedAddWithOverflowCheck(ref promise._firstSmallFields._waitCount, addCount, 0) == 0)
                            {
                                promise.MaybeDispose();
                            }
                        }
                    }

                    return promise;
                }

                internal override void Handle(ref ExecutionScheduler executionScheduler)
                {
                    Handle(ref _firstSmallFields._waitCount, ref executionScheduler);
                }

                internal override void Handle(PromiseRef owner, ValueContainer valueContainer, PromisePassThrough passThrough, ref ExecutionScheduler executionScheduler)
                {
                    // Retain while handling, then release when complete for thread safety.
                    InterlockedRetainDisregardId();

                    if (owner.State != Promise.State.Resolved) // Rejected/Canceled
                    {
                        int remaining = InterlockedAddWithOverflowCheck(ref _firstSmallFields._waitCount, -1, 0);
                        if (remaining == 1)
                        {
                            if (Interlocked.CompareExchange(ref _valueOrPrevious, valueContainer, null) == null)
                            {
                                valueContainer.Retain();
                                executionScheduler.ScheduleSynchronous(this);
                            }
                        }
                        else if (remaining == 0)
                        {
                            _smallFields.InterlockedTryReleaseComplete();
                        }
                    }
                    else // Resolved
                    {
                        if (Interlocked.CompareExchange(ref _valueOrPrevious, valueContainer, null) == null)
                        {
                            valueContainer.Retain();
                            executionScheduler.ScheduleSynchronous(this);
                        }
                        if (InterlockedAddWithOverflowCheck(ref _firstSmallFields._waitCount, -1, 0) == 0)
                        {
                            _smallFields.InterlockedTryReleaseComplete();
                        }
                    }

                    MaybeDispose();
                }
            }

#if PROMISE_PROGRESS
            partial class FirstPromise : IProgressInvokable
            {
                new private void Reset(ushort depth)
                {
                    _firstSmallFields._currentProgress = default(Fixed32);
                    base.Reset(depth);
                }

                internal override void HandleProgressListener(Promise.State state, ref ExecutionScheduler executionScheduler)
                {
                    HandleProgressListener(state, Fixed32.FromWholePlusOne(Depth), ref executionScheduler);
                }

                protected override sealed PromiseRef MaybeAddProgressListenerAndGetPreviousRetained(ref IProgressListener progressListener, ref Fixed32 lastKnownProgress)
                {
                    // Unnecessary to set last known since we know SetInitialProgress will be called on this.
                    ThrowIfInPool(this);
                    progressListener.Retain();
                    _progressListener = progressListener;
                    return null;
                }

                protected override sealed void SetInitialProgress(IProgressListener progressListener, ref Fixed32 progress, out PromiseSingleAwaitWithProgress nextRef, ref ExecutionScheduler executionScheduler)
                {
                    progress = _firstSmallFields._currentProgress;
                    SetInitialProgress(progressListener, ref progress, Fixed32.FromWholePlusOne(Depth), out nextRef, ref executionScheduler);
                }

                internal override void IncrementProgress(uint amount, Fixed32 senderAmount, Fixed32 ownerAmount, ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);

                    var newAmount = senderAmount.MultiplyAndDivide(Depth + 1, ownerAmount.WholePart + 1);
                    if (_firstSmallFields._currentProgress.InterlockedTrySetIfGreater(newAmount, senderAmount))
                    {
                        if ((_smallFields.InterlockedSetFlags(PromiseFlags.InProgressQueue) & PromiseFlags.InProgressQueue) == 0) // Was not already in progress queue?
                        {
                            InterlockedRetainDisregardId();
                            executionScheduler.ScheduleProgressSynchronous(this);
                        }
                    }
                }

                void IProgressInvokable.Invoke(ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    Thread.MemoryBarrier(); // Make sure we're reading fresh progress (since the field cannot be marked volatile).
                    var progress = _firstSmallFields._currentProgress;
                    _smallFields.InterlockedUnsetFlags(PromiseFlags.InProgressQueue);
                    ReportProgress(progress, ref executionScheduler);
                    MaybeDispose();
                }
            }
#endif
        }
    }
}