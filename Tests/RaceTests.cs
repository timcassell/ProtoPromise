#if !PROTO_PROMISE_PROGRESS_DISABLE
#define PROMISE_PROGRESS
#else
#undef PROMISE_PROGRESS
#endif

using NUnit.Framework;

namespace Proto.Promises.Tests
{
    public class RaceTests
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
        public void RaceIsResolvedWhenFirstPromiseIsResolvedFirst()
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();

            var resolved = 0;

            //Promise.Race(deferred1.Promise, deferred2.Promise)
            //.Then(i => { Assert.AreEqual(5, i); ++resolved; });

            Promise.Race((Promise) deferred1.Promise, deferred2.Promise)
                .Then(() => ++resolved);

            deferred1.Resolve(5);

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(1, resolved);

            deferred2.Resolve(1);

            TestHelper.Cleanup();
        }

        [Test]
        public void RaceIsResolvedWhenSecondPromiseIsResolvedFirst()
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();

            var resolved = 0;

            Promise.Race(deferred1.Promise, deferred2.Promise)
                .Then(i => { Assert.AreEqual(5, i); ++resolved; });

            Promise.Race((Promise) deferred1.Promise, deferred2.Promise)
                .Then(() => ++resolved);

            deferred2.Resolve(5);

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(2, resolved);

            deferred1.Resolve(1);

            TestHelper.Cleanup();
        }

        [Test]
        public void RaceIsRejectedWhenFirstPromiseIsRejectedFirst()
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();

            var invoked = 0;
            string expected = "Error!";

            Promise.Race(deferred1.Promise, deferred2.Promise)
                .Then(i => Assert.Fail("Promise was resolved when it should have been rejected."))
                .Catch<string>(rej => { Assert.AreEqual(expected, rej); ++invoked; });

            Promise.Race((Promise) deferred1.Promise, deferred2.Promise)
                .Then(() => Assert.Fail("Promise was resolved when it should have been rejected."))
                .Catch<string>(rej => { Assert.AreEqual(expected, rej); ++invoked; });

            deferred1.Reject(expected);

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(2, invoked);

            deferred2.Resolve(5);

            TestHelper.Cleanup();
        }

        [Test]
        public void RaceIsRejectedWhenSecondPromiseIsRejectedFirst()
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();

            var invoked = 0;
            string expected = "Error!";

            Promise.Race(deferred1.Promise, deferred2.Promise)
                .Then(i => Assert.Fail("Promise was resolved when it should have been rejected."))
                .Catch<string>(rej => { Assert.AreEqual(expected, rej); ++invoked; });

            Promise.Race((Promise) deferred1.Promise, deferred2.Promise)
                .Then(() => Assert.Fail("Promise was resolved when it should have been rejected."))
                .Catch<string>(rej => { Assert.AreEqual(expected, rej); ++invoked; });

            deferred2.Reject(expected);

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(2, invoked);

            deferred1.Resolve(5);

            TestHelper.Cleanup();
        }

        [Test]
        public void RaceIsCanceledWhenFirstPromiseIsCanceledFirst()
        {
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred1 = Promise.NewDeferred<int>(cancelationSource.Token);
            var deferred2 = Promise.NewDeferred<int>();

            var invoked = 0;
            string expected = "Error!";

            Promise.Race(deferred1.Promise, deferred2.Promise)
                .Then(i => Assert.Fail("Promise was resolved when it should have been canceled."))
                .CatchCancelation(reason => { Assert.AreEqual(expected, reason.Value); ++invoked; });

            Promise.Race((Promise) deferred1.Promise, deferred2.Promise)
                .Then(() => Assert.Fail("Promise was resolved when it should have been canceled."))
                .CatchCancelation(reason => { Assert.AreEqual(expected, reason.Value); ++invoked; });

            cancelationSource.Cancel(expected);

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(2, invoked);

            deferred2.Resolve(5);

            cancelationSource.Dispose();
            TestHelper.Cleanup();
        }

        [Test]
        public void RaceIsCanceledWhenSecondPromiseIsCanceledFirst()
        {
            var deferred1 = Promise.NewDeferred<int>();
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred2 = Promise.NewDeferred<int>(cancelationSource.Token);

            var invoked = 0;
            string expected = "Error!";

            Promise.Race(deferred1.Promise, deferred2.Promise)
                .Then(i => Assert.Fail("Promise was resolved when it should have been canceled."))
                .CatchCancelation(reason => { Assert.AreEqual(expected, reason.Value); ++invoked; });

            Promise.Race((Promise) deferred1.Promise, deferred2.Promise)
                .Then(() => Assert.Fail("Promise was resolved when it should have been canceled."))
                .CatchCancelation(reason => { Assert.AreEqual(expected, reason.Value); ++invoked; });

            cancelationSource.Cancel(expected);

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(2, invoked);

            deferred1.Resolve(5);

            cancelationSource.Dispose();
            TestHelper.Cleanup();
        }

