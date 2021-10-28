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
using System.Threading;

namespace Proto.Promises
{
    partial class Internal
    {
        static partial void ExecuteProgress(ValueLinkedStack<PromiseRef.IProgressInvokable> executionStack);

#if PROMISE_PROGRESS
        partial class SynchronizationHandler : ILinked<PromiseRef.IProgressInvokable>
        {
            PromiseRef.IProgressInvokable ILinked<PromiseRef.IProgressInvokable>.Next { get; set; }

            // This must not be readonly.
            private ValueWriteOnlyLinkedQueue<PromiseRef.IProgressInvokable> _progressQueue;

            partial void InitProgress()
            {
                _progressQueue = new ValueWriteOnlyLinkedQueue<PromiseRef.IProgressInvokable>(this);
            }

            internal void PostProgress(PromiseRef.IProgressInvokable progressInvokable)
            {
                _locker.Enter();
                bool wasEmpty = _progressQueue.IsEmpty;
                _progressQueue.Enqueue(progressInvokable);
                _locker.Exit();

                if (wasEmpty)
                {
                    _context.Post(_synchronizationContextCallback, this);
                }
            }

            partial void TakeProgress(ref ValueLinkedStack<PromiseRef.IProgressInvokable> progressStack)
            {
                progressStack = _progressQueue.MoveElementsToStack();
            }
        }

        static partial void ExecuteProgress(ValueLinkedStack<PromiseRef.IProgressInvokable> executionStack)
        {
            ValueLinkedQueue<PromiseRef.IProgressInvokable> executionQueue = new ValueLinkedQueue<PromiseRef.IProgressInvokable>();
            while (executionStack.IsNotEmpty)
            {
                do
                {
                    executionStack.Pop().Invoke(ref executionQueue);
                } while (executionStack.IsNotEmpty);
                executionStack = executionQueue.MoveElementsToStack();
            }
        }

        [MethodImpl(InlineOption)]
        private static void ExecuteProgress(ValueLinkedQueue<PromiseRef.IProgressInvokable> executionQueue)
        {
            ExecuteProgress(executionQueue.MoveElementsToStack());
        }
#endif
        partial class PromiseRef
        {
            internal partial interface IProgressListener { }

            // Calls to these get compiled away when PROGRESS is undefined.
            partial void WaitWhileProgressFlags(ProgressFlags progressFlags);

            partial class PromiseMultiAwait
            {
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

#if !PROMISE_PROGRESS
            partial void HandleProgressListener(Promise.State state);

            [MethodImpl(InlineOption)]
            protected void Reset(int depth)
            {
                Reset();
            }
#else

