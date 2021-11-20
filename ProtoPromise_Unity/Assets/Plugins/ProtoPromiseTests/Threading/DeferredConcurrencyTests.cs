﻿#if CSHARP_7_3_OR_NEWER && !UNITY_WEBGL

#if !PROTO_PROMISE_PROGRESS_DISABLE
#define PROMISE_PROGRESS
#else
#undef PROMISE_PROGRESS
#endif

using NUnit.Framework;
using System.Threading;

namespace Proto.Promises.Tests.Threading
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
            [Values] ProgressType progressType1,
            [Values] SynchronizationType synchronizationType1,
            [Values] ProgressType progressType2,
            [Values] SynchronizationType synchronizationType2,
            [Values] bool withCancelationToken)
        {
            var cancelationSource = CancelationSource.New();
            var deferred = default(Promise.Deferred);

            var progressHelper1 = default(ProgressHelper);
            var progressHelper2 = default(ProgressHelper);

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(true,
                // Setup
                () =>
                {
                    deferred = withCancelationToken
                        ? Promise.NewDeferred(cancelationSource.Token)
                        : Promise.NewDeferred();
                    progressHelper1 = new ProgressHelper(progressType1, synchronizationType1);
                    progressHelper2 = new ProgressHelper(progressType2, synchronizationType2);

                    deferred.Promise
                        .SubscribeProgress(progressHelper1)
                        .SubscribeProgress(progressHelper2)
                        .Forget();
                    progressHelper1.AssertCurrentProgress(0f);
                    progressHelper2.AssertCurrentProgress(0f);

                    if (synchronizationType1 != SynchronizationType.Synchronous)
                    {
                        Monitor.Enter(progressHelper1._locker);
                    }
                    if (synchronizationType2 != SynchronizationType.Synchronous)
                    {
                        Monitor.Enter(progressHelper2._locker);
                    }
                    progressHelper1.Reset();
                    progressHelper2.Reset();
                },
                // Teardown
                () =>
                {
                    // Each progress is reported concurrently, so we can't know which stuck.
                    // Just check to make sure any of them stuck, so it should be >= min and <= max.
                    float progress1 = progressHelper1.GetCurrentProgress(true, true);
                    float progress2 = progressHelper2.GetCurrentProgress(true, false);
                    if (synchronizationType1 != SynchronizationType.Synchronous)
                    {
                        Monitor.Exit(progressHelper1._locker);
                    }
                    if (synchronizationType2 != SynchronizationType.Synchronous)
                    {
                        Monitor.Exit(progressHelper2._locker);
                    }
                    Assert.Greater(progress1, 0.2f - TestHelper.progressEpsilon);
                    Assert.LessOrEqual(progress1, 0.4f);
                    Assert.Greater(progress2, 0.2f - TestHelper.progressEpsilon);
                    Assert.LessOrEqual(progress2, 0.4f);

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
            [Values] ProgressType progressType1,
            [Values] SynchronizationType synchronizationType1,
            [Values] ProgressType progressType2,
            [Values] SynchronizationType synchronizationType2,
            [Values] bool withCancelationToken)
        {
            var cancelationSource = CancelationSource.New();
            var deferred = default(Promise<int>.Deferred);

            var progressHelper1 = default(ProgressHelper);
            var progressHelper2 = default(ProgressHelper);

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(true,
                // Setup
                () =>
                {
                    deferred = withCancelationToken
                        ? Promise.NewDeferred<int>(cancelationSource.Token)
                        : Promise.NewDeferred<int>();
                    progressHelper1 = new ProgressHelper(progressType1, synchronizationType1);
                    progressHelper2 = new ProgressHelper(progressType2, synchronizationType2);

                    deferred.Promise
                        .SubscribeProgress(progressHelper1)
                        .SubscribeProgress(progressHelper2)
                        .Forget();
                    progressHelper1.AssertCurrentProgress(0f);
                    progressHelper2.AssertCurrentProgress(0f);

                    if (synchronizationType1 != SynchronizationType.Synchronous)
                    {
                        Monitor.Enter(progressHelper1._locker);
                    }
                    if (synchronizationType2 != SynchronizationType.Synchronous)
                    {
                        Monitor.Enter(progressHelper2._locker);
                    }
                    progressHelper1.Reset();
                    progressHelper2.Reset();
                },
                // Teardown
                () =>
                {
                    // Each progress is reported concurrently, so we can't know which stuck.
                    // Just check to make sure any of them stuck, so it should be >= min and <= max.
                    float progress1 = progressHelper1.GetCurrentProgress(true, true);
                    float progress2 = progressHelper2.GetCurrentProgress(true, false);
                    if (synchronizationType1 != SynchronizationType.Synchronous)
                    {
                        Monitor.Exit(progressHelper1._locker);
                    }
                    if (synchronizationType2 != SynchronizationType.Synchronous)
                    {
                        Monitor.Exit(progressHelper2._locker);
                    }
                    Assert.Greater(progress1, 0.2f - TestHelper.progressEpsilon);
                    Assert.LessOrEqual(progress1, 0.4f);
                    Assert.Greater(progress2, 0.2f - TestHelper.progressEpsilon);
                    Assert.LessOrEqual(progress2, 0.4f);

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
                .CatchCancelation(_ => { Interlocked.Increment(ref invokedCount); })
                .Forget();
            int failedTryResolveCount = 0;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(() =>
            {
                if (!deferred.TryCancel("Cancel"))
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
                .CatchCancelation(_ => { Interlocked.Increment(ref invokedCount); })
                .Forget();
            int failedTryResolveCount = 0;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(() =>
            {
                if (!deferred.TryCancel("Cancel"))
                {
                    Interlocked.Increment(ref failedTryResolveCount);
                }
            });

            Assert.AreEqual(ThreadHelper.multiExecutionCount - 1, failedTryResolveCount); // TryResolve should succeed once.
            Assert.AreEqual(1, invokedCount);

            cancelationSource.Dispose();
        }

        [Test]
        public void DeferredCancelMayNotBeCalledConcurrently_void2()
        {
            int invokedCount = 0;
            var deferred = Promise.NewDeferred();
            deferred.Promise
                .CatchCancelation(_ => { Interlocked.Increment(ref invokedCount); })
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
        public void DeferredCancelMayNotBeCalledConcurrently_void3()
        {
            int invokedCount = 0;
            var cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred(cancelationSource.Token);
            deferred.Promise
                .CatchCancelation(_ => { Interlocked.Increment(ref invokedCount); })
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
                .CatchCancelation(_ => { Interlocked.Increment(ref invokedCount); })
                .Forget();
            int failedTryResolveCount = 0;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(() =>
            {
                if (!deferred.TryCancel("Cancel"))
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
                .CatchCancelation(_ => { Interlocked.Increment(ref invokedCount); })
                .Forget();
            int failedTryResolveCount = 0;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(() =>
            {
                if (!deferred.TryCancel("Cancel"))
                {
                    Interlocked.Increment(ref failedTryResolveCount);
                }
            });

            Assert.AreEqual(ThreadHelper.multiExecutionCount - 1, failedTryResolveCount); // TryResolve should succeed once.
            Assert.AreEqual(1, invokedCount);

            cancelationSource.Dispose();
        }

        [Test]
        public void DeferredCancelMayNotBeCalledConcurrently_T2()
        {
            int invokedCount = 0;
            var deferred = Promise.NewDeferred<int>();
            deferred.Promise
                .CatchCancelation(_ => { Interlocked.Increment(ref invokedCount); })
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
        public void DeferredCancelMayNotBeCalledConcurrently_T3()
        {
            int invokedCount = 0;
            var cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred<int>(cancelationSource.Token);
            deferred.Promise
                .CatchCancelation(_ => { Interlocked.Increment(ref invokedCount); })
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
                    if (synchronizationType != SynchronizationType.Synchronous)
                    {
                        Monitor.Enter(progressHelper._locker);
                    }
                },
                // Teardown
                () =>
                {
                    float progress = progressHelper.GetCurrentProgress(true, true);
                    if (synchronizationType != SynchronizationType.Synchronous)
                    {
                        Monitor.Exit(progressHelper._locker);
                    }
                    // Race condition could report 0 instead of expected from background threads.
                    if (synchronizationType == SynchronizationType.Background && progress < expected * 0.5f)
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
                () => progressHelper.Subscribe(promise).Forget()
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
                    if (synchronizationType != SynchronizationType.Synchronous)
                    {
                        Monitor.Enter(progressHelper._locker);
                    }
                },
                // Teardown
                () =>
                {
                    float progress = progressHelper.GetCurrentProgress(true, true);
                    if (synchronizationType != SynchronizationType.Synchronous)
                    {
                        Monitor.Exit(progressHelper._locker);
                    }
                    // Race condition could report 0 instead of expected from background threads.
                    if (synchronizationType == SynchronizationType.Background && progress < expected * 0.5f)
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
                () => progressHelper.Subscribe(promise).Forget()
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
                () => cancelationSource.Cancel("Cancel"),
                () => promise.CatchCancelation(_ => { Interlocked.Increment(ref invokedCount); }).Forget()
            );
        }

        [Test]
        public void DeferredMayBeCanceledAndPromiseAwaitedConcurrently_void1()
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
                () => promise.CatchCancelation(_ => { Interlocked.Increment(ref invokedCount); }).Forget()
            );
        }

        [Test]
        public void DeferredMayBeCanceledAndPromiseAwaitedConcurrently_void2()
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
                () => deferred.Cancel("Cancel"),
                () => promise.CatchCancelation(_ => { Interlocked.Increment(ref invokedCount); }).Forget()
            );
        }

        [Test]
        public void DeferredMayBeCanceledAndPromiseAwaitedConcurrently_void3()
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
                () => promise.CatchCancelation(_ => { Interlocked.Increment(ref invokedCount); }).Forget()
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
                () => cancelationSource.Cancel("Cancel"),
                () => promise.CatchCancelation(_ => { Interlocked.Increment(ref invokedCount); }).Forget()
            );
        }

        [Test]
        public void DeferredMayBeCanceledAndPromiseAwaitedConcurrently_T1()
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
                () => promise.CatchCancelation(_ => { Interlocked.Increment(ref invokedCount); }).Forget()
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
                () => deferred.Cancel("Cancel"),
                () => promise.CatchCancelation(_ => { Interlocked.Increment(ref invokedCount); }).Forget()
            );
        }

        [Test]
        public void DeferredMayBeCanceledAndPromiseAwaitedConcurrently_T3()
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
                () => promise.CatchCancelation(_ => { Interlocked.Increment(ref invokedCount); }).Forget()
            );
        }
    }
}

#endif