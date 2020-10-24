#if !PROTO_PROMISE_PROGRESS_DISABLE
#define PROMISE_PROGRESS
#else
#undef PROMISE_PROGRESS
#endif

using System;
using System.Linq;
using NUnit.Framework;

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

            TestHelper.Cleanup();
        }

        [Test]
        public void SequencePromiseIsResolvedIfThereAreNoDelegates()
        {
            var completed = 0;

            Promise.Sequence(Enumerable.Empty<Func<Promise>>())
                .Then(() => ++completed);

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(1, completed);

            TestHelper.Cleanup();
        }

        [Test]
        public void SequencePromiseIsResolvedWhenAllPromisesAreAlreadyResolved()
        {
            var completed = 0;

            Promise.Sequence(() => Promise.Resolved(), () => Promise.Resolved())
                .Then(() => ++completed);

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(1, completed);

            TestHelper.Cleanup();
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

            TestHelper.Cleanup();
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

            TestHelper.Cleanup();
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

            TestHelper.Cleanup();
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

            TestHelper.Cleanup();
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

            cancelationSource.Dispose();
            TestHelper.Cleanup();
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

            cancelationSource.Dispose();
            TestHelper.Cleanup();
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

            cancelationSource.Dispose();
            TestHelper.Cleanup();
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

            TestHelper.Cleanup();
        }

        [Test]
        public void SequencePromiseIsCanceledWhenTokenIsCanceled0()
        {
            CancelationSource cancelationSource = CancelationSource.New();
            bool canceled = false;
            int invokedIndex = -1;

            Promise.Sequence(
                cancelationSource.Token,
                () =>
                {
                    invokedIndex = 0;
                    return Promise.Resolved();
                },
                () =>
                {
                    invokedIndex = 1;
                    return Promise.Resolved();
                }
            )
                .CatchCancelation(reason =>
                {
                    canceled = true;
                });

            cancelationSource.Cancel();
            Promise.Manager.HandleCompletes();

            Assert.AreEqual(-1, invokedIndex);
            Assert.IsTrue(canceled);

            cancelationSource.Dispose();
            TestHelper.Cleanup();
        }

        [Test]
        public void SequencePromiseIsCanceledWhenTokenIsCanceled1()
        {
            CancelationSource cancelationSource = CancelationSource.New();
            int invokedIndex = -1;

            var deferred = Promise.NewDeferred();

            Promise.Sequence(
                cancelationSource.Token,
                () =>
                {
                    invokedIndex = 0;
                    return deferred.Promise;
                },
                () =>
                {
                    invokedIndex = 1;
                    return Promise.Resolved();
                }
            );

            Promise.Manager.HandleCompletes();
            cancelationSource.Cancel();
            deferred.Resolve();
            Promise.Manager.HandleCompletes();

            Assert.AreEqual(0, invokedIndex);

            cancelationSource.Dispose();
            TestHelper.Cleanup();
        }

        [Test]
        public void SequencePromiseIsCanceledWhenTokenIsCanceled2()
        {
            CancelationSource cancelationSource = CancelationSource.New();
            int invokedIndex = -1;

            Promise.Sequence(
                cancelationSource.Token,
                () =>
                {
                    invokedIndex = 0;
                    cancelationSource.Cancel();
                    return Promise.Resolved();
                },
                () =>
                {
                    invokedIndex = 1;
                    return Promise.Resolved();
                }
            );

            Promise.Manager.HandleCompletes();

            Assert.AreEqual(0, invokedIndex);

            cancelationSource.Dispose();
            TestHelper.Cleanup();
        }

        [Test]
        public void SequencePromiseIsNotCanceledWhenTokenIsCanceledAfterAllCallbacksHaveBeenInvoked()
        {
            CancelationSource cancelationSource = CancelationSource.New();
            bool canceled = false;
            int invokedIndex = -1;

            Promise.Sequence(
                cancelationSource.Token,
                () =>
                {
                    invokedIndex = 0;
                    return Promise.Resolved();
                },
                () =>
                {
                    invokedIndex = 1;
                    return Promise.Resolved();
                },
                () =>
                {
                    invokedIndex = 2;
                    cancelationSource.Cancel();
                    return Promise.Resolved();
                }
            )
                .CatchCancelation(reason =>
                {
                    canceled = true;
                });

            Promise.Manager.HandleCompletes();

            Assert.AreEqual(2, invokedIndex);
            Assert.IsFalse(canceled);

            cancelationSource.Dispose();
            TestHelper.Cleanup();
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
            Assert.AreEqual(0.5f / 4f, progress, TestHelper.progressEpsilon);

            deferred1.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f / 4f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1.5f / 4f, progress, TestHelper.progressEpsilon);

            deferred2.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(2f / 4f, progress, TestHelper.progressEpsilon);

            deferred3.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(2.5f / 4f, progress, TestHelper.progressEpsilon);

            deferred3.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(3f / 4f, progress, TestHelper.progressEpsilon);

            deferred4.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(3.5f / 4f, progress, TestHelper.progressEpsilon);

            deferred4.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(4f / 4f, progress, TestHelper.progressEpsilon);

            TestHelper.Cleanup();
        }

        [Test]
        public void SequenceProgressIsNoLongerReportedFromRejected()
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();
            var deferred3 = Promise.NewDeferred();
            var deferred4 = Promise.NewDeferred();

            float progress = float.NaN;

            Promise.Sequence(() => deferred1.Promise, () => deferred2.Promise, () => deferred3.Promise, () => deferred4.Promise)
                .Progress(p => progress = p)
                .Catch(() => { });

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0f, progress, 0f);

            deferred1.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f / 4f, progress, TestHelper.progressEpsilon);

            deferred1.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f / 4f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1.5f / 4f, progress, TestHelper.progressEpsilon);

            deferred2.Reject("Reject");
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1.5f / 4f, progress, TestHelper.progressEpsilon);

            deferred3.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1.5f / 4f, progress, TestHelper.progressEpsilon);

            deferred3.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1.5f / 4f, progress, TestHelper.progressEpsilon);

            deferred4.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1.5f / 4f, progress, TestHelper.progressEpsilon);

            deferred4.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1.5f / 4f, progress, TestHelper.progressEpsilon);

            TestHelper.Cleanup();
        }

        [Test]
        public void SequenceProgressIsNoLongerReportedFromCanceled()
        {
            var deferred1 = Promise.NewDeferred();
            var cancelationSource = CancelationSource.New();
            var deferred2 = Promise.NewDeferred(cancelationSource.Token);
            var deferred3 = Promise.NewDeferred();
            var deferred4 = Promise.NewDeferred();

            float progress = float.NaN;

            Promise.Sequence(() => deferred1.Promise, () => deferred2.Promise, () => deferred3.Promise, () => deferred4.Promise)
                .Progress(p => progress = p);

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0f, progress, 0f);

            deferred1.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f / 4f, progress, TestHelper.progressEpsilon);

            deferred1.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f / 4f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1.5f / 4f, progress, TestHelper.progressEpsilon);

            cancelationSource.Cancel();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1.5f / 4f, progress, TestHelper.progressEpsilon);

            deferred3.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1.5f / 4f, progress, TestHelper.progressEpsilon);

            deferred3.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1.5f / 4f, progress, TestHelper.progressEpsilon);

            deferred4.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1.5f / 4f, progress, TestHelper.progressEpsilon);

            deferred4.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1.5f / 4f, progress, TestHelper.progressEpsilon);

            cancelationSource.Dispose();
            TestHelper.Cleanup();
        }
#endif
    }
}