#if !UNITY_WEBGL

#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using NUnit.Framework;
using Proto.Promises;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ProtoPromiseTests.Concurrency
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
            var deferred = Promise.NewDeferred();
            var promise = deferred.Promise.Preserve();

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(() => promise.CatchCancelation(() => Interlocked.Increment(ref invokedCount)).Forget());
            threadHelper.ExecuteMultiActionParallel(() => promise.CatchCancelation(1, cv => Interlocked.Increment(ref invokedCount)).Forget());
            promise.Forget();
            deferred.Cancel();
            Assert.AreEqual(ThreadHelper.multiExecutionCount * 2, invokedCount);
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
            var deferred = Promise.NewDeferred<int>();
            var promise = deferred.Promise.Preserve();

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(() => promise.CatchCancelation(() => Interlocked.Increment(ref invokedCount)).Forget());
            threadHelper.ExecuteMultiActionParallel(() => promise.CatchCancelation(1, cv => Interlocked.Increment(ref invokedCount)).Forget());
            promise.Forget();
            deferred.Cancel();
            Assert.AreEqual(ThreadHelper.multiExecutionCount * 2, invokedCount);
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
            var deferred = Promise.NewDeferred();
            var promise = deferred.Promise.Preserve();

            var continueActions = TestHelper.ContinueWithActionsVoid(() => Interlocked.Increment(ref invokedCount));
            var threadHelper = new ThreadHelper();
            foreach (var action in continueActions)
            {
                threadHelper.ExecuteMultiActionParallel(() => action(promise));
            }
            promise.Forget();
            deferred.Cancel();
            Assert.AreEqual(ThreadHelper.multiExecutionCount * TestHelper.continueVoidCallbacks / TestHelper.callbacksMultiplier, invokedCount);
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
            var deferred = Promise.NewDeferred<int>();
            var promise = deferred.Promise.Preserve();

            var continueActions = TestHelper.ContinueWithActions<int>(() => Interlocked.Increment(ref invokedCount));
            var threadHelper = new ThreadHelper();
            foreach (var action in continueActions)
            {
                threadHelper.ExecuteMultiActionParallel(() => action(promise));
            }
            promise.Forget();
            deferred.Cancel();
            Assert.AreEqual(ThreadHelper.multiExecutionCount * TestHelper.continueTCallbacks / TestHelper.callbacksMultiplier, invokedCount);
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
            var deferred = Promise.NewDeferred();
            var promise = deferred.Promise.Preserve();

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(() => promise.Finally(() => Interlocked.Increment(ref invokedCount)).Forget());
            threadHelper.ExecuteMultiActionParallel(() => promise.Finally(1, cv => Interlocked.Increment(ref invokedCount)).Forget());
            promise.Forget();
            deferred.Cancel();
            Assert.AreEqual(ThreadHelper.multiExecutionCount * 2, invokedCount);
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
            var deferred = Promise.NewDeferred<int>();
            var promise = deferred.Promise.Preserve();

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(() => promise.Finally(() => Interlocked.Increment(ref invokedCount)).Forget());
            threadHelper.ExecuteMultiActionParallel(() => promise.Finally(1, cv => Interlocked.Increment(ref invokedCount)).Forget());
            promise.Forget();
            deferred.Cancel();
            Assert.AreEqual(ThreadHelper.multiExecutionCount * 2, invokedCount);
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