            partial struct SmallFields
            {
                partial struct StateAndFlags
                {
                    [MethodImpl(InlineOption)]
                    internal ProgressFlags InterlockedSetSubscribedIfSecondPrevious()
                    {
                        Thread.MemoryBarrier();
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
                        Thread.MemoryBarrier();
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
                        Thread.MemoryBarrier();
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

            private void SubscribeListener(IProgressListener progressListener, Fixed32 depthAndProgress)
            {
                PromiseRef current = this;
                current.InterlockedRetainDisregardId(); // this retain is redundant for the loop logic to work easier.
                while (true)
                {
                    IProgressListener currentListener = progressListener;
                    PromiseRef previous = current.MaybeAddProgressListenerAndGetPreviousRetained(ref progressListener, ref depthAndProgress);
                    if (previous == null)
                    {
                        var executionQueue = new ValueLinkedQueue<IProgressInvokable>();
                        current._smallFields._stateAndFlags.InterlockedSetProgressFlags(ProgressFlags.SettingInitial);
                        current.SetInitialProgress(currentListener, depthAndProgress, ref executionQueue);
                        current._smallFields._stateAndFlags.InterlockedUnsetProgressFlags(ProgressFlags.SettingInitial);
                        current.MaybeDispose();
                        ExecuteProgress(executionQueue);
                        return;
                    }
                    current.MaybeDispose();
                    current = previous;
                }
            }

            protected virtual PromiseRef MaybeAddProgressListenerAndGetPreviousRetained(ref IProgressListener progressListener, ref Fixed32 lastKnownProgress)
            {
                ThrowIfInPool(this);
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

            protected virtual void SetInitialProgress(IProgressListener progressListener, Fixed32 lastKnownProgress, ref ValueLinkedQueue<IProgressInvokable> executionQueue) { }

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
                    | state == Promise.State.Rejected;
            }

            [MethodImpl(InlineOption)]
            protected void InterlockedRetainDisregardId()
            {
                ThrowIfInPool(this);
                _idsAndRetains.InterlockedRetainDisregardId();
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
                    Thread.MemoryBarrier();
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
                    Thread.MemoryBarrier();
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
                    Thread.MemoryBarrier();
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
                    Thread.MemoryBarrier();
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
#if PROMISE_DEBUG
                    checked
#endif
                    {
                        int newValue = (_value & WholeMask & PositiveMask) + (1 << Promise.Config.ProgressDecimalBits);
                        return new Fixed32(newValue, true);
                    }
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
                void Invoke(ref ValueLinkedQueue<IProgressInvokable> executionQueue);
            }

            partial interface IProgressListener : ILinked<IProgressListener>
            {
                void SetInitialProgress(PromiseRef sender, Promise.State state, Fixed32 progress, ref ValueLinkedQueue<IProgressInvokable> executionQueue);
                void SetProgress(PromiseRef sender, Fixed32 progress, out PromiseSingleAwaitWithProgress nextRef, ref ValueLinkedQueue<IProgressInvokable> executionQueue);
                void ResolveOrSetProgress(PromiseRef sender, Fixed32 progress, ref ValueLinkedQueue<IProgressInvokable> executionQueue);
                void CancelProgress();
                void Retain();
            }

            partial interface IMultiTreeHandleable
            {
                void IncrementProgress(uint increment, Fixed32 senderAmount, Fixed32 ownerAmount, ref ValueLinkedQueue<IProgressInvokable> executionQueue);
            }

            private static readonly WaitCallback _progressThreadPoolCallback = ExecuteProgressFromContext;
            private static readonly SendOrPostCallback _progressSynchronizationContextCallback = ExecuteProgressFromContext;

            private static void ExecuteProgressFromContext(object state)
            {
                ValueLinkedQueue<IProgressInvokable> executionQueue = new ValueLinkedQueue<IProgressInvokable>();
                ((IProgressInvokable) state).Invoke(ref executionQueue);
                ExecuteProgress(executionQueue);
            }


#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal sealed partial class PromiseProgress<TProgress> : PromiseSingleAwaitWithProgress, IProgressListener, IProgressInvokable, ICancelDelegate, ITreeHandleable
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

                internal static PromiseProgress<TProgress> GetOrCreate(TProgress progress, CancelationToken cancelationToken, int depth, bool isSynchronous, SynchronizationContext synchronizationContext)
                {
                    var promise = ObjectPool<ITreeHandleable>.TryTake<PromiseProgress<TProgress>>()
                        ?? new PromiseProgress<TProgress>();
                    promise.Reset();
                    promise._progress = progress;
                    promise.IsComplete = false;
                    promise.IsCanceled = false;
                    promise._smallProgressFields._currentProgress = default(Fixed32);
                    promise._smallProgressFields._depthAndProgress = new Fixed32(depth);
                    promise._smallProgressFields._isSynchronous = isSynchronous;
                    promise._synchronizationContext = synchronizationContext; // TODO
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

                private void ScheduleProgressOnContext()
                {
                    var foregroundHandler = _foregroundSynchronizationHandler;
                    if (foregroundHandler != null && foregroundHandler._context == _synchronizationContext)
                    {
                        foregroundHandler.PostProgress(this);
                    }
                    else if (_synchronizationContext != null)
                    {
                        _synchronizationContext.Post(_progressSynchronizationContextCallback, this);
                    }
                    else
                    {
                        ThreadPool.QueueUserWorkItem(_progressThreadPoolCallback, this);
                    }
                }

                private void ScheduleCompleteOnContext()
                {
                    var foregroundHandler = _foregroundSynchronizationHandler;
                    if (foregroundHandler != null && foregroundHandler._context == _synchronizationContext)
                    {
                        foregroundHandler.PostHandleable(this);
                    }
                    else if (_synchronizationContext != null)
                    {
                        _synchronizationContext.Post(_synchronizationContextCallback, this);
                    }
                    else
                    {
                        ThreadPool.QueueUserWorkItem(_threadPoolCallback, this);
                    }
                }

                void IProgressInvokable.Invoke(ref ValueLinkedQueue<IProgressInvokable> executionQueue)
                {
                    ThrowIfInPool(this);
                    // Calculate the normalized progress for the depth that the listener was added.
                    // Use double for better precision.
                    double expected = _smallProgressFields._depthAndProgress.WholePart + 1u;
                    float value = (float) (_smallProgressFields._currentProgress.ToDouble() / expected);
                    _smallFields._stateAndFlags.InterlockedUnsetProgressFlags(ProgressFlags.InProgressQueue);
                    bool _, isCancelationRequested;
                    _cancelationRegistration.GetIsRegisteredAndIsCancelationRequested(out _, out isCancelationRequested);
                    if (value >= 0 & !IsComplete & !IsCanceled & !isCancelationRequested)
                    {
                        CallbackHelper.InvokeAndCatchProgress(ref _progress, value, this);
                    }
                    MaybeDispose();
                }

                private void SetProgress(Fixed32 progress, ref ValueLinkedQueue<IProgressInvokable> executionQueue)
                {
                    if (_smallProgressFields._currentProgress.InterlockedTrySetIfNotNegativeAndWholeIsGreater(progress) & !IsComplete & !IsCanceled)
                    {
                        if ((_smallFields._stateAndFlags.InterlockedSetProgressFlags(ProgressFlags.InProgressQueue) & ProgressFlags.InProgressQueue) == 0) // Was not already in progress queue?
                        {
                            InterlockedRetainDisregardId();
                            if (_smallProgressFields._isSynchronous)
                            {
                                executionQueue.Enqueue(this);
                            }
                            else
                            {
                                ScheduleProgressOnContext();
                            }
                            //AddToBackOfProgressQueue(this);
                        }
                    }
                }

                void IProgressListener.SetProgress(PromiseRef sender, Fixed32 progress, out PromiseSingleAwaitWithProgress nextRef, ref ValueLinkedQueue<IProgressInvokable> executionQueue)
                {
                    ThrowIfInPool(this);
                    SetProgress(progress, ref executionQueue);
                    nextRef = this;
                }

                void IProgressListener.ResolveOrSetProgress(PromiseRef sender, Fixed32 progress, ref ValueLinkedQueue<IProgressInvokable> executionQueue)
                {
                    ThrowIfInPool(this);
                    if (!IsComplete)
                    {
                        SetProgress(progress, ref executionQueue);
                    }
                    MaybeDispose();
                }

                void IProgressListener.SetInitialProgress(PromiseRef sender, Promise.State state, Fixed32 progress, ref ValueLinkedQueue<IProgressInvokable> executionQueue)
                {
                    ThrowIfInPool(this);
                    switch (state)
                    {
                        case Promise.State.Pending:
                        {
                            if (!sender.GetIsProgressSuspended() && TrySetInitialProgressAndMarkInQueue(progress))
                            {
                                InterlockedRetainDisregardId();
                                if (_smallProgressFields._isSynchronous)
                                {
                                    executionQueue.Enqueue(this);
                                }
                                else
                                {
                                    ScheduleProgressOnContext();
                                }
                                //AddToBackOfProgressQueue(this);
                            }
                            break;
                        }
                        case Promise.State.Resolved:
                        {
                            if (sender != _valueOrPrevious && TrySetInitialProgressAndMarkInQueue(progress))
                            {
                                if (_smallProgressFields._isSynchronous)
                                {
                                    executionQueue.Enqueue(this);
                                }
                                else
                                {
                                    ScheduleProgressOnContext();
                                }
                                //AddToBackOfProgressQueue(this);
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
                    MaybeDispose();
                }

                protected override void SetInitialProgress(IProgressListener progressListener, Fixed32 progress, ref ValueLinkedQueue<IProgressInvokable> executionQueue)
                {
                    ThrowIfInPool(this);
                    Promise.State state = State;
                    if (state == Promise.State.Pending & !IsComplete & !IsCanceled)
                    {
                        if (_progressListener == progressListener)
                        {
                            progressListener.SetInitialProgress(this, state, _smallProgressFields._currentProgress, ref executionQueue);
                        }
                    }
                    else
                    {
                        if (Interlocked.CompareExchange(ref _progressListener, null, progressListener) == progressListener)
                        {
                            progressListener.SetInitialProgress(this, Promise.State.Canceled, _smallProgressFields._depthAndProgress.GetIncrementedWholeTruncated(), ref executionQueue);
                        }
                    }
                }

                public override void Handle(ref ValueLinkedStack<ITreeHandleable> executionStack)
                {
                    ThrowIfInPool(this);
                    bool notCanceled = TryUnregisterAndIsNotCanceling(ref _cancelationRegistration) & !IsCanceled;

                    // HandleSelf
                    IValueContainer valueContainer = (IValueContainer) _valueOrPrevious;
                    Promise.State state = valueContainer.GetState();
                    State = state;
                    if (state == Promise.State.Resolved & notCanceled)
                    {
                        CallbackHelper.InvokeAndCatchProgress(ref _progress, 1f, this);
                    }
                    HandleWaiter(valueContainer, ref executionStack);
                    HandleProgressListener(state);

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
                protected override PromiseRef MaybeAddProgressListenerAndGetPreviousRetained(ref IProgressListener progressListener, ref Fixed32 lastKnownProgress)
                {
                    ThrowIfInPool(this);
                    progressListener.Retain();
                    _progressListener = progressListener;
                    //lastKnownProgress = _smallProgressFields._depthAndProgress; // Unnecessary to set last known since we know SetInitialProgress will be called on this.
                    return null;
                }

                internal override void HandleProgressListener(Promise.State state)
                {
                    HandleProgressListener(state, _smallProgressFields._depthAndProgress.GetIncrementedWholeTruncated());
                }

                internal override void Forget(short promiseId)
                {
                    IncrementId(promiseId);
                    WasAwaitedOrForgotten = true;
                    if (!_smallProgressFields._isSynchronous & IsComplete)
                    {
                        ScheduleCompleteOnContext();
                    }
                    MaybeDispose();
                }

                internal override void AddWaiter(ITreeHandleable waiter, ref ValueLinkedStack<ITreeHandleable> executionStack)
                {
                    ThrowIfInPool(this);
                    // When this is completed, State is set then _waiter is swapped, so we must reverse that process here.
                    _waiter = waiter;
                    Thread.MemoryBarrier(); // Make sure State and _isPreviousComplete are read after _waiter is written.
                    if (State != Promise.State.Pending)
                    {
                        // Exchange and check for null to handle race condition with HandleWaiter on another thread.
                        waiter = Interlocked.Exchange(ref _waiter, null);
                        if (waiter != null)
                        {
                            waiter.MakeReadyFromSettled(this, (IValueContainer) _valueOrPrevious, ref executionStack);
                        }
                    }
                    // It is only possible to not be pending here if this is configured to run synchronously.
                    // But it is still possible to be pending here either way, so we need to check.
                    else if (!_smallProgressFields._isSynchronous & IsComplete)
                    {
                        ScheduleCompleteOnContext();
                    }
                    MaybeDispose();
                }

                void ITreeHandleable.MakeReady(PromiseRef owner, IValueContainer valueContainer, ref ValueLinkedStack<ITreeHandleable> executionStack)
                {
                    ThrowIfInPool(this);
                    IsComplete = true;
                    owner.SuppressRejection = true;
                    valueContainer.Retain();
                    _valueOrPrevious = valueContainer;
                    Thread.MemoryBarrier(); // Make sure _waiter is read after IsComplete is written.
                    // If not synchronous, leave pending until this is awaited or forgotten.
                    if (_smallProgressFields._isSynchronous)
                    {
                        executionStack.Push(this);
                    }
                    else if (_waiter != null | WasAwaitedOrForgotten)
                    {
                        ScheduleCompleteOnContext();
                    }
                    //AddToHandleQueueFront(this);
                    WaitWhileProgressFlags(ProgressFlags.Subscribing);
                }

                void ITreeHandleable.MakeReadyFromSettled(PromiseRef owner, IValueContainer valueContainer, ref ValueLinkedStack<ITreeHandleable> executionStack)
                {
                    ThrowIfInPool(this);
                    IsComplete = true;
                    owner.SuppressRejection = true;
                    valueContainer.Retain();
                    _valueOrPrevious = valueContainer;
                    // If not synchronous, leave pending until this is awaited or forgotten.
                    if (_smallProgressFields._isSynchronous)
                    {
                        State = valueContainer.GetState();
                        _idsAndRetains.InterlockedTryReleaseComplete();
                    }
                }
            } // PromiseProgress<TProgress>

            partial class PromiseSingleAwait
            {
                internal virtual void HandleProgressListener(Promise.State state) { }
            }

            partial class PromiseSingleAwaitWithProgress
            {
                protected void SetInitialProgress(IProgressListener progressListener, Fixed32 currentProgress, Fixed32 expectedProgress, ref ValueLinkedQueue<IProgressInvokable> executionQueue)
                {
                    ThrowIfInPool(this);
                    Promise.State state = State;
                    if (state == Promise.State.Pending)
                    {
                        if (_progressListener == progressListener)
                        {
                            progressListener.SetInitialProgress(this, state, currentProgress, ref executionQueue);
                        }
                        return;
                    }
                    if (Interlocked.CompareExchange(ref _progressListener, null, progressListener) == progressListener)
                    {
                        progressListener.SetInitialProgress(this, state, expectedProgress, ref executionQueue);
                    }
                }

                protected void HandleProgressListener(Promise.State state, Fixed32 progress)
                {
                    IProgressListener progressListener = Interlocked.Exchange(ref _progressListener, null);
                    if (progressListener != null)
                    {
                        WaitWhileProgressFlags(ProgressFlags.Reporting | ProgressFlags.SettingInitial);
                        if (state == Promise.State.Resolved)
                        {
                            var executionQueue = new ValueLinkedQueue<IProgressInvokable>();
                            progressListener.ResolveOrSetProgress(this, progress, ref executionQueue);
                            ExecuteProgress(executionQueue);
                        }
                        else
                        {
                            progressListener.CancelProgress();
                        }
                    }
                }

                internal void ReportProgress(Fixed32 progress, ref ValueLinkedQueue<IProgressInvokable> executionQueue)
                {
                    PromiseSingleAwaitWithProgress setter = this;
                    do
                    {
                        if ((setter._smallFields._stateAndFlags.InterlockedSetProgressFlags(ProgressFlags.Reporting) & ProgressFlags.Reporting) != 0)
                        {
                            break;
                        }
                        PromiseSingleAwaitWithProgress unsetter = setter;

                        IProgressListener progressListener = setter._progressListener;
                        if (progressListener != null)
                        {
                            progressListener.SetProgress(this, progress, out setter, ref executionQueue);
                        }
                        else
                        {
                            setter = null;
                        }
                        unsetter._smallFields._stateAndFlags.InterlockedUnsetProgressFlags(ProgressFlags.Reporting);
                    } while (setter != null);
                }
            } // PromiseSingleAwaitWithProgress

            partial class PromiseMultiAwait : IProgressInvokable
            {
                [MethodImpl(InlineOption)]
                private void Reset(int depth)
                {
                    _progressAndLocker._currentProgress = default(Fixed32);
                    _progressAndLocker._depthAndProgress = new Fixed32(depth);
                    Reset();
                }

                protected override PromiseRef MaybeAddProgressListenerAndGetPreviousRetained(ref IProgressListener progressListener, ref Fixed32 lastKnownProgress)
                {
                    ThrowIfInPool(this);
                    progressListener.Retain();
                    lastKnownProgress = _progressAndLocker._currentProgress;
                    bool notSubscribed = (_smallFields._stateAndFlags.InterlockedSetProgressFlags(ProgressFlags.SelfSubscribed) & ProgressFlags.SelfSubscribed) == 0;
                    _progressAndLocker._progressCollectionLocker.Enter();
                    _progressListeners.Enqueue(progressListener);
                    _progressAndLocker._progressCollectionLocker.Exit();

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

                protected override void SetInitialProgress(IProgressListener progressListener, Fixed32 lastKnownProgress, ref ValueLinkedQueue<IProgressInvokable> executionQueue)
                {
                    ThrowIfInPool(this);
                    Promise.State state = State;
                    if (state == Promise.State.Pending)
                    {
                        _progressAndLocker._progressCollectionLocker.Enter();
                        bool contained = _progressListeners.Contains(progressListener);
                        _progressAndLocker._progressCollectionLocker.Exit();

                        if (contained)
                        {
                            progressListener.SetInitialProgress(this, state, _progressAndLocker._currentProgress, ref executionQueue);
                        }
                        return;
                    }

                    _progressAndLocker._progressCollectionLocker.Enter();
                    bool removed = _progressListeners.TryRemove(progressListener);
                    _progressAndLocker._progressCollectionLocker.Exit();

                    if (removed)
                    {
                        progressListener.SetInitialProgress(this, state, _progressAndLocker._depthAndProgress.GetIncrementedWholeTruncated(), ref executionQueue);
                    }
                }

                partial void HandleProgressListeners(Promise.State state)
                {
                    _progressAndLocker._progressCollectionLocker.Enter();
                    var progressListeners = _progressListeners.MoveElementsToStack();
                    _progressAndLocker._progressCollectionLocker.Exit();

                    if (progressListeners.IsEmpty)
                    {
                        return;
                    }
                    WaitWhileProgressFlags(ProgressFlags.Reporting | ProgressFlags.SettingInitial);

                    if (state == Promise.State.Resolved)
                    {
                        Fixed32 progress = _progressAndLocker._depthAndProgress.GetIncrementedWholeTruncated();
                        var executionQueue = new ValueLinkedQueue<IProgressInvokable>();
                        do
                        {
                            progressListeners.Pop().ResolveOrSetProgress(this, progress, ref executionQueue);
                        } while (progressListeners.IsNotEmpty);
                        ExecuteProgress(executionQueue);
                        return;
                    }

                    do
                    {
                        progressListeners.Pop().CancelProgress();
                    } while (progressListeners.IsNotEmpty);
                }

                private void SetProgress(Fixed32 progress, ref ValueLinkedQueue<IProgressInvokable> executionQueue)
                {
                    if (_progressAndLocker._currentProgress.InterlockedTrySetIfNotNegativeAndWholeIsGreater(progress)
                        && (_smallFields._stateAndFlags.InterlockedSetProgressFlags(ProgressFlags.InProgressQueue) & ProgressFlags.InProgressQueue) == 0) // Was not already in progress queue?
                    {
                        InterlockedRetainDisregardId();
                        executionQueue.Push(this);
                    }
                }

                void IProgressListener.CancelProgress()
                {
                    _progressAndLocker._currentProgress.InterlockedMaybeMakeNegativeIfWholeIsNotGreater();
                    MaybeDispose();
                }

                void IProgressListener.ResolveOrSetProgress(PromiseRef sender, Fixed32 progress, ref ValueLinkedQueue<IProgressInvokable> executionQueue)
                {
                    ThrowIfInPool(this);
                    SetProgress(progress, ref executionQueue);
                    MaybeDispose();
                }

                void IProgressListener.SetInitialProgress(PromiseRef sender, Promise.State state, Fixed32 progress, ref ValueLinkedQueue<IProgressInvokable> executionQueue)
                {
                    ThrowIfInPool(this);
                    switch (state)
                    {
                        case Promise.State.Pending:
                        {
                            if (!sender.GetIsProgressSuspended() && TrySetInitialProgressAndMarkInQueue(progress))
                            {
                                InterlockedRetainDisregardId();
                                executionQueue.Push(this);
                            }
                            break;
                        }
                        case Promise.State.Resolved:
                        {
                            if (sender != _valueOrPrevious && TrySetInitialProgressAndMarkInQueue(progress))
                            {
                                executionQueue.Push(this);
                                break; // Break instead of InterlockedRetainDisregardId().
                            }
                            MaybeDispose();
                            break;
                        }
                        default: // Rejected or Canceled:
                        {
                            _progressAndLocker._currentProgress.MaybeMakeNegative();
                            MaybeDispose();
                            break;
                        }
                    }
                }

                private bool TrySetInitialProgressAndMarkInQueue(Fixed32 progress)
                {
                    return _progressAndLocker._currentProgress.TrySetIfZeroOrWasPositive(progress)
                        && (_smallFields._stateAndFlags.InterlockedSetProgressFlags(ProgressFlags.InProgressQueue) & ProgressFlags.InProgressQueue) == 0; // Was not already in progress queue?
                }

                void IProgressListener.SetProgress(PromiseRef sender, Fixed32 progress, out PromiseSingleAwaitWithProgress nextRef, ref ValueLinkedQueue<IProgressInvokable> executionQueue)
                {
                    ThrowIfInPool(this);
                    SetProgress(progress, ref executionQueue);
                    nextRef = null;
                }

                void IProgressListener.Retain()
                {
                    ThrowIfInPool(this);
                    InterlockedRetainDisregardId();
                }

                void IProgressInvokable.Invoke(ref ValueLinkedQueue<IProgressInvokable> executionQueue)
                {
                    ThrowIfInPool(this);
                    Thread.MemoryBarrier(); // Make sure we're reading fresh progress (since the field cannot be marked volatile).
                    var progress = _progressAndLocker._currentProgress;
                    _smallFields._stateAndFlags.InterlockedUnsetProgressFlags(ProgressFlags.InProgressQueue);
                    if (!progress.IsNegative) // Was it not suspended?
                    {
                        // Lock is necessary for race condition with Handle.
                        // TODO: refactor to remove the need for a lock here.
                        _progressAndLocker._progressCollectionLocker.Enter();
                        foreach (var progressListener in _progressListeners)
                        {
                            PromiseSingleAwaitWithProgress nextRef;
                            progressListener.SetProgress(this, progress, out nextRef, ref executionQueue);
                            if (nextRef != null)
                            {
                                nextRef.ReportProgress(progress, ref executionQueue);
                            }
                        }
                        _progressAndLocker._progressCollectionLocker.Exit();
                    }
                    MaybeDispose();
                }

                [MethodImpl(InlineOption)]
                protected override sealed bool GetIsProgressSuspended()
                {
                    return _progressAndLocker._currentProgress.IsNegative;
                }
            } // PromiseMultiAwait

            partial class AsyncPromiseBase
            {
                [MethodImpl(InlineOption)]
                new protected void Reset()
                {
                    _currentProgress = default(Fixed32);
                    base.Reset();
                }

                protected override sealed PromiseRef MaybeAddProgressListenerAndGetPreviousRetained(ref IProgressListener progressListener, ref Fixed32 lastKnownProgress)
                {
                    ThrowIfInPool(this);
                    progressListener.Retain();
                    lastKnownProgress = _currentProgress;
                    _progressListener = progressListener;
                    return base.MaybeAddProgressListenerAndGetPreviousRetained(ref progressListener, ref lastKnownProgress);
                }

                protected override sealed void SetInitialProgress(IProgressListener progressListener, Fixed32 lastKnownProgress, ref ValueLinkedQueue<IProgressInvokable> executionQueue)
                {
                    SetInitialProgress(progressListener, _currentProgress, _currentProgress.GetIncrementedWholeTruncated(), ref executionQueue);
                }

                internal override sealed void HandleProgressListener(Promise.State state)
                {
                    HandleProgressListener(state, _currentProgress.GetIncrementedWholeTruncated());
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
                        var newProgress = _currentProgress.SetNewDecimalPart(progress);
                        var executionQueue = new ValueLinkedQueue<IProgressInvokable>();
                        ReportProgress(newProgress, ref executionQueue);
                        ExecuteProgress(executionQueue);
                    }
                    MaybeDispose();
                    return true;
                }
            }

            partial class PromiseWaitPromise : IProgressInvokable
            {
                [MethodImpl(InlineOption)]
                protected void Reset(int depth)
                {
                    _progressFields._depthAndProgress = new Fixed32(depth);
                    Reset();
                }

                [MethodImpl(InlineOption)]
                protected override sealed bool GetIsProgressSuspended()
                {
                    return _progressFields._depthAndProgress.IsNegative;
                }

                partial void SubscribeProgressToOther(PromiseRef other, int depth)
                {
                    _progressFields._previousDepthPlusOne = depth + 1;
                    // Lazy subscribe: only subscribe to second previous if a progress listener is added to this (this keeps execution more efficient when progress isn't used).
                    bool hasListener = _progressListener != null;
                    ProgressFlags subscribedFlag = hasListener ? ProgressFlags.SelfSubscribed : ProgressFlags.None;
                    ProgressFlags oldFlags = _smallFields._stateAndFlags.InterlockedSetProgressFlags(ProgressFlags.SecondPrevious | subscribedFlag);
                    if (hasListener & (oldFlags & ProgressFlags.SelfSubscribed) == 0) // Has listener and was not already subscribed?
                    {
                        other.SubscribeListener(this, new Fixed32(depth));
                    }
                }

                internal void WaitForWithprogress<T>(Promise<T> other)
                {
                    ThrowIfInPool(this);
                    var executionStack = new ValueLinkedStack<ITreeHandleable>();

                    var _ref = other._ref;
                    _ref.MarkAwaited(other.Id);
                    _valueOrPrevious = _ref;
                    SubscribeProgressToOther(_ref, other.Depth);
                    _ref.AddWaiter(this, ref executionStack);

                    ExecuteHandlers(executionStack);
                }

                protected override PromiseRef MaybeAddProgressListenerAndGetPreviousRetained(ref IProgressListener progressListener, ref Fixed32 lastKnownProgress)
                {
                    ThrowIfInPool(this);
                    progressListener.Retain();
                    _progressListener = progressListener;
                    ProgressFlags oldFlags = _smallFields._stateAndFlags.InterlockedSetSubscribedIfSecondPrevious();
                    bool secondPrevious = (oldFlags & ProgressFlags.SecondPrevious) != 0;
                    bool secondSubscribed = (oldFlags & ProgressFlags.SelfSubscribed) != 0;
                    if (secondPrevious) // Are we waiting on second previous?
                    {
                        lastKnownProgress = new Fixed32(_progressFields._previousDepthPlusOne - 1);
                        if (secondSubscribed) // Was already subscribed?
                        {
                            return null;
                        }
                        progressListener = this;
                    }
                    else
                    {
                        lastKnownProgress = _progressFields._depthAndProgress;
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

                protected override sealed void SetInitialProgress(IProgressListener progressListener, Fixed32 lastKnownProgress, ref ValueLinkedQueue<IProgressInvokable> executionQueue)
                {
                    SetInitialProgress(progressListener, _progressFields._depthAndProgress, _progressFields._depthAndProgress.GetIncrementedWholeTruncated(), ref executionQueue);
                }

                void IProgressListener.SetInitialProgress(PromiseRef sender, Promise.State state, Fixed32 progress, ref ValueLinkedQueue<IProgressInvokable> executionQueue)
                {
                    ThrowIfInPool(this);
                    switch (state)
                    {
                        case Promise.State.Pending:
                        {
                            if (!sender.GetIsProgressSuspended() && TrySetInitialProgressAndMarkInQueue(progress))
                            {
                                InterlockedRetainDisregardId();
                                executionQueue.Push(this);
                            }
                            break;
                        }
                        case Promise.State.Resolved:
                        {
                            if (sender != _valueOrPrevious && TrySetInitialProgressAndMarkInQueue(progress))
                            {
                                executionQueue.Push(this);
                                break; // Break instead of InterlockedRetainDisregardId().
                            }
                            MaybeDispose();
                            break;
                        }
                        default: // Rejected or Canceled:
                        {
                            _progressFields._depthAndProgress.MaybeMakeNegative();
                            MaybeDispose();
                            break;
                        }
                    }
                }

                private bool TrySetInitialProgressAndMarkInQueue(Fixed32 progress)
                {
                    bool isNotSuspended;
                    _progressFields._depthAndProgress.SetNewDecimalPartIfNotNegativeAndWhole(NormalizeProgress(progress), out isNotSuspended);
                    return isNotSuspended
                        && (_smallFields._stateAndFlags.InterlockedSetProgressFlags(ProgressFlags.InProgressQueue) & ProgressFlags.InProgressQueue) == 0; // Was not already in progress queue?
                }

                private double NormalizeProgress(Fixed32 progress)
                {
                    // Calculate the normalized progress for the depth of the returned promise.
                    // Use double for better precision.
                    return progress.ToDouble() / (double) _progressFields._previousDepthPlusOne;
                }

                private void SetProgressAndMaybeAddToQueue(Fixed32 progress, ref ValueLinkedQueue<IProgressInvokable> executionQueue)
                {
                    _progressFields._depthAndProgress.InterlockedSetNewDecimalPartIfNotNegativeAndDecimalIsGreater(NormalizeProgress(progress));
                    if ((_smallFields._stateAndFlags.InterlockedSetProgressFlags(ProgressFlags.InProgressQueue) & ProgressFlags.InProgressQueue) == 0) // Was not already in progress queue?
                    {
                        InterlockedRetainDisregardId();
                        executionQueue.Push(this);
                    }
                }

                void IProgressListener.SetProgress(PromiseRef sender, Fixed32 progress, out PromiseSingleAwaitWithProgress nextRef, ref ValueLinkedQueue<IProgressInvokable> executionQueue)
                {
                    ThrowIfInPool(this);
                    SetProgressAndMaybeAddToQueue(progress, ref executionQueue);
                    nextRef = null;
                }

                void IProgressListener.ResolveOrSetProgress(PromiseRef sender, Fixed32 progress, ref ValueLinkedQueue<IProgressInvokable> executionQueue)
                {
                    ThrowIfInPool(this);
                    // Don't set progress if this is resolved by the second wait.
                    // Have to check the value's type since MakeReady is called before this.
                    if (!(_valueOrPrevious is IValueContainer))
                    {
                        SetProgressAndMaybeAddToQueue(progress, ref executionQueue);
                    }
                    MaybeDispose();
                }

                void IProgressListener.CancelProgress()
                {
                    _progressFields._depthAndProgress.InterlockedMaybeMakeNegativeIfDecimalIsNotGreater();
                    MaybeDispose();
                }

                void IProgressListener.Retain()
                {
                    InterlockedRetainDisregardId();
                }

                void IProgressInvokable.Invoke(ref ValueLinkedQueue<IProgressInvokable> executionQueue)
                {
                    Thread.MemoryBarrier(); // Make sure we're reading fresh progress (since the field cannot be marked volatile).
                    var progress = _progressFields._depthAndProgress;
                    _smallFields._stateAndFlags.InterlockedUnsetProgressFlags(ProgressFlags.InProgressQueue);
                    if (!progress.IsNegative) // Was it not suspended?
                    {
                        ReportProgress(progress, ref executionQueue);
                    }
                    MaybeDispose();
                }

                internal override sealed void HandleProgressListener(Promise.State state)
                {
                    HandleProgressListener(state, _progressFields._depthAndProgress.GetIncrementedWholeTruncated());
                }
            } // PromiseWaitPromise

            partial class PromisePassThrough
            {
                void IProgressListener.SetInitialProgress(PromiseRef sender, Promise.State state, Fixed32 progress, ref ValueLinkedQueue<IProgressInvokable> executionQueue)
                {
                    ThrowIfInPool(this);
                    if (state == Promise.State.Pending)
                    {
                        _smallFields._settingInitialProgress = true;
                        Thread.MemoryBarrier(); // Make sure _owner is read after _settingInitialProgress is written.
                        bool didSet = _smallFields._currentProgress.TrySetIfZero(progress);
                        var owner = _owner;
                        if (didSet & owner != null)
                        {
                            _target.IncrementProgress(progress.ToPositiveUInt32(), progress, _smallFields._depth, ref executionQueue);
                        }
                        _smallFields._settingInitialProgress = false;
                    }
                    else
                    {
                        Release();
                    }
                }

                void IProgressListener.SetProgress(PromiseRef sender, Fixed32 progress, out PromiseSingleAwaitWithProgress nextRef, ref ValueLinkedQueue<IProgressInvokable> executionQueue)
                {
                    ThrowIfInPool(this);
                    _smallFields._reportingProgress = true;
                    Thread.MemoryBarrier(); // Make sure _owner is read after _reportingProgress is written.
                    uint dif;
                    bool didSet = _smallFields._currentProgress.InterlockedTrySetAndGetDifferenceIfNotNegativeAndWholeIsGreater(progress, out dif);
                    var owner = _owner;
                    if (didSet & owner != null)
                    {
                        _target.IncrementProgress(dif, progress, _smallFields._depth, ref executionQueue);
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

                void IProgressListener.ResolveOrSetProgress(PromiseRef sender, Fixed32 progress, ref ValueLinkedQueue<IProgressInvokable> executionQueue)
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
                internal uint GetProgressDifferenceToCompletion()
                {
                    ThrowIfInPool(this);
                    return _smallFields._depth.GetIncrementedWholeTruncated().ToPositiveUInt32() - _smallFields._currentProgress.ToPositiveUInt32();
                }

                [MethodImpl(InlineOption)]
                partial void ResetProgress(int depth)
                {
                    _smallFields._currentProgress = default(Fixed32);
                    _smallFields._depth = new Fixed32(depth);
                }

                internal int Depth
                {
                    [MethodImpl(InlineOption)]
                    get { return _smallFields._depth.WholePart; }
                }
            } // PromisePassThrough
#endif // PROMISE_PROGRESS
        } // PromiseRef
    } // Internal
}