#if CSHARP_7_3_OR_NEWER && !UNITY_WEBGL

#if !PROTO_PROMISE_PROGRESS_DISABLE
#define PROMISE_PROGRESS
#else
#undef PROMISE_PROGRESS
#endif

using NUnit.Framework;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Proto.Promises.Tests.Threading
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
                    catch (InvalidOperationException)
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
                    catch (InvalidOperationException)
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
        public void PromiseProgressMayBeCalledConcurrently_Resolved_void()
        {
            int invokedCount = 0;
            Promise promise = Promise.Resolved().Preserve();

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(
                () => promise.Progress(v => { Interlocked.Increment(ref invokedCount); }).Forget()
            );
            promise.Forget();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(ThreadHelper.multiExecutionCount, invokedCount);
        }

        [Test]
        public void PromiseProgressMayBeCalledConcurrently_Resolved_T()
        {
            int invokedCount = 0;
            Promise<int> promise = Promise.Resolved(1).Preserve();

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(
                () => promise.Progress(v => { Interlocked.Increment(ref invokedCount); }).Forget()
            );
            promise.Forget();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(ThreadHelper.multiExecutionCount, invokedCount);
        }

        [Test]
        public void PromiseProgressMayBeSubscribedWhilePromiseIsCompletedAndProgressIsReporteddConcurrently_Pending_void(
            [Values] ActionType subscribeType,
            [Values] ActionType reportType,
            [Values] ActionType completePlace,
            [Values] CompleteType completeType)
        {
            int expectedInvokes = completeType == CompleteType.Resolve ? 10 : 0;

            var deferred = default(Promise.Deferred);
            var promise = default(Promise);
            var cancelationSource = default(CancelationSource);
            int invokedCount = 0;

            List<Action> parallelActions = new List<Action>();

            var progressSubscriber = ParallelActionTestHelper.Create(
                subscribeType,
                10,
                () => promise.Progress(v => { Interlocked.Increment(ref invokedCount); }).Catch(() => { }).Forget()
            );
            progressSubscriber.MaybeAddParallelAction(parallelActions);

            var progressReporter = ParallelActionTestHelper.Create(
                reportType,
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

            Action setupAction = () =>
            {
                invokedCount = 0;
                deferred = TestHelper.GetNewDeferredVoid(completeType, out cancelationSource);
                promise = deferred.Promise.Preserve();
                progressSubscriber.Setup();
                progressReporter.Setup();
                promiseCompleter.Setup();
            };
            Action teardownAction = () =>
            {
                progressSubscriber.Teardown();
                progressReporter.Teardown();
                promiseCompleter.Teardown();
                cancelationSource.TryDispose();
                promise.Forget();
                Promise.Manager.HandleCompletes();
                Assert.AreEqual(expectedInvokes, invokedCount);
            };
            if (parallelActions.Count == 0)
            {
                setupAction();
                teardownAction();
                return;
            }
            var threadHelper = new ThreadHelper(TimeSpan.FromSeconds(10));
            threadHelper.ExecuteParallelActions(ThreadHelper.multiExecutionCount,
                setupAction,
                teardownAction,
                parallelActions.ToArray()
            );
        }

        [Test]
        public void PromiseProgressMayBeSubscribedWhilePromiseIsCompletedAndProgressIsReporteddConcurrently_Pending_T(
            [Values] ActionType subscribeType,
            [Values] ActionType reportType,
            [Values] ActionType completePlace,
            [Values] CompleteType completeType)
        {
            int expectedInvokes = completeType == CompleteType.Resolve ? 10 : 0;

            var deferred = default(Promise<int>.Deferred);
            var promise = default(Promise<int>);
            var cancelationSource = default(CancelationSource);
            int invokedCount = 0;

            List<Action> parallelActions = new List<Action>();

            var progressSubscriber = ParallelActionTestHelper.Create(
                subscribeType,
                10,
                () => promise.Progress(v => { Interlocked.Increment(ref invokedCount); }).Catch(() => { }).Forget()
            );
            progressSubscriber.MaybeAddParallelAction(parallelActions);

            var progressReporter = ParallelActionTestHelper.Create(
                reportType,
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

            Action setupAction = () =>
            {
                invokedCount = 0;
                deferred = TestHelper.GetNewDeferredT<int>(completeType, out cancelationSource);
                promise = deferred.Promise.Preserve();
                progressSubscriber.Setup();
                progressReporter.Setup();
                promiseCompleter.Setup();
            };
            Action teardownAction = () =>
            {
                progressSubscriber.Teardown();
                progressReporter.Teardown();
                promiseCompleter.Teardown();
                cancelationSource.TryDispose();
                promise.Forget();
                Promise.Manager.HandleCompletes();
                Assert.AreEqual(expectedInvokes, invokedCount);
            };
            if (parallelActions.Count == 0)
            {
                setupAction();
                teardownAction();
                return;
            }
            var threadHelper = new ThreadHelper(TimeSpan.FromSeconds(10));
            threadHelper.ExecuteParallelActions(ThreadHelper.multiExecutionCount,
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
                            Promise.Manager.HandleCompletesAndProgress();
                        }
                    },
                    parallelActions: new Action[]
                    {
                        () => tryCompleter(returnDeferred, cancelationSource)
                    },
                    teardown: () =>
                    {
                        cancelationSource.TryDispose();
                        Promise.Manager.HandleCompletesAndProgress();
                        
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
                            Promise.Manager.HandleCompletesAndProgress();
                        }
                    },
                    parallelActions: new Action[]
                    {
                        () => tryCompleter(returnDeferred, cancelationSource)
                    },
                    teardown: () =>
                    {
                        cancelationSource.TryDispose();
                        Promise.Manager.HandleCompletesAndProgress();

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
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(ThreadHelper.multiExecutionCount * TestHelper.resolveVoidCallbacks, invokedCount);
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
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(ThreadHelper.multiExecutionCount * TestHelper.resolveVoidCallbacks, invokedCount);
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
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(ThreadHelper.multiExecutionCount * TestHelper.resolveTCallbacks, invokedCount);
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
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(ThreadHelper.multiExecutionCount * TestHelper.resolveTCallbacks, invokedCount);
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
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(ThreadHelper.multiExecutionCount * TestHelper.rejectVoidCallbacks, invokedCount);
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
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(ThreadHelper.multiExecutionCount * TestHelper.rejectVoidCallbacks, invokedCount);
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
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(ThreadHelper.multiExecutionCount * TestHelper.rejectTCallbacks, invokedCount);
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
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(ThreadHelper.multiExecutionCount * TestHelper.rejectTCallbacks, invokedCount);
        }

        [Test]
        public void PromiseCatchCancelationnMayBeCalledConcurrently_PendingThenCanceled_void()
        {
            int invokedCount = 0;
            var cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred(cancelationSource.Token);
            var promise = deferred.Promise.Preserve();

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(() => promise.CatchCancelation(_ => Interlocked.Increment(ref invokedCount)).Forget());
            threadHelper.ExecuteMultiActionParallel(() => promise.CatchCancelation(1, (cv, _) => Interlocked.Increment(ref invokedCount)).Forget());
            promise.Forget();
            cancelationSource.Cancel();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(ThreadHelper.multiExecutionCount * 2, invokedCount);

            cancelationSource.Dispose();
        }

        [Test]
        public void PromiseCatchCancelationnMayBeCalledConcurrently_Canceled_void()
        {
            int invokedCount = 0;
            var promise = Promise.Canceled().Preserve();

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(() => promise.CatchCancelation(_ => Interlocked.Increment(ref invokedCount)).Forget());
            threadHelper.ExecuteMultiActionParallel(() => promise.CatchCancelation(1, (cv, _) => Interlocked.Increment(ref invokedCount)).Forget());
            promise.Forget();
            Promise.Manager.HandleCompletesAndProgress();
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
            threadHelper.ExecuteMultiActionParallel(() => promise.CatchCancelation(_ => Interlocked.Increment(ref invokedCount)).Forget());
            threadHelper.ExecuteMultiActionParallel(() => promise.CatchCancelation(1, (cv, _) => Interlocked.Increment(ref invokedCount)).Forget());
            promise.Forget();
            cancelationSource.Cancel();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(ThreadHelper.multiExecutionCount * 2, invokedCount);

            cancelationSource.Dispose();
        }

        [Test]
        public void PromiseCatchCancelationnMayBeCalledConcurrently_Canceled_T()
        {
            int invokedCount = 0;
            var promise = Promise<int>.Canceled().Preserve();

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(() => promise.CatchCancelation(_ => Interlocked.Increment(ref invokedCount)).Forget());
            threadHelper.ExecuteMultiActionParallel(() => promise.CatchCancelation(1, (cv, _) => Interlocked.Increment(ref invokedCount)).Forget());
            promise.Forget();
            Promise.Manager.HandleCompletesAndProgress();
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
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(ThreadHelper.multiExecutionCount * TestHelper.continueVoidCallbacks, invokedCount);
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
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(ThreadHelper.multiExecutionCount * TestHelper.continueVoidCallbacks, invokedCount);
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
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(ThreadHelper.multiExecutionCount * TestHelper.continueTCallbacks, invokedCount);
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
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(ThreadHelper.multiExecutionCount * TestHelper.continueTCallbacks, invokedCount);
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
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(ThreadHelper.multiExecutionCount * TestHelper.continueVoidCallbacks, invokedCount);
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
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(ThreadHelper.multiExecutionCount * TestHelper.continueVoidCallbacks, invokedCount);
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
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(ThreadHelper.multiExecutionCount * TestHelper.continueTCallbacks, invokedCount);
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
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(ThreadHelper.multiExecutionCount * TestHelper.continueTCallbacks, invokedCount);
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
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(ThreadHelper.multiExecutionCount * TestHelper.continueVoidCallbacks, invokedCount);

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
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(ThreadHelper.multiExecutionCount * TestHelper.continueVoidCallbacks, invokedCount);
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
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(ThreadHelper.multiExecutionCount * TestHelper.continueTCallbacks, invokedCount);

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
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(ThreadHelper.multiExecutionCount * TestHelper.continueTCallbacks, invokedCount);
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
            Promise.Manager.HandleCompletesAndProgress();
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
            Promise.Manager.HandleCompletesAndProgress();
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
            Promise.Manager.HandleCompletesAndProgress();
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
            Promise.Manager.HandleCompletesAndProgress();
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
            Promise.Manager.HandleCompletesAndProgress();
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
            Promise.Manager.HandleCompletesAndProgress();
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
            Promise.Manager.HandleCompletesAndProgress();
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
            Promise.Manager.HandleCompletesAndProgress();
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
            Promise.Manager.HandleCompletesAndProgress();
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
            Promise.Manager.HandleCompletesAndProgress();
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
            Promise.Manager.HandleCompletesAndProgress();
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
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(ThreadHelper.multiExecutionCount * 2, invokedCount);
        }
    }
}

#endif