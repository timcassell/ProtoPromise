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
            partial class MultiHandleablePromiseBase
            {
                protected void Handle(ref int _waitCount, ref PromiseRef handler, out HandleablePromiseBase nextHandler, ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    var valueContainer = (ValueContainer) _valueOrPrevious;
                    var state = handler.State;
                    State = state;
#if !CSHARP_7_3_OR_NEWER // Interlocked.Exchange doesn't seem to work properly in Unity's old runtime. I'm not sure why, but we need a lock here to pass multi-threaded tests.
                    lock (this)
#endif
                    {
                        Thread.MemoryBarrier(); // Make sure previous writes are done before swapping _waiter.
                        nextHandler = Interlocked.Exchange(ref _waiter, null);
                    }
                    HandleProgressListener(state, Depth, ref executionScheduler);
                    // handler will be disposed higher in the call stack. We only set it if this is released completely.
                    if (InterlockedAddWithOverflowCheck(ref _waitCount, -1, 0) == 0)
                    {
                        handler.MaybeDispose();
                        handler = this;
                    }
                }
            }

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

                internal static MergePromise GetOrCreate(ValueLinkedStack<PromisePassThrough> promisePassThroughs, uint pendingAwaits, ulong completedProgress, ulong totalProgress, ushort depth)
                {
                    var promise = ObjectPool<HandleablePromiseBase>.TryTake<MergePromise>()
                        ?? new MergePromise();
                    promise.Setup(promisePassThroughs, pendingAwaits, completedProgress, totalProgress, depth);
                    return promise;
                }

                internal static MergePromise GetOrCreate<T>(
                    ValueLinkedStack<PromisePassThrough> promisePassThroughs,

#if CSHARP_7_3_OR_NEWER
                    in
#endif
                    T value,
                    Action<ValueContainer, ResolveContainer<T>, int> onPromiseResolved,
                    uint pendingAwaits, ulong completedProgress, ulong totalProgress, ushort depth)
                {
                    var promise = MergePromiseT<T>.GetOrCreate(value, onPromiseResolved);
                    promise.Setup(promisePassThroughs, pendingAwaits, completedProgress, totalProgress, depth);
                    return promise;
                }

                private void Setup(ValueLinkedStack<PromisePassThrough> promisePassThroughs, uint pendingAwaits, ulong completedProgress, ulong totalProgress, ushort depth)
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
                    Reset(depth);
                    SetupProgress(promisePassThroughs, completedProgress, totalProgress);

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
                            if (addCount != 0 && InterlockedAddWithOverflowCheck(ref _waitCount, addCount, 0) == 0)
                            {
                                MaybeDispose();
                            }
                        }
                    }
                }

                internal override void Handle(ref PromiseRef handler, ValueContainer valueContainer, PromisePassThrough passThrough, out HandleablePromiseBase nextHandler, ref ExecutionScheduler executionScheduler)
                {
                    // Retain while handling, then release when complete for thread safety.
                    InterlockedRetainDisregardId();
                    nextHandler = null;

                    if (handler.State != Promise.State.Resolved) // Rejected/Canceled
                    {
                        if (Interlocked.CompareExchange(ref _valueOrPrevious, valueContainer, null) == null)
                        {
                            valueContainer.Retain();
                            Handle(ref _waitCount, ref handler, out nextHandler, ref executionScheduler);
                        }
                        if (InterlockedAddWithOverflowCheck(ref _waitCount, -1, 0) == 0)
                        {
                            _smallFields.InterlockedTryReleaseComplete();
                        }
                    }
                    else // Resolved
                    {
                        IncrementProgress(passThrough, ref executionScheduler);
                        int remaining = InterlockedAddWithOverflowCheck(ref _waitCount, -1, 0);
                        if (remaining == 1)
                        {
                            if (Interlocked.CompareExchange(ref _valueOrPrevious, valueContainer, null) == null)
                            {
                                valueContainer.Retain();
                                Handle(ref _waitCount, ref handler, out nextHandler, ref executionScheduler);
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
                partial void SetupProgress(ValueLinkedStack<PromisePassThrough> promisePassThroughs, ulong completedProgress, ulong totalProgress);

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

                    internal override void Handle(ref PromiseRef handler, ValueContainer valueContainer, PromisePassThrough passThrough, out HandleablePromiseBase nextHandler, ref ExecutionScheduler executionScheduler)
                    {
                        // Retain while handling, then release when complete for thread safety.
                        InterlockedRetainDisregardId();
                        nextHandler = null;

                        if (handler.State != Promise.State.Resolved) // Rejected/Canceled
                        {
                            if (Interlocked.CompareExchange(ref _valueOrPrevious, valueContainer, null) == null)
                            {
                                valueContainer.Retain();
                                Handle(ref _waitCount, ref handler, out nextHandler, ref executionScheduler);
                            }
                            if (InterlockedAddWithOverflowCheck(ref _waitCount, -1, 0) == 0)
                            {
                                _smallFields.InterlockedTryReleaseComplete();
                            }
                        }
                        else // Resolved
                        {
                            _onPromiseResolved.Invoke(valueContainer, _valueContainer, passThrough.Index);
                            IncrementProgress(passThrough, ref executionScheduler);
                            int remaining = InterlockedAddWithOverflowCheck(ref _waitCount, -1, 0);
                            if (remaining == 1)
                            {
                                if (Interlocked.CompareExchange(ref _valueOrPrevious, _valueContainer, null) == null)
                                {
                                    // Only nullify if all promises resolved, otherwise we let Dispose release it.
                                    _valueContainer = null;
                                    Handle(ref _waitCount, ref handler, out nextHandler, ref executionScheduler);
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
            partial class MergePromise
            {
                protected override sealed PromiseRef MaybeAddProgressListenerAndGetPreviousRetained(ref IProgressListener progressListener, ref Fixed32 lastKnownProgress)
                {
                    // Unnecessary to set last known since we know SetInitialProgress will be called on this.
                    ThrowIfInPool(this);
                    progressListener.Retain();
                    _progressListener = progressListener;
                    return null;
                }

                protected override sealed void SetInitialProgress(IProgressListener progressListener, ref Fixed32 progress, out PromiseSingleAwaitWithProgress nextRef, ref ExecutionScheduler executionScheduler)
                {
                    progress = NormalizeProgress(_unscaledProgress);
                    SetInitialProgress(progressListener, ref progress, Fixed32.FromWholePlusOne(Depth), out nextRef, ref executionScheduler);
                }

                partial void SetupProgress(ValueLinkedStack<PromisePassThrough> promisePassThroughs, ulong completedProgress, ulong totalProgress)
                {
                    _unscaledProgress = new UnsignedFixed64(completedProgress);
                    _progressScaler = (double) (Depth + 1u) / (double) totalProgress;
                }

                partial void IncrementProgress(PromisePassThrough passThrough, ref ExecutionScheduler executionScheduler)
                {
                    Fixed32 progressFlags;
                    uint dif = passThrough.GetProgressDifferenceToCompletion(out progressFlags);
                    var progress = IncrementProgress(dif, progressFlags);
                    ReportProgress(progress, ref executionScheduler);
                }

                private Fixed32 NormalizeProgress(UnsignedFixed64 unscaledProgress)
                {
                    ThrowIfInPool(this);
                    return Fixed32.GetScaled(unscaledProgress, _progressScaler);
                }

                internal override void IncrementProgress(uint amount, ref Fixed32 progress, ushort depth, out PromiseSingleAwaitWithProgress nextRef)
                {
                    ThrowIfInPool(this);
                    // This essentially acts as a pass-through to normalize the progress.
                    nextRef = this;
                    progress = IncrementProgress(amount, progress);
                }

                private Fixed32 IncrementProgress(uint amount, Fixed32 otherFlags)
                {
                    var unscaledProgress = _unscaledProgress.InterlockedIncrement(amount, otherFlags);
                    return NormalizeProgress(unscaledProgress);
                }
            }
#endif
        }
    }
}