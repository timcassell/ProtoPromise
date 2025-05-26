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

namespace ProtoPromise.Tests.APIs
{
    public enum TimerFactoryType
    {
#if !UNITY_WEBGL
        System = 0,
#endif
        FakeDelayed = 1,
        FakeImmediate = 2
    }

    public abstract class FakeTimerFactory : TimerFactory
    {
        internal virtual void Invoke() { }
    }

    public class FakeDelayedTimerFactory : FakeTimerFactory, ITimerSource
    {
        private TimerCallback _callback;
        private object _state;

        public override Proto.Timers.Timer CreateTimer(TimerCallback callback, object state, TimeSpan dueTime, TimeSpan period)
        {
            lock (this)
            {
                _callback = callback;
                _state = state;
            }
            return new Proto.Timers.Timer(this, 0);
        }

        void ITimerSource.Change(TimeSpan dueTime, TimeSpan period, int token) { }

        Promise ITimerSource.DisposeAsync(int token)
        {
            lock (this)
            {
                _callback = null;
                _state = null;
            }
            return Promise.Resolved();
        }

        internal override void Invoke()
        {
            TimerCallback callback;
            object state;
            lock (this)
            {
                callback = _callback;
                state = _state;
            }
            callback?.Invoke(state);
        }
    }

    public class FakeImmediateTimerFactory : FakeTimerFactory, ITimerSource
    {
        public override Proto.Timers.Timer CreateTimer(TimerCallback callback, object state, TimeSpan dueTime, TimeSpan period)
        {
            callback.Invoke(state);
            return new Proto.Timers.Timer(this, 0);
        }

        void ITimerSource.Change(TimeSpan dueTime, TimeSpan period, int token) { }

        Promise ITimerSource.DisposeAsync(int token)
            => Promise.Resolved();
    }

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
            TestHelper.Cleanup(spinForThreadPool: true);
        }

        [Test]
        public void PromiseDelay(
            [Values(0, 1, 500)] int milliseconds)
        {
            Promise.Delay(TimeSpan.FromMilliseconds(milliseconds))
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(2));
        }

        [Test]
        public void PromiseDelay_WithTimerFactory(
            [Values] TimerFactoryType timerFactoryType,
            [Values(0, 1, 500)] int milliseconds)
        {
            FakeTimerFactory fakeFactory = timerFactoryType == TimerFactoryType.FakeDelayed
                ? new FakeDelayedTimerFactory()
                : (FakeTimerFactory) new FakeImmediateTimerFactory();
            var promise = Promise.Delay(TimeSpan.FromMilliseconds(milliseconds),
                timerFactoryType == 0 ? TimerFactory.System : fakeFactory);

            fakeFactory.Invoke();
            promise.WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(2));
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
                .ContinueWith(resultContainer => result = resultContainer.State)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(2));

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
            if (cancelationType == CancelationType.Immediate)
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

            fakeFactory.Invoke();
            promise.WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(2));

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