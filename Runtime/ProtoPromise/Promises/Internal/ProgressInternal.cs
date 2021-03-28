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

#pragma warning disable IDE0018 // Inline variable declaration
#pragma warning disable IDE0034 // Simplify 'default' expression

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Proto.Utils;

namespace Proto.Promises
{
    partial class Internal
    {
        partial class PromiseRef
        {
#if PROMISE_DEBUG || PROMISE_PROGRESS
            [ThreadStatic]
            private static Stack<PromisePassThrough> _passthroughsForIterativeAlgorithm;
            private static Stack<PromisePassThrough> PassthroughsForIterativeAlgorithm
            {
                get
                {
                    if (_passthroughsForIterativeAlgorithm == null)
                    {
                        _passthroughsForIterativeAlgorithm = new Stack<PromisePassThrough>();
                    }
                    return _passthroughsForIterativeAlgorithm;
                }
            }

            protected virtual void BorrowPassthroughs(Stack<PromisePassThrough> borrower) { }

            private static void ExchangePassthroughs(ref ValueLinkedStack<PromisePassThrough> from, Stack<PromisePassThrough> to, object locker)
            {
                // TODO: always subscribe multi-promises to progress when they are created so that this won't be necessary for an iterative algorithm (for multi-threading).

                lock (locker)
                {
                    foreach (var passthrough in from)
                    {
                        if (passthrough.Owner._state == Promise.State.Pending)
                        {
                            passthrough.Retain();
                            to.Push(passthrough);
                        }
                    }
                }

                //// Remove this.passThroughs before adding to passThroughs. They are re-added by the caller.
                //while (from.IsNotEmpty)
                //{
                //    var passThrough = from.Pop();
                //    if (passThrough.Owner != null && passThrough.Owner._state == Promise.State.Pending)
                //    {
                //        to.Push(passThrough);
                //    }
                //}
            }

            partial class MergePromise
            {
                protected override void BorrowPassthroughs(Stack<PromisePassThrough> borrower)
                {
                    ThrowIfInPool(this);
                    ExchangePassthroughs(ref _passThroughs, borrower, _locker);
                }
            }

            partial class RacePromise
            {
                protected override void BorrowPassthroughs(Stack<PromisePassThrough> borrower)
                {
                    ThrowIfInPool(this);
                    ExchangePassthroughs(ref _passThroughs, borrower, _locker);
                }
            }

            partial class FirstPromise
            {
                protected override void BorrowPassthroughs(Stack<PromisePassThrough> borrower)
                {
                    ThrowIfInPool(this);
                    ExchangePassthroughs(ref _passThroughs, borrower, _locker);
                }
            }
#endif

            // Calls to these get compiled away when PROGRESS is undefined.
            partial void SetDepth(PromiseRef previous);
            partial void SetDepth();
            partial void ResetDepth();

            partial void ResolveProgressListeners();
            partial void CancelProgressListeners(object previous);

#if PROMISE_PROGRESS
            private ValueLinkedStackZeroGC<IProgressListener> _progressListeners;
            private UnsignedFixed32 _waitDepthAndProgress;

            private uint NextWholeProgress { get { return _waitDepthAndProgress.WholePart + 1u; } }

            partial void ResolveProgressListeners()
            {
                UnsignedFixed32 progress = _waitDepthAndProgress.GetIncrementedWholeTruncated();
                while (_progressListeners.IsNotEmpty)
                {
                    _progressListeners.Pop().ResolveOrSetProgress(this, progress);
                }
            }

            partial void CancelProgressListeners(object previous)
            {
                // TODO: concurrent cancelation of promise chain in multiple places, re-add progress retains.
                // TODO: this algorithm is O(n^3), refactor progress to reduce runtime costs of cancelations.
                UnsignedFixed32 progress = _waitDepthAndProgress.GetIncrementedWholeTruncated();
                while (_progressListeners.IsNotEmpty)
                {
                    var listener = _progressListeners.Pop();
#if CSHARP_7_OR_LATER
                    object prev = previous;
                    while (prev is PromiseRef promise)
                    {
                        promise._progressListeners.TryRemove(listener);
                        prev = promise._valueOrPrevious;
                    }
#else
                    PromiseRef promise = previous as PromiseRef;
                    while (promise != null)
                    {
                        promise._progressListeners.TryRemove(listener);
                        promise = promise._valueOrPrevious as PromiseRef;
                    }
#endif
                    listener.CancelOrSetProgress(this, progress);
                }
            }

