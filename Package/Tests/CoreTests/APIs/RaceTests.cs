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
        public void RaceIsResolvedWhenFirstPromiseIsResolvedFirst_void()
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();

            bool resolved = false;

            Promise.Race(deferred1.Promise, deferred2.Promise)
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
        public void RaceIsResolvedWhenFirstPromiseIsResolvedFirst_T()
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();

            bool resolved = false;

            Promise<int>.Race(deferred1.Promise, deferred2.Promise)
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
        public void RaceIsResolvedWhenSecondPromiseIsResolvedFirst_void()
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();

            bool resolved = false;

            Promise.Race(deferred1.Promise, deferred2.Promise)
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
        public void RaceIsResolvedWhenSecondPromiseIsResolvedFirst_T()
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();

            bool resolved = false;

            Promise<int>.Race(deferred1.Promise, deferred2.Promise)
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
        public void RaceIsRejectedWhenFirstPromiseIsRejectedFirst_void()
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();

            bool invoked = false;
            string expected = "Error";

            Promise.Race(deferred1.Promise, deferred2.Promise)
                .Catch((string rej) =>
                {
                    Assert.AreEqual(expected, rej);
                    invoked = true;
                })
                .Forget();

            deferred1.Reject(expected);

            Assert.IsTrue(invoked);

            deferred2.Resolve();

            Assert.IsTrue(invoked);
        }

        [Test]
        public void RaceIsRejectedWhenFirstPromiseIsRejectedFirst_T()
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();

            bool invoked = false;
            string expected = "Error";

            Promise<int>.Race(deferred1.Promise, deferred2.Promise)
                .Catch((string rej) =>
                {
                    Assert.AreEqual(expected, rej);
                    invoked = true;
                })
                .Forget();

            deferred1.Reject(expected);

            Assert.IsTrue(invoked);

            deferred2.Resolve(5);

            Assert.IsTrue(invoked);
        }

        [Test]
        public void RaceIsRejectedWhenSecondPromiseIsRejectedFirst_void()
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();

            bool invoked = false;
            string expected = "Error";

            Promise.Race(deferred1.Promise, deferred2.Promise)
                .Catch((string rej) =>
                {
                    Assert.AreEqual(expected, rej);
                    invoked = true;
                })
                .Forget();

            deferred2.Reject(expected);

            Assert.IsTrue(invoked);

            deferred1.Resolve();

            Assert.IsTrue(invoked);
        }

        [Test]
        public void RaceIsRejectedWhenSecondPromiseIsRejectedFirst_T()
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();

            bool invoked = false;
            string expected = "Error";

            Promise<int>.Race(deferred1.Promise, deferred2.Promise)
                .Catch((string rej) =>
                {
                    Assert.AreEqual(expected, rej);
                    invoked = true;
                })
                .Forget();

            deferred2.Reject(expected);

            Assert.IsTrue(invoked);

            deferred1.Resolve(5);

            Assert.IsTrue(invoked);
        }

        [Test]
        public void RaceIsCanceledWhenFirstPromiseIsCanceledFirst_void()
        {
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred1 = Promise.NewDeferred();
            cancelationSource.Token.Register(deferred1);
            var deferred2 = Promise.NewDeferred();

            bool invoked = false;

            Promise.Race(deferred1.Promise, deferred2.Promise)
                .CatchCancelation(() =>
                {
                    invoked = true;
                })
                .Forget();

            cancelationSource.Cancel();

            Assert.IsTrue(invoked);

            deferred2.Resolve();

            cancelationSource.Dispose();

            Assert.IsTrue(invoked);
        }

        [Test]
        public void RaceIsCanceledWhenFirstPromiseIsCanceledFirst_T()
        {
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred1 = Promise.NewDeferred<int>();
            cancelationSource.Token.Register(deferred1);
            var deferred2 = Promise.NewDeferred<int>();

            bool invoked = false;

            Promise<int>.Race(deferred1.Promise, deferred2.Promise)
                .CatchCancelation(() =>
                {
                    invoked = true;
                })
                .Forget();

            cancelationSource.Cancel();

            Assert.IsTrue(invoked);

            deferred2.Resolve(5);

            cancelationSource.Dispose();

            Assert.IsTrue(invoked);
        }

        [Test]
        public void RaceIsCanceledWhenSecondPromiseIsCanceledFirst_void()
        {
            var deferred1 = Promise.NewDeferred();
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred2 = Promise.NewDeferred();
            cancelationSource.Token.Register(deferred2);

            bool invoked = false;

            Promise.Race(deferred1.Promise, deferred2.Promise)
                .CatchCancelation(() =>
                {
                    invoked = true;
                })
                .Forget();

            cancelationSource.Cancel();

            Assert.IsTrue(invoked);

            deferred1.Resolve();

            cancelationSource.Dispose();

            Assert.IsTrue(invoked);
        }

        [Test]
        public void RaceIsCanceledWhenSecondPromiseIsCanceledFirst_T()
        {
            var deferred1 = Promise.NewDeferred<int>();
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred2 = Promise.NewDeferred<int>();
            cancelationSource.Token.Register(deferred2);

            bool invoked = false;

            Promise<int>.Race(deferred1.Promise, deferred2.Promise)
                .CatchCancelation(() =>
                {
                    invoked = true;
                })
                .Forget();

            cancelationSource.Cancel();

            Assert.IsTrue(invoked);

            deferred1.Resolve(5);

            cancelationSource.Dispose();

            Assert.IsTrue(invoked);
        }

        private static void Swap(ref Promise.Deferred deferred1, ref Promise.Deferred deferred2)
        {
            var temp = deferred1;
            deferred1 = deferred2;
            deferred2 = temp;
        }

        [Test]
        public void RaceWithIndex_2_void([Values(0, 1)] int winIndex)
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();

            int resultIndex = -1;

            Promise.RaceWithIndex(deferred1.Promise, deferred2.Promise)
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
        public void RaceWithIndex_3_void([Values(0, 1, 2)] int winIndex)
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();
            var deferred3 = Promise.NewDeferred();

            int resultIndex = -1;

            Promise.RaceWithIndex(deferred1.Promise, deferred2.Promise, deferred3.Promise)
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
        public void RaceWithIndex_4_void([Values(0, 1, 2, 3)] int winIndex)
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();
            var deferred3 = Promise.NewDeferred();
            var deferred4 = Promise.NewDeferred();

            int resultIndex = -1;

            Promise.RaceWithIndex(deferred1.Promise, deferred2.Promise, deferred3.Promise, deferred4.Promise)
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
        public void RaceWithIndex_array_void([Values(0, 1, 2, 3)] int winIndex)
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();
            var deferred3 = Promise.NewDeferred();
            var deferred4 = Promise.NewDeferred();

            int resultIndex = -1;

            Promise.RaceWithIndex(new Promise[] { deferred1.Promise, deferred2.Promise, deferred3.Promise, deferred4.Promise })
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
        public void RaceWithIndex_2_T([Values(0, 1)] int winIndex)
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();

            int resultIndex = -1;
            int result = -1;

            Promise.RaceWithIndex(deferred1.Promise, deferred2.Promise)
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
        public void RaceWithIndex_3_T([Values(0, 1, 2)] int winIndex)
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();
            var deferred3 = Promise.NewDeferred<int>();

            int resultIndex = -1;
            int result = -1;

            Promise.RaceWithIndex(deferred1.Promise, deferred2.Promise, deferred3.Promise)
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
        public void RaceWithIndex_4_T([Values(0, 1, 2, 3)] int winIndex)
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();
            var deferred3 = Promise.NewDeferred<int>();
            var deferred4 = Promise.NewDeferred<int>();

            int resultIndex = -1;
            int result = -1;

            Promise.RaceWithIndex(deferred1.Promise, deferred2.Promise, deferred3.Promise, deferred4.Promise)
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
        public void RaceWithIndex_array_T([Values(0, 1, 2, 3)] int winIndex)
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();
            var deferred3 = Promise.NewDeferred<int>();
            var deferred4 = Promise.NewDeferred<int>();

            int resultIndex = -1;
            int result = -1;

            Promise.RaceWithIndex(new Promise<int>[] { deferred1.Promise, deferred2.Promise, deferred3.Promise, deferred4.Promise })
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