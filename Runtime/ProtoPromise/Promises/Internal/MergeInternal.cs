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
#pragma warning disable IDE0034 // Simplify 'default' expression

using System;
using System.Runtime.CompilerServices;
using Proto.Utils;

namespace Proto.Promises
{
    partial class Internal
    {
        partial class PromiseRef
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal sealed partial class MergePromise : PromiseRef, IMultiTreeHandleable
            {
                private struct Creator : ICreator<MergePromise>
                {
                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    public MergePromise Create()
                    {
                        return new MergePromise();
                    }
                }

                private ValueLinkedStack<PromisePassThrough> _passThroughs;
                private Action<IValueContainer, object, int> _onPromiseResolved;
                private uint _waitCount;
                private bool _pending;

                private MergePromise() { }

                protected override void Dispose()
                {
                    base.Dispose();
                    _onPromiseResolved = null;
                    ObjectPool<ITreeHandleable>.MaybeRepool(this);
                }

                private static MergePromise Create()
                {
                    return ObjectPool<ITreeHandleable>.GetOrCreate<MergePromise, Creator>(new Creator());
                }

                public static MergePromise GetOrCreate<T>(ValueLinkedStack<PromisePassThrough> promisePassThroughs, ref T value, Action<IValueContainer, object, int> onPromiseResolved, int count)
                {
                    var promise = Create();
                    promise._onPromiseResolved = onPromiseResolved;
                    var container = ResolveContainer<T>.GetOrCreate(ref value);
                    container.Retain();
                    promise._valueOrPrevious = container;
                    promise.Setup(promisePassThroughs, count);
                    return promise;
                }

                public static MergePromise GetOrCreate(ValueLinkedStack<PromisePassThrough> promisePassThroughs, int count)
                {
                    var promise = Create();
                    promise._onPromiseResolved = (_, __, ___) => { };
                    promise._valueOrPrevious = ResolveContainerVoid.GetOrCreate();
                    promise.Setup(promisePassThroughs, count);
                    return promise;
                }

                private void Setup(ValueLinkedStack<PromisePassThrough> promisePassThroughs, int count)
                {
                    _passThroughs = promisePassThroughs;
                    _waitCount = (uint) count;
                    Reset();
                    _pending = true;
                    // Retain this until all promises resolve/reject/cancel.
                    RetainInternal();

                    foreach (var passThrough in promisePassThroughs)
                    {
                        passThrough.SetTargetAndAddToOwner(this);
                    }
                }

                protected override void Execute(IValueContainer valueContainer)
                {
                    HandleSelf(valueContainer);
                }

                bool IMultiTreeHandleable.Handle(IValueContainer valueContainer, PromisePassThrough passThrough, int index)
                {
                    PromiseRef owner = passThrough.Owner;
                    bool done = --_waitCount == 0;
                    bool handle = false;
                    if (_pending)
                    {
                        owner._wasWaitedOn = true;
                        if (owner._state != Promise.State.Resolved)
                        {
                            _pending = false;
                            ((IValueContainer) _valueOrPrevious).Release();
                            valueContainer.Retain();
                            _valueOrPrevious = valueContainer;
                            handle = true;
                        }
                        else
                        {
                            _onPromiseResolved.Invoke(valueContainer, _valueOrPrevious, index);
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
            partial class MergePromise : IInvokable
            {
                // These are used to avoid rounding errors when normalizing the progress.
                // Use 64 bits to allow combining many promises with very deep chains.
                private double _progressScaler;
                private UnsignedFixed64 _unscaledProgress;
                private bool _invokingProgress;

                protected override void Reset()
                {
#if PROMISE_DEBUG
                    checked
#endif
                    {
                        base.Reset();
                        _unscaledProgress = default(UnsignedFixed64);
                        _invokingProgress = false;

                        ulong expectedProgressCounter = 0L;
                        uint maxWaitDepth = 0;
                        foreach (var passThrough in _passThroughs)
                        {
                            PromiseRef owner = passThrough.Owner;
                            if (owner != null)
                            {
                                uint waitDepth = owner._waitDepthAndProgress.WholePart;
                                expectedProgressCounter += waitDepth;
                                maxWaitDepth = Math.Max(maxWaitDepth, waitDepth);
                            }
                        }

                        // Use the longest chain as this depth.
                        _waitDepthAndProgress = new UnsignedFixed32(maxWaitDepth);
                        _progressScaler = (double) NextWholeProgress / (double) (expectedProgressCounter + _waitCount);
                    }
                }

                partial void IncrementProgress(PromisePassThrough passThrough)
                {
                    IncrementProgress(passThrough.GetProgressDifferenceToCompletion());
                }

                protected override bool SubscribeProgressAndContinueLoop(ref IProgressListener progressListener, out PromiseRef previous)
                {
                    // This is guaranteed to be pending.
                    previous = this;
                    return true;
                }

                protected override bool SubscribeProgressIfWaiterAndContinueLoop(ref IProgressListener progressListener, out PromiseRef previous, ref ValueLinkedStack<PromisePassThrough> passThroughs)
                {
                    bool firstSubscribe = _progressListeners.IsEmpty;
                    progressListener.Retain();
                    _progressListeners.Push(progressListener);
                    if (firstSubscribe & _state == Promise.State.Pending)
                    {
                        BorrowPassthroughs(ref passThroughs);
                    }

                    previous = null;
                    return false;
                }

                protected override UnsignedFixed32 CurrentProgress()
                {
                    return new UnsignedFixed32(_unscaledProgress.ToDouble() * _progressScaler);
                }

                void IMultiTreeHandleable.IncrementProgress(uint amount, UnsignedFixed32 senderAmount, UnsignedFixed32 ownerAmount)
                {
                    IncrementProgress(amount);
                }

                private void IncrementProgress(uint amount)
                {
                    _unscaledProgress.Increment(amount);
                    if (!_invokingProgress & _state == Promise.State.Pending)
                    {
                        RetainInternal();
                        _invokingProgress = true;
                        AddToFrontOfProgressQueue(this);
                    }
                }

                void IInvokable.Invoke()
                {
                    if (_state != Promise.State.Pending)
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