#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#if !UNITY_WEBGL

using NUnit.Framework;
using Proto.Promises;
using Proto.Promises.Threading;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace ProtoPromiseTests.APIs.Utilities
{
    public class PoolableTimerFactoryTests
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

        private class TimerState
        {
            public TimerState()
            {
                Counter = 0;
                Period = 300;
                Stopwatch = new Stopwatch();
                Stopwatch.Start();
            }

            public int Counter { get; set; }
            public int Period { get; set; }
            public IPoolableTimer Timer { get; set; }
            public Stopwatch Stopwatch { get; set; }
            public Promise DisposePromise { get; set; }
        };

        private sealed class CustomTimeProvider : TimeProvider { }

        [Test]
        public void PoolableTimerFactory_CreateTimer(
            [Values] bool customTimeProvider,
            [Values] bool flowExecutionContext)
        {
            // Normally AsyncFlowExecutionContextEnabled cannot be disabled after it is enabled, but we need to test both configurations.
            Promise.Config.s_asyncFlowExecutionContextEnabled = flowExecutionContext;

            PoolableTimerFactory provider = customTimeProvider
                ? PoolableTimerFactory.FromTimeProvider(new CustomTimeProvider())
                : PoolableTimerFactory.System;
            int minMilliseconds = 1200;
            TimerState state = new TimerState();

            lock (state)
            {
                state.Timer = provider.CreateTimer(
                stat =>
                {
                    TimerState s = (TimerState) stat;
                    lock (s)
                    {
                        s.Counter++;

                        switch (s.Counter)
                        {
                            case 2:
                                s.Period = 400;
                                s.Timer.Change(TimeSpan.FromMilliseconds(s.Period), TimeSpan.FromMilliseconds(s.Period));
                                break;

                            case 4:
                                s.Stopwatch.Stop();
                                s.DisposePromise = s.Timer.DisposeAsync();
                                s.Timer = null;
                                break;
                        }
                    }
                },
                state,
                TimeSpan.FromMilliseconds(state.Period), TimeSpan.FromMilliseconds(state.Period));
            }

            SpinWait.SpinUntil(() => state.Timer == null, TimeSpan.FromSeconds(8));
            state.DisposePromise.WaitWithTimeout(TimeSpan.FromSeconds(2));

            Assert.AreEqual(4, state.Counter);
            Assert.AreEqual(400, state.Period);
            Assert.GreaterOrEqual(state.Stopwatch.ElapsedMilliseconds, minMilliseconds, $"The total fired periods {state.Stopwatch.ElapsedMilliseconds}ms expected to be greater than the expected min {minMilliseconds}ms");
        }
    }
}

#endif