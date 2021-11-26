#if !UNITY_WEBGL

#if !PROTO_PROMISE_PROGRESS_DISABLE
#define PROMISE_PROGRESS
#else
#undef PROMISE_PROGRESS
#endif

using NUnit.Framework;
using Proto.Promises;
using System;
using System.Linq;
using System.Threading;

namespace ProtoPromiseTests.Threading
{
    public class ApiWithCancelationTokenConcurrencyTests
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
        public void Then_PromiseMayBeResolvedAndCallbackCanceledConcurrently_void()
        {
            var cancelationSource = default(CancelationSource);
            var deferred = default(Promise.Deferred);
            bool completed = false;

            var resolveActions = TestHelper.ResolveActionsVoidWithCancelation(() => { });
            var thenActions = TestHelper.ThenActionsVoidWithCancelation(() => { }, null);
            var continueActions = TestHelper.ContinueWithActionsVoidWithCancelation(() => { });
            var threadHelper = new ThreadHelper();
            foreach (var action in resolveActions.Concat(thenActions).Concat(continueActions))
            {
                threadHelper.ExecuteParallelActionsWithOffsets(false,
                    // Setup
                    () =>
                    {
                        cancelationSource = CancelationSource.New();
                        deferred = Promise.NewDeferred();
                        completed = false;
                        action(deferred.Promise, cancelationSource.Token)
                            .Finally(() => completed = true) // State of the promise is indeterminable, just make sure it completes.
                            .Forget();
                    },
                    // Teardown
                    () =>
                    {
                        cancelationSource.Dispose();
                        Assert.IsTrue(completed);
                    },
                    // Parallel actions
                    () => cancelationSource.Cancel(),
                    () => deferred.Resolve()
                );
            }
        }

        [Test]
        public void Then_PromiseMayBeResolvedAndCallbackCanceledConcurrently_T()
        {
            var cancelationSource = default(CancelationSource);
            var deferred = default(Promise<int>.Deferred);
            bool completed = false;

            var resolveActions = TestHelper.ResolveActionsWithCancelation<int>(v => { });
            var thenActions = TestHelper.ThenActionsWithCancelation<int>(v => { }, null);
            var continueActions = TestHelper.ContinueWithActionsWithCancelation<int>(() => { });
            var threadHelper = new ThreadHelper();
            foreach (var action in resolveActions.Concat(thenActions).Concat(continueActions))
            {
                threadHelper.ExecuteParallelActionsWithOffsets(false,
                    // Setup
                    () =>
                    {
                        cancelationSource = CancelationSource.New();
                        deferred = Promise.NewDeferred<int>();
                        completed = false;
                        action(deferred.Promise, cancelationSource.Token)
                            .Finally(() => completed = true) // State of the promise is indeterminable, just make sure it completes.
                            .Forget();
                    },
                    // Teardown
                    () =>
                    {
                        cancelationSource.Dispose();
                        Assert.IsTrue(completed);
                    },
                    // Parallel actions
                    () => cancelationSource.Cancel(),
                    () => deferred.Resolve(1)
                );
            }
        }

        [Test]
        public void Catch_PromiseMayBeRejectedAndCallbackCanceledConcurrently_void()
        {
            int rejection = 1;

            var cancelationSource = default(CancelationSource);
            var deferred = default(Promise.Deferred);
            bool completed = false;

            var catchActions = TestHelper.CatchActionsVoidWithCancelation(() => { });
            var thenActions = TestHelper.ThenActionsVoidWithCancelation(null, () => { });
            var continueActions = TestHelper.ContinueWithActionsVoidWithCancelation(() => { });
            var threadHelper = new ThreadHelper();
            foreach (var action in catchActions.Concat(thenActions).Concat(continueActions))
            {
                threadHelper.ExecuteParallelActionsWithOffsets(false,
                    // Setup
                    () =>
                    {
                        cancelationSource = CancelationSource.New();
                        deferred = Promise.NewDeferred();
                        completed = false;
                        action(deferred.Promise, cancelationSource.Token)
                            .Finally(() => completed = true) // State of the promise is indeterminable, just make sure it completes.
                            .Forget();
                    },
                    // Teardown
                    () =>
                    {
                        cancelationSource.Dispose();
                        Assert.IsTrue(completed);
                    },
                    // Parallel actions
                    () => cancelationSource.Cancel(),
                    () => deferred.Reject(rejection)
                );
            }
        }

