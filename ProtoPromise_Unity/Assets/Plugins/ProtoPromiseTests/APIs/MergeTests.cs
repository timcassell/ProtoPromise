#if !PROTO_PROMISE_PROGRESS_DISABLE
#define PROMISE_PROGRESS
#else
#undef PROMISE_PROGRESS
#endif

using NUnit.Framework;
using Proto.Promises;

namespace ProtoPromiseTests.APIs
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

            Assert.IsTrue(canceled);
        }

#if PROMISE_PROGRESS
        [Test]
        public void MergeProgressIsNormalized0(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<string>();
            var deferred3 = Promise.NewDeferred<float>();
            var deferred4 = Promise.NewDeferred<bool>();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            progressHelper.Subscribe(
                Promise.Merge(deferred1.Promise, deferred2.Promise, deferred3.Promise, deferred4.Promise)
            )
                .Forget();

            progressHelper.AssertCurrentProgress(0f / 4f);

            progressHelper.ReportProgressAndAssertResult(deferred1, 0.5f, 0.5f / 4f);
            progressHelper.ResolveAndAssertResult(deferred1, 1, 1f / 4f);

            progressHelper.ReportProgressAndAssertResult(deferred2, 0.5f, 1.5f / 4f);
            progressHelper.ResolveAndAssertResult(deferred2, "Success", 2f / 4f);

            progressHelper.ReportProgressAndAssertResult(deferred3, 0.5f, 2.5f / 4f);
            progressHelper.ResolveAndAssertResult(deferred3, 2f, 3f / 4f);

            progressHelper.ReportProgressAndAssertResult(deferred4, 0.5f, 3.5f / 4f);
            progressHelper.ResolveAndAssertResult(deferred4, true, 4f / 4f);
        }

        [Test]
        public void MergeProgressIsNormalized1(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred3 = Promise.NewDeferred<float>();
            var deferred4 = Promise.NewDeferred<bool>();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            progressHelper.Subscribe(
                Promise.Merge(deferred1.Promise, Promise.Resolved("Success"), deferred3.Promise, deferred4.Promise)
            )
                .Forget();

            progressHelper.AssertCurrentProgress(1f / 4f);

            progressHelper.ReportProgressAndAssertResult(deferred1, 0.5f, 1.5f / 4f);
            progressHelper.ResolveAndAssertResult(deferred1, 1, 2f / 4f);

            progressHelper.ReportProgressAndAssertResult(deferred3, 0.5f, 2.5f / 4f);
            progressHelper.ResolveAndAssertResult(deferred3, 2f, 3f / 4f);

            progressHelper.ReportProgressAndAssertResult(deferred4, 0.5f, 3.5f / 4f);
            progressHelper.ResolveAndAssertResult(deferred4, true, 4f / 4f);
        }

        [Test]
        public void MergeProgressIsNormalized2(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred3 = Promise.NewDeferred<bool>();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            progressHelper.Subscribe(
                Promise.Merge(deferred1.Promise, Promise.Resolved(1f), deferred3.Promise)
            )
                .Forget();

            progressHelper.AssertCurrentProgress(1f / 3f);

            progressHelper.ReportProgressAndAssertResult(deferred1, 0.5f, 1.5f / 3f);
            progressHelper.ResolveAndAssertResult(deferred1, 1, 2f / 3f);

            progressHelper.ReportProgressAndAssertResult(deferred3, 0.5f, 2.5f / 3f);
            progressHelper.ResolveAndAssertResult(deferred3, true, 3f / 3f);
        }

        [Test]
        public void MergeProgressIsNormalized3(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<float>();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            progressHelper.Subscribe(
                Promise.Merge
                (
                    deferred1.Promise.ThenDuplicate(),
                    deferred2.Promise.ThenDuplicate()
                )
            )
                .Forget();

            progressHelper.AssertCurrentProgress(0f / 2f);

            progressHelper.ReportProgressAndAssertResult(deferred1, 0.5f, 0.5f / 2f);
            progressHelper.ResolveAndAssertResult(deferred1, 1, 1f / 2f);

            progressHelper.ReportProgressAndAssertResult(deferred2, 0.5f, 1.5f / 2f);
            progressHelper.ResolveAndAssertResult(deferred2, 1f, 2f / 2f);
        }

        [Test]
        public void MergeProgressIsNormalized4(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<float>();
            var deferred3 = Promise.NewDeferred<int>();
            var deferred4 = Promise.NewDeferred<float>();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            progressHelper.Subscribe(
                Promise.Merge
                (
                    deferred1.Promise
                        .Then(() => deferred3.Promise),
                    deferred2.Promise
                        .Then(() => deferred4.Promise)
                )
            )
                .Forget();

            progressHelper.AssertCurrentProgress(0f / 4f);

            progressHelper.ReportProgressAndAssertResult(deferred1, 0.5f, 0.5f / 4f);
            progressHelper.ResolveAndAssertResult(deferred1, 1, 1f / 4f);

            progressHelper.ReportProgressAndAssertResult(deferred3, 0.5f, 1.5f / 4f);
            progressHelper.ResolveAndAssertResult(deferred3, 1, 2f / 4f);

            progressHelper.ReportProgressAndAssertResult(deferred2, 0.5f, 2.5f / 4f);
            progressHelper.ResolveAndAssertResult(deferred2, 1f, 3f / 4f);

            progressHelper.ReportProgressAndAssertResult(deferred4, 0.5f, 3.5f / 4f);
            progressHelper.ResolveAndAssertResult(deferred4, 1f, 4f / 4f);
        }

        [Test]
        public void MergeProgressIsNormalized5(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<float>();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            progressHelper.Subscribe(
                Promise.Merge
                (
                    deferred1.Promise
                        .Then(x => Promise.Resolved(x)),
                    deferred2.Promise
                        .Then(x => Promise.Resolved(x))
                )
            )
                .Forget();

            progressHelper.AssertCurrentProgress(0f / 4f);

            progressHelper.ReportProgressAndAssertResult(deferred1, 0.5f, 0.5f / 4f);
            progressHelper.ResolveAndAssertResult(deferred1, 1, 2f / 4f);

            progressHelper.ReportProgressAndAssertResult(deferred2, 0.5f, 2.5f / 4f);
            progressHelper.ResolveAndAssertResult(deferred2, 1, 4f / 4f);
        }

        [Test]
        public void MergeProgressIsNoLongerReportedFromRejected(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<float>();
            var deferred3 = Promise.NewDeferred<bool>();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            progressHelper.Subscribe(
                Promise.Merge(deferred1.Promise, deferred2.Promise, deferred3.Promise)
            )
                .Catch(() => { })
                .Forget();

            progressHelper.AssertCurrentProgress(0f / 3f);

            progressHelper.ReportProgressAndAssertResult(deferred1, 0.5f, 0.5f / 3f);
            progressHelper.ResolveAndAssertResult(deferred1, 1, 1f / 3f);

            progressHelper.ReportProgressAndAssertResult(deferred2, 0.5f, 1.5f / 3f);
            progressHelper.RejectAndAssertResult(deferred2, "Reject", 1.5f / 3f, false);

            progressHelper.ReportProgressAndAssertResult(deferred3, 0.5f, 1.5f / 3f, false);
            progressHelper.ResolveAndAssertResult(deferred3, true, 1.5f / 3f, false);
        }

        [Test]
        public void MergeProgressIsNoLongerReportedFromCanceled(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred1 = Promise.NewDeferred<int>();
            var cancelationSource = CancelationSource.New();
            var deferred2 = Promise.NewDeferred<float>(cancelationSource.Token);
            var deferred3 = Promise.NewDeferred<bool>();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            progressHelper.Subscribe(
                Promise.Merge(deferred1.Promise, deferred2.Promise, deferred3.Promise)
            )
                .Forget();

            progressHelper.AssertCurrentProgress(0f / 3f);

            progressHelper.ReportProgressAndAssertResult(deferred1, 0.5f, 0.5f / 3f);
            progressHelper.ResolveAndAssertResult(deferred1, 1, 1f / 3f);

            progressHelper.ReportProgressAndAssertResult(deferred2, 0.5f, 1.5f / 3f);
            progressHelper.CancelAndAssertResult(deferred2, 1.5f / 3f, false);

            progressHelper.ReportProgressAndAssertResult(deferred3, 0.5f, 1.5f / 3f, false);
            progressHelper.ResolveAndAssertResult(deferred3, true, 1.5f / 3f, false);

            cancelationSource.Dispose();
        }

        [Test]
        public void MergeProgressWillBeInvokedProperlyFromARecoveredPromise(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<float>();
            var deferred3 = Promise.NewDeferred<string>();
            var deferred4 = Promise.NewDeferred<bool>();
            var cancelationSource = CancelationSource.New();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            progressHelper.Subscribe(
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
            )
                .Forget();

            progressHelper.AssertCurrentProgress(0f / 6f);

            progressHelper.ReportProgressAndAssertResult(deferred1, 0.5f, 0.5f / 6f);
            progressHelper.ResolveAndAssertResult(deferred1, 1, 3f / 6f);

            progressHelper.ReportProgressAndAssertResult(deferred2, 0.25f, 3.25f / 6f);
            progressHelper.CancelAndAssertResult(cancelationSource, 5f / 6f);

            progressHelper.ReportProgressAndAssertResult(deferred2, 0.5f, 5f / 6f, false);
            progressHelper.ReportProgressAndAssertResult(deferred3, 0.5f, 5f / 6f, false);
            progressHelper.ResolveAndAssertResult(deferred3, "Success", 5f / 6f, false);
            progressHelper.ReportProgressAndAssertResult(deferred2, 0.7f, 5f / 6f, false);

            progressHelper.ReportProgressAndAssertResult(deferred4, 0.5f, 5.5f / 6f);
            progressHelper.ResolveAndAssertResult(deferred4, true, 6f / 6f);

            progressHelper.ReportProgressAndAssertResult(deferred2, 0.5f, 6f / 6f, false);
            progressHelper.ResolveAndAssertResult(deferred2, 1f, 6f / 6f, false);

            cancelationSource.Dispose();
            deferred3.Promise.Forget(); // Need to forget this promise because it was never awaited due to the cancelation.
        }
#endif
    }
}