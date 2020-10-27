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
            public static Promise CreateRace<TEnumerator>(TEnumerator promises) where TEnumerator : IEnumerator<Promise>
            {
                ValidateArgument(promises, "promises", 2);
                if (!promises.MoveNext())
                {
                    throw new EmptyArgumentException("promises", "You must provide at least one element to Race.", Internal.GetFormattedStacktrace(2));
                }
                int count;
                var passThroughs = WrapInPassThroughs(promises, out count);
                return RacePromiseVoid.GetOrCreate(passThroughs, count);
            }

            public static Promise<T> CreateRace<T, TEnumerator>(TEnumerator promises) where TEnumerator : IEnumerator<Promise<T>>
            {
                ValidateArgument(promises, "promises", 2);
                if (!promises.MoveNext())
                {
                    throw new EmptyArgumentException("promises", "You must provide at least one element to Race.", Internal.GetFormattedStacktrace(2));
                }
                int count;
                var passThroughs = WrapInPassThroughs<T, TEnumerator>(promises, out count);
                return RacePromise<T>.GetOrCreate(passThroughs, count);
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal sealed partial class RacePromiseVoid : PromiseIntermediate, IMultiTreeHandleable
            {
                private static ValueLinkedStack<Internal.ITreeHandleable> _pool;

                static RacePromiseVoid()
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

                private RacePromiseVoid() { }

                public static Promise GetOrCreate(ValueLinkedStack<PromisePassThrough> promisePassThroughs, int count)
                {
                    var promise = _pool.IsNotEmpty ? (RacePromiseVoid) _pool.Pop() : new RacePromiseVoid();

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

                bool IMultiTreeHandleable.Handle(Internal.IValueContainer valueContainer, PromisePassThrough passThrough, int index)
                {
                    Promise owner = passThrough.Owner;
                    bool handle = _valueOrPrevious == null;
                    if (handle)
                    {
                        owner._wasWaitedOn = true;
                        valueContainer.Retain();
                        _valueOrPrevious = valueContainer;
                    }
                    if (--_waitCount == 0)
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

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal sealed partial class RacePromise<T> : PromiseIntermediate<T>, IMultiTreeHandleable
            {
                private static ValueLinkedStack<Internal.ITreeHandleable> _pool;

                static RacePromise()
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

                private RacePromise() { }

                public static Promise<T> GetOrCreate(ValueLinkedStack<PromisePassThrough> promisePassThroughs, int count)
                {
                    var promise = _pool.IsNotEmpty ? (RacePromise<T>) _pool.Pop() : new RacePromise<T>();

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

                bool IMultiTreeHandleable.Handle(Internal.IValueContainer valueContainer, PromisePassThrough passThrough, int index)
                {
                    Promise owner = passThrough.Owner;
                    bool handle = _valueOrPrevious == null;
                    if (handle)
                    {
                        owner._wasWaitedOn = true;
                        valueContainer.Retain();
                        _valueOrPrevious = valueContainer;
                    }
                    if (--_waitCount == 0)
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
            partial class RacePromiseVoid : IInvokable
            {
                private UnsignedFixed32 _currentAmount;
                private bool _invokingProgress;

                protected override void Reset()
                {
                    base.Reset();
                    _currentAmount = default(UnsignedFixed32);
                    _invokingProgress = false;

                    uint minWaitDepth = uint.MaxValue;
                    foreach (var passThrough in _passThroughs)
                    {
                        Promise owner = passThrough.Owner;
                        if (owner != null)
                        {
                            minWaitDepth = Math.Min(minWaitDepth, owner._waitDepthAndProgress.WholePart);
                        }
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

                protected override UnsignedFixed32 CurrentProgress()
                {
                    return _currentAmount;
                }

                void IMultiTreeHandleable.IncrementProgress(uint amount, UnsignedFixed32 senderAmount, UnsignedFixed32 ownerAmount)
                {
                    // Use double for better precision.
                    float progress = (float) (senderAmount.ToDouble() * NextWholeProgress / (double) (ownerAmount.WholePart + 1u));
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

                void IInvokable.Invoke()
                {
                    if (_state != State.Pending)
                    {
                        ReleaseInternal();
                        return;
                    }

                    _invokingProgress = false;

                    foreach (var progressListener in _progressListeners)
                    {
                        progressListener.SetProgress(this, _currentAmount);
                    }

                    ReleaseInternal();
                }
            }

            partial class RacePromise<T> : IInvokable
            {
                private UnsignedFixed32 _currentAmount;
                private bool _invokingProgress;

                protected override void Reset()
                {
                    base.Reset();
                    _currentAmount = default(UnsignedFixed32);
                    _invokingProgress = false;

                    uint minWaitDepth = uint.MaxValue;
                    foreach (var passThrough in _passThroughs)
                    {
                        Promise owner = passThrough.Owner;
                        if (owner != null)
                        {
                            minWaitDepth = Math.Min(minWaitDepth, owner._waitDepthAndProgress.WholePart);
                        }
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

                protected override UnsignedFixed32 CurrentProgress()
                {
                    return _currentAmount;
                }

                void IMultiTreeHandleable.IncrementProgress(uint amount, UnsignedFixed32 senderAmount, UnsignedFixed32 ownerAmount)
                {
                    // Use double for better precision.
                    float progress = (float) (senderAmount.ToDouble() * NextWholeProgress / (double) (ownerAmount.WholePart + 1u));
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

                void IInvokable.Invoke()
                {
                    if (_state != State.Pending)
                    {
                        ReleaseInternal();
                        return;
                    }

                    _invokingProgress = false;

                    foreach (var progressListener in _progressListeners)
                    {
                        progressListener.SetProgress(this, _currentAmount);
                    }

                    ReleaseInternal();
                }
            }
#endif
        }
    }
}