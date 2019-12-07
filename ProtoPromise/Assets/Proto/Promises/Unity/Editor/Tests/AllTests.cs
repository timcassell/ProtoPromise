#if !PROTO_PROMISE_CANCEL_DISABLE
#define PROMISE_CANCEL
#endif
#if !PROTO_PROMISE_PROGRESS_DISABLE
#define PROMISE_PROGRESS
#endif

using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Proto.Promises.Tests
{
    public class AllTests
    {
        [Test]
        public void AllPromiseIsResolvedWhenAllPromisesAreResolved()
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();

            var completed = 0;

            Promise.All(deferred1.Promise, deferred2.Promise)
                .Then(values =>
                {
                    ++completed;

                    Assert.AreEqual(2, values.Count);
                    Assert.AreEqual(1, values[0]);
                    Assert.AreEqual(2, values[1]);
                });

            Promise.All((Promise) deferred1.Promise, deferred2.Promise)
                .Then(() => ++completed);

            deferred1.Resolve(1);
            deferred2.Resolve(2);

            Promise.Manager.HandleCompletes();

            Assert.AreEqual(2, completed);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletes();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void AllPromiseIsResolvedIfThereAreNoPromises()
        {
            var completed = 0;

            Promise.All(Enumerable.Empty<Promise<int>>())
                .Then(v =>
                {
                    ++completed;

                    Assert.IsEmpty(v);
                });

            Promise.All(Enumerable.Empty<Promise>())
                .Then(() => ++completed);

            Promise.Manager.HandleCompletes();

            Assert.AreEqual(2, completed);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletes();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void AllPromiseIsResolvedWhenAllPromisesAreAlreadyResolved()
        {
            var promise1 = Promise.Resolved(1);
            var promise2 = Promise.Resolved(1);

            promise1.Retain();
            promise2.Retain();
            Promise.Manager.HandleCompletes();

            var completed = 0;

            Promise.All(promise1, promise2)
                .Then(v => ++completed);

            Promise.All((Promise) promise1, promise2)
                .Then(() => ++completed);

            promise1.Release();
            promise2.Release();
            Promise.Manager.HandleCompletes();

            Assert.AreEqual(2, completed);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletes();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void AllPromiseIsRejectedWhenFirstPromiseIsRejected()
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();

            var errors = 0;

            Promise.All(deferred1.Promise, deferred2.Promise)
                .Then(v => Assert.Fail("Promise was resolved when it should have been rejected."))
                .Catch<string>(e => { ++errors; });

            Promise.All((Promise) deferred1.Promise, deferred2.Promise)
                .Then(() => Assert.Fail("Promise was resolved when it should have been rejected."))
                .Catch<string>(e => { ++errors; });

            deferred1.Reject("Error!");
            deferred2.Resolve(2);

            Promise.Manager.HandleCompletes();

            Assert.AreEqual(2, errors);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletes();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void AllPromiseIsRejectedWhenSecondPromiseIsRejected()
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();

            var errors = 0;

            Promise.All(deferred1.Promise, deferred2.Promise)
                .Then(v => Assert.Fail("Promise was resolved when it should have been rejected."))
                .Catch<string>(e => { ++errors; });

            Promise.All((Promise) deferred1.Promise, deferred2.Promise)
                .Then(() => Assert.Fail("Promise was resolved when it should have been rejected."))
                .Catch<string>(e => { ++errors; });

            deferred1.Resolve(2);
            deferred2.Reject("Error!");

            Promise.Manager.HandleCompletes();

            Assert.AreEqual(2, errors);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletes();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void AllPromiseIsRejectedWhenBothPromisesAreRejected()
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();

            var errors = 0;

            Promise.All(deferred1.Promise, deferred2.Promise)
                .Then(v => Assert.Fail("Promise was resolved when it should have been rejected."))
                .Catch<string>(e => { ++errors; });

            Promise.All((Promise) deferred1.Promise, deferred2.Promise)
                .Then(() => Assert.Fail("Promise was resolved when it should have been rejected."))
                .Catch<string>(e => { ++errors; });

            deferred1.Reject("Error!");
            deferred2.Reject("Error!");

            // Only 1 rejection is caught, so expect an unhandled throw.
            Assert.Throws<AggregateException>(Promise.Manager.HandleCompletes);

            Assert.AreEqual(2, errors);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletes();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void AllPromiseIsRejectedWhenAnyPromiseIsAlreadyRejected()
        {
            int rejected = 0;
            string rejection = "Error!";

            var deferred = Promise.NewDeferred<int>();
            var promise = Promise.Rejected<int, string>(rejection);

            promise.Retain();
            Promise.Manager.HandleCompletes();

            Promise.All(deferred.Promise, promise)
                .Then(v => Assert.Fail("Promise was resolved when it should have been rejected."))
                .Catch<string>(ex =>
                {
                    Assert.AreEqual(rejection, ex);
                    ++rejected;
                });

            Promise.All((Promise) deferred.Promise, promise)
                .Then(() => Assert.Fail("Promise was resolved when it should have been rejected."))
                .Catch<string>(ex =>
                {
                    Assert.AreEqual(rejection, ex);
                    ++rejected;
                });

            deferred.Resolve(0);
            promise.Release();

            Promise.Manager.HandleCompletes();

            Assert.AreEqual(2, rejected);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletes();
            LogAssert.NoUnexpectedReceived();
        }

#if PROMISE_CANCEL
        [Test]
        public void AllPromiseIsCanceledWhenFirstPromiseIsCanceled()
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();

            var cancelations = 0;

            Promise.All(deferred1.Promise, deferred2.Promise)
                .Then(v => Assert.Fail("Promise was resolved when it should have been rejected."))
                .CatchCancelation<string>(e => { ++cancelations; });

            Promise.All((Promise) deferred1.Promise, deferred2.Promise)
                .Then(() => Assert.Fail("Promise was resolved when it should have been rejected."))
                .CatchCancelation<string>(e => { ++cancelations; });

            deferred1.Cancel("Cancel!");
            deferred2.Resolve(2);

            Promise.Manager.HandleCompletes();

            Assert.AreEqual(2, cancelations);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletes();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void AllPromiseIsCanceledWhenSecondPromiseIsCanceled()
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();

            var cancelations = 0;

            Promise.All(deferred1.Promise, deferred2.Promise)
                .Then(v => Assert.Fail("Promise was resolved when it should have been rejected."))
                .CatchCancelation<string>(e => { ++cancelations; });

            Promise.All((Promise) deferred1.Promise, deferred2.Promise)
                .Then(() => Assert.Fail("Promise was resolved when it should have been rejected."))
                .CatchCancelation<string>(e => { ++cancelations; });

            deferred1.Resolve(2);
            deferred2.Cancel("Cancel!");

            Promise.Manager.HandleCompletes();

            Assert.AreEqual(2, cancelations);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletes();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void AllPromiseIsCanceledWhenBothPromisesAreCanceled()
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();

            var cancelations = 0;

            Promise.All(deferred1.Promise, deferred2.Promise)
                .Then(v => Assert.Fail("Promise was resolved when it should have been rejected."))
                .CatchCancelation<string>(e => { ++cancelations; });

            Promise.All((Promise) deferred1.Promise, deferred2.Promise)
                .Then(() => Assert.Fail("Promise was resolved when it should have been rejected."))
                .CatchCancelation<string>(e => { ++cancelations; });

            deferred1.Cancel("Cancel!");
            deferred2.Cancel("Cancel!");

            Promise.Manager.HandleCompletes();

            Assert.AreEqual(2, cancelations);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletes();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void AllPromiseIsCancelededWhenAnyPromiseIsAlreadyCanceled()
        {
            int rejected = 0;
            string cancelation = "Cancel!";

            var deferred = Promise.NewDeferred<int>();
            var promise = Promise.Canceled<int, string>(cancelation);

            promise.Retain();
            Promise.Manager.HandleCompletes();

            Promise.All(deferred.Promise, promise)
                .Then(v => Assert.Fail("Promise was resolved when it should have been rejected."))
                .CatchCancelation<string>(ex =>
                {
                    Assert.AreEqual(cancelation, ex);
                    ++rejected;
                });

            Promise.All((Promise) deferred.Promise, promise)
                .Then(() => Assert.Fail("Promise was resolved when it should have been rejected."))
                .CatchCancelation<string>(ex =>
                {
                    Assert.AreEqual(cancelation, ex);
                    ++rejected;
                });

            deferred.Resolve(0);
            promise.Release();

            Promise.Manager.HandleCompletes();

            Assert.AreEqual(2, rejected);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletes();
            LogAssert.NoUnexpectedReceived();
        }
#endif

#if PROMISE_PROGRESS
        [Test]
        public void AllProgressIsNormalized()
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();
            var deferred3 = Promise.NewDeferred();
            var deferred4 = Promise.NewDeferred();

            float progress = float.NaN;

            Promise.All(deferred1.Promise, deferred2.Promise, deferred3.Promise, deferred4.Promise)
                .Progress(p => progress = p);

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0f, progress, 0f);

            deferred1.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f / 8f, progress, TestHelper.progressEpsilon);

            deferred1.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(2f / 8f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(3f / 8f, progress, TestHelper.progressEpsilon);

            deferred2.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(4f / 8f, progress, TestHelper.progressEpsilon);

            deferred3.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(5f / 8f, progress, TestHelper.progressEpsilon);

            deferred3.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(6f / 8f, progress, TestHelper.progressEpsilon);

            deferred4.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(7f / 8f, progress, TestHelper.progressEpsilon);

            deferred4.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(8f / 8f, progress, TestHelper.progressEpsilon);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }
#endif
    }
}