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
    public class SequenceTests
    {
        [Test]
        public void SequencePromiseIsResolvedWhenAllPromisesAreResolved()
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();

            var completed = 0;

            Promise.Sequence(() => deferred1.Promise, () => deferred2.Promise)
                .Then(() => ++completed);

            deferred1.Resolve();
            deferred2.Resolve();

            Promise.Manager.HandleCompletes();

            Assert.AreEqual(1, completed);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletes();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void SequencePromiseIsResolvedIfThereAreNoDelegates()
        {
            var completed = 0;

            Promise.Sequence(Enumerable.Empty<Func<Promise>>())
                .Then(() => ++completed);

            Promise.Manager.HandleCompletes();

            Assert.AreEqual(1, completed);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletes();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void SequencePromiseIsResolvedWhenAllPromisesAreAlreadyResolved()
        {
            var promise1 = Promise.Resolved();
            var promise2 = Promise.Resolved();

            promise1.Retain();
            promise2.Retain();
            Promise.Manager.HandleCompletes();

            var completed = 0;

            Promise.Sequence(() => promise1, () => promise2)
                .Then(() => ++completed);

            Promise.Manager.HandleCompletes();
            promise1.Release();
            promise2.Release();

            Assert.AreEqual(1, completed);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletes();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void SequencePromiseIsRejectedWhenFirstPromiseIsRejected()
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();

            var errors = 0;

            Promise.Sequence(() => deferred1.Promise, () => deferred2.Promise)
                .Then(() => Assert.Fail("Promise was resolved when it should have been rejected."))
                .Catch<string>(e => ++errors);

            deferred1.Reject("Error!");
            deferred2.Resolve();

            Promise.Manager.HandleCompletes();

            Assert.AreEqual(1, errors);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletes();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void SequencePromiseIsRejectedWhenSecondPromiseIsRejected()
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();

            var errors = 0;

            Promise.Sequence(() => deferred1.Promise, () => deferred2.Promise)
                .Then(() => Assert.Fail("Promise was resolved when it should have been rejected."))
                .Catch<string>(e => ++errors);

            deferred1.Resolve();
            deferred2.Reject("Error!");

            Promise.Manager.HandleCompletes();

            Assert.AreEqual(1, errors);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletes();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void SequenceDelegatesStopGettingInvokedWhenAPromiseIsRejected()
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();

            int invokes = 0;

            Promise.Sequence(() => { ++invokes; return deferred1.Promise; }, () => { ++invokes; return deferred2.Promise; })
                .Then(() => Assert.Fail("Promise was resolved when it should have been rejected."))
                .Catch((object e) => { if (e is AssertionException) throw (AssertionException) e; });

            deferred1.Reject("Error!");
            deferred2.Resolve();

            Promise.Manager.HandleCompletes();

            Assert.AreEqual(1, invokes);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletes();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void SequencePromiseIsRejectedWhenAnyPromiseIsAlreadyRejected()
        {
            int rejected = 0;
            string rejection = "Error!";

            var deferred = Promise.NewDeferred<int>();
            var promise = Promise.Rejected<int, string>(rejection);

            promise.Retain();
            Promise.Manager.HandleCompletes();

            Promise.Sequence(() => deferred.Promise, () => promise)
                .Then(() => Assert.Fail("Promise was resolved when it should have been rejected."))
                .Catch<string>(ex =>
                {
                    Assert.AreEqual(rejection, ex);
                    ++rejected;
                });

            deferred.Resolve(0);

            Promise.Manager.HandleCompletes();
            promise.Release();

            Assert.AreEqual(1, rejected);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletes();
            LogAssert.NoUnexpectedReceived();
        }

#if PROMISE_CANCEL
        [Test]
        public void SequencePromiseIsCanceledWhenFirstPromiseIsCanceled()
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();

            var cancelations = 0;

            Promise.Sequence(() => deferred1.Promise, () => deferred2.Promise)
                .Then(() => Assert.Fail("Promise was resolved when it should have been canceled."))
                .CatchCancelation<string>(e => ++cancelations);

            deferred1.Cancel("Cancel!");
            deferred2.Resolve();

            Promise.Manager.HandleCompletes();

            Assert.AreEqual(1, cancelations);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletes();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void SequencePromiseIsCanceledWhenSecondPromiseIsCanceled()
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();

            var cancelations = 0;

            Promise.Sequence(() => deferred1.Promise, () => deferred2.Promise)
                .Then(() => Assert.Fail("Promise was resolved when it should have been canceled."))
                .CatchCancelation<string>(e => ++cancelations);

            deferred1.Resolve();
            deferred2.Cancel("Cancel!");

            Promise.Manager.HandleCompletes();

            Assert.AreEqual(1, cancelations);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletes();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void SequenceDelegatesStopGettingInvokedWhenAPromiseIsCanceled()
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();

            int invokes = 0;

            Promise.Sequence(() => { ++invokes; return deferred1.Promise; }, () => { ++invokes; return deferred2.Promise; })
                .Then(() => Assert.Fail("Promise was resolved when it should have been canceled."));

            deferred1.Cancel("Cancel!");
            deferred2.Resolve();

            Promise.Manager.HandleCompletes();

            Assert.AreEqual(1, invokes);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletes();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void SequencePromiseIsCanceledWhenAnyPromiseIsAlreadyCanceled()
        {
            int canceled = 0;
            string cancelation = "Cancel!";

            var deferred = Promise.NewDeferred<int>();
            var promise = Promise.Canceled<int, string>(cancelation);

            promise.Retain();
            Promise.Manager.HandleCompletes();

            Promise.Sequence(() => deferred.Promise, () => promise)
                .Then(() => Assert.Fail("Promise was resolved when it should have been canceled."))
                .CatchCancelation<string>(ex =>
                {
                    Assert.AreEqual(cancelation, ex);
                    ++canceled;
                });

            deferred.Resolve(0);

            Promise.Manager.HandleCompletes();
            promise.Release();

            Assert.AreEqual(1, canceled);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletes();
            LogAssert.NoUnexpectedReceived();
        }
#endif

#if PROMISE_PROGRESS
        [Test]
        public void SequenceProgressIsNormalized()
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();
            var deferred3 = Promise.NewDeferred();
            var deferred4 = Promise.NewDeferred();

            float progress = float.NaN;

            Promise.Sequence(() => deferred1.Promise, () => deferred2.Promise, () => deferred3.Promise, () => deferred4.Promise)
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