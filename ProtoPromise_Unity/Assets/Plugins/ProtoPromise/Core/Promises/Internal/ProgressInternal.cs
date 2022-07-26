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

#pragma warning disable IDE0016 // Use 'throw' expression
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

        private static readonly SendOrPostCallback s_synchronizationContextProgressCallback = ProgressFromContext;
        private static readonly WaitCallback s_threadPoolProgressCallback = ProgressFromContext;

        private static void ScheduleForProgress(HandleablePromiseBase progressable, SynchronizationContext context)
        {
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
            if (context == null)
            {
                throw new InvalidOperationException("context cannot be null");
            }
#endif
            if (context == BackgroundSynchronizationContextSentinel.s_instance)
            {
                ThreadPool.QueueUserWorkItem(s_threadPoolProgressCallback, progressable);
            }
            else
            {
                context.Post(s_synchronizationContextProgressCallback, progressable);
            }
        }

        private static void ProgressFromContext(object state)
        {
            // In case this is executed from a background thread, catch the exception and report it instead of crashing the app.
            try
            {
                state.UnsafeAs<HandleablePromiseBase>().InvokeProgressFromContext();
            }
            catch (Exception e)
            {
                // This should never happen.
                ReportRejection(e, state as ITraceable);
            }
        }

        partial struct StackUnwindHelper
        {
            [ThreadStatic]
            private static Queue<HandleablePromiseBase> ts_progressors;

            [MethodImpl(InlineOption)]
            internal static void AddProgressor(HandleablePromiseBase progressor)
            {
                if (ts_progressors == null)
                {
                    ts_progressors = new Queue<HandleablePromiseBase>();
                }
                ts_progressors.Enqueue(progressor);
            }

            internal static void InvokeProgressors()
            {
                if (ts_progressors != null)
                {
                    while (ts_progressors.Count > 0)
                    {
                        ts_progressors.Dequeue().InvokeProgressFromContext();
                    }
                }
            }
        }

