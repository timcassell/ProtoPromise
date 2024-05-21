#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using NUnit.Framework;
using Proto.Promises;
using System;
using System.Collections.Generic;

namespace ProtoPromiseTests.APIs
{
    public class FinallyTests
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

#if PROMISE_DEBUG
        [Test]
        public void IfOnFinallyIsNullThrow_void()
        {
            var deferred = Promise.NewDeferred();
            var promise = deferred.Promise.Preserve();

            Assert.Throws<Proto.Promises.ArgumentNullException>(() => promise.Finally(default(Action)));
            Assert.Throws<Proto.Promises.ArgumentNullException>(() => promise.Finally(42, default(Action<int>)));
            Assert.Throws<Proto.Promises.ArgumentNullException>(() => promise.Finally(default(Func<Promise>)));
            Assert.Throws<Proto.Promises.ArgumentNullException>(() => promise.Finally(42, default(Func<int, Promise>)));

            deferred.Resolve();

            promise.Forget();
        }

        [Test]
        public void IfOnFinallyIsNullThrow_T()
        {
            var deferred = Promise.NewDeferred<int>();
            var promise = deferred.Promise.Preserve();

            Assert.Throws<Proto.Promises.ArgumentNullException>(() => promise.Finally(default(Action)));
            Assert.Throws<Proto.Promises.ArgumentNullException>(() => promise.Finally(42, default(Action<int>)));
            Assert.Throws<Proto.Promises.ArgumentNullException>(() => promise.Finally(default(Func<Promise>)));
            Assert.Throws<Proto.Promises.ArgumentNullException>(() => promise.Finally(42, default(Func<int, Promise>)));

            deferred.Resolve(1);

            promise.Forget();
        }
#endif

        [Test]
        public void OnFinallyIsInvokedWhenPromiseIsSettled_void(
            [Values] CompleteType completeType,
            [Values] bool isAlreadyComplete)
        {
            const string rejection = "Reject";
            var preservedPromise = TestHelper.BuildPromise(completeType, isAlreadyComplete, rejection, out var deferred)
                .Preserve();

            int testCount = 0;
            int finallyCount = 0;
            int completeCount = 0;
            const string captureValue = "captureValue";

            foreach (var promise in TestHelper.GetTestablePromises(preservedPromise))
            {
                ++testCount;
                promise
                    .Finally(() => ++finallyCount)
                    .Finally(captureValue, s =>
                    {
                        Assert.AreEqual(captureValue, s);
                        ++finallyCount;
                    })
                    .ContinueWith(resultContainer =>
                    {
                        if (completeType == CompleteType.Resolve)
                        {
                            Assert.AreEqual(Promise.State.Resolved, resultContainer.State);
                        }
                        else if (completeType == CompleteType.Reject)
                        {
                            Assert.AreEqual(Promise.State.Rejected, resultContainer.State);
                            Assert.AreEqual(rejection, resultContainer.Reason);
                        }
                        else
                        {
                            Assert.AreEqual(Promise.State.Canceled, resultContainer.State);
                        }
                        ++completeCount;
                    })
                    .Forget();
            }

            Assert.Greater(testCount, 0);

            TestHelper.GetTryCompleterVoid(completeType, rejection).Invoke(deferred);

            Assert.AreEqual(2 * testCount, finallyCount);
            Assert.AreEqual(testCount, completeCount);

            preservedPromise.Forget();
        }

