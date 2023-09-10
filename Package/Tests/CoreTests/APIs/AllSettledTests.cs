#if !PROTO_PROMISE_PROGRESS_DISABLE
#define PROMISE_PROGRESS
#else
#undef PROMISE_PROGRESS
#endif

using System.Linq;
using Proto.Promises;
using NUnit.Framework;

namespace ProtoPromiseTests.APIs
{
    public class AllSettledTests
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
        public void AllSettledPromiseIsResolvedWhenAllPromisesAreResolved_void()
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();

            bool invoked = false;

            Promise.AllSettled(deferred1.Promise, deferred2.Promise)
                .Then(results =>
                {
                    invoked = true;
                    Assert.AreEqual(2, results.Count);
                    Assert.AreEqual(Promise.State.Resolved, results[0].State);
                    Assert.AreEqual(Promise.State.Resolved, results[1].State);
                })
                .Forget();

            Assert.IsFalse(invoked);

            deferred1.Resolve();
            Assert.IsFalse(invoked);

            deferred2.Resolve();
            Assert.IsTrue(invoked);
        }

        [Test]
        public void AllSettledPromiseIsResolvedWhenAllPromisesAreResolved_T()
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();

            bool invoked = false;

            Promise<int>.AllSettled(deferred1.Promise, deferred2.Promise)
                .Then(results =>
                {
                    invoked = true;
                    Assert.AreEqual(2, results.Count);
                    Assert.AreEqual(10, results[0].Value);
                    Assert.AreEqual(20, results[1].Value);
                })
                .Forget();

            Assert.IsFalse(invoked);

            deferred1.Resolve(10);

            Assert.IsFalse(invoked);

            deferred2.Resolve(20);

            Assert.IsTrue(invoked);
        }

        [Test]
        public void AllSettledPromiseIsResolvedIfThereAreNoPromises_void()
        {
            bool invoked = false;

            Promise.AllSettled(Enumerable.Empty<Promise>())
                .Then(v =>
                {
                    invoked = true;
                    Assert.IsEmpty(v);
                })
                .Forget();

            Assert.IsTrue(invoked);
        }

        [Test]
        public void AllSettledPromiseIsResolvedIfThereAreNoPromises_T()
        {
            bool invoked = false;

            Promise<int>.AllSettled(Enumerable.Empty<Promise<int>>())
                .Then(v =>
                {
                    invoked = true;
                    Assert.IsEmpty(v);
                })
                .Forget();

            Assert.IsTrue(invoked);
        }


        [Test]
        public void AllSettledPromiseIsResolvedWhenAllPromisesAreAlreadyResolved_void()
        {
            bool invoked = false;

            Promise.AllSettled(Promise.Resolved(), Promise.Resolved())
                .Then(results =>
                {
                    invoked = true;
                    Assert.AreEqual(2, results.Count);
                    Assert.AreEqual(Promise.State.Resolved, results[0].State);
                    Assert.AreEqual(Promise.State.Resolved, results[1].State);
                })
                .Forget();

            Assert.IsTrue(invoked);
        }

        [Test]
        public void AllSettledPromiseIsResolvedWhenAllPromisesAreAlreadyResolved_T()
        {
            bool invoked = false;

            Promise<int>.AllSettled(Promise.Resolved(1), Promise.Resolved(2))
                .Then(results =>
                {
                    invoked = true;
                    Assert.AreEqual(2, results.Count);
                    Assert.AreEqual(Promise.State.Resolved, results[0].State);
                    Assert.AreEqual(Promise.State.Resolved, results[1].State);
                    Assert.AreEqual(1, results[0].Value);
                    Assert.AreEqual(2, results[1].Value);
                })
                .Forget();

            Assert.IsTrue(invoked);
        }

        [Test]
        public void AllSettledPromiseIsPendingWhenFirstPromiseIsRejected_void()
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();

            bool invoked = false;
            string reason = "reject";

            Promise.AllSettled(deferred1.Promise, deferred2.Promise)
                .Then(results =>
                {
                    invoked = true;
                    Assert.AreEqual(2, results.Count);
                    Assert.AreEqual(Promise.State.Rejected, results[0].State);
                    Assert.AreEqual(Promise.State.Resolved, results[1].State);
                    Assert.AreEqual(reason, results[0].Reason);
                })
                .Forget();

            deferred1.Reject(reason);

            Assert.IsFalse(invoked);

            deferred2.Resolve();

            Assert.IsTrue(invoked);
        }

        [Test]
        public void AllSettledPromiseIsPendingWhenFirstPromiseIsRejected_T()
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();

            bool invoked = false;
            string reason = "reject";

            Promise<int>.AllSettled(deferred1.Promise, deferred2.Promise)
                .Then(results =>
                {
                    invoked = true;
                    Assert.AreEqual(2, results.Count);
                    Assert.AreEqual(Promise.State.Rejected, results[0].State);
                    Assert.AreEqual(Promise.State.Resolved, results[1].State);
                    Assert.AreEqual(reason, results[0].Reason);
                    Assert.AreEqual(2, results[1].Value);
                })
                .Forget();

            deferred1.Reject(reason);

            Assert.IsFalse(invoked);

            deferred2.Resolve(2);

            Assert.IsTrue(invoked);
        }

        [Test]
        public void AllSettledPromiseIsResolvedWhenSecondPromiseIsRejected_void()
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();

            bool invoked = false;
            string reason = "reject";

            Promise.AllSettled(deferred1.Promise, deferred2.Promise)
                .Then(results =>
                {
                    invoked = true;
                    Assert.AreEqual(2, results.Count);
                    Assert.AreEqual(Promise.State.Resolved, results[0].State);
                    Assert.AreEqual(Promise.State.Rejected, results[1].State);
                    Assert.AreEqual(reason, results[1].Reason);
                })
                .Forget();

            deferred1.Resolve();

            Assert.IsFalse(invoked);

            deferred2.Reject(reason);

            Assert.IsTrue(invoked);
        }

        [Test]
        public void AllSettledPromiseIsResolvedWhenSecondPromiseIsRejected_T()
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();

            bool invoked = false;
            string reason = "reject";

            Promise<int>.AllSettled(deferred1.Promise, deferred2.Promise)
                .Then(results =>
                {
                    invoked = true;
                    Assert.AreEqual(2, results.Count);
                    Assert.AreEqual(Promise.State.Resolved, results[0].State);
                    Assert.AreEqual(Promise.State.Rejected, results[1].State);
                    Assert.AreEqual(2, results[0].Value);
                    Assert.AreEqual(reason, results[1].Reason);
                })
                .Forget();

            deferred1.Resolve(2);

            Assert.IsFalse(invoked);

            deferred2.Reject(reason);

            Assert.IsTrue(invoked);
        }

        [Test]
        public void AllSettledPromiseIsResolvedWhenBothPromisesAreRejected_void()
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();

            bool invoked = false;
            string reason = "reject";

            Promise.AllSettled(deferred1.Promise, deferred2.Promise)
                .Then(results =>
                {
                    invoked = true;
                    Assert.AreEqual(2, results.Count);
                    Assert.AreEqual(Promise.State.Rejected, results[0].State);
                    Assert.AreEqual(Promise.State.Rejected, results[1].State);
                    Assert.AreEqual(reason, results[0].Reason);
                    Assert.AreEqual(reason, results[1].Reason);
                })
                .Forget();

            deferred1.Reject(reason);

            Assert.IsFalse(invoked);

            deferred2.Reject(reason);

            Assert.IsTrue(invoked);
        }

        [Test]
        public void AllSettledPromiseIsResolvedWhenBothPromisesAreRejected_T()
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();

            bool invoked = false;
            string reason = "reject";

            Promise<int>.AllSettled(deferred1.Promise, deferred2.Promise)
                .Then(results =>
                {
                    invoked = true;
                    Assert.AreEqual(2, results.Count);
                    Assert.AreEqual(Promise.State.Rejected, results[0].State);
                    Assert.AreEqual(Promise.State.Rejected, results[1].State);
                    Assert.AreEqual(reason, results[0].Reason);
                    Assert.AreEqual(reason, results[1].Reason);
                })
                .Forget();

            deferred1.Reject(reason);

            Assert.IsFalse(invoked);

            deferred2.Reject(reason);

            Assert.IsTrue(invoked);
        }

        [Test]
        public void AllSettledPromiseIsPendingWhenAnyPromiseIsAlreadyRejected_void()
        {
            int invokeCount = 0;
            string reason = "reject";

            var deferred = Promise.NewDeferred();
            var promise1 = deferred.Promise.Preserve();
            var promise2 = Promise.Rejected(reason).Preserve();

            Promise.AllSettled(promise1, promise2)
                .Finally(() => ++invokeCount)
                .Then(results =>
                {
                    Assert.AreEqual(2, results.Count);
                    Assert.AreEqual(Promise.State.Resolved, results[0].State);
                    Assert.AreEqual(Promise.State.Rejected, results[1].State);
                    Assert.AreEqual(reason, results[1].Reason);
                })
                .Forget();

            Promise.AllSettled(promise2, promise1)
                .Finally(() => ++invokeCount)
                .Then(results =>
                {
                    Assert.AreEqual(2, results.Count);
                    Assert.AreEqual(Promise.State.Rejected, results[0].State);
                    Assert.AreEqual(Promise.State.Resolved, results[1].State);
                    Assert.AreEqual(reason, results[0].Reason);
                })
                .Forget();

            Assert.AreEqual(0, invokeCount);

            deferred.Resolve();

            Assert.AreEqual(2, invokeCount);

            promise1.Forget();
            promise2.Forget();
        }

        [Test]
        public void AllSettledPromiseIsPendingWhenAnyPromiseIsAlreadyRejected_T()
        {
            int invokeCount = 0;
            string reason = "reject";

            var deferred = Promise.NewDeferred<int>();
            var promise1 = deferred.Promise.Preserve();
            var promise2 = Promise<int>.Rejected(reason).Preserve();

            Promise<int>.AllSettled(promise1, promise2)
                .Finally(() => ++invokeCount)
                .Then(results =>
                {
                    Assert.AreEqual(2, results.Count);
                    Assert.AreEqual(Promise.State.Resolved, results[0].State);
                    Assert.AreEqual(Promise.State.Rejected, results[1].State);
                    Assert.AreEqual(1, results[0].Value);
                    Assert.AreEqual(reason, results[1].Reason);
                })
                .Forget();

            Promise<int>.AllSettled(promise2, promise1)
                .Finally(() => ++invokeCount)
                .Then(results =>
                {
                    Assert.AreEqual(2, results.Count);
                    Assert.AreEqual(Promise.State.Rejected, results[0].State);
                    Assert.AreEqual(Promise.State.Resolved, results[1].State);
                    Assert.AreEqual(1, results[1].Value);
                    Assert.AreEqual(reason, results[0].Reason);
                })
                .Forget();

            Assert.AreEqual(0, invokeCount);

            deferred.Resolve(1);

            Assert.AreEqual(2, invokeCount);

            promise1.Forget();
            promise2.Forget();
        }

        [Test]
        public void AllSettledPromiseIsPendingWhenFirstPromiseIsCanceled_void()
        {
            CancelationSource cancelationSource = CancelationSource.New();

            var deferred1 = Promise.NewDeferred();
            cancelationSource.Token.Register(deferred1);
            var deferred2 = Promise.NewDeferred();

            bool invoked = false;

            Promise.AllSettled(deferred1.Promise, deferred2.Promise)
                .Then(results =>
                {
                    invoked = true;
                    Assert.AreEqual(2, results.Count);
                    Assert.AreEqual(Promise.State.Canceled, results[0].State);
                    Assert.AreEqual(Promise.State.Resolved, results[1].State);
                })
                .Forget();

            cancelationSource.Cancel();

            Assert.IsFalse(invoked);

            deferred2.Resolve();

            Assert.IsTrue(invoked);

            cancelationSource.Dispose();
        }

        [Test]
        public void AllSettledPromiseIsPendingWhenFirstPromiseIsCanceled_T()
        {
            CancelationSource cancelationSource = CancelationSource.New();

            var deferred1 = Promise.NewDeferred<int>();
            cancelationSource.Token.Register(deferred1);
            var deferred2 = Promise.NewDeferred<int>();

            bool invoked = false;

            Promise<int>.AllSettled(deferred1.Promise, deferred2.Promise)
                .Then(results =>
                {
                    invoked = true;
                    Assert.AreEqual(2, results.Count);
                    Assert.AreEqual(Promise.State.Canceled, results[0].State);
                    Assert.AreEqual(Promise.State.Resolved, results[1].State);
                    Assert.AreEqual(2, results[1].Value);
                })
                .Forget();

            cancelationSource.Cancel();

            Assert.IsFalse(invoked);

            deferred2.Resolve(2);

            Assert.IsTrue(invoked);

            cancelationSource.Dispose();
        }

        [Test]
        public void AllSettledPromiseIsResolvedWhenSecondPromiseIsCanceled_void()
        {
            var deferred1 = Promise.NewDeferred();
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred2 = Promise.NewDeferred();
            cancelationSource.Token.Register(deferred2);

            bool invoked = false;

            Promise.AllSettled(deferred1.Promise, deferred2.Promise)
                .Then(results =>
                {
                    invoked = true;
                    Assert.AreEqual(2, results.Count);
                    Assert.AreEqual(Promise.State.Resolved, results[0].State);
                    Assert.AreEqual(Promise.State.Canceled, results[1].State);
                })
                .Forget();

            deferred1.Resolve();

            Assert.IsFalse(invoked);

            cancelationSource.Cancel();

            Assert.IsTrue(invoked);

            cancelationSource.Dispose();
        }

        [Test]
        public void AllSettledPromiseIsResolvedWhenSecondPromiseIsCanceled_T()
        {
            var deferred1 = Promise.NewDeferred<int>();
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred2 = Promise.NewDeferred<int>();
            cancelationSource.Token.Register(deferred2);

            bool invoked = false;

            Promise<int>.AllSettled(deferred1.Promise, deferred2.Promise)
                .Then(results =>
                {
                    invoked = true;
                    Assert.AreEqual(2, results.Count);
                    Assert.AreEqual(Promise.State.Resolved, results[0].State);
                    Assert.AreEqual(Promise.State.Canceled, results[1].State);
                    Assert.AreEqual(2, results[0].Value);
                })
                .Forget();

            deferred1.Resolve(2);

            Assert.IsFalse(invoked);

            cancelationSource.Cancel();

            Assert.IsTrue(invoked);

            cancelationSource.Dispose();
        }

        [Test]
        public void AllSettledPromiseIsResolvedWhenBothPromisesAreCanceled_void()
        {
            CancelationSource cancelationSource1 = CancelationSource.New();
            CancelationSource cancelationSource2 = CancelationSource.New();

            var deferred1 = Promise.NewDeferred();
            cancelationSource1.Token.Register(deferred1);
            var deferred2 = Promise.NewDeferred();
            cancelationSource2.Token.Register(deferred2);

            bool invoked = false;

            Promise.AllSettled(deferred1.Promise, deferred2.Promise)
                .Then(results =>
                {
                    invoked = true;
                    Assert.AreEqual(2, results.Count);
                    Assert.AreEqual(Promise.State.Canceled, results[0].State);
                    Assert.AreEqual(Promise.State.Canceled, results[1].State);
                })
                .Forget();

            cancelationSource1.Cancel();

            Assert.IsFalse(invoked);

            cancelationSource2.Cancel();

            Assert.IsTrue(invoked);

            cancelationSource1.Dispose();
            cancelationSource2.Dispose();
        }

        [Test]
        public void AllSettledPromiseIsResolvedWhenBothPromisesAreCanceled_T()
        {
            CancelationSource cancelationSource1 = CancelationSource.New();
            CancelationSource cancelationSource2 = CancelationSource.New();

            var deferred1 = Promise.NewDeferred<int>();
            cancelationSource1.Token.Register(deferred1);
            var deferred2 = Promise.NewDeferred<int>();
            cancelationSource2.Token.Register(deferred2);

            bool invoked = false;

            Promise<int>.AllSettled(deferred1.Promise, deferred2.Promise)
                .Then(results =>
                {
                    invoked = true;
                    Assert.AreEqual(2, results.Count);
                    Assert.AreEqual(Promise.State.Canceled, results[0].State);
                    Assert.AreEqual(Promise.State.Canceled, results[1].State);
                })
                .Forget();

            cancelationSource1.Cancel();

            Assert.IsFalse(invoked);

            cancelationSource2.Cancel();

            Assert.IsTrue(invoked);

            cancelationSource1.Dispose();
            cancelationSource2.Dispose();
        }

        [Test]
        public void AllSettledPromiseIsPendingWhenAnyPromiseIsAlreadyCanceled_void()
        {
            int invokeCount = 0;

            var deferred = Promise.NewDeferred();
            var promise1 = deferred.Promise.Preserve();
            var promise2 = Promise.Canceled().Preserve();

            Promise.AllSettled(promise1, promise2)
                .Then(results =>
                {
                    ++invokeCount;
                    Assert.AreEqual(2, results.Count);
                    Assert.AreEqual(Promise.State.Resolved, results[0].State);
                    Assert.AreEqual(Promise.State.Canceled, results[1].State);
                })
                .Forget();

            Promise.AllSettled(promise2, promise1)
                .Then(results =>
                {
                    ++invokeCount;
                    Assert.AreEqual(2, results.Count);
                    Assert.AreEqual(Promise.State.Canceled, results[0].State);
                    Assert.AreEqual(Promise.State.Resolved, results[1].State);
                })
                .Forget();

            Assert.AreEqual(0, invokeCount);

            deferred.Resolve();

            Assert.AreEqual(2, invokeCount);

            promise1.Forget();
            promise2.Forget();
        }

        [Test]
        public void AllSettledPromiseIsPendingWhenAnyPromiseIsAlreadyCanceled_T()
        {
            int invokeCount = 0;

            var deferred = Promise.NewDeferred<int>();
            var promise1 = deferred.Promise.Preserve();
            var promise2 = Promise<int>.Canceled().Preserve();

            Promise<int>.AllSettled(promise1, promise2)
                .Then(results =>
                {
                    ++invokeCount;
                    Assert.AreEqual(2, results.Count);
                    Assert.AreEqual(Promise.State.Resolved, results[0].State);
                    Assert.AreEqual(Promise.State.Canceled, results[1].State);
                    Assert.AreEqual(1, results[0].Value);
                })
                .Forget();

            Promise<int>.AllSettled(promise2, promise1)
                .Then(results =>
                {
                    ++invokeCount;
                    Assert.AreEqual(2, results.Count);
                    Assert.AreEqual(Promise.State.Canceled, results[0].State);
                    Assert.AreEqual(Promise.State.Resolved, results[1].State);
                    Assert.AreEqual(1, results[1].Value);
                })
                .Forget();

            Assert.AreEqual(0, invokeCount);

            deferred.Resolve(1);

            Assert.AreEqual(2, invokeCount);

            promise1.Forget();
            promise2.Forget();
        }

