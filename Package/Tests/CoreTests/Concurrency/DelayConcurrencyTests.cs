#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using NUnit.Framework;
using Proto.Promises;
using Proto.Timers;
using System;
using System.Linq;
using System.Threading;

#if !UNITY_WEBGL

namespace ProtoPromiseTests.Concurrency
{
    public class DelayConcurrencyTests
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

        private class FakeTimerFactory : TimerFactory
        {
            private class FakeTimerSource : ITimerSource
            {
                private readonly Promise.Deferred deferred = Promise.NewDeferred();
                private TimerCallback _callback;
                private object _state;

                public FakeTimerSource(TimerCallback callback, object state)
                {
                    _callback = callback;
                    _state = state;
                }

                void ITimerSource.Change(TimeSpan dueTime, TimeSpan period, int token) { }

                Promise ITimerSource.DisposeAsync(int token)
                    => deferred.Promise;

                internal void Invoke()
                {
                    _callback.Invoke(_state);
                    deferred.Resolve();
                }
            }

            public override Proto.Timers.Timer CreateTimer(TimerCallback callback, object state, TimeSpan dueTime, TimeSpan period)
            {
                var fakeTimer = new FakeTimerSource(callback, state);
                TestHelper._backgroundContext.Post(obj => obj.UnsafeAs<FakeTimerSource>().Invoke(), fakeTimer);
                return new Proto.Timers.Timer(fakeTimer, 0);
            }
        }

        [Test]
        public void PromiseDelay()
        {
            // 1 thread for call to Promise.Delay, 1 thread for timer callback.
            int concurrencyFactor = Environment.ProcessorCount / 2;
            var delayCalls = Enumerable.Repeat<Action>(() => Promise.Delay(TimeSpan.FromMilliseconds(1)).Forget(), concurrencyFactor);
            new ThreadHelper().ExecuteParallelActionsMaybeWithOffsets(
                // setup
                () => { },
                // teardown
                () => TestHelper._backgroundContext.WaitForAllThreadsToComplete(),
                delayCalls.ToList()
            );
        }

        [Test]
        public void PromiseDelay_WithFakeTimerFactory()
        {
            var fakeTimerFactory = new FakeTimerFactory();
            // 1 thread for call to Promise.Delay, 1 thread for timer callback.
            int concurrencyFactor = Environment.ProcessorCount / 2;
            var delayCalls = Enumerable.Repeat<Action>(() => Promise.Delay(TimeSpan.FromMilliseconds(1), fakeTimerFactory).Forget(), concurrencyFactor);
            new ThreadHelper().ExecuteParallelActionsMaybeWithOffsets(
                // setup
                () => { },
                // teardown
                () => TestHelper._backgroundContext.WaitForAllThreadsToComplete(),
                delayCalls.ToList()
            );
        }

        [Test]
        public void PromiseDelay_WithCancelationToken()
        {
            // 1 thread for call to Promise.Delay, 1 thread for timer callback, 1 thread for cancelation.
            int concurrencyFactor = Environment.ProcessorCount / 3;
            var cancelationSources = new CancelationSource[concurrencyFactor];
            var delayCalls = new Action[concurrencyFactor];
            var cancelationCalls = new Action[concurrencyFactor];
            for (int i = 0; i < concurrencyFactor; ++i)
            {
                int index = i;
                cancelationCalls[index] = () => cancelationSources[index].Cancel();
                delayCalls[index] = () => Promise.Delay(TimeSpan.FromMilliseconds(1), cancelationSources[index].Token).Forget();
            }
            new ThreadHelper().ExecuteParallelActionsMaybeWithOffsets(
                // setup
                () =>
                {
                    for (int i = 0; i < concurrencyFactor; ++i)
                    {
                        cancelationSources[i] = CancelationSource.New();
                    }
                },
                // teardown
                () =>
                {
                    for (int i = 0; i < concurrencyFactor; ++i)
                    {
                        cancelationSources[i].Dispose();
                    }
                    TestHelper._backgroundContext.WaitForAllThreadsToComplete();
                },
                delayCalls.Concat(cancelationCalls).ToList()
            );
        }

        [Test]
        public void PromiseDelay_WithTimerFactoryAndCancelationToken()
        {
            var fakeTimerFactory = new FakeTimerFactory();
            // 1 thread for call to Promise.Delay, 1 thread for timer callback, 1 thread for cancelation.
            int concurrencyFactor = Environment.ProcessorCount / 3;
            var cancelationSources = new CancelationSource[concurrencyFactor];
            var delayCalls = new Action[concurrencyFactor]; 
            var cancelationCalls = new Action[concurrencyFactor];
            for (int i = 0; i < concurrencyFactor; ++i)
            {
                int index = i;
                cancelationCalls[index] = () => cancelationSources[index].Cancel();
                delayCalls[index] = () => Promise.Delay(TimeSpan.FromMilliseconds(1), fakeTimerFactory, cancelationSources[index].Token).Forget();
            }
            new ThreadHelper().ExecuteParallelActionsMaybeWithOffsets(
                // setup
                () =>
                {
                    for (int i = 0; i < concurrencyFactor; ++i)
                    {
                        cancelationSources[i] = CancelationSource.New();
                    }
                },
                // teardown
                () =>
                {
                    for (int i = 0; i < concurrencyFactor; ++i)
                    {
                        cancelationSources[i].Dispose();
                    }
                    TestHelper._backgroundContext.WaitForAllThreadsToComplete();
                },
                delayCalls.Concat(cancelationCalls).ToList()
            );
        }
    }
}

#endif