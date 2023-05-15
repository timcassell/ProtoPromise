#if !UNITY_WEBGL

#if !PROTO_PROMISE_PROGRESS_DISABLE
#define PROMISE_PROGRESS
#else
#undef PROMISE_PROGRESS
#endif

using NUnit.Framework;
using Proto.Promises;
using System;
using System.Threading;

namespace ProtoPromiseTests.Concurrency
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
            [Values] bool waitForDeferred)
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
                    lazy = new AsyncLazy<int>(() =>
                    {
                        Assert.AreEqual(1, Interlocked.Increment(ref invokedCount));
                        return waitForDeferred
                            ? deferred.Promise
                            : Promise.Resolved(expectedResult);
                    });
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
                parallelAction
            );
        }

        [Test]
        public void AsyncLazy_AccessedConcurrently_ResultIsExpected()
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
                    lazy = new AsyncLazy<int>(() =>
                    {
                        Assert.AreEqual(1, Interlocked.Increment(ref invokedCount));
                        return deferred.Promise;
                    });
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
                parallelAction
            );
        }

#if PROMISE_PROGRESS
        private static ProgressHelper GetProgressHelper()
        {
            return new ProgressHelper(ProgressType.Interface, SynchronizationType.Synchronous, onProgress: p =>
            {
                Assert.GreaterOrEqual(p, 0);
                Assert.LessOrEqual(p, 1);
            });
        }

        [Test]
        public void AsyncLazy_AccessedConcurrently_ProgressIsReportedProperly()
        {
            ProgressHelper progressHelper1 = GetProgressHelper();
            ProgressHelper progressHelper2 = GetProgressHelper();
            ProgressHelper progressHelper3 = GetProgressHelper();
            ProgressHelper progressHelper4 = GetProgressHelper();

            int expectedResult = 42;
            int invokedCount = 0;
            var deferred = default(Promise<int>.Deferred);
            AsyncLazy<int> lazy = null;

            new ThreadHelper().ExecuteParallelActionsWithOffsets(false,
                // setup
                () =>
                {
                    invokedCount = 0;
                    deferred = Promise.NewDeferred<int>();
                    lazy = new AsyncLazy<int>(() =>
                    {
                        Assert.AreEqual(1, Interlocked.Increment(ref invokedCount));
                        return deferred.Promise;
                    });
                },
                // teardown
                () =>
                {
                    TestHelper.ExecuteForegroundCallbacksAndWaitForThreadsToComplete();
                    progressHelper1.AssertCurrentProgress(0f);
                    progressHelper2.AssertCurrentProgress(0f);
                    progressHelper3.AssertCurrentProgress(0f);
                    progressHelper4.AssertCurrentProgress(0f);
                    deferred.ReportProgress(0.5f);
                    progressHelper1.AssertCurrentProgress(0.5f);
                    progressHelper2.AssertCurrentProgress(0.5f);
                    progressHelper3.AssertCurrentProgress(0.5f);
                    progressHelper4.AssertCurrentProgress(0.5f);
                    deferred.Resolve(expectedResult);
                    progressHelper1.AssertCurrentProgress(1f);
                    progressHelper2.AssertCurrentProgress(1f);
                    progressHelper3.AssertCurrentProgress(1f);
                    progressHelper4.AssertCurrentProgress(1f);
                    Assert.AreEqual(1, invokedCount);
                },
                // parallel actions
                () =>
                {
                    lazy.Promise
                        .SubscribeProgress(progressHelper1)
                        .Then(v => Assert.AreEqual(expectedResult, v))
                        .Forget();
                },
                () =>
                {
                    lazy.Promise
                        .SubscribeProgress(progressHelper2)
                        .Then(v => Assert.AreEqual(expectedResult, v))
                        .Forget();
                },
                () =>
                {
                    lazy.Promise
                        .SubscribeProgress(progressHelper3)
                        .Then(v => Assert.AreEqual(expectedResult, v))
                        .Forget();
                },
                () =>
                {
                    lazy.Promise
                        .SubscribeProgress(progressHelper4)
                        .Then(v => Assert.AreEqual(expectedResult, v))
                        .Forget();
                }
            );
        }
#endif
    }
}

#endif // !UNITY_WEBGL