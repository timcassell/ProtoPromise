#if !UNITY_WEBGL

#if !PROTO_PROMISE_PROGRESS_DISABLE
#define PROMISE_PROGRESS
#else
#undef PROMISE_PROGRESS
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

#if PROMISE_PROGRESS
        [Test]
        public void DeferredReportProgressMayBeCalledConcurrently_void(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType,
            [Values] bool withCancelationToken)
        {
            var cancelationSource = CancelationSource.New();
            var deferred = default(Promise.Deferred);

            var progressHelper = default(ProgressHelper);

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(true,
                // Setup
                () =>
                {
                    deferred = withCancelationToken
                        ? Promise.NewDeferred(cancelationSource.Token)
                        : Promise.NewDeferred();
                    progressHelper = new ProgressHelper(progressType, synchronizationType);

                    deferred.Promise
                        .SubscribeProgressAndAssert(progressHelper, 0f)
                        .Forget();

                    progressHelper.MaybeEnterLock();
                    progressHelper.PrepareForInvoke();
                },
                // Teardown
                () =>
                {
                    // Each progress is reported concurrently, so we can't know which stuck.
                    // Just check to make sure any of them stuck, so it should be >= min and <= max.
                    float progress1 = progressHelper.GetCurrentProgress(true, true);
                    progressHelper.MaybeExitLock();
                    Assert.Greater(progress1, 0.2f - TestHelper.progressEpsilon);
                    Assert.LessOrEqual(progress1, 0.4f);

                    deferred.Resolve();
                },
                // Parallel Actions
                () => deferred.ReportProgress(0.2f),
                () => deferred.ReportProgress(0.3f),
                () => deferred.ReportProgress(0.4f)
            );

            cancelationSource.Dispose();
        }

        [Test]
        public void DeferredReportProgressMayBeCalledConcurrently_T(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType,
            [Values] bool withCancelationToken)
        {
            var cancelationSource = CancelationSource.New();
            var deferred = default(Promise<int>.Deferred);

            var progressHelper = default(ProgressHelper);

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(true,
                // Setup
                () =>
                {
                    deferred = withCancelationToken
                        ? Promise.NewDeferred<int>(cancelationSource.Token)
                        : Promise.NewDeferred<int>();
                    progressHelper = new ProgressHelper(progressType, synchronizationType);

                    deferred.Promise
                        .SubscribeProgressAndAssert(progressHelper, 0f)
                        .Forget();

                    progressHelper.MaybeEnterLock();
                    progressHelper.PrepareForInvoke();
                },
                // Teardown
                () =>
                {
                    // Each progress is reported concurrently, so we can't know which stuck.
                    // Just check to make sure any of them stuck, so it should be >= min and <= max.
                    float progress1 = progressHelper.GetCurrentProgress(true, true);
                    progressHelper.MaybeExitLock();
                    Assert.Greater(progress1, 0.2f - TestHelper.progressEpsilon);
                    Assert.LessOrEqual(progress1, 0.4f);

                    deferred.Resolve(1);
                },
                // Parallel Actions
                () => deferred.ReportProgress(0.2f),
                () => deferred.ReportProgress(0.3f),
                () => deferred.ReportProgress(0.4f)
            );
         
            cancelationSource.Dispose();
        }
#endif

        [Test]
        public void DeferredResolveMayNotBeCalledConcurrently_void0()
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
        public void DeferredResolveMayNotBeCalledConcurrently_void1()
        {
            int invokedCount = 0;
            var cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred(cancelationSource.Token);
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

            cancelationSource.Dispose();
        }

        [Test]
        public void DeferredResolveMayNotBeCalledConcurrently_T0()
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
        public void DeferredResolveMayNotBeCalledConcurrently_T1()
        {
            int invokedCount = 0;
            var cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred<int>(cancelationSource.Token);
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

            cancelationSource.Dispose();
        }

        [Test]
        public void DeferredRejectMayNotBeCalledConcurrently_void0()
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
        public void DeferredRejectMayNotBeCalledConcurrently_void1()
        {
            int invokedCount = 0;
            var cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred(cancelationSource.Token);
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

            cancelationSource.Dispose();
        }

        [Test]
        public void DeferredRejectMayNotBeCalledConcurrently_T0()
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
        public void DeferredRejectMayNotBeCalledConcurrently_T1()
        {
            int invokedCount = 0;
            var cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred<int>(cancelationSource.Token);
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

            cancelationSource.Dispose();
        }

        [Test]
        public void DeferredCancelMayNotBeCalledConcurrently_void0()
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
        public void DeferredCancelMayNotBeCalledConcurrently_void1()
        {
            int invokedCount = 0;
            var cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred(cancelationSource.Token);
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

            cancelationSource.Dispose();
        }

        [Test]
        public void DeferredCancelMayNotBeCalledConcurrently_T0()
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
        public void DeferredCancelMayNotBeCalledConcurrently_T1()
        {
            int invokedCount = 0;
            var cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred<int>(cancelationSource.Token);
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

            cancelationSource.Dispose();
        }

