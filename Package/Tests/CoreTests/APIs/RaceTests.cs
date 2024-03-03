#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using NUnit.Framework;
using Proto.Promises;

namespace ProtoPromiseTests.APIs
{
    public class RaceTests
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
        public void RaceIsResolvedWhenFirstPromiseIsResolvedFirst_void(
            [Values] bool alreadyResolved)
        {
            var resolvedPromise1 = TestHelper.BuildPromise(CompleteType.Resolve, alreadyResolved, "Error", out var deferred1, out _);
            var resolvedPromise2 = TestHelper.BuildPromise(CompleteType.Resolve, alreadyResolved, "Error", out var deferred2, out _);

            bool resolved = false;

            Promise.Race(resolvedPromise1, resolvedPromise2)
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
        public void RaceIsResolvedWhenFirstPromiseIsResolvedFirst_T(
            [Values] bool alreadyResolved)
        {
            var resolvedPromise1 = TestHelper.BuildPromise(CompleteType.Resolve, alreadyResolved, 5, "Error", out var deferred1, out _);
            var resolvedPromise2 = TestHelper.BuildPromise(CompleteType.Resolve, alreadyResolved, 1, "Error", out var deferred2, out _);

            bool resolved = false;

            Promise<int>.Race(resolvedPromise1, resolvedPromise2)
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
        public void RaceIsResolvedWhenSecondPromiseIsResolvedFirst_void(
            [Values] bool alreadyResolved)
        {
            var deferred1 = Promise.NewDeferred();
            var resolvedPromise = TestHelper.BuildPromise(CompleteType.Resolve, alreadyResolved, "Error", out var deferred2, out _);

            bool resolved = false;

            Promise.Race(deferred1.Promise, resolvedPromise)
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
        public void RaceIsResolvedWhenSecondPromiseIsResolvedFirst_T(
            [Values] bool alreadyResolved)
        {
            var deferred1 = Promise.NewDeferred<int>();
            var resolvedPromise = TestHelper.BuildPromise(CompleteType.Resolve, alreadyResolved, 5, "Error", out var deferred2, out _);

            bool resolved = false;

            Promise<int>.Race(deferred1.Promise, resolvedPromise)
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
        public void RaceIsRejectedWhenFirstPromiseIsRejectedFirst_void(
            [Values] bool alreadyRejected)
        {
            var rejectPromise = TestHelper.BuildPromise(CompleteType.Reject, alreadyRejected, "Error", out var deferred1, out _);
            var deferred2 = Promise.NewDeferred();

            bool invoked = false;
            string expected = "Error";

            Promise.Race(rejectPromise, deferred2.Promise)
                .Catch((string rej) =>
                {
                    Assert.AreEqual(expected, rej);
                    invoked = true;
                })
                .Forget();

            deferred1.TryReject(expected);

            Assert.IsTrue(invoked);

            deferred2.Resolve();

            Assert.IsTrue(invoked);
        }

        [Test]
        public void RaceIsRejectedWhenFirstPromiseIsRejectedFirst_T(
            [Values] bool alreadyRejected)
        {
            var rejectPromise = TestHelper.BuildPromise(CompleteType.Reject, alreadyRejected, 5, "Error", out var deferred1, out _);
            var deferred2 = Promise.NewDeferred<int>();

            bool invoked = false;
            string expected = "Error";

            Promise<int>.Race(rejectPromise, deferred2.Promise)
                .Catch((string rej) =>
                {
                    Assert.AreEqual(expected, rej);
                    invoked = true;
                })
                .Forget();

            deferred1.TryReject(expected);

            Assert.IsTrue(invoked);

            deferred2.Resolve(5);

            Assert.IsTrue(invoked);
        }

        [Test]
        public void RaceIsRejectedWhenSecondPromiseIsRejectedFirst_void(
            [Values] bool alreadyRejected)
        {
            var deferred1 = Promise.NewDeferred();
            var rejectPromise = TestHelper.BuildPromise(CompleteType.Reject, alreadyRejected, 5, "Error", out var deferred2, out _);

            bool invoked = false;
            string expected = "Error";

            Promise.Race(deferred1.Promise, rejectPromise)
                .Catch((string rej) =>
                {
                    Assert.AreEqual(expected, rej);
                    invoked = true;
                })
                .Forget();

            deferred2.TryReject(expected);

            Assert.IsTrue(invoked);

            deferred1.Resolve();

            Assert.IsTrue(invoked);
        }

        [Test]
        public void RaceIsRejectedWhenSecondPromiseIsRejectedFirst_T(
            [Values] bool alreadyRejected)
        {
            var deferred1 = Promise.NewDeferred<int>();
            var rejectPromise = TestHelper.BuildPromise(CompleteType.Reject, alreadyRejected, 5, "Error", out var deferred2, out _);

            bool invoked = false;
            string expected = "Error";

            Promise<int>.Race(deferred1.Promise, rejectPromise)
                .Catch((string rej) =>
                {
                    Assert.AreEqual(expected, rej);
                    invoked = true;
                })
                .Forget();

            deferred2.TryReject(expected);

            Assert.IsTrue(invoked);

            deferred1.Resolve(5);

            Assert.IsTrue(invoked);
        }

        [Test]
        public void RaceIsCanceledWhenFirstPromiseIsCanceledFirst_void(
            [Values] bool alreadyCanceled)
        {
            var cancelPromise = TestHelper.BuildPromise(CompleteType.Cancel, alreadyCanceled, "Error", out var deferred1, out _);
            var deferred2 = Promise.NewDeferred();

            bool invoked = false;

            Promise.Race(cancelPromise, deferred2.Promise)
                .CatchCancelation(() =>
                {
                    invoked = true;
                })
                .Forget();

            deferred1.TryCancel();

            Assert.IsTrue(invoked);

            deferred2.Resolve();

            Assert.IsTrue(invoked);
        }

        [Test]
        public void RaceIsCanceledWhenFirstPromiseIsCanceledFirst_T(
            [Values] bool alreadyCanceled)
        {
            var cancelPromise = TestHelper.BuildPromise(CompleteType.Cancel, alreadyCanceled, 5, "Error", out var deferred1, out _);
            var deferred2 = Promise.NewDeferred<int>();

            bool invoked = false;

            Promise<int>.Race(cancelPromise, deferred2.Promise)
                .CatchCancelation(() =>
                {
                    invoked = true;
                })
                .Forget();

            deferred1.TryCancel();

            Assert.IsTrue(invoked);

            deferred2.Resolve(5);

            Assert.IsTrue(invoked);
        }

        [Test]
        public void RaceIsCanceledWhenSecondPromiseIsCanceledFirst_void(
            [Values] bool alreadyCanceled)
        {
            var deferred1 = Promise.NewDeferred();
            var cancelPromise = TestHelper.BuildPromise(CompleteType.Cancel, alreadyCanceled, "Error", out var deferred2, out _);

            bool invoked = false;

            Promise.Race(deferred1.Promise, cancelPromise)
                .CatchCancelation(() =>
                {
                    invoked = true;
                })
                .Forget();

            deferred2.TryCancel();

            Assert.IsTrue(invoked);

            deferred1.Resolve();

            Assert.IsTrue(invoked);
        }

        [Test]
        public void RaceIsCanceledWhenSecondPromiseIsCanceledFirst_T(
            [Values] bool alreadyCanceled)
        {
            var deferred1 = Promise.NewDeferred<int>();
            var cancelPromise = TestHelper.BuildPromise(CompleteType.Cancel, alreadyCanceled, 5, "Error", out var deferred2, out _);

            bool invoked = false;

            Promise<int>.Race(deferred1.Promise, cancelPromise)
                .CatchCancelation(() =>
                {
                    invoked = true;
                })
                .Forget();

            deferred2.TryCancel();

            Assert.IsTrue(invoked);

            deferred1.Resolve(5);

            Assert.IsTrue(invoked);
        }

        private static void Swap<T>(ref T a, ref T b)
        {
            var temp = a;
            a = b;
            b = temp;
        }

        [Test]
        public void RaceWithIndex_2_void(
            [Values(0, 1)] int winIndex,
            [Values] bool alreadyResolved)
        {
            var promise1 = TestHelper.BuildPromise(CompleteType.Resolve, alreadyResolved && winIndex == 0, "Error", out var deferred1, out _);
            var promise2 = TestHelper.BuildPromise(CompleteType.Resolve, alreadyResolved && winIndex == 1, "Error", out var deferred2, out _);

            int resultIndex = -1;

            Promise.RaceWithIndex(promise1, promise2)
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
        public void RaceWithIndex_3_void(
            [Values(0, 1, 2)] int winIndex,
            [Values] bool alreadyResolved)
        {
            var promise1 = TestHelper.BuildPromise(CompleteType.Resolve, alreadyResolved && winIndex == 0, "Error", out var deferred1, out _);
            var promise2 = TestHelper.BuildPromise(CompleteType.Resolve, alreadyResolved && winIndex == 1, "Error", out var deferred2, out _);
            var promise3 = TestHelper.BuildPromise(CompleteType.Resolve, alreadyResolved && winIndex == 2, "Error", out var deferred3, out _);

            int resultIndex = -1;

            Promise.RaceWithIndex(promise1, promise2, promise3)
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
        public void RaceWithIndex_4_void(
            [Values(0, 1, 2, 3)] int winIndex,
            [Values] bool alreadyResolved)
        {
            var promise1 = TestHelper.BuildPromise(CompleteType.Resolve, alreadyResolved && winIndex == 0, "Error", out var deferred1, out _);
            var promise2 = TestHelper.BuildPromise(CompleteType.Resolve, alreadyResolved && winIndex == 1, "Error", out var deferred2, out _);
            var promise3 = TestHelper.BuildPromise(CompleteType.Resolve, alreadyResolved && winIndex == 2, "Error", out var deferred3, out _);
            var promise4 = TestHelper.BuildPromise(CompleteType.Resolve, alreadyResolved && winIndex == 3, "Error", out var deferred4, out _);

            int resultIndex = -1;

            Promise.RaceWithIndex(promise1, promise2, promise3, promise4)
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
        public void RaceWithIndex_array_void(
            [Values(0, 1, 2, 3)] int winIndex,
            [Values] bool alreadyResolved)
        {
            var promise1 = TestHelper.BuildPromise(CompleteType.Resolve, alreadyResolved && winIndex == 0, "Error", out var deferred1, out _);
            var promise2 = TestHelper.BuildPromise(CompleteType.Resolve, alreadyResolved && winIndex == 1, "Error", out var deferred2, out _);
            var promise3 = TestHelper.BuildPromise(CompleteType.Resolve, alreadyResolved && winIndex == 2, "Error", out var deferred3, out _);
            var promise4 = TestHelper.BuildPromise(CompleteType.Resolve, alreadyResolved && winIndex == 3, "Error", out var deferred4, out _);

            int resultIndex = -1;

            Promise.RaceWithIndex(new Promise[] { promise1, promise2, promise3, promise4 })
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
        public void RaceWithIndex_2_T(
            [Values(0, 1)] int winIndex,
            [Values] bool alreadyResolved)
        {
            var promise1 = TestHelper.BuildPromise(CompleteType.Resolve, alreadyResolved && winIndex == 0, alreadyResolved && winIndex == 0 ? 1 : 0, "Error", out var deferred1, out _);
            var promise2 = TestHelper.BuildPromise(CompleteType.Resolve, alreadyResolved && winIndex == 1, alreadyResolved && winIndex == 1 ? 1 : 2, "Error", out var deferred2, out _);

            int resultIndex = -1;
            int result = -1;

            Promise.RaceWithIndex(promise1, promise2)
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
        public void RaceWithIndex_3_T(
            [Values(0, 1, 2)] int winIndex,
            [Values] bool alreadyResolved)
        {
            var promise1 = TestHelper.BuildPromise(CompleteType.Resolve, alreadyResolved && winIndex == 0, alreadyResolved && winIndex == 0 ? 1 : 0, "Error", out var deferred1, out _);
            var promise2 = TestHelper.BuildPromise(CompleteType.Resolve, alreadyResolved && winIndex == 1, alreadyResolved && winIndex == 1 ? 1 : 2, "Error", out var deferred2, out _);
            var promise3 = TestHelper.BuildPromise(CompleteType.Resolve, alreadyResolved && winIndex == 2, alreadyResolved && winIndex == 2 ? 1 : 3, "Error", out var deferred3, out _);

            int resultIndex = -1;
            int result = -1;

            Promise.RaceWithIndex(promise1, promise2, promise3)
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
        public void RaceWithIndex_4_T(
            [Values(0, 1, 2, 3)] int winIndex,
            [Values] bool alreadyResolved)
        {
            var promise1 = TestHelper.BuildPromise(CompleteType.Resolve, alreadyResolved && winIndex == 0, alreadyResolved && winIndex == 0 ? 1 : 0, "Error", out var deferred1, out _);
            var promise2 = TestHelper.BuildPromise(CompleteType.Resolve, alreadyResolved && winIndex == 1, alreadyResolved && winIndex == 1 ? 1 : 2, "Error", out var deferred2, out _);
            var promise3 = TestHelper.BuildPromise(CompleteType.Resolve, alreadyResolved && winIndex == 2, alreadyResolved && winIndex == 2 ? 1 : 3, "Error", out var deferred3, out _);
            var promise4 = TestHelper.BuildPromise(CompleteType.Resolve, alreadyResolved && winIndex == 3, alreadyResolved && winIndex == 3 ? 1 : 4, "Error", out var deferred4, out _);

            int resultIndex = -1;
            int result = -1;

            Promise.RaceWithIndex(promise1, promise2, promise3, promise4)
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
        public void RaceWithIndex_array_T(
            [Values(0, 1, 2, 3)] int winIndex,
            [Values] bool alreadyResolved)
        {
            var promise1 = TestHelper.BuildPromise(CompleteType.Resolve, alreadyResolved && winIndex == 0, alreadyResolved && winIndex == 0 ? 1 : 0, "Error", out var deferred1, out _);
            var promise2 = TestHelper.BuildPromise(CompleteType.Resolve, alreadyResolved && winIndex == 1, alreadyResolved && winIndex == 1 ? 1 : 2, "Error", out var deferred2, out _);
            var promise3 = TestHelper.BuildPromise(CompleteType.Resolve, alreadyResolved && winIndex == 2, alreadyResolved && winIndex == 2 ? 1 : 3, "Error", out var deferred3, out _);
            var promise4 = TestHelper.BuildPromise(CompleteType.Resolve, alreadyResolved && winIndex == 3, alreadyResolved && winIndex == 3 ? 1 : 4, "Error", out var deferred4, out _);

            int resultIndex = -1;
            int result = -1;

            Promise.RaceWithIndex(new Promise<int>[] { promise1, promise2, promise3, promise4 })
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