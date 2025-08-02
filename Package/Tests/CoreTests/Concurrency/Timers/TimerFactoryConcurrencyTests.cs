#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#if !UNITY_WEBGL

using NUnit.Framework;
using Proto.Promises;
using Proto.Promises.Threading;
using Proto.Timers;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace ProtoPromise.Tests.Concurrency.Timers
{
    public class TimerFactoryConcurrencyTests
    {
        [SetUp]
        public void Setup()
        {
            TestHelper.Setup();
        }

        [TearDown]
        public void Teardown()
        {
            TestHelper.Cleanup(spinForThreadPool: true);
        }

        private sealed class CustomTimeProvider : TimeProvider { }

        [Test]
        public void TimerFactory_CreateTimer_Concurrent(
            [Values(0, 1, 2)] int numPeriods1,
            [Values(0, 1, 2)] int numPeriods2,
            [Values] bool customTimeProvider,
            [Values] bool flowExecutionContext)
        {
            // Normally AsyncFlowExecutionContextEnabled cannot be disabled after it is enabled, but we need to test both configurations.
            Promise.Config.s_asyncFlowExecutionContextEnabled = flowExecutionContext;

            TimerFactory provider = customTimeProvider
                ? TimerFactory.FromTimeProvider(new CustomTimeProvider())
                : TimerFactory.System;

            int stateChecker = 0;
            int invokedCounter = 0;
            int timersRunningCounter = ThreadHelper.multiExecutionCount;

            var disposePromises = new ConcurrentBag<Promise>();

            new ThreadHelper().ExecuteMultiActionParallel(() =>
            {
                int periodCounter = 0;
                int state = Interlocked.Increment(ref stateChecker);
                Proto.Timers.Timer timer = default;
                timer = provider.CreateTimer(s =>
                {
                    Interlocked.Increment(ref invokedCounter);
                    Assert.AreEqual(state, s);
                    if ((Interlocked.Increment(ref periodCounter) - 1) == numPeriods1)
                    {
                        TestHelper.SpinUntil(() => timer != default, TimeSpan.FromSeconds(1));
                        disposePromises.Add(timer.DisposeAsync());

                        int periodCounter2 = 0;
                        int state2 = Interlocked.Increment(ref stateChecker);
                        Proto.Timers.Timer timer2 = default;
                        timer2 = provider.CreateTimer(s2 =>
                        {
                            Interlocked.Increment(ref invokedCounter);
                            Assert.AreEqual(state2, s2);
                            if ((Interlocked.Increment(ref periodCounter2) - 1) == numPeriods2)
                            {
                                TestHelper.SpinUntil(() => timer2 != default, TimeSpan.FromSeconds(1));
                                disposePromises.Add(timer2.DisposeAsync());

                                Interlocked.Decrement(ref timersRunningCounter);
                            }
                        }, state2, TimeSpan.FromMilliseconds(1), numPeriods2 == 0 ? Timeout.InfiniteTimeSpan : TimeSpan.FromMilliseconds(1));
                    }
                }, state, TimeSpan.FromMilliseconds(1), numPeriods1 == 0 ? Timeout.InfiniteTimeSpan : TimeSpan.FromMilliseconds(1));
            });

            int expectedInvokes = ThreadHelper.multiExecutionCount * (numPeriods1 + numPeriods2 + 2);
            TestHelper.SpinUntil(() => timersRunningCounter == 0, TimeSpan.FromSeconds(expectedInvokes));
            Promise.All(disposePromises).WaitWithTimeout(TimeSpan.FromSeconds(1));
            if ((numPeriods1 + numPeriods2) == 0)
            {
                Assert.AreEqual(expectedInvokes, invokedCounter);
            }
            else
            {
                // With a small period, the timer could fire again on another thread before the timer is diposed.
                // We just make sure there are at least the amount of expected invokes.
                Assert.GreaterOrEqual(invokedCounter, expectedInvokes);
            }
        }
    }
}

#endif