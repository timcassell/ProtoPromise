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

#pragma warning disable 0420 // A reference to a volatile field will not be treated as volatile

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
            internal partial class MergePromise : MultiHandleablePromiseBase
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
                    ObjectPool<HandleablePromiseBase>.MaybeRepool(this);
                }

                [MethodImpl(InlineOption)]
                private void SuperDispose()
                {
                    base.Dispose();
                }

                internal static MergePromise GetOrCreate(ValueLinkedStack<PromisePassThrough> promisePassThroughs, uint pendingAwaits, uint totalAwaits, ulong completedProgress)
                {
                    var promise = ObjectPool<HandleablePromiseBase>.TryTake<MergePromise>()
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
                    Action<ValueContainer, ResolveContainer<T>, int> onPromiseResolved,
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
                    ushort depth = promisePassThroughs.Peek().Depth;
                    SetupProgress(promisePassThroughs, totalAwaits, completedProgress, ref depth);
                    Reset(depth);

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

                internal override void Handle(ref ExecutionScheduler executionScheduler)
                {
                    ValueContainer valueContainer = (ValueContainer) _valueOrPrevious;
                    Promise.State state = valueContainer.GetState();
                    State = state;
                    HandleWaiter(valueContainer, ref executionScheduler);
                    HandleProgressListener(state, ref executionScheduler);

                    if (Interlocked.Decrement(ref _waitCount) == 0)
                    {
                        MaybeDispose();
                    }
                }

                internal override void Handle(PromiseRef owner, ValueContainer valueContainer, PromisePassThrough passThrough, ref ExecutionScheduler executionScheduler)
                {
                    // Retain while handling, then release when complete for thread safety.
                    InterlockedRetainDisregardId();

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

                partial void IncrementProgress(PromisePassThrough passThrough, ref ExecutionScheduler executionScheduler);
                partial void SetupProgress(ValueLinkedStack<PromisePassThrough> promisePassThroughs, uint totalAwaits, ulong completedProgress, ref ushort depth);

                private sealed class MergePromiseT<T> : MergePromise
                {
                    private Action<ValueContainer, ResolveContainer<T>, int> _onPromiseResolved;
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
                        ObjectPool<HandleablePromiseBase>.MaybeRepool(this);
                    }

                    internal static MergePromiseT<T> GetOrCreate(
#if CSHARP_7_3_OR_NEWER
                        in
#endif
                        T value, Action<ValueContainer, ResolveContainer<T>, int> onPromiseResolved)
                    {
                        var promise = ObjectPool<HandleablePromiseBase>.TryTake<MergePromiseT<T>>()
                            ?? new MergePromiseT<T>();
                        promise._onPromiseResolved = onPromiseResolved;
                        promise._valueContainer = ResolveContainer<T>.GetOrCreate(value);
                        return promise;
                    }

                    internal override void Handle(PromiseRef owner, ValueContainer valueContainer, PromisePassThrough passThrough, ref ExecutionScheduler executionScheduler)
                    {
                        // Retain while handling, then release when complete for thread safety.
                        InterlockedRetainDisregardId();

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
                    HandleProgressListener(state, Fixed32.FromWholePlusOne(Depth), ref executionScheduler);
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
                    SetInitialProgress(progressListener, CurrentProgress(), Fixed32.FromWholePlusOne(Depth), ref executionScheduler);
                }

                partial void SetupProgress(ValueLinkedStack<PromisePassThrough> promisePassThroughs, uint totalAwaits, ulong completedProgress, ref ushort depth)
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
                        depth = (ushort) maxWaitDepth;
                        _progressScaler = (double) (maxWaitDepth + 1d) / (double) expectedProgressCounter;
                    }
                }

                partial void IncrementProgress(PromisePassThrough passThrough, ref ExecutionScheduler executionScheduler)
                {
                    Fixed32 progressFlags;
                    uint dif = passThrough.GetProgressDifferenceToCompletion(out progressFlags);
                    IncrementProgress(dif, progressFlags, ref executionScheduler);
                }

                private Fixed32 CurrentProgress()
                {
                    ThrowIfInPool(this);
                    return Fixed32.GetScaled(_unscaledProgress, _progressScaler);
                }

                internal override void IncrementProgress(uint amount, Fixed32 senderAmount, Fixed32 ownerAmount, ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    IncrementProgress(amount, senderAmount, ref executionScheduler);
                }

                private void IncrementProgress(uint amount, Fixed32 otherFlags, ref ExecutionScheduler executionScheduler)
                {
                    _unscaledProgress.InterlockedIncrement(amount, otherFlags);
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