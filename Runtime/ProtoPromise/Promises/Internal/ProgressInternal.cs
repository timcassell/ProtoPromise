﻿#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
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

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Proto.Utils;

namespace Proto.Promises
{
    partial class Internal
    {
        partial class PromiseRef
        {
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
                None = 0,

                // Don't change the layout, very important.
                SecondPrevious = 1 << 0,
                SecondSubscribed = 1 << 1,
                InProgressQueue = 1 << 2,
                SubscribeProgressLocked = 1 << 3,

                All = SecondPrevious | SecondSubscribed | InProgressQueue | SubscribeProgressLocked
            }

            partial struct SmallFields
            {
                partial struct StateAndFlags
                {
                    [MethodImpl(InlineOption)]
                    internal ProgressFlags InterlockedSetSubscribedIfSecondPrevious()
                    {
                        StateAndFlags initialValue = default(StateAndFlags), newValue;
                        do
                        {
                            initialValue._intValue = _intValue;
                            newValue = initialValue;
                            ProgressFlags setFlags = (ProgressFlags) ((byte) (newValue._progressFlags & ProgressFlags.SecondPrevious) << 1); // Change SecondPrevious flag to SecondSubscribed.
                            newValue._progressFlags |= setFlags;
                        }
                        while (Interlocked.CompareExchange(ref _intValue, newValue._intValue, initialValue._intValue) != initialValue._intValue);
                        return initialValue._progressFlags;
                    }

                    internal ProgressFlags InterlockedSetProgressFlags(ProgressFlags progressFlags)
                    {
                        StateAndFlags initialValue = default(StateAndFlags), newValue;
                        do
                        {
                            initialValue._intValue = _intValue;
                            newValue = initialValue;
                            newValue._progressFlags |= progressFlags;
                        }
                        while (Interlocked.CompareExchange(ref _intValue, newValue._intValue, initialValue._intValue) != initialValue._intValue);
                        return initialValue._progressFlags;
                    }

                    internal ProgressFlags InterlockedUnsetProgressFlags(ProgressFlags progressFlags)
                    {
                        StateAndFlags initialValue = default(StateAndFlags), newValue;
                        ProgressFlags unsetFlags = ~progressFlags;
                        do
                        {
                            initialValue._intValue = _intValue;
                            newValue = initialValue;
                            newValue._progressFlags &= unsetFlags;
                        }
                        while (Interlocked.CompareExchange(ref _intValue, newValue._intValue, initialValue._intValue) != initialValue._intValue);
                        return initialValue._progressFlags;
                    }

                    [MethodImpl(InlineOption)]
                    internal bool ProgressFlagsAreSet(ProgressFlags progressFlags)
                    {
                        return (_progressFlags & progressFlags) != 0;
                    }
                } // StateAndFlags
            } // SmallFields

