#if !PROTO_PROMISE_PROGRESS_DISABLE
#define PROMISE_PROGRESS
#else
#undef PROMISE_PROGRESS
#endif

using System;
using NUnit.Framework;
using UnityEngine.TestTools;

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

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
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

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
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

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
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

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
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

            // Clean up.
            cancelationSource.Dispose();
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
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

            // Clean up.
            cancelationSource.Dispose();
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
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

            deferred2.Resolve();

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
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

            deferred2.Resolve(1);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
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

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
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

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }
#endif
    }
}