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
using System.Collections.Generic;
using Proto.Utils;

namespace Proto.Promises
{
    partial class Promise
    {
        partial class Internal
        {
            public sealed partial class FirstPromise0 : PoolablePromise<FirstPromise0>, IMultiTreeHandleable
            {
                private ValueLinkedStack<PromisePassThrough> passThroughs;
                private uint _waitCount;

                private FirstPromise0() { }

                public static Promise GetOrCreate<TEnumerator>(TEnumerator promises, int skipFrames) where TEnumerator : IEnumerator<Promise>
                {
                    if (!promises.MoveNext())
                    {
                        throw new EmptyArgumentException("promises", "You must provide at least one element to First.", GetFormattedStacktrace(skipFrames + 1));
                    }

                    var promise = _pool.IsNotEmpty ? (FirstPromise0) _pool.Pop() : new FirstPromise0();

                    var target = promises.Current;
                    ValidateElementNotNull(target, "promises", "A promise was null", skipFrames + 1);
                    ValidateOperation(target, skipFrames + 1);
                    int promiseIndex = 0;
                    // Hook up pass throughs
                    var passThroughs = new ValueLinkedStack<PromisePassThrough>(PromisePassThrough.GetOrCreate(target, promise, promiseIndex));
                    while (promises.MoveNext())
                    {
                        target = promises.Current;
                        ValidateElementNotNull(target, "promises", "A promise was null", skipFrames + 1);
                        ValidateOperation(target, skipFrames + 1);
                        passThroughs.Push(PromisePassThrough.GetOrCreate(target, promise, ++promiseIndex));
                    }
                    promise.passThroughs = passThroughs;

                    promise._waitCount = (uint) promiseIndex + 1u;
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
                        while (passThroughs.IsNotEmpty)
                        {
                            passThroughs.Pop().Release();
                        }
                        ReleaseInternal();
                    }
                }

                void IMultiTreeHandleable.Cancel(IValueContainerOrPrevious cancelValue)
                {
                    bool done = ReleaseOne();
                    if (_state == State.Pending & done)
                    {
                        CancelInternal(cancelValue);
                    }
                    MaybeRelease(done);
                }

                void IMultiTreeHandleable.Handle(Promise feed, int index)
                {
                    bool done = ReleaseOne();
                    if (_state == State.Pending)
                    {
                        if (feed._state == State.Resolved)
                        {
                            feed._wasWaitedOn = true;
                            ResolveInternalWithoutRelease();
                        }
                        else if (done)
                        {
                            feed._wasWaitedOn = true;
                            HandleSelfWithoutRelease(feed);
                        }
                    }
                    MaybeRelease(done);
                }

                void IMultiTreeHandleable.ReAdd(PromisePassThrough passThrough)
                {
                    passThroughs.Push(passThrough);
                }

                protected override void OnCancel()
                {
                    CancelProgressListeners();
                    AddToCancelQueueFront(ref _nextBranches);
                }
            }

            public sealed partial class FirstPromise<T> : PoolablePromise<T, FirstPromise<T>>, IMultiTreeHandleable
            {
                private ValueLinkedStack<PromisePassThrough> passThroughs;
                private uint _waitCount;

                private FirstPromise() { }

                public static Promise<T> GetOrCreate<TEnumerator>(TEnumerator promises, int skipFrames) where TEnumerator : IEnumerator<Promise<T>>
                {
                    if (!promises.MoveNext())
                    {
                        throw new EmptyArgumentException("promises", "You must provide at least one element to First.", GetFormattedStacktrace(skipFrames + 1));
                    }

                    var promise = _pool.IsNotEmpty ? (FirstPromise<T>) _pool.Pop() : new FirstPromise<T>();

                    var target = promises.Current;
                    ValidateElementNotNull(target, "promises", "A promise was null", skipFrames + 1);
                    ValidateOperation(target, skipFrames + 1);
                    int promiseIndex = 0;
                    // Hook up pass throughs
                    var passThroughs = new ValueLinkedStack<PromisePassThrough>(PromisePassThrough.GetOrCreate(target, promise, promiseIndex));
                    while (promises.MoveNext())
                    {
                        target = promises.Current;
                        ValidateElementNotNull(target, "promises", "A promise was null", skipFrames + 1);
                        ValidateOperation(target, skipFrames + 1);
                        passThroughs.Push(PromisePassThrough.GetOrCreate(target, promise, ++promiseIndex));
                    }
                    promise.passThroughs = passThroughs;

                    promise._waitCount = (uint) promiseIndex + 1u;
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
                        while (passThroughs.IsNotEmpty)
                        {
                            passThroughs.Pop().Release();
                        }
                        ReleaseInternal();
                    }
                }

                void IMultiTreeHandleable.Cancel(IValueContainerOrPrevious cancelValue)
                {
                    bool done = ReleaseOne();
                    if (_state == State.Pending & done)
                    {
                        CancelInternal(cancelValue);
                    }
                    MaybeRelease(done);
                }