        [Test]
        public void Catch_PromiseMayBeRejectedAndCallbackCanceledConcurrently_T()
        {
            int rejection = 1;

            var cancelationSource = default(CancelationSource);
            var deferred = default(Promise<int>.Deferred);
            bool completed = false;

            var catchActions = TestHelper.CatchActionsWithCancelation<int>(() => { });
            var thenActions = TestHelper.ThenActionsWithCancelation<int>(null, () => { });
            var continueActions = TestHelper.ContinueWithActionsWithCancelation<int>(() => { });
            var threadHelper = new ThreadHelper();
            foreach (var action in catchActions.Concat(thenActions).Concat(continueActions))
            {
                threadHelper.ExecuteParallelActionsWithOffsets(false,
                    // Setup
                    () =>
                    {
                        cancelationSource = CancelationSource.New();
                        deferred = Promise.NewDeferred<int>();
                        completed = false;
                        action(deferred.Promise, cancelationSource.Token)
                            .Finally(() => completed = true) // State of the promise is indeterminable, just make sure it completes.
                            .Forget();
                    },
                    // Teardown
                    () =>
                    {
                        cancelationSource.Dispose();
                        Assert.IsTrue(completed);
                    },
                    // Parallel actions
                    () => cancelationSource.Cancel(),
                    () => deferred.Reject(rejection)
                );
            }
        }

        [Test]
        public void CatchCancelation_PromiseMayBeCanceledAndCallbackCanceledConcurrently_void()
        {
            var cancelationSource = default(CancelationSource);
            var deferred = default(Promise.Deferred);
            bool completed = false;

            var threadHelper = new ThreadHelper();
            foreach (var action in new Func<Promise, CancelationToken, Promise>[]
                {
                    (promise, token) => promise.CatchCancelation(_ => { }, token),
                    (promise, token) => promise.CatchCancelation(1, (cv, _) => { }, token),
                })
            {
                threadHelper.ExecuteParallelActionsWithOffsets(false,
                    // Setup
                    () =>
                    {
                        cancelationSource = CancelationSource.New();
                        deferred = Promise.NewDeferred();
                        action(deferred.Promise, cancelationSource.Token)
                            .Finally(() => completed = true) // Make sure it completes.
                            .Forget();
                    },
                    // Teardown
                    () =>
                    {
                        cancelationSource.Dispose();
                        Assert.IsTrue(completed);
                    },
                    // Parallel actions
                    () => cancelationSource.Cancel(),
                    () => deferred.Cancel(1)
                );
            }
        }

        [Test]
        public void CatchCancelation_PromiseMayBeCanceledAndCallbackCanceledConcurrently_T()
        {
            var cancelationSource = default(CancelationSource);
            var deferred = default(Promise<int>.Deferred);
            bool completed = false;

            var threadHelper = new ThreadHelper();
            foreach (var action in new Func<Promise<int>, CancelationToken, Promise<int>>[]
                {
                    (promise, token) => promise.CatchCancelation(_ => { }, token),
                    (promise, token) => promise.CatchCancelation(1, (cv, _) => { }, token),
                })
            {
                threadHelper.ExecuteParallelActionsWithOffsets(false,
                    // Setup
                    () =>
                    {
                        cancelationSource = CancelationSource.New();
                        deferred = Promise.NewDeferred<int>();
                        action(deferred.Promise, cancelationSource.Token)
                            .Finally(() => completed = true) // Make sure it completes.
                            .Forget();
                    },
                    // Teardown
                    () =>
                    {
                        cancelationSource.Dispose();
                        Assert.IsTrue(completed);
                    },
                    // Parallel actions
                    () => cancelationSource.Cancel(),
                    () => deferred.Cancel(1)
                );
            }
        }

