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

#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable CS0420 // A reference to a volatile field will not be treated as volatile

using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Proto.Promises
{
    partial class Internal
    {
        partial class PromiseRef
        {
#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal partial class MergePromise : PromiseSingleAwaitWithProgress, IMultiTreeHandleable
            {
                private MergePromise() { }

                protected override void Dispose()
                {
                    base.Dispose();
#if PROMISE_DEBUG
                    lock (_locker)
                    {
                        while (_passThroughs.IsNotEmpty)
                        {
                            _passThroughs.Pop().Release();
                        }
                    }
#endif
                    ObjectPool<ITreeHandleable>.MaybeRepool(this);
                }

                [MethodImpl(InlineOption)]
                private void SuperDispose()
                {
                    base.Dispose();
                }

                internal static MergePromise GetOrCreate(ValueLinkedStack<PromisePassThrough> promisePassThroughs, uint pendingAwaits, uint totalAwaits, ulong completedProgress)
                {
                    var promise = ObjectPool<ITreeHandleable>.TryTake<MergePromise>()
                        ?? new MergePromise();
                    promise.Setup(promisePassThroughs, pendingAwaits, totalAwaits, completedProgress);
                    return promise;
                }

                internal static MergePromise GetOrCreate<T>(
                    ValueLinkedStack<PromisePassThrough> promisePassThroughs,

#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    T value,
                    Action<IValueContainer, ResolveContainer<T>, int> onPromiseResolved,
                    uint pendingAwaits,
                    uint totalAwaits,
                    ulong completedProgress)
                {
                    var promise = MergePromiseT<T>.GetOrCreate(value, onPromiseResolved);
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
#if PROMISE_DEBUG
                        passThrough.Retain();
                        lock (_locker)
                        {
                            _passThroughs.Push(passThrough);
                        }
#endif
                        passThrough.SetTargetAndAddToOwner(this);
                        if (_valueOrPrevious != null)
                        {
                            // This was rejected or canceled potentially before all passthroughs were hooked up. Release all remaining passthroughs.
                            int addCount = 0;
                            while (promisePassThroughs.IsNotEmpty)
                            {
                                var p = promisePassThroughs.Pop();
                                p.Owner.MaybeDispose();
                                p.Release();
                                --addCount;
                            }
                            if (addCount != 0 && Interlocked.Add(ref _waitCount, addCount) == 0)
                            {
                                MaybeDispose();
                            }
                        }
                    }
                }

                public override void Handle(ref ExecutionScheduler executionScheduler)
                {
                    IValueContainer valueContainer = (IValueContainer) _valueOrPrevious;
                    Promise.State state = valueContainer.GetState();
                    State = state;
                    HandleWaiter(valueContainer, ref executionScheduler);
                    HandleProgressListener(state, ref executionScheduler);

                    if (Interlocked.Decrement(ref _waitCount) == 0)
                    {
                        MaybeDispose();
                    }
                }

                public virtual void Handle(PromiseRef owner, IValueContainer valueContainer, PromisePassThrough passThrough, ref ExecutionScheduler executionScheduler) // IMultiTreeHandleable.Handle
                {
                    // Retain while handling, then release when complete for thread safety.
                    InterlockedRetainDisregardId();

                    owner.SuppressRejection = true;
                    if (owner.State != Promise.State.Resolved) // Rejected/Canceled
                    {
                        if (Interlocked.CompareExchange(ref _valueOrPrevious, valueContainer, null) != null)
                        {
                            if (Interlocked.Decrement(ref _waitCount) == 0)
                            {
                                _smallFields.InterlockedTryReleaseComplete();
                            }
                        }
                        else
                        {
                            valueContainer.Retain();
                            Interlocked.Decrement(ref _waitCount);
                            executionScheduler.ScheduleSynchronous(this);
                        }
                    }
                    else // Resolved
                    {
                        IncrementProgress(passThrough, ref executionScheduler);
                        int remaining = Interlocked.Decrement(ref _waitCount);
                        if (remaining == 1)
                        {
                            if (Interlocked.CompareExchange(ref _valueOrPrevious, valueContainer, null) == null)
                            {
                                valueContainer.Retain();
                                executionScheduler.ScheduleSynchronous(this);
                            }
                        }
                        else if (remaining == 0)
                        {
                            _smallFields.InterlockedTryReleaseComplete();
                        }
                    }
                    MaybeDispose();
                }

                internal int Depth
                {
#if PROMISE_PROGRESS
                    [MethodImpl(InlineOption)]
                    get { return _maxWaitDepth; }
#else
                    [MethodImpl(InlineOption)]
                    get { return 0; }
#endif
                }

                partial void IncrementProgress(PromisePassThrough passThrough, ref ExecutionScheduler executionScheduler);
                partial void SetupProgress(ValueLinkedStack<PromisePassThrough> promisePassThroughs, uint totalAwaits, ulong completedProgress);

                private sealed class MergePromiseT<T> : MergePromise, IMultiTreeHandleable
                {
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
#if PROMISE_DEBUG
                        lock (_locker)
                        {
                            while (_passThroughs.IsNotEmpty)
                            {
                                _passThroughs.Pop().Release();
                            }
                        }
#endif
                        ObjectPool<ITreeHandleable>.MaybeRepool(this);
                    }

                    internal static MergePromiseT<T> GetOrCreate(
#if CSHARP_7_3_OR_NEWER
                        in
#endif
                        T value, Action<IValueContainer, ResolveContainer<T>, int> onPromiseResolved)
                    {
                        var promise = ObjectPool<ITreeHandleable>.TryTake<MergePromiseT<T>>()
                            ?? new MergePromiseT<T>();
                        promise._onPromiseResolved = onPromiseResolved;
                        promise._valueContainer = ResolveContainer<T>.GetOrCreate(value, 1);
                        return promise;
                    }

                    public override void Handle(PromiseRef owner, IValueContainer valueContainer, PromisePassThrough passThrough, ref ExecutionScheduler executionScheduler)
                    {
                        // Retain while handling, then release when complete for thread safety.
                        InterlockedRetainDisregardId();

                        owner.SuppressRejection = true;
                        if (owner.State != Promise.State.Resolved) // Rejected/Canceled
                        {
                            if (Interlocked.CompareExchange(ref _valueOrPrevious, valueContainer, null) != null)
                            {
                                if (Interlocked.Decrement(ref _waitCount) == 0)
                                {
                                    _smallFields.InterlockedTryReleaseComplete();
                                }
                            }
                            else
                            {
                                valueContainer.Retain();
                                Interlocked.Decrement(ref _waitCount);
                                executionScheduler.ScheduleSynchronous(this);
                            }
                        }
                        else // Resolved
                        {
                            _onPromiseResolved.Invoke(valueContainer, _valueContainer, passThrough.Index);
                            IncrementProgress(passThrough, ref executionScheduler);
                            int remaining = Interlocked.Decrement(ref _waitCount);
                            if (remaining == 1)
                            {
                                if (Interlocked.CompareExchange(ref _valueOrPrevious, _valueContainer, null) == null)
                                {
                                    // Only nullify if all promises resolved, otherwise we let Dispose release it.
                                    _valueContainer = null;
                                    executionScheduler.ScheduleSynchronous(this);
                                }
                            }
                            else if (remaining == 0)
                            {
                                _smallFields.InterlockedTryReleaseComplete();
                            }
                        }
                        MaybeDispose();
                    }
                }
            }

#if PROMISE_PROGRESS
            partial class MergePromise : IProgressInvokable
            {
                internal override void HandleProgressListener(Promise.State state, ref ExecutionScheduler executionScheduler)
                {
                    HandleProgressListener(state, new Fixed32(_maxWaitDepth + 1), ref executionScheduler);
                }

                protected override sealed PromiseRef MaybeAddProgressListenerAndGetPreviousRetained(ref IProgressListener progressListener, ref Fixed32 lastKnownProgress)
                {
                    // Unnecessary to set last known since we know SetInitialProgress will be called on this.
                    ThrowIfInPool(this);
                    progressListener.Retain();
                    _progressListener = progressListener;
                    return null;
                }

                protected override sealed void SetInitialProgress(IProgressListener progressListener, Fixed32 lastKnownProgress, ref ExecutionScheduler executionScheduler)
                {
                    SetInitialProgress(progressListener, CurrentProgress(), new Fixed32(_maxWaitDepth + 1), ref executionScheduler);
                }

                partial void SetupProgress(ValueLinkedStack<PromisePassThrough> promisePassThroughs, uint totalAwaits, ulong completedProgress)
                {
#if PROMISE_DEBUG
                    checked
#endif
                    {
                        _unscaledProgress = new UnsignedFixed64(completedProgress);

                        long expectedProgressCounter = totalAwaits;
                        int maxWaitDepth = 0;
                        foreach (var passThrough in promisePassThroughs)
                        {
                            int waitDepth = passThrough.Depth;
                            expectedProgressCounter += waitDepth;
                            maxWaitDepth = Math.Max(maxWaitDepth, waitDepth);
                        }

                        // Use the longest chain as this depth.
                        _maxWaitDepth = maxWaitDepth;
                        _progressScaler = (double) (_maxWaitDepth + 1) / (double) expectedProgressCounter;
                    }
                }

                partial void IncrementProgress(PromisePassThrough passThrough, ref ExecutionScheduler executionScheduler)
                {
                    IncrementProgress(passThrough.GetProgressDifferenceToCompletion(), ref executionScheduler);
                }

                private Fixed32 CurrentProgress()
                {
                    ThrowIfInPool(this);
                    return new Fixed32(_unscaledProgress.ToDouble() * _progressScaler);
                }

                void IMultiTreeHandleable.IncrementProgress(uint amount, Fixed32 senderAmount, Fixed32 ownerAmount, ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    IncrementProgress(amount, ref executionScheduler);
                }

                private void IncrementProgress(uint amount, ref ExecutionScheduler executionScheduler)
                {
                    _unscaledProgress.InterlockedIncrement(amount);
                    if ((_smallFields.InterlockedSetFlags(PromiseFlags.InProgressQueue) & PromiseFlags.InProgressQueue) == 0) // Was not already in progress queue?
                    {
                        InterlockedRetainDisregardId();
                        executionScheduler.ScheduleProgressSynchronous(this);
                    }
                }

                void IProgressInvokable.Invoke(ref ExecutionScheduler executionScheduler)
                {
                    var progress = CurrentProgress();
                    _smallFields.InterlockedUnsetFlags(PromiseFlags.InProgressQueue);
                    ReportProgress(progress, ref executionScheduler);
                    MaybeDispose();
                }
            }
#endif
        }
    }
}