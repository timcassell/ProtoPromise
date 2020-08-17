#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif
#if !PROTO_PROMISE_PROGRESS_DISABLE
#define PROMISE_PROGRESS
#else
#undef PROMISE_PROGRESS
#endif

#if PROMISE_PROGRESS
using System;
using NUnit.Framework;

namespace Proto.Promises.Tests
{
    public class ProgressTests
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
        public void OnProgressMayBeInvokedWhenThePromisesProgressHasChanged()
        {
            var deferred = Promise.NewDeferred();
            Assert.AreEqual(Promise.State.Pending, deferred.State);

            float progress = float.NaN;

            deferred.Promise.Progress(p => progress = p);

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0f, progress, 0f);

            deferred.ReportProgress(0.25f);
            Assert.AreEqual(0f, progress, TestHelper.progressEpsilon);

            deferred.ReportProgress(0.5f);
            Assert.AreEqual(0f, progress, TestHelper.progressEpsilon);

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f, progress, TestHelper.progressEpsilon);

            deferred.Resolve();

            TestHelper.Cleanup();
        }

        [Test]
        public void OnProgressMayBeInvokedWithTheCapturedValueWhenThePromisesProgressHasChanged()
        {
            var deferred = Promise.NewDeferred();
            Assert.AreEqual(Promise.State.Pending, deferred.State);

            float progress = float.NaN;
            string capturedValue = "Capture Value";

            deferred.Promise.Progress(
                capturedValue,
                (cv, p) =>
                {
                    Assert.AreEqual(capturedValue, cv);
                    progress = p;
                });

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0f, progress, 0f);

            deferred.ReportProgress(0.25f);
            Promise.Manager.HandleCompletesAndProgress();

            deferred.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();

            deferred.ReportProgress(0.75f);
            Promise.Manager.HandleCompletesAndProgress();

            deferred.Resolve();
            Promise.Manager.HandleCompletesAndProgress();

            TestHelper.Cleanup();
        }

        [Test]
        public void OnProgressWillBeInvokedWith1WhenPromiseIsResolved()
        {
            var deferred = Promise.NewDeferred();
            Assert.AreEqual(Promise.State.Pending, deferred.State);

            float progress = float.NaN;

            deferred.Promise.Progress(p => progress = p);

            deferred.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, 0f);

            TestHelper.Cleanup();
        }

        [Test]
        public void OnProgressWillNoLongerBeInvokedWhenPromiseIsRejected()
        {
            var deferred = Promise.NewDeferred();
            Assert.AreEqual(Promise.State.Pending, deferred.State);

            float progress = float.NaN;

            deferred.Promise.Progress(p => progress = p)
                .Catch(() => { });

            deferred.Reject("Fail Value");
            Promise.Manager.HandleCompletesAndProgress();
            Assert.IsNaN(progress);

            TestHelper.Cleanup();
        }

        [Test]
        public void OnProgressWillNoLongerBeInvokedWhenAPromiseIsRejectedAndContinueBeingInvokedWhenAChainedPromisesProgressIsUpdated()
        {
            var deferred = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();
            Assert.AreEqual(Promise.State.Pending, deferred.State);
            Assert.AreEqual(Promise.State.Pending, deferred2.State);

            float progress = float.NaN;

            deferred.Promise
                .Catch(() => deferred2.Promise)
                .Progress(p => progress = p);

            deferred.ReportProgress(0.5f);
            deferred.Reject("Fail Value");
            Promise.Manager.HandleCompletesAndProgress();
            Assert.IsNaN(progress);

            deferred2.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.75f, progress, TestHelper.progressEpsilon);

            deferred2.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);

            TestHelper.Cleanup();
        }

        [Test]
        public void OnProgressWillNoLongerBeInvokedWhenPromiseIsCanceled0()
        {
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred(cancelationSource.Token);
            Assert.AreEqual(Promise.State.Pending, deferred.State);

            float progress = float.NaN;

            deferred.Promise.Progress(p => progress = p);

            cancelationSource.Cancel();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.IsNaN(progress);

            cancelationSource.Dispose();
            TestHelper.Cleanup();
        }

        [Test]
        public void OnProgressWillNoLongerBeInvokedWhenPromiseIsCanceled1()
        {
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred();
            Assert.AreEqual(Promise.State.Pending, deferred.State);

            float progress = float.NaN;

            deferred.Promise
                .ThenDuplicate()
                .Then(() => { }, cancelationSource.Token)
                .Progress(p => progress = p)
                .Finally(cancelationSource.Dispose);

            deferred.ReportProgress(0.25f);
            cancelationSource.Cancel();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.IsNaN(progress);

            deferred.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.IsNaN(progress);

            deferred.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.IsNaN(progress);

            TestHelper.Cleanup();
        }

        [Test]
        public void OnProgressWillNoLongerBeInvokedWhenPromiseIsCanceled2()
        {
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred();
            Assert.AreEqual(Promise.State.Pending, deferred.State);
            var deferred2 = Promise.NewDeferred();
            Assert.AreEqual(Promise.State.Pending, deferred2.State);

            float progress = float.NaN;

            deferred.Promise
                .ThenDuplicate()
                .Then(() =>
                    deferred2.Promise
                        .Then(() => { }, cancelationSource.Token)
                )
                .Progress(p => progress = p)
                .Finally(cancelationSource.Dispose);

            deferred.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.25f, progress, TestHelper.progressEpsilon);

            deferred.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.5f);
            cancelationSource.Cancel();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f, progress, TestHelper.progressEpsilon);

            deferred2.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f, progress, TestHelper.progressEpsilon);

            TestHelper.Cleanup();
        }

        [Test]
        public void OnProgressWillNoLongerBeInvokedWhenPromiseIsCanceled3()
        {
            var deferred = Promise.NewDeferred();
            Assert.AreEqual(Promise.State.Pending, deferred.State);
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred2 = Promise.NewDeferred(cancelationSource.Token);
            Assert.AreEqual(Promise.State.Pending, deferred2.State);

            float progress = float.NaN;

            deferred.Promise
                .ThenDuplicate()
                .Then(() => deferred2.Promise)
                .Progress(p => progress = p);

            deferred.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.25f, progress, TestHelper.progressEpsilon);

            deferred.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.5f);
            cancelationSource.Cancel();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f, progress, TestHelper.progressEpsilon);

            cancelationSource.Dispose();
            TestHelper.Cleanup();
        }

        [Test]
        public void OnProgressWillNoLongerBeInvokedWhenTokenIsCanceled0()
        {
            var deferred = Promise.NewDeferred();
            CancelationSource cancelationSource = CancelationSource.New();

            float progress = float.NaN;

            deferred.Promise
                .Progress(p => progress = p, cancelationSource.Token);

            deferred.ReportProgress(0.25f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.25f, progress, TestHelper.progressEpsilon);

            cancelationSource.Cancel();
            deferred.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.25f, progress, TestHelper.progressEpsilon);

            deferred.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.25f, progress, TestHelper.progressEpsilon);

            cancelationSource.Dispose();
            TestHelper.Cleanup();
        }

        [Test]
        public void OnProgressWillNoLongerBeInvokedWhenTokenIsCanceled1()
        {
            var deferred = Promise.NewDeferred();
            CancelationSource cancelationSource = CancelationSource.New();

            float progress = float.NaN;

            deferred.Promise
                .Progress(1, (cv, p) => progress = p, cancelationSource.Token);

            deferred.ReportProgress(0.25f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.25f, progress, TestHelper.progressEpsilon);

            cancelationSource.Cancel();
            deferred.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.25f, progress, TestHelper.progressEpsilon);

            deferred.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.25f, progress, TestHelper.progressEpsilon);

            cancelationSource.Dispose();
            TestHelper.Cleanup();
        }

        [Test]
        public void MultipleOnProgressAreInvokedProperly()
        {
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred();
            Assert.AreEqual(Promise.State.Pending, deferred.State);
            var deferred2 = Promise.NewDeferred();
            Assert.AreEqual(Promise.State.Pending, deferred2.State);

            float progress = float.NaN;
            bool firstInvoked = false;
            bool secondInvoked = false;

            deferred.Promise
                .ThenDuplicate()
                .Then(() => { }, cancelationSource.Token)
                .Progress(p => { firstInvoked = true; progress = p; })
                .Finally(cancelationSource.Dispose);

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0f, progress, TestHelper.progressEpsilon);
            Assert.IsTrue(firstInvoked);
            Assert.IsFalse(secondInvoked);

            progress = float.NaN;
            firstInvoked = false;
            secondInvoked = false;
            cancelationSource.Cancel();

            deferred2.Promise
                .ThenDuplicate()
                .ThenDuplicate()
                .Progress(p => { secondInvoked = true; progress = p; });

            deferred.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0f, progress, TestHelper.progressEpsilon);
            Assert.IsTrue(secondInvoked);
            Assert.IsFalse(firstInvoked);

            progress = float.NaN;
            firstInvoked = false;
            secondInvoked = false;

            deferred.Resolve();
            deferred2.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);
            Assert.IsTrue(secondInvoked);
            Assert.IsFalse(firstInvoked);

            TestHelper.Cleanup();
        }

        [Test]
        public void OnProgressWillNotBeInvokedWith1UntilPromiseIsResolved()
        {
            var deferred = Promise.NewDeferred();
            Assert.AreEqual(Promise.State.Pending, deferred.State);

            float progress = float.NaN;

            deferred.Promise.Progress(p => progress = p);

            deferred.ReportProgress(1f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0f, progress, 0f);

            deferred.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, 0f);

            TestHelper.Cleanup();
        }

