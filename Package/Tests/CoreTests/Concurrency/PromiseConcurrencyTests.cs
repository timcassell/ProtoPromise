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
        public void PromiseRetainerWithReferenceBacking_DisposeMayOnlyBeCalledOnce_void()
        {
            var deferred = Promise.NewDeferred();
            var promiseRetainer = deferred.Promise.GetRetainer();

            int successCount = 0, invalidCount = 0;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(
                () =>
                {
                    try
                    {
                        promiseRetainer.Dispose();
                        Interlocked.Increment(ref successCount);
                    }
                    catch
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
        public void PromiseRetainerWithReferenceBacking_DisposeMayOnlyBeCalledOnce_T()
        {
            var deferred = Promise.NewDeferred<int>();
            var promiseRetainer = deferred.Promise.GetRetainer();

            int successCount = 0, invalidCount = 0;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(
                () =>
                {
                    try
                    {
                        promiseRetainer.Dispose();
                        Interlocked.Increment(ref successCount);
                    }
                    catch
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
        public void PreservedPromiseWithReferenceBacking_ForgetMayOnlyBeCalledOnce_void()
        {
            var deferred = Promise.NewDeferred();
#pragma warning disable CS0618 // Type or member is obsolete
            var promise = deferred.Promise.Preserve();
#pragma warning restore CS0618 // Type or member is obsolete

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
#pragma warning disable CS0618 // Type or member is obsolete
            var promise = deferred.Promise.Preserve();
#pragma warning restore CS0618 // Type or member is obsolete

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
#pragma warning disable CS0618 // Type or member is obsolete
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
#pragma warning restore CS0618 // Type or member is obsolete
        }

        [Test]
        public void PreservedPromiseWithReferenceBacking_DuplicateCalledConcurrentlyAlwaysReturnsUnique_T()
        {
#pragma warning disable CS0618 // Type or member is obsolete
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
#pragma warning restore CS0618 // Type or member is obsolete
        }

        [Test]
        public void PreservedPromiseWithReferenceBacking_PreserveCalledConcurrentlyAlwaysReturnsUnique_void()
        {
#pragma warning disable CS0618 // Type or member is obsolete
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
#pragma warning restore CS0618 // Type or member is obsolete
        }

        [Test]
        public void PreservedPromiseWithReferenceBacking_PreserveCalledConcurrentlyAlwaysReturnsUnique_T()
        {
#pragma warning disable CS0618 // Type or member is obsolete
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
#pragma warning restore CS0618 // Type or member is obsolete
        }

        [Test]
        public void PromiseReturnedInCallbackMayBeCompletedConcurrently_void(
            [Values] CompleteType completeType)
        {
            var returnDeferred = default(Promise.Deferred);
            var returnPromise = default(Promise);
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
                        returnDeferred = Promise.NewDeferred();
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
                        () => tryCompleter(returnDeferred)
                    },
                    teardown: () =>
                    {
                        Assert.AreNotEqual(Promise.State.Pending, result);
                        switch (completeType)
                        {
                            case CompleteType.Resolve:
                                Assert.AreEqual(Promise.State.Resolved, result);
                                break;
                            case CompleteType.Reject:
                                Assert.AreEqual(Promise.State.Rejected, result);
                                break;
                            default:
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
                        returnDeferred = Promise<int>.NewDeferred();
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
                        () => tryCompleter(returnDeferred)
                    },
                    teardown: () =>
                    {
                        Assert.AreNotEqual(Promise.State.Pending, result);
                        switch (completeType)
                        {
                            case CompleteType.Resolve:
                                Assert.AreEqual(Promise.State.Resolved, result);
                                break;
                            case CompleteType.Reject:
                                Assert.AreEqual(Promise.State.Rejected, result);
                                break;
                            default:
                                Assert.AreEqual(Promise.State.Canceled, result);
                                break;
                        }
                    }
                );
            }
        }

        [Test]
        public void PreservedPromise_ThenMayBeCalledConcurrently_void(
            [Values(CompleteType.Resolve, CompleteType.Reject)] CompleteType completeType,
            [Values] bool isAlreadyComplete)
        {
            int invokedCount = 0;
#pragma warning disable CS0618 // Type or member is obsolete
            var promise = TestHelper.BuildPromise(CompleteType.Cancel, isAlreadyComplete, "Rejected", out var tryCompleter).Preserve();
#pragma warning restore CS0618 // Type or member is obsolete

            var actions = TestHelper.ThenActionsVoid(null, () => Interlocked.Increment(ref invokedCount))
                .Concat(completeType == CompleteType.Resolve
                    ? TestHelper.ResolveActionsVoid(() => Interlocked.Increment(ref invokedCount))
                    : TestHelper.CatchActionsVoid(() => Interlocked.Increment(ref invokedCount))
                );
            var threadHelper = new ThreadHelper();
            foreach (var action in actions)
            {
                threadHelper.ExecuteMultiActionParallel(() => action(promise));
            }
            promise.Forget();
            Assert.AreEqual(ThreadHelper.multiExecutionCount * TestHelper.rejectVoidCallbacks / TestHelper.callbacksMultiplier, invokedCount);
        }

        [Test]
        public void PreservedPromise_ThenMayBeCalledConcurrently_T(
            [Values(CompleteType.Resolve, CompleteType.Reject)] CompleteType completeType,
            [Values] bool isAlreadyComplete)
        {
            int invokedCount = 0;
#pragma warning disable CS0618 // Type or member is obsolete
            var promise = TestHelper.BuildPromise(CompleteType.Cancel, isAlreadyComplete, 42, "Rejected", out var tryCompleter).Preserve();
#pragma warning restore CS0618 // Type or member is obsolete

            var actions = TestHelper.ThenActions<int>(null, () => Interlocked.Increment(ref invokedCount))
                .Concat(completeType == CompleteType.Resolve
                    ? TestHelper.ResolveActions<int>(v => Interlocked.Increment(ref invokedCount))
                    : TestHelper.CatchActions<int>(() => Interlocked.Increment(ref invokedCount))
                );
            var threadHelper = new ThreadHelper();
            foreach (var action in actions)
            {
                threadHelper.ExecuteMultiActionParallel(() => action(promise));
            }
            promise.Forget();
            tryCompleter();
            Assert.AreEqual(ThreadHelper.multiExecutionCount * TestHelper.rejectTCallbacks / TestHelper.callbacksMultiplier, invokedCount);
        }

        [Test]
        public void PreservedPromise_CatchCancelationMayBeCalledConcurrently_void(
            [Values] bool isAlreadyComplete)
        {
            int invokedCount = 0;
#pragma warning disable CS0618 // Type or member is obsolete
            var promise = TestHelper.BuildPromise(CompleteType.Cancel, isAlreadyComplete, "Rejected", out var tryCompleter).Preserve();
#pragma warning restore CS0618 // Type or member is obsolete

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(() => promise.CatchCancelation(() => Interlocked.Increment(ref invokedCount)).Forget());
            threadHelper.ExecuteMultiActionParallel(() => promise.CatchCancelation(1, cv => Interlocked.Increment(ref invokedCount)).Forget());
            promise.Forget();
            tryCompleter();
            Assert.AreEqual(ThreadHelper.multiExecutionCount * 2, invokedCount);
        }

        [Test]
        public void PreservedPromise_CatchCancelationMayBeCalledConcurrently_T(
            [Values] bool isAlreadyComplete)
        {
            int invokedCount = 0;
#pragma warning disable CS0618 // Type or member is obsolete
            var promise = TestHelper.BuildPromise(CompleteType.Cancel, isAlreadyComplete, "Rejected", out var tryCompleter).Preserve();
#pragma warning restore CS0618 // Type or member is obsolete

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(() => promise.CatchCancelation(() => Interlocked.Increment(ref invokedCount)).Forget());
            threadHelper.ExecuteMultiActionParallel(() => promise.CatchCancelation(1, cv => Interlocked.Increment(ref invokedCount)).Forget());
            promise.Forget();
            tryCompleter();
            Assert.AreEqual(ThreadHelper.multiExecutionCount * 2, invokedCount);
        }

        [Test]
        public void PreservedPromise_ContinueWithMayBeCalledConcurrently_void(
            [Values] CompleteType completeType,
            [Values] bool isAlreadyComplete)
        {
            int invokedCount = 0;
#pragma warning disable CS0618 // Type or member is obsolete
            var promise = TestHelper.BuildPromise(completeType, isAlreadyComplete, "Rejected", out var tryCompleter).Preserve();
#pragma warning restore CS0618 // Type or member is obsolete

            var continueActions = TestHelper.ContinueWithActionsVoid(() => Interlocked.Increment(ref invokedCount));
            var threadHelper = new ThreadHelper();
            foreach (var action in continueActions)
            {
                threadHelper.ExecuteMultiActionParallel(() => action(promise));
            }
            promise.Forget();
            tryCompleter();
            Assert.AreEqual(ThreadHelper.multiExecutionCount * TestHelper.continueVoidCallbacks / TestHelper.callbacksMultiplier, invokedCount);
        }

        [Test]
        public void PreservedPromise_ContinueWithMayBeCalledConcurrently_T(
            [Values] CompleteType completeType,
            [Values] bool isAlreadyComplete)
        {
            int invokedCount = 0;
#pragma warning disable CS0618 // Type or member is obsolete
            var promise = TestHelper.BuildPromise(completeType, isAlreadyComplete, 42, "Rejected", out var tryCompleter).Preserve();
#pragma warning restore CS0618 // Type or member is obsolete

            var continueActions = TestHelper.ContinueWithActions<int>(() => Interlocked.Increment(ref invokedCount));
            var threadHelper = new ThreadHelper();
            foreach (var action in continueActions)
            {
                threadHelper.ExecuteMultiActionParallel(() => action(promise));
            }
            promise.Forget();
            tryCompleter();
            Assert.AreEqual(ThreadHelper.multiExecutionCount * TestHelper.continueTCallbacks / TestHelper.callbacksMultiplier, invokedCount);
        }

        [Test]
        public void PreservedPromise_FinallyMayBeCalledConcurrently_void(
            [Values] CompleteType completeType,
            [Values] bool isAlreadyComplete)
        {
            int invokedCount = 0;
#pragma warning disable CS0618 // Type or member is obsolete
            var promise = TestHelper.BuildPromise(completeType, isAlreadyComplete, "Rejected", out var tryCompleter).Preserve();
#pragma warning restore CS0618 // Type or member is obsolete

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(() => promise.Finally(() => Interlocked.Increment(ref invokedCount)).Forget());
            threadHelper.ExecuteMultiActionParallel(() => promise.Finally(1, cv => Interlocked.Increment(ref invokedCount)).Forget());
            promise.Forget();
            tryCompleter();
            Assert.AreEqual(ThreadHelper.multiExecutionCount * 2, invokedCount);
        }

        [Test]
        public void PreservedPromise_FinallyMayBeCalledConcurrently_T(
            [Values] CompleteType completeType,
            [Values] bool isAlreadyComplete)
        {
            int invokedCount = 0;
#pragma warning disable CS0618 // Type or member is obsolete
            var promise = TestHelper.BuildPromise(completeType, isAlreadyComplete, 42, "Rejected", out var tryCompleter).Preserve();
#pragma warning restore CS0618 // Type or member is obsolete

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(() => promise.Finally(() => Interlocked.Increment(ref invokedCount)).Forget());
            threadHelper.ExecuteMultiActionParallel(() => promise.Finally(1, cv => Interlocked.Increment(ref invokedCount)).Forget());
            promise.Forget();
            tryCompleter();
            Assert.AreEqual(ThreadHelper.multiExecutionCount * 2, invokedCount);
        }

        [Test]
        public void PromiseRetainer_WaitAsyncMayBeCalledConcurrently_void(
            [Values] CompleteType completeType,
            [Values] bool isAlreadyComplete)
        {
            var expectedRejection = "Rejected";
            int invokedCount = 0;
            using (var promiseRetainer = TestHelper.BuildPromise(completeType, isAlreadyComplete, expectedRejection, out var tryCompleter)
                .GetRetainer())
            {
                new ThreadHelper().ExecuteMultiActionParallel(() =>
                    promiseRetainer.WaitAsync()
                        .ContinueWith(resultContainer =>
                        {
                            Interlocked.Increment(ref invokedCount);
                            Assert.AreEqual((Promise.State) completeType, resultContainer.State);
                            if (completeType == CompleteType.Reject)
                            {
                                Assert.AreEqual(expectedRejection, resultContainer.Reason);
                            }
                        })
                        .Forget()
                    );
                tryCompleter();
            }
            Assert.AreEqual(ThreadHelper.multiExecutionCount * 2, invokedCount);
        }

        [Test]
        public void PromiseRetainer_WaitAsyncMayBeCalledConcurrently_T(
            [Values] CompleteType completeType,
            [Values] bool isAlreadyComplete)
        {
            var expectedRejection = "Rejected";
            var expectedResult = 42;
            int invokedCount = 0;
            using (var promiseRetainer = TestHelper.BuildPromise(completeType, isAlreadyComplete, expectedResult, expectedRejection, out var tryCompleter)
                .GetRetainer())
            {
                new ThreadHelper().ExecuteMultiActionParallel(() =>
                    promiseRetainer.WaitAsync()
                        .ContinueWith(resultContainer =>
                        {
                            Interlocked.Increment(ref invokedCount);
                            Assert.AreEqual((Promise.State) completeType, resultContainer.State);
                            if (completeType == CompleteType.Reject)
                            {
                                Assert.AreEqual(expectedRejection, resultContainer.Reason);
                            }
                        })
                        .Forget()
                    );
                tryCompleter();
            }
            Assert.AreEqual(ThreadHelper.multiExecutionCount * 2, invokedCount);
        }
    }
}

#endif // !UNITY_WEBGL