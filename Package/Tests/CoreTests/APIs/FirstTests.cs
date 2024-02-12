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

            Promise<int>.First(deferred1.Promise, deferred2.Promise)
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

            Promise<int>.First(deferred1.Promise, deferred2.Promise)
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

            Promise<int>.First(deferred1.Promise, deferred2.Promise)
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

            Promise<int>.First(deferred1.Promise, deferred2.Promise)
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

            Promise<int>.First(deferred1.Promise, deferred2.Promise)
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
            var deferred1 = Promise.NewDeferred();
            cancelationSource.Token.Register(deferred1);
            var deferred2 = Promise.NewDeferred();

            bool resolved = false;

            Promise.First(deferred1.Promise, deferred2.Promise)
                .Then(() =>
                {
                    resolved = true;
                })
                .Forget();

            cancelationSource.Cancel();

            Assert.IsFalse(resolved);

            deferred2.Resolve();

            Assert.IsTrue(resolved);

            cancelationSource.Dispose();
        }

        [Test]
        public void FirstIsResolvedWhenFirstPromiseIsCanceledThenSecondPromiseIsResolved_T()
        {
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred1 = Promise.NewDeferred<int>();
            cancelationSource.Token.Register(deferred1);
            var deferred2 = Promise.NewDeferred<int>();

            bool resolved = false;

            Promise<int>.First(deferred1.Promise, deferred2.Promise)
                .Then(i =>
                {
                    Assert.AreEqual(5, i);
                    resolved = true;
                })
                .Forget();

            cancelationSource.Cancel();

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
            var deferred2 = Promise.NewDeferred();
            cancelationSource.Token.Register(deferred2);

            bool resolved = false;

            Promise.First(deferred1.Promise, deferred2.Promise)
                .Then(() =>
                {
                    resolved = true;
                })
                .Forget();

            cancelationSource.Cancel();

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
            var deferred2 = Promise.NewDeferred<int>();
            cancelationSource.Token.Register(deferred2);

            bool resolved = false;

            Promise<int>.First(deferred1.Promise, deferred2.Promise)
                .Then(i =>
                {
                    Assert.AreEqual(5, i);
                    resolved = true;
                })
                .Forget();

            cancelationSource.Cancel();

            Assert.IsFalse(resolved);

            deferred1.Resolve(5);

            Assert.IsTrue(resolved);

            cancelationSource.Dispose();
        }

        [Test]
        public void FirstIsCanceledWhenAllPromisesAreCanceled_void()
        {
            CancelationSource cancelationSource1 = CancelationSource.New();
            var deferred1 = Promise.NewDeferred();
            cancelationSource1.Token.Register(deferred1);
            CancelationSource cancelationSource2 = CancelationSource.New();
            var deferred2 = Promise.NewDeferred();
            cancelationSource2.Token.Register(deferred2);

            bool canceled = false;

            Promise.First(deferred1.Promise, deferred2.Promise)
                .CatchCancelation(() =>
                {
                    canceled = true;
                })
                .Forget();

            cancelationSource1.Cancel();
            Assert.IsFalse(canceled);

            cancelationSource2.Cancel();
            Assert.IsTrue(canceled);

            cancelationSource1.Dispose();
            cancelationSource2.Dispose();
        }

        [Test]
        public void FirstIsCanceledWhenAllPromisesAreCanceled_T()
        {
            CancelationSource cancelationSource1 = CancelationSource.New();
            var deferred1 = Promise.NewDeferred<int>();
            cancelationSource1.Token.Register(deferred1);
            CancelationSource cancelationSource2 = CancelationSource.New();
            var deferred2 = Promise.NewDeferred<int>();
            cancelationSource2.Token.Register(deferred2);

            bool canceled = false;

            Promise<int>.First(deferred1.Promise, deferred2.Promise)
                .CatchCancelation(() =>
                {
                    canceled = true;
                })
                .Forget();

            cancelationSource1.Cancel();
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

        private static void Swap(ref Promise.Deferred deferred1, ref Promise.Deferred deferred2)
        {
            var temp = deferred1;
            deferred1 = deferred2;
            deferred2 = temp;
        }

        [Test]
        public void FirstWithIndex_2_void([Values(0, 1)] int winIndex)
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();

            int resultIndex = -1;

            Promise.FirstWithIndex(deferred1.Promise, deferred2.Promise)
                .Then(index => resultIndex = index)
                .Forget();

            if (winIndex == 1)
            {
                Swap(ref deferred1, ref deferred2);
            }
            deferred1.Resolve();
            deferred2.Resolve();

            Assert.AreEqual(winIndex, resultIndex);
        }

        [Test]
        public void FirstWithIndex_3_void([Values(0, 1, 2)] int winIndex)
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();
            var deferred3 = Promise.NewDeferred();

            int resultIndex = -1;

            Promise.FirstWithIndex(deferred1.Promise, deferred2.Promise, deferred3.Promise)
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
            deferred1.Resolve();
            deferred2.Resolve();
            deferred3.Resolve();

            Assert.AreEqual(winIndex, resultIndex);
        }

        [Test]
        public void FirstWithIndex_4_void([Values(0, 1, 2, 3)] int winIndex)
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();
            var deferred3 = Promise.NewDeferred();
            var deferred4 = Promise.NewDeferred();

            int resultIndex = -1;

            Promise.FirstWithIndex(deferred1.Promise, deferred2.Promise, deferred3.Promise, deferred4.Promise)
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
            deferred1.Resolve();
            deferred2.Resolve();
            deferred3.Resolve();
            deferred4.Resolve();

            Assert.AreEqual(winIndex, resultIndex);
        }

        [Test]
        public void FirstWithIndex_array_void([Values(0, 1, 2, 3)] int winIndex)
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();
            var deferred3 = Promise.NewDeferred();
            var deferred4 = Promise.NewDeferred();

            int resultIndex = -1;

            Promise.FirstWithIndex(new Promise[] { deferred1.Promise, deferred2.Promise, deferred3.Promise, deferred4.Promise })
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
            deferred1.Resolve();
            deferred2.Resolve();
            deferred3.Resolve();
            deferred4.Resolve();

            Assert.AreEqual(winIndex, resultIndex);
        }

        private static void Swap(ref Promise<int>.Deferred deferred1, ref Promise<int>.Deferred deferred2)
        {
            var temp = deferred1;
            deferred1 = deferred2;
            deferred2 = temp;
        }

        [Test]
        public void FirstWithIndex_2_T([Values(0, 1)] int winIndex)
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();

            int resultIndex = -1;
            int result = -1;

            Promise.FirstWithIndex(deferred1.Promise, deferred2.Promise)
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
            deferred1.Resolve(1);
            deferred2.Resolve(2);

            Assert.AreEqual(winIndex, resultIndex);
            Assert.AreEqual(1, result);
        }

        [Test]
        public void FirstWithIndex_3_T([Values(0, 1, 2)] int winIndex)
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();
            var deferred3 = Promise.NewDeferred<int>();

            int resultIndex = -1;
            int result = -1;

            Promise.FirstWithIndex(deferred1.Promise, deferred2.Promise, deferred3.Promise)
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
            deferred1.Resolve(1);
            deferred2.Resolve(2);
            deferred3.Resolve(3);

            Assert.AreEqual(winIndex, resultIndex);
            Assert.AreEqual(1, result);
        }

        [Test]
        public void FirstWithIndex_4_T([Values(0, 1, 2, 3)] int winIndex)
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();
            var deferred3 = Promise.NewDeferred<int>();
            var deferred4 = Promise.NewDeferred<int>();

            int resultIndex = -1;
            int result = -1;

            Promise.FirstWithIndex(deferred1.Promise, deferred2.Promise, deferred3.Promise, deferred4.Promise)
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
            deferred1.Resolve(1);
            deferred2.Resolve(2);
            deferred3.Resolve(3);
            deferred4.Resolve(4);

            Assert.AreEqual(winIndex, resultIndex);
            Assert.AreEqual(1, result);
        }

        [Test]
        public void FirstWithIndex_array_T([Values(0, 1, 2, 3)] int winIndex)
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();
            var deferred3 = Promise.NewDeferred<int>();
            var deferred4 = Promise.NewDeferred<int>();

            int resultIndex = -1;
            int result = -1;

            Promise.FirstWithIndex(new Promise<int>[] { deferred1.Promise, deferred2.Promise, deferred3.Promise, deferred4.Promise })
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
            deferred1.Resolve(1);
            deferred2.Resolve(2);
            deferred3.Resolve(3);
            deferred4.Resolve(4);

            Assert.AreEqual(winIndex, resultIndex);
            Assert.AreEqual(1, result);
        }
    }
}