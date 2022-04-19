﻿#if UNITY_5_5 || NET_2_0 || NET_2_0_SUBSET
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

#pragma warning disable IDE0018 // Inline variable declaration
#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable 0420 // A reference to a volatile field will not be treated as volatile

using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Proto.Promises
{
    partial class Internal
    {
        internal const ushort NegativeOneDepth = ushort.MaxValue; // Same as (ushort) -1, but compiler complains that it needs unchecked context.

#if !PROMISE_PROGRESS

#if UNITY_UNITY_5_5_OR_NEWER
        internal const string ProgressDisabledMessage = "Progress is disabled. Progress will not be reported. Remove PROTO_PROMISE_PROGRESS_DISABLE from your scripting compilation symbols to enable progress.";
#else
        internal const string ProgressDisabledMessage = "Progress is disabled. Progress will not be reported. Use a version of the library compiled with progress enabled for progress reports.";
#endif

#else // !PROMISE_PROGRESS
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
                _progressQueue.Enqueue(progress);
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
                    var executionScheduler = new ExecutionScheduler(false);
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
#endif // !PROMISE_PROGRESS

        partial class PromiseRef
        {
            // Calls to these get compiled away when PROGRESS is undefined.
            partial void WaitWhileProgressReporting();
            partial void InterlockedIncrementProgressReportingCount();
            partial void InterlockedDecrementProgressReportingCount();

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal partial struct Fixed32
            {
                // 1 bit is taken for HasReportedFlag.

                // 15 bits for decimal part gives us 1/2^15 or 0.000030517578125 step size (4 digits of precision)
                // and the remaining 16 bits for whole part/depth allows up to 2^16 - 2 or 65534 promise.Then(() => otherPromise) chains, which should be plenty for typical use cases.
                // Also, SmallFields._depth is a ushort with 16 bits, so this should not be smaller than 15 (though it can be larger, as long as it leaves some bits for the whole part).
                internal const int DecimalBits = 15;
            }

#if PROMISE_PROGRESS
            [MethodImpl(InlineOption)]
            partial void WaitWhileProgressReporting()
            {
                Thread.MemoryBarrier(); // Make sure any writes happen before we read.
                // This is used to make sure progress reports are complete before the next handler is handled.
                if (_smallFields._reportingProgressCount != 0)
                {
                    WaitWhileProgressReportingCore();
                }
            }

            private void WaitWhileProgressReportingCore()
            {
                var spinner = new SpinWait();
                do
                {
                    spinner.SpinOnce();
                } while (_smallFields._reportingProgressCount != 0);
            }

            internal partial struct Fixed32
            {
                // Necessary to fix a race condition when hooking up a promise and the promise's deferred reports progress. Deferred report takes precedence.
                internal const int HasReportedFlag = 1 << 31;

                private const int ValueMask = ~HasReportedFlag;

                private const double DecimalMax = 1 << DecimalBits;
                private const int DecimalMask = (1 << DecimalBits) - 1;
                private const int WholeMask = ~DecimalMask & ValueMask;

                [MethodImpl(InlineOption)]
                private Fixed32(int value)
                {
                    _value = value;
                }

                [MethodImpl(InlineOption)]
                internal static Fixed32 FromWhole(ushort wholeValue)
                {
                    return new Fixed32(wholeValue << DecimalBits);
                }

                [MethodImpl(InlineOption)]
                internal static Fixed32 FromWholePlusOne(ushort wholeValue)
                {
                    // We don't need to check for overflow here.
                    return new Fixed32((wholeValue + 1) << DecimalBits);
                }

                [MethodImpl(InlineOption)]
                internal static Fixed32 FromDecimalForResolve(double decimalValue)
                {
                    return new Fixed32(ConvertToValue(decimalValue) | HasReportedFlag);
                }

                [MethodImpl(InlineOption)]
                internal Fixed32 WithoutHasReportedFlag()
                {
                    return new Fixed32(_value & ValueMask);
                }

#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE // Useful for debugging, but not actually used.
                internal ushort WholePart
                {
                    get { return (ushort) ((_value & WholeMask) >> DecimalBits); }
                }

                internal double DecimalPart
                {
                    get { return (double) (_value & DecimalMask) / DecimalMax; }
                }

                internal bool HasReported
                {
                    get { return (_value & HasReportedFlag) != 0; }
                }
#endif

                [MethodImpl(InlineOption)]
                internal static ushort GetNextDepth(ushort depth)
                {
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                    // We allow ushort.MaxValue to rollover for progress normalization purposes, but we don't allow overflow for regular user chains.
                    const int DepthBits = (32 - 1) - DecimalBits;
                    const ushort MaxValue = (1 << DepthBits) - 2;
                    if (depth == MaxValue)
                    {
                        throw new OverflowException("Promise chain length exceeded maximum of " + MaxValue);
                    }
#endif
                    unchecked
                    {
                        return (ushort) (depth + 1);
                    }
                }

                [MethodImpl(InlineOption)]
                internal uint GetRawValue()
                {
                    unchecked
                    {
                        return (uint) (_value & ValueMask);
                    }
                }

                [MethodImpl(InlineOption)]
                internal double ToDouble()
                {
                    return ConvertToDouble(_value);
                }

                private static double ConvertToDouble(int value)
                {
                    double wholePart = (value & WholeMask) >> DecimalBits;
                    double decimalPart = (double) (value & DecimalMask) / DecimalMax;
                    return wholePart + decimalPart;
                }

                [MethodImpl(InlineOption)]
                internal static Fixed32 GetScaled(UnsignedFixed64 value, double scaler, Fixed32 otherFlags)
                {
                    unchecked
                    {
                        // Don't bother rounding, we don't want to accidentally round to 1.0.
                        int newValue = (int) (value.ToDouble() * scaler * DecimalMax);
                        return new Fixed32(newValue | (otherFlags._value & HasReportedFlag));
                    }
                }

                internal bool InterlockedTrySetIfGreater(Fixed32 other)
                {
                    Thread.MemoryBarrier();
                    int setValue = other._value;
                    int otherValue = other._value & ValueMask;
                    int current;
                    do
                    {
                        current = _value;
                        bool currentHasReported = (current & HasReportedFlag) != 0;
                        bool isLessOrEqual = otherValue <= (current & ValueMask);
                        if (currentHasReported & isLessOrEqual)
                        {
                            return false;
                        }
                    } while (Interlocked.CompareExchange(ref _value, setValue, current) != current);
                    return true;
                }

                [MethodImpl(InlineOption)]
                internal bool InterlockedTrySet(Fixed32 other)
                {
                    int _;
                    return InterlockedTrySet(other, out _);
                }

                [MethodImpl(InlineOption)]
                internal long InterlockedSetAndGetDifference(Fixed32 other)
                {
                    int oldValue;
                    if (InterlockedTrySet(other, out oldValue))
                    {
                        unchecked
                        {
                            return (long) (other._value & ValueMask) - (long) (oldValue & ValueMask);
                        }
                    }
                    return 0;
                }

                private bool InterlockedTrySet(Fixed32 other, out int oldValue)
                {
                    Thread.MemoryBarrier();
                    bool otherHasReported = (other._value & HasReportedFlag) != 0;
                    int otherValue = other._value & ValueMask;
                    int setValue = otherValue | HasReportedFlag;
                    int current;
                    do
                    {
                        current = _value;
                        bool currentHasReported = (current & HasReportedFlag) != 0;
                        bool valuesAreSame = otherValue == (current & ValueMask);
                        // If this has not reported, set.
                        if (currentHasReported
                            & (valuesAreSame // If the value is the same, don't report.
                            | !otherHasReported)) // If this has already reported, and other is coming from hookup, don't report.
                        {
                            oldValue = 0;
                            return false;
                        }
                    } while (Interlocked.CompareExchange(ref _value, setValue, current) != current);
                    oldValue = current;
                    return true;
                }

                [MethodImpl(InlineOption)]
                internal Fixed32 SetNewDecimalPartFromDeferred(double decimalPart)
                {
                    int newValue = ConvertToValue(decimalPart) | HasReportedFlag;
                    _value = newValue;
                    return new Fixed32(newValue);
                }

                [MethodImpl(InlineOption)]
                internal bool TrySetNewDecimalPartFromAsync(double decimalPart, Fixed32 other, out Fixed32 result)
                {
                    int newValue = ConvertToValue(decimalPart);
                    int setValue = newValue | HasReportedFlag;
                    bool otherHasReported = (other._value & HasReportedFlag) != 0;
                    Thread.MemoryBarrier();
                    int current;
                    do
                    {
                        current = _value;
                        bool currentHasReported = (current & HasReportedFlag) != 0;
                        bool valuesAreSame = newValue == (current & ValueMask);
                        // If this has not reported, set.
                        if (currentHasReported
                            & (valuesAreSame // If the value is the same, don't report.
                            | !otherHasReported)) // If this has already reported, and other is coming from hookup, don't report.
                        {
                            result = default(Fixed32);
                            return false;
                        }
                    } while (Interlocked.CompareExchange(ref _value, setValue, current) != current);
                    result = new Fixed32(setValue);
                    return true;
                }

                [MethodImpl(InlineOption)]
                internal bool TrySetNewDecimalPartFromWaitPromise(double decimalPart, ushort wholePart, Fixed32 other, out Fixed32 result)
                {
                    int newValue = ConvertToValue(decimalPart) | (wholePart << DecimalBits) | HasReportedFlag;
                    bool otherHasReported = (other._value & HasReportedFlag) != 0;
                    Thread.MemoryBarrier();
                    int current;
                    do
                    {
                        current = _value;
                        bool currentHasReported = (current & HasReportedFlag) != 0;
                        bool valuesAreSame = newValue == (current & ValueMask);
                        // If this has not reported, set.
                        if (currentHasReported
                            & (valuesAreSame // If the value is the same, don't report.
                            | !otherHasReported)) // If this has already reported, and other is coming from hookup, don't report.
                        {
                            result = default(Fixed32);
                            return false;
                        }
                    } while (Interlocked.CompareExchange(ref _value, newValue, current) != current);
                    result = new Fixed32(newValue);
                    return true;
                }

                [MethodImpl(InlineOption)]
                private static int ConvertToValue(double dValue)
                {
                    // Don't round.
                    return (int) (dValue * DecimalMax);
                }

                // Using double for better precision.
                internal Fixed32 MultiplyAndDivide(double multiplier, double divisor)
                {
                    int value = _value;
                    double dValue = ConvertToDouble(value) * multiplier / divisor;
                    return new Fixed32(ConvertToValue(dValue) | (value & HasReportedFlag));
                }
            }

            /// <summary>
            /// Max Whole Number: 2^(64-<see cref="Promise.Config.ProgressDecimalBits"/>)
            /// Precision: 1/(2^<see cref="Promise.Config.ProgressDecimalBits"/>)
            /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal partial struct UnsignedFixed64 // Simplified compared to Fixed32 to remove unused functions.
            {
                private const double DecimalMax = 1L << Fixed32.DecimalBits;
                private const long DecimalMask = (1L << Fixed32.DecimalBits) - 1L;
                private const long WholeMask = ~DecimalMask;

                [MethodImpl(InlineOption)]
                internal UnsignedFixed64(ulong wholePart)
                {
                    unchecked
                    {
                        _value = (long) (wholePart << Fixed32.DecimalBits);
                    }
                }

                [MethodImpl(InlineOption)]
                private UnsignedFixed64(long value)
                {
                    _value = value;
                }

                internal double ToDouble()
                {
                    unchecked
                    {
                        long val = Interlocked.Read(ref _value);
                        double wholePart = (val & WholeMask) >> Fixed32.DecimalBits;
                        double decimalPart = (double) (val & DecimalMask) / DecimalMax;
                        return wholePart + decimalPart;
                    }
                }

                [MethodImpl(InlineOption)]
                internal UnsignedFixed64 InterlockedIncrement(long increment)
                {
                    Thread.MemoryBarrier();
                    long current;
                    long newValue;
                    do
                    {
                        current = Interlocked.Read(ref _value);
                        newValue = current + increment;
                    } while (Interlocked.CompareExchange(ref _value, newValue, current) != current);
                    return new UnsignedFixed64(newValue);
                }
            }

            partial class MultiHandleablePromiseBase
            {
                internal abstract PromiseSingleAwait IncrementProgress(long increment, ref Fixed32 progress, ushort depth);
            }


#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal sealed partial class PromiseProgress<TProgress> : PromiseSingleAwait, IProgressInvokable, ICancelable
                where TProgress : IProgress<float>
            {
                private static readonly WaitCallback _threadPoolCallback = ExecuteFromContext;
                private static readonly SendOrPostCallback _synchronizationContextCallback = ExecuteFromContext;

                internal bool IsInvoking1
                {
                    [MethodImpl(InlineOption)]
                    get { return _smallProgressFields._previousState != Promise.State.Pending; }
                }

                private bool IsCanceled
                {
                    [MethodImpl(InlineOption)]
                    get { return _smallProgressFields._canceled; }
                    [MethodImpl(InlineOption)]
                    set { _smallProgressFields._canceled = value; }
                }

                private PromiseProgress() { }

                internal static PromiseProgress<TProgress> GetOrCreate(TProgress progress, CancelationToken cancelationToken, ushort depth, bool isSynchronous, SynchronizationContext synchronizationContext)
                {
                    var promise = ObjectPool<HandleablePromiseBase>.TryTake<PromiseProgress<TProgress>>()
                        ?? new PromiseProgress<TProgress>();
                    promise.Reset(depth);
                    promise._progress = progress;
                    promise.IsCanceled = false;
                    promise._smallProgressFields._isSynchronous = isSynchronous;
                    promise._smallProgressFields._previousState = Promise.State.Pending;
                    promise._smallProgressFields._mostRecentPotentialScheduleMethod = (int) ScheduleMethod.None;
                    promise._synchronizationContext = synchronizationContext;
                    cancelationToken.TryRegister(promise, out promise._cancelationRegistration); // Very important, must register after promise is fully setup.
                    return promise;
                }

                internal static PromiseProgress<TProgress> GetOrCreateFromNull(TProgress progress, CancelationToken cancelationToken, ushort depth, SynchronizationContext synchronizationContext, ValueContainer valueContainer)
                {
                    var promise = ObjectPool<HandleablePromiseBase>.TryTake<PromiseProgress<TProgress>>()
                        ?? new PromiseProgress<TProgress>();
                    promise.Reset(depth);
                    promise._progress = progress;
                    promise.IsCanceled = false;
                    promise._smallProgressFields._isSynchronous = false;
                    promise._smallProgressFields._previousState = Promise.State.Resolved;
                    promise._smallProgressFields._mostRecentPotentialScheduleMethod = (int) ScheduleMethod.None;
                    promise._synchronizationContext = synchronizationContext;
                    promise._valueContainer = valueContainer;
                    cancelationToken.TryRegister(promise, out promise._cancelationRegistration); // Very important, must register after promise is fully setup.
                    return promise;
                }

                protected override void Dispose()
                {
                    base.Dispose();
                    _cancelationRegistration = default(CancelationRegistration);
                    _progress = default(TProgress);
                    ObjectPool<HandleablePromiseBase>.MaybeRepool(this);
                }

                void IProgressInvokable.Invoke(ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    Thread.MemoryBarrier(); // Make sure we're reading fresh progress (since the field cannot be marked volatile).
                    var progress = _smallFields._currentProgress;
                    _smallFields.InterlockedUnsetFlags(PromiseFlags.InProgressQueue);
                    // Calculate the normalized progress for the depth that the listener was added.
                    // Use double for better precision.
                    double expected = Depth + 1u;
                    float value = (float) (progress.ToDouble() / expected);
                    if (!IsInvoking1 & !IsCanceled & !_cancelationRegistration.Token.IsCancelationRequested)
                    {
                        CallbackHelper.InvokeAndCatchProgress(_progress, value, this);
                    }
                    MaybeDispose();
                }

                internal void MaybeReportProgress(ref ExecutionScheduler executionScheduler)
                {
                    PromiseFlags oldFlags = _smallFields.InterlockedSetFlags(PromiseFlags.InProgressQueue);
                    bool inProgressQueue = (oldFlags & PromiseFlags.InProgressQueue) != 0;
                    if (!inProgressQueue)
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

                internal override PromiseSingleAwait SetProgress(ref Fixed32 progress, ushort depth, ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    if (_smallFields._currentProgress.InterlockedTrySet(progress) & !IsCanceled)
                    {
                        MaybeReportProgress(ref executionScheduler);
                        return this;
                    }
                    return null;
                }

                internal override void Handle(ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);

                    HandleablePromiseBase nextHandler;
                    Invoke1(_smallProgressFields._previousState, out nextHandler, ref executionScheduler);
                    MaybeHandleNext(nextHandler, ref executionScheduler);
                }

                internal override void Handle(ref PromiseRef handler, out HandleablePromiseBase nextHandler, ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    var state = handler.State;
                    _smallProgressFields._previousState = state;
                    _valueContainer = handler._valueContainer.Clone();

                    if (!_smallProgressFields._isSynchronous)
                    {
                        nextHandler = null;
                        executionScheduler.ScheduleOnContext(_synchronizationContext, this);
                        return;
                    }

                    handler.MaybeDispose();
                    handler = this;

                    Invoke1(state, out nextHandler, ref executionScheduler);
                }

                private void Invoke1(Promise.State state, out HandleablePromiseBase nextHandler, ref ExecutionScheduler executionScheduler)
                {
                    bool resolved = TryUnregisterAndIsNotCanceling(ref _cancelationRegistration) & !IsCanceled & state == Promise.State.Resolved;
                    if (resolved)
                    {
                        CallbackHelper.InvokeAndCatchProgress(_progress, 1f, this);
                    }

#if NET_LEGACY // Interlocked.Exchange doesn't seem to work properly in Unity's old runtime. I'm not sure why, but we need a lock here to pass multi-threaded tests.
                    lock (this)
#endif
                    {
                        Thread.MemoryBarrier(); // Make sure previous writes are done before swapping schedule method.
                        ScheduleMethod previousScheduleType = (ScheduleMethod) Interlocked.Exchange(ref _smallProgressFields._mostRecentPotentialScheduleMethod, (int) ScheduleMethod.Handle);

                        // Only set state and handle next waiter after callback is executed and a waiter was added (or failed to add or forgotten)
                        // to make sure the next waiter will be executed on the correct context for consistency.
                        if (!_smallProgressFields._isSynchronous & previousScheduleType == ScheduleMethod.None)
                        {
                            nextHandler = null;
                            return;
                        }

                        State = state;
                        Thread.MemoryBarrier(); // Make sure previous writes are done before swapping _waiter.
                        nextHandler = Interlocked.Exchange(ref _waiter, null);
                    }
                }

                void ICancelable.Cancel()
                {
                    ThrowIfInPool(this);
                    IsCanceled = true;
                }

                protected override void OnForgetOrHookupFailed()
                {
#if NET_LEGACY // Interlocked.Exchange doesn't seem to work properly in Unity's old runtime. I'm not sure why, but we need a lock here to pass multi-threaded tests.
                    lock (this)
#endif
                    {
                        ThrowIfInPool(this);
                        if ((ScheduleMethod) Interlocked.Exchange(ref _smallProgressFields._mostRecentPotentialScheduleMethod, (int) ScheduleMethod.OnForgetOrHookupFailed) == ScheduleMethod.Handle)
                        {
                            State = _smallProgressFields._previousState;
                        }
                        base.OnForgetOrHookupFailed();
                    }
                }

                internal override void AddWaiter(HandleablePromiseBase waiter, ref ExecutionScheduler executionScheduler)
                {
                    HandleablePromiseBase nextHandler;
                    AddWaiter(waiter, out nextHandler, ref executionScheduler);
                    MaybeHandleNext(nextHandler, ref executionScheduler);
                }

                internal override void AddWaiter(HandleablePromiseBase waiter, out HandleablePromiseBase nextHandler, ref ExecutionScheduler executionScheduler)
                {
#if NET_LEGACY // Interlocked.Exchange doesn't seem to work properly in Unity's old runtime. I'm not sure why, but we need a lock here to pass multi-threaded tests.
                    lock (this)
#endif
                    {
                        ThrowIfInPool(this);
                        // When this is completed, State is set then _waiter is swapped, so we must reverse that process here.
                        Thread.MemoryBarrier();
                        SetWaiter(waiter);
                        Thread.MemoryBarrier(); // Make sure previous writes are done before swapping schedule method.
                        ScheduleMethod previousScheduleType = (ScheduleMethod) Interlocked.Exchange(ref _smallProgressFields._mostRecentPotentialScheduleMethod, (int) ScheduleMethod.AddWaiter);
                        if (previousScheduleType == ScheduleMethod.Handle)
                        {
                            if (_smallProgressFields._isSynchronous)
                            {
                                State = _smallProgressFields._previousState;
                                Thread.MemoryBarrier(); // Make sure previous writes are done before swapping _waiter.
                                nextHandler = Interlocked.Exchange(ref _waiter, null);
                                return;
                            }

                            // If this was configured to execute progress on a SynchronizationContext or the ThreadPool, force the waiter to execute on the same context for consistency.
                            // Retain since this will be released higher in the call stack.
                            InterlockedRetainDisregardId();
                            if (_synchronizationContext == null)
                            {
                                // If there is no context, send it to the ThreadPool.
                                ThreadPool.QueueUserWorkItem(_threadPoolCallback, this);
                            }
                            else
                            {
                                _synchronizationContext.Post(_synchronizationContextCallback, this);
                            }
                        }
                        nextHandler = null;
                    }
                }

                private static void ExecuteFromContext(object state)
                {
                    // In case this is executed from a background thread, catch the exception and report it instead of crashing the app.
                    try
                    {
                        // This handles the waiter that was added after this was already complete.
                        var _this = (PromiseProgress<TProgress>) state;
                        ThrowIfInPool(_this);
                        var _state = _this._smallProgressFields._previousState;
                        _this.State = _state;
                        // We don't need to synchronize access here because this is only called when the previous promise completed and the waiter has already been added (or failed to add), so there are no race conditions.
                        HandleablePromiseBase nextHandler = _this._waiter;
                        _this._waiter = null;
                        var executionScheduler = new ExecutionScheduler(true);
                        _this.MaybeHandleNext(nextHandler, ref executionScheduler);
                        executionScheduler.Execute();
                    }
                    catch (Exception e)
                    {
                        // This should never happen.
                        AddRejectionToUnhandledStack(e, state as ITraceable);
                    }
                }
            } // PromiseProgress<TProgress>

            [MethodImpl(InlineOption)]
            partial void InterlockedIncrementProgressReportingCount()
            {
                InterlockedAddWithOverflowCheck(ref _smallFields._reportingProgressCount, 1, -1);
            }

            [MethodImpl(InlineOption)]
            partial void InterlockedDecrementProgressReportingCount()
            {
                InterlockedAddWithOverflowCheck(ref _smallFields._reportingProgressCount, -1, 0);
            }

            partial class PromiseSingleAwait
            {
                internal override PromiseSingleAwait SetProgress(ref Fixed32 progress, ushort depth, ref ExecutionScheduler executionScheduler)
                {
                    return _smallFields._currentProgress.InterlockedTrySet(progress) ? this : null;
                }

                [MethodImpl(InlineOption)]
                internal void ReportProgress(Fixed32 progress, ushort depth, ref ExecutionScheduler executionScheduler)
                {
                    InterlockedIncrementProgressReportingCount();
                    ReportProgressAlreadyIncremented(progress, depth, ref executionScheduler);
                }

                protected void ReportProgressAlreadyIncremented(Fixed32 progress, ushort depth, ref ExecutionScheduler executionScheduler)
                {
                    PromiseSingleAwait current = this;
                    while (true)
                    {
                        var progressListener = current._waiter;
                        if (progressListener == null)
                        {
                            break;
                        }
                        var next = progressListener.SetProgress(ref progress, depth, ref executionScheduler);
                        if (next == null)
                        {
                            break;
                        }
                        next.InterlockedIncrementProgressReportingCount();
                        current.InterlockedDecrementProgressReportingCount();
                        current = next;
                        depth = current.Depth;
                    }
                    current.InterlockedDecrementProgressReportingCount();
                }
            } // PromiseSingleAwaitWithProgress

            partial class PromiseMultiAwait : IProgressInvokable
            {
                internal override PromiseSingleAwait SetProgress(ref Fixed32 progress, ushort depth, ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    if (_smallFields._currentProgress.InterlockedTrySet(progress)
                        && (_smallFields.InterlockedSetFlags(PromiseFlags.InProgressQueue) & PromiseFlags.InProgressQueue) == 0) // Was not already in progress queue?
                    {
                        InterlockedRetainDisregardId();
                        executionScheduler.ScheduleProgressSynchronous(this);
                    }
                    return null;
                }

                void IProgressInvokable.Invoke(ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    Thread.MemoryBarrier(); // Make sure we're reading fresh progress (since the field cannot be marked volatile).
                    var progress = _smallFields._currentProgress;
                    _smallFields.InterlockedUnsetFlags(PromiseFlags.InProgressQueue);
                    // Lock is necessary for race condition with Handle.
                    // TODO: refactor to remove the need for a lock here.
                    lock (this)
                    {
                        if (State == Promise.State.Pending)
                        {
                            foreach (var progressListener in _nextBranches)
                            {
                                Fixed32 progressCopy = progress;
                                PromiseSingleAwait nextRef = progressListener.SetProgress(ref progressCopy, Depth, ref executionScheduler);
                                if (nextRef != null)
                                {
                                    nextRef.ReportProgress(progressCopy, nextRef.Depth, ref executionScheduler);
                                }
                            }
                        }
                    }
                    MaybeDispose();
                }
            } // PromiseMultiAwait

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
                        Fixed32 newProgress = _smallFields._currentProgress.SetNewDecimalPartFromDeferred(progress);
                        var executionScheduler = new ExecutionScheduler(false);
                        ReportProgress(newProgress, 0, ref executionScheduler);
                        executionScheduler.ExecuteProgress();
                    }
                    MaybeDispose();
                    return true;
                }
            }

            partial class PromiseWaitPromise
            {
                //[MethodImpl(InlineOption)]
                //new protected void Reset(ushort depth)
                //{
                //    _secondPrevious = false; // TODO
                //    base.Reset(depth);
                //}

                internal void WaitForWithProgress<T>(Promise<T> other)
                {
                    ThrowIfInPool(this);
                    var _ref = other._ref;
                    _ref.MarkAwaited(other.Id, PromiseFlags.WasAwaitedOrForgotten | PromiseFlags.SuppressRejection);

                    var executionScheduler = new ExecutionScheduler(true);
                    SetSecondPrevious(_ref);
                    InterlockedIncrementProgressReportingCount();
                    HandleablePromiseBase nextRef;
                    _ref.AddWaiter(this, out nextRef, ref executionScheduler);
                    MaybeReportProgressAfterSecondPreviousHookup(_ref, other.Depth, ref executionScheduler);
                    _ref.MaybeHandleNext(nextRef, ref executionScheduler);
                    executionScheduler.Execute();
                }

                [MethodImpl(InlineOption)]
                private void SetSecondPrevious(PromiseRef secondPrevious)
                {
#if PROMISE_DEBUG
                    _previous = secondPrevious;
#endif
                    //_secondPrevious = true; // TODO
                    _smallFields.InterlockedSetFlags(PromiseFlags.SecondPrevious);
                    _smallFields._currentProgress = Fixed32.FromWhole(Depth);
                }

                [MethodImpl(InlineOption)]
                partial void MaybeReportProgressAfterSecondPreviousHookup(PromiseRef secondPrevious, ushort depth, ref ExecutionScheduler executionScheduler)
                {
                    Fixed32 progress;
                    if (secondPrevious.State == Promise.State.Pending
                        & TryNormalizeProgress(secondPrevious._smallFields._currentProgress, depth, out progress))
                    {
                        ReportProgressAlreadyIncremented(progress, Depth, ref executionScheduler);
                    }
                    else
                    {
                        InterlockedDecrementProgressReportingCount();
                    }
                }

                [MethodImpl(InlineOption)]
                private bool TryNormalizeProgress(Fixed32 progress, ushort depth, out Fixed32 result)
                {
                    // Calculate the normalized progress for this and previous depth.
                    double normalizedProgress = progress.ToDouble() / (depth + 1d);
                    return _smallFields._currentProgress.TrySetNewDecimalPartFromWaitPromise(normalizedProgress, Depth, progress, out result);
                }

                internal override sealed PromiseSingleAwait SetProgress(ref Fixed32 progress, ushort depth, ref ExecutionScheduler executionScheduler)
                {
                    // This acts as a pass-through to normalize the progress.
                    ThrowIfInPool(this);
                    //if (_secondPrevious) // TODO
                    bool didSet = _smallFields.AreFlagsSet(PromiseFlags.SecondPrevious)
                        ? TryNormalizeProgress(progress, depth, out progress)
                        : _smallFields._currentProgress.InterlockedTrySet(progress);
                    return didSet ? this : null;
                }
            } // PromiseWaitPromise

            partial class PromisePassThrough
            {
                internal override PromiseSingleAwait SetProgress(ref Fixed32 progress, ushort depth, ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    long dif = _smallFields._currentProgress.InterlockedSetAndGetDifference(progress);
                    return _target.IncrementProgress(dif, ref progress, _smallFields._depth);
                }

                [MethodImpl(InlineOption)]
                internal uint GetProgressDifferenceToCompletion(out Fixed32 progressFlags)
                {
                    ThrowIfInPool(this);
                    Fixed32 incrementedWhole = Fixed32.FromWholePlusOne(_smallFields._depth);
                    progressFlags = _smallFields._currentProgress;
                    return incrementedWhole.GetRawValue() - progressFlags.GetRawValue();
                }

                [MethodImpl(InlineOption)]
                partial void SetDepth(ushort depth)
                {
                    _smallFields._depth = depth;
                }

                partial void SetInitialProgress()
                {
                    var progress = _owner._smallFields._currentProgress;
                    _smallFields._currentProgress = progress;
                    uint increment = progress.GetRawValue();
                    _target.IncrementProgress(increment, ref progress, _smallFields._depth);
                }
            } // PromisePassThrough

            partial class AsyncPromiseRef
            {
                [MethodImpl(InlineOption)]
                new private void Reset()
                {
                    _minProgress = _maxProgress = float.NaN;
                    base.Reset();
                }

                [MethodImpl(InlineOption)]
                private static double Lerp(double a, double b, double t)
                {
                    return a + (b - a) * t;
                }

                private double LerpProgress(Fixed32 progress, ushort depth)
                {
                    ThrowIfInPool(this);
                    double normalizedProgress = progress.ToDouble() / (depth + 1d);
                    double newValue = Lerp(_minProgress, _maxProgress, normalizedProgress);
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                    if (newValue < 0 || newValue >= 1)
                    {
                        throw new ArithmeticException("Async progress calculated outside allowed bounds of [0, 1), value: " + newValue
                            + ", progress: " + progress.ToDouble() + ", depth: " + depth
                            + ", _minProgress: " + _minProgress + ", _maxProgress: " + _maxProgress);
                    }
#endif
                    return newValue;
                }

                internal override PromiseSingleAwait SetProgress(ref Fixed32 progress, ushort depth, ref ExecutionScheduler executionScheduler)
                {
                    if (float.IsNaN(_minProgress))
                    {
                        return null;
                    }
                    var lerpedProgress = LerpProgress(progress, depth);
                    return _smallFields._currentProgress.TrySetNewDecimalPartFromAsync(lerpedProgress, progress, out progress) ? this : null;
                }

                private void SetAwaitedComplete(PromiseRef handler, ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    // Don't report progress if it's 1. That will be reported when the async promise is resolved.
                    // Also don't report if the awaited promise was rejected or canceled.
                    if (handler.State == Promise.State.Resolved & _maxProgress < 1f)
                    {
                        var progress = Fixed32.FromDecimalForResolve(_maxProgress);
                        _smallFields._currentProgress = progress.WithoutHasReportedFlag();
                        ReportProgress(progress, 0, ref executionScheduler);
                    }
                    _minProgress = _maxProgress = float.NaN;
                }

                [MethodImpl(InlineOption)]
                internal void SetSecondPreviousAndProgress(PromiseRef waiter, float minProgress, float maxProgress)
                {
#if PROMISE_DEBUG
                    _previous = waiter;
#endif
                    _minProgress = minProgress;
                    _maxProgress = maxProgress;
                }

                [MethodImpl(InlineOption)]
                partial void MaybeReportProgressAfterHookup(PromiseRef waiter, ushort depth, ref ExecutionScheduler executionScheduler)
                {
                    if (waiter.State != Promise.State.Pending)
                    {
                        return;
                    }
                    var waiterProgress = waiter._smallFields._currentProgress;
                    var lerpedProgress = LerpProgress(waiterProgress, waiter.Depth);
                    Fixed32 progress;
                    if (_smallFields._currentProgress.TrySetNewDecimalPartFromAsync(lerpedProgress, waiterProgress, out progress))
                    {
                        ReportProgressAlreadyIncremented(progress, 0, ref executionScheduler);
                    }
                    else
                    {
                        InterlockedDecrementProgressReportingCount();
                    }
                }
            } // AsyncPromiseRef
#endif // PROMISE_PROGRESS
        } // PromiseRef
    } // Internal
}