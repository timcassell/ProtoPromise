#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using NUnit.Framework;
using Proto.Promises;
using Proto.Timers;
using ProtoPromiseTests.Concurrency;
using System;
using System.Collections.Generic;
using System.Threading;

namespace ProtoPromiseTests.APIs
{
    public class WaitAsyncTests
    {
        public enum WaitAsyncCancelType
        {
            DefaultToken,
            AlreadyCanceled,
            CancelableToken_NoCancel,
            CancelBeforeComplete,
            CancelAfterComplete
        }

        const string rejectValue = "Fail";

        [SetUp]
        public void Setup()
        {
            // When a promise is canceled, the previous rejected promise is unhandled, so its rejection is sent to the UncaughtRejectionHandler.
            // So we set the expected uncaught reject value.
            TestHelper.s_expectedUncaughtRejectValue = rejectValue;

            TestHelper.Setup();
        }

        [TearDown]
        public void Teardown()
        {
            TestHelper.Cleanup();

            TestHelper.s_expectedUncaughtRejectValue = null;
        }

        private static IEnumerable<TestCaseData> GetArgs_CancelationToken()
        {
            foreach (CompleteType completeType in Enum.GetValues(typeof(CompleteType)))
            foreach (WaitAsyncCancelType cancelType in Enum.GetValues(typeof(WaitAsyncCancelType)))
            foreach (bool alreadyComplete in new[] { true, false })
            {
                if (alreadyComplete && cancelType == WaitAsyncCancelType.CancelBeforeComplete) continue;

                yield return new TestCaseData(completeType, cancelType, alreadyComplete);
            }
        }

        [Test, TestCaseSource(nameof(GetArgs_CancelationToken))]
        public void WaitAsync_CancelationToken_void(CompleteType completeType, WaitAsyncCancelType cancelType, bool alreadyComplete)
        {
            var cancelationSource = CancelationSource.New();
            var cancelationToken = cancelType == WaitAsyncCancelType.DefaultToken ? default(CancelationToken)
                : cancelType == WaitAsyncCancelType.AlreadyCanceled ? CancelationToken.Canceled()
                : cancelationSource.Token;

            bool cancelationExpected = cancelType == WaitAsyncCancelType.AlreadyCanceled || cancelType == WaitAsyncCancelType.CancelBeforeComplete;
            var expectedCompleteState = cancelationExpected ? Promise.State.Canceled : (Promise.State) completeType;

            var promise = TestHelper.BuildPromise(completeType, alreadyComplete, rejectValue, out var tryCompleter)
                .WaitAsync(cancelationToken)
                .ContinueWith(container =>
                {
                    Assert.AreEqual(expectedCompleteState, container.State);
                });
            if (cancelType == WaitAsyncCancelType.CancelBeforeComplete)
            {
                cancelationSource.Cancel();
            }
            tryCompleter();
            if (cancelType == WaitAsyncCancelType.CancelAfterComplete)
            {
                cancelationSource.Cancel();
            }

            promise.WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
            cancelationSource.Dispose();
        }

        [Test, TestCaseSource(nameof(GetArgs_CancelationToken))]
        public void WaitAsync_CancelationToken_T(CompleteType completeType, WaitAsyncCancelType cancelType, bool alreadyComplete)
        {
            var cancelationSource = CancelationSource.New();
            var cancelationToken = cancelType == WaitAsyncCancelType.DefaultToken ? default(CancelationToken)
                : cancelType == WaitAsyncCancelType.AlreadyCanceled ? CancelationToken.Canceled()
                : cancelationSource.Token;

            bool cancelationExpected = cancelType == WaitAsyncCancelType.AlreadyCanceled || cancelType == WaitAsyncCancelType.CancelBeforeComplete;
            var expectedCompleteState = cancelationExpected ? Promise.State.Canceled : (Promise.State) completeType;
            const int resolveValue = 1;

            var promise = TestHelper.BuildPromise(completeType, alreadyComplete, resolveValue, rejectValue, out var tryCompleter)
                .WaitAsync(cancelationToken)
                .ContinueWith(container =>
                {
                    Assert.AreEqual(expectedCompleteState, container.State);
                    if (expectedCompleteState == Promise.State.Resolved)
                    {
                        Assert.AreEqual(resolveValue, container.Value);
                    }
                });
            if (cancelType == WaitAsyncCancelType.CancelBeforeComplete)
            {
                cancelationSource.Cancel();
            }
            tryCompleter();
            if (cancelType == WaitAsyncCancelType.CancelAfterComplete)
            {
                cancelationSource.Cancel();
            }

            promise.WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
            cancelationSource.Dispose();
        }

