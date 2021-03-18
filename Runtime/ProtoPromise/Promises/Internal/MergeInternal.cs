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
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
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
            internal sealed partial class MergePromise : PromiseBranch, IMultiTreeHandleable
            {
                private struct Creator : ICreator<MergePromise>
                {
                    [MethodImpl(InlineOption)]
                    public MergePromise Create()
                    {
                        return new MergePromise();
                    }
                }

                private readonly object _locker = new object();
                private ValueLinkedStack<PromisePassThrough> _passThroughs;
                private Action<IValueContainer, object, int> _onPromiseResolved;
                private int _waitCount;

                private MergePromise() { }

                protected override void Dispose()
                {
                    base.Dispose();
                    _onPromiseResolved = null;
                    ObjectPool<ITreeHandleable>.MaybeRepool(this);
                }

                private static MergePromise Create()
                {
                    return ObjectPool<ITreeHandleable>.GetOrCreate<MergePromise, Creator>();
                }

                public static MergePromise GetOrCreate<T>(ValueLinkedStack<PromisePassThrough> promisePassThroughs, ref T value, Action<IValueContainer, object, int> onPromiseResolved,
                    uint pendingAwaits, uint totalAwaits, ulong completedProgress)
                {
                    var promise = Create();
                    promise._onPromiseResolved = onPromiseResolved;
                    promise._valueOrPrevious = ResolveContainer<T>.GetOrCreate(ref value, 1);
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
                    unchecked
                    {
                        _waitCount = (int) pendingAwaits;
                    }
                    Reset();
                    SetupProgress(promisePassThroughs, totalAwaits, completedProgress);

                    while (promisePassThroughs.IsNotEmpty)
                    {
                        var passThrough = promisePassThroughs.Pop();
                        // TODO: optimize away the lock here.
                        lock (_locker)
                        {
                            _passThroughs.Push(passThrough);
                            passThrough.SetTargetAndAddToOwner(this);
                        }
                    }
                }

                public override void Handle()
                {
                    HandleWaiter((IValueContainer) _valueOrPrevious);
                    if (_state == Promise.State.Resolved)
                    {
                        ResolveProgressListeners();
                    }
                    else
                    {
                        CancelProgressListeners(null);
                    }

                    if (Interlocked.Decrement(ref _waitCount) == 0)
                    {
                        MaybeDispose();
                    }
                }

                bool IMultiTreeHandleable.Handle(IValueContainer valueContainer, PromisePassThrough passThrough, int index)
                {
                    ThrowIfInPool(this);
                    lock (_locker)
                    {
                        if (_state != Promise.State.Pending)
                        {
                            _passThroughs.TryRemove(passThrough);
                        }
                        else
                        {
                            PromiseRef owner = passThrough.Owner;
                            owner._suppressRejection = true;
                            if (owner._state != Promise.State.Resolved) // Rejected/Canceled
                            {
                                valueContainer.Retain();
                                ((IValueContainer) _valueOrPrevious).Release();
                                _valueOrPrevious = valueContainer;
                                _state = owner._state;

                                // Remove all passthroughs
                                while (_passThroughs.IsNotEmpty)
                                {
                                    var p = _passThroughs.Pop();
                                    if (p != passThrough && p.TryRemoveFromOwnerAndRelease())
                                    {
                                        Interlocked.Decrement(ref _waitCount);
                                    }
                                }
                                return true;
                            }
                            else // Resolved
                            {
                                // TODO: optimize lock so that resolves can run in parallel.
                                _onPromiseResolved.Invoke(valueContainer, _valueOrPrevious, index);
                                _passThroughs.TryRemove(passThrough);
                                if (_waitCount == 1)
                                {
                                    _state = Promise.State.Resolved;
                                    return true;
                                }
                                IncrementProgress(passThrough);
                            }
                        }
                        if (Interlocked.Decrement(ref _waitCount) == 0)
                        {
                            MaybeDispose();
                        }
                        return false;
                    }
                }

                partial void IncrementProgress(PromisePassThrough passThrough);
                partial void SetupProgress(ValueLinkedStack<PromisePassThrough> promisePassThroughs, uint totalAwaits, ulong completedProgress);
            }

#if PROMISE_PROGRESS
            partial class MergePromise
            {
                // These are used to avoid rounding errors when normalizing the progress.
                // Use 64 bits to allow combining many promises with very deep chains.
                private double _progressScaler;
                private UnsignedFixed64 _unscaledProgress;

                partial void SetupProgress(ValueLinkedStack<PromisePassThrough> promisePassThroughs, uint totalAwaits, ulong completedProgress)
                {
#if PROMISE_DEBUG
                    checked
#endif
                    {
                        _unscaledProgress = new UnsignedFixed64(completedProgress);

                        ulong expectedProgressCounter = totalAwaits;
                        uint maxWaitDepth = 0;
                        foreach (var passThrough in promisePassThroughs)
                        {
                            uint waitDepth = passThrough.Owner._waitDepthAndProgress.WholePart;
                            expectedProgressCounter += waitDepth;
                            maxWaitDepth = Math.Max(maxWaitDepth, waitDepth);
                        }

                        // Use the longest chain as this depth.
                        _waitDepthAndProgress = new UnsignedFixed32(maxWaitDepth);
                        _progressScaler = (double) NextWholeProgress / (double) expectedProgressCounter;
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

                protected override bool SubscribeProgressIfWaiterAndContinueLoop(ref IProgressListener progressListener, out PromiseRef previous, Stack<PromisePassThrough> passThroughs)
                {
                    ThrowIfInPool(this);
                    bool firstSubscribe = _progressListeners.IsEmpty;
                    _progressListeners.Push(progressListener);
                    if (firstSubscribe & _state == Promise.State.Pending)
                    {
                        BorrowPassthroughs(passThroughs);
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