#if PROMISE_PROGRESS
        [Test]
        public void DeferredMayReportProgressAndPromiseMaySubscribeProgressConcurrently_void(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType,
            [Values] bool withCancelationToken)
        {
            float expected = 0.1f;
            var cancelationSource = CancelationSource.New();
            var deferred = default(Promise.Deferred);
            var promise = default(Promise);

            var progressHelper = default(ProgressHelper);

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    deferred = withCancelationToken
                        ? Promise.NewDeferred(cancelationSource.Token)
                        : Promise.NewDeferred();
                    promise = deferred.Promise;
                    progressHelper = new ProgressHelper(progressType, synchronizationType);
                    progressHelper.MaybeEnterLock();
                },
                // Teardown
                () =>
                {
                    float progress = progressHelper.GetCurrentProgress(true, true);
                    progressHelper.MaybeExitLock();
                    // Race condition could report 0 instead of expected from background threads.
                    if (progress < expected * 0.5f)
                    {
                        Assert.AreEqual(0f, progress, TestHelper.progressEpsilon);
                    }
                    else
                    {
                        Assert.AreEqual(expected, progress, TestHelper.progressEpsilon);
                    }
                    deferred.Resolve();
                },
                // Parallel Actions
                () => deferred.ReportProgress(expected),
                () => promise.SubscribeProgress(progressHelper).Forget()
            );

            cancelationSource.Dispose();
        }

        [Test]
        public void DeferredMayReportProgressAndPromiseMaySubscribeProgressConcurrently_T0(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType,
            [Values] bool withCancelationToken)
        {
            float expected = 0.1f;
            var cancelationSource = CancelationSource.New();
            var deferred = default(Promise<int>.Deferred);
            var promise = default(Promise);

            var progressHelper = default(ProgressHelper);

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    deferred = withCancelationToken
                        ? Promise.NewDeferred<int>(cancelationSource.Token)
                        : Promise.NewDeferred<int>();
                    promise = deferred.Promise;
                    progressHelper = new ProgressHelper(progressType, synchronizationType);
                    progressHelper.MaybeEnterLock();
                },
                // Teardown
                () =>
                {
                    float progress = progressHelper.GetCurrentProgress(true, true);
                    progressHelper.MaybeExitLock();
                    // Race condition could report 0 instead of expected from background threads.
                    if (progress < expected * 0.5f)
                    {
                        Assert.AreEqual(0f, progress, TestHelper.progressEpsilon);
                    }
                    else
                    {
                        Assert.AreEqual(expected, progress, TestHelper.progressEpsilon);
                    }
                    deferred.Resolve(1);
                },
                // Parallel Actions
                () => deferred.ReportProgress(expected),
                () => promise.SubscribeProgress(progressHelper).Forget()
            );

            cancelationSource.Dispose();
        }