        public enum CompleteTime
        {
            None,
            Immediate,
            Delayed
        }

        private static IEnumerable<TestCaseData> GetArgs_Timeout()
        {
            foreach (CompleteType completeType in Enum.GetValues(typeof(CompleteType)))
            foreach (int milliseconds in new[] { 0, 200, -1 })
            foreach (CompleteTime completeTime in Enum.GetValues(typeof(CompleteTime)))
            {
                if (completeTime == CompleteTime.None && milliseconds == -1) continue;

                yield return new TestCaseData(completeType, milliseconds, completeTime);
            }
        }

        [Test, TestCaseSource(nameof(GetArgs_Timeout))]
        public void WaitAsync_Timeout_void(CompleteType completeType, int milliseconds, CompleteTime completeTime)
        {
            bool expectedTimeout =
                completeTime == CompleteTime.None ? true
                : completeTime == CompleteTime.Immediate ? false
                : milliseconds == 0;
            var expectedCompleteState = expectedTimeout ? Promise.State.Rejected : (Promise.State) completeType;

            var promise = TestHelper.BuildPromise(completeType, completeTime == CompleteTime.Immediate, rejectValue, out var tryCompleter)
                .WaitAsync(TimeSpan.FromMilliseconds(milliseconds));

            if (completeTime == CompleteTime.Delayed)
            {
                tryCompleter.Invoke();
            }

            promise
                .ContinueWith(container =>
                {
                    Assert.AreEqual(expectedCompleteState, container.State);
                    if (expectedTimeout)
                    {
                        Assert.IsInstanceOf<TimeoutException>(container.Reason);
                    }
                })
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));

