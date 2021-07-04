#if !PROTO_PROMISE_PROGRESS_DISABLE
#define PROMISE_PROGRESS
#else
#undef PROMISE_PROGRESS
#endif

using NUnit.Framework;

namespace Proto.Promises.Tests
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

            Promise.Manager.HandleCompletes();
            Assert.IsTrue(resolved);

            deferred2.Resolve();

            Promise.Manager.HandleCompletes();
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

            Promise.Manager.HandleCompletes();
            Assert.IsTrue(resolved);

            deferred2.Resolve(1);

            Promise.Manager.HandleCompletes();
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

            Promise.Manager.HandleCompletes();
            Assert.IsTrue(resolved);

            deferred1.Resolve();

            Promise.Manager.HandleCompletes();
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

            Promise.Manager.HandleCompletes();
            Assert.IsTrue(resolved);

            deferred1.Resolve(1);

            Promise.Manager.HandleCompletes();
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

            Promise.Manager.HandleCompletes();
            Assert.IsTrue(invoked);

            deferred2.Resolve();

            Promise.Manager.HandleCompletes();
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

            Promise.Manager.HandleCompletes();
            Assert.IsTrue(invoked);

            deferred2.Resolve(5);

            Promise.Manager.HandleCompletes();
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

            Promise.Manager.HandleCompletes();
            Assert.IsTrue(invoked);

            deferred1.Resolve();

            Promise.Manager.HandleCompletes();
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

            Promise.Manager.HandleCompletes();
            Assert.IsTrue(invoked);

            deferred1.Resolve(5);

            Promise.Manager.HandleCompletes();
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

            Promise.Manager.HandleCompletes();
            Assert.IsTrue(invoked);

            deferred2.Resolve();

            cancelationSource.Dispose();

            Promise.Manager.HandleCompletes();
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

            Promise.Manager.HandleCompletes();
            Assert.IsTrue(invoked);

            deferred2.Resolve(5);

            cancelationSource.Dispose();

            Promise.Manager.HandleCompletes();
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

            Promise.Manager.HandleCompletes();
            Assert.IsTrue(invoked);

            deferred2.Resolve();

            cancelationSource.Dispose();

            Promise.Manager.HandleCompletes();
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

            Promise.Manager.HandleCompletes();
            Assert.IsTrue(invoked);

            deferred2.Resolve(5);

            cancelationSource.Dispose();

            Promise.Manager.HandleCompletes();
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

            Promise.Manager.HandleCompletes();
            Assert.IsTrue(invoked);

            deferred1.Resolve();

            cancelationSource.Dispose();

            Promise.Manager.HandleCompletes();
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

            Promise.Manager.HandleCompletes();
            Assert.IsTrue(invoked);

            deferred1.Resolve(5);

            cancelationSource.Dispose();

            Promise.Manager.HandleCompletes();
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

            Promise.Manager.HandleCompletes();
            Assert.IsTrue(invoked);

            deferred1.Resolve();

            cancelationSource.Dispose();

            Promise.Manager.HandleCompletes();
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

            Promise.Manager.HandleCompletes();
            Assert.IsTrue(invoked);

            deferred1.Resolve(5);

            cancelationSource.Dispose();

            Promise.Manager.HandleCompletes();
            Assert.IsTrue(invoked);
        }

