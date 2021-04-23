#if CSHARP_7_OR_LATER

#if !PROTO_PROMISE_PROGRESS_DISABLE
#define PROMISE_PROGRESS
#else
#undef PROMISE_PROGRESS
#endif

using NUnit.Framework;
using System;
using System.Linq;

namespace Proto.Promises.Tests.Threading
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
                        deferred.Resolve();
                    },
                    // Teardown
                    () =>
                    {
                        cancelationSource.Dispose();
                        Promise.Manager.HandleCompletes();
                        Assert.IsTrue(completed);
                    },
                    // Parallel actions
                    () => cancelationSource.Cancel(),
                    () => Promise.Manager.HandleCompletes()
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
                        deferred.Resolve(1);
                    },
                    // Teardown
                    () =>
                    {
                        cancelationSource.Dispose();
                        Promise.Manager.HandleCompletes();
                        Assert.IsTrue(completed);
                    },
                    // Parallel actions
                    () => cancelationSource.Cancel(),
                    () => Promise.Manager.HandleCompletes()
                );
            }
        }

        [Test]
        public void Catch_PromiseMayBeRejectedAndCallbackCanceledConcurrently_void()
        {
            int rejection = 1;

            // If onRejected is canceled, the rejection is unhandled. So we need to catch it here and make sure it's what we expect.
            var prevRejectionHandler = Promise.Config.UncaughtRejectionHandler;
            Promise.Config.UncaughtRejectionHandler = ex =>
            {
                if (!ex.Value.Equals(rejection))
                {
                    throw ex;
                }
            };

            try
            {
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
                            deferred.Reject(rejection);
                        },
                        // Teardown
                        () =>
                        {
                            cancelationSource.Dispose();
                            Promise.Manager.HandleCompletes();
                            Assert.IsTrue(completed);
                        },
                        // Parallel actions
                        () => cancelationSource.Cancel(),
                        () => Promise.Manager.HandleCompletes()
                    );
                }
            }
            finally
            {
                Promise.Config.UncaughtRejectionHandler = prevRejectionHandler;
            }
        }

        [Test]
        public void Catch_PromiseMayBeRejectedAndCallbackCanceledConcurrently_T()
        {
            int rejection = 1;

            // If onRejected is canceled, the rejection is unhandled. So we need to catch it here and make sure it's what we expect.
            var prevRejectionHandler = Promise.Config.UncaughtRejectionHandler;
            Promise.Config.UncaughtRejectionHandler = ex =>
            {
                if (!ex.Value.Equals(rejection))
                {
                    throw ex;
                }
            };

            try
            {
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
                            deferred.Reject(rejection);
                        },
                        // Teardown
                        () =>
                        {
                            cancelationSource.Dispose();
                            Promise.Manager.HandleCompletes();
                            Assert.IsTrue(completed);
                        },
                        // Parallel actions
                        () => cancelationSource.Cancel(),
                        () => Promise.Manager.HandleCompletes()
                    );
                }
            }
            finally
            {
                Promise.Config.UncaughtRejectionHandler = prevRejectionHandler;
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
                        deferred.Cancel(1);
                    },
                    // Teardown
                    () =>
                    {
                        cancelationSource.Dispose();
                        Promise.Manager.HandleCompletes();
                        Assert.IsTrue(completed);
                    },
                    // Parallel actions
                    () => cancelationSource.Cancel(),
                    () => Promise.Manager.HandleCompletes()
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
                        deferred.Cancel(1);
                    },
                    // Teardown
                    () =>
                    {
                        cancelationSource.Dispose();
                        Promise.Manager.HandleCompletes();
                        Assert.IsTrue(completed);
                    },
                    // Parallel actions
                    () => cancelationSource.Cancel(),
                    () => Promise.Manager.HandleCompletes()
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
                        deferred.Resolve();
                    },
                    // Teardown
                    () =>
                    {
                        cancelationSource.Dispose();
                        Promise.Manager.HandleCompletes();
                        Assert.IsTrue(completed);
                    },
                    // Parallel actions
                    () => cancelationSource.Cancel(),
                    () => Promise.Manager.HandleCompletes(),
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
                        deferred.Resolve(1);
                    },
                    // Teardown
                    () =>
                    {
                        cancelationSource.Dispose();
                        Promise.Manager.HandleCompletes();
                        Assert.IsTrue(completed);
                    },
                    // Parallel actions
                    () => cancelationSource.Cancel(),
                    () => Promise.Manager.HandleCompletes(),
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

            // If onRejected is canceled, the rejection is unhandled. So we need to catch it here and make sure it's what we expect.
            var prevRejectionHandler = Promise.Config.UncaughtRejectionHandler;
            Promise.Config.UncaughtRejectionHandler = ex =>
            {
                if (!ex.Value.Equals(rejection))
                {
                    throw ex;
                }
            };

            try
            {
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
                            deferred.Reject(rejection);
                        },
                        // Teardown
                        () =>
                        {
                            cancelationSource.Dispose();
                            Promise.Manager.HandleCompletes();
                            Assert.IsTrue(completed);
                        },
                        // Parallel actions
                        () => cancelationSource.Cancel(),
                        () => Promise.Manager.HandleCompletes(),
                        () => action(promise, cancelationToken)
                            .Finally(() => completed = true) // State of the promise is indeterminable, just make sure it completes.
                            .Forget()
                    );
                }
            }
            finally
            {
                Promise.Config.UncaughtRejectionHandler = prevRejectionHandler;
            }
        }

        [Test]
        public void Catch_PromiseMayBeRejectedAndAwaitedAndCallbackCanceledConcurrently_T()
        {
            int rejection = 1;

            // If onRejected is canceled, the rejection is unhandled. So we need to catch it here and make sure it's what we expect.
            var prevRejectionHandler = Promise.Config.UncaughtRejectionHandler;
            Promise.Config.UncaughtRejectionHandler = ex =>
            {
                if (!ex.Value.Equals(rejection))
                {
                    throw ex;
                }
            };

            try
            {
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
                            deferred.Reject(rejection);
                        },
                        // Teardown
                        () =>
                        {
                            cancelationSource.Dispose();
                            Promise.Manager.HandleCompletes();
                            Assert.IsTrue(completed);
                        },
                        // Parallel actions
                        () => cancelationSource.Cancel(),
                        () => Promise.Manager.HandleCompletes(),
                        () => action(promise, cancelationToken)
                            .Finally(() => completed = true) // State of the promise is indeterminable, just make sure it completes.
                            .Forget()
                    );
                }
            }
            finally
            {
                Promise.Config.UncaughtRejectionHandler = prevRejectionHandler;
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
                        cancelationToken = cancelationSource.Token;
                        promise = deferred.Promise;
                        deferred.Cancel(1);
                    },
                    // Teardown
                    () =>
                    {
                        cancelationSource.Dispose();
                        Promise.Manager.HandleCompletes();
                        Assert.IsTrue(completed);
                    },
                    // Parallel actions
                    () => cancelationSource.Cancel(),
                    () => Promise.Manager.HandleCompletes(),
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
                        cancelationToken = cancelationSource.Token;
                        promise = deferred.Promise;
                        deferred.Cancel(1);
                    },
                    // Teardown
                    () =>
                    {
                        cancelationSource.Dispose();
                        Promise.Manager.HandleCompletes();
                        Assert.IsTrue(completed);
                    },
                    // Parallel actions
                    () => cancelationSource.Cancel(),
                    () => Promise.Manager.HandleCompletes(),
                    () => action(promise, cancelationToken)
                        .Finally(() => completed = true) // Make sure it completes.
                        .Forget()
                );
            }
        }