            partial void ResetDepth()
            {
                _waitDepthAndProgress = default(UnsignedFixed32);
            }

            partial void SetDepth(PromiseRef previous)
            {
                SetDepth(previous._waitDepthAndProgress);
            }

            partial void SetDepth()
            {
                SetDepth(default(UnsignedFixed32));
            }

            protected virtual void SetDepth(UnsignedFixed32 previousDepth)
            {
                _waitDepthAndProgress = previousDepth;
            }

            protected virtual bool SubscribeProgressAndContinueLoop(ref IProgressListener progressListener, out PromiseRef previous)
            {
                ThrowIfInPool(this);
                _progressListeners.Push(progressListener);
                return (previous = _valueOrPrevious as PromiseRef) != null;
            }

            protected virtual bool SubscribeProgressIfWaiterAndContinueLoop(ref IProgressListener progressListener, out PromiseRef previous, Stack<PromisePassThrough> passThroughs)
            {
                return SubscribeProgressAndContinueLoop(ref progressListener, out previous);
                //ThrowIfTracked(this);
                //return (previous = _valueOrPrevious as PromiseRef) != null;
            }

            private static void SubscribeListenerToTree(PromiseRef _ref, IProgressListener progressListener)
            {
                // Directly add to listeners for this promise.
                // Sets promise to the one this is waiting on. Returns false if not waiting on another promise.
                PromiseRef promise;
                if (_ref.SubscribeProgressAndContinueLoop(ref progressListener, out promise))
                {
                    SubscribeProgressToBranchesAndRoots(promise, progressListener);
                }
                else
                {
                    // _ref is the root of the promise tree.
                    progressListener.SetInitialProgress(_ref._waitDepthAndProgress);
                }
            }

            private static void SubscribeProgressToBranchesAndRoots(PromiseRef promise, IProgressListener progressListener)
            {
                // This allows us to subscribe progress to AllPromises and RacePromises iteratively instead of recursively
                Stack<PromisePassThrough> passThroughs = PassthroughsForIterativeAlgorithm;

            Repeat:
                SubscribeProgressToChain(promise, progressListener, passThroughs);

                if (passThroughs.Count > 0)
                {
                    var passThrough = passThroughs.Pop();
                    promise = passThrough.Owner;
                    progressListener = passThrough;
                    goto Repeat;
                }
            }

            private static void SubscribeProgressToChain(PromiseRef promise, IProgressListener progressListener, Stack<PromisePassThrough> passThroughs)
            {
                PromiseRef next;
                // If the promise is not waiting on another promise (is the root), it sets next to null, does not add the listener, and returns false.
                // If the promise is waiting on another promise that is not its previous, it adds the listener, transforms progresslistener, sets next to the one it's waiting on, and returns true.
                // Otherwise, it sets next to its previous, adds the listener only if it is a WaitPromise, and returns true.
                while (promise.SubscribeProgressIfWaiterAndContinueLoop(ref progressListener, out next, passThroughs))
                {
                    promise = next;
                }

                // promise is the root of the promise tree.
                switch (promise._state)
                {
                    case Promise.State.Pending:
                    {
                        progressListener.SetInitialProgress(promise.CurrentProgress());
                        break;
                    }
                    case Promise.State.Resolved:
                    {
                        progressListener.SetInitialProgress(promise._waitDepthAndProgress.GetIncrementedWholeTruncated());
                        break;
                    }
                    default: // Rejected or Canceled:
                    {
                        progressListener.CancelOrSetProgress(promise, promise._waitDepthAndProgress);
                        break;
                    }
                }
            }

            protected virtual UnsignedFixed32 CurrentProgress()
            {
                ThrowIfInPool(this);
                return _waitDepthAndProgress;
            }

            // Handle progress.
            private static ValueLinkedQueueZeroGC<IInvokable> _progressQueue;
            private static bool _runningProgress;

            private static void AddToFrontOfProgressQueue(IInvokable progressListener)
            {
                _progressQueue.Push(progressListener);
            }

            private static void AddToBackOfProgressQueue(IInvokable progressListener)
            {
                _progressQueue.Enqueue(progressListener);
            }