#if PROMISE_PROGRESS
        [Test]
        public void RaceProgressReportsTheMaximumProgress_void0()
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();

            float progress = float.NaN;

            Promise.Race(deferred1.Promise, deferred2.Promise)
                .Progress(p => progress = p)
                .Forget();

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0f, progress, 0f);

            deferred1.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.3f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.7f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.7f, progress, TestHelper.progressEpsilon);

            deferred1.ReportProgress(0.6f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.7f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.8f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.8f, progress, TestHelper.progressEpsilon);

            deferred1.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.9f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);

            deferred2.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);
        }

        [Test]
        public void RaceProgressReportsTheMaximumProgress_T0()
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();

            float progress = float.NaN;

            Promise.Race(deferred1.Promise, deferred2.Promise)
                .Progress(p => progress = p)
                .Forget();

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0f, progress, 0f);

            deferred1.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.3f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.7f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.7f, progress, TestHelper.progressEpsilon);

            deferred1.ReportProgress(0.6f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.7f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.8f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.8f, progress, TestHelper.progressEpsilon);

            deferred1.Resolve(1);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.9f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);

            deferred2.Resolve(1);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);
        }

        [Test]
        public void RaceProgressReportsTheMaximumProgress_void1()
        {
            for (int i = 0; i < 2; ++i)
            {
                var deferred1 = Promise.NewDeferred();

                float progress = float.NaN;

                Promise.Race(deferred1.Promise, Promise.Resolved())
                    .Progress(p => progress = p)
                    .Forget();

                Promise.Manager.HandleCompletesAndProgress();
                Assert.AreEqual(1f, progress, 0f);

                deferred1.Resolve();
                Promise.Manager.HandleCompletesAndProgress();
                Assert.AreEqual(1f, progress, 0f);
            }
        }

        [Test]
        public void RaceProgressReportsTheMaximumProgress_T1()
        {
            var deferred1 = Promise.NewDeferred<int>();

            float progress = float.NaN;

            Promise.Race(deferred1.Promise, Promise.Resolved(1))
                .Progress(p => progress = p)
                .Forget();

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, 0f);

            deferred1.Resolve(1);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, 0f);
        }

        [Test]
        public void RaceProgressReportsTheMaximumProgress_void2()
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();

            float progress = float.NaN;

            Promise.Race
            (
                deferred1.Promise
                    .Then(() => { }),
                deferred2.Promise
                    .Then(() => { })
            )
                .Progress(p => progress = p)
                .Forget();

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0f, progress, 0f);

            deferred1.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.3f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.7f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.7f, progress, TestHelper.progressEpsilon);

            deferred1.ReportProgress(0.6f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.7f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.8f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.8f, progress, TestHelper.progressEpsilon);

            deferred1.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.9f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);

            deferred2.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);
        }

        [Test]
        public void RaceProgressReportsTheMaximumProgress_T2()
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();

            float progress = float.NaN;

            Promise.Race
            (
                deferred1.Promise
                    .Then(() => 1),
                deferred2.Promise
                    .Then(() => 1)
            )
                .Progress(p => progress = p)
                .Forget();

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0f, progress, 0f);

            deferred1.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.3f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.7f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.7f, progress, TestHelper.progressEpsilon);

            deferred1.ReportProgress(0.6f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.7f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.8f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.8f, progress, TestHelper.progressEpsilon);

            deferred1.Resolve(1);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.9f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);

            deferred2.Resolve(1);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);
        }

        [Test]
        public void RaceProgressReportsTheMaximumProgress_void3()
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();
            var deferred3 = Promise.NewDeferred();
            var deferred4 = Promise.NewDeferred();

            float progress = float.NaN;

            Promise.Race
            (
                deferred1.Promise
                    .Then(() => deferred3.Promise),
                deferred2.Promise
                    .Then(() => deferred4.Promise)
            )
                .Progress(p => progress = p)
                .Forget();

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0f, progress, 0f);

            deferred1.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f / 2f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.3f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f / 2f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.7f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.7f / 2f, progress, TestHelper.progressEpsilon);

            deferred1.ReportProgress(0.6f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.7f / 2f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.8f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.8f / 2f, progress, TestHelper.progressEpsilon);

            deferred1.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f / 2f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.9f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f / 2f, progress, TestHelper.progressEpsilon);

            deferred3.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1.5f / 2f, progress, TestHelper.progressEpsilon);

            deferred2.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1.5f / 2f, progress, TestHelper.progressEpsilon);

            deferred4.ReportProgress(0.3f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1.5f / 2f, progress, TestHelper.progressEpsilon);

            deferred4.ReportProgress(0.7f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1.7f / 2f, progress, TestHelper.progressEpsilon);

            deferred3.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);

            deferred4.ReportProgress(0.9f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);

            deferred4.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);
        }

        [Test]
        public void RaceProgressReportsTheMaximumProgress_T3()
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();
            var deferred3 = Promise.NewDeferred<int>();
            var deferred4 = Promise.NewDeferred<int>();

            float progress = float.NaN;

            Promise.Race
            (
                deferred1.Promise
                    .Then(() => deferred3.Promise),
                deferred2.Promise
                    .Then(() => deferred4.Promise)
            )
                .Progress(p => progress = p)
                .Forget();

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0f, progress, 0f);

            deferred1.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f / 2f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.3f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f / 2f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.7f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.7f / 2f, progress, TestHelper.progressEpsilon);

            deferred1.ReportProgress(0.6f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.7f / 2f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.8f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.8f / 2f, progress, TestHelper.progressEpsilon);

            deferred1.Resolve(1);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f / 2f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.9f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f / 2f, progress, TestHelper.progressEpsilon);

            deferred3.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1.5f / 2f, progress, TestHelper.progressEpsilon);

            deferred2.Resolve(1);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1.5f / 2f, progress, TestHelper.progressEpsilon);

            deferred4.ReportProgress(0.3f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1.5f / 2f, progress, TestHelper.progressEpsilon);

            deferred4.ReportProgress(0.7f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1.7f / 2f, progress, TestHelper.progressEpsilon);

            deferred3.Resolve(1);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);

            deferred4.ReportProgress(0.9f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);

            deferred4.Resolve(1);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);
        }

        [Test]
        public void RaceProgressReportsTheMaximumProgress_void4()
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();

            float progress = float.NaN;

            Promise.Race
            (
                deferred1.Promise
                    .Then(Promise.Resolved),
                deferred2.Promise
                    .Then(Promise.Resolved)
            )
                .Progress(p => progress = p)
                .Forget();

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0f, progress, 0f);

            deferred1.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f / 2f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.3f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f / 2f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.7f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.7f / 2f, progress, TestHelper.progressEpsilon);

            deferred1.ReportProgress(0.6f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.7f / 2f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.8f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.8f / 2f, progress, TestHelper.progressEpsilon);

            deferred1.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.9f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);

            deferred2.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);
        }

        [Test]
        public void RaceProgressReportsTheMaximumProgress_T4()
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();

            float progress = float.NaN;

            Promise.Race
            (
                deferred1.Promise
                    .Then(x => Promise.Resolved(x)),
                deferred2.Promise
                    .Then(x => Promise.Resolved(x))
            )
                .Progress(p => progress = p)
                .Forget();

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0f, progress, 0f);

            deferred1.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f / 2f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.3f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f / 2f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.7f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.7f / 2f, progress, TestHelper.progressEpsilon);

            deferred1.ReportProgress(0.6f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.7f / 2f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.8f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.8f / 2f, progress, TestHelper.progressEpsilon);

            deferred1.Resolve(1);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.9f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);

            deferred2.Resolve(1);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);
        }

        [Test]
        public void RaceProgressIsNoLongerReportedFromRejected_void()
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();

            float progress = float.NaN;

            Promise.Race(deferred1.Promise, deferred2.Promise)
                .Progress(p => progress = p)
                .Catch(() => { })
                .Forget();

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0f, progress, 0f);

            deferred1.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.7f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.7f, progress, TestHelper.progressEpsilon);

            deferred2.Reject("Reject");
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.7f, progress, TestHelper.progressEpsilon);

            deferred1.ReportProgress(0.8f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.7f, progress, TestHelper.progressEpsilon);

            deferred1.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.7f, progress, TestHelper.progressEpsilon);
        }

        [Test]
        public void RaceProgressIsNoLongerReportedFromRejected_T()
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();

            float progress = float.NaN;

            Promise.Race(deferred1.Promise, deferred2.Promise)
                .Progress(p => progress = p)
                .Catch(() => { })
                .Forget();

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0f, progress, 0f);

            deferred1.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.7f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.7f, progress, TestHelper.progressEpsilon);

            deferred2.Reject("Reject");
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.7f, progress, TestHelper.progressEpsilon);

            deferred1.ReportProgress(0.8f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.7f, progress, TestHelper.progressEpsilon);

            deferred1.Resolve(1);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.7f, progress, TestHelper.progressEpsilon);
        }

        [Test]
        public void RaceProgressIsNoLongerReportedFromCanceled_void()
        {
            var deferred1 = Promise.NewDeferred();
            var cancelationSource1 = CancelationSource.New();
            var deferred2 = Promise.NewDeferred(cancelationSource1.Token);

            float progress = float.NaN;

            Promise.Race(deferred1.Promise, deferred2.Promise)
                .Progress(p => progress = p)
                .Forget();

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0f, progress, 0f);

            deferred1.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.7f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.7f, progress, TestHelper.progressEpsilon);

            cancelationSource1.Cancel();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.7f, progress, TestHelper.progressEpsilon);

            deferred1.ReportProgress(0.8f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.7f, progress, TestHelper.progressEpsilon);

            deferred1.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.7f, progress, TestHelper.progressEpsilon);

            cancelationSource1.Dispose();
        }

        [Test]
        public void RaceProgressIsNoLongerReportedFromCanceled_T()
        {
            var deferred1 = Promise.NewDeferred();
            var cancelationSource1 = CancelationSource.New();
            var deferred2 = Promise.NewDeferred(cancelationSource1.Token);

            float progress = float.NaN;

            Promise.Race(deferred1.Promise, deferred2.Promise)
                .Progress(p => progress = p)
                .Forget();

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0f, progress, 0f);

            deferred1.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.7f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.7f, progress, TestHelper.progressEpsilon);

            cancelationSource1.Cancel();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.7f, progress, TestHelper.progressEpsilon);

            deferred1.ReportProgress(0.8f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.7f, progress, TestHelper.progressEpsilon);

            deferred1.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.7f, progress, TestHelper.progressEpsilon);

            cancelationSource1.Dispose();
        }

        [Test]
        public void RaceProgressWillBeInvokedProperlyFromARecoveredPromise_void()
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();
            var deferred3 = Promise.NewDeferred();
            var deferred4 = Promise.NewDeferred();
            var cancelationSource = CancelationSource.New();

            float progress = float.NaN;

            Promise.Race
            (
                // Make first and second promise chains the same length
                deferred1.Promise
                    .Then(Promise.Resolved)
                    .Then(Promise.Resolved),
                deferred2.Promise
                    .Then(() => deferred3.Promise, cancelationSource.Token)
                    .ContinueWith(_ => deferred4.Promise)
            )
                .Progress(p => progress = p)
                .Forget();

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0f / 3f, progress, TestHelper.progressEpsilon);

            deferred1.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f / 3f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.25f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f / 3f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.6f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.6f / 3f, progress, TestHelper.progressEpsilon);

            cancelationSource.Cancel();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(2f / 3f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.7f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(2f / 3f, progress, TestHelper.progressEpsilon);

            deferred3.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(2f / 3f, progress, TestHelper.progressEpsilon);

            deferred3.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(2f / 3f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.8f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(2f / 3f, progress, TestHelper.progressEpsilon);

            deferred4.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(2.5f / 3f, progress, TestHelper.progressEpsilon);

            deferred4.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.8f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);

            deferred2.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);

            deferred1.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);

            cancelationSource.Dispose();
            deferred3.Promise.Forget(); // Need to forget this promise because it was never awaited due to the cancelation.
        }

        [Test]
        public void RaceProgressWillBeInvokedProperlyFromARecoveredPromise_T()
        {
            var deferred1 = Promise.NewDeferred<int>();
            var deferred2 = Promise.NewDeferred<int>();
            var deferred3 = Promise.NewDeferred<int>();
            var deferred4 = Promise.NewDeferred<int>();
            var cancelationSource = CancelationSource.New();

            float progress = float.NaN;

            Promise.Race
            (
                // Make first and second promise chains the same length
                deferred1.Promise
                    .Then(x => Promise.Resolved(x))
                    .Then(x => Promise.Resolved(x)),
                deferred2.Promise
                    .Then(() => deferred3.Promise, cancelationSource.Token)
                    .ContinueWith(_ => deferred4.Promise)
            )
                .Progress(p => progress = p)
                .Forget();

            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0f / 3f, progress, TestHelper.progressEpsilon);

            deferred1.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f / 3f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.25f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.5f / 3f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.6f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.6f / 3f, progress, TestHelper.progressEpsilon);

            cancelationSource.Cancel();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(2f / 3f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.7f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(2f / 3f, progress, TestHelper.progressEpsilon);

            deferred3.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(2f / 3f, progress, TestHelper.progressEpsilon);

            deferred3.Resolve(1);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(2f / 3f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.8f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(2f / 3f, progress, TestHelper.progressEpsilon);

            deferred4.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(2.5f / 3f, progress, TestHelper.progressEpsilon);

            deferred4.Resolve(1);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);

            deferred2.ReportProgress(0.8f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);

            deferred2.Resolve(1);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);

            deferred1.Resolve(1);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(1f, progress, TestHelper.progressEpsilon);

            cancelationSource.Dispose();
            deferred3.Promise.Forget(); // Need to forget this promise because it was never awaited due to the cancelation.
        }
#endif
    }
}