#if PROMISE_PROGRESS
        [Test]
        public void AllSettledProgressIsNormalized_void0(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();
            var deferred3 = Promise.NewDeferred();
            var deferred4 = Promise.NewDeferred();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            Promise.AllSettled(deferred1.Promise, deferred2.Promise, deferred3.Promise, deferred4.Promise)
                .SubscribeProgressAndAssert(progressHelper, 0f / 4f)
                .Forget();

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
        public void AllSettledProgressIsNormalized_T0(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();
            var deferred3 = Promise.NewDeferred<int>();
            var deferred4 = Promise.NewDeferred<int>();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            Promise<int>.AllSettled(deferred1.Promise, deferred2.Promise, deferred3.Promise, deferred4.Promise)
                .SubscribeProgressAndAssert(progressHelper, 0f / 4f)
                .Forget();

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
        public void AllSettledProgressIsNormalized_void1(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred1 = Promise.NewDeferred();
            var deferred3 = Promise.NewDeferred();
            var deferred4 = Promise.NewDeferred();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            Promise.AllSettled(deferred1.Promise, Promise.Resolved(), deferred3.Promise, deferred4.Promise)
                .SubscribeProgressAndAssert(progressHelper, 1f / 4f)
                .Forget();

            progressHelper.ReportProgressAndAssertResult(deferred1, 0.5f, 1.5f / 4f);
            progressHelper.ResolveAndAssertResult(deferred1, 2f / 4f);

            progressHelper.ReportProgressAndAssertResult(deferred3, 0.5f, 2.5f / 4f);
            progressHelper.ResolveAndAssertResult(deferred3, 3f / 4f);

            progressHelper.ReportProgressAndAssertResult(deferred4, 0.5f, 3.5f / 4f);
            progressHelper.ResolveAndAssertResult(deferred4, 4f / 4f);
        }

        [Test]
        public void AllSettledProgressIsNormalized_T1(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred3 = Promise.NewDeferred<int>();
            var deferred4 = Promise.NewDeferred<int>();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            Promise<int>.AllSettled(deferred1.Promise, Promise.Resolved(1), deferred3.Promise, deferred4.Promise)
                .SubscribeProgressAndAssert(progressHelper, 1f / 4f)
                .Forget();

            progressHelper.ReportProgressAndAssertResult(deferred1, 0.5f, 1.5f / 4f);
            progressHelper.ResolveAndAssertResult(deferred1, 1, 2f / 4f);

            progressHelper.ReportProgressAndAssertResult(deferred3, 0.5f, 2.5f / 4f);
            progressHelper.ResolveAndAssertResult(deferred3, 1, 3f / 4f);

            progressHelper.ReportProgressAndAssertResult(deferred4, 0.5f, 3.5f / 4f);
            progressHelper.ResolveAndAssertResult(deferred4, 1, 4f / 4f);
        }

        [Test]
        public void AllSettledProgressIsNormalized_void2(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            Promise.AllSettled(deferred1.Promise.ThenDuplicate(), deferred2.Promise.ThenDuplicate())
                .SubscribeProgressAndAssert(progressHelper, 0f / 2f)
                .Forget();

            progressHelper.ReportProgressAndAssertResult(deferred1, 0.5f, 0.5f / 2f);
            progressHelper.ResolveAndAssertResult(deferred1, 1f / 2f);

            progressHelper.ReportProgressAndAssertResult(deferred2, 0.5f, 1.5f / 2f);
            progressHelper.ResolveAndAssertResult(deferred2, 2f / 2f);
        }

        [Test]
        public void AllSettledProgressIsNormalized_T2(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            Promise<int>.AllSettled(deferred1.Promise.ThenDuplicate(), deferred2.Promise.ThenDuplicate())
                .SubscribeProgressAndAssert(progressHelper, 0f / 2f)
                .Forget();

            progressHelper.ReportProgressAndAssertResult(deferred1, 0.5f, 0.5f / 2f);
            progressHelper.ResolveAndAssertResult(deferred1, 1, 1f / 2f);

            progressHelper.ReportProgressAndAssertResult(deferred2, 0.5f, 1.5f / 2f);
            progressHelper.ResolveAndAssertResult(deferred2, 1, 2f / 2f);
        }

        [Test]
        public void AllSettledProgressIsNormalized_void3(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();
            var deferred3 = Promise.NewDeferred();
            var deferred4 = Promise.NewDeferred();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            Promise.AllSettled(
                deferred1.Promise.Then(() => deferred3.Promise),
                deferred2.Promise.Then(() => deferred4.Promise)
            )
                .SubscribeProgressAndAssert(progressHelper, 0f / 4f)
                .Forget();

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
        public void AllSettledProgressIsNormalized_T3(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();
            var deferred3 = Promise.NewDeferred<int>();
            var deferred4 = Promise.NewDeferred<int>();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            Promise<int>.AllSettled(
                deferred1.Promise.Then(v => deferred3.Promise),
                deferred2.Promise.Then(v => deferred4.Promise)
            )
                .SubscribeProgressAndAssert(progressHelper, 0f / 4f)
                .Forget();

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
        public void AllSettledProgressIsNormalized_void4(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            Promise.AllSettled(
                deferred1.Promise.Then(() => Promise.Resolved()),
                deferred2.Promise.Then(() => Promise.Resolved())
            )
                .SubscribeProgressAndAssert(progressHelper, 0f / 4f)
                .Forget();

            progressHelper.ReportProgressAndAssertResult(deferred1, 0.5f, 0.5f / 4f);
            progressHelper.ResolveAndAssertResult(deferred1, 2f / 4f);

            progressHelper.ReportProgressAndAssertResult(deferred2, 0.5f, 2.5f / 4f);
            progressHelper.ResolveAndAssertResult(deferred2, 4f / 4f);
        }

        [Test]
        public void AllSettledProgressIsNormalized_T4(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            Promise<int>.AllSettled(
                deferred1.Promise.Then(x => Promise.Resolved(x)),
                deferred2.Promise.Then(x => Promise.Resolved(x))
            )
                .SubscribeProgressAndAssert(progressHelper, 0f / 4f)
                .Forget();

            progressHelper.ReportProgressAndAssertResult(deferred1, 0.5f, 0.5f / 4f);
            progressHelper.ResolveAndAssertResult(deferred1, 1, 2f / 4f);

            progressHelper.ReportProgressAndAssertResult(deferred2, 0.5f, 2.5f / 4f);
            progressHelper.ResolveAndAssertResult(deferred2, 1, 4f / 4f);
        }

        [Test]
        public void AllSettledProgressIsReportedFromRejected_void(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();
            var deferred3 = Promise.NewDeferred();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            Promise.AllSettled(deferred1.Promise, deferred2.Promise, deferred3.Promise)
                .SubscribeProgressAndAssert(progressHelper, 0f / 3f)
                .Forget();

            progressHelper.ReportProgressAndAssertResult(deferred1, 0.5f, 0.5f / 3f);
            progressHelper.ResolveAndAssertResult(deferred1, 1f / 3f);

            progressHelper.ReportProgressAndAssertResult(deferred2, 0.5f, 1.5f / 3f);
            progressHelper.RejectAndAssertResult(deferred2, "Reject", 2f / 3f);

            progressHelper.ReportProgressAndAssertResult(deferred3, 0.5f, 2.5f / 3f);
            progressHelper.ResolveAndAssertResult(deferred3, 3f / 3f);
        }

        [Test]
        public void AllSettledProgressIsReportedFromRejected_T(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();
            var deferred3 = Promise.NewDeferred<int>();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            Promise<int>.AllSettled(deferred1.Promise, deferred2.Promise, deferred3.Promise)
                .SubscribeProgressAndAssert(progressHelper, 0f / 3f)
                .Catch(() => { })
                .Forget();

            progressHelper.ReportProgressAndAssertResult(deferred1, 0.5f, 0.5f / 3f);
            progressHelper.ResolveAndAssertResult(deferred1, 1, 1f / 3f);

            progressHelper.ReportProgressAndAssertResult(deferred2, 0.5f, 1.5f / 3f);
            progressHelper.RejectAndAssertResult(deferred2, "Reject", 2f / 3f);

            progressHelper.ReportProgressAndAssertResult(deferred3, 0.5f, 2.5f / 3f);
            progressHelper.ResolveAndAssertResult(deferred3, 1, 3f / 3f);
        }

        [Test]
        public void AllSettledProgressIsReportedFromCanceled_void(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred1 = Promise.NewDeferred();
            var cancelationSource = CancelationSource.New();
            var deferred2 = Promise.NewDeferred();
            cancelationSource.Token.Register(deferred2);
            var deferred3 = Promise.NewDeferred();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            Promise.AllSettled(deferred1.Promise, deferred2.Promise, deferred3.Promise)
                .SubscribeProgressAndAssert(progressHelper, 0f / 3f)
                .Forget();

            progressHelper.ReportProgressAndAssertResult(deferred1, 0.5f, 0.5f / 3f);
            progressHelper.ResolveAndAssertResult(deferred1, 1f / 3f);

            progressHelper.ReportProgressAndAssertResult(deferred2, 0.5f, 1.5f / 3f);
            progressHelper.CancelAndAssertResult(cancelationSource, 2f / 3f);

            progressHelper.ReportProgressAndAssertResult(deferred3, 0.5f, 2.5f / 3f);
            progressHelper.ResolveAndAssertResult(deferred3, 3f / 3f);

            cancelationSource.Dispose();
        }

        [Test]
        public void AllSettledProgressIsReportedFromCanceled_T(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred1 = Promise.NewDeferred<int>();
            var cancelationSource = CancelationSource.New();
            var deferred2 = Promise.NewDeferred<int>();
            cancelationSource.Token.Register(deferred2);
            var deferred3 = Promise.NewDeferred<int>();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            Promise<int>.AllSettled(deferred1.Promise, deferred2.Promise, deferred3.Promise)
                .SubscribeProgressAndAssert(progressHelper, 0f / 3f)
                .Forget();

            progressHelper.ReportProgressAndAssertResult(deferred1, 0.5f, 0.5f / 3f);
            progressHelper.ResolveAndAssertResult(deferred1, 1, 1f / 3f);

            progressHelper.ReportProgressAndAssertResult(deferred2, 0.5f, 1.5f / 3f);
            progressHelper.CancelAndAssertResult(cancelationSource, 2f / 3f);

            progressHelper.ReportProgressAndAssertResult(deferred3, 0.5f, 2.5f / 3f);
            progressHelper.ResolveAndAssertResult(deferred3, 1, 3f / 3f);

            cancelationSource.Dispose();
        }

        [Test]
        public void AllSettledProgressWillBeInvokedProperlyFromARecoveredPromise_void(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();
            var deferred3 = Promise.NewDeferred();
            var deferred4 = Promise.NewDeferred();
            var cancelationSource = CancelationSource.New();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            Promise.AllSettled(
                // Make first and second promise chains the same length
                deferred1.Promise
                    .Then(() => Promise.Resolved())
                    .Then(() => Promise.Resolved()),
                deferred2.Promise
                    .Then(() => deferred3.Promise, cancelationSource.Token)
                    .ContinueWith(_ => deferred4.Promise)
            )
                .SubscribeProgressAndAssert(progressHelper, 0f / 6f)
                .Forget();

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
        public void AllSettledProgressWillBeInvokedProperlyFromARecoveredPromise_T(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();
            var deferred3 = Promise.NewDeferred<int>();
            var deferred4 = Promise.NewDeferred<int>();
            var cancelationSource = CancelationSource.New();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            Promise<int>.AllSettled(
                // Make first and second promise chains the same length
                deferred1.Promise
                    .Then(x => Promise.Resolved(x))
                    .Then(x => Promise.Resolved(x)),
                deferred2.Promise
                    .Then(() => deferred3.Promise, cancelationSource.Token)
                    .ContinueWith(_ => deferred4.Promise)
            )
                .SubscribeProgressAndAssert(progressHelper, 0f / 6f)
                .Forget();

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

        [Test]
        public void AllSettledProgressWillBeInvokedProperlyFromChainedPromise_FlatDepth_void([Values] bool isPending)
        {
            // Testing an implementation detail, not guaranteed by the API - Promise.AllSettled's depth is set to the longest promise chain's depth.
            // We test if all promises are already resolved to make sure progress reports remain consistent.
            var maybePendingDeferred = isPending
                ? Promise.NewDeferred()
                : default(Promise.Deferred);

            // .Then waiting on another promise increases the depth of the promise chain from 0 to 1.
            var promise1 = (isPending ? maybePendingDeferred.Promise : Promise.Resolved())
                .Then(() => Promise.Resolved());
            var promise2 = Promise.Resolved()
                .Then(() => Promise.Resolved());
            var promise3 = Promise.Resolved()
                .Then(() => Promise.Resolved());
            var promise4 = Promise.Resolved()
                .Then(() => Promise.Resolved());

            const float initialCompletedProgress = 3f / 4f;
            const float expectedCompletedProgress = initialCompletedProgress * 2f / 3f;

            var deferredForProgress = Promise.NewDeferred();

            ProgressHelper progressHelper = new ProgressHelper(ProgressType.Interface, SynchronizationType.Synchronous);
            Promise.AllSettled(promise1, promise2, promise3, promise4)
                .Then(() => deferredForProgress.Promise) // Increases the depth to 2.
                .SubscribeProgressAndAssert(progressHelper, isPending ? expectedCompletedProgress : 2f / 3f)
                .Forget();

            maybePendingDeferred.TryResolve();

            progressHelper.AssertCurrentProgress(2f / 3f);

            progressHelper.ReportProgressAndAssertResult(deferredForProgress, 0.5f, 2.5f / 3f);
            progressHelper.ResolveAndAssertResult(deferredForProgress, 3f / 3f);
        }

        [Test]
        public void AllSettledProgressWillBeInvokedProperlyFromChainedPromise_FlatDepth_T([Values] bool isPending)
        {
            // Testing an implementation detail, not guaranteed by the API - Promise.AllSettled's depth is set to the longest promise chain's depth.
            // We test if all promises are already resolved to make sure progress reports remain consistent.
            var maybePendingDeferred = isPending
                ? Promise.NewDeferred()
                : default(Promise.Deferred);
            
            // .Then waiting on another promise increases the depth of the promise chain from 0 to 1.
            var promise1 = (isPending ? maybePendingDeferred.Promise : Promise.Resolved())
                .Then(() => Promise.Resolved(1));
            var promise2 = Promise.Resolved()
                .Then(() => Promise.Resolved(2));
            var promise3 = Promise.Resolved()
                .Then(() => Promise.Resolved(3));
            var promise4 = Promise.Resolved()
                .Then(() => Promise.Resolved(4));

            const float initialCompletedProgress = 3f / 4f;
            const float expectedCompletedProgress = initialCompletedProgress * 2f / 3f;

            var deferredForProgress = Promise.NewDeferred();

            ProgressHelper progressHelper = new ProgressHelper(ProgressType.Interface, SynchronizationType.Synchronous);
            Promise<int>.AllSettled(promise1, promise2, promise3, promise4)
                .Then(v => deferredForProgress.Promise) // Increases the depth to 1.
                .SubscribeProgressAndAssert(progressHelper, isPending ? expectedCompletedProgress : 2f / 3f)
                .Forget();

            maybePendingDeferred.TryResolve();

            progressHelper.AssertCurrentProgress(2f / 3f);

            progressHelper.ReportProgressAndAssertResult(deferredForProgress, 0.5f, 2.5f / 3f);
            progressHelper.ResolveAndAssertResult(deferredForProgress, 3f / 3f);
        }

        [Test]
        public void AllSettledProgressWillBeInvokedProperlyFromChainedPromise_StaggeredDepth_void([Values] bool isPending)
        {
            // Testing an implementation detail, not guaranteed by the API - Promise.AllSettled's depth is set to the longest promise chain's depth.
            // We test if all promises are already resolved to make sure progress reports remain consistent.
            var maybePendingDeferred = isPending
                ? Promise.NewDeferred()
                : default(Promise.Deferred);
            
            // .Then waiting on another promise increases the depth of the promise chain from 0 to 1.
            var promise1 = (isPending ? maybePendingDeferred.Promise : Promise.Resolved())
                .Then(() => Promise.Resolved());
            var promise2 = Promise.Resolved();
            var promise3 = Promise.Resolved();
            var promise4 = Promise.Resolved();

            // Implementation detail - progress isn't divided evenly for each promise, their weights are based on their depth.
            const float initialCompletedProgress = 3f / 5f;
            const float expectedCompletedProgress = initialCompletedProgress * 2f / 3f;

            var deferredForProgress = Promise.NewDeferred();

            ProgressHelper progressHelper = new ProgressHelper(ProgressType.Interface, SynchronizationType.Synchronous);
            Promise.AllSettled(promise1, promise2, promise3, promise4)
                .Then(() => deferredForProgress.Promise) // Increases the depth to 2.
                .SubscribeProgressAndAssert(progressHelper, isPending ? expectedCompletedProgress : 2f / 3f)
                .Forget();

            maybePendingDeferred.TryResolve();

            progressHelper.AssertCurrentProgress(2f / 3f);

            progressHelper.ReportProgressAndAssertResult(deferredForProgress, 0.5f, 2.5f / 3f);
            progressHelper.ResolveAndAssertResult(deferredForProgress, 3f / 3f);
        }

        [Test]
        public void AllSettledProgressWillBeInvokedProperlyFromChainedPromise_StaggeredDepth_T([Values] bool isPending)
        {
            // Testing an implementation detail, not guaranteed by the API - Promise.AllSettled's depth is set to the longest promise chain's depth.
            // We test if all promises are already resolved to make sure progress reports remain consistent.
            var maybePendingDeferred = isPending
                ? Promise.NewDeferred()
                : default(Promise.Deferred);
            
            // .Then waiting on another promise increases the depth of the promise chain from 0 to 1.
            var promise1 = (isPending ? maybePendingDeferred.Promise : Promise.Resolved())
                .Then(() => Promise.Resolved(1));
            var promise2 = Promise.Resolved(2);
            var promise3 = Promise.Resolved(3);
            var promise4 = Promise.Resolved(4);

            // Implementation detail - progress isn't divided evenly for each promise, their weights are based on their depth.
            const float initialCompletedProgress = 3f / 5f;
            const float expectedCompletedProgress = initialCompletedProgress * 2f / 3f;

            var deferredForProgress = Promise.NewDeferred();

            ProgressHelper progressHelper = new ProgressHelper(ProgressType.Interface, SynchronizationType.Synchronous);
            Promise<int>.AllSettled(promise1, promise2, promise3, promise4)
                .Then(v => deferredForProgress.Promise) // Increases the depth to 2.
                .SubscribeProgressAndAssert(progressHelper, isPending ? expectedCompletedProgress : 2f / 3f)
                .Forget();

            maybePendingDeferred.TryResolve();

            progressHelper.AssertCurrentProgress(2f / 3f);

            progressHelper.ReportProgressAndAssertResult(deferredForProgress, 0.5f, 2.5f / 3f);
            progressHelper.ResolveAndAssertResult(deferredForProgress, 3f / 3f);
        }
#endif // PROMISE_PROGRESS
    }
}