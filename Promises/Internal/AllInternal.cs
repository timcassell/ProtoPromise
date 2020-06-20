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

#pragma warning disable RECS0001 // Class is declared partial but has only one part
#pragma warning disable IDE0018 // Inline variable declaration
#pragma warning disable IDE0034 // Simplify 'default' expression

using System;
using System.Collections.Generic;
using Proto.Utils;

namespace Proto.Promises
{
    partial class Promise
    {
        [System.Diagnostics.DebuggerNonUserCode]
        partial class InternalProtected
        {
            public static Promise CreateAll<TEnumerator>(TEnumerator promises) where TEnumerator : IEnumerator<Promise>
            {
                ValidateArgument(promises, "promises", 2);
                if (!promises.MoveNext())
                {
                    // If promises is empty, just return a resolved promise.
                    return Resolved();
                }
                int count;
                var passThroughs = WrapInPassThroughs(promises, out count);
                return AllPromise0.GetOrCreate(passThroughs, count);
            }

            public static Promise<IList<T>> CreateAll<T, TEnumerator>(TEnumerator promises, IList<T> valueContainer) where TEnumerator : IEnumerator<Promise<T>>
            {
                ValidateArgument(promises, "promises", 2);
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
                var passThroughs = WrapInPassThroughs<T, TEnumerator>(promises, out count);

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

                return MergePromise<IList<T>>.GetOrCreate(passThroughs, ref valueContainer, (feed, target, index) =>
                {
                    target.value[index] = ((Internal.ResolveContainer<T>) feed).value;
                }, count);
            }

            [System.Diagnostics.DebuggerNonUserCode]
            internal sealed partial class AllPromise0 : PromiseIntermediate, IMultiTreeHandleable
            {
                private static ValueLinkedStack<Internal.ITreeHandleable> _pool;

                static AllPromise0()
                {
                    Internal.OnClearPool += () => _pool.Clear();
                }

                protected override void Dispose()
                {
                    base.Dispose();
                    if (Config.ObjectPooling == PoolType.All)
                    {
                        _pool.Push(this);
                    }
                }

                private ValueLinkedStack<PromisePassThrough> _passThroughs;
                private uint _waitCount;

                private AllPromise0() { }

                public static Promise GetOrCreate(ValueLinkedStack<PromisePassThrough> promisePassThroughs, int count)
                {
                    var promise = _pool.IsNotEmpty ? (AllPromise0) _pool.Pop() : new AllPromise0();

                    promise._passThroughs = promisePassThroughs;

                    promise._waitCount = (uint) count;
                    promise.Reset();
                    // Retain this until all promises resolve/reject/cancel.
                    promise.RetainInternal();

                    foreach (var passThrough in promisePassThroughs)
                    {
                        passThrough.SetTargetAndAddToOwner(promise);
                    }

                    return promise;
                }

#if CSHARP_7_3_OR_NEWER // Really C# 7.2 but this is the closest symbol Unity offers.
                private
#endif
                protected override void Execute(Internal.IValueContainer valueContainer)
                {
                    HandleSelf(valueContainer);
                }

                bool IMultiTreeHandleable.Handle(Internal.IValueContainer valueContainer, Promise owner, int index)
                {
                    bool done = --_waitCount == 0;
                    bool handle = false;
                    if (_valueOrPrevious == null)
                    {
                        owner._wasWaitedOn = true;
                        if (owner._state != State.Resolved | done)
                        {
                            valueContainer.Retain();
                            _valueOrPrevious = valueContainer;
                            handle = true;
                        }
                        else
                        {
                            IncrementProgress(owner);
                        }
                    }
                    if (done)
                    {
                        while (_passThroughs.IsNotEmpty)
                        {
                            _passThroughs.Pop().Release();
                        }
                        ReleaseInternal();
                    }
                    return handle;
                }

                void IMultiTreeHandleable.ReAdd(PromisePassThrough passThrough)
                {
                    _passThroughs.Push(passThrough);
                }

                partial void IncrementProgress(Promise feed);
            }

#if PROMISE_PROGRESS
            partial class AllPromise0 : IInvokable
            {
                // These are used to avoid rounding errors when normalizing the progress.
                private uint _expected;
                private UnsignedFixed32 _currentAmount;
                private bool _invokingProgress;
                private bool _suspended;

                protected override void Reset()
                {
#if PROMISE_DEBUG
                    checked
#endif
                    {
                        base.Reset();
                        _currentAmount = default(UnsignedFixed32);
                        _invokingProgress = false;
                        _suspended = false;

                        uint expectedProgressCounter = 0;
                        uint maxWaitDepth = 0;
                        foreach (var passThrough in _passThroughs)
                        {
                            Promise owner = passThrough.Owner;
                            if (owner != null)
                            {
                                uint waitDepth = owner._waitDepthAndProgress.WholePart;
                                expectedProgressCounter += waitDepth;
                                maxWaitDepth = Math.Max(maxWaitDepth, waitDepth);
                            }
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
                        RetainInternal();
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
                        ReleaseInternal();
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

                    ReleaseInternal();
                }
            }
#endif
        }
    }
}