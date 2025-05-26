#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using NUnit.Framework;
using Proto.Promises;
using Proto.Timers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

#if !UNITY_WEBGL

namespace ProtoPromise.Tests.Concurrency
{
    public enum FakeConcurrentTimerType
    {
        Immediate,
        NoInvoke,
        DisposeWhenInvoked,
        Invoke,
        BackgroundContext,
    }

    internal abstract class FakeConcurrentTimerFactory : TimerFactory
    {
        internal virtual void Invoke() { }

        internal static FakeConcurrentTimerFactory Create(FakeConcurrentTimerType type)
        {
            switch (type)
            {
                case FakeConcurrentTimerType.Immediate:          return new ImmediateTimerFactory();
                case FakeConcurrentTimerType.NoInvoke:           return new NoInvokeTimerFactory();
                case FakeConcurrentTimerType.DisposeWhenInvoked: return new DisposeWhenInvokedTimerFactory();
                case FakeConcurrentTimerType.Invoke:             return new InvokeTimerFactory();
                case FakeConcurrentTimerType.BackgroundContext:  return new BackgroundContextTimerFactory();
                default: throw new System.ArgumentOutOfRangeException(nameof(type));
            }
        }
    }

    internal class ImmediateTimerFactory : FakeConcurrentTimerFactory, ITimerSource
    {
        public override Proto.Timers.Timer CreateTimer(TimerCallback callback, object state, TimeSpan dueTime, TimeSpan period)
        {
            callback.Invoke(state);
            return new Proto.Timers.Timer(this, 0);
        }

        void ITimerSource.Change(TimeSpan dueTime, TimeSpan period, int token) { }
        Promise ITimerSource.DisposeAsync(int token) => Promise.Resolved();
    }

    internal class NoInvokeTimerFactory : FakeConcurrentTimerFactory, ITimerSource
    {
        public override Proto.Timers.Timer CreateTimer(TimerCallback callback, object state, TimeSpan dueTime, TimeSpan period)
        {
            return new Proto.Timers.Timer(this, 0);
        }

        void ITimerSource.Change(TimeSpan dueTime, TimeSpan period, int token) { }
        Promise ITimerSource.DisposeAsync(int token) => Promise.Resolved();
    }

    internal class DisposeWhenInvokedTimerFactory : FakeConcurrentTimerFactory, ITimerSource
    {
        private Promise.Deferred _deferred;
        private int _counter;

        public override Proto.Timers.Timer CreateTimer(TimerCallback callback, object state, TimeSpan dueTime, TimeSpan period)
        {
            lock (this)
            {
                Assert.True(_deferred == default);
                _counter = 2;
                _deferred = Promise.NewDeferred();
            }
            return new Proto.Timers.Timer(this, 0);
        }

        void ITimerSource.Change(TimeSpan dueTime, TimeSpan period, int token) { }

        Promise ITimerSource.DisposeAsync(int token)
        {
            Promise.Deferred deferred;
            lock (this)
            {
                deferred = _deferred;
                if (--_counter == 0)
                {
                    _deferred = default;
                }
            }
            return deferred.Promise;
        }

        internal override void Invoke()
        {
            Promise.Deferred deferred;
            lock (this)
            {
                deferred = _deferred;
                Assert.True(deferred != default);
                if (--_counter == 0)
                {
                    _deferred = default;
                }
            }

            deferred.Resolve();
        }
    }

    internal class InvokeTimerFactory : FakeConcurrentTimerFactory, ITimerSource
    {
        private Promise.Deferred _deferred;
        private TimerCallback _callback;
        private object _state;
        private int _counter;

        public override Proto.Timers.Timer CreateTimer(TimerCallback callback, object state, TimeSpan dueTime, TimeSpan period)
        {
            lock (this)
            {
                Assert.IsNull(_callback);
                _counter = 2;
                _deferred = Promise.NewDeferred();
                _callback = callback;
                _state = state;
            }
            return new Proto.Timers.Timer(this, 0);
        }

        void ITimerSource.Change(TimeSpan dueTime, TimeSpan period, int token) { }

        Promise ITimerSource.DisposeAsync(int token)
        {
            Promise.Deferred deferred;
            lock (this)
            {
                deferred = _deferred;
                if (--_counter == 0)
                {
                    _deferred = default;
                    _callback = null;
                    _state = null;
                }
            }
            return deferred.Promise;
        }

        internal override void Invoke()
        {
            Promise.Deferred deferred;
            TimerCallback callback;
            object state;
            lock (this)
            {
                callback = _callback;
                Assert.IsNotNull(callback);
                deferred = _deferred;
                state = _state;
                if (--_counter == 0)
                {
                    _deferred = default;
                    _callback = null;
                    _state = null;
                }
            }

            callback.Invoke(state);
            deferred.Resolve();
        }
    }

