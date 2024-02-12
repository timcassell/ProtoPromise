#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
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
    }
}