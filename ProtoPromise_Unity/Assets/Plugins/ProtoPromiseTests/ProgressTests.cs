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

using System;
using NUnit.Framework;

namespace Proto.Promises.Tests
{
    public class ProgressTests
    {
        [SetUp]
        public void Setup()
        {
            TestHelper.Setup();
        }

        [TearDown]
        public void Teardown()
        {
            TestHelper.Cleanup();
        }

        private class ProgressListener : IProgress<float>
        {
            public float CurrentProgress { get; private set; }

            void IProgress<float>.Report(float value)
            {
                CurrentProgress = value;
            }

            public ProgressListener(float initialProgress)
            {
                CurrentProgress = initialProgress;
            }
        }

#if PROMISE_PROGRESS

        [Test]
        public void OnProgressMayBeInvokedWhenThePromisesProgressHasChanged_void0()
        {
            var deferred = Promise.NewDeferred();

            float progress = float.NaN;

            deferred.Promise
                .Progress(p => progress = p)
                .Forget();

            Assert.AreEqual(0f, progress, TestHelper.progressEpsilon);

            deferred.ReportProgress(0.25f);
            Assert.AreEqual(0f, progress, TestHelper.progressEpsilon);

            deferred.ReportProgress(0.5f);
            Assert.AreEqual(0f, progress, TestHelper.progressEpsilon);

            Assert.AreEqual(0.5f, progress, TestHelper.progressEpsilon);

            deferred.Resolve();
        }

        [Test]
        public void OnProgressMayBeInvokedWhenThePromisesProgressHasChanged_void1()
        {
            var cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred(cancelationSource.Token);

            float progress = float.NaN;

            deferred.Promise
                .Progress(p => progress = p)
                .Forget();

            Assert.AreEqual(0f, progress, TestHelper.progressEpsilon);

            deferred.ReportProgress(0.25f);
            Assert.AreEqual(0f, progress, TestHelper.progressEpsilon);

            deferred.ReportProgress(0.5f);
            Assert.AreEqual(0f, progress, TestHelper.progressEpsilon);

            Assert.AreEqual(0.5f, progress, TestHelper.progressEpsilon);

            deferred.Resolve();

            cancelationSource.Dispose();
        }

        [Test]
        public void OnProgressMayBeInvokedWhenThePromisesProgressHasChanged_T0()
        {
            var deferred = Promise.NewDeferred<int>();

            float progress = float.NaN;

            deferred.Promise
                .Progress(p => progress = p) // TODO: execution/synchronization context
                .Forget();

            Assert.AreEqual(0f, progress, TestHelper.progressEpsilon);

            deferred.ReportProgress(0.25f);
            Assert.AreEqual(0f, progress, TestHelper.progressEpsilon);

            deferred.ReportProgress(0.5f);
            Assert.AreEqual(0f, progress, TestHelper.progressEpsilon);

            Assert.AreEqual(0.5f, progress, TestHelper.progressEpsilon);

            deferred.Resolve(1);
        }

        [Test]
        public void OnProgressMayBeInvokedWhenThePromisesProgressHasChanged_T1()
        {
            var cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred<int>(cancelationSource.Token);

            float progress = float.NaN;

            deferred.Promise
                .Progress(p => progress = p)
                .Forget();

            Assert.AreEqual(0f, progress, TestHelper.progressEpsilon);

            deferred.ReportProgress(0.25f);
            Assert.AreEqual(0f, progress, TestHelper.progressEpsilon);

            deferred.ReportProgress(0.5f);
            Assert.AreEqual(0f, progress, TestHelper.progressEpsilon);

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

            Assert.AreEqual(0f, progress, TestHelper.progressEpsilon);

            deferred.ReportProgress(0.25f);

            deferred.ReportProgress(0.5f);

            deferred.ReportProgress(0.75f);

            deferred.Resolve();
        }

