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
    public class AsyncSemaphoreTests
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

        private static void AsyncSemaphore_Ctor_Helper(int initial, int maximum, bool expectThrow)
        {
            if (expectThrow)
            {
                Assert.Catch<System.ArgumentOutOfRangeException>(() =>
                {
                    AsyncSemaphore semaphore = new AsyncSemaphore(initial, maximum);
                    Assert.AreEqual(initial, semaphore.CurrentCount);
                });
            }
            else
            {
                AsyncSemaphore semaphore = new AsyncSemaphore(initial, maximum);
                Assert.AreEqual(initial, semaphore.CurrentCount);
            }
        }

        [Test]
        public static void AsyncSemaphore_Ctor()
        {
            AsyncSemaphore_Ctor_Helper(0, 10, false);
            AsyncSemaphore_Ctor_Helper(5, 10, false);
            AsyncSemaphore_Ctor_Helper(10, 10, false);
        }

        [Test]
        public static void AsyncSemaphore_Ctor_Negative()
        {
            AsyncSemaphore_Ctor_Helper(10, 0, true);
            AsyncSemaphore_Ctor_Helper(10, -1, true);
            AsyncSemaphore_Ctor_Helper(-1, 10, true);
        }

        [Test]
        public void AsyncSemaphore_WaitAsync_SlotAvailable_IsCompletedSynchronously()
        {
            var semaphore = new AsyncSemaphore(1, 1);
            Assert.AreEqual(1, semaphore.CurrentCount);

            bool complete1 = false;
            semaphore.WaitAsync()
                .Then(() => complete1 = true)
                .Forget();
            Assert.AreEqual(0, semaphore.CurrentCount);
            Assert.True(complete1);

            bool complete2 = false;
            semaphore.WaitAsync()
                .Then(() => complete2 = true)
                .Forget();
            Assert.AreEqual(0, semaphore.CurrentCount);
            Assert.False(complete2);

            semaphore.Release();
            TestHelper.ExecuteForegroundCallbacks();
            Assert.AreEqual(0, semaphore.CurrentCount);
            Assert.True(complete2);

            semaphore.Release();
            Assert.AreEqual(1, semaphore.CurrentCount);
        }

        [Test, Timeout(1000)]
        public void AsyncSemaphore_Wait_SlotAvailable()
        {
            var semaphore = new AsyncSemaphore(1, 1);
            Assert.AreEqual(1, semaphore.CurrentCount);

            semaphore.Wait();
            Assert.AreEqual(0, semaphore.CurrentCount);

            semaphore.Release();
            Assert.AreEqual(1, semaphore.CurrentCount);
        }

        [Test]
        public void AsyncSemaphore_WaitAsync_MultipleSlotsAvailable_MultipleCompleteSynchronously()
        {
            var semaphore = new AsyncSemaphore(2, 2);
            Assert.AreEqual(2, semaphore.CurrentCount);

            bool complete1 = false;
            semaphore.WaitAsync()
                .Then(() => complete1 = true)
                .Forget();
            Assert.AreEqual(1, semaphore.CurrentCount);
            Assert.True(complete1);

            bool complete2 = false;
            semaphore.WaitAsync()
                .Then(() => complete2 = true)
                .Forget();
            Assert.AreEqual(0, semaphore.CurrentCount);
            Assert.True(complete2);

            semaphore.Release();
            Assert.AreEqual(1, semaphore.CurrentCount);
            semaphore.Release();
            Assert.AreEqual(2, semaphore.CurrentCount);
        }

        [Test, Timeout(1000)]
        public void AsyncSemaphore_Wait_MultipleSlotsAvailable()
        {
            var semaphore = new AsyncSemaphore(2, 2);
            Assert.AreEqual(2, semaphore.CurrentCount);
            
            semaphore.Wait();
            Assert.AreEqual(1, semaphore.CurrentCount);
            semaphore.Wait();
            Assert.AreEqual(0, semaphore.CurrentCount);

            semaphore.Release();
            Assert.AreEqual(1, semaphore.CurrentCount);
            semaphore.Release();
            Assert.AreEqual(2, semaphore.CurrentCount);
        }

        [Test]
        public void AsyncSemaphore_TryWaitAsync_PreCanceled_SlotAvailable_SucceedsSynchronously()
        {
            var semaphore = new AsyncSemaphore(1, 1);
            Assert.AreEqual(1, semaphore.CurrentCount);

            Promise.State state = Promise.State.Pending;
            semaphore.TryWaitAsync(CancelationToken.Canceled())
                .Then(success => state = success ? Promise.State.Resolved : Promise.State.Canceled)
                .Forget();
            Assert.AreEqual(0, semaphore.CurrentCount);
            Assert.AreEqual(Promise.State.Resolved, state);

            semaphore.Release();
            Assert.AreEqual(1, semaphore.CurrentCount);
        }

        [Test, Timeout(1000)]
        public void AsyncSemaphore_TryWait_PreCanceled_SlotAvailable_Succeeds()
        {
            var semaphore = new AsyncSemaphore(1, 1);
            Assert.AreEqual(1, semaphore.CurrentCount);

            bool success = semaphore.TryWait(CancelationToken.Canceled());
            Assert.AreEqual(0, semaphore.CurrentCount);
            Assert.True(success);

            semaphore.Release();
            Assert.AreEqual(1, semaphore.CurrentCount);
        }

        [Test]
        public void AsyncSemaphore_TryWaitAsync_PreCanceled_NoSlotAvailable_CancelsSynchronously()
        {
            var semaphore = new AsyncSemaphore(0, 1);
            Assert.AreEqual(0, semaphore.CurrentCount);

            Promise.State state = Promise.State.Pending;
            semaphore.TryWaitAsync(CancelationToken.Canceled())
                .Then(success => state = success ? Promise.State.Resolved : Promise.State.Canceled)
                .Forget();
            Assert.AreEqual(0, semaphore.CurrentCount);
            Assert.AreEqual(Promise.State.Canceled, state);

            semaphore.Release();
            Assert.AreEqual(1, semaphore.CurrentCount);
        }

        [Test, Timeout(1000)]
        public void AsyncSemaphore_TryWait_PreCanceled_NoSlotAvailable_Cancels()
        {
            var semaphore = new AsyncSemaphore(0, 1);
            Assert.AreEqual(0, semaphore.CurrentCount);

            bool success = semaphore.TryWait(CancelationToken.Canceled());
            Assert.AreEqual(0, semaphore.CurrentCount);
            Assert.False(success);

            semaphore.Release();
            Assert.AreEqual(1, semaphore.CurrentCount);
        }

        [Test]
        public void AsyncSemaphore_TryWaitAsync_Canceled_DoesNotTakeSlot()
        {
            var semaphore = new AsyncSemaphore(0);
            var cancelationSource = CancelationSource.New();
            Assert.AreEqual(0, semaphore.CurrentCount);

            Promise.State state = Promise.State.Pending;
            semaphore.TryWaitAsync(cancelationSource.Token)
                .Then(success => state = success ? Promise.State.Resolved : Promise.State.Canceled)
                .Forget();
            Assert.AreEqual(0, semaphore.CurrentCount);
            Assert.AreEqual(Promise.State.Pending, state);

            cancelationSource.Cancel();
            TestHelper.ExecuteForegroundCallbacks();
            Assert.AreEqual(Promise.State.Canceled, state);
            
            semaphore.Release();
            Assert.AreEqual(1, semaphore.CurrentCount);

            cancelationSource.Dispose();
        }

        [Test]
        public void AsyncSemaphore_Release_WithoutWaiters_IncrementsCount()
        {
            var semaphore = new AsyncSemaphore(0);
            Assert.AreEqual(0, semaphore.CurrentCount);
            semaphore.Release();
            Assert.AreEqual(1, semaphore.CurrentCount);
            semaphore.Release();
            Assert.AreEqual(2, semaphore.CurrentCount);

            bool complete = false;
            semaphore.WaitAsync()
                .Then(() => complete = true)
                .Forget();
            Assert.AreEqual(1, semaphore.CurrentCount);
            Assert.True(complete);
        }

        [Test]
        public void AsyncSemaphore_ReleaseMultiple_WithoutWaiters_IncrementsCount()
        {
            var semaphore = new AsyncSemaphore(0);
            Assert.AreEqual(0, semaphore.CurrentCount);
            semaphore.Release(2);
            Assert.AreEqual(2, semaphore.CurrentCount);
            semaphore.Release(3);
            Assert.AreEqual(5, semaphore.CurrentCount);

            bool complete = false;
            semaphore.WaitAsync()
                .Then(() => complete = true)
                .Forget();
            Assert.AreEqual(4, semaphore.CurrentCount);
            Assert.True(complete);
        }

        [Test]
        public void AsyncSemaphore_Release_WithWaiters_Releases1Waiter_Async()
        {
            var semaphore = new AsyncSemaphore(0);
            var cancelationSource = CancelationSource.New();

            bool complete1 = false;
            semaphore.WaitAsync()
                .Then(() => complete1 = true)
                .Forget();
            Assert.AreEqual(0, semaphore.CurrentCount);
            Assert.False(complete1);

            bool complete2 = false;
            semaphore.TryWaitAsync(cancelationSource.Token)
                .Then(success =>
                {
                    Assert.True(success);
                    complete2 = true;
                })
                .Forget();
            Assert.AreEqual(0, semaphore.CurrentCount);
            Assert.False(complete2);

            semaphore.Release();
            Assert.AreEqual(0, semaphore.CurrentCount);
            TestHelper.ExecuteForegroundCallbacks();
            Assert.True(complete1);
            Assert.False(complete2);

            semaphore.Release();
            Assert.AreEqual(0, semaphore.CurrentCount);
            TestHelper.ExecuteForegroundCallbacks();
            Assert.True(complete1);
            Assert.True(complete2);

            bool complete3 = false;
            semaphore.WaitAsync()
                .Then(() => complete3 = true)
                .Forget();
            Assert.AreEqual(0, semaphore.CurrentCount);
            Assert.False(complete3);

            semaphore.Release();
            Assert.AreEqual(0, semaphore.CurrentCount);
            TestHelper.ExecuteForegroundCallbacks();
            Assert.True(complete3);

            cancelationSource.Dispose();
        }

        [Test]
        public void AsyncSemaphore_ReleaseMultiple_WithWaiters_ReleasesWaitersCount_Async()
        {
            var semaphore = new AsyncSemaphore(0);
            var cancelationSource = CancelationSource.New();

            bool complete1 = false;
            semaphore.WaitAsync()
                .Then(() => complete1 = true)
                .Forget();
            Assert.AreEqual(0, semaphore.CurrentCount);
            Assert.False(complete1);

            bool complete2 = false;
            semaphore.TryWaitAsync(cancelationSource.Token)
                .Then(success =>
                {
                    Assert.True(success);
                    complete2 = true;
                })
                .Forget();
            Assert.AreEqual(0, semaphore.CurrentCount);
            Assert.False(complete2);

            bool complete3 = false;
            semaphore.WaitAsync()
                .Then(() => complete3 = true)
                .Forget();
            Assert.AreEqual(0, semaphore.CurrentCount);
            Assert.False(complete3);

            bool complete4 = false;
            semaphore.TryWaitAsync(CancelationToken.None)
                .Then(success =>
                {
                    Assert.True(success);
                    complete4 = true;
                })
                .Forget();
            Assert.AreEqual(0, semaphore.CurrentCount);
            Assert.False(complete4);

            semaphore.Release(2);
            TestHelper.ExecuteForegroundCallbacks();
            Assert.AreEqual(0, semaphore.CurrentCount);
            Assert.True(complete1);
            Assert.True(complete2);
            Assert.False(complete3);
            Assert.False(complete4);

            semaphore.Release(1);
            Assert.AreEqual(0, semaphore.CurrentCount);
            TestHelper.ExecuteForegroundCallbacks();
            Assert.True(complete1);
            Assert.True(complete2);
            Assert.True(complete3);
            Assert.False(complete4);

            semaphore.Release(4);
            Assert.AreEqual(3, semaphore.CurrentCount);
            TestHelper.ExecuteForegroundCallbacks();
            Assert.True(complete1);
            Assert.True(complete2);
            Assert.True(complete3);
            Assert.True(complete4);

            cancelationSource.Dispose();
        }

        [Test]
        public void AsyncSemaphore_ReleaseMultiple_WithWaiters_ReleasesExactWaitersCount_Async()
        {
            var semaphore = new AsyncSemaphore(0);
            var cancelationSource = CancelationSource.New();

            bool complete1 = false;
            semaphore.WaitAsync()
                .Then(() => complete1 = true)
                .Forget();
            Assert.AreEqual(0, semaphore.CurrentCount);
            Assert.False(complete1);

            bool complete2 = false;
            semaphore.TryWaitAsync(cancelationSource.Token)
                .Then(success =>
                {
                    Assert.True(success);
                    complete2 = true;
                })
                .Forget();
            Assert.AreEqual(0, semaphore.CurrentCount);
            Assert.False(complete2);

            semaphore.Release(2);
            Assert.AreEqual(0, semaphore.CurrentCount);
            TestHelper.ExecuteForegroundCallbacks();
            Assert.True(complete1);
            Assert.True(complete2);

            bool complete3 = false;
            semaphore.WaitAsync()
                .Then(() => complete3 = true)
                .Forget();
            Assert.AreEqual(0, semaphore.CurrentCount);
            Assert.False(complete3);

            semaphore.Release(1);
            Assert.AreEqual(0, semaphore.CurrentCount);
            TestHelper.ExecuteForegroundCallbacks();
            Assert.True(complete1);
            Assert.True(complete2);
            Assert.True(complete3);

            cancelationSource.Dispose();
        }

