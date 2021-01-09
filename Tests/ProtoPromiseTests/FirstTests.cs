#if !PROTO_PROMISE_PROGRESS_DISABLE
#define PROMISE_PROGRESS
#else
#undef PROMISE_PROGRESS
#endif

using NUnit.Framework;

namespace Proto.Promises.Tests
{
    public class FirstTests
    {
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

            Promise.Manager.HandleCompletes();
            Assert.IsTrue(resolved);

            deferred2.Resolve();

            Promise.Manager.HandleCompletes();
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

            Promise.Manager.HandleCompletes();
            Assert.IsTrue(resolved);

            deferred2.Resolve(1);

            Promise.Manager.HandleCompletes();
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

            Promise.Manager.HandleCompletes();
            Assert.IsTrue(resolved);

            deferred1.Resolve();

            Promise.Manager.HandleCompletes();
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

            Promise.Manager.HandleCompletes();
            Assert.IsTrue(resolved);

            deferred1.Resolve(1);

            Promise.Manager.HandleCompletes();
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

            Promise.Manager.HandleCompletes();
            Assert.IsFalse(resolved);

            deferred2.Resolve();

            Promise.Manager.HandleCompletes();
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

            Promise.Manager.HandleCompletes();
            Assert.IsFalse(resolved);

            deferred2.Resolve(5);

            Promise.Manager.HandleCompletes();
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

            Promise.Manager.HandleCompletes();
            Assert.IsFalse(resolved);

            deferred1.Resolve();

            Promise.Manager.HandleCompletes();
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

            Promise.Manager.HandleCompletes();
            Assert.IsFalse(resolved);

            deferred1.Resolve(5);

            Promise.Manager.HandleCompletes();
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

            Promise.Manager.HandleCompletes();
            Assert.IsFalse(rejected);

            deferred2.Reject(expected);

            Promise.Manager.HandleCompletes();
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

            Promise.Manager.HandleCompletes();
            Assert.IsFalse(rejected);

            deferred2.Reject(expected);

            Promise.Manager.HandleCompletes();
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

            Promise.Manager.HandleCompletes();
            Assert.IsFalse(resolved);

            deferred2.Resolve();

            Promise.Manager.HandleCompletes();
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

            Promise.Manager.HandleCompletes();
            Assert.IsFalse(resolved);

            deferred2.Resolve(5);

            Promise.Manager.HandleCompletes();
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

            Promise.Manager.HandleCompletes();
            Assert.IsFalse(resolved);

            deferred1.Resolve();

            Promise.Manager.HandleCompletes();
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

            Promise.Manager.HandleCompletes();
            Assert.IsFalse(resolved);

            deferred1.Resolve(5);

            Promise.Manager.HandleCompletes();
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

            Promise.Manager.HandleCompletes();
            Assert.IsFalse(canceled);

            cancelationSource2.Cancel(expected);

            Promise.Manager.HandleCompletes();
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

            Promise.Manager.HandleCompletes();
            Assert.IsFalse(canceled);

            cancelationSource2.Cancel(expected);

            Promise.Manager.HandleCompletes();
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

            Promise.Manager.HandleCompletes();
            Assert.IsFalse(canceled);

            cancelationSource2.Cancel();

            Promise.Manager.HandleCompletes();
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

            Promise.Manager.HandleCompletes();
            Assert.IsFalse(canceled);

            cancelationSource2.Cancel();

            Promise.Manager.HandleCompletes();
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

            Promise.Manager.HandleCompletes();
            Assert.IsFalse(rejected);

            deferred2.Reject(expected);

            Promise.Manager.HandleCompletes();
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

            Promise.Manager.HandleCompletes();
            Assert.IsFalse(rejected);

            deferred2.Reject(expected);

            Promise.Manager.HandleCompletes();
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

            Promise.Manager.HandleCompletes();
            Assert.IsFalse(rejected);

            deferred1.Reject(expected);

            Promise.Manager.HandleCompletes();
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

            Promise.Manager.HandleCompletes();
            Assert.IsFalse(rejected);

            deferred1.Reject(expected);

            Promise.Manager.HandleCompletes();
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

            Promise.Manager.HandleCompletes();
            Assert.IsFalse(canceled);

            cancelationSource.Cancel(expected);

            Promise.Manager.HandleCompletes();
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

            Promise.Manager.HandleCompletes();
            Assert.IsFalse(canceled);

            cancelationSource.Cancel(expected);

            Promise.Manager.HandleCompletes();
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

            Promise.Manager.HandleCompletes();
            Assert.IsFalse(canceled);

            cancelationSource.Cancel();

            Promise.Manager.HandleCompletes();
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

            Promise.Manager.HandleCompletes();
            Assert.IsFalse(canceled);

