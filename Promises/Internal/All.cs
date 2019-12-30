#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif
#if !PROTO_PROMISE_CANCEL_DISABLE
#define PROMISE_CANCEL
#else
#undef PROMISE_CANCEL
#endif
#if !PROTO_PROMISE_PROGRESS_DISABLE
#define PROMISE_PROGRESS
#else
#undef PROMISE_PROGRESS
#endif

#pragma warning disable RECS0001 // Class is declared partial but has only one part
#pragma warning disable RECS0096 // Type parameter is never used
#pragma warning disable IDE0018 // Inline variable declaration
#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable RECS0029 // Warns about property or indexer setters and event adders or removers that do not use the value parameter

using System;
using System.Collections.Generic;
using Proto.Utils;

namespace Proto.Promises
{
    partial class Promise
    {
        partial class Internal
        {
            public static Promise _All<TEnumerator>(TEnumerator promises, int skipFrames) where TEnumerator : IEnumerator<Promise>
            {
                ValidateArgument(promises, "promises", skipFrames + 1);
                if (!promises.MoveNext())
                {
                    // If promises is empty, just return a resolved promise.
                    return Resolved();
                }
                int count;
                var passThroughs = WrapInPassThroughs(promises, out count, skipFrames + 1);
                return AllPromise0.GetOrCreate(passThroughs, count, skipFrames + 1);
            }

            public static Promise<IList<T>> _All<T, TEnumerator>(TEnumerator promises, IList<T> valueContainer, int skipFrames) where TEnumerator : IEnumerator<Promise<T>>
            {
                ValidateArgument(promises, "promises", skipFrames + 1);
                if (!promises.MoveNext())
                {
                    // If promises is empty, just return a resolved promise.
                    if (valueContainer.Count > 0) // Count check in case valueContainer is an array .
                    {
                        valueContainer.Clear();
                    }
                    return Resolved(valueContainer);
                }
                int count;
                var passThroughs = WrapInPassThroughs<T, TEnumerator>(promises, out count, skipFrames + 1);

                // Only change the count of the valueContainer if it's greater or less than the promises count. This allows arrays to be used if they are the proper length.
                int i = valueContainer.Count;
                if (i < count)
                {
                    do
                    {
                        valueContainer.Add(default(T));
                    }
                    while (++i < count);
                }
                else while (i > count)
                    {
                        valueContainer.RemoveAt(--i);
                    }

                var promise = MergePromise<IList<T>>.GetOrCreate(passThroughs, (feed, target, index) =>
                {
                    target._value[index] = ((PromiseInternal<T>) feed)._value;
                }, count, skipFrames + 1);
                promise._value = valueContainer;
                return promise;
            }

            public sealed partial class AllPromise0 : PoolablePromise<AllPromise0>, IMultiTreeHandleable
            {
                private ValueLinkedStack<PromisePassThrough> _passThroughs;
                private uint _waitCount;

                private AllPromise0() { }

                public static Promise GetOrCreate(ValueLinkedStack<PromisePassThrough> promisePassThroughs, int count, int skipFrames)
                {
                    var promise = _pool.IsNotEmpty ? (AllPromise0) _pool.Pop() : new AllPromise0();

                    foreach (var passThrough in promisePassThroughs)
                    {
                        passThrough.target = promise;
                    }
                    promise._passThroughs = promisePassThroughs;

                    promise._waitCount = (uint) count;
                    // Retain this until all promises resolve/reject/cancel
                    promise.Reset(skipFrames + 1);
                    promise._retainCounter = promise._waitCount + 1u;

                    return promise;
                }

                private bool ReleaseOne()
                {
                    ReleaseWithoutDisposeCheck();
                    return --_waitCount == 0;
                }

                private void MaybeRelease(bool done)
                {
                    if (done)
                    {
                        while (_passThroughs.IsNotEmpty)
                        {
                            _passThroughs.Pop().Release();
                        }
                        ReleaseInternal();
                    }
                }

                void IMultiTreeHandleable.Cancel(IValueContainerOrPrevious cancelValue)
                {
                    if (_state == State.Pending)
                    {
                        CancelInternal(cancelValue);
                    }
                    MaybeRelease(ReleaseOne());
                }

