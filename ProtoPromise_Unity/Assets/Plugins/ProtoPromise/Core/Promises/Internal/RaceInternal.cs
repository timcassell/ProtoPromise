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
#pragma warning disable CS0420 // A reference to a volatile field will not be treated as volatile

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
            internal sealed partial class RacePromise : MultiHandleablePromiseBase
            {
                private RacePromise() { }

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

                internal static RacePromise GetOrCreate(ValueLinkedStack<PromisePassThrough> promisePassThroughs, uint pendingAwaits)
                {
                    var promise = ObjectPool<HandleablePromiseBase>.TryTake<RacePromise>()
                        ?? new RacePromise();

                    checked
                    {
                        // Extra retain for handle.
                        ++pendingAwaits;
                    }
                    unchecked
                    {
                        promise._raceSmallFields._waitCount = (int) pendingAwaits;
                    }
                    promise.Reset();
                    promise.SetupProgress(promisePassThroughs);

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
                            if (addCount != 0 && Interlocked.Add(ref promise._raceSmallFields._waitCount, addCount) == 0)
                            {
                                promise.MaybeDispose();
                            }
                        }
                    }

                    return promise;
                }

                internal override void Handle(ref ExecutionScheduler executionScheduler)
                {
                    ValueContainer valueContainer = (ValueContainer) _valueOrPrevious;
                    Promise.State state = valueContainer.GetState();
                    State = state;
                    HandleWaiter(valueContainer, ref executionScheduler);
                    HandleProgressListener(state, ref executionScheduler);

                    if (Interlocked.Decrement(ref _raceSmallFields._waitCount) == 0)
                    {
                        MaybeDispose();
                    }
                }

                internal override void Handle(PromiseRef owner, ValueContainer valueContainer, PromisePassThrough passThrough, ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);

                    if (Interlocked.CompareExchange(ref _valueOrPrevious, valueContainer, null) == null)
                    {
                        owner.SuppressRejection = true;
                        valueContainer.Retain();

                        Interlocked.Decrement(ref _raceSmallFields._waitCount);
                        executionScheduler.ScheduleSynchronous(this);
                        return;
                    }
                    if (Interlocked.Decrement(ref _raceSmallFields._waitCount) == 0)
                    {
                        MaybeDispose();
                    }
                }

                internal int Depth
                {
#if PROMISE_PROGRESS
                    [MethodImpl(InlineOption)]
                    get { return _raceSmallFields._depthAndProgress.WholePart; }
#else
                    [MethodImpl(InlineOption)]
                    get { return 0; }
#endif
                }

                partial void SetupProgress(ValueLinkedStack<PromisePassThrough> promisePassThroughs);
            }

#if PROMISE_PROGRESS
            partial class RacePromise : IProgressInvokable
            {
                internal override void HandleProgressListener(Promise.State state, ref ExecutionScheduler executionScheduler)
                {
                    HandleProgressListener(state, _raceSmallFields._depthAndProgress.GetIncrementedWholeTruncated(), ref executionScheduler);
                }

                protected override sealed PromiseRef MaybeAddProgressListenerAndGetPreviousRetained(ref IProgressListener progressListener, ref Fixed32 lastKnownProgress)
                {
                    // Unnecessary to set last known since we know SetInitialProgress will be called on this.
                    ThrowIfInPool(this);
                    progressListener.Retain();
                    _progressListener = progressListener;
                    return null;
                }

                protected override sealed void SetInitialProgress(IProgressListener progressListener, Fixed32 lastKnownProgress, ref ExecutionScheduler executionScheduler)
                {
                    SetInitialProgress(progressListener, _raceSmallFields._currentProgress, _raceSmallFields._depthAndProgress.GetIncrementedWholeTruncated(), ref executionScheduler);
                }

                partial void SetupProgress(ValueLinkedStack<PromisePassThrough> promisePassThroughs)
                {
                    _raceSmallFields._currentProgress = default(Fixed32);

                    // Expect the shortest chain to finish first.
                    int minWaitDepth = int.MaxValue;
                    foreach (var passThrough in promisePassThroughs)
                    {
                        minWaitDepth = Math.Min(minWaitDepth, passThrough.Depth);
                    }
                    _raceSmallFields._depthAndProgress = new Fixed32(minWaitDepth);
                }

                internal override void IncrementProgress(uint amount, Fixed32 senderAmount, Fixed32 ownerAmount, ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);

                    // Use double for better precision.
                    var newAmount = new Fixed32(senderAmount.ToDouble() * (_raceSmallFields._depthAndProgress.WholePart + 1) / (double) (ownerAmount.WholePart + 1));
                    if (_raceSmallFields._currentProgress.InterlockedTrySetIfGreater(newAmount, senderAmount))
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
                    var progress = _raceSmallFields._currentProgress;
                    _smallFields.InterlockedUnsetFlags(PromiseFlags.InProgressQueue);
                    ReportProgress(progress, ref executionScheduler);
                    MaybeDispose();
                }
            }
#endif
        }
    }
}