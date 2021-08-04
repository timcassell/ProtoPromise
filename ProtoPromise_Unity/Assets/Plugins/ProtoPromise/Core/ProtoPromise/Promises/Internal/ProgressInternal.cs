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
#pragma warning disable CS0420 // A reference to a volatile field will not be treated as volatile

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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

            partial void WaitWhileProgressFlags(ProgressFlags progressFlags);

            partial class PromiseSingleAwait
            {
                [MethodImpl(InlineOption)]
                protected void ResolveProgressListener() { ResolveProgressListenerPartial(); }
                [MethodImpl(InlineOption)]
                internal void CancelProgressListener() { CancelProgressListenerPartial(); }
                [MethodImpl(InlineOption)]
                protected void HandleProgressListener(Promise.State state) { HandleProgressListenerPartial(state); }

                partial void ResolveProgressListenerPartial();
                partial void CancelProgressListenerPartial();
                partial void HandleProgressListenerPartial(Promise.State state);
            }

            partial class PromiseMultiAwait
            {
                partial void ResetProgress();
                partial void ResolveProgressListeners();
                partial void CancelProgressListeners();
                partial void HandleProgressListeners(Promise.State state);
            }

            [Flags]
            private enum ProgressFlags : byte
            {
                None = 0,

                // Don't change the layout, very important for InterlockedSetSubscribedIfSecondPrevious().
                SecondPrevious = 1 << 0,
                SelfSubscribed = 1 << 1,
                InProgressQueue = 1 << 2,
                Subscribing = 1 << 3,
                Reporting = 1 << 4,
                SettingInitial = 1 << 5,

                All = byte.MaxValue
            }