            private void SubscribeListener(IProgressListener progressListener)
            {
                PromiseRef current = this, previous;
                current.InterlockedRetainDisregardId(); // this retain is redundant for the loop logic to work easier.
                IProgressListener currentListener = progressListener;
                currentListener.Retain();
                bool continueLoop = AddProgressListenerAndContinueLoop(progressListener);
                while (true)
                {
                    current._smallFields._stateAndFlags.InterlockedSetProgressFlags(ProgressFlags.SubscribeProgressLocked);
                    Thread.MemoryBarrier(); // Make sure to write ProgressLocked before reading _valueOrPrevious.
                    previous = current.GetPreviousForProgress(ref progressListener);
                    if (!continueLoop | previous == null)
                    {
                        current._smallFields._stateAndFlags.InterlockedUnsetProgressFlags(ProgressFlags.SubscribeProgressLocked);
                        current.SetInitialProgress(currentListener);
                        current.MaybeDispose();
                        return;
                    }
                    previous.InterlockedRetainDisregardId();
                    current._smallFields._stateAndFlags.InterlockedUnsetProgressFlags(ProgressFlags.SubscribeProgressLocked); // We only need to hold the lock until we retain. That way we're not holding the lock while the listener is added to a collection.
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
                while (_smallFields._stateAndFlags.ProgressFlagsAreSet(ProgressFlags.SubscribeProgressLocked))
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
                void SetInitialProgress(PromiseRef sender, Promise.State state, UnsignedFixed32 progress);
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
            internal sealed partial class PromiseProgress<TProgress> : PromiseProgressBase, IProgressListener, IProgressInvokable, ICancelDelegate
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
                // TODO: thread synchronization.
                private PromiseProgress() { }

                internal static PromiseProgress<TProgress> GetOrCreate(TProgress progress, CancelationToken cancelationToken = default(CancelationToken))
                {
                    var promise = ObjectPool<ITreeHandleable>.GetOrCreate<PromiseProgress<TProgress>, Creator>();
                    promise.Reset();
                    promise._progress = progress;
                    promise._handling = false;
                    promise._suspended = false;
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
                    UnsignedFixed32 current = _current;
                    _handling = false;
                    if (!_suspended & !_canceled & !_canceledFromToken)
                    {
                        // Calculate the normalized progress for the depth that the listener was added.
                        // Use double for better precision.
                        double expected = _smallFields._waitDepthAndProgress.WholePart + 1u;
                        InvokeAndCatch((float) (current.ToDouble() / expected));
                    }
                    MaybeDispose();
                }

                private void SetProgress(UnsignedFixed32 progress)
                {
                    _current = progress;
                    _suspended = false;
                    if (!_handling & !_canceled & !_canceledFromToken)
                    {
                        _handling = true;
                        InterlockedRetainDisregardId();
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
                    MaybeDispose();
                }

                void IProgressListener.SetInitialProgress(PromiseRef sender, Promise.State state, UnsignedFixed32 progress)
                {
                    ThrowIfInPool(this);
                    switch (state)
                    {
                        case Promise.State.Pending:
                        {
                            _current = progress;
#if CSHARP_7_OR_LATER
                            bool senderIsSuspended = (object) sender is PromiseProgressBase progressBase && progressBase._suspended;
#else
                            PromiseProgressBase progressBase = sender as PromiseProgressBase;
                            bool senderIsSuspended = progressBase != null && progressBase._suspended;
#endif
                            if (!senderIsSuspended & !_handling & !_suspended & !_canceled & !_canceledFromToken)
                            {
                                _handling = true;
                                InterlockedRetainDisregardId();
                                // Always add new listeners to the back.
                                AddToBackOfProgressQueue(this);
                            }
                            break;
                        }
                        case Promise.State.Resolved:
                        {
                            if (sender != _valueOrPrevious)
                            {
                                _current = progress;
                                _suspended = false;
                                if (!_handling & !_canceled & !_canceledFromToken)
                                {
                                    _handling = true;
                                    // Always add new listeners to the back.
                                    AddToBackOfProgressQueue(this);
                                    break; // Break instead of InterlockedRetainDisregardId().
                                }
                            }
                            MaybeDispose();
                            break;
                        }
                        default: // Rejected or Canceled:
                        {
                            _suspended = true;
                            MaybeDispose();
                            break;
                        }
                    }
                }

                void IProgressListener.CancelProgress(PromiseRef sender)
                {
                    ThrowIfInPool(this);
                    if (!(_valueOrPrevious is PromiseRef))
                    {
                        // PromiseRef will have already made ready, just set _canceled to prevent progress queue from invoking.
                        _canceled = true;
                    }
                    _suspended = true;
                    MaybeDispose();
                }

                protected override void SetInitialProgress(IProgressListener progressListener)
                {
                    ThrowIfInPool(this);
                    Promise.State state = State;
                    if (state == Promise.State.Pending & !_canceled)
                    {
                        if (_progressListener != null)
                        {
                            progressListener.SetInitialProgress(this, state, CurrentProgress());
                        }
                    }
                    else
                    {
                        progressListener = Interlocked.Exchange(ref _progressListener, null);
                        if (progressListener != null)
                        {
                            progressListener.SetInitialProgress(this, Promise.State.Canceled, _smallFields._waitDepthAndProgress.GetIncrementedWholeTruncated());
                        }
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

                    MaybeDispose();
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
                protected override bool AddProgressListenerAndContinueLoop(IProgressListener progressListener)
                {
                    ThrowIfInPool(this);
                    _progressListener = progressListener;
                    return true;
                }

                protected override void SetInitialProgress(IProgressListener progressListener)
                {
                    ThrowIfInPool(this);
                    Promise.State state = State;
                    if (state == Promise.State.Pending)
                    {
                        if (_progressListener != null)
                        {
                            progressListener.SetInitialProgress(this, state, CurrentProgress());
                        }
                    }
                    else
                    {
                        progressListener = Interlocked.Exchange(ref _progressListener, null);
                        if (progressListener != null)
                        {
                            progressListener.SetInitialProgress(this, state, _smallFields._waitDepthAndProgress.GetIncrementedWholeTruncated());
                        }
                    }
                }

                partial void ResolveProgressListenerPartial()
                {
                    IProgressListener progressListener = Interlocked.Exchange(ref _progressListener, null);
                    if (progressListener != null)
                    {
                        progressListener.ResolveOrSetProgress(this, _smallFields._waitDepthAndProgress.GetIncrementedWholeTruncated());
                    }
                }

                partial void CancelProgressListenerPartial()
                {
                    IProgressListener progressListener = Interlocked.Exchange(ref _progressListener, null);
                    if (progressListener != null)
                    {
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
                    ThrowIfInPool(this);
                    if (Interlocked.CompareExchange(ref _progressListener, null, progressListener) == progressListener)
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
                    ThrowIfInPool(this);
                    Promise.State state = State;
                    if (state == Promise.State.Pending)
                    {
                        lock (_progressCollectionLocker)
                        {
                            if (_progressListeners.IsNotEmpty)
                            {
                                progressListener.SetInitialProgress(this, state, CurrentProgress());
                            }
                        }
                    }
                    else
                    {
                        bool removed;
                        lock (_progressCollectionLocker)
                        {
                            removed = _progressListeners.TryRemove(progressListener);
                        }
                        if (removed)
                        {
                            progressListener.SetInitialProgress(this, state, _smallFields._waitDepthAndProgress.GetIncrementedWholeTruncated());
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
                    ThrowIfInPool(this);
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
                    if ((_smallFields._stateAndFlags.InterlockedSetProgressFlags(ProgressFlags.InProgressQueue) & ProgressFlags.InProgressQueue) != 0) // Was not already in progress queue?
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
                    ThrowIfInPool(this);
                    SetProgress(progress);
                    MaybeDispose();
                }

                void IProgressListener.SetInitialProgress(PromiseRef sender, Promise.State state, UnsignedFixed32 progress)
                {
                    ThrowIfInPool(this);
                    switch (state)
                    {
                        case Promise.State.Pending:
                        {
                            _smallFields._waitDepthAndProgress = progress;
                            if ((_smallFields._stateAndFlags.InterlockedSetProgressFlags(ProgressFlags.InProgressQueue) & ProgressFlags.InProgressQueue) != 0) // Was not already in progress queue?
                            {
                                InterlockedRetainDisregardId();
                                // Always add new listeners to the back.
                                AddToBackOfProgressQueue(this);
                            }
                            break;
                        }
                        case Promise.State.Resolved:
                        {
                            if (sender != _valueOrPrevious)
                            {
                                _smallFields._waitDepthAndProgress = progress;
                                if ((_smallFields._stateAndFlags.InterlockedSetProgressFlags(ProgressFlags.InProgressQueue) & ProgressFlags.InProgressQueue) != 0) // Was not already in progress queue?
                                {
                                    // Always add new listeners to the back.
                                    AddToBackOfProgressQueue(this);
                                    break; // Break instead of InterlockedRetainDisregardId().
                                }
                            }
                            MaybeDispose();
                            break;
                        }
                        default: // Rejected or Canceled:
                        {
                            MaybeDispose();
                            break;
                        }
                    }
                }

                void IProgressListener.SetProgress(PromiseRef sender, UnsignedFixed32 progress)
                {
                    ThrowIfInPool(this);
                    SetProgress(progress);
                }

                void IProgressListener.Retain()
                {
                    ThrowIfInPool(this);
                    InterlockedRetainDisregardId();
                }

                void IProgressInvokable.Invoke()
                {
                    ThrowIfInPool(this);
                    var progress = _smallFields._waitDepthAndProgress;
                    _smallFields._stateAndFlags.InterlockedUnsetProgressFlags(ProgressFlags.InProgressQueue);
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

                protected override PromiseRef GetPreviousForProgress(ref IProgressListener progressListener)
                {
                    ThrowIfInPool(this);
                    return null;
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

                    // Don't report progress 1.0, that will be reported automatically when the promise is resolved.
                    if (progress < 1f)
                    {
                        _smallFields._waitDepthAndProgress = newProgress;
                        IProgressListener progressListener = _progressListener;
                        if (progressListener != null)
                        {
                            progressListener.SetProgress(this, newProgress);
                        }
                    }
                    MaybeDispose();
                    return true;
                }
            }

            partial class PromiseWaitPromise : IProgressInvokable
            {
                partial void SubscribeProgressToOther(PromiseRef other)
                {
                    // Lazy subscribe: only subscribe to second previous if a progress listener is added to this (this keeps execution more efficient when progress isn't used).
                    bool hasListener = _progressListener != null;
                    ProgressFlags subscribedFlag = hasListener ? ProgressFlags.SecondSubscribed : ProgressFlags.None;
                    ProgressFlags oldFlags = _smallFields._stateAndFlags.InterlockedSetProgressFlags(ProgressFlags.SecondPrevious | subscribedFlag);
                    if (hasListener & (oldFlags & ProgressFlags.SecondSubscribed) == 0) // Has listener and was not already subscribed?
                    {
                        other.SubscribeListener(this);
                    }
                }

                protected override PromiseRef GetPreviousForProgress(ref IProgressListener progressListener)
                {
                    ThrowIfInPool(this);
#if CSHARP_7_OR_LATER
                    if (!(_valueOrPrevious is PromiseRef previous))
#else
                    PromiseRef previous = _valueOrPrevious as PromiseRef;
                    if (previous == null)
#endif
                    {
                        // Promise is either transitioning to second previous, or has already completed.
                        return null;
                    }

                    ProgressFlags oldFlags = _smallFields._stateAndFlags.InterlockedSetSubscribedIfSecondPrevious();
                    bool secondPrevious = (oldFlags & ProgressFlags.SecondPrevious) != 0;
                    bool secondSubscribed = (oldFlags & ProgressFlags.SecondSubscribed) != 0;
                    if (secondPrevious) // Are we waiting on second previous?
                    {
                        if (secondSubscribed) // Was already subscribed?
                        {
                            return null;
                        }
                        progressListener = this;
                    }
                    return previous;
                }

                protected override sealed void SetDepth(UnsignedFixed32 previousDepth)
                {
                    _smallFields._waitDepthAndProgress = previousDepth.GetIncrementedWholeTruncated();
                }

                void IProgressListener.SetInitialProgress(PromiseRef sender, Promise.State state, UnsignedFixed32 progress)
                {
                    ThrowIfInPool(this);
                    PromiseRef previous = (PromiseRef) _valueOrPrevious;
                    switch (state)
                    {
                        case Promise.State.Pending:
                        {
                            if (SetProgressAndTryMarkInQueue(previous, progress))
                            {
                                InterlockedRetainDisregardId();
                                // Always add new listeners to the back.
                                AddToBackOfProgressQueue(this);
                            }
                            break;
                        }
                        case Promise.State.Resolved:
                        {
                            if (sender != _valueOrPrevious && SetProgressAndTryMarkInQueue(previous, progress))
                            {
                                // Always add new listeners to the back.
                                AddToBackOfProgressQueue(this);
                            }
                            else
                            {
                                MaybeDispose();
                            }
                            break;
                        }
                        default: // Rejected or Canceled:
                        {
                            MaybeDispose();
                            break;
                        }
                    }
                }

                private bool SetProgressAndTryMarkInQueue(PromiseRef previous, UnsignedFixed32 progress)
                {
                    // Calculate the normalized progress for the depth of the returned promise.
                    // Use double for better precision.
                    double expected = previous._smallFields._waitDepthAndProgress.WholePart + 1u;
                    float normalizedProgress = (float) (progress.ToDouble() / expected);
                    _smallFields._waitDepthAndProgress = _smallFields._waitDepthAndProgress.WithNewDecimalPart(normalizedProgress);
                    return (_smallFields._stateAndFlags.InterlockedSetProgressFlags(ProgressFlags.InProgressQueue) & ProgressFlags.InProgressQueue) == 0; // Was not already in progress queue?
                }

                private void SetProgressAndMaybeAddToQueue(PromiseRef previous, UnsignedFixed32 progress)
                {
                    if (SetProgressAndTryMarkInQueue(previous, progress))
                    {
                        InterlockedRetainDisregardId();
                        AddToFrontOfProgressQueue(this);
                    }
                }

                void IProgressListener.SetProgress(PromiseRef sender, UnsignedFixed32 progress)
                {
                    ThrowIfInPool(this);
                    SetProgressAndMaybeAddToQueue((PromiseRef) _valueOrPrevious, progress);
                }

                void IProgressListener.ResolveOrSetProgress(PromiseRef sender, UnsignedFixed32 progress)
                {
                    ThrowIfInPool(this);
                    // Don't set progress if this is resolved by the second wait.
                    // Have to check the value's type since MakeReady is called before this.
                    if (!(_valueOrPrevious is IValueContainer))
                    {
                        SetProgressAndMaybeAddToQueue(sender, progress);
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
                    _smallFields._stateAndFlags.InterlockedUnsetProgressFlags(ProgressFlags.InProgressQueue);
                    IProgressListener progressListener = _progressListener;
                    if (progressListener != null)
                    {
                        progressListener.SetProgress(this, progress);
                    }
                    MaybeDispose();
                }
            } // PromiseWaitPromise

            partial class PromisePassThrough
            {
                // TODO: thread safety
                void IProgressListener.SetInitialProgress(PromiseRef sender, Promise.State state, UnsignedFixed32 progress)
                {
                    ThrowIfInPool(this);
                    if (state == Promise.State.Pending)
                    {
                        _currentProgress = progress;
                        _target.IncrementProgress(progress.ToUInt32(), progress, _owner._smallFields._waitDepthAndProgress);
                    }
                    else
                    {
                        Release();
                    }
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
                internal uint GetProgressDifferenceToCompletion(PromiseRef owner)
                {
                    ThrowIfInPool(this);
                    return owner._smallFields._waitDepthAndProgress.GetIncrementedWholeTruncated().ToUInt32() - _currentProgress.ToUInt32();
                }

                [MethodImpl(InlineOption)]
                partial void ResetProgress()
                {
                    _currentProgress = default(UnsignedFixed32);
                }

                [MethodImpl(InlineOption)]
                partial void TryUnsubscribeProgressAndRelease(PromiseRef owner)
                {
                    IProgressListener progressListener = this;
                    while (owner != null)
                    {
                        owner.UnsubscribeProgressListener(ref progressListener, out owner);
                    }
                }
            } // PromisePassThrough
#endif
        } // PromiseRef
    } // Internal
}