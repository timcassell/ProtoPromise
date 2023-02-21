using NUnit.Framework;
using Proto.Promises;
using System;
using System.Threading;

namespace ProtoPromiseTests.Threading
{
    public class AsyncLazyConcurrencyTests
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
        public void AsyncLazy_AccessedConcurrently_DelegateIsInvokedOnlyOnce(
            [Values] bool waitForDeferred,
            [Values] SynchronizationType synchronizationType)
        {
            int expectedResult = 42;
            int invokedCount = 0;
            var deferred = default(Promise<int>.Deferred);
            AsyncLazy<int> lazy = null;
            Action parallelAction = () =>
            {
                lazy.Promise
                    .Then(v => Assert.AreEqual(expectedResult, v))
                    .Forget();
            };

            new ThreadHelper().ExecuteParallelActionsWithOffsets(true, // Repeat the parallel action for as many available hardware threads.
                // setup
                () =>
                {
                    invokedCount = 0;
                    if (waitForDeferred)
                    {
                        deferred = Promise.NewDeferred<int>();
                    }
                    Func<Promise<int>> del = () =>
                    {
                        Assert.AreEqual(1, Interlocked.Increment(ref invokedCount));
                        return waitForDeferred
                            ? deferred.Promise
                            : Promise.Resolved(expectedResult);
                    };
                    lazy = synchronizationType == SynchronizationType.Explicit
                        ? new AsyncLazy<int>(del, TestHelper._foregroundContext)
                        : new AsyncLazy<int>(del, (SynchronizationOption) synchronizationType);
                },
                // teardown
                () =>
                {
                    TestHelper.ExecuteForegroundCallbacksAndWaitForThreadsToComplete();
                    Assert.AreEqual(1, invokedCount);
                    deferred.TryResolve(expectedResult);
                    Assert.AreEqual(1, invokedCount);
                },
                // parallel actions, repeated to generate offsets
                parallelAction,
                parallelAction,
                parallelAction,
                parallelAction);
        }

        [Test]
        public void AsyncLazy_AccessedConcurrently_ResultIsExpected(
            [Values] SynchronizationType synchronizationType)
        {
            int expectedResult = 42;
            int invokedCount = 0;
            var deferred = default(Promise<int>.Deferred);
            AsyncLazy<int> lazy = null;
            Action parallelAction = () =>
            {
                lazy.Promise
                    .Then(v => Assert.AreEqual(expectedResult, v))
                    .Forget();
            };

            new ThreadHelper().ExecuteParallelActionsWithOffsets(true, // Repeat the parallel actions for as many available hardware threads.
                // setup
                () =>
                {
                    invokedCount = 0;
                    deferred = Promise.NewDeferred<int>();
                    Func<Promise<int>> del = () =>
                    {
                        Assert.AreEqual(1, Interlocked.Increment(ref invokedCount));
                        return deferred.Promise;
                    };
                    lazy = synchronizationType == SynchronizationType.Explicit
                        ? new AsyncLazy<int>(del, TestHelper._foregroundContext)
                        : new AsyncLazy<int>(del, (SynchronizationOption) synchronizationType);
                },
                // teardown
                () =>
                {
                    TestHelper.ExecuteForegroundCallbacksAndWaitForThreadsToComplete();
                    Assert.AreEqual(1, invokedCount);
                },
                // parallel actions
                () => deferred.TryResolve(expectedResult),
                // repeated to generate offsets
                parallelAction,
                parallelAction,
                parallelAction,
                parallelAction);
        }
    }
}