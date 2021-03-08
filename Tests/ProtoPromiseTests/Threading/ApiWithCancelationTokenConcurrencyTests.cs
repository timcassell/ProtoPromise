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
        public void PromiseThenMayBeResolvedAndCanceledConcurrently_void()
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
        public void PromiseThenMayBeResolvedAndCanceledConcurrently_T()
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
        public void PromiseCatchMayBeRejectedAndCanceledConcurrently_void()
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
                        deferred.Reject(1);
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
        public void PromiseCatchMayBeRejectedAndCanceledConcurrently_T()
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
                        deferred.Reject(1);
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

#if PROMISE_PROGRESS
        [Test]
        public void PromiseMayReportProgressAndCallbackCanceledConcurrently_void()
        {
            var cancelationSource = default(CancelationSource);
            var deferred = default(Promise.Deferred);
            bool completed = false;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    cancelationSource = CancelationSource.New();
                    deferred = Promise.NewDeferred();
                    // Whether the callback is called or not is indeterminable, this test is really to make sure nothing explodes.
                    deferred.Promise
                        .Progress(_ => { }, cancelationSource.Token)
                        .Progress(1, (cv, _) => { }, cancelationSource.Token)
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

        [Test]
        public void PromiseMayReportProgressAndCallbackCanceledConcurrently_T()
        {
            var cancelationSource = default(CancelationSource);
            var deferred = default(Promise<int>.Deferred);
            bool completed = false;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    cancelationSource = CancelationSource.New();
                    deferred = Promise.NewDeferred<int>();
                    // Whether the callback is called or not is indeterminable, this test is really to make sure nothing explodes.
                    deferred.Promise
                        .Progress(_ => { }, cancelationSource.Token)
                        .Progress(1, (cv, _) => { }, cancelationSource.Token)
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
#endif
    }
}

#endif