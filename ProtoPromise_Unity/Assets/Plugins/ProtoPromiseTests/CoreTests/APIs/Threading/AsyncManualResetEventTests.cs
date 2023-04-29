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

namespace ProtoPromiseTests.APIs.Threading
{
    public class AsyncManualResetEventTests
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
        public void AsyncManualResetEvent_StateTrans([Values] bool init)
        {
            AsyncManualResetEvent ev = new AsyncManualResetEvent(init);
            Assert.AreEqual(init, ev.IsSet);

            if (!init)
            {
                ev.Reset();
                Assert.False(ev.IsSet);
            }

            for (int i = 0; i < 50; i++)
            {
                ev.Set();
                Assert.True(ev.IsSet);

                ev.Reset();
                Assert.False(ev.IsSet);
            }
        }

#if !UNITY_WEBGL
        [Test]
        // Uses 3 events to coordinate between two threads.
        public void AsyncManualResetEvent_2ThreadCoordination()
        {
            AsyncManualResetEvent ev1 = new AsyncManualResetEvent(false);
            AsyncManualResetEvent ev2 = new AsyncManualResetEvent(false);
            AsyncManualResetEvent ev3 = new AsyncManualResetEvent(false);

            bool first = false, second = false, third = false;

            Promise.Run(() =>
            {
                Assert.False(first);
                Assert.False(second);
                Assert.False(third);

                first = true;
                ev1.Set();
                // No asserts here, race condition with other thread until we wait.
                ev2.Wait();
                Assert.True(first);
                Assert.True(second);
                Assert.False(third);

                third = true;
                ev3.Set();
                // No asserts here, other thread will assert after wait completes.
            }, SynchronizationOption.Background)
                .Forget();

            Assert.False(second);
            Assert.False(third);

            ev1.Wait();
            Assert.True(first);
            Assert.False(second);
            Assert.False(third);

            second = true;
            ev2.Set();
            // No asserts here, race condition with other thread until we wait.
            ev3.Wait();
            Assert.True(first);
            Assert.True(second);
            Assert.True(third);
        }

        [Test]
        public void AsyncManualResetEvent_CancelAfterWait()
        {
            AsyncManualResetEvent mre = new AsyncManualResetEvent();
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
            Assert.False(mre.TryWait(cs.Token));

            cs.Dispose();
        }
#endif

        [Test]
        public void AsyncManualResetEvent_CancelBeforeWait()
        {
            AsyncManualResetEvent mre = new AsyncManualResetEvent();
            CancelationSource cs = CancelationSource.New();
            cs.Cancel();

            Assert.False(mre.TryWait(cs.Token));

            cs.Dispose();
        }

        [Test]
        public void AsyncManualResetEvent_CancelAfterSetAndBeforeWait()
        {
            AsyncManualResetEvent are = new AsyncManualResetEvent();
            are.Set();
            CancelationSource cs = CancelationSource.New();
            cs.Cancel();

            Assert.True(are.TryWait(cs.Token));

            cs.Dispose();
        }

        [Test]
        public void AsyncManualResetEvent_AsyncCoordination_Then()
        {
            AsyncManualResetEvent ev1 = new AsyncManualResetEvent(false);
            AsyncManualResetEvent ev2 = new AsyncManualResetEvent(false);
            AsyncManualResetEvent ev3 = new AsyncManualResetEvent(false);

            bool first = false, second = false, third = false;

            Promise.Run(() =>
            {
                Assert.False(second);
                Assert.False(third);

                return ev1.WaitAsync()
                    .Then(() =>
                    {
                        Assert.True(first);
                        Assert.False(second);
                        Assert.False(third);

                        second = true;
                        ev2.Set();
                        return ev3.WaitAsync();
                    })
                    .Then(() =>
                    {
                        Assert.True(first);
                        Assert.True(second);
                        Assert.True(third);
                    });
            }, SynchronizationOption.Synchronous)
                .Forget();

            Promise.Run(() =>
            {
                Assert.False(first);
                Assert.False(second);
                Assert.False(third);

                first = true;
                ev1.Set();
                return ev2.WaitAsync()
                    .Then(() =>
                    {
                        Assert.True(first);
                        Assert.True(second);
                        Assert.False(third);

                        third = true;
                        ev3.Set();
                    });
            }, SynchronizationOption.Synchronous)
                .Forget();

            // WaitAsync continuations happen on the caller's context, so we need to execute the foreground context.
            // There were 3 waits and sets, so we execute it 3 times.
            TestHelper.ExecuteForegroundCallbacks();
            TestHelper.ExecuteForegroundCallbacks();
            TestHelper.ExecuteForegroundCallbacks();
            Assert.True(first);
            Assert.True(second);
            Assert.True(third);
        }

        [Test]
        public void AsyncManualResetEvent_CancelAfterWaitAsync()
        {
            AsyncManualResetEvent mre = new AsyncManualResetEvent();
            CancelationSource cs = CancelationSource.New();

            var promise = mre.TryWaitAsync(cs.Token);

            cs.Cancel();

            Assert.False(promise.WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1)));

            cs.Dispose();
        }

        [Test]
        public void AsyncManualResetEvent_CancelBeforeWaitAsync()
        {
            AsyncManualResetEvent mre = new AsyncManualResetEvent();
            CancelationSource cs = CancelationSource.New();
            cs.Cancel();

            Assert.False(mre.TryWaitAsync(cs.Token).WaitWithTimeout(TimeSpan.FromSeconds(1)));

            cs.Dispose();
        }

        [Test]
        public void AsyncManualResetEvent_CancelAfterSetAndBeforeWaitAsync()
        {
            AsyncManualResetEvent are = new AsyncManualResetEvent();
            are.Set();
            CancelationSource cs = CancelationSource.New();
            cs.Cancel();

            Assert.True(are.TryWaitAsync(cs.Token).WaitWithTimeout(TimeSpan.FromSeconds(1)));

            cs.Dispose();
        }