#if !UNITY_WEBGL
        [Test]
        public void AsyncSemaphore_TryWait_Canceled_DoesNotTakeSlot()
        {
            var semaphore = new AsyncSemaphore(0);
            var cancelationSource = CancelationSource.New();
            Assert.AreEqual(0, semaphore.CurrentCount);

            bool ready = false;
            var promise = Promise.Run(() =>
            {
                ready = true;
                return semaphore.TryWait(cancelationSource.Token);
            }, SynchronizationOption.Background, forceAsync: true);

            SpinWait.SpinUntil(() => ready);
            Thread.Sleep(10);

            cancelationSource.Cancel();
            Assert.False(promise.WaitWithTimeout(TimeSpan.FromSeconds(1)));

            semaphore.Release();
            Assert.AreEqual(1, semaphore.CurrentCount);

            cancelationSource.Dispose();
        }

        [Test]
        public void AsyncSemaphore_Release_WithWaiters_Releases1Waiter_Sync()
        {
            var semaphore = new AsyncSemaphore(0);
            var cancelationSource = CancelationSource.New();

            int readyCount = 0;
            int completeCount = 0;

            Promise.Run(() =>
            {
                Interlocked.Increment(ref readyCount);
                semaphore.Wait();
                Interlocked.Increment(ref completeCount);
            }, SynchronizationOption.Background, forceAsync: true)
                .Forget();
            Promise.Run(() =>
            {
                Interlocked.Increment(ref readyCount);
                Assert.True(semaphore.TryWait(cancelationSource.Token));
                Interlocked.Increment(ref completeCount);
            }, SynchronizationOption.Background, forceAsync: true)
                .Forget();

            SpinWait.SpinUntil(() => readyCount == 2);
            Thread.Sleep(10);
            Assert.AreEqual(0, completeCount);
            
            semaphore.Release();
            // It's a threading race condition, we can't know which one queued up first.
            if (!SpinWait.SpinUntil(() => completeCount == 1, TimeSpan.FromSeconds(1)))
            {
                throw new TimeoutException();
            }
            semaphore.Release();
            if (!SpinWait.SpinUntil(() => completeCount == 2, TimeSpan.FromSeconds(1)))
            {
                throw new TimeoutException();
            }

            cancelationSource.Dispose();
        }

        [Test]
        public void AsyncSemaphore_ReleaseMultiple_WithWaiters_ReleasesWaitersCount_Sync()
        {
            var semaphore = new AsyncSemaphore(0);
            var cancelationSource = CancelationSource.New();

            int readyCount = 0;
            int completeCount = 0;

            Promise.Run(() =>
            {
                Interlocked.Increment(ref readyCount);
                semaphore.Wait();
                Interlocked.Increment(ref completeCount);
            }, SynchronizationOption.Background, forceAsync: true)
                .Forget();
            Promise.Run(() =>
            {
                Interlocked.Increment(ref readyCount);
                Assert.True(semaphore.TryWait(cancelationSource.Token));
                Interlocked.Increment(ref completeCount);
            }, SynchronizationOption.Background, forceAsync: true)
                .Forget();
            Promise.Run(() =>
            {
                Interlocked.Increment(ref readyCount);
                semaphore.Wait();
                Interlocked.Increment(ref completeCount);
            }, SynchronizationOption.Background, forceAsync: true)
                .Forget();
            Promise.Run(() =>
            {
                Interlocked.Increment(ref readyCount);
                Assert.True(semaphore.TryWait(CancelationToken.None));
                Interlocked.Increment(ref completeCount);
            }, SynchronizationOption.Background, forceAsync: true)
                .Forget();

            SpinWait.SpinUntil(() => readyCount == 4);
            Thread.Sleep(10);
            Assert.AreEqual(0, semaphore.CurrentCount);
            Assert.AreEqual(0, completeCount);

            semaphore.Release(2);
            Assert.AreEqual(0, semaphore.CurrentCount);
            // It's a threading race condition, we can't know which ones queued up first.
            if (!SpinWait.SpinUntil(() => completeCount == 2, TimeSpan.FromSeconds(1)))
            {
                throw new TimeoutException();
            }
            semaphore.Release(1);
            Assert.AreEqual(0, semaphore.CurrentCount);
            if (!SpinWait.SpinUntil(() => completeCount == 3, TimeSpan.FromSeconds(1)))
            {
                throw new TimeoutException();
            }
            semaphore.Release(4);
            Assert.AreEqual(3, semaphore.CurrentCount);
            if (!SpinWait.SpinUntil(() => completeCount == 4, TimeSpan.FromSeconds(1)))
            {
                throw new TimeoutException();
            }

            cancelationSource.Dispose();
        }
