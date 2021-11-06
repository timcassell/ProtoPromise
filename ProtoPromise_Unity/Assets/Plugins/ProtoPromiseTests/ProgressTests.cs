﻿#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
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

#if PROMISE_PROGRESS
        [Test]
        public void ProgressMayBeReportedWhenThePromisesProgressHasChanged_void0(
            [Values] ProgressType progressType)
        {
            var deferred = Promise.NewDeferred();

            ProgressHelper progressHelper = new ProgressHelper(progressType, SynchronizationType.Foreground);
            progressHelper.Subscribe(deferred.Promise)
                .Forget();

            TestHelper.ExecuteForegroundCallbacks();
            progressHelper.AssertCurrentProgress(0f);

            progressHelper.ReportProgressAndAssertResult(deferred, 0.25f, 0f, false, false);
            progressHelper.ReportProgressAndAssertResult(deferred, 0.5f, 0f, false, false);
            progressHelper.ReportProgressAndAssertResult(deferred, 0.75f, 0.75f);

            deferred.Resolve();
        }

        [Test]
        public void ProgressMayBeReportedWhenThePromisesProgressHasChanged_void1(
            [Values] ProgressType progressType)
        {
            var cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred(cancelationSource.Token);

            ProgressHelper progressHelper = new ProgressHelper(progressType, SynchronizationType.Foreground);
            progressHelper.Subscribe(deferred.Promise)
                .Forget();

            TestHelper.ExecuteForegroundCallbacks();
            progressHelper.AssertCurrentProgress(0f);

            progressHelper.ReportProgressAndAssertResult(deferred, 0.25f, 0f, false, false);
            progressHelper.ReportProgressAndAssertResult(deferred, 0.5f, 0f, false, false);
            progressHelper.ReportProgressAndAssertResult(deferred, 0.75f, 0.75f);

            deferred.Resolve();
            cancelationSource.Dispose();
        }

        [Test]
        public void ProgressMayBeReportedWhenThePromisesProgressHasChanged_T0(
            [Values] ProgressType progressType)
        {
            var deferred = Promise.NewDeferred<int>();

            ProgressHelper progressHelper = new ProgressHelper(progressType, SynchronizationType.Foreground);
            progressHelper.Subscribe(deferred.Promise)
                .Forget();

            TestHelper.ExecuteForegroundCallbacks();
            progressHelper.AssertCurrentProgress(0f);

            progressHelper.ReportProgressAndAssertResult(deferred, 0.25f, 0f, false, false);
            progressHelper.ReportProgressAndAssertResult(deferred, 0.5f, 0f, false, false);
            progressHelper.ReportProgressAndAssertResult(deferred, 0.75f, 0.75f);
            
            deferred.Resolve(1);
        }

        [Test]
        public void ProgressMayBeReportedWhenThePromisesProgressHasChanged_T1(
            [Values] ProgressType progressType)
        {
            var cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred<int>(cancelationSource.Token);

            ProgressHelper progressHelper = new ProgressHelper(progressType, SynchronizationType.Foreground);
            progressHelper.Subscribe(deferred.Promise)
                .Forget();

            TestHelper.ExecuteForegroundCallbacks();
            progressHelper.AssertCurrentProgress(0f);

            progressHelper.ReportProgressAndAssertResult(deferred, 0.25f, 0f, false, false);
            progressHelper.ReportProgressAndAssertResult(deferred, 0.5f, 0f, false, false);
            progressHelper.ReportProgressAndAssertResult(deferred, 0.75f, 0.75f);

            deferred.Resolve(1);
            cancelationSource.Dispose();
        }

        [Test]
        public void OnProgressWillBeInvokedWith1WhenPromiseIsResolved_void0(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred = Promise.NewDeferred();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            progressHelper.Subscribe(deferred.Promise)
                .Forget();

            progressHelper.ResolveAndAssertResult(deferred, 1f);
        }

        [Test]
        public void OnProgressWillBeInvokedWith1WhenPromiseIsResolved_void1(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            progressHelper.Subscribe(Promise.Resolved())
                .Forget();

            progressHelper.AssertCurrentProgress(1f);
        }

        [Test]
        public void OnProgressWillBeInvokedWith1WhenPromiseIsResolved_T0(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred = Promise.NewDeferred<int>();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            progressHelper.Subscribe(deferred.Promise)
                .Forget();

            progressHelper.ResolveAndAssertResult(deferred, 1, 1f);
        }

        [Test]
        public void OnProgressWillBeInvokedWith1WhenPromiseIsResolved_T1(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            progressHelper.Subscribe(Promise.Resolved(1))
                .Forget();

            progressHelper.AssertCurrentProgress(1f);
        }

        [Test]
        public void OnProgressWillNoLongerBeInvokedWhenPromiseIsRejected_void0(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred = Promise.NewDeferred();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            progressHelper.Subscribe(deferred.Promise)
                .Catch(() => { })
                .Forget();

            progressHelper.ReportProgressAndAssertResult(deferred, 0.5f, 0.5f);
            progressHelper.RejectAndAssertResult(deferred, "Reject", 0.5f, false);
            deferred.TryReportProgress(0.75f);
            progressHelper.AssertCurrentProgress(0.5f, false);
        }

        [Test]
        public void OnProgressWillNoLongerBeInvokedWhenPromiseIsRejected_void1(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            progressHelper.Subscribe(Promise.Rejected("Reject"))
                .Catch(() => { })
                .Forget();

            progressHelper.AssertCurrentProgress(float.NaN, false);
        }

        [Test]
        public void OnProgressWillNoLongerBeInvokedWhenPromiseIsRejected_T0(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred = Promise.NewDeferred<int>();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            progressHelper.Subscribe(deferred.Promise)
                .Catch(() => { })
                .Forget();

            progressHelper.ReportProgressAndAssertResult(deferred, 0.5f, 0.5f);
            progressHelper.RejectAndAssertResult(deferred, "Reject", 0.5f, false);
            deferred.TryReportProgress(0.75f);
            progressHelper.AssertCurrentProgress(0.5f, false);
        }

        [Test]
        public void OnProgressWillNoLongerBeInvokedWhenPromiseIsRejected_T1(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            progressHelper.Subscribe(Promise<int>.Rejected("Reject"))
                .Catch(() => { })
                .Forget();

            progressHelper.AssertCurrentProgress(float.NaN, false);
        }

        [Test]
        public void WhenAPromiseIsRejectedAndCaught_OnProgressWillBeInvokedWithExpectedValue_void(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            progressHelper.Subscribe(
                deferred.Promise
                    .Catch(() => deferred2.Promise))
                .Forget();

            progressHelper.AssertCurrentProgress(0f / 2f);

            progressHelper.ReportProgressAndAssertResult(deferred, 0.5f, 0.5f / 2f);
            progressHelper.RejectAndAssertResult(deferred, "Reject", 1f / 2f);

            progressHelper.ReportProgressAndAssertResult(deferred2, 0.5f, 1.5f / 2f);
            progressHelper.ResolveAndAssertResult(deferred2, 2f / 2f);
        }

        [Test]
        public void WhenAPromiseIsRejectedAndCaught_OnProgressWillBeInvokedWithExpectedValue_T(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            progressHelper.Subscribe(
                deferred.Promise
                    .Catch(() => deferred2.Promise))
                .Forget();

            progressHelper.AssertCurrentProgress(0f / 2f);

            progressHelper.ReportProgressAndAssertResult(deferred, 0.5f, 0.5f / 2f);
            progressHelper.RejectAndAssertResult(deferred, "Reject", 1f / 2f);

            progressHelper.ReportProgressAndAssertResult(deferred2, 0.5f, 1.5f / 2f);
            progressHelper.ResolveAndAssertResult(deferred2, 1, 2f / 2f);
        }

        [Test]
        public void OnProgressWillNoLongerBeInvokedWhenPromiseIsCanceled_void0(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred = Promise.NewDeferred();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            progressHelper.Subscribe(deferred.Promise)
                .Forget();

            progressHelper.AssertCurrentProgress(0f);
            progressHelper.ReportProgressAndAssertResult(deferred, 0.5f, 0.5f);
            progressHelper.CancelAndAssertResult(deferred, 0.5f, false);
            deferred.TryReportProgress(0.75f);
            progressHelper.AssertCurrentProgress(0.5f, false);
        }

        [Test]
        public void OnProgressWillNoLongerBeInvokedWhenPromiseIsCanceled_void1(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred(cancelationSource.Token);

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            progressHelper.Subscribe(deferred.Promise)
                .Forget();

            progressHelper.AssertCurrentProgress(0f);
            progressHelper.ReportProgressAndAssertResult(deferred, 0.5f, 0.5f);
            progressHelper.CancelAndAssertResult(cancelationSource, 0.5f, false);

            cancelationSource.Dispose();

            progressHelper.AssertCurrentProgress(0.5f, false);
        }

        [Test]
        public void OnProgressWillNoLongerBeInvokedWhenPromiseIsCanceled_void2(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            progressHelper.Subscribe(Promise.Canceled())
                .Forget();

            progressHelper.AssertCurrentProgress(float.NaN, false);
        }

        [Test]
        public void OnProgressWillNoLongerBeInvokedWhenPromiseIsCanceled_T0(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred = Promise.NewDeferred<int>();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            progressHelper.Subscribe(deferred.Promise)
                .Forget();

            progressHelper.AssertCurrentProgress(0f);
            progressHelper.ReportProgressAndAssertResult(deferred, 0.5f, 0.5f);
            progressHelper.CancelAndAssertResult(deferred, 0.5f, false);

            deferred.TryReportProgress(0.75f);
            progressHelper.AssertCurrentProgress(0.5f, false);
        }

        [Test]
        public void OnProgressWillNoLongerBeInvokedWhenPromiseIsCanceled_T1(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred<int>(cancelationSource.Token);

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            progressHelper.Subscribe(deferred.Promise)
                .Forget();

            progressHelper.AssertCurrentProgress(0f);
            progressHelper.ReportProgressAndAssertResult(deferred, 0.5f, 0.5f);
            progressHelper.CancelAndAssertResult(cancelationSource, 0.5f, false);

            cancelationSource.Dispose();

            progressHelper.AssertCurrentProgress(0.5f, false);
        }

        [Test]
        public void OnProgressWillNoLongerBeInvokedWhenPromiseIsCanceled_T2(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            progressHelper.Subscribe(Promise<int>.Canceled())
                .Forget();

            progressHelper.AssertCurrentProgress(float.NaN, false);
        }

        [Test]
        public void OnProgressWillNoLongerBeInvokedWhenPromiseIsCanceled1(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            progressHelper.Subscribe(
                deferred.Promise
                    .ThenDuplicate()
                    .ThenDuplicate(cancelationSource.Token))
                .Forget();

            progressHelper.AssertCurrentProgress(0f);

            progressHelper.ReportProgressAndAssertResult(deferred, 0.25f, 0.25f);

            progressHelper.CancelAndAssertResult(cancelationSource, 0.25f, false);
            progressHelper.ReportProgressAndAssertResult(deferred, 0.5f, 0.25f, false);
            progressHelper.ResolveAndAssertResult(deferred, 0.25f, false);

            cancelationSource.Dispose();
        }

        [Test]
        public void OnProgressWillNoLongerBeInvokedWhenPromiseIsCanceled2(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            progressHelper.Subscribe(
                deferred.Promise
                    .ThenDuplicate(cancelationSource.Token)
                    .ThenDuplicate())
                .Forget();

            progressHelper.AssertCurrentProgress(0f);

            progressHelper.ReportProgressAndAssertResult(deferred, 0.25f, 0.25f);

            progressHelper.CancelAndAssertResult(cancelationSource, 0.25f, false);
            progressHelper.ReportProgressAndAssertResult(deferred, 0.5f, 0.25f, false);
            progressHelper.ResolveAndAssertResult(deferred, 0.25f, false);

            cancelationSource.Dispose();
        }

        [Test]
        public void OnProgressWillNoLongerBeInvokedWhenPromiseIsCanceled3(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            progressHelper.Subscribe(
                deferred.Promise
                    .ThenDuplicate()
                    .Then(() =>
                        deferred2.Promise
                            .ThenDuplicate(cancelationSource.Token)
                    ))
                .Forget();

            progressHelper.AssertCurrentProgress(0f / 2f);

            progressHelper.ReportProgressAndAssertResult(deferred, 0.5f, 0.5f / 2f);

            progressHelper.ResolveAndAssertResult(deferred, 1f / 2f);
            progressHelper.CancelAndAssertResult(cancelationSource, 1f / 2f, false);
            progressHelper.ResolveAndAssertResult(deferred2, 1f / 2f, false);

            cancelationSource.Dispose();
        }

        [Test]
        public void OnProgressWillNoLongerBeInvokedWhenPromiseIsCanceled4(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            progressHelper.Subscribe(
                deferred.Promise
                    .Then(() =>
                        deferred2.Promise
                            .ThenDuplicate(cancelationSource.Token)
                    )
                    .ThenDuplicate())
                .Forget();

            progressHelper.AssertCurrentProgress(0f / 2f);

            progressHelper.ReportProgressAndAssertResult(deferred, 0.5f, 0.5f / 2f);

            progressHelper.ResolveAndAssertResult(deferred, 1f / 2f);
            progressHelper.CancelAndAssertResult(cancelationSource, 1f / 2f, false);
            progressHelper.ResolveAndAssertResult(deferred2, 1f / 2f, false);

            cancelationSource.Dispose();
        }

        [Test]
        public void OnProgressWillNoLongerBeInvokedWhenPromiseIsCanceled5(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred = Promise.NewDeferred();
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred2 = Promise.NewDeferred(cancelationSource.Token);

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            progressHelper.Subscribe(
                deferred.Promise
                    .ThenDuplicate()
                    .Then(() => deferred2.Promise))
                .Forget();

            progressHelper.AssertCurrentProgress(0f / 2f);

            progressHelper.ReportProgressAndAssertResult(deferred, 0.5f, 0.5f / 2f);

            progressHelper.ResolveAndAssertResult(deferred, 1f / 2f);
            progressHelper.CancelAndAssertResult(cancelationSource, 1f / 2f, false);
            deferred2.TryReportProgress(0.5f);
            progressHelper.AssertCurrentProgress(1f / 2f, false);

            cancelationSource.Dispose();
        }

        [Test]
        public void OnProgressWillNoLongerBeInvokedWhenPromiseIsCanceled6(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred = Promise.NewDeferred();
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred2 = Promise.NewDeferred(cancelationSource.Token);

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            progressHelper.Subscribe(
                deferred.Promise
                    .Then(() => deferred2.Promise)
                    .ThenDuplicate())
                .Forget();

            progressHelper.AssertCurrentProgress(0f / 2f);

            progressHelper.ReportProgressAndAssertResult(deferred, 0.5f, 0.5f / 2f);

            progressHelper.ResolveAndAssertResult(deferred, 1f / 2f);
            progressHelper.CancelAndAssertResult(cancelationSource, 1f / 2f, false);
            deferred2.TryReportProgress(0.5f);
            progressHelper.AssertCurrentProgress(1f / 2f, false);

            cancelationSource.Dispose();
        }

        [Test]
        public void OnProgressWillNoLongerBeInvokedWhenTokenIsCanceled_void(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred = Promise.NewDeferred();
            CancelationSource cancelationSource = CancelationSource.New();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            progressHelper.Subscribe(deferred.Promise, cancelationSource.Token)
                .Forget();

            progressHelper.AssertCurrentProgress(0f);

            progressHelper.ReportProgressAndAssertResult(deferred, 0.25f, 0.25f);

            progressHelper.CancelAndAssertResult(cancelationSource, 0.25f, false);

            progressHelper.ReportProgressAndAssertResult(deferred, 0.5f, 0.25f, false);
            deferred.TryResolve();
            progressHelper.AssertCurrentProgress(0.25f, false);

            cancelationSource.Dispose();
        }

        [Test]
        public void OnProgressWillNoLongerBeInvokedWhenTokenIsCanceled_T(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred = Promise.NewDeferred<int>();
            CancelationSource cancelationSource = CancelationSource.New();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            progressHelper.Subscribe(deferred.Promise, cancelationSource.Token)
                .Forget();

            progressHelper.AssertCurrentProgress(0f);

            progressHelper.ReportProgressAndAssertResult(deferred, 0.25f, 0.25f);

            progressHelper.CancelAndAssertResult(cancelationSource, 0.25f, false);

            progressHelper.ReportProgressAndAssertResult(deferred, 0.5f, 0.25f, false);
            progressHelper.ResolveAndAssertResult(deferred, 1, 0.25f, false);

            cancelationSource.Dispose();
        }

        [Test]
        public void ProgressWillBeReportedWithCurrentProgressWhenItIsSubscribed_void(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred = Promise.NewDeferred();
            deferred.ReportProgress(0.5f);

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            progressHelper.Subscribe(deferred.Promise)
                .Forget();

            progressHelper.AssertCurrentProgress(0.5f);
            progressHelper.ResolveAndAssertResult(deferred, 1f);
        }

        [Test]
        public void ProgressWillBeReportedWithCurrentProgressWhenItIsSubscribed_T(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred = Promise.NewDeferred<int>();
            deferred.ReportProgress(0.5f);

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            progressHelper.Subscribe(deferred.Promise)
                .Forget();

            progressHelper.AssertCurrentProgress(0.5f);
            progressHelper.ResolveAndAssertResult(deferred, 1, 1f);
        }

        [Test]
        public void MultipleOnProgressAreInvokedProperly(
            [Values(SynchronizationOption.Synchronous, SynchronizationOption.Foreground)] SynchronizationOption synchronizationOption)
        {
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();

            float progress = float.NaN;
            bool firstInvoked = false;
            bool secondInvoked = false;

            deferred.Promise
                .ThenDuplicate()
                .ThenDuplicate(cancelationSource.Token)
                .Progress(p => { firstInvoked = true; progress = p; }, synchronizationOption)
                .Finally(cancelationSource.Dispose)
                .Forget();

            TestHelper.ExecuteForegroundCallbacks();

            Assert.AreEqual(0f, progress, TestHelper.progressEpsilon);
            Assert.IsTrue(firstInvoked);
            Assert.IsFalse(secondInvoked);

            progress = float.NaN;
            firstInvoked = false;
            secondInvoked = false;
            cancelationSource.Cancel();
            TestHelper.ExecuteForegroundCallbacks();

            deferred2.Promise
                .ThenDuplicate()
                .ThenDuplicate()
                .Progress(p => { secondInvoked = true; progress = p; }, synchronizationOption)
                .Forget();

            deferred.ReportProgress(0.5f);
            TestHelper.ExecuteForegroundCallbacks();
            Assert.AreEqual(0f, progress, TestHelper.progressEpsilon);
            Assert.IsTrue(secondInvoked);
            Assert.IsFalse(firstInvoked);

            progress = float.NaN;
            firstInvoked = false;
            secondInvoked = false;

            deferred.Resolve();
            deferred2.Resolve();
            TestHelper.ExecuteForegroundCallbacks();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);
            Assert.IsTrue(secondInvoked);
            Assert.IsFalse(firstInvoked);
        }

        [Test]
        public void OnProgressWillNotBeInvokedWith1UntilPromiseIsResolved(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred = Promise.NewDeferred();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            progressHelper.Subscribe(deferred.Promise)
                .Forget();

            progressHelper.AssertCurrentProgress(0f);
            progressHelper.ReportProgressAndAssertResult(deferred, 1f, 0f, false);
            progressHelper.ResolveAndAssertResult(deferred, 1f);
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
            Assert.Throws<ArgumentNullException>(() =>
            {
                deferred.Promise.Progress(default(IProgress<float>));
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
            Assert.Throws<ArgumentNullException>(() =>
            {
                deferredInt.Promise.Progress(default(IProgress<float>));
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
        public void OnProgressWillBeInvokedWithANormalizedValueFromAllWaitPromisesInTheChain_void_void(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred = Promise.NewDeferred();
            var nextDeferred = Promise.NewDeferred();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            progressHelper.Subscribe(
                deferred.Promise
                    .Then(() => nextDeferred.Promise))
                .Forget();

            progressHelper.AssertCurrentProgress(0f / 2f);

            progressHelper.ReportProgressAndAssertResult(deferred, 0.5f, 0.5f / 2f);
            progressHelper.ResolveAndAssertResult(deferred, 1f / 2f);

            try
            {
                progressHelper.ReportProgressAndAssertResult(nextDeferred, 0.5f, 1.5f / 2f);
            }
            catch
            {
                throw;
            }
            progressHelper.ResolveAndAssertResult(nextDeferred, 2f / 2f);
        }

        [Test]
        public void OnProgressWillBeInvokedWithANormalizedValueFromAllWaitPromisesInTheChain_void_T(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred = Promise.NewDeferred();
            var nextDeferred = Promise.NewDeferred<int>();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            progressHelper.Subscribe(
                deferred.Promise
                    .Then(() => nextDeferred.Promise))
                .Forget();

            progressHelper.AssertCurrentProgress(0f / 2f);

            progressHelper.ReportProgressAndAssertResult(deferred, 0.5f, 0.5f / 2f);
            progressHelper.ResolveAndAssertResult(deferred, 1f / 2f);

            // TODO: why is this failing with background synchronization?
            try
            {
                progressHelper.ReportProgressAndAssertResult(nextDeferred, 0.5f, 1.5f / 2f);
            }
            catch
            {
                throw;
            }
            progressHelper.ResolveAndAssertResult(nextDeferred, 1, 2f / 2f);
        }

        [Test]
        public void OnProgressWillBeInvokedWithANormalizedValueFromAllWaitPromisesInTheChain_T_void(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred = Promise.NewDeferred<int>();
            var nextDeferred = Promise.NewDeferred();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            progressHelper.Subscribe(
                deferred.Promise
                    .Then(() => nextDeferred.Promise))
                .Forget();

            progressHelper.AssertCurrentProgress(0f / 2f);

            progressHelper.ReportProgressAndAssertResult(deferred, 0.5f, 0.5f / 2f);
            progressHelper.ResolveAndAssertResult(deferred, 1, 1f / 2f);

            progressHelper.ReportProgressAndAssertResult(nextDeferred, 0.5f, 1.5f / 2f);
            progressHelper.ResolveAndAssertResult(nextDeferred, 2f / 2f);
        }

        [Test]
        public void OnProgressWillBeInvokedWithANormalizedValueFromAllWaitPromisesInTheChain_T_T(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred = Promise.NewDeferred<int>();
            var nextDeferred = Promise.NewDeferred<int>();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            progressHelper.Subscribe(
                deferred.Promise
                    .Then(() => nextDeferred.Promise))
                .Forget();

            progressHelper.AssertCurrentProgress(0f / 2f);

            progressHelper.ReportProgressAndAssertResult(deferred, 0.5f, 0.5f / 2f);
            progressHelper.ResolveAndAssertResult(deferred, 1, 1f / 2f);

            progressHelper.ReportProgressAndAssertResult(nextDeferred, 0.5f, 1.5f / 2f);
            progressHelper.ResolveAndAssertResult(nextDeferred, 1, 2f / 2f);
        }

        [Test]
        public void OnProgressWillBeInvokedWithANormalizedValueFromAllWaitPromisesInTheChain_voidPending_voidResolved(
            [Values] ProgressType progressType,
            // This test is unverifiable when progress is executed on background threads.
            [Values(SynchronizationType.Synchronous, SynchronizationType.Foreground, SynchronizationType.Explicit)] SynchronizationType synchronizationType)
        {
            var deferred = Promise.NewDeferred();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            progressHelper.Subscribe(
                deferred.Promise
                    .Then(() => Promise.Resolved()))
                .Forget();

            progressHelper.AssertCurrentProgress(0f / 2f);

            progressHelper.ReportProgressAndAssertResult(deferred, 0.5f, 0.5f / 2f);
            progressHelper.ResolveAndAssertResult(deferred, 2f / 2f);
        }

        [Test]
        public void OnProgressWillBeInvokedWithANormalizedValueFromAllWaitPromisesInTheChain_voidResolved_voidPending(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred = Promise.NewDeferred();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            progressHelper.Subscribe(
                Promise.Resolved()
                    .Then(() => deferred.Promise))
                .Forget();

            progressHelper.AssertCurrentProgress(1f / 2f);

            progressHelper.ReportProgressAndAssertResult(deferred, 0.5f, 1.5f / 2f);
            progressHelper.ResolveAndAssertResult(deferred, 2f / 2f);
        }

        [Test]
        public void OnProgressWillBeInvokedWithANormalizedValueFromAllWaitPromisesInTheChain_voidPending_TResolved(
            [Values] ProgressType progressType,
            // This test is unverifiable when progress is executed on background threads.
            [Values(SynchronizationType.Synchronous, SynchronizationType.Foreground, SynchronizationType.Explicit)] SynchronizationType synchronizationType)
        {
            var deferred = Promise.NewDeferred();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            progressHelper.Subscribe(
                deferred.Promise
                    .Then(() => Promise.Resolved()))
                .Forget();

            progressHelper.AssertCurrentProgress(0f / 2f);

            progressHelper.ReportProgressAndAssertResult(deferred, 0.5f, 0.5f / 2f);
            progressHelper.ResolveAndAssertResult(deferred, 2f / 2f);
        }

        [Test]
        public void OnProgressWillBeInvokedWithANormalizedValueFromAllWaitPromisesInTheChain_TResolved_voidPending(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred = Promise.NewDeferred();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            progressHelper.Subscribe(
                Promise.Resolved()
                    .Then(() => deferred.Promise))
                .Forget();

            progressHelper.AssertCurrentProgress(1f / 2f);

            progressHelper.ReportProgressAndAssertResult(deferred, 0.5f, 1.5f / 2f);
            progressHelper.ResolveAndAssertResult(deferred, 2f / 2f);
        }

        [Test]
        public void OnProgressWillBeInvokedWithANormalizedValueFromAllWaitPromisesInTheChain_voidResolved_voidResolved_voidPending(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred = Promise.NewDeferred();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            progressHelper.Subscribe(
                Promise.Resolved()
                    .Then(() => Promise.Resolved())
                    .Then(() => deferred.Promise))
                .Forget();

            progressHelper.AssertCurrentProgress(2f / 3f);

            progressHelper.ReportProgressAndAssertResult(deferred, 0.5f, 2.5f / 3f);
            progressHelper.ResolveAndAssertResult(deferred, 3f / 3f);
        }

        [Test]
        public void OnProgressWillBeInvokedWithANormalizedValueFromAllWaitPromisesInTheChain_TResolved_TResolved_voidPending(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred = Promise.NewDeferred();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            progressHelper.Subscribe(
                Promise.Resolved(1)
                    .Then(() => Promise.Resolved(1))
                    .Then(() => deferred.Promise))
                .Forget();

            progressHelper.AssertCurrentProgress(2f / 3f);

            progressHelper.ReportProgressAndAssertResult(deferred, 0.5f, 2.5f / 3f);
            progressHelper.ResolveAndAssertResult(deferred, 3f / 3f);
        }

        [Test]
        public void OnProgressWillBeInvokedProperlyFromARecoveredPromise0(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();
            var deferred3 = Promise.NewDeferred();
            CancelationSource cancelationSource = CancelationSource.New();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            progressHelper.Subscribe(
                deferred1.Promise
                    .Then(() => deferred2.Promise, cancelationSource.Token)
                    .ContinueWith(_ => deferred3.Promise))
                .Forget();

            progressHelper.AssertCurrentProgress(0f / 3f);

            progressHelper.ReportProgressAndAssertResult(deferred1, 0.25f, 0.25f / 3f);
            progressHelper.CancelAndAssertResult(cancelationSource, 2f / 3f);

            progressHelper.ReportProgressAndAssertResult(deferred1, 0.5f, 2f / 3f, false);
            progressHelper.ReportProgressAndAssertResult(deferred2, 0.5f, 2f / 3f, false);
            progressHelper.ResolveAndAssertResult(deferred2, 2f / 3f, false);
            progressHelper.ReportProgressAndAssertResult(deferred1, 0.7f, 2f / 3f, false);

            progressHelper.ReportProgressAndAssertResult(deferred3, 0.25f, 2.25f / 3f);

            progressHelper.ReportProgressAndAssertResult(deferred1, 0.8f, 2.25f / 3f, false);

            progressHelper.ReportProgressAndAssertResult(deferred3, 0.5f, 2.5f / 3f);
            progressHelper.ResolveAndAssertResult(deferred3, 3f / 3f);

            progressHelper.ReportProgressAndAssertResult(deferred1, 0.9f, 3f / 3f, false);
            progressHelper.ResolveAndAssertResult(deferred1, 3f / 3f, false);

            cancelationSource.Dispose();
            // Promise must be forgotten since it was never returned in onResolved because it was canceled.
            deferred2.Promise.Forget();
        }

        [Test]
        public void OnProgressWillBeInvokedProperlyFromARecoveredPromise1(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();
            var deferred3 = Promise.NewDeferred<int>();
            CancelationSource cancelationSource = CancelationSource.New();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            progressHelper.Subscribe(
                deferred1.Promise
                    .Then(() => deferred2.Promise, cancelationSource.Token)
                    .ContinueWith(_ => deferred3.Promise))
                .Forget();

            progressHelper.AssertCurrentProgress(0f / 3f);

            progressHelper.ReportProgressAndAssertResult(deferred1, 0.25f, 0.25f / 3f);
            progressHelper.CancelAndAssertResult(cancelationSource, 2f / 3f);

            progressHelper.ReportProgressAndAssertResult(deferred1, 0.5f, 2f / 3f, false);
            progressHelper.ReportProgressAndAssertResult(deferred2, 0.5f, 2f / 3f, false);
            progressHelper.ResolveAndAssertResult(deferred2, 1, 2f / 3f, false);
            progressHelper.ReportProgressAndAssertResult(deferred1, 0.7f, 2f / 3f, false);

            progressHelper.ReportProgressAndAssertResult(deferred3, 0.25f, 2.25f / 3f);

            progressHelper.ReportProgressAndAssertResult(deferred1, 0.8f, 2.25f / 3f, false);

            progressHelper.ReportProgressAndAssertResult(deferred3, 0.5f, 2.5f / 3f);
            progressHelper.ResolveAndAssertResult(deferred3, 1, 3f / 3f);

            progressHelper.ReportProgressAndAssertResult(deferred1, 0.9f, 3f / 3f, false);
            progressHelper.ResolveAndAssertResult(deferred1, 1, 3f / 3f, false);

            cancelationSource.Dispose();
            // Promise must be forgotten since it was never returned in onResolved because it was canceled.
            deferred2.Promise.Forget();
        }

        const int MultiProgressCount = 100;

        [Test]
        public void ProgressMayBeSubscribedToPreservedPromiseMultipleTimes_Pending_void(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred = Promise.NewDeferred();
            var promise = deferred.Promise.Preserve();
            ProgressHelper[] progressHelpers = new ProgressHelper[MultiProgressCount];

            for (int i = 0; i < MultiProgressCount; ++i)
            {
                progressHelpers[i] = new ProgressHelper(progressType, synchronizationType);
                progressHelpers[i].Subscribe(promise)
                    .Forget();
            }

            promise.Forget();
            for (int i = 0; i < MultiProgressCount; ++i)
            {
                progressHelpers[i].ReportProgressAndAssertResult(deferred, 0.1f, 0.1f);
            }
            deferred.Resolve();
            for (int i = 0; i < MultiProgressCount; ++i)
            {
                progressHelpers[i].AssertCurrentProgress(1f);
            }
        }

        [Test]
        public void ProgressMayBeSubscribedToPreservedPromiseMultipleTimes_Resolved_void(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            Promise promise = Promise.Resolved().Preserve();
            ProgressHelper[] progressHelpers = new ProgressHelper[MultiProgressCount];

            for (int i = 0; i < MultiProgressCount; ++i)
            {
                progressHelpers[i] = new ProgressHelper(progressType, synchronizationType);
                progressHelpers[i].Subscribe(promise)
                    .Forget();
            }

            promise.Forget();
            for (int i = 0; i < MultiProgressCount; ++i)
            {
                progressHelpers[i].AssertCurrentProgress(1f);
            }
        }

        [Test]
        public void ProgressMayBeSubscribedToPreservedPromiseMultipleTimes_Pending_T(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred = Promise.NewDeferred<int>();
            var promise = deferred.Promise.Preserve();
            ProgressHelper[] progressHelpers = new ProgressHelper[MultiProgressCount];

            for (int i = 0; i < MultiProgressCount; ++i)
            {
                progressHelpers[i] = new ProgressHelper(progressType, synchronizationType);
                progressHelpers[i].Subscribe(promise)
                    .Forget();
            }

            promise.Forget();
            for (int i = 0; i < MultiProgressCount; ++i)
            {
                progressHelpers[i].ReportProgressAndAssertResult(deferred, 0.1f, 0.1f);
            }
            deferred.Resolve(1);
            for (int i = 0; i < MultiProgressCount; ++i)
            {
                progressHelpers[i].AssertCurrentProgress(1f);
            }
        }

        [Test]
        public void ProgressMayBeSubscribedToPreservedPromiseMultipleTimes_Resolved_T(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            Promise<int> promise = Promise.Resolved(1).Preserve();
            ProgressHelper[] progressHelpers = new ProgressHelper[MultiProgressCount];

            for (int i = 0; i < MultiProgressCount; ++i)
            {
                progressHelpers[i] = new ProgressHelper(progressType, synchronizationType);
                progressHelpers[i].Subscribe(promise)
                    .Forget();
            }

            promise.Forget();
            for (int i = 0; i < MultiProgressCount; ++i)
            {
                progressHelpers[i].AssertCurrentProgress(1f);
            }
        }

        [Test]
        public void ProgressMayBeChainSubscribedMultipleTimes_Pending_void(
            [Values] ProgressType progressType,
            // This test is unverifiable when progress is executed on background threads.
            [Values(SynchronizationType.Synchronous, SynchronizationType.Foreground, SynchronizationType.Explicit)] SynchronizationType synchronizationType)
        {
            var deferred = Promise.NewDeferred();
            var promise = deferred.Promise;
            ProgressHelper[] progressHelpers = new ProgressHelper[MultiProgressCount];

            for (int i = 0; i < MultiProgressCount; ++i)
            {
                progressHelpers[i] = new ProgressHelper(progressType, synchronizationType);
                promise = progressHelpers[i].Subscribe(promise);
            }

            promise.Forget();
            for (int i = 0; i < MultiProgressCount; ++i)
            {
                progressHelpers[i].ReportProgressAndAssertResult(deferred, 0.1f, 0.1f);
            }
            deferred.Resolve();
            for (int i = 0; i < MultiProgressCount; ++i)
            {
                progressHelpers[i].AssertCurrentProgress(1f);
            }
        }

        [Test]
        public void ProgressMayBeChainSubscribedMultipleTimes_Resolved_void(
            [Values] ProgressType progressType,
            // This test is unverifiable when progress is executed on background threads.
            [Values(SynchronizationType.Synchronous, SynchronizationType.Foreground, SynchronizationType.Explicit)] SynchronizationType synchronizationType)
        {
            Promise promise = Promise.Resolved();
            ProgressHelper[] progressHelpers = new ProgressHelper[MultiProgressCount];

            for (int i = 0; i < MultiProgressCount; ++i)
            {
                progressHelpers[i] = new ProgressHelper(progressType, synchronizationType);
                promise = progressHelpers[i].Subscribe(promise);
            }

            promise.Forget();
            for (int i = 0; i < MultiProgressCount; ++i)
            {
                progressHelpers[i].AssertCurrentProgress(1f);
            }
        }

        [Test]
        public void ProgressMayBeChainSubscribedMultipleTimes_Pending_T(
            [Values] ProgressType progressType,
            // This test is unverifiable when progress is executed on background threads.
            [Values(SynchronizationType.Synchronous, SynchronizationType.Foreground, SynchronizationType.Explicit)] SynchronizationType synchronizationType)
        {
            var deferred = Promise.NewDeferred<int>();
            var promise = deferred.Promise;
            ProgressHelper[] progressHelpers = new ProgressHelper[MultiProgressCount];

            for (int i = 0; i < MultiProgressCount; ++i)
            {
                progressHelpers[i] = new ProgressHelper(progressType, synchronizationType);
                promise = progressHelpers[i].Subscribe(promise);
            }

            promise.Forget();
            for (int i = 0; i < MultiProgressCount; ++i)
            {
                progressHelpers[i].ReportProgressAndAssertResult(deferred, 0.1f, 0.1f);
            }
            deferred.Resolve(1);
            for (int i = 0; i < MultiProgressCount; ++i)
            {
                progressHelpers[i].AssertCurrentProgress(1f);
            }
        }

        [Test]
        public void ProgressMayBeChainSubscribedMultipleTimes_Resolved_T(
            [Values] ProgressType progressType,
            // This test is unverifiable when progress is executed on background threads.
            [Values(SynchronizationType.Synchronous, SynchronizationType.Foreground, SynchronizationType.Explicit)] SynchronizationType synchronizationType)
        {
            Promise<int> promise = Promise.Resolved(1);
            ProgressHelper[] progressHelpers = new ProgressHelper[MultiProgressCount];

            for (int i = 0; i < MultiProgressCount; ++i)
            {
                progressHelpers[i] = new ProgressHelper(progressType, synchronizationType);
                promise = progressHelpers[i].Subscribe(promise);
            }

            promise.Forget();
            for (int i = 0; i < MultiProgressCount; ++i)
            {
                progressHelpers[i].AssertCurrentProgress(1f);
            }
        }

        [Test]
        public void ProgressSubscribedToPreservedPromiseWillBeInvokedInOrder_Pending_void(
            // This test is unverifiable when progress is executed on background threads.
            [Values(SynchronizationOption.Synchronous, SynchronizationOption.Foreground)] SynchronizationOption synchronizationOption)
        {
            var deferred = Promise.NewDeferred();
            var promise = deferred.Promise.Preserve();
            int[] results = new int[MultiProgressCount];
            int index = 0;

            for (int i = 0; i < MultiProgressCount; ++i)
            {
                int num = i;
                promise.Progress(v => { results[index++] = num; }, synchronizationOption).Forget();
            }

            promise.Forget();
            index = 0;
            deferred.ReportProgress(0.1f);
            TestHelper.ExecuteForegroundCallbacks();
            CollectionAssert.AreEqual(System.Linq.Enumerable.Range(0, results.Length), results);
            index = 0;
            deferred.Resolve();
        }

        [Test]
        public void ProgressSubscribedToPreservedPromiseWillBeInvokedInOrder_Resolved_void(
            // This test is unverifiable when progress is executed on background threads.
            [Values(SynchronizationOption.Synchronous, SynchronizationOption.Foreground)] SynchronizationOption synchronizationOption)
        {
            Promise promise = Promise.Resolved().Preserve();
            int[] results = new int[MultiProgressCount];
            int index = 0;

            for (int i = 0; i < MultiProgressCount; ++i)
            {
                int num = i;
                promise.Progress(v => { results[index++] = num; }, synchronizationOption).Forget();
            }

            promise.Forget();
            TestHelper.ExecuteForegroundCallbacks();
            CollectionAssert.AreEqual(System.Linq.Enumerable.Range(0, results.Length), results);
        }

        [Test]
        public void ProgressSubscribedToPreservedPromiseWillBeInvokedInOrder_Pending_T(
            // This test is unverifiable when progress is executed on background threads.
            [Values(SynchronizationOption.Synchronous, SynchronizationOption.Foreground)] SynchronizationOption synchronizationOption)
        {
            var deferred = Promise.NewDeferred<int>();
            var promise = deferred.Promise.Preserve();
            int[] results = new int[MultiProgressCount];
            int index = 0;

            for (int i = 0; i < MultiProgressCount; ++i)
            {
                int num = i;
                promise.Progress(v => { results[index++] = num; }, synchronizationOption).Forget();
            }

            promise.Forget();
            index = 0;
            deferred.ReportProgress(0.1f);
            TestHelper.ExecuteForegroundCallbacks();
            CollectionAssert.AreEqual(System.Linq.Enumerable.Range(0, results.Length), results);
            index = 0;
            deferred.Resolve(1);
        }

        [Test]
        public void ProgressSubscribedToPreservedPromiseWillBeInvokedInOrder_Resolved_T(
            // This test is unverifiable when progress is executed on background threads.
            [Values(SynchronizationOption.Synchronous, SynchronizationOption.Foreground)] SynchronizationOption synchronizationOption)
        {
            Promise<int> promise = Promise.Resolved(1).Preserve();
            int[] results = new int[MultiProgressCount];
            int index = 0;

            for (int i = 0; i < MultiProgressCount; ++i)
            {
                int num = i;
                promise.Progress(v => { results[index++] = num; }, synchronizationOption).Forget();
            }

            promise.Forget();
            TestHelper.ExecuteForegroundCallbacks();
            CollectionAssert.AreEqual(System.Linq.Enumerable.Range(0, results.Length), results);
        }

        [Test]
        public void ProgressChainSubscribedWillBeInvokedInOrder_Pending_void(
            // This test is unverifiable when progress is executed on background threads.
            [Values(SynchronizationOption.Synchronous, SynchronizationOption.Foreground)] SynchronizationOption synchronizationOption)
        {
            var deferred = Promise.NewDeferred();
            var promise = deferred.Promise;
            int[] results = new int[MultiProgressCount];
            int index = 0;

            for (int i = 0; i < MultiProgressCount; ++i)
            {
                int num = i;
                promise = promise.Progress(v => { results[index++] = num; }, synchronizationOption);
            }

            promise.Forget();
            index = 0;
            deferred.ReportProgress(0.1f);
            TestHelper.ExecuteForegroundCallbacks();
            CollectionAssert.AreEqual(System.Linq.Enumerable.Range(0, results.Length), results);
            index = 0;
            deferred.Resolve();
        }

        [Test]
        public void ProgressChainSubscribedWillBeInvokedInOrder_Resolved_void(
            // This test is unverifiable when progress is executed on background threads.
            [Values(SynchronizationOption.Synchronous, SynchronizationOption.Foreground)] SynchronizationOption synchronizationOption)
        {
            Promise promise = Promise.Resolved();
            int[] results = new int[MultiProgressCount];
            int index = 0;

            for (int i = 0; i < MultiProgressCount; ++i)
            {
                int num = i;
                promise = promise.Progress(v => { results[index++] = num; }, synchronizationOption);
            }

            promise.Forget();
            TestHelper.ExecuteForegroundCallbacks();
            CollectionAssert.AreEqual(System.Linq.Enumerable.Range(0, results.Length), results);
        }

        [Test]
        public void ProgressChainSubscribedWillBeInvokedInOrder_Pending_T(
            // This test is unverifiable when progress is executed on background threads.
            [Values(SynchronizationOption.Synchronous, SynchronizationOption.Foreground)] SynchronizationOption synchronizationOption)
        {
            var deferred = Promise.NewDeferred<int>();
            var promise = deferred.Promise;
            int[] results = new int[MultiProgressCount];
            int index = 0;

            for (int i = 0; i < MultiProgressCount; ++i)
            {
                int num = i;
                promise = promise.Progress(v => { results[index++] = num; }, synchronizationOption);
            }

            promise.Forget();
            index = 0;
            deferred.ReportProgress(0.1f);
            TestHelper.ExecuteForegroundCallbacks();
            CollectionAssert.AreEqual(System.Linq.Enumerable.Range(0, results.Length), results);
            index = 0;
            deferred.Resolve(1);
        }

        [Test]
        public void ProgressChainSubscribedWillBeInvokedInOrder_Resolved_T(
            // This test is unverifiable when progress is executed on background threads.
            [Values(SynchronizationOption.Synchronous, SynchronizationOption.Foreground)] SynchronizationOption synchronizationOption)
        {
            Promise<int> promise = Promise.Resolved(1);
            int[] results = new int[MultiProgressCount];
            int index = 0;

            for (int i = 0; i < MultiProgressCount; ++i)
            {
                int num = i;
                promise = promise.Progress(v => { results[index++] = num; }, synchronizationOption);
            }

            promise.Forget();
            TestHelper.ExecuteForegroundCallbacks();
            CollectionAssert.AreEqual(System.Linq.Enumerable.Range(0, results.Length), results);
        }

#else // PROMISE_PROGRESS

#pragma warning disable CS0618 // Type or member is obsolete
        [Test]
        public void ProgressDisabled_OnProgressWillNotBeInvoked_void(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred = Promise.NewDeferred();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            progressHelper.Subscribe(deferred.Promise)
                .Forget();

            progressHelper.AssertCurrentProgress(float.NaN, false);
            progressHelper.ReportProgressAndAssertResult(deferred, 0.5f, float.NaN, false);
            progressHelper.ResolveAndAssertResult(deferred, float.NaN, false);
        }

        [Test]
        public void ProgressDisabled_OnProgressWillNotBeInvoked_T(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred = Promise<int>.NewDeferred();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            progressHelper.Subscribe(deferred.Promise)
                .Forget();

            progressHelper.AssertCurrentProgress(float.NaN, false);
            progressHelper.ReportProgressAndAssertResult(deferred, 0.5f, float.NaN, false);
            progressHelper.ResolveAndAssertResult(deferred, 1, float.NaN, false);
        }
#pragma warning restore CS0618 // Type or member is obsolete

#endif // PROMISE_PROGRESS
    }
}