                void IMultiTreeHandleable.Handle(Promise feed, int index)
                {
                    bool done = ReleaseOne();
                    if (_state == State.Pending)
                    {
                        feed._wasWaitedOn = true;
                        if (feed._state == State.Rejected)
                        {
                            RejectInternalWithoutRelease(feed._rejectedOrCanceledValueOrPrevious);
                        }
                        else if (done)
                        {
                            ResolveInternalWithoutRelease();
                        }
                        else
                        {
                            IncrementProgress(feed);
                        }
                    }
                    MaybeRelease(done);
                }

                partial void IncrementProgress(Promise feed);

                void IMultiTreeHandleable.ReAdd(PromisePassThrough passThrough)
                {
                    _passThroughs.Push(passThrough);
                }

                protected override void OnCancel()
                {
                    CancelProgressListeners();
                    AddToCancelQueueFront(ref _nextBranches);
                }
            }

#if PROMISE_PROGRESS
            partial class AllPromise0 : IInvokable
            {
                // These are used to avoid rounding errors when normalizing the progress.
                private float _expected;
                private UnsignedFixed32 _currentAmount;
                private bool _invokingProgress;
                private bool _suspended;

                protected override void Reset(int skipFrames)
                {
#if PROMISE_DEBUG
                    checked
#endif
                    {
                        base.Reset(skipFrames + 1);
                        _currentAmount = default(UnsignedFixed32);
                        _invokingProgress = false;
                        _suspended = false;

                        uint expectedProgressCounter = 0;
                        uint maxWaitDepth = 0;
                        foreach (var passThrough in _passThroughs)
                        {
                            uint waitDepth = passThrough.Owner._waitDepthAndProgress.WholePart;
                            expectedProgressCounter += waitDepth;
                            maxWaitDepth = Math.Max(maxWaitDepth, waitDepth);
                        }
                        _expected = expectedProgressCounter + _waitCount;

                        // Use the longest chain as this depth.
                        _waitDepthAndProgress = new UnsignedFixed32(maxWaitDepth);
                    }
                }

                partial void IncrementProgress(Promise feed)
                {
                    bool subscribedProgress = _progressListeners.IsNotEmpty;
                    uint increment = subscribedProgress ? feed._waitDepthAndProgress.GetDifferenceToNextWholeAsUInt32() : feed._waitDepthAndProgress.GetIncrementedWholeTruncated().ToUInt32();
                    IncrementProgress(increment);
                }

                protected override bool SubscribeProgressAndContinueLoop(ref IProgressListener progressListener, out Promise previous)
                {
                    // This is guaranteed to be pending.
                    previous = this;
                    return true;
                }

                protected override bool SubscribeProgressIfWaiterAndContinueLoop(ref IProgressListener progressListener, out Promise previous, ref ValueLinkedStack<PromisePassThrough> passThroughs)
                {
                    bool firstSubscribe = _progressListeners.IsEmpty;
                    progressListener.Retain();
                    _progressListeners.Push(progressListener);
                    if (firstSubscribe & _state == State.Pending)
                    {
                        BorrowPassthroughs(ref passThroughs);
                    }

                    previous = null;
                    return false;
                }

                void IMultiTreeHandleable.IncrementProgress(uint amount, UnsignedFixed32 senderAmount, UnsignedFixed32 ownerAmount)
                {
                    IncrementProgress(amount);
                }

                void IMultiTreeHandleable.CancelOrIncrementProgress(uint amount, UnsignedFixed32 senderAmount, UnsignedFixed32 ownerAmount)
                {
                    _suspended = true;
                    _currentAmount.Increment(amount);
                }

                private void IncrementProgress(uint amount)
                {
                    _suspended = false;
                    _currentAmount.Increment(amount);
                    if (!_invokingProgress & _state == State.Pending)
                    {
                        _invokingProgress = true;
                        AddToFrontOfProgressQueue(this);
                    }
                }

                protected override uint GetIncrementMultiplier()
                {
                    return _waitDepthAndProgress.WholePart + 1u;
                }

                void IInvokable.Invoke()
                {
                    if (_state != State.Pending | _suspended)
                    {
                        return;
                    }

                    _invokingProgress = false;

                    // Calculate the normalized progress for all the awaited promises.
                    // Use double for better precision.
                    float progress = (float) (_currentAmount.ToDouble() / _expected);

                    uint increment = _waitDepthAndProgress.AssignNewDecimalPartAndGetDifferenceAsUInt32(progress) * GetIncrementMultiplier();

                    foreach (var progressListener in _progressListeners)
                    {
                        progressListener.IncrementProgress(this, increment);
                    }
                }
            }
#endif
        }
    }
}