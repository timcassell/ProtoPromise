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

            Promise.Race(deferred1.Promise, deferred2.Promise)
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

            Promise.Race(deferred1.Promise, deferred2.Promise)
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

            Promise.Race(deferred1.Promise, deferred2.Promise)
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

            Promise.Race(deferred1.Promise, deferred2.Promise)
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
        public void RaceIsCanceledWhenFirstPromiseIsCanceledFirst_void0()
        {
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred1 = Promise.NewDeferred(cancelationSource.Token);
            var deferred2 = Promise.NewDeferred();

            bool invoked = false;
            string expected = "Cancel";

            Promise.Race(deferred1.Promise, deferred2.Promise)
                .CatchCancelation(reason =>
                {
                    Assert.AreEqual(expected, reason.Value);
                    invoked = true;
                })
                .Forget();

            cancelationSource.Cancel(expected);

            Assert.IsTrue(invoked);

            deferred2.Resolve();

            cancelationSource.Dispose();

            Assert.IsTrue(invoked);
        }

        [Test]
        public void RaceIsCanceledWhenFirstPromiseIsCanceledFirst_T0()
        {
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred1 = Promise.NewDeferred<int>(cancelationSource.Token);
            var deferred2 = Promise.NewDeferred<int>();

            bool invoked = false;
            string expected = "Cancel";

            Promise.Race(deferred1.Promise, deferred2.Promise)
                .CatchCancelation(reason =>
                {
                    Assert.AreEqual(expected, reason.Value);
                    invoked = true;
                })
                .Forget();

            cancelationSource.Cancel(expected);

            Assert.IsTrue(invoked);

            deferred2.Resolve(5);

            cancelationSource.Dispose();

            Assert.IsTrue(invoked);
        }

        [Test]
        public void RaceIsCanceledWhenFirstPromiseIsCanceledFirst_void1()
        {
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred1 = Promise.NewDeferred(cancelationSource.Token);
            var deferred2 = Promise.NewDeferred();

            bool invoked = false;

            Promise.Race(deferred1.Promise, deferred2.Promise)
                .CatchCancelation(reason =>
                {
                    Assert.IsNull(reason.ValueType);
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
        public void RaceIsCanceledWhenFirstPromiseIsCanceledFirst_T1()
        {
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred1 = Promise.NewDeferred<int>(cancelationSource.Token);
            var deferred2 = Promise.NewDeferred<int>();

            bool invoked = false;

            Promise.Race(deferred1.Promise, deferred2.Promise)
                .CatchCancelation(reason =>
                {
                    Assert.IsNull(reason.ValueType);
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
        public void RaceIsCanceledWhenSecondPromiseIsCanceledFirst_void0()
        {
            var deferred1 = Promise.NewDeferred();
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred2 = Promise.NewDeferred(cancelationSource.Token);

            bool invoked = false;
            string expected = "Cancel";

            Promise.Race(deferred1.Promise, deferred2.Promise)
                .CatchCancelation(reason =>
                {
                    Assert.AreEqual(expected, reason.Value);
                    invoked = true;
                })
                .Forget();

            cancelationSource.Cancel(expected);

            Assert.IsTrue(invoked);

            deferred1.Resolve();

            cancelationSource.Dispose();

            Assert.IsTrue(invoked);
        }

        [Test]
        public void RaceIsCanceledWhenSecondPromiseIsCanceledFirst_T0()
        {
            var deferred1 = Promise.NewDeferred<int>();
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred2 = Promise.NewDeferred<int>(cancelationSource.Token);

            bool invoked = false;
            string expected = "Cancel";

            Promise.Race(deferred1.Promise, deferred2.Promise)
                .CatchCancelation(reason =>
                {
                    Assert.AreEqual(expected, reason.Value);
                    invoked = true;
                })
                .Forget();

            cancelationSource.Cancel(expected);

            Assert.IsTrue(invoked);

            deferred1.Resolve(5);

            cancelationSource.Dispose();

            Assert.IsTrue(invoked);
        }

        [Test]
        public void RaceIsCanceledWhenSecondPromiseIsCanceledFirst_void1()
        {
            var deferred1 = Promise.NewDeferred();
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred2 = Promise.NewDeferred(cancelationSource.Token);

            bool invoked = false;

            Promise.Race(deferred1.Promise, deferred2.Promise)
                .CatchCancelation(reason =>
                {
                    Assert.IsNull(reason.ValueType);
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
        public void RaceIsCanceledWhenSecondPromiseIsCanceledFirst_T1()
        {
            var deferred1 = Promise.NewDeferred<int>();
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred2 = Promise.NewDeferred<int>(cancelationSource.Token);

            bool invoked = false;

            Promise.Race(deferred1.Promise, deferred2.Promise)
                .CatchCancelation(reason =>
                {
                    Assert.IsNull(reason.ValueType);
                    invoked = true;
                })
                .Forget();

            cancelationSource.Cancel();

            Assert.IsTrue(invoked);

            deferred1.Resolve(5);

            cancelationSource.Dispose();

            Assert.IsTrue(invoked);
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
            progressHelper.Subscribe(
                Promise.Race(deferred1.Promise, deferred2.Promise)
            )
                .Forget();

            progressHelper.AssertCurrentProgress(0f);

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
            progressHelper.Subscribe(
                Promise.Race(deferred1.Promise, deferred2.Promise)
            )
                .Forget();

            progressHelper.AssertCurrentProgress(0f);

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
            progressHelper.Subscribe(
                Promise.Race(deferred1.Promise, Promise.Resolved())
            )
                .Forget();

            progressHelper.AssertCurrentProgress(1f);
            progressHelper.ResolveAndAssertResult(deferred1, 1f, false);
        }

        [Test]
        public void RaceProgressReportsTheMaximumProgress_T1(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType)
        {
            var deferred1 = Promise.NewDeferred<int>();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);
            progressHelper.Subscribe(
                Promise.Race(deferred1.Promise, Promise.Resolved(1))
            )
                .Forget();

            progressHelper.AssertCurrentProgress(1f);
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
            progressHelper.Subscribe(
                Promise.Race(
                    deferred1.Promise.ThenDuplicate(),
                    deferred2.Promise.ThenDuplicate()
                )
            )
                .Forget();

            progressHelper.AssertCurrentProgress(0f);

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
            progressHelper.Subscribe(
                Promise.Race(
                    deferred1.Promise.ThenDuplicate(),
                    deferred2.Promise.ThenDuplicate()
                )
            )
                .Forget();

            progressHelper.AssertCurrentProgress(0f);

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
            progressHelper.Subscribe(
                Promise.Race(
                    deferred1.Promise
                        .Then(() => deferred3.Promise),
                    deferred2.Promise
                        .Then(() => deferred4.Promise)
                )
            )
                .Forget();

            progressHelper.AssertCurrentProgress(0f);

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
            progressHelper.Subscribe(
                Promise.Race(
                    deferred1.Promise
                        .Then(() => deferred3.Promise),
                    deferred2.Promise
                        .Then(() => deferred4.Promise)
                )
            )
                .Forget();

            progressHelper.AssertCurrentProgress(0f);

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
            progressHelper.Subscribe(
                Promise.Race(
                    deferred1.Promise
                        .Then(() => Promise.Resolved()),
                    deferred2.Promise
                        .Then(() => Promise.Resolved())
                )
            )
                .Forget();

            progressHelper.AssertCurrentProgress(0f);

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
            progressHelper.Subscribe(
                Promise.Race(
                    deferred1.Promise
                        .Then(x => Promise.Resolved(x)),
                    deferred2.Promise
                        .Then(x => Promise.Resolved(x))
                )
            )
                .Forget();

            progressHelper.AssertCurrentProgress(0f);

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
            progressHelper.Subscribe(
                Promise.Race(deferred1.Promise, deferred2.Promise)
            )
                .Catch(() => { })
                .Forget();

            progressHelper.AssertCurrentProgress(0f);

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
            progressHelper.Subscribe(
                Promise.Race(deferred1.Promise, deferred2.Promise)
            )
                .Catch(() => { })
                .Forget();

            progressHelper.AssertCurrentProgress(0f);

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
            progressHelper.Subscribe(
                Promise.Race(deferred1.Promise, deferred2.Promise)
            )
                .Forget();

            progressHelper.AssertCurrentProgress(0f);

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
            progressHelper.Subscribe(
                Promise.Race(deferred1.Promise, deferred2.Promise)
            )
                .Forget();

            progressHelper.AssertCurrentProgress(0f);

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
            progressHelper.Subscribe(
                Promise.Race(
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

            progressHelper.AssertCurrentProgress(0f / 3f);

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
            progressHelper.Subscribe(
                Promise.Race(
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

            progressHelper.AssertCurrentProgress(0f / 3f);

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
#endif
    }
}