#if PROMISE_PROGRESS
        [Test]
        public void RaceProgressReportsTheMaximumProgress0()
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();

            float progress = float.NaN;

            Promise.Race(deferred1.Promise, deferred2.Promise)
                .Progress(p => progress = p);

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0f, progress, 0f);

            deferred1.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.3f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.7f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.7f, progress, TestHelper.progressEpsilon);

            deferred1.ReportProgress(0.6f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.7f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.8f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.8f, progress, TestHelper.progressEpsilon);

            deferred1.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.9f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);

            deferred2.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);

            TestHelper.Cleanup();
        }

        [Test]
        public void RaceProgressReportsTheMaximumProgress1()
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();

            float progress = float.NaN;

            Promise.Race(deferred1.Promise, deferred2.Promise)
                .Progress(p => progress = p);

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0f, progress, 0f);

            deferred1.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.3f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.7f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.7f, progress, TestHelper.progressEpsilon);

            deferred1.ReportProgress(0.6f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.7f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.8f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.8f, progress, TestHelper.progressEpsilon);

            deferred1.Resolve(1);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.9f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);

            deferred2.Resolve(1);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);

            TestHelper.Cleanup();
        }

        [Test]
        public void RaceProgressReportsTheMaximumProgress2()
        {
            var deferred1 = Promise.NewDeferred();

            float progress = float.NaN;

            Promise.Race(deferred1.Promise, Promise.Resolved())
                .Progress(p => progress = p);

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, 0f);

            deferred1.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, 0f);

            TestHelper.Cleanup();
        }

        [Test]
        public void RaceProgressReportsTheMaximumProgress3()
        {
            var deferred1 = Promise.NewDeferred<int>();

            float progress = float.NaN;

            Promise.Race(deferred1.Promise, Promise.Resolved(1))
                .Progress(p => progress = p);

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, 0f);

            deferred1.Resolve(1);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, 0f);

            TestHelper.Cleanup();
        }

        [Test]
        public void RaceProgressReportsTheMaximumProgress4()
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();

            float progress = float.NaN;

            Promise.Race
            (
                deferred1.Promise
                    .Then(() => { }),
                deferred2.Promise
                    .Then(() => { })
            )
                .Progress(p => progress = p);

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0f, progress, 0f);

            deferred1.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.3f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.7f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.7f, progress, TestHelper.progressEpsilon);

            deferred1.ReportProgress(0.6f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.7f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.8f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.8f, progress, TestHelper.progressEpsilon);

            deferred1.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.9f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);

            deferred2.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);

            TestHelper.Cleanup();
        }

        [Test]
        public void RaceProgressReportsTheMaximumProgress5()
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();

            float progress = float.NaN;

            Promise.Race
            (
                deferred1.Promise
                    .Then(() => 1),
                deferred2.Promise
                    .Then(() => 1)
            )
                .Progress(p => progress = p);

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0f, progress, 0f);

            deferred1.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.3f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.7f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.7f, progress, TestHelper.progressEpsilon);

            deferred1.ReportProgress(0.6f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.7f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.8f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.8f, progress, TestHelper.progressEpsilon);

            deferred1.Resolve(1);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.9f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);

            deferred2.Resolve(1);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);

            TestHelper.Cleanup();
        }

        [Test]
        public void RaceProgressReportsTheMaximumProgress6()
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();
            var deferred3 = Promise.NewDeferred();
            var deferred4 = Promise.NewDeferred();

            float progress = float.NaN;

            Promise.Race
            (
                deferred1.Promise
                    .Then(() => deferred3.Promise),
                deferred2.Promise
                    .Then(() => deferred4.Promise)
            )
                .Progress(p => progress = p);

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0f, progress, 0f);

            deferred1.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f / 2f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.3f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f / 2f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.7f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.7f / 2f, progress, TestHelper.progressEpsilon);

            deferred1.ReportProgress(0.6f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.7f / 2f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.8f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.8f / 2f, progress, TestHelper.progressEpsilon);

            deferred1.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f / 2f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.9f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f / 2f, progress, TestHelper.progressEpsilon);

            deferred3.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1.5f / 2f, progress, TestHelper.progressEpsilon);

            deferred2.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1.5f / 2f, progress, TestHelper.progressEpsilon);

            deferred4.ReportProgress(0.3f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1.5f / 2f, progress, TestHelper.progressEpsilon);

            deferred4.ReportProgress(0.7f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1.7f / 2f, progress, TestHelper.progressEpsilon);

            deferred3.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);

            deferred4.ReportProgress(0.9f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);

            deferred4.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);

            TestHelper.Cleanup();
        }

        [Test]
        public void RaceProgressReportsTheMaximumProgress7()
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();
            var deferred3 = Promise.NewDeferred<int>();
            var deferred4 = Promise.NewDeferred<int>();

            float progress = float.NaN;

            Promise.Race
            (
                deferred1.Promise
                    .Then(() => deferred3.Promise),
                deferred2.Promise
                    .Then(() => deferred4.Promise)
            )
                .Progress(p => progress = p);

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0f, progress, 0f);

            deferred1.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f / 2f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.3f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f / 2f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.7f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.7f / 2f, progress, TestHelper.progressEpsilon);

            deferred1.ReportProgress(0.6f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.7f / 2f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.8f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.8f / 2f, progress, TestHelper.progressEpsilon);

            deferred1.Resolve(1);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f / 2f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.9f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f / 2f, progress, TestHelper.progressEpsilon);

            deferred3.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1.5f / 2f, progress, TestHelper.progressEpsilon);

            deferred2.Resolve(1);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1.5f / 2f, progress, TestHelper.progressEpsilon);

            deferred4.ReportProgress(0.3f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1.5f / 2f, progress, TestHelper.progressEpsilon);

            deferred4.ReportProgress(0.7f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1.7f / 2f, progress, TestHelper.progressEpsilon);

            deferred3.Resolve(1);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);

            deferred4.ReportProgress(0.9f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);

            deferred4.Resolve(1);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);

            TestHelper.Cleanup();
        }

        [Test]
        public void RaceProgressReportsTheMaximumProgress8()
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();

            float progress = float.NaN;

            Promise.Race
            (
                deferred1.Promise
                    .Then(Promise.Resolved),
                deferred2.Promise
                    .Then(Promise.Resolved)
            )
                .Progress(p => progress = p);

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0f, progress, 0f);

            deferred1.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f / 2f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.3f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f / 2f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.7f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.7f / 2f, progress, TestHelper.progressEpsilon);

            deferred1.ReportProgress(0.6f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.7f / 2f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.8f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.8f / 2f, progress, TestHelper.progressEpsilon);

            deferred1.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.9f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);

            deferred2.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);

            TestHelper.Cleanup();
        }

        [Test]
        public void RaceProgressReportsTheMaximumProgress9()
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();

            float progress = float.NaN;

            Promise.Race
            (
                deferred1.Promise
                    .Then(x => Promise.Resolved(x)),
                deferred2.Promise
                    .Then(x => Promise.Resolved(x))
            )
                .Progress(p => progress = p);

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0f, progress, 0f);

            deferred1.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f / 2f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.3f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f / 2f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.7f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.7f / 2f, progress, TestHelper.progressEpsilon);

            deferred1.ReportProgress(0.6f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.7f / 2f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.8f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.8f / 2f, progress, TestHelper.progressEpsilon);

            deferred1.Resolve(1);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.9f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);

            deferred2.Resolve(1);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);

            TestHelper.Cleanup();
        }

        [Test]
        public void RaceProgressIsNoLongerReportedFromRejected0()
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();

            float progress = float.NaN;

            Promise.Race(deferred1.Promise, deferred2.Promise)
                .Progress(p => progress = p)
                .Catch(() => { });

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0f, progress, 0f);

            deferred1.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.7f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.7f, progress, TestHelper.progressEpsilon);

            deferred2.Reject("Reject");
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.7f, progress, TestHelper.progressEpsilon);

            deferred1.ReportProgress(0.8f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.7f, progress, TestHelper.progressEpsilon);

            deferred1.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.7f, progress, TestHelper.progressEpsilon);

            TestHelper.Cleanup();
        }

        [Test]
        public void RaceProgressIsNoLongerReportedFromRejected1()
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();

            float progress = float.NaN;

            Promise.Race(deferred1.Promise, deferred2.Promise)
                .Progress(p => progress = p)
                .Catch(() => { });

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0f, progress, 0f);

            deferred1.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.7f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.7f, progress, TestHelper.progressEpsilon);

            deferred2.Reject("Reject");
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.7f, progress, TestHelper.progressEpsilon);

            deferred1.ReportProgress(0.8f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.7f, progress, TestHelper.progressEpsilon);

            deferred1.Resolve(1);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.7f, progress, TestHelper.progressEpsilon);

            TestHelper.Cleanup();
        }

        [Test]
        public void RaceProgressIsNoLongerReportedFromCanceled0()
        {
            var deferred1 = Promise.NewDeferred();
            var cancelationSource1 = CancelationSource.New();
            var deferred2 = Promise.NewDeferred(cancelationSource1.Token);

            float progress = float.NaN;

            Promise.Race(deferred1.Promise, deferred2.Promise)
                .Progress(p => progress = p);

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0f, progress, 0f);

            deferred1.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.7f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.7f, progress, TestHelper.progressEpsilon);

            cancelationSource1.Cancel();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.7f, progress, TestHelper.progressEpsilon);

            deferred1.ReportProgress(0.8f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.7f, progress, TestHelper.progressEpsilon);

            deferred1.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.7f, progress, TestHelper.progressEpsilon);

            cancelationSource1.Dispose();
            TestHelper.Cleanup();
        }

        [Test]
        public void RaceProgressIsNoLongerReportedFromCanceled1()
        {
            var deferred1 = Promise.NewDeferred();
            var cancelationSource1 = CancelationSource.New();
            var deferred2 = Promise.NewDeferred(cancelationSource1.Token);

            float progress = float.NaN;

            Promise.Race(deferred1.Promise, deferred2.Promise)
                .Progress(p => progress = p);

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0f, progress, 0f);

            deferred1.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.7f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.7f, progress, TestHelper.progressEpsilon);

            cancelationSource1.Cancel();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.7f, progress, TestHelper.progressEpsilon);

            deferred1.ReportProgress(0.8f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.7f, progress, TestHelper.progressEpsilon);

            deferred1.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.7f, progress, TestHelper.progressEpsilon);

            cancelationSource1.Dispose();
            TestHelper.Cleanup();
        }

        [Test]
        public void RaceProgressWillBeInvokedProperlyFromARecoveredPromise0()
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();
            var deferred3 = Promise.NewDeferred();
            var deferred4 = Promise.NewDeferred();
            var cancelationSource = CancelationSource.New();

            float progress = float.NaN;

            Promise.Race
            (
                // Make first and second promise chains the same length
                deferred1.Promise
                    .Then(Promise.Resolved)
                    .Then(Promise.Resolved),
                deferred2.Promise
                    .Then(() => deferred3.Promise, cancelationSource.Token)
                    .ContinueWith(_ => deferred4.Promise)
            )
                .Progress(p => progress = p);

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0f / 3f, progress, TestHelper.progressEpsilon);

            deferred1.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f / 3f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.25f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f / 3f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.6f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.6f / 3f, progress, TestHelper.progressEpsilon);

            cancelationSource.Cancel();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(2f / 3f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.7f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(2f / 3f, progress, TestHelper.progressEpsilon);

            deferred3.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(2f / 3f, progress, TestHelper.progressEpsilon);

            deferred3.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(2f / 3f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.8f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(2f / 3f, progress, TestHelper.progressEpsilon);

            deferred4.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(2.5f / 3f, progress, TestHelper.progressEpsilon);

            deferred4.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.8f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);

            deferred2.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);

            deferred1.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);

            cancelationSource.Dispose();
            TestHelper.Cleanup();
        }

        [Test]
        public void RaceProgressWillBeInvokedProperlyFromARecoveredPromise1()
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();
            var deferred3 = Promise.NewDeferred<int>();
            var deferred4 = Promise.NewDeferred<int>();
            var cancelationSource = CancelationSource.New();

            float progress = float.NaN;

            Promise.Race
            (
                // Make first and second promise chains the same length
                deferred1.Promise
                    .Then(x => Promise.Resolved(x))
                    .Then(x => Promise.Resolved(x)),
                deferred2.Promise
                    .Then(() => deferred3.Promise, cancelationSource.Token)
                    .ContinueWith(_ => deferred4.Promise)
            )
                .Progress(p => progress = p);

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0f / 3f, progress, TestHelper.progressEpsilon);

            deferred1.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f / 3f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.25f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f / 3f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.6f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.6f / 3f, progress, TestHelper.progressEpsilon);

            cancelationSource.Cancel();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(2f / 3f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.7f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(2f / 3f, progress, TestHelper.progressEpsilon);

            deferred3.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(2f / 3f, progress, TestHelper.progressEpsilon);

            deferred3.Resolve(1);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(2f / 3f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.8f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(2f / 3f, progress, TestHelper.progressEpsilon);

            deferred4.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(2.5f / 3f, progress, TestHelper.progressEpsilon);

            deferred4.Resolve(1);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.8f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);

            deferred2.Resolve(1);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);

            deferred1.Resolve(1);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);

            cancelationSource.Dispose();
            TestHelper.Cleanup();
        }
#endif
    }
}