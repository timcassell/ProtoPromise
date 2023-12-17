#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using NUnit.Framework;
using Proto.Promises;
using ProtoPromiseTests.Concurrency;
using System;
using System.Linq;
using System.Threading;

namespace ProtoPromiseTests.APIs.Utilities
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

        [Test]
        public void EmptyProgressTokenDoesNotHaveListener()
        {
            Assert.False(ProgressToken.None.HasListener);
        }

        [Test]
        public void ProgressTokenFromProgressHasListener()
        {
            var progress = Progress.New(v => { });
            Assert.True(progress.Token.HasListener);

            progress.DisposeAsync()
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void ProgressTokenFromDisposedProgressDoesNotHaveListener()
        {
            var progress = Progress.New(v => { });

            progress.DisposeAsync()
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));

            Assert.False(progress.Token.HasListener);
        }

        [Test]
        public void EmptyProgressTokenReportDoesNothing()
        {
            ProgressToken.None.Report(0d);
            ProgressToken.None.Report(0.5d);
            ProgressToken.None.Report(1d);
        }

        [Test]
        public void ProgressTokenFromDisposedProgressReportDoesNothing()
        {
            var progress = Progress.New(v => { });

            progress.DisposeAsync()
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));

            ProgressToken.None.Report(0d);
            ProgressToken.None.Report(0.5d);
            ProgressToken.None.Report(1d);
        }

        [Test]
        public void ProgressMayBeReported(
            [Values] ProgressType progressType)
        {
            ProgressHelper progressHelper = new ProgressHelper(progressType, SynchronizationType.Foreground, forceAsync: true);
            var progress = progressHelper.ToProgress();
            var progressToken = progress.Token;

            progressHelper.AssertCurrentProgress(double.NaN, false, false);

            progressToken.Report(0d);
            progressHelper.AssertCurrentProgress(0d);

            progressToken.Report(0.25f);
            progressHelper.AssertCurrentProgress(0f, false, false);
            progressToken.Report(0.5f);
            progressHelper.AssertCurrentProgress(0f, false, false);
            progressToken.Report(0.75f);
            progressHelper.AssertCurrentProgress(0.75f, true, true);

            progress.DisposeAsync()
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void ProgressDisposeAsyncWaitsForInvoke(
            [Values] ProgressType progressType)
        {
            ProgressHelper progressHelper = new ProgressHelper(progressType, SynchronizationType.Foreground, forceAsync: true);
            var progress = progressHelper.ToProgress();
            var progressToken = progress.Token;

            progressHelper.AssertCurrentProgress(double.NaN, false, false);

            progressHelper.ReportProgressAndAssertResult(progressToken, 0d, 0d);

            progressHelper.ReportProgressAndAssertResult(progressToken, 1d, 0d, false, false);

            bool completed = false;
            progress.DisposeAsync()
                .Finally(() => completed = true)
                .Forget();

            Assert.False(completed);

            progressHelper.AssertCurrentProgress(1f, true, true);
            Assert.True(completed);
        }

        [Test]
        public void ProgressIsReportedOnContext(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationCallbackType,
            [Values(SynchronizationType.Foreground
#if !UNITY_WEBGL
                , SynchronizationType.Background
#endif
            )] SynchronizationType synchronizationReportType)
        {
            Thread foregroundThread = Thread.CurrentThread;
            ThreadHelper threadHelper = new ThreadHelper();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationCallbackType, v => TestHelper.AssertCallbackContext(synchronizationCallbackType, synchronizationReportType, foregroundThread));
            var progress = progressHelper.ToProgress();
            var progressToken = progress.Token;

            progressHelper.AssertCurrentProgress(double.NaN, false, false);

            progressHelper.MaybeEnterLock();
            progressHelper.PrepareForInvoke();
            threadHelper.ExecuteSynchronousOrOnThread(() =>
            {
                progressToken.Report(0d);
            }, synchronizationReportType == SynchronizationType.Foreground);
            progressHelper.AssertCurrentProgress(0f);

            progressHelper.PrepareForInvoke();
            threadHelper.ExecuteSynchronousOrOnThread(() => progressToken.Report(0.5f),
                synchronizationReportType == SynchronizationType.Foreground);
            progressHelper.AssertCurrentProgress(0.5f);

            progressHelper.PrepareForInvoke();
            threadHelper.ExecuteSynchronousOrOnThread(() => progressToken.Report(1f),
                synchronizationReportType == SynchronizationType.Foreground);
            progressHelper.AssertCurrentProgress(1f);
            progressHelper.MaybeExitLock();

            progress.DisposeAsync()
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void ProgressIsNoLongerInvokedWhenCancelationTokenIsCanceled(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            CancelationSource cancelationSource = CancelationSource.New();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            var progress = progressHelper.ToProgress(cancelationSource.Token);
            var progressToken = progress.Token;

            progressHelper.AssertCurrentProgress(double.NaN, false, false);

            progressHelper.ReportProgressAndAssertResult(progressToken, 0d, 0d);
            progressHelper.ReportProgressAndAssertResult(progressToken, 0.25f, 0.25f);

            progressHelper.CancelAndAssertResult(cancelationSource, 0.25f, false);

            progressHelper.ReportProgressAndAssertResult(progressToken, 0.5f, 0.25f, false);
            progressHelper.AssertCurrentProgress(0.25f, false);

            cancelationSource.Dispose();

            progress.DisposeAsync()
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void OnProgressWillOnlyBeInvokedWithAValueBetween0And1()
        {
            var progress = Progress.New(p =>
            {
                Assert.GreaterOrEqual(p, 0f);
                Assert.LessOrEqual(p, 1f);
            }, SynchronizationOption.Synchronous);
            var progressToken = progress.Token;

            Assert.Throws<Proto.Promises.ArgumentOutOfRangeException>(() => progressToken.Report(double.NaN));
            Assert.Throws<Proto.Promises.ArgumentOutOfRangeException>(() => progressToken.Report(double.NegativeInfinity));
            Assert.Throws<Proto.Promises.ArgumentOutOfRangeException>(() => progressToken.Report(double.PositiveInfinity));
            Assert.Throws<Proto.Promises.ArgumentOutOfRangeException>(() => progressToken.Report(double.MinValue));
            Assert.Throws<Proto.Promises.ArgumentOutOfRangeException>(() => progressToken.Report(double.MaxValue));
            Assert.Throws<Proto.Promises.ArgumentOutOfRangeException>(() => progressToken.Report(-0.1d));
            Assert.Throws<Proto.Promises.ArgumentOutOfRangeException>(() => progressToken.Report(1.1d));

            progress.DisposeAsync()
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void ProgressTokenCanOnlyBeChunkedBetween0And1()
        {
            var progress = Progress.New(p => { }, SynchronizationOption.Synchronous);
            var progressToken = progress.Token;

            Assert.Throws<Proto.Promises.ArgumentOutOfRangeException>(() => progressToken.Chunk(double.NaN, 0d));
            Assert.Throws<Proto.Promises.ArgumentOutOfRangeException>(() => progressToken.Chunk(0d, double.NaN));
            Assert.Throws<Proto.Promises.ArgumentOutOfRangeException>(() => progressToken.Chunk(double.NegativeInfinity, 0d));
            Assert.Throws<Proto.Promises.ArgumentOutOfRangeException>(() => progressToken.Chunk(0d, double.NegativeInfinity));
            Assert.Throws<Proto.Promises.ArgumentOutOfRangeException>(() => progressToken.Chunk(double.PositiveInfinity, 0d));
            Assert.Throws<Proto.Promises.ArgumentOutOfRangeException>(() => progressToken.Chunk(0d, double.PositiveInfinity));
            Assert.Throws<Proto.Promises.ArgumentOutOfRangeException>(() => progressToken.Chunk(double.MinValue, 0d));
            Assert.Throws<Proto.Promises.ArgumentOutOfRangeException>(() => progressToken.Chunk(0d, double.MinValue));
            Assert.Throws<Proto.Promises.ArgumentOutOfRangeException>(() => progressToken.Chunk(double.MaxValue, 0d));
            Assert.Throws<Proto.Promises.ArgumentOutOfRangeException>(() => progressToken.Chunk(0d, double.MaxValue));
            Assert.Throws<Proto.Promises.ArgumentOutOfRangeException>(() => progressToken.Chunk(-0.1d, 0d));
            Assert.Throws<Proto.Promises.ArgumentOutOfRangeException>(() => progressToken.Chunk(0d, -0.1d));
            Assert.Throws<Proto.Promises.ArgumentOutOfRangeException>(() => progressToken.Chunk(1.1d, 0d));
            Assert.Throws<Proto.Promises.ArgumentOutOfRangeException>(() => progressToken.Chunk(0d, 1.1d));

            progressToken.Chunk(0d, 0d);
            progressToken.Chunk(0d, 1d);
            progressToken.Chunk(1d, 0d);
            progressToken.Chunk(0.5d, 1d);
            progressToken.Chunk(1d, 0.5d);
            progressToken.Chunk(1d, 1d);

            progress.DisposeAsync()
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void ProgressIsInvokedCorrectlyFromChunkedTokens(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            var progress = progressHelper.ToProgress();
            var progressToken = progress.Token;

            progressHelper.AssertCurrentProgress(double.NaN, false, false);

            var chunk1 = progressToken.Chunk(0d, 0.5d);
            progressHelper.ReportProgressAndAssertResult(chunk1, 0d, 0f / 2f);
            progressHelper.ReportProgressAndAssertResult(chunk1, 0.5f, 0.5f / 2f);
            progressHelper.ReportProgressAndAssertResult(chunk1, 1f, 1f / 2f);

            var chunk2 = progressToken.Chunk(0.5d, 1d);
            // chunk1.Report(1) and chunk2.Report(0) report the same value
            // implementation detail - the progress listener does not get invoked with same value reported twice in a row, so we don't wait for the invoke.
            progressHelper.ReportProgressAndAssertResult(chunk2, 0d, 1f / 2f, false, false);
            progressHelper.ReportProgressAndAssertResult(chunk2, 0.5f, 1.5f / 2f);
            progressHelper.ReportProgressAndAssertResult(chunk2, 1f, 2f / 2f);

            progress.DisposeAsync()
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void ProgressIsInvokedCorrectlyFromChunkedTokens_Recursive(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            var progress = progressHelper.ToProgress();
            var progressToken = progress.Token;

            progressHelper.AssertCurrentProgress(double.NaN, false, false);

            var progressHalf1 = progressToken.Chunk(0d, 0.5d);
            var progressQuarter1 = progressHalf1.Chunk(0d, 0.5d);
            progressHelper.ReportProgressAndAssertResult(progressQuarter1, 0d, 0f / 4f);
            progressHelper.ReportProgressAndAssertResult(progressQuarter1, 0.5f, 0.5f / 4f);
            progressHelper.ReportProgressAndAssertResult(progressQuarter1, 1f, 1f / 4f);

            var progressQuarter2 = progressHalf1.Chunk(0.5d, 1d);
            // progressQuarter1.Report(1) and progressQuarter2.Report(0) report the same value
            progressHelper.ReportProgressAndAssertResult(progressQuarter2, 0d, 1f / 4f, false, false);
            progressHelper.ReportProgressAndAssertResult(progressQuarter2, 0.5f, 1.5f / 4f);
            progressHelper.ReportProgressAndAssertResult(progressQuarter2, 1f, 2f / 4f);

            var progressHalf2 = progressToken.Chunk(0.5d, 1d);
            var progressQuarter3 = progressHalf2.Chunk(0d, 0.5d);
            // progressQuarter2.Report(1) and progressQuarter3.Report(0) report the same value
            progressHelper.ReportProgressAndAssertResult(progressQuarter3, 0d, 2f / 4f, false, false);
            progressHelper.ReportProgressAndAssertResult(progressQuarter3, 0.5f, 2.5f / 4f);
            progressHelper.ReportProgressAndAssertResult(progressQuarter3, 1f, 3f / 4f);

            var progressQuarter4 = progressHalf2.Chunk(0.5d, 1d);
            // progressQuarter3.Report(1) and progressQuarter4.Report(0) report the same value
            progressHelper.ReportProgressAndAssertResult(progressQuarter4, 0d, 3f / 4f, false, false);
            progressHelper.ReportProgressAndAssertResult(progressQuarter4, 0.5f, 3.5f / 4f);
            progressHelper.ReportProgressAndAssertResult(progressQuarter4, 1f, 4f / 4f);

            progress.DisposeAsync()
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void ProgressMultiHandler(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            const int MultiProgressCount = 2;

            var multiProgress = Progress.NewMultiHandler();
            ProgressHelper[] progressHelpers = new ProgressHelper[MultiProgressCount];
            Progress[] progresses = new Progress[MultiProgressCount];

            TimeSpan timeout = TimeSpan.FromSeconds(MultiProgressCount);

            for (int i = 0; i < MultiProgressCount; ++i)
            {
                progressHelpers[i] = new ProgressHelper(progressType, synchronizationType);
                progresses[i] = progressHelpers[i].ToProgress();
                multiProgress.Add(progresses[i].Token);
                progressHelpers[i].AssertCurrentProgress(double.NaN, false, false);
                progressHelpers[i].MaybeEnterLock();
                progressHelpers[i].PrepareForInvoke();
            }

            var progressToken = multiProgress.Token;

            progressToken.Report(0.1d);
            for (int i = 0; i < MultiProgressCount; ++i)
            {
                progressHelpers[i].AssertCurrentProgress(0.1d, timeout: timeout);
                progressHelpers[i].PrepareForInvoke();
            }

            progressToken.Report(1d);
            for (int i = 0; i < MultiProgressCount; ++i)
            {
                progressHelpers[i].AssertCurrentProgress(1f, timeout: timeout);
                progressHelpers[i].MaybeExitLock();

                progresses[i].DisposeAsync()
                    .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
            }

            multiProgress.Dispose();
        }

        [Test]
        public void ProgressMergeBuilderProgressIsMerged(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            var progress = progressHelper.ToProgress();
            var progressToken = progress.Token;

            progressHelper.AssertCurrentProgress(double.NaN, false, false);

            var progressMerger = Progress.NewMergeBuilder(progressToken);

            var progressToken1 = progressMerger.NewToken();
            var progressToken2 = progressMerger.NewToken();

            progressHelper.AssertCurrentProgress(double.NaN, false, false);

            progressHelper.ReportProgressAndAssertResult(progressToken1, 0.5f, 0.5f / 2f);
            progressHelper.ReportProgressAndAssertResult(progressToken2, 0.5f, 1f / 2f);
            progressHelper.ReportProgressAndAssertResult(progressToken1, 0f, 0.5f / 2f);
            progressHelper.ReportProgressAndAssertResult(progressToken2, 0.75f, 0.75f / 2f);
            progressHelper.ReportProgressAndAssertResult(progressToken2, 0f, 0f / 2f);
            progressHelper.ReportProgressAndAssertResult(progressToken1, 1f, 1f / 2f);
            progressHelper.ReportProgressAndAssertResult(progressToken2, 0.5f, 1.5f / 2f);
            progressHelper.ReportProgressAndAssertResult(progressToken2, 0f, 1f / 2f);
            progressHelper.ReportProgressAndAssertResult(progressToken2, 1f, 2f / 2f);

            var progressToken3 = progressMerger.NewToken();
            progressHelper.AssertCurrentProgress(2f / 2f, false, false);
            progressHelper.ReportProgressAndAssertResult(progressToken3, 0f, 2f / 3f);
            progressHelper.ReportProgressAndAssertResult(progressToken3, 0.5f, 2.5f / 3f);
            progressHelper.ReportProgressAndAssertResult(progressToken3, 1f, 3f / 3f);

            progressMerger.Dispose();
            progress.DisposeAsync()
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void ProgressMergeBuilderProgressIsMergedWithWeight(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            var progress = progressHelper.ToProgress();
            var progressToken = progress.Token;

            progressHelper.AssertCurrentProgress(double.NaN, false, false);

            var progressMerger = Progress.NewMergeBuilder(progressToken);

            var progressToken1 = progressMerger.NewToken();
            var progressToken2 = progressMerger.NewToken(2);

            progressHelper.AssertCurrentProgress(double.NaN, false, false);

            progressHelper.ReportProgressAndAssertResult(progressToken1, 0.5f, 0.5f / 3f);
            progressHelper.ReportProgressAndAssertResult(progressToken2, 0.5f, 1.5f / 3f);
            progressHelper.ReportProgressAndAssertResult(progressToken1, 0f, 1f / 3f);
            progressHelper.ReportProgressAndAssertResult(progressToken2, 0.75f, 1.5f / 3f);
            progressHelper.ReportProgressAndAssertResult(progressToken2, 0f, 0f / 3f);
            progressHelper.ReportProgressAndAssertResult(progressToken1, 1f, 1f / 3f);
            progressHelper.ReportProgressAndAssertResult(progressToken2, 0.5f, 2f / 3f);
            progressHelper.ReportProgressAndAssertResult(progressToken2, 0f, 1f / 3f);
            progressHelper.ReportProgressAndAssertResult(progressToken2, 1f, 3f / 3f);

            progressMerger.Dispose();
            progress.DisposeAsync()
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void ProgressRaceBuilderProgressIsReportedMaximum(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            var progress = progressHelper.ToProgress();
            var progressToken = progress.Token;

            progressHelper.AssertCurrentProgress(double.NaN, false, false);

            var progressRacer = Progress.NewRaceBuilder(progressToken);

            var progressToken1 = progressRacer.NewToken();
            var progressToken2 = progressRacer.NewToken();

            progressHelper.AssertCurrentProgress(double.NaN, false, false);

            progressHelper.ReportProgressAndAssertResult(progressToken1, 0.5f, 0.5f);
            progressHelper.ReportProgressAndAssertResult(progressToken2, 0.3f, 0.5f, false);
            progressHelper.ReportProgressAndAssertResult(progressToken2, 0.7f, 0.7f);
            progressHelper.ReportProgressAndAssertResult(progressToken1, 0.6f, 0.7f, false);
            progressHelper.ReportProgressAndAssertResult(progressToken2, 0.8f, 0.8f);

            progressHelper.ReportProgressAndAssertResult(progressToken1, 1f, 1f);
            progressHelper.ReportProgressAndAssertResult(progressToken2, 1f, 1f, false, false);

            progressRacer.Dispose();
            progress.DisposeAsync()
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }
    }
}