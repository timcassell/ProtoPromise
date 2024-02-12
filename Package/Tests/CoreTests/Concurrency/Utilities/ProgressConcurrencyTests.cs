#if !UNITY_WEBGL

#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using NUnit.Framework;
using Proto.Promises;
using System;
using System.Threading;

namespace ProtoPromiseTests.Concurrency.Utilities
{
    public class ProgressConcurrencyTests
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
        public void ProgressMayBeReportedConcurrently(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var progressHelper = default(ProgressHelper);
            var progress = default(Progress);
            var progressToken = default(ProgressToken);

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(true,
                // Setup
                () =>
                {
                    progressHelper = new ProgressHelper(progressType, synchronizationType);
                    progress = progressHelper.ToProgress();
                    progressToken = progress.Token;

                    progressHelper.MaybeEnterLock();
                    progressHelper.PrepareForInvoke();
                },
                // Teardown
                () =>
                {
                    // Each progress is reported concurrently, so we can't know which stuck.
                    // Just check to make sure any of them stuck, so it should be >= min and <= max.
                    var progress1 = progressHelper.GetCurrentProgress(true, true);
                    progressHelper.MaybeExitLock();
                    Assert.Greater(progress1, 0.2f - TestHelper.progressEpsilon);
                    Assert.LessOrEqual(progress1, 0.4f);

                    progress.DisposeAsync()
                        .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
                },
                // Parallel Actions
                () => progressToken.Report(0.2d),
                () => progressToken.Report(0.3d),
                () => progressToken.Report(0.4d)
            );
        }

        [Test]
        public void ProgressMayBeReportedAndDisposedConcurrently(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var progressHelper = default(ProgressHelper);
            var progress = default(Progress);
            var progressToken = default(ProgressToken);
            var disposePromise = default(Promise);

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(true,
                // Setup
                () =>
                {
                    progressHelper = new ProgressHelper(progressType, synchronizationType);
                    progress = progressHelper.ToProgress();
                    progressToken = progress.Token;

                    progressHelper.MaybeEnterLock();
                    progressHelper.PrepareForInvoke();
                    // Progress is disposed concurrently, so we report a value in setup for the teardown to verify.
                    // Otherwise, it would be impossible to determine whether a report happened first or dispose.
                    progressToken.Report(0.2d);
                },
                // Teardown
                () =>
                {
                    // Each progress is reported concurrently, so we can't know which stuck.
                    // Just check to make sure any of them stuck, so it should be >= min and <= max.
                    var progress1 = progressHelper.GetCurrentProgress(true, true);
                    progressHelper.MaybeExitLock();
                    Assert.Greater(progress1, 0.2f - TestHelper.progressEpsilon);
                    Assert.LessOrEqual(progress1, 0.4f);

                    disposePromise.WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
                },
                // Parallel Actions
                () => progressToken.Report(0.2d),
                () => progressToken.Report(0.3d),
                () => progressToken.Report(0.4d),
                () => disposePromise = progress.DisposeAsync()
            );
        }

        [Test]
        public void ProgressMayBeReportedAndDisposedAndCanceledConcurrently(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var progressHelper = default(ProgressHelper);
            var progress = default(Progress);
            var progressToken = default(ProgressToken);
            var cancelationSource = default(CancelationSource);
            var cancelationToken = default(CancelationToken);
            var disposePromise = default(Promise);

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(true,
                // Setup
                () =>
                {
                    cancelationSource = CancelationSource.New();
                    cancelationToken = cancelationSource.Token;
                    progressHelper = new ProgressHelper(progressType, synchronizationType);
                    progress = progressHelper.ToProgress(cancelationToken);
                    progressToken = progress.Token;

                    progressHelper.MaybeEnterLock();
                    progressHelper.PrepareForInvoke();
                    // Progress is disposed and canceled concurrently, so we report a value in setup for the teardown to verify.
                    // Otherwise, it would be impossible to determine whether a report happened first or dispose.
                    progressToken.Report(0.2d);
                    progressHelper.MaybeWaitForInvoke(true, true);
                },
                // Teardown
                () =>
                {
                    // Each progress is reported concurrently, so we can't know which stuck.
                    // Just check to make sure any of them stuck, so it should be >= min and <= max.
                    var progress1 = progressHelper.GetCurrentProgress(false, true);
                    progressHelper.MaybeExitLock();
                    Assert.Greater(progress1, 0.2f - TestHelper.progressEpsilon);
                    Assert.LessOrEqual(progress1, 0.4f);

                    disposePromise.WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
                    cancelationSource.Dispose();
                },
                // Parallel Actions
                () => progressToken.Report(0.2d),
                () => progressToken.Report(0.3d),
                () => progressToken.Report(0.4d),
                () => disposePromise = progress.DisposeAsync(),
                () => cancelationSource.Cancel()
            );
        }

        [Test]
        public void ProgressMayBeReportedConcurrentlyFromAny(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var progressHelper = default(ProgressHelper);
            var progress = default(Progress);
            var progressMerger = default(Progress.MergeBuilder);
            var progressMergeToken1 = default(ProgressToken);
            var progressMergeToken2 = default(ProgressToken);
            var progressRacer = default(Progress.RaceBuilder);
            var progressRaceToken1 = default(ProgressToken);
            var progressRaceToken2 = default(ProgressToken);
            var progressMultiHandler = default(Progress.MultiHandler);

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(true,
                // Setup
                () =>
                {
                    progressHelper = new ProgressHelper(progressType, synchronizationType);
                    progress = progressHelper.ToProgress();
                    var progressToken = progress.Token;
                    progressMerger = Progress.NewMergeBuilder(progressToken.Slice(0d, 0.3d));
                    progressRacer = Progress.NewRaceBuilder(progressToken.Slice(0.3d, 0.6d));
                    progressMergeToken1 = progressMerger.NewToken();
                    progressMergeToken2 = progressMerger.NewToken(2d);
                    progressRaceToken1 = progressRacer.NewToken();
                    progressRaceToken2 = progressRacer.NewToken();
                    progressMultiHandler = Progress.NewMultiHandler();
                    progressMultiHandler.Add(progressToken.Slice(0.6d, 1d));
                },
                // Teardown
                () =>
                {
                    // We don't bother to check the current progress, this just makes sure nothing explodes or deadlocks.
                    progressMultiHandler.Dispose();
                    progressRacer.Dispose();
                    progressMerger.Dispose();
                    progress.DisposeAsync()
                        .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
                },
                // Parallel Actions
                () => progressMergeToken1.Report(0.2d),
                () => progressMergeToken2.Report(0.3d),
                () => progressRaceToken1.Report(0.4d),
                () => progressRaceToken2.Report(0.5d),
                () => progressMultiHandler.Token.Report(0.5d)
            );
        }
    }
}

#endif // !UNITY_WEBGL