        [Test]
        public void Then_PromiseMayBeResolvedAndAwaitedAndCallbackCanceledConcurrently_void()
        {
            var cancelationSource = default(CancelationSource);
            var deferred = default(Promise.Deferred);
            var cancelationToken = default(CancelationToken);
            var promise = default(Promise);
            bool completed = false;

            var resolveActions = TestHelper.ResolveActionsVoidWithCancelation(() => { });
            var thenActions = TestHelper.ThenActionsVoidWithCancelation(() => { }, null);
            var continueActions = TestHelper.ContinueWithActionsVoidWithCancelation(() => { });
            var threadHelper = new ThreadHelper();
            foreach (var action in resolveActions.Concat(thenActions).Concat(continueActions))
            {
                threadHelper.ExecuteParallelActionsWithOffsets(false,
                    // Setup
                    () =>
                    {
                        cancelationSource = CancelationSource.New();
                        deferred = Promise.NewDeferred();
                        cancelationToken = cancelationSource.Token;
                        promise = deferred.Promise;
                        completed = false;
                    },
                    // Teardown
                    () =>
                    {
                        cancelationSource.Dispose();
                        Assert.IsTrue(completed);
                    },
                    // Parallel actions
                    () => cancelationSource.Cancel(),
                    () => deferred.Resolve(),
                    () => action(promise, cancelationToken)
                        .Finally(() => completed = true) // State of the promise is indeterminable, just make sure it completes.
                        .Forget()
                );
            }
        }

        [Test]
        public void Then_PromiseMayBeResolvedAndAwaitedAndCallbackCanceledConcurrently_T()
        {
            var cancelationSource = default(CancelationSource);
            var deferred = default(Promise<int>.Deferred);
            var cancelationToken = default(CancelationToken);
            var promise = default(Promise<int>);
            bool completed = false;

            var resolveActions = TestHelper.ResolveActionsWithCancelation<int>(v => { });
            var thenActions = TestHelper.ThenActionsWithCancelation<int>(v => { }, null);
            var continueActions = TestHelper.ContinueWithActionsWithCancelation<int>(() => { });
            var threadHelper = new ThreadHelper();
            foreach (var action in resolveActions.Concat(thenActions).Concat(continueActions))
            {
                threadHelper.ExecuteParallelActionsWithOffsets(false,
                    // Setup
                    () =>
                    {
                        cancelationSource = CancelationSource.New();
                        deferred = Promise.NewDeferred<int>();
                        cancelationToken = cancelationSource.Token;
                        promise = deferred.Promise;
                        completed = false;
                    },
                    // Teardown
                    () =>
                    {
                        cancelationSource.Dispose();
                        Assert.IsTrue(completed);
                    },
                    // Parallel actions
                    () => cancelationSource.Cancel(),
                    () => deferred.Resolve(1),
                    () => action(promise, cancelationToken)
                        .Finally(() => completed = true) // State of the promise is indeterminable, just make sure it completes.
                        .Forget()
                );
            }
        }

        [Test]
        public void Catch_PromiseMayBeRejectedAndAwaitedAndCallbackCanceledConcurrently_void()
        {
            int rejection = 1;

            var cancelationSource = default(CancelationSource);
            var deferred = default(Promise.Deferred);
            var cancelationToken = default(CancelationToken);
            var promise = default(Promise);
            bool completed = false;

            var catchActions = TestHelper.CatchActionsVoidWithCancelation(() => { });
            var thenActions = TestHelper.ThenActionsVoidWithCancelation(null, () => { });
            var continueActions = TestHelper.ContinueWithActionsVoidWithCancelation(() => { });
            var threadHelper = new ThreadHelper();
            foreach (var action in catchActions.Concat(thenActions).Concat(continueActions))
            {
                threadHelper.ExecuteParallelActionsWithOffsets(false,
                    // Setup
                    () =>
                    {
                        cancelationSource = CancelationSource.New();
                        deferred = Promise.NewDeferred();
                        cancelationToken = cancelationSource.Token;
                        promise = deferred.Promise;
                        completed = false;
                    },
                    // Teardown
                    () =>
                    {
                        cancelationSource.Dispose();
                        Assert.IsTrue(completed);
                    },
                    // Parallel actions
                    () => cancelationSource.Cancel(),
                    () => deferred.Reject(rejection),
                    () => action(promise, cancelationToken)
                        .Finally(() => completed = true) // State of the promise is indeterminable, just make sure it completes.
                        .Forget()
                );
            }
        }

