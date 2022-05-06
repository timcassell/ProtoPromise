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

#pragma warning disable IDE0018 // Inline variable declaration
#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable 0420 // A reference to a volatile field will not be treated as volatile

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
#if PROTO_PROMISE_STACK_UNWIND_DISABLE && PROTO_PROMISE_DEVELOPER_MODE
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
                // 16 bits for decimal part gives us 1/2^16 or 0.0000152587890625 step size (nearly 5 digits of precision)
                // and the remaining 16 bits for whole part/depth allows up to 2^16 - 2 or 65534 promise.Then(() => otherPromise) chains, which should be plenty for typical use cases.
                // Also, SmallFields._depth is a ushort with 16 bits, so this should not be smaller than 16 (though it can be larger, as long as it leaves some bits for the whole part).
                internal const int DecimalBits = 16;
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

            [MethodImpl(MethodImplOptions.NoInlining)]
            private void WaitWhileProgressReportingCore()
            {
                var spinner = new SpinWait();
                do
                {
                    spinner.SpinOnce();
                } while (_smallFields._reportingProgressCount != 0);
            }

            partial class PromiseCompletionSentinel : HandleablePromiseBase
            {
                internal override PromiseSingleAwait SetProgress(ref Fixed32 progress, ref ushort depth, ref ExecutionScheduler executionScheduler)
                {
                    return null;
                }
            }

            internal sealed partial class PromiseForgetSentinel : HandleablePromiseBase
            {
                internal override PromiseSingleAwait SetProgress(ref Fixed32 progress, ref ushort depth, ref ExecutionScheduler executionScheduler)
                {
                    return null;
                }
            }

            internal sealed partial class InvalidAwaitSentinel : PromiseSingleAwait
            {
                internal override PromiseSingleAwait SetProgress(ref Fixed32 progress, ref ushort depth, ref ExecutionScheduler executionScheduler)
                {
                    return null;
                }
            }

            internal partial struct Fixed32
            {
                // Necessary to fix a race condition when hooking up a promise and the promise's deferred reports progress. Deferred report takes precedence.
                [ThreadStatic]
                internal static bool ts_reportingPriority;

                private const double DecimalMax = 1 << DecimalBits;
                private const int DecimalMask = (1 << DecimalBits) - 1;
                private const int WholeMask = ~DecimalMask;

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
                    return new Fixed32(ConvertToValue(decimalValue));
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
#endif

                [MethodImpl(InlineOption)]
                internal static ushort GetNextDepth(ushort depth)
                {
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                    // We allow ushort.MaxValue to rollover for progress normalization purposes, but we don't allow overflow for regular user chains.
                    const int DepthBits = 32 - DecimalBits;
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
                        return (uint) _value;
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
                    unchecked
                    {
                        // Don't bother rounding, we don't want to accidentally round to 1.0.
                        int newValue = (int) (value.ToDouble() * scaler * DecimalMax);
                        return new Fixed32(newValue);
                    }
                }

                internal bool InterlockedTrySetIfGreater(Fixed32 other)
                {
                    Thread.MemoryBarrier();
                    int otherValue = other._value;
                    int current;
                    do
                    {
                        current = _value;
                        unchecked
                        {
                            if ((uint) otherValue <= (uint) current)
                            {
                                return false;
                            }
                        }
                    } while (Interlocked.CompareExchange(ref _value, otherValue, current) != current);
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
                            return (long) (uint) other._value - (long) (uint) oldValue;
                        }
                    }
                    return 0;
                }

                private bool InterlockedTrySet(Fixed32 other, out int oldValue)
                {
                    Thread.MemoryBarrier();
                    int otherValue = other._value;
                    bool isReportingPriority = ts_reportingPriority;
                    int current;
                    do
                    {
                        current = _value;
                        // If value is greater, always set.
                        // If value is equal, don't set.
                        // if value is less, only set if progress is being reported with priority.
                        unchecked
                        {
                            bool valueIsGreater = (uint) otherValue > (uint) current;
                            bool valueIsLess = (uint) otherValue < (uint) current;
                            bool isOkay = valueIsGreater | (valueIsLess & isReportingPriority);
                            if (!isOkay)
                            {
                                oldValue = 0;
                                return false;
                            }
                        }
                    } while (Interlocked.CompareExchange(ref _value, otherValue, current) != current);
                    oldValue = current;
                    return true;
                }

                [MethodImpl(InlineOption)]
                internal Fixed32 SetNewDecimalPartFromDeferred(double decimalPart)
                {
                    int newValue = ConvertToValue(decimalPart);
                    _value = newValue;
                    return new Fixed32(newValue);
                }

                [MethodImpl(InlineOption)]
                internal bool TrySetNewDecimalPartFromAsync(double decimalPart, out Fixed32 result)
                {
                    int newValue = ConvertToValue(decimalPart);
                    int _;
                    bool success = InterlockedTrySet(new Fixed32(newValue), out _);
                    result = new Fixed32(newValue);
                    return success;
                }

                [MethodImpl(InlineOption)]
                internal bool TrySetNewDecimalPartFromWaitPromise(double decimalPart, ushort wholePart, out Fixed32 result)
                {
                    int newValue = ConvertToValue(decimalPart) | (wholePart << DecimalBits);
                    int _;
                    bool success = InterlockedTrySet(new Fixed32(newValue), out _);
                    result = new Fixed32(newValue);
                    return success;
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
                    double dValue = ConvertToDouble(_value) * multiplier / divisor;
                    return new Fixed32(ConvertToValue(dValue));
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
                    get { return _previousState != Promise.State.Pending; }
                }

                private bool IsCanceled
                {
                    [MethodImpl(InlineOption)]
                    get { return _canceled; }
                    [MethodImpl(InlineOption)]
                    set { _canceled = value; }
                }

                private PromiseProgress() { }

                [MethodImpl(InlineOption)]
                new private void Reset(ushort depth)
                {
                    base.Reset(depth);
                    // _retainCounter is necessary to make sure the promise is disposed after the cancelation has invoked or unregistered,
                    // and the next awaited promise has been handled, and this is not invoking progress.
                    _retainCounter = 2;
                }

                internal static PromiseProgress<TProgress> GetOrCreate(TProgress progress, CancelationToken cancelationToken, ushort depth, bool isSynchronous, SynchronizationContext synchronizationContext)
                {
                    var promise = ObjectPool<HandleablePromiseBase>.TryTake<PromiseProgress<TProgress>>()
                        ?? new PromiseProgress<TProgress>();
                    promise.Reset(depth);
                    promise._progress = progress;
                    promise.IsCanceled = false;
                    promise._isSynchronous = isSynchronous;
                    promise._previousState = Promise.State.Pending;
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
                    promise._isSynchronous = false;
                    promise._previousState = Promise.State.Resolved;
                    promise._synchronizationContext = synchronizationContext;
                    promise._valueContainer = valueContainer;
                    cancelationToken.TryRegister(promise, out promise._cancelationRegistration); // Very important, must register after promise is fully setup.
                    return promise;
                }

                protected override void MaybeDispose()
                {
                    if (InterlockedAddWithOverflowCheck(ref _retainCounter, -1, 0) == 0)
                    {
                        Dispose();
                        _cancelationRegistration = default(CancelationRegistration);
                        _progress = default(TProgress);
                        ObjectPool<HandleablePromiseBase>.MaybeRepool(this);
                    }
                }

                void IProgressInvokable.Invoke(ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    Thread.MemoryBarrier(); // Make sure we're reading fresh progress (since the field cannot be marked volatile).
                    var progress = _smallFields._currentProgress;
                    _isProgressScheduled = 0;
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
#if NET_LEGACY // Interlocked.Exchange doesn't seem to work properly in Unity's old runtime. So use CompareExchange instead
                    bool isProgressScheduled = Interlocked.CompareExchange(ref _isProgressScheduled, 1, 0) != 0;
#else
                    bool isProgressScheduled = Interlocked.Exchange(ref _isProgressScheduled, 1) != 0;
#endif
                    if (!isProgressScheduled)
                    {
                        InterlockedAddWithOverflowCheck(ref _retainCounter, 1, -1);
                        if (_isSynchronous)
                        {
                            executionScheduler.ScheduleProgressSynchronous(this);
                        }
                        else
                        {
                            executionScheduler.ScheduleProgressOnContext(_synchronizationContext, this);
                        }
                    }
                }

                internal override PromiseSingleAwait SetProgress(ref Fixed32 progress, ref ushort depth, ref ExecutionScheduler executionScheduler)
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
                    Invoke1(_previousState, out nextHandler, ref executionScheduler);
                    MaybeHandleNext(nextHandler, ref executionScheduler);
                }

                internal override void Handle(ref PromiseRef handler, out HandleablePromiseBase nextHandler, ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    handler.SuppressRejection = true;
                    var state = handler.State;
                    _previousState = state;
                    _valueContainer = handler._valueContainer.Clone();
                    handler.MaybeDispose();

                    if (_isSynchronous)
                    {
                        handler = this;
                        Invoke1(state, out nextHandler, ref executionScheduler);
                        return;
                    }

                    nextHandler = null;
                    executionScheduler.ScheduleOnContext(_synchronizationContext, this);
                }

                private void Invoke1(Promise.State state, out HandleablePromiseBase nextHandler, ref ExecutionScheduler executionScheduler)
                {
                    if (TryUnregisterAndIsNotCanceling(ref _cancelationRegistration) & !IsCanceled)
                    {
                        if (state == Promise.State.Resolved)
                        {
                            CallbackHelper.InvokeAndCatchProgress(_progress, 1f, this);
                        }
                        // Release since Cancel() will not be invoked.
                        InterlockedAddWithOverflowCheck(ref _retainCounter, -1, 0);
                    }

                    State = state;
                    nextHandler = TakeOrHandleNextWaiter(ref executionScheduler);
                }

                void ICancelable.Cancel()
                {
                    ThrowIfInPool(this);
                    IsCanceled = true;
                    MaybeDispose();
                }

                internal override PromiseSingleAwait AddWaiter(short promiseId, HandleablePromiseBase waiter, out HandleablePromiseBase previousWaiter, ref ExecutionScheduler executionScheduler)
                {
                    if (_isSynchronous)
                    {
                        return AddWaiterImpl(promiseId, waiter, out previousWaiter, Depth, ref executionScheduler);
                    }

                    if (promiseId != Id)
                    {
                        previousWaiter = InvalidAwaitSentinel._instance;
                        return InvalidAwaitSentinel._instance;
                    }
                    ThrowIfInPool(this);
                    WasAwaitedOrForgotten = true;

                    var previous = CompareExchangeWaiter(waiter, null);
                    if (previous != null)
                    {
                        // We do the verification process here instead of in the caller, because we need to handle continuations on the synchronization context.
                        if (CompareExchangeWaiter(waiter, PromiseCompletionSentinel._instance) != PromiseCompletionSentinel._instance)
                        {
                            previousWaiter = InvalidAwaitSentinel._instance;
                            return InvalidAwaitSentinel._instance;
                        }

                        // If this was configured to execute progress on a SynchronizationContext or the ThreadPool, force the waiter to execute on the same context for consistency.
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
                    previousWaiter = null;
                    return this; // It doesn't matter what we return since previousWaiter is set to null.
                }

                private static void ExecuteFromContext(object state)
                {
                    // In case this is executed from a background thread, catch the exception and report it instead of crashing the app.
                    try
                    {
                        // This handles the waiter that was added after this was already complete.
                        var _this = (PromiseProgress<TProgress>) state;
                        ThrowIfInPool(_this);
                        var _state = _this._previousState;
                        _this.State = _state;
                        var executionScheduler = new ExecutionScheduler(true);
                        // We don't need to synchronize access here because this is only called when the waiter is added after Invoke1 has completed, so there are no race conditions.
                        // _this._waiter is guaranteed to be non-null here, so we can call HandleNext instead of MaybeHandleNext.
                        _this.HandleNext(_this._waiter, ref executionScheduler);
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
                internal override PromiseSingleAwait SetProgress(ref Fixed32 progress, ref ushort depth, ref ExecutionScheduler executionScheduler)
                {
                    return _smallFields._currentProgress.InterlockedTrySet(progress) ? this : null;
                }

                [MethodImpl(InlineOption)]
                internal void ReportProgress(Fixed32 progress, ushort depth, ref ExecutionScheduler executionScheduler)
                {
                    InterlockedIncrementProgressReportingCount();
                    ReportProgressAlreadyIncremented(progress, depth, ref executionScheduler);
                }

                internal void ReportProgressAlreadyIncremented(Fixed32 progress, ushort depth, ref ExecutionScheduler executionScheduler)
                {
                    PromiseSingleAwait current = this;
                    while (true)
                    {
                        var progressListener = current._waiter;
                        if (progressListener == null)
                        {
                            break;
                        }
                        var next = progressListener.SetProgress(ref progress, ref depth, ref executionScheduler);
                        if (next == null)
                        {
                            break;
                        }
                        next.InterlockedIncrementProgressReportingCount();
                        current.InterlockedDecrementProgressReportingCount();
                        current = next;
                    }
                    current.InterlockedDecrementProgressReportingCount();
                }
            } // PromiseSingleAwait

            partial class PromiseMultiAwait : IProgressInvokable
            {
                internal override PromiseSingleAwait SetProgress(ref Fixed32 progress, ref ushort depth, ref ExecutionScheduler executionScheduler)
                {
                    lock (this)
                    {
                        ThrowIfInPool(this);
                        if (_smallFields._currentProgress.InterlockedTrySet(progress))
                        {
                            if (!_isProgressScheduled)
                            {
                                _isProgressScheduled = true;
                                Retain(); // Retain until IProgressInvokable.Invoke is complete.
                                executionScheduler.ScheduleProgressSynchronous(this);
                            }
                        }
                        return null;
                    }
                }

                void IProgressInvokable.Invoke(ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    InterlockedIncrementProgressReportingCount();
                    Thread.MemoryBarrier(); // Make sure we're reading fresh progress (since the field cannot be marked volatile).
                    var progress = _smallFields._currentProgress;
                    _isProgressScheduled = false;
                    if (State == Promise.State.Pending)
                    {
                        foreach (var progressListener in _nextBranches)
                        {
                            Fixed32 progressCopy = progress;
                            var depth = Depth;
                            PromiseSingleAwait nextRef = progressListener.SetProgress(ref progressCopy, ref depth, ref executionScheduler);
                            if (nextRef != null)
                            {
                                nextRef.ReportProgress(progressCopy, depth, ref executionScheduler);
                            }
                        }
                    }
                    InterlockedDecrementProgressReportingCount();
                    MaybeDispose();
                }
            } // PromiseMultiAwait

            partial class DeferredPromiseBase
            {
                [MethodImpl(InlineOption)]
                internal bool TryReportProgress(short deferredId, float progress)
                {
                    InterlockedIncrementProgressReportingCount();
                    if (deferredId != DeferredId)
                    {
                        InterlockedDecrementProgressReportingCount();
                        return false;
                    }

                    ThrowIfInPool(this);

                    // Don't report progress 1.0, that will be reported automatically when the promise is resolved.
                    if (progress >= 0 & progress < 1f)
                    {
                        Fixed32 newProgress = _smallFields._currentProgress.SetNewDecimalPartFromDeferred(progress);
                        var wasReportingPriority = Fixed32.ts_reportingPriority;
                        Fixed32.ts_reportingPriority = true;

                        var executionScheduler = new ExecutionScheduler(false);
                        ReportProgressAlreadyIncremented(newProgress, 0, ref executionScheduler);
                        executionScheduler.ExecuteProgress();

                        Fixed32.ts_reportingPriority = wasReportingPriority;
                    }
                    else
                    {
                        InterlockedDecrementProgressReportingCount();
                    }
                    return true;
                }
            }

            partial class PromiseWaitPromise
            {

                [MethodImpl(InlineOption)]
                new protected void Reset(ushort depth)
                {
                    base.Reset(depth);
                    _smallFields._secondPrevious = false;
                }

                internal void WaitForWithProgress<T>(Promise<T> other)
                {
                    ThrowIfInPool(this);
                    SetSecondPrevious(other._ref);
                    _smallFields._currentProgress = Fixed32.FromWhole(Depth);
                    other._ref.HookupNewWaiter(other.Id, this);
                }

                [MethodImpl(InlineOption)]
                partial void SetSecondPrevious(PromiseRef secondPrevious)
                {
#if PROMISE_DEBUG
                    _previous = secondPrevious;
#endif
                    _smallFields._secondPrevious = true;
                }

                partial void ReportProgressFromWaitFor(PromiseRef other, ushort depth, ref ExecutionScheduler executionScheduler)
                {
                    var wasReportingPriority = Fixed32.ts_reportingPriority;
                    Fixed32.ts_reportingPriority = false;

                    Fixed32 progress;
                    if (TryNormalizeProgress(other._smallFields._currentProgress, depth, out progress))
                    {
                        InterlockedIncrementProgressReportingCount();
                        other.InterlockedDecrementProgressReportingCount();
                        ReportProgressAlreadyIncremented(progress, Depth, ref executionScheduler);
                    }
                    else
                    {
                        other.InterlockedDecrementProgressReportingCount();
                    }

                    Fixed32.ts_reportingPriority = wasReportingPriority;
                }

                [MethodImpl(InlineOption)]
                private bool TryNormalizeProgress(Fixed32 progress, ushort depth, out Fixed32 result)
                {
                    // Calculate the normalized progress for this and previous depth.
                    double normalizedProgress = progress.ToDouble() / (depth + 1d);
                    return _smallFields._currentProgress.TrySetNewDecimalPartFromWaitPromise(normalizedProgress, Depth, out result);
                }

                internal override sealed PromiseSingleAwait SetProgress(ref Fixed32 progress, ref ushort depth, ref ExecutionScheduler executionScheduler)
                {
                    // This acts as a pass-through to normalize the progress.
                    ThrowIfInPool(this);
                    bool didSet = _smallFields._secondPrevious
                        ? TryNormalizeProgress(progress, depth, out progress)
                        : _smallFields._currentProgress.InterlockedTrySet(progress);
                    depth = Depth;
                    return didSet ? this : null;
                }
            } // PromiseWaitPromise

            partial class PromisePassThrough
            {
                internal override PromiseSingleAwait SetProgress(ref Fixed32 progress, ref ushort depth, ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    depth = _target.Depth;
                    long dif = _smallFields._currentProgress.InterlockedSetAndGetDifference(progress);
                    return _target.IncrementProgress(dif, ref progress, _smallFields._depth);
                }

                [MethodImpl(InlineOption)]
                internal uint GetProgressDifferenceToCompletion()
                {
                    ThrowIfInPool(this);
                    Fixed32 incrementedWhole = Fixed32.FromWholePlusOne(_smallFields._depth);
                    return incrementedWhole.GetRawValue() - _smallFields._currentProgress.GetRawValue();
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

                internal override PromiseSingleAwait SetProgress(ref Fixed32 progress, ref ushort depth, ref ExecutionScheduler executionScheduler)
                {
                    if (float.IsNaN(_minProgress))
                    {
                        return null;
                    }
                    bool didSet = TryLerpAndSetProgress(ref progress, depth);
                    depth = 0;
                    return didSet ? this : null;
                }

                private bool TryLerpAndSetProgress(ref Fixed32 progress, ushort depth)
                {
                    var lerpedProgress = LerpProgress(progress, depth);
                    return _smallFields._currentProgress.TrySetNewDecimalPartFromAsync(lerpedProgress, out progress);
                }

                private void SetAwaitedComplete(PromiseRef handler, ref ExecutionScheduler executionScheduler)
                {
                    ThrowIfInPool(this);
                    // Don't report progress if it's 1 or NaN. 1 will be reported when the async promise is resolved.
                    // Also don't report if the awaited promise was rejected or canceled.
                    if (handler.State == Promise.State.Resolved & _maxProgress < 1f)
                    {
                        var progress = Fixed32.FromDecimalForResolve(_maxProgress);
                        _smallFields._currentProgress = progress;
                        ReportProgress(progress, 0, ref executionScheduler);
                    }
                    _minProgress = _maxProgress = float.NaN;
                }

                [MethodImpl(InlineOption)]
                internal void SetPreviousAndProgress(PromiseRef waiter, float minProgress, float maxProgress)
                {
#if PROMISE_DEBUG
                    _previous = waiter;
#endif
                    _minProgress = minProgress;
                    _maxProgress = maxProgress;
                }

                partial void ReportProgressFromHookupWaiterWithProgress(PromiseRef other, ushort depth, ref ExecutionScheduler executionScheduler)
                {
                    var wasReportingPriority = Fixed32.ts_reportingPriority;
                    Fixed32.ts_reportingPriority = false;

                    Fixed32 progress = other._smallFields._currentProgress;
                    if (TryLerpAndSetProgress(ref progress, depth))
                    {
                        InterlockedIncrementProgressReportingCount();
                        other.InterlockedDecrementProgressReportingCount();
                        ReportProgressAlreadyIncremented(progress, 0, ref executionScheduler);
                    }
                    else
                    {
                        other.InterlockedDecrementProgressReportingCount();
                    }

                    Fixed32.ts_reportingPriority = wasReportingPriority;
                }
            } // AsyncPromiseRef
#endif // PROMISE_PROGRESS
        } // PromiseRef
    } // Internal
}