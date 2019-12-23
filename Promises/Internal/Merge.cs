#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#endif
#if !PROTO_PROMISE_CANCEL_DISABLE
#define PROMISE_CANCEL
#endif
#if !PROTO_PROMISE_PROGRESS_DISABLE
#define PROMISE_PROGRESS
#endif

#pragma warning disable RECS0096 // Type parameter is never used
#pragma warning disable IDE0018 // Inline variable declaration
#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable RECS0029 // Warns about property or indexer setters and event adders or removers that do not use the value parameter

using System;
using Proto.Utils;

namespace Proto.Promises
{
    partial class Promise
    {
        partial class Internal
        {
            public sealed partial class MergePromise<T> : PoolablePromise<T, MergePromise<T>>, IMultiTreeHandleable
            {
                private ValueLinkedStack<PromisePassThrough> _passThroughs;
                private uint _waitCount;
                Action<Promise, MergePromise<T>, int> _onPromiseResolved;

                private MergePromise() { }

                public static MergePromise<T> GetOrCreate(ValueLinkedStack<PromisePassThrough> promisePassThroughs, Action<Promise, MergePromise<T>, int> onPromiseResolved, int count, int skipFrames)
                {
                    var promise = _pool.IsNotEmpty ? (MergePromise<T>) _pool.Pop() : new MergePromise<T>();

                    foreach (var passThrough in promisePassThroughs)
                    {
                        passThrough.target = promise;
                    }
                    promise._passThroughs = promisePassThroughs;

                    promise._onPromiseResolved = onPromiseResolved;

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
                        else
                        {
                            _onPromiseResolved.Invoke(feed, this, index);
                            if (done)
                            {
                                ResolveInternalWithoutRelease();
                            }
                            else
                            {
                                IncrementProgress(feed);
                            }
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
            partial class MergePromise<T> : IInvokable
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