            internal static void InvokeProgressListeners()
            {
                if (_runningProgress)
                {
                    // HandleProgress is running higher in the program stack, so just return.
                    return;
                }

                _runningProgress = true;

                while (_progressQueue.IsNotEmpty)
                {
                    _progressQueue.DequeueRisky().Invoke();
                }

                _progressQueue.ClearLast();
                _runningProgress = false;
            }

            /// <summary>
            /// Max Whole Number: 2^(32-<see cref="Promise.Config.ProgressDecimalBits"/>)
            /// <para/>Precision: 1/(2^<see cref="Promise.Config.ProgressDecimalBits"/>)
            /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal struct UnsignedFixed32
            {
                private const double DecimalMax = 1u << Promise.Config.ProgressDecimalBits;
                private const uint DecimalMask = (1u << Promise.Config.ProgressDecimalBits) - 1u;
                private const uint WholeMask = ~DecimalMask;

                private readonly uint _value;

                internal UnsignedFixed32(uint wholePart)
                {
                    _value = wholePart << Promise.Config.ProgressDecimalBits;
                }

                internal UnsignedFixed32(double value)
                {
                    // Don't bother rounding, we don't want to accidentally round to 1.0.
                    _value = (uint) (value * DecimalMax);
                }

                private UnsignedFixed32(uint value, bool _)
                {
                    _value = value;
                }

                internal uint WholePart { get { return _value >> Promise.Config.ProgressDecimalBits; } }
                private double DecimalPart { get { return (double) DecimalPartAsUInt32 / DecimalMax; } }
                private uint DecimalPartAsUInt32 { get { return _value & DecimalMask; } }

                internal uint ToUInt32()
                {
                    return _value;
                }

                internal double ToDouble()
                {
                    return (double) WholePart + DecimalPart;
                }

                internal UnsignedFixed32 WithNewDecimalPart(double decimalPart)
                {
                    uint newDecimalPart = (uint) (decimalPart * DecimalMax);
                    return new UnsignedFixed32((_value & WholeMask) | newDecimalPart, true);
                }

                internal UnsignedFixed32 GetIncrementedWholeTruncated()
                {
#if PROMISE_DEBUG
                    checked
#endif
                    {
                        return new UnsignedFixed32((_value & WholeMask) + (1u << Promise.Config.ProgressDecimalBits), true);
                    }
                }

                public static bool operator >(UnsignedFixed32 a, UnsignedFixed32 b)
                {
                    return a._value > b._value;
                }

                public static bool operator <(UnsignedFixed32 a, UnsignedFixed32 b)
                {
                    return a._value < b._value;
                }
            }

            /// <summary>
            /// Max Whole Number: 2^(64-<see cref="Promise.Config.ProgressDecimalBits"/>)
            /// Precision: 1/(2^<see cref="Promise.Config.ProgressDecimalBits"/>)
            /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal struct UnsignedFixed64 // Simplified compared to UnsignedFixed32 to remove unused functions.
            {
                private const double DecimalMax = 1ul << Promise.Config.ProgressDecimalBits;
                private const ulong DecimalMask = (1ul << Promise.Config.ProgressDecimalBits) - 1ul;

                private ulong _value;

                internal UnsignedFixed64(ulong wholePart)
                {
                    _value = wholePart << Promise.Config.ProgressDecimalBits;
                }

                internal ulong WholePart { get { return _value >> Promise.Config.ProgressDecimalBits; } }
                private double DecimalPart { get { return (double) DecimalPartAsUInt32 / DecimalMax; } }
                private ulong DecimalPartAsUInt32 { get { return _value & DecimalMask; } }

                internal double ToDouble()
                {
                    // TODO: thread synchronization.
                    return (double) WholePart + DecimalPart;
                }

                internal void Increment(uint increment)
                {
                    // TODO: thread synchronization.
                    _value += increment;
                }
            }

            internal interface IInvokable
            {
                void Invoke();
            }

            internal interface IProgressListener
            {
                void SetInitialProgress(UnsignedFixed32 progress);
                void SetProgress(PromiseRef sender, UnsignedFixed32 progress);
                void ResolveOrSetProgress(PromiseRef sender, UnsignedFixed32 progress);
                void CancelOrSetProgress(PromiseRef sender, UnsignedFixed32 progress);
            }

