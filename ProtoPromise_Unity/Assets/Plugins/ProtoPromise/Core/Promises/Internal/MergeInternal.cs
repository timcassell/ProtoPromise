#if UNITY_5_5 || NET_2_0 || NET_2_0_SUBSET
#define NET_LEGACY
#endif

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
#if PROMISE_DEBUG
                new protected void Dispose()
                {
                    base.Dispose();
                    lock (_locker)
                    {
                        while (_passThroughs.IsNotEmpty)
                        {
                            _passThroughs.Pop().Release();
                        }
                    }
                }
#endif
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal partial class MergePromise : MultiHandleablePromiseBase
            {
                private MergePromise() { }

                protected override void MaybeDispose()
                {
                    MaybeDisposeNonVirt();
                }

                private void MaybeDisposeNonVirt()
                {
                    if (InterlockedAddWithOverflowCheck(ref _retainCounter, -1, 0) == 0)
                    {
                        Dispose();
                        ObjectPool<HandleablePromiseBase>.MaybeRepool(this);
                    }
                }

                internal static MergePromise GetOrCreate(ValueLinkedStack<PromisePassThrough> promisePassThroughs, int pendingAwaits, ulong completedProgress, ulong totalProgress, ushort depth)
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
                    int pendingAwaits, ulong completedProgress, ulong totalProgress, ushort depth)
                {
                    var promise = MergePromiseT<T>.GetOrCreate(value, onPromiseResolved);
                    promise.Setup(promisePassThroughs, pendingAwaits, completedProgress, totalProgress, depth);
                    return promise;
                }

                private void Setup(ValueLinkedStack<PromisePassThrough> promisePassThroughs, int pendingAwaits, ulong completedProgress, ulong totalProgress, ushort depth)
                {
                    _waitCount = pendingAwaits;
                    unchecked
                    {
                        _retainCounter = pendingAwaits + 1;
                    }
                    Reset(depth);
                    SetupProgress(completedProgress, totalProgress);

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
                        if (_valueContainer != null)
                        {
                            // This was rejected or canceled potentially before all passthroughs were hooked up. Release all remaining passthroughs.
                            while (promisePassThroughs.IsNotEmpty)
                            {
                                var p = promisePassThroughs.Pop();
                                p.Owner.MaybeDispose();
                                p.Release();
                                MaybeDispose();
                            }
                        }
                    }
                }

                internal override void Handle(PromisePassThrough passThrough, out HandleablePromiseBase nextHandler, ref ExecutionScheduler executionScheduler)
                {
                    var handler = passThrough.Owner;
                    var valueContainer = handler._valueContainer;
                    nextHandler = null;
                    var state = handler.State;
                    if (state != Promise.State.Resolved) // Rejected/Canceled
                    {
                        if (Interlocked.CompareExchange(ref _valueContainer, valueContainer, null) == null)
                        {
                            handler.SuppressRejection = true;
                            SetResultAndMaybeHandle(valueContainer.Clone(), state, out nextHandler);
                        }
                        InterlockedAddWithOverflowCheck(ref _waitCount, -1, 0);
                    }
                    else // Resolved
                    {
                        IncrementProgress(passThrough, ref executionScheduler);
                        if (InterlockedAddWithOverflowCheck(ref _waitCount, -1, 0) == 0
                            && Interlocked.CompareExchange(ref _valueContainer, valueContainer, null) == null)
                        {
                            SetResultAndMaybeHandle(valueContainer.Clone(), state, out nextHandler);
                        }
                    }
                    MaybeDisposeNonVirt();
                }

                partial void IncrementProgress(PromisePassThrough passThrough, ref ExecutionScheduler executionScheduler);
                partial void SetupProgress(ulong completedProgress, ulong totalProgress);

                private sealed class MergePromiseT<T> : MergePromise
                {
                    private Action<ValueContainer, ResolveContainer<T>, int> _onPromiseResolved;
                    private ResolveContainer<T> _resolveContainer;

                    private MergePromiseT() { }

                    protected override void MaybeDispose()
                    {
                        if (InterlockedAddWithOverflowCheck(ref _retainCounter, -1, 0) == 0)
                        {
                            Dispose();
                            _onPromiseResolved = null;
                            if (_resolveContainer != null)
                            {
                                _resolveContainer.DisposeAndMaybeAddToUnhandledStack(false);
                                _resolveContainer = null;
                            }
                            ObjectPool<HandleablePromiseBase>.MaybeRepool(this);
                        }
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
                        promise._resolveContainer = ResolveContainer<T>.GetOrCreate(value);
                        return promise;
                    }

                    internal override void Handle(PromisePassThrough passThrough, out HandleablePromiseBase nextHandler, ref ExecutionScheduler executionScheduler)
                    {
                        var handler = passThrough.Owner;
                        var valueContainer = handler._valueContainer;
                        nextHandler = null;
                        var state = handler.State;
                        if (state != Promise.State.Resolved) // Rejected/Canceled
                        {
                            if (Interlocked.CompareExchange(ref _valueContainer, valueContainer, null) == null)
                            {
                                handler.SuppressRejection = true;
                                SetResultAndMaybeHandle(valueContainer.Clone(), state, out nextHandler);
                            }
                            InterlockedAddWithOverflowCheck(ref _waitCount, -1, 0);
                        }
                        else // Resolved
                        {
                            IncrementProgress(passThrough, ref executionScheduler);
                            _onPromiseResolved.Invoke(valueContainer, _resolveContainer, passThrough.Index);
                            if (InterlockedAddWithOverflowCheck(ref _waitCount, -1, 0) == 0
                                && Interlocked.CompareExchange(ref _valueContainer, valueContainer, null) == null)
                            {
                                SetResultAndMaybeHandle(valueContainer.Clone(), state, out nextHandler);
                            }
                        }
                        MaybeDispose();
                    }
                }
            }

#if PROMISE_PROGRESS
            partial class MergePromise
            {
                partial void SetupProgress(ulong completedProgress, ulong totalProgress)
                {
                    _unscaledProgress = new UnsignedFixed64(completedProgress);
                    _progressScaler = (double) (Depth + 1u) / (double) totalProgress;
                }

                partial void IncrementProgress(PromisePassThrough passThrough, ref ExecutionScheduler executionScheduler)
                {
                    var wasReportingPriority = Fixed32.ts_reportingPriority;
                    Fixed32.ts_reportingPriority = true;

                    uint dif = passThrough.GetProgressDifferenceToCompletion();
                    var progress = IncrementProgress(dif);
                    ReportProgress(progress, Depth, ref executionScheduler);
                    
                    Fixed32.ts_reportingPriority = wasReportingPriority;
                }

                private Fixed32 NormalizeProgress(UnsignedFixed64 unscaledProgress)
                {
                    ThrowIfInPool(this);
                    var scaledProgress = Fixed32.GetScaled(unscaledProgress, _progressScaler);
                    _smallFields._currentProgress = scaledProgress;
                    return scaledProgress;
                }

                internal override PromiseSingleAwait IncrementProgress(long amount, ref Fixed32 progress, ushort depth)
                {
                    ThrowIfInPool(this);
                    // This essentially acts as a pass-through to normalize the progress.
                    progress = IncrementProgress(amount);
                    return this;
                }

                private Fixed32 IncrementProgress(long amount)
                {
                    var unscaledProgress = _unscaledProgress.InterlockedIncrement(amount);
                    return NormalizeProgress(unscaledProgress);
                }
            }
#endif
        }
    }
}