        [Test]
        public void Catch_PromiseMayBeRejectedAndAwaitedAndCallbackCanceledConcurrently_T()
        {
            int rejection = 1;

            var cancelationSource = default(CancelationSource);
            var deferred = default(Promise<int>.Deferred);
            var cancelationToken = default(CancelationToken);
            var promise = default(Promise<int>);
            bool completed = false;

            var catchActions = TestHelper.CatchActionsWithCancelation<int>(() => { });
            var thenActions = TestHelper.ThenActionsWithCancelation<int>(null, () => { });
            var continueActions = TestHelper.ContinueWithActionsWithCancelation<int>(() => { });
            var threadHelper = new ThreadHelper();
            foreach (var action in catchActions.Concat(thenActions).Concat(continueActions))
            {
                threadHelper.ExecuteParallelActionsWithOffsets(false,
                    // Setup
                    () =>
                    {
                        cancelationSource = CancelationSource.New();
                        deferred = Promise.NewDeferred<int>();
                        cancelationToken = cancelationSource.Token;
                        promise = deferred.Promise;
                        completed = false;
                    },
                    // Teardown
                    () =>
                    {
                        cancelationSource.Dispose();
                        Assert.IsTrue(completed);
                    },
                    // Parallel actions
                    () => cancelationSource.Cancel(),
                    () => deferred.Reject(rejection),
                    () => action(promise, cancelationToken)
                        .Finally(() => completed = true) // State of the promise is indeterminable, just make sure it completes.
                        .Forget()
                );
            }
        }

        [Test]
        public void CatchCancelation_PromiseMayBeCanceledAndAwaitedAndCallbackCanceledConcurrently_void()
        {
            var cancelationSource = default(CancelationSource);
            var deferred = default(Promise.Deferred);
            var cancelationToken = default(CancelationToken);
            var promise = default(Promise);
            bool completed = false;

            var threadHelper = new ThreadHelper();
            foreach (var action in new Func<Promise, CancelationToken, Promise>[]
                {
                    (p, token) => p.CatchCancelation(_ => { }, token),
                    (p, token) => p.CatchCancelation(1, (cv, _) => { }, token),
                })
            {
                threadHelper.ExecuteParallelActionsWithOffsets(false,
                    // Setup
                    () =>
                    {
                        cancelationSource = CancelationSource.New();
                        deferred = Promise.NewDeferred();
                        cancelationToken = cancelationSource.Token;
                        promise = deferred.Promise;
                    },
                    // Teardown
                    () =>
                    {
                        cancelationSource.Dispose();
                        Assert.IsTrue(completed);
                    },
                    // Parallel actions
                    () => cancelationSource.Cancel(),
                    () => deferred.Cancel(1),
                    () => action(promise, cancelationToken)
                        .Finally(() => completed = true) // Make sure it completes.
                        .Forget()
                );
            }
        }