        [Test]
        public void ProgressListenerMayBeReportedWhenThePromisesProgressHasChanged()
        {
            var deferred = Promise.NewDeferred();

            var progressListener = new ProgressListener(float.NaN);

            deferred.Promise
                .Progress(progressListener)
                .Forget();

            Assert.AreEqual(0f, progressListener.CurrentProgress, TestHelper.progressEpsilon);

            deferred.ReportProgress(0.25f);
            Assert.AreEqual(0.25f, progressListener.CurrentProgress, TestHelper.progressEpsilon);

            deferred.ReportProgress(0.5f);
            Assert.AreEqual(0.5f, progressListener.CurrentProgress, TestHelper.progressEpsilon);

            deferred.ReportProgress(0.75f);
            Assert.AreEqual(0.75f, progressListener.CurrentProgress, TestHelper.progressEpsilon);

            deferred.Resolve();
            Assert.AreEqual(1f, progressListener.CurrentProgress, TestHelper.progressEpsilon);
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
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);
        }

        [Test]
        public void OnProgressWillBeInvokedWith1WhenPromiseIsResolved1()
        {
            float progress = float.NaN;

            Promise.Resolved()
                .Progress(p => progress = p)
                .Forget();

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

            Assert.AreEqual(0.5f, progress, TestHelper.progressEpsilon);
            progress = float.NaN;

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
            Assert.AreEqual(0.5f, progress, TestHelper.progressEpsilon);

            deferred.Reject("Fail Value");
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

            Assert.AreEqual(0f, progress, TestHelper.progressEpsilon);

            deferred.ReportProgress(0.5f);
            deferred.Reject("Fail Value");
            Assert.AreEqual(1f / 2f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.5f);
            Assert.AreEqual(1.5f / 2f, progress, TestHelper.progressEpsilon);

            deferred2.Resolve();
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
            
            Assert.AreEqual(0f, progress, TestHelper.progressEpsilon);

            deferred.ReportProgress(0.5f);
            Assert.AreEqual(0.5f / 2f, progress, TestHelper.progressEpsilon);

            deferred.Reject("Fail Value");
            Assert.AreEqual(1f / 2f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.5f);
            Assert.AreEqual(1.5f / 2f, progress, TestHelper.progressEpsilon);

            deferred2.Resolve();
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

            Assert.AreEqual(0f, progress, TestHelper.progressEpsilon);
            progress = float.NaN;

            cancelationSource.Cancel();
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
            Assert.AreEqual(0.5f, progress, TestHelper.progressEpsilon);

            cancelationSource.Cancel();
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

            Assert.AreEqual(0.25f, progress, TestHelper.progressEpsilon);
            progress = float.NaN;

            cancelationSource.Cancel();
            Assert.IsNaN(progress);

            deferred.ReportProgress(0.5f);
            Assert.IsNaN(progress);

            deferred.Resolve();
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

            Assert.AreEqual(0.25f, progress, TestHelper.progressEpsilon);
            progress = float.NaN;

            cancelationSource.Cancel();
            Assert.IsNaN(progress);

            deferred.ReportProgress(0.5f);
            Assert.IsNaN(progress);

            deferred.Resolve();
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
            Assert.AreEqual(0.25f, progress, TestHelper.progressEpsilon);

            cancelationSource.Cancel();
            Assert.AreEqual(0.25f, progress, TestHelper.progressEpsilon);

            deferred.ReportProgress(0.5f);
            Assert.AreEqual(0.25f, progress, TestHelper.progressEpsilon);

            deferred.Resolve();
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
            Assert.AreEqual(0.25f, progress, TestHelper.progressEpsilon);

            cancelationSource.Cancel();
            Assert.AreEqual(0.25f, progress, TestHelper.progressEpsilon);

            deferred.ReportProgress(0.5f);
            Assert.AreEqual(0.25f, progress, TestHelper.progressEpsilon);

            deferred.Resolve();
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
            Assert.AreEqual(0.25f, progress, TestHelper.progressEpsilon);

            deferred.Resolve();
            Assert.AreEqual(0.5f, progress, TestHelper.progressEpsilon);

            cancelationSource.Cancel();
            deferred2.ReportProgress(0.5f);
            Assert.AreEqual(0.5f, progress, TestHelper.progressEpsilon);

            deferred2.Resolve();
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
            Assert.AreEqual(0.25f, progress, TestHelper.progressEpsilon);

            deferred.Resolve();
            Assert.AreEqual(0.5f, progress, TestHelper.progressEpsilon);

            cancelationSource.Cancel();
            deferred2.ReportProgress(0.5f);
            Assert.AreEqual(0.5f, progress, TestHelper.progressEpsilon);

            deferred2.Resolve();
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
            Assert.AreEqual(0.25f, progress, TestHelper.progressEpsilon);

            deferred.Resolve();
            Assert.AreEqual(0.5f, progress, TestHelper.progressEpsilon);

            cancelationSource.Cancel();
            deferred2.TryReportProgress(0.5f);
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
            Assert.AreEqual(0.25f, progress, TestHelper.progressEpsilon);

            deferred.Resolve();
            Assert.AreEqual(0.5f, progress, TestHelper.progressEpsilon);

            cancelationSource.Cancel();
            deferred2.TryReportProgress(0.5f);
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

            Assert.AreEqual(0f, progress, TestHelper.progressEpsilon);

            deferred.ReportProgress(0.5f);
            Assert.AreEqual(0.5f, progress, TestHelper.progressEpsilon);

            cancelationSource.Cancel();
            deferred.TryReportProgress(0.75f);
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

            Assert.AreEqual(0f, progress, TestHelper.progressEpsilon);

            deferred.ReportProgress(0.5f);
            Assert.AreEqual(0.5f, progress, TestHelper.progressEpsilon);

            cancelationSource.Cancel();
            deferred.TryReportProgress(0.75f);
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
            Assert.AreEqual(0.25f, progress, TestHelper.progressEpsilon);

            cancelationSource.Cancel();
            deferred.ReportProgress(0.5f);
            Assert.AreEqual(0.25f, progress, TestHelper.progressEpsilon);

            deferred.Resolve();
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
            Assert.AreEqual(0.25f, progress, TestHelper.progressEpsilon);

            cancelationSource.Cancel();
            deferred.ReportProgress(0.5f);
            Assert.AreEqual(0.25f, progress, TestHelper.progressEpsilon);

            deferred.Resolve();
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
            Assert.AreEqual(0f, progress, TestHelper.progressEpsilon);
            Assert.IsTrue(secondInvoked);
            Assert.IsFalse(firstInvoked);

            progress = float.NaN;
            firstInvoked = false;
            secondInvoked = false;

            deferred.Resolve();
            deferred2.Resolve();
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
            Assert.AreEqual(0f, progress, TestHelper.progressEpsilon);

            deferred.Resolve();
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

            Assert.AreEqual(0f / 2f, progress, TestHelper.progressEpsilon);

            deferred.ReportProgress(0.5f);
            Assert.AreEqual(0.5f / 2f, progress, TestHelper.progressEpsilon);

            deferred.Resolve();
            Assert.AreEqual(1f / 2f, progress, TestHelper.progressEpsilon);

            nextDeferred.ReportProgress(0.5f);
            Assert.AreEqual(1.5f / 2f, progress, TestHelper.progressEpsilon);

            nextDeferred.Resolve();
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

            Assert.AreEqual(0f / 2f, progress, TestHelper.progressEpsilon);

            deferred.ReportProgress(0.5f);
            Assert.AreEqual(0.5f / 2f, progress, TestHelper.progressEpsilon);

            deferred.Resolve();
            Assert.AreEqual(1f / 2f, progress, TestHelper.progressEpsilon);

            nextDeferred.ReportProgress(0.5f);
            Assert.AreEqual(1.5f / 2f, progress, TestHelper.progressEpsilon);

            nextDeferred.Resolve(100);
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

            Assert.AreEqual(0f / 2f, progress, TestHelper.progressEpsilon);

            deferred.ReportProgress(0.5f);
            Assert.AreEqual(0.5f / 2f, progress, TestHelper.progressEpsilon);

            deferred.Resolve(100);
            Assert.AreEqual(1f / 2f, progress, TestHelper.progressEpsilon);

            nextDeferred.ReportProgress(0.5f);
            Assert.AreEqual(1.5f / 2f, progress, TestHelper.progressEpsilon);

            nextDeferred.Resolve();
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

            Assert.AreEqual(0f / 2f, progress, TestHelper.progressEpsilon);

            deferred.ReportProgress(0.5f);
            Assert.AreEqual(0.5f / 2f, progress, TestHelper.progressEpsilon);

            deferred.Resolve(100);
            Assert.AreEqual(1f / 2f, progress, TestHelper.progressEpsilon);

            nextDeferred.ReportProgress(0.5f);
            Assert.AreEqual(1.5f / 2f, progress, TestHelper.progressEpsilon);

            nextDeferred.Resolve(100);
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

            Assert.AreEqual(0f / 2f, progress, TestHelper.progressEpsilon);

            deferred.ReportProgress(0.5f);
            Assert.AreEqual(0.5f / 2f, progress, TestHelper.progressEpsilon);

            deferred.Resolve();
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

            Assert.AreEqual(1f / 2f, progress, TestHelper.progressEpsilon);

            deferred.ReportProgress(0.5f);
            Assert.AreEqual(1.5f / 2f, progress, TestHelper.progressEpsilon);

            deferred.Resolve();
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

            Assert.AreEqual(0f / 2f, progress, TestHelper.progressEpsilon);

            deferred.ReportProgress(0.5f);
            Assert.AreEqual(0.5f / 2f, progress, TestHelper.progressEpsilon);

            deferred.Resolve();
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

            Assert.AreEqual(1f / 2f, progress, TestHelper.progressEpsilon);

            deferred.ReportProgress(0.5f);
            Assert.AreEqual(1.5f / 2f, progress, TestHelper.progressEpsilon);

            deferred.Resolve();
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

            Assert.AreEqual(2f / 3f, progress, TestHelper.progressEpsilon);

            deferred.ReportProgress(0.5f);
            Assert.AreEqual(2.5f / 3f, progress, TestHelper.progressEpsilon);

            deferred.Resolve();
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

            Assert.AreEqual(2f / 3f, progress, TestHelper.progressEpsilon);

            deferred.ReportProgress(0.5f);
            Assert.AreEqual(2.5f / 3f, progress, TestHelper.progressEpsilon);

            deferred.Resolve();
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

            Assert.AreEqual(0f, progress, TestHelper.progressEpsilon);

            deferred1.ReportProgress(0.25f);
            Assert.AreEqual(0.25f / 3f, progress, TestHelper.progressEpsilon);

            cancelationSource.Cancel();
            Assert.AreEqual(2f / 3f, progress, TestHelper.progressEpsilon);

            deferred1.ReportProgress(0.5f);
            Assert.AreEqual(2f / 3f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.5f);
            Assert.AreEqual(2f / 3f, progress, TestHelper.progressEpsilon);

            deferred2.Resolve();
            Assert.AreEqual(2f / 3f, progress, TestHelper.progressEpsilon);

            deferred1.ReportProgress(0.7f);
            Assert.AreEqual(2f / 3f, progress, TestHelper.progressEpsilon);

            deferred3.ReportProgress(0.25f);
            Assert.AreEqual(2.25f / 3f, progress, TestHelper.progressEpsilon);

            deferred1.ReportProgress(0.8f);
            Assert.AreEqual(2.25f / 3f, progress, TestHelper.progressEpsilon);

            deferred3.ReportProgress(0.5f);
            Assert.AreEqual(2.5f / 3f, progress, TestHelper.progressEpsilon);

            deferred3.Resolve();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);