            partial interface IMultiTreeHandleable
            {
                void IncrementProgress(uint increment, UnsignedFixed32 senderAmount, UnsignedFixed32 ownerAmount);
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal sealed class PromiseProgress<TProgress> : PromiseBranch, IProgressListener, IInvokable, ITraceable, ICancelDelegate
                where TProgress : IProgress<float>
            {
                private struct Creator : ICreator<PromiseProgress<TProgress>>
                {
                    [MethodImpl(InlineOption)]
                    public PromiseProgress<TProgress> Create()
                    {
                        return new PromiseProgress<TProgress>();
                    }
                }

#if PROMISE_DEBUG
                CausalityTrace ITraceable.Trace { get; set; }
#endif

                // TODO: thread synchronization.
                private TProgress _progress;
                private PromiseRef _owner;
                private UnsignedFixed32 _current;
                private bool _handling;
                private bool _done;
                private bool _canceled;
                private bool _canceledFromToken;
                private CancelationRegistration _cancelationRegistration;

                private PromiseProgress() { }

                internal static PromiseProgress<TProgress> GetOrCreate(TProgress progress, PromiseRef owner, CancelationToken cancelationToken = default(CancelationToken))
                {
                    var promise = ObjectPool<ITreeHandleable>.GetOrCreate<PromiseProgress<TProgress>, Creator>();
                    promise.Reset();
                    promise._progress = progress;
                    promise._owner = owner;
                    promise._handling = false;
                    promise._done = false;
                    promise._canceled = false;
                    promise._canceledFromToken = false;
                    promise._current = default(UnsignedFixed32);
                    cancelationToken.TryRegisterInternal(promise, out promise._cancelationRegistration);
                    return promise;
                }

                protected override void Dispose()
                {
                    base.Dispose();
                    _owner = null;
                    _cancelationRegistration = default(CancelationRegistration);
                    _progress = default(TProgress);
                    ObjectPool<ITreeHandleable>.MaybeRepool(this);
                }

                private void InvokeAndCatch(float progress)
                {
                    SetCurrentInvoker(this);
                    try
                    {
                        _progress.Report(progress);
                    }
                    catch (Exception e)
                    {
                        AddRejectionToUnhandledStack(e, this);
                    }
                    ClearCurrentInvoker();
                }

                void IInvokable.Invoke()
                {
                    ThrowIfInPool(this);
                    _handling = false;
                    if (_done)
                    {
                        MaybeDispose();
                        return;
                    }
                    if (_canceled | _canceledFromToken)
                    {
                        return;
                    }

                    // Calculate the normalized progress for the depth that the listener was added.
                    // Use double for better precision.
                    double expected = _owner._waitDepthAndProgress.WholePart + 1u;
                    InvokeAndCatch((float) (_current.ToDouble() / expected));
                }

                private void SetProgress(UnsignedFixed32 progress)
                {
                    _current = progress;
                    if (!_handling & !_canceled & !_canceledFromToken)
                    {
                        _handling = true;
                        // This is called by the promise in reverse order that listeners were added, adding to the front reverses that and puts them in proper order.
                        AddToFrontOfProgressQueue(this);
                    }
                }

                void IProgressListener.SetProgress(PromiseRef sender, UnsignedFixed32 progress)
                {
                    ThrowIfInPool(this);
                    SetProgress(progress);
                }

                void IProgressListener.ResolveOrSetProgress(PromiseRef sender, UnsignedFixed32 progress)
                {
                    ThrowIfInPool(this);
                    if (sender == _owner)
                    {
                        // PromiseRef will have already made ready, just set _canceled to prevent progress queue from invoking.
                        _canceled = true;
                    }
                    else
                    {
                        SetProgress(progress);
                    }
                }

                void IProgressListener.SetInitialProgress(UnsignedFixed32 progress)
                {
                    ThrowIfInPool(this);
                    _current = progress;
                    _handling = true;
                    // Always add new listeners to the back.
                    AddToBackOfProgressQueue(this);
                }

                void IProgressListener.CancelOrSetProgress(PromiseRef sender, UnsignedFixed32 progress)
                {
                    ThrowIfInPool(this);
                    if (sender == _owner)
                    {
                        // PromiseRef will have already made ready, just set _canceled to prevent progress queue from invoking.
                        _canceled = true;
                    }
                    else
                    {
                        _current = progress;
                    }
                }

                private void MarkOrDispose()
                {
                    if (_handling)
                    {
                        // Mark done so Invoke will dispose.
                        _done = true;
                    }
                    else
                    {
                        // Dispose only if it's not in the progress queue.
                        MaybeDispose();
                    }
                }

                public override void Handle()
                {
                    ThrowIfInPool(this);
                    // TODO: handle thread race conditions (don't dispose early)
                    bool notCanceled = TryUnregisterAndIsNotCanceling(ref _cancelationRegistration) & !_canceledFromToken;
                    _canceled = true;

                    // HandleSelf
                    IValueContainer valueContainer = (IValueContainer) _valueOrPrevious;
                    _state = valueContainer.GetState();
                    if (_state == Promise.State.Resolved)
                    {
                        if (notCanceled)
                        {
                            InvokeAndCatch(1f);
                        }
                        HandleWaiter(valueContainer);
                        ResolveProgressListeners();
                    }
                    else
                    {
                        HandleWaiter(valueContainer);
                        CancelProgressListeners(null);
                    }

                    MarkOrDispose();
                }

                void ICancelDelegate.Invoke(ICancelValueContainer valueContainer)
                {
                    ThrowIfInPool(this);
                    // TODO: Remove this from the owner's progress listeners.
                    _canceledFromToken = true;
                }

                void ICancelDelegate.Dispose() { ThrowIfInPool(this); }
            }

