#if !PROTO_PROMISE_PROGRESS_DISABLE
#define PROMISE_PROGRESS
#else
#undef PROMISE_PROGRESS
#endif

using NUnit.Framework;

namespace Proto.Promises.Tests
{
    public class MergeTests
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
        public void MergePromiseIsResolvedWhenAllPromisesAreResolved()
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<string>();

            string success = "Success";
            bool resolved = false;

            Promise.Merge(deferred1.Promise, deferred2.Promise)
                .Then(values =>
                {
                    resolved = true;

                    Assert.AreEqual(1, values.Item1);
                    Assert.AreEqual(success, values.Item2);
                })
                .Forget();

            deferred1.Resolve(1);
            deferred2.Resolve(success);

            Promise.Manager.HandleCompletes();
            Assert.IsTrue(resolved);
        }

        [Test]
        public void MergePromiseIsResolvedWhenAllPromisesAreAlreadyResolved()
        {
            string success = "Success";

            bool resolved = false;

            Promise.Merge(Promise.Resolved(1), Promise.Resolved(success))
                .Then(values =>
                {
                    resolved = true;

                    Assert.AreEqual(1, values.Item1);
                    Assert.AreEqual(success, values.Item2);
                })
                .Forget();

            Promise.Manager.HandleCompletes();
            Assert.IsTrue(resolved);
        }

        [Test]
        public void MergePromiseIsRejectedWhenFirstPromiseIsRejected()
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<string>();

            string expected = "Error";
            bool rejected = false;

            Promise.Merge(deferred1.Promise, deferred2.Promise)
                .Catch((string e) =>
                {
                    Assert.AreEqual(expected, e);
                    rejected = true;
                })
                .Forget();

            deferred1.Reject(expected);
            deferred2.Resolve("Success");

            Promise.Manager.HandleCompletes();
            Assert.IsTrue(rejected);
        }

        [Test]
        public void MergePromiseIsRejectedWhenSecondPromiseIsRejected()
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<string>();

            string expected = "Error";
            bool rejected = false;

            Promise.Merge(deferred1.Promise, deferred2.Promise)
                .Catch((string e) =>
                {
                    Assert.AreEqual(expected, e);
                    rejected = true;
                })
                .Forget();

            deferred1.Resolve(2);
            deferred2.Reject(expected);

            Promise.Manager.HandleCompletes();
            Assert.IsTrue(rejected);
        }

        [Test]
        public void MergePromiseIsRejectedWhenBothPromisesAreRejected()
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<string>();

            var promise1 = deferred1.Promise.Preserve();
            var promise2 = deferred2.Promise.Preserve();

            promise1.Catch((string _) => { }).Forget();
            promise2.Catch((string _) => { }).Forget();

            string expected = "Error";
            bool rejected = false;

            Promise.Merge(promise1, promise2)
                .Catch((string e) =>
                {
                    Assert.AreEqual(expected, e);
                    rejected = true;
                })
                .Forget();

            deferred1.Reject(expected);
            deferred2.Reject(expected);

            Promise.Manager.HandleCompletes();
            Assert.IsTrue(rejected);

            promise1.Forget();
            promise2.Forget();
        }

        [Test]
        public void MergePromiseIsRejectedWhenAnyPromiseIsAlreadyRejected()
        {
            bool rejected = false;
            string expected = "Error";

            var deferred = Promise.NewDeferred<int>();

            Promise.Merge(deferred.Promise, Promise<int>.Rejected(expected))
                .Catch((string e) =>
                {
                    Assert.AreEqual(expected, e);
                    rejected = true;
                })
                .Forget();

            deferred.Resolve(0);

            Promise.Manager.HandleCompletes();
            Assert.IsTrue(rejected);
        }

        [Test]
        public void MergePromiseIsCanceledWhenFirstPromiseIsCanceled0()
        {
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred1 = Promise.NewDeferred<int>(cancelationSource.Token);
            var deferred2 = Promise.NewDeferred<string>();

            string expected = "Cancel";
            bool canceled = false;

            Promise.Merge(deferred1.Promise, deferred2.Promise)
                .CatchCancelation(e =>
                {
                    Assert.AreEqual(expected, e.Value);
                    canceled = true;
                })
                .Forget();

            cancelationSource.Cancel(expected);
            deferred2.Resolve("Success");

            Promise.Manager.HandleCompletes();
            Assert.IsTrue(canceled);

            cancelationSource.Dispose();
        }

        [Test]
        public void MergePromiseIsCanceledWhenFirstPromiseIsCanceled1()
        {
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred1 = Promise.NewDeferred<int>(cancelationSource.Token);
            var deferred2 = Promise.NewDeferred<string>();

            bool canceled = false;

            Promise.Merge(deferred1.Promise, deferred2.Promise)
                .CatchCancelation(e =>
                {
                    Assert.IsNull(e.ValueType);
                    canceled = true;
                })
                .Forget();

            cancelationSource.Cancel();
            deferred2.Resolve("Success");

            Promise.Manager.HandleCompletes();
            Assert.IsTrue(canceled);

            cancelationSource.Dispose();
        }

        [Test]
        public void MergePromiseIsCanceledWhenSecondPromiseIsCanceled0()
        {
            var deferred1 = Promise.NewDeferred<int>();
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred2 = Promise.NewDeferred<string>(cancelationSource.Token);

            string expected = "Cancel";
            bool canceled = false;

            Promise.Merge(deferred1.Promise, deferred2.Promise)
                .CatchCancelation(e =>
                {
                    Assert.AreEqual(expected, e.Value);
                    canceled = true;
                })
                .Forget();

            deferred1.Resolve(2);
            cancelationSource.Cancel(expected);

            Promise.Manager.HandleCompletes();
            Assert.IsTrue(canceled);

            cancelationSource.Dispose();
        }

        [Test]
        public void MergePromiseIsCanceledWhenSecondPromiseIsCanceled1()
        {
            var deferred1 = Promise.NewDeferred<int>();
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred2 = Promise.NewDeferred<string>(cancelationSource.Token);

            bool canceled = false;

            Promise.Merge(deferred1.Promise, deferred2.Promise)
                .CatchCancelation(e =>
                {
                    Assert.IsNull(e.ValueType);
                    canceled = true;
                })
                .Forget();

            deferred1.Resolve(2);
            cancelationSource.Cancel();

            Promise.Manager.HandleCompletes();
            Assert.IsTrue(canceled);

            cancelationSource.Dispose();
        }

        [Test]
        public void MergePromiseIsCanceledWhenBothPromisesAreCanceled0()
        {
            CancelationSource cancelationSource1 = CancelationSource.New();
            var deferred1 = Promise.NewDeferred<int>(cancelationSource1.Token);
            CancelationSource cancelationSource2 = CancelationSource.New();
            var deferred2 = Promise.NewDeferred<string>(cancelationSource2.Token);

            string expected = "Cancel";
            bool canceled = false;

            Promise.Merge(deferred1.Promise, deferred2.Promise)
                .CatchCancelation(e =>
                {
                    Assert.AreEqual(expected, e.Value);
                    canceled = true;
                })
                .Forget();

            cancelationSource1.Cancel(expected);
            cancelationSource2.Cancel("Different Cancel");

            Promise.Manager.HandleCompletes();
            Assert.IsTrue(canceled);

            cancelationSource1.Dispose();
            cancelationSource2.Dispose();
        }

        [Test]
        public void MergePromiseIsCanceledWhenBothPromisesAreCanceled1()
        {
            CancelationSource cancelationSource1 = CancelationSource.New();
            var deferred1 = Promise.NewDeferred<int>(cancelationSource1.Token);
            CancelationSource cancelationSource2 = CancelationSource.New();
            var deferred2 = Promise.NewDeferred<string>(cancelationSource2.Token);

            bool canceled = false;

            Promise.Merge(deferred1.Promise, deferred2.Promise)
                .CatchCancelation(e =>
                {
                    Assert.IsNull(e.ValueType);
                    canceled = true;
                })
                .Forget();

            cancelationSource1.Cancel();
            cancelationSource2.Cancel("Different Cancel");

            Promise.Manager.HandleCompletes();
            Assert.IsTrue(canceled);

            cancelationSource1.Dispose();
            cancelationSource2.Dispose();
        }

        [Test]
        public void MergePromiseIsCanceledWhenAnyPromiseIsAlreadyCanceled()
        {
            bool canceled = false;
            string expected = "Cancel";

            var deferred = Promise.NewDeferred<int>();

            Promise.Merge(deferred.Promise, Promise<int>.Canceled(expected))
                .CatchCancelation(reason =>
                {
                    Assert.AreEqual(expected, reason.Value);
                    canceled = true;
                })
                .Forget();

            deferred.Resolve(0);

            Promise.Manager.HandleCompletes();
            Assert.IsTrue(canceled);
        }