        [Test]
        public void OnFinallyIsInvokedWhenPromiseIsSettled_T(
            [Values] CompleteType completeType,
            [Values] bool isAlreadyComplete)
        {
            const int resolveValue = 42;
            const string rejection = "Reject";
            var preservedPromise = TestHelper.BuildPromise(completeType, isAlreadyComplete, resolveValue, rejection, out var deferred)
                .Preserve();

            int testCount = 0;
            int finallyCount = 0;
            int completeCount = 0;
            const string captureValue = "captureValue";

            foreach (var promise in TestHelper.GetTestablePromises(preservedPromise))
            {
                ++testCount;
                promise
                    .Finally(() => ++finallyCount)
                    .Finally(captureValue, s =>
                    {
                        Assert.AreEqual(captureValue, s);
                        ++finallyCount;
                    })
                    .ContinueWith(resultContainer =>
                    {
                        if (completeType == CompleteType.Resolve)
                        {
                            Assert.AreEqual(Promise.State.Resolved, resultContainer.State);
                            Assert.AreEqual(resolveValue, resultContainer.Value);
                        }
                        else if (completeType == CompleteType.Reject)
                        {
                            Assert.AreEqual(Promise.State.Rejected, resultContainer.State);
                            Assert.AreEqual(rejection, resultContainer.Reason);
                        }
                        else
                        {
                            Assert.AreEqual(Promise.State.Canceled, resultContainer.State);
                        }
                        ++completeCount;
                    })
                    .Forget();
            }

            Assert.Greater(testCount, 0);

            TestHelper.GetTryCompleterT(completeType, resolveValue, rejection).Invoke(deferred);

            Assert.AreEqual(2 * testCount, finallyCount);
            Assert.AreEqual(testCount, completeCount);

            preservedPromise.Forget();
        }

        [Test]
        public void PromiseIsRejectedWithThrownExceptionWhenOnFinallyThrows_void(
            [Values] CompleteType completeType,
            [Values] bool isAlreadyComplete,
            [Values] bool isAsync)
        {
            const string rejection = "Reject";

            // When the exception thrown in onFinally overwrites the current rejection, the current rejection gets sent to the UncaughtRejectionHandler.
            // So we need to suppress that here and make sure it actually gets sent to it.
            var currentHandler = Promise.Config.UncaughtRejectionHandler;
            int uncaughtHandledCount = 0;
            Promise.Config.UncaughtRejectionHandler = e =>
            {
                Assert.AreEqual(rejection, e.Value);
                ++uncaughtHandledCount;
            };

            var preservedPromise = TestHelper.BuildPromise(completeType, isAlreadyComplete, rejection, out var deferred)
                .Preserve();

            int testCount = 0;
            int catchCount = 0;
            Exception expected = new Exception();

            foreach (var promise in TestHelper.GetTestablePromises(preservedPromise))
            {
                ++testCount;
#pragma warning disable CS0162 // Unreachable code detected
                (isAsync
                    ? promise.Finally(() => { throw expected; return Promise.Resolved(); })
                    : promise.Finally(() => { throw expected; }))
#pragma warning restore CS0162 // Unreachable code detected
                    .Catch((Exception e) =>
                    {
                        Assert.AreEqual(expected, e);
                        ++catchCount;
                    })
                    .Forget();
            }

            Assert.Greater(testCount, 0);

            TestHelper.GetTryCompleterVoid(completeType, rejection).Invoke(deferred);

            Assert.AreEqual(testCount, catchCount);
            Assert.AreEqual(completeType == CompleteType.Reject ? testCount : 0, uncaughtHandledCount);

            preservedPromise.Forget();

            Promise.Config.UncaughtRejectionHandler = currentHandler;
        }

        [Test]
        public void PromiseIsRejectedWithThrownExceptionWhenOnFinallyThrows_T(
            [Values] CompleteType completeType,
            [Values] bool isAlreadyComplete,
            [Values] bool isAsync)
        {
            const string rejection = "Reject";

            // When the exception thrown in onFinally overwrites the current rejection, the current rejection gets sent to the UncaughtRejectionHandler.
            // So we need to suppress that here and make sure it actually gets sent to it.
            var currentHandler = Promise.Config.UncaughtRejectionHandler;
            int uncaughtHandledCount = 0;
            Promise.Config.UncaughtRejectionHandler = e =>
            {
                Assert.AreEqual(rejection, e.Value);
                ++uncaughtHandledCount;
            };

            const int resolveValue = 42;
            var preservedPromise = TestHelper.BuildPromise(completeType, isAlreadyComplete, resolveValue, rejection, out var deferred)
                .Preserve();

            int testCount = 0;
            int catchCount = 0;
            Exception expected = new Exception();

            foreach (var promise in TestHelper.GetTestablePromises(preservedPromise))
            {
                ++testCount;
#pragma warning disable CS0162 // Unreachable code detected
                (isAsync
                    ? promise.Finally(() => { throw expected; return Promise.Resolved(); })
                    : promise.Finally(() => { throw expected; }))
#pragma warning restore CS0162 // Unreachable code detected
                    .Catch((Exception e) =>
                    {
                        Assert.AreEqual(expected, e);
                        ++catchCount;
                    })
                    .Forget();
            }

            Assert.Greater(testCount, 0);

            TestHelper.GetTryCompleterT(completeType, resolveValue, rejection).Invoke(deferred);

            Assert.AreEqual(testCount, catchCount);
            Assert.AreEqual(completeType == CompleteType.Reject ? testCount : 0, uncaughtHandledCount);

            preservedPromise.Forget();

            Promise.Config.UncaughtRejectionHandler = currentHandler;
        }

