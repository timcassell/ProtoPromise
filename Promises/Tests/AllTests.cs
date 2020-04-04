#if !PROTO_PROMISE_CANCEL_DISABLE
#define PROMISE_CANCEL
#else
#undef PROMISE_CANCEL
#endif
#if !PROTO_PROMISE_PROGRESS_DISABLE
#define PROMISE_PROGRESS
#else
#undef PROMISE_PROGRESS
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

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(0, completed);

            deferred1.Resolve(1);

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(0, completed);

            deferred2.Resolve(2);

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(2, completed);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
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
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void AllPromiseIsResolvedWhenAllPromisesAreAlreadyResolved()
        {
            var completed = 0;

            Promise.All(Promise.Resolved(1), Promise.Resolved(1))
                .Then(v => ++completed);

            Promise.All(Promise.Resolved(), Promise.Resolved())
                .Then(() => ++completed);

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(2, completed);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
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

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(2, errors);

            deferred2.Resolve(2);

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(2, errors);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
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

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(0, errors);

            deferred2.Reject("Error!");

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(2, errors);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void AllPromiseIsRejectedWhenBothPromisesAreRejected()
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();

            deferred1.Promise.Catch((string _) => { });
            deferred2.Promise.Catch((string _) => { });

            var errors = 0;

            Promise.All(deferred1.Promise, deferred2.Promise)
                .Then(v => Assert.Fail("Promise was resolved when it should have been rejected."))
                .Catch<string>(e => { ++errors; });

            Promise.All((Promise) deferred1.Promise, deferred2.Promise)
                .Then(() => Assert.Fail("Promise was resolved when it should have been rejected."))
                .Catch<string>(e => { ++errors; });

            deferred1.Reject("Error!");

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(2, errors);

            deferred2.Reject("Error!");

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(2, errors);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
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

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(2, rejected);

            promise.Release();

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(2, rejected);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
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
                .Then(v => Assert.Fail("Promise was resolved when it should have been canceled."))
                .CatchCancelation(e => { ++cancelations; });

            Promise.All((Promise) deferred1.Promise, deferred2.Promise)
                .Then(() => Assert.Fail("Promise was resolved when it should have been canceled."))
                .CatchCancelation(e => { ++cancelations; });

            deferred1.Cancel("Cancel!");

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(2, cancelations);

            deferred2.Resolve(2);

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(2, cancelations);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void AllPromiseIsCanceledWhenSecondPromiseIsCanceled()
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();

            var cancelations = 0;

            Promise.All(deferred1.Promise, deferred2.Promise)
                .Then(v => Assert.Fail("Promise was resolved when it should have been canceled."))
                .CatchCancelation(e => { ++cancelations; });

            Promise.All((Promise) deferred1.Promise, deferred2.Promise)
                .Then(() => Assert.Fail("Promise was resolved when it should have been canceled."))
                .CatchCancelation(e => { ++cancelations; });

            deferred1.Resolve(2);

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(0, cancelations);

            deferred2.Cancel("Cancel!");

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(2, cancelations);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void AllPromiseIsCanceledWhenBothPromisesAreCanceled()
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();

            var cancelations = 0;

            Promise.All(deferred1.Promise, deferred2.Promise)
                .Then(v => Assert.Fail("Promise was resolved when it should have been canceled."))
                .CatchCancelation(e => { ++cancelations; });

            Promise.All((Promise) deferred1.Promise, deferred2.Promise)
                .Then(() => Assert.Fail("Promise was resolved when it should have been canceled."))
                .CatchCancelation(e => { ++cancelations; });

            deferred1.Cancel("Cancel!");

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(2, cancelations);

            deferred2.Cancel("Cancel!");

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(2, cancelations);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void AllPromiseIsCancelededWhenAnyPromiseIsAlreadyCanceled()
        {
            int cancelations = 0;
            string cancelation = "Cancel!";

            var deferred = Promise.NewDeferred<int>();
            var promise = Promise.Canceled<int, string>(cancelation);

            promise.Retain();
            Promise.Manager.HandleCompletes();

            Promise.All(deferred.Promise, promise)
                .Then(v => Assert.Fail("Promise was resolved when it should have been canceled."))
                .CatchCancelation(ex =>
                {
                    Assert.AreEqual(cancelation, ex.Value);
                    ++cancelations;
                });

            Promise.All((Promise) deferred.Promise, promise)
                .Then(() => Assert.Fail("Promise was resolved when it should have been canceled."))
                .CatchCancelation(ex =>
                {
                    Assert.AreEqual(cancelation, ex.Value);
                    ++cancelations;
                });

            deferred.Resolve(0);

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(2, cancelations);

            promise.Release();

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(2, cancelations);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }
#endif

#if PROMISE_PROGRESS
        [Test]
        public void AllProgressIsNormalized0()
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();
            var deferred3 = Promise.NewDeferred();
            var deferred4 = Promise.NewDeferred();

            float progress = float.NaN;

            Promise.All(deferred1.Promise, deferred2.Promise, deferred3.Promise, deferred4.Promise)
                .Progress(p => progress = p);

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0f / 8f, progress, 0f);

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

        [Test]
        public void AllProgressIsNormalized1()
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();
            var deferred3 = Promise.NewDeferred<int>();
            var deferred4 = Promise.NewDeferred<int>();

            float progress = float.NaN;

            Promise.All(deferred1.Promise, deferred2.Promise, deferred3.Promise, deferred4.Promise)
                .Progress(p => progress = p);

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0f / 8f, progress, 0f);

            deferred1.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f / 8f, progress, TestHelper.progressEpsilon);

            deferred1.Resolve(1);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(2f / 8f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(3f / 8f, progress, TestHelper.progressEpsilon);

            deferred2.Resolve(1);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(4f / 8f, progress, TestHelper.progressEpsilon);

            deferred3.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(5f / 8f, progress, TestHelper.progressEpsilon);

            deferred3.Resolve(1);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(6f / 8f, progress, TestHelper.progressEpsilon);

            deferred4.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(7f / 8f, progress, TestHelper.progressEpsilon);

            deferred4.Resolve(1);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(8f / 8f, progress, TestHelper.progressEpsilon);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void AllProgressIsNormalized2()
        {
            var deferred1 = Promise.NewDeferred();
            var deferred3 = Promise.NewDeferred();
            var deferred4 = Promise.NewDeferred();

            float progress = float.NaN;

            Promise.All(deferred1.Promise, Promise.Resolved(), deferred3.Promise, deferred4.Promise)
                .Progress(p => progress = p);

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(2f / 8f, progress, 0f);

            deferred1.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(3f / 8f, progress, TestHelper.progressEpsilon);

            deferred1.Resolve();
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

        [Test]
        public void AllProgressIsNormalized3()
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred3 = Promise.NewDeferred<int>();
            var deferred4 = Promise.NewDeferred<int>();

            float progress = float.NaN;

            Promise.All(deferred1.Promise, Promise.Resolved(1), deferred3.Promise, deferred4.Promise)
                .Progress(p => progress = p);

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(2f / 8f, progress, 0f);

            deferred1.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(3f / 8f, progress, TestHelper.progressEpsilon);

            deferred1.Resolve(1);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(4f / 8f, progress, TestHelper.progressEpsilon);

            deferred3.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(5f / 8f, progress, TestHelper.progressEpsilon);

            deferred3.Resolve(1);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(6f / 8f, progress, TestHelper.progressEpsilon);

            deferred4.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(7f / 8f, progress, TestHelper.progressEpsilon);

            deferred4.Resolve(1);
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