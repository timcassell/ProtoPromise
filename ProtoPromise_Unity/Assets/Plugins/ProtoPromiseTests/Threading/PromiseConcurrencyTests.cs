#if !UNITY_WEBGL

#if !PROTO_PROMISE_PROGRESS_DISABLE
#define PROMISE_PROGRESS
#else
#undef PROMISE_PROGRESS
#endif

using NUnit.Framework;
using Proto.Promises;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ProtoPromiseTests.Threading
{
    public class PromiseConcurrencyTests
    {
        const string rejectValue = "Fail";

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
        public void PreservedPromiseWithReferenceBacking_ForgetMayOnlyBeCalledOnce_void()
        {
            var deferred = Promise.NewDeferred();
            var promise = deferred.Promise.Preserve();

            int successCount = 0, invalidCount = 0;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(
                () =>
                {
                    try
                    {
                        promise.Forget();
                        Interlocked.Increment(ref successCount);
                    }
                    catch (Proto.Promises.InvalidOperationException)
                    {
                        Interlocked.Increment(ref invalidCount);
                    }
                }
            );

            deferred.Resolve();
            Assert.AreEqual(1, successCount);
            Assert.AreEqual(ThreadHelper.multiExecutionCount - 1, invalidCount);
        }

        [Test]
        public void PreservedPromiseWithReferenceBacking_ForgetMayOnlyBeCalledOnce_T()
        {
            var deferred = Promise.NewDeferred<int>();
            var promise = deferred.Promise.Preserve();

            int successCount = 0, invalidCount = 0;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(
                () =>
                {
                    try
                    {
                        promise.Forget();
                        Interlocked.Increment(ref successCount);
                    }
                    catch (Proto.Promises.InvalidOperationException)
                    {
                        Interlocked.Increment(ref invalidCount);
                    }
                }
            );

            deferred.Resolve(1);
            Assert.AreEqual(1, successCount);
            Assert.AreEqual(ThreadHelper.multiExecutionCount - 1, invalidCount);
        }

        [Test]
        public void PreservedPromiseWithReferenceBacking_DuplicateCalledConcurrentlyAlwaysReturnsUnique_void()
        {
            var deferred = Promise.NewDeferred();
            var promise = deferred.Promise.Preserve();

            var duplicates = new ConcurrentBag<Promise>();

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(
                () =>
                {
                    var duplicate = promise.Duplicate();
                    duplicates.Add(duplicate);
                    duplicate.Forget();
                }
            );

            promise.Forget();
            deferred.Resolve();

            var diffChecker = new HashSet<Promise>();
            if (!duplicates.All(diffChecker.Add))
            {
                Assert.Fail("Duplicate returned at least one of the same promise instance. Duplicate should always return a unique instance from a reference-backed promise.");
            }
        }

        [Test]
        public void PreservedPromiseWithReferenceBacking_DuplicateCalledConcurrentlyAlwaysReturnsUnique_T()
        {
            var deferred = Promise.NewDeferred<int>();
            var promise = deferred.Promise.Preserve();

            var duplicates = new ConcurrentBag<Promise>();

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(
                () =>
                {
                    var duplicate = promise.Duplicate();
                    duplicates.Add(duplicate);
                    duplicate.Forget();
                }
            );

            promise.Forget();
            deferred.Resolve(1);

            var diffChecker = new HashSet<Promise>();
            if (!duplicates.All(diffChecker.Add))
            {
                Assert.Fail("Duplicate returned at least one of the same promise instance. Duplicate should always return a unique instance from a reference-backed promise.");
            }
        }

        [Test]
        public void PreservedPromiseWithReferenceBacking_PreserveCalledConcurrentlyAlwaysReturnsUnique_void()
        {
            var deferred = Promise.NewDeferred();
            var promise = deferred.Promise.Preserve();

            var duplicates = new ConcurrentBag<Promise>();

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(
                () =>
                {
                    var preserved = promise.Preserve();
                    duplicates.Add(preserved);
                    preserved.Forget();
                }
            );

            promise.Forget();
            deferred.Resolve();

            var diffChecker = new HashSet<Promise>();
            if (!duplicates.All(diffChecker.Add))
            {
                Assert.Fail("Duplicate returned at least one of the same promise instance. Duplicate should always return a unique instance from a reference-backed promise.");
            }
        }

        [Test]
        public void PreservedPromiseWithReferenceBacking_PreserveCalledConcurrentlyAlwaysReturnsUnique_T()
        {
            var deferred = Promise.NewDeferred<int>();
            var promise = deferred.Promise.Preserve();

            var duplicates = new ConcurrentBag<Promise>();

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(
                () =>
                {
                    var preserved = promise.Preserve();
                    duplicates.Add(preserved);
                    preserved.Forget();
                }
            );

            promise.Forget();
            deferred.Resolve(1);

            var diffChecker = new HashSet<Promise>();
            if (!duplicates.All(diffChecker.Add))
            {
                Assert.Fail("Duplicate returned at least one of the same promise instance. Duplicate should always return a unique instance from a reference-backed promise.");
            }
        }

#if PROMISE_PROGRESS
        [Test]
        public void PromiseProgressMayBeCalledConcurrently_Resolved_void(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            int invokedCount = 0;
            Promise promise = Promise.Resolved().Preserve();

            ProgressHelper[] progressHelpers = new ProgressHelper[ThreadHelper.multiExecutionCount];
            for (int i = 0; i < progressHelpers.Length; ++i)
            {
                progressHelpers[i] = new ProgressHelper(progressType, synchronizationType,
                    v => { Interlocked.Increment(ref invokedCount); }
                );
            }

            int index = -1;
            new ThreadHelper().ExecuteMultiActionParallel(
                () => promise
                    .SubscribeProgress(progressHelpers[Interlocked.Increment(ref index)])
                    .Forget()
            );
            promise.Forget();

            for (int i = 0; i < progressHelpers.Length; ++i)
            {
                progressHelpers[i].AssertCurrentProgress(1f, true, i == 0); // Only need to execute foreground the first time.
            }
            Assert.AreEqual(ThreadHelper.multiExecutionCount, invokedCount);
        }

        [Test]
        public void PromiseProgressMayBeCalledConcurrently_Resolved_T(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            int invokedCount = 0;
            Promise promise = Promise.Resolved(1).Preserve();

            ProgressHelper[] progressHelpers = new ProgressHelper[ThreadHelper.multiExecutionCount];
            for (int i = 0; i < progressHelpers.Length; ++i)
            {
                progressHelpers[i] = new ProgressHelper(progressType, synchronizationType,
                    v => { Interlocked.Increment(ref invokedCount); }
                );
            }

            int index = -1;
            new ThreadHelper().ExecuteMultiActionParallel(
                () => promise
                    .SubscribeProgress(progressHelpers[Interlocked.Increment(ref index)])
                    .Forget()
            );
            promise.Forget();

            for (int i = 0; i < progressHelpers.Length; ++i)
            {
                progressHelpers[i].AssertCurrentProgress(1f, true, i == 0); // Only need to execute foreground the first time.
            }
            Assert.AreEqual(ThreadHelper.multiExecutionCount, invokedCount);
        }

        [Test]
        public void PromiseProgressMayBeSubscribedWhilePromiseIsCompletedAndProgressIsReportedConcurrently_Pending_void(
            [Values] ActionPlace subscribePlace,
            [Values] ActionPlace reportPlace,
            [Values] ActionPlace completePlace,
            [Values] CompleteType completeType,
            // Testing all ProgressTypes takes too long, and they are tested in other tests.
            [Values(ProgressType.Interface)] ProgressType progressType,
            [Values(SynchronizationType.Synchronous, SynchronizationType.Foreground, SynchronizationType.Background)] SynchronizationType synchronizationType)
        {
            int expectedInvokes = completeType == CompleteType.Resolve ? 10 : 0;
            if (subscribePlace == ActionPlace.InSetup)
            {
                expectedInvokes += 10;
                if (reportPlace == ActionPlace.InSetup)
                {
                    // Implementation detail: when progress is reported with the same value, the callback will not be invoked. This behavior is not guaranteed by the API.
                    expectedInvokes += 10;
                }
            }
            else // parallel or teardown
            {
                // If all types are parallel, we can't know if it will be extra invokes.
                // We only know there are extra invokes if complete (and report) come after subscribe.
                if (completeType == CompleteType.Resolve && completePlace == ActionPlace.InTeardown)
                {
                    expectedInvokes += 10;
                }
            }

            var deferred = default(Promise.Deferred);
            var promise = default(Promise);
            var cancelationSource = default(CancelationSource);
            int invokedCount = 0;

            ProgressHelper[] progressHelpers = new ProgressHelper[10];

            Action AssertInvokes = completeType != CompleteType.Resolve && completePlace == ActionPlace.InSetup
                // If the promise is rejected or canceled in the setup, we know that it should not be invoked more times than expected.
                ? (Action) (() => Assert.AreEqual(expectedInvokes, invokedCount))
                // OnProgress is potentially invoked from background threads concurrently,
                // but each TryReportProgress call is not guaranteed to invoke the callback,
                // so we can't know how many onProgress invokes actually occurred, so just make sure it happened at least expectedInvokes times.
                : () => Assert.GreaterOrEqual(invokedCount, expectedInvokes);

            List<Action> parallelActions = new List<Action>();

            int index = -1;
            var progressSubscriber = ParallelActionTestHelper.Create(
                subscribePlace,
                10,
                () => promise
                    .SubscribeProgress(progressHelpers[Interlocked.Increment(ref index)])
                    .Catch((string error) => { Assert.AreEqual(rejectValue, error); })
                    .Forget()
            );
            progressSubscriber.MaybeAddParallelAction(parallelActions);

            var progressReporter = ParallelActionTestHelper.Create(
                reportPlace,
                10,
                () => deferred.TryReportProgress(0.5f)
            );
            progressReporter.MaybeAddParallelAction(parallelActions);

            var tryCompleter = TestHelper.GetTryCompleterVoid(completeType, rejectValue);
            var promiseCompleter = ParallelActionTestHelper.Create(
                completePlace,
                10,
                () => tryCompleter(deferred, cancelationSource)
            );
            promiseCompleter.MaybeAddParallelAction(parallelActions);

            bool waitForSubscribeSetup = subscribePlace == ActionPlace.InSetup;
            bool waitForReportSetup = subscribePlace == ActionPlace.InSetup && reportPlace == ActionPlace.InSetup;
            Action setupAction = () =>
            {
                index = -1;
                for (int i = 0; i < progressHelpers.Length; ++i)
                {
                    progressHelpers[i] = new ProgressHelper(progressType, synchronizationType,
                        v => { Interlocked.Increment(ref invokedCount); }
                    );
                    progressHelpers[i].MaybeEnterLock();
                }
                invokedCount = 0;
                deferred = TestHelper.GetNewDeferredVoid(completeType, out cancelationSource);
                promise = deferred.Promise.Preserve();
                progressSubscriber.Setup();
                if (waitForSubscribeSetup)
                {
                    for (int i = 0; i < progressHelpers.Length; ++i)
                    {
                        progressHelpers[i].AssertCurrentProgress(0f, true, i == 0); // Only need to execute foreground the first time.
                        progressHelpers[i].PrepareForInvoke();
                    }
                }
                progressReporter.Setup();
                if (waitForReportSetup)
                {
                    for (int i = 0; i < progressHelpers.Length; ++i)
                    {
                        progressHelpers[i].AssertCurrentProgress(0.5f, true, i == 0); // Only need to execute foreground the first time.
                        progressHelpers[i].PrepareForInvoke();
                    }
                }
                promiseCompleter.Setup();
            };
            bool waitForSubscribeTeardown = !waitForSubscribeSetup && completePlace == ActionPlace.InTeardown;
            Action teardownAction = () =>
            {
                progressSubscriber.Teardown();
                if (waitForSubscribeTeardown)
                {
                    for (int i = 0; i < progressHelpers.Length; ++i)
                    {
                        progressHelpers[i].MaybeWaitForInvoke(true, i == 0, TimeSpan.FromSeconds(ThreadHelper.multiExecutionCount)); // Only need to execute foreground the first time.
                        progressHelpers[i].PrepareForInvoke();
                    }
                }
                progressReporter.Teardown();
                // Not checking for report invoke here because of background thread race conditions.
                promiseCompleter.Teardown();
                cancelationSource.TryDispose();
                promise.Forget();

                for (int i = 0; i < progressHelpers.Length; ++i)
                {
                    // Progress may have been invoked simultaneously, so we can't know what the progress value is here.
                    // We must only WaitForInvoke instead of AssertCurrentProgress.
                    progressHelpers[i].MaybeWaitForInvoke(completeType == CompleteType.Resolve, i == 0, TimeSpan.FromSeconds(ThreadHelper.multiExecutionCount)); // Only need to execute foreground the first time.
                    progressHelpers[i].MaybeExitLock();
                }
                AssertInvokes();
            };
            new ThreadHelper().ExecuteParallelActions(ThreadHelper.multiExecutionCount,
                setupAction,
                teardownAction,
                parallelActions.ToArray()
            );
        }

        [Test]
        public void PromiseProgressMayBeSubscribedWhilePromiseIsCompletedAndProgressIsReportedConcurrently_Pending_T(
            [Values] ActionPlace subscribePlace,
            [Values] ActionPlace reportPlace,
            [Values] ActionPlace completePlace,
            [Values] CompleteType completeType,
            // Testing all ProgressTypes takes too long, and they are tested in other tests.
            [Values(ProgressType.Interface)] ProgressType progressType,
            [Values(SynchronizationType.Synchronous, SynchronizationType.Foreground, SynchronizationType.Background)] SynchronizationType synchronizationType)
        {
            int expectedInvokes = completeType == CompleteType.Resolve ? 10 : 0;
            if (subscribePlace == ActionPlace.InSetup)
            {
                expectedInvokes += 10;
                if (reportPlace == ActionPlace.InSetup)
                {
                    // Implementation detail: when progress is reported with the same value, the callback will not be invoked. This behavior is not guaranteed by the API.
                    expectedInvokes += 10;
                }
            }
            else // parallel or teardown
            {
                // If all types are parallel, we can't know if it will be extra invokes.
                // We only know there are extra invokes if complete (and report) come after subscribe.
                if (completeType == CompleteType.Resolve && completePlace == ActionPlace.InTeardown)
                {
                    expectedInvokes += 10;
                }
            }

            var deferred = default(Promise<int>.Deferred);
            var promise = default(Promise<int>);
            var cancelationSource = default(CancelationSource);
            int invokedCount = 0;

            ProgressHelper[] progressHelpers = new ProgressHelper[10];

            Action AssertInvokes = completeType != CompleteType.Resolve && completePlace == ActionPlace.InSetup
                // If the promise is rejected or canceled in the setup, we know that it should not be invoked more times than expected.
                ? (Action) (() => Assert.AreEqual(expectedInvokes, invokedCount))
                // OnProgress is potentially invoked from background threads concurrently,
                // but each TryReportProgress call is not guaranteed to invoke the callback,
                // so we can't know how many onProgress invokes actually occurred, so just make sure it happened at least expectedInvokes times.
                : () => Assert.GreaterOrEqual(invokedCount, expectedInvokes);

            List<Action> parallelActions = new List<Action>();

            int index = -1;
            var progressSubscriber = ParallelActionTestHelper.Create(
                subscribePlace,
                10,
                () => promise
                    .SubscribeProgress(progressHelpers[Interlocked.Increment(ref index)])
                    .Catch((string error) => { Assert.AreEqual(rejectValue, error); })
                    .Forget()
            );
            progressSubscriber.MaybeAddParallelAction(parallelActions);

            var progressReporter = ParallelActionTestHelper.Create(
                reportPlace,
                10,
                () => deferred.TryReportProgress(0.5f)
            );
            progressReporter.MaybeAddParallelAction(parallelActions);

            var tryCompleter = TestHelper.GetTryCompleterT(completeType, 1, rejectValue);
            var promiseCompleter = ParallelActionTestHelper.Create(
                completePlace,
                10,
                () => tryCompleter(deferred, cancelationSource)
            );
            promiseCompleter.MaybeAddParallelAction(parallelActions);

            bool waitForSubscribeSetup = subscribePlace == ActionPlace.InSetup;
            bool waitForReportSetup = subscribePlace == ActionPlace.InSetup && reportPlace == ActionPlace.InSetup;
            Action setupAction = () =>
            {
                index = -1;
                for (int i = 0; i < progressHelpers.Length; ++i)
                {
                    progressHelpers[i] = new ProgressHelper(progressType, synchronizationType,
                        v => { Interlocked.Increment(ref invokedCount); }
                    );
                    progressHelpers[i].MaybeEnterLock();
                }
                invokedCount = 0;
                deferred = TestHelper.GetNewDeferredT<int>(completeType, out cancelationSource);
                promise = deferred.Promise.Preserve();
                progressSubscriber.Setup();
                if (waitForSubscribeSetup)
                {
                    for (int i = 0; i < progressHelpers.Length; ++i)
                    {
                        progressHelpers[i].AssertCurrentProgress(0f, true, i == 0); // Only need to execute foreground the first time.
                        progressHelpers[i].PrepareForInvoke();
                    }
                }
                progressReporter.Setup();
                if (waitForReportSetup)
                {
                    for (int i = 0; i < progressHelpers.Length; ++i)
                    {
                        progressHelpers[i].AssertCurrentProgress(0.5f, true, i == 0); // Only need to execute foreground the first time.
                        progressHelpers[i].PrepareForInvoke();
                    }
                }
                promiseCompleter.Setup();
            };
            bool waitForSubscribeTeardown = !waitForSubscribeSetup && completePlace == ActionPlace.InTeardown;
            Action teardownAction = () =>
            {
                progressSubscriber.Teardown();
                if (waitForSubscribeTeardown)
                {
                    for (int i = 0; i < progressHelpers.Length; ++i)
                    {
                        progressHelpers[i].MaybeWaitForInvoke(true, i == 0, TimeSpan.FromSeconds(ThreadHelper.multiExecutionCount)); // Only need to execute foreground the first time.
                        progressHelpers[i].PrepareForInvoke();
                    }
                }
                progressReporter.Teardown();
                // Not checking for report invoke here because of background thread race conditions.
                promiseCompleter.Teardown();
                cancelationSource.TryDispose();
                promise.Forget();

                for (int i = 0; i < progressHelpers.Length; ++i)
                {
                    // Progress may have been invoked simultaneously, so we can't know what the progress value is here.
                    // We must only WaitForInvoke instead of AssertCurrentProgress.
                    progressHelpers[i].MaybeWaitForInvoke(completeType == CompleteType.Resolve, i == 0, TimeSpan.FromSeconds(ThreadHelper.multiExecutionCount)); // Only need to execute foreground the first time.
                    progressHelpers[i].MaybeExitLock();
                }
                AssertInvokes();
            };
            new ThreadHelper().ExecuteParallelActions(ThreadHelper.multiExecutionCount,
                setupAction,
                teardownAction,
                parallelActions.ToArray()
            );
        }
#endif

        [Test]
        public void PromiseReturnedInCallbackMayBeCompletedConcurrently_void(
            [Values] CompleteType completeType)
        {
            var returnDeferred = default(Promise.Deferred);
            var returnPromise = default(Promise);
            var cancelationSource = default(CancelationSource);
            Action threadBarrier = null;
            var tryCompleter = TestHelper.GetTryCompleterVoid(completeType, rejectValue);

            Promise.State result = Promise.State.Pending;

            var actions = TestHelper.ActionsReturningPromiseVoid(() =>
            {
                threadBarrier();
                return returnPromise;
            });
            var threadHelper = new ThreadHelper();
            foreach (var action in actions)
            {
                threadHelper.ExecuteParallelActionsWithOffsetsAndSetup(
                    setup: () =>
                    {
                        result = Promise.State.Pending;
                        returnDeferred = TestHelper.GetNewDeferredVoid(completeType, out cancelationSource);
                        returnPromise = returnDeferred.Promise;
                    },
                    parallelActionsSetup: new Action<Action>[]
                    {
                        barrierAction =>
                        {
                            threadBarrier = barrierAction;
                            action.Invoke()
                                .ContinueWith(r => result = r.State)
                                .Forget();
                        }
                    },
                    parallelActions: new Action[]
                    {
                        () => tryCompleter(returnDeferred, cancelationSource)
                    },
                    teardown: () =>
                    {
                        cancelationSource.TryDispose();
                        
                        Assert.AreNotEqual(Promise.State.Pending, result);
                        switch (completeType)
                        {
                            case CompleteType.Resolve:
                                Assert.AreEqual(Promise.State.Resolved, result);
                                break;
                            case CompleteType.Reject:
                                Assert.AreEqual(Promise.State.Rejected, result);
                                break;
                            case CompleteType.Cancel:
                            case CompleteType.CancelFromToken:
                                Assert.AreEqual(Promise.State.Canceled, result);
                                break;
                        }
                    }
                );
            }
        }

        [Test]
        public void PromiseReturnedInCallbackMayBeCompletedConcurrently_T(
            [Values] CompleteType completeType)
        {
            var returnDeferred = default(Promise<int>.Deferred);
            var returnPromise = default(Promise<int>);
            var cancelationSource = default(CancelationSource);
            Action threadBarrier = null;
            var tryCompleter = TestHelper.GetTryCompleterT(completeType, 1, rejectValue);

            Promise.State result = Promise.State.Pending;

            var actions = TestHelper.ActionsReturningPromiseT(() =>
            {
                threadBarrier();
                return returnPromise;
            });
            var threadHelper = new ThreadHelper();
            foreach (var action in actions)
            {
                threadHelper.ExecuteParallelActionsWithOffsetsAndSetup(
                    setup: () =>
                    {
                        result = Promise.State.Pending;
                        returnDeferred = TestHelper.GetNewDeferredT<int>(completeType, out cancelationSource);
                        returnPromise = returnDeferred.Promise;
                    },
                    parallelActionsSetup: new Action<Action>[]
                    {
                        barrierAction =>
                        {
                            threadBarrier = barrierAction;
                            action.Invoke()
                                .ContinueWith(r => result = r.State)
                                .Forget();
                        }
                    },
                    parallelActions: new Action[]
                    {
                        () => tryCompleter(returnDeferred, cancelationSource)
                    },
                    teardown: () =>
                    {
                        cancelationSource.TryDispose();

                        Assert.AreNotEqual(Promise.State.Pending, result);
                        switch (completeType)
                        {
                            case CompleteType.Resolve:
                                Assert.AreEqual(Promise.State.Resolved, result);
                                break;
                            case CompleteType.Reject:
                                Assert.AreEqual(Promise.State.Rejected, result);
                                break;
                            case CompleteType.Cancel:
                            case CompleteType.CancelFromToken:
                                Assert.AreEqual(Promise.State.Canceled, result);
                                break;
                        }
                    }
                );
            }
        }

        [Test]
        public void PreservedPromiseThenMayBeCalledConcurrently_PendingThenResolved_void()
        {
            int invokedCount = 0;
            var deferred = Promise.NewDeferred();
            var promise = deferred.Promise.Preserve();

            var resolveActions = TestHelper.ResolveActionsVoid(() => Interlocked.Increment(ref invokedCount));
            var thenActions = TestHelper.ThenActionsVoid(() => Interlocked.Increment(ref invokedCount), null);
            var threadHelper = new ThreadHelper();
            foreach (var action in resolveActions.Concat(thenActions))
            {
                threadHelper.ExecuteMultiActionParallel(() => action(promise));
            }
            promise.Forget();
            deferred.Resolve();
            Assert.AreEqual(ThreadHelper.multiExecutionCount * TestHelper.resolveVoidCallbacks / TestHelper.callbacksMultiplier, invokedCount);
        }

        [Test]
        public void PreservedPromiseThenMayBeCalledConcurrently_Resolved_void()
        {
            int invokedCount = 0;
            var promise = Promise.Resolved().Preserve();

            var resolveActions = TestHelper.ResolveActionsVoid(() => Interlocked.Increment(ref invokedCount));
            var thenActions = TestHelper.ThenActionsVoid(() => Interlocked.Increment(ref invokedCount), null);
            var threadHelper = new ThreadHelper();
            foreach (var action in resolveActions.Concat(thenActions))
            {
                threadHelper.ExecuteMultiActionParallel(() => action(promise));
            }
            promise.Forget();
            Assert.AreEqual(ThreadHelper.multiExecutionCount * TestHelper.resolveVoidCallbacks / TestHelper.callbacksMultiplier, invokedCount);
        }

        [Test]
        public void PreservedPromiseThenMayBeCalledConcurrently_PendingThenResolved_T()
        {
            int invokedCount = 0;
            var deferred = Promise.NewDeferred<int>();
            var promise = deferred.Promise.Preserve();

            var resolveActions = TestHelper.ResolveActions<int>(v => Interlocked.Increment(ref invokedCount));
            var thenActions = TestHelper.ThenActions<int>(v => Interlocked.Increment(ref invokedCount), null);
            var threadHelper = new ThreadHelper();
            foreach (var action in resolveActions.Concat(thenActions))
            {
                threadHelper.ExecuteMultiActionParallel(() => action(promise));
            }
            promise.Forget();
            deferred.Resolve(1);
            Assert.AreEqual(ThreadHelper.multiExecutionCount * TestHelper.resolveTCallbacks / TestHelper.callbacksMultiplier, invokedCount);
        }

        [Test]
        public void PreservedPromiseThenMayBeCalledConcurrently_Resolved_T()
        {
            int invokedCount = 0;
            var promise = Promise.Resolved(1).Preserve();

            var resolveActions = TestHelper.ResolveActions<int>(v => Interlocked.Increment(ref invokedCount));
            var thenActions = TestHelper.ThenActions<int>(v => Interlocked.Increment(ref invokedCount), null);
            var threadHelper = new ThreadHelper();
            foreach (var action in resolveActions.Concat(thenActions))
            {
                threadHelper.ExecuteMultiActionParallel(() => action(promise));
            }
            promise.Forget();
            Assert.AreEqual(ThreadHelper.multiExecutionCount * TestHelper.resolveTCallbacks / TestHelper.callbacksMultiplier, invokedCount);
        }

        [Test]
        public void PreservedPromiseThenMayBeCalledConcurrently_PendingThenRejected_void()
        {
            int invokedCount = 0;
            var deferred = Promise.NewDeferred();
            var promise = deferred.Promise.Preserve();

            var catchActions = TestHelper.CatchActionsVoid(() => Interlocked.Increment(ref invokedCount));
            var thenActions = TestHelper.ThenActionsVoid(null, () => Interlocked.Increment(ref invokedCount));
            var threadHelper = new ThreadHelper();
            foreach (var action in catchActions.Concat(thenActions))
            {
                threadHelper.ExecuteMultiActionParallel(() => action(promise));
            }
            promise.Forget();
            deferred.Reject("Reject");
            Assert.AreEqual(ThreadHelper.multiExecutionCount * TestHelper.rejectVoidCallbacks / TestHelper.callbacksMultiplier, invokedCount);
        }

        [Test]
        public void PreservedPromiseThenMayBeCalledConcurrently_Rejected_void()
        {
            int invokedCount = 0;
            var promise = Promise.Rejected("Reject").Preserve();

            var catchActions = TestHelper.CatchActionsVoid(() => Interlocked.Increment(ref invokedCount));
            var thenActions = TestHelper.ThenActionsVoid(null, () => Interlocked.Increment(ref invokedCount));
            var threadHelper = new ThreadHelper();
            foreach (var action in catchActions.Concat(thenActions))
            {
                threadHelper.ExecuteMultiActionParallel(() => action(promise));
            }
            promise.Forget();
            Assert.AreEqual(ThreadHelper.multiExecutionCount * TestHelper.rejectVoidCallbacks / TestHelper.callbacksMultiplier, invokedCount);
        }

        [Test]
        public void PreservedPromiseThenMayBeCalledConcurrently_PendingThenRejected_T()
        {
            int invokedCount = 0;
            var deferred = Promise.NewDeferred<int>();
            var promise = deferred.Promise.Preserve();

            var catchActions = TestHelper.CatchActions<int>(() => Interlocked.Increment(ref invokedCount));
            var thenActions = TestHelper.ThenActions<int>(null, () => Interlocked.Increment(ref invokedCount));
            var threadHelper = new ThreadHelper();
            foreach (var action in catchActions.Concat(thenActions))
            {
                threadHelper.ExecuteMultiActionParallel(() => action(promise));
            }
            promise.Forget();
            deferred.Reject("Reject");
            Assert.AreEqual(ThreadHelper.multiExecutionCount * TestHelper.rejectTCallbacks / TestHelper.callbacksMultiplier, invokedCount);
        }

        [Test]
        public void PreservedPromiseThenMayBeCalledConcurrently_Rejected_T()
        {
            int invokedCount = 0;
            var promise = Promise<int>.Rejected("Reject").Preserve();

            var catchActions = TestHelper.CatchActions<int>(() => Interlocked.Increment(ref invokedCount));
            var thenActions = TestHelper.ThenActions<int>(null, () => Interlocked.Increment(ref invokedCount));
            var threadHelper = new ThreadHelper();
            foreach (var action in catchActions.Concat(thenActions))
            {
                threadHelper.ExecuteMultiActionParallel(() => action(promise));
            }
            promise.Forget();
            Assert.AreEqual(ThreadHelper.multiExecutionCount * TestHelper.rejectTCallbacks / TestHelper.callbacksMultiplier, invokedCount);
        }

        [Test]
        public void PromiseCatchCancelationnMayBeCalledConcurrently_PendingThenCanceled_void()
        {
            int invokedCount = 0;
            var cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred(cancelationSource.Token);
            var promise = deferred.Promise.Preserve();

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(() => promise.CatchCancelation(() => Interlocked.Increment(ref invokedCount)).Forget());
            threadHelper.ExecuteMultiActionParallel(() => promise.CatchCancelation(1, cv => Interlocked.Increment(ref invokedCount)).Forget());
            promise.Forget();
            cancelationSource.Cancel();
            Assert.AreEqual(ThreadHelper.multiExecutionCount * 2, invokedCount);

            cancelationSource.Dispose();
        }

        [Test]
        public void PromiseCatchCancelationnMayBeCalledConcurrently_Canceled_void()
        {
            int invokedCount = 0;
            var promise = Promise.Canceled().Preserve();

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(() => promise.CatchCancelation(() => Interlocked.Increment(ref invokedCount)).Forget());
            threadHelper.ExecuteMultiActionParallel(() => promise.CatchCancelation(1, cv => Interlocked.Increment(ref invokedCount)).Forget());
            promise.Forget();
            Assert.AreEqual(ThreadHelper.multiExecutionCount * 2, invokedCount);
        }

        [Test]
        public void PromiseCatchCancelationnMayBeCalledConcurrently_PendingThenCanceled_T()
        {
            int invokedCount = 0;
            var cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred<int>(cancelationSource.Token);
            var promise = deferred.Promise.Preserve();

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(() => promise.CatchCancelation(() => Interlocked.Increment(ref invokedCount)).Forget());
            threadHelper.ExecuteMultiActionParallel(() => promise.CatchCancelation(1, cv => Interlocked.Increment(ref invokedCount)).Forget());
            promise.Forget();
            cancelationSource.Cancel();
            Assert.AreEqual(ThreadHelper.multiExecutionCount * 2, invokedCount);

            cancelationSource.Dispose();
        }

        [Test]
        public void PromiseCatchCancelationnMayBeCalledConcurrently_Canceled_T()
        {
            int invokedCount = 0;
            var promise = Promise<int>.Canceled().Preserve();

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(() => promise.CatchCancelation(() => Interlocked.Increment(ref invokedCount)).Forget());
            threadHelper.ExecuteMultiActionParallel(() => promise.CatchCancelation(1, cv => Interlocked.Increment(ref invokedCount)).Forget());
            promise.Forget();
            Assert.AreEqual(ThreadHelper.multiExecutionCount * 2, invokedCount);
        }

        [Test]
        public void PreservedPromiseContinueWithMayBeCalledConcurrently_PendingThenResolved_void()
        {
            int invokedCount = 0;
            var deferred = Promise.NewDeferred();
            var promise = deferred.Promise.Preserve();

            var continueActions = TestHelper.ContinueWithActionsVoid(() => Interlocked.Increment(ref invokedCount));
            var threadHelper = new ThreadHelper();
            foreach (var action in continueActions)
            {
                threadHelper.ExecuteMultiActionParallel(() => action(promise));
            }
            promise.Forget();
            deferred.Resolve();
            Assert.AreEqual(ThreadHelper.multiExecutionCount * TestHelper.continueVoidCallbacks / TestHelper.callbacksMultiplier, invokedCount);
        }

        [Test]
        public void PreservedPromiseContinueWithMayBeCalledConcurrently_Resolved_void()
        {
            int invokedCount = 0;
            var promise = Promise.Resolved().Preserve();

            var continueActions = TestHelper.ContinueWithActionsVoid(() => Interlocked.Increment(ref invokedCount));
            var threadHelper = new ThreadHelper();
            foreach (var action in continueActions)
            {
                threadHelper.ExecuteMultiActionParallel(() => action(promise));
            }
            promise.Forget();
            Assert.AreEqual(ThreadHelper.multiExecutionCount * TestHelper.continueVoidCallbacks / TestHelper.callbacksMultiplier, invokedCount);
        }

        [Test]
        public void PreservedPromiseContinueWithMayBeCalledConcurrently_PendingThenResolved_T()
        {
            int invokedCount = 0;
            var deferred = Promise.NewDeferred<int>();
            var promise = deferred.Promise.Preserve();

            var continueActions = TestHelper.ContinueWithActions<int>(() => Interlocked.Increment(ref invokedCount));
            var threadHelper = new ThreadHelper();
            foreach (var action in continueActions)
            {
                threadHelper.ExecuteMultiActionParallel(() => action(promise));
            }
            promise.Forget();
            deferred.Resolve(1);
            Assert.AreEqual(ThreadHelper.multiExecutionCount * TestHelper.continueTCallbacks / TestHelper.callbacksMultiplier, invokedCount);
        }

        [Test]
        public void PreservedPromiseContinueWithMayBeCalledConcurrently_Resolved_T()
        {
            int invokedCount = 0;
            var promise = Promise.Resolved(1).Preserve();

            var continueActions = TestHelper.ContinueWithActions<int>(() => Interlocked.Increment(ref invokedCount));
            var threadHelper = new ThreadHelper();
            foreach (var action in continueActions)
            {
                threadHelper.ExecuteMultiActionParallel(() => action(promise));
            }
            promise.Forget();
            Assert.AreEqual(ThreadHelper.multiExecutionCount * TestHelper.continueTCallbacks / TestHelper.callbacksMultiplier, invokedCount);
        }

        [Test]
        public void PreservedPromiseContinueWithMayBeCalledConcurrently_PendingThenRejected_void()
        {
            int invokedCount = 0;
            var deferred = Promise.NewDeferred();
            var promise = deferred.Promise.Preserve();

            var continueActions = TestHelper.ContinueWithActionsVoid(() => Interlocked.Increment(ref invokedCount));
            var threadHelper = new ThreadHelper();
            foreach (var action in continueActions)
            {
                threadHelper.ExecuteMultiActionParallel(() => action(promise));
            }
            promise.Forget();
            deferred.Reject("Reject");
            Assert.AreEqual(ThreadHelper.multiExecutionCount * TestHelper.continueVoidCallbacks / TestHelper.callbacksMultiplier, invokedCount);
        }

        [Test]
        public void PreservedPromiseContinueWithMayBeCalledConcurrently_Rejected_void()
        {
            int invokedCount = 0;
            var promise = Promise.Rejected("Reject").Preserve();

            var continueActions = TestHelper.ContinueWithActionsVoid(() => Interlocked.Increment(ref invokedCount));
            var threadHelper = new ThreadHelper();
            foreach (var action in continueActions)
            {
                threadHelper.ExecuteMultiActionParallel(() => action(promise));
            }
            promise.Forget();
            Assert.AreEqual(ThreadHelper.multiExecutionCount * TestHelper.continueVoidCallbacks / TestHelper.callbacksMultiplier, invokedCount);
        }

        [Test]
        public void PreservedPromiseContinueWithMayBeCalledConcurrently_PendingThenRejected_T()
        {
            int invokedCount = 0;
            var deferred = Promise.NewDeferred<int>();
            var promise = deferred.Promise.Preserve();

            var continueActions = TestHelper.ContinueWithActions<int>(() => Interlocked.Increment(ref invokedCount));
            var threadHelper = new ThreadHelper();
            foreach (var action in continueActions)
            {
                threadHelper.ExecuteMultiActionParallel(() => action(promise));
            }
            promise.Forget();
            deferred.Reject("Reject");
            Assert.AreEqual(ThreadHelper.multiExecutionCount * TestHelper.continueTCallbacks / TestHelper.callbacksMultiplier, invokedCount);
        }

        [Test]
        public void PreservedPromiseContinueWithMayBeCalledConcurrently_Rejected_T()
        {
            int invokedCount = 0;
            var promise = Promise<int>.Rejected("Reject").Preserve();

            var continueActions = TestHelper.ContinueWithActions<int>(() => Interlocked.Increment(ref invokedCount));
            var threadHelper = new ThreadHelper();
            foreach (var action in continueActions)
            {
                threadHelper.ExecuteMultiActionParallel(() => action(promise));
            }
            promise.Forget();
            Assert.AreEqual(ThreadHelper.multiExecutionCount * TestHelper.continueTCallbacks / TestHelper.callbacksMultiplier, invokedCount);
        }

        [Test]
        public void PreservedPromiseContinueWithMayBeCalledConcurrently_PendingThenCanceled_void()
        {
            int invokedCount = 0;
            var cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred(cancelationSource.Token);
            var promise = deferred.Promise.Preserve();

            var continueActions = TestHelper.ContinueWithActionsVoid(() => Interlocked.Increment(ref invokedCount));
            var threadHelper = new ThreadHelper();
            foreach (var action in continueActions)
            {
                threadHelper.ExecuteMultiActionParallel(() => action(promise));
            }
            promise.Forget();
            cancelationSource.Cancel();
            Assert.AreEqual(ThreadHelper.multiExecutionCount * TestHelper.continueVoidCallbacks / TestHelper.callbacksMultiplier, invokedCount);

            cancelationSource.Dispose();
        }

        [Test]
        public void PreservedPromiseContinueWithMayBeCalledConcurrently_Canceled_void()
        {
            int invokedCount = 0;
            var promise = Promise.Canceled().Preserve();

            var continueActions = TestHelper.ContinueWithActionsVoid(() => Interlocked.Increment(ref invokedCount));
            var threadHelper = new ThreadHelper();
            foreach (var action in continueActions)
            {
                threadHelper.ExecuteMultiActionParallel(() => action(promise));
            }
            promise.Forget();
            Assert.AreEqual(ThreadHelper.multiExecutionCount * TestHelper.continueVoidCallbacks / TestHelper.callbacksMultiplier, invokedCount);
        }

        [Test]
        public void PreservedPromiseContinueWithMayBeCalledConcurrently_PendingThenCanceled_T()
        {
            int invokedCount = 0;
            var cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred<int>(cancelationSource.Token);
            var promise = deferred.Promise.Preserve();

            var continueActions = TestHelper.ContinueWithActions<int>(() => Interlocked.Increment(ref invokedCount));
            var threadHelper = new ThreadHelper();
            foreach (var action in continueActions)
            {
                threadHelper.ExecuteMultiActionParallel(() => action(promise));
            }
            promise.Forget();
            cancelationSource.Cancel();
            Assert.AreEqual(ThreadHelper.multiExecutionCount * TestHelper.continueTCallbacks / TestHelper.callbacksMultiplier, invokedCount);

            cancelationSource.Dispose();
        }

        [Test]
        public void PreservedPromiseContinueWithMayBeCalledConcurrently_Canceled_T()
        {
            int invokedCount = 0;
            var promise = Promise<int>.Canceled().Preserve();

            var continueActions = TestHelper.ContinueWithActions<int>(() => Interlocked.Increment(ref invokedCount));
            var threadHelper = new ThreadHelper();
            foreach (var action in continueActions)
            {
                threadHelper.ExecuteMultiActionParallel(() => action(promise));
            }
            promise.Forget();
            Assert.AreEqual(ThreadHelper.multiExecutionCount * TestHelper.continueTCallbacks / TestHelper.callbacksMultiplier, invokedCount);
        }

        [Test]
        public void PromiseFinallyMayBeCalledConcurrently_PendingThenResolved_void()
        {
            int invokedCount = 0;
            var deferred = Promise.NewDeferred();
            var promise = deferred.Promise.Preserve();

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(() => promise.Finally(() => Interlocked.Increment(ref invokedCount)).Forget());
            threadHelper.ExecuteMultiActionParallel(() => promise.Finally(1, cv => Interlocked.Increment(ref invokedCount)).Forget());
            promise.Forget();
            deferred.Resolve();
            Assert.AreEqual(ThreadHelper.multiExecutionCount * 2, invokedCount);
        }

        [Test]
        public void PromiseFinallyMayBeCalledConcurrently_Resolved_void()
        {
            int invokedCount = 0;
            var promise = Promise.Resolved().Preserve();

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(() => promise.Finally(() => Interlocked.Increment(ref invokedCount)).Forget());
            threadHelper.ExecuteMultiActionParallel(() => promise.Finally(1, cv => Interlocked.Increment(ref invokedCount)).Forget());
            promise.Forget();
            Assert.AreEqual(ThreadHelper.multiExecutionCount * 2, invokedCount);
        }

        [Test]
        public void PromiseFinallyMayBeCalledConcurrently_PendingThenResolved_T()
        {
            int invokedCount = 0;
            var deferred = Promise.NewDeferred<int>();
            var promise = deferred.Promise.Preserve();

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(() => promise.Finally(() => Interlocked.Increment(ref invokedCount)).Forget());
            threadHelper.ExecuteMultiActionParallel(() => promise.Finally(1, cv => Interlocked.Increment(ref invokedCount)).Forget());
            promise.Forget();
            deferred.Resolve(1);
            Assert.AreEqual(ThreadHelper.multiExecutionCount * 2, invokedCount);
        }

        [Test]
        public void PromiseFinallyMayBeCalledConcurrently_Resolved_T()
        {
            int invokedCount = 0;
            var promise = Promise.Resolved(1).Preserve();

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(() => promise.Finally(() => Interlocked.Increment(ref invokedCount)).Forget());
            threadHelper.ExecuteMultiActionParallel(() => promise.Finally(1, cv => Interlocked.Increment(ref invokedCount)).Forget());
            promise.Forget();
            Assert.AreEqual(ThreadHelper.multiExecutionCount * 2, invokedCount);
        }

        [Test]
        public void PromiseFinallyMayBeCalledConcurrently_PendingThenRejected_void()
        {
            int invokedCount = 0;
            var deferred = Promise.NewDeferred();
            var promise = deferred.Promise.Preserve();

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(() => promise.Finally(() => Interlocked.Increment(ref invokedCount)).Catch(() => { }).Forget());
            threadHelper.ExecuteMultiActionParallel(() => promise.Finally(1, cv => Interlocked.Increment(ref invokedCount)).Catch(() => { }).Forget());
            promise.Forget();
            deferred.Reject("Reject");
            Assert.AreEqual(ThreadHelper.multiExecutionCount * 2, invokedCount);
        }

        [Test]
        public void PromiseFinallyMayBeCalledConcurrently_Rejected_void()
        {
            int invokedCount = 0;
            var promise = Promise.Rejected("Reject").Preserve();

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(() => promise.Finally(() => Interlocked.Increment(ref invokedCount)).Catch(() => { }).Forget());
            threadHelper.ExecuteMultiActionParallel(() => promise.Finally(1, cv => Interlocked.Increment(ref invokedCount)).Catch(() => { }).Forget());
            promise.Forget();
            Assert.AreEqual(ThreadHelper.multiExecutionCount * 2, invokedCount);
        }

        [Test]
        public void PromiseFinallyMayBeCalledConcurrently_PendingThenRejected_T()
        {
            int invokedCount = 0;
            var deferred = Promise.NewDeferred<int>();
            var promise = deferred.Promise.Preserve();

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(() => promise.Finally(() => Interlocked.Increment(ref invokedCount)).Catch(() => { }).Forget());
            threadHelper.ExecuteMultiActionParallel(() => promise.Finally(1, cv => Interlocked.Increment(ref invokedCount)).Catch(() => { }).Forget());
            promise.Forget();
            deferred.Reject("Reject");
            Assert.AreEqual(ThreadHelper.multiExecutionCount * 2, invokedCount);
        }

        [Test]
        public void PromiseFinallyMayBeCalledConcurrently_Rejected_T()
        {
            int invokedCount = 0;
            var promise = Promise<int>.Rejected("Reject").Preserve();

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(() => promise.Finally(() => Interlocked.Increment(ref invokedCount)).Catch(() => { }).Forget());
            threadHelper.ExecuteMultiActionParallel(() => promise.Finally(1, cv => Interlocked.Increment(ref invokedCount)).Catch(() => { }).Forget());
            promise.Forget();
            Assert.AreEqual(ThreadHelper.multiExecutionCount * 2, invokedCount);
        }

        [Test]
        public void PromiseFinallyMayBeCalledConcurrently_PendingThenCanceled_void()
        {
            int invokedCount = 0;
            var cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred(cancelationSource.Token);
            var promise = deferred.Promise.Preserve();

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(() => promise.Finally(() => Interlocked.Increment(ref invokedCount)).Forget());
            threadHelper.ExecuteMultiActionParallel(() => promise.Finally(1, cv => Interlocked.Increment(ref invokedCount)).Forget());
            promise.Forget();
            cancelationSource.Cancel();
            Assert.AreEqual(ThreadHelper.multiExecutionCount * 2, invokedCount);

            cancelationSource.Dispose();
        }

        [Test]
        public void PromiseFinallyMayBeCalledConcurrently_Canceled_void()
        {
            int invokedCount = 0;
            var promise = Promise.Canceled().Preserve();

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(() => promise.Finally(() => Interlocked.Increment(ref invokedCount)).Forget());
            threadHelper.ExecuteMultiActionParallel(() => promise.Finally(1, cv => Interlocked.Increment(ref invokedCount)).Forget());
            promise.Forget();
            Assert.AreEqual(ThreadHelper.multiExecutionCount * 2, invokedCount);
        }

        [Test]
        public void PromiseFinallyMayBeCalledConcurrently_PendingThenCanceled_T()
        {
            int invokedCount = 0;
            var cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred<int>(cancelationSource.Token);
            var promise = deferred.Promise.Preserve();

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(() => promise.Finally(() => Interlocked.Increment(ref invokedCount)).Forget());
            threadHelper.ExecuteMultiActionParallel(() => promise.Finally(1, cv => Interlocked.Increment(ref invokedCount)).Forget());
            promise.Forget();
            cancelationSource.Cancel();
            Assert.AreEqual(ThreadHelper.multiExecutionCount * 2, invokedCount);

            cancelationSource.Dispose();
        }

        [Test]
        public void PromiseFinallyMayBeCalledConcurrently_Canceled_T()
        {
            int invokedCount = 0;
            var promise = Promise<int>.Canceled().Preserve();

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(() => promise.Finally(() => Interlocked.Increment(ref invokedCount)).Forget());
            threadHelper.ExecuteMultiActionParallel(() => promise.Finally(1, cv => Interlocked.Increment(ref invokedCount)).Forget());
            promise.Forget();
            Assert.AreEqual(ThreadHelper.multiExecutionCount * 2, invokedCount);
        }
    }
}

#endif // !UNITY_WEBGL