        [Test]
        public void PromiseIsCanceledWhenOnFinallyThrows_void(
            [Values] CompleteType completeType,
            [Values] bool isAlreadyComplete,
            [Values] bool isAsync)
        {
            const string rejection = "Reject";

            // When the exception thrown in onFinally overwrites the current rejection, the current rejection gets sent to the UncaughtRejectionHandler.
            // So we need to suppress that here and make sure it actually gets sent to it.
            var currentHandler = Promise.Config.UncaughtRejectionHandler;
            int uncaughtHandledCount = 0;
            Promise.Config.UncaughtRejectionHandler = e =>
            {
                Assert.AreEqual(rejection, e.Value);
                ++uncaughtHandledCount;
            };

            var preservedPromise = TestHelper.BuildPromise(completeType, isAlreadyComplete, rejection, out var deferred)
                .Preserve();

            int testCount = 0;
            int catchCount = 0;

            foreach (var promise in TestHelper.GetTestablePromises(preservedPromise))
            {
                ++testCount;
#pragma warning disable CS0162 // Unreachable code detected
                (isAsync
                    ? promise.Finally(() => { throw Promise.CancelException(); return Promise.Resolved(); })
                    : promise.Finally(() => { throw Promise.CancelException(); }))
#pragma warning restore CS0162 // Unreachable code detected
                    .CatchCancelation(() => ++catchCount)
                    .Forget();
            }

            Assert.Greater(testCount, 0);

            TestHelper.GetTryCompleterVoid(completeType, rejection).Invoke(deferred);

            Assert.AreEqual(testCount, catchCount);
            Assert.AreEqual(completeType == CompleteType.Reject ? testCount : 0, uncaughtHandledCount);

            preservedPromise.Forget();

            Promise.Config.UncaughtRejectionHandler = currentHandler;
        }

        [Test]
        public void PromiseIsCanceledWhenOnFinallyThrows_T(
            [Values] CompleteType completeType,
            [Values] bool isAlreadyComplete,
            [Values] bool isAsync)
        {
            const string rejection = "Reject";

            // When the exception thrown in onFinally overwrites the current rejection, the current rejection gets sent to the UncaughtRejectionHandler.
            // So we need to suppress that here and make sure it actually gets sent to it.
            var currentHandler = Promise.Config.UncaughtRejectionHandler;
            int uncaughtHandledCount = 0;
            Promise.Config.UncaughtRejectionHandler = e =>
            {
                Assert.AreEqual(rejection, e.Value);
                ++uncaughtHandledCount;
            };

            const int resolveValue = 42;
            var preservedPromise = TestHelper.BuildPromise(completeType, isAlreadyComplete, resolveValue, rejection, out var deferred)
                .Preserve();

            int testCount = 0;
            int catchCount = 0;

            foreach (var promise in TestHelper.GetTestablePromises(preservedPromise))
            {
                ++testCount;
#pragma warning disable CS0162 // Unreachable code detected
                (isAsync
                    ? promise.Finally(() => { throw Promise.CancelException(); return Promise.Resolved(); })
                    : promise.Finally(() => { throw Promise.CancelException(); }))
#pragma warning restore CS0162 // Unreachable code detected
                    .CatchCancelation(() => ++catchCount)
                    .Forget();
            }

            Assert.Greater(testCount, 0);

            TestHelper.GetTryCompleterT(completeType, resolveValue, rejection).Invoke(deferred);

            Assert.AreEqual(testCount, catchCount);
            Assert.AreEqual(completeType == CompleteType.Reject ? testCount : 0, uncaughtHandledCount);

            preservedPromise.Forget();

            Promise.Config.UncaughtRejectionHandler = currentHandler;
        }