            deferred1.ReportProgress(0.9f);
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);

            deferred1.Resolve();
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

            Assert.AreEqual(0f, progress, TestHelper.progressEpsilon);

            deferred1.ReportProgress(0.25f);
            Assert.AreEqual(0.25f / 3f, progress, TestHelper.progressEpsilon);

            cancelationSource.Cancel();
            Assert.AreEqual(2f / 3f, progress, TestHelper.progressEpsilon);

            deferred1.ReportProgress(0.5f);
            Assert.AreEqual(2f / 3f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.5f);
            Assert.AreEqual(2f / 3f, progress, TestHelper.progressEpsilon);

            deferred2.Resolve(1);
            Assert.AreEqual(2f / 3f, progress, TestHelper.progressEpsilon);

            deferred1.ReportProgress(0.7f);
            Assert.AreEqual(2f / 3f, progress, TestHelper.progressEpsilon);

            deferred3.ReportProgress(0.25f);
            Assert.AreEqual(2.25f / 3f, progress, TestHelper.progressEpsilon);

            deferred1.ReportProgress(0.8f);
            Assert.AreEqual(2.25f / 3f, progress, TestHelper.progressEpsilon);

            deferred3.ReportProgress(0.5f);
            Assert.AreEqual(2.5f / 3f, progress, TestHelper.progressEpsilon);

            deferred3.Resolve(1);
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);

            deferred1.ReportProgress(0.9f);
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);

            deferred1.Resolve(1);
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);

            cancelationSource.Dispose();
            // Promise must be forgotten since it was never returned in onResolved because it was canceled.
            deferred2.Promise.Forget();
        }

        [Test]
        public void ProgressMayBeSubscribedToPreservedPromiseMultipleTimes_Pending_void()
        {
            int invokedCount = 0;
            var deferred = Promise.NewDeferred();
            var promise = deferred.Promise.Preserve();

            for (int i = 0; i < 600; ++i)
            {
                promise.Progress(v => { ++invokedCount; }).Forget();
            }

            promise.Forget();
            Assert.AreEqual(600, invokedCount);
            deferred.ReportProgress(0.1f);
            Assert.AreEqual(600 * 2, invokedCount);
            deferred.Resolve();
        }

        [Test]
        public void ProgressMayBeSubscribedToPreservedPromiseMultipleTimes_Resolved_void()
        {
            int invokedCount = 0;
            Promise promise = Promise.Resolved().Preserve();

            for (int i = 0; i < 600; ++i)
            {
                promise.Progress(v => { ++invokedCount; }).Forget();
            }

            promise.Forget();
            Assert.AreEqual(600, invokedCount);
        }

        [Test]
        public void ProgressMayBeSubscribedToPreservedPromiseMultipleTimes_Pending_T()
        {
            int invokedCount = 0;
            var deferred = Promise.NewDeferred<int>();
            var promise = deferred.Promise.Preserve();

            for (int i = 0; i < 600; ++i)
            {
                promise.Progress(v => { ++invokedCount; }).Forget();
            }

            promise.Forget();
            Assert.AreEqual(600, invokedCount);
            deferred.ReportProgress(0.1f);
            Assert.AreEqual(600 * 2, invokedCount);
            deferred.Resolve(1);
        }

        [Test]
        public void ProgressMayBeSubscribedToPreservedPromiseMultipleTimes_Resolved_T()
        {
            int invokedCount = 0;
            Promise<int> promise = Promise.Resolved(1).Preserve();

            for (int i = 0; i < 600; ++i)
            {
                promise.Progress(v => { ++invokedCount; }).Forget();
            }

            promise.Forget();
            Assert.AreEqual(600, invokedCount);
        }

        [Test]
        public void ProgressMayBeChainSubscribedMultipleTimes_Pending_void()
        {
            int invokedCount = 0;
            var deferred = Promise.NewDeferred();
            var promise = deferred.Promise;

            for (int i = 0; i < 600; ++i)
            {
                promise = promise.Progress(v => { ++invokedCount; });
            }

            promise.Forget();
            Assert.AreEqual(600, invokedCount);
            deferred.ReportProgress(0.1f);
            Assert.AreEqual(600 * 2, invokedCount);
            deferred.Resolve();
        }

        [Test]
        public void ProgressMayBeChainSubscribedMultipleTimes_Resolved_void()
        {
            int invokedCount = 0;
            Promise promise = Promise.Resolved();

            for (int i = 0; i < 600; ++i)
            {
                promise = promise.Progress(v => { ++invokedCount; });
            }

            promise.Forget();
            Assert.AreEqual(600, invokedCount);
        }

        [Test]
        public void ProgressMayBeChainSubscribedMultipleTimes_Pending_T()
        {
            int invokedCount = 0;
            var deferred = Promise.NewDeferred<int>();
            var promise = deferred.Promise;

            for (int i = 0; i < 600; ++i)
            {
                promise = promise.Progress(v => { ++invokedCount; });
            }

            promise.Forget();
            Assert.AreEqual(600, invokedCount);
            deferred.ReportProgress(0.1f);
            Assert.AreEqual(600 * 2, invokedCount);
            deferred.Resolve(1);
        }

        [Test]
        public void ProgressMayBeChainSubscribedMultipleTimes_Resolved_T()
        {
            int invokedCount = 0;
            Promise<int> promise = Promise.Resolved(1);

            for (int i = 0; i < 600; ++i)
            {
                promise = promise.Progress(v => { ++invokedCount; });
            }

            promise.Forget();
            Assert.AreEqual(600, invokedCount);
        }

        [Test]
        public void ProgressSubscribedToPreservedPromiseWillBeInvokedInOrder_Pending_void()
        {
            var deferred = Promise.NewDeferred();
            var promise = deferred.Promise.Preserve();
            int[] results = new int[600];

            for (int i = 0; i < 600; ++i)
            {
                int index = i;
                promise.Progress(i, (num, v) => { results[index] = num; }).Forget();
            }

            promise.Forget();
            deferred.ReportProgress(0.1f);
            CollectionAssert.AreEqual(System.Linq.Enumerable.Range(0, results.Length), results);
            deferred.Resolve();
        }

        [Test]
        public void ProgressSubscribedToPreservedPromiseWillBeInvokedInOrder_Resolved_void()
        {
            Promise promise = Promise.Resolved().Preserve();
            int[] results = new int[600];

            for (int i = 0; i < 600; ++i)
            {
                int index = i;
                promise.Progress(i, (num, v) => { results[index] = num; }).Forget();
            }

            promise.Forget();
            CollectionAssert.AreEqual(System.Linq.Enumerable.Range(0, results.Length), results);
        }

        [Test]
        public void ProgressSubscribedToPreservedPromiseWillBeInvokedInOrder_Pending_T()
        {
            var deferred = Promise.NewDeferred<int>();
            var promise = deferred.Promise.Preserve();
            int[] results = new int[600];

            for (int i = 0; i < 600; ++i)
            {
                int index = i;
                promise.Progress(i, (num, v) => { results[index] = num; }).Forget();
            }

            promise.Forget();
            deferred.ReportProgress(0.1f);
            CollectionAssert.AreEqual(System.Linq.Enumerable.Range(0, results.Length), results);
            deferred.Resolve(1);
        }

        [Test]
        public void ProgressSubscribedToPreservedPromiseWillBeInvokedInOrder_Resolved_T()
        {
            Promise<int> promise = Promise.Resolved(1).Preserve();
            int[] results = new int[600];

            for (int i = 0; i < 600; ++i)
            {
                int index = i;
                promise.Progress(i, (num, v) => { results[index] = num; }).Forget();
            }

            promise.Forget();
            CollectionAssert.AreEqual(System.Linq.Enumerable.Range(0, results.Length), results);
        }

        [Test]
        public void ProgressChainSubscribedWillBeInvokedInOrder_Pending_void()
        {
            var deferred = Promise.NewDeferred();
            var promise = deferred.Promise;
            int[] results = new int[600];

            for (int i = 0; i < 600; ++i)
            {
                int index = i;
                promise = promise.Progress(i, (num, v) => { results[index] = num; });
            }

            promise.Forget();
            deferred.ReportProgress(0.1f);
            CollectionAssert.AreEqual(System.Linq.Enumerable.Range(0, results.Length), results);
            deferred.Resolve();
        }

        [Test]
        public void ProgressChainSubscribedWillBeInvokedInOrder_Resolved_void()
        {
            Promise promise = Promise.Resolved();
            int[] results = new int[600];

            for (int i = 0; i < 600; ++i)
            {
                int index = i;
                promise = promise.Progress(i, (num, v) => { results[index] = num; });
            }

            promise.Forget();
            CollectionAssert.AreEqual(System.Linq.Enumerable.Range(0, results.Length), results);
        }

        [Test]
        public void ProgressChainSubscribedWillBeInvokedInOrder_Pending_T()
        {
            var deferred = Promise.NewDeferred<int>();
            var promise = deferred.Promise;
            int[] results = new int[600];

            for (int i = 0; i < 600; ++i)
            {
                int index = i;
                promise = promise.Progress(i, (num, v) => { results[index] = num; });
            }

            promise.Forget();
            deferred.ReportProgress(0.1f);
            CollectionAssert.AreEqual(System.Linq.Enumerable.Range(0, results.Length), results);
            deferred.Resolve(1);
        }

        [Test]
        public void ProgressChainSubscribedWillBeInvokedInOrder_Resolved_T()
        {
            Promise<int> promise = Promise.Resolved(1);
            int[] results = new int[600];

            for (int i = 0; i < 600; ++i)
            {
                int index = i;
                promise = promise.Progress(i, (num, v) => { results[index] = num; });
            }

            promise.Forget();
            CollectionAssert.AreEqual(System.Linq.Enumerable.Range(0, results.Length), results);
        }

