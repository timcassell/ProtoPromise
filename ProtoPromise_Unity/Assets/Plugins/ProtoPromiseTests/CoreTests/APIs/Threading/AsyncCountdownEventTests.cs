#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using NUnit.Framework;
using Proto.Promises;
using Proto.Promises.Threading;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Xml.Schema;

namespace ProtoPromiseTests.APIs.Threading
{
    public class AsyncCountdownEventTests
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

        [Test]
        public void AsyncCountdownEvent_WaitAsync_Unset_IsNotCompleted()
        {
            var ce = new AsyncCountdownEvent(1);
            bool isComplete = false;
            ce.WaitAsync()
                .Then(() => isComplete = true)
                .Forget();
            Assert.AreEqual(1, ce.CurrentCount);
            Assert.False(isComplete);

            ce.Signal();
            Assert.AreEqual(0, ce.CurrentCount);
            TestHelper.ExecuteForegroundCallbacks();
            Assert.True(isComplete);
        }

        [Test]
        public void AsyncCountdownEvent_WaitAsync_Set_IsCompleted()
        {
            var ce = new AsyncCountdownEvent(0);
            bool isComplete = false;
            ce.WaitAsync()
                .Then(() => isComplete = true)
                .Forget();

            Assert.AreEqual(0, ce.CurrentCount);
            Assert.True(isComplete);
        }

        [Test]
        public void AsyncCountdownEvent_AddCount_IncrementsCount()
        {
            var ce = new AsyncCountdownEvent(1);
            bool isComplete = false;
            ce.WaitAsync()
                .Then(() => isComplete = true)
                .Forget();
            Assert.AreEqual(1, ce.CurrentCount);
            Assert.False(isComplete);

            ce.AddCount();

            Assert.AreEqual(2, ce.CurrentCount);
            Assert.False(isComplete);

            ce.Signal(2);
            Assert.AreEqual(0, ce.CurrentCount);
            TestHelper.ExecuteForegroundCallbacks();
            Assert.True(isComplete);
        }

        [Test]
        public void AsyncCountdownEvent_Signal_Nonzero_IsNotCompleted()
        {
            var ce = new AsyncCountdownEvent(2);
            bool isComplete = false;
            ce.WaitAsync()
                .Then(() => isComplete = true)
                .Forget();
            Assert.AreEqual(2, ce.CurrentCount);
            Assert.False(isComplete);

            ce.Signal();

            Assert.AreEqual(1, ce.CurrentCount);
            Assert.False(isComplete);

            ce.Signal();
            Assert.AreEqual(0, ce.CurrentCount);
            TestHelper.ExecuteForegroundCallbacks();
            Assert.True(isComplete);
        }

        [Test]
        public void AsyncCountdownEvent_Signal_LessThan1_Throws()
        {
            var ce = new AsyncCountdownEvent(5);
            Assert.Catch<System.ArgumentOutOfRangeException>(() => ce.Signal(0));
            Assert.Catch<System.ArgumentOutOfRangeException>(() => ce.Signal(-1));
        }

        [Test]
        public void AsyncCountdownEvent_Signal_AfterSet_Throws()
        {
            var ce = new AsyncCountdownEvent(0);
            Assert.Catch<System.InvalidOperationException>(() => ce.Signal());
        }

        [Test]
        public void AsyncCountdownEvent_Signal_MoreThanCurrentCount_Throws()
        {
            var ce = new AsyncCountdownEvent(1);
            Assert.Catch<System.InvalidOperationException>(() => ce.Signal(2));
        }

        [Test]
        public void AsyncCountdownEvent_AddCount_AfterSet_Throws()
        {
            var ce = new AsyncCountdownEvent(0);
            Assert.Catch<System.InvalidOperationException>(() => ce.AddCount());
            Assert.Catch<System.InvalidOperationException>(() => ce.AddCount(2));
        }

        [Test]
        public void AsyncCountdownEvent_TryAddCount_AfterSet_ReturnsFalse()
        {
            var ce = new AsyncCountdownEvent(0);
            Assert.False(ce.TryAddCount());
            Assert.False(ce.TryAddCount(2));
        }

        [Test]
        public void AsyncCountdownEvent_AddCount_LessThan1_Throws()
        {
            var ce = new AsyncCountdownEvent(1);
            Assert.Catch<System.ArgumentOutOfRangeException>(() => ce.AddCount(0));
            Assert.Catch<System.ArgumentOutOfRangeException>(() => ce.AddCount(-1));
        }

        [Test]
        public void AsyncCountdownEvent_TryAddCount_LessThan1_Throws()
        {
            var ce = new AsyncCountdownEvent(1);
            Assert.Catch<System.ArgumentOutOfRangeException>(() => ce.TryAddCount(0));
            Assert.Catch<System.ArgumentOutOfRangeException>(() => ce.TryAddCount(-1));
        }

