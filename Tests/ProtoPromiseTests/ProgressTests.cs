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
        [TearDown]
        public void Teardown()
        {
            TestHelper.Cleanup();
        }

        [Test]
        public void OnProgressMayBeInvokedWhenThePromisesProgressHasChanged0()
        {
            var deferred = Promise.NewDeferred();

            float progress = float.NaN;

            deferred.Promise
                .Progress(p => progress = p)
                .Forget();

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0f, progress, TestHelper.progressEpsilon);

            deferred.ReportProgress(0.25f);
            Assert.AreEqual(0f, progress, TestHelper.progressEpsilon);

            deferred.ReportProgress(0.5f);
            Assert.AreEqual(0f, progress, TestHelper.progressEpsilon);

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f, progress, TestHelper.progressEpsilon);

            deferred.Resolve();
        }

        [Test]
        public void OnProgressMayBeInvokedWhenThePromisesProgressHasChanged1()
        {
            var deferred = Promise.NewDeferred<int>();

            float progress = float.NaN;

            deferred.Promise
                .Progress(p => progress = p)
                .Forget();

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0f, progress, TestHelper.progressEpsilon);

            deferred.ReportProgress(0.25f);
            Assert.AreEqual(0f, progress, TestHelper.progressEpsilon);

            deferred.ReportProgress(0.5f);
            Assert.AreEqual(0f, progress, TestHelper.progressEpsilon);

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f, progress, TestHelper.progressEpsilon);

            deferred.Resolve(1);
        }

        [Test]
        public void OnProgressMayBeInvokedWhenThePromisesProgressHasChanged2()
        {
            var cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred(cancelationSource.Token);

            float progress = float.NaN;

            deferred.Promise
                .Progress(p => progress = p)
                .Forget();

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0f, progress, TestHelper.progressEpsilon);

            deferred.ReportProgress(0.25f);
            Assert.AreEqual(0f, progress, TestHelper.progressEpsilon);

            deferred.ReportProgress(0.5f);
            Assert.AreEqual(0f, progress, TestHelper.progressEpsilon);

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f, progress, TestHelper.progressEpsilon);

            deferred.Resolve();

            cancelationSource.Dispose();
        }

        [Test]
        public void OnProgressMayBeInvokedWhenThePromisesProgressHasChanged3()
        {
            var cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred<int>(cancelationSource.Token);

            float progress = float.NaN;

            deferred.Promise
                .Progress(p => progress = p)
                .Forget();

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0f, progress, TestHelper.progressEpsilon);

            deferred.ReportProgress(0.25f);
            Assert.AreEqual(0f, progress, TestHelper.progressEpsilon);

            deferred.ReportProgress(0.5f);
            Assert.AreEqual(0f, progress, TestHelper.progressEpsilon);

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f, progress, TestHelper.progressEpsilon);

            deferred.Resolve(1);

            cancelationSource.Dispose();
        }

        [Test]
        public void OnProgressMayBeInvokedWithTheCapturedValueWhenThePromisesProgressHasChanged()
        {
            var deferred = Promise.NewDeferred();

            float progress = float.NaN;
            string capturedValue = "Capture Value";

            deferred.Promise
                .Progress(capturedValue, (cv, p) =>
                {
                    Assert.AreEqual(capturedValue, cv);
                    progress = p;
                })
                .Forget();

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0f, progress, TestHelper.progressEpsilon);

            deferred.ReportProgress(0.25f);
            Promise.Manager.HandleCompletesAndProgress();

            deferred.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();

            deferred.ReportProgress(0.75f);
            Promise.Manager.HandleCompletesAndProgress();

            deferred.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
        }

        [Test]
        public void OnProgressWillBeInvokedWith1WhenPromiseIsResolved0()
        {
            var deferred = Promise.NewDeferred();

            float progress = float.NaN;

            deferred.Promise
                .Progress(p => progress = p)
                .Forget();

            deferred.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);
        }

        [Test]
        public void OnProgressWillBeInvokedWith1WhenPromiseIsResolved1()
        {
            float progress = float.NaN;

            Promise.Resolved()
                .Progress(p => progress = p)
                .Forget();

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);
        }

        [Test]
        public void OnProgressWillNoLongerBeInvokedWhenPromiseIsRejected0()
        {
            var deferred = Promise.NewDeferred();

            float progress = float.NaN;

            deferred.Promise
                .Progress(p => progress = p)
                .Catch(() => { })
                .Forget();

            deferred.ReportProgress(0.5f);
            deferred.Reject("Fail Value");
            Promise.Manager.HandleCompletesAndProgress();
            Assert.IsNaN(progress);
        }

        [Test]
        public void OnProgressWillNoLongerBeInvokedWhenPromiseIsRejected1()
        {
            var deferred = Promise.NewDeferred();

            float progress = float.NaN;

            deferred.Promise
                .Progress(p => progress = p)
                .Catch(() => { })
                .Forget();

            deferred.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f, progress, TestHelper.progressEpsilon);

            deferred.Reject("Fail Value");
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f, progress, TestHelper.progressEpsilon);
        }

        [Test]
        public void WhenAPromiseIsRejectedAndCaught_OnProgressWillBeInvokedWithExpectedValue0()
        {
            var deferred = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();

            float progress = float.NaN;

            deferred.Promise
                .Catch(() => deferred2.Promise)
                .Progress(p => progress = p)
                .Forget();
            Assert.IsNaN(progress);

            deferred.ReportProgress(0.5f);
            deferred.Reject("Fail Value");
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f / 2f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1.5f / 2f, progress, TestHelper.progressEpsilon);

            deferred2.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(2f / 2f, progress, TestHelper.progressEpsilon);
        }

        [Test]
        public void WhenAPromiseIsRejectedAndCaught_OnProgressWillBeInvokedWithExpectedValue1()
        {
            var deferred = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();

            float progress = float.NaN;

            deferred.Promise
                .Catch(() => deferred2.Promise)
                .Progress(p => progress = p)
                .Forget();
            Assert.IsNaN(progress);

            deferred.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f / 2f, progress, TestHelper.progressEpsilon);

            deferred.Reject("Fail Value");
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f / 2f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1.5f / 2f, progress, TestHelper.progressEpsilon);

            deferred2.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(2f / 2f, progress, TestHelper.progressEpsilon);

        }

        [Test]
        public void OnProgressWillNoLongerBeInvokedWhenPromiseIsCanceled0()
        {
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred(cancelationSource.Token);

            float progress = float.NaN;

            deferred.Promise
                .Progress(p => progress = p)
                .Forget();

            cancelationSource.Cancel();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.IsNaN(progress);

            cancelationSource.Dispose();
        }

        [Test]
        public void OnProgressWillNoLongerBeInvokedWhenPromiseIsCanceled1()
        {
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred(cancelationSource.Token);

            float progress = float.NaN;

            deferred.Promise
                .Progress(p => progress = p)
                .Forget();

            deferred.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f, progress, TestHelper.progressEpsilon);

            cancelationSource.Cancel();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f, progress, TestHelper.progressEpsilon);

            cancelationSource.Dispose();
        }

        [Test]
        public void OnProgressWillNoLongerBeInvokedWhenPromiseIsCanceled2()
        {
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred();

            float progress = float.NaN;

            deferred.Promise
                .ThenDuplicate()
                .Then(() => { }, cancelationSource.Token)
                .Progress(p => progress = p)
                .Finally(cancelationSource.Dispose)
                .Forget();

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
        }

        [Test]
        public void OnProgressWillNoLongerBeInvokedWhenPromiseIsCanceled3()
        {
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred();

            float progress = float.NaN;

            deferred.Promise
                .Then(() => { }, cancelationSource.Token)
                .ThenDuplicate()
                .Progress(p => progress = p)
                .Finally(cancelationSource.Dispose)
                .Forget();

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
        }

        [Test]
        public void OnProgressWillNoLongerBeInvokedWhenPromiseIsCanceled4()
        {
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred();

            float progress = float.NaN;

            deferred.Promise
                .ThenDuplicate()
                .Then(() => { }, cancelationSource.Token)
                .Progress(p => progress = p)
                .Finally(cancelationSource.Dispose)
                .Forget();

            deferred.ReportProgress(0.25f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.25f, progress, TestHelper.progressEpsilon);

            cancelationSource.Cancel();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.25f, progress, TestHelper.progressEpsilon);

            deferred.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.25f, progress, TestHelper.progressEpsilon);

            deferred.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.25f, progress, TestHelper.progressEpsilon);
        }

        [Test]
        public void OnProgressWillNoLongerBeInvokedWhenPromiseIsCanceled5()
        {
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred();

            float progress = float.NaN;

            deferred.Promise
                .Then(() => { }, cancelationSource.Token)
                .ThenDuplicate()
                .Progress(p => progress = p)
                .Finally(cancelationSource.Dispose)
                .Forget();

            deferred.ReportProgress(0.25f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.25f, progress, TestHelper.progressEpsilon);

            cancelationSource.Cancel();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.25f, progress, TestHelper.progressEpsilon);

            deferred.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.25f, progress, TestHelper.progressEpsilon);

            deferred.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.25f, progress, TestHelper.progressEpsilon);
        }

        [Test]
        public void OnProgressWillNoLongerBeInvokedWhenPromiseIsCanceled6()
        {
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();

            float progress = float.NaN;

            deferred.Promise
                .ThenDuplicate()
                .Then(() =>
                    deferred2.Promise
                        .Then(() => { }, cancelationSource.Token)
                )
                .Progress(p => progress = p)
                .Finally(cancelationSource.Dispose)
                .Forget();

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
        }

        [Test]
        public void OnProgressWillNoLongerBeInvokedWhenPromiseIsCanceled7()
        {
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();

            float progress = float.NaN;

            deferred.Promise
                .Then(() =>
                    deferred2.Promise
                        .Then(() => { }, cancelationSource.Token)
                )
                .ThenDuplicate()
                .Progress(p => progress = p)
                .Finally(cancelationSource.Dispose)
                .Forget();

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
        }

        [Test]
        public void OnProgressWillNoLongerBeInvokedWhenPromiseIsCanceled8()
        {
            var deferred = Promise.NewDeferred();
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred2 = Promise.NewDeferred(cancelationSource.Token);

            float progress = float.NaN;

            deferred.Promise
                .ThenDuplicate()
                .Then(() => deferred2.Promise)
                .Progress(p => progress = p)
                .Forget();

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
        }

        [Test]
        public void OnProgressWillNoLongerBeInvokedWhenPromiseIsCanceled9()
        {
            var deferred = Promise.NewDeferred();
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred2 = Promise.NewDeferred(cancelationSource.Token);

            float progress = float.NaN;

            deferred.Promise
                .Then(() => deferred2.Promise)
                .ThenDuplicate()
                .Progress(p => progress = p)
                .Forget();

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
        }

        [Test]
        public void OnProgressWillNoLongerBeInvokedWhenPromiseIsCanceled10()
        {
            var cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred(cancelationSource.Token);

            float progress = float.NaN;

            deferred.Promise
                .Progress(p => progress = p)
                .Forget();

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0f, progress, TestHelper.progressEpsilon);

            deferred.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f, progress, TestHelper.progressEpsilon);

            deferred.ReportProgress(0.75f);
            cancelationSource.Cancel();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f, progress, TestHelper.progressEpsilon);

            cancelationSource.Dispose();
        }

        [Test]
        public void OnProgressWillNoLongerBeInvokedWhenPromiseIsCanceled11()
        {
            var cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred<int>(cancelationSource.Token);

            float progress = float.NaN;

            deferred.Promise
                .Progress(p => progress = p)
                .Forget();

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0f, progress, TestHelper.progressEpsilon);

            deferred.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f, progress, TestHelper.progressEpsilon);

            deferred.ReportProgress(0.75f);
            cancelationSource.Cancel();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f, progress, TestHelper.progressEpsilon);

            cancelationSource.Dispose();
        }

        [Test]
        public void OnProgressWillNoLongerBeInvokedWhenTokenIsCanceled0()
        {
            var deferred = Promise.NewDeferred();
            CancelationSource cancelationSource = CancelationSource.New();

            float progress = float.NaN;

            deferred.Promise
                .Progress(p => progress = p, cancelationSource.Token)
                .Forget();

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
        }

        [Test]
        public void OnProgressWillNoLongerBeInvokedWhenTokenIsCanceled1()
        {
            var deferred = Promise.NewDeferred();
            CancelationSource cancelationSource = CancelationSource.New();

            float progress = float.NaN;

            deferred.Promise
                .Progress(1, (cv, p) => progress = p, cancelationSource.Token)
                .Forget();

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
        }

        [Test]
        public void MultipleOnProgressAreInvokedProperly()
        {
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();

            float progress = float.NaN;
            bool firstInvoked = false;
            bool secondInvoked = false;

            deferred.Promise
                .ThenDuplicate()
                .Then(() => { }, cancelationSource.Token)
                .Progress(p => { firstInvoked = true; progress = p; })
                .Finally(cancelationSource.Dispose)
                .Forget();

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
                .Progress(p => { secondInvoked = true; progress = p; })
                .Forget();

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
        }

        [Test]
        public void OnProgressWillNotBeInvokedWith1UntilPromiseIsResolved()
        {
            var deferred = Promise.NewDeferred();

            float progress = float.NaN;

            deferred.Promise
                .Progress(p => progress = p)
                .Forget();

            deferred.ReportProgress(1f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0f, progress, TestHelper.progressEpsilon);

            deferred.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);
        }

