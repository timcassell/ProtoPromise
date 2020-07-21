#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using System;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Proto.Promises.Tests
{
    public class PromiseCancelationTests
    {
        [SetUp]
        public void Setup()
        {
            TestHelper.cachedRejectionHandler = Promise.Config.UncaughtRejectionHandler;
            Promise.Config.UncaughtRejectionHandler = null;
        }

        [TearDown]
        public void Teardown()
        {
            Promise.Config.UncaughtRejectionHandler = TestHelper.cachedRejectionHandler;
        }

        public class WhenPendingAPromise
        {
            [SetUp]
            public void Setup()
            {
                TestHelper.cachedRejectionHandler = Promise.Config.UncaughtRejectionHandler;
                Promise.Config.UncaughtRejectionHandler = null;
            }

            [TearDown]
            public void Teardown()
            {
                Promise.Config.UncaughtRejectionHandler = TestHelper.cachedRejectionHandler;
            }

            [Test]
            public void MayTransitionToEitherTheFulfilledOrRejectedOrCanceledState()
            {
                var deferred = Promise.NewDeferred();
                Assert.AreEqual(Promise.State.Pending, deferred.State);

                deferred.Resolve();

                Assert.AreEqual(Promise.State.Resolved, deferred.State);

                deferred = Promise.NewDeferred();

                Assert.AreEqual(Promise.State.Pending, deferred.State);

                deferred.Reject("Fail Value");

                Assert.AreEqual(Promise.State.Rejected, deferred.State);

                CancelationSource cancelationSource = CancelationSource.New();
                deferred = Promise.NewDeferred(cancelationSource.Token);

                Assert.AreEqual(Promise.State.Pending, deferred.State);

                cancelationSource.Cancel();

                Assert.AreEqual(Promise.State.Canceled, deferred.State);

                Assert.Throws<AggregateException>(Promise.Manager.HandleCompletes);

                // Clean up.
                cancelationSource.Dispose();
                GC.Collect();
                Promise.Manager.HandleCompletesAndProgress();
                LogAssert.NoUnexpectedReceived();
            }
        }

        public class WhenFulfilledAPromise
        {
            [SetUp]
            public void Setup()
            {
                TestHelper.cachedRejectionHandler = Promise.Config.UncaughtRejectionHandler;
                Promise.Config.UncaughtRejectionHandler = null;
            }

            [TearDown]
            public void Teardown()
            {
                Promise.Config.UncaughtRejectionHandler = TestHelper.cachedRejectionHandler;
            }

            [Test]
            public void MustNotTransitionToAnyOtherState()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                var deferred = Promise.NewDeferred(cancelationSource.Token);
                var deferredInt = Promise.NewDeferred<int>(cancelationSource.Token);

                Assert.AreEqual(Promise.State.Pending, deferred.State);
                Assert.AreEqual(Promise.State.Pending, deferredInt.State);

                int resolved = 0;

                deferred.Promise
                    .Then(() => { ++resolved; })
                    .Catch(() => Assert.Fail("Promise was rejected when it was already resolved."))
                    .CatchCancelation(e => Assert.Fail("Promise was canceled when it was already resolved."));
                deferredInt.Promise
                    .Then(v => { ++resolved; })
                    .Catch(() => Assert.Fail("Promise was rejected when it was already resolved."))
                    .CatchCancelation(e => Assert.Fail("Promise was canceled when it was already resolved."));

                deferred.Resolve();
                deferredInt.Resolve(0);

                LogAssert.Expect(UnityEngine.LogType.Warning, "Deferred.Reject - Deferred is not in the pending state.");
                deferred.Reject("Fail Value");
                LogAssert.Expect(UnityEngine.LogType.Warning, "Deferred.Reject - Deferred is not in the pending state.");
                deferredInt.Reject("Fail Value");

                cancelationSource.Cancel();

                Assert.AreEqual(Promise.State.Resolved, deferred.State);
                Assert.AreEqual(Promise.State.Resolved, deferredInt.State);

                Assert.Throws<AggregateException>(Promise.Manager.HandleCompletes);

                Assert.AreEqual(2, resolved);

                // Clean up.
                cancelationSource.Dispose();
                GC.Collect();
                Promise.Manager.HandleCompletesAndProgress();
                LogAssert.NoUnexpectedReceived();
            }
        }

        public class WhenRejectedAPromise
        {
            [SetUp]
            public void Setup()
            {
                TestHelper.cachedRejectionHandler = Promise.Config.UncaughtRejectionHandler;
                Promise.Config.UncaughtRejectionHandler = null;
            }

            [TearDown]
            public void Teardown()
            {
                Promise.Config.UncaughtRejectionHandler = TestHelper.cachedRejectionHandler;
            }

            [Test]
            public void MustNotTransitionToAnyOtherState()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                var deferred = Promise.NewDeferred(cancelationSource.Token);
                var deferredInt = Promise.NewDeferred<int>(cancelationSource.Token);

                Assert.AreEqual(Promise.State.Pending, deferred.State);
                Assert.AreEqual(Promise.State.Pending, deferredInt.State);

                int rejected = 0;

                deferred.Promise
                    .Then(() => Assert.Fail("Promise was resolved when it was already rejected."))
                    .Catch(() => { ++rejected; })
                    .CatchCancelation(e => Assert.Fail("Promise was canceled when it was already rejected."));
                deferredInt.Promise
                    .Then(() => Assert.Fail("Promise was resolved when it was already rejected."))
                    .Catch(() => { ++rejected; })
                    .CatchCancelation(e => Assert.Fail("Promise was canceled when it was already rejected."));

                deferred.Reject("Fail Value");
                deferredInt.Reject("Fail Value");

                LogAssert.Expect(UnityEngine.LogType.Warning, "Deferred.Resolve - Deferred is not in the pending state.");
                deferred.Resolve();
                LogAssert.Expect(UnityEngine.LogType.Warning, "Deferred.Resolve - Deferred is not in the pending state.");
                deferredInt.Resolve(0);

                cancelationSource.Cancel();

                Assert.AreEqual(Promise.State.Rejected, deferred.State);
                Assert.AreEqual(Promise.State.Rejected, deferredInt.State);

                Promise.Manager.HandleCompletes();

                Assert.AreEqual(2, rejected);

                // Clean up.
                cancelationSource.Dispose();
                GC.Collect();
                Promise.Manager.HandleCompletesAndProgress();
                LogAssert.NoUnexpectedReceived();
            }
        }

        public class WhenCanceledAPromise
        {
            [SetUp]
            public void Setup()
            {
                TestHelper.cachedRejectionHandler = Promise.Config.UncaughtRejectionHandler;
                Promise.Config.UncaughtRejectionHandler = null;
            }

            [TearDown]
            public void Teardown()
            {
                Promise.Config.UncaughtRejectionHandler = TestHelper.cachedRejectionHandler;
            }

            [Test]
            public void MustNotTransitionToAnyOtherState()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                var deferred = Promise.NewDeferred(cancelationSource.Token);
                var deferredInt = Promise.NewDeferred<int>(cancelationSource.Token);

                Assert.AreEqual(Promise.State.Pending, deferred.State);
                Assert.AreEqual(Promise.State.Pending, deferredInt.State);

                int canceled = 0;

                deferred.Promise
                    .Then(() => Assert.Fail("Promise was resolved when it was already canceled."))
                    .Catch(() => Assert.Fail("Promise was rejected when it was already canceled."))
                    .CatchCancelation(e => { ++canceled; });
                deferredInt.Promise
                    .Then(v => Assert.Fail("Promise was resolved when it was already canceled."))
                    .Catch(() => Assert.Fail("Promise was rejected when it was already canceled."))
                    .CatchCancelation(e => { ++canceled; });

                cancelationSource.Cancel("Cancel Value");

                LogAssert.Expect(UnityEngine.LogType.Warning, "Deferred.Resolve - Deferred is not in the pending state.");
                deferred.Resolve();
                LogAssert.Expect(UnityEngine.LogType.Warning, "Deferred.Resolve - Deferred is not in the pending state.");
                deferredInt.Resolve(0);

                LogAssert.Expect(UnityEngine.LogType.Warning, "Deferred.Reject - Deferred is not in the pending state.");
                deferred.Reject("Fail Value");
                LogAssert.Expect(UnityEngine.LogType.Warning, "Deferred.Reject - Deferred is not in the pending state.");
                deferredInt.Reject("Fail Value");

                Assert.AreEqual(Promise.State.Canceled, deferred.State);
                Assert.AreEqual(Promise.State.Canceled, deferredInt.State);

                Assert.Throws<AggregateException>(Promise.Manager.HandleCompletes);

                Assert.AreEqual(2, canceled);

                // Clean up.
                cancelationSource.Dispose();
                GC.Collect();
                Promise.Manager.HandleCompletesAndProgress();
                LogAssert.NoUnexpectedReceived();
            }

            [Test]
            public void MustHaveAReasonWhichMustNotChange_0()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                var deferred = Promise.NewDeferred(cancelationSource.Token);

                Assert.AreEqual(Promise.State.Pending, deferred.State);

                deferred.Retain();
                object cancelation = null;
                string expected = "Cancel Value";

                Action rejectAssert = () => Assert.Fail("Promise was rejected when it should have been resolved.");
                Action resolveAssert = () => Assert.Fail("Promise was resolved when it should have been rejected.");

                TestHelper.AddCallbacks<int, object, string>(deferred.Promise,
                    onResolve: resolveAssert,
                    onReject: failValue => rejectAssert(),
                    onUnknownRejection: rejectAssert);
                deferred.Promise.CatchCancelation(cancelValue => Assert.AreEqual(expected, cancelation = cancelValue.Value));
                cancelationSource.Cancel(expected);
                Promise.Manager.HandleCompletes();

                Assert.AreEqual(expected, cancelation);

                TestHelper.AddCallbacks<int, object, string>(deferred.Promise,
                    onResolve: resolveAssert,
                    onReject: failValue => rejectAssert(),
                    onUnknownRejection: rejectAssert);
                deferred.Promise.CatchCancelation(cancelValue => Assert.AreEqual(expected, cancelation = cancelValue.Value));

                Assert.Throws<InvalidOperationException>(() =>
                    cancelationSource.Cancel("Different Cancel Value")
                );

                deferred.Release();
                Promise.Manager.HandleCompletes();

                Assert.AreEqual(expected, cancelation);

                // Clean up.
                cancelationSource.Dispose();
                GC.Collect();
                Promise.Manager.HandleCompletesAndProgress();
                LogAssert.NoUnexpectedReceived();
            }

            [Test]
            public void MustHaveAReasonWhichMustNotChange_1()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                var deferred = Promise.NewDeferred<int>(cancelationSource.Token);

                Assert.AreEqual(Promise.State.Pending, deferred.State);

                deferred.Retain();
                object cancelation = null;
                string expected = "Cancel Value";

                Action rejectAssert = () => Assert.Fail("Promise was rejected when it should have been resolved.");
                Action resolveAssert = () => Assert.Fail("Promise was resolved when it should have been rejected.");

                TestHelper.AddCallbacks<int, bool, object, string>(deferred.Promise,
                    onResolve: _ => resolveAssert(),
                    onReject: _ => rejectAssert(),
                    onUnknownRejection: rejectAssert);
                deferred.Promise.CatchCancelation(cancelValue => Assert.AreEqual(expected, cancelation = cancelValue.Value));
                cancelationSource.Cancel(expected);
                Promise.Manager.HandleCompletes();

                Assert.AreEqual(expected, cancelation);

                TestHelper.AddCallbacks<int, bool, object, string>(deferred.Promise,
                    onResolve: _ => resolveAssert(),
                    onReject: _ => rejectAssert(),
                    onUnknownRejection: rejectAssert);
                deferred.Promise.CatchCancelation(cancelValue => Assert.AreEqual(expected, cancelation = cancelValue.Value));

                Assert.Throws<InvalidOperationException>(() =>
                    cancelationSource.Cancel("Different Cancel Value")
                );

                deferred.Release();
                Promise.Manager.HandleCompletes();

                Assert.AreEqual(expected, cancelation);

                // Clean up.
                GC.Collect();
                Promise.Manager.HandleCompletesAndProgress();
                LogAssert.NoUnexpectedReceived();
            }
        }

