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
            internal sealed partial class FirstPromise : PromiseBranch, IMultiTreeHandleable
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
                    ObjectPool<ITreeHandleable>.MaybeRepool(this);
                }

                public static FirstPromise GetOrCreate(ValueLinkedStack<PromisePassThrough> promisePassThroughs, uint pendingAwaits)
                {
                    var promise = ObjectPool<ITreeHandleable>.TryTake<FirstPromise>()
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
                    promise.Reset();
                    promise.SetupProgress(promisePassThroughs);

                    while (promisePassThroughs.IsNotEmpty)
                    {
                        var passThrough = promisePassThroughs.Pop();
                        passThrough.Owner.SuppressRejection = true;
#if PROMISE_DEBUG
                        passThrough.Retain();
                        promise._passThroughs.Push(passThrough);
#endif
                        passThrough.SetTargetAndAddToOwner(promise);
                        if (promise._valueOrPrevious != null)
                        {
                            // This was completed potentially before all passthroughs were hooked up. Release all remaining passthroughs.
                            int addCount = 0;
                            while (promisePassThroughs.IsNotEmpty)
                            {
                                var p = promisePassThroughs.Pop();
                                p.Owner.SuppressRejection = true;
                                p.Owner.MaybeDispose();
                                p.Release();
                                --addCount;
                            }
                            if (addCount != 0 && Interlocked.Add(ref promise._firstSmallFields._waitCount, addCount) == 0)
                            {
                                promise.MaybeDispose();
                            }
                        }
                    }

                    return promise;
                }

                public override void Handle(ref ValueLinkedStack<ITreeHandleable> executionStack)
                {
                    IValueContainer valueContainer = (IValueContainer) _valueOrPrevious;
                    Promise.State state = valueContainer.GetState();
                    State = state;
                    HandleWaiter(valueContainer, ref executionStack);
                    HandleProgressListener(state);

                    if (Interlocked.Decrement(ref _firstSmallFields._waitCount) == 0)
                    {
                        MaybeDispose();
                    }
                }

                public bool Handle(PromiseRef owner, IValueContainer valueContainer, PromisePassThrough passThrough, int index) // IMultiTreeHandleable.Handle
                {
                    ThrowIfInPool(this);

                    int addWaitCount;
                    if (owner.State != Promise.State.Resolved) // Rejected/Canceled
                    {
                        int remaining = Interlocked.Decrement(ref _firstSmallFields._waitCount);
                        if (remaining != 1 || Interlocked.CompareExchange(ref _valueOrPrevious, valueContainer, null) != null)
                        {
                            if (remaining == 0)
                            {
                                MaybeDispose();
                            }
                            return false;
                        }
                        addWaitCount = 0;
                    }
                    else // Resolved
                    {
                        if (Interlocked.CompareExchange(ref _valueOrPrevious, valueContainer, null) != null)
                        {
                            if (Interlocked.Decrement(ref _firstSmallFields._waitCount) == 0)
                            {
                                MaybeDispose();
                            }
                            return false;
                        }
                        addWaitCount = -1;
                    }
                    valueContainer.Retain();
                    Interlocked.Add(ref _firstSmallFields._waitCount, addWaitCount);
                    return true;
                }

                partial void SetupProgress(ValueLinkedStack<PromisePassThrough> promisePassThroughs);
            }

#if PROMISE_PROGRESS
            partial class FirstPromise : IProgressInvokable
            {
                protected override PromiseRef AddProgressListenerAndGetPreviousRetained(ref IProgressListener progressListener)
                {
                    ThrowIfInPool(this);
                    _progressListener = progressListener;
                    return null;
                }

                partial void SetupProgress(ValueLinkedStack<PromisePassThrough> promisePassThroughs)
                {
                    _firstSmallFields._currentAmount = default(Fixed32);

                    // Expect the shortest chain to finish first.
                    int minWaitDepth = int.MaxValue;
                    foreach (var passThrough in promisePassThroughs)
                    {
                        minWaitDepth = Math.Min(minWaitDepth, passThrough.Owner._smallFields._waitDepthAndProgress.WholePart);
                    }
                    _smallFields._waitDepthAndProgress = new Fixed32(minWaitDepth);
                }

                protected override Fixed32 CurrentProgress()
                {
                    ThrowIfInPool(this);
                    Thread.MemoryBarrier(); // Make sure we're reading fresh progress (since the field cannot be marked volatile).
                    return _firstSmallFields._currentAmount;
                }

                void IMultiTreeHandleable.IncrementProgress(uint amount, Fixed32 senderAmount, Fixed32 ownerAmount, bool shouldReport)
                {
                    ThrowIfInPool(this);

                    // Use double for better precision.
                    var newAmount = new Fixed32(senderAmount.ToDouble() * NextWholeProgress / (double) (ownerAmount.WholePart + 1u));
                    if (shouldReport & _firstSmallFields._currentAmount.InterlockedTrySetIfGreater(newAmount))
                    {
                        if ((_smallFields._stateAndFlags.InterlockedSetProgressFlags(ProgressFlags.InProgressQueue) & ProgressFlags.InProgressQueue) == 0) // Was not already in progress queue?
                        {
                            InterlockedRetainDisregardId();
                            AddToFrontOfProgressQueue(this);
                        }
                    }
                }

                void IProgressInvokable.Invoke()
                {
                    var progress = CurrentProgress();
                    _smallFields._stateAndFlags.InterlockedUnsetProgressFlags(ProgressFlags.InProgressQueue);
                    ReportProgress(progress);
                    MaybeDispose();
                }
            }
#endif
        }
    }
}