#if PROMISE_PROGRESS
        [Test]
        public void Progress_PromiseMayReportProgressAndCallbackCanceledConcurrently_void()
        {
            var cancelationSource = default(CancelationSource);
            var deferred = default(Promise.Deferred);
            bool completed = false;

            var threadHelper = new ThreadHelper();
            foreach (var action in new Func<Promise, CancelationToken, Promise>[]
                {
                    (promise, token) => promise.Progress(_ => { }, token),
                    (promise, token) => promise.Progress(1, (cv, _) => { }, token),
                })
            {
                threadHelper.ExecuteParallelActionsWithOffsets(false,
                    // Setup
                    () =>
                    {
                        cancelationSource = CancelationSource.New();
                        deferred = Promise.NewDeferred();
                        // Whether the callback is called or not is indeterminable, this test is really to make sure nothing explodes.
                        action(deferred.Promise, cancelationSource.Token)
                            .Finally(() => completed = true) // Make sure it completes.
                            .Forget();
                    },
                    // Teardown
                    () =>
                    {
                        cancelationSource.Dispose();
                        deferred.Resolve();
                        Promise.Manager.HandleCompletesAndProgress();
                        Assert.IsTrue(completed);
                    },
                    // Parallel actions
                    () => cancelationSource.Cancel(),
                    () => deferred.ReportProgress(0.5f)
                );
            }
        }

        [Test]
        public void Progress_PromiseMayReportProgressAndCallbackCanceledConcurrently_T()
        {
            var cancelationSource = default(CancelationSource);
            var deferred = default(Promise<int>.Deferred);
            bool completed = false;

            var threadHelper = new ThreadHelper();
            foreach (var action in new Func<Promise, CancelationToken, Promise>[]
                {
                    (promise, token) => promise.Progress(_ => { }, token),
                    (promise, token) => promise.Progress(1, (cv, _) => { }, token),
                })
            {
                threadHelper.ExecuteParallelActionsWithOffsets(false,
                    // Setup
                    () =>
                    {
                        cancelationSource = CancelationSource.New();
                        deferred = Promise.NewDeferred<int>();
                        // Whether the callback is called or not is indeterminable, this test is really to make sure nothing explodes.
                        action(deferred.Promise, cancelationSource.Token)
                            .Finally(() => completed = true) // Make sure it completes.
                            .Forget();
                    },
                    // Teardown
                    () =>
                    {
                        cancelationSource.Dispose();
                        deferred.Resolve(1);
                        Promise.Manager.HandleCompletesAndProgress();
                        Assert.IsTrue(completed);
                    },
                    // Parallel actions
                    () => cancelationSource.Cancel(),
                    () => deferred.ReportProgress(0.5f)
                );
            }
        }

        [Test]
        public void Progress_PromiseMayReportProgressAndBeSubscribedProgressAndCallbackCanceledConcurrently_void()
        {
            var cancelationSource = default(CancelationSource);
            var deferred = default(Promise.Deferred);
            var cancelationToken = default(CancelationToken);
            var promise = default(Promise);
            bool completed = false;

            var threadHelper = new ThreadHelper();
            foreach (var action in new Func<Promise, CancelationToken, Promise>[]
                {
                    (promise, token) => promise.Progress(_ => { }, token),
                    (promise, token) => promise.Progress(1, (cv, _) => { }, token),
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
                        deferred.Resolve();
                        Promise.Manager.HandleCompletesAndProgress();
                        Assert.IsTrue(completed);
                    },
                    // Parallel actions
                    () => cancelationSource.Cancel(),
                    () => deferred.ReportProgress(0.5f),
                    () => action(promise, cancelationToken)
                        .Finally(() => completed = true) // Whether the callback is called or not is indeterminable, just make sure it completes.
                        .Forget()
                );
            }
        }

        [Test]
        public void Progress_PromiseMayReportProgressAndBeSubscribedProgressAndCallbackCanceledConcurrently_T()
        {
            var cancelationSource = default(CancelationSource);
            var deferred = default(Promise<int>.Deferred);
            var cancelationToken = default(CancelationToken);
            var promise = default(Promise<int>);
            bool completed = false;

            var threadHelper = new ThreadHelper();
            foreach (var action in new Func<Promise, CancelationToken, Promise>[]
                {
                    (promise, token) => promise.Progress(_ => { }, token),
                    (promise, token) => promise.Progress(1, (cv, _) => { }, token),
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
                        deferred.Resolve(1);
                        Promise.Manager.HandleCompletesAndProgress();
                        Assert.IsTrue(completed);
                    },
                    // Parallel actions
                    () => cancelationSource.Cancel(),
                    () => deferred.ReportProgress(0.5f),
                    () => action(promise, cancelationToken)
                        .Finally(() => completed = true) // Whether the callback is called or not is indeterminable, just make sure it completes.
                        .Forget()
                );
            }
        }

        [Test]
        public void Progress_PromisesChainMayBeCanceledInMultiplePlacesAndProgressReportedConcurrently_void()
        {
            var cancelationSource1 = default(CancelationSource);
            var cancelationSource2 = default(CancelationSource);
            var deferred = default(Promise.Deferred);
            bool completed = false;

            var threadHelper = new ThreadHelper();
            foreach (var action in new Func<Promise, CancelationToken, CancelationToken, Promise>[]
                {
                    (promise, token1, token2) => promise.ThenDuplicate(token1).ThenDuplicate().ThenDuplicate(token2).ThenDuplicate().Progress(_ => { }),
                    (promise, token1, token2) => promise.ThenDuplicate(token1).ThenDuplicate().ThenDuplicate(token2).ThenDuplicate().Progress(1, (cv, _) => { }),
                })
            {
                threadHelper.ExecuteParallelActionsWithOffsets(false,
                    // Setup
                    () =>
                    {
                        cancelationSource1 = CancelationSource.New();
                        cancelationSource2 = CancelationSource.New();
                        deferred = Promise.NewDeferred();
                        // Whether the callback is called or not is indeterminable, this test is really to make sure nothing explodes.
                        action(deferred.Promise, cancelationSource1.Token, cancelationSource2.Token)
                            .Finally(() => completed = true) // Make sure it completes.
                            .Forget();
                    },
                    // Teardown
                    () =>
                    {
                        cancelationSource1.Dispose();
                        cancelationSource2.Dispose();
                        deferred.Resolve();
                        Promise.Manager.HandleCompletesAndProgress();
                        Assert.IsTrue(completed);
                    },
                    // Parallel actions
                    () => cancelationSource1.Cancel(),
                    () => cancelationSource2.Cancel(),
                    () => deferred.ReportProgress(0.5f)
                );
            }
        }

        [Test]
        public void Progress_PromisesChainMayBeCanceledInMultiplePlacesAndProgressReportedConcurrently_T()
        {
            var cancelationSource1 = default(CancelationSource);
            var cancelationSource2 = default(CancelationSource);
            var deferred = default(Promise<int>.Deferred);
            bool completed = false;

            var threadHelper = new ThreadHelper();
            foreach (var action in new Func<Promise, CancelationToken, CancelationToken, Promise>[]
                {
                    (promise, token1, token2) => promise.ThenDuplicate(token1).ThenDuplicate().ThenDuplicate().ThenDuplicate(token2).ThenDuplicate().Progress(_ => { }),
                    (promise, token1, token2) => promise.ThenDuplicate(token1).ThenDuplicate().ThenDuplicate().ThenDuplicate(token2).ThenDuplicate().Progress(1, (cv, _) => { }),
                })
            {
                threadHelper.ExecuteParallelActionsWithOffsets(false,
                    // Setup
                    () =>
                    {
                        cancelationSource1 = CancelationSource.New();
                        cancelationSource2 = CancelationSource.New();
                        deferred = Promise.NewDeferred<int>();
                        // Whether the callback is called or not is indeterminable, this test is really to make sure nothing explodes.
                        action(deferred.Promise, cancelationSource1.Token, cancelationSource2.Token)
                            .Finally(() => completed = true) // Make sure it completes.
                            .Forget();
                    },
                    // Teardown
                    () =>
                    {
                        cancelationSource1.Dispose();
                        cancelationSource2.Dispose();
                        deferred.Resolve(1);
                        Promise.Manager.HandleCompletesAndProgress();
                        Assert.IsTrue(completed);
                    },
                    // Parallel actions
                    () => cancelationSource1.Cancel(),
                    () => cancelationSource2.Cancel(),
                    () => deferred.ReportProgress(0.5f)
                );
            }
        }
#endif
    }
}

#endif