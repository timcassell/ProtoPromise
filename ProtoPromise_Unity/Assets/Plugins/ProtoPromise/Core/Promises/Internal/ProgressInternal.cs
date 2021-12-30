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
#if PROMISE_PROGRESS
        partial class SynchronizationHandler : ILinked<IProgressInvokable>
        {
            IProgressInvokable ILinked<IProgressInvokable>.Next { get; set; }

            // This must not be readonly.
            private ValueWriteOnlyLinkedQueue<IProgressInvokable> _progressQueue;

            partial void InitProgress()
            {
                _progressQueue = new ValueWriteOnlyLinkedQueue<IProgressInvokable>(this);
            }

            internal void PostProgress(IProgressInvokable progressInvokable)
            {
                _locker.Enter();
                bool wasScheduled = _isScheduled;
                _isScheduled = true;
                _progressQueue.Enqueue(progressInvokable);
                _locker.Exit();

                if (!wasScheduled)
                {
                    _context.Post(_synchronizationContextCallback, this);
                }
            }

            partial void TakeProgress(ref ValueLinkedQueue<IProgressInvokable> progressStack)
            {
                progressStack = _progressQueue.MoveElementsToQueue();
            }
        }

        partial struct ExecutionScheduler
        {
            private static readonly WaitCallback _progressThreadPoolCallback = ExecuteProgressFromContext;
            private static readonly SendOrPostCallback _progressSynchronizationContextCallback = ExecuteProgressFromContext;

            partial void ExecuteProgressPartial()
            {
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                _isExecutingProgress = true; // This is only used on the CPU stack, so we never need to set this back to false.
#endif

                // In case this is executed from a background thread, catch the exception and report it instead of crashing the app.
                IProgressInvokable lastExecuted = null;
                try
                {
                    while (_progressQueue.IsNotEmpty)
                    {
                        ValueLinkedStack<IProgressInvokable> executionStack = _progressQueue.MoveElementsToStack();
                        do
                        {
                            lastExecuted = executionStack.Pop();
                            lastExecuted.Invoke(ref this);
                        } while (executionStack.IsNotEmpty);
                    }
                }
                catch (Exception e)
                {
                    // This should never happen.
                    AddRejectionToUnhandledStack(e, lastExecuted as ITraceable);
                }
            }

#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
            partial void AssertNotExecutingProgress()
            {
                if (_isExecutingProgress)
                {
                    throw new System.InvalidOperationException("Cannot schedule handleable while executing progress.");
                }
            }
#endif

            internal void ExecuteProgress()
            {
                ExecuteProgressPartial();
                MaybeReportUnhandledRejections();
            }

            [MethodImpl(InlineOption)]
            internal void ScheduleProgressSynchronous(IProgressInvokable progress)
            {
#if PROTO_PROMISE_DEVELOPER_MODE // Helps to see full causality trace with internal stacktraces in exceptions (may cause StackOverflowException if the chain is very long).
                progress.Invoke(ref this);
#else
                _progressQueue.Enqueue(progress);
#endif
            }

            internal void ScheduleProgressOnContext(SynchronizationContext synchronizationContext, IProgressInvokable progress)
            {
                if (_synchronizationHandler != null && _synchronizationHandler._context == synchronizationContext)
                {
                    // We're scheduling to the context that is currently executing, just place it on the queue instead of going through the context.
                    ScheduleProgressSynchronous(progress);
                    return;
                }
                if (synchronizationContext == null)
                {
                    // If there is no context, send it to the ThreadPool.
                    ThreadPool.QueueUserWorkItem(_progressThreadPoolCallback, progress);
                    return;
                }
                SynchronizationHandler foregroundHandler = _foregroundSynchronizationHandler;
                if (foregroundHandler != null && foregroundHandler._context == synchronizationContext)
                {
                    // Schedule on the optimized foregroundHandler instead of going through the context.
                    foregroundHandler.PostProgress(progress);
                    return;
                }
                synchronizationContext.Post(_progressSynchronizationContextCallback, progress);
            }

            private static void ExecuteProgressFromContext(object state)
            {
                // In case this is executed from a background thread, catch the exception and report it instead of crashing the app.
                try
                {
                    ExecutionScheduler executionScheduler = new ExecutionScheduler(false);
                    ((IProgressInvokable) state).Invoke(ref executionScheduler);
                    executionScheduler.ExecuteProgress();
                }
                catch (Exception e)
                {
                    // This should never happen.
                    AddRejectionToUnhandledStack(e, state as ITraceable);
                }
            }
        }
