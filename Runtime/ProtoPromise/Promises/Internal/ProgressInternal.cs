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
                        if (passthrough.Owner.State == Promise.State.Pending)
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

            internal partial interface IProgressListener { }

            // Calls to these get compiled away when PROGRESS is undefined.
            partial void SetDepth(PromiseRef previous);
            partial void SetDepth();
            partial void ResetDepth();

            partial void WaitForProgressRetain();

            partial class PromiseSingleAwait
            {
                [MethodImpl(InlineOption)]
                protected void ResolveProgressListener() { ResolveProgressListenerPartial(); }
                [MethodImpl(InlineOption)]
                protected void CancelProgressListener() { CancelProgressListenerPartial(); }
                [MethodImpl(InlineOption)]
                protected void HandleProgressListener(Promise.State state) { HandleProgressListenerPartial(state); }

                partial void ResolveProgressListenerPartial();
                partial void CancelProgressListenerPartial();
                partial void HandleProgressListenerPartial(Promise.State state);
            }

            partial class PromiseMultiAwait
            {
                partial void ResolveProgressListeners();
                partial void CancelProgressListeners();
                partial void HandleProgressListeners(Promise.State state);
            }

#if PROMISE_PROGRESS
            [Flags]
            private enum ProgressFlags : byte
            {
                SecondPrevious = 1 << 0,
                SecondSubscribed = 1 << 1,
                InProgressQueue = 1 << 2,
                SetProgressLocked = 1 << 3,
                SubscribeProgressLocked = 1 << 4,

                SecondPreviousAndSubscribed = SecondPrevious | SecondSubscribed,

                All = SecondPreviousAndSubscribed | InProgressQueue | SetProgressLocked | SubscribeProgressLocked
            }

            partial struct SmallFields
            {
                internal ProgressFlags InterlockedSetProgressFlags(ProgressFlags progressFlags)
                {
                    StateAndFlags initialValue = default(StateAndFlags), newValue;
                    do
                    {
                        initialValue._intValue = _stateAndFlags._intValue;
                        newValue = initialValue;
                        newValue._progressFlags |= progressFlags;
                    }
                    while (Interlocked.CompareExchange(ref _stateAndFlags._intValue, newValue._intValue, initialValue._intValue) != initialValue._intValue);
                    return initialValue._progressFlags;
                }

                internal ProgressFlags InterlockedUnsetProgressFlags(ProgressFlags progressFlags)
                {
                    StateAndFlags initialValue = default(StateAndFlags), newValue;
                    ProgressFlags unsetFlags = ~progressFlags;
                    do
                    {
                        initialValue._intValue = _stateAndFlags._intValue;
                        newValue = initialValue;
                        newValue._progressFlags &= unsetFlags;
                    }
                    while (Interlocked.CompareExchange(ref _stateAndFlags._intValue, newValue._intValue, initialValue._intValue) != initialValue._intValue);
                    return initialValue._progressFlags;
                }

                [MethodImpl(InlineOption)]
                internal bool ProgressFlagsAreSet(ProgressFlags progressFlags)
                {
                    return (_stateAndFlags._progressFlags & progressFlags) != 0;
                }
            }

            private void SubscribeListener(IProgressListener progressListener)
            {
                PromiseRef current = this, previous;
                current.InterlockedRetainDisregardId(); // this retain is redundant for the loop logic to work easier.
                IProgressListener currentListener = progressListener;
                currentListener.Retain();
                bool continueLoop = AddProgressListenerAndContinueLoop(progressListener);
                while (true)
                {
                    current._smallFields.InterlockedSetProgressFlags(ProgressFlags.SubscribeProgressLocked);
                    Thread.MemoryBarrier(); // Make sure to write _progressLocker before reading _valueOrPrevious.
                    previous = GetPreviousForProgress(ref progressListener);
                    if (!continueLoop | previous == null)
                    {
                        current._smallFields.InterlockedUnsetProgressFlags(ProgressFlags.SubscribeProgressLocked);
                        current.SetInitialProgress(currentListener);
                        current.MaybeDispose();
                        return;
                    }
                    previous.InterlockedRetainDisregardId();
                    current._smallFields.InterlockedUnsetProgressFlags(ProgressFlags.SubscribeProgressLocked); // We only need to hold the lock until we retain. That way we're not holding the lock while the listener is added to a collection.
                    current.MaybeDispose();
                    currentListener = progressListener;
                    currentListener.Retain();
                    continueLoop = previous.AddProgressListenerAndContinueLoop(progressListener);
                    current = previous;
                }
            }

            protected abstract bool AddProgressListenerAndContinueLoop(IProgressListener progressListener);
            protected abstract void SetInitialProgress(IProgressListener progressListener);
            protected virtual PromiseRef GetPreviousForProgress(ref IProgressListener progressListener)
            {
                return _valueOrPrevious as PromiseRef;
            }

            partial void WaitForProgressRetain()
            {
                // Wait until SubscribeListener has retained this.
                SpinWait spinner = new SpinWait();
                while (_smallFields.ProgressFlagsAreSet(ProgressFlags.SubscribeProgressLocked))
                {
                    spinner.SpinOnce();
                }
            }

            protected void InterlockedRetainDisregardId()
            {
                ThrowIfInPool(this);
                _idsAndRetains.InterlockedRetainDisregardId();
            }

            private uint NextWholeProgress { get { return _smallFields._waitDepthAndProgress.WholePart + 1u; } }
            protected abstract void UnsubscribeProgressListener(ref IProgressListener progressListener, out PromiseRef previous);

            partial void ResetDepth()
            {
                _smallFields._waitDepthAndProgress = default(UnsignedFixed32);
            }

            partial void SetDepth(PromiseRef previous)
            {
                SetDepth(previous._smallFields._waitDepthAndProgress);
            }

            partial void SetDepth()
            {
                SetDepth(default(UnsignedFixed32));
            }

            protected virtual void SetDepth(UnsignedFixed32 previousDepth)
            {
                _smallFields._waitDepthAndProgress = previousDepth;
            }

            protected virtual UnsignedFixed32 CurrentProgress()
            {
                ThrowIfInPool(this);
                return _smallFields._waitDepthAndProgress;
            }

            // Handle progress.
            private static ValueLinkedQueueZeroGC<IProgressInvokable> _progressQueue;
            private static bool _runningProgress;

            private static void AddToFrontOfProgressQueue(IProgressInvokable progressListener)
            {
                _progressQueue.Push(progressListener);
            }

            private static void AddToBackOfProgressQueue(IProgressInvokable progressListener)
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
                    // Don't bother rounding, we don't want to accidentally round to 1.0.
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

            internal interface IProgressInvokable : ILinked<IProgressInvokable>
            {
                void Invoke();
            }

            partial interface IProgressListener : ILinked<IProgressListener>
            {
                void SetInitialProgress(UnsignedFixed32 progress);
                void SetProgress(PromiseRef sender, UnsignedFixed32 progress);
                void ResolveOrSetProgress(PromiseRef sender, UnsignedFixed32 progress);
                void CancelProgress(PromiseRef sender);
                void Retain();
            }

            partial interface IMultiTreeHandleable
            {
                void IncrementProgress(uint increment, UnsignedFixed32 senderAmount, UnsignedFixed32 ownerAmount);
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal sealed class PromiseProgress<TProgress> : PromiseBranch, IProgressListener, IProgressInvokable, ITraceable, ICancelDelegate
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
                IProgressListener ILinked<IProgressListener>.Next { get; set; }
                IProgressInvokable ILinked<IProgressInvokable>.Next { get; set; }

                // TODO: thread synchronization.
                private TProgress _progress;
                private UnsignedFixed32 _current;
                private bool _handling;
                private bool _done;
                private bool _canceled;
                private bool _canceledFromToken;
                private CancelationRegistration _cancelationRegistration;

                private PromiseProgress() { }

                internal static PromiseProgress<TProgress> GetOrCreate(TProgress progress, CancelationToken cancelationToken = default(CancelationToken))
                {
                    var promise = ObjectPool<ITreeHandleable>.GetOrCreate<PromiseProgress<TProgress>, Creator>();
                    promise.Reset();
                    promise._progress = progress;
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

                void IProgressInvokable.Invoke()
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
                    double expected = _smallFields._waitDepthAndProgress.WholePart + 1u;
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
                    if (!(_valueOrPrevious is PromiseRef))
                    {
                        // PromiseRef will have already made ready, just set _canceled to prevent progress queue from invoking.
                        _canceled = true;
                    }
                    else
                    {
                        SetProgress(progress);
                    }
                    MarkOrDispose();
                }

                void IProgressListener.SetInitialProgress(UnsignedFixed32 progress)
                {
                    ThrowIfInPool(this);
                    _current = progress;
                    _handling = true;
                    // Always add new listeners to the back.
                    AddToBackOfProgressQueue(this);
                }

                void IProgressListener.CancelProgress(PromiseRef sender)
                {
                    ThrowIfInPool(this);
                    if (!(_valueOrPrevious is PromiseRef))
                    {
                        // PromiseRef will have already made ready, just set _canceled to prevent progress queue from invoking.
                        _canceled = true;
                    }
                    MarkOrDispose();
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
                    Promise.State state = valueContainer.GetState();
                    State = state;
                    if (state == Promise.State.Resolved)
                    {
                        if (notCanceled)
                        {
                            InvokeAndCatch(1f);
                        }
                        HandleWaiter(valueContainer);
                        ResolveProgressListener();
                    }
                    else
                    {
                        HandleWaiter(valueContainer);
                        CancelProgressListener();
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

                void IProgressListener.Retain()
                {
                    InterlockedRetainDisregardId();
                }

                protected override bool AddProgressListenerAndContinueLoop(IProgressListener progressListener)
                {
                    ThrowIfInPool(this);
                    _progressListener = progressListener;
                    return false;
                }

                protected override void UnsubscribeProgressListener(ref IProgressListener progressListener, out PromiseRef previous)
                {
                    if (Interlocked.Exchange(ref _progressListener, null) != null)
                    {
                        progressListener.CancelProgress(this);
                    }
                    previous = null;
                }
            } // PromiseProgress<TProgress>

            partial class PromiseSingleAwait
            {
                volatile protected IProgressListener _progressListener;

                protected override bool AddProgressListenerAndContinueLoop(IProgressListener progressListener)
                {
                    ThrowIfInPool(this);
                    _progressListener = progressListener;
                    return true;
                }

                protected override void SetInitialProgress(IProgressListener progressListener)
                {
                    switch (State)
                    {
                        case Promise.State.Pending:
                            {
                                var progress = CurrentProgress();
                                if ((_smallFields.InterlockedSetProgressFlags(ProgressFlags.SetProgressLocked) & ProgressFlags.SetProgressLocked) == 0)
                                {
                                    progressListener = _progressListener;
                                    if (progressListener != null)
                                    {
                                        progressListener.SetInitialProgress(progress);
                                    }
                                    _smallFields.InterlockedUnsetProgressFlags(ProgressFlags.SetProgressLocked);
                                }
                                break;
                            }
                        case Promise.State.Resolved:
                            {
                                ResolveProgressListenerPartial();
                                break;
                            }
                        default: // Rejected or Canceled:
                            {
                                CancelProgressListenerPartial();
                                break;
                            }
                    }
                }

                // Waits for progressListener.SetProgress() calls to prevent stalled threads from operating on a pooled object.
                private void WaitForProgressLock()
                {
                    SpinWait spinner = new SpinWait();
                    while (_smallFields.ProgressFlagsAreSet(ProgressFlags.SetProgressLocked))
                    {
                        spinner.SpinOnce();
                    }
                }

                partial void ResolveProgressListenerPartial()
                {
                    IProgressListener progressListener = Interlocked.Exchange(ref _progressListener, null);
                    if (progressListener != null)
                    {
                        WaitForProgressLock();
                        progressListener.ResolveOrSetProgress(this, _smallFields._waitDepthAndProgress.GetIncrementedWholeTruncated());
                    }
                }

                partial void CancelProgressListenerPartial()
                {
                    IProgressListener progressListener = Interlocked.Exchange(ref _progressListener, null);
                    if (progressListener != null)
                    {
                        WaitForProgressLock();
                        progressListener.CancelProgress(this);
                    }
                }

                partial void HandleProgressListenerPartial(Promise.State state)
                {
                    if (state == Promise.State.Resolved)
                    {
                        ResolveProgressListenerPartial();
                    }
                    else
                    {
                        CancelProgressListenerPartial();
                    }
                }

                protected override void UnsubscribeProgressListener(ref IProgressListener progressListener, out PromiseRef previous)
                {
                    if (Interlocked.Exchange(ref _progressListener, null) != null)
                    {
                        progressListener.CancelProgress(this);
                        previous = _valueOrPrevious as PromiseRef;
                    }
                    else
                    {
                        previous = null;
                    }
                }
            } // PromiseSingleAwait

            partial class PromiseMultiAwait : IProgressInvokable
            {
                private readonly object _progressCollectionLocker = new object();
                private ValueLinkedStack<IProgressListener> _progressListeners;

                IProgressListener ILinked<IProgressListener>.Next { get; set; }
                IProgressInvokable ILinked<IProgressInvokable>.Next { get; set; }

                protected override bool AddProgressListenerAndContinueLoop(IProgressListener progressListener)
                {
                    ThrowIfInPool(this);
                    bool firstSubscribe;
                    lock (_progressCollectionLocker)
                    {
                        firstSubscribe = _progressListeners.IsEmpty;
                        _progressListeners.Push(progressListener);
                    }
                    return firstSubscribe;
                }

                protected override PromiseRef GetPreviousForProgress(ref IProgressListener progressListener)
                {
                    ThrowIfInPool(this);
                    progressListener = this;
                    return _valueOrPrevious as PromiseRef;
                }

                protected override void SetInitialProgress(IProgressListener progressListener)
                {
                    Promise.State state = State;
                    if (state == Promise.State.Pending)
                    {
                        lock (_progressCollectionLocker)
                        {
                            if (_progressListeners.IsNotEmpty)
                            {
                                progressListener.SetInitialProgress(CurrentProgress());
                            }
                        }
                    }
                    else
                    {
                        ValueLinkedStack<IProgressListener> progressListeners;
                        lock (_progressCollectionLocker)
                        {
                            progressListeners = _progressListeners;
                            _progressListeners.Clear();
                        }
                        if (state == Promise.State.Resolved)
                        {
                            while (progressListeners.IsNotEmpty)
                            {
                                progressListeners.Pop().SetInitialProgress(_smallFields._waitDepthAndProgress.GetIncrementedWholeTruncated());
                            }
                        }
                        else // Rejected or Canceled
                        {
                            while (progressListeners.IsNotEmpty)
                            {
                                progressListeners.Pop().CancelProgress(this);
                            }
                        }
                    }
                }

                partial void ResolveProgressListeners()
                {
                    ValueLinkedStack<IProgressListener> progressListeners;
                    lock (_progressCollectionLocker)
                    {
                        progressListeners = _progressListeners;
                        _progressListeners.Clear();
                    }
                    UnsignedFixed32 progress = _smallFields._waitDepthAndProgress.GetIncrementedWholeTruncated();
                    while (progressListeners.IsNotEmpty)
                    {
                        progressListeners.Pop().ResolveOrSetProgress(this, progress);
                    }
                }

                partial void CancelProgressListeners()
                {
                    ValueLinkedStack<IProgressListener> progressListeners;
                    lock (_progressCollectionLocker)
                    {
                        progressListeners = _progressListeners;
                        _progressListeners.Clear();
                    }
                    while (progressListeners.IsNotEmpty)
                    {
                        progressListeners.Pop().CancelProgress(this);
                    }
                }

                partial void HandleProgressListeners(Promise.State state)
                {
                    if (state == Promise.State.Resolved)
                    {
                        ResolveProgressListeners();
                    }
                    else
                    {
                        CancelProgressListeners();
                    }
                }

                protected override void UnsubscribeProgressListener(ref IProgressListener progressListener, out PromiseRef previous)
                {
                    bool removed;
                    lock (_progressCollectionLocker)
                    {
                        removed = _progressListeners.TryRemove(progressListener);
                    }
                    if (removed)
                    {
                        progressListener.CancelProgress(this);
                    }
                    previous = null;
                }

                private void SetProgress(UnsignedFixed32 progress)
                {
                    _smallFields._waitDepthAndProgress = progress;
                    if ((_smallFields.InterlockedSetProgressFlags(ProgressFlags.InProgressQueue) & ProgressFlags.InProgressQueue) != 0) // Was not already in progress queue?
                    {
                        InterlockedRetainDisregardId();
                        AddToFrontOfProgressQueue(this);
                    }
                }

                void IProgressListener.CancelProgress(PromiseRef sender)
                {
                    MaybeDispose();
                }

                void IProgressListener.ResolveOrSetProgress(PromiseRef sender, UnsignedFixed32 progress)
                {
                    SetProgress(progress);
                    MaybeDispose();
                }

                void IProgressListener.SetInitialProgress(UnsignedFixed32 progress)
                {
                    SetProgress(progress);
                }

                void IProgressListener.SetProgress(PromiseRef sender, UnsignedFixed32 progress)
                {
                    SetProgress(progress);
                }

                void IProgressListener.Retain()
                {
                    InterlockedRetainDisregardId();
                }

                void IProgressInvokable.Invoke()
                {
                    var progress = _smallFields._waitDepthAndProgress;
                    _smallFields.InterlockedUnsetProgressFlags(ProgressFlags.InProgressQueue);
                    lock (_progressCollectionLocker)
                    {
                        foreach (var progressListener in _progressListeners)
                        {
                            progressListener.SetProgress(this, progress);
                        }
                    }
                    MaybeDispose();
                }
            } // PromiseMultiAwait

            partial class PromiseBranch
            {
                internal void UnsubscribeProgressListener(object previous)
                {
                    IProgressListener progressListener = Interlocked.Exchange(ref _progressListener, null);
                    if (progressListener != null)
                    {
                        progressListener.CancelProgress(this);

                        PromiseRef promise = previous as PromiseRef;
                        while (promise != null)
                        {
                            promise.UnsubscribeProgressListener(ref progressListener, out promise);
                        }
                    }
                }
            }

            partial class AsyncPromiseBase
            {
                protected override bool AddProgressListenerAndContinueLoop(IProgressListener progressListener)
                {
                    ThrowIfInPool(this);
                    _progressListener = progressListener;
                    return false;
                }
            }

            partial class DeferredPromiseBase
            {
                internal bool TryReportProgress(float progress, short deferredId)
                {
                    if (!_idsAndRetains.InterlockedTryRetainWithDeferredId(deferredId))
                    {
                        return false;
                    }

                    ThrowIfInPool(this);
                    var newProgress = _smallFields._waitDepthAndProgress.WithNewDecimalPart(progress);
                    bool tookLock = (_smallFields.InterlockedSetProgressFlags(ProgressFlags.SetProgressLocked) & ProgressFlags.SetProgressLocked) == 0;
                    if (deferredId != DeferredId)
                    {
                        if (tookLock)
                        {
                            _smallFields.InterlockedUnsetProgressFlags(ProgressFlags.SetProgressLocked);
                        }
                        MaybeDispose();
                        return false;
                    }

                    // Don't report progress 1.0, that will be reported automatically when the promise is resolved.
                    if (progress < 1f)
                    {
                        _smallFields._waitDepthAndProgress = newProgress;
                        if (tookLock)
                        {
                            IProgressListener progressListener = _progressListener;
                            if (progressListener != null)
                            {
                                progressListener.SetProgress(this, _smallFields._waitDepthAndProgress);
                            }
                            _smallFields.InterlockedUnsetProgressFlags(ProgressFlags.SetProgressLocked);
                        }
                    }
                    MaybeDispose();
                    return true;
                }
            }

            partial class PromiseWaitPromise : IProgressInvokable
            {
                IProgressListener ILinked<IProgressListener>.Next { get; set; }
                IProgressInvokable ILinked<IProgressInvokable>.Next { get; set; }

                // Lazy subscribe: only subscribe to second previous if a progress listener is added to this (this keeps execution more efficient when progress isn't used).
                partial void SubscribeProgressToOther(PromiseRef other)
                {
                    bool hasListener = _progressListener != null;
                    ProgressFlags oldFlags = _smallFields.InterlockedSetProgressFlags(hasListener ? ProgressFlags.SecondPreviousAndSubscribed : ProgressFlags.SecondPrevious);
                    if (hasListener & (oldFlags & ProgressFlags.SecondSubscribed) == 0) // Has listener and was not already subscribed?
                    {
                        other.SubscribeListener(this);
                    }
                }

                protected override PromiseRef GetPreviousForProgress(ref IProgressListener progressListener)
                {
                    ThrowIfInPool(this);
                    if (_smallFields.ProgressFlagsAreSet(ProgressFlags.SecondPrevious)) // Are we waiting on second previous?
                    {
                        if ((_smallFields.InterlockedSetProgressFlags(ProgressFlags.SecondSubscribed) & ProgressFlags.SecondSubscribed) != 0) // Was already subscribed?
                        {
                            return null;
                        }
                        progressListener = this;
                    }
                    return _valueOrPrevious as PromiseRef;
                }

                protected override sealed void SetDepth(UnsignedFixed32 previousDepth)
                {
                    _smallFields._waitDepthAndProgress = previousDepth.GetIncrementedWholeTruncated();
                }

                void IProgressListener.SetInitialProgress(UnsignedFixed32 progress)
                {
                    ThrowIfInPool(this);
                    SetProgress((PromiseRef) _valueOrPrevious, progress);
                }

                private void SetProgress(PromiseRef previous, UnsignedFixed32 progress)
                {
                    // Calculate the normalized progress for the depth of the returned promise.
                    // Use double for better precision.
                    double expected = previous._smallFields._waitDepthAndProgress.WholePart + 1u;
                    float normalizedProgress = (float) (progress.ToDouble() / expected);
                    _smallFields._waitDepthAndProgress = _smallFields._waitDepthAndProgress.WithNewDecimalPart(normalizedProgress);
                    if ((_smallFields.InterlockedSetProgressFlags(ProgressFlags.InProgressQueue) & ProgressFlags.InProgressQueue) != 0) // Was not already in progress queue?
                    {
                        InterlockedRetainDisregardId();
                        AddToFrontOfProgressQueue(this);
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
                    MaybeDispose();
                }

                void IProgressListener.CancelProgress(PromiseRef sender)
                {
                    MaybeDispose();
                }

                void IProgressListener.Retain()
                {
                    InterlockedRetainDisregardId();
                }

                void IProgressInvokable.Invoke()
                {
                    var progress = _smallFields._waitDepthAndProgress;
                    _smallFields.InterlockedUnsetProgressFlags(ProgressFlags.InProgressQueue);
                    if ((_smallFields.InterlockedSetProgressFlags(ProgressFlags.SetProgressLocked) & ProgressFlags.SetProgressLocked) == 0)
                    {
                        IProgressListener progressListener = _progressListener;
                        if (progressListener != null)
                        {
                            progressListener.SetProgress(this, progress);
                        }
                        _smallFields.InterlockedUnsetProgressFlags(ProgressFlags.SetProgressLocked);
                    }
                    MaybeDispose();
                }
            } // PromiseWaitPromise

            partial class PromisePassThrough : IProgressListener
            {
                IProgressListener ILinked<IProgressListener>.Next { get; set; }

                private UnsignedFixed32 _currentProgress;

                void IProgressListener.SetInitialProgress(UnsignedFixed32 progress)
                {
                    ThrowIfInPool(this);
                    //Retain();
                    _currentProgress = progress;
                    _target.IncrementProgress(progress.ToUInt32(), progress, _owner._smallFields._waitDepthAndProgress);
                }

                void IProgressListener.SetProgress(PromiseRef sender, UnsignedFixed32 progress)
                {
                    ThrowIfInPool(this);
                    uint dif = progress.ToUInt32() - _currentProgress.ToUInt32();
                    _currentProgress = progress;
                    _target.IncrementProgress(dif, progress, _owner._smallFields._waitDepthAndProgress);
                }

                void IProgressListener.ResolveOrSetProgress(PromiseRef sender, UnsignedFixed32 progress)
                {
                    Release();
                }

                void IProgressListener.CancelProgress(PromiseRef sender)
                {
                    Release();
                }

                void IProgressListener.Retain()
                {
                    Retain();
                }

                [MethodImpl(InlineOption)]
                internal uint GetProgressDifferenceToCompletion()
                {
                    ThrowIfInPool(this);
                    return _owner._smallFields._waitDepthAndProgress.GetIncrementedWholeTruncated().ToUInt32() - _currentProgress.ToUInt32();
                }

                [MethodImpl(InlineOption)]
                partial void ResetProgress()
                {
                    _currentProgress = default(UnsignedFixed32);
                }

                [MethodImpl(InlineOption)]
                partial void TryUnsubscribeProgressAndRelease()
                {
                    IProgressListener progressListener = this;

                    PromiseRef promise = _owner;
                    do
                    {
                        promise.UnsubscribeProgressListener(ref progressListener, out promise);
                    } while (promise != null);
                }
            } // PromisePassThrough
#endif
        } // PromiseRef
    } // Internal
}