        [Test]
        public void OnFinallyIsInvokedWhenPromiseIsSettled_WaitsForReturnedPromise_void(
            [Values] CompleteType completeType,
            [Values] bool isAlreadyComplete,
            [Values] CompleteType returnCompleteType,
            [Values] bool returnIsAlreadyComplete)
        {
            const string rejection1 = "Reject1";
            const string rejection2 = "Reject2";

            // When the returnPromise in onFinally is rejected, it overwrites the current rejection, and the current rejection gets sent to the UncaughtRejectionHandler.
            // So we need to suppress that here and make sure it actually gets sent to it.
            var currentHandler = Promise.Config.UncaughtRejectionHandler;
            int uncaughtHandledCount = 0;
            Promise.Config.UncaughtRejectionHandler = e =>
            {
                // The uncaught rejection could come from either the first promise or the returnPromise, since we add 2 .Finally back-to-back.
                if (!rejection1.Equals(e.Value))
                {
                    Assert.AreEqual(rejection2, e.Value);
                }
                ++uncaughtHandledCount;
            };

            var preservedPromise = TestHelper.BuildPromise(completeType, isAlreadyComplete, rejection1, out var deferred)
                .Preserve();

            int finallyCount = 0;
            int completeCount = 0;
            const string captureValue = "captureValue";

            var returnPromises = new List<(Promise.Deferred deferred, Promise promise)>();
            foreach (var promise in TestHelper.GetTestablePromises(preservedPromise))
            {
                var returnPromise = TestHelper.BuildPromise(returnCompleteType, returnIsAlreadyComplete, rejection2, out var returnDeferred)
                    .Preserve();
                returnPromises.Add((returnDeferred, returnPromise));

                promise
                    .Finally(() =>
                    {
                        ++finallyCount;
                        return returnPromise;
                    })
                    .Finally(captureValue, s =>
                    {
                        Assert.AreEqual(captureValue, s);
                        ++finallyCount;
                        return returnPromise;
                    })
                    .ContinueWith(resultContainer =>
                    {
                        if (returnCompleteType == CompleteType.Resolve)
                        {
                            if (completeType == CompleteType.Resolve)
                            {
                                Assert.AreEqual(Promise.State.Resolved, resultContainer.State);
                            }
                            else if (completeType == CompleteType.Reject)
                            {
                                Assert.AreEqual(Promise.State.Rejected, resultContainer.State);
                                Assert.AreEqual(rejection1, resultContainer.Reason);
                            }
                            else
                            {
                                Assert.AreEqual(Promise.State.Canceled, resultContainer.State);
                            }
                        }
                        else if (returnCompleteType == CompleteType.Reject)
                        {
                            Assert.AreEqual(Promise.State.Rejected, resultContainer.State);
                            Assert.AreEqual(rejection2, resultContainer.Reason);
                        }
                        else
                        {
                            Assert.AreEqual(Promise.State.Canceled, resultContainer.State);
                        }
                        ++completeCount;
                    })
                    .Forget();
            }

            Assert.Greater(returnPromises.Count, 0);

            TestHelper.GetTryCompleterVoid(completeType, rejection1).Invoke(deferred);

            Assert.AreEqual(returnPromises.Count * (returnIsAlreadyComplete ? 2 : 1), finallyCount);
            Assert.AreEqual(returnIsAlreadyComplete ? returnPromises.Count : 0, completeCount);

            var completer = TestHelper.GetTryCompleterVoid(returnCompleteType, rejection2);
            foreach (var tuple in returnPromises)
            {
                completer.Invoke(tuple.deferred);
                tuple.promise.Forget();
            }

            Assert.AreEqual(returnPromises.Count, completeCount);
            int expectedUncaughtCount = completeType == CompleteType.Reject
                ? returnCompleteType == CompleteType.Reject
                    ? returnPromises.Count * 2
                    : returnCompleteType != CompleteType.Resolve // Cancel
                    ? returnPromises.Count
                    : 0
                : returnCompleteType == CompleteType.Reject
                    ? returnPromises.Count
                    : 0;
            Assert.AreEqual(expectedUncaughtCount, uncaughtHandledCount);

            preservedPromise.Forget();

            Promise.Config.UncaughtRejectionHandler = currentHandler;
        }