#if PROMISE_PROGRESS
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
                        } while (Interlocked.CompareExchange(ref _intValue, newValue._intValue, initialValue._intValue) != initialValue._intValue);
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
                        } while (Interlocked.CompareExchange(ref _intValue, newValue._intValue, initialValue._intValue) != initialValue._intValue);
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
                        } while (Interlocked.CompareExchange(ref _intValue, newValue._intValue, initialValue._intValue) != initialValue._intValue);
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
                PromiseRef current = this;
                current.InterlockedRetainDisregardId(); // this retain is redundant for the loop logic to work easier.
                while (true)
                {
                    IProgressListener currentListener = progressListener;
                    currentListener.Retain();
                    PromiseRef previous = current.AddProgressListenerAndGetPreviousRetained(ref progressListener);
                    if (previous == null)
                    {
                        current._smallFields._stateAndFlags.InterlockedSetProgressFlags(ProgressFlags.SettingInitial);
                        current.SetInitialProgress(currentListener, currentListener == progressListener);
                        current._smallFields._stateAndFlags.InterlockedUnsetProgressFlags(ProgressFlags.SettingInitial);
                        current.MaybeDispose();
                        return;
                    }
                    current.MaybeDispose();
                    current = previous;
                }
            }

            protected abstract PromiseRef AddProgressListenerAndGetPreviousRetained(ref IProgressListener progressListener);
            protected abstract void SetInitialProgress(IProgressListener progressListener, bool shouldReport);

            partial void WaitWhileProgressFlags(ProgressFlags progressFlags)
            {
                // Wait until progressFlags are unset.
                // This is used to make sure promises and progress listeners aren't disposed while still in use on another thread.
                SpinWait spinner = new SpinWait();
                Thread.MemoryBarrier(); // Make sure any writes happen before we read progress flags.
                while (_smallFields._stateAndFlags.ProgressFlagsAreSet(progressFlags))
                {
                    spinner.SpinOnce();
                }
            }

            protected virtual bool GetIsProgressSuspended()
            {
                var state = _smallFields._stateAndFlags._state;
                return state == Promise.State.Canceled
                    | state == Promise.State.Rejected
                    | _smallFields._waitDepthAndProgress.IsNegative;
            }

            protected void InterlockedRetainDisregardId()
            {
                ThrowIfInPool(this);
                _idsAndRetains.InterlockedRetainDisregardId();
            }

            private int NextWholeProgress { get { return _smallFields._waitDepthAndProgress.PositiveWholePart + 1; } }

            partial void ResetDepth()
            {
                _smallFields._waitDepthAndProgress = default(Fixed32);
            }

            partial void SetDepth(PromiseRef previous)
            {
                SetDepth(previous._smallFields._waitDepthAndProgress);
            }

            partial void SetDepth()
            {
                SetDepth(default(Fixed32));
            }

            protected virtual void SetDepth(Fixed32 previousDepth)
            {
                _smallFields._waitDepthAndProgress = previousDepth;
            }

            protected virtual Fixed32 CurrentProgress()
            {
                ThrowIfInPool(this);
                return _smallFields._waitDepthAndProgress;
            }

            // Handle progress.
            private static readonly object _progressLocker = new object();
            private static ValueLinkedQueue<IProgressInvokable> _progressQueue;

            private static void AddToFrontOfProgressQueue(IProgressInvokable progressListener)
            {
                lock (_progressLocker)
                {
                    _progressQueue.Push(progressListener);
                }
            }

            private static void AddToBackOfProgressQueue(IProgressInvokable progressListener)
            {
                lock (_progressLocker)
                {
                    _progressQueue.Enqueue(progressListener);
                }
            }

            internal static void InvokeProgressListeners()
            {
                while (true)
                {
                    ValueLinkedQueue<IProgressInvokable> queue;
                    lock (_progressLocker)
                    {
                        queue = _progressQueue;
                        _progressQueue.Clear();
                    }
                    if (queue.IsEmpty)
                    {
                        break;
                    }

                    do
                    {
                        queue.DequeueRisky().Invoke();
                    } while (queue.IsNotEmpty);
                }
            }

            /// <summary>
            /// Max Whole Number: 2^(31-<see cref="Promise.Config.ProgressDecimalBits"/>)
            /// <para/>Precision: 1/(2^<see cref="Promise.Config.ProgressDecimalBits"/>)
            /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal struct Fixed32
            {
                private const double DecimalMax = 1 << Promise.Config.ProgressDecimalBits;
                private const int DecimalMask = (1 << Promise.Config.ProgressDecimalBits) - 1;
                private const int WholeMask = ~DecimalMask;
                private const int PositiveMask = ~(1 << 31);

                // Negative bit is used as a flag for progress to know if it's suspended.
                // This is necessary to update the value and the flag atomically without a lock.
                private volatile int _value;

                [MethodImpl(InlineOption)]
                internal Fixed32(int wholePart)
                {
                    _value = wholePart << Promise.Config.ProgressDecimalBits;
                }

                [MethodImpl(InlineOption)]
                internal Fixed32(double value)
                {
                    // Don't bother rounding, we don't want to accidentally round to 1.0.
                    _value = (int) (value * DecimalMax);
                }

                [MethodImpl(InlineOption)]
                private Fixed32(int value, bool _)
                {
                    _value = value;
                }

                internal int WholePart
                {
                    [MethodImpl(InlineOption)]
                    get { return _value >> Promise.Config.ProgressDecimalBits; }
                }

                internal int PositiveWholePart
                {
                    [MethodImpl(InlineOption)]
                    get { return (_value & PositiveMask) >> Promise.Config.ProgressDecimalBits; }
                }

                internal double DecimalPart
                {
                    [MethodImpl(InlineOption)]
                    get { return (double) (_value & DecimalMask) / DecimalMax; }
                }

                internal bool IsNegative
                {
                    [MethodImpl(InlineOption)]
                    get { return _value < 0; }
                }

                [MethodImpl(InlineOption)]
                internal uint ToPositiveUInt32()
                {
                    unchecked
                    {
                        return (uint) (_value & PositiveMask);
                    }
                }

                internal double ToDouble()
                {
                    int val = _value;
                    double wholePart = val >> Promise.Config.ProgressDecimalBits;
                    double decimalPart = (double) (val & DecimalMask) / DecimalMax;
                    return wholePart + decimalPart;
                }

                internal bool InterlockedTrySetIfGreater(Fixed32 other)
                {
                    int otherValue = other._value;
                    int current;
                    do
                    {
                        current = _value;
                        if (otherValue < current)
                        {
                            return false;
                        }
                    } while (Interlocked.CompareExchange(ref _value, otherValue, current) != current);
                    return true;
                }

                [MethodImpl(InlineOption)]
                internal bool InterlockedTrySetAndGetDifferenceIfNotNegativeAndWholeIsGreater(Fixed32 other, out uint dif)
                {
                    int otherValue = other._value;
                    int otherWholePart = other.WholePart;
                    int current;
                    do
                    {
                        current = _value;
                        bool failIfEquals = current < 0;
                        int currentWholePart = (current & PositiveMask) >> Promise.Config.ProgressDecimalBits; // Make positive before comparing.
                        if (otherWholePart < currentWholePart | (failIfEquals & otherWholePart == currentWholePart))
                        {
                            dif = 0;
                            return false;
                        }
                    } while (Interlocked.CompareExchange(ref _value, otherValue, current) != current);
                    unchecked
                    {
                        dif = (uint) other._value - (uint) (current & PositiveMask); // Make positive before getting difference.
                    }
                    return true;
                }

                [MethodImpl(InlineOption)]
                internal Fixed32 SetNewDecimalPart(double decimalPart)
                {
                    // Don't bother rounding, we don't want to accidentally round to 1.0.
                    int newDecimalPart = (int) (decimalPart * DecimalMax);
                    int newValue = (_value & WholeMask) | newDecimalPart;
                    _value = newValue;
                    return new Fixed32(newValue, true);
                }

                [MethodImpl(InlineOption)]
                internal void InterlockedSetNewDecimalPartIfNotNegativeAndDecimalIsGreater(double decimalPart)
                {
                    // Don't bother rounding, we don't want to accidentally round to 1.0.
                    int newDecimalPart = (int) (decimalPart * DecimalMax);
                    int current, newValue;
                    do
                    {
                        current = _value;
                        bool failIfEquals = current < 0;
                        int currentDecimalPart = current & DecimalMask;
                        if (newDecimalPart < currentDecimalPart | (failIfEquals & newDecimalPart == currentDecimalPart))
                        {
                            return;
                        }
                        newValue = (current & WholeMask) | newDecimalPart;
                    } while (Interlocked.CompareExchange(ref _value, newValue, current) != current);
                }

                internal bool InterlockedTrySetIfNotNegativeAndWholeIsGreater(Fixed32 other)
                {
                    int otherValue = other._value;
                    int otherWholePart = other.WholePart;
                    int current;
                    do
                    {
                        current = _value;
                        bool failIfEquals = current < 0;
                        int currentWholePart = (current & PositiveMask) >> Promise.Config.ProgressDecimalBits; // Make positive before comparing.
                        if (otherWholePart < currentWholePart | (failIfEquals & otherWholePart == currentWholePart))
                        {
                            return false;
                        }
                    } while (Interlocked.CompareExchange(ref _value, otherValue, current) != current);
                    return true;
                }

                internal void MaybeMakeNegative()
                {
                    int current = _value;
                    int negated = current | ~PositiveMask;
                    Interlocked.CompareExchange(ref _value, negated, current);
                }

                internal void InterlockedMaybeMakeNegativeIfWholeIsNotGreater()
                {
                    // Try to make negative, only retry if the updated whole value is not greater than the old whole value.
                    int current = _value;
                    int oldWholePart = current & PositiveMask;
                Retry:
                    int negated = current | ~PositiveMask;
                    if (Interlocked.CompareExchange(ref _value, negated, current) != current)
                    {
                        int newValue = _value;
                        int newWholePart = _value & PositiveMask;
                        if (newWholePart <= oldWholePart)
                        {
                            current = newValue;
                            goto Retry;
                        }
                    }
                }

                [MethodImpl(InlineOption)]
                internal void InterlockedMaybeMakeNegativeIfDecimalIsNotGreater()
                {
                    // Try to make negative, only retry if the updated decimal value is not greater than the old decimal value.
                    int current = _value;
                    int oldDecimalPart = current & DecimalMask;
                Retry:
                    int negated = current | ~PositiveMask;
                    if (Interlocked.CompareExchange(ref _value, negated, current) != current)
                    {
                        int newValue = _value;
                        int newDecimalPart = _value & DecimalMask;
                        if (newDecimalPart <= oldDecimalPart)
                        {
                            current = newValue;
                            goto Retry;
                        }
                    }
                }

                // For SetInitialProgress
                [MethodImpl(InlineOption)]
                internal bool SetNewDecimalPartIfNotNegativeAndWhole(double decimalPart, out bool wasPositive)
                {
                    // Don't bother rounding, we don't want to accidentally round to 1.0.
                    int newDecimalPart = (int) (decimalPart * DecimalMax);
                    int oldValue = _value;
                    wasPositive = oldValue >= 0;
                    if (wasPositive & (oldValue & DecimalMask) == 0)
                    {
                        int newValue = (oldValue & WholeMask) | newDecimalPart;
                        return Interlocked.CompareExchange(ref _value, newValue, oldValue) == oldValue;
                    }
                    return false;
                }

                // For SetInitialProgress
                [MethodImpl(InlineOption)]
                internal bool TrySetIfZero(Fixed32 other)
                {
                    return Interlocked.CompareExchange(ref _value, other._value, 0) == 0;
                }

                // For SetInitialProgress
                [MethodImpl(InlineOption)]
                internal bool TrySetIfZeroOrWasPositive(Fixed32 other)
                {
                    // Don't care if it was set, just care if the old value was 0 or positive.
                    return Interlocked.CompareExchange(ref _value, other._value, 0) >= 0;
                }

                internal Fixed32 GetIncrementedWholeTruncated()
                {
                    int newValue = (_value & WholeMask & PositiveMask) + (1 << Promise.Config.ProgressDecimalBits);
#if PROMISE_DEBUG
                    if (newValue < 0)
                    {
                        throw new OverflowException();
                    }
#endif
                    return new Fixed32(newValue, true);
                }

                [MethodImpl(InlineOption)]
                public static bool operator >(Fixed32 lhs, Fixed32 rhs)
                {
                    return lhs._value > rhs._value;
                }

                [MethodImpl(InlineOption)]
                public static bool operator <(Fixed32 lhs, Fixed32 rhs)
                {
                    return lhs._value < rhs._value;
                }
            }

            /// <summary>
            /// Max Whole Number: 2^(64-<see cref="Promise.Config.ProgressDecimalBits"/>)
            /// Precision: 1/(2^<see cref="Promise.Config.ProgressDecimalBits"/>)
            /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal struct UnsignedFixed64 // Simplified compared to Fixed32 to remove unused functions.
            {
                private const double DecimalMax = 1ul << Promise.Config.ProgressDecimalBits;
                private const ulong DecimalMask = (1ul << Promise.Config.ProgressDecimalBits) - 1ul;

                private long _value; // long for Interlocked.

                [MethodImpl(InlineOption)]
                internal UnsignedFixed64(ulong wholePart)
                {
                    unchecked
                    {
                        _value = (long) (wholePart << Promise.Config.ProgressDecimalBits);
                    }
                }

                internal double ToDouble()
                {
                    unchecked
                    {
                        ulong val = (ulong) Interlocked.Read(ref _value);
                        double wholePart = val >> Promise.Config.ProgressDecimalBits;
                        double decimalPart = (double) (val & DecimalMask) / DecimalMax;
                        return wholePart + decimalPart;
                    }
                }

                [MethodImpl(InlineOption)]
                internal void InterlockedIncrement(uint increment)
                {
                    Interlocked.Add(ref _value, increment);
                }
            }

            internal interface IProgressInvokable : ILinked<IProgressInvokable>
            {
                void Invoke();
            }

            partial interface IProgressListener : ILinked<IProgressListener>
            {
                void SetInitialProgress(PromiseRef sender, Promise.State state, Fixed32 progress, bool shouldReport);
                void SetProgress(PromiseRef sender, Fixed32 progress, out PromiseSingleAwait nextRef);
                void ResolveOrSetProgress(PromiseRef sender, Fixed32 progress);
                void CancelProgress();
                void Retain();
            }

            partial interface IMultiTreeHandleable
            {
                void IncrementProgress(uint increment, Fixed32 senderAmount, Fixed32 ownerAmount, bool shouldReport);
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal sealed partial class PromiseProgress<TProgress> : PromiseBranch, IProgressListener, IProgressInvokable, ICancelDelegate
                where TProgress : IProgress<float>
            {
                [MethodImpl(InlineOption)]
                protected override bool GetIsProgressSuspended()
                {
                    return _smallProgressFields._currentProgress.IsNegative;
                }

                private bool IsComplete
                {
                    [MethodImpl(InlineOption)]
                    get { return _smallProgressFields._complete; }
                    [MethodImpl(InlineOption)]
                    set { _smallProgressFields._complete = value; }
                }

                private bool IsCanceled
                {
                    [MethodImpl(InlineOption)]
                    get { return _smallProgressFields._canceled; }
                    [MethodImpl(InlineOption)]
                    set { _smallProgressFields._canceled = value; }
                }

                private PromiseProgress() { }

                internal static PromiseProgress<TProgress> GetOrCreate(TProgress progress, CancelationToken cancelationToken = default(CancelationToken))
                {
                    var promise = ObjectPool<ITreeHandleable>.TryTake<PromiseProgress<TProgress>>()
                        ?? new PromiseProgress<TProgress>();
                    promise.Reset();
                    promise._progress = progress;
                    promise.IsComplete = false;
                    promise.IsCanceled = false;
                    promise._smallProgressFields._currentProgress = default(Fixed32);
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
                    // Calculate the normalized progress for the depth that the listener was added.
                    // Use double for better precision.
                    double expected = _smallFields._waitDepthAndProgress.WholePart + 1u;
                    float value = (float) (_smallProgressFields._currentProgress.ToDouble() / expected);
                    _smallFields._stateAndFlags.InterlockedUnsetProgressFlags(ProgressFlags.InProgressQueue);
                    if (value >= 0 & !IsComplete & !IsCanceled)
                    {
                        InvokeAndCatch(value);
                    }
                    MaybeDispose();
                }

                private void SetProgress(Fixed32 progress)
                {
                    if (_smallProgressFields._currentProgress.InterlockedTrySetIfNotNegativeAndWholeIsGreater(progress) & !IsComplete & !IsCanceled)
                    {
                        if ((_smallFields._stateAndFlags.InterlockedSetProgressFlags(ProgressFlags.InProgressQueue) & ProgressFlags.InProgressQueue) == 0) // Was not already in progress queue?
                        {
                            InterlockedRetainDisregardId();
                            AddToBackOfProgressQueue(this);
                        }
                    }
                }

                void IProgressListener.SetProgress(PromiseRef sender, Fixed32 progress, out PromiseSingleAwait nextRef)
                {
                    ThrowIfInPool(this);
                    SetProgress(progress);
                    nextRef = this;
                }

                void IProgressListener.ResolveOrSetProgress(PromiseRef sender, Fixed32 progress)
                {
                    ThrowIfInPool(this);
                    if (!(_valueOrPrevious is PromiseRef))
                    {
                        // PromiseRef will have already made ready, just set IsComplete to prevent progress queue from invoking.
                        IsComplete = true;
                    }
                    else
                    {
                        SetProgress(progress);
                    }
                    MaybeDispose();
                }

                void IProgressListener.SetInitialProgress(PromiseRef sender, Promise.State state, Fixed32 progress, bool shouldReport)
                {
                    ThrowIfInPool(this);
                    switch (state)
                    {
                        case Promise.State.Pending:
                        {
                            if ((shouldReport & !sender.GetIsProgressSuspended()) && TrySetInitialProgressAndMarkInQueue(progress))
                            {
                                InterlockedRetainDisregardId();
                                // Always add new listeners to the back.
                                AddToBackOfProgressQueue(this);
                            }
                            break;
                        }
                        case Promise.State.Resolved:
                        {
                            if ((shouldReport & sender != _valueOrPrevious) && TrySetInitialProgressAndMarkInQueue(progress))
                            {
                                // Always add new listeners to the back.
                                AddToBackOfProgressQueue(this);
                                break; // Break instead of InterlockedRetainDisregardId().
                            }
                            MaybeDispose();
                            break;
                        }
                        default: // Rejected or Canceled:
                        {
                            _smallProgressFields._currentProgress.MaybeMakeNegative();
                            MaybeDispose();
                            break;
                        }
                    }
                }

                private bool TrySetInitialProgressAndMarkInQueue(Fixed32 progress)
                {
                    return _smallProgressFields._currentProgress.TrySetIfZeroOrWasPositive(progress)
                        && (_smallFields._stateAndFlags.InterlockedSetProgressFlags(ProgressFlags.InProgressQueue) & ProgressFlags.InProgressQueue) == 0; // Was not already in progress queue?
                }

                void IProgressListener.CancelProgress()
                {
                    ThrowIfInPool(this);
                    _smallProgressFields._currentProgress.InterlockedMaybeMakeNegativeIfWholeIsNotGreater();
                    if (!(_valueOrPrevious is PromiseRef))
                    {
                        // PromiseRef will have already made ready, just set IsComplete to prevent progress queue from invoking.
                        IsComplete = true;
                    }
                    MaybeDispose();
                }

                protected override void SetInitialProgress(IProgressListener progressListener, bool shouldReport)
                {
                    ThrowIfInPool(this);
                    Promise.State state = State;
                    if (state == Promise.State.Pending & !IsComplete & !IsCanceled)
                    {
                        if (_progressListener == progressListener)
                        {
                            progressListener.SetInitialProgress(this, state, _smallProgressFields._currentProgress, shouldReport);
                        }
                    }
                    else
                    {
                        if (Interlocked.CompareExchange(ref _progressListener, null, progressListener) == progressListener)
                        {
                            progressListener.SetInitialProgress(this, Promise.State.Canceled, _smallFields._waitDepthAndProgress.GetIncrementedWholeTruncated(), false);
                        }
                    }
                }

                public override void Handle()
                {
                    ThrowIfInPool(this);
                    bool notCanceled = TryUnregisterAndIsNotCanceling(ref _cancelationRegistration) & !IsCanceled;
                    IsComplete = true;

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
                    IsCanceled = true;
                }

                void ICancelDelegate.Dispose() { ThrowIfInPool(this); }

                void IProgressListener.Retain()
                {
                    ThrowIfInPool(this);
                    InterlockedRetainDisregardId();
                }
                protected override PromiseRef AddProgressListenerAndGetPreviousRetained(ref IProgressListener progressListener)
                {
                    ThrowIfInPool(this);
                    _progressListener = progressListener;
                    return null;
                }
            } // PromiseProgress<TProgress>

            partial class PromiseSingleAwait
            {
                protected override PromiseRef AddProgressListenerAndGetPreviousRetained(ref IProgressListener progressListener)
                {
                    ThrowIfInPool(this);
                    _progressListener = progressListener;
                    // Mark subscribing to prevent repooling while we get previous, then unmark after we have retained previous.
                    _smallFields._stateAndFlags.InterlockedSetProgressFlags(ProgressFlags.Subscribing);
                    PromiseRef previous = _valueOrPrevious as PromiseRef;
                    if (previous != null)
                    {
                        previous.InterlockedRetainDisregardId();
                    }
                    _smallFields._stateAndFlags.InterlockedUnsetProgressFlags(ProgressFlags.Subscribing);
                    return previous;
                }

                protected override void SetInitialProgress(IProgressListener progressListener, bool shouldReport)
                {
                    ThrowIfInPool(this);
                    Promise.State state = State;
                    if (state == Promise.State.Pending)
                    {
                        if (_progressListener == progressListener)
                        {
                            progressListener.SetInitialProgress(this, state, CurrentProgress(), shouldReport);
                        }
                    }
                    else
                    {
                        if (Interlocked.CompareExchange(ref _progressListener, null, progressListener) == progressListener)
                        {
                            progressListener.SetInitialProgress(this, state, _smallFields._waitDepthAndProgress.GetIncrementedWholeTruncated(), shouldReport);
                        }
                    }
                }

                partial void ResolveProgressListenerPartial()
                {
                    IProgressListener progressListener = Interlocked.Exchange(ref _progressListener, null);
                    if (progressListener != null)
                    {
                        WaitWhileProgressFlags(ProgressFlags.Reporting | ProgressFlags.SettingInitial);
                        progressListener.ResolveOrSetProgress(this, _smallFields._waitDepthAndProgress.GetIncrementedWholeTruncated());
                    }
                }

                partial void CancelProgressListenerPartial()
                {
                    IProgressListener progressListener = Interlocked.Exchange(ref _progressListener, null);
                    if (progressListener != null)
                    {
                        WaitWhileProgressFlags(ProgressFlags.Reporting | ProgressFlags.SettingInitial);
                        progressListener.CancelProgress();
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

                internal void ReportProgress(Fixed32 progress)
                {
                    PromiseSingleAwait setter = this;
                    do
                    {
                        if ((setter._smallFields._stateAndFlags.InterlockedSetProgressFlags(ProgressFlags.Reporting) & ProgressFlags.Reporting) != 0)
                        {
                            break;
                        }
                        PromiseSingleAwait unsetter = setter;

                        IProgressListener progressListener = setter._progressListener;
                        if (progressListener != null)
                        {
                            progressListener.SetProgress(this, progress, out setter);
                        }
                        else
                        {
                            setter = null;
                        }
                        unsetter._smallFields._stateAndFlags.InterlockedUnsetProgressFlags(ProgressFlags.Reporting);
                    } while (setter != null);
                }
            } // PromiseSingleAwait

            partial class PromiseMultiAwait : IProgressInvokable
            {
                [MethodImpl(InlineOption)]
                partial void ResetProgress()
                {
                    _currentProgress = default(Fixed32);
                }

                protected override PromiseRef AddProgressListenerAndGetPreviousRetained(ref IProgressListener progressListener)
                {
                    ThrowIfInPool(this);
                    bool notSubscribed = (_smallFields._stateAndFlags.InterlockedSetProgressFlags(ProgressFlags.SelfSubscribed) & ProgressFlags.SelfSubscribed) == 0;
                    lock (_progressCollectionLocker)
                    {
                        _progressListeners.Enqueue(progressListener);
                    }
                    PromiseRef previous = null;
                    if (notSubscribed)
                    {
                        // Mark subscribing to prevent repooling while we get previous, then unmark after we have retained previous.
                        _smallFields._stateAndFlags.InterlockedSetProgressFlags(ProgressFlags.Subscribing);
                        previous = _valueOrPrevious as PromiseRef;
                        if (previous != null)
                        {
                            previous.InterlockedRetainDisregardId();
                        }
                        _smallFields._stateAndFlags.InterlockedUnsetProgressFlags(ProgressFlags.Subscribing);
                    }
                    progressListener = this;
                    return previous;
                }

                protected override void SetInitialProgress(IProgressListener progressListener, bool shouldReport)
                {
                    ThrowIfInPool(this);
                    Promise.State state = State;
                    if (state == Promise.State.Pending)
                    {
                        bool contained;
                        lock (_progressCollectionLocker)
                        {
                            contained = _progressListeners.Contains(progressListener);
                        }
                        if (contained)
                        {
                            progressListener.SetInitialProgress(this, state, CurrentProgress(), shouldReport);
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
                            progressListener.SetInitialProgress(this, state, _smallFields._waitDepthAndProgress.GetIncrementedWholeTruncated(), shouldReport);
                        }
                    }
                }

                partial void ResolveProgressListeners()
                {
                    ValueLinkedQueue<IProgressListener> progressListeners;
                    lock (_progressCollectionLocker)
                    {
                        progressListeners = _progressListeners;
                        _progressListeners.Clear();
                    }
                    Fixed32 progress = _smallFields._waitDepthAndProgress.GetIncrementedWholeTruncated();
                    while (progressListeners.IsNotEmpty)
                    {
                        progressListeners.DequeueRisky().ResolveOrSetProgress(this, progress);
                    }
                }

                partial void CancelProgressListeners()
                {
                    ValueLinkedQueue<IProgressListener> progressListeners;
                    lock (_progressCollectionLocker)
                    {
                        progressListeners = _progressListeners;
                        _progressListeners.Clear();
                    }
                    while (progressListeners.IsNotEmpty)
                    {
                        progressListeners.DequeueRisky().CancelProgress();
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

                private void SetProgress(Fixed32 progress)
                {
                    if (_currentProgress.InterlockedTrySetIfNotNegativeAndWholeIsGreater(progress)
                        && (_smallFields._stateAndFlags.InterlockedSetProgressFlags(ProgressFlags.InProgressQueue) & ProgressFlags.InProgressQueue) == 0) // Was not already in progress queue?
                    {
                        InterlockedRetainDisregardId();
                        AddToFrontOfProgressQueue(this);
                    }
                }

                void IProgressListener.CancelProgress()
                {
                    _currentProgress.InterlockedMaybeMakeNegativeIfWholeIsNotGreater();
                    MaybeDispose();
                }

                void IProgressListener.ResolveOrSetProgress(PromiseRef sender, Fixed32 progress)
                {
                    ThrowIfInPool(this);
                    SetProgress(progress);
                    MaybeDispose();
                }

                void IProgressListener.SetInitialProgress(PromiseRef sender, Promise.State state, Fixed32 progress, bool shouldReport)
                {
                    ThrowIfInPool(this);
                    switch (state)
                    {
                        case Promise.State.Pending:
                        {
                            if ((shouldReport & !sender.GetIsProgressSuspended()) && TrySetInitialProgressAndMarkInQueue(progress))
                            {
                                InterlockedRetainDisregardId();
                                AddToFrontOfProgressQueue(this);
                            }
                            break;
                        }
                        case Promise.State.Resolved:
                        {
                            if ((shouldReport & sender != _valueOrPrevious) && TrySetInitialProgressAndMarkInQueue(progress))
                            {
                                AddToFrontOfProgressQueue(this);
                                break; // Break instead of InterlockedRetainDisregardId().
                            }
                            MaybeDispose();
                            break;
                        }
                        default: // Rejected or Canceled:
                        {
                            _currentProgress.MaybeMakeNegative();
                            MaybeDispose();
                            break;
                        }
                    }
                }

                private bool TrySetInitialProgressAndMarkInQueue(Fixed32 progress)
                {
                    return _currentProgress.TrySetIfZeroOrWasPositive(progress)
                        && (_smallFields._stateAndFlags.InterlockedSetProgressFlags(ProgressFlags.InProgressQueue) & ProgressFlags.InProgressQueue) == 0; // Was not already in progress queue?
                }

                void IProgressListener.SetProgress(PromiseRef sender, Fixed32 progress, out PromiseSingleAwait nextRef)
                {
                    ThrowIfInPool(this);
                    SetProgress(progress);
                    nextRef = null;
                }

                void IProgressListener.Retain()
                {
                    ThrowIfInPool(this);
                    InterlockedRetainDisregardId();
                }

                void IProgressInvokable.Invoke()
                {
                    ThrowIfInPool(this);
                    Thread.MemoryBarrier(); // Make sure we're reading fresh progress (since the field cannot be marked volatile).
                    var progress = _smallFields._waitDepthAndProgress;
                    _smallFields._stateAndFlags.InterlockedUnsetProgressFlags(ProgressFlags.InProgressQueue);
                    if (!progress.IsNegative) // Was it not suspended?
                    {
                        lock (_progressCollectionLocker)
                        {
                            foreach (var progressListener in _progressListeners)
                            {
                                PromiseSingleAwait nextRef;
                                progressListener.SetProgress(this, progress, out nextRef);
                                if (nextRef != null)
                                {
                                    nextRef.ReportProgress(progress);
                                }
                            }
                        }
                    }
                    MaybeDispose();
                }

                [MethodImpl(InlineOption)]
                protected override sealed bool GetIsProgressSuspended()
                {
                    return _currentProgress.IsNegative;
                }
            } // PromiseMultiAwait

            partial class AsyncPromiseBase
            {
                protected override PromiseRef AddProgressListenerAndGetPreviousRetained(ref IProgressListener progressListener)
                {
                    ThrowIfInPool(this);
                    _progressListener = progressListener;
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

                    // Don't report progress 1.0, that will be reported automatically when the promise is resolved.
                    if (progress >= 0 & progress < 1f)
                    {
                        var newProgress = _smallFields._waitDepthAndProgress.SetNewDecimalPart(progress);
                        ReportProgress(newProgress);
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
                    ProgressFlags subscribedFlag = hasListener ? ProgressFlags.SelfSubscribed : ProgressFlags.None;
                    ProgressFlags oldFlags = _smallFields._stateAndFlags.InterlockedSetProgressFlags(ProgressFlags.SecondPrevious | subscribedFlag);
                    if (hasListener & (oldFlags & ProgressFlags.SelfSubscribed) == 0) // Has listener and was not already subscribed?
                    {
                        other.SubscribeListener(this);
                    }
                }

                protected override PromiseRef AddProgressListenerAndGetPreviousRetained(ref IProgressListener progressListener)
                {
                    ThrowIfInPool(this);
                    _progressListener = progressListener;
                    ProgressFlags oldFlags = _smallFields._stateAndFlags.InterlockedSetSubscribedIfSecondPrevious();
                    bool secondPrevious = (oldFlags & ProgressFlags.SecondPrevious) != 0;
                    bool secondSubscribed = (oldFlags & ProgressFlags.SelfSubscribed) != 0;
                    if (secondPrevious) // Are we waiting on second previous?
                    {
                        if (secondSubscribed) // Was already subscribed?
                        {
                            return null;
                        }
                        progressListener = this;
                    }
                    // Mark subscribing to prevent repooling while we get previous, then unmark after we have retained previous.
                    _smallFields._stateAndFlags.InterlockedSetProgressFlags(ProgressFlags.Subscribing);
                    PromiseRef previous = _valueOrPrevious as PromiseRef;
                    if (previous != null) // If previous is null, this is either transitioning to second previous, or has already completed.
                    {
                        previous.InterlockedRetainDisregardId();
                    }
                    _smallFields._stateAndFlags.InterlockedUnsetProgressFlags(ProgressFlags.Subscribing);
                    return previous;
                }

                protected override sealed void SetDepth(Fixed32 previousDepth)
                {
                    _smallFields._waitDepthAndProgress = previousDepth.GetIncrementedWholeTruncated();
                }

                void IProgressListener.SetInitialProgress(PromiseRef sender, Promise.State state, Fixed32 progress, bool shouldReport)
                {
                    ThrowIfInPool(this);
                    // Check type in case of race condition with MakeReady.
#if CSHARP_7_3_OR_NEWER
                    if (!(_valueOrPrevious is PromiseRef previous))
#else
                    PromiseRef previous = _valueOrPrevious as PromiseRef;
                    if (previous == null)
#endif
                    {
                        return;
                    }
                    switch (state)
                    {
                        case Promise.State.Pending:
                        {
                            if ((shouldReport & !sender.GetIsProgressSuspended()) && TrySetInitialProgressAndMarkInQueue(previous, progress))
                            {
                                InterlockedRetainDisregardId();
                                AddToFrontOfProgressQueue(this);
                            }
                            break;
                        }
                        case Promise.State.Resolved:
                        {
                            if ((shouldReport & sender != _valueOrPrevious) && TrySetInitialProgressAndMarkInQueue(previous, progress))
                            {
                                AddToFrontOfProgressQueue(this);
                            }
                            else
                            {
                                MaybeDispose();
                            }
                            break;
                        }
                        default: // Rejected or Canceled:
                        {
                            _smallFields._waitDepthAndProgress.MaybeMakeNegative();
                            MaybeDispose();
                            break;
                        }
                    }
                }

                private bool TrySetInitialProgressAndMarkInQueue(PromiseRef previous, Fixed32 progress)
                {
                    bool isNotSuspended;
                    _smallFields._waitDepthAndProgress.SetNewDecimalPartIfNotNegativeAndWhole(NormalizeProgress(previous, progress), out isNotSuspended);
                    return isNotSuspended
                        && (_smallFields._stateAndFlags.InterlockedSetProgressFlags(ProgressFlags.InProgressQueue) & ProgressFlags.InProgressQueue) == 0; // Was not already in progress queue?
                }

                private double NormalizeProgress(PromiseRef previous, Fixed32 progress)
                {
                    // Calculate the normalized progress for the depth of the returned promise.
                    // Use double for better precision.
                    double expected = previous._smallFields._waitDepthAndProgress.WholePart + 1u;
                    return progress.ToDouble() / expected;
                }

                private void SetProgressAndMaybeAddToQueue(PromiseRef previous, Fixed32 progress)
                {
                    _smallFields._waitDepthAndProgress.InterlockedSetNewDecimalPartIfNotNegativeAndDecimalIsGreater(NormalizeProgress(previous, progress));
                    if ((_smallFields._stateAndFlags.InterlockedSetProgressFlags(ProgressFlags.InProgressQueue) & ProgressFlags.InProgressQueue) == 0) // Was not already in progress queue?
                    {
                        InterlockedRetainDisregardId();
                        AddToFrontOfProgressQueue(this);
                    }
                }

                void IProgressListener.SetProgress(PromiseRef sender, Fixed32 progress, out PromiseSingleAwait nextRef)
                {
                    ThrowIfInPool(this);
                    // Check type in case of race condition with MakeReady.
#if CSHARP_7_3_OR_NEWER
                    if (_valueOrPrevious is PromiseRef previous)
#else
                    PromiseRef previous = _valueOrPrevious as PromiseRef;
                    if (previous != null)
#endif
                    {
                        SetProgressAndMaybeAddToQueue(previous, progress);
                    }
                    nextRef = null;
                }

                void IProgressListener.ResolveOrSetProgress(PromiseRef sender, Fixed32 progress)
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

                void IProgressListener.CancelProgress()
                {
                    _smallFields._waitDepthAndProgress.InterlockedMaybeMakeNegativeIfDecimalIsNotGreater();
                    MaybeDispose();
                }

                void IProgressListener.Retain()
                {
                    InterlockedRetainDisregardId();
                }

                void IProgressInvokable.Invoke()
                {
                    Thread.MemoryBarrier(); // Make sure we're reading fresh progress (since the field cannot be marked volatile).
                    var progress = _smallFields._waitDepthAndProgress;
                    _smallFields._stateAndFlags.InterlockedUnsetProgressFlags(ProgressFlags.InProgressQueue);
                    if (!progress.IsNegative) // Was it not suspended?
                    {
                        ReportProgress(progress);
                    }
                    MaybeDispose();
                }
            } // PromiseWaitPromise

            partial class PromisePassThrough
            {
                void IProgressListener.SetInitialProgress(PromiseRef sender, Promise.State state, Fixed32 progress, bool shouldReport)
                {
                    ThrowIfInPool(this);
                    if (state == Promise.State.Pending)
                    {
                        bool didSet = _smallFields._currentProgress.TrySetIfZero(progress);
                        _smallFields._settingInitialProgress = true;
                        Thread.MemoryBarrier(); // Make sure _owner is read after _settingInitialProgress is written.
                        var owner = _owner;
                        if (didSet & owner != null)
                        {
                            _target.IncrementProgress(progress.ToPositiveUInt32(), progress, owner._smallFields._waitDepthAndProgress, shouldReport);
                        }
                        _smallFields._settingInitialProgress = false;
                    }
                    else
                    {
                        Release();
                    }
                }

                // This will never be called concurrently on multiple threads (ensured by PromiseSingleAwait and PromiseMultiAwait).
                void IProgressListener.SetProgress(PromiseRef sender, Fixed32 progress, out PromiseSingleAwait nextRef)
                {
                    ThrowIfInPool(this);
                    uint dif;
                    bool didSet = _smallFields._currentProgress.InterlockedTrySetAndGetDifferenceIfNotNegativeAndWholeIsGreater(progress, out dif);
                    _smallFields._reportingProgress = true;
                    Thread.MemoryBarrier(); // Make sure _owner is read after _reportingProgress is written.
                    var owner = _owner;
                    if (didSet & owner != null)
                    {
                        _target.IncrementProgress(dif, progress, owner._smallFields._waitDepthAndProgress, true);
                    }
                    _smallFields._reportingProgress = false;
                    nextRef = null;
                }

                partial void WaitWhileProgressIsBusy()
                {
                    SpinWait spinner = new SpinWait();
                    Thread.MemoryBarrier(); // Make sure any writes happen before reading the flags.
                    while (_smallFields._reportingProgress | _smallFields._settingInitialProgress)
                    {
                        spinner.SpinOnce();
                    }
                }

                void IProgressListener.ResolveOrSetProgress(PromiseRef sender, Fixed32 progress)
                {
                    Release();
                }

                void IProgressListener.CancelProgress()
                {
                    _smallFields._currentProgress.InterlockedMaybeMakeNegativeIfWholeIsNotGreater();
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
                    return owner._smallFields._waitDepthAndProgress.GetIncrementedWholeTruncated().ToPositiveUInt32() - _smallFields._currentProgress.ToPositiveUInt32();
                }

                [MethodImpl(InlineOption)]
                partial void ResetProgress()
                {
                    _smallFields._currentProgress = default(Fixed32);
                }
            } // PromisePassThrough
#endif // PROMISE_PROGRESS
        } // PromiseRef
    } // Internal
}