#if PROMISE_DEBUG
        [Test]
        public void IfOnProgressIsNullThrow()
        {
            var deferred = Promise.NewDeferred();

            Assert.Throws<ArgumentNullException>(() =>
            {
                deferred.Promise.Progress(default(Action<float>));
            });
            Assert.Throws<ArgumentNullException>(() =>
            {
                deferred.Promise.Progress(1, default(Action<int, float>));
            });

            deferred.Resolve();

            var deferredInt = Promise.NewDeferred<int>();

            Assert.Throws<ArgumentNullException>(() =>
            {
                deferredInt.Promise.Progress(default(Action<float>));
            });
            Assert.Throws<ArgumentNullException>(() =>
            {
                deferredInt.Promise.Progress(1, default(Action<int, float>));
            });

            deferredInt.Resolve(0);

            deferred.Promise.Forget();
            deferredInt.Promise.Forget();
        }

        [Test]
        public void OnProgressWillOnlyBeInvokedWithAValueBetween0And1()
        {
            var deferred = Promise.NewDeferred();

            deferred.Promise
                .Progress(p =>
                {
                    Assert.GreaterOrEqual(p, 0f);
                    Assert.LessOrEqual(p, 1f);
                })
                .Forget();

            Assert.Throws<ArgumentOutOfRangeException>(() => deferred.ReportProgress(float.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => deferred.ReportProgress(float.NegativeInfinity));
            Assert.Throws<ArgumentOutOfRangeException>(() => deferred.ReportProgress(float.PositiveInfinity));
            Assert.Throws<ArgumentOutOfRangeException>(() => deferred.ReportProgress(float.MinValue));
            Assert.Throws<ArgumentOutOfRangeException>(() => deferred.ReportProgress(float.MaxValue));
            Assert.Throws<ArgumentOutOfRangeException>(() => deferred.ReportProgress(-0.1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => deferred.ReportProgress(1.1f));

            deferred.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
        }
#endif

        // A wait promise is a promise that waits on another promise.
        [Test]
        public void OnProgressWillBeInvokedWithANormalizedValueFromAllWaitPromisesInTheChain0()
        {
            var deferred = Promise.NewDeferred();
            var nextDeferred = Promise.NewDeferred();

            float progress = float.NaN;

            deferred.Promise
                .Then(() => nextDeferred.Promise)
                .Progress(p => progress = p)
                .Forget();

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0f / 2f, progress, TestHelper.progressEpsilon);

            deferred.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f / 2f, progress, TestHelper.progressEpsilon);

            deferred.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f / 2f, progress, TestHelper.progressEpsilon);

            nextDeferred.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1.5f / 2f, progress, TestHelper.progressEpsilon);

            nextDeferred.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(2f / 2f, progress, TestHelper.progressEpsilon);
        }

        [Test]
        public void OnProgressWillBeInvokedWithANormalizedValueFromAllWaitPromisesInTheChain1()
        {
            var deferred = Promise.NewDeferred();
            var nextDeferred = Promise.NewDeferred<int>();

            float progress = float.NaN;

            deferred.Promise
                .Then(() => nextDeferred.Promise)
                .Progress(p => progress = p)
                .Forget();

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0f / 2f, progress, TestHelper.progressEpsilon);

            deferred.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f / 2f, progress, TestHelper.progressEpsilon);

            deferred.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f / 2f, progress, TestHelper.progressEpsilon);

            nextDeferred.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1.5f / 2f, progress, TestHelper.progressEpsilon);

            nextDeferred.Resolve(100);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(2f / 2f, progress, TestHelper.progressEpsilon);
        }

        [Test]
        public void OnProgressWillBeInvokedWithANormalizedValueFromAllWaitPromisesInTheChain2()
        {
            var deferred = Promise.NewDeferred<int>();
            var nextDeferred = Promise.NewDeferred();

            float progress = float.NaN;

            deferred.Promise
                .Then(() => nextDeferred.Promise)
                .Progress(p => progress = p)
                .Forget();

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0f / 2f, progress, TestHelper.progressEpsilon);

            deferred.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f / 2f, progress, TestHelper.progressEpsilon);

            deferred.Resolve(100);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f / 2f, progress, TestHelper.progressEpsilon);

            nextDeferred.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1.5f / 2f, progress, TestHelper.progressEpsilon);

            nextDeferred.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(2f / 2f, progress, TestHelper.progressEpsilon);
        }

        [Test]
        public void OnProgressWillBeInvokedWithANormalizedValueFromAllWaitPromisesInTheChain3()
        {
            var deferred = Promise.NewDeferred<int>();
            var nextDeferred = Promise.NewDeferred<int>();

            float progress = float.NaN;

            deferred.Promise
                .Then(() => nextDeferred.Promise)
                .Progress(p => progress = p)
                .Forget();

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0f / 2f, progress, TestHelper.progressEpsilon);

            deferred.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f / 2f, progress, TestHelper.progressEpsilon);

            deferred.Resolve(100);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f / 2f, progress, TestHelper.progressEpsilon);

            nextDeferred.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1.5f / 2f, progress, TestHelper.progressEpsilon);

            nextDeferred.Resolve(100);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(2f / 2f, progress, TestHelper.progressEpsilon);
        }

        [Test]
        public void OnProgressWillBeInvokedWithANormalizedValueFromAllWaitPromisesInTheChain4()
        {
            var deferred = Promise.NewDeferred();

            float progress = float.NaN;

            deferred.Promise
                .Then(() => Promise.Resolved())
                .Progress(p => progress = p)
                .Forget();

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0f / 2f, progress, TestHelper.progressEpsilon);

            deferred.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f / 2f, progress, TestHelper.progressEpsilon);

            deferred.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(2f / 2f, progress, TestHelper.progressEpsilon);
        }

        [Test]
        public void OnProgressWillBeInvokedWithANormalizedValueFromAllWaitPromisesInTheChain5()
        {
            var deferred = Promise.NewDeferred();

            float progress = float.NaN;

            Promise.Resolved()
                .Then(() => deferred.Promise)
                .Progress(p => progress = p)
                .Forget();

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f / 2f, progress, TestHelper.progressEpsilon);

            deferred.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1.5f / 2f, progress, TestHelper.progressEpsilon);

            deferred.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(2f / 2f, progress, TestHelper.progressEpsilon);
        }

        [Test]
        public void OnProgressWillBeInvokedWithANormalizedValueFromAllWaitPromisesInTheChain6()
        {
            var deferred = Promise.NewDeferred();

            float progress = float.NaN;

            deferred.Promise
                .Then(() => Promise.Resolved(1))
                .Progress(p => progress = p)
                .Forget();

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0f / 2f, progress, TestHelper.progressEpsilon);

            deferred.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f / 2f, progress, TestHelper.progressEpsilon);

            deferred.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(2f / 2f, progress, TestHelper.progressEpsilon);
        }

        [Test]
        public void OnProgressWillBeInvokedWithANormalizedValueFromAllWaitPromisesInTheChain7()
        {
            var deferred = Promise.NewDeferred();

            float progress = float.NaN;

            Promise.Resolved(1)
                .Then(() => deferred.Promise)
                .Progress(p => progress = p)
                .Forget();

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f / 2f, progress, TestHelper.progressEpsilon);

            deferred.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1.5f / 2f, progress, TestHelper.progressEpsilon);

            deferred.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(2f / 2f, progress, TestHelper.progressEpsilon);
        }

        [Test]
        public void OnProgressWillBeInvokedWithANormalizedValueFromAllWaitPromisesInTheChain8()
        {
            var deferred = Promise.NewDeferred();

            float progress = float.NaN;

            Promise.Resolved()
                .Then(() => Promise.Resolved())
                .Then(() => deferred.Promise)
                .Progress(p => progress = p)
                .Forget();

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(2f / 3f, progress, TestHelper.progressEpsilon);

            deferred.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(2.5f / 3f, progress, TestHelper.progressEpsilon);

            deferred.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(3f / 3f, progress, TestHelper.progressEpsilon);
        }

        [Test]
        public void OnProgressWillBeInvokedWithANormalizedValueFromAllWaitPromisesInTheChain9()
        {
            var deferred = Promise.NewDeferred();

            float progress = float.NaN;

            Promise.Resolved(1)
                .Then(() => Promise.Resolved(2))
                .Then(() => deferred.Promise)
                .Progress(p => progress = p)
                .Forget();

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(2f / 3f, progress, TestHelper.progressEpsilon);

            deferred.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(2.5f / 3f, progress, TestHelper.progressEpsilon);

            deferred.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(3f / 3f, progress, TestHelper.progressEpsilon);
        }

        [Test]
        public void OnProgressWillBeInvokedProperlyFromARecoveredPromise0()
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();
            var deferred3 = Promise.NewDeferred();

            float progress = float.NaN;

            CancelationSource cancelationSource = CancelationSource.New();

            deferred1.Promise
                .Then(() => deferred2.Promise, cancelationSource.Token)
                .ContinueWith(_ => deferred3.Promise)
                .Progress(p => progress = p)
                .Forget();

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0f, progress, TestHelper.progressEpsilon);

            deferred1.ReportProgress(0.25f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.25f / 3f, progress, TestHelper.progressEpsilon);

            cancelationSource.Cancel();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(2f / 3f, progress, TestHelper.progressEpsilon);

            deferred1.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(2f / 3f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(2f / 3f, progress, TestHelper.progressEpsilon);

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

            deferred1.ReportProgress(0.9f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);

            deferred1.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);

            cancelationSource.Dispose();
            // Promise must be forgotten since it was never returned in onResolved because it was canceled.
            deferred2.Promise.Forget();
        }

        [Test]
        public void OnProgressWillBeInvokedProperlyFromARecoveredPromise1()
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();
            var deferred3 = Promise.NewDeferred<int>();

            float progress = float.NaN;

            CancelationSource cancelationSource = CancelationSource.New();

            deferred1.Promise
                .Then(() => deferred2.Promise, cancelationSource.Token)
                .ContinueWith(_ => deferred3.Promise)
                .Progress(p => progress = p)
                .Forget();

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0f, progress, TestHelper.progressEpsilon);

            deferred1.ReportProgress(0.25f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.25f / 3f, progress, TestHelper.progressEpsilon);

            cancelationSource.Cancel();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(2f / 3f, progress, TestHelper.progressEpsilon);

            deferred1.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(2f / 3f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(2f / 3f, progress, TestHelper.progressEpsilon);

            deferred2.Resolve(1);
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

            deferred3.Resolve(1);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);

            deferred1.ReportProgress(0.9f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);

            deferred1.Resolve(1);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);

            cancelationSource.Dispose();
            // Promise must be forgotten since it was never returned in onResolved because it was canceled.
            deferred2.Promise.Forget();
        }
    }
}
#endif