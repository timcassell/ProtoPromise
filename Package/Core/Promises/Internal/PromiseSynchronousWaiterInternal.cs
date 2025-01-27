#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Proto.Promises
{
    partial class Internal
    {
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        internal sealed partial class PromiseSynchronousWaiter : HandleablePromiseBase
        {
            private const int InitialState = 0;
            private const int WaitingState = 1;
            private const int CompletedState = 2;
            private const int WaitedSuccessState = 3;
            private const int WaitedFailedState = 4;

            private PromiseSynchronousWaiter() { }

            [MethodImpl(InlineOption)]
            private static PromiseSynchronousWaiter GetOrCreate()
            {
                var obj = ObjectPool.TryTakeOrInvalid<PromiseSynchronousWaiter>();
                return obj == PromiseRefBase.InvalidAwaitSentinel.s_instance
                    ? new PromiseSynchronousWaiter()
                    : obj.UnsafeAs<PromiseSynchronousWaiter>();
            }

            [MethodImpl(InlineOption)]
            internal static bool TryWaitForResult(PromiseRefBase promise, short promiseId, TimeSpan timeout, out Promise.ResultContainer resultContainer)
            {
                if (!TryWaitForCompletion(promise, promiseId, timeout))
                {
                    resultContainer = default;
                    return false;
                }
                resultContainer = promise.GetResultContainerAndMaybeDispose();
                return true;
            }

            [MethodImpl(InlineOption)]
            internal static bool TryWaitForResult<TResult>(PromiseRefBase.PromiseRef<TResult> promise, short promiseId, TimeSpan timeout, out Promise<TResult>.ResultContainer resultContainer)
            {
                if (!TryWaitForCompletion(promise, promiseId, timeout))
                {
                    resultContainer = default;
                    return false;
                }
                resultContainer = promise.GetResultContainerAndMaybeDispose();
                return true;
            }

            private static bool TryWaitForCompletion(PromiseRefBase promise, short promiseId, TimeSpan timeout)
            {
                var stopwatch = ValueStopwatch.StartNew();
                if (timeout < TimeSpan.Zero & timeout.Milliseconds != Timeout.Infinite)
                {
                    throw new ArgumentOutOfRangeException(nameof(timeout), "timeout must be greater than or equal to 0, or Timeout.InfiniteTimespan (-1 ms).", GetFormattedStacktrace(3));
                }

                var waiter = GetOrCreate();
                waiter._next = null;
                waiter._waitState = InitialState;
                promise.HookupExistingWaiter(promiseId, waiter);
                return waiter.TryWaitForCompletion(promise, timeout, stopwatch);
            }

            private bool TryWaitForCompletion(PromiseRefBase promise, TimeSpan timeout, ValueStopwatch stopwatch)
            {
                // We do a short spinwait before yielding the thread.
                var spinner = new SpinWait();
                if (timeout.Milliseconds == Timeout.Infinite)
                {
                    while (promise.State == Promise.State.Pending & !spinner.NextSpinWillYield)
                    {
                        spinner.SpinOnce();
                    }
                }
                else
                {
                    while (promise.State == Promise.State.Pending & !spinner.NextSpinWillYield & stopwatch.GetElapsedTime() < timeout)
                    {
                        spinner.SpinOnce();
                    }
                }

                if (promise.State != Promise.State.Pending)
                {
                    if (Interlocked.Exchange(ref _waitState, WaitedSuccessState) == CompletedState)
                    {
                        // Handle was already called (possibly synchronously) and returned without repooling, so we repool here.
                        ObjectPool.MaybeRepool(this);
                    }
                    return true;
                }

                lock (this)
                {
                    // Check the completion state and set to waiting before monitor wait.
                    if (Interlocked.Exchange(ref _waitState, WaitingState) == CompletedState)
                    {
                        // Handle was called and returned without pulsing or repooling, so we repool here.
                        // Exit the lock before repool.
                        goto Repool;
                    }

                    if (timeout.Milliseconds == Timeout.Infinite)
                    {
                        Monitor.Wait(this);
                    }
                    else
                    {
                        // Since we did a spinwait, subtract the time it took to spin,
                        // and make sure there is still positive time remaining for the monitor wait.
                        timeout -= stopwatch.GetElapsedTime();
                        if (timeout > TimeSpan.Zero)
                        {
                            Monitor.Wait(this, timeout);
                        }
                    }
                }

                // We determine the success state from the promise state, rather than whether the Monitor.Wait timed out or not.
                bool success = promise.State != Promise.State.Pending;
                // Set the wait state for Handle cleanup (just a volatile write, no Interlocked).
                _waitState = success ? WaitedSuccessState : WaitedFailedState;
                return success;

            Repool:
                ObjectPool.MaybeRepool(this);
                return true;
            }

            internal override void Handle(PromiseRefBase handler, Promise.State state)
            {
                ThrowIfInPool(this);
                handler.SetCompletionState(state);

                int waitState = Interlocked.Exchange(ref _waitState, CompletedState);
                if (waitState == InitialState)
                {
                    return;
                }

                lock (this)
                {
                    // Wake the other thread.
                    Monitor.Pulse(this);
                }

                // Wait until we're sure the other thread has continued.
                var spinner = new SpinWait();
                while (waitState <= CompletedState)
                {
                    spinner.SpinOnce();
                    waitState = _waitState;
                }

                // If the timeout expired before completion, we dispose the handler here. Otherwise, the original caller will dispose it.
                if (waitState == WaitedFailedState)
                {
                    // Maybe report the rejection here since the original caller was unable to observe it.
                    handler.MaybeReportUnhandledAndDispose(state);
                }
                ObjectPool.MaybeRepool(this);
            }
        } // class PromiseSynchronousWaiter
    } // class Internal
} // namespace Proto.Promises