            partial class PromiseWaitPromise : IProgressListener
            {
                // This is used to avoid rounding errors when normalizing the progress.
                private UnsignedFixed32 _currentAmount;
                private bool _secondPrevious;

                protected override void Dispose()
                {
                    base.Dispose();
                    _secondPrevious = false;
                }

                partial void SubscribeProgressToOther(PromiseRef other)
                {
                    _secondPrevious = true;
                    if (_progressListeners.IsNotEmpty)
                    {
                        SubscribeProgressToBranchesAndRoots(other, this);
                    }
                }

                protected override bool SubscribeProgressAndContinueLoop(ref IProgressListener progressListener, out PromiseRef previous)
                {
                    ThrowIfInPool(this);
                    // This is guaranteed to be pending.
                    bool firstSubscribe = _progressListeners.IsEmpty;
                    _progressListeners.Push(progressListener);
                    previous = _valueOrPrevious as PromiseRef;
                    if (_secondPrevious)
                    {
                        if (!firstSubscribe)
                        {
                            return false;
                        }
                        // Subscribe this to the returned promise.
                        progressListener = this;
                    }
                    return previous != null;
                }

                protected override bool SubscribeProgressIfWaiterAndContinueLoop(ref IProgressListener progressListener, out PromiseRef previous, Stack<PromisePassThrough> passThroughs)
                {
                    ThrowIfInPool(this);
                    if (_state != Promise.State.Pending)
                    {
                        previous = null;
                        return false;
                    }
                    return SubscribeProgressAndContinueLoop(ref progressListener, out previous);
                }

                protected override sealed void SetDepth(UnsignedFixed32 previousDepth)
                {
                    _waitDepthAndProgress = previousDepth.GetIncrementedWholeTruncated();
                }

                void IProgressListener.SetInitialProgress(UnsignedFixed32 progress)
                {
                    ThrowIfInPool(this);
                    PromiseRef previous = (PromiseRef) _valueOrPrevious;
                    if (previous._state == Promise.State.Pending)
                    {
                        SetProgress(previous, progress);
                    }
                }

                private UnsignedFixed32 NormalizeProgress(PromiseRef previous, UnsignedFixed32 progress)
                {
                    _currentAmount = progress;
                    // Calculate the normalized progress for the depth of the returned promise.
                    // Use double for better precision.
                    double expected = previous._waitDepthAndProgress.WholePart + 1u;
                    float normalizedProgress = (float) (_currentAmount.ToDouble() / expected);

                    return _waitDepthAndProgress = _waitDepthAndProgress.WithNewDecimalPart(normalizedProgress);
                }

                private void SetProgress(PromiseRef previous, UnsignedFixed32 progress)
                {
                    var newProgress = NormalizeProgress(previous, progress);
                    foreach (var progressListener in _progressListeners)
                    {
                        progressListener.SetProgress(this, newProgress);
                    }
                }

                void IProgressListener.SetProgress(PromiseRef sender, UnsignedFixed32 progress)
                {
                    ThrowIfInPool(this);
                    SetProgress((PromiseRef) _valueOrPrevious, progress);
                }