    internal class BackgroundContextTimerFactory : FakeConcurrentTimerFactory
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
            TestHelper.Cleanup(spinForThreadPool: true);
        }

        [Test]
        public void PromiseDelay_Concurrent()
        {
            var bag = new ConcurrentBag<Promise>();
            // 1 thread for call to Promise.Delay, 1 thread for timer callback.
            int concurrencyFactor = Math.Min(1, Environment.ProcessorCount / 2);
            var delayCalls = Enumerable.Repeat<Action>(() => bag.Add(Promise.Delay(TimeSpan.FromMilliseconds(1))), concurrencyFactor);
            new ThreadHelper().ExecuteParallelActionsMaybeWithOffsets(
                // setup
                () => { },
                // teardown
                () =>
                {
                    TestHelper._backgroundContext.WaitForAllThreadsToComplete();
                    Promise.All(bag)
                        .WaitWithTimeout(TimeSpan.FromSeconds(1));
                    bag = new ConcurrentBag<Promise>();
                },
                delayCalls.ToList()
            );
        }

        [Test]
        public void PromiseDelay_WithFakeTimerFactory_Concurrent()
        {
            var bag = new ConcurrentBag<Promise>();
            var fakeTimerFactory = new BackgroundContextTimerFactory();
            // 1 thread for call to Promise.Delay, 1 thread for timer callback.
            int concurrencyFactor = Math.Min(1, Environment.ProcessorCount / 2);
            var delayCalls = Enumerable.Repeat<Action>(() => bag.Add(Promise.Delay(TimeSpan.FromMilliseconds(1), fakeTimerFactory)), concurrencyFactor);
            new ThreadHelper().ExecuteParallelActionsMaybeWithOffsets(
                // setup
                () => { },
                // teardown
                () =>
                {
                    TestHelper._backgroundContext.WaitForAllThreadsToComplete();
                    Promise.All(bag)
                        .WaitWithTimeout(TimeSpan.FromSeconds(1));
                    bag = new ConcurrentBag<Promise>();
                },
                delayCalls.ToList()
            );
        }

        [Test]
        public void PromiseDelay_WithCancelationToken_Concurrent()
        {
            var bag = new ConcurrentBag<Promise>();
            // 1 thread for call to Promise.Delay, 1 thread for timer callback, 1 thread for cancelation.
            int concurrencyFactor = Math.Min(1, Environment.ProcessorCount / 3);
            var cancelationSources = new CancelationSource[concurrencyFactor];
            var parallelActions = new List<Action>(concurrencyFactor * 2);
            for (int i = 0; i < concurrencyFactor; ++i)
            {
                int index = i;
                parallelActions.Add(
                    () => cancelationSources[index].Cancel()
                );
                parallelActions.Add(
                    () => bag.Add(Promise.Delay(TimeSpan.FromMilliseconds(1), cancelationSources[index].Token))
                );
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
                    TestHelper._backgroundContext.WaitForAllThreadsToComplete();
                    Promise.AllSettled(bag)
                        .Then(results =>
                        {
                            foreach (var result in results)
                            {
                                result.RethrowIfRejected();
                            }
                        })
                        .WaitWithTimeout(TimeSpan.FromSeconds(1));
                    for (int i = 0; i < concurrencyFactor; ++i)
                    {
                        cancelationSources[i].Dispose();
                    }
                    bag = new ConcurrentBag<Promise>();
                },
                parallelActions
            );
        }

        private static IEnumerable<TestCaseData> GetArgs_Delay_WithTimerFactoryAndCancelationToken()
        {
            foreach (FakeConcurrentTimerType fakeTimerType in Enum.GetValues(typeof(FakeConcurrentTimerType)))
            foreach (ActionPlace actionPlace in new ActionPlace[] { ActionPlace.InSetup, ActionPlace.Parallel })
            {
                // These timer types rely on the timer being created, so we skip these combinations.
                if ((fakeTimerType == FakeConcurrentTimerType.Invoke || fakeTimerType == FakeConcurrentTimerType.DisposeWhenInvoked)
                    // If Delay is called in parallel with cancelations, the timer might not be created.
                    && actionPlace == ActionPlace.Parallel)
                {
                    continue;
                }
                yield return new TestCaseData(fakeTimerType, actionPlace);
            }
        }

        [Test, TestCaseSource(nameof(GetArgs_Delay_WithTimerFactoryAndCancelationToken))]
        public void PromiseDelay_WithTimerFactoryAndCancelationToken_Concurrent(
            FakeConcurrentTimerType fakeTimerType,
            ActionPlace actionPlace)
        {
            var bag = new ConcurrentBag<Promise>();
            var fakeTimerFactory = FakeConcurrentTimerFactory.Create(fakeTimerType);
            // 1 thread for call to Promise.Delay, 1 thread for timer callback, 1 thread for cancelation.
            int concurrencyFactor = Math.Min(1, Environment.ProcessorCount / 3);
            var cancelationSources = new CancelationSource[concurrencyFactor];
            var parallelActions = new List<Action>(concurrencyFactor * 2);
            for (int i = 0; i < concurrencyFactor; ++i)
            {
                int index = i;
                parallelActions.Add(
                    () => cancelationSources[index].Cancel()
                );
                parallelActions.Add(fakeTimerFactory.Invoke);
                if (actionPlace == ActionPlace.Parallel)
                {
                    parallelActions.Add(
                        () => bag.Add(Promise.Delay(TimeSpan.FromMilliseconds(1), fakeTimerFactory, cancelationSources[index].Token))
                    );
                }
            }
            new ThreadHelper().ExecuteParallelActionsMaybeWithOffsets(
                // setup
                () =>
                {
                    for (int i = 0; i < concurrencyFactor; ++i)
                    {
                        cancelationSources[i] = CancelationSource.New();
                        if (actionPlace == ActionPlace.InSetup)
                        {
                            int index = i;
                            bag.Add(Promise.Delay(TimeSpan.FromMilliseconds(1), fakeTimerFactory, cancelationSources[index].Token));
                        }
                    }
                },
                // teardown
                () =>
                {
                    TestHelper._backgroundContext.WaitForAllThreadsToComplete();
                    Promise.AllSettled(bag)
                        .Then(results =>
                        {
                            foreach (var result in results)
                            {
                                result.RethrowIfRejected();
                            }
                        })
                        .WaitWithTimeout(TimeSpan.FromSeconds(1));
                    for (int i = 0; i < concurrencyFactor; ++i)
                    {
                        cancelationSources[i].Dispose();
                    }
                    bag = new ConcurrentBag<Promise>();
                },
                parallelActions
            );
        }
    }
}

#endif