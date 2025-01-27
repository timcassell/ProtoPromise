#if !UNITY_WEBGL

#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using NUnit.Framework;
using Proto.Promises;
using System.Threading;

namespace ProtoPromiseTests.Concurrency
{
    public class DeferredConcurrencyTests
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
        public void DeferredResolveMayNotBeCalledConcurrently_void()
        {
            int invokedCount = 0;
            var deferred = Promise.NewDeferred();
            deferred.Promise
                .Then(() => { Interlocked.Increment(ref invokedCount); })
                .Forget();
            int failedResolveCount = 0;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(() =>
            {
                try
                {
                    deferred.Resolve();
                }
                catch (InvalidOperationException)
                {
                    Interlocked.Increment(ref failedResolveCount);
                }
            });

            Assert.AreEqual(ThreadHelper.multiExecutionCount - 1, failedResolveCount); // Resolve should succeed once.
            Assert.AreEqual(1, invokedCount);
        }

        [Test]
        public void DeferredResolveMayNotBeCalledConcurrently_T()
        {
            int invokedCount = 0;
            var deferred = Promise.NewDeferred<int>();
            deferred.Promise
                .Then(v => { Interlocked.Increment(ref invokedCount); })
                .Forget();
            int failedResolveCount = 0;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(() =>
            {
                try
                {
                    deferred.Resolve(1);
                }
                catch (InvalidOperationException)
                {
                    Interlocked.Increment(ref failedResolveCount);
                }
            });

            Assert.AreEqual(ThreadHelper.multiExecutionCount - 1, failedResolveCount); // Resolve should succeed once.
            Assert.AreEqual(1, invokedCount);
        }

        [Test]
        public void DeferredRejectMayNotBeCalledConcurrently_void()
        {
            int invokedCount = 0;
            var deferred = Promise.NewDeferred();
            deferred.Promise
                .Catch(() => { Interlocked.Increment(ref invokedCount); })
                .Forget();
            int failedRejectCount = 0;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(() =>
            {
                try
                {
                    deferred.Reject("Reject");
                }
                catch (InvalidOperationException)
                {
                    Interlocked.Increment(ref failedRejectCount);
                }
            });

            Assert.AreEqual(ThreadHelper.multiExecutionCount - 1, failedRejectCount); // Reject should succeed once.
            Assert.AreEqual(1, invokedCount);
        }

        [Test]
        public void DeferredRejectMayNotBeCalledConcurrently_T()
        {
            int invokedCount = 0;
            var deferred = Promise.NewDeferred<int>();
            deferred.Promise
                .Catch(() => { Interlocked.Increment(ref invokedCount); })
                .Forget();
            int failedRejectCount = 0;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(() =>
            {
                try
                {
                    deferred.Reject("Reject");
                }
                catch (InvalidOperationException)
                {
                    Interlocked.Increment(ref failedRejectCount);
                }
            });

            Assert.AreEqual(ThreadHelper.multiExecutionCount - 1, failedRejectCount); // Reject should succeed once.
            Assert.AreEqual(1, invokedCount);
        }

        [Test]
        public void DeferredCancelMayNotBeCalledConcurrently_void()
        {
            int invokedCount = 0;
            var deferred = Promise.NewDeferred();
            deferred.Promise
                .CatchCancelation(() => { Interlocked.Increment(ref invokedCount); })
                .Forget();
            int failedCancelCount = 0;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(() =>
            {
                try
                {
                    deferred.Cancel();
                }
                catch (InvalidOperationException)
                {
                    Interlocked.Increment(ref failedCancelCount);
                }
            });

            Assert.AreEqual(ThreadHelper.multiExecutionCount - 1, failedCancelCount); // Cancel should succeed once.
            Assert.AreEqual(1, invokedCount);
        }

        [Test]
        public void DeferredCancelMayNotBeCalledConcurrently_T()
        {
            int invokedCount = 0;
            var deferred = Promise.NewDeferred<int>();
            deferred.Promise
                .CatchCancelation(() => { Interlocked.Increment(ref invokedCount); })
                .Forget();
            int failedCancelCount = 0;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(() =>
            {
                try
                {
                    deferred.Cancel();
                }
                catch (InvalidOperationException)
                {
                    Interlocked.Increment(ref failedCancelCount);
                }
            });

            Assert.AreEqual(ThreadHelper.multiExecutionCount - 1, failedCancelCount); // Cancel should succeed once.
            Assert.AreEqual(1, invokedCount);
        }

        [Test]
        public void DeferredMayBeResolvedAndPromiseAwaitedConcurrently_void()
        {
            var deferred = default(Promise.Deferred);
            var promise = default(Promise);

            int invokedCount = 0;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    invokedCount = 0;
                    deferred = Promise.NewDeferred();
                    promise = deferred.Promise;
                },
                // Teardown
                () =>
                {
                    Assert.AreEqual(1, invokedCount);
                },
                // Parallel Actions
                () => deferred.Resolve(),
                () => promise.Then(() => { Interlocked.Increment(ref invokedCount); }).Forget()
            );
        }

        [Test]
        public void DeferredMayBeResolvedAndPromiseAwaitedConcurrently_T()
        {
            var deferred = default(Promise<int>.Deferred);
            var promise = default(Promise<int>);

            int invokedCount = 0;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    invokedCount = 0;
                    deferred = Promise.NewDeferred<int>();
                    promise = deferred.Promise;
                },
                // Teardown
                () =>
                {
                    Assert.AreEqual(1, invokedCount);
                },
                // Parallel Actions
                () => deferred.Resolve(1),
                () => promise.Then(v => { Interlocked.Increment(ref invokedCount); }).Forget()
            );
        }

        [Test]
        public void DeferredMayBeRejectedAndPromiseAwaitedConcurrently_void()
        {
            var deferred = default(Promise.Deferred);
            var promise = default(Promise);

            int invokedCount = 0;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    invokedCount = 0;
                    deferred = Promise.NewDeferred();
                    promise = deferred.Promise;
                },
                // Teardown
                () =>
                {
                    Assert.AreEqual(1, invokedCount);
                },
                // Parallel Actions
                () => deferred.Reject(1),
                () => promise.Catch(() => { Interlocked.Increment(ref invokedCount); }).Forget()
            );
        }

        [Test]
        public void DeferredMayBeRejectedAndPromiseAwaitedConcurrently_T()
        {
            var deferred = default(Promise<int>.Deferred);
            var promise = default(Promise<int>);

            int invokedCount = 0;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    invokedCount = 0;
                    deferred = Promise.NewDeferred<int>();
                    promise = deferred.Promise;
                },
                // Teardown
                () =>
                {
                    Assert.AreEqual(1, invokedCount);
                },
                // Parallel Actions
                () => deferred.Reject(1),
                () => promise.Catch(() => { Interlocked.Increment(ref invokedCount); }).Forget()
            );
        }

        [Test]
        public void DeferredMayBeCanceledAndPromiseAwaitedConcurrently_void()
        {
            var deferred = default(Promise.Deferred);
            var promise = default(Promise);

            int invokedCount = 0;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    invokedCount = 0;
                    deferred = Promise.NewDeferred();
                    promise = deferred.Promise;
                },
                // Teardown
                () =>
                {
                    Assert.AreEqual(1, invokedCount);
                },
                // Parallel Actions
                () => deferred.Cancel(),
                () => promise.CatchCancelation(() => { Interlocked.Increment(ref invokedCount); }).Forget()
            );
        }

        [Test]
        public void DeferredMayBeCanceledAndPromiseAwaitedConcurrently_T()
        {
            var deferred = default(Promise<int>.Deferred);
            var promise = default(Promise<int>);

            int invokedCount = 0;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    invokedCount = 0;
                    deferred = Promise.NewDeferred<int>();
                    promise = deferred.Promise;
                },
                // Teardown
                () =>
                {
                    Assert.AreEqual(1, invokedCount);
                },
                // Parallel Actions
                () => deferred.Cancel(),
                () => promise.CatchCancelation(() => { Interlocked.Increment(ref invokedCount); }).Forget()
            );
        }
    }
}

#endif // !UNITY_WEBGL