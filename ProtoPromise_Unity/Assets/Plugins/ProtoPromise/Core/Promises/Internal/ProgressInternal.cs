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
#if PROTO_PROMISE_NO_STACK_UNWIND // Helps to see full causality trace with internal stacktraces in exceptions (may cause StackOverflowException if the chain is very long).
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
#endif // !PROMISE_PROGRESS

        partial class PromiseRef
        {
            internal partial interface IProgressListener { }

            // Calls to these get compiled away when PROGRESS is undefined.
            partial void WaitWhileProgressFlags(PromiseFlags progressFlags);

            partial class PromiseMultiAwait
            {
                partial void HandleProgressListeners(Promise.State state, ref ExecutionScheduler executionScheduler);
            }

#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal partial struct Fixed32
            {
                // Since 3 bits are taken for flags, that leaves 29 bits for progress.
                // 13 bits for decimal part gives us 1/2^13 or 0.0001220703125 step size which is nearly 4 digits of precision
                // and the remaining 16 bits for whole part/depth allows up to 2^16 - 4 or 65532 promise.Then(() => otherPromise) chains, which should be plenty for typical use cases.
                // Also, SmallFields._depth is a ushort with 16 bits, so this should not be smaller than 13 (though it can be larger, as long as it leaves some bits for the whole part).
                internal const int DecimalBits = 13;
            }

#if !PROMISE_PROGRESS
            partial void HandleProgressListener(Promise.State state, ref ExecutionScheduler executionScheduler);
#else

            private void SubscribeListener(IProgressListener progressListener, Fixed32 depthAndProgress, ref ExecutionScheduler executionScheduler)
            {
                PromiseRef current = this;
                current.InterlockedRetainDisregardId();
                while (true)
                {
                    IProgressListener currentListener = progressListener;
                    PromiseRef previous = current.MaybeAddProgressListenerAndGetPreviousRetained(ref progressListener, ref depthAndProgress);
                    if (previous == null)
                    {
                        PromiseSingleAwaitWithProgress nextRef;
                        current._smallFields.InterlockedSetFlags(PromiseFlags.SettingInitial);
                        current.SetInitialProgress(currentListener, ref depthAndProgress, out nextRef, ref executionScheduler);
                        current._smallFields.InterlockedUnsetFlags(PromiseFlags.SettingInitial);
                        current.MaybeDispose();
                        if (nextRef != null)
                        {
                            nextRef.ReportProgress(depthAndProgress, ref executionScheduler);
                        }
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

            protected virtual void SetInitialProgress(IProgressListener progressListener, ref Fixed32 progress, out PromiseSingleAwaitWithProgress nextRef, ref ExecutionScheduler executionScheduler)
            {
                // Rare occurrence,
                // this will only be called on a PromiseSingleAwait (without progress) in a race condition with another thread completing promises,
                // or if for some reason the user subscribes progress from a .Then callback (which would be unusual, but perfectly legal).
                // In either case, do nothing. The progress will be updated on the other thread or current thread when the promise chain completes.
                ThrowIfInPool(this);
                nextRef = null;
            }

            partial void WaitWhileProgressFlags(PromiseFlags progressFlags)
            {
                Thread.MemoryBarrier(); // Make sure any writes happen before we read progress flags.
                // Wait until progressFlags are unset.
                // This is used to make sure promises and progress listeners aren't disposed while still in use on another thread.
                SpinWait spinner = new SpinWait();
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

            internal partial struct Fixed32
            {
                // Extra flags are necessary to update the value and the flags atomically without a lock.
                // Unfortunately, this digs into how how many promises can be chained, but it should still be large enough for most use cases.
                private const int SuspendedFlag = 1 << 31;
                internal const int ReportedFlag = 1 << 30;
                internal const int PriorityFlag = 1 << 29; // Priority is true when called from `Deferred.ReportProgress` or when a promise is resolved, false when called from `Promise.Progress`.
                
                private const int FlagsMask = SuspendedFlag | ReportedFlag | PriorityFlag;
                private const int ValueMask = ~FlagsMask;

                private const double DecimalMax = 1 << DecimalBits;
                private const int DecimalMask = (1 << DecimalBits) - 1;
                private const int WholeMask = ValueMask & ~DecimalMask;

                private volatile int _value; // int for Interlocked.

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
                internal static Fixed32 FromDecimalForAsync(double decimalValue)
                {
                    // Don't bother rounding, we don't want to accidentally round to 1.0.
                    return new Fixed32((ushort) (decimalValue * DecimalMax) | PriorityFlag);
                }

                internal ushort WholePart
                {
                    [MethodImpl(InlineOption)]
                    get { return (ushort) ((_value & WholeMask) >> DecimalBits); }
                }

                internal double DecimalPart
                {
                    [MethodImpl(InlineOption)]
                    get { return (double) (_value & DecimalMask) / DecimalMax; }
                }

                internal bool IsSuspended
                {
                    [MethodImpl(InlineOption)]
                    get { return (_value & SuspendedFlag) != 0; }
                }

                internal bool HasReported
                {
                    [MethodImpl(InlineOption)]
                    get { return (_value & ReportedFlag) != 0; }
                }

                internal bool IsPriority
                {
                    [MethodImpl(InlineOption)]
                    get { return GetPriorityFlag() != 0; }
                }

                [MethodImpl(InlineOption)]
                internal int GetPriorityFlag()
                {
                    return _value & PriorityFlag;
                }

                [MethodImpl(InlineOption)]
                internal static ushort GetNextDepth(ushort depth)
                {
#if PROMISE_DEBUG
                    // We allow ushort.MaxValue to rollover for progress normalization purposes, but we don't allow overflow for regular user chains.
                    // Subtract 4 so that promise retains will not overflow when subscribing progress (2 initial retains plus a retain in SubscribeListener plus some buffer).
                    const int DepthBits = (32 - 3) - DecimalBits;
                    const ushort MaxValue = ((1 << DepthBits) - 2) - (DecimalBits > 13 ? 0 : 4);
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
                internal static Fixed32 GetScaled(UnsignedFixed64 value, double scaler)
                {
                    // Don't bother rounding, we don't want to accidentally round to 1.0.
                    int newValue = (int) (value.ToDouble() * scaler * DecimalMax);
                    unchecked
                    {
                        return new Fixed32(newValue | (int) (value.GetPriorityFlag() >> 32));
                    }
                }

                internal bool InterlockedTrySetIfGreater(Fixed32 other, Fixed32 otherFlags)
                {
                    Thread.MemoryBarrier();
                    int otherValue = other._value;
                    int otherComparer = otherValue & ValueMask;
                    int newValue = otherValue | ReportedFlag | (otherFlags._value & FlagsMask);
                    int current;
                    do
                    {
                        current = _value;
                        if (otherComparer < (current & ValueMask))
                        {
                            return false;
                        }
                    } while (Interlocked.CompareExchange(ref _value, newValue, current) != current);
                    return true;
                }

                [MethodImpl(InlineOption)]
                internal bool InterlockedTrySetAndGetDifference(Fixed32 other, out uint dif)
                {
                    int oldValue;
                    bool didSet = InterlockedTrySet(other, out oldValue);
                    unchecked
                    {
                        dif = (uint) (other._value & ValueMask) - (uint) (oldValue & ValueMask);
                    }
                    return didSet;
                }

                [MethodImpl(InlineOption)]
                internal Fixed32 SetNewDecimalPartFromDeferred(double decimalPart)
                {
                    // Don't bother rounding, we don't want to accidentally round to 1.0.
                    int newDecimalPart = (int) (decimalPart * DecimalMax);
                    int newValue = (_value & WholeMask) | newDecimalPart | ReportedFlag | PriorityFlag;
                    _value = newValue;
                    return new Fixed32(newValue);
                }

                internal bool InterlockedTrySet(Fixed32 other)
                {
                    int _;
                    return InterlockedTrySet(other, out _);
                }

                private bool InterlockedTrySet(Fixed32 other, out int oldValue)
                {
                    Thread.MemoryBarrier();
                    int otherValue = other._value;
                    int otherWholePart = other.WholePart;
                    bool otherIsPriority = (other._value & PriorityFlag) != 0;
                    int current, newValue;
                    bool success;
                    do
                    {
                        current = _value;
                        int currentWholePart = (current & WholeMask) >> DecimalBits;
                        bool currentIsSuspended = (current & SuspendedFlag) != 0;
                        bool currentHasReported = (current & ReportedFlag) != 0;
                        success = !(otherWholePart < currentWholePart
                            // Same thing, but more edge-case. If this is suspended, it means this was updated from a canceled or rejected promise, so it can only be further updated by a promise with a higher depth (WholePart).
                            | (currentIsSuspended & otherWholePart == currentWholePart)
                            // Don't bother updating if the values are the same, unless this is the first time being set.
                            | ((current & ValueMask) == (otherValue & ValueMask) & currentHasReported)
                            // Only update if other is priority or this has not reported, or other whole is definitely larger.
                            | (!otherIsPriority & currentHasReported & otherWholePart <= currentWholePart));
                        newValue = success
                            ? otherValue | ReportedFlag
                            // Just update reported flag
                            : current | ReportedFlag;
                    } while (Interlocked.CompareExchange(ref _value, newValue, current) != current);
                    oldValue = current;
                    return success;
                }

                internal bool InterlockedTrySetFromResolve(Fixed32 other)
                {
                    Thread.MemoryBarrier();
                    int otherValue = other._value;
                    int otherWholePart = other.WholePart;
                    int current, newValue;
                    bool success;
                    do
                    {
                        current = _value;
                        int currentWholePart = (current & WholeMask) >> DecimalBits;
                        success = otherWholePart > currentWholePart;
                        newValue = success
                            ? otherValue | ReportedFlag
                            // Just update reported flag
                            : current | ReportedFlag;
                    } while (Interlocked.CompareExchange(ref _value, newValue, current) != current);
                    return success;
                }

                internal void MaybeSuspend()
                {
                    int current = _value;
                    Interlocked.CompareExchange(ref _value, current | SuspendedFlag, current);
                }

                internal void InterlockedSuspendIfOtherWholeIsGreater(Fixed32 other)
                {
                    Thread.MemoryBarrier();
                    int otherWholePart = other.WholePart;
                    int oldWholePart = WholePart;
                    int current;
                    int newValue;
                    do
                    {
                        current = _value;
                        int currentWholePart = (current & WholeMask) >> DecimalBits;
                        // If other whole is less than or equal, or if the updated whole is greater than the old whole, do nothing.
                        if (otherWholePart <= currentWholePart | oldWholePart < currentWholePart)
                        {
                            return;
                        }
                        newValue = current | SuspendedFlag;
                    } while (Interlocked.CompareExchange(ref _value, newValue, current) != current);
                }

                [MethodImpl(InlineOption)]
                internal Fixed32 GetIncrementedWholeTruncated()
                {
                    return new Fixed32(GetIncrementedWholeTruncatedValue());
                }

                [MethodImpl(InlineOption)]
                internal Fixed32 GetIncrementedWholeTruncatedForResolve()
                {
                    return new Fixed32(GetIncrementedWholeTruncatedValue() | PriorityFlag);
                }

                private int GetIncrementedWholeTruncatedValue()
                {
                    int value = _value;
                    int newValue = (value & WholeMask) + (1 << DecimalBits);
#if PROMISE_DEBUG
                    if ((newValue & ValueMask) != newValue)
                    {
                        throw new OverflowException();
                    }
#endif
                    int currentReportedAndPriorityFlags = value & ReportedFlag & PriorityFlag;
                    return newValue | currentReportedAndPriorityFlags;
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
                    int flags = value & FlagsMask;
                    double dValue = ConvertToDouble(value) * multiplier / divisor;
                    return new Fixed32(ConvertToValue(dValue) | flags);
                }

                internal Fixed32 DivideAndAdd(double divisor, ushort addend)
                {
                    int value = _value;
                    int flags = value & FlagsMask;
                    double dValue = ConvertToDouble(value) / divisor;
                    value = ConvertToValue(dValue) + (addend << DecimalBits);
                    return new Fixed32(value | flags);
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
                internal const long PriorityFlag = ((long) Fixed32.PriorityFlag) << 32;
                private const double DecimalMax = 1L << Fixed32.DecimalBits;
                private const long DecimalMask = (1L << Fixed32.DecimalBits) - 1L;
                private const long WholeMask = ~PriorityFlag & ~DecimalMask;

                private long _value; // long for Interlocked.

                [MethodImpl(InlineOption)]
                internal UnsignedFixed64(ulong wholePart)
                {
                    unchecked
                    {
                        _value = (long) (wholePart << Fixed32.DecimalBits);
                    }
                }

                [MethodImpl(InlineOption)]
                internal long GetPriorityFlag()
                {
                    return _value | PriorityFlag;
                }

                internal bool IsPriority
                {
                    [MethodImpl(InlineOption)]
                    get { return GetPriorityFlag() != 0; }
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
                internal void InterlockedIncrement(uint increment, Fixed32 otherFlags)
                {
                    Thread.MemoryBarrier();
                    long priorityFlag = ((long) otherFlags.GetPriorityFlag()) << 32;
                    long current;
                    long newValue;
                    do
                    {
                        current = Interlocked.Read(ref _value);
                        newValue = (current + increment) | priorityFlag;
                    } while (Interlocked.CompareExchange(ref _value, newValue, current) != current);
                }
            }

            partial interface IProgressListener : ILinked<IProgressListener>
            {
                void SetInitialProgress(PromiseRef sender, Promise.State state, ref Fixed32 progress, out PromiseSingleAwaitWithProgress nextRef, ref ExecutionScheduler executionScheduler);
                void SetProgress(PromiseRef sender, ref Fixed32 progress, out PromiseSingleAwaitWithProgress nextRef, ref ExecutionScheduler executionScheduler);
                void ResolveOrSetProgress(PromiseRef sender, Fixed32 progress, ref ExecutionScheduler executionScheduler);
                void MaybeCancelProgress(Fixed32 progress);
                void Retain();
            }

            partial class MultiHandleablePromiseBase
            {
                internal abstract void IncrementProgress(uint increment, Fixed32 senderAmount, Fixed32 ownerAmount, ref ExecutionScheduler executionScheduler);
            }


#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal sealed partial class PromiseProgress<TProgress> : PromiseSingleAwaitWithProgress, IProgressListener, IProgressInvokable, ICancelable
                where TProgress : IProgress<float>
            {
                [MethodImpl(InlineOption)]
                protected override bool GetIsProgressSuspended()
                {
                    return _smallProgressFields._currentProgress.IsSuspended;
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

                private PromiseProgress() { }

                internal static PromiseProgress<TProgress> GetOrCreate(TProgress progress, CancelationToken cancelationToken, ushort depth, bool isSynchronous, SynchronizationContext synchronizationContext)
                {
                    var promise = ObjectPool<HandleablePromiseBase>.TryTake<PromiseProgress<TProgress>>()
                        ?? new PromiseProgress<TProgress>();
                    promise.Reset(depth);
                    promise._progress = progress;
                    promise.IsComplete = false;
                    promise.IsCanceled = false;
                    promise._smallProgressFields._currentProgress = default(Fixed32);
                    promise._smallProgressFields._isSynchronous = isSynchronous;
                    promise._synchronizationContext = synchronizationContext;
                    cancelationToken.TryRegister(promise, out promise._cancelationRegistration);
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
                    var progress = _smallProgressFields._currentProgress;
                    _smallFields.InterlockedUnsetFlags(PromiseFlags.InProgressQueue);
                    // Calculate the normalized progress for the depth that the listener was added.
                    // Use double for better precision.
                    double expected = Depth + 1u;
                    float value = (float) (progress.ToDouble() / expected);
                    if (!progress.IsSuspended & !IsComplete & !IsCanceled & !_cancelationRegistration.Token.IsCancelationRequested)
                    {
                        CallbackHelper.InvokeAndCatchProgress(_progress, value, this);
                    }
                    MaybeDispose();
                }

                void IProgressListener.SetProgress(PromiseRef sender, ref Fixed32 progress, out PromiseSingleAwaitWithProgress nextRef, ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    bool needsInvoke = _smallProgressFields._currentProgress.InterlockedTrySet(progress);
                    if (needsInvoke & !IsComplete & !IsCanceled)
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
                        nextRef = this;
                    }
                    else
                    {
                        nextRef = null;
                    }
                }

                [MethodImpl(InlineOption)]
                private void SetProgressFromResolve(Fixed32 progress, ref ExecutionScheduler executionScheduler)
                {
                    bool needsInvoke = _smallProgressFields._currentProgress.InterlockedTrySetFromResolve(progress);
                    if (needsInvoke & !IsComplete & !IsCanceled)
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
                }

                void IProgressListener.ResolveOrSetProgress(PromiseRef sender, Fixed32 progress, ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    if (!IsComplete)
                    {
                        SetProgressFromResolve(progress, ref executionScheduler);
                    }
                    MaybeDispose();
                }

                void IProgressListener.SetInitialProgress(PromiseRef sender, Promise.State state, ref Fixed32 progress, out PromiseSingleAwaitWithProgress nextRef, ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    switch (state)
                    {
                        case Promise.State.Pending:
                        {
                            if (!sender.GetIsProgressSuspended()
                                && _smallProgressFields._currentProgress.InterlockedTrySet(progress)
                                && (_smallFields.InterlockedSetFlags(PromiseFlags.InProgressQueue) & PromiseFlags.InProgressQueue) == 0) // Was not already in progress queue?
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
                            if (sender != _valueOrPrevious
                                && _smallProgressFields._currentProgress.InterlockedTrySetFromResolve(progress)
                                && (_smallFields.InterlockedSetFlags(PromiseFlags.InProgressQueue) & PromiseFlags.InProgressQueue) == 0) // Was not already in progress queue?
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
                            _smallProgressFields._currentProgress.MaybeSuspend();
                            MaybeDispose();
                            break;
                        }
                    }
                    nextRef = null;
                }

                void IProgressListener.MaybeCancelProgress(Fixed32 progress)
                {
                    ThrowIfInPool(this);
                    _smallProgressFields._currentProgress.InterlockedSuspendIfOtherWholeIsGreater(progress);
                    MaybeDispose();
                }

                protected override void SetInitialProgress(IProgressListener progressListener, ref Fixed32 progress, out PromiseSingleAwaitWithProgress nextRef, ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    Promise.State state = State;
                    if (state == Promise.State.Pending & !IsCanceled)
                    {
                        progress = _smallProgressFields._currentProgress;
                        progressListener.SetInitialProgress(this, state, ref progress, out nextRef, ref executionScheduler);
                        return;
                    }
                    if (Interlocked.CompareExchange(ref _progressListener, null, progressListener) == progressListener)
                    {
                        progress = Fixed32.FromWholePlusOne(Depth);
                        WaitWhileProgressFlags(PromiseFlags.ReportingPriority | PromiseFlags.ReportingInitial);
                        progressListener.SetInitialProgress(this, Promise.State.Canceled, ref progress, out nextRef, ref executionScheduler);
                        return;
                    }
                    nextRef = null;
                }

                internal override void Handle(ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    bool notCanceled = TryUnregisterAndIsNotCanceling(ref _cancelationRegistration) & !IsCanceled;

                    // HandleSelf
                    ValueContainer valueContainer = (ValueContainer) _valueOrPrevious;
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

                void ICancelable.Cancel()
                {
                    ThrowIfInPool(this);
                    IsCanceled = true;
                }

                void IProgressListener.Retain()
                {
                    ThrowIfInPool(this);
                    InterlockedRetainDisregardId();
                }
                protected override PromiseRef MaybeAddProgressListenerAndGetPreviousRetained(ref IProgressListener progressListener, ref Fixed32 lastKnownProgress)
                {
                    ThrowIfInPool(this);
                    progressListener.Retain();
                    SetProgressListener(progressListener);
                    //lastKnownProgress = _smallProgressFields._depthAndProgress; // Unnecessary to set last known since we know SetInitialProgress will be called on this.
                    return null;
                }

                internal override void HandleProgressListener(Promise.State state, ref ExecutionScheduler executionScheduler)
                {
                    HandleProgressListener(state, Fixed32.FromWhole(Depth).GetIncrementedWholeTruncatedForResolve(), ref executionScheduler);
                }

                internal override void AddWaiter(HandleablePromiseBase waiter, ref ExecutionScheduler executionScheduler)
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
                                    waiter.MakeReady(this, (ValueContainer) _valueOrPrevious, ref executionScheduler);
                                }
                                else
                                {
                                    // If this was configured to execute progress on a SynchronizationContext or the ThreadPool, force the waiter to execute on the same context for consistency.

                                    // Taking advantage of an implementation detail that MakeReady will only add itself or nothing to the stack, so we can just send it to the context instead.
                                    // This is better than adding a new method to the interface.
                                    ExecutionScheduler overrideScheduler = executionScheduler.GetEmptyCopy();
                                    waiter.MakeReady(this, (ValueContainer) _valueOrPrevious, ref overrideScheduler);
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

                internal override void MakeReady(PromiseRef owner, ValueContainer valueContainer, ref ExecutionScheduler executionScheduler)
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
                [MethodImpl(InlineOption)]
                protected void SetProgressListener(IProgressListener progressListener)
                {
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                    var oldListener = Interlocked.CompareExchange(ref _progressListener, progressListener, null);
                    if (oldListener != null)
                    {
                        throw new System.InvalidOperationException("Cannot add more than 1 progress listener."
                            + "\nAttempted to add listener: " + progressListener
                            + "\nexisting listener: " + oldListener);
                    }
#else
                    _progressListener = progressListener;
#endif
                }

                protected void SetInitialProgress(IProgressListener progressListener, ref Fixed32 progress, Fixed32 expectedProgress, out PromiseSingleAwaitWithProgress nextRef, ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    Promise.State state = State;
                    if (state == Promise.State.Pending)
                    {
                        progressListener.SetInitialProgress(this, state, ref progress, out nextRef, ref executionScheduler);
                        return;
                    }
                    if (Interlocked.CompareExchange(ref _progressListener, null, progressListener) == progressListener)
                    {
                        progress = expectedProgress;
                        WaitWhileProgressFlags(PromiseFlags.ReportingPriority | PromiseFlags.ReportingInitial);
                        progressListener.SetInitialProgress(this, state, ref progress, out nextRef, ref executionScheduler);
                        return;
                    }
                    nextRef = null;
                }

                protected void HandleProgressListener(Promise.State state, Fixed32 progress, ref ExecutionScheduler executionScheduler)
                {
                    IProgressListener progressListener = Interlocked.Exchange(ref _progressListener, null);
                    WaitWhileProgressFlags(PromiseFlags.ReportingPriority | PromiseFlags.ReportingInitial | PromiseFlags.SettingInitial);
                    if (progressListener != null)
                    {
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
                        PromiseFlags setFlag = progress.IsPriority ? PromiseFlags.ReportingPriority : PromiseFlags.ReportingInitial;
                        if ((setter._smallFields.InterlockedSetFlags(setFlag) & setFlag) != 0)
                        {
                            break;
                        }
                        PromiseSingleAwaitWithProgress unsetter = setter;

                        IProgressListener progressListener = setter._progressListener;
                        if (progressListener != null)
                        {
                            progressListener.SetProgress(this, ref progress, out setter, ref executionScheduler);
                        }
                        else
                        {
                            setter = null;
                        }
                        unsetter._smallFields.InterlockedUnsetFlags(setFlag);
                    } while (setter != null);
                }
            } // PromiseSingleAwaitWithProgress

            partial class PromiseMultiAwait : IProgressInvokable
            {
                [MethodImpl(InlineOption)]
                new private void Reset(ushort depth)
                {
                    _progressAndLocker._currentProgress = default(Fixed32);
                    base.Reset(depth);
                }

                protected override PromiseRef MaybeAddProgressListenerAndGetPreviousRetained(ref IProgressListener progressListener, ref Fixed32 lastKnownProgress)
                {
                    ThrowIfInPool(this);
                    progressListener.Retain();
                    lastKnownProgress = _progressAndLocker._currentProgress;
                    bool notSubscribed = (_smallFields.InterlockedSetFlags(PromiseFlags.Subscribed) & PromiseFlags.Subscribed) == 0;
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

                protected override void SetInitialProgress(IProgressListener progressListener, ref Fixed32 progress, out PromiseSingleAwaitWithProgress nextRef, ref ExecutionScheduler executionScheduler)
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
                            progress = _progressAndLocker._currentProgress;
                            progressListener.SetInitialProgress(this, state, ref progress, out nextRef, ref executionScheduler);
                            return;
                        }
                    }
                    else
                    {
                        _progressAndLocker._progressCollectionLocker.Enter();
                        bool removed = _progressListeners.TryRemove(progressListener);
                        _progressAndLocker._progressCollectionLocker.Exit();

                        if (removed)
                        {
                            progress = Fixed32.FromWholePlusOne(Depth);
                            WaitWhileProgressFlags(PromiseFlags.ReportingPriority | PromiseFlags.ReportingInitial);
                            progressListener.SetInitialProgress(this, state, ref progress, out nextRef, ref executionScheduler);
                            return;
                        }
                    }
                    nextRef = null;
                }

                partial void HandleProgressListeners(Promise.State state, ref ExecutionScheduler executionScheduler)
                {
                    _progressAndLocker._progressCollectionLocker.Enter();
                    var progressListeners = _progressListeners.MoveElementsToStack();
                    _progressAndLocker._progressCollectionLocker.Exit();

                    WaitWhileProgressFlags(PromiseFlags.ReportingPriority | PromiseFlags.ReportingInitial | PromiseFlags.SettingInitial);
                    if (progressListeners.IsEmpty)
                    {
                        return;
                    }

                    Fixed32 progress = Fixed32.FromWhole(Depth).GetIncrementedWholeTruncatedForResolve();
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

                void IProgressListener.SetInitialProgress(PromiseRef sender, Promise.State state, ref Fixed32 progress, out PromiseSingleAwaitWithProgress nextRef, ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    switch (state)
                    {
                        case Promise.State.Pending:
                        {
                            if (!sender.GetIsProgressSuspended()
                                && _progressAndLocker._currentProgress.InterlockedTrySet(progress)
                                && (_smallFields.InterlockedSetFlags(PromiseFlags.InProgressQueue) & PromiseFlags.InProgressQueue) == 0) // Was not already in progress queue?
                            {
                                InterlockedRetainDisregardId();
                                executionScheduler.ScheduleProgressSynchronous(this);
                            }
                            break;
                        }
                        case Promise.State.Resolved:
                        {
                            if (sender != _valueOrPrevious
                                && _progressAndLocker._currentProgress.InterlockedTrySetFromResolve(progress)
                                && (_smallFields.InterlockedSetFlags(PromiseFlags.InProgressQueue) & PromiseFlags.InProgressQueue) == 0) // Was not already in progress queue?
                            {
                                executionScheduler.ScheduleProgressSynchronous(this);
                                break; // Break instead of InterlockedRetainDisregardId().
                            }
                            MaybeDispose();
                            break;
                        }
                        default: // Rejected or Canceled:
                        {
                            _progressAndLocker._currentProgress.MaybeSuspend();
                            MaybeDispose();
                            break;
                        }
                    }
                    nextRef = null;
                }

                private void SetProgress(Fixed32 progress, ref ExecutionScheduler executionScheduler)
                {
                    // If this is coming from hookup progress, we can possibly report without updating the progress.
                    // This is to handle race condition on separate threads.
                    if ((_progressAndLocker._currentProgress.InterlockedTrySet(progress) | !progress.IsPriority)
                        && (_smallFields.InterlockedSetFlags(PromiseFlags.InProgressQueue) & PromiseFlags.InProgressQueue) == 0) // Was not already in progress queue?
                    {
                        InterlockedRetainDisregardId();
                        executionScheduler.ScheduleProgressSynchronous(this);
                    }
                }

                void IProgressListener.SetProgress(PromiseRef sender, ref Fixed32 progress, out PromiseSingleAwaitWithProgress nextRef, ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    SetProgress(progress, ref executionScheduler);
                    nextRef = null;
                }

                void IProgressListener.MaybeCancelProgress(Fixed32 progress)
                {
                    _progressAndLocker._currentProgress.InterlockedSuspendIfOtherWholeIsGreater(progress);
                    MaybeDispose();
                }

                private void SetProgressFromResolve(Fixed32 progress, ref ExecutionScheduler executionScheduler)
                {
                    if ((_progressAndLocker._currentProgress.InterlockedTrySetFromResolve(progress))
                        && (_smallFields.InterlockedSetFlags(PromiseFlags.InProgressQueue) & PromiseFlags.InProgressQueue) == 0) // Was not already in progress queue?
                    {
                        InterlockedRetainDisregardId();
                        executionScheduler.ScheduleProgressSynchronous(this);
                    }
                }

                void IProgressListener.ResolveOrSetProgress(PromiseRef sender, Fixed32 progress, ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    SetProgressFromResolve(progress, ref executionScheduler);
                    MaybeDispose();
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
                    if (!progress.IsSuspended)
                    {
                        // Lock is necessary for race condition with Handle.
                        // TODO: refactor to remove the need for a lock here.
                        _progressAndLocker._progressCollectionLocker.Enter();
                        foreach (var progressListener in _progressListeners)
                        {
                            Fixed32 progressCopy = progress;
                            PromiseSingleAwaitWithProgress nextRef;
                            progressListener.SetProgress(this, ref progressCopy, out nextRef, ref executionScheduler);
                            if (nextRef != null)
                            {
                                nextRef.ReportProgress(progressCopy, ref executionScheduler);
                            }
                        }
                        _progressAndLocker._progressCollectionLocker.Exit();
                    }
                    MaybeDispose();
                }

                [MethodImpl(InlineOption)]
                protected override bool GetIsProgressSuspended()
                {
                    return _progressAndLocker._currentProgress.IsSuspended;
                }
            } // PromiseMultiAwait

            partial class AsyncPromiseBase
            {
                protected override sealed void SetInitialProgress(IProgressListener progressListener, ref Fixed32 progress, out PromiseSingleAwaitWithProgress nextRef, ref ExecutionScheduler executionScheduler)
                {
                    progress = _progressAndSubscribeFields._currentProgress;
                    SetInitialProgress(progressListener, ref progress, _progressAndSubscribeFields._currentProgress.GetIncrementedWholeTruncated(), out nextRef, ref executionScheduler);
                }

                internal override sealed void HandleProgressListener(Promise.State state, ref ExecutionScheduler executionScheduler)
                {
                    HandleProgressListener(state, _progressAndSubscribeFields._currentProgress.GetIncrementedWholeTruncatedForResolve(), ref executionScheduler);
                }
            }

            partial class DeferredPromiseBase
            {
                protected override sealed PromiseRef MaybeAddProgressListenerAndGetPreviousRetained(ref IProgressListener progressListener, ref Fixed32 lastKnownProgress)
                {
                    ThrowIfInPool(this);
                    progressListener.Retain();
                    lastKnownProgress = _progressAndSubscribeFields._currentProgress;
                    SetProgressListener(progressListener);
                    return null;
                }

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
                        var newProgress = _progressAndSubscribeFields._currentProgress.SetNewDecimalPartFromDeferred(progress);
                        ExecutionScheduler executionScheduler = new ExecutionScheduler(false);
                        ReportProgress(newProgress, ref executionScheduler);
                        executionScheduler.ExecuteProgress();
                    }
                    MaybeDispose();
                    return true;
                }
            }

            partial struct DepthAndFlags
            {
                internal ProgressSubscribeFlags InterlockedSetPreviousDepthAndFlags(ushort previousDepth, ProgressSubscribeFlags flags)
                {
                    Thread.MemoryBarrier();
                    DepthAndFlags current = default(DepthAndFlags), newValue;
                    do
                    {
                        current._intValue = _intValue;
                        newValue = current;
                        newValue._previousDepth = previousDepth;
                        newValue._flags |= flags;
                    } while (Interlocked.CompareExchange(ref _intValue, newValue._intValue, current._intValue) != current._intValue);
                    return current._flags;
                }

                internal ProgressSubscribeFlags InterlockedSetFlags(ProgressSubscribeFlags flags)
                {
                    Thread.MemoryBarrier();
                    DepthAndFlags current = default(DepthAndFlags), newValue;
                    do
                    {
                        current._intValue = _intValue;
                        newValue = current;
                        newValue._flags |= flags;
                    } while (Interlocked.CompareExchange(ref _intValue, newValue._intValue, current._intValue) != current._intValue);
                    return current._flags;
                }

                internal ProgressSubscribeFlags InterlockedUnsetFlags(ProgressSubscribeFlags flags)
                {
                    Thread.MemoryBarrier();
                    ProgressSubscribeFlags unsetFlags = ~flags;
                    DepthAndFlags current = default(DepthAndFlags), newValue;
                    do
                    {
                        current._intValue = _intValue;
                        newValue = current;
                        newValue._flags &= unsetFlags;
                    } while (Interlocked.CompareExchange(ref _intValue, newValue._intValue, current._intValue) != current._intValue);
                    return current._flags;
                }

                [MethodImpl(InlineOption)]
                internal void SetPreviousDepth(ushort previousDepth)
                {
                    _previousDepth = previousDepth;
                }

                [MethodImpl(InlineOption)]
                internal ProgressSubscribeFlags GetFlags()
                {
                    return _flags;
                }

                [MethodImpl(InlineOption)]
                internal void SetFlags(ProgressSubscribeFlags flags)
                {
                    _flags |= flags;
                }

                [MethodImpl(InlineOption)]
                internal ProgressSubscribeFlags UnsetFlags(ProgressSubscribeFlags flags)
                {
                    var oldFlags = _flags;
                    _flags &= ~flags;
                    return oldFlags;
                }

                [MethodImpl(InlineOption)]
                internal ushort GetPreviousDepthPlusOne()
                {
                    unchecked
                    {
                        return (ushort) (GetPreviousDepth() + 1u);
                    }
                }

                [MethodImpl(InlineOption)]
                internal ushort GetPreviousDepth()
                {
                    return _previousDepth;
                }
            } // DepthAndFlags

            protected partial struct ProgressSubscribeFields
            {

                [MethodImpl(InlineOption)]
                internal void Reset()
                {
                    _previousDepthAndFlags = default(DepthAndFlags);
                    _currentProgress = default(Fixed32);
                }
            }

            partial class PromiseWaitPromise
            {
                [MethodImpl(InlineOption)]
                new protected void Reset(ushort depth)
                {
                    _progressFields.Reset();
                    base.Reset(depth);
                }

                [MethodImpl(InlineOption)]
                protected override sealed bool GetIsProgressSuspended()
                {
                    return _progressFields._currentProgress.IsSuspended;
                }

                internal void WaitForWithProgress<T>(Promise<T> other)
                {
                    ThrowIfInPool(this);
                    var _ref = other._ref;
                    _ref.MarkAwaited(other.Id, PromiseFlags.WasAwaitedOrForgotten | PromiseFlags.SuppressRejection);

                    ExecutionScheduler executionScheduler = new ExecutionScheduler(true);
                    SetPreviousAndSubscribeProgress(_ref, other.Depth, ref executionScheduler);
                    _ref.AddWaiter(this, ref executionScheduler);
                    executionScheduler.Execute();
                }

                [MethodImpl(InlineOption)]
                private void SetPreviousAndSubscribeProgress(PromiseRef other, ushort depth, ref ExecutionScheduler executionScheduler)
                {
                    // Write SecondPrevious flag before writing previous to fix race condition with hookup MaybeAddProgressListenerAndGetPreviousRetained.
                    _progressFields._previousDepthAndFlags.InterlockedSetFlags(ProgressSubscribeFlags.AboutToSetPrevious);
                    _valueOrPrevious = other;

                    // Lazy subscribe: only subscribe to second previous if a progress listener is added to this (this keeps execution more efficient when progress isn't used).
                    ProgressSubscribeFlags oldFlags = _progressFields._previousDepthAndFlags.InterlockedSetPreviousDepthAndFlags(depth, ProgressSubscribeFlags.HasPrevious);
                    bool hasListener = (oldFlags & ProgressSubscribeFlags.HasListener) != 0;
                    if (hasListener)
                    {
                        oldFlags = _progressFields._previousDepthAndFlags.InterlockedSetFlags(ProgressSubscribeFlags.SubscribedFromSetPrevious);
                        bool notSubscribed = (oldFlags & ProgressSubscribeFlags.SubscribedFromAddListener) == 0;
                        if (notSubscribed)
                        {
                            other.SubscribeListener(this, Fixed32.FromWhole(depth), ref executionScheduler);
                        }
                    }
                }

                protected override PromiseRef MaybeAddProgressListenerAndGetPreviousRetained(ref IProgressListener progressListener, ref Fixed32 lastKnownProgress)
                {
                    ThrowIfInPool(this);
                    progressListener.Retain();
                    SetProgressListener(progressListener);

                    // Mark subscribing to prevent repooling while we get previous, then unmark after we have retained previous.
                    _smallFields.InterlockedSetFlags(PromiseFlags.Subscribing);
                    // Read previous before setting flag to fix race condition with SetPreviousAndSubscribeProgress.
                    object firstRead = _valueOrPrevious;
                    ProgressSubscribeFlags oldFlags = _progressFields._previousDepthAndFlags.InterlockedSetFlags(ProgressSubscribeFlags.HasListener);
                    PromiseRef previous;
                    bool hasSecondPrevious = (oldFlags & ProgressSubscribeFlags.HasPrevious) != 0;
                    if (hasSecondPrevious)
                    {
                        lastKnownProgress = Fixed32.FromWhole(_progressFields._previousDepthAndFlags.GetPreviousDepth());
                        oldFlags = _progressFields._previousDepthAndFlags.InterlockedSetFlags(ProgressSubscribeFlags.SubscribedFromAddListener);
                        bool alreadySubscribed = (oldFlags & ProgressSubscribeFlags.SubscribedFromSetPrevious) != 0;
                        if (alreadySubscribed)
                        {
                            _smallFields.InterlockedUnsetFlags(PromiseFlags.Subscribing);
                            return null;
                        }
                        progressListener = this;
                        // Read previous again to fix race condition with previous dispose.
                        previous = _valueOrPrevious as PromiseRef;
                    }
                    else
                    {
                        lastKnownProgress = Fixed32.FromWhole(Depth);
                        bool notAboutToSetSecondPrevious = (oldFlags & ProgressSubscribeFlags.AboutToSetPrevious) == 0;
                        previous = notAboutToSetSecondPrevious
                            ? firstRead as PromiseRef
                            : null;
                    }

                    if (previous != null) // If previous is null, this is either transitioning to second previous, or has already completed.
                    {
                        previous.InterlockedRetainDisregardId();
                    }
                    _smallFields.InterlockedUnsetFlags(PromiseFlags.Subscribing);
                    return previous;
                }

                protected override sealed void SetInitialProgress(IProgressListener progressListener, ref Fixed32 progress, out PromiseSingleAwaitWithProgress nextRef, ref ExecutionScheduler executionScheduler)
                {
                    progress = Fixed32.FromWhole(Depth);
                    SetInitialProgress(progressListener, ref progress, Fixed32.FromWholePlusOne(Depth), out nextRef, ref executionScheduler);
                }

                void IProgressListener.SetInitialProgress(PromiseRef sender, Promise.State state, ref Fixed32 progress, out PromiseSingleAwaitWithProgress nextRef, ref ExecutionScheduler executionScheduler)
                {
                    // This essentially acts as a pass-through to normalize the progress.
                    // We don't store the calculated progress here, it gets passed to the _progressListener in ReportProgress.
                    // Progress TrySet is only used for progress suspension purposes.
                    ThrowIfInPool(this);
                    nextRef = null;
                    switch (state)
                    {
                        case Promise.State.Pending:
                        {
                            if (!sender.GetIsProgressSuspended() && _progressFields._currentProgress.InterlockedTrySet(progress))
                            {
                                progress = NormalizeProgress(progress);
                                nextRef = this;
                            }
                            return;
                        }
                        case Promise.State.Resolved:
                        {
                            if (sender != _valueOrPrevious && _progressFields._currentProgress.InterlockedTrySetFromResolve(progress))
                            {
                                progress = NormalizeProgress(progress);
                                nextRef = this;
                            }
                            break;
                        }
                        default: // Rejected or Canceled:
                        {
                            _progressFields._currentProgress.MaybeSuspend();
                            nextRef = null;
                            break;
                        }
                    }
                    MaybeDispose();
                }

                [MethodImpl(InlineOption)]
                private Fixed32 NormalizeProgress(Fixed32 progress)
                {
                    // Calculate the normalized progress for this and previous depth.
                    return progress.DivideAndAdd(_progressFields._previousDepthAndFlags.GetPreviousDepthPlusOne(), Depth);
                }

                void IProgressListener.SetProgress(PromiseRef sender, ref Fixed32 progress, out PromiseSingleAwaitWithProgress nextRef, ref ExecutionScheduler executionScheduler)
                {
                    // This essentially acts as a pass-through to normalize the progress.
                    // We don't store the calculated progress here, it gets passed to the _progressListener in ReportProgress.
                    // TrySetProgress is only used for progress suspension purposes.
                    ThrowIfInPool(this);
                    if (_progressFields._currentProgress.InterlockedTrySet(progress))
                    {
                        progress = NormalizeProgress(progress);
                        nextRef = this;
                    }
                    else
                    {
                        nextRef = null;
                    }
                }

                void IProgressListener.ResolveOrSetProgress(PromiseRef sender, Fixed32 progress, ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    // Don't set progress if this is resolved by the second wait.
                    // Have to check the value's type since MakeReady is called before this.
                    if (!(_valueOrPrevious is ValueContainer) && _progressFields._currentProgress.InterlockedTrySetFromResolve(progress))
                    {
                        ReportProgress(NormalizeProgress(progress), ref executionScheduler);
                    }
                    MaybeDispose();
                }

                void IProgressListener.MaybeCancelProgress(Fixed32 progress)
                {
                    ThrowIfInPool(this);
                    _progressFields._currentProgress.InterlockedSuspendIfOtherWholeIsGreater(progress);
                    MaybeDispose();
                }

                void IProgressListener.Retain()
                {
                    InterlockedRetainDisregardId();
                }

                internal override sealed void HandleProgressListener(Promise.State state, ref ExecutionScheduler executionScheduler)
                {
                    HandleProgressListener(state, Fixed32.FromWhole(Depth).GetIncrementedWholeTruncatedForResolve(), ref executionScheduler);
                }
            } // PromiseWaitPromise

            partial class PromisePassThrough
            {
                void IProgressListener.SetInitialProgress(PromiseRef sender, Promise.State state, ref Fixed32 progress, out PromiseSingleAwaitWithProgress nextRef, ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    if (state == Promise.State.Pending)
                    {
                        _smallFields._settingInitialProgress = true;
                        // InterlockedTrySet has a MemoryBarrier in it, so we know _owner is read after _settingInitialProgress is written.
                        bool didSet = _smallFields._currentProgress.InterlockedTrySet(progress);
                        var owner = _owner;
                        if (didSet & owner != null)
                        {
                            // TODO: change Merge/Race promises to use `out nextRef` instead of the scheduler.
                            _target.IncrementProgress(progress.GetRawValue(), progress, Fixed32.FromWhole(_smallFields._depth), ref executionScheduler);
                        }
                        _smallFields._settingInitialProgress = false;
                    }
                    else
                    {
                        Release();
                    }
                    nextRef = null;
                }

                void IProgressListener.SetProgress(PromiseRef sender, ref Fixed32 progress, out PromiseSingleAwaitWithProgress nextRef, ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    _smallFields._reportingProgress = true;
                    // InterlockedTrySetAndGetDifference has a MemoryBarrier in it, so we know _owner is read after _reportingProgress is written.
                    uint dif;
                    bool didSet = _smallFields._currentProgress.InterlockedTrySetAndGetDifference(progress, out dif);
                    var owner = _owner;
                    if (didSet & owner != null)
                    {
                        _target.IncrementProgress(dif, progress, Fixed32.FromWhole(_smallFields._depth), ref executionScheduler);
                    }
                    _smallFields._reportingProgress = false;
                    nextRef = null;
                }

                partial void WaitWhileProgressIsBusy()
                {
                    Thread.MemoryBarrier(); // Make sure any writes happen before reading the flags.
                    SpinWait spinner = new SpinWait();
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                    System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
#endif
                    while (_smallFields._reportingProgress | _smallFields._settingInitialProgress)
                    {
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                        if (stopwatch.Elapsed.TotalSeconds > 1)
                        {
                            throw new TimeoutException();
                        }
#endif
                        spinner.SpinOnce();
                    }
                }

                void IProgressListener.ResolveOrSetProgress(PromiseRef sender, Fixed32 progress, ref ExecutionScheduler executionScheduler)
                {
                    Release();
                }

                void IProgressListener.MaybeCancelProgress(Fixed32 progress)
                {
                    _smallFields._currentProgress.InterlockedSuspendIfOtherWholeIsGreater(progress);
                    Release();
                }

                void IProgressListener.Retain()
                {
                    Retain();
                }

                [MethodImpl(InlineOption)]
                internal uint GetProgressDifferenceToCompletion(out Fixed32 progress)
                {
                    ThrowIfInPool(this);
                    progress = _smallFields._currentProgress;
                    Fixed32 incrementedWhole = Fixed32.FromWholePlusOne(_smallFields._depth);
                    return incrementedWhole.GetRawValue() - _smallFields._currentProgress.GetRawValue();
                }

                [MethodImpl(InlineOption)]
                partial void ResetProgress(ushort depth)
                {
                    _smallFields._currentProgress = default(Fixed32);
                    _smallFields._depth = depth;
                }

                internal ushort Depth
                {
                    [MethodImpl(InlineOption)]
                    get { return _smallFields._depth; }
                }
            } // PromisePassThrough

#if CSHARP_7_3_OR_NEWER
            // We have to use a pass-through for cancelation purposes. If we used the AsyncPromiseRef directly as the listener,
            // the promise chain could be broken and it still subscribed on another promise lower in the chain,
            // then subscribing to the next `await`ed promise would cause problems with it being subscribed to multiple promise chains simultaneously.
            // Using a pass-through is able to check for cancelations and not report the progress.
#if !PROTO_PROMISE_DEVELOPER_MODE
            [System.Diagnostics.DebuggerNonUserCode]
#endif
            internal sealed partial class AsyncProgressPassThrough :
#if PROMISE_DEBUG
                PromiseRef, // For circular promise await detection (promise waiting on itself).
#endif
                IProgressListener, ILinked<AsyncProgressPassThrough>
            {
                partial struct ProgressSmallFields
                {
                    internal void InterlockedRetain()
                    {
                        unchecked
                        {
                            Thread.MemoryBarrier();
                            ProgressSmallFields initialValue = default(ProgressSmallFields), newValue = default(ProgressSmallFields);
                            do
                            {
                                initialValue._intValue = _intValue;
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                                if (initialValue._retainCounter == ushort.MaxValue)
                                {
                                    throw new OverflowException();
                                }
#endif
                                newValue._intValue = initialValue._intValue;
                                ++newValue._retainCounter;
                            } while (Interlocked.CompareExchange(ref _intValue, newValue._intValue, initialValue._intValue) != initialValue._intValue);
                        }
                    }

                    internal bool InterlockedTryReleaseComplete()
                    {
                        unchecked
                        {
                            Thread.MemoryBarrier();
                            ProgressSmallFields initialValue = default(ProgressSmallFields), newValue = default(ProgressSmallFields);
                            do
                            {
                                initialValue._intValue = _intValue;
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                                if (initialValue._retainCounter == 0)
                                {
                                    throw new OverflowException();
                                }
#endif
                                newValue._intValue = initialValue._intValue;
                                --newValue._retainCounter;
                            } while (Interlocked.CompareExchange(ref _intValue, newValue._intValue, initialValue._intValue) != initialValue._intValue);
                            return newValue._retainCounter == 0;
                        }
                    }

                    internal ushort GetCurrentRetains()
                    {
                        return _retainCounter;
                    }
                }

                internal static AsyncProgressPassThrough GetOrCreate(AsyncPromiseRef target, ushort expectedProgress, object previous)
                {
                    var passThrough = ObjectPool<AsyncProgressPassThrough>.TryTake<AsyncProgressPassThrough>()
                        ?? new AsyncProgressPassThrough();
                    passThrough._target = target;
                    passThrough._progressSmallFields._currentProgress = default(Fixed32);
                    passThrough._progressSmallFields._expectedProgress = expectedProgress;
#if PROMISE_DEBUG
                    passThrough._valueOrPrevious = previous;
                    passThrough._smallFields.InterlockedSetFlags(PromiseFlags.WasAwaitedOrForgotten | PromiseFlags.SuppressRejection);
#endif
                    return passThrough;
                }

                ~AsyncProgressPassThrough()
                {
                    if (_progressSmallFields.GetCurrentRetains() != 0)
                    {
                        // For debugging. This should never happen.
                        string message = "An AsyncProgressPassThrough was garbage collected without it being released."
                            + " _retainCounter: " + _progressSmallFields.GetCurrentRetains() + ", _target: " + _target
                            + ", _currentProgress: " + _progressSmallFields._currentProgress.ToDouble()
                            + ", _expectedProgress: " + _progressSmallFields._expectedProgress;
                        AddRejectionToUnhandledStack(new UnreleasedObjectException(message), _target);
                    }
                }

                void IProgressListener.SetInitialProgress(PromiseRef sender, Promise.State state, ref Fixed32 progress, out PromiseSingleAwaitWithProgress nextRef, ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    var target = _target;
                    if (state == Promise.State.Pending)
                    {
                        if (_progressSmallFields._currentProgress.InterlockedTrySet(progress))
                        {
                            target.SetProgress(ref progress, out nextRef);
                            return;
                        }
                    }
                    else
                    {
                        _progressSmallFields._currentProgress.MaybeSuspend();
                        MaybeDispose();
                    }
                    nextRef = null;
                }

                void IProgressListener.SetProgress(PromiseRef sender, ref Fixed32 progress, out PromiseSingleAwaitWithProgress nextRef, ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    if (_progressSmallFields._currentProgress.InterlockedTrySet(progress))
                    {
                        _target.SetProgress(ref progress, out nextRef);
                    }
                    else
                    {
                        nextRef = null;
                    }
                }

                void IProgressListener.ResolveOrSetProgress(PromiseRef sender, Fixed32 progress, ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    bool isComplete = progress.WholePart == _progressSmallFields._expectedProgress;
                    bool didSet = _progressSmallFields._currentProgress.InterlockedTrySetFromResolve(progress);
                    var target = _target;
                    MaybeDispose();
                    // Don't set progress if this is complete.
                    if (didSet & !isComplete)
                    {
                        target.SetProgress(progress, ref executionScheduler);
                    }
                }

                void IProgressListener.MaybeCancelProgress(Fixed32 progress)
                {
                    ThrowIfInPool(this);
                    _progressSmallFields._currentProgress.InterlockedSuspendIfOtherWholeIsGreater(progress);
                    MaybeDispose();
                }

                void IProgressListener.Retain()
                {
                    ThrowIfInPool(this);
                    _progressSmallFields.InterlockedRetain();
                }

                internal void MarkComplete(Fixed32 expectedProgress)
                {
                    // Setting the progress to the expected progress will prevent any other progress updates from promises lower in the promise chain after a cancelation has broken the chain.
                    _progressSmallFields._currentProgress.InterlockedTrySetFromResolve(expectedProgress);
                }

#if PROMISE_DEBUG
                new
#endif
                    private void MaybeDispose()
                {
                    if (_progressSmallFields.InterlockedTryReleaseComplete())
                    {
                        Dispose();
                    }
                }

#if PROMISE_DEBUG
                new
#endif
                    internal void Dispose()
                {
#if PROMISE_DEBUG
                    _valueOrPrevious = null;
#endif
                    _target = null;
                    ObjectPool<AsyncProgressPassThrough>.MaybeRepool(this);
                }

#if PROMISE_DEBUG
                protected override void MarkAwaited(short promiseId, PromiseFlags flags) { throw new System.InvalidOperationException(); }
                internal override PromiseRef GetDuplicate(short promiseId, ushort depth) { throw new System.InvalidOperationException(); }
                internal override void AddWaiter(HandleablePromiseBase waiter, ref ExecutionScheduler executionScheduler) { throw new System.InvalidOperationException(); }
                internal override void Handle(ref ExecutionScheduler executionScheduler) { throw new System.InvalidOperationException(); }
#endif
            }

            partial class AsyncPromiseRef
            {
                [MethodImpl(InlineOption)]
                private static double Lerp(double a, double b, double t)
                {
                    return a + (b - a) * t;
                }

                private Fixed32 LerpProgress(Fixed32 progress)
                {
                    ThrowIfInPool(this);
                    double normalizedProgress = progress.ToDouble() / _progressAndSubscribeFields._previousDepthAndFlags.GetPreviousDepthPlusOne();
                    double newValue = Lerp(_minProgress, _maxProgress, normalizedProgress);
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                    if (newValue < 0 || newValue >= 1)
                    {
                        throw new ArithmeticException("Async progress calculated outside allowed bounds of [0, 1), value: " + newValue
                            + ", progress: " + progress.ToDouble() + ", depthPlusOne: " + _progressAndSubscribeFields._previousDepthAndFlags.GetPreviousDepthPlusOne()
                            + ", _minProgress: " + _minProgress + ", _maxProgress: " + _maxProgress);
                    }
#endif
                    return _progressAndSubscribeFields._currentProgress.SetNewDecimalPartFromDeferred(newValue);
                }

                internal void SetProgress(ref Fixed32 progress, out PromiseSingleAwaitWithProgress nextRef)
                {
                    progress = LerpProgress(progress);
                    nextRef = this;
                }

                internal void SetProgress(Fixed32 progress, ref ExecutionScheduler executionScheduler)
                {
                    ReportProgress(LerpProgress(progress), ref executionScheduler);
                }

                private void SetAwaitedComplete(PromiseRef owner, ValueContainer valueContainer, ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    var oldFlags = _progressAndSubscribeFields._previousDepthAndFlags.InterlockedUnsetFlags(ProgressSubscribeFlags.AboutToSetPrevious);
                    var oldPrevious = _valueOrPrevious;
                    _valueOrPrevious = null;
                    bool wasListeningToProgress = (oldFlags & ProgressSubscribeFlags.AboutToSetPrevious) != 0;
                    if (wasListeningToProgress)
                    {
                        Fixed32 expectedProgress = Fixed32.FromWhole(_progressAndSubscribeFields._previousDepthAndFlags.GetPreviousDepthPlusOne());
                        ((AsyncProgressPassThrough) oldPrevious).MarkComplete(expectedProgress);
                        // Don't report progress if it's 1. That will be reported when the async promise is resolved.
                        // Also don't report if the awaited promise was rejected or canceled.
                        if (valueContainer.GetState() == Promise.State.Resolved & _maxProgress < 1f)
                        {
                            ReportProgress(Fixed32.FromDecimalForAsync(_maxProgress), ref executionScheduler);
                        }
                    }
                }

                // SetPreviousAndMaybeSubscribeProgress may be called multiple times, but never concurrently with itself,
                // while MaybeAddProgressListenerAndGetPreviousRetained will only be called once, but may be called concurrently with SetPreviousAndMaybeSubscribeProgress
                [MethodImpl(InlineOption)]
                internal void SetPreviousAndMaybeSubscribeProgress(PromiseRef other, ushort depth, float minProgress, float maxProgress, ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    lock (this) // Unfortunately, I couldn't figure out a lock-free solution to thread synchronization.
                    {
                        _progressAndSubscribeFields._previousDepthAndFlags.InterlockedSetFlags(ProgressSubscribeFlags.AboutToSetPrevious);
                        _valueOrPrevious = other;
                        // Lazy subscribe: only subscribe to the awaited promise if a progress listener is added to this (this keeps execution and memory more efficient when progress isn't used).
                        if (_progressListener == null)
                        {
                            // These must be set inside the lock so they will be visible if/when the listener is subscribed in MaybeAddProgressListenerAndGetPreviousRetained.
                            _progressAndSubscribeFields._previousDepthAndFlags.SetPreviousDepth(depth);
                            _minProgress = minProgress;
                            _maxProgress = maxProgress;
                            return;
                        }
                    }
                    // Exit the lock before subscribing the new listener.
                    _minProgress = minProgress;
                    _maxProgress = maxProgress;
                    var passthrough = AsyncProgressPassThrough.GetOrCreate(this, _progressAndSubscribeFields._previousDepthAndFlags.GetPreviousDepthPlusOne(), other);
                    // So the passthrough can be marked completed when the awaited promise completes without adding a new field.
                    _valueOrPrevious = passthrough;
                    other.SubscribeListener(passthrough, Fixed32.FromWhole(depth), ref executionScheduler);
                }

                protected override sealed PromiseRef MaybeAddProgressListenerAndGetPreviousRetained(ref IProgressListener progressListener, ref Fixed32 lastKnownProgress)
                {
                    ThrowIfInPool(this);
                    progressListener.Retain();
                    object previous;
                    ProgressSubscribeFlags oldFlags;
                    // Mark subscribing to prevent repooling while we get previous, then unmark after we have retained previous.
                    _smallFields.InterlockedSetFlags(PromiseFlags.Subscribing);
                    lock (this) // Unfortunately, I couldn't figure out a lock-free solution to thread synchronization.
                    {
                        SetProgressListener(progressListener);
                        // Lazy subscribe: only subscribe to the awaited promise if a progress listener is added to this (this keeps execution and memory more efficient when progress isn't used).
                        previous = _valueOrPrevious;
                        oldFlags = _progressAndSubscribeFields._previousDepthAndFlags.GetFlags();
                    }
                    // Exit the lock once we have read previous before subscribing the new listener.
                    // If previousRef is null, this is either invoking the async state machine, or has awaited a non-promise awaitable, or has already completed.
                    PromiseRef previousRef = previous as PromiseRef;
                    // Don't subscribe to the previous promise if it was not awaited with the AwaitWithProgress API.
                    bool hasAwaitedPrevious = (oldFlags & ProgressSubscribeFlags.AboutToSetPrevious) != 0 & previousRef != null;
                    if (hasAwaitedPrevious)
                    {
                        previousRef.InterlockedRetainDisregardId();
                    }
                    _smallFields.InterlockedUnsetFlags(PromiseFlags.Subscribing);
                    if (hasAwaitedPrevious)
                    {
                        var passthrough = AsyncProgressPassThrough.GetOrCreate(this, _progressAndSubscribeFields._previousDepthAndFlags.GetPreviousDepthPlusOne(), previous);
                        // So the passthrough can be marked completed when the awaited promise completes without adding a new field.
                        if (Interlocked.CompareExchange(ref _valueOrPrevious, passthrough, previous) != previous)
                        {
                            // previous was changed, which means the awaited promise completed on another thread.
                            passthrough.Dispose();
                            previousRef.MaybeDispose();
                            return null;
                        }
                        progressListener = passthrough;
                    }
                    else
                    {
                        lastKnownProgress = Fixed32.FromWhole(Depth);
                        previousRef = null;
                    }
                    return previousRef;
                }
            } // AsyncPromiseRef
#endif // CSHARP_7_3_OR_NEWER
#endif // PROMISE_PROGRESS
        } // PromiseRef
    } // Internal
}