#if PROMISE_DEBUG
        [Test]
        public void IfOnCanceledIsNullThrow()
        {
            CancelationSource cancelationSource1 = CancelationSource.New();
            var deferred = Promise.NewDeferred(cancelationSource1.Token);

            Assert.AreEqual(Promise.State.Pending, deferred.State);

            Assert.Throws<ArgumentNullException>(() =>
            {
                deferred.Promise.CatchCancelation(default(Action<ReasonContainer>));
            });

            cancelationSource1.Cancel();

            CancelationSource cancelationSource2 = CancelationSource.New();
            var deferredInt = Promise.NewDeferred<int>(cancelationSource2.Token);
            Assert.AreEqual(Promise.State.Pending, deferredInt.State);

            Assert.Throws<ArgumentNullException>(() =>
            {
                deferredInt.Promise.CatchCancelation(default(Action<ReasonContainer>));
            });

            cancelationSource2.Cancel(0);

            // Clean up.
            cancelationSource1.Dispose();
            cancelationSource2.Dispose();
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }
#endif

        public class IfOnCanceledIsAFunction
        {
            [SetUp]
            public void Setup()
            {
                TestHelper.cachedRejectionHandler = Promise.Config.UncaughtRejectionHandler;
                Promise.Config.UncaughtRejectionHandler = null;
            }

            [TearDown]
            public void Teardown()
            {
                Promise.Config.UncaughtRejectionHandler = TestHelper.cachedRejectionHandler;
            }

            [Test]
            public void ItMustBeCalledAfterPromiseIsCanceledWithPromisesReasonAsItsFirstArgument()
            {
                string cancelReason = "Cancel value";
                var canceled = false;
                CancelationSource cancelationSource = CancelationSource.New();
                var deferred = Promise.NewDeferred(cancelationSource.Token);
                Assert.AreEqual(Promise.State.Pending, deferred.State);
                deferred.Promise.CatchCancelation(r =>
                {
                    Assert.AreEqual(cancelReason, r.Value);
                    canceled = true;
                });
                cancelationSource.Cancel(cancelReason);
                Promise.Manager.HandleCompletes();

                Assert.True(canceled);

                // Clean up.
                cancelationSource.Dispose();
                GC.Collect();
                Promise.Manager.HandleCompletesAndProgress();
                LogAssert.NoUnexpectedReceived();
            }

            [Test]
            public void ItMustNotBeCalledBeforePromiseIsCanceled()
            {
                string cancelReason = "Cancel value";
                var canceled = false;
                CancelationSource cancelationSource = CancelationSource.New();
                var deferred = Promise.NewDeferred(cancelationSource.Token);
                Assert.AreEqual(Promise.State.Pending, deferred.State);
                deferred.Promise.CatchCancelation(r =>
                {
                    Assert.AreEqual(cancelReason, r.Value);
                    canceled = true;
                });
                Promise.Manager.HandleCompletes();

                Assert.False(canceled);

                cancelationSource.Cancel(cancelReason);
                Promise.Manager.HandleCompletes();

                Assert.True(canceled);

                // Clean up.
                cancelationSource.Dispose();
                GC.Collect();
                Promise.Manager.HandleCompletesAndProgress();
                LogAssert.NoUnexpectedReceived();
            }

            [Test]
            public void ItMustNotBeCalledMoreThanOnce()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                var deferred = Promise.NewDeferred(cancelationSource.Token);
                Assert.AreEqual(Promise.State.Pending, deferred.State);
                deferred.Retain();
                var cancelCount = 0;
                deferred.Promise.CatchCancelation(r => ++cancelCount);
                cancelationSource.Cancel("Cancel value");
                Promise.Manager.HandleCompletes();

                Assert.Throws<InvalidOperationException>(() =>
                    cancelationSource.Cancel("Cancel value")
                );

                deferred.Release();
                Promise.Manager.HandleCompletes();

                Assert.AreEqual(1, cancelCount);

                // Clean up.
                cancelationSource.Dispose();
                GC.Collect();
                Promise.Manager.HandleCompletesAndProgress();
                LogAssert.NoUnexpectedReceived();
            }
        }

        [Test]
        public void OnCanceledMustNotBeCalledUntilTheExecutionContextStackContainsOnlyPlatformCode()
        {
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred(cancelationSource.Token);
            Assert.AreEqual(Promise.State.Pending, deferred.State);

            bool canceled = false;
            deferred.Promise.CatchCancelation(e => canceled = true);
            cancelationSource.Cancel("Cancel value");
            Assert.False(canceled);


            Promise.Manager.HandleCompletes();
            Assert.True(canceled);

            // Clean up.
            cancelationSource.Dispose();
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        public class CatchCancelationMayBeCalledMultipleTimesOnTheSamePromise
        {
            [SetUp]
            public void Setup()
            {
                TestHelper.cachedRejectionHandler = Promise.Config.UncaughtRejectionHandler;
                Promise.Config.UncaughtRejectionHandler = null;
            }

            [TearDown]
            public void Teardown()
            {
                Promise.Config.UncaughtRejectionHandler = TestHelper.cachedRejectionHandler;
            }

            [Test]
            public void IfWhenPromiseCancelationIsCanceledAllRespectiveOnCanceledCallbacksMustExecuteInTheOrderOfTheirOriginatingCallsToCatchCancelation()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                var deferred = Promise.NewDeferred(cancelationSource.Token);
                Assert.AreEqual(Promise.State.Pending, deferred.State);

                int counter = 0;

                deferred.Promise.CatchCancelation(e => Assert.AreEqual(0, counter++));
                deferred.Promise.CatchCancelation(e => Assert.AreEqual(1, counter++));
                deferred.Promise.CatchCancelation(e => Assert.AreEqual(2, counter++));

                cancelationSource.Cancel();
                Promise.Manager.HandleCompletes();

                Assert.AreEqual(3, counter);

                // Clean up.
                cancelationSource.Dispose();
                GC.Collect();
                Promise.Manager.HandleCompletesAndProgress();
                LogAssert.NoUnexpectedReceived();
            }
        }

        [Test]
        public void IfPromiseIsCanceledOnResolveAndOnRejectedMustNotBeInvoked()
        {
            CancelationSource cancelationSource1 = CancelationSource.New();
            var deferred = Promise.NewDeferred(cancelationSource1.Token);
            Assert.AreEqual(Promise.State.Pending, deferred.State);
            CancelationSource cancelationSource2 = CancelationSource.New();
            var deferredInt = Promise.NewDeferred<int>(cancelationSource2.Token);
            Assert.AreEqual(Promise.State.Pending, deferredInt.State);

            Action resolveAssert = () => Assert.Fail("Promise was resolved when it should have been canceled.");
            Action rejectAssert = () => Assert.Fail("Promise was rejected when it should have been canceled.");

            TestHelper.AddCallbacks<int, object, string>(deferred.Promise,
                onResolve: resolveAssert,
                onReject: _ => rejectAssert(),
                onUnknownRejection: rejectAssert);
            TestHelper.AddCallbacks<int, bool, object, string>(deferredInt.Promise,
                onResolve: _ => resolveAssert(),
                onReject: _ => rejectAssert(),
                onUnknownRejection: rejectAssert);

            cancelationSource1.Cancel();
            cancelationSource2.Cancel();
            Promise.Manager.HandleCompletes();

            // Clean up.
            cancelationSource1.Dispose();
            cancelationSource2.Dispose();
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void CancelationsDoNotPropagateToRoot()
        {
            var deferred = Promise.NewDeferred();
            Assert.AreEqual(Promise.State.Pending, deferred.State);

            CancelationSource cancelationSource = CancelationSource.New();

            bool resolved = false;

            deferred.Promise
                .Then(() => resolved = true)
                .Then(_ => Assert.Fail("Promise was resolved when it should have been canceled."), cancelationSource.Token)
                .Finally(cancelationSource.Dispose);

            cancelationSource.Cancel();
            deferred.Resolve();
            Promise.Manager.HandleCompletes();

            Assert.AreEqual(true, resolved);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void CancelationsPropagateToBranches()
        {
            Promise.Config.UncaughtRejectionHandler = UnityEngine.Debug.LogException;

            CancelationSource cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred(cancelationSource.Token);
            Assert.AreEqual(Promise.State.Pending, deferred.State);

            bool invoked = false;

            deferred.Promise
                .Then(() => { })
                .CatchCancelation(e => invoked = true);

            cancelationSource.Cancel();
            Promise.Manager.HandleCompletes();

            Assert.AreEqual(true, invoked);

            // Clean up.
            cancelationSource.Dispose();
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void IfPromiseIsCanceledAndAPreviousPromiseIsAlsoCanceledPromiseMustBeCanceledWithTheInitialCancelReason()
        {
            var deferred = Promise.NewDeferred();
            Assert.AreEqual(Promise.State.Pending, deferred.State);

            bool invoked = false;

            object cancelValue = "Cancel";
            CancelationSource cancelationSource = CancelationSource.New();

            deferred.Promise
                .Then(() => { }, cancelationSource.Token)
                .CatchCancelation(reason =>
                {
                    Assert.AreEqual(cancelValue, reason.Value);
                    invoked = true;
                })
                .Finally(cancelationSource.Dispose);

            cancelationSource.Cancel(cancelValue);
            deferred.Resolve();
            Promise.Manager.HandleCompletes();

            Assert.AreEqual(true, invoked);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void APromiseMayBeCanceledWhenItIsPending()
        {
            CancelationSource cancelationSource = CancelationSource.New();
            string cancelValue = "Cancel";

            Promise.Resolved()
                .Then(() => cancelationSource.Cancel(cancelValue))
                .Then(() => { }, cancelationSource.Token)
                .Then(() => Assert.Fail("Promise was resolved when it should have been canceled."),
                    () => Assert.Fail("Promise was rejected when it should have been canceled."))
                .CatchCancelation(reason => Assert.AreEqual(cancelValue, reason.Value))
                .Finally(cancelationSource.Dispose);

            Promise.Manager.HandleCompletes();

            // Clean up.
            Promise.Manager.HandleCompletesAndProgress();
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }
    }
}