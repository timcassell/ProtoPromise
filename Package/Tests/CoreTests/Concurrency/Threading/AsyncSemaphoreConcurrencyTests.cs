#if !UNITY_WEBGL

using NUnit.Framework;
using Proto.Promises;
using Proto.Promises.Threading;
using System;
using System.Threading;

namespace ProtoPromiseTests.Concurrency.Threading
{
    public class AsyncSemaphoreConcurrencyTests
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
        public void AsyncSemaphore_EnteredConcurrenctly_OnlyAllowsCountEntries(
            [Values(1, 2, 5)] int count,
            [Values] bool maxIsSameAsCount,
            [Values] bool delayCancel)
        {
            var semaphore = maxIsSameAsCount ? new AsyncSemaphore(count, count) : new AsyncSemaphore(count);
            var cancelationSource = default(CancelationSource);
            int enteredCount = 0;
            int exitedCount = 0;
            int expectedInvokes = 4;
            Action<bool> syncAction = observeCancelation =>
            {
                bool didWait;
                if (observeCancelation)
                {
                    didWait = semaphore.TryWait(cancelationSource.Token);
                }
                else
                {
                    semaphore.Wait();
                    didWait = true;
                }
                if (didWait)
                {
                    Assert.LessOrEqual(Interlocked.Increment(ref enteredCount), count);
                    Thread.Sleep(10);
                    Assert.LessOrEqual(Interlocked.Decrement(ref enteredCount), count - 1);
                    semaphore.Release();
                }
                Interlocked.Increment(ref exitedCount);
            };
            Action<bool> asyncAction = observeCancelation =>
            {
                Promise<bool> promise;
                if (observeCancelation)
                {
                    promise = semaphore.TryWaitAsync(cancelationSource.Token);
                }
                else
                {
                    promise = semaphore.WaitAsync()
                        .Then(() => true);
                }
                promise
                    .Then(didWait =>
                    {
                        if (didWait)
                        {
                            Assert.LessOrEqual(Interlocked.Increment(ref enteredCount), count);
                            Thread.Sleep(10);
                            Assert.LessOrEqual(Interlocked.Decrement(ref enteredCount), count - 1);
                            semaphore.Release();
                        }
                        Interlocked.Increment(ref exitedCount);
                    })
                    .Forget();
            };

            new ThreadHelper().ExecuteParallelActionsWithOffsets(false,
                // setup
                () =>
                {
                    exitedCount = 0;
                    cancelationSource = CancelationSource.New();
                },
                // teardown
                () =>
                {
                    while (exitedCount < expectedInvokes)
                    {
                        TestHelper.ExecuteForegroundCallbacksAndWaitForThreadsToComplete();
                    }
                    Assert.AreEqual(0, enteredCount);
                    cancelationSource.Dispose();
                },
                // parallel actions
                () => syncAction(false),
                () => syncAction(true),
                () => asyncAction(false),
                () => asyncAction(true),
                () =>
                {
                    if (delayCancel)
                    {
                        Thread.Sleep(10);
                    }
                    cancelationSource.Cancel();
                });
        }
    }
}

#endif // !UNITY_WEBGL