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
                    [MethodImpl((MethodImplOptions) 256)]
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
                    if (_waitCount == 0) // Quick fix until TODO is done.
                    {
                        base.Dispose();
                        _onPromiseResolved = null;
                        ObjectPool<ITreeHandleable>.MaybeRepool(this);
                    }
                }

                private static MergePromise Create()
                {
                    return ObjectPool<ITreeHandleable>.GetOrCreate<MergePromise, Creator>(new Creator());
                }

                public static MergePromise GetOrCreate<T>(ValueLinkedStack<PromisePassThrough> promisePassThroughs, ref T value, Action<IValueContainer, object, int> onPromiseResolved,
                    uint pendingAwaits, uint totalAwaits, ulong completedProgress)
                {
                    var promise = Create();
                    promise._onPromiseResolved = onPromiseResolved;
                    var container = ResolveContainer<T>.GetOrCreate(ref value);
                    container.Retain();
                    promise._valueOrPrevious = container;
                    promise.Setup(promisePassThroughs, pendingAwaits, totalAwaits, completedProgress);
                    return promise;
                }

                public static MergePromise GetOrCreate(ValueLinkedStack<PromisePassThrough> promisePassThroughs, uint pendingAwaits, uint totalAwaits, ulong completedProgress)
                {
                    var promise = Create();
                    promise._onPromiseResolved = (_, __, ___) => { };
                    promise._valueOrPrevious = ResolveContainerVoid.GetOrCreate();
                    promise.Setup(promisePassThroughs, pendingAwaits, totalAwaits, completedProgress);
                    return promise;
                }

                private void Setup(ValueLinkedStack<PromisePassThrough> promisePassThroughs, uint pendingAwaits, uint totalAwaits, ulong completedProgress)
                {
                    _passThroughs = promisePassThroughs;
                    _waitCount = pendingAwaits;
                    Reset();
                    SetupProgress(totalAwaits, completedProgress);
                    _pending = true;

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
                    ThrowIfInPool(this);
                    // TODO: remove all passthroughs from their owners when this is completed early.
                    _passThroughs.Remove(passThrough);
                    PromiseRef owner = passThrough.Owner;
                    bool done = --_waitCount == 0;
                    bool handle = false;
                    if (_pending)
                    {
                        owner._suppressRejection = true;
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
                    return handle;
                }

                void IMultiTreeHandleable.ReAdd(PromisePassThrough passThrough)
                {
                    _passThroughs.Push(passThrough);
                }

                partial void IncrementProgress(PromisePassThrough passThrough);
                partial void SetupProgress(uint totalAwaits, ulong completedProgress);
            }

#if PROMISE_PROGRESS
            partial class MergePromise
            {
                // These are used to avoid rounding errors when normalizing the progress.
                // Use 64 bits to allow combining many promises with very deep chains.
                private double _progressScaler;
                private UnsignedFixed64 _unscaledProgress;

                partial void SetupProgress(uint totalAwaits, ulong completedProgress)
                {
#if PROMISE_DEBUG
                    checked
#endif
                    {
                        _unscaledProgress = new UnsignedFixed64(completedProgress);

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
                        _progressScaler = (double) NextWholeProgress / (double) (expectedProgressCounter + totalAwaits);
                    }
                }

                partial void IncrementProgress(PromisePassThrough passThrough)
                {
                    IncrementProgress(passThrough.GetProgressDifferenceToCompletion());
                }

                protected override bool SubscribeProgressAndContinueLoop(ref IProgressListener progressListener, out PromiseRef previous)
                {
                    ThrowIfInPool(this);
                    // This is guaranteed to be pending.
                    previous = this;
                    return true;
                }

                protected override bool SubscribeProgressIfWaiterAndContinueLoop(ref IProgressListener progressListener, out PromiseRef previous, ref ValueLinkedStack<PromisePassThrough> passThroughs)
                {
                    ThrowIfInPool(this);
                    bool firstSubscribe = _progressListeners.IsEmpty;
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
                    ThrowIfInPool(this);
                    return new UnsignedFixed32(_unscaledProgress.ToDouble() * _progressScaler);
                }

                void IMultiTreeHandleable.IncrementProgress(uint amount, UnsignedFixed32 senderAmount, UnsignedFixed32 ownerAmount)
                {
                    ThrowIfInPool(this);
                    IncrementProgress(amount);
                }

                private void IncrementProgress(uint amount)
                {
                    if (_state != Promise.State.Pending) return;

                    // TODO: thread synchronization.
                    _unscaledProgress.Increment(amount);
                    UnsignedFixed32 newProgress = CurrentProgress();

                    foreach (var progressListener in _progressListeners)
                    {
                        progressListener.SetProgress(this, newProgress);
                    }
                }
            }
#endif
        }
    }
}