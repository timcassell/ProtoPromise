﻿#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using NUnit.Framework;
using Proto.Promises;

namespace ProtoPromise.Tests.APIs
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
            var resolvedPromise1 = TestHelper.BuildPromise(CompleteType.Resolve, alreadyResolved, "Error", out var tryCompleter1);
            var resolvedPromise2 = TestHelper.BuildPromise(CompleteType.Resolve, alreadyResolved, "Error", out var tryCompleter2);

            bool resolved = false;

            Promise.Race(resolvedPromise1, resolvedPromise2)
                .Then(() =>
                {
                    resolved = true;
                })
                .Forget();

            tryCompleter1();

            Assert.IsTrue(resolved);

            tryCompleter2();

            Assert.IsTrue(resolved);
        }

        [Test]
        public void RaceIsResolvedWhenFirstPromiseIsResolvedFirst_T(
            [Values] bool alreadyResolved)
        {
            var resolvedPromise1 = TestHelper.BuildPromise(CompleteType.Resolve, alreadyResolved, 5, "Error", out var tryCompleter1);
            var resolvedPromise2 = TestHelper.BuildPromise(CompleteType.Resolve, alreadyResolved, 1, "Error", out var tryCompleter2);

            bool resolved = false;

            Promise<int>.Race(resolvedPromise1, resolvedPromise2)
                .Then(i =>
                {
                    Assert.AreEqual(5, i);
                    resolved = true;
                })
                .Forget();

            tryCompleter1();

            Assert.IsTrue(resolved);

            tryCompleter2();

            Assert.IsTrue(resolved);
        }

        [Test]
        public void RaceIsResolvedWhenSecondPromiseIsResolvedFirst_void(
            [Values] bool alreadyResolved)
        {
            var deferred1 = Promise.NewDeferred();
            var resolvedPromise = TestHelper.BuildPromise(CompleteType.Resolve, alreadyResolved, "Error", out var tryCompleter2);

            bool resolved = false;

            Promise.Race(deferred1.Promise, resolvedPromise)
                .Then(() =>
                {
                    resolved = true;
                })
                .Forget();

            tryCompleter2();

            Assert.IsTrue(resolved);

            deferred1.Resolve();

            Assert.IsTrue(resolved);
        }

        [Test]
        public void RaceIsResolvedWhenSecondPromiseIsResolvedFirst_T(
            [Values] bool alreadyResolved)
        {
            var deferred1 = Promise.NewDeferred<int>();
            var resolvedPromise = TestHelper.BuildPromise(CompleteType.Resolve, alreadyResolved, 5, "Error", out var tryCompleter2);

            bool resolved = false;

            Promise<int>.Race(deferred1.Promise, resolvedPromise)
                .Then(i =>
                {
                    Assert.AreEqual(5, i);
                    resolved = true;
                })
                .Forget();

            tryCompleter2();

            Assert.IsTrue(resolved);

            deferred1.Resolve(1);

            Assert.IsTrue(resolved);
        }

        [Test]
        public void RaceIsRejectedWhenFirstPromiseIsRejectedFirst_void(
            [Values] bool alreadyRejected)
        {
            string expected = "Error";
            var rejectPromise = TestHelper.BuildPromise(CompleteType.Reject, alreadyRejected, expected, out var tryCompleter1);
            var deferred2 = Promise.NewDeferred();

            bool invoked = false;

            Promise.Race(rejectPromise, deferred2.Promise)
                .Catch((string rej) =>
                {
                    Assert.AreEqual(expected, rej);
                    invoked = true;
                })
                .Forget();

            tryCompleter1();

            Assert.IsTrue(invoked);

            deferred2.Resolve();

            Assert.IsTrue(invoked);
        }

        [Test]
        public void RaceIsRejectedWhenFirstPromiseIsRejectedFirst_T(
            [Values] bool alreadyRejected)
        {
            string expected = "Error";
            var rejectPromise = TestHelper.BuildPromise(CompleteType.Reject, alreadyRejected, 5, expected, out var tryCompleter1);
            var deferred2 = Promise.NewDeferred<int>();

            bool invoked = false;

            Promise<int>.Race(rejectPromise, deferred2.Promise)
                .Catch((string rej) =>
                {
                    Assert.AreEqual(expected, rej);
                    invoked = true;
                })
                .Forget();

            tryCompleter1();

            Assert.IsTrue(invoked);

            deferred2.Resolve(5);

            Assert.IsTrue(invoked);
        }

        [Test]
        public void RaceIsRejectedWhenSecondPromiseIsRejectedFirst_void(
            [Values] bool alreadyRejected)
        {
            string expected = "Error";
            var deferred1 = Promise.NewDeferred();
            var rejectPromise = TestHelper.BuildPromise(CompleteType.Reject, alreadyRejected, 5, expected, out var tryCompleter2);

            bool invoked = false;

            Promise.Race(deferred1.Promise, rejectPromise)
                .Catch((string rej) =>
                {
                    Assert.AreEqual(expected, rej);
                    invoked = true;
                })
                .Forget();

            tryCompleter2();

            Assert.IsTrue(invoked);

            deferred1.Resolve();

            Assert.IsTrue(invoked);
        }

        [Test]
        public void RaceIsRejectedWhenSecondPromiseIsRejectedFirst_T(
            [Values] bool alreadyRejected)
        {
            string expected = "Error";
            var deferred1 = Promise.NewDeferred<int>();
            var rejectPromise = TestHelper.BuildPromise(CompleteType.Reject, alreadyRejected, 5, expected, out var tryCompleter2);

            bool invoked = false;

            Promise<int>.Race(deferred1.Promise, rejectPromise)
                .Catch((string rej) =>
                {
                    Assert.AreEqual(expected, rej);
                    invoked = true;
                })
                .Forget();

            tryCompleter2();

            Assert.IsTrue(invoked);

            deferred1.Resolve(5);

            Assert.IsTrue(invoked);
        }

        [Test]
        public void RaceIsCanceledWhenFirstPromiseIsCanceledFirst_void(
            [Values] bool alreadyCanceled)
        {
            var cancelPromise = TestHelper.BuildPromise(CompleteType.Cancel, alreadyCanceled, "Error", out var tryCompleter);
            var deferred2 = Promise.NewDeferred();

            bool invoked = false;

            Promise.Race(cancelPromise, deferred2.Promise)
                .CatchCancelation(() =>
                {
                    invoked = true;
                })
                .Forget();

            tryCompleter();

            Assert.IsTrue(invoked);

            deferred2.Resolve();

            Assert.IsTrue(invoked);
        }

        [Test]
        public void RaceIsCanceledWhenFirstPromiseIsCanceledFirst_T(
            [Values] bool alreadyCanceled)
        {
            var cancelPromise = TestHelper.BuildPromise(CompleteType.Cancel, alreadyCanceled, 5, "Error", out var tryCompleter1);
            var deferred2 = Promise.NewDeferred<int>();

            bool invoked = false;

            Promise<int>.Race(cancelPromise, deferred2.Promise)
                .CatchCancelation(() =>
                {
                    invoked = true;
                })
                .Forget();

            tryCompleter1();

            Assert.IsTrue(invoked);

            deferred2.Resolve(5);

            Assert.IsTrue(invoked);
        }

        [Test]
        public void RaceIsCanceledWhenSecondPromiseIsCanceledFirst_void(
            [Values] bool alreadyCanceled)
        {
            var deferred1 = Promise.NewDeferred();
            var cancelPromise = TestHelper.BuildPromise(CompleteType.Cancel, alreadyCanceled, "Error", out var tryCompleter2);

            bool invoked = false;

            Promise.Race(deferred1.Promise, cancelPromise)
                .CatchCancelation(() =>
                {
                    invoked = true;
                })
                .Forget();

            tryCompleter2();

            Assert.IsTrue(invoked);

            deferred1.Resolve();

            Assert.IsTrue(invoked);
        }

        [Test]
        public void RaceIsCanceledWhenSecondPromiseIsCanceledFirst_T(
            [Values] bool alreadyCanceled)
        {
            var deferred1 = Promise.NewDeferred<int>();
            var cancelPromise = TestHelper.BuildPromise(CompleteType.Cancel, alreadyCanceled, 5, "Error", out var tryCompleter2);

            bool invoked = false;

            Promise<int>.Race(deferred1.Promise, cancelPromise)
                .CatchCancelation(() =>
                {
                    invoked = true;
                })
                .Forget();

            tryCompleter2();

            Assert.IsTrue(invoked);

            deferred1.Resolve(5);

            Assert.IsTrue(invoked);
        }

