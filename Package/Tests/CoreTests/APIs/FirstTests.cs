#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
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
        public void FirstIsResolvedWhenFirstPromiseIsResolvedFirst_void(
            [Values] bool alreadyResolved)
        {
            var resolvedPromise1 = TestHelper.BuildPromise(CompleteType.Resolve, alreadyResolved, "Error", out var deferred1);
            var resolvedPromise2 = TestHelper.BuildPromise(CompleteType.Resolve, alreadyResolved, "Error", out var deferred2);

            bool resolved = false;

            Promise.First(resolvedPromise1, resolvedPromise2)
                .Then(() =>
                {
                    resolved = true;
                })
                .Forget();

            deferred1.TryResolve();

            Assert.IsTrue(resolved);

            deferred2.TryResolve();

            Assert.IsTrue(resolved);
        }

        [Test]
        public void FirstIsResolvedWhenFirstPromiseIsResolvedFirst_T(
            [Values] bool alreadyResolved)
        {
            var resolvedPromise1 = TestHelper.BuildPromise(CompleteType.Resolve, alreadyResolved, 5, "Error", out var deferred1);
            var resolvedPromise2 = TestHelper.BuildPromise(CompleteType.Resolve, alreadyResolved, 2, "Error", out var deferred2);

            bool resolved = false;

            Promise<int>.First(resolvedPromise1, resolvedPromise2)
                .Then(i =>
                {
                    Assert.AreEqual(5, i);
                    resolved = true;
                })
                .Forget();

            deferred1.TryResolve(5);

            Assert.IsTrue(resolved);

            deferred2.TryResolve(1);

            Assert.IsTrue(resolved);
        }

        [Test]
        public void FirstIsResolvedWhenSecondPromiseIsResolvedFirst_void(
            [Values] bool alreadyResolved)
        {
            var deferred1 = Promise.NewDeferred();
            var resolvedPromise = TestHelper.BuildPromise(CompleteType.Resolve, alreadyResolved, "Error", out var deferred2);

            bool resolved = false;

            Promise.First(deferred1.Promise, resolvedPromise)
                .Then(() =>
                {
                    resolved = true;
                })
                .Forget();

            deferred2.TryResolve();

            Assert.IsTrue(resolved);

            deferred1.Resolve();

            Assert.IsTrue(resolved);
        }

        [Test]
        public void FirstIsResolvedWhenSecondPromiseIsResolvedFirst_T(
            [Values] bool alreadyResolved)
        {
            var deferred1 = Promise.NewDeferred<int>();
            var resolvedPromise = TestHelper.BuildPromise(CompleteType.Resolve, alreadyResolved, 5, "Error", out var deferred2);

            bool resolved = false;

            Promise<int>.First(deferred1.Promise, resolvedPromise)
                .Then(i =>
                {
                    Assert.AreEqual(5, i);
                    resolved = true;
                })
                .Forget();

            deferred2.TryResolve(5);

            Assert.IsTrue(resolved);

            deferred1.Resolve(1);

            Assert.IsTrue(resolved);
        }

        [Test]
        public void FirstIsResolvedWhenFirstPromiseIsRejectedThenSecondPromiseIsResolved_void(
            [Values] bool alreadyRejected,
            [Values] bool alreadyResolved)
        {
            var rejectPromise = TestHelper.BuildPromise(CompleteType.Reject, alreadyRejected, "Error", out var deferred1);
            var resolvedPromise = TestHelper.BuildPromise(CompleteType.Resolve, alreadyResolved, "Error", out var deferred2);

            bool resolved = false;

            Promise.First(rejectPromise, resolvedPromise)
                .Then(() =>
                {
                    resolved = true;
                })
                .Forget();

            deferred1.TryReject("Error");

            Assert.AreEqual(alreadyResolved, resolved);

            deferred2.TryResolve();

            Assert.IsTrue(resolved);
        }

        [Test]
        public void FirstIsResolvedWhenFirstPromiseIsRejectedThenSecondPromiseIsResolved_T(
            [Values] bool alreadyRejected,
            [Values] bool alreadyResolved)
        {
            var rejectPromise = TestHelper.BuildPromise(CompleteType.Reject, alreadyRejected, 5, "Error", out var deferred1);
            var resolvedPromise = TestHelper.BuildPromise(CompleteType.Resolve, alreadyResolved, 5, "Error", out var deferred2);

            bool resolved = false;

            Promise<int>.First(rejectPromise, resolvedPromise)
                .Then(i =>
                {
                    Assert.AreEqual(5, i);
                    resolved = true;
                })
                .Forget();

            deferred1.TryReject("Error");

            Assert.AreEqual(alreadyResolved, resolved);

            deferred2.TryResolve(5);

            Assert.IsTrue(resolved);
        }

        [Test]
        public void FirstIsResolvedWhenSecondPromiseIsRejectedThenFirstPromiseIsResolved_void(
            [Values] bool alreadyRejected)
        {
            var deferred1 = Promise.NewDeferred();
            var rejectPromise = TestHelper.BuildPromise(CompleteType.Reject, alreadyRejected, "Error", out var deferred2);

            bool resolved = false;

            Promise.First(deferred1.Promise, rejectPromise)
                .Then(() =>
                {
                    resolved = true;
                })
                .Forget();

            deferred2.TryReject("Error");

            Assert.IsFalse(resolved);

            deferred1.Resolve();

            Assert.IsTrue(resolved);
        }

        [Test]
        public void FirstIsResolvedWhenSecondPromiseIsRejectedThenFirstPromiseIsResolved_T(
            [Values] bool alreadyRejected)
        {
            var deferred1 = Promise.NewDeferred<int>();
            var rejectPromise = TestHelper.BuildPromise(CompleteType.Reject, alreadyRejected, 5, "Error", out var deferred2);

            bool resolved = false;

            Promise<int>.First(deferred1.Promise, rejectPromise)
                .Then(i =>
                {
                    Assert.AreEqual(5, i);
                    resolved = true;
                })
                .Forget();

            deferred2.TryReject("Error");

            Assert.IsFalse(resolved);

            deferred1.Resolve(5);

            Assert.IsTrue(resolved);
        }

        [Test]
        public void FirstIsRejectedWhenAllPromisesAreRejected_void(
            [Values] bool alreadyRejected)
        {
            bool rejected = false;
            string expected = "Error";

            var rejectPromise1 = TestHelper.BuildPromise(CompleteType.Reject, alreadyRejected, "Different Error", out var deferred1);
            var rejectPromise2 = TestHelper.BuildPromise(CompleteType.Reject, alreadyRejected, expected, out var deferred2);

            Promise.First(rejectPromise1, rejectPromise2)
                .Catch((string rej) =>
                {
                    Assert.AreEqual(expected, rej);
                    rejected = true;
                })
                .Forget();

            deferred1.TryReject("Different Error");

            Assert.AreEqual(alreadyRejected, rejected);

            deferred2.TryReject(expected);

            Assert.IsTrue(rejected);
        }

        [Test]
        public void FirstIsRejectedWhenAllPromisesAreRejected_T(
            [Values] bool alreadyRejected)
        {
            bool rejected = false;
            string expected = "Error";

            var rejectPromise1 = TestHelper.BuildPromise(CompleteType.Reject, alreadyRejected, 5, "Different Error", out var deferred1);
            var rejectPromise2 = TestHelper.BuildPromise(CompleteType.Reject, alreadyRejected, 5, expected, out var deferred2);

            Promise<int>.First(rejectPromise1, rejectPromise2)
                .Catch((string rej) =>
                {
                    Assert.AreEqual(expected, rej);
                    rejected = true;
                })
                .Forget();

            deferred1.TryReject("Different Error");

            Assert.AreEqual(alreadyRejected, rejected);

            deferred2.TryReject(expected);

            Assert.IsTrue(rejected);
        }

        [Test]
        public void FirstIsResolvedWhenFirstPromiseIsCanceledThenSecondPromiseIsResolved_void(
            [Values] bool alreadyCanceled,
            [Values] bool alreadyResolved)
        {
            var cancelPromise = TestHelper.BuildPromise(CompleteType.Cancel, alreadyCanceled, "Error", out var deferred1);
            var resolvedPromise = TestHelper.BuildPromise(CompleteType.Resolve, alreadyResolved, "Error", out var deferred2);

            bool resolved = false;

            Promise.First(cancelPromise, resolvedPromise)
                .Then(() =>
                {
                    resolved = true;
                })
                .Forget();

            deferred1.TryCancel();

            Assert.AreEqual(alreadyResolved, resolved);

            deferred2.TryResolve();

            Assert.IsTrue(resolved);
        }

        [Test]
        public void FirstIsResolvedWhenFirstPromiseIsCanceledThenSecondPromiseIsResolved_T(
            [Values] bool alreadyCanceled,
            [Values] bool alreadyResolved)
        {
            var cancelPromise = TestHelper.BuildPromise(CompleteType.Cancel, alreadyCanceled, 2, "Error", out var deferred1);
            var resolvedPromise = TestHelper.BuildPromise(CompleteType.Resolve, alreadyResolved, 5, "Error", out var deferred2);

            bool resolved = false;

            Promise<int>.First(cancelPromise, resolvedPromise)
                .Then(i =>
                {
                    Assert.AreEqual(5, i);
                    resolved = true;
                })
                .Forget();

            deferred1.TryCancel();

            Assert.AreEqual(alreadyResolved, resolved);

            deferred2.TryResolve(5);

            Assert.IsTrue(resolved);
        }

        [Test]
        public void FirstIsResolvedWhenSecondPromiseIsCanceledThenFirstPromiseIsResolved_void(
            [Values] bool alreadyCanceled)
        {
            var deferred1 = Promise.NewDeferred();
            var cancelPromise = TestHelper.BuildPromise(CompleteType.Cancel, alreadyCanceled, "Error", out var deferred2);

            bool resolved = false;

            Promise.First(deferred1.Promise, cancelPromise)
                .Then(() =>
                {
                    resolved = true;
                })
                .Forget();

            deferred2.TryCancel();

            Assert.IsFalse(resolved);

            deferred1.TryResolve();

            Assert.IsTrue(resolved);
        }

        [Test]
        public void FirstIsResolvedWhenSecondPromiseIsCanceledThenFirstPromiseIsResolved_T(
            [Values] bool alreadyCanceled)
        {
            var deferred1 = Promise.NewDeferred<int>();
            var cancelPromise = TestHelper.BuildPromise(CompleteType.Cancel, alreadyCanceled, 5, "Error", out var deferred2);

            bool resolved = false;

            Promise<int>.First(deferred1.Promise, cancelPromise)
                .Then(i =>
                {
                    Assert.AreEqual(5, i);
                    resolved = true;
                })
                .Forget();

            deferred2.TryCancel();

            Assert.IsFalse(resolved);

            deferred1.TryResolve(5);

            Assert.IsTrue(resolved);
        }

        [Test]
        public void FirstIsCanceledWhenAllPromisesAreCanceled_void(
            [Values] bool alreadyCanceled)
        {
            var cancelPromise1 = TestHelper.BuildPromise(CompleteType.Cancel, alreadyCanceled, "Error", out var deferred1);
            var cancelPromise2 = TestHelper.BuildPromise(CompleteType.Cancel, alreadyCanceled, "Error", out var deferred2);

            bool canceled = false;

            Promise.First(cancelPromise1, cancelPromise2)
                .CatchCancelation(() =>
                {
                    canceled = true;
                })
                .Forget();

            deferred1.TryCancel();
            Assert.AreEqual(alreadyCanceled, canceled);

            deferred2.TryCancel();
            Assert.IsTrue(canceled);
        }

        [Test]
        public void FirstIsCanceledWhenAllPromisesAreCanceled_T(
            [Values] bool alreadyCanceled)
        {
            var cancelPromise1 = TestHelper.BuildPromise(CompleteType.Cancel, alreadyCanceled, 5, "Error", out var deferred1);
            var cancelPromise2 = TestHelper.BuildPromise(CompleteType.Cancel, alreadyCanceled, 5, "Error", out var deferred2);

            bool canceled = false;

            Promise<int>.First(cancelPromise1, cancelPromise2)
                .CatchCancelation(() =>
                {
                    canceled = true;
                })
                .Forget();

            deferred1.TryCancel();
            Assert.AreEqual(alreadyCanceled, canceled);

            deferred2.TryCancel();
            Assert.IsTrue(canceled);
        }

        [Test]
        public void FirstIsRejectededWhenFirstPromiseIsCanceledThenSecondPromiseIsRejected_void()
        {
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred1 = Promise.NewDeferred();
            cancelationSource.Token.Register(deferred1);
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

            cancelationSource.Cancel();

            Assert.IsFalse(rejected);

            deferred2.Reject(expected);

            Assert.IsTrue(rejected);

            cancelationSource.Dispose();
        }

        [Test]
        public void FirstIsRejectededWhenFirstPromiseIsCanceledThenSecondPromiseIsRejected_T()
        {
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred1 = Promise.NewDeferred<int>();
            cancelationSource.Token.Register(deferred1);
            var deferred2 = Promise.NewDeferred<int>();

            bool rejected = false;
            string expected = "Error";

            Promise<int>.First(deferred1.Promise, deferred2.Promise)
                .Catch((string rej) =>
                {
                    Assert.AreEqual(expected, rej);
                    rejected = true;
                })
                .Forget();

            cancelationSource.Cancel();

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
            var deferred2 = Promise.NewDeferred();
            cancelationSource.Token.Register(deferred2);

            bool rejected = false;
            string expected = "Error";

            Promise.First(deferred1.Promise, deferred2.Promise)
                .Catch((string rej) =>
                {
                    Assert.AreEqual(expected, rej);
                    rejected = true;
                })
                .Forget();

            cancelationSource.Cancel();

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
            var deferred2 = Promise.NewDeferred<int>();
            cancelationSource.Token.Register(deferred2);

            bool rejected = false;
            string expected = "Error";

            Promise<int>.First(deferred1.Promise, deferred2.Promise)
                .Catch((string rej) =>
                {
                    Assert.AreEqual(expected, rej);
                    rejected = true;
                })
                .Forget();

            cancelationSource.Cancel();

            Assert.IsFalse(rejected);

            deferred1.Reject(expected);

            Assert.IsTrue(rejected);

            cancelationSource.Dispose();
        }

        [Test]
        public void FirstIsCancelededWhenFirstPromiseIsRejectedThenSecondPromiseIsCanceled_void()
        {
            var deferred1 = Promise.NewDeferred();
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred2 = Promise.NewDeferred();
            cancelationSource.Token.Register(deferred2);

            bool canceled = false;

            Promise.First(deferred1.Promise, deferred2.Promise)
                .CatchCancelation(() =>
                {
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
        public void FirstIsCancelededWhenFirstPromiseIsRejectedThenSecondPromiseIsCanceled_T()
        {
            var deferred1 = Promise.NewDeferred<int>();
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred2 = Promise.NewDeferred<int>();
            cancelationSource.Token.Register(deferred2);

            bool canceled = false;

            Promise<int>.First(deferred1.Promise, deferred2.Promise)
                .CatchCancelation(() =>
                {
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
        public void FirstIsCancelededWhenSecondPromiseIsRejectedThenFirstPromiseIsCanceled_void()
        {
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred1 = Promise.NewDeferred();
            cancelationSource.Token.Register(deferred1);
            var deferred2 = Promise.NewDeferred();

            bool canceled = false;

            Promise.First(deferred1.Promise, deferred2.Promise)
                .CatchCancelation(() =>
                {
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
        public void FirstIsCancelededWhenSecondPromiseIsRejectedThenFirstPromiseIsCanceled_T()
        {
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred1 = Promise.NewDeferred<int>();
            cancelationSource.Token.Register(deferred1);
            var deferred2 = Promise.NewDeferred<int>();

            bool canceled = false;

            Promise<int>.First(deferred1.Promise, deferred2.Promise)
                .CatchCancelation(() =>
                {
                    canceled = true;
                })
                .Forget();

            deferred2.Reject("Error");

            Assert.IsFalse(canceled);

            cancelationSource.Cancel();

            Assert.IsTrue(canceled);

            cancelationSource.Dispose();
        }

        private static void Swap<T>(ref T a, ref T b)
        {
            var temp = a;
            a = b;
            b = temp;
        }

        [Test]
        public void FirstWithIndex_2_void(
            [Values(0, 1)] int winIndex,
            [Values] bool alreadyResolved)
        {
            var promise1 = TestHelper.BuildPromise(CompleteType.Resolve, alreadyResolved && winIndex == 0, "Error", out var deferred1);
            var promise2 = TestHelper.BuildPromise(CompleteType.Resolve, alreadyResolved && winIndex == 1, "Error", out var deferred2);

            int resultIndex = -1;

            Promise.FirstWithIndex(promise1, promise2)
                .Then(index => resultIndex = index)
                .Forget();

            if (winIndex == 1)
            {
                Swap(ref deferred1, ref deferred2);
            }
            deferred1.TryResolve();
            deferred2.TryResolve();

            Assert.AreEqual(winIndex, resultIndex);
        }

        [Test]
        public void FirstWithIndex_3_void(
            [Values(0, 1, 2)] int winIndex,
            [Values] bool alreadyResolved)
        {
            var promise1 = TestHelper.BuildPromise(CompleteType.Resolve, alreadyResolved && winIndex == 0, "Error", out var deferred1);
            var promise2 = TestHelper.BuildPromise(CompleteType.Resolve, alreadyResolved && winIndex == 1, "Error", out var deferred2);
            var promise3 = TestHelper.BuildPromise(CompleteType.Resolve, alreadyResolved && winIndex == 2, "Error", out var deferred3);

            int resultIndex = -1;

            Promise.FirstWithIndex(promise1, promise2, promise3)
                .Then(index => resultIndex = index)
                .Forget();

            if (winIndex == 1)
            {
                Swap(ref deferred1, ref deferred2);
            }
            else if (winIndex == 2)
            {
                Swap(ref deferred1, ref deferred3);
            }
            deferred1.TryResolve();
            deferred2.TryResolve();
            deferred3.TryResolve();

            Assert.AreEqual(winIndex, resultIndex);
        }

        [Test]
        public void FirstWithIndex_4_void(
            [Values(0, 1, 2, 3)] int winIndex,
            [Values] bool alreadyResolved)
        {
            var promise1 = TestHelper.BuildPromise(CompleteType.Resolve, alreadyResolved && winIndex == 0, "Error", out var deferred1);
            var promise2 = TestHelper.BuildPromise(CompleteType.Resolve, alreadyResolved && winIndex == 1, "Error", out var deferred2);
            var promise3 = TestHelper.BuildPromise(CompleteType.Resolve, alreadyResolved && winIndex == 2, "Error", out var deferred3);
            var promise4 = TestHelper.BuildPromise(CompleteType.Resolve, alreadyResolved && winIndex == 3, "Error", out var deferred4);

            int resultIndex = -1;

            Promise.FirstWithIndex(promise1, promise2, promise3, promise4)
                .Then(index => resultIndex = index)
                .Forget();

            if (winIndex == 1)
            {
                Swap(ref deferred1, ref deferred2);
            }
            else if (winIndex == 2)
            {
                Swap(ref deferred1, ref deferred3);
            }
            else if (winIndex == 3)
            {
                Swap(ref deferred1, ref deferred4);
            }
            deferred1.TryResolve();
            deferred2.TryResolve();
            deferred3.TryResolve();
            deferred4.TryResolve();

            Assert.AreEqual(winIndex, resultIndex);
        }

        [Test]
        public void FirstWithIndex_array_void(
            [Values(0, 1, 2, 3)] int winIndex,
            [Values] bool alreadyResolved)
        {
            var promise1 = TestHelper.BuildPromise(CompleteType.Resolve, alreadyResolved && winIndex == 0, "Error", out var deferred1);
            var promise2 = TestHelper.BuildPromise(CompleteType.Resolve, alreadyResolved && winIndex == 1, "Error", out var deferred2);
            var promise3 = TestHelper.BuildPromise(CompleteType.Resolve, alreadyResolved && winIndex == 2, "Error", out var deferred3);
            var promise4 = TestHelper.BuildPromise(CompleteType.Resolve, alreadyResolved && winIndex == 3, "Error", out var deferred4);

            int resultIndex = -1;

            Promise.FirstWithIndex(new Promise[] { promise1, promise2, promise3, promise4 })
                .Then(index => resultIndex = index)
                .Forget();

            if (winIndex == 1)
            {
                Swap(ref deferred1, ref deferred2);
            }
            else if (winIndex == 2)
            {
                Swap(ref deferred1, ref deferred3);
            }
            else if (winIndex == 3)
            {
                Swap(ref deferred1, ref deferred4);
            }
            deferred1.TryResolve();
            deferred2.TryResolve();
            deferred3.TryResolve();
            deferred4.TryResolve();

            Assert.AreEqual(winIndex, resultIndex);
        }

        [Test]
        public void FirstWithIndex_2_T(
            [Values(0, 1)] int winIndex,
            [Values] bool alreadyResolved)
        {
            var promise1 = TestHelper.BuildPromise(CompleteType.Resolve, alreadyResolved && winIndex == 0, alreadyResolved && winIndex == 0 ? 1 : 0, "Error", out var deferred1);
            var promise2 = TestHelper.BuildPromise(CompleteType.Resolve, alreadyResolved && winIndex == 1, alreadyResolved && winIndex == 1 ? 1 : 2, "Error", out var deferred2);

            int resultIndex = -1;
            int result = -1;

            Promise.FirstWithIndex(promise1, promise2)
                .Then(cv =>
                {
                    resultIndex = cv.Item1;
                    result = cv.Item2;
                })
                .Forget();

            if (winIndex == 1)
            {
                Swap(ref deferred1, ref deferred2);
            }
            deferred1.TryResolve(1);
            deferred2.TryResolve(2);

            Assert.AreEqual(winIndex, resultIndex);
            Assert.AreEqual(1, result);
        }

        [Test]
        public void FirstWithIndex_3_T(
            [Values(0, 1, 2)] int winIndex,
            [Values] bool alreadyResolved)
        {
            var promise1 = TestHelper.BuildPromise(CompleteType.Resolve, alreadyResolved && winIndex == 0, alreadyResolved && winIndex == 0 ? 1 : 0, "Error", out var deferred1);
            var promise2 = TestHelper.BuildPromise(CompleteType.Resolve, alreadyResolved && winIndex == 1, alreadyResolved && winIndex == 1 ? 1 : 2, "Error", out var deferred2);
            var promise3 = TestHelper.BuildPromise(CompleteType.Resolve, alreadyResolved && winIndex == 2, alreadyResolved && winIndex == 2 ? 1 : 3, "Error", out var deferred3);

            int resultIndex = -1;
            int result = -1;

            Promise.FirstWithIndex(promise1, promise2, promise3)
                .Then(cv =>
                {
                    resultIndex = cv.Item1;
                    result = cv.Item2;
                })
                .Forget();

            if (winIndex == 1)
            {
                Swap(ref deferred1, ref deferred2);
            }
            else if (winIndex == 2)
            {
                Swap(ref deferred1, ref deferred3);
            }
            deferred1.TryResolve(1);
            deferred2.TryResolve(2);
            deferred3.TryResolve(3);

            Assert.AreEqual(winIndex, resultIndex);
            Assert.AreEqual(1, result);
        }

        [Test]
        public void FirstWithIndex_4_T(
            [Values(0, 1, 2, 3)] int winIndex,
            [Values] bool alreadyResolved)
        {
            var promise1 = TestHelper.BuildPromise(CompleteType.Resolve, alreadyResolved && winIndex == 0, alreadyResolved && winIndex == 0 ? 1 : 0, "Error", out var deferred1);
            var promise2 = TestHelper.BuildPromise(CompleteType.Resolve, alreadyResolved && winIndex == 1, alreadyResolved && winIndex == 1 ? 1 : 2, "Error", out var deferred2);
            var promise3 = TestHelper.BuildPromise(CompleteType.Resolve, alreadyResolved && winIndex == 2, alreadyResolved && winIndex == 2 ? 1 : 3, "Error", out var deferred3);
            var promise4 = TestHelper.BuildPromise(CompleteType.Resolve, alreadyResolved && winIndex == 3, alreadyResolved && winIndex == 3 ? 1 : 4, "Error", out var deferred4);

            int resultIndex = -1;
            int result = -1;

            Promise.FirstWithIndex(promise1, promise2, promise3, promise4)
                .Then(cv =>
                {
                    resultIndex = cv.Item1;
                    result = cv.Item2;
                })
                .Forget();

            if (winIndex == 1)
            {
                Swap(ref deferred1, ref deferred2);
            }
            else if (winIndex == 2)
            {
                Swap(ref deferred1, ref deferred3);
            }
            else if (winIndex == 3)
            {
                Swap(ref deferred1, ref deferred4);
            }
            deferred1.TryResolve(1);
            deferred2.TryResolve(2);
            deferred3.TryResolve(3);
            deferred4.TryResolve(4);

            Assert.AreEqual(winIndex, resultIndex);
            Assert.AreEqual(1, result);
        }

        [Test]
        public void FirstWithIndex_array_T(
            [Values(0, 1, 2, 3)] int winIndex,
            [Values] bool alreadyResolved)
        {
            var promise1 = TestHelper.BuildPromise(CompleteType.Resolve, alreadyResolved && winIndex == 0, alreadyResolved && winIndex == 0 ? 1 : 0, "Error", out var deferred1);
            var promise2 = TestHelper.BuildPromise(CompleteType.Resolve, alreadyResolved && winIndex == 1, alreadyResolved && winIndex == 1 ? 1 : 2, "Error", out var deferred2);
            var promise3 = TestHelper.BuildPromise(CompleteType.Resolve, alreadyResolved && winIndex == 2, alreadyResolved && winIndex == 2 ? 1 : 3, "Error", out var deferred3);
            var promise4 = TestHelper.BuildPromise(CompleteType.Resolve, alreadyResolved && winIndex == 3, alreadyResolved && winIndex == 3 ? 1 : 4, "Error", out var deferred4);

            int resultIndex = -1;
            int result = -1;

            Promise.FirstWithIndex(new Promise<int>[] { promise1, promise2, promise3, promise4 })
                .Then(cv =>
                {
                    resultIndex = cv.Item1;
                    result = cv.Item2;
                })
                .Forget();

            if (winIndex == 1)
            {
                Swap(ref deferred1, ref deferred2);
            }
            else if (winIndex == 2)
            {
                Swap(ref deferred1, ref deferred3);
            }
            else if (winIndex == 3)
            {
                Swap(ref deferred1, ref deferred4);
            }
            deferred1.TryResolve(1);
            deferred2.TryResolve(2);
            deferred3.TryResolve(3);
            deferred4.TryResolve(4);

            Assert.AreEqual(winIndex, resultIndex);
            Assert.AreEqual(1, result);
        }
    }
}