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
        const int rejectValue = 1;
        Action<UnhandledException> currentHandler;

        [SetUp]
        public void Setup()
        {
            TestHelper.Setup();

            // When a callback is canceled and the previous promise is rejected, the rejection is unhandled.
            // So we need to suppress that here and make sure it's correct.
            currentHandler = Promise.Config.UncaughtRejectionHandler;
            Promise.Config.UncaughtRejectionHandler = e => Assert.AreEqual(rejectValue, e.Value);
        }

        [TearDown]
        public void Teardown()
        {
            TestHelper.WaitForAllThreadsAndReplaceRejectionHandler(currentHandler);

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
                    () => deferred.Reject(rejectValue)
                );
            }
        }

        [Test]
        public void Catch_PromiseMayBeRejectedAndCallbackCanceledConcurrently_T()
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
                    },
                    // Teardown
                    () =>
                    {
                        cancelationSource.Dispose();
                        Assert.IsTrue(completed);
                    },
                    // Parallel actions
                    () => cancelationSource.Cancel(),
                    () => deferred.Reject(rejectValue)
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
                    (promise, token) => promise.CatchCancelation(() => { }, token),
                    (promise, token) => promise.CatchCancelation(1, cv => { }, token),
                    (promise, token) => promise.CatchCancelation(() => Promise.Resolved(), token),
                    (promise, token) => promise.CatchCancelation(1, cv => Promise.Resolved(), token),
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
                    () => deferred.Cancel()
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
                    (promise, token) => promise.CatchCancelation(() => 1, token),
                    (promise, token) => promise.CatchCancelation(1, cv => 1, token),
                    (promise, token) => promise.CatchCancelation(() => Promise.Resolved(1), token),
                    (promise, token) => promise.CatchCancelation(1, cv => Promise.Resolved(1), token),
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
                    () => deferred.Cancel()
                );
            }
        }

        [Test]
        public void Then_PromiseMayBeResolvedAndAwaitedAndCallbackCanceledConcurrently_void(
            [Values] ConfigureAwaitType configureAwaitType)
        {
            var cancelationSource = default(CancelationSource);
            var deferred = default(Promise.Deferred);
            var cancelationToken = default(CancelationToken);
            var promise = default(Promise);
            bool completed = false;

            Thread foregroundThread = Thread.CurrentThread;

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
                        TestHelper.ExecuteForegroundCallbacks();
                        cancelationSource.Dispose();
                        if (configureAwaitType != (ConfigureAwaitType) TestHelper.backgroundType)
                        {
                            Assert.IsTrue(completed);
                        }
                        else if (!SpinWait.SpinUntil(() => completed, TimeSpan.FromSeconds(1)))
                        {
                            throw new TimeoutException();
                        }
                    },
                    // Parallel actions
                    () => cancelationSource.Cancel(),
                    () => deferred.Resolve(),
                    () => action(promise.ConfigureAwait(configureAwaitType), cancelationToken)
                        .Finally(() =>
                        {
                            TestHelper.AssertCallbackContext(
                                configureAwaitType == ConfigureAwaitType.Foreground || configureAwaitType == ConfigureAwaitType.Explicit ? SynchronizationType.Synchronous : TestHelper.backgroundType,
                                SynchronizationType.Background,
                                foregroundThread);
                            completed = true; // State of the promise is indeterminable, just make sure it completes.
                        })
                        .Forget()
                );
            }
        }

        [Test]
        public void Then_PromiseMayBeResolvedAndAwaitedAndCallbackCanceledConcurrently_T(
            [Values] ConfigureAwaitType configureAwaitType)
        {
            var cancelationSource = default(CancelationSource);
            var deferred = default(Promise<int>.Deferred);
            var cancelationToken = default(CancelationToken);
            var promise = default(Promise<int>);
            bool completed = false;

            Thread foregroundThread = Thread.CurrentThread;

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
                        TestHelper.ExecuteForegroundCallbacks();
                        cancelationSource.Dispose();
                        if (configureAwaitType != (ConfigureAwaitType) TestHelper.backgroundType)
                        {
                            Assert.IsTrue(completed);
                        }
                        else if (!SpinWait.SpinUntil(() => completed, TimeSpan.FromSeconds(1)))
                        {
                            throw new TimeoutException();
                        }
                    },
                    // Parallel actions
                    () => cancelationSource.Cancel(),
                    () => deferred.Resolve(1),
                    () => action(promise.ConfigureAwait(configureAwaitType), cancelationToken)
                        .Finally(() =>
                        {
                            TestHelper.AssertCallbackContext(
                                configureAwaitType == ConfigureAwaitType.Foreground || configureAwaitType == ConfigureAwaitType.Explicit ? SynchronizationType.Synchronous : TestHelper.backgroundType,
                                SynchronizationType.Background,
                                foregroundThread);
                            completed = true; // State of the promise is indeterminable, just make sure it completes.
                        })
                        .Forget()
                );
            }
        }

        [Test]
        public void Catch_PromiseMayBeRejectedAndAwaitedAndCallbackCanceledConcurrently_void(
            [Values] ConfigureAwaitType configureAwaitType)
        {
            var cancelationSource = default(CancelationSource);
            var deferred = default(Promise.Deferred);
            var cancelationToken = default(CancelationToken);
            var promise = default(Promise);
            bool completed = false;

            Thread foregroundThread = Thread.CurrentThread;

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
                        TestHelper.ExecuteForegroundCallbacks();
                        cancelationSource.Dispose();
                        if (configureAwaitType != (ConfigureAwaitType) TestHelper.backgroundType)
                        {
                            Assert.IsTrue(completed);
                        }
                        else if (!SpinWait.SpinUntil(() => completed, TimeSpan.FromSeconds(1)))
                        {
                            throw new TimeoutException();
                        }
                    },
                    // Parallel actions
                    () => cancelationSource.Cancel(),
                    () => deferred.Reject(rejectValue),
                    () => action(promise.ConfigureAwait(configureAwaitType), cancelationToken)
                        .Finally(() =>
                        {
                            TestHelper.AssertCallbackContext(
                                configureAwaitType == ConfigureAwaitType.Foreground || configureAwaitType == ConfigureAwaitType.Explicit ? SynchronizationType.Synchronous : TestHelper.backgroundType,
                                SynchronizationType.Background,
                                foregroundThread);
                            completed = true; // State of the promise is indeterminable, just make sure it completes.
                        })
                        .Forget()
                );
            }
        }

        [Test]
        public void Catch_PromiseMayBeRejectedAndAwaitedAndCallbackCanceledConcurrently_T(
            [Values] ConfigureAwaitType configureAwaitType)
        {
            var cancelationSource = default(CancelationSource);
            var deferred = default(Promise<int>.Deferred);
            var cancelationToken = default(CancelationToken);
            var promise = default(Promise<int>);
            bool completed = false;

            Thread foregroundThread = Thread.CurrentThread;

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
                        TestHelper.ExecuteForegroundCallbacks();
                        cancelationSource.Dispose();
                        if (configureAwaitType != (ConfigureAwaitType) TestHelper.backgroundType)
                        {
                            Assert.IsTrue(completed);
                        }
                        else if (!SpinWait.SpinUntil(() => completed, TimeSpan.FromSeconds(1)))
                        {
                            throw new TimeoutException();
                        }
                    },
                    // Parallel actions
                    () => cancelationSource.Cancel(),
                    () => deferred.Reject(rejectValue),
                    () => action(promise.ConfigureAwait(configureAwaitType), cancelationToken)
                        .Finally(() =>
                        {
                            TestHelper.AssertCallbackContext(
                                configureAwaitType == ConfigureAwaitType.Foreground || configureAwaitType == ConfigureAwaitType.Explicit ? SynchronizationType.Synchronous : TestHelper.backgroundType,
                                SynchronizationType.Background,
                                foregroundThread);
                            completed = true; // State of the promise is indeterminable, just make sure it completes.
                        })
                        .Forget()
                );
            }
        }

        [Test]
        public void CatchCancelation_PromiseMayBeCanceledAndAwaitedAndCallbackCanceledConcurrently_void(
            [Values] ConfigureAwaitType configureAwaitType)
        {
            var cancelationSource = default(CancelationSource);
            var deferred = default(Promise.Deferred);
            var cancelationToken = default(CancelationToken);
            var promise = default(Promise);
            bool completed = false;

            Thread foregroundThread = Thread.CurrentThread;

            var threadHelper = new ThreadHelper();
            foreach (var action in new Func<Promise, CancelationToken, Promise>[]
                {
                    (p, token) => p.CatchCancelation(() => { }, token),
                    (p, token) => p.CatchCancelation(1, cv => { }, token),
                    (p, token) => p.CatchCancelation(() => Promise.Resolved(), token),
                    (p, token) => p.CatchCancelation(1, cv => Promise.Resolved(), token),
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
                        TestHelper.ExecuteForegroundCallbacks();
                        cancelationSource.Dispose();
                        if (configureAwaitType != (ConfigureAwaitType) TestHelper.backgroundType)
                        {
                            Assert.IsTrue(completed);
                        }
                        else if (!SpinWait.SpinUntil(() => completed, TimeSpan.FromSeconds(1)))
                        {
                            throw new TimeoutException();
                        }
                    },
                    // Parallel actions
                    () => cancelationSource.Cancel(),
                    () => deferred.Cancel(),
                    () => action(promise.ConfigureAwait(configureAwaitType), cancelationToken)
                        .Finally(() =>
                        {
                            TestHelper.AssertCallbackContext(
                                configureAwaitType == ConfigureAwaitType.Foreground || configureAwaitType == ConfigureAwaitType.Explicit ? SynchronizationType.Synchronous : TestHelper.backgroundType,
                                SynchronizationType.Background,
                                foregroundThread);
                            completed = true; // State of the promise is indeterminable, just make sure it completes.
                        })
                        .Forget()
                );
            }
        }

        [Test]
        public void CatchCancelation_PromiseMayBeCanceledAndAwaitedAndCallbackCanceledConcurrently_T(
            [Values] ConfigureAwaitType configureAwaitType)
        {
            var cancelationSource = default(CancelationSource);
            var deferred = default(Promise<int>.Deferred);
            var cancelationToken = default(CancelationToken);
            var promise = default(Promise<int>);
            bool completed = false;

            Thread foregroundThread = Thread.CurrentThread;

            var threadHelper = new ThreadHelper();
            foreach (var action in new Func<Promise<int>, CancelationToken, Promise<int>>[]
                {
                    (p, token) => p.CatchCancelation(() => 1, token),
                    (p, token) => p.CatchCancelation(1, cv => 1, token),
                    (p, token) => p.CatchCancelation(() => Promise.Resolved(1), token),
                    (p, token) => p.CatchCancelation(1, cv => Promise.Resolved(1), token),
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
                        TestHelper.ExecuteForegroundCallbacks();
                        cancelationSource.Dispose();
                        if (configureAwaitType != (ConfigureAwaitType) TestHelper.backgroundType)
                        {
                            Assert.IsTrue(completed);
                        }
                        else if (!SpinWait.SpinUntil(() => completed, TimeSpan.FromSeconds(1)))
                        {
                            throw new TimeoutException();
                        }
                    },
                    // Parallel actions
                    () => cancelationSource.Cancel(),
                    () => deferred.Cancel(),
                    () => action(promise.ConfigureAwait(configureAwaitType), cancelationToken)
                        .Finally(() =>
                        {
                            TestHelper.AssertCallbackContext(
                                configureAwaitType == ConfigureAwaitType.Foreground || configureAwaitType == ConfigureAwaitType.Explicit ? SynchronizationType.Synchronous : TestHelper.backgroundType,
                                SynchronizationType.Background,
                                foregroundThread);
                            completed = true; // State of the promise is indeterminable, just make sure it completes.
                        })
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
                    deferred.Promise
                        .SubscribeProgress(progressHelper, cancelationSource.Token)
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
                    deferred.Promise
                        .SubscribeProgress(progressHelper, cancelationSource.Token)
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
                () => promise
                    .SubscribeProgress(progressHelper, cancelationToken)
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
                () => promise
                    .SubscribeProgress(progressHelper, cancelationToken)
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
                    deferred.Promise
                        .ThenDuplicate(cancelationSource1.Token)
                        .ThenDuplicate()
                        .ThenDuplicate(cancelationSource2.Token)
                        .ThenDuplicate()
                        .SubscribeProgress(progressHelper)
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
                    deferred.Promise
                        .ThenDuplicate(cancelationSource1.Token)
                        .ThenDuplicate()
                        .ThenDuplicate(cancelationSource2.Token)
                        .ThenDuplicate()
                        .SubscribeProgress(progressHelper)
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