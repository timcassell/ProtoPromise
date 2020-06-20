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
        partial class InternalProtected
        {
            public static Promise CreateFirst<TEnumerator>(TEnumerator promises) where TEnumerator : IEnumerator<Promise>
            {
                ValidateArgument(promises, "promises", 2);
                if (!promises.MoveNext())
                {
                    throw new EmptyArgumentException("promises", "You must provide at least one element to First.", Internal.GetFormattedStacktrace(2));
                }
                int count;
                var passThroughs = WrapInPassThroughs(promises, out count);
                return FirstPromise0.GetOrCreate(passThroughs, count);
            }

            public static Promise<T> CreateFirst<T, TEnumerator>(TEnumerator promises) where TEnumerator : IEnumerator<Promise<T>>
            {
                ValidateArgument(promises, "promises", 2);
                if (!promises.MoveNext())
                {
                    throw new EmptyArgumentException("promises", "You must provide at least one element to First.", Internal.GetFormattedStacktrace(2));
                }
                int count;
                var passThroughs = WrapInPassThroughs<T, TEnumerator>(promises, out count);
                return FirstPromise<T>.GetOrCreate(passThroughs, count);
            }

            [System.Diagnostics.DebuggerNonUserCode]
            internal sealed partial class FirstPromise0 : PromiseIntermediate, IMultiTreeHandleable
            {
                private static ValueLinkedStack<Internal.ITreeHandleable> _pool;

                static FirstPromise0()
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

                private FirstPromise0() { }

                public static Promise GetOrCreate(ValueLinkedStack<PromisePassThrough> promisePassThroughs, int count)
                {
                    var promise = _pool.IsNotEmpty ? (FirstPromise0) _pool.Pop() : new FirstPromise0();

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
                    owner._wasWaitedOn = true;
                    bool done = --_waitCount == 0;
                    bool handle = _valueOrPrevious == null & (owner._state == State.Resolved | done);
                    if (handle)
                    {
                        valueContainer.Retain();
                        _valueOrPrevious = valueContainer;
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
            }

            [System.Diagnostics.DebuggerNonUserCode]
            internal sealed partial class FirstPromise<T> : PromiseIntermediate<T>, IMultiTreeHandleable
            {
                private static ValueLinkedStack<Internal.ITreeHandleable> _pool;

                static FirstPromise()
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

                private FirstPromise() { }

                public static Promise<T> GetOrCreate(ValueLinkedStack<PromisePassThrough> promisePassThroughs, int count)
                {
                    var promise = _pool.IsNotEmpty ? (FirstPromise<T>) _pool.Pop() : new FirstPromise<T>();

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
                    owner._wasWaitedOn = true;
                    bool done = --_waitCount == 0;
                    bool handle = _valueOrPrevious == null & (owner._state == State.Resolved | done);
                    if (handle)
                    {
                        valueContainer.Retain();
                        _valueOrPrevious = valueContainer;
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
            }

#if PROMISE_PROGRESS
            partial class FirstPromise0 : IInvokable
            {
                private UnsignedFixed32 _currentAmount;
                private bool _invokingProgress;
                private bool _suspended;

                protected override void Reset()
                {
                    base.Reset();
                    _currentAmount = default(UnsignedFixed32);
                    _invokingProgress = false;
                    _suspended = false;

                    uint minWaitDepth = uint.MaxValue;
                    foreach (var passThrough in _passThroughs)
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
                            RetainInternal();
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
                        ReleaseInternal();
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

                    ReleaseInternal();
                }
            }

            partial class FirstPromise<T> : IInvokable
            {
                private UnsignedFixed32 _currentAmount;
                private bool _invokingProgress;
                private bool _suspended;

                protected override void Reset()
                {
                    base.Reset();
                    _currentAmount = default(UnsignedFixed32);
                    _invokingProgress = false;
                    _suspended = false;

                    uint minWaitDepth = uint.MaxValue;
                    foreach (var passThrough in _passThroughs)
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
                            RetainInternal();
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
                        ReleaseInternal();
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

                    ReleaseInternal();
                }
            }
#endif
        }
    }
}