        [Test]
        public void CatchCancelation_PromiseMayBeCanceledAndAwaitedAndCallbackCanceledConcurrently_T()
        {
            var cancelationSource = default(CancelationSource);
            var deferred = default(Promise<int>.Deferred);
            var cancelationToken = default(CancelationToken);
            var promise = default(Promise<int>);
            bool completed = false;

            var threadHelper = new ThreadHelper();
            foreach (var action in new Func<Promise<int>, CancelationToken, Promise<int>>[]
                {
                    (p, token) => p.CatchCancelation(_ => { }, token),
                    (p, token) => p.CatchCancelation(1, (cv, _) => { }, token),
                })
            {
                threadHelper.ExecuteParallelActionsWithOffsets(false,
                    // Setup
                    () =>
                    {
                        cancelationSource = CancelationSource.New();
                        deferred = Promise.NewDeferred<int>();
                        cancelationToken = cancelationSource.Token;
                        promise = deferred.Promise;
                    },
                    // Teardown
                    () =>
                    {
                        cancelationSource.Dispose();
                        Assert.IsTrue(completed);
                    },
                    // Parallel actions
                    () => cancelationSource.Cancel(),
                    () => deferred.Cancel(1),
                    () => action(promise, cancelationToken)
                        .Finally(() => completed = true) // Make sure it completes.
                        .Forget()
                );
            }
        }

#if PROMISE_PROGRESS
        [Test]
        public void Progress_PromiseMayReportProgressAndCallbackCanceledConcurrently_void(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var cancelationSource = default(CancelationSource);
            var deferred = default(Promise.Deferred);
            bool completed = false;
            bool invoked = false;

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    cancelationSource = CancelationSource.New();
                    deferred = Promise.NewDeferred();
                    // Whether the callback is called or not is indeterminable, this test is really to make sure nothing explodes.
                    progressHelper.Subscribe(deferred.Promise, cancelationSource.Token)
                        .Finally(() => completed = true) // Make sure it completes.
                        .Forget();
                },
                // Teardown
                () =>
                {
                    invoked = false;
                    cancelationSource.Dispose();
                    deferred.Resolve();
                    TestHelper.ExecuteForegroundCallbacks();
                    if (synchronizationType == SynchronizationType.Background)
                    {
                        SpinWait.SpinUntil(() => completed, TimeSpan.FromSeconds(1));
                    }
                    else
                    {
                        Assert.IsTrue(completed);
                    }
                    Assert.IsFalse(invoked);
                },
                // Parallel actions
                () => cancelationSource.Cancel(),
                () => deferred.ReportProgress(0.5f)
            );
        }

        [Test]
        public void Progress_PromiseMayReportProgressAndCallbackCanceledConcurrently_T(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var cancelationSource = default(CancelationSource);
            var deferred = default(Promise<int>.Deferred);
            bool completed = false;
            bool invoked = false;

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    cancelationSource = CancelationSource.New();
                    deferred = Promise.NewDeferred<int>();
                    // Whether the callback is called or not is indeterminable, this test is really to make sure nothing explodes.
                    progressHelper.Subscribe(deferred.Promise, cancelationSource.Token)
                        .Finally(() => completed = true) // Make sure it completes.
                        .Forget();
                },
                // Teardown
                () =>
                {
                    invoked = false;
                    cancelationSource.Dispose();
                    deferred.Resolve(1);
                    TestHelper.ExecuteForegroundCallbacks();
                    if (synchronizationType == SynchronizationType.Background)
                    {
                        SpinWait.SpinUntil(() => completed, TimeSpan.FromSeconds(1));
                    }
                    else
                    {
                        Assert.IsTrue(completed);
                    }
                    Assert.IsFalse(invoked);
                },
                // Parallel actions
                () => cancelationSource.Cancel(),
                () => deferred.ReportProgress(0.5f)
            );
        }

        [Test]
        public void Progress_PromiseMayReportProgressAndBeSubscribedProgressAndCallbackCanceledConcurrently_void(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var cancelationSource = default(CancelationSource);
            var deferred = default(Promise.Deferred);
            var cancelationToken = default(CancelationToken);
            var promise = default(Promise);
            bool completed = false;
            bool invoked = false;

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    cancelationSource = CancelationSource.New();
                    deferred = Promise.NewDeferred();
                    cancelationToken = cancelationSource.Token;
                    promise = deferred.Promise;
                },
                // Teardown
                () =>
                {
                    invoked = false;
                    cancelationSource.Dispose();
                    deferred.Resolve();
                    TestHelper.ExecuteForegroundCallbacks();
                    if (synchronizationType == SynchronizationType.Background)
                    {
                        SpinWait.SpinUntil(() => completed, TimeSpan.FromSeconds(1));
                    }
                    else
                    {
                        Assert.IsTrue(completed);
                    }
                    Assert.IsFalse(invoked);
                },
                // Parallel actions
                () => cancelationSource.Cancel(),
                () => deferred.ReportProgress(0.5f),
                () => progressHelper.Subscribe(promise, cancelationToken)
                    .Finally(() => completed = true) // Whether the callback is called or not is indeterminable, just make sure it completes.
                    .Forget()
            );
        }

        [Test]
        public void Progress_PromiseMayReportProgressAndBeSubscribedProgressAndCallbackCanceledConcurrently_T(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var cancelationSource = default(CancelationSource);
            var deferred = default(Promise<int>.Deferred);
            var cancelationToken = default(CancelationToken);
            var promise = default(Promise<int>);
            bool completed = false;
            bool invoked = false;

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    cancelationSource = CancelationSource.New();
                    deferred = Promise.NewDeferred<int>();
                    cancelationToken = cancelationSource.Token;
                    promise = deferred.Promise;
                },
                // Teardown
                () =>
                {
                    invoked = false;
                    cancelationSource.Dispose();
                    deferred.Resolve(1);
                    TestHelper.ExecuteForegroundCallbacks();
                    if (synchronizationType == SynchronizationType.Background)
                    {
                        SpinWait.SpinUntil(() => completed, TimeSpan.FromSeconds(1));
                    }
                    else
                    {
                        Assert.IsTrue(completed);
                    }
                    Assert.IsFalse(invoked);
                },
                // Parallel actions
                () => cancelationSource.Cancel(),
                () => deferred.ReportProgress(0.5f),
                () => progressHelper.Subscribe(promise, cancelationToken)
                    .Finally(() => completed = true) // Whether the callback is called or not is indeterminable, just make sure it completes.
                    .Forget()
            );
        }

        [Test]
        public void Progress_PromisesChainMayBeCanceledInMultiplePlacesAndProgressReportedConcurrently_void(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var cancelationSource1 = default(CancelationSource);
            var cancelationSource2 = default(CancelationSource);
            var deferred = default(Promise.Deferred);
            bool completed = false;

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    cancelationSource1 = CancelationSource.New();
                    cancelationSource2 = CancelationSource.New();
                    deferred = Promise.NewDeferred();
                    // Whether the callback is called or not is indeterminable, this test is really to make sure nothing explodes.
                    progressHelper.Subscribe(
                        deferred.Promise.ThenDuplicate(cancelationSource1.Token).ThenDuplicate().ThenDuplicate(cancelationSource2.Token).ThenDuplicate()
                    )
                        .Finally(() => completed = true) // Make sure it completes.
                        .Forget();
                },
                // Teardown
                () =>
                {
                    cancelationSource1.Dispose();
                    cancelationSource2.Dispose();
                    deferred.Resolve();
                    TestHelper.ExecuteForegroundCallbacks();
                    if (synchronizationType == SynchronizationType.Background)
                    {
                        SpinWait.SpinUntil(() => completed, TimeSpan.FromSeconds(1));
                    }
                    else
                    {
                        Assert.IsTrue(completed);
                    }
                },
                // Parallel actions
                () => cancelationSource1.Cancel(),
                () => cancelationSource2.Cancel(),
                () => deferred.ReportProgress(0.5f)
            );
        }

        [Test]
        public void Progress_PromisesChainMayBeCanceledInMultiplePlacesAndProgressReportedConcurrently_T(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var cancelationSource1 = default(CancelationSource);
            var cancelationSource2 = default(CancelationSource);
            var deferred = default(Promise<int>.Deferred);
            bool completed = false;

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    cancelationSource1 = CancelationSource.New();
                    cancelationSource2 = CancelationSource.New();
                    deferred = Promise.NewDeferred<int>();
                    // Whether the callback is called or not is indeterminable, this test is really to make sure nothing explodes.
                    progressHelper.Subscribe(
                        deferred.Promise.ThenDuplicate(cancelationSource1.Token).ThenDuplicate().ThenDuplicate(cancelationSource2.Token).ThenDuplicate()
                    )
                        .Finally(() => completed = true) // Make sure it completes.
                        .Forget();
                },
                // Teardown
                () =>
                {
                    cancelationSource1.Dispose();
                    cancelationSource2.Dispose();
                    deferred.Resolve(1);
                    TestHelper.ExecuteForegroundCallbacks();
                    if (synchronizationType == SynchronizationType.Background)
                    {
                        SpinWait.SpinUntil(() => completed, TimeSpan.FromSeconds(1));
                    }
                    else
                    {
                        Assert.IsTrue(completed);
                    }
                },
                // Parallel actions
                () => cancelationSource1.Cancel(),
                () => cancelationSource2.Cancel(),
                () => deferred.ReportProgress(0.5f)
            );
        }
#endif
    }
}

#endif // !UNITY_WEBGL