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
        [SetUp]
        public void Setup()
        {
            TestHelper.cachedRejectionHandler = Promise.Config.UncaughtRejectionHandler;
            Promise.Config.UncaughtRejectionHandler = null;
        }

        [TearDown]
        public void Teardown()
        {
            Promise.Config.UncaughtRejectionHandler = TestHelper.cachedRejectionHandler;
        }

        [Test]
        public void SequencePromiseIsResolvedWhenAllPromisesAreResolved()
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();

            var completed = 0;

            Promise.Sequence(() => deferred1.Promise, () => deferred2.Promise)
                .Then(() => ++completed);

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(0, completed);

            deferred1.Resolve();

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(0, completed);

            deferred2.Resolve();

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(1, completed);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
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
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void SequencePromiseIsResolvedWhenAllPromisesAreAlreadyResolved()
        {
            var completed = 0;

            Promise.Sequence(() => Promise.Resolved(), () => Promise.Resolved())
                .Then(() => ++completed);

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(1, completed);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
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

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(0, errors);

            deferred1.Reject("Error!");

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(1, errors);

            deferred2.Resolve();

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(1, errors);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
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

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(0, errors);

            deferred1.Resolve();

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(0, errors);

            deferred2.Reject("Error!");

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(1, errors);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
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

            Assert.AreEqual(0, invokes);

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(1, invokes);

            deferred1.Reject("Error!");

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(1, invokes);

            deferred2.Resolve();

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(1, invokes);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void SequencePromiseIsRejectedWhenAnyPromiseIsAlreadyRejected()
        {
            int rejected = 0;
            string rejection = "Error!";

            var deferred = Promise.NewDeferred<int>();

            Promise.Sequence(() => deferred.Promise, () => Promise.Rejected<int, string>(rejection))
                .Then(() => Assert.Fail("Promise was resolved when it should have been rejected."))
                .Catch<string>(ex =>
                {
                    Assert.AreEqual(rejection, ex);
                    ++rejected;
                });

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(0, rejected);

            deferred.Resolve(0);

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(1, rejected);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void SequencePromiseIsCanceledWhenFirstPromiseIsCanceled()
        {
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred1 = Promise.NewDeferred(cancelationSource.Token);
            var deferred2 = Promise.NewDeferred();

            var cancelations = 0;

            Promise.Sequence(() => deferred1.Promise, () => deferred2.Promise)
                .Then(() => Assert.Fail("Promise was resolved when it should have been canceled."))
                .CatchCancelation(e => ++cancelations);

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(0, cancelations);

            cancelationSource.Cancel("Cancel!");

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(1, cancelations);

            deferred2.Resolve();

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(1, cancelations);

            // Clean up.
            cancelationSource.Dispose();
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void SequencePromiseIsCanceledWhenSecondPromiseIsCanceled()
        {
            var deferred1 = Promise.NewDeferred();
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred2 = Promise.NewDeferred(cancelationSource.Token);

            var cancelations = 0;

            Promise.Sequence(() => deferred1.Promise, () => deferred2.Promise)
                .Then(() => Assert.Fail("Promise was resolved when it should have been canceled."))
                .CatchCancelation(e => ++cancelations);

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(0, cancelations);

            deferred1.Resolve();

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(0, cancelations);

            cancelationSource.Cancel("Cancel!");

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(1, cancelations);

            // Clean up.
            cancelationSource.Dispose();
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void SequenceDelegatesStopGettingInvokedWhenAPromiseIsCanceled()
        {
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred1 = Promise.NewDeferred(cancelationSource.Token);
            var deferred2 = Promise.NewDeferred();

            int invokes = 0;

            Promise.Sequence(() => { ++invokes; return deferred1.Promise; }, () => { ++invokes; return deferred2.Promise; })
                .Then(() => Assert.Fail("Promise was resolved when it should have been canceled."));

            Assert.AreEqual(0, invokes);

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(1, invokes);

            cancelationSource.Cancel("Cancel!");

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(1, invokes);

            deferred2.Resolve();

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(1, invokes);

            // Clean up.
            cancelationSource.Dispose();
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void SequencePromiseIsCanceledWhenAnyPromiseIsAlreadyCanceled()
        {
            int canceled = 0;
            string cancelation = "Cancel!";

            var deferred = Promise.NewDeferred<int>();

            Promise.Sequence(() => deferred.Promise, () => Promise.Canceled<int, string>(cancelation))
                .Then(() => Assert.Fail("Promise was resolved when it should have been canceled."))
                .CatchCancelation(reason =>
                {
                    Assert.AreEqual(cancelation, reason.Value);
                    ++canceled;
                });

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(0, canceled);

            deferred.Resolve(0);

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(1, canceled);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

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