#if CSHARP_7_3_OR_NEWER
        [Test]
        public void AsyncManualResetEvent_AsyncCoordination_Async()
        {
            AsyncManualResetEvent ev1 = new AsyncManualResetEvent(false);
            AsyncManualResetEvent ev2 = new AsyncManualResetEvent(false);
            AsyncManualResetEvent ev3 = new AsyncManualResetEvent(false);

            bool first = false, second = false, third = false;

            Promise.Run(async () =>
            {
                Assert.False(second);
                Assert.False(third);

                await ev1.WaitAsync();
                Assert.True(first);
                Assert.False(second);
                Assert.False(third);

                second = true;
                ev2.Set();
                await ev3.WaitAsync();
                Assert.True(first);
                Assert.True(second);
                Assert.True(third);
            }, SynchronizationOption.Synchronous)
                .Forget();

            Promise.Run(async () =>
            {
                Assert.False(first);
                Assert.False(second);
                Assert.False(third);

                first = true;
                ev1.Set();
                await ev2.WaitAsync();
                Assert.True(first);
                Assert.True(second);
                Assert.False(third);

                third = true;
                ev3.Set();
            }, SynchronizationOption.Synchronous)
                .Forget();

            // WaitAsync continuations happen on the caller's context, so we need to execute the foreground context.
            // There were 3 waits and sets, so we execute it 3 times.
            TestHelper.ExecuteForegroundCallbacks();
            TestHelper.ExecuteForegroundCallbacks();
            TestHelper.ExecuteForegroundCallbacks();
            Assert.True(first);
            Assert.True(second);
            Assert.True(third);
        }
#endif // CSHARP_7_3_OR_NEWER

        [Test]
        public void AsyncManualResetEvent_WaitAsync_AfterSet_IsCompleted()
        {
            var mre = new AsyncManualResetEvent();

            mre.Set();

            bool isComplete = false;
            mre.WaitAsync()
                .Then(() => isComplete = true)
                .Forget();

            Assert.True(isComplete);
        }

        [Test, Timeout(1000)]
        public void AsyncManualResetEvent_Wait_AfterSet_IsCompleted()
        {
            var mre = new AsyncManualResetEvent();

            mre.Set();
            mre.Wait();
        }

        [Test]
        public void AsyncManualResetEvent_WaitAsync_Set_IsCompleted()
        {
            var mre = new AsyncManualResetEvent(true);

            bool isComplete = false;
            mre.WaitAsync()
                .Then(() => isComplete = true)
                .Forget();

            Assert.True(isComplete);
        }

        [Test, Timeout(1000)]
        public void AsyncManualResetEvent_Wait_Set_IsCompleted()
        {
            var mre = new AsyncManualResetEvent(true);

            mre.Wait();
        }

        [Test]
        public void AsyncManualResetEvent_MultipleWaitAsync_AfterSet_IsCompleted()
        {
            var mre = new AsyncManualResetEvent();

            mre.Set();

            bool isComplete = false;
            mre.WaitAsync()
                .Then(() => isComplete = true)
                .Forget();
            Assert.True(isComplete);

            isComplete = false;
            mre.WaitAsync()
                .Then(() => isComplete = true)
                .Forget();
            Assert.True(isComplete);
        }

        [Test, Timeout(1000)]
        public void AsyncManualResetEvent_MultipleWait_AfterSet_IsCompleted()
        {
            var mre = new AsyncManualResetEvent();

            mre.Set();
            mre.Wait();
            mre.Wait();
        }

        [Test]
        public void AsyncManualResetEvent_MultipleWaitAsync_Set_IsCompleted()
        {
            var mre = new AsyncManualResetEvent(true);

            bool isComplete = false;
            mre.WaitAsync()
                .Then(() => isComplete = true)
                .Forget();
            Assert.True(isComplete);

            isComplete = false;
            mre.WaitAsync()
                .Then(() => isComplete = true)
                .Forget();
            Assert.True(isComplete);
        }

        [Test, Timeout(1000)]
        public void AsyncManualResetEvent_MultipleWait_Set_IsCompleted()
        {
            var mre = new AsyncManualResetEvent(true);

            mre.Wait();
            mre.Wait();
        }

#if PROTO_PROMISE_TEST_GC_ENABLED
        [Test]
        public void AsyncManualResetEvent_AbandonedResetEventReported()
        {
            var currentRejectionHandler = Promise.Config.UncaughtRejectionHandler;
            AbandonedResetEventException abandonedResetEventException = null;
            Promise.Config.UncaughtRejectionHandler = ex =>
            {
                abandonedResetEventException = ex.Value as AbandonedResetEventException;
            };

            WaitAndAbandonManualResetEvent();
            TestHelper.GcCollectAndWaitForFinalizers();
            TestHelper.ExecuteForegroundCallbacksAndWaitForThreadsToComplete();
            Assert.IsNotNull(abandonedResetEventException);

            Promise.Config.UncaughtRejectionHandler = currentRejectionHandler;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void WaitAndAbandonManualResetEvent()
        {
            var mre = new AsyncManualResetEvent(false);
            mre.WaitAsync()
                .Forget();
        }
#endif // PROTO_PROMISE_TEST_GC_ENABLED
    }
}