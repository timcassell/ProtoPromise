#if !PROTO_PROMISE_PROGRESS_DISABLE
#define PROMISE_PROGRESS
#else
#undef PROMISE_PROGRESS
#endif

using NUnit.Framework;
using Proto.Promises;

namespace ProtoPromiseTests.APIs
{
    public class FirstTests
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
        public void FirstIsResolvedWhenFirstPromiseIsResolvedFirst_void()
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();

            bool resolved = false;

            Promise.First(deferred1.Promise, deferred2.Promise)
                .Then(() =>
                {
                    resolved = true;
                })
                .Forget();

            deferred1.Resolve();

            Assert.IsTrue(resolved);

            deferred2.Resolve();

            Assert.IsTrue(resolved);
        }

        [Test]
        public void FirstIsResolvedWhenFirstPromiseIsResolvedFirst_T()
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();

            bool resolved = false;

            Promise.First(deferred1.Promise, deferred2.Promise)
                .Then(i =>
                {
                    Assert.AreEqual(5, i);
                    resolved = true;
                })
                .Forget();

            deferred1.Resolve(5);

            Assert.IsTrue(resolved);

            deferred2.Resolve(1);

            Assert.IsTrue(resolved);
        }

        [Test]
        public void FirstIsResolvedWhenSecondPromiseIsResolvedFirst_void()
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();

            bool resolved = false;

            Promise.First(deferred1.Promise, deferred2.Promise)
                .Then(() =>
                {
                    resolved = true;
                })
                .Forget();

            deferred2.Resolve();

            Assert.IsTrue(resolved);

            deferred1.Resolve();

            Assert.IsTrue(resolved);
        }

        [Test]
        public void FirstIsResolvedWhenSecondPromiseIsResolvedFirst_T()
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();

            bool resolved = false;

            Promise.First(deferred1.Promise, deferred2.Promise)
                .Then(i =>
                {
                    Assert.AreEqual(5, i);
                    resolved = true;
                })
                .Forget();

            deferred2.Resolve(5);

            Assert.IsTrue(resolved);

            deferred1.Resolve(1);

            Assert.IsTrue(resolved);
        }

        [Test]
        public void FirstIsResolvedWhenFirstPromiseIsRejectedThenSecondPromiseIsResolved_void()
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();

            bool resolved = false;

            Promise.First(deferred1.Promise, deferred2.Promise)
                .Then(() =>
                {
                    resolved = true;
                })
                .Forget();

            deferred1.Reject("Error");

            Assert.IsFalse(resolved);

            deferred2.Resolve();

            Assert.IsTrue(resolved);
        }

        [Test]
        public void FirstIsResolvedWhenFirstPromiseIsRejectedThenSecondPromiseIsResolved_T()
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();

            bool resolved = false;

            Promise.First(deferred1.Promise, deferred2.Promise)
                .Then(i =>
                {
                    Assert.AreEqual(5, i);
                    resolved = true;
                })
                .Forget();

            deferred1.Reject("Error");

            Assert.IsFalse(resolved);

            deferred2.Resolve(5);

            Assert.IsTrue(resolved);
        }

        [Test]
        public void FirstIsResolvedWhenSecondPromiseIsRejectedThenFirstPromiseIsResolved_void()
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();

            bool resolved = false;

            Promise.First(deferred1.Promise, deferred2.Promise)
                .Then(() =>
                {
                    resolved = true;
                })
                .Forget();

            deferred2.Reject("Error");

            Assert.IsFalse(resolved);

            deferred1.Resolve();

            Assert.IsTrue(resolved);
        }

        [Test]
        public void FirstIsResolvedWhenSecondPromiseIsRejectedThenFirstPromiseIsResolved_T()
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();

            bool resolved = false;

            Promise.First(deferred1.Promise, deferred2.Promise)
                .Then(i =>
                {
                    Assert.AreEqual(5, i);
                    resolved = true;
                })
                .Forget();

            deferred2.Reject("Error");

            Assert.IsFalse(resolved);

            deferred1.Resolve(5);

            Assert.IsTrue(resolved);
        }

        [Test]
        public void FirstIsRejectedWhenAllPromisesAreRejected_void()
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();

            bool rejected = false;
            string expected = "Error";

            Promise.First(deferred1.Promise, deferred2.Promise)
                .Catch((string rej) =>
                {
                    Assert.AreEqual(expected, rej);
                    rejected = true;
                })
                .Forget();

            deferred1.Reject("Different Error");

            Assert.IsFalse(rejected);

            deferred2.Reject(expected);

            Assert.IsTrue(rejected);
        }

        [Test]
        public void FirstIsRejectedWhenAllPromisesAreRejected_T()
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();

            bool rejected = false;
            string expected = "Error";

            Promise.First(deferred1.Promise, deferred2.Promise)
                .Catch((string rej) =>
                {
                    Assert.AreEqual(expected, rej);
                    rejected = true;
                })
                .Forget();

            deferred1.Reject("Different Error");

            Assert.IsFalse(rejected);

            deferred2.Reject(expected);

            Assert.IsTrue(rejected);
        }

        [Test]
        public void FirstIsResolvedWhenFirstPromiseIsCanceledThenSecondPromiseIsResolved_void()
        {
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred1 = Promise.NewDeferred(cancelationSource.Token);
            var deferred2 = Promise.NewDeferred();

            bool resolved = false;

            Promise.First(deferred1.Promise, deferred2.Promise)
                .Then(() =>
                {
                    resolved = true;
                })
                .Forget();

            cancelationSource.Cancel("Cancel");

            Assert.IsFalse(resolved);

            deferred2.Resolve();

            Assert.IsTrue(resolved);

            cancelationSource.Dispose();
        }

        [Test]
        public void FirstIsResolvedWhenFirstPromiseIsCanceledThenSecondPromiseIsResolved_T()
        {
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred1 = Promise.NewDeferred<int>(cancelationSource.Token);
            var deferred2 = Promise.NewDeferred<int>();

            bool resolved = false;

            Promise.First(deferred1.Promise, deferred2.Promise)
                .Then(i =>
                {
                    Assert.AreEqual(5, i);
                    resolved = true;
                })
                .Forget();

            cancelationSource.Cancel("Cancel");

            Assert.IsFalse(resolved);

            deferred2.Resolve(5);

            Assert.IsTrue(resolved);

            cancelationSource.Dispose();
        }

        [Test]
        public void FirstIsResolvedWhenSecondPromiseIsCanceledThenFirstPromiseIsResolved_void()
        {
            var deferred1 = Promise.NewDeferred();
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred2 = Promise.NewDeferred(cancelationSource.Token);

            bool resolved = false;

            Promise.First(deferred1.Promise, deferred2.Promise)
                .Then(() =>
                {
                    resolved = true;
                })
                .Forget();

            cancelationSource.Cancel("Cancel");

            Assert.IsFalse(resolved);

            deferred1.Resolve();

            Assert.IsTrue(resolved);

            cancelationSource.Dispose();
        }

        [Test]
        public void FirstIsResolvedWhenSecondPromiseIsCanceledThenFirstPromiseIsResolved_T()
        {
            var deferred1 = Promise.NewDeferred<int>();
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred2 = Promise.NewDeferred<int>(cancelationSource.Token);

            bool resolved = false;

            Promise.First(deferred1.Promise, deferred2.Promise)
                .Then(i =>
                {
                    Assert.AreEqual(5, i);
                    resolved = true;
                })
                .Forget();

            cancelationSource.Cancel("Cancel");

            Assert.IsFalse(resolved);

            deferred1.Resolve(5);

            Assert.IsTrue(resolved);

            cancelationSource.Dispose();
        }

        [Test]
        public void FirstIsCanceledWhenAllPromisesAreCanceled_void0()
        {
            CancelationSource cancelationSource1 = CancelationSource.New();
            var deferred1 = Promise.NewDeferred(cancelationSource1.Token);
            CancelationSource cancelationSource2 = CancelationSource.New();
            var deferred2 = Promise.NewDeferred(cancelationSource2.Token);

            bool canceled = false;
            string expected = "Cancel";

            Promise.First(deferred1.Promise, deferred2.Promise)
                .CatchCancelation(reason =>
                {
                    Assert.AreEqual(expected, reason.Value);
                    canceled = true;
                })
                .Forget();

            cancelationSource1.Cancel("Different Cancel");

            Assert.IsFalse(canceled);

            cancelationSource2.Cancel(expected);

            Assert.IsTrue(canceled);

            cancelationSource1.Dispose();
            cancelationSource2.Dispose();
        }

        [Test]
        public void FirstIsCanceledWhenAllPromisesAreCanceled_T0()
        {
            CancelationSource cancelationSource1 = CancelationSource.New();
            var deferred1 = Promise.NewDeferred<int>(cancelationSource1.Token);
            CancelationSource cancelationSource2 = CancelationSource.New();
            var deferred2 = Promise.NewDeferred<int>(cancelationSource2.Token);

            bool canceled = false;
            string expected = "Cancel";

            Promise.First(deferred1.Promise, deferred2.Promise)
                .CatchCancelation(reason =>
                {
                    Assert.AreEqual(expected, reason.Value);
                    canceled = true;
                })
                .Forget();

            cancelationSource1.Cancel("Different Cancel");

            Assert.IsFalse(canceled);

            cancelationSource2.Cancel(expected);

            Assert.IsTrue(canceled);

            cancelationSource1.Dispose();
            cancelationSource2.Dispose();
        }

        [Test]
        public void FirstIsCanceledWhenAllPromisesAreCanceled_void1()
        {
            CancelationSource cancelationSource1 = CancelationSource.New();
            var deferred1 = Promise.NewDeferred(cancelationSource1.Token);
            CancelationSource cancelationSource2 = CancelationSource.New();
            var deferred2 = Promise.NewDeferred(cancelationSource2.Token);

            bool canceled = false;

            Promise.First(deferred1.Promise, deferred2.Promise)
                .CatchCancelation(reason =>
                {
                    Assert.IsNull(reason.ValueType);
                    canceled = true;
                })
                .Forget();

            cancelationSource1.Cancel("Different Cancel");

            Assert.IsFalse(canceled);

            cancelationSource2.Cancel();

            Assert.IsTrue(canceled);

            cancelationSource1.Dispose();
            cancelationSource2.Dispose();
        }

        [Test]
        public void FirstIsCanceledWhenAllPromisesAreCanceled_T1()
        {
            CancelationSource cancelationSource1 = CancelationSource.New();
            var deferred1 = Promise.NewDeferred<int>(cancelationSource1.Token);
            CancelationSource cancelationSource2 = CancelationSource.New();
            var deferred2 = Promise.NewDeferred<int>(cancelationSource2.Token);

            bool canceled = false;

            Promise.First(deferred1.Promise, deferred2.Promise)
                .CatchCancelation(reason =>
                {
                    Assert.IsNull(reason.ValueType);
                    canceled = true;
                })
                .Forget();

            cancelationSource1.Cancel("Different Cancel");

            Assert.IsFalse(canceled);

            cancelationSource2.Cancel();

            Assert.IsTrue(canceled);

            cancelationSource1.Dispose();
            cancelationSource2.Dispose();
        }

        [Test]
        public void FirstIsRejectededWhenFirstPromiseIsCanceledThenSecondPromiseIsRejected_void()
        {
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred1 = Promise.NewDeferred(cancelationSource.Token);
            var deferred2 = Promise.NewDeferred();

            bool rejected = false;
            string expected = "Error";

            Promise.First(deferred1.Promise, deferred2.Promise)
                .Catch((string rej) =>
                {
                    Assert.AreEqual(expected, rej);
                    rejected = true;
                })
                .Forget();

            cancelationSource.Cancel("Cancel");

            Assert.IsFalse(rejected);

            deferred2.Reject(expected);

            Assert.IsTrue(rejected);

            cancelationSource.Dispose();
        }

        [Test]
        public void FirstIsRejectededWhenFirstPromiseIsCanceledThenSecondPromiseIsRejected_T()
        {
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred1 = Promise.NewDeferred<int>(cancelationSource.Token);
            var deferred2 = Promise.NewDeferred<int>();

            bool rejected = false;
            string expected = "Error";

            Promise.First(deferred1.Promise, deferred2.Promise)
                .Catch((string rej) =>
                {
                    Assert.AreEqual(expected, rej);
                    rejected = true;
                })
                .Forget();

            cancelationSource.Cancel("Cancel");

            Assert.IsFalse(rejected);

            deferred2.Reject(expected);

            Assert.IsTrue(rejected);

            cancelationSource.Dispose();
        }

        [Test]
        public void FirstIsRejectededWhenSecondPromiseIsCanceledThenFirstPromiseIsRejected_void()
        {
            var deferred1 = Promise.NewDeferred();
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred2 = Promise.NewDeferred(cancelationSource.Token);

            bool rejected = false;
            string expected = "Error";

            Promise.First(deferred1.Promise, deferred2.Promise)
                .Catch((string rej) =>
                {
                    Assert.AreEqual(expected, rej);
                    rejected = true;
                })
                .Forget();

            cancelationSource.Cancel("Cancel");

            Assert.IsFalse(rejected);

            deferred1.Reject(expected);

            Assert.IsTrue(rejected);

            cancelationSource.Dispose();
        }

        [Test]
        public void FirstIsRejectededWhenSecondPromiseIsCanceledThenFirstPromiseIsRejected_T()
        {
            var deferred1 = Promise.NewDeferred<int>();
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred2 = Promise.NewDeferred<int>(cancelationSource.Token);

            bool rejected = false;
            string expected = "Error";

            Promise.First(deferred1.Promise, deferred2.Promise)
                .Catch((string rej) =>
                {
                    Assert.AreEqual(expected, rej);
                    rejected = true;
                })
                .Forget();

            cancelationSource.Cancel("Cancel");

            Assert.IsFalse(rejected);

            deferred1.Reject(expected);

            Assert.IsTrue(rejected);

            cancelationSource.Dispose();
        }

        [Test]
        public void FirstIsCancelededWhenFirstPromiseIsRejectedThenSecondPromiseIsCanceled_void0()
        {
            var deferred1 = Promise.NewDeferred();
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred2 = Promise.NewDeferred(cancelationSource.Token);

            bool canceled = false;
            string expected = "Cancel";

            Promise.First(deferred1.Promise, deferred2.Promise)
                .CatchCancelation(reason =>
                {
                    Assert.AreEqual(expected, reason.Value);
                    canceled = true;
                })
                .Forget();

            deferred1.Reject("Error");

            Assert.IsFalse(canceled);

            cancelationSource.Cancel(expected);

            Assert.IsTrue(canceled);

            cancelationSource.Dispose();
        }

        [Test]
        public void FirstIsCancelededWhenFirstPromiseIsRejectedThenSecondPromiseIsCanceled_T0()
        {
            var deferred1 = Promise.NewDeferred<int>();
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred2 = Promise.NewDeferred<int>(cancelationSource.Token);

            bool canceled = false;
            string expected = "Cancel";

            Promise.First(deferred1.Promise, deferred2.Promise)
                .CatchCancelation(reason =>
                {
                    Assert.AreEqual(expected, reason.Value);
                    canceled = true;
                })
                .Forget();

            deferred1.Reject("Error");

            Assert.IsFalse(canceled);

            cancelationSource.Cancel(expected);

            Assert.IsTrue(canceled);

            cancelationSource.Dispose();
        }

        [Test]
        public void FirstIsCancelededWhenFirstPromiseIsRejectedThenSecondPromiseIsCanceled_void1()
        {
            var deferred1 = Promise.NewDeferred();
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred2 = Promise.NewDeferred(cancelationSource.Token);

            bool canceled = false;

            Promise.First(deferred1.Promise, deferred2.Promise)
                .CatchCancelation(reason =>
                {
                    Assert.IsNull(reason.ValueType);
                    canceled = true;
                })
                .Forget();

            deferred1.Reject("Error");

            Assert.IsFalse(canceled);

            cancelationSource.Cancel();

            Assert.IsTrue(canceled);

            cancelationSource.Dispose();
        }

        [Test]
        public void FirstIsCancelededWhenFirstPromiseIsRejectedThenSecondPromiseIsCanceled_T1()
        {
            var deferred1 = Promise.NewDeferred<int>();
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred2 = Promise.NewDeferred<int>(cancelationSource.Token);

            bool canceled = false;

            Promise.First(deferred1.Promise, deferred2.Promise)
                .CatchCancelation(reason =>
                {
                    Assert.IsNull(reason.ValueType);
                    canceled = true;
                })
                .Forget();

            deferred1.Reject("Error");

            Assert.IsFalse(canceled);

            cancelationSource.Cancel();

            Assert.IsTrue(canceled);

            cancelationSource.Dispose();
        }

        [Test]
        public void FirstIsCancelededWhenSecondPromiseIsRejectedThenFirstPromiseIsCanceled_void0()
        {
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred1 = Promise.NewDeferred(cancelationSource.Token);
            var deferred2 = Promise.NewDeferred();

            bool canceled = false;
            string expected = "Cancel";

            Promise.First(deferred1.Promise, deferred2.Promise)
                .CatchCancelation(reason =>
                {
                    Assert.AreEqual(expected, reason.Value);
                    canceled = true;
                })
                .Forget();

            deferred2.Reject("Error");

            Assert.IsFalse(canceled);

            cancelationSource.Cancel(expected);

            Assert.IsTrue(canceled);

            cancelationSource.Dispose();
        }

        [Test]
        public void FirstIsCancelededWhenSecondPromiseIsRejectedThenFirstPromiseIsCanceled_T0()
        {
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred1 = Promise.NewDeferred<int>(cancelationSource.Token);
            var deferred2 = Promise.NewDeferred<int>();

            bool canceled = false;
            string expected = "Cancel";

            Promise.First(deferred1.Promise, deferred2.Promise)
                .CatchCancelation(reason =>
                {
                    Assert.AreEqual(expected, reason.Value);
                    canceled = true;
                })
                .Forget();

            deferred2.Reject("Error");

            Assert.IsFalse(canceled);

            cancelationSource.Cancel(expected);

            Assert.IsTrue(canceled);

            cancelationSource.Dispose();
        }

        [Test]
        public void FirstIsCancelededWhenSecondPromiseIsRejectedThenFirstPromiseIsCanceled_void1()
        {
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred1 = Promise.NewDeferred(cancelationSource.Token);
            var deferred2 = Promise.NewDeferred();

            bool canceled = false;

            Promise.First(deferred1.Promise, deferred2.Promise)
                .CatchCancelation(reason =>
                {
                    Assert.IsNull(reason.ValueType);
                    canceled = true;
                })
                .Forget();

            deferred2.Reject("Error");

            Assert.IsFalse(canceled);

            cancelationSource.Cancel();

            Assert.IsTrue(canceled);

            cancelationSource.Dispose();
        }

        [Test]
        public void FirstIsCancelededWhenSecondPromiseIsRejectedThenFirstPromiseIsCanceled_T1()
        {
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred1 = Promise.NewDeferred<int>(cancelationSource.Token);
            var deferred2 = Promise.NewDeferred<int>();

            bool canceled = false;

            Promise.First(deferred1.Promise, deferred2.Promise)
                .CatchCancelation(reason =>
                {
                    Assert.IsNull(reason.ValueType);
                    canceled = true;
                })
                .Forget();

            deferred2.Reject("Error");

            Assert.IsFalse(canceled);

            cancelationSource.Cancel();

            Assert.IsTrue(canceled);

            cancelationSource.Dispose();
        }