#else // PROMISE_PROGRESS

#pragma warning disable CS0618 // Type or member is obsolete
        [Test]
        public void ProgressDisabled_OnProgressWillNotBeInvoked_void()
        {
            var deferred = Promise.NewDeferred();

            float progress = float.NaN;

            var progressListener = new ProgressListener(progress);

            deferred.Promise
                .Progress(p => progress = p)
                .Progress(50, (cv, p) => progress = p)
                .Progress(progressListener)
                .Forget();

            deferred.ReportProgress(0.5f);

            Assert.IsNaN(progress);
            Assert.IsNaN(progressListener.CurrentProgress);

            deferred.Resolve();

            Assert.IsNaN(progress);
            Assert.IsNaN(progressListener.CurrentProgress);
        }

        [Test]
        public void ProgressDisabled_OnProgressWillNotBeInvoked_T()
        {
            var deferred = Promise<int>.NewDeferred();

            float progress = float.NaN;

            var progressListener = new ProgressListener(progress);

            deferred.Promise
                .Progress(p => progress = p)
                .Progress(50, (cv, p) => progress = p)
                .Progress(progressListener)
                .Forget();

            deferred.ReportProgress(0.5f);

            Assert.IsNaN(progress);
            Assert.IsNaN(progressListener.CurrentProgress);

            deferred.Resolve(1);

            Assert.IsNaN(progress);
            Assert.IsNaN(progressListener.CurrentProgress);
        }
#pragma warning restore CS0618 // Type or member is obsolete

#endif // PROMISE_PROGRESS
    }
}