#endif // PROMISE_PROGRESS

        partial class PromiseRef
        {
            internal partial interface IProgressListener { }

            // Calls to these get compiled away when PROGRESS is undefined.
            partial void WaitWhileProgressFlags(PromiseFlags progressFlags);

            partial class PromiseMultiAwait
            {
                partial void HandleProgressListeners(Promise.State state, ref ExecutionScheduler executionScheduler);
            }

#if !PROMISE_PROGRESS
            partial void HandleProgressListener(Promise.State state, ref ExecutionScheduler executionScheduler);

            [MethodImpl(InlineOption)]
            protected void Reset(int depth)
            {
                Reset();
            }
#else

            partial struct SmallFields
            {
                [MethodImpl(InlineOption)]
                internal PromiseFlags InterlockedSetSubscribedIfSecondPrevious()
                {
                    Thread.MemoryBarrier();
                    SmallFields initialValue = default(SmallFields), newValue;
                    do
                    {
                        initialValue._longValue = Interlocked.Read(ref _longValue);
                        newValue = initialValue;
                        PromiseFlags setFlags = (PromiseFlags) ((byte) (newValue._flags & PromiseFlags.SecondPrevious) << 1); // Change SecondPrevious flag to SecondSubscribed.
                        newValue._flags |= setFlags;
                    } while (Interlocked.CompareExchange(ref _longValue, newValue._longValue, initialValue._longValue) != initialValue._longValue);
                    return initialValue._flags;
                }
            } // SmallFields

            private void SubscribeListener(IProgressListener progressListener, Fixed32 depthAndProgress, ref ExecutionScheduler executionScheduler)
            {
                PromiseRef current = this;
                current.InterlockedRetainDisregardId(); // this retain is redundant for the loop logic to work easier.
                while (true)
                {
                    IProgressListener currentListener = progressListener;
                    PromiseRef previous = current.MaybeAddProgressListenerAndGetPreviousRetained(ref progressListener, ref depthAndProgress);
                    if (previous == null)
                    {
                        current._smallFields.InterlockedSetFlags(PromiseFlags.SettingInitial);
                        current.SetInitialProgress(currentListener, depthAndProgress, ref executionScheduler);
                        current._smallFields.InterlockedUnsetFlags(PromiseFlags.SettingInitial);
                        current.MaybeDispose();
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
                _smallFields.InterlockedSetFlags(PromiseFlags.Subscribing);
                PromiseRef previous = _valueOrPrevious as PromiseRef;
                if (previous != null)
                {
                    previous.InterlockedRetainDisregardId();
                }
                _smallFields.InterlockedUnsetFlags(PromiseFlags.Subscribing);
                return previous;
            }

            protected virtual void SetInitialProgress(IProgressListener progressListener, Fixed32 lastKnownProgress, ref ExecutionScheduler executionScheduler) { }

            partial void WaitWhileProgressFlags(PromiseFlags progressFlags)
            {
                // Wait until progressFlags are unset.
                // This is used to make sure promises and progress listeners aren't disposed while still in use on another thread.
                SpinWait spinner = new SpinWait();
                Thread.MemoryBarrier(); // Make sure any writes happen before we read progress flags.
                while (_smallFields.AreFlagsSet(progressFlags))
                {
                    spinner.SpinOnce();
                }
            }

            protected virtual bool GetIsProgressSuspended()
            {
                var state = _smallFields._state;
                return state == Promise.State.Canceled
                    | state == Promise.State.Rejected;
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
                private const int MaxWholeValue = -(1 << Promise.Config.ProgressDecimalBits) & PositiveMask;

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

                internal bool InterlockedTrySet(Fixed32 other)
                {
                    Thread.MemoryBarrier();
                    int otherValue = other._value;
                    int otherWholePart = other.WholePart;
                    int current;
                    do
                    {
                        current = _value;
                        int currentWholePart = (current & PositiveMask) >> Promise.Config.ProgressDecimalBits; // Make positive before comparing.
                        // Prevents promises from a broken chain from updating progress (cancelations break the chain).
                        if (otherWholePart < currentWholePart
                            // Same thing, but more edge-case. If current is negative, it means this was updated from a canceled or rejected promise, so it can only be further updated by a promise with a higher depth (WholePart).
                            | (current < 0 & otherWholePart == currentWholePart)
                            // Don't bother updating if the values are the same.
                            | current == otherValue)
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

                internal void InterlockedMakeNegativeIfOtherWholeIsGreater(Fixed32 other)
                {
                    Thread.MemoryBarrier();
                    int otherWholePart = other.PositiveWholePart;
                    int oldWholePart = PositiveWholePart;
                    int current;
                    int negated;
                    do
                    {
                        current = _value;
                        int currentWholePart = (current & PositiveMask) >> Promise.Config.ProgressDecimalBits; // Make positive before comparing.
                        // If other whole is less than or equal, or if the updated whole is greater than the old whole, do nothing.
                        if (otherWholePart <= currentWholePart | oldWholePart < currentWholePart)
                        {
                            return;
                        }
                        negated = current | ~PositiveMask;
                    } while (Interlocked.CompareExchange(ref _value, negated, current) != current);
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
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                    // MaxWholeValue allows some promises to start with -1 depth so that the next await will increment it to 0,
                    // but still throws in general use if the promise chain gets too long.
                    if (newValue >= MaxWholeValue)
                    {
                        throw new System.ArgumentOutOfRangeException();
                    }
#endif
                    return new Fixed32(newValue, true);
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

            partial interface IProgressListener : ILinked<IProgressListener>
            {
                void SetInitialProgress(PromiseRef sender, Promise.State state, Fixed32 progress, ref ExecutionScheduler executionScheduler);
                void SetProgress(PromiseRef sender, Fixed32 progress, out PromiseSingleAwaitWithProgress nextRef, ref ExecutionScheduler executionScheduler);
                void ResolveOrSetProgress(PromiseRef sender, Fixed32 progress, ref ExecutionScheduler executionScheduler);
                void MaybeCancelProgress(Fixed32 progress);
                void Retain();
            }

            partial interface IMultiTreeHandleable
            {
                void IncrementProgress(uint increment, Fixed32 senderAmount, Fixed32 ownerAmount, ref ExecutionScheduler executionScheduler);
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

                internal bool IsComplete
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

                internal bool DidFirstInvoke
                {
                    [MethodImpl(InlineOption)]
                    get { return _smallProgressFields._didFirstInvoke; }
                    [MethodImpl(InlineOption)]
                    set { _smallProgressFields._didFirstInvoke = value; }
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
                    promise.DidFirstInvoke = false;
                    promise._smallProgressFields._currentProgress = default(Fixed32);
                    promise._smallProgressFields._depthAndProgress = new Fixed32(depth);
                    promise._smallProgressFields._isSynchronous = isSynchronous;
                    promise._synchronizationContext = synchronizationContext;
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

                void IProgressInvokable.Invoke(ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    // Calculate the normalized progress for the depth that the listener was added.
                    // Use double for better precision.
                    double expected = _smallProgressFields._depthAndProgress.WholePart + 1u;
                    float value = (float) (_smallProgressFields._currentProgress.ToDouble() / expected);
                    _smallFields.InterlockedUnsetFlags(PromiseFlags.InProgressQueue);
                    bool _, isCancelationRequested;
                    _cancelationRegistration.GetIsRegisteredAndIsCancelationRequested(out _, out isCancelationRequested);
                    if (value >= 0 & !IsComplete & !IsCanceled & !isCancelationRequested)
                    {
                        CallbackHelper.InvokeAndCatchProgress(_progress, value, this);
                    }
                    MaybeDispose();
                }

                private void SetProgress(Fixed32 progress, ref ExecutionScheduler executionScheduler)
                {
                    // InterlockedTrySet checks if progress is the same, so when this is called with progress 0 from another Promise for the first time, we need to force the invoke if possible.
                    bool invoked = DidFirstInvoke;
                    DidFirstInvoke = true;
                    bool needsInvoke = _smallProgressFields._currentProgress.InterlockedTrySet(progress) | !invoked;
                    if (needsInvoke & !IsComplete & !IsCanceled)
                    {
                        if ((_smallFields.InterlockedSetFlags(PromiseFlags.InProgressQueue) & PromiseFlags.InProgressQueue) == 0) // Was not already in progress queue?
                        {
                            InterlockedRetainDisregardId();
                            if (_smallProgressFields._isSynchronous)
                            {
                                executionScheduler.ScheduleProgressSynchronous(this);
                            }
                            else
                            {
                                executionScheduler.ScheduleProgressOnContext(_synchronizationContext, this);
                            }
                        }
                    }
                }

                void IProgressListener.SetProgress(PromiseRef sender, Fixed32 progress, out PromiseSingleAwaitWithProgress nextRef, ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    SetProgress(progress, ref executionScheduler);
                    nextRef = this;
                }

                void IProgressListener.ResolveOrSetProgress(PromiseRef sender, Fixed32 progress, ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    if (!IsComplete)
                    {
                        SetProgress(progress, ref executionScheduler);
                    }
                    MaybeDispose();
                }

                void IProgressListener.SetInitialProgress(PromiseRef sender, Promise.State state, Fixed32 progress, ref ExecutionScheduler executionScheduler)
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
                                    executionScheduler.ScheduleProgressSynchronous(this);
                                }
                                else
                                {
                                    executionScheduler.ScheduleProgressOnContext(_synchronizationContext, this);
                                }
                            }
                            break;
                        }
                        case Promise.State.Resolved:
                        {
                            if (sender != _valueOrPrevious && TrySetInitialProgressAndMarkInQueue(progress))
                            {
                                if (_smallProgressFields._isSynchronous)
                                {
                                    executionScheduler.ScheduleProgressSynchronous(this);
                                }
                                else
                                {
                                    executionScheduler.ScheduleProgressOnContext(_synchronizationContext, this);
                                }
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
                        && (_smallFields.InterlockedSetFlags(PromiseFlags.InProgressQueue) & PromiseFlags.InProgressQueue) == 0; // Was not already in progress queue?
                }

                void IProgressListener.MaybeCancelProgress(Fixed32 progress)
                {
                    ThrowIfInPool(this);
                    _smallProgressFields._currentProgress.InterlockedMakeNegativeIfOtherWholeIsGreater(progress);
                    MaybeDispose();
                }

                protected override void SetInitialProgress(IProgressListener progressListener, Fixed32 progress, ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    Promise.State state = State;
                    if (state == Promise.State.Pending & !IsCanceled)
                    {
                        if (_progressListener == progressListener)
                        {
                            progressListener.SetInitialProgress(this, state, _smallProgressFields._currentProgress, ref executionScheduler);
                        }
                    }
                    else
                    {
                        if (Interlocked.CompareExchange(ref _progressListener, null, progressListener) == progressListener)
                        {
                            progressListener.SetInitialProgress(this, Promise.State.Canceled, _smallProgressFields._depthAndProgress.GetIncrementedWholeTruncated(), ref executionScheduler);
                        }
                    }
                }

                public override void Handle(ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    bool notCanceled = TryUnregisterAndIsNotCanceling(ref _cancelationRegistration) & !IsCanceled;

                    // HandleSelf
                    IValueContainer valueContainer = (IValueContainer) _valueOrPrevious;
                    Promise.State state = valueContainer.GetState();
                    if (state == Promise.State.Resolved & notCanceled)
                    {
                        CallbackHelper.InvokeAndCatchProgress(_progress, 1f, this);
                    }
                    State = state; // Set state after callback is executed to make sure it completes before the next waiter begins execution (in another thread).
                    HandleWaiter(valueContainer, ref executionScheduler);
                    HandleProgressListener(state, ref executionScheduler);

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

                internal override void HandleProgressListener(Promise.State state, ref ExecutionScheduler executionScheduler)
                {
                    HandleProgressListener(state, _smallProgressFields._depthAndProgress.GetIncrementedWholeTruncated(), ref executionScheduler);
                }

                internal override void AddWaiter(ITreeHandleable waiter, ref ExecutionScheduler executionScheduler)
                {
#if !CSHARP_7_3_OR_NEWER // Interlocked.Exchange doesn't seem to work properly in Unity's old runtime. I'm not sure why, but we need a lock here to pass multi-threaded tests.
                    lock (this)
#endif
                    {
                        ThrowIfInPool(this);
                        // When this is completed, State is set then _waiter is swapped, so we must reverse that process here.
                        Thread.MemoryBarrier();
                        _waiter = waiter;
                        Thread.MemoryBarrier(); // Make sure State and _isPreviousComplete are read after _waiter is written.
                        if (State != Promise.State.Pending)
                        {
                            // Exchange and check for null to handle race condition with HandleWaiter on another thread.
                            waiter = Interlocked.Exchange(ref _waiter, null);
                            if (waiter != null)
                            {
                                if (_smallProgressFields._isSynchronous)
                                {
                                    waiter.MakeReady(this, (IValueContainer) _valueOrPrevious, ref executionScheduler);
                                }
                                else
                                {
                                    // If this was configured to execute progress on a SynchronizationContext or the ThreadPool, force the waiter to execute on the same context for consistency.

                                    // Taking advantage of an implementation detail that MakeReady will only add itself or nothing to the stack, so we can just send it to the context instead.
                                    // This is better than adding a new method to the interface.
                                    ExecutionScheduler overrideScheduler = executionScheduler.GetEmptyCopy();
                                    waiter.MakeReady(this, (IValueContainer) _valueOrPrevious, ref overrideScheduler);
                                    if (overrideScheduler._handleStack.IsNotEmpty)
                                    {
                                        executionScheduler.ScheduleOnContext(_synchronizationContext, overrideScheduler._handleStack.Pop());
                                    }
    #if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                                    if (overrideScheduler._handleStack.IsNotEmpty)
                                    {
                                        throw new Exception("This should never happen.");
                                    }
    #endif
                                }
                            }
                        }
                        MaybeDispose();
                    }
                }

                void ITreeHandleable.MakeReady(PromiseRef owner, IValueContainer valueContainer, ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    IsComplete = true;
                    valueContainer.Retain();
                    _valueOrPrevious = valueContainer;
                    if (_smallProgressFields._isSynchronous)
                    {
                        executionScheduler.ScheduleSynchronous(this);
                    }
                    else
                    {
                        executionScheduler.ScheduleOnContext(_synchronizationContext, this);
                    }
                    WaitWhileProgressFlags(PromiseFlags.Subscribing);
                }
            } // PromiseProgress<TProgress>

            partial class PromiseSingleAwait
            {
                internal virtual void HandleProgressListener(Promise.State state, ref ExecutionScheduler executionScheduler) { }
            }

            partial class PromiseSingleAwaitWithProgress
            {
                protected void SetInitialProgress(IProgressListener progressListener, Fixed32 currentProgress, Fixed32 expectedProgress, ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    Promise.State state = State;
                    if (state == Promise.State.Pending)
                    {
                        if (_progressListener == progressListener)
                        {
                            progressListener.SetInitialProgress(this, state, currentProgress, ref executionScheduler);
                        }
                        return;
                    }
                    if (Interlocked.CompareExchange(ref _progressListener, null, progressListener) == progressListener)
                    {
                        progressListener.SetInitialProgress(this, state, expectedProgress, ref executionScheduler);
                    }
                }

                protected void HandleProgressListener(Promise.State state, Fixed32 progress, ref ExecutionScheduler executionScheduler)
                {
                    IProgressListener progressListener = Interlocked.Exchange(ref _progressListener, null);
                    if (progressListener != null)
                    {
                        WaitWhileProgressFlags(PromiseFlags.Reporting | PromiseFlags.SettingInitial);
                        if (state == Promise.State.Resolved)
                        {
                            progressListener.ResolveOrSetProgress(this, progress, ref executionScheduler);
                        }
                        else
                        {
                            progressListener.MaybeCancelProgress(progress);
                        }
                    }
                }

                internal void ReportProgress(Fixed32 progress, ref ExecutionScheduler executionScheduler)
                {
                    PromiseSingleAwaitWithProgress setter = this;
                    do
                    {
                        if ((setter._smallFields.InterlockedSetFlags(PromiseFlags.Reporting) & PromiseFlags.Reporting) != 0)
                        {
                            break;
                        }
                        PromiseSingleAwaitWithProgress unsetter = setter;

                        IProgressListener progressListener = setter._progressListener;
                        if (progressListener != null)
                        {
                            progressListener.SetProgress(this, progress, out setter, ref executionScheduler);
                        }
                        else
                        {
                            setter = null;
                        }
                        unsetter._smallFields.InterlockedUnsetFlags(PromiseFlags.Reporting);
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
                    bool notSubscribed = (_smallFields.InterlockedSetFlags(PromiseFlags.SelfSubscribed) & PromiseFlags.SelfSubscribed) == 0;
                    _progressAndLocker._progressCollectionLocker.Enter();
                    _progressListeners.Enqueue(progressListener);
                    _progressAndLocker._progressCollectionLocker.Exit();

                    PromiseRef previous = null;
                    if (notSubscribed)
                    {
                        // Mark subscribing to prevent repooling while we get previous, then unmark after we have retained previous.
                        _smallFields.InterlockedSetFlags(PromiseFlags.Subscribing);
                        previous = _valueOrPrevious as PromiseRef;
                        if (previous != null)
                        {
                            previous.InterlockedRetainDisregardId();
                        }
                        _smallFields.InterlockedUnsetFlags(PromiseFlags.Subscribing);
                    }
                    progressListener = this;
                    return previous;
                }

                protected override void SetInitialProgress(IProgressListener progressListener, Fixed32 lastKnownProgress, ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    Promise.State state = State;
                    if (state == Promise.State.Pending)
                    {
                        _progressAndLocker._progressCollectionLocker.Enter();
                        bool contained = _progressListeners.Contains(progressListener); // TODO: progress is no longer unsubscribed, so we only need to check if it's empty.
                        _progressAndLocker._progressCollectionLocker.Exit();

                        if (contained)
                        {
                            progressListener.SetInitialProgress(this, state, _progressAndLocker._currentProgress, ref executionScheduler);
                        }
                        return;
                    }

                    _progressAndLocker._progressCollectionLocker.Enter();
                    bool removed = _progressListeners.TryRemove(progressListener);
                    _progressAndLocker._progressCollectionLocker.Exit();

                    if (removed)
                    {
                        progressListener.SetInitialProgress(this, state, _progressAndLocker._depthAndProgress.GetIncrementedWholeTruncated(), ref executionScheduler);
                    }
                }

                partial void HandleProgressListeners(Promise.State state, ref ExecutionScheduler executionScheduler)
                {
                    _progressAndLocker._progressCollectionLocker.Enter();
                    var progressListeners = _progressListeners.MoveElementsToStack();
                    _progressAndLocker._progressCollectionLocker.Exit();

                    if (progressListeners.IsEmpty)
                    {
                        return;
                    }
                    WaitWhileProgressFlags(PromiseFlags.Reporting | PromiseFlags.SettingInitial);

                    Fixed32 progress = _progressAndLocker._depthAndProgress.GetIncrementedWholeTruncated();
                    if (state == Promise.State.Resolved)
                    {
                        do
                        {
                            progressListeners.Pop().ResolveOrSetProgress(this, progress, ref executionScheduler);
                        } while (progressListeners.IsNotEmpty);
                        return;
                    }

                    do
                    {
                        progressListeners.Pop().MaybeCancelProgress(progress);
                    } while (progressListeners.IsNotEmpty);
                }

                private void SetProgress(Fixed32 progress, ref ExecutionScheduler executionScheduler)
                {
                    if (_progressAndLocker._currentProgress.InterlockedTrySet(progress)
                        && (_smallFields.InterlockedSetFlags(PromiseFlags.InProgressQueue) & PromiseFlags.InProgressQueue) == 0) // Was not already in progress queue?
                    {
                        InterlockedRetainDisregardId();
                        executionScheduler.ScheduleProgressSynchronous(this);
                    }
                }

                void IProgressListener.MaybeCancelProgress(Fixed32 progress)
                {
                    _progressAndLocker._currentProgress.InterlockedMakeNegativeIfOtherWholeIsGreater(progress);
                    MaybeDispose();
                }

                void IProgressListener.ResolveOrSetProgress(PromiseRef sender, Fixed32 progress, ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    SetProgress(progress, ref executionScheduler);
                    MaybeDispose();
                }

                void IProgressListener.SetInitialProgress(PromiseRef sender, Promise.State state, Fixed32 progress, ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    switch (state)
                    {
                        case Promise.State.Pending:
                        {
                            if (!sender.GetIsProgressSuspended() && TrySetInitialProgressAndMarkInQueue(progress))
                            {
                                InterlockedRetainDisregardId();
                                executionScheduler.ScheduleProgressSynchronous(this);
                            }
                            break;
                        }
                        case Promise.State.Resolved:
                        {
                            if (sender != _valueOrPrevious && TrySetInitialProgressAndMarkInQueue(progress))
                            {
                                executionScheduler.ScheduleProgressSynchronous(this);
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
                        && (_smallFields.InterlockedSetFlags(PromiseFlags.InProgressQueue) & PromiseFlags.InProgressQueue) == 0; // Was not already in progress queue?
                }

                void IProgressListener.SetProgress(PromiseRef sender, Fixed32 progress, out PromiseSingleAwaitWithProgress nextRef, ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    SetProgress(progress, ref executionScheduler);
                    nextRef = null;
                }

                void IProgressListener.Retain()
                {
                    ThrowIfInPool(this);
                    InterlockedRetainDisregardId();
                }

                void IProgressInvokable.Invoke(ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    Thread.MemoryBarrier(); // Make sure we're reading fresh progress (since the field cannot be marked volatile).
                    var progress = _progressAndLocker._currentProgress;
                    _smallFields.InterlockedUnsetFlags(PromiseFlags.InProgressQueue);
                    if (!progress.IsNegative) // Was it not suspended?
                    {
                        // Lock is necessary for race condition with Handle.
                        // TODO: refactor to remove the need for a lock here.
                        _progressAndLocker._progressCollectionLocker.Enter();
                        foreach (var progressListener in _progressListeners)
                        {
                            PromiseSingleAwaitWithProgress nextRef;
                            progressListener.SetProgress(this, progress, out nextRef, ref executionScheduler);
                            if (nextRef != null)
                            {
                                nextRef.ReportProgress(progress, ref executionScheduler);
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
                    return null;
                }

                protected override sealed void SetInitialProgress(IProgressListener progressListener, Fixed32 lastKnownProgress, ref ExecutionScheduler executionScheduler)
                {
                    SetInitialProgress(progressListener, _currentProgress, _currentProgress.GetIncrementedWholeTruncated(), ref executionScheduler);
                }

                internal override sealed void HandleProgressListener(Promise.State state, ref ExecutionScheduler executionScheduler)
                {
                    HandleProgressListener(state, _currentProgress.GetIncrementedWholeTruncated(), ref executionScheduler);
                }
            }

            partial class DeferredPromiseBase
            {
                [MethodImpl(InlineOption)]
                internal bool TryReportProgress(short deferredId, float progress)
                {
                    if (!_smallFields.InterlockedTryRetainWithDeferredId(deferredId))
                    {
                        return false;
                    }

                    ThrowIfInPool(this);

                    // Don't report progress 1.0, that will be reported automatically when the promise is resolved.
                    if (progress >= 0 & progress < 1f)
                    {
                        var newProgress = _currentProgress.SetNewDecimalPart(progress);
                        ExecutionScheduler executionScheduler = new ExecutionScheduler(false);
                        ReportProgress(newProgress, ref executionScheduler);
                        executionScheduler.ExecuteProgress();
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

                partial void SubscribeProgressToOther(PromiseRef other, int depth, ref ExecutionScheduler executionScheduler)
                {
                    _progressFields._previousDepthPlusOne = depth + 1;
                    // Lazy subscribe: only subscribe to second previous if a progress listener is added to this (this keeps execution more efficient when progress isn't used).
                    bool hasListener = _progressListener != null;
                    PromiseFlags subscribedFlag = hasListener ? PromiseFlags.SelfSubscribed : PromiseFlags.None;
                    PromiseFlags oldFlags = _smallFields.InterlockedSetFlags(PromiseFlags.SecondPrevious | subscribedFlag);
                    if (hasListener & (oldFlags & PromiseFlags.SelfSubscribed) == 0) // Has listener and was not already subscribed?
                    {
                        other.SubscribeListener(this, new Fixed32(depth), ref executionScheduler);
                    }
                }

                internal void WaitForWithProgress<T>(Promise<T> other)
                {
                    ThrowIfInPool(this);
                    var _ref = other._ref;
                    _ref.MarkAwaited(other.Id, PromiseFlags.WasAwaitedOrForgotten | PromiseFlags.SuppressRejection);
                    _valueOrPrevious = _ref;

                    ExecutionScheduler executionScheduler = new ExecutionScheduler(true);
                    SubscribeProgressToOther(_ref, other.Depth, ref executionScheduler);
                    _ref.AddWaiter(this, ref executionScheduler);
                    executionScheduler.Execute();
                }

                protected override PromiseRef MaybeAddProgressListenerAndGetPreviousRetained(ref IProgressListener progressListener, ref Fixed32 lastKnownProgress)
                {
                    ThrowIfInPool(this);
                    progressListener.Retain();
                    _progressListener = progressListener;
                    PromiseFlags oldFlags = _smallFields.InterlockedSetSubscribedIfSecondPrevious();
                    bool secondPrevious = (oldFlags & PromiseFlags.SecondPrevious) != 0;
                    bool secondSubscribed = (oldFlags & PromiseFlags.SelfSubscribed) != 0;
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
                    _smallFields.InterlockedSetFlags(PromiseFlags.Subscribing);
                    PromiseRef previous = _valueOrPrevious as PromiseRef;
                    if (previous != null) // If previous is null, this is either transitioning to second previous, or has already completed.
                    {
                        previous.InterlockedRetainDisregardId();
                    }
                    _smallFields.InterlockedUnsetFlags(PromiseFlags.Subscribing);
                    return previous;
                }

                protected override sealed void SetInitialProgress(IProgressListener progressListener, Fixed32 lastKnownProgress, ref ExecutionScheduler executionScheduler)
                {
                    SetInitialProgress(progressListener, _progressFields._depthAndProgress, _progressFields._depthAndProgress.GetIncrementedWholeTruncated(), ref executionScheduler);
                }

                void IProgressListener.SetInitialProgress(PromiseRef sender, Promise.State state, Fixed32 progress, ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    switch (state)
                    {
                        case Promise.State.Pending:
                        {
                            if (!sender.GetIsProgressSuspended() && TrySetInitialProgressAndMarkInQueue(progress))
                            {
                                InterlockedRetainDisregardId();
                                executionScheduler.ScheduleProgressSynchronous(this);
                            }
                            break;
                        }
                        case Promise.State.Resolved:
                        {
                            if (sender != _valueOrPrevious && TrySetInitialProgressAndMarkInQueue(progress))
                            {
                                executionScheduler.ScheduleProgressSynchronous(this);
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
                        && (_smallFields.InterlockedSetFlags(PromiseFlags.InProgressQueue) & PromiseFlags.InProgressQueue) == 0; // Was not already in progress queue?
                }

                private double NormalizeProgress(Fixed32 progress)
                {
                    // Calculate the normalized progress for the depth of the returned promise.
                    // Use double for better precision.
                    return progress.ToDouble() / (double) _progressFields._previousDepthPlusOne;
                }

                private void SetProgressAndMaybeAddToQueue(Fixed32 progress, ref ExecutionScheduler executionScheduler)
                {
                    _progressFields._depthAndProgress.InterlockedSetNewDecimalPartIfNotNegativeAndDecimalIsGreater(NormalizeProgress(progress));
                    if ((_smallFields.InterlockedSetFlags(PromiseFlags.InProgressQueue) & PromiseFlags.InProgressQueue) == 0) // Was not already in progress queue?
                    {
                        InterlockedRetainDisregardId();
                        executionScheduler.ScheduleProgressSynchronous(this);
                    }
                }

                void IProgressListener.SetProgress(PromiseRef sender, Fixed32 progress, out PromiseSingleAwaitWithProgress nextRef, ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    SetProgressAndMaybeAddToQueue(progress, ref executionScheduler);
                    nextRef = null;
                }

                void IProgressListener.ResolveOrSetProgress(PromiseRef sender, Fixed32 progress, ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    // Don't set progress if this is resolved by the second wait.
                    // Have to check the value's type since MakeReady is called before this.
                    if (!(_valueOrPrevious is IValueContainer))
                    {
                        SetProgressAndMaybeAddToQueue(progress, ref executionScheduler);
                    }
                    MaybeDispose();
                }

                void IProgressListener.MaybeCancelProgress(Fixed32 progress)
                {
                    _progressFields._depthAndProgress.InterlockedMaybeMakeNegativeIfDecimalIsNotGreater();
                    MaybeDispose();
                }

                void IProgressListener.Retain()
                {
                    InterlockedRetainDisregardId();
                }

                void IProgressInvokable.Invoke(ref ExecutionScheduler executionScheduler)
                {
                    Thread.MemoryBarrier(); // Make sure we're reading fresh progress (since the field cannot be marked volatile).
                    var progress = _progressFields._depthAndProgress;
                    _smallFields.InterlockedUnsetFlags(PromiseFlags.InProgressQueue);
                    if (!progress.IsNegative) // Was it not suspended?
                    {
                        ReportProgress(progress, ref executionScheduler);
                    }
                    MaybeDispose();
                }

                internal override sealed void HandleProgressListener(Promise.State state, ref ExecutionScheduler executionScheduler)
                {
                    HandleProgressListener(state, _progressFields._depthAndProgress.GetIncrementedWholeTruncated(), ref executionScheduler);
                }
            } // PromiseWaitPromise

            partial class PromisePassThrough
            {
                void IProgressListener.SetInitialProgress(PromiseRef sender, Promise.State state, Fixed32 progress, ref ExecutionScheduler executionScheduler)
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
                            _target.IncrementProgress(progress.ToPositiveUInt32(), progress, _smallFields._depth, ref executionScheduler);
                        }
                        _smallFields._settingInitialProgress = false;
                    }
                    else
                    {
                        Release();
                    }
                }

                void IProgressListener.SetProgress(PromiseRef sender, Fixed32 progress, out PromiseSingleAwaitWithProgress nextRef, ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    _smallFields._reportingProgress = true;
                    Thread.MemoryBarrier(); // Make sure _owner is read after _reportingProgress is written.
                    uint dif;
                    bool didSet = _smallFields._currentProgress.InterlockedTrySetAndGetDifferenceIfNotNegativeAndWholeIsGreater(progress, out dif);
                    var owner = _owner;
                    if (didSet & owner != null)
                    {
                        _target.IncrementProgress(dif, progress, _smallFields._depth, ref executionScheduler);
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

                void IProgressListener.ResolveOrSetProgress(PromiseRef sender, Fixed32 progress, ref ExecutionScheduler executionScheduler)
                {
                    Release();
                }

                void IProgressListener.MaybeCancelProgress(Fixed32 progress)
                {
                    _smallFields._currentProgress.InterlockedMakeNegativeIfOtherWholeIsGreater(progress);
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