#if PROMISE_PROGRESS
        [Test]
        public void FirstProgressReportsTheMaximumProgress_void0(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            progressHelper.Subscribe(
                Promise.First(deferred1.Promise, deferred2.Promise)
            )
                .Forget();

            progressHelper.AssertCurrentProgress(0f);

            progressHelper.ReportProgressAndAssertResult(deferred1, 0.5f, 0.5f);
            progressHelper.ReportProgressAndAssertResult(deferred2, 0.3f, 0.5f, false);
            progressHelper.ReportProgressAndAssertResult(deferred2, 0.7f, 0.7f);
            progressHelper.ReportProgressAndAssertResult(deferred1, 0.6f, 0.7f, false);
            progressHelper.ReportProgressAndAssertResult(deferred2, 0.8f, 0.8f);

            progressHelper.ResolveAndAssertResult(deferred1, 1f);
            progressHelper.ReportProgressAndAssertResult(deferred2, 0.9f, 1f, false);
            progressHelper.ResolveAndAssertResult(deferred2, 1f, false);
        }

        [Test]
        public void FirstProgressReportsTheMaximumProgress_T0(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            progressHelper.Subscribe(
                Promise.First(deferred1.Promise, deferred2.Promise)
            )
                .Forget();

            progressHelper.AssertCurrentProgress(0f);

            progressHelper.ReportProgressAndAssertResult(deferred1, 0.5f, 0.5f);
            progressHelper.ReportProgressAndAssertResult(deferred2, 0.3f, 0.5f, false);
            progressHelper.ReportProgressAndAssertResult(deferred2, 0.7f, 0.7f);
            progressHelper.ReportProgressAndAssertResult(deferred1, 0.6f, 0.7f, false);
            progressHelper.ReportProgressAndAssertResult(deferred2, 0.8f, 0.8f);

            progressHelper.ResolveAndAssertResult(deferred1, 1, 1f);
            progressHelper.ReportProgressAndAssertResult(deferred2, 0.9f, 1f, false);
            progressHelper.ResolveAndAssertResult(deferred2, 1, 1f, false);
        }

        [Test]
        public void FirstProgressReportsTheMaximumProgress_void1(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred1 = Promise.NewDeferred();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            progressHelper.Subscribe(
                Promise.First(deferred1.Promise, Promise.Resolved())
            )
                .Forget();

            progressHelper.AssertCurrentProgress(1f);
            progressHelper.ResolveAndAssertResult(deferred1, 1f, false);
        }

        [Test]
        public void FirstProgressReportsTheMaximumProgress_T1(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred1 = Promise.NewDeferred<int>();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            progressHelper.Subscribe(
                Promise.First(deferred1.Promise, Promise.Resolved(1))
            )
                .Forget();

            progressHelper.AssertCurrentProgress(1f);
            progressHelper.ResolveAndAssertResult(deferred1, 1, 1f, false);
        }

        [Test]
        public void FirstProgressReportsTheMaximumProgress_void2(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            progressHelper.Subscribe(
                Promise.First(
                    deferred1.Promise.ThenDuplicate(),
                    deferred2.Promise.ThenDuplicate()
                )
            )
                .Forget();

            progressHelper.AssertCurrentProgress(0f);

            progressHelper.ReportProgressAndAssertResult(deferred1, 0.5f, 0.5f);
            progressHelper.ReportProgressAndAssertResult(deferred2, 0.3f, 0.5f, false);
            progressHelper.ReportProgressAndAssertResult(deferred2, 0.7f, 0.7f);
            progressHelper.ReportProgressAndAssertResult(deferred1, 0.6f, 0.7f, false);
            progressHelper.ReportProgressAndAssertResult(deferred2, 0.8f, 0.8f);

            progressHelper.ResolveAndAssertResult(deferred1, 1f);
            progressHelper.ReportProgressAndAssertResult(deferred2, 0.9f, 1f, false);
            progressHelper.ResolveAndAssertResult(deferred2, 1f, false);
        }

        [Test]
        public void FirstProgressReportsTheMaximumProgress_T2(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            progressHelper.Subscribe(
                Promise.First(
                    deferred1.Promise.ThenDuplicate(),
                    deferred2.Promise.ThenDuplicate()
                )
            )
                .Forget();

            progressHelper.AssertCurrentProgress(0f);

            progressHelper.ReportProgressAndAssertResult(deferred1, 0.5f, 0.5f);
            progressHelper.ReportProgressAndAssertResult(deferred2, 0.3f, 0.5f, false);
            progressHelper.ReportProgressAndAssertResult(deferred2, 0.7f, 0.7f);
            progressHelper.ReportProgressAndAssertResult(deferred1, 0.6f, 0.7f, false);
            progressHelper.ReportProgressAndAssertResult(deferred2, 0.8f, 0.8f);

            progressHelper.ResolveAndAssertResult(deferred1, 1, 1f);
            progressHelper.ReportProgressAndAssertResult(deferred2, 0.9f, 1f, false);
            progressHelper.ResolveAndAssertResult(deferred2, 1, 1f, false);
        }

        [Test]
        public void FirstProgressReportsTheMaximumProgress_void3(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();
            var deferred3 = Promise.NewDeferred();
            var deferred4 = Promise.NewDeferred();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            progressHelper.Subscribe(
                Promise.First(
                    deferred1.Promise
                        .Then(() => deferred3.Promise),
                    deferred2.Promise
                        .Then(() => deferred4.Promise)
                )
            )
                .Forget();

            progressHelper.AssertCurrentProgress(0f);

            progressHelper.ReportProgressAndAssertResult(deferred1, 0.5f, 0.5f / 2f);
            progressHelper.ReportProgressAndAssertResult(deferred2, 0.3f, 0.5f / 2f, false);
            progressHelper.ReportProgressAndAssertResult(deferred2, 0.7f, 0.7f / 2f);
            progressHelper.ReportProgressAndAssertResult(deferred1, 0.6f, 0.7f / 2f, false);
            progressHelper.ReportProgressAndAssertResult(deferred2, 0.8f, 0.8f / 2f);

            progressHelper.ResolveAndAssertResult(deferred1, 1f / 2f);

            progressHelper.ReportProgressAndAssertResult(deferred2, 0.9f, 1f / 2f, false);
            progressHelper.ReportProgressAndAssertResult(deferred3, 0.5f, 1.5f / 2f);

            progressHelper.ResolveAndAssertResult(deferred2, 1.5f / 2f, false);

            progressHelper.ReportProgressAndAssertResult(deferred4, 0.3f, 1.5f / 2f, false);
            progressHelper.ReportProgressAndAssertResult(deferred4, 0.7f, 1.7f / 2f);

            progressHelper.ResolveAndAssertResult(deferred3, 2f / 2f);

            progressHelper.ReportProgressAndAssertResult(deferred4, 0.9f, 2f / 2f, false);

            progressHelper.ResolveAndAssertResult(deferred4, 2f / 2f, false);
        }

        [Test]
        public void FirstProgressReportsTheMaximumProgress_T3(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();
            var deferred3 = Promise.NewDeferred<int>();
            var deferred4 = Promise.NewDeferred<int>();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            progressHelper.Subscribe(
                Promise.First(
                    deferred1.Promise
                        .Then(() => deferred3.Promise),
                    deferred2.Promise
                        .Then(() => deferred4.Promise)
                )
            )
                .Forget();

            progressHelper.AssertCurrentProgress(0f);

            progressHelper.ReportProgressAndAssertResult(deferred1, 0.5f, 0.5f / 2f);
            progressHelper.ReportProgressAndAssertResult(deferred2, 0.3f, 0.5f / 2f, false);
            progressHelper.ReportProgressAndAssertResult(deferred2, 0.7f, 0.7f / 2f);
            progressHelper.ReportProgressAndAssertResult(deferred1, 0.6f, 0.7f / 2f, false);
            progressHelper.ReportProgressAndAssertResult(deferred2, 0.8f, 0.8f / 2f);

            progressHelper.ResolveAndAssertResult(deferred1, 1, 1f / 2f);

            progressHelper.ReportProgressAndAssertResult(deferred2, 0.9f, 1f / 2f, false);
            progressHelper.ReportProgressAndAssertResult(deferred3, 0.5f, 1.5f / 2f);

            progressHelper.ResolveAndAssertResult(deferred2, 1, 1.5f / 2f, false);

            progressHelper.ReportProgressAndAssertResult(deferred4, 0.3f, 1.5f / 2f, false);
            progressHelper.ReportProgressAndAssertResult(deferred4, 0.7f, 1.7f / 2f);

            progressHelper.ResolveAndAssertResult(deferred3, 1, 2f / 2f);

            progressHelper.ReportProgressAndAssertResult(deferred4, 0.9f, 2f / 2f, false);

            progressHelper.ResolveAndAssertResult(deferred4, 1, 2f / 2f, false);
        }

        [Test]
        public void FirstProgressReportsTheMaximumProgress_void4(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            progressHelper.Subscribe(
                Promise.First(
                    deferred1.Promise
                        .Then(() => Promise.Resolved()),
                    deferred2.Promise
                        .Then(() => Promise.Resolved())
                )
            )
                .Forget();

            progressHelper.AssertCurrentProgress(0f);

            progressHelper.ReportProgressAndAssertResult(deferred1, 0.5f, 0.5f / 2f);
            progressHelper.ReportProgressAndAssertResult(deferred2, 0.3f, 0.5f / 2f, false);
            progressHelper.ReportProgressAndAssertResult(deferred2, 0.7f, 0.7f / 2f);
            progressHelper.ReportProgressAndAssertResult(deferred1, 0.6f, 0.7f / 2f, false);
            progressHelper.ReportProgressAndAssertResult(deferred2, 0.8f, 0.8f / 2f);

            progressHelper.ResolveAndAssertResult(deferred1, 2f / 2f);

            progressHelper.ReportProgressAndAssertResult(deferred2, 0.9f, 2f / 2f, false);

            progressHelper.ResolveAndAssertResult(deferred2, 2f / 2f, false);
        }

        [Test]
        public void FirstProgressReportsTheMaximumProgress_T4(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            progressHelper.Subscribe(
                Promise.First(
                    deferred1.Promise
                        .Then(x => Promise.Resolved(x)),
                    deferred2.Promise
                        .Then(x => Promise.Resolved(x))
                )
            )
                .Forget();

            progressHelper.AssertCurrentProgress(0f);

            progressHelper.ReportProgressAndAssertResult(deferred1, 0.5f, 0.5f / 2f);
            progressHelper.ReportProgressAndAssertResult(deferred2, 0.3f, 0.5f / 2f, false);
            progressHelper.ReportProgressAndAssertResult(deferred2, 0.7f, 0.7f / 2f);
            progressHelper.ReportProgressAndAssertResult(deferred1, 0.6f, 0.7f / 2f, false);
            progressHelper.ReportProgressAndAssertResult(deferred2, 0.8f, 0.8f / 2f);

            progressHelper.ResolveAndAssertResult(deferred1, 1, 2f / 2f);

            progressHelper.ReportProgressAndAssertResult(deferred2, 0.9f, 2f / 2f, false);

            progressHelper.ResolveAndAssertResult(deferred2, 1, 2f / 2f, false);
        }

        [Test]
        public void FirstProgressIsNoLongerReportedFromRejected_void(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            progressHelper.Subscribe(
                Promise.First(deferred1.Promise, deferred2.Promise)
            )
                .Catch(() => { })
                .Forget();

            progressHelper.AssertCurrentProgress(0f);

            progressHelper.ReportProgressAndAssertResult(deferred1, 0.5f, 0.5f);
            progressHelper.ReportProgressAndAssertResult(deferred2, 0.7f, 0.7f);

            progressHelper.RejectAndAssertResult(deferred2, "Reject", 0.7f, false);
            progressHelper.RejectAndAssertResult(deferred1, "Reject", 0.7f, false);
        }

        [Test]
        public void FirstProgressIsNoLongerReportedFromRejected_T(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            progressHelper.Subscribe(
                Promise.First(deferred1.Promise, deferred2.Promise)
            )
                .Catch(() => { })
                .Forget();

            progressHelper.AssertCurrentProgress(0f);

            progressHelper.ReportProgressAndAssertResult(deferred1, 0.5f, 0.5f);
            progressHelper.ReportProgressAndAssertResult(deferred2, 0.7f, 0.7f);

            progressHelper.RejectAndAssertResult(deferred2, "Reject", 0.7f, false);
            progressHelper.RejectAndAssertResult(deferred1, "Reject", 0.7f, false);
        }

        [Test]
        public void FirstProgressIsNoLongerReportedFromCanceled_void(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var cancelationSource1 = CancelationSource.New();
            var cancelationSource2 = CancelationSource.New();
            var deferred1 = Promise.NewDeferred(cancelationSource1.Token);
            var deferred2 = Promise.NewDeferred(cancelationSource2.Token);

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            progressHelper.Subscribe(
                Promise.First(deferred1.Promise, deferred2.Promise)
            )
                .Forget();

            progressHelper.AssertCurrentProgress(0f);

            progressHelper.ReportProgressAndAssertResult(deferred1, 0.5f, 0.5f);
            progressHelper.ReportProgressAndAssertResult(deferred2, 0.7f, 0.7f);

            progressHelper.CancelAndAssertResult(cancelationSource2, 0.7f, false);
            progressHelper.CancelAndAssertResult(cancelationSource1, 0.7f, false);

            cancelationSource1.Dispose();
            cancelationSource2.Dispose();
        }

        [Test]
        public void FirstProgressIsNoLongerReportedFromCanceled_T(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var cancelationSource1 = CancelationSource.New();
            var cancelationSource2 = CancelationSource.New();
            var deferred1 = Promise.NewDeferred<int>(cancelationSource1.Token);
            var deferred2 = Promise.NewDeferred<int>(cancelationSource2.Token);

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            progressHelper.Subscribe(
                Promise.First(deferred1.Promise, deferred2.Promise)
            )
                .Forget();

            progressHelper.AssertCurrentProgress(0f);

            progressHelper.ReportProgressAndAssertResult(deferred1, 0.5f, 0.5f);
            progressHelper.ReportProgressAndAssertResult(deferred2, 0.7f, 0.7f);

            progressHelper.CancelAndAssertResult(cancelationSource2, 0.7f, false);
            progressHelper.CancelAndAssertResult(cancelationSource1, 0.7f, false);

            cancelationSource1.Dispose();
            cancelationSource2.Dispose();
        }

        [Test]
        public void FirstProgressWillBeInvokedProperlyFromARecoveredPromise_void(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();
            var deferred3 = Promise.NewDeferred();
            var deferred4 = Promise.NewDeferred();
            var cancelationSource = CancelationSource.New();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            progressHelper.Subscribe(
                Promise.First(
                    // Make first and second promise chains the same length
                    deferred1.Promise
                        .Then(() => Promise.Resolved())
                        .Then(() => Promise.Resolved()),
                    deferred2.Promise
                        .Then(() => deferred3.Promise, cancelationSource.Token)
                        .ContinueWith(_ => deferred4.Promise)
                )
            )
                .Forget();

            progressHelper.AssertCurrentProgress(0f / 3f);

            progressHelper.ReportProgressAndAssertResult(deferred1, 0.5f, 0.5f / 3f);
            progressHelper.ReportProgressAndAssertResult(deferred2, 0.2f, 0.5f / 3f, false);
            progressHelper.ReportProgressAndAssertResult(deferred2, 0.6f, 0.6f / 3f);

            progressHelper.CancelAndAssertResult(cancelationSource, 2f / 3f);

            progressHelper.ReportProgressAndAssertResult(deferred2, 0.7f, 2f / 3f, false);
            progressHelper.ReportProgressAndAssertResult(deferred3, 0.5f, 2f / 3f, false);
            progressHelper.ResolveAndAssertResult(deferred3, 2f / 3f, false);

            progressHelper.ReportProgressAndAssertResult(deferred2, 0.8f, 2f / 3f, false);
            progressHelper.ReportProgressAndAssertResult(deferred1, 0.9f, 2f / 3f, false);

            progressHelper.ReportProgressAndAssertResult(deferred4, 0.5f, 2.5f / 3f);
            progressHelper.ResolveAndAssertResult(deferred4, 3f / 3f);

            progressHelper.ReportProgressAndAssertResult(deferred2, 0.8f, 3f / 3f, false);
            progressHelper.ResolveAndAssertResult(deferred2, 3f / 3f, false);
            progressHelper.ResolveAndAssertResult(deferred1, 3f / 3f, false);

            cancelationSource.Dispose();
            deferred3.Promise.Forget(); // Need to forget this promise because it was never awaited due to the cancelation.
        }

        [Test]
        public void FirstProgressWillBeInvokedProperlyFromARecoveredPromise_T(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();
            var deferred3 = Promise.NewDeferred<int>();
            var deferred4 = Promise.NewDeferred<int>();
            var cancelationSource = CancelationSource.New();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            progressHelper.Subscribe(
                Promise.First(
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

            progressHelper.AssertCurrentProgress(0f / 3f);

            progressHelper.ReportProgressAndAssertResult(deferred1, 0.5f, 0.5f / 3f);
            progressHelper.ReportProgressAndAssertResult(deferred2, 0.2f, 0.5f / 3f, false);
            progressHelper.ReportProgressAndAssertResult(deferred2, 0.6f, 0.6f / 3f);

            progressHelper.CancelAndAssertResult(cancelationSource, 2f / 3f);

            progressHelper.ReportProgressAndAssertResult(deferred2, 0.7f, 2f / 3f, false);
            progressHelper.ReportProgressAndAssertResult(deferred3, 0.5f, 2f / 3f, false);
            progressHelper.ResolveAndAssertResult(deferred3, 1, 2f / 3f, false);

            progressHelper.ReportProgressAndAssertResult(deferred2, 0.8f, 2f / 3f, false);
            progressHelper.ReportProgressAndAssertResult(deferred1, 0.9f, 2f / 3f, false);

            progressHelper.ReportProgressAndAssertResult(deferred4, 0.5f, 2.5f / 3f);
            progressHelper.ResolveAndAssertResult(deferred4, 1, 3f / 3f);

            progressHelper.ReportProgressAndAssertResult(deferred2, 0.8f, 3f / 3f, false);
            progressHelper.ResolveAndAssertResult(deferred2, 1, 3f / 3f, false);
            progressHelper.ResolveAndAssertResult(deferred1, 1, 3f / 3f, false);

            cancelationSource.Dispose();
            deferred3.Promise.Forget(); // Need to forget this promise because it was never awaited due to the cancelation.
        }
#endif
    }
}