                void IProgressListener.ResolveOrSetProgress(PromiseRef sender, UnsignedFixed32 progress)
                {
                    ThrowIfInPool(this);
                    // Don't set progress if this is resolved by the second wait.
                    // Have to check the value's type since MakeReady is called before this.
                    if (!(_valueOrPrevious is IValueContainer))
                    {
                        SetProgress(sender, progress);
                    }
                }

                void IProgressListener.CancelOrSetProgress(PromiseRef sender, UnsignedFixed32 progress)
                {
                    ThrowIfInPool(this);
                    var newProgress = NormalizeProgress(sender, progress);
                    foreach (var progressListener in _progressListeners)
                    {
                        progressListener.CancelOrSetProgress(sender, newProgress);
                    }
                }
            }

            partial class AsyncPromiseBase
            {
                protected override sealed bool SubscribeProgressIfWaiterAndContinueLoop(ref IProgressListener progressListener, out PromiseRef previous, Stack<PromisePassThrough> passThroughs)
                {
                    ThrowIfInPool(this);
                    if (_state != Promise.State.Pending)
                    {
                        previous = null;
                        return false;
                    }
                    return SubscribeProgressAndContinueLoop(ref progressListener, out previous);
                }
            }

            partial class DeferredPromiseBase
            {
                internal bool TryReportProgress(float progress, int deferredId)
                {
                    if (deferredId != DeferredId) return false;
                    ThrowIfInPool(this);

                    // Don't report progress 1.0, that will be reported automatically when the promise is resolved.
                    if (progress >= 1f) return true;

                    UnsignedFixed32 newProgress = _waitDepthAndProgress.WithNewDecimalPart(progress);
                    _waitDepthAndProgress = newProgress;
                    foreach (var progressListener in _progressListeners)
                    {
                        progressListener.SetProgress(this, newProgress);
                    }
                    return true;
                }
            }

            partial class PromisePassThrough : IProgressListener
            {
                private UnsignedFixed32 _currentProgress;

                void IProgressListener.SetInitialProgress(UnsignedFixed32 progress)
                {
                    ThrowIfInPool(this);
                    //Retain();
                    _currentProgress = progress;
                    _target.IncrementProgress(progress.ToUInt32(), progress, _owner._waitDepthAndProgress);
                }

                void IProgressListener.SetProgress(PromiseRef sender, UnsignedFixed32 progress)
                {
                    ThrowIfInPool(this);
                    uint dif = progress.ToUInt32() - _currentProgress.ToUInt32();
                    _currentProgress = progress;
                    _target.IncrementProgress(dif, progress, _owner._waitDepthAndProgress);
                }

                void IProgressListener.ResolveOrSetProgress(PromiseRef sender, UnsignedFixed32 progress)
                {
                    ThrowIfInPool(this);
                    if (sender == _owner)
                    {
                        Release();
                    }
                }

                void IProgressListener.CancelOrSetProgress(PromiseRef sender, UnsignedFixed32 progress)
                {
                    ThrowIfInPool(this);
                    if (sender == _owner)
                    {
                        Release();
                    }
                }

                [MethodImpl(InlineOption)]
                internal uint GetProgressDifferenceToCompletion()
                {
                    ThrowIfInPool(this);
                    return _owner._waitDepthAndProgress.GetIncrementedWholeTruncated().ToUInt32() - _currentProgress.ToUInt32();
                }

                [MethodImpl(InlineOption)]
                partial void ResetProgress()
                {
                    _currentProgress = default(UnsignedFixed32);
                }

                [MethodImpl(InlineOption)]
                partial void TryUnsubscribeProgressAndRelease()
                {
                    if (_owner._progressListeners.TryRemove(this))
                    {
#if CSHARP_7_OR_LATER
                        object prev = _owner._valueOrPrevious;
                        while (prev is PromiseRef promise)
                        {
                            promise._progressListeners.TryRemove(this);
                            prev = promise._valueOrPrevious;
                        }
#else
                        PromiseRef promise = _owner._valueOrPrevious as PromiseRef;
                        while (promise != null)
                        {
                            promise._progressListeners.TryRemove(this);
                            promise = promise._valueOrPrevious as PromiseRef;
                        }
#endif
                        Release();
                    }
                }
            }
#endif
        }
    }
}