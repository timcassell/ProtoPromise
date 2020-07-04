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
    public class FirstTests
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
        public void FirstIsResolvedWhenFirstPromiseIsResolvedFirst()
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();

            var resolved = 0;

            Promise.First(deferred1.Promise, deferred2.Promise)
                .Then(i => { Assert.AreEqual(5, i); ++resolved; });

            Promise.First((Promise) deferred1.Promise, deferred2.Promise)
                .Then(() => ++resolved);

            deferred1.Resolve(5);

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(2, resolved);

            deferred2.Resolve(1);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void FirstIsResolvedWhenSecondPromiseIsResolvedFirst()
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();

            var resolved = 0;

            Promise.First(deferred1.Promise, deferred2.Promise)
                .Then(i => { Assert.AreEqual(5, i); ++resolved; });

            Promise.First((Promise) deferred1.Promise, deferred2.Promise)
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
        public void FirstIsResolvedWhenFirstPromiseIsRejectedThenSecondPromiseIsResolved()
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();

            var resolved = 0;

            Promise.First(deferred1.Promise, deferred2.Promise)
                .Then(i => { Assert.AreEqual(5, i); ++resolved; });

            Promise.First((Promise) deferred1.Promise, deferred2.Promise)
                .Then(() => ++resolved);

            deferred1.Reject("Error!");
            deferred1.Promise.Catch((string _) => { });

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(0, resolved);

            deferred2.Resolve(5);

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(2, resolved);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void FirstIsResolvedWhenSecondPromiseIsRejectedThenFirstPromiseIsResolved()
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();

            var resolved = 0;

            Promise.First(deferred1.Promise, deferred2.Promise)
                .Then(i => { Assert.AreEqual(5, i); ++resolved; });

            Promise.First((Promise) deferred1.Promise, deferred2.Promise)
                .Then(() => ++resolved);

            deferred2.Reject("Error!");
            deferred2.Promise.Catch((string _) => { });

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(0, resolved);

            deferred1.Resolve(5);

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(2, resolved);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void FirstIsRejectedWhenAllPromisesAreRejected()
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();

            var rejected = 0;
            string rejection = "Error!";

            Promise.First(deferred1.Promise, deferred2.Promise)
                .Then(i => Assert.Fail("Promise was resolved when it should have been rejected."))
                .Catch<string>(rej => { Assert.AreEqual(rejection, rej); ++rejected; });

            Promise.First((Promise) deferred1.Promise, deferred2.Promise)
                .Then(() => Assert.Fail("Promise was resolved when it should have been rejected."))
                .Catch<string>(rej => { Assert.AreEqual(rejection, rej); ++rejected; });

            deferred1.Reject("Different Error!");
            deferred1.Promise.Catch((string _) => { });

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(0, rejected);

            deferred2.Reject(rejection);

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(2, rejected);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void FirstIsResolvedWhenFirstPromiseIsCanceledThenSecondPromiseIsResolved()
        {
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred1 = Promise.NewDeferred<int>(cancelationSource.Token);
            var deferred2 = Promise.NewDeferred<int>();

            var resolved = 0;

            Promise.First(deferred1.Promise, deferred2.Promise)
                .Then(i => { Assert.AreEqual(5, i); ++resolved; });

            Promise.First((Promise) deferred1.Promise, deferred2.Promise)
                .Then(() => ++resolved);

            cancelationSource.Cancel("Cancel!");

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(0, resolved);

            deferred2.Resolve(5);

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(2, resolved);

            // Clean up.
            cancelationSource.Dispose();
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void FirstIsResolvedWhenSecondPromiseIsCanceledThenFirstPromiseIsResolved()
        {
            var deferred1 = Promise.NewDeferred<int>();
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred2 = Promise.NewDeferred<int>(cancelationSource.Token);

            var resolved = 0;

            Promise.First(deferred1.Promise, deferred2.Promise)
                .Then(i => { Assert.AreEqual(5, i); ++resolved; });

            Promise.First((Promise) deferred1.Promise, deferred2.Promise)
                .Then(() => ++resolved);

            cancelationSource.Cancel("Cancel!");

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(0, resolved);

            deferred1.Resolve(5);

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(2, resolved);

            // Clean up.
            cancelationSource.Dispose();
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void FirstIsCanceledWhenAllPromisesAreCanceled()
        {
            CancelationSource cancelationSource1 = CancelationSource.New();
            var deferred1 = Promise.NewDeferred<int>(cancelationSource1.Token);
            CancelationSource cancelationSource2 = CancelationSource.New();
            var deferred2 = Promise.NewDeferred<int>(cancelationSource2.Token);

            var canceled = 0;
            string cancelation = "Cancel!";

            Promise.First(deferred1.Promise, deferred2.Promise)
                .Then(i => Assert.Fail("Promise was resolved when it should have been rejected."))
                .CatchCancelation(reason => { Assert.AreEqual(cancelation, reason.Value); ++canceled; });

            Promise.First((Promise) deferred1.Promise, deferred2.Promise)
                .Then(() => Assert.Fail("Promise was resolved when it should have been rejected."))
                .CatchCancelation(reason => { Assert.AreEqual(cancelation, reason.Value); ++canceled; });

            cancelationSource1.Cancel("Different Cancel!");

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(0, canceled);

            cancelationSource2.Cancel(cancelation);

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(2, canceled);

            // Clean up.
            cancelationSource1.Dispose();
            cancelationSource2.Dispose();
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void FirstIsRejectededWhenFirstPromiseIsCanceledThenSecondPromiseIsRejected()
        {
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred1 = Promise.NewDeferred<int>(cancelationSource.Token);
            var deferred2 = Promise.NewDeferred<int>();

            var rejected = 0;
            string rejection = "Error!";

            Promise.First(deferred1.Promise, deferred2.Promise)
                .Then(i => Assert.Fail("Promise was resolved when it should have been rejected."))
                .Catch<string>(rej => { Assert.AreEqual(rejection, rej); ++rejected; });

            Promise.First((Promise) deferred1.Promise, deferred2.Promise)
                .Then(() => Assert.Fail("Promise was resolved when it should have been rejected."))
                .Catch<string>(rej => { Assert.AreEqual(rejection, rej); ++rejected; });

            cancelationSource.Cancel("Cancel!");

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(0, rejected);

            deferred2.Reject(rejection);

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(2, rejected);

            // Clean up.
            cancelationSource.Dispose();
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void FirstIsRejectededWhenSecondPromiseIsCanceledThenFirstPromiseIsRejected()
        {
            var deferred1 = Promise.NewDeferred<int>();
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred2 = Promise.NewDeferred<int>(cancelationSource.Token);

            var rejected = 0;
            string rejection = "Error!";

            Promise.First(deferred1.Promise, deferred2.Promise)
                .Then(i => Assert.Fail("Promise was resolved when it should have been rejected."))
                .Catch<string>(rej => { Assert.AreEqual(rejection, rej); ++rejected; });

            Promise.First((Promise) deferred1.Promise, deferred2.Promise)
                .Then(() => Assert.Fail("Promise was resolved when it should have been rejected."))
                .Catch<string>(rej => { Assert.AreEqual(rejection, rej); ++rejected; });

            cancelationSource.Cancel("Cancel!");

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(0, rejected);

            deferred1.Reject(rejection);

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(2, rejected);

            // Clean up.
            cancelationSource.Dispose();
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void FirstIsCancelededWhenFirstPromiseIsRejectedThenSecondPromiseIsCanceled()
        {
            var deferred1 = Promise.NewDeferred<int>();
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred2 = Promise.NewDeferred<int>(cancelationSource.Token);

            var canceled = 0;
            string cancelation = "Cancel!";

            Promise.First(deferred1.Promise, deferred2.Promise)
                .Then(i => Assert.Fail("Promise was resolved when it should have been canceled."))
                .CatchCancelation(reason => { Assert.AreEqual(cancelation, reason.Value); ++canceled; });

            Promise.First((Promise) deferred1.Promise, deferred2.Promise)
                .Then(() => Assert.Fail("Promise was resolved when it should have been canceled."))
                .CatchCancelation(reason => { Assert.AreEqual(cancelation, reason.Value); ++canceled; });

            deferred1.Reject("Error!");
            deferred1.Promise.Catch((string _) => { });

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(0, canceled);

            cancelationSource.Cancel(cancelation);

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(2, canceled);

            // Clean up.
            cancelationSource.Dispose();
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void FirstIsCancelededWhenSecondPromiseIsRejectedThenFirstPromiseIsCanceled()
        {
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred1 = Promise.NewDeferred<int>(cancelationSource.Token);
            var deferred2 = Promise.NewDeferred<int>();

            var canceled = 0;
            string cancelation = "Cancel!";

            Promise.First(deferred1.Promise, deferred2.Promise)
                .Then(i => Assert.Fail("Promise was resolved when it should have been rejected."))
                .CatchCancelation(reason => { Assert.AreEqual(cancelation, reason.Value); ++canceled; });

            Promise.First((Promise) deferred1.Promise, deferred2.Promise)
                .Then(() => Assert.Fail("Promise was resolved when it should have been rejected."))
                .CatchCancelation(reason => { Assert.AreEqual(cancelation, reason.Value); ++canceled; });

            deferred2.Reject("Error!");
            deferred2.Promise.Catch((string _) => { });

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(0, canceled);

            cancelationSource.Cancel(cancelation);

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(2, canceled);

            // Clean up.
            cancelationSource.Dispose();
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

#if PROMISE_PROGRESS
        [Test]
        public void FirstProgressReportsTheMaximumProgress0()
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();

            float progress = float.NaN;

            Promise.First(deferred1.Promise, deferred2.Promise)
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
        public void FirstProgressReportsTheMaximumProgress1()
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();

            float progress = float.NaN;

            Promise.First(deferred1.Promise, deferred2.Promise)
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
        public void FirstProgressReportsTheMaximumProgress2()
        {
            var deferred1 = Promise.NewDeferred();

            float progress = float.NaN;

            Promise.First(deferred1.Promise, Promise.Resolved())
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
        public void FirstProgressReportsTheMaximumProgress3()
        {
            var deferred1 = Promise.NewDeferred<int>();

            float progress = float.NaN;

            Promise.First(deferred1.Promise, Promise.Resolved(1))
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