            cancelationSource.Cancel();

            Promise.Manager.HandleCompletes();
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

            Promise.Manager.HandleCompletes();
            Assert.IsFalse(canceled);

            cancelationSource.Cancel(expected);

            Promise.Manager.HandleCompletes();
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

            Promise.Manager.HandleCompletes();
            Assert.IsFalse(canceled);

            cancelationSource.Cancel(expected);

            Promise.Manager.HandleCompletes();
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

            Promise.Manager.HandleCompletes();
            Assert.IsFalse(canceled);

            cancelationSource.Cancel();

            Promise.Manager.HandleCompletes();
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

            Promise.Manager.HandleCompletes();
            Assert.IsFalse(canceled);

            cancelationSource.Cancel();

            Promise.Manager.HandleCompletes();
            Assert.IsTrue(canceled);

            cancelationSource.Dispose();
        }

#if PROMISE_PROGRESS
        [Test]
        public void FirstProgressReportsTheMaximumProgress_void0()
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();

            float progress = float.NaN;

            Promise.First(deferred1.Promise, deferred2.Promise)
                .Progress(p => progress = p)
                .Forget();

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0f, progress, 0f);

            deferred1.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.3f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.7f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.7f, progress, TestHelper.progressEpsilon);

            deferred1.ReportProgress(0.6f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.7f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.8f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.8f, progress, TestHelper.progressEpsilon);

            deferred1.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);

            deferred2.Resolve();
        }

        [Test]
        public void FirstProgressReportsTheMaximumProgress_T0()
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();

            float progress = float.NaN;

            Promise.First(deferred1.Promise, deferred2.Promise)
                .Progress(p => progress = p)
                .Forget();

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0f, progress, 0f);

            deferred1.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.3f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.7f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.7f, progress, TestHelper.progressEpsilon);

            deferred1.ReportProgress(0.6f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.7f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.8f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.8f, progress, TestHelper.progressEpsilon);

            deferred1.Resolve(1);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);

            deferred2.Resolve(1);
        }

        [Test]
        public void FirstProgressReportsTheMaximumProgress_void1()
        {
            var deferred1 = Promise.NewDeferred();

            float progress = float.NaN;

            Promise.First(deferred1.Promise, Promise.Resolved())
                .Progress(p => progress = p)
                .Forget();

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, 0f);

            deferred1.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
        }

        [Test]
        public void FirstProgressReportsTheMaximumProgress_T1()
        {
            var deferred1 = Promise.NewDeferred<int>();

            float progress = float.NaN;

            Promise.First(deferred1.Promise, Promise.Resolved(1))
                .Progress(p => progress = p)
                .Forget();

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, 0f);

            deferred1.Resolve(1);
            Promise.Manager.HandleCompletesAndProgress();
        }

        [Test]
        public void FirstProgressReportsTheMaximumProgress_void2()
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();

            float progress = float.NaN;

            Promise.First
            (
                deferred1.Promise
                    .Then(() => { }),
                deferred2.Promise
                    .Then(() => { })
            )
                .Progress(p => progress = p)
                .Forget();

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0f, progress, 0f);

            deferred1.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.3f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.7f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.7f, progress, TestHelper.progressEpsilon);

            deferred1.ReportProgress(0.6f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.7f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.8f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.8f, progress, TestHelper.progressEpsilon);

            deferred1.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.9f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);

            deferred2.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);
        }

        [Test]
        public void FirstProgressReportsTheMaximumProgress_T2()
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();

            float progress = float.NaN;

            Promise.First
            (
                deferred1.Promise
                    .Then(() => 1),
                deferred2.Promise
                    .Then(() => 1)
            )
                .Progress(p => progress = p)
                .Forget();

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0f, progress, 0f);

            deferred1.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.3f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.7f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.7f, progress, TestHelper.progressEpsilon);

            deferred1.ReportProgress(0.6f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.7f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.8f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.8f, progress, TestHelper.progressEpsilon);

            deferred1.Resolve(1);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.9f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);

            deferred2.Resolve(1);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);
        }

        [Test]
        public void FirstProgressReportsTheMaximumProgress_void3()
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();
            var deferred3 = Promise.NewDeferred();
            var deferred4 = Promise.NewDeferred();

            float progress = float.NaN;

            Promise.First
            (
                deferred1.Promise
                    .Then(() => deferred3.Promise),
                deferred2.Promise
                    .Then(() => deferred4.Promise)
            )
                .Progress(p => progress = p)
                .Forget();

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0f, progress, 0f);

            deferred1.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f / 2f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.3f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f / 2f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.7f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.7f / 2f, progress, TestHelper.progressEpsilon);

            deferred1.ReportProgress(0.6f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.7f / 2f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.8f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.8f / 2f, progress, TestHelper.progressEpsilon);

            deferred1.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f / 2f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.9f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f / 2f, progress, TestHelper.progressEpsilon);

            deferred3.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1.5f / 2f, progress, TestHelper.progressEpsilon);

            deferred2.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1.5f / 2f, progress, TestHelper.progressEpsilon);

            deferred4.ReportProgress(0.3f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1.5f / 2f, progress, TestHelper.progressEpsilon);

            deferred4.ReportProgress(0.7f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1.7f / 2f, progress, TestHelper.progressEpsilon);

            deferred3.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);

            deferred4.ReportProgress(0.9f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);

            deferred4.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);
        }

        [Test]
        public void FirstProgressReportsTheMaximumProgress_T3()
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();
            var deferred3 = Promise.NewDeferred<int>();
            var deferred4 = Promise.NewDeferred<int>();

            float progress = float.NaN;

            Promise.First
            (
                deferred1.Promise
                    .Then(() => deferred3.Promise),
                deferred2.Promise
                    .Then(() => deferred4.Promise)
            )
                .Progress(p => progress = p)
                .Forget();

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0f, progress, 0f);

            deferred1.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f / 2f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.3f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f / 2f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.7f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.7f / 2f, progress, TestHelper.progressEpsilon);

            deferred1.ReportProgress(0.6f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.7f / 2f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.8f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.8f / 2f, progress, TestHelper.progressEpsilon);

            deferred1.Resolve(1);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f / 2f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.9f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f / 2f, progress, TestHelper.progressEpsilon);

            deferred3.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1.5f / 2f, progress, TestHelper.progressEpsilon);

            deferred2.Resolve(1);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1.5f / 2f, progress, TestHelper.progressEpsilon);

            deferred4.ReportProgress(0.3f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1.5f / 2f, progress, TestHelper.progressEpsilon);

            deferred4.ReportProgress(0.7f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1.7f / 2f, progress, TestHelper.progressEpsilon);

            deferred3.Resolve(1);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);

            deferred4.ReportProgress(0.9f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);

            deferred4.Resolve(1);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);
        }

        [Test]
        public void FirstProgressReportsTheMaximumProgress_void4()
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();

            float progress = float.NaN;

            Promise.First
            (
                deferred1.Promise
                    .Then(Promise.Resolved),
                deferred2.Promise
                    .Then(Promise.Resolved)
            )
                .Progress(p => progress = p)
                .Forget();

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0f, progress, 0f);

            deferred1.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f / 2f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.3f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f / 2f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.7f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.7f / 2f, progress, TestHelper.progressEpsilon);

            deferred1.ReportProgress(0.6f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.7f / 2f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.8f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.8f / 2f, progress, TestHelper.progressEpsilon);

            deferred1.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.9f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);

            deferred2.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);
        }

        [Test]
        public void FirstProgressReportsTheMaximumProgress_T4()
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();

            float progress = float.NaN;

            Promise.First
            (
                deferred1.Promise
                    .Then(x => Promise.Resolved(x)),
                deferred2.Promise
                    .Then(x => Promise.Resolved(x))
            )
                .Progress(p => progress = p)
                .Forget();

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0f, progress, 0f);

            deferred1.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f / 2f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.3f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f / 2f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.7f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.7f / 2f, progress, TestHelper.progressEpsilon);

            deferred1.ReportProgress(0.6f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.7f / 2f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.8f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.8f / 2f, progress, TestHelper.progressEpsilon);

            deferred1.Resolve(1);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.9f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);

            deferred2.Resolve(1);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);
        }

        [Test]
        public void FirstProgressIsNoLongerReportedFromRejected_void()
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();

            float progress = float.NaN;

            Promise.First(deferred1.Promise, deferred2.Promise)
                .Progress(p => progress = p)
                .Catch(() => { })
                .Forget();

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0f, progress, 0f);

            deferred1.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.7f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.7f, progress, TestHelper.progressEpsilon);

            deferred2.Reject("Reject");
            deferred1.Reject("Reject");
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.7f, progress, TestHelper.progressEpsilon);

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.7f, progress, TestHelper.progressEpsilon);
        }

        [Test]
        public void FirstProgressIsNoLongerReportedFromRejected_T()
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();

            float progress = float.NaN;

            Promise.First(deferred1.Promise, deferred2.Promise)
                .Progress(p => progress = p)
                .Catch(() => { })
                .Forget();

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0f, progress, 0f);

            deferred1.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.7f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.7f, progress, TestHelper.progressEpsilon);

            deferred2.Reject("Reject");
            deferred1.Reject("Reject");
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.7f, progress, TestHelper.progressEpsilon);

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.7f, progress, TestHelper.progressEpsilon);
        }

        [Test]
        public void FirstProgressIsNoLongerReportedFromCanceled_void()
        {
            var cancelationSource1 = CancelationSource.New();
            var cancelationSource2 = CancelationSource.New();
            var deferred1 = Promise.NewDeferred(cancelationSource1.Token);
            var deferred2 = Promise.NewDeferred(cancelationSource2.Token);

            float progress = float.NaN;

            Promise.First(deferred1.Promise, deferred2.Promise)
                .Progress(p => progress = p)
                .Forget();

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0f, progress, 0f);

            deferred1.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.7f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.7f, progress, TestHelper.progressEpsilon);

            cancelationSource1.Cancel();
            cancelationSource2.Cancel();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.7f, progress, TestHelper.progressEpsilon);

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.7f, progress, TestHelper.progressEpsilon);

            cancelationSource1.Dispose();
            cancelationSource2.Dispose();
        }

        [Test]
        public void FirstProgressIsNoLongerReportedFromCanceled_T()
        {
            var cancelationSource1 = CancelationSource.New();
            var cancelationSource2 = CancelationSource.New();
            var deferred1 = Promise.NewDeferred<int>(cancelationSource1.Token);
            var deferred2 = Promise.NewDeferred<int>(cancelationSource2.Token);

            float progress = float.NaN;

            Promise.First(deferred1.Promise, deferred2.Promise)
                .Progress(p => progress = p)
                .Forget();

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0f, progress, 0f);

            deferred1.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.7f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.7f, progress, TestHelper.progressEpsilon);

            cancelationSource1.Cancel();
            cancelationSource2.Cancel();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.7f, progress, TestHelper.progressEpsilon);

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.7f, progress, TestHelper.progressEpsilon);

            cancelationSource1.Dispose();
            cancelationSource2.Dispose();
        }

        [Test]
        public void FirstProgressWillBeInvokedProperlyFromARecoveredPromise_void()
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();
            var deferred3 = Promise.NewDeferred();
            var deferred4 = Promise.NewDeferred();
            var cancelationSource = CancelationSource.New();

            float progress = float.NaN;

            Promise.First
            (
                // Make first and second promise chains the same length
                deferred1.Promise
                    .Then(Promise.Resolved)
                    .Then(Promise.Resolved),
                deferred2.Promise
                    .Then(() => deferred3.Promise, cancelationSource.Token)
                    .ContinueWith(_ => deferred4.Promise)
            )
                .Progress(p => progress = p)
                .Forget();

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0f / 3f, progress, TestHelper.progressEpsilon);

            deferred1.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f / 3f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.25f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f / 3f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.6f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.6f / 3f, progress, TestHelper.progressEpsilon);

            cancelationSource.Cancel();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(2f / 3f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.7f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(2f / 3f, progress, TestHelper.progressEpsilon);

            deferred3.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(2f / 3f, progress, TestHelper.progressEpsilon);

            deferred3.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(2f / 3f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.8f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(2f / 3f, progress, TestHelper.progressEpsilon);

            deferred4.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(2.5f / 3f, progress, TestHelper.progressEpsilon);

            deferred4.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.8f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);

            deferred2.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);

            deferred1.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);

            cancelationSource.Dispose();
            deferred3.Promise.Forget(); // Need to forget this promise because it was never awaited due to the cancelation.
        }

        [Test]
        public void FirstProgressWillBeInvokedProperlyFromARecoveredPromise_T()
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();
            var deferred3 = Promise.NewDeferred<int>();
            var deferred4 = Promise.NewDeferred<int>();
            var cancelationSource = CancelationSource.New();

            float progress = float.NaN;

            Promise.First
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
            Assert.AreEqual(0f / 3f, progress, TestHelper.progressEpsilon);

            deferred1.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f / 3f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.25f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f / 3f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.6f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.6f / 3f, progress, TestHelper.progressEpsilon);

            cancelationSource.Cancel();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(2f / 3f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.7f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(2f / 3f, progress, TestHelper.progressEpsilon);

            deferred3.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(2f / 3f, progress, TestHelper.progressEpsilon);

            deferred3.Resolve(1);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(2f / 3f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.8f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(2f / 3f, progress, TestHelper.progressEpsilon);

            deferred4.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(2.5f / 3f, progress, TestHelper.progressEpsilon);

            deferred4.Resolve(1);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.8f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);

            deferred2.Resolve(1);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);

            deferred1.Resolve(1);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);

            cancelationSource.Dispose();
            deferred3.Promise.Forget(); // Need to forget this promise because it was never awaited due to the cancelation.
        }
#endif
    }
}