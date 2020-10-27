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
using Proto.Utils;

namespace Proto.Promises
{
    partial class Promise
    {
        partial class InternalProtected
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal sealed partial class MergePromise<T> : PromiseIntermediate<T>, IMultiTreeHandleable
            {
                private static ValueLinkedStack<Internal.ITreeHandleable> _pool;

                static MergePromise()
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
                Action<Internal.IValueContainer, Internal.ResolveContainer<T>, int> _onPromiseResolved;
                private uint _waitCount;
                private bool _pending;

                private MergePromise() { }

                public static MergePromise<T> GetOrCreate(ValueLinkedStack<PromisePassThrough> promisePassThroughs, ref T value, Action<Internal.IValueContainer, Internal.ResolveContainer<T>, int> onPromiseResolved, int count)
                {
                    var promise = _pool.IsNotEmpty ? (MergePromise<T>) _pool.Pop() : new MergePromise<T>();

                    promise._passThroughs = promisePassThroughs;
                    promise._onPromiseResolved = onPromiseResolved;

                    promise._waitCount = (uint) count;
                    promise.Reset();
                    promise._pending = true;
                    // Retain this until all promises resolve/reject/cancel.
                    promise.RetainInternal();

                    var container = Internal.ResolveContainer<T>.GetOrCreate(ref value);
                    container.Retain();
                    promise._valueOrPrevious = container;

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
                    bool done = --_waitCount == 0;
                    bool handle = false;
                    if (_pending)
                    {
                        owner._wasWaitedOn = true;
                        if (owner._state != State.Resolved)
                        {
                            _pending = false;
                            ((Internal.ResolveContainer<T>) _valueOrPrevious).Release();
                            valueContainer.Retain();
                            _valueOrPrevious = valueContainer;
                            handle = true;
                        }
                        else
                        {
                            _onPromiseResolved.Invoke(valueContainer, (Internal.ResolveContainer<T>) _valueOrPrevious, index);
                            if (done)
                            {
                                _pending = false;
                                handle = true;
                            }
                            else
                            {
                                IncrementProgress(passThrough);
                            }
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

                partial void IncrementProgress(PromisePassThrough passThrough);
            }

#if PROMISE_PROGRESS
            partial class MergePromise<T> : IInvokable
            {
                // These are used to avoid rounding errors when normalizing the progress.
                private uint _expected;
                private UnsignedFixed32 _currentAmount;
                private bool _invokingProgress;

                protected override void Reset()
                {
#if PROMISE_DEBUG
                    checked
#endif
                    {
                        base.Reset();
                        _currentAmount = default(UnsignedFixed32);
                        _invokingProgress = false;

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

                partial void IncrementProgress(PromisePassThrough passThrough)
                {
                    IncrementProgress(passThrough.GetProgressDifferenceToCompletion());
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
                    // Calculate the normalized progress for all the awaited promises.
                    // Use double for better precision. Scale to the calculated depth.
                    double progress = _currentAmount.ToDouble() * NextWholeProgress / (double) _expected;
                    return new UnsignedFixed32((float) progress);
                }

                void IMultiTreeHandleable.IncrementProgress(uint amount, UnsignedFixed32 senderAmount, UnsignedFixed32 ownerAmount)
                {
                    IncrementProgress(amount);
                }

                private void IncrementProgress(uint amount)
                {
                    _currentAmount.Increment(amount);
                    if (!_invokingProgress & _state == State.Pending)
                    {
                        RetainInternal();
                        _invokingProgress = true;
                        AddToFrontOfProgressQueue(this);
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
                    UnsignedFixed32 newProgress = CurrentProgress();

                    foreach (var progressListener in _progressListeners)
                    {
                        progressListener.SetProgress(this, newProgress);
                    }

                    ReleaseInternal();
                }
            }
#endif
        }
    }
}