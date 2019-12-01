using System;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Proto.Promises.Tests
{
    public class Progress
    {
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

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
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

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void OnProgressWillNoLongerBeInvokedWhenPromiseIsRejected()
        {
            var deferred = Promise.NewDeferred();
            Assert.AreEqual(Promise.State.Pending, deferred.State);

            float progress = float.NaN;

            deferred.Promise.Progress(p => progress = p)
                .Catch(() => { });

            deferred.Reject();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.IsNaN(progress);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void OnProgressWillNoLongerBeInvokedWhenAPromiseIsRejectedAndContinueBeingInvokedWhenAChainedPromisesProgressIsUpdated()
        {
            var deferred = Promise.NewDeferred();
            Assert.AreEqual(Promise.State.Pending, deferred.State);

            float progress = float.NaN;

            deferred.Promise
                .CatchDefer(() => d => deferred = d)
                .Progress(p => progress = p);

            deferred.ReportProgress(0.5f);
            deferred.Reject();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.IsNaN(progress);

            deferred.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.75f, progress, TestHelper.progressEpsilon);

            deferred.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void OnProgressWillNoLongerBeInvokedWhenPromiseIsCanceled0()
        {
            var deferred = Promise.NewDeferred();
            Assert.AreEqual(Promise.State.Pending, deferred.State);

            float progress = float.NaN;

            deferred.Promise.Progress(p => progress = p);

            deferred.Cancel();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.IsNaN(progress);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void OnProgressWillNoLongerBeInvokedWhenPromiseIsCanceled1()
        {
            var deferred = Promise.NewDeferred();
            Assert.AreEqual(Promise.State.Pending, deferred.State);

            float progress = float.NaN;

            var cancelable = deferred.Promise
                .ThenDuplicate()
                .ThenDuplicate()
                .Progress(p => progress = p);

            deferred.ReportProgress(0.25f);
            cancelable.Cancel();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.IsNaN(progress);

            deferred.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.IsNaN(progress);

            deferred.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.IsNaN(progress);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
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

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void OnProgressWillOnlyBeInvokedWithAValueBetween0And1()
        {
            var deferred = Promise.NewDeferred();
            Assert.AreEqual(Promise.State.Pending, deferred.State);

            deferred.Promise.Progress(p => {
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

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        // A wait promise is a promise that waits on a deferred or another promise.
        [Test]
        public void OnProgressWillBeInvokedWithANormalizedValueFromAllWaitPromisesInTheChain0()
        {
            var deferred = Promise.NewDeferred();
            Assert.AreEqual(Promise.State.Pending, deferred.State);
            Promise.Deferred nextDeferred = null;

            float progress = float.NaN;

            deferred.Promise
                .ThenDefer(() => d => nextDeferred = d)
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

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void OnProgressWillBeInvokedWithANormalizedValueFromAllWaitPromisesInTheChain1()
        {
            var deferred = Promise.NewDeferred();
            Assert.AreEqual(Promise.State.Pending, deferred.State);
            Promise<int>.Deferred nextDeferred = null;

            float progress = float.NaN;

            deferred.Promise
                .ThenDefer<int>(() => d => nextDeferred = d)
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

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void OnProgressWillBeInvokedWithANormalizedValueFromAllWaitPromisesInTheChain2()
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

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void OnProgressWillBeInvokedWithANormalizedValueFromAllWaitPromisesInTheChain3()
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

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void OnProgressWillBeInvokedWithANormalizedValueFromAllWaitPromisesInTheChain4()
        {
            var deferred = Promise.NewDeferred<int>();
            Assert.AreEqual(Promise.State.Pending, deferred.State);
            Promise.Deferred nextDeferred = null;

            float progress = float.NaN;

            deferred.Promise
                .ThenDefer(() => d => nextDeferred = d)
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

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void OnProgressWillBeInvokedWithANormalizedValueFromAllWaitPromisesInTheChain5()
        {
            var deferred = Promise.NewDeferred<int>();
            Assert.AreEqual(Promise.State.Pending, deferred.State);
            Promise<int>.Deferred nextDeferred = null;

            float progress = float.NaN;

            deferred.Promise
                .ThenDefer<int>(() => d => nextDeferred = d)
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

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void OnProgressWillBeInvokedWithANormalizedValueFromAllWaitPromisesInTheChain6()
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

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void OnProgressWillBeInvokedWithANormalizedValueFromAllWaitPromisesInTheChain7()
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

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }
    }
}