#endif // !PROMISE_PROGRESS

        partial class PromiseRefBase
        {
            // Calls to these get compiled away when PROGRESS is undefined.
            partial void WaitWhileProgressReporting();
            partial void InterlockedIncrementProgressReportingCount();
            partial void InterlockedDecrementProgressReportingCount();

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
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
                internal override PromiseRefBase SetProgress(ref Fixed32 progress, ref ushort depth)
                {
                    return null;
                }
            }

            partial class PromiseForgetSentinel : HandleablePromiseBase
            {
                internal override PromiseRefBase SetProgress(ref Fixed32 progress, ref ushort depth)
                {
                    return null;
                }
            }

            partial class InvalidAwaitSentinel : PromiseRefBase
            {
                internal override PromiseRefBase SetProgress(ref Fixed32 progress, ref ushort depth)
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
#endif

                internal double DecimalPart
                {
                    get { return (double) (_value & DecimalMask) / DecimalMax; }
                }

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
            [DebuggerNonUserCode, StackTraceHidden]
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

#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            internal sealed partial class PromiseProgress<TResult, TProgress> : PromiseSingleAwait<TResult>, ICancelable
                where TProgress : IProgress<float>
            {
                private static readonly WaitCallback s_threadPoolCallback = ExecuteFromContext;
                private static readonly SendOrPostCallback s_synchronizationContextCallback = ExecuteFromContext;

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

                internal static PromiseProgress<TResult, TProgress> GetOrCreate(TProgress progress, CancelationToken cancelationToken, ushort depth, bool isSynchronous, SynchronizationContext synchronizationContext)
                {
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                    if (!isSynchronous && synchronizationContext == null)
                    {
                        throw new InvalidOperationException("synchronizationContext cannot be null");
                    }
#endif
                    var promise = ObjectPool.TryTake<PromiseProgress<TResult, TProgress>>()
                        ?? new PromiseProgress<TResult, TProgress>();
                    promise.Reset(depth);
                    promise._progress = progress;
                    promise.IsCanceled = false;
                    promise._isSynchronous = isSynchronous;
                    promise._previousState = Promise.State.Pending;
                    promise._synchronizationContext = synchronizationContext;
                    cancelationToken.TryRegister(promise, out promise._cancelationRegistration); // Very important, must register after promise is fully setup.
                    return promise;
                }

                internal static PromiseProgress<TResult, TProgress> GetOrCreateFromResolved(TProgress progress, CancelationToken cancelationToken, ushort depth, SynchronizationContext synchronizationContext, TResult result)
                {
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                    if (synchronizationContext == null)
                    {
                        throw new InvalidOperationException("synchronizationContext cannot be null");
                    }
#endif
                    var promise = ObjectPool.TryTake<PromiseProgress<TResult, TProgress>>()
                        ?? new PromiseProgress<TResult, TProgress>();
                    promise.Reset(depth);
                    promise._progress = progress;
                    promise.IsCanceled = false;
                    promise._isSynchronous = false;
                    promise._previousState = Promise.State.Resolved;
                    promise._synchronizationContext = synchronizationContext;
                    promise._result = result;
                    cancelationToken.TryRegister(promise, out promise._cancelationRegistration); // Very important, must register after promise is fully setup.
                    return promise;
                }

                [MethodImpl(InlineOption)]
                private bool ShouldInvokeSynchronous()
                {
                    return _isSynchronous | _synchronizationContext == ts_currentContext;
                }

                protected override void MaybeDispose()
                {
                    if (InterlockedAddWithOverflowCheck(ref _retainCounter, -1, 0) == 0)
                    {
                        Dispose();
                        _cancelationRegistration = default(CancelationRegistration);
                        _progress = default(TProgress);
                        ObjectPool.MaybeRepool(this);
                    }
                }

                internal override void InvokeProgressFromContext()
                {
                    ThrowIfInPool(this);
                    var currentContext = ts_currentContext;
                    ts_currentContext = _synchronizationContext;

                    Thread.MemoryBarrier(); // Make sure we're reading fresh progress (since the field cannot be marked volatile).
                    var progress = _smallFields._currentProgress;
                    _isProgressScheduled = 0;
                    // Calculate the normalized progress for the depth that the listener was added.
                    // Use double for better precision.
                    double expected = Depth + 1u;
                    float value = (float) (progress.ToDouble() / expected);
                    if (!IsInvoking1 & !IsCanceled & !_cancelationRegistration.Token.IsCancelationRequested)
                    {
                        CallbackHelperVoid.InvokeAndCatchProgress(_progress, value, this);
                    }
                    MaybeDispose();

                    ts_currentContext = currentContext;
                }

                internal void MaybeScheduleProgress()
                {
#if NET_LEGACY // Interlocked.Exchange doesn't seem to work properly in Unity's old runtime. So use CompareExchange instead
                    bool isProgressScheduled = Interlocked.CompareExchange(ref _isProgressScheduled, 1, 0) != 0;
#else
                    bool isProgressScheduled = Interlocked.Exchange(ref _isProgressScheduled, 1) != 0;
#endif
                    if (isProgressScheduled)
                    {
                        return;
                    }
                    InterlockedAddWithOverflowCheck(ref _retainCounter, 1, -1);
                    // Even though it's scheduled synchronous, we still have to let the stack unwind to prevent a deadlock in case user code tries to complete the promise.
                    if (ShouldInvokeSynchronous())
                    {
                        StackUnwindHelper.AddProgressor(this);
                        return;
                    }
                    ScheduleForProgress(this, _synchronizationContext);
                }

                internal override PromiseRefBase SetProgress(ref Fixed32 progress, ref ushort depth)
                {
                    ThrowIfInPool(this);
                    if (_smallFields._currentProgress.InterlockedTrySet(progress) & !IsCanceled)
                    {
                        MaybeScheduleProgress();
                        return this;
                    }
                    return null;
                }

                internal override void HandleFromContext()
                {
                    ThrowIfInPool(this);
                    var currentContext = ts_currentContext;
                    ts_currentContext = _synchronizationContext;

                    HandleablePromiseBase nextHandler;
                    Invoke1(_previousState, out nextHandler);
                    MaybeHandleNext(nextHandler);

                    ts_currentContext = currentContext;
                }

                internal override void Handle(ref PromiseRefBase handler, out HandleablePromiseBase nextHandler)
                {
                    ThrowIfInPool(this);
                    handler.SuppressRejection = true;
                    _result = handler.GetResult<TResult>();
                    _rejectContainer = handler._rejectContainer;
                    handler.MaybeDispose();
                    var state = handler.State;
                    _previousState = state;

                    if (ShouldInvokeSynchronous())
                    {
                        handler = this;
                        Invoke1(state, out nextHandler);
                        return;
                    }

                    nextHandler = null;
                    ScheduleForHandle(this, _synchronizationContext);
                }

                private void Invoke1(Promise.State state, out HandleablePromiseBase nextHandler)
                {
                    if (TryUnregisterAndIsNotCanceling(ref _cancelationRegistration) & !IsCanceled)
                    {
                        if (state == Promise.State.Resolved)
                        {
                            CallbackHelperVoid.InvokeAndCatchProgress(_progress, 1f, this);
                        }
                        // Release since Cancel() will not be invoked.
                        InterlockedAddWithOverflowCheck(ref _retainCounter, -1, 0);
                    }

                    State = state;
                    nextHandler = TakeOrHandleNextWaiter();
                }

                void ICancelable.Cancel()
                {
                    ThrowIfInPool(this);
                    IsCanceled = true;
                    MaybeDispose();
                }

                internal override PromiseRefBase AddWaiter(short promiseId, HandleablePromiseBase waiter, out HandleablePromiseBase previousWaiter)
                {
                    if (ShouldInvokeSynchronous())
                    {
                        return AddWaiterImpl(promiseId, waiter, out previousWaiter, Depth);
                    }

                    if (promiseId != Id)
                    {
                        previousWaiter = InvalidAwaitSentinel.s_instance;
                        return InvalidAwaitSentinel.s_instance;
                    }
                    ThrowIfInPool(this);
                    WasAwaitedOrForgotten = true;

                    var previous = CompareExchangeWaiter(waiter, null);
                    if (previous != null)
                    {
                        // We do the verification process here instead of in the caller, because we need to handle continuations on the synchronization context.
                        if (CompareExchangeWaiter(waiter, PromiseCompletionSentinel.s_instance) != PromiseCompletionSentinel.s_instance)
                        {
                            previousWaiter = InvalidAwaitSentinel.s_instance;
                            return InvalidAwaitSentinel.s_instance;
                        }

                        // If this was configured to execute progress on a SynchronizationContext or the ThreadPool, force the waiter to execute on the same context for consistency.
                        if (_synchronizationContext == null)
                        {
                            // If there is no context, send it to the ThreadPool.
                            ThreadPool.QueueUserWorkItem(s_threadPoolCallback, this);
                        }
                        else
                        {
                            _synchronizationContext.Post(s_synchronizationContextCallback, this);
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
                        var _this = state.UnsafeAs<PromiseProgress<TResult, TProgress>>();
                        ThrowIfInPool(_this);
                        _this.State = _this._previousState;
                        // We don't need to synchronize access here because this is only called when the waiter is added after Invoke1 has completed, so there are no race conditions.
                        // _this._next is guaranteed to be non-null here, so we can call HandleNext instead of MaybeHandleNext.
                        _this.HandleNext(_this._next);
                    }
                    catch (Exception e)
                    {
                        // This should never happen.
                        ReportRejection(e, state as ITraceable);
                    }
                }

                internal override bool GetIsCompleted(short promiseId)
                {
                    ValidateId(promiseId, this, 2);
                    ThrowIfInPool(this);
                    // Make sure the continuation happens on the synchronization context.
                    if ((ShouldInvokeSynchronous())
                        && CompareExchangeWaiter(InvalidAwaitSentinel.s_instance, PromiseCompletionSentinel.s_instance) == PromiseCompletionSentinel.s_instance)
                    {
                        WasAwaitedOrForgotten = true;
                        State = _previousState;
                        return true;
                    }
                    return false;
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

            [MethodImpl(InlineOption)]
            internal void ReportProgress(Fixed32 progress, ushort depth)
            {
                InterlockedIncrementProgressReportingCount();
                ReportProgressAlreadyIncremented(progress, depth);
            }

            internal void ReportProgressAlreadyIncremented(Fixed32 progress, ushort depth)
            {
                PromiseRefBase current = this;
                while (true)
                {
                    var progressListener = current._next;
                    if (progressListener == null)
                    {
                        break;
                    }
                    var next = progressListener.SetProgress(ref progress, ref depth);
                    if (next == null)
                    {
                        break;
                    }
                    next.InterlockedIncrementProgressReportingCount();
                    current.InterlockedDecrementProgressReportingCount();
                    current = next;
                }
                current.InterlockedDecrementProgressReportingCount();

                StackUnwindHelper.InvokeProgressors();
            }

            partial class PromiseSingleAwait<TResult>
            {
                internal override PromiseRefBase SetProgress(ref Fixed32 progress, ref ushort depth)
                {
                    return _smallFields._currentProgress.InterlockedTrySet(progress) ? this : null;
                }
            } // PromiseSingleAwait

            // Used to increase concurrency when invoking progress in PromiseMultiAwait. The cost is slower single-threaded invoke.
            [ThreadStatic]
            private static List<ValueTuple<PromiseRefBase, Fixed32, ushort>> ts_progressListenersForMultiAwait;

            partial class PromiseMultiAwait<TResult>
            {
                internal override PromiseRefBase SetProgress(ref Fixed32 progress, ref ushort depth)
                {
                    lock (this)
                    {
                        ThrowIfInPool(this);
                        if (_smallFields._currentProgress.InterlockedTrySet(progress))
                        {
                            if (!_isProgressScheduled)
                            {
                                _isProgressScheduled = true;
                                Retain(); // Retain until InvokeProgressFromContext is complete.
                                StackUnwindHelper.AddProgressor(this);
                            }
                        }
                        return null;
                    }
                }

                internal override void InvokeProgressFromContext()
                {
                    ThrowIfInPool(this);
                    InterlockedIncrementProgressReportingCount();
                    Thread.MemoryBarrier(); // Make sure we're reading fresh progress (since the field cannot be marked volatile).
                    var progress = _smallFields._currentProgress;
                    _isProgressScheduled = false;
                    // cache _nextBranches in a local because another thread can nullify it in Handle.
                    var branches = _nextBranches;
                    if (State != Promise.State.Pending | branches.Count == 0)
                    {
                        InterlockedDecrementProgressReportingCount();
                        MaybeDispose();
                        return;
                    }

                    var nextListeners = ts_progressListenersForMultiAwait;
                    if (nextListeners == null)
                    {
                        nextListeners = new List<ValueTuple<PromiseRefBase, Fixed32, ushort>>(branches.Count);
                        ts_progressListenersForMultiAwait = nextListeners;
                    }

                    // New waiters could be added on another thread while this is looping. That is fine.
                    // This will only iterate over the items that were captured for the snapshot in the branches local copy.
                    // Items will not be removed until all threads invoking progress are complete.
                    for (int i = 0, max = branches.Count; i < max; ++i)
                    {
                        var progressListener = branches[i];
                        Fixed32 progressCopy = progress;
                        ushort depth = Depth;
                        PromiseRefBase nextRef = progressListener.SetProgress(ref progressCopy, ref depth);
                        if (nextRef != null)
                        {
                            nextRef.InterlockedIncrementProgressReportingCount();
                            nextListeners.Add(ValueTuple.Create(nextRef, progressCopy, depth));
                        }
                    }
                    InterlockedDecrementProgressReportingCount();
                    MaybeDispose();

                    foreach (var tuple in nextListeners)
                    {
                        tuple.Item1.ReportProgressAlreadyIncremented(tuple.Item2, tuple.Item3);
                    }
                    nextListeners.Clear();
                }
            } // PromiseMultiAwait

            partial class DeferredPromiseBase<TResult>
            {
                [MethodImpl(InlineOption)]
                public bool TryReportProgress(int deferredId, float progress)
                {
                    // It is possible this is called concurrently on another thread after this object has been repooled.
                    // User code really shouldn't use this in that manner, which the deferredId protects against accidental usage.
                    // But in case that does happen (like in unit tests for stress testing), calling SetProgress on the progressListener will be a no-op.

                    var progressListener = _next;
                    InterlockedIncrementProgressReportingCount();
                    if (deferredId != DeferredId)
                    {
                        InterlockedDecrementProgressReportingCount();
                        return false;
                    }

                    // Don't report progress 1.0, that will be reported automatically when the promise is resolved.
                    if (progress >= 0 & progress < 1f)
                    {
                        Fixed32 newProgress = _smallFields._currentProgress.SetNewDecimalPartFromDeferred(progress);
                        var wasReportingPriority = Fixed32.ts_reportingPriority;
                        Fixed32.ts_reportingPriority = true;

                        ReportProgressAlreadyIncremented(newProgress, progressListener);

                        Fixed32.ts_reportingPriority = wasReportingPriority;
                    }
                    else
                    {
                        InterlockedDecrementProgressReportingCount();
                    }
                    return true;
                }

                private void ReportProgressAlreadyIncremented(Fixed32 progress, HandleablePromiseBase progressListener)
                {
                    if (progressListener == null)
                    {
                        InterlockedDecrementProgressReportingCount();
                        return;
                    }
                    ushort depth = 0;
                    var next = progressListener.SetProgress(ref progress, ref depth);
                    if (next == null)
                    {
                        InterlockedDecrementProgressReportingCount();
                        StackUnwindHelper.InvokeProgressors();
                        return;
                    }
                    next.InterlockedIncrementProgressReportingCount();
                    InterlockedDecrementProgressReportingCount();
                    next.ReportProgressAlreadyIncremented(progress, depth);
                }
            }

            [MethodImpl(InlineOption)]
            partial void SetSecondPrevious(PromiseRefBase secondPrevious)
            {
#if PROMISE_DEBUG
                _previous = secondPrevious;
#endif
                _smallFields._secondPrevious = true;
            }

            partial void ReportProgressFromWaitFor(PromiseRefBase other, ushort depth)
            {
                var wasReportingPriority = Fixed32.ts_reportingPriority;
                Fixed32.ts_reportingPriority = false;

                Fixed32 progress;
                if (TryNormalizeProgress(other._smallFields._currentProgress, depth, out progress))
                {
                    InterlockedIncrementProgressReportingCount();
                    other.InterlockedDecrementProgressReportingCount();
                    ReportProgressAlreadyIncremented(progress, Depth);
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

            partial class PromiseWaitPromise<TResult>
            {

                [MethodImpl(InlineOption)]
                new protected void Reset(ushort depth)
                {
                    base.Reset(depth);
                    _smallFields._secondPrevious = false;
                }

                [MethodImpl(InlineOption)]
                internal void WaitForWithProgress(PromiseRefBase _ref, short promiseId)
                {
                    ThrowIfInPool(this);
                    SetSecondPrevious(_ref);
                    _smallFields._currentProgress = Fixed32.FromWhole(Depth);
                    _ref.HookupNewWaiter(promiseId, this);
                }

                internal override sealed PromiseRefBase SetProgress(ref Fixed32 progress, ref ushort depth)
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
                internal override PromiseRefBase SetProgress(ref Fixed32 progress, ref ushort depth)
                {
                    ThrowIfInPool(this);
                    depth = _ownerOrTarget.Depth;
                    long dif = _smallFields._currentProgress.InterlockedSetAndGetDifference(progress);
                    return _ownerOrTarget.IncrementProgress(dif, ref progress, _smallFields._depth);
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

                partial void SetInitialProgress(PromiseRefBase owner, PromiseRefBase target)
                {
                    var progress = owner._smallFields._currentProgress;
                    _smallFields._currentProgress = progress;
                    uint increment = progress.GetRawValue();
                    target.IncrementProgress(increment, ref progress, _smallFields._depth);
                }
            } // PromisePassThrough

            partial class AsyncPromiseRef<TResult>
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

                internal override PromiseRefBase SetProgress(ref Fixed32 progress, ref ushort depth)
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

                partial void SetAwaitedComplete(PromiseRefBase handler)
                {
                    ThrowIfInPool(this);
#if PROMISE_DEBUG
                    _previous = null;
#endif
                    // Don't report progress if it's 1 or NaN. 1 will be reported when the async promise is resolved.
                    // Also don't report if the awaited promise was rejected or canceled.
                    if (handler.State == Promise.State.Resolved & _maxProgress < 1f)
                    {
                        var progress = Fixed32.FromDecimalForResolve(_maxProgress);
                        _smallFields._currentProgress = progress;
                        ReportProgress(progress, 0);
                    }
                    _minProgress = _maxProgress = float.NaN;
                }

                [MethodImpl(InlineOption)]
                private void SetPreviousAndProgress(PromiseRefBase waiter, float minProgress, float maxProgress)
                {
#if PROMISE_DEBUG
                    _previous = waiter;
#endif
                    _minProgress = float.IsNaN(minProgress)
                        ? (float) _smallFields._currentProgress.DecimalPart
                        : minProgress;
                    _maxProgress = maxProgress;
                }

                private void ReportProgressFromHookupWaiterWithProgress(PromiseRefBase other, ushort depth)
                {
                    var wasReportingPriority = Fixed32.ts_reportingPriority;
                    Fixed32.ts_reportingPriority = false;

                    Fixed32 progress = other._smallFields._currentProgress;
                    if (TryLerpAndSetProgress(ref progress, depth))
                    {
                        InterlockedIncrementProgressReportingCount();
                        other.InterlockedDecrementProgressReportingCount();
                        ReportProgressAlreadyIncremented(progress, 0);
                    }
                    else
                    {
                        other.InterlockedDecrementProgressReportingCount();
                    }

                    Fixed32.ts_reportingPriority = wasReportingPriority;
                }
            } // AsyncPromiseRef
#endif // PROMISE_PROGRESS
        } // PromiseRefBase
    } // Internal
}