#endif

        private static void AsyncSemaphore_ReleaseTooMany_Throws_Helper(int initialCount, int maxCount, int releaseCount)
        {
            var semaphore = maxCount > 0
                ? new AsyncSemaphore(initialCount, maxCount)
                : new AsyncSemaphore(initialCount);
            Assert.AreEqual(initialCount, semaphore.CurrentCount);
            if (releaseCount > 0)
            {
                Assert.Catch<System.Threading.SemaphoreFullException>(() => semaphore.Release(releaseCount));
            }
            else
            {
                Assert.Catch<System.Threading.SemaphoreFullException>(() => semaphore.Release());
            }
        }

        [Test]
        public void AsyncSemaphore_ReleaseTooMany_Throws()
        {
            AsyncSemaphore_ReleaseTooMany_Throws_Helper(1, 1, 1);
            AsyncSemaphore_ReleaseTooMany_Throws_Helper(1, 1, -1);
            AsyncSemaphore_ReleaseTooMany_Throws_Helper(1, 4, 10);
            AsyncSemaphore_ReleaseTooMany_Throws_Helper(4, 4, -1);
            AsyncSemaphore_ReleaseTooMany_Throws_Helper(4, 4, 1);
            AsyncSemaphore_ReleaseTooMany_Throws_Helper(4, 4, 10);
            AsyncSemaphore_ReleaseTooMany_Throws_Helper(4, 4, 10);
            AsyncSemaphore_ReleaseTooMany_Throws_Helper(1, -1, int.MaxValue);
        }

        [Test]
        public void AsyncSemaphore_ReleaseTooFew_Throws()
        {
            var semaphore = new AsyncSemaphore(0);
            Assert.Catch<System.ArgumentOutOfRangeException>(() => semaphore.Release(0));
            Assert.Catch<System.ArgumentOutOfRangeException>(() => semaphore.Release(-1));
        }

#if PROTO_PROMISE_TEST_GC_ENABLED
        [Test]
        public void AsyncSemaphore_AbandonedSemaphoreReported()
        {
            var currentRejectionHandler = Promise.Config.UncaughtRejectionHandler;
            AbandonedSemaphoreException abandonedSemaphoreException = null;
            Promise.Config.UncaughtRejectionHandler = ex =>
            {
                abandonedSemaphoreException = ex.Value as AbandonedSemaphoreException;
            };

            EnterAndAbandonSemaphore();
            TestHelper.GcCollectAndWaitForFinalizers();
            TestHelper.ExecuteForegroundCallbacksAndWaitForThreadsToComplete();
            Assert.IsNotNull(abandonedSemaphoreException);

            Promise.Config.UncaughtRejectionHandler = currentRejectionHandler;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void EnterAndAbandonSemaphore()
        {
            var semaphore = new AsyncSemaphore(0);
            semaphore.WaitAsync()
                .Forget();
        }
#endif // PROTO_PROMISE_TEST_GC_ENABLED
    }
}