#if PROMISE_PROGRESS
        [Test]
        public void MergeProgressIsNormalized0()
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<string>();
            var deferred3 = Promise.NewDeferred<float>();
            var deferred4 = Promise.NewDeferred<bool>();

            float progress = float.NaN;

            Promise.Merge(deferred1.Promise, deferred2.Promise, deferred3.Promise, deferred4.Promise)
                .Progress(p => progress = p)
                .Forget();

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0f / 8f, progress, 0f);

            deferred1.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f / 8f, progress, TestHelper.progressEpsilon);

            deferred1.Resolve(1);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(2f / 8f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(3f / 8f, progress, TestHelper.progressEpsilon);

            deferred2.Resolve("Success");
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(4f / 8f, progress, TestHelper.progressEpsilon);

            deferred3.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(5f / 8f, progress, TestHelper.progressEpsilon);

            deferred3.Resolve(2f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(6f / 8f, progress, TestHelper.progressEpsilon);

            deferred4.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(7f / 8f, progress, TestHelper.progressEpsilon);

            deferred4.Resolve(true);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(8f / 8f, progress, TestHelper.progressEpsilon);
        }

        [Test]
        public void MergeProgressIsNormalized1()
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred3 = Promise.NewDeferred<float>();
            var deferred4 = Promise.NewDeferred<bool>();

            float progress = float.NaN;

            Promise.Merge(deferred1.Promise, Promise.Resolved("Success"), deferred3.Promise, deferred4.Promise)
                .Progress(p => progress = p)
                .Forget();

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(2f / 8f, progress, TestHelper.progressEpsilon);

            deferred1.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(3f / 8f, progress, TestHelper.progressEpsilon);

            deferred1.Resolve(1);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(4f / 8f, progress, TestHelper.progressEpsilon);

            deferred3.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(5f / 8f, progress, TestHelper.progressEpsilon);

            deferred3.Resolve(2f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(6f / 8f, progress, TestHelper.progressEpsilon);

            deferred4.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(7f / 8f, progress, TestHelper.progressEpsilon);

            deferred4.Resolve(true);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(8f / 8f, progress, TestHelper.progressEpsilon);
        }

        [Test]
        public void MergeProgressIsNormalized2()
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred3 = Promise.NewDeferred<bool>();

            float progress = float.NaN;

            Promise.Merge(deferred1.Promise, Promise.Resolved(1f), deferred3.Promise)
                .Progress(p => progress = p)
                .Forget();

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f / 3f, progress, TestHelper.progressEpsilon);

            deferred1.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1.5f / 3f, progress, TestHelper.progressEpsilon);

            deferred1.Resolve(1);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(2f / 3f, progress, TestHelper.progressEpsilon);

            deferred3.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(2.5f / 3f, progress, TestHelper.progressEpsilon);

            deferred3.Resolve(true);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(3f / 3f, progress, TestHelper.progressEpsilon);
        }

        [Test]
        public void MergeProgressIsNormalized3()
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<float>();

            float progress = float.NaN;

            Promise.Merge
            (
                deferred1.Promise
                    .Then(() => 1),
                deferred2.Promise
                    .Then(() => 1f)
            )
                .Progress(p => progress = p)
                .Forget();

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0f / 2f, progress, 0f);

            deferred1.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f / 2f, progress, TestHelper.progressEpsilon);

            deferred1.Resolve(1);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f / 2f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1.5f / 2f, progress, TestHelper.progressEpsilon);

            deferred2.Resolve(1);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(2f / 2f, progress, TestHelper.progressEpsilon);
        }

        [Test]
        public void MergeProgressIsNormalized4()
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<float>();
            var deferred3 = Promise.NewDeferred<int>();
            var deferred4 = Promise.NewDeferred<float>();

            float progress = float.NaN;

            Promise.Merge
            (
                deferred1.Promise
                    .Then(() => deferred3.Promise),
                deferred2.Promise
                    .Then(() => deferred4.Promise)
            )
                .Progress(p => progress = p)
                .Forget();

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0f / 4f, progress, 0f);

            deferred1.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f / 4f, progress, TestHelper.progressEpsilon);

            deferred1.Resolve(1);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f / 4f, progress, TestHelper.progressEpsilon);

            deferred3.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1.5f / 4f, progress, TestHelper.progressEpsilon);

            deferred3.Resolve(1);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(2f / 4f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(2.5f / 4f, progress, TestHelper.progressEpsilon);

            deferred2.Resolve(1);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(3f / 4f, progress, TestHelper.progressEpsilon);

            deferred4.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(3.5f / 4f, progress, TestHelper.progressEpsilon);

            deferred4.Resolve(1);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(4f / 4f, progress, TestHelper.progressEpsilon);
        }

        [Test]
        public void MergeProgressIsNormalized5()
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<float>();

            float progress = float.NaN;

            Promise.Merge
            (
                deferred1.Promise
                    .Then(x => Promise.Resolved(x)),
                deferred2.Promise
                    .Then(x => Promise.Resolved(x))
            )
                .Progress(p => progress = p)
                .Forget();

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0f / 4f, progress, 0f);

            deferred1.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f / 4f, progress, TestHelper.progressEpsilon);

            deferred1.Resolve(1);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(2f / 4f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(2.5f / 4f, progress, TestHelper.progressEpsilon);

            deferred2.Resolve(1);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(4f / 4f, progress, TestHelper.progressEpsilon);
        }

        [Test]
        public void MergeProgressIsNoLongerReportedFromRejected()
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<float>();
            var deferred3 = Promise.NewDeferred<bool>();

            float progress = float.NaN;

            Promise.Merge(deferred1.Promise, deferred2.Promise, deferred3.Promise)
                .Progress(p => progress = p)
                .Catch(() => { })
                .Forget();

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0f / 3f, progress, TestHelper.progressEpsilon);

            deferred1.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f / 3f, progress, TestHelper.progressEpsilon);

            deferred1.Resolve(1);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f / 3f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1.5f / 3f, progress, TestHelper.progressEpsilon);

            deferred2.Reject("Reject");
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1.5f / 3f, progress, TestHelper.progressEpsilon);

            deferred3.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1.5f / 3f, progress, TestHelper.progressEpsilon);

            deferred3.Resolve(true);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1.5f / 3f, progress, TestHelper.progressEpsilon);
        }

        [Test]
        public void MergeProgressIsNoLongerReportedFromCanceled()
        {
            var deferred1 = Promise.NewDeferred<int>();
            var cancelationSource = CancelationSource.New();
            var deferred2 = Promise.NewDeferred<float>(cancelationSource.Token);
            var deferred3 = Promise.NewDeferred<bool>();

            float progress = float.NaN;

            Promise.Merge(deferred1.Promise, deferred2.Promise, deferred3.Promise)
                .Progress(p => progress = p)
                .Forget();

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0f / 3f, progress, TestHelper.progressEpsilon);

            deferred1.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f / 3f, progress, TestHelper.progressEpsilon);

            deferred1.Resolve(1);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f / 3f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1.5f / 3f, progress, TestHelper.progressEpsilon);

            cancelationSource.Cancel();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1.5f / 3f, progress, TestHelper.progressEpsilon);

            deferred3.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1.5f / 3f, progress, TestHelper.progressEpsilon);

            deferred3.Resolve(true);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1.5f / 3f, progress, TestHelper.progressEpsilon);

            cancelationSource.Dispose();
        }

        [Test]
        public void MergeProgressWillBeInvokedProperlyFromARecoveredPromise()
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<float>();
            var deferred3 = Promise.NewDeferred<string>();
            var deferred4 = Promise.NewDeferred<bool>();
            var cancelationSource = CancelationSource.New();

            float progress = float.NaN;

            Promise.Merge
            (
                // Make first and second promise chains the same length
                deferred1.Promise
                    .Then(x => Promise.Resolved(x))
                    .Then(x => Promise.Resolved(x)),
                deferred2.Promise
                    .Then(() => deferred3.Promise, cancelationSource.Token)
                    .ContinueWith(_ => deferred4.Promise)
            )
                .Progress(p => progress = p)
                .Forget();

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0f / 6f, progress, TestHelper.progressEpsilon);

            deferred1.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f / 6f, progress, TestHelper.progressEpsilon);

            deferred1.Resolve(1);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(3f / 6f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.25f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(3.25f / 6f, progress, TestHelper.progressEpsilon);

            cancelationSource.Cancel();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(5f / 6f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(5f / 6f, progress, TestHelper.progressEpsilon);

            deferred3.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(5f / 6f, progress, TestHelper.progressEpsilon);

            deferred3.Resolve("Success");
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(5f / 6f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.7f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(5f / 6f, progress, TestHelper.progressEpsilon);

            deferred4.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(5.5f / 6f, progress, TestHelper.progressEpsilon);

            deferred4.Resolve(true);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.8f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);

            deferred2.Resolve(1f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);

            cancelationSource.Dispose();
            deferred3.Promise.Forget(); // Need to forget this promise because it was never awaited due to the cancelation.
        }
#endif
    }
}