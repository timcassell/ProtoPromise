#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using NUnit.Framework;
using Proto.Promises;
using Proto.Timers;
using System;
using System.Threading;

namespace ProtoPromiseTests.APIs
{
    public class DelayTests
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

        public enum TimerFactoryType
        {
#if !UNITY_WEBGL
            System = 0,
#endif
            FakeDelayed = 1,
            FakeImmediate = 2
        }

        private abstract class FakeTimerFactory : TimerFactory
        {
            internal virtual void Invoke() { }
        }

        private class FakeDelayedTimerFactory : FakeTimerFactory
        {
            private class FakeTimerSource : ITimerSource
            {
                internal static FakeTimerSource s_instance = new FakeTimerSource();

                private TimerCallback _callback;
                private object _state;

                internal static FakeTimerSource Create(TimerCallback callback, object state)
                {
                    s_instance._callback = callback;
                    s_instance._state = state;
                    return s_instance;
                }

                void ITimerSource.Change(TimeSpan dueTime, TimeSpan period, int token) { }
                
                Promise ITimerSource.DisposeAsync(int token)
                {
                    _callback = null;
                    _state = null;
                    return Promise.Resolved();
                }

                internal void Invoke()
                    => _callback?.Invoke(_state);
            }

            public override Proto.Timers.Timer CreateTimer(TimerCallback callback, object state, TimeSpan dueTime, TimeSpan period)
            {
                var fakeTimer = FakeTimerSource.Create(callback, state);
                return new Proto.Timers.Timer(fakeTimer, 0);
            }

            internal override void Invoke()
                => FakeTimerSource.s_instance.Invoke();
        }

        private class FakeImmediateTimerFactory : FakeTimerFactory
        {
            private class FakeTimerSource : ITimerSource
            {
                internal static FakeTimerSource s_instance = new FakeTimerSource();

                void ITimerSource.Change(TimeSpan dueTime, TimeSpan period, int token) { }

                Promise ITimerSource.DisposeAsync(int token)
                    => Promise.Resolved();
            }

            public override Proto.Timers.Timer CreateTimer(TimerCallback callback, object state, TimeSpan dueTime, TimeSpan period)
            {
                callback.Invoke(state);
                var fakeTimer = FakeTimerSource.s_instance;
                return new Proto.Timers.Timer(fakeTimer, 0);
            }
        }

        [Test]
        public void PromiseDelay(
            [Values(0, 1, 500)] int milliseconds)
        {
            bool continued = false;
            Promise.Delay(TimeSpan.FromMilliseconds(milliseconds))
                .Then(() => continued = true)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(2));

            Assert.True(continued);
        }

        [Test]
        public void PromiseDelay_WithTimerFactory(
            [Values] TimerFactoryType timerFactoryType,
            [Values(0, 1, 500)] int milliseconds)
        {
            FakeTimerFactory fakeFactory = timerFactoryType == TimerFactoryType.FakeDelayed
                ? new FakeDelayedTimerFactory()
                : (FakeTimerFactory) new FakeImmediateTimerFactory();
            bool continued = false;
            var promise = Promise.Delay(TimeSpan.FromMilliseconds(milliseconds),
                timerFactoryType == 0 ? TimerFactory.System : fakeFactory)
                .Then(() => continued = true);

            if (milliseconds == 0)
            {
                promise.Forget();
            }
            else
            {
                fakeFactory.Invoke();
                promise.WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(2));
            }
            Assert.True(continued);
        }

        [Test]
        public void PromiseDelay_WithCancelationToken(
            [Values] CancelationType cancelationType,
            [Values(0, 1, 500)] int milliseconds)
        {
            var cancelationSource = CancelationSource.New();
            if (cancelationType == CancelationType.Immediate)
            {
                cancelationSource.Cancel();
            }

            Promise.State result = Promise.State.Pending;
            var promise = Promise.Delay(TimeSpan.FromMilliseconds(milliseconds), cancelationSource.Token)
                .ContinueWith(resultContainer => result = resultContainer.State);

            if (milliseconds == 0)
            {
                promise.Forget();
            }
            else
            {
                promise.WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(2));
            }

            if (cancelationType == CancelationType.Deferred)
            {
                cancelationSource.Cancel();
            }

            Promise.State expectedResult = cancelationType == CancelationType.Immediate
                ? Promise.State.Canceled
                : Promise.State.Resolved;
            Assert.AreEqual(expectedResult, result);

            cancelationSource.Dispose();
        }

        [Test]
        public void PromiseDelay_WithTimerFactoryAndCancelationToken(
            [Values] TimerFactoryType timerFactoryType,
            [Values] CancelationType cancelationType,
            [Values(0, 1, 500)] int milliseconds)
        {
            var cancelationSource = CancelationSource.New();
            if ((milliseconds == 0 || timerFactoryType == TimerFactoryType.FakeImmediate)
                && cancelationType == CancelationType.Immediate)
            {
                cancelationSource.Cancel();
            }

            FakeTimerFactory fakeFactory = timerFactoryType == TimerFactoryType.FakeDelayed
                ? new FakeDelayedTimerFactory()
                : (FakeTimerFactory) new FakeImmediateTimerFactory();
            Promise.State result = Promise.State.Pending;
            var promise = Promise.Delay(TimeSpan.FromMilliseconds(milliseconds),
                timerFactoryType == 0 ? TimerFactory.System : fakeFactory,
                cancelationSource.Token)
                .ContinueWith(resultContainer => result = resultContainer.State);

            if (milliseconds == 0 || timerFactoryType == TimerFactoryType.FakeImmediate)
            {
                promise.Forget();
            }
            else
            {
                if (cancelationType == CancelationType.Immediate)
                {
                    cancelationSource.Cancel();
                }

                fakeFactory.Invoke();
                promise.WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(2));
            }

            if (cancelationType == CancelationType.Deferred)
            {
                cancelationSource.Cancel();
            }

            Promise.State expectedResult = cancelationType == CancelationType.Immediate
                ? Promise.State.Canceled
                : Promise.State.Resolved;
            Assert.AreEqual(expectedResult, result);
            
            cancelationSource.Dispose();
        }
    }
}