#if PROMISE_DEBUG
        [Test]
        public void IfOnProgressIsNullThrow()
        {
            var deferred = Promise.NewDeferred();

            Assert.AreEqual(Promise.State.Pending, deferred.State);

            Assert.Throws<ArgumentNullException>(() =>
            {
                deferred.Promise.Progress(default(Action<float>));
            });

            deferred.Resolve();

            var deferredInt = Promise.NewDeferred<int>();
            Assert.AreEqual(Promise.State.Pending, deferredInt.State);

            Assert.Throws<ArgumentNullException>(() =>
            {
                deferredInt.Promise.Progress(default(Action<float>));
            });

            deferredInt.Resolve(0);

            TestHelper.Cleanup();
        }

        [Test]
        public void OnProgressWillOnlyBeInvokedWithAValueBetween0And1()
        {
            var deferred = Promise.NewDeferred();
            Assert.AreEqual(Promise.State.Pending, deferred.State);

            deferred.Promise.Progress(p =>
            {
                Assert.GreaterOrEqual(p, 0f);
                Assert.LessOrEqual(p, 1f);
            });

            Assert.Throws<ArgumentOutOfRangeException>(() => deferred.ReportProgress(float.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => deferred.ReportProgress(float.NegativeInfinity));
            Assert.Throws<ArgumentOutOfRangeException>(() => deferred.ReportProgress(float.PositiveInfinity));
            Assert.Throws<ArgumentOutOfRangeException>(() => deferred.ReportProgress(float.MinValue));
            Assert.Throws<ArgumentOutOfRangeException>(() => deferred.ReportProgress(float.MaxValue));
            Assert.Throws<ArgumentOutOfRangeException>(() => deferred.ReportProgress(-0.1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => deferred.ReportProgress(1.1f));

            deferred.Resolve();
            Promise.Manager.HandleCompletesAndProgress();

            TestHelper.Cleanup();
        }
#endif

        // A wait promise is a promise that waits on another promise.
        [Test]
        public void OnProgressWillBeInvokedWithANormalizedValueFromAllWaitPromisesInTheChain0()
        {
            var deferred = Promise.NewDeferred();
            Assert.AreEqual(Promise.State.Pending, deferred.State);
            var nextDeferred = Promise.NewDeferred();
            Assert.AreEqual(Promise.State.Pending, deferred.State);

            float progress = float.NaN;

            deferred.Promise
                .Then(() => nextDeferred.Promise)
                .Progress(p => progress = p);

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0f, progress, 0f);

            deferred.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.25f, progress, TestHelper.progressEpsilon);

            deferred.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f, progress, TestHelper.progressEpsilon);

            nextDeferred.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.75f, progress, TestHelper.progressEpsilon);

            nextDeferred.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, 0f);

            TestHelper.Cleanup();
        }

        [Test]
        public void OnProgressWillBeInvokedWithANormalizedValueFromAllWaitPromisesInTheChain1()
        {
            var deferred = Promise.NewDeferred();
            Assert.AreEqual(Promise.State.Pending, deferred.State);
            var nextDeferred = Promise.NewDeferred<int>();
            Assert.AreEqual(Promise.State.Pending, deferred.State);

            float progress = float.NaN;

            deferred.Promise
                .Then(() => nextDeferred.Promise)
                .Progress(p => progress = p);

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0f, progress, 0f);

            deferred.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.25f, progress, TestHelper.progressEpsilon);

            deferred.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f, progress, TestHelper.progressEpsilon);

            nextDeferred.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.75f, progress, TestHelper.progressEpsilon);

            nextDeferred.Resolve(100);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, 0f);

            TestHelper.Cleanup();
        }

        [Test]
        public void OnProgressWillBeInvokedWithANormalizedValueFromAllWaitPromisesInTheChain2()
        {
            var deferred = Promise.NewDeferred<int>();
            Assert.AreEqual(Promise.State.Pending, deferred.State);
            var nextDeferred = Promise.NewDeferred();
            Assert.AreEqual(Promise.State.Pending, deferred.State);

            float progress = float.NaN;

            deferred.Promise
                .Then(() => nextDeferred.Promise)
                .Progress(p => progress = p);

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0f, progress, 0f);

            deferred.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.25f, progress, TestHelper.progressEpsilon);

            deferred.Resolve(100);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f, progress, TestHelper.progressEpsilon);

            nextDeferred.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.75f, progress, TestHelper.progressEpsilon);

            nextDeferred.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, 0f);

            TestHelper.Cleanup();
        }

        [Test]
        public void OnProgressWillBeInvokedWithANormalizedValueFromAllWaitPromisesInTheChain3()
        {
            var deferred = Promise.NewDeferred<int>();
            Assert.AreEqual(Promise.State.Pending, deferred.State);
            var nextDeferred = Promise.NewDeferred<int>();
            Assert.AreEqual(Promise.State.Pending, deferred.State);

            float progress = float.NaN;

            deferred.Promise
                .Then(() => nextDeferred.Promise)
                .Progress(p => progress = p);

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0f, progress, 0f);

            deferred.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.25f, progress, TestHelper.progressEpsilon);

            deferred.Resolve(100);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f, progress, TestHelper.progressEpsilon);

            nextDeferred.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.75f, progress, TestHelper.progressEpsilon);

            nextDeferred.Resolve(100);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, 0f);

            TestHelper.Cleanup();
        }

        [Test]
        public void OnProgressWillBeInvokedProperlyFromARecoveredPromise()
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();
            var deferred3 = Promise.NewDeferred();

            float progress = float.NaN;

            CancelationSource cancelationSource = CancelationSource.New();

            deferred1.Promise
                .Then(() => deferred2.Promise, cancelationSource.Token)
                .ContinueWith(_ => deferred3.Promise)
                .Progress(p => progress = p);

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0f, progress, 0f);

            deferred1.ReportProgress(0.25f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.25f / 3f, progress, TestHelper.progressEpsilon);

            cancelationSource.Cancel();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.25f / 3f, progress, TestHelper.progressEpsilon);

            deferred1.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.25f / 3f, progress, TestHelper.progressEpsilon);

            deferred2.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(2f / 3f, progress, TestHelper.progressEpsilon);

            deferred1.ReportProgress(0.7f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(2f / 3f, progress, TestHelper.progressEpsilon);

            deferred3.ReportProgress(0.25f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(2.25f / 3f, progress, TestHelper.progressEpsilon);

            deferred1.ReportProgress(0.8f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(2.25f / 3f, progress, TestHelper.progressEpsilon);

            deferred3.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(2.5f / 3f, progress, TestHelper.progressEpsilon);

            deferred3.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);

            deferred1.ReportProgress(0.8f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);

            deferred1.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);

            cancelationSource.Dispose();
            TestHelper.Cleanup();
        }
    }
}
#endif