#pragma warning disable CS0618 // Type or member is obsolete
        [Test]
        public void RaceWithIndex_2_void(
            [Values(0, 1)] int winIndex,
            [Values] bool alreadyResolved)
        {
            var promise1 = TestHelper.BuildPromise(CompleteType.Resolve, alreadyResolved && winIndex == 0, "Error", out var tryCompleter1);
            var promise2 = TestHelper.BuildPromise(CompleteType.Resolve, alreadyResolved && winIndex == 1, "Error", out var tryCompleter2);

            int resultIndex = -1;

            Promise.RaceWithIndex(promise1, promise2)
                .Then(index => resultIndex = index)
                .Forget();

            if (winIndex == 1)
            {
                (tryCompleter1, tryCompleter2) = (tryCompleter2, tryCompleter1);
            }
            tryCompleter1();
            tryCompleter2();

            Assert.AreEqual(winIndex, resultIndex);
        }

        [Test]
        public void RaceWithIndex_3_void(
            [Values(0, 1, 2)] int winIndex,
            [Values] bool alreadyResolved)
        {
            var promise1 = TestHelper.BuildPromise(CompleteType.Resolve, alreadyResolved && winIndex == 0, "Error", out var tryCompleter1);
            var promise2 = TestHelper.BuildPromise(CompleteType.Resolve, alreadyResolved && winIndex == 1, "Error", out var tryCompleter2);
            var promise3 = TestHelper.BuildPromise(CompleteType.Resolve, alreadyResolved && winIndex == 2, "Error", out var tryCompleter3);

            int resultIndex = -1;

            Promise.RaceWithIndex(promise1, promise2, promise3)
                .Then(index => resultIndex = index)
                .Forget();

            if (winIndex == 1)
            {
                (tryCompleter1, tryCompleter2) = (tryCompleter2, tryCompleter1);
            }
            else if (winIndex == 2)
            {
                (tryCompleter1, tryCompleter3) = (tryCompleter3, tryCompleter1);
            }
            tryCompleter1();
            tryCompleter2();
            tryCompleter3();

            Assert.AreEqual(winIndex, resultIndex);
        }

        [Test]
        public void RaceWithIndex_4_void(
            [Values(0, 1, 2, 3)] int winIndex,
            [Values] bool alreadyResolved)
        {
            var promise1 = TestHelper.BuildPromise(CompleteType.Resolve, alreadyResolved && winIndex == 0, "Error", out var tryCompleter1);
            var promise2 = TestHelper.BuildPromise(CompleteType.Resolve, alreadyResolved && winIndex == 1, "Error", out var tryCompleter2);
            var promise3 = TestHelper.BuildPromise(CompleteType.Resolve, alreadyResolved && winIndex == 2, "Error", out var tryCompleter3);
            var promise4 = TestHelper.BuildPromise(CompleteType.Resolve, alreadyResolved && winIndex == 3, "Error", out var tryCompleter4);

            int resultIndex = -1;

            Promise.RaceWithIndex(promise1, promise2, promise3, promise4)
                .Then(index => resultIndex = index)
                .Forget();

            if (winIndex == 1)
            {
                (tryCompleter1, tryCompleter2) = (tryCompleter2, tryCompleter1);
            }
            else if (winIndex == 2)
            {
                (tryCompleter1, tryCompleter3) = (tryCompleter3, tryCompleter1);
            }
            else if (winIndex == 3)
            {
                (tryCompleter1, tryCompleter4) = (tryCompleter4, tryCompleter1);
            }
            tryCompleter1();
            tryCompleter2();
            tryCompleter3();
            tryCompleter4();

            Assert.AreEqual(winIndex, resultIndex);
        }

        [Test]
        public void RaceWithIndex_array_void(
            [Values(0, 1, 2, 3)] int winIndex,
            [Values] bool alreadyResolved)
        {
            var promise1 = TestHelper.BuildPromise(CompleteType.Resolve, alreadyResolved && winIndex == 0, "Error", out var tryCompleter1);
            var promise2 = TestHelper.BuildPromise(CompleteType.Resolve, alreadyResolved && winIndex == 1, "Error", out var tryCompleter2);
            var promise3 = TestHelper.BuildPromise(CompleteType.Resolve, alreadyResolved && winIndex == 2, "Error", out var tryCompleter3);
            var promise4 = TestHelper.BuildPromise(CompleteType.Resolve, alreadyResolved && winIndex == 3, "Error", out var tryCompleter4);

            int resultIndex = -1;

            Promise.RaceWithIndex(new Promise[] { promise1, promise2, promise3, promise4 })
                .Then(index => resultIndex = index)
                .Forget();

            if (winIndex == 1)
            {
                (tryCompleter1, tryCompleter2) = (tryCompleter2, tryCompleter1);
            }
            else if (winIndex == 2)
            {
                (tryCompleter1, tryCompleter3) = (tryCompleter3, tryCompleter1);
            }
            else if (winIndex == 3)
            {
                (tryCompleter1, tryCompleter4) = (tryCompleter4, tryCompleter1);
            }
            tryCompleter1();
            tryCompleter2();
            tryCompleter3();
            tryCompleter4();

            Assert.AreEqual(winIndex, resultIndex);
        }

        [Test]
        public void RaceWithIndex_2_T(
            [Values(0, 1)] int winIndex,
            [Values] bool alreadyResolved)
        {
            var promise1 = TestHelper.BuildPromise(CompleteType.Resolve, alreadyResolved && winIndex == 0, winIndex == 0 ? 1 : 0, "Error", out var tryCompleter1);
            var promise2 = TestHelper.BuildPromise(CompleteType.Resolve, alreadyResolved && winIndex == 1, winIndex == 1 ? 1 : 2, "Error", out var tryCompleter2);

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
                (tryCompleter1, tryCompleter2) = (tryCompleter2, tryCompleter1);
            }
            tryCompleter1();
            tryCompleter2();

            Assert.AreEqual(winIndex, resultIndex);
            Assert.AreEqual(1, result);
        }

        [Test]
        public void RaceWithIndex_3_T(
            [Values(0, 1, 2)] int winIndex,
            [Values] bool alreadyResolved)
        {
            var promise1 = TestHelper.BuildPromise(CompleteType.Resolve, alreadyResolved && winIndex == 0, winIndex == 0 ? 1 : 0, "Error", out var tryCompleter1);
            var promise2 = TestHelper.BuildPromise(CompleteType.Resolve, alreadyResolved && winIndex == 1, winIndex == 1 ? 1 : 2, "Error", out var tryCompleter2);
            var promise3 = TestHelper.BuildPromise(CompleteType.Resolve, alreadyResolved && winIndex == 2, winIndex == 2 ? 1 : 3, "Error", out var tryCompleter3);

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
                (tryCompleter1, tryCompleter2) = (tryCompleter2, tryCompleter1);
            }
            else if (winIndex == 2)
            {
                (tryCompleter1, tryCompleter3) = (tryCompleter3, tryCompleter1);
            }
            tryCompleter1();
            tryCompleter2();
            tryCompleter3();

            Assert.AreEqual(winIndex, resultIndex);
            Assert.AreEqual(1, result);
        }

        [Test]
        public void RaceWithIndex_4_T(
            [Values(0, 1, 2, 3)] int winIndex,
            [Values] bool alreadyResolved)
        {
            var promise1 = TestHelper.BuildPromise(CompleteType.Resolve, alreadyResolved && winIndex == 0, winIndex == 0 ? 1 : 0, "Error", out var tryCompleter1);
            var promise2 = TestHelper.BuildPromise(CompleteType.Resolve, alreadyResolved && winIndex == 1, winIndex == 1 ? 1 : 2, "Error", out var tryCompleter2);
            var promise3 = TestHelper.BuildPromise(CompleteType.Resolve, alreadyResolved && winIndex == 2, winIndex == 2 ? 1 : 3, "Error", out var tryCompleter3);
            var promise4 = TestHelper.BuildPromise(CompleteType.Resolve, alreadyResolved && winIndex == 3, winIndex == 3 ? 1 : 4, "Error", out var tryCompleter4);

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
                (tryCompleter1, tryCompleter2) = (tryCompleter2, tryCompleter1);
            }
            else if (winIndex == 2)
            {
                (tryCompleter1, tryCompleter3) = (tryCompleter3, tryCompleter1);
            }
            else if (winIndex == 3)
            {
                (tryCompleter1, tryCompleter4) = (tryCompleter4, tryCompleter1);
            }
            tryCompleter1();
            tryCompleter2();
            tryCompleter3();
            tryCompleter4();

            Assert.AreEqual(winIndex, resultIndex);
            Assert.AreEqual(1, result);
        }

        [Test]
        public void RaceWithIndex_array_T(
            [Values(0, 1, 2, 3)] int winIndex,
            [Values] bool alreadyResolved)
        {
            var promise1 = TestHelper.BuildPromise(CompleteType.Resolve, alreadyResolved && winIndex == 0, winIndex == 0 ? 1 : 0, "Error", out var tryCompleter1);
            var promise2 = TestHelper.BuildPromise(CompleteType.Resolve, alreadyResolved && winIndex == 1, winIndex == 1 ? 1 : 2, "Error", out var tryCompleter2);
            var promise3 = TestHelper.BuildPromise(CompleteType.Resolve, alreadyResolved && winIndex == 2, winIndex == 2 ? 1 : 3, "Error", out var tryCompleter3);
            var promise4 = TestHelper.BuildPromise(CompleteType.Resolve, alreadyResolved && winIndex == 3, winIndex == 3 ? 1 : 4, "Error", out var tryCompleter4);

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
                (tryCompleter1, tryCompleter2) = (tryCompleter2, tryCompleter1);
            }
            else if (winIndex == 2)
            {
                (tryCompleter1, tryCompleter3) = (tryCompleter3, tryCompleter1);
            }
            else if (winIndex == 3)
            {
                (tryCompleter1, tryCompleter4) = (tryCompleter4, tryCompleter1);
            }
            tryCompleter1();
            tryCompleter2();
            tryCompleter3();
            tryCompleter4();

            Assert.AreEqual(winIndex, resultIndex);
            Assert.AreEqual(1, result);
        }
#pragma warning restore CS0618 // Type or member is obsolete
    }
}