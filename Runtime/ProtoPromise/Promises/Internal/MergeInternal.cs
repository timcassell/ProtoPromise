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
            internal partial class MergePromise : PromiseBranch, IMultiTreeHandleable
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
                private int _waitCount;

                private MergePromise() { }

                protected override void Dispose()
                {
                    base.Dispose();
                    // Release all passthroughs.
                    while (_passThroughs.IsNotEmpty)
                    {
                        _passThroughs.Pop().Release();
                    }
                    ObjectPool<ITreeHandleable>.MaybeRepool(this);
                }

                [MethodImpl(InlineOption)]
                private void SuperDispose()
                {
                    base.Dispose();
                }

                public static MergePromise GetOrCreate(ValueLinkedStack<PromisePassThrough> promisePassThroughs, uint pendingAwaits, uint totalAwaits, ulong completedProgress)
                {
                    var promise = ObjectPool<ITreeHandleable>.GetOrCreate<MergePromise, Creator>();
                    promise.Setup(promisePassThroughs, pendingAwaits, totalAwaits, completedProgress);
                    return promise;
                }

                public static MergePromise GetOrCreate<T>(ValueLinkedStack<PromisePassThrough> promisePassThroughs, ref T value, Action<IValueContainer, ResolveContainer<T>, int> onPromiseResolved,
                    uint pendingAwaits, uint totalAwaits, ulong completedProgress)
                {
                    var promise = MergePromiseT<T>.GetOrCreate(ref value, onPromiseResolved);
                    promise.Setup(promisePassThroughs, pendingAwaits, totalAwaits, completedProgress);
                    return promise;
                }

                private void Setup(ValueLinkedStack<PromisePassThrough> promisePassThroughs, uint pendingAwaits, uint totalAwaits, ulong completedProgress)
                {
                    checked
                    {
                        // Extra retain for handle.
                        ++pendingAwaits;
                    }
                    unchecked
                    {
                        _waitCount = (int) pendingAwaits;
                    }
                    Reset();
                    SetupProgress(promisePassThroughs, totalAwaits, completedProgress);

                    while (promisePassThroughs.IsNotEmpty)
                    {
                        var passThrough = promisePassThroughs.Pop();
                        lock (_locker)
                        {
                            _passThroughs.Push(passThrough);
                        }
                        passThrough.SetTargetAndAddToOwner(this);
                        if (_valueOrPrevious != null)
                        {
                            // This was rejected or canceled potentially before all passthroughs were hooked up.
                            // Try to unhook current passthrough (in case of thread race condition), and release all remaining passthroughs.
                            int addCount = passThrough.TryRemoveFromOwner() ? -1 : 0;
                            while (promisePassThroughs.IsNotEmpty)
                            {
                                var p = promisePassThroughs.Pop();
                                p.Owner.MaybeDispose();
                                p.Release2(-2);
                                --addCount;
                            }
                            if (addCount != 0 && Interlocked.Add(ref _waitCount, addCount) == 0)
                            {
                                MaybeDispose();
                            }
                        }
                        passThrough.Release();
                    }
                }

                public override void Handle()
                {
                    IValueContainer valueContainer = (IValueContainer) _valueOrPrevious;
                    _state = valueContainer.GetState();
                    HandleWaiter(valueContainer);
                    HandleProgressListener(_state);

                    if (Interlocked.Decrement(ref _waitCount) == 0)
                    {
                        MaybeDispose();
                    }
                }

                public virtual bool Handle(IValueContainer valueContainer, PromisePassThrough passThrough, int index) // IMultiTreeHandleable.Handle
                {
                    ThrowIfInPool(this);

                    PromiseRef owner = passThrough.Owner;
                    owner._suppressRejection = true;
                    if (owner._state != Promise.State.Resolved) // Rejected/Canceled
                    {
                        if (Interlocked.CompareExchange(ref _valueOrPrevious, valueContainer, null) != null)
                        {
                            if (Interlocked.Decrement(ref _waitCount) == 0)
                            {
                                MaybeDispose();
                            }
                            return false;
                        }
                        valueContainer.Retain();

                        // Try to unhook and release all passthroughs.
                        ValueLinkedStack<PromisePassThrough> removedPassThroughs = new ValueLinkedStack<PromisePassThrough>();
                        ValueLinkedStack<PromisePassThrough> unRemovedPassThroughs = new ValueLinkedStack<PromisePassThrough>();
                        lock (_locker)
                        {
                            while (_passThroughs.IsNotEmpty)
                            {
                                var p = _passThroughs.Pop();
                                if (p.TryRemoveFromOwner())
                                {
                                    removedPassThroughs.Push(p);
                                }
                                else
                                {
                                    unRemovedPassThroughs.Push(p);
                                }
                            }
                            _passThroughs = unRemovedPassThroughs;
                        }

                        int addWaitCount = -1;
                        while (removedPassThroughs.IsNotEmpty)
                        {
                            removedPassThroughs.Pop().Release();
                            --addWaitCount;
                        }
                        Interlocked.Add(ref _waitCount, addWaitCount);
                        return true;
                    }
                    else // Resolved
                    {
                        int remaining = Interlocked.Decrement(ref _waitCount);
                        if (remaining == 1)
                        {
                            return Interlocked.CompareExchange(ref _valueOrPrevious, ResolveContainerVoid.GetOrCreate(), null) == null;
                        }
                        else if (remaining == 0)
                        {
                            MaybeDispose();
                        }
                        else
                        {
                            IncrementProgress(passThrough);
                        }
                    }
                    return false;
                }

                partial void IncrementProgress(PromisePassThrough passThrough);
                partial void SetupProgress(ValueLinkedStack<PromisePassThrough> promisePassThroughs, uint totalAwaits, ulong completedProgress);

                private sealed class MergePromiseT<T> : MergePromise, IMultiTreeHandleable
                {
                    private struct CreatorT : ICreator<MergePromiseT<T>>
                    {
                        [MethodImpl(InlineOption)]
                        public MergePromiseT<T> Create()
                        {
                            return new MergePromiseT<T>();
                        }
                    }

                    private Action<IValueContainer, ResolveContainer<T>, int> _onPromiseResolved;
                    private ResolveContainer<T> _valueContainer;

                    private MergePromiseT() { }

                    protected override void Dispose()
                    {
                        SuperDispose();
                        _onPromiseResolved = null;
                        if (_valueContainer != null)
                        {
                            _valueContainer.Release();
                            _valueContainer = null;
                        }
                        // Release all passthroughs.
                        while (_passThroughs.IsNotEmpty)
                        {
                            _passThroughs.Pop().Release();
                        }
                        ObjectPool<ITreeHandleable>.MaybeRepool(this);
                    }

                    public static MergePromiseT<T> GetOrCreate(ref T value, Action<IValueContainer, ResolveContainer<T>, int> onPromiseResolved)
                    {
                        var promise = ObjectPool<ITreeHandleable>.GetOrCreate<MergePromiseT<T>, CreatorT>();
                        promise._onPromiseResolved = onPromiseResolved;
                        promise._valueContainer = ResolveContainer<T>.GetOrCreate(ref value, 1);
                        return promise;
                    }

                    public override bool Handle(IValueContainer valueContainer, PromisePassThrough passThrough, int index)
                    {
                        ThrowIfInPool(this);

                        PromiseRef owner = passThrough.Owner;
                        owner._suppressRejection = true;
                        if (owner._state != Promise.State.Resolved) // Rejected/Canceled
                        {
                            if (Interlocked.CompareExchange(ref _valueOrPrevious, valueContainer, null) != null)
                            {
                                if (Interlocked.Decrement(ref _waitCount) == 0)
                                {
                                    MaybeDispose();
                                }
                                return false;
                            }
                            valueContainer.Retain();

                            // Try to unhook and release all passthroughs.
                            ValueLinkedStack<PromisePassThrough> removedPassThroughs = new ValueLinkedStack<PromisePassThrough>();
                            ValueLinkedStack<PromisePassThrough> unRemovedPassThroughs = new ValueLinkedStack<PromisePassThrough>();
                            lock (_locker)
                            {
                                while (_passThroughs.IsNotEmpty)
                                {
                                    var p = _passThroughs.Pop();
                                    if (p.TryRemoveFromOwner())
                                    {
                                        removedPassThroughs.Push(p);
                                    }
                                    else
                                    {
                                        unRemovedPassThroughs.Push(p);
                                    }
                                }
                                _passThroughs = unRemovedPassThroughs;
                            }

                            int addWaitCount = -1;
                            while (removedPassThroughs.IsNotEmpty)
                            {
                                removedPassThroughs.Pop().Release();
                                --addWaitCount;
                            }
                            Interlocked.Add(ref _waitCount, addWaitCount);
                            return true;
                        }
                        else // Resolved
                        {
                            _onPromiseResolved.Invoke(valueContainer, _valueContainer, index);
                            int remaining = Interlocked.Decrement(ref _waitCount);
                            if (remaining == 1)
                            {
                                if (Interlocked.CompareExchange(ref _valueOrPrevious, _valueContainer, null) == null)
                                {
                                    // Only nullify if all promises resolved, otherwise we let Dispose release it.
                                    _valueContainer = null;
                                    return true;
                                }
                            }
                            else if (remaining == 0)
                            {
                                MaybeDispose();
                            }
                            else
                            {
                                IncrementProgress(passThrough);
                            }
                        }
                        return false;
                    }
                }
            }

