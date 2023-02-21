using NUnit.Framework;
using Proto.Promises;
using Proto.Promises.Threading;
using System;
using System.Threading;

namespace ProtoPromiseTests.Threading
{
#if UNITY_2021_2_OR_NEWER || NETSTANDARD2_1_OR_GREATER || NETCOREAPP
    public class AsyncLockConcurrencyTests
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
        public void AsyncLock_EnteredConcurrenctly_OnlyAllowsSingleLocker()
        {
            var mutex = new AsyncLock();
            int enteredCount = 0;
            int exitedCount = 0;
            int expectedInvokes = ThreadHelper.GetExpandCount(4);
            Action syncLockAction = () =>
            {
                using (mutex.Lock())
                {
                    Assert.AreEqual(1, Interlocked.Increment(ref enteredCount));
                    Thread.Sleep(10);
                    Assert.AreEqual(0, Interlocked.Decrement(ref enteredCount));
                }
                Interlocked.Increment(ref exitedCount);
            };
            Action asyncLockAction = () =>
            {
                mutex.LockAsync()
                    .Then(key =>
                    {
                        Assert.AreEqual(1, Interlocked.Increment(ref enteredCount));
                        Thread.Sleep(10);
                        Assert.AreEqual(0, Interlocked.Decrement(ref enteredCount));
                        key.Dispose();
                        Interlocked.Increment(ref exitedCount);
                    })
                    .Forget();
            };

            new ThreadHelper().ExecuteParallelActionsWithOffsets(true, // Repeat the parallel actions for as many available hardware threads.
                // setup
                () =>
                {
                    exitedCount = 0;
                },
                // teardown
                () =>
                {
                    while (exitedCount < expectedInvokes)
                    {
                        TestHelper.ExecuteForegroundCallbacksAndWaitForThreadsToComplete();
                    }
                },
                // parallel actions, repeated to generate offsets
                syncLockAction,
                syncLockAction,
                asyncLockAction,
                asyncLockAction);
        }
    }
#endif // UNITY_2021_2_OR_NEWER || NETSTANDARD2_1_OR_GREATER || NETCOREAPP
}