        [Test]
        public void AsyncCountdownEvent_AddCount_TooMany_Throws()
        {
            var ce = new AsyncCountdownEvent(int.MaxValue);
            Assert.Catch<System.InvalidOperationException>(() => ce.AddCount());
            ce.Reset(1);
            Assert.Catch<System.InvalidOperationException>(() => ce.AddCount(int.MaxValue));
        }

        [Test]
        public void AsyncCountdownEvent_TryAddCount_TooMany_Throws()
        {
            var ce = new AsyncCountdownEvent(int.MaxValue);
            Assert.Catch<System.InvalidOperationException>(() => ce.TryAddCount());
            ce.Reset(1);
            Assert.Catch<System.InvalidOperationException>(() => ce.TryAddCount(int.MaxValue));
        }

        [Test]
        public void AsyncCountdownEvent_NegativeInitialCount_Throws()
        {
            Assert.Catch<System.ArgumentOutOfRangeException>(() => new AsyncCountdownEvent(-1));
        }

        [Test]
        public void AsyncCountdownEvent_CancelBeforeWait()
        {
            AsyncCountdownEvent ce = new AsyncCountdownEvent(1);
            CancelationSource cs = CancelationSource.New();
            cs.Cancel();

            Assert.False(ce.TryWait(cs.Token));

            cs.Dispose();
        }

        [Test]
        public void AsyncCountdownEvent_CancelAfterSetAndBeforeWait()
        {
            AsyncCountdownEvent ce = new AsyncCountdownEvent(1);
            ce.Signal();
            CancelationSource cs = CancelationSource.New();
            cs.Cancel();

            Assert.True(ce.TryWait(cs.Token));

            cs.Dispose();
        }

        [Test]
        public void AsyncCountdownEvent_CancelAfterWaitAsync()
        {
            AsyncCountdownEvent ce = new AsyncCountdownEvent(1);
            CancelationSource cs = CancelationSource.New();

            var promise = ce.TryWaitAsync(cs.Token);

            cs.Cancel();

            Assert.False(promise.WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1)));

            cs.Dispose();
        }

        [Test]
        public void AsyncCountdownEvent_CancelBeforeWaitAsync()
        {
            AsyncCountdownEvent ce = new AsyncCountdownEvent(1);
            CancelationSource cs = CancelationSource.New();
            cs.Cancel();

            Assert.False(ce.TryWaitAsync(cs.Token).WaitWithTimeout(TimeSpan.FromSeconds(1)));

            cs.Dispose();
        }

        [Test]
        public void AsyncCountdownEvent_CancelAfterSetAndBeforeWaitAsync()
        {
            AsyncCountdownEvent ce = new AsyncCountdownEvent(1);
            ce.Signal();
            CancelationSource cs = CancelationSource.New();
            cs.Cancel();

            Assert.True(ce.TryWaitAsync(cs.Token).WaitWithTimeout(TimeSpan.FromSeconds(1)));

            cs.Dispose();
        }

#if !UNITY_WEBGL
        [Test]
        public void AsyncCountdownEvent_CancelAfterWait()
        {
            AsyncCountdownEvent ce = new AsyncCountdownEvent(1);
            CancelationSource cs = CancelationSource.New();
            bool isAboutToWait = false;

            Promise.Run(() =>
            {
                SpinWait.SpinUntil(() => isAboutToWait);
                Thread.Sleep(TimeSpan.FromSeconds(0.5f));
                cs.Cancel();
            }, SynchronizationOption.Background)
                .Forget();

            isAboutToWait = true;
            Assert.False(ce.TryWait(cs.Token));

            cs.Dispose();
        }
#endif

#if PROTO_PROMISE_TEST_GC_ENABLED
        [Test]
        public void AsyncCountdownEvent_AbandonedResetEventReported()
        {
            var currentRejectionHandler = Promise.Config.UncaughtRejectionHandler;
            AbandonedResetEventException abandonedResetEventException = null;
            Promise.Config.UncaughtRejectionHandler = ex =>
            {
                abandonedResetEventException = ex.Value as AbandonedResetEventException;
            };

            WaitAndAbandonCountdownEvent();
            TestHelper.GcCollectAndWaitForFinalizers();
            TestHelper.ExecuteForegroundCallbacksAndWaitForThreadsToComplete();
            Assert.IsNotNull(abandonedResetEventException);

            Promise.Config.UncaughtRejectionHandler = currentRejectionHandler;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void WaitAndAbandonCountdownEvent()
        {
            var ce = new AsyncCountdownEvent(1);
            ce.WaitAsync()
                .Forget();
        }
#endif // PROTO_PROMISE_TEST_GC_ENABLED
    }
}