#endif

        [Test]
        public void DeferredMayBeResolvedAndPromiseAwaitedConcurrently_void0()
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
        public void DeferredMayBeResolvedAndPromiseAwaitedConcurrently_void1()
        {
            var cancelationSource = default(CancelationSource);
            var deferred = default(Promise.Deferred);
            var promise = default(Promise);

            int invokedCount = 0;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    invokedCount = 0;
                    cancelationSource = CancelationSource.New();
                    deferred = Promise.NewDeferred(cancelationSource.Token);
                    promise = deferred.Promise;
                },
                // Teardown
                () =>
                {
                    cancelationSource.Dispose();
                    Assert.AreEqual(1, invokedCount);
                },
                // Parallel Actions
                () => deferred.Resolve(),
                () => promise.Then(() => { Interlocked.Increment(ref invokedCount); }).Forget()
            );
        }

        [Test]
        public void DeferredMayBeResolvedAndPromiseAwaitedConcurrently_T0()
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
        public void DeferredMayBeResolvedAndPromiseAwaitedConcurrently_T1()
        {
            var cancelationSource = default(CancelationSource);
            var deferred = default(Promise<int>.Deferred);
            var promise = default(Promise<int>);

            int invokedCount = 0;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    invokedCount = 0;
                    cancelationSource = CancelationSource.New();
                    deferred = Promise.NewDeferred<int>(cancelationSource.Token);
                    promise = deferred.Promise;
                },
                // Teardown
                () =>
                {
                    cancelationSource.Dispose();
                    Assert.AreEqual(1, invokedCount);
                },
                // Parallel Actions
                () => deferred.Resolve(1),
                () => promise.Then(v => { Interlocked.Increment(ref invokedCount); }).Forget()
            );
        }

        [Test]
        public void DeferredMayBeRejectedAndPromiseAwaitedConcurrently_void0()
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
        public void DeferredMayBeRejectedAndPromiseAwaitedConcurrently_void1()
        {
            var cancelationSource = default(CancelationSource);
            var deferred = default(Promise.Deferred);
            var promise = default(Promise);

            int invokedCount = 0;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    invokedCount = 0;
                    cancelationSource = CancelationSource.New();
                    deferred = Promise.NewDeferred(cancelationSource.Token);
                    promise = deferred.Promise;
                },
                // Teardown
                () =>
                {
                    cancelationSource.Dispose();
                    Assert.AreEqual(1, invokedCount);
                },
                // Parallel Actions
                () => deferred.Reject(1),
                () => promise.Catch(() => { Interlocked.Increment(ref invokedCount); }).Forget()
            );
        }

        [Test]
        public void DeferredMayBeRejectedAndPromiseAwaitedConcurrently_T0()
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
        public void DeferredMayBeRejectedAndPromiseAwaitedConcurrently_T1()
        {
            var cancelationSource = default(CancelationSource);
            var deferred = default(Promise<int>.Deferred);
            var promise = default(Promise<int>);

            int invokedCount = 0;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    invokedCount = 0;
                    cancelationSource = CancelationSource.New();
                    deferred = Promise.NewDeferred<int>(cancelationSource.Token);
                    promise = deferred.Promise;
                },
                // Teardown
                () =>
                {
                    cancelationSource.Dispose();
                    Assert.AreEqual(1, invokedCount);
                },
                // Parallel Actions
                () => deferred.Reject(1),
                () => promise.Catch(() => { Interlocked.Increment(ref invokedCount); }).Forget()
            );
        }

        [Test]
        public void DeferredMayBeCanceledAndPromiseAwaitedConcurrently_void0()
        {
            var cancelationSource = default(CancelationSource);
            var deferred = default(Promise.Deferred);
            var promise = default(Promise);

            int invokedCount = 0;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    invokedCount = 0;
                    cancelationSource = CancelationSource.New();
                    deferred = Promise.NewDeferred(cancelationSource.Token);
                    promise = deferred.Promise;
                },
                // Teardown
                () =>
                {
                    cancelationSource.Dispose();
                    Assert.AreEqual(1, invokedCount);
                },
                // Parallel Actions
                () => cancelationSource.Cancel(),
                () => promise.CatchCancelation(() => { Interlocked.Increment(ref invokedCount); }).Forget()
            );
        }

        [Test]
        public void DeferredMayBeCanceledAndPromiseAwaitedConcurrently_void1()
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
        public void DeferredMayBeCanceledAndPromiseAwaitedConcurrently_T0()
        {
            var cancelationSource = default(CancelationSource);
            var deferred = default(Promise<int>.Deferred);
            var promise = default(Promise<int>);

            int invokedCount = 0;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    invokedCount = 0;
                    cancelationSource = CancelationSource.New();
                    deferred = Promise.NewDeferred<int>(cancelationSource.Token);
                    promise = deferred.Promise;
                },
                // Teardown
                () =>
                {
                    cancelationSource.Dispose();
                    Assert.AreEqual(1, invokedCount);
                },
                // Parallel Actions
                () => cancelationSource.Cancel(),
                () => promise.CatchCancelation(() => { Interlocked.Increment(ref invokedCount); }).Forget()
            );
        }

        [Test]
        public void DeferredMayBeCanceledAndPromiseAwaitedConcurrently_T2()
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