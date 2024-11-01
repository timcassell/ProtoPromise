#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using NUnit.Framework;
using Proto.Promises;
using Proto.Promises.Threading;
using ProtoPromiseTests.Concurrency;
using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace ProtoPromiseTests.APIs.Threading
{
    public class AsyncAutoResetEventTests
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
        public void AsyncAutoResetEvent_StateTrans([Values] bool init)
        {
            AsyncAutoResetEvent ev = new AsyncAutoResetEvent(init);
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

        [Test, Timeout(1000)]
        public void AsyncAutoResetEvent_SetAndResetTest()
        {
            var e = new AsyncAutoResetEvent(true);
            e.Reset();
            Assert.False(e.TryWait(CancelationToken.Canceled()));
            Assert.False(e.TryWait(CancelationToken.Canceled()));
            e.Reset();
            Assert.False(e.TryWait(CancelationToken.Canceled()));
            e.Set();
            Assert.True(e.TryWait(CancelationToken.Canceled()));
            Assert.False(e.TryWait(CancelationToken.Canceled()));
            e.Set();
            e.Set();
            Assert.True(e.TryWait(CancelationToken.Canceled()));
        }

#if !UNITY_WEBGL
        [Test]
        public void AsyncAutoResetEvent_PingPong()
        {
            const int Iters = 10;
            var are1 = new AsyncAutoResetEvent(true);
            var are2 = new AsyncAutoResetEvent(false);
            Promise.All(
                Promise.Run(() =>
                {
                    for (int i = 0; i < Iters; i++)
                    {
                        are1.Wait();
                        are2.Set();
                    }
                }, SynchronizationOption.Background, forceAsync: true),
                Promise.Run(() =>
                {
                    for (int i = 0; i < Iters; i++)
                    {
                        are2.Wait();
                        are1.Set();
                    }
                }, SynchronizationOption.Background, forceAsync: true))
                .WaitWithTimeout(TimeSpan.FromSeconds(Iters));
        }

        [Test]
        public void AsyncAutoResetEvent_CancelAfterWait()
        {
            AsyncAutoResetEvent are = new AsyncAutoResetEvent();
            CancelationSource cs = CancelationSource.New();
            bool isAboutToWait = false;

            Promise.Run(() =>
            {
                TestHelper.SpinUntil(() => isAboutToWait, TimeSpan.FromSeconds(1));
                Thread.Sleep(TimeSpan.FromSeconds(0.5f));
                cs.Cancel();
            }, SynchronizationOption.Background)
                .Forget();

            isAboutToWait = true;
            Assert.False(are.TryWait(cs.Token));

            cs.Dispose();
        }
#endif

        [Test]
        public void AsyncAutoResetEvent_CancelBeforeWait()
        {
            AsyncAutoResetEvent are = new AsyncAutoResetEvent();
            CancelationSource cs = CancelationSource.New();
            cs.Cancel();

            Assert.False(are.TryWait(cs.Token));

            cs.Dispose();
        }

        [Test]
        public void AsyncAutoResetEvent_CancelAfterSetAndBeforeWait()
        {
            AsyncAutoResetEvent are = new AsyncAutoResetEvent();
            are.Set();
            CancelationSource cs = CancelationSource.New();
            cs.Cancel();

            Assert.True(are.TryWait(cs.Token));

            cs.Dispose();
        }

        [Test]
        public void AsyncAutoResetEvent_AsyncCoordination_Then()
        {
            AsyncAutoResetEvent ev1 = new AsyncAutoResetEvent(false);
            AsyncAutoResetEvent ev2 = new AsyncAutoResetEvent(false);
            AsyncAutoResetEvent ev3 = new AsyncAutoResetEvent(false);

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
        public void AsyncAutoResetEvent_CancelAfterWaitAsync()
        {
            AsyncAutoResetEvent are = new AsyncAutoResetEvent();
            CancelationSource cs = CancelationSource.New();

            var promise = are.TryWaitAsync(cs.Token);

            cs.Cancel();

            Assert.False(promise.WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1)));

            cs.Dispose();
        }

        [Test]
        public void AsyncAutoResetEvent_CancelBeforeWaitAsync()
        {
            AsyncAutoResetEvent are = new AsyncAutoResetEvent();
            CancelationSource cs = CancelationSource.New();
            cs.Cancel();

            Assert.False(are.TryWaitAsync(cs.Token).WaitWithTimeout(TimeSpan.FromSeconds(1)));

            cs.Dispose();
        }

        [Test]
        public void AsyncAutoResetEvent_CancelAfterSetAndBeforeWaitAsync()
        {
            AsyncAutoResetEvent are = new AsyncAutoResetEvent();
            are.Set();
            CancelationSource cs = CancelationSource.New();
            cs.Cancel();

            Assert.True(are.TryWaitAsync(cs.Token).WaitWithTimeout(TimeSpan.FromSeconds(1)));

            cs.Dispose();
        }

        [Test]
        public void AsyncAutoResetEvent_AsyncCoordination_Async()
        {
            AsyncAutoResetEvent ev1 = new AsyncAutoResetEvent(false);
            AsyncAutoResetEvent ev2 = new AsyncAutoResetEvent(false);
            AsyncAutoResetEvent ev3 = new AsyncAutoResetEvent(false);

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

        [Test]
        public void AsyncAutoResetEvent_WaitAsync_AfterSet_IsCompleted()
        {
            var are = new AsyncAutoResetEvent();

            are.Set();

            bool isComplete = false;
            are.WaitAsync()
                .Then(() => isComplete = true)
                .Forget();

            Assert.True(isComplete);
        }

        [Test, Timeout(1000)]
        public void AsyncAutoResetEvent_Wait_AfterSet_IsCompleted()
        {
            var are = new AsyncAutoResetEvent();

            are.Set();
            are.Wait();
        }

        [Test]
        public void AsyncAutoResetEvent_WaitAsync_Set_IsCompleted()
        {
            var are = new AsyncAutoResetEvent(true);

            bool isComplete = false;
            are.WaitAsync()
                .Then(() => isComplete = true)
                .Forget();

            Assert.True(isComplete);
        }

        [Test, Timeout(1000)]
        public void AsyncAutoResetEvent_Wait_Set_IsCompleted()
        {
            var are = new AsyncAutoResetEvent(true);

            are.Wait();
        }

        [Test]
        public void AsyncAutoResetEvent_MultipleWaitAsync_AfterSet_OnlyOneIsCompleted()
        {
            var are = new AsyncAutoResetEvent();

            are.Set();

            bool isComplete = false;
            are.WaitAsync()
                .Then(() => isComplete = true)
                .Forget();
            Assert.True(isComplete);

            isComplete = false;
            are.WaitAsync()
                .Then(() => isComplete = true)
                .Forget();
            Assert.False(isComplete);
            Assert.False(are.IsSet);

            are.Set();
            TestHelper.ExecuteForegroundCallbacks();
            Assert.True(isComplete);
        }

        [Test]
        public void AsyncAutoResetEvent_MultipleWaitAsync_Set_OnlyOneIsCompleted()
        {
            var are = new AsyncAutoResetEvent(true);

            bool isComplete = false;
            are.WaitAsync()
                .Then(() => isComplete = true)
                .Forget();
            Assert.True(isComplete);

            isComplete = false;
            are.WaitAsync()
                .Then(() => isComplete = true)
                .Forget();
            Assert.False(isComplete);
            Assert.False(are.IsSet);

            are.Set();
            TestHelper.ExecuteForegroundCallbacks();
            Assert.True(isComplete);
        }

        [Test]
        public void AsyncAutoResetEvent_WaitAsyncWithContinuationOptions_ContinuesOnConfiguredContext_Then(
            [Values] SynchronizationType continuationContext,
            [Values] CompletedContinuationBehavior completedBehavior,
            [Values(SynchronizationType.Foreground, SynchronizationType.Background)] SynchronizationType invokeContext)
        {
            var foregroundThread = Thread.CurrentThread;
            var are = new AsyncAutoResetEvent(false);

            var promise = are.WaitAsync(TestHelper.GetContinuationOptions(continuationContext, completedBehavior))
                .Then(() =>
                {
                    TestHelper.AssertCallbackContext(continuationContext, invokeContext, foregroundThread);
                });

            Assert.False(are.IsSet);
            new ThreadHelper().ExecuteSynchronousOrOnThread(
                () => are.Set(),
                invokeContext == SynchronizationType.Foreground
            );
            promise.WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AsyncAutoResetEvent_TryWaitAsyncWithContinuationOptions_ContinuesOnConfiguredContext_Then(
            [Values] SynchronizationType continuationContext,
            [Values] CompletedContinuationBehavior completedBehavior,
            [Values(SynchronizationType.Foreground, SynchronizationType.Background)] SynchronizationType invokeContext)
        {
            var foregroundThread = Thread.CurrentThread;
            var are = new AsyncAutoResetEvent(false);
            var cancelationSource = CancelationSource.New();

            var promise = are.TryWaitAsync(cancelationSource.Token, TestHelper.GetContinuationOptions(continuationContext, completedBehavior))
                .Then(success =>
                {
                    Assert.True(success);
                    TestHelper.AssertCallbackContext(continuationContext, invokeContext, foregroundThread);
                });

            Assert.False(are.IsSet);
            new ThreadHelper().ExecuteSynchronousOrOnThread(
                () => are.Set(),
                invokeContext == SynchronizationType.Foreground
            );
            promise.WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));

            cancelationSource.Dispose();
        }

        [Test]
        public void AsyncAutoResetEvent_WaitAsyncWithContinuationOptions_ContinuesOnConfiguredContext_await(
            [Values] SynchronizationType continuationContext,
            [Values] CompletedContinuationBehavior completedBehavior,
            [Values(SynchronizationType.Foreground, SynchronizationType.Background)] SynchronizationType invokeContext)
        {
            var foregroundThread = Thread.CurrentThread;
            var are = new AsyncAutoResetEvent(false);

            var promise = Promise.Run(async () =>
            {
                await are.WaitAsync(TestHelper.GetContinuationOptions(continuationContext, completedBehavior));
                TestHelper.AssertCallbackContext(continuationContext, invokeContext, foregroundThread);
            }, SynchronizationOption.Synchronous);

            Assert.False(are.IsSet);
            new ThreadHelper().ExecuteSynchronousOrOnThread(
                () => are.Set(),
                invokeContext == SynchronizationType.Foreground
            );
            promise.WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AsyncAutoResetEvent_TryWaitAsyncWithContinuationOptions_ContinuesOnConfiguredContext_await(
            [Values] SynchronizationType continuationContext,
            [Values] CompletedContinuationBehavior completedBehavior,
            [Values(SynchronizationType.Foreground, SynchronizationType.Background)] SynchronizationType invokeContext)
        {
            var foregroundThread = Thread.CurrentThread;
            var are = new AsyncAutoResetEvent(false);
            var cancelationSource = CancelationSource.New();

            var promise = Promise.Run(async () =>
            {
                var success = await are.TryWaitAsync(cancelationSource.Token, TestHelper.GetContinuationOptions(continuationContext, completedBehavior));
                Assert.True(success);
                TestHelper.AssertCallbackContext(continuationContext, invokeContext, foregroundThread);
            }, SynchronizationOption.Synchronous);

            Assert.False(are.IsSet);
            new ThreadHelper().ExecuteSynchronousOrOnThread(
                () => are.Set(),
                invokeContext == SynchronizationType.Foreground
            );
            promise.WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));

            cancelationSource.Dispose();
        }

#if PROTO_PROMISE_TEST_GC_ENABLED
        [Test]
        public void AsyncAutoResetEvent_AbandonedResetEventReported()
        {
            var currentRejectionHandler = Promise.Config.UncaughtRejectionHandler;
            AbandonedResetEventException abandonedResetEventException = null;
            Promise.Config.UncaughtRejectionHandler = ex =>
            {
                abandonedResetEventException = ex.Value as AbandonedResetEventException;
            };

            WaitAndAbandonAutoResetEvent();
            TestHelper.GcCollectAndWaitForFinalizers();
            TestHelper.ExecuteForegroundCallbacksAndWaitForThreadsToComplete();
            Assert.IsNotNull(abandonedResetEventException);

            Promise.Config.UncaughtRejectionHandler = currentRejectionHandler;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void WaitAndAbandonAutoResetEvent()
        {
            var are = new AsyncAutoResetEvent(false);
            are.WaitAsync()
                .Forget();
        }
#endif // PROTO_PROMISE_TEST_GC_ENABLED
    }
}