#if PROMISE_PROGRESS
            partial class MergePromise : IProgressInvokable
            {
                IProgressInvokable ILinked<IProgressInvokable>.Next { get; set; }

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

                protected override bool AddProgressListenerAndContinueLoop(IProgressListener progressListener)
                {
                    ThrowIfInPool(this);
                    _progressListener = progressListener;
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
                    // TODO: thread synchronization.
                    _unscaledProgress.Increment(amount);
                    if ((InterlockedSetProgressFlags(ProgressFlags.InProgressQueue) & ProgressFlags.InProgressQueue) != 0) // Was not already in progress queue?
                    {
                        InterlockedRetainDisregardId();
                        AddToFrontOfProgressQueue(this);
                    }
                }

                void IProgressInvokable.Invoke()
                {
                    var progress = CurrentProgress();
                    InterlockedUnsetProgressFlags(ProgressFlags.InProgressQueue);
                    if ((InterlockedSetProgressFlags(ProgressFlags.SetProgressLocked) & ProgressFlags.SetProgressLocked) == 0)
                    {
                        IProgressListener progressListener = _progressListener;
                        if (progressListener != null)
                        {
                            progressListener.SetProgress(this, progress);
                        }
                        InterlockedUnsetProgressFlags(ProgressFlags.SetProgressLocked);
                    }
                    MaybeDispose();
                }
            }
#endif
        }
    }
}