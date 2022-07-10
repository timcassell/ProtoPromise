#if !PROTO_PROMISE_PROGRESS_DISABLE
#define PROMISE_PROGRESS
#else
#undef PROMISE_PROGRESS
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
            var deferred1 = Promise.NewDeferred(cancelationSource.Token);
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
            var deferred1 = Promise.NewDeferred<int>(cancelationSource.Token);
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
            var deferred2 = Promise.NewDeferred(cancelationSource.Token);

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
            var deferred2 = Promise.NewDeferred<int>(cancelationSource.Token);

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

            Promise<int>.RaceWithIndex(deferred1.Promise, deferred2.Promise)
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

            Promise<int>.RaceWithIndex(deferred1.Promise, deferred2.Promise, deferred3.Promise)
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

            Promise<int>.RaceWithIndex(deferred1.Promise, deferred2.Promise, deferred3.Promise, deferred4.Promise)
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

            Promise<int>.RaceWithIndex(new Promise<int>[] { deferred1.Promise, deferred2.Promise, deferred3.Promise, deferred4.Promise })
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

#if PROMISE_PROGRESS
        [Test]
        public void RaceProgressReportsTheMaximumProgress_void0(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            Promise.Race(deferred1.Promise, deferred2.Promise)
                .SubscribeProgressAndAssert(progressHelper, 0f)
                .Forget();

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
        public void RaceProgressReportsTheMaximumProgress_T0(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            Promise<int>.Race(deferred1.Promise, deferred2.Promise)
                .SubscribeProgressAndAssert(progressHelper, 0f)
                .Forget();

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
        public void RaceProgressReportsTheMaximumProgress_void1(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred1 = Promise.NewDeferred();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            Promise.Race(deferred1.Promise, Promise.Resolved())
                .SubscribeProgressAndAssert(progressHelper, 1f)
                .Forget();

            progressHelper.ResolveAndAssertResult(deferred1, 1f, false);
        }

        [Test]
        public void RaceProgressReportsTheMaximumProgress_T1(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred1 = Promise.NewDeferred<int>();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            Promise<int>.Race(deferred1.Promise, Promise.Resolved(1))
                .SubscribeProgressAndAssert(progressHelper, 1f)
                .Forget();

            progressHelper.ResolveAndAssertResult(deferred1, 1, 1f, false);
        }

        [Test]
        public void RaceProgressReportsTheMaximumProgress_void2(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            Promise.Race(
                deferred1.Promise.ThenDuplicate(),
                deferred2.Promise.ThenDuplicate()
            )
                .SubscribeProgressAndAssert(progressHelper, 0f)
                .Forget();

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
        public void RaceProgressReportsTheMaximumProgress_T2(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            Promise<int>.Race(
                deferred1.Promise.ThenDuplicate(),
                deferred2.Promise.ThenDuplicate()
            )
                .SubscribeProgressAndAssert(progressHelper, 0f)
                .Forget();

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
        public void RaceProgressReportsTheMaximumProgress_void3(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();
            var deferred3 = Promise.NewDeferred();
            var deferred4 = Promise.NewDeferred();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            Promise.Race(
                deferred1.Promise
                    .Then(() => deferred3.Promise),
                deferred2.Promise
                    .Then(() => deferred4.Promise)
            )
                .SubscribeProgressAndAssert(progressHelper, 0f)
                .Forget();

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
        public void RaceProgressReportsTheMaximumProgress_T3(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();
            var deferred3 = Promise.NewDeferred<int>();
            var deferred4 = Promise.NewDeferred<int>();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            Promise<int>.Race(
                deferred1.Promise
                    .Then(() => deferred3.Promise),
                deferred2.Promise
                    .Then(() => deferred4.Promise)
            )
                .SubscribeProgressAndAssert(progressHelper, 0f)
                .Forget();

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
        public void RaceProgressReportsTheMaximumProgress_void4(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            Promise.Race(
                deferred1.Promise
                    .Then(() => Promise.Resolved()),
                deferred2.Promise
                    .Then(() => Promise.Resolved())
            )
                .SubscribeProgressAndAssert(progressHelper, 0f)
                .Forget();

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
        public void RaceProgressReportsTheMaximumProgress_T4(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            Promise<int>.Race(
                deferred1.Promise
                    .Then(x => Promise.Resolved(x)),
                deferred2.Promise
                    .Then(x => Promise.Resolved(x))
            )
                .SubscribeProgressAndAssert(progressHelper, 0f)
                .Forget();

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
        public void RaceProgressIsNoLongerReportedFromRejected_void(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            Promise.Race(deferred1.Promise, deferred2.Promise)
                .SubscribeProgressAndAssert(progressHelper, 0f)
                .Catch(() => { })
                .Forget();

            progressHelper.ReportProgressAndAssertResult(deferred1, 0.5f, 0.5f);
            progressHelper.ReportProgressAndAssertResult(deferred2, 0.7f, 0.7f);

            progressHelper.RejectAndAssertResult(deferred2, "Reject", 0.7f, false);

            progressHelper.ReportProgressAndAssertResult(deferred1, 0.8f, 0.7f, false);
            progressHelper.ResolveAndAssertResult(deferred1, 0.7f, false);
        }

        [Test]
        public void RaceProgressIsNoLongerReportedFromRejected_T(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            Promise<int>.Race(deferred1.Promise, deferred2.Promise)
                .SubscribeProgressAndAssert(progressHelper, 0f)
                .Catch(() => { })
                .Forget();

            progressHelper.ReportProgressAndAssertResult(deferred1, 0.5f, 0.5f);
            progressHelper.ReportProgressAndAssertResult(deferred2, 0.7f, 0.7f);

            progressHelper.RejectAndAssertResult(deferred2, "Reject", 0.7f, false);

            progressHelper.ReportProgressAndAssertResult(deferred1, 0.8f, 0.7f, false);
            progressHelper.ResolveAndAssertResult(deferred1, 1, 0.7f, false);
        }

        [Test]
        public void RaceProgressIsNoLongerReportedFromCanceled_void(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred1 = Promise.NewDeferred();
            var cancelationSource1 = CancelationSource.New();
            var deferred2 = Promise.NewDeferred(cancelationSource1.Token);

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            Promise.Race(deferred1.Promise, deferred2.Promise)
                .SubscribeProgressAndAssert(progressHelper, 0f)
                .Forget();

            progressHelper.ReportProgressAndAssertResult(deferred1, 0.5f, 0.5f);
            progressHelper.ReportProgressAndAssertResult(deferred2, 0.7f, 0.7f);

            progressHelper.CancelAndAssertResult(cancelationSource1, 0.7f, false);

            progressHelper.ReportProgressAndAssertResult(deferred1, 0.8f, 0.7f, false);
            progressHelper.ResolveAndAssertResult(deferred1, 0.7f, false);

            cancelationSource1.Dispose();
        }

        [Test]
        public void RaceProgressIsNoLongerReportedFromCanceled_T(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred1 = Promise.NewDeferred<int>();
            var cancelationSource1 = CancelationSource.New();
            var deferred2 = Promise.NewDeferred<int>(cancelationSource1.Token);

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            Promise<int>.Race(deferred1.Promise, deferred2.Promise)
                .SubscribeProgressAndAssert(progressHelper, 0f)
                .Forget();

            progressHelper.ReportProgressAndAssertResult(deferred1, 0.5f, 0.5f);
            progressHelper.ReportProgressAndAssertResult(deferred2, 0.7f, 0.7f);

            progressHelper.CancelAndAssertResult(cancelationSource1, 0.7f, false);

            progressHelper.ReportProgressAndAssertResult(deferred1, 0.8f, 0.7f, false);
            progressHelper.ResolveAndAssertResult(deferred1, 1, 0.7f, false);

            cancelationSource1.Dispose();
        }

        [Test]
        public void RaceProgressWillBeInvokedProperlyFromARecoveredPromise_void(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();
            var deferred3 = Promise.NewDeferred();
            var deferred4 = Promise.NewDeferred();
            var cancelationSource = CancelationSource.New();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            Promise.Race(
                // Make first and second promise chains the same length
                deferred1.Promise
                    .Then(() => Promise.Resolved())
                    .Then(() => Promise.Resolved()),
                deferred2.Promise
                    .Then(() => deferred3.Promise, cancelationSource.Token)
                    .ContinueWith(_ => deferred4.Promise)
            )
                .SubscribeProgressAndAssert(progressHelper, 0f / 3f)
                .Forget();

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
        public void RaceProgressWillBeInvokedProperlyFromARecoveredPromise_T(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();
            var deferred3 = Promise.NewDeferred<int>();
            var deferred4 = Promise.NewDeferred<int>();
            var cancelationSource = CancelationSource.New();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            Promise<int>.Race(
                // Make first and second promise chains the same length
                deferred1.Promise
                    .Then(x => Promise.Resolved(x))
                    .Then(x => Promise.Resolved(x)),
                deferred2.Promise
                    .Then(() => deferred3.Promise, cancelationSource.Token)
                    .ContinueWith(_ => deferred4.Promise)
            )
                .SubscribeProgressAndAssert(progressHelper, 0f / 3f)
                .Forget();

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

        [Test]
        public void RaceProgressWillBeInvokedProperlyFromChainedPromise_FlatDepth_void([Values] bool isPending)
        {
            // Testing an implementation detail, not guaranteed by the API - Promise.Race's depth is set to the shortest promise chain's depth.
            // We test if all promises are already resolved to make sure progress reports remain consistent.
            var maybePendingDeferred = isPending
                ? Promise.NewDeferred()
                : default(Promise.Deferred);
            var deferred2 = Promise.NewDeferred();
            var deferred3 = Promise.NewDeferred();
            var deferred4 = Promise.NewDeferred();

            // .Then waiting on another promise increases the depth of the promise chain from 0 to 1.
            var promise1 = (isPending ? maybePendingDeferred.Promise : Promise.Resolved())
                .Then(() => Promise.Resolved());
            var promise2 = deferred2.Promise
                .Then(() => Promise.Resolved());
            var promise3 = deferred3.Promise
                .Then(() => Promise.Resolved());
            var promise4 = deferred4.Promise
                .Then(() => Promise.Resolved());

            var deferredForProgress = Promise.NewDeferred();

            ProgressHelper progressHelper = new ProgressHelper(ProgressType.Interface, SynchronizationType.Synchronous);
            Promise.Race(promise1, promise2, promise3, promise4)
                .Then(() => deferredForProgress.Promise) // Increases the depth to 2.
                .SubscribeProgressAndAssert(progressHelper, isPending ? 0f : 2f / 3f)
                .Forget();

            maybePendingDeferred.TryResolve();

            progressHelper.AssertCurrentProgress(2f / 3f);

            progressHelper.ReportProgressAndAssertResult(deferredForProgress, 0.5f, 2.5f / 3f);
            progressHelper.ResolveAndAssertResult(deferredForProgress, 3f / 3f);

            deferred2.Resolve();
            deferred3.Resolve();
            deferred4.Resolve();
        }

        [Test]
        public void RaceProgressWillBeInvokedProperlyFromChainedPromise_FlatDepth_T([Values] bool isPending)
        {
            // Testing an implementation detail, not guaranteed by the API - Promise.Race's depth is set to the shortest promise chain's depth.
            // We test if all promises are already resolved to make sure progress reports remain consistent.
            var maybePendingDeferred = isPending
                ? Promise.NewDeferred()
                : default(Promise.Deferred);
            var deferred2 = Promise.NewDeferred();
            var deferred3 = Promise.NewDeferred();
            var deferred4 = Promise.NewDeferred();

            // .Then waiting on another promise increases the depth of the promise chain from 0 to 1.
            var promise1 = (isPending ? maybePendingDeferred.Promise : Promise.Resolved())
                .Then(() => Promise.Resolved(1));
            var promise2 = deferred2.Promise
                .Then(() => Promise.Resolved(2));
            var promise3 = deferred3.Promise
                .Then(() => Promise.Resolved(3));
            var promise4 = deferred4.Promise
                .Then(() => Promise.Resolved(4));

            var deferredForProgress = Promise.NewDeferred();

            ProgressHelper progressHelper = new ProgressHelper(ProgressType.Interface, SynchronizationType.Synchronous);
            Promise<int>.Race(promise1, promise2, promise3, promise4)
                .Then(() => deferredForProgress.Promise) // Increases the depth to 2.
                .SubscribeProgressAndAssert(progressHelper, isPending ? 0f : 2f / 3f)
                .Forget();

            maybePendingDeferred.TryResolve();

            progressHelper.AssertCurrentProgress(2f / 3f);

            progressHelper.ReportProgressAndAssertResult(deferredForProgress, 0.5f, 2.5f / 3f);
            progressHelper.ResolveAndAssertResult(deferredForProgress, 3f / 3f);

            deferred2.Resolve();
            deferred3.Resolve();
            deferred4.Resolve();
        }

        [Test]
        public void RaceProgressWillBeInvokedProperlyFromChainedPromise_StaggeredDepth_void([Values] bool isPending)
        {
            // Testing an implementation detail, not guaranteed by the API - Promise.Race's depth is set to the shortest promise chain's depth.
            // We test if all promises are already resolved to make sure progress reports remain consistent.
            var maybePendingDeferred = isPending
                ? Promise.NewDeferred()
                : default(Promise.Deferred);
            var deferred2 = Promise.NewDeferred();
            var deferred3 = Promise.NewDeferred();
            var deferred4 = Promise.NewDeferred();

            var promise1 = isPending ? maybePendingDeferred.Promise : Promise.Resolved();
            // .Then waiting on another promise increases the depth of the promise chain from 0 to 1.
            var promise2 = deferred2.Promise
                .Then(() => Promise.Resolved());
            var promise3 = deferred3.Promise
                .Then(() => Promise.Resolved());
            var promise4 = deferred4.Promise
                .Then(() => Promise.Resolved());

            var deferredForProgress = Promise.NewDeferred();

            ProgressHelper progressHelper = new ProgressHelper(ProgressType.Interface, SynchronizationType.Synchronous);
            Promise.Race(promise1, promise2, promise3, promise4)
                .Then(() => deferredForProgress.Promise) // Increases the depth to 2.
                .SubscribeProgressAndAssert(progressHelper, isPending ? 0f : 1f / 2f)
                .Forget();

            maybePendingDeferred.TryResolve();

            progressHelper.AssertCurrentProgress(1f / 2f);

            progressHelper.ReportProgressAndAssertResult(deferredForProgress, 0.5f, 1.5f / 2f);
            progressHelper.ResolveAndAssertResult(deferredForProgress, 2f / 2f);

            deferred2.Resolve();
            deferred3.Resolve();
            deferred4.Resolve();
        }

        [Test]
        public void RaceProgressWillBeInvokedProperlyFromChainedPromise_StaggeredDepth_T([Values] bool isPending)
        {
            // Testing an implementation detail, not guaranteed by the API - Promise.Race's depth is set to the shortest promise chain's depth.
            // We test if all promises are already resolved to make sure progress reports remain consistent.
            var maybePendingDeferred = isPending
                ? Promise.NewDeferred<int>()
                : default(Promise<int>.Deferred);
            var deferred2 = Promise.NewDeferred();
            var deferred3 = Promise.NewDeferred();
            var deferred4 = Promise.NewDeferred();

            var promise1 = isPending ? maybePendingDeferred.Promise : Promise.Resolved(1);
            // .Then waiting on another promise increases the depth of the promise chain from 0 to 1.
            var promise2 = deferred2.Promise
                .Then(() => Promise.Resolved(2));
            var promise3 = deferred3.Promise
                .Then(() => Promise.Resolved(3));
            var promise4 = deferred4.Promise
                .Then(() => Promise.Resolved(4));

            var deferredForProgress = Promise.NewDeferred();

            ProgressHelper progressHelper = new ProgressHelper(ProgressType.Interface, SynchronizationType.Synchronous);
            Promise<int>.Race(promise1, promise2, promise3, promise4)
                .Then(() => deferredForProgress.Promise) // Increases the depth to 2.
                .SubscribeProgressAndAssert(progressHelper, isPending ? 0f : 1f / 2f)
                .Forget();

            maybePendingDeferred.TryResolve(1);

            progressHelper.AssertCurrentProgress(1f / 2f);

            progressHelper.ReportProgressAndAssertResult(deferredForProgress, 0.5f, 1.5f / 2f);
            progressHelper.ResolveAndAssertResult(deferredForProgress, 2f / 2f);

            deferred2.Resolve();
            deferred3.Resolve();
            deferred4.Resolve();
        }
#endif // PROMISE_PROGRESS
    }
}