            if (completeTime != CompleteTime.Delayed)
            {
                tryCompleter.Invoke();
            }
        }

        [Test, TestCaseSource(nameof(GetArgs_Timeout))]
        public void WaitAsync_Timeout_T(CompleteType completeType, int milliseconds, CompleteTime completeTime)
        {
            bool expectedTimeout =
                completeTime == CompleteTime.None ? true
                : completeTime == CompleteTime.Immediate ? false
                : milliseconds == 0;
            var expectedCompleteState = expectedTimeout ? Promise.State.Rejected : (Promise.State) completeType;
            const int resolveValue = 1;

            var promise = TestHelper.BuildPromise(completeType, completeTime == CompleteTime.Immediate, resolveValue, rejectValue, out var tryCompleter)
                .WaitAsync(TimeSpan.FromMilliseconds(milliseconds));

            if (completeTime == CompleteTime.Delayed)
            {
                tryCompleter.Invoke();
            }

            promise
                .ContinueWith(container =>
                {
                    Assert.AreEqual(expectedCompleteState, container.State);
                    if (expectedCompleteState == Promise.State.Resolved)
                    {
                        Assert.AreEqual(resolveValue, container.Value);
                    }
                    if (expectedTimeout)
                    {
                        Assert.IsInstanceOf<TimeoutException>(container.Reason);
                    }
                })
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));

            if (completeTime != CompleteTime.Delayed)
            {
                tryCompleter.Invoke();
            }
        }

        private static IEnumerable<TestCaseData> GetArgs_TimeoutFactory()
        {
            foreach (CompleteType completeType in Enum.GetValues(typeof(CompleteType)))
            foreach (int milliseconds in new[] { 0, 200, -1 })
            foreach (TimerFactoryType timerFactoryType in Enum.GetValues(typeof(TimerFactoryType)))
            foreach (CompleteTime completeTime in Enum.GetValues(typeof(CompleteTime)))
            {
                if (milliseconds == -1
                    && (completeTime == CompleteTime.None || timerFactoryType == TimerFactoryType.FakeImmediate)) continue;

                yield return new TestCaseData(completeType, milliseconds, timerFactoryType, completeTime);
            }
        }

        [Test, TestCaseSource(nameof(GetArgs_TimeoutFactory))]
        public void WaitAsync_TimeoutFactory_void(CompleteType completeType, int milliseconds, TimerFactoryType timerFactoryType, CompleteTime completeTime)
        {
            FakeTimerFactory fakeFactory = timerFactoryType == TimerFactoryType.FakeDelayed
                ? new FakeDelayedTimerFactory()
                : (FakeTimerFactory) new FakeImmediateTimerFactory();

            bool expectedTimeout =
                completeTime == CompleteTime.None ? true
                : completeTime == CompleteTime.Immediate ? false
                : milliseconds == 0 || timerFactoryType == TimerFactoryType.FakeImmediate;
            var expectedCompleteState = expectedTimeout ? Promise.State.Rejected : (Promise.State) completeType;

            var promise = TestHelper.BuildPromise(completeType, completeTime == CompleteTime.Immediate, rejectValue, out var tryCompleter)
                .WaitAsync(TimeSpan.FromMilliseconds(milliseconds), timerFactoryType == 0 ? TimerFactory.System : fakeFactory);

            if (completeTime == CompleteTime.Delayed)
            {
                tryCompleter.Invoke();
            }
            fakeFactory.Invoke();

            promise
                .ContinueWith(container =>
                {
                    Assert.AreEqual(expectedCompleteState, container.State);
                    if (expectedTimeout)
                    {
                        Assert.IsInstanceOf<TimeoutException>(container.Reason);
                    }
                })
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));

            if (completeTime != CompleteTime.Delayed)
            {
                tryCompleter.Invoke();
            }
        }

        [Test, TestCaseSource(nameof(GetArgs_TimeoutFactory))]
        public void WaitAsync_TimeoutFactory_T(CompleteType completeType, int milliseconds, TimerFactoryType timerFactoryType, CompleteTime completeTime)
        {
            FakeTimerFactory fakeFactory = timerFactoryType == TimerFactoryType.FakeDelayed
                ? new FakeDelayedTimerFactory()
                : (FakeTimerFactory) new FakeImmediateTimerFactory();

            bool expectedTimeout =
                completeTime == CompleteTime.None ? true
                : completeTime == CompleteTime.Immediate ? false
                : milliseconds == 0 || timerFactoryType == TimerFactoryType.FakeImmediate;
            var expectedCompleteState = expectedTimeout ? Promise.State.Rejected : (Promise.State) completeType;
            const int resolveValue = 1;

            var promise = TestHelper.BuildPromise(completeType, completeTime == CompleteTime.Immediate, resolveValue, rejectValue, out var tryCompleter)
                .WaitAsync(TimeSpan.FromMilliseconds(milliseconds), timerFactoryType == 0 ? TimerFactory.System : fakeFactory);

            if (completeTime == CompleteTime.Delayed)
            {
                tryCompleter.Invoke();
            }
            fakeFactory.Invoke();

            promise
                .ContinueWith(container =>
                {
                    Assert.AreEqual(expectedCompleteState, container.State);
                    if (expectedCompleteState == Promise.State.Resolved)
                    {
                        Assert.AreEqual(resolveValue, container.Value);
                    }
                    if (expectedTimeout)
                    {
                        Assert.IsInstanceOf<TimeoutException>(container.Reason);
                    }
                })
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));

            if (completeTime != CompleteTime.Delayed)
            {
                tryCompleter.Invoke();
            }
        }

        private static IEnumerable<TestCaseData> GetArgs_TimeoutCancelationToken()
        {
            foreach (CompleteType completeType in Enum.GetValues(typeof(CompleteType)))
            foreach (int milliseconds in new[] { 0, 200, -1 })
            foreach (CompleteTime completeTime in Enum.GetValues(typeof(CompleteTime)))
            foreach (WaitAsyncCancelType cancelType in Enum.GetValues(typeof(WaitAsyncCancelType)))
            {
                if (milliseconds == -1 && completeTime == CompleteTime.None) continue;
                if (completeTime == CompleteTime.Immediate && cancelType == WaitAsyncCancelType.CancelBeforeComplete) continue;

                yield return new TestCaseData(completeType, milliseconds, completeTime, cancelType);
            }
        }

        [Test, TestCaseSource(nameof(GetArgs_TimeoutCancelationToken))]
        public void WaitAsync_TimeoutCancelationToken_void(CompleteType completeType, int milliseconds, CompleteTime completeTime, WaitAsyncCancelType cancelType)
        {
            var cancelationSource = CancelationSource.New();
            var cancelationToken = cancelType == WaitAsyncCancelType.DefaultToken ? default(CancelationToken)
                : cancelType == WaitAsyncCancelType.AlreadyCanceled ? CancelationToken.Canceled()
                : cancelationSource.Token;

            bool expectedCanceled =
                cancelType == WaitAsyncCancelType.AlreadyCanceled ? true
                : cancelType == WaitAsyncCancelType.DefaultToken || cancelType == WaitAsyncCancelType.CancelableToken_NoCancel || cancelType == WaitAsyncCancelType.CancelAfterComplete ? false
                // CancelBeforeComplete, expect canceled only if timeout is not immediate.
                : milliseconds != 0;
            bool expectedTimeout =
                expectedCanceled ? false
                : completeTime == CompleteTime.None ? true
                : completeTime == CompleteTime.Immediate ? false
                : milliseconds == 0;
            var expectedCompleteState =
                expectedCanceled ? Promise.State.Canceled
                : expectedTimeout ? Promise.State.Rejected
                : (Promise.State) completeType;

            var promise = TestHelper.BuildPromise(completeType, completeTime == CompleteTime.Immediate, rejectValue, out var tryCompleter)
                .WaitAsync(TimeSpan.FromMilliseconds(milliseconds), cancelationToken);

            if (cancelType == WaitAsyncCancelType.CancelBeforeComplete)
            {
                cancelationSource.Cancel();
            }
            if (completeTime == CompleteTime.Delayed)
            {
                tryCompleter();
                if (cancelType == WaitAsyncCancelType.CancelAfterComplete)
                {
                    cancelationSource.Cancel();
                }
            }

            promise
                .ContinueWith(container =>
                {
                    Assert.AreEqual(expectedCompleteState, container.State);
                    if (expectedTimeout)
                    {
                        Assert.IsInstanceOf<TimeoutException>(container.Reason);
                    }
                })
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));

            if (completeTime != CompleteTime.Delayed)
            {
                tryCompleter.Invoke();
            }
            cancelationSource.Dispose();
        }

        [Test, TestCaseSource(nameof(GetArgs_TimeoutCancelationToken))]
        public void WaitAsync_TimeoutCancelationToken_T(CompleteType completeType, int milliseconds, CompleteTime completeTime, WaitAsyncCancelType cancelType)
        {
            var cancelationSource = CancelationSource.New();
            var cancelationToken = cancelType == WaitAsyncCancelType.DefaultToken ? default(CancelationToken)
                : cancelType == WaitAsyncCancelType.AlreadyCanceled ? CancelationToken.Canceled()
                : cancelationSource.Token;

            bool expectedCanceled =
                cancelType == WaitAsyncCancelType.AlreadyCanceled ? true
                : cancelType == WaitAsyncCancelType.DefaultToken || cancelType == WaitAsyncCancelType.CancelableToken_NoCancel || cancelType == WaitAsyncCancelType.CancelAfterComplete ? false
                // CancelBeforeComplete, expect canceled only if timeout is not immediate.
                : milliseconds != 0;
            bool expectedTimeout =
                expectedCanceled ? false
                : completeTime == CompleteTime.None ? true
                : completeTime == CompleteTime.Immediate ? false
                : milliseconds == 0;
            var expectedCompleteState =
                expectedCanceled ? Promise.State.Canceled
                : expectedTimeout ? Promise.State.Rejected
                : (Promise.State) completeType;
            const int resolveValue = 1;

            var promise = TestHelper.BuildPromise(completeType, completeTime == CompleteTime.Immediate, resolveValue, rejectValue, out var tryCompleter)
                .WaitAsync(TimeSpan.FromMilliseconds(milliseconds), cancelationToken);

            if (cancelType == WaitAsyncCancelType.CancelBeforeComplete)
            {
                cancelationSource.Cancel();
            }
            if (completeTime == CompleteTime.Delayed)
            {
                tryCompleter();
                if (cancelType == WaitAsyncCancelType.CancelAfterComplete)
                {
                    cancelationSource.Cancel();
                }
            }

            promise
                .ContinueWith(container =>
                {
                    Assert.AreEqual(expectedCompleteState, container.State);
                    if (expectedCompleteState == Promise.State.Resolved)
                    {
                        Assert.AreEqual(resolveValue, container.Value);
                    }
                    if (expectedTimeout)
                    {
                        Assert.IsInstanceOf<TimeoutException>(container.Reason);
                    }
                })
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));

            if (completeTime != CompleteTime.Delayed)
            {
                tryCompleter.Invoke();
            }
            cancelationSource.Dispose();
        }

        private static IEnumerable<TestCaseData> GetArgs_TimeoutFactoryCancelationToken()
        {
            foreach (CompleteType completeType in Enum.GetValues(typeof(CompleteType)))
            foreach (int milliseconds in new[] { 0, 200, -1 })
            foreach (TimerFactoryType timerFactoryType in Enum.GetValues(typeof(TimerFactoryType)))
            foreach (CompleteTime completeTime in Enum.GetValues(typeof(CompleteTime)))
            foreach (WaitAsyncCancelType cancelType in Enum.GetValues(typeof(WaitAsyncCancelType)))
            {
                if (milliseconds == -1
                    && (completeTime == CompleteTime.None || timerFactoryType == TimerFactoryType.FakeImmediate)) continue;
                if (completeTime == CompleteTime.Immediate && cancelType == WaitAsyncCancelType.CancelBeforeComplete) continue;

                yield return new TestCaseData(completeType, milliseconds, timerFactoryType, completeTime, cancelType);
            }
        }

        [Test, TestCaseSource(nameof(GetArgs_TimeoutFactoryCancelationToken))]
        public void WaitAsync_TimeoutFactoryCancelationToken_void(CompleteType completeType, int milliseconds, TimerFactoryType timerFactoryType, CompleteTime completeTime, WaitAsyncCancelType cancelType)
        {
            var cancelationSource = CancelationSource.New();
            var cancelationToken = cancelType == WaitAsyncCancelType.DefaultToken ? default(CancelationToken)
                : cancelType == WaitAsyncCancelType.AlreadyCanceled ? CancelationToken.Canceled()
                : cancelationSource.Token;

            FakeTimerFactory fakeFactory = timerFactoryType == TimerFactoryType.FakeDelayed
                ? new FakeDelayedTimerFactory()
                : (FakeTimerFactory) new FakeImmediateTimerFactory();

            bool expectedCanceled =
                cancelType == WaitAsyncCancelType.AlreadyCanceled ? true
                : cancelType == WaitAsyncCancelType.DefaultToken || cancelType == WaitAsyncCancelType.CancelableToken_NoCancel || cancelType == WaitAsyncCancelType.CancelAfterComplete ? false
                // CancelBeforeComplete, expect canceled only if timeout is not immediate.
                : milliseconds != 0 && timerFactoryType != TimerFactoryType.FakeImmediate;
            bool expectedTimeout =
                expectedCanceled ? false
                : completeTime == CompleteTime.None ? true
                : completeTime == CompleteTime.Immediate ? false
                : milliseconds == 0 || timerFactoryType == TimerFactoryType.FakeImmediate;
            var expectedCompleteState =
                expectedCanceled ? Promise.State.Canceled
                : expectedTimeout ? Promise.State.Rejected
                : (Promise.State) completeType;

            var promise = TestHelper.BuildPromise(completeType, completeTime == CompleteTime.Immediate, rejectValue, out var tryCompleter)
                .WaitAsync(TimeSpan.FromMilliseconds(milliseconds), timerFactoryType == 0 ? TimerFactory.System : fakeFactory, cancelationToken);

            if (cancelType == WaitAsyncCancelType.CancelBeforeComplete)
            {
                cancelationSource.Cancel();
            }
            if (completeTime == CompleteTime.Delayed)
            {
                tryCompleter();
                if (cancelType == WaitAsyncCancelType.CancelAfterComplete)
                {
                    cancelationSource.Cancel();
                }
            }
            fakeFactory.Invoke();

            promise
                .ContinueWith(container =>
                {
                    Assert.AreEqual(expectedCompleteState, container.State);
                    if (expectedTimeout)
                    {
                        Assert.IsInstanceOf<TimeoutException>(container.Reason);
                    }
                })
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));

            if (completeTime != CompleteTime.Delayed)
            {
                tryCompleter.Invoke();
            }
            cancelationSource.Dispose();
        }

        [Test, TestCaseSource(nameof(GetArgs_TimeoutFactoryCancelationToken))]
        public void WaitAsync_TimeoutFactoryCancelationToken_T(CompleteType completeType, int milliseconds, TimerFactoryType timerFactoryType, CompleteTime completeTime, WaitAsyncCancelType cancelType)
        {
            var cancelationSource = CancelationSource.New();
            var cancelationToken = cancelType == WaitAsyncCancelType.DefaultToken ? default(CancelationToken)
                : cancelType == WaitAsyncCancelType.AlreadyCanceled ? CancelationToken.Canceled()
                : cancelationSource.Token;

            FakeTimerFactory fakeFactory = timerFactoryType == TimerFactoryType.FakeDelayed
                ? new FakeDelayedTimerFactory()
                : (FakeTimerFactory) new FakeImmediateTimerFactory();

            bool expectedCanceled =
                cancelType == WaitAsyncCancelType.AlreadyCanceled ? true
                : cancelType == WaitAsyncCancelType.DefaultToken || cancelType == WaitAsyncCancelType.CancelableToken_NoCancel || cancelType == WaitAsyncCancelType.CancelAfterComplete ? false
                // CancelBeforeComplete, expect canceled only if timeout is not immediate.
                : milliseconds != 0 && timerFactoryType != TimerFactoryType.FakeImmediate;
            bool expectedTimeout =
                expectedCanceled ? false
                : completeTime == CompleteTime.None ? true
                : completeTime == CompleteTime.Immediate ? false
                : milliseconds == 0 || timerFactoryType == TimerFactoryType.FakeImmediate;
            var expectedCompleteState =
                expectedCanceled ? Promise.State.Canceled
                : expectedTimeout ? Promise.State.Rejected
                : (Promise.State) completeType;
            const int resolveValue = 1;

            var promise = TestHelper.BuildPromise(completeType, completeTime == CompleteTime.Immediate, resolveValue, rejectValue, out var tryCompleter)
                .WaitAsync(TimeSpan.FromMilliseconds(milliseconds), timerFactoryType == 0 ? TimerFactory.System : fakeFactory, cancelationToken);

            if (cancelType == WaitAsyncCancelType.CancelBeforeComplete)
            {
                cancelationSource.Cancel();
            }
            if (completeTime == CompleteTime.Delayed)
            {
                tryCompleter();
                if (cancelType == WaitAsyncCancelType.CancelAfterComplete)
                {
                    cancelationSource.Cancel();
                }
            }
            fakeFactory.Invoke();

            promise
                .ContinueWith(container =>
                {
                    Assert.AreEqual(expectedCompleteState, container.State);
                    if (expectedCompleteState == Promise.State.Resolved)
                    {
                        Assert.AreEqual(resolveValue, container.Value);
                    }
                    if (expectedTimeout)
                    {
                        Assert.IsInstanceOf<TimeoutException>(container.Reason);
                    }
                })
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));

            if (completeTime != CompleteTime.Delayed)
            {
                tryCompleter.Invoke();
            }
            cancelationSource.Dispose();
        }
    }
}