        [Test]
        public void OnFinallyIsInvokedWhenPromiseIsSettled_WaitsForReturnedPromise_T(
            [Values] CompleteType completeType,
            [Values] bool isAlreadyComplete,
            [Values] CompleteType returnCompleteType,
            [Values] bool returnIsAlreadyComplete)
        {
            const string rejection1 = "Reject1";
            const string rejection2 = "Reject2";

            // When the returnPromise in onFinally is rejected, it overwrites the current rejection, and the current rejection gets sent to the UncaughtRejectionHandler.
            // So we need to suppress that here and make sure it actually gets sent to it.
            var currentHandler = Promise.Config.UncaughtRejectionHandler;
            int uncaughtHandledCount = 0;
            Promise.Config.UncaughtRejectionHandler = e =>
            {
                // The uncaught rejection could come from either the first promise or the returnPromise, since we add 2 .Finally back-to-back.
                if (!rejection1.Equals(e.Value))
                {
                    Assert.AreEqual(rejection2, e.Value);
                }
                ++uncaughtHandledCount;
            };

            const int resolveValue = 42;
            var preservedPromise = TestHelper.BuildPromise(completeType, isAlreadyComplete, resolveValue, rejection1, out var deferred)
                .Preserve();

            int finallyCount = 0;
            int completeCount = 0;
            const string captureValue = "captureValue";

            var returnPromises = new List<(Promise.Deferred deferred, Promise promise)>();
            foreach (var promise in TestHelper.GetTestablePromises(preservedPromise))
            {
                var returnPromise = TestHelper.BuildPromise(returnCompleteType, returnIsAlreadyComplete, rejection2, out var returnDeferred)
                    .Preserve();
                returnPromises.Add((returnDeferred, returnPromise));

                promise
                    .Finally(() =>
                    {
                        ++finallyCount;
                        return returnPromise;
                    })
                    .Finally(captureValue, s =>
                    {
                        Assert.AreEqual(captureValue, s);
                        ++finallyCount;
                        return returnPromise;
                    })
                    .ContinueWith(resultContainer =>
                    {
                        if (returnCompleteType == CompleteType.Resolve)
                        {
                            if (completeType == CompleteType.Resolve)
                            {
                                Assert.AreEqual(Promise.State.Resolved, resultContainer.State);
                            }
                            else if (completeType == CompleteType.Reject)
                            {
                                Assert.AreEqual(Promise.State.Rejected, resultContainer.State);
                                Assert.AreEqual(rejection1, resultContainer.Reason);
                            }
                            else
                            {
                                Assert.AreEqual(Promise.State.Canceled, resultContainer.State);
                            }
                        }
                        else if (returnCompleteType == CompleteType.Reject)
                        {
                            Assert.AreEqual(Promise.State.Rejected, resultContainer.State);
                            Assert.AreEqual(rejection2, resultContainer.Reason);
                        }
                        else
                        {
                            Assert.AreEqual(Promise.State.Canceled, resultContainer.State);
                        }
                        ++completeCount;
                    })
                    .Forget();
            }

            Assert.Greater(returnPromises.Count, 0);

            TestHelper.GetTryCompleterT(completeType, resolveValue, rejection1).Invoke(deferred);

            Assert.AreEqual(returnPromises.Count * (returnIsAlreadyComplete ? 2 : 1), finallyCount);
            Assert.AreEqual(returnIsAlreadyComplete ? returnPromises.Count : 0, completeCount);

            var completer = TestHelper.GetTryCompleterVoid(returnCompleteType, rejection2);
            foreach (var tuple in returnPromises)
            {
                completer.Invoke(tuple.deferred);
                tuple.promise.Forget();
            }

            Assert.AreEqual(returnPromises.Count, completeCount);
            int expectedUncaughtCount = completeType == CompleteType.Reject
                ? returnCompleteType == CompleteType.Reject
                    ? returnPromises.Count * 2
                    : returnCompleteType != CompleteType.Resolve // Cancel
                    ? returnPromises.Count
                    : 0
                : returnCompleteType == CompleteType.Reject
                    ? returnPromises.Count
                    : 0;
            Assert.AreEqual(expectedUncaughtCount, uncaughtHandledCount);

            preservedPromise.Forget();

            Promise.Config.UncaughtRejectionHandler = currentHandler;
        }
    }
}