                void IMultiTreeHandleable.Handle(Promise feed, int index)
                {
                    bool done = ReleaseOne();
                    if (_state == State.Pending)
                    {
                        if (feed._state == State.Resolved)
                        {
                            feed._wasWaitedOn = true;
                            _value = ((PromiseInternal<T>) feed).Value;
                            ResolveInternalWithoutRelease();
                        }
                        else if (done)
                        {
                            feed._wasWaitedOn = true;
                            HandleSelfWithoutRelease(feed);
                        }
                    }
                    MaybeRelease(done);
                }

                void IMultiTreeHandleable.ReAdd(PromisePassThrough passThrough)
                {
                    passThroughs.Push(passThrough);
                }

                protected override void OnCancel()
                {
                    CancelProgressListeners();
                    AddToCancelQueueFront(ref _nextBranches);
                }
            }

#if PROMISE_PROGRESS
            partial class FirstPromise0 : IInvokable
            {
                private UnsignedFixed32 _currentAmount;
                private bool _invokingProgress;
                private bool _suspended;

                protected override void Reset(int skipFrames)
                {
                    base.Reset(skipFrames + 1);
                    _currentAmount = default(UnsignedFixed32);
                    _invokingProgress = false;
                    _suspended = false;

                    uint minWaitDepth = uint.MaxValue;
                    foreach (var passThrough in passThroughs)
                    {
                        minWaitDepth = Math.Min(minWaitDepth, passThrough.Owner._waitDepthAndProgress.WholePart);
                    }

                    // Expect the shortest chain to finish first.
                    _waitDepthAndProgress = new UnsignedFixed32(minWaitDepth);
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
                    _suspended = false;
                    // Use double for better precision.
                    float progress = (float) ((double) senderAmount.ToUInt32() * (double) GetIncrementMultiplier() / (double) ownerAmount.GetIncrementedWholeTruncated().ToUInt32());
                    var newAmount = new UnsignedFixed32(progress);
                    if (newAmount > _currentAmount)
                    {
                        _currentAmount = newAmount;
                        if (!_invokingProgress)
                        {
                            _invokingProgress = true;
                            AddToFrontOfProgressQueue(this);
                        }
                    }
                }

                void IMultiTreeHandleable.CancelOrIncrementProgress(uint amount, UnsignedFixed32 senderAmount, UnsignedFixed32 ownerAmount)
                {
                    _suspended = true;
                    // Use double for better precision.
                    float progress = (float) ((double) senderAmount.ToUInt32() * (double) GetIncrementMultiplier() / (double) ownerAmount.GetIncrementedWholeTruncated().ToUInt32());
                    var newAmount = new UnsignedFixed32(progress);
                    if (newAmount > _currentAmount)
                    {
                        _currentAmount = newAmount;
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

                    uint multiplier = GetIncrementMultiplier();

                    // Calculate the normalized progress.
                    // Use double for better precision.
                    float progress = (float) (_currentAmount.ToDouble() / multiplier);

                    uint increment = _waitDepthAndProgress.AssignNewDecimalPartAndGetDifferenceAsUInt32(progress) * multiplier;

                    foreach (var progressListener in _progressListeners)
                    {
                        progressListener.IncrementProgress(this, increment);
                    }
                }
            }

            partial class FirstPromise<T> : IInvokable
            {
                private UnsignedFixed32 _currentAmount;
                private bool _invokingProgress;
                private bool _suspended;

                protected override void Reset(int skipFrames)
                {
                    base.Reset(skipFrames + 1);
                    _currentAmount = default(UnsignedFixed32);
                    _invokingProgress = false;
                    _suspended = false;

                    uint minWaitDepth = uint.MaxValue;
                    foreach (var passThrough in passThroughs)
                    {
                        minWaitDepth = Math.Min(minWaitDepth, passThrough.Owner._waitDepthAndProgress.WholePart);
                    }

                    // Expect the shortest chain to finish first.
                    _waitDepthAndProgress = new UnsignedFixed32(minWaitDepth);
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
                    _suspended = false;
                    // Use double for better precision.
                    float progress = (float) ((double) senderAmount.ToUInt32() * (double) GetIncrementMultiplier() / (double) ownerAmount.GetIncrementedWholeTruncated().ToUInt32());
                    var newAmount = new UnsignedFixed32(progress);
                    if (newAmount > _currentAmount)
                    {
                        _currentAmount = newAmount;
                        if (!_invokingProgress)
                        {
                            _invokingProgress = true;
                            AddToFrontOfProgressQueue(this);
                        }
                    }
                }

                void IMultiTreeHandleable.CancelOrIncrementProgress(uint amount, UnsignedFixed32 senderAmount, UnsignedFixed32 ownerAmount)
                {
                    _suspended = true;
                    // Use double for better precision.
                    float progress = (float) ((double) senderAmount.ToUInt32() * (double) GetIncrementMultiplier() / (double) ownerAmount.GetIncrementedWholeTruncated().ToUInt32());
                    var newAmount = new UnsignedFixed32(progress);
                    if (newAmount > _currentAmount)
                    {
                        _currentAmount = newAmount;
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

                    uint multiplier = GetIncrementMultiplier();

                    // Calculate the normalized progress.
                    // Use double for better precision.
                    float progress = (float) (_currentAmount.ToDouble() / multiplier);

                    uint increment = _waitDepthAndProgress.AssignNewDecimalPartAndGetDifferenceAsUInt32(progress) * multiplier;

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