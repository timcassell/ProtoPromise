#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using NUnit.Framework;
using Proto.Promises;
using Proto.Timers;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;

namespace ProtoPromise.Tests.Unity
{
    public class UnityTimerFactoryTests
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
                Period = TimeSpan.FromMilliseconds(300);
            }

            public int Counter { get; set; }
            public TimeSpan Period { get; set; }
            public Timer Timer { get; set; }
            public float StartTime { get; set; }
            public float EndTime { get; set; }
            public Promise DisposePromise { get; set; }
        };

        [UnityTest]
        public IEnumerator UnityRealTimerFactory_CreateTimer(
            [Values(0.5f, 1f, 2f)] float timeScale)
        {
            float oldTimeScale = Time.timeScale;
            Time.timeScale = timeScale;

            TimerFactory provider = UnityRealTimerFactory.Instance;
            float minSeconds = 1.2f;
            TimerState state = new TimerState()
            {
                StartTime = Time.realtimeSinceStartup
            };

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
                                s.Period = TimeSpan.FromMilliseconds(400);
                                s.Timer.Change(s.Period, s.Period);
                                break;

                            case 4:
                                s.EndTime = Time.realtimeSinceStartup;
                                s.DisposePromise = s.Timer.DisposeAsync();
                                break;
                        }
                    }
                },
                state,
                state.Period, state.Period);

            yield return new WaitUntil(() => state.EndTime > 0f);
            using (var yieldInstruction = state.DisposePromise.ToYieldInstruction())
            {
                yield return yieldInstruction;
            }

            Assert.AreEqual(4, state.Counter);
            Assert.AreEqual(TimeSpan.FromMilliseconds(400), state.Period);
            var elapsed = state.EndTime - state.StartTime;
            Assert.GreaterOrEqual(elapsed, minSeconds, $"The total fired periods {elapsed}s expected to be greater than the expected min {minSeconds}s");

            Time.timeScale = oldTimeScale;
        }

        [UnityTest]
        public IEnumerator UnityRealTimerFactory_InvokeOnce(
            [Values(-1, 0)] int createPeriod,
            [Values(-1, 0)] int changePeriod)
        {
            TimerFactory provider = UnityRealTimerFactory.Instance;
            TimerState state = new TimerState();

            state.Timer = provider.CreateTimer(
                stat =>
                {
                    TimerState s = (TimerState) stat;
                    lock (s)
                    {
                        s.Counter++;
                    }
                },
                state,
                TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(createPeriod));

            yield return new WaitUntil(() => state.Counter > 0);
            Assert.AreEqual(1, state.Counter);
            yield return new WaitForSecondsRealtime(0.2f);
            Assert.AreEqual(1, state.Counter);

            state.Timer.Change(TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(changePeriod));

            yield return new WaitUntil(() => state.Counter > 1);
            Assert.AreEqual(2, state.Counter);
            yield return new WaitForSecondsRealtime(0.2f);
            Assert.AreEqual(2, state.Counter);

            using (var yieldInstruction = state.Timer.DisposeAsync().ToYieldInstruction())
            {
                yield return yieldInstruction;
            }
        }

        [UnityTest]
        public IEnumerator UnitySimulatedTimerFactory_CreateTimer(
            [Values(0.5f, 1f, 2f)] float timeScale)
        {
            float oldTimeScale = Time.timeScale;
            Time.timeScale = timeScale;

            TimerFactory provider = UnitySimulatedTimerFactory.Instance;
            float minSeconds = 1.2f;
            TimerState state = new TimerState()
            {
                StartTime = Time.time
            };

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
                                s.Period = TimeSpan.FromMilliseconds(400);
                                s.Timer.Change(s.Period, s.Period);
                                break;

                            case 4:
                                s.EndTime = Time.time;
                                s.DisposePromise = s.Timer.DisposeAsync();
                                break;
                        }
                    }
                },
                state,
                state.Period, state.Period);

            yield return new WaitUntil(() => state.EndTime > 0f);
            using (var yieldInstruction = state.DisposePromise.ToYieldInstruction())
            {
                yield return yieldInstruction;
            }

            Assert.AreEqual(4, state.Counter);
            Assert.AreEqual(TimeSpan.FromMilliseconds(400), state.Period);
            var elapsed = state.EndTime - state.StartTime;
            Assert.GreaterOrEqual(elapsed, minSeconds, $"The total fired periods {elapsed}s expected to be greater than the expected min {minSeconds}s");

            Time.timeScale = oldTimeScale;
        }

        [UnityTest]
        public IEnumerator UnitySimulatedTimerFactory_InvokeOnce(
            [Values(-1, 0)] int createPeriod,
            [Values(-1, 0)] int changePeriod)
        {
            TimerFactory provider = UnitySimulatedTimerFactory.Instance;
            TimerState state = new TimerState();

            state.Timer = provider.CreateTimer(
                stat =>
                {
                    TimerState s = (TimerState) stat;
                    lock (s)
                    {
                        s.Counter++;
                    }
                },
                state,
                TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(createPeriod));

            yield return new WaitUntil(() => state.Counter > 0);
            Assert.AreEqual(1, state.Counter);
            yield return new WaitForSeconds(0.2f);
            Assert.AreEqual(1, state.Counter);

            state.Timer.Change(TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(changePeriod));

            yield return new WaitUntil(() => state.Counter > 1);
            Assert.AreEqual(2, state.Counter);
            yield return new WaitForSeconds(0.2f);
            Assert.AreEqual(2, state.Counter);

            using (var yieldInstruction = state.Timer.DisposeAsync().ToYieldInstruction())
            {
                yield return yieldInstruction;
            }
        }

        // Written after the helper's call, so that IL2CPP's C++ compiler doesn't turn it into a tail call, which would remove the helper's frame from the trace
        // (see MiscellaneousTests.s_frameKeeper).
        private static int s_frameKeeper;

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static Timer CreateTimerForTraceTest(TimerFactory timerFactory, Proto.Diagnostics.CausalityTrace[] callbackTrace)
        {
            var timer = timerFactory.CreateTimer(_ => callbackTrace[0] = Proto.Diagnostics.CausalityTrace.Capture(),
                null, TimeSpan.FromMilliseconds(100), System.Threading.Timeout.InfiniteTimeSpan);
            System.Threading.Volatile.Write(ref s_frameKeeper, 1);
            return timer;
        }

        [UnityTest]
        public IEnumerator TimerCallback_RunsInsideTheScopeOfWhereTheTimerWasCreated(
            [Values] bool simulated)
        {
            Proto.Diagnostics.CausalityTrace.IsEnabled = true;
            try
            {
                TimerFactory provider = simulated
                    ? (TimerFactory) UnitySimulatedTimerFactory.Instance
                    : UnityRealTimerFactory.Instance;
                var callbackTrace = new Proto.Diagnostics.CausalityTrace[1];
                var timer = CreateTimerForTraceTest(provider, callbackTrace);

                float timeout = Time.realtimeSinceStartup + 5f;
                yield return new WaitUntil(() => callbackTrace[0] != null || Time.realtimeSinceStartup > timeout);
                using (var yieldInstruction = timer.DisposeAsync().ToYieldInstruction())
                {
                    yield return yieldInstruction;
                }

                Assert.IsNotNull(callbackTrace[0], "The timer callback was not invoked.");
                Assert.IsNotNull(callbackTrace[0].Previous, callbackTrace[0].ToString());
                StringAssert.Contains(nameof(CreateTimerForTraceTest), callbackTrace[0].Previous.StackTrace.ToString());
                CollectionAssert.IsEmpty(callbackTrace[0].OuterTraces, callbackTrace[0].ToString());
            }
            finally
            {
                Proto.Diagnostics.CausalityTrace.IsEnabled = false;
            }
        }
    }
}