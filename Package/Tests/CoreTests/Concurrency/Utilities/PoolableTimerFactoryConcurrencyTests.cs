#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#if !UNITY_WEBGL

using NUnit.Framework;
using Proto.Promises;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace ProtoPromiseTests.Concurrency.Utilities
{
    public class PoolableTimerFactoryConcurrencyTests
    {
        [SetUp]
        public void Setup()
        {
            TestHelper.Setup();
        }

        [TearDown]
        public void Teardown()
        {
            TestHelper.Cleanup();
        }

        private sealed class CustomTimeProvider : TimeProvider { }

        [Test]
        public void PoolableTimerFactory_CreateTimer_Concurrent(
            [Values(0, 1, 2)] int numPeriods1,
            [Values(0, 1, 2)] int numPeriods2,
            [Values] bool customTimeProvider,
            [Values] bool flowExecutionContext)
        {
            // Normally AsyncFlowExecutionContextEnabled cannot be disabled after it is enabled, but we need to test both configurations.
            Promise.Config.s_asyncFlowExecutionContextEnabled = flowExecutionContext;

            PoolableTimerFactory provider = customTimeProvider
                ? PoolableTimerFactory.FromTimeProvider(new CustomTimeProvider())
                : PoolableTimerFactory.System;

            int stateChecker = 0;
            int invokedCounter = 0;
            int timersRunningCounter = ThreadHelper.multiExecutionCount;

            new ThreadHelper().ExecuteMultiActionParallel(() =>
            {
                int periodCounter = 0;
                int state = Interlocked.Increment(ref stateChecker);
                IPoolableTimer timer = null;
                timer = provider.CreateTimer(s =>
                {
                    Interlocked.Increment(ref invokedCounter);
                    Assert.AreEqual(state, s);
                    if ((Interlocked.Increment(ref periodCounter) - 1) == numPeriods1)
                    {
                        SpinWait.SpinUntil(() => timer != null);
                        timer.DisposeAsync().Forget();

                        int periodCounter2 = 0;
                        int state2 = Interlocked.Increment(ref stateChecker);
                        IPoolableTimer timer2 = null;
                        timer2 = provider.CreateTimer(s2 =>
                        {
                            Interlocked.Increment(ref invokedCounter);
                            Assert.AreEqual(state2, s2);
                            if ((Interlocked.Increment(ref periodCounter2) - 1) == numPeriods2)
                            {
                                SpinWait.SpinUntil(() => timer2 != null);
                                timer2.DisposeAsync().Forget();

                                Interlocked.Decrement(ref timersRunningCounter);
                            }
                        }, state2, TimeSpan.FromMilliseconds(1), numPeriods2 == 0 ? Timeout.InfiniteTimeSpan : TimeSpan.FromMilliseconds(1));
                    }
                }, state, TimeSpan.FromMilliseconds(1), numPeriods1 == 0 ? Timeout.InfiniteTimeSpan : TimeSpan.FromMilliseconds(1));
            });

            int expectedInvokes = ThreadHelper.multiExecutionCount * (numPeriods1 + numPeriods2 + 2);
            SpinWait.SpinUntil(() => timersRunningCounter == 0, TimeSpan.FromMilliseconds(expectedInvokes * 4));
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