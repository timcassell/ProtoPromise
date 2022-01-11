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
    public static class Dummy
    {
        private static readonly System.Collections.Generic.List<ValueTuple<Thread, string, object[]>> lines = new System.Collections.Generic.List<ValueTuple<Thread, string, object[]>>();

        public static void Clear()
        {
            lock(lines)
            {
                lines.Clear();
            }
        }

        public static void WriteLine(string format, params object[] args)
        {
            lock(lines)
            {
                lines.Add(ValueTuple.Create(Thread.CurrentThread, format, args));
            }
        }

        public static string Read()
        {
            lock(lines)
            {
                var sb = new System.Text.StringBuilder();
                foreach (var line in lines)
                {
                    sb.AppendLine();
                    sb.AppendFormat("Thread ID: " + line.Item1.ManagedThreadId + ", Background: " + line.Item1.IsBackground + ", " + line.Item2, line.Item3);

                }
                return sb.ToString();
            }
        }
    }

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
                Thread.MemoryBarrier(); // Make sure any writes happen before we read progress flags.
                // Wait until progressFlags are unset.
                // This is used to make sure promises and progress listeners aren't disposed while still in use on another thread.
                SpinWait spinner = new SpinWait();
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
#endif
                while (_smallFields.AreFlagsSet(progressFlags))
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
                // Extra flags are necessary to update the value and the flags atomically without a lock.
                // Unfortunately, this digs into how how many promises can be chained, but it should still be large enough for most use cases.
                private const int SuspendedFlag = 1 << 31;
                internal const int ReportedFlag = 1 << 30;
                internal const int PriorityFlag = 1 << 29; // Priority is true when called from `Deferred.ReportProgress` or when a promise is resolved, false when called from `Promise.Progress`.
                private const int FlagsMask = SuspendedFlag | ReportedFlag | PriorityFlag;
                private const int ValueMask = ~FlagsMask;
                private const double DecimalMax = 1 << Promise.Config.ProgressDecimalBits;
                private const int DecimalMask = (1 << Promise.Config.ProgressDecimalBits) - 1;
                private const int WholeMask = ValueMask & ~DecimalMask;

                private volatile int _value; // int for Interlocked.

                public override string ToString()
                {
                    return "{ DoubleValue: " + ToDouble()
                        + ", HasReported: " + HasReported
                        + ", IsPriority: " + IsPriority
                        + ", IsSuspended: " + IsSuspended + " }";
                }

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
                    get { return (_value & WholeMask) >> Promise.Config.ProgressDecimalBits; }
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
                internal static int GetNextDepth(int depth)
                {
#if PROMISE_DEBUG
                    int newDepth = depth + 1;
                    int checkVal = newDepth & (WholeMask >> Promise.Config.ProgressDecimalBits);
                    if (newDepth != checkVal)
                    {
                        throw new OverflowException();
                    }
                    return newDepth;
#else
                    return depth + 1;
#endif
                }

                [MethodImpl(InlineOption)]
                internal uint GetRawValue()
                {
                    unchecked
                    {
                        return (uint) (_value & ValueMask);
                    }
                }

                internal double ToDouble()
                {
                    int val = _value;
                    double wholePart = (val & WholeMask) >> Promise.Config.ProgressDecimalBits;
                    double decimalPart = (double) (val & DecimalMask) / DecimalMax;
                    return wholePart + decimalPart;
                }

                [MethodImpl(InlineOption)]
                internal static Fixed32 GetScaled(UnsignedFixed64 value, double scaler)
                {
                    // Don't bother rounding, we don't want to accidentally round to 1.0.
                    int newValue = (int) (value.ToDouble() * scaler * DecimalMax);
                    unchecked
                    {
                        return new Fixed32(newValue | (int) (value.GetPriorityFlag() >> 32), true);
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
                    return new Fixed32(newValue, true);
                }

                [MethodImpl(InlineOption)]
                internal bool InterlockedTrySetDecimalPart(double decimalPart, Fixed32 otherFlags)
                {
                    Thread.MemoryBarrier();
                    // Don't bother rounding, we don't want to accidentally round to 1.0.
                    int newDecimalPart = (int) (decimalPart * DecimalMax);
                    int otherPriorityFlag = otherFlags._value & PriorityFlag;
                    bool otherIsPriority = otherPriorityFlag != 0;
                    int current, newValue;
                    bool success;
                    do
                    {
                        current = _value;
                        int currentDecimalPart = current & DecimalMask;
                        bool currentIsSuspended = (current & SuspendedFlag) != 0;
                        bool currentHasReported = (current & ReportedFlag) != 0;
                        if (newDecimalPart < currentDecimalPart
                            | (currentIsSuspended & newDecimalPart == currentDecimalPart)
                            | (!otherIsPriority & currentHasReported & newDecimalPart <= currentDecimalPart))
                        {
                            // Quit early if this has already reported.
                            if (currentHasReported)
                            {
                                return false;
                            }
                            success = false;
                            // Just update reported flag
                            newValue = current | ReportedFlag;
                        }
                        else
                        {
                            success = true;
                            newValue = (current & WholeMask) | newDecimalPart | ReportedFlag | otherPriorityFlag;
                        }
                    } while (Interlocked.CompareExchange(ref _value, newValue, current) != current);
                    return success;
                }

                internal bool InterlockedTrySet(Fixed32 other, out Fixed32 oldProgress)
                {
                    int oldValue;
                    bool didSet = InterlockedTrySet(other, out oldValue);
                    oldProgress = new Fixed32(oldValue, true);
                    return didSet;
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
                        int currentWholePart = (current & WholeMask) >> Promise.Config.ProgressDecimalBits;
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
                        int currentWholePart = (current & WholeMask) >> Promise.Config.ProgressDecimalBits;
                        // If other whole is less than or equal, or if the updated whole is greater than the old whole, do nothing.
                        if (otherWholePart <= currentWholePart | oldWholePart < currentWholePart)
                        {
                            return;
                        }
                        newValue = current | SuspendedFlag;
                    } while (Interlocked.CompareExchange(ref _value, newValue, current) != current);
                }

                [MethodImpl(InlineOption)]
                internal void InterlockedMaybeSuspendIfDecimalIsNotGreater()
                {
                    // Try to mark suspended, only retry if the updated decimal value is not greater than the old decimal value.
                    int current = _value;
                    int oldDecimalPart = current & DecimalMask;
                Retry:
                    if (Interlocked.CompareExchange(ref _value, current | SuspendedFlag, current) != current)
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

                [MethodImpl(InlineOption)]
                internal Fixed32 GetIncrementedWholeTruncated()
                {
                    return new Fixed32(GetIncrementedWholeTruncatedValue(), true);
                }

                [MethodImpl(InlineOption)]
                internal Fixed32 GetIncrementedWholeTruncatedForResolve()
                {
                    return new Fixed32(GetIncrementedWholeTruncatedValue() | PriorityFlag, true);
                }

                private int GetIncrementedWholeTruncatedValue()
                {
#if PROMISE_DEBUG
                    checked
#endif
                    {
                        int value = _value;
                        int currentReportedAndPriorityFlags = value & ReportedFlag & PriorityFlag;
                        int newValue = (value & WholeMask | currentReportedAndPriorityFlags) + (1 << Promise.Config.ProgressDecimalBits);
                        return newValue;
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
                internal const long PriorityFlag = ((long) Fixed32.PriorityFlag) << 32;
                private const double DecimalMax = 1L << Promise.Config.ProgressDecimalBits;
                private const long DecimalMask = (1L << Promise.Config.ProgressDecimalBits) - 1L;
                private const long WholeMask = ~PriorityFlag & ~DecimalMask;

                private long _value; // long for Interlocked.

                [MethodImpl(InlineOption)]
                internal UnsignedFixed64(ulong wholePart)
                {
                    unchecked
                    {
                        _value = (long) (wholePart << Promise.Config.ProgressDecimalBits);
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
                        double wholePart = (val & WholeMask) >> Promise.Config.ProgressDecimalBits;
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
            internal sealed partial class PromiseProgress<TProgress> : PromiseSingleAwaitWithProgress, IProgressListener, IProgressInvokable, ICancelable, ITreeHandleable
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

                private static int idCount;
                private int id;

                private PromiseProgress() {
                    id = Interlocked.Increment(ref idCount);
                }

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
                    promise._synchronizationContext = synchronizationContext;
                    cancelationToken.TryRegister(promise, out promise._cancelationRegistration);
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
                    Thread.MemoryBarrier(); // Make sure we're reading fresh progress (since the field cannot be marked volatile).
                    var progress = _smallProgressFields._currentProgress;
                    _smallFields.InterlockedUnsetFlags(PromiseFlags.InProgressQueue);
                    // Calculate the normalized progress for the depth that the listener was added.
                    // Use double for better precision.
                    double expected = _smallProgressFields._depthAndProgress.WholePart + 1u;
                    float value = (float) (progress.ToDouble() / expected);
                    bool invoke = !progress.IsSuspended & !IsComplete & !IsCanceled & !_cancelationRegistration.Token.IsCancelationRequested;
                    if (invoke)
                    {
                        CallbackHelper.InvokeAndCatchProgress(_progress, value, this);
                    }
                    Dummy.WriteLine("PromiseProgress id: {0} Invoke, invoke: {1}, IsComplete: {2}, IsCanceled: {3}, progress: {4}", id, invoke, IsComplete, IsCanceled, progress);
                    MaybeDispose();
                }

                private bool TrySetProgress(Fixed32 progress, ref ExecutionScheduler executionScheduler)
                {
                    Fixed32 oldProgress;
                    bool needsInvoke = _smallProgressFields._currentProgress.InterlockedTrySet(progress, out oldProgress);
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
                        //else
                        {
                            Dummy.WriteLine("PromiseProgress id: {0} TrySetProgress, needsInvoke: {1}, inProgressQueue: {2}, progress: {3}, oldProgress: {4}", id, needsInvoke, inProgressQueue, progress, oldProgress);
                        }
                        return true;
                    }
                    Dummy.WriteLine("PromiseProgress id: {0} TrySetProgress, needsInvoke: {1}, progress: {2}, oldProgress: {3}", id, needsInvoke, progress, oldProgress);
                    return false;
                }

                void IProgressListener.SetProgress(PromiseRef sender, Fixed32 progress, out PromiseSingleAwaitWithProgress nextRef, ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    nextRef = TrySetProgress(progress, ref executionScheduler) ? this : null;
                }

                void IProgressListener.ResolveOrSetProgress(PromiseRef sender, Fixed32 progress, ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    if (!IsComplete)
                    {
                        TrySetProgress(progress, ref executionScheduler);
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
                            _smallProgressFields._currentProgress.MaybeSuspend();
                            MaybeDispose();
                            break;
                        }
                    }
                }

                private bool TrySetInitialProgressAndMarkInQueue(Fixed32 progress)
                {
                    Fixed32 oldProgress;
                    bool didSet = _smallProgressFields._currentProgress.InterlockedTrySet(progress, out oldProgress);
                    bool notInQueue = didSet && (_smallFields.InterlockedSetFlags(PromiseFlags.InProgressQueue) & PromiseFlags.InProgressQueue) == 0; // Was not already in progress queue?
                    //if (!notInQueue)
                    {
                        Dummy.WriteLine("PromiseProgress id: {0} TrySetInitialProgressAndMarkInQueue, didSet: {1}, notInQueue: {2}, progress: {3}, oldProgress: {4}", id, didSet, notInQueue, progress, oldProgress);
                    }
                    return notInQueue;
                    //return _smallProgressFields._currentProgress.InterlockedTrySet(progress)
                    //    && (_smallFields.InterlockedSetFlags(PromiseFlags.InProgressQueue) & PromiseFlags.InProgressQueue) == 0; // Was not already in progress queue?
                }

                void IProgressListener.MaybeCancelProgress(Fixed32 progress)
                {
                    ThrowIfInPool(this);
                    _smallProgressFields._currentProgress.InterlockedSuspendIfOtherWholeIsGreater(progress);
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
                    bool didUnregister = TryUnregisterAndIsNotCanceling(ref _cancelationRegistration);
                    bool notCanceled = didUnregister & !IsCanceled;

                    // HandleSelf
                    IValueContainer valueContainer = (IValueContainer) _valueOrPrevious;
                    Promise.State state = valueContainer.GetState();
                    bool invoke = state == Promise.State.Resolved & notCanceled;
                    if (invoke)
                    {
                        CallbackHelper.InvokeAndCatchProgress(_progress, 1f, this);
                    }
                    Dummy.WriteLine("PromiseProgress id: {0} Handle, invoke: {1}, state: {2}, didUnregister: {3}, IsCanceled: {4}", id, invoke, state, didUnregister, IsCanceled);
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
                    HandleProgressListener(state, _smallProgressFields._depthAndProgress.GetIncrementedWholeTruncatedForResolve(), ref executionScheduler);
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
                [MethodImpl(InlineOption)]
                protected void SetProgressListener(IProgressListener progressListener)
                {
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                    var oldListener = Interlocked.CompareExchange(ref _progressListener, progressListener, null);
                    if (oldListener != null)
                    {
                        System.Diagnostics.Debugger.Launch();
                        throw new System.InvalidOperationException("Cannot add more than 1 progress listener."
                            + "\nAttempted to add listener: " + progressListener
                            + "\nexisting listener: " + oldListener);
                    }
#else
                    _progressListener = progressListener;
#endif
                }

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
                        WaitWhileProgressFlags(PromiseFlags.ReportingPriority | PromiseFlags.ReportingInitial | PromiseFlags.SettingInitial);
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
                            Dummy.WriteLine("PromiseSingleAwaitWithProgress ReportProgress, failed to set flag: {0} , progress: {1}", setFlag, progress);
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
                        unsetter._smallFields.InterlockedUnsetFlags(setFlag);
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
                    WaitWhileProgressFlags(PromiseFlags.ReportingPriority | PromiseFlags.ReportingInitial | PromiseFlags.SettingInitial);

                    Fixed32 progress = _progressAndLocker._depthAndProgress.GetIncrementedWholeTruncatedForResolve();
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
                    // If this is coming from hookup progress, we can possibly report without updating the progress.
                    // This is to handle race condition on separate threads.
                    Fixed32 oldProgress;
                    bool didSet = _progressAndLocker._currentProgress.InterlockedTrySet(progress, out oldProgress);
                    if (didSet | !progress.IsPriority)
                    {
                        bool notInQueue = (_smallFields.InterlockedSetFlags(PromiseFlags.InProgressQueue) & PromiseFlags.InProgressQueue) == 0; // Was not already in progress queue?
                        if (notInQueue)
                        {
                            InterlockedRetainDisregardId();
                            executionScheduler.ScheduleProgressSynchronous(this);
                        }
                        //else
                        {
                            Dummy.WriteLine("PromiseMultiAwait SetProgress didSet: {0}, notInQueue: {1}, progress: {2}, oldProgress: {3}", didSet, notInQueue, progress, oldProgress);
                        }
                        return;
                    }
                    Dummy.WriteLine("PromiseMultiAwait SetProgress didSet: {0}, progress: {1}, oldProgress: {2}", didSet, progress, oldProgress);
                }

                void IProgressListener.MaybeCancelProgress(Fixed32 progress)
                {
                    _progressAndLocker._currentProgress.InterlockedSuspendIfOtherWholeIsGreater(progress);
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
                            _progressAndLocker._currentProgress.MaybeSuspend();
                            MaybeDispose();
                            break;
                        }
                    }
                }

                private bool TrySetInitialProgressAndMarkInQueue(Fixed32 progress)
                {
                    Fixed32 oldProgress;
                    bool didSet = _progressAndLocker._currentProgress.InterlockedTrySet(progress, out oldProgress);
                    bool notInQueue = didSet && (_smallFields.InterlockedSetFlags(PromiseFlags.InProgressQueue) & PromiseFlags.InProgressQueue) == 0; // Was not already in progress queue?
                    //if (!notInQueue)
                    {
                        Dummy.WriteLine("PromiseMultiAwait TrySetInitialProgressAndMarkInQueue didSet: {0}, notInQueue: {1}, progress: {2}, oldProgress: {3}", didSet, notInQueue, progress, oldProgress);
                    }
                    return notInQueue;
                    //return _progressAndLocker._currentProgress.InterlockedTrySet(progress)
                    //    && (_smallFields.InterlockedSetFlags(PromiseFlags.InProgressQueue) & PromiseFlags.InProgressQueue) == 0; // Was not already in progress queue?
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
                    if (!progress.IsSuspended)
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
                protected override bool GetIsProgressSuspended()
                {
                    return _progressAndLocker._currentProgress.IsSuspended;
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
                    SetProgressListener(progressListener);
                    return null;
                }

                protected override sealed void SetInitialProgress(IProgressListener progressListener, Fixed32 lastKnownProgress, ref ExecutionScheduler executionScheduler)
                {
                    SetInitialProgress(progressListener, _currentProgress, _currentProgress.GetIncrementedWholeTruncated(), ref executionScheduler);
                }

                internal override sealed void HandleProgressListener(Promise.State state, ref ExecutionScheduler executionScheduler)
                {
                    HandleProgressListener(state, _currentProgress.GetIncrementedWholeTruncatedForResolve(), ref executionScheduler);
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
                        var newProgress = _currentProgress.SetNewDecimalPartFromDeferred(progress);
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
                [Flags]
                private enum WaitFlags
                {
                    SecondPrevious = 1 << 31,
                    AboutToSubscribe = 1 << 30,
                    Subscribed = 1 << 29
                }

                partial struct PromiseWaitSmallFields
                {
                    private const int FlagsMask = (int) (WaitFlags.SecondPrevious | WaitFlags.AboutToSubscribe | WaitFlags.Subscribed);
                    private const int DepthMask = ~FlagsMask;

                    internal WaitFlags InterlockedSetPreviousDepthAndAboutToSubscribeFlag(int previousDepth)
                    {
                        ++previousDepth;
                        Thread.MemoryBarrier();
                        int current, newValue;
                        do
                        {
                            current = _previousDepthPlusOneAndFlags;
                            newValue = (current & FlagsMask) | previousDepth | (int) WaitFlags.AboutToSubscribe;
                        } while (Interlocked.CompareExchange(ref _previousDepthPlusOneAndFlags, newValue, current) != current);
                        return (WaitFlags) (current & FlagsMask);
                    }

                    internal WaitFlags InterlockedSetFlags(WaitFlags flags)
                    {
                        Thread.MemoryBarrier();
                        int current, newValue;
                        do
                        {
                            current = _previousDepthPlusOneAndFlags;
                            newValue = current | (int) flags;
                        } while (Interlocked.CompareExchange(ref _previousDepthPlusOneAndFlags, newValue, current) != current);
                        return (WaitFlags) (current & FlagsMask);
                    }

                    [MethodImpl(InlineOption)]
                    internal int GetPreviousDepthPlusOne()
                    {
                        return _previousDepthPlusOneAndFlags & DepthMask;
                    }

                    [MethodImpl(InlineOption)]
                    internal void Reset(int depth)
                    {
                        _previousDepthPlusOneAndFlags = 0;
                        _depthAndProgress = new Fixed32(depth);
                    }
                }

                [MethodImpl(InlineOption)]
                protected void Reset(int depth)
                {
                    _progressFields.Reset(depth);
                    Reset();
                }

                [MethodImpl(InlineOption)]
                protected override sealed bool GetIsProgressSuspended()
                {
                    return _progressFields._depthAndProgress.IsSuspended;
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
                private void SetPreviousAndSubscribeProgress(PromiseRef other, int depth, ref ExecutionScheduler executionScheduler)
                {
                    // Write SecondPrevious flag before writing previous to fix race condition with hookup MaybeAddProgressListenerAndGetPreviousRetained.
                    _progressFields.InterlockedSetFlags(WaitFlags.SecondPrevious);
                    _valueOrPrevious = other;

                    // Lazy subscribe: only subscribe to second previous if a progress listener is added to this (this keeps execution more efficient when progress isn't used).
                    WaitFlags oldFlags = _progressFields.InterlockedSetPreviousDepthAndAboutToSubscribeFlag(depth);
                    bool hasListener = (oldFlags & WaitFlags.AboutToSubscribe) != 0;
                    if (hasListener)
                    {
                        oldFlags = _progressFields.InterlockedSetFlags(WaitFlags.Subscribed);
                        bool notSubscribed = (oldFlags & WaitFlags.Subscribed) == 0;
                        if (notSubscribed)
                        {
                            other.SubscribeListener(this, new Fixed32(depth), ref executionScheduler);
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
                    WaitFlags oldFlags = _progressFields.InterlockedSetFlags(WaitFlags.AboutToSubscribe);
                    PromiseRef previous;
                    bool hasSecondPrevious = (oldFlags & WaitFlags.AboutToSubscribe) != 0;
                    if (hasSecondPrevious)
                    {
                        lastKnownProgress = new Fixed32(_progressFields.GetPreviousDepthPlusOne() - 1);
                        oldFlags = _progressFields.InterlockedSetFlags(WaitFlags.Subscribed);
                        bool alreadySubscribed = (oldFlags & WaitFlags.Subscribed) != 0;
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
                        lastKnownProgress = _progressFields._depthAndProgress;
                        bool notAboutToSetSecondPrevious = (oldFlags & WaitFlags.SecondPrevious) == 0;
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
                            _progressFields._depthAndProgress.MaybeSuspend();
                            MaybeDispose();
                            break;
                        }
                    }
                }

                private bool TrySetInitialProgressAndMarkInQueue(Fixed32 progress)
                {
                    return _progressFields._depthAndProgress.InterlockedTrySetDecimalPart(NormalizeProgress(progress), progress)
                        && (_smallFields.InterlockedSetFlags(PromiseFlags.InProgressQueue) & PromiseFlags.InProgressQueue) == 0; // Was not already in progress queue?
                }

                private double NormalizeProgress(Fixed32 progress)
                {
                    // Calculate the normalized progress for the depth of the returned promise.
                    // Use double for better precision.
                    return progress.ToDouble() / (double) _progressFields.GetPreviousDepthPlusOne();
                }

                private void SetProgressAndMaybeAddToQueue(Fixed32 progress, ref ExecutionScheduler executionScheduler)
                {
                    if (_progressFields._depthAndProgress.InterlockedTrySetDecimalPart(NormalizeProgress(progress), progress)
                        && (_smallFields.InterlockedSetFlags(PromiseFlags.InProgressQueue) & PromiseFlags.InProgressQueue) == 0) // Was not already in progress queue?
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
                    _progressFields._depthAndProgress.InterlockedMaybeSuspendIfDecimalIsNotGreater();
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
                    if (!progress.IsSuspended)
                    {
                        ReportProgress(progress, ref executionScheduler);
                    }
                    MaybeDispose();
                }

                internal override sealed void HandleProgressListener(Promise.State state, ref ExecutionScheduler executionScheduler)
                {
                    HandleProgressListener(state, _progressFields._depthAndProgress.GetIncrementedWholeTruncatedForResolve(), ref executionScheduler);
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
                        // InterlockedTrySet has a MemoryBarrier in it, so we know _owner is read after _settingInitialProgress is written.
                        Fixed32 oldProgress;
                        bool didSet = _smallFields._currentProgress.InterlockedTrySet(progress, out oldProgress);
                        var owner = _owner;
                        if (didSet & owner != null)
                        {
                            _target.IncrementProgress(progress.GetRawValue(), progress, _smallFields._depth, ref executionScheduler);
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
                    // InterlockedTrySetAndGetDifference has a MemoryBarrier in it, so we know _owner is read after _reportingProgress is written.
                    uint dif;
                    bool didSet = _smallFields._currentProgress.InterlockedTrySetAndGetDifference(progress, out dif);
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
                    Fixed32 incrementedWhole = _smallFields._depth.GetIncrementedWholeTruncated();
                    return incrementedWhole.GetRawValue() - _smallFields._currentProgress.GetRawValue();
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