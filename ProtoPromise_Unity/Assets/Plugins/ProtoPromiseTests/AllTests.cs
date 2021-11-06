#if !PROTO_PROMISE_PROGRESS_DISABLE
#define PROMISE_PROGRESS
#else
#undef PROMISE_PROGRESS
#endif

using System.Linq;
using NUnit.Framework;

namespace Proto.Promises.Tests
{
    public class AllTests
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
        public void AllPromiseIsResolvedWhenAllPromisesAreResolved_void()
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();

            bool invoked = false;

            Promise.All(deferred1.Promise, deferred2.Promise)
                .Then(() => { invoked = true; })
                .Forget();

            Assert.IsFalse(invoked);

            deferred1.Resolve();
            Assert.IsFalse(invoked);

            deferred2.Resolve();
            Assert.IsTrue(invoked);
        }

        [Test]
        public void AllPromiseIsResolvedWhenAllPromisesAreResolved_T()
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();

            bool invoked = false;

            Promise.All(deferred1.Promise, deferred2.Promise)
                .Then(values =>
                {
                    invoked = true;
                    Assert.AreEqual(2, values.Count);
                    Assert.AreEqual(10, values[0]);
                    Assert.AreEqual(20, values[1]);
                })
                .Forget();

            Assert.IsFalse(invoked);

            deferred1.Resolve(10);

            Assert.IsFalse(invoked);

            deferred2.Resolve(20);

            Assert.IsTrue(invoked);
        }

        [Test]
        public void AllPromiseIsResolvedIfThereAreNoPromises_void()
        {
            bool invoked = false;

            Promise.All(Enumerable.Empty<Promise>())
                .Then(() => { invoked = true; })
                .Forget();

            Assert.IsTrue(invoked);
        }

        [Test]
        public void AllPromiseIsResolvedIfThereAreNoPromises_T()
        {
            bool invoked = false;

            Promise.All(Enumerable.Empty<Promise<int>>())
                .Then(v =>
                {
                    invoked = true;
                    Assert.IsEmpty(v);
                })
                .Forget();

            Assert.IsTrue(invoked);
        }


        [Test]
        public void AllPromiseIsResolvedWhenAllPromisesAreAlreadyResolved_void()
        {
            bool invoked = false;

            Promise.All(Promise.Resolved(), Promise.Resolved())
                .Then(() => { invoked = true; })
                .Forget();

            Assert.IsTrue(invoked);
        }

        [Test]
        public void AllPromiseIsResolvedWhenAllPromisesAreAlreadyResolved_T()
        {
            bool invoked = false;

            Promise.All(Promise.Resolved(1), Promise.Resolved(2))
                .Then(v => { invoked = true; })
                .Forget();

            Assert.IsTrue(invoked);
        }

        [Test]
        public void AllPromiseIsRejectedWhenFirstPromiseIsRejected_void()
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();

            bool errored = false;

            Promise.All(deferred1.Promise, deferred2.Promise)
                .Then(
                    () => Assert.Fail("Promise was resolved when it should have been rejected."),
                    (string e) => { errored = true; })
                .Forget();

            deferred1.Reject("Error!");

            Assert.IsTrue(errored);

            deferred2.Resolve();

            Assert.IsTrue(errored);
        }

        [Test]
        public void AllPromiseIsRejectedWhenFirstPromiseIsRejected_T()
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();

            bool errored = false;

            Promise.All(deferred1.Promise, deferred2.Promise)
                .Then(
                    v => Assert.Fail("Promise was resolved when it should have been rejected."),
                    (string e) => { errored = true; })
                .Forget();

            deferred1.Reject("Error!");

            Assert.IsTrue(errored);

            deferred2.Resolve(2);

            Assert.IsTrue(errored);
        }

        [Test]
        public void AllPromiseIsRejectedWhenSecondPromiseIsRejected_void()
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();

            bool errored = false;

            Promise.All(deferred1.Promise, deferred2.Promise)
                .Then(
                    () => Assert.Fail("Promise was resolved when it should have been rejected."),
                    (string e) => { errored = true; })
                .Forget();

            deferred1.Resolve();

            Assert.IsFalse(errored);

            deferred2.Reject("Error!");

            Assert.IsTrue(errored);
        }

        [Test]
        public void AllPromiseIsRejectedWhenSecondPromiseIsRejected_T()
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();

            bool errored = false;

            Promise.All(deferred1.Promise, deferred2.Promise)
                .Then(
                    v => Assert.Fail("Promise was resolved when it should have been rejected."),
                    (string e) => { errored = true; })
                .Forget();

            deferred1.Resolve(2);

            Assert.IsFalse(errored);

            deferred2.Reject("Error!");

            Assert.IsTrue(errored);
        }

        [Test]
        public void AllPromiseIsRejectedWhenBothPromisesAreRejected_void()
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();
            var promise1 = deferred1.Promise.Preserve();
            var promise2 = deferred2.Promise.Preserve();

            // All does not suppress rejections if one of the promises is rejected before the others complete.
            promise1.Catch((string _) => { }).Forget();
            promise2.Catch((string _) => { }).Forget();

            bool errored = false;

            Promise.All(promise1, promise2)
                .Then(
                    () => Assert.Fail("Promise was resolved when it should have been rejected."),
                    (string e) => { errored = true; })
                .Forget();

            deferred1.Reject("Error!");

            Assert.IsTrue(errored);

            deferred2.Reject("Error!");

            Assert.IsTrue(errored);

            promise1.Forget();
            promise2.Forget();
        }

        [Test]
        public void AllPromiseIsRejectedWhenBothPromisesAreRejected_T()
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();
            var promise1 = deferred1.Promise.Preserve();
            var promise2 = deferred2.Promise.Preserve();

            // All does not suppress rejections if one of the promises is rejected before the others complete.
            promise1.Catch((string _) => { }).Forget();
            promise2.Catch((string _) => { }).Forget();

            bool errored = false;

            Promise.All(promise1, promise2)
                .Then(
                    v => Assert.Fail("Promise was resolved when it should have been rejected."),
                    (string e) => { errored = true; })
                .Forget();

            deferred1.Reject("Error!");

            Assert.IsTrue(errored);

            deferred2.Reject("Error!");

            Assert.IsTrue(errored);

            promise1.Forget();
            promise2.Forget();
        }

        [Test]
        public void AllPromiseIsRejectedWhenAnyPromiseIsAlreadyRejected_void()
        {
            int rejectCount = 0;
            string rejection = "Error!";

            var deferred = Promise.NewDeferred();
            var promise1 = deferred.Promise.Preserve();
            var promise2 = Promise.Rejected(rejection).Preserve();

            Promise.All(promise1, promise2)
                .Then(
                    () => Assert.Fail("Promise was resolved when it should have been rejected."),
                    (string ex) =>
                    {
                        Assert.AreEqual(rejection, ex);
                        ++rejectCount;
                    })
                .Forget();

            Promise.All(promise2, promise1)
                .Then(
                    () => Assert.Fail("Promise was resolved when it should have been rejected."),
                    (string ex) =>
                    {
                        Assert.AreEqual(rejection, ex);
                        ++rejectCount;
                    })
                .Forget();

            Assert.AreEqual(2, rejectCount);

            deferred.Resolve();

            Assert.AreEqual(2, rejectCount);

            promise1.Forget();
            promise2.Forget();
        }

        [Test]
        public void AllPromiseIsRejectedWhenAnyPromiseIsAlreadyRejected_T()
        {
            int rejectCount = 0;
            string rejection = "Error!";

            var deferred = Promise.NewDeferred<int>();
            var promise1 = deferred.Promise.Preserve();
            var promise2 = Promise<int>.Rejected(rejection).Preserve();

            Promise.All(promise1, promise2)
                .Then(
                    v => Assert.Fail("Promise was resolved when it should have been rejected."),
                    (string ex) =>
                    {
                        Assert.AreEqual(rejection, ex);
                        ++rejectCount;
                    })
                .Forget();

            Promise.All(promise2, promise1)
                .Then(
                    v => Assert.Fail("Promise was resolved when it should have been rejected."),
                    (string ex) =>
                    {
                        Assert.AreEqual(rejection, ex);
                        ++rejectCount;
                    })
                .Forget();

            Assert.AreEqual(2, rejectCount);

            deferred.Resolve(1);

            Assert.AreEqual(2, rejectCount);

            promise1.Forget();
            promise2.Forget();
        }

        [Test]
        public void AllPromiseIsCanceledWhenFirstPromiseIsCanceled_void()
        {
            CancelationSource cancelationSource = CancelationSource.New();

            var deferred1 = Promise.NewDeferred(cancelationSource.Token);
            var deferred2 = Promise.NewDeferred();

            bool canceled = false;

            Promise.All(deferred1.Promise, deferred2.Promise)
                .Then(() => Assert.Fail("Promise was resolved when it should have been canceled."))
                .CatchCancelation(e => { canceled = true; })
                .Forget();

            cancelationSource.Cancel("Cancel!");

            Assert.IsTrue(canceled);

            deferred2.Resolve();

            Assert.IsTrue(canceled);

            cancelationSource.Dispose();
        }

        [Test]
        public void AllPromiseIsCanceledWhenFirstPromiseIsCanceled_T()
        {
            CancelationSource cancelationSource = CancelationSource.New();

            var deferred1 = Promise.NewDeferred<int>(cancelationSource.Token);
            var deferred2 = Promise.NewDeferred<int>();

            bool canceled = false;

            Promise.All(deferred1.Promise, deferred2.Promise)
                .Then(v => Assert.Fail("Promise was resolved when it should have been canceled."))
                .CatchCancelation(e => { canceled = true; })
                .Forget();

            cancelationSource.Cancel("Cancel!");

            Assert.IsTrue(canceled);

            deferred2.Resolve(2);

            Assert.IsTrue(canceled);

            cancelationSource.Dispose();
        }

        [Test]
        public void AllPromiseIsCanceledWhenSecondPromiseIsCanceled_void()
        {
            var deferred1 = Promise.NewDeferred();
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred2 = Promise.NewDeferred(cancelationSource.Token);

            bool canceled = false;

            Promise.All(deferred1.Promise, deferred2.Promise)
                .Then(() => Assert.Fail("Promise was resolved when it should have been canceled."))
                .CatchCancelation(e => { canceled = true; })
                .Forget();

            deferred1.Resolve();

            Assert.IsFalse(canceled);

            cancelationSource.Cancel("Cancel!");

            Assert.IsTrue(canceled);

            cancelationSource.Dispose();
        }

        [Test]
        public void AllPromiseIsCanceledWhenSecondPromiseIsCanceled_T()
        {
            var deferred1 = Promise.NewDeferred<int>();
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred2 = Promise.NewDeferred<int>(cancelationSource.Token);

            bool canceled = false;

            Promise.All(deferred1.Promise, deferred2.Promise)
                .Then(v => Assert.Fail("Promise was resolved when it should have been canceled."))
                .CatchCancelation(e => { canceled = true; })
                .Forget();

            deferred1.Resolve(2);

            Assert.IsFalse(canceled);

            cancelationSource.Cancel("Cancel!");

            Assert.IsTrue(canceled);

            cancelationSource.Dispose();
        }

        [Test]
        public void AllPromiseIsCanceledWhenBothPromisesAreCanceled_void()
        {
            CancelationSource cancelationSource1 = CancelationSource.New();
            CancelationSource cancelationSource2 = CancelationSource.New();

            var deferred1 = Promise.NewDeferred(cancelationSource1.Token);
            var deferred2 = Promise.NewDeferred(cancelationSource2.Token);

            bool canceled = false;

            Promise.All(deferred1.Promise, deferred2.Promise)
                .Then(() => Assert.Fail("Promise was resolved when it should have been canceled."))
                .CatchCancelation(e => { canceled = true; })
                .Forget();

            cancelationSource1.Cancel("Cancel!");

            Assert.IsTrue(canceled);

            cancelationSource2.Cancel("Cancel!");

            Assert.IsTrue(canceled);

            cancelationSource1.Dispose();
            cancelationSource2.Dispose();
        }

        [Test]
        public void AllPromiseIsCanceledWhenBothPromisesAreCanceled_T()
        {
            CancelationSource cancelationSource1 = CancelationSource.New();
            CancelationSource cancelationSource2 = CancelationSource.New();

            var deferred1 = Promise.NewDeferred<int>(cancelationSource1.Token);
            var deferred2 = Promise.NewDeferred<int>(cancelationSource2.Token);

            bool canceled = false;

            Promise.All(deferred1.Promise, deferred2.Promise)
                .Then(v => Assert.Fail("Promise was resolved when it should have been canceled."))
                .CatchCancelation(e => { canceled = true; })
                .Forget();

            cancelationSource1.Cancel("Cancel!");

            Assert.IsTrue(canceled);

            cancelationSource2.Cancel("Cancel!");

            Assert.IsTrue(canceled);

            cancelationSource1.Dispose();
            cancelationSource2.Dispose();
        }

        [Test]
        public void AllPromiseIsCancelededWhenAnyPromiseIsAlreadyCanceled_void()
        {
            int cancelCount = 0;
            string cancelation = "Cancel!";

            var deferred = Promise.NewDeferred();
            var promise1 = deferred.Promise.Preserve();
            var promise2 = Promise.Canceled(cancelation).Preserve();

            Promise.All(promise1, promise2)
                .Then(() => Assert.Fail("Promise was resolved when it should have been canceled."))
                .CatchCancelation(ex =>
                {
                    Assert.AreEqual(cancelation, ex.Value);
                    ++cancelCount;
                })
                .Forget();

            Promise.All(promise2, promise1)
                .Then(() => Assert.Fail("Promise was resolved when it should have been canceled."))
                .CatchCancelation(ex =>
                {
                    Assert.AreEqual(cancelation, ex.Value);
                    ++cancelCount;
                })
                .Forget();

            deferred.Resolve();

            Assert.AreEqual(2, cancelCount);

            promise1.Forget();
            promise2.Forget();
        }

        [Test]
        public void AllPromiseIsCancelededWhenAnyPromiseIsAlreadyCanceled_T()
        {
            int cancelCount = 0;
            string cancelation = "Cancel!";

            var deferred = Promise.NewDeferred<int>();
            var promise1 = deferred.Promise.Preserve();
            var promise2 = Promise<int>.Canceled(cancelation).Preserve();

            Promise.All(promise1, promise2)
                .Then(v => Assert.Fail("Promise was resolved when it should have been canceled."))
                .CatchCancelation(ex =>
                {
                    Assert.AreEqual(cancelation, ex.Value);
                    ++cancelCount;
                })
                .Forget();

            Promise.All(promise2, promise1)
                .Then(v => Assert.Fail("Promise was resolved when it should have been canceled."))
                .CatchCancelation(ex =>
                {
                    Assert.AreEqual(cancelation, ex.Value);
                    ++cancelCount;
                })
                .Forget();

            deferred.Resolve(1);

            Assert.AreEqual(2, cancelCount);

            promise1.Forget();
            promise2.Forget();
        }

#if PROMISE_PROGRESS
        [Test]
        public void AllProgressIsNormalized_void0(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();
            var deferred3 = Promise.NewDeferred();
            var deferred4 = Promise.NewDeferred();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            progressHelper.Subscribe(
                Promise.All(deferred1.Promise, deferred2.Promise, deferred3.Promise, deferred4.Promise)
            )
                .Forget();

            progressHelper.AssertCurrentProgress(0f / 4f);

            progressHelper.ReportProgressAndAssertResult(deferred1, 0.5f, 0.5f / 4f);
            progressHelper.ResolveAndAssertResult(deferred1, 1f / 4f);

            progressHelper.ReportProgressAndAssertResult(deferred2, 0.5f, 1.5f / 4f);
            progressHelper.ResolveAndAssertResult(deferred2, 2f / 4f);

            progressHelper.ReportProgressAndAssertResult(deferred3, 0.5f, 2.5f / 4f);
            progressHelper.ResolveAndAssertResult(deferred3, 3f / 4f);

            progressHelper.ReportProgressAndAssertResult(deferred4, 0.5f, 3.5f / 4f);
            progressHelper.ResolveAndAssertResult(deferred4, 4f / 4f);
        }

        [Test]
        public void AllProgressIsNormalized_T0(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();
            var deferred3 = Promise.NewDeferred<int>();
            var deferred4 = Promise.NewDeferred<int>();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            progressHelper.Subscribe(
                Promise.All(deferred1.Promise, deferred2.Promise, deferred3.Promise, deferred4.Promise)
            )
                .Forget();

            progressHelper.AssertCurrentProgress(0f / 4f);

            progressHelper.ReportProgressAndAssertResult(deferred1, 0.5f, 0.5f / 4f);
            progressHelper.ResolveAndAssertResult(deferred1, 1, 1f / 4f);

            progressHelper.ReportProgressAndAssertResult(deferred2, 0.5f, 1.5f / 4f);
            progressHelper.ResolveAndAssertResult(deferred2, 1, 2f / 4f);

            progressHelper.ReportProgressAndAssertResult(deferred3, 0.5f, 2.5f / 4f);
            progressHelper.ResolveAndAssertResult(deferred3, 1, 3f / 4f);

            progressHelper.ReportProgressAndAssertResult(deferred4, 0.5f, 3.5f / 4f);
            progressHelper.ResolveAndAssertResult(deferred4, 1, 4f / 4f);
        }

        [Test]
        public void AllProgressIsNormalized_void1(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred1 = Promise.NewDeferred();
            var deferred3 = Promise.NewDeferred();
            var deferred4 = Promise.NewDeferred();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            progressHelper.Subscribe(
                Promise.All(deferred1.Promise, Promise.Resolved(), deferred3.Promise, deferred4.Promise)
            )
                .Forget();

            progressHelper.AssertCurrentProgress(1f / 4f);

            progressHelper.ReportProgressAndAssertResult(deferred1, 0.5f, 1.5f / 4f);
            progressHelper.ResolveAndAssertResult(deferred1, 2f / 4f);

            progressHelper.ReportProgressAndAssertResult(deferred3, 0.5f, 2.5f / 4f);
            progressHelper.ResolveAndAssertResult(deferred3, 3f / 4f);

            progressHelper.ReportProgressAndAssertResult(deferred4, 0.5f, 3.5f / 4f);
            progressHelper.ResolveAndAssertResult(deferred4, 4f / 4f);
        }

        [Test]
        public void AllProgressIsNormalized_T1(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred3 = Promise.NewDeferred<int>();
            var deferred4 = Promise.NewDeferred<int>();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            progressHelper.Subscribe(
                Promise.All(deferred1.Promise, Promise.Resolved(), deferred3.Promise, deferred4.Promise)
            )
                .Forget();

            progressHelper.AssertCurrentProgress(1f / 4f);

            progressHelper.ReportProgressAndAssertResult(deferred1, 0.5f, 1.5f / 4f);
            progressHelper.ResolveAndAssertResult(deferred1, 1, 2f / 4f);

            progressHelper.ReportProgressAndAssertResult(deferred3, 0.5f, 2.5f / 4f);
            progressHelper.ResolveAndAssertResult(deferred3, 1, 3f / 4f);

            progressHelper.ReportProgressAndAssertResult(deferred4, 0.5f, 3.5f / 4f);
            progressHelper.ResolveAndAssertResult(deferred4, 1, 4f / 4f);
        }

        [Test]
        public void AllProgressIsNormalized_void2(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            progressHelper.Subscribe(
                Promise.All(deferred1.Promise.ThenDuplicate(), deferred2.Promise.ThenDuplicate())
            )
                .Forget();

            progressHelper.AssertCurrentProgress(0f / 2f);

            progressHelper.ReportProgressAndAssertResult(deferred1, 0.5f, 0.5f / 2f);
            progressHelper.ResolveAndAssertResult(deferred1, 1f / 2f);

            progressHelper.ReportProgressAndAssertResult(deferred2, 0.5f, 1.5f / 2f);
            progressHelper.ResolveAndAssertResult(deferred2, 2f / 2f);
        }

        [Test]
        public void AllProgressIsNormalized_T2(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            progressHelper.Subscribe(
                Promise.All(deferred1.Promise.ThenDuplicate(), deferred2.Promise.ThenDuplicate())
            )
                .Forget();

            progressHelper.AssertCurrentProgress(0f / 2f);

            progressHelper.ReportProgressAndAssertResult(deferred1, 0.5f, 0.5f / 2f);
            progressHelper.ResolveAndAssertResult(deferred1, 1, 1f / 2f);

            progressHelper.ReportProgressAndAssertResult(deferred2, 0.5f, 1.5f / 2f);
            progressHelper.ResolveAndAssertResult(deferred2, 1, 2f / 2f);
        }

        [Test]
        public void AllProgressIsNormalized_void3(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();
            var deferred3 = Promise.NewDeferred();
            var deferred4 = Promise.NewDeferred();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            progressHelper.Subscribe(
                Promise.All(
                    deferred1.Promise.Then(() => deferred3.Promise),
                    deferred2.Promise.Then(() => deferred4.Promise)
                )
            )
                .Forget();

            progressHelper.AssertCurrentProgress(0f / 4f);

            progressHelper.ReportProgressAndAssertResult(deferred1, 0.5f, 0.5f / 4f);
            progressHelper.ResolveAndAssertResult(deferred1, 1f / 4f);

            progressHelper.ReportProgressAndAssertResult(deferred3, 0.5f, 1.5f / 4f);
            progressHelper.ResolveAndAssertResult(deferred3, 2f / 4f);

            progressHelper.ReportProgressAndAssertResult(deferred2, 0.5f, 2.5f / 4f);
            progressHelper.ResolveAndAssertResult(deferred2, 3f / 4f);

            progressHelper.ReportProgressAndAssertResult(deferred4, 0.5f, 3.5f / 4f);
            progressHelper.ResolveAndAssertResult(deferred4, 4f / 4f);
        }

        [Test]
        public void AllProgressIsNormalized_T3(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();
            var deferred3 = Promise.NewDeferred<int>();
            var deferred4 = Promise.NewDeferred<int>();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            progressHelper.Subscribe(
                Promise.All(
                    deferred1.Promise.Then(() => deferred3.Promise),
                    deferred2.Promise.Then(() => deferred4.Promise)
                )
            )
                .Forget();

            progressHelper.AssertCurrentProgress(0f / 4f);

            progressHelper.ReportProgressAndAssertResult(deferred1, 0.5f, 0.5f / 4f);
            progressHelper.ResolveAndAssertResult(deferred1, 1, 1f / 4f);

            progressHelper.ReportProgressAndAssertResult(deferred3, 0.5f, 1.5f / 4f);
            progressHelper.ResolveAndAssertResult(deferred3, 1, 2f / 4f);

            progressHelper.ReportProgressAndAssertResult(deferred2, 0.5f, 2.5f / 4f);
            progressHelper.ResolveAndAssertResult(deferred2, 1, 3f / 4f);

            progressHelper.ReportProgressAndAssertResult(deferred4, 0.5f, 3.5f / 4f);
            progressHelper.ResolveAndAssertResult(deferred4, 1, 4f / 4f);
        }

        [Test]
        public void AllProgressIsNormalized_void4(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            progressHelper.Subscribe(
                Promise.All(
                    deferred1.Promise.Then(() => Promise.Resolved()),
                    deferred2.Promise.Then(() => Promise.Resolved())
                )
            )
                .Forget();

            progressHelper.AssertCurrentProgress(0f / 4f);

            progressHelper.ReportProgressAndAssertResult(deferred1, 0.5f, 0.5f / 4f);
            progressHelper.ResolveAndAssertResult(deferred1, 2f / 4f);

            progressHelper.ReportProgressAndAssertResult(deferred2, 0.5f, 2.5f / 4f);
            progressHelper.ResolveAndAssertResult(deferred2, 4f / 4f);
        }

        [Test]
        public void AllProgressIsNormalized_T4(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            progressHelper.Subscribe(
                Promise.All(
                    deferred1.Promise.Then(x => Promise.Resolved(x)),
                    deferred2.Promise.Then(x => Promise.Resolved(x))
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
        public void AllProgressIsNoLongerReportedFromRejected_void(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();
            var deferred3 = Promise.NewDeferred();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            progressHelper.Subscribe(
                Promise.All(deferred1.Promise, deferred2.Promise, deferred3.Promise)
            )
                .Catch(() => { })
                .Forget();

            progressHelper.AssertCurrentProgress(0f / 3f);

            progressHelper.ReportProgressAndAssertResult(deferred1, 0.5f, 0.5f / 3f);
            progressHelper.ResolveAndAssertResult(deferred1, 1f / 3f);

            progressHelper.ReportProgressAndAssertResult(deferred2, 0.5f, 1.5f / 3f);
            progressHelper.RejectAndAssertResult(deferred2, "Reject", 1.5f / 3f, false);

            progressHelper.ReportProgressAndAssertResult(deferred3, 0.5f, 1.5f / 3f, false);
            progressHelper.ResolveAndAssertResult(deferred3, 1.5f / 3f, false);
        }

        [Test]
        public void AllProgressIsNoLongerReportedFromRejected_T(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();
            var deferred3 = Promise.NewDeferred<int>();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            progressHelper.Subscribe(
                Promise.All(deferred1.Promise, deferred2.Promise, deferred3.Promise)
            )
                .Catch(() => { })
                .Forget();

            progressHelper.AssertCurrentProgress(0f / 3f);

            progressHelper.ReportProgressAndAssertResult(deferred1, 0.5f, 0.5f / 3f);
            progressHelper.ResolveAndAssertResult(deferred1, 1, 1f / 3f);

            progressHelper.ReportProgressAndAssertResult(deferred2, 0.5f, 1.5f / 3f);
            progressHelper.RejectAndAssertResult(deferred2, "Reject", 1.5f / 3f, false);

            progressHelper.ReportProgressAndAssertResult(deferred3, 0.5f, 1.5f / 3f, false);
            progressHelper.ResolveAndAssertResult(deferred3, 1, 1.5f / 3f, false);
        }

        [Test]
        public void AllProgressIsNoLongerReportedFromCanceled_void(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred1 = Promise.NewDeferred();
            var cancelationSource = CancelationSource.New();
            var deferred2 = Promise.NewDeferred(cancelationSource.Token);
            var deferred3 = Promise.NewDeferred();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            progressHelper.Subscribe(
                Promise.All(deferred1.Promise, deferred2.Promise, deferred3.Promise)
            )
                .Forget();

            progressHelper.AssertCurrentProgress(0f / 3f);

            progressHelper.ReportProgressAndAssertResult(deferred1, 0.5f, 0.5f / 3f);
            progressHelper.ResolveAndAssertResult(deferred1, 1f / 3f);

            progressHelper.ReportProgressAndAssertResult(deferred2, 0.5f, 1.5f / 3f);
            progressHelper.CancelAndAssertResult(cancelationSource, 1.5f / 3f, false);

            progressHelper.ReportProgressAndAssertResult(deferred3, 0.5f, 1.5f / 3f, false);
            progressHelper.ResolveAndAssertResult(deferred3, 1.5f / 3f, false);

            cancelationSource.Dispose();
        }

        [Test]
        public void AllProgressIsNoLongerReportedFromCanceled_T(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred1 = Promise.NewDeferred<int>();
            var cancelationSource = CancelationSource.New();
            var deferred2 = Promise.NewDeferred<int>(cancelationSource.Token);
            var deferred3 = Promise.NewDeferred<int>();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            progressHelper.Subscribe(
                Promise.All(deferred1.Promise, deferred2.Promise, deferred3.Promise)
            )
                .Forget();

            progressHelper.AssertCurrentProgress(0f / 3f);

            progressHelper.ReportProgressAndAssertResult(deferred1, 0.5f, 0.5f / 3f);
            progressHelper.ResolveAndAssertResult(deferred1, 1, 1f / 3f);

            progressHelper.ReportProgressAndAssertResult(deferred2, 0.5f, 1.5f / 3f);
            progressHelper.CancelAndAssertResult(cancelationSource, 1.5f / 3f, false);

            progressHelper.ReportProgressAndAssertResult(deferred3, 0.5f, 1.5f / 3f, false);
            progressHelper.ResolveAndAssertResult(deferred3, 1, 1.5f / 3f, false);

            cancelationSource.Dispose();
        }

        [Test]
        public void AllProgressWillBeInvokedProperlyFromARecoveredPromise_void(
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
                Promise.All(
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

            progressHelper.AssertCurrentProgress(0f / 6f);

            progressHelper.ReportProgressAndAssertResult(deferred1, 0.5f, 0.5f / 6f);
            progressHelper.ResolveAndAssertResult(deferred1, 3f / 6f);

            progressHelper.ReportProgressAndAssertResult(deferred2, 0.25f, 3.25f / 6f);
            progressHelper.CancelAndAssertResult(cancelationSource, 5f / 6f);

            progressHelper.ReportProgressAndAssertResult(deferred2, 0.5f, 5f / 6f, false);
            progressHelper.ReportProgressAndAssertResult(deferred3, 0.5f, 5f / 6f, false);
            progressHelper.ResolveAndAssertResult(deferred3, 5f / 6f, false);
            progressHelper.ReportProgressAndAssertResult(deferred2, 0.7f, 5f / 6f, false);

            progressHelper.ReportProgressAndAssertResult(deferred4, 0.5f, 5.5f / 6f);
            progressHelper.ResolveAndAssertResult(deferred4, 6f / 6f);

            progressHelper.ReportProgressAndAssertResult(deferred2, 0.8f, 6f / 6f, false);
            progressHelper.ResolveAndAssertResult(deferred2, 6f / 6f, false);

            cancelationSource.Dispose();
            deferred3.Promise.Forget(); // Need to forget this promise because it was never awaited due to the cancelation.
        }

        [Test]
        public void AllProgressWillBeInvokedProperlyFromARecoveredPromise_T(
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
                Promise.All(
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
            progressHelper.ResolveAndAssertResult(deferred3, 1, 5f / 6f, false);
            progressHelper.ReportProgressAndAssertResult(deferred2, 0.7f, 5f / 6f, false);

            progressHelper.ReportProgressAndAssertResult(deferred4, 0.5f, 5.5f / 6f);
            progressHelper.ResolveAndAssertResult(deferred4, 1, 6f / 6f);

            progressHelper.ReportProgressAndAssertResult(deferred2, 0.8f, 6f / 6f, false);
            progressHelper.ResolveAndAssertResult(deferred2, 1, 6f / 6f, false);

            cancelationSource.Dispose();
            deferred3.Promise.Forget(); // Need to forget this promise because it was never awaited due to the cancelation.
        }
#endif // PROMISE_PROGRESS
    }
}