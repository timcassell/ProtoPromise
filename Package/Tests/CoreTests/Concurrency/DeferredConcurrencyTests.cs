#if !UNITY_WEBGL

#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using NUnit.Framework;
using Proto.Promises;
using System.Threading;

#pragma warning disable 0618 // Type or member is obsolete

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
            int failedTryResolveCount = 0;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(() =>
            {
                if (!deferred.TryResolve())
                {
                    Interlocked.Increment(ref failedTryResolveCount);
                }
            });

            Assert.AreEqual(ThreadHelper.multiExecutionCount - 1, failedTryResolveCount); // TryResolve should succeed once.
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
            int failedTryResolveCount = 0;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(() =>
            {
                if (!deferred.TryResolve(1))
                {
                    Interlocked.Increment(ref failedTryResolveCount);
                }
            });

            Assert.AreEqual(ThreadHelper.multiExecutionCount - 1, failedTryResolveCount); // TryResolve should succeed once.
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
            int failedTryResolveCount = 0;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(() =>
            {
                if (!deferred.TryReject("Reject"))
                {
                    Interlocked.Increment(ref failedTryResolveCount);
                }
            });

            Assert.AreEqual(ThreadHelper.multiExecutionCount - 1, failedTryResolveCount); // TryResolve should succeed once.
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
            int failedTryResolveCount = 0;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(() =>
            {
                if (!deferred.TryReject("Reject"))
                {
                    Interlocked.Increment(ref failedTryResolveCount);
                }
            });

            Assert.AreEqual(ThreadHelper.multiExecutionCount - 1, failedTryResolveCount); // TryResolve should succeed once.
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
            int failedTryResolveCount = 0;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(() =>
            {
                if (!deferred.TryCancel())
                {
                    Interlocked.Increment(ref failedTryResolveCount);
                }
            });

            Assert.AreEqual(ThreadHelper.multiExecutionCount - 1, failedTryResolveCount); // TryResolve should succeed once.
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
            int failedTryResolveCount = 0;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(() =>
            {
                if (!deferred.TryCancel())
                {
                    Interlocked.Increment(ref failedTryResolveCount);
                }
            });

            Assert.AreEqual(ThreadHelper.multiExecutionCount - 1, failedTryResolveCount); // TryResolve should succeed once.
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