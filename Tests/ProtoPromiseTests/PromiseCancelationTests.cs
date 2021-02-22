#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using System;
using NUnit.Framework;

namespace Proto.Promises.Tests
{
    public class PromiseCancelationTests
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

        public class WhenPendingAPromise
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
            public void MayTransitionToEitherTheFulfilledOrRejectedOrCanceledState_void()
            {
                string Resolved = "Resolved", Rejected = "Rejected", Canceled = "Canceled";
                string state = null;

                var deferred = Promise.NewDeferred();
                Assert.IsTrue(deferred.IsPending);

                deferred.Promise
                    .CatchCancelation(_ => state = Canceled)
                    .Then(() => state = Resolved, () => state = Rejected)
                    .Forget();
                Assert.IsNull(state);

                deferred.Resolve();
                Assert.IsFalse(deferred.IsPending);
                Promise.Manager.HandleCompletesAndProgress();

                Assert.AreEqual(Resolved, state);

                state = null;
                deferred = Promise.NewDeferred();
                Assert.IsTrue(deferred.IsPending);

                deferred.Promise
                    .CatchCancelation(_ => state = Canceled)
                    .Then(() => state = Resolved, () => state = Rejected)
                    .Forget();
                Assert.IsNull(state);

                deferred.Reject("Fail Value");
                Assert.IsFalse(deferred.IsPending);
                Promise.Manager.HandleCompletesAndProgress();

                state = null;
                CancelationSource cancelationSource = CancelationSource.New();
                deferred = Promise.NewDeferred(cancelationSource.Token);
                Assert.IsTrue(deferred.IsPending);

                deferred.Promise
                    .CatchCancelation(_ => state = Canceled)
                    .Then(() => state = Resolved, () => state = Rejected)
                    .Forget();
                Assert.IsNull(state);

                cancelationSource.Cancel();
                Assert.IsFalse(deferred.IsPending);
                Promise.Manager.HandleCompletesAndProgress();

                Assert.AreEqual(Canceled, state);
                cancelationSource.Dispose();

                state = null;
                cancelationSource = CancelationSource.New();
                deferred = Promise.NewDeferred(cancelationSource.Token);
                Assert.IsTrue(deferred.IsPending);

                deferred.Promise
                    .CatchCancelation(_ => state = Canceled)
                    .Then(() => state = Resolved, () => state = Rejected)
                    .Forget();
                Assert.IsNull(state);

                cancelationSource.Cancel("Cancel Value");
                Assert.IsFalse(deferred.IsPending);
                Promise.Manager.HandleCompletesAndProgress();

                Assert.AreEqual(Canceled, state);
                cancelationSource.Dispose();
            }

            [Test]
            public void MayTransitionToEitherTheFulfilledOrRejectedOrCanceledState_T()
            {
                string Resolved = "Resolved", Rejected = "Rejected", Canceled = "Canceled";
                string state = null;

                var deferred = Promise.NewDeferred<int>();
                Assert.IsTrue(deferred.IsPending);

                deferred.Promise
                    .CatchCancelation(_ => state = Canceled)
                    .Then(v => state = Resolved, () => state = Rejected)
                    .Forget();
                Assert.IsNull(state);

                deferred.Resolve(1);
                Assert.IsFalse(deferred.IsPending);
                Promise.Manager.HandleCompletesAndProgress();

                Assert.AreEqual(Resolved, state);

                state = null;
                deferred = Promise.NewDeferred<int>();
                Assert.IsTrue(deferred.IsPending);

                deferred.Promise
                    .CatchCancelation(_ => state = Canceled)
                    .Then(v => state = Resolved, () => state = Rejected)
                    .Forget();
                Assert.IsNull(state);

                deferred.Reject("Fail Value");
                Assert.IsFalse(deferred.IsPending);
                Promise.Manager.HandleCompletesAndProgress();

                state = null;
                CancelationSource cancelationSource = CancelationSource.New();
                deferred = Promise.NewDeferred<int>(cancelationSource.Token);
                Assert.IsTrue(deferred.IsPending);

                deferred.Promise
                    .CatchCancelation(_ => state = Canceled)
                    .Then(v => state = Resolved, () => state = Rejected)
                    .Forget();
                Assert.IsNull(state);

                cancelationSource.Cancel();
                Assert.IsFalse(deferred.IsPending);
                Promise.Manager.HandleCompletesAndProgress();

                Assert.AreEqual(Canceled, state);
                cancelationSource.Dispose();

                state = null;
                cancelationSource = CancelationSource.New();
                deferred = Promise.NewDeferred<int>(cancelationSource.Token);
                Assert.IsTrue(deferred.IsPending);

                deferred.Promise
                    .CatchCancelation(_ => state = Canceled)
                    .Then(v => state = Resolved, () => state = Rejected)
                    .Forget();
                Assert.IsNull(state);

                cancelationSource.Cancel("Cancel Value");
                Assert.IsFalse(deferred.IsPending);
                Promise.Manager.HandleCompletesAndProgress();

                Assert.AreEqual(Canceled, state);
                cancelationSource.Dispose();
            }
        }

        public class WhenFulfilledAPromise
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
            public void MustNotTransitionToAnyOtherState_void()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                var deferred = Promise.NewDeferred(cancelationSource.Token);

                bool resolved = false;

                deferred.Promise
                    .Then(() => { resolved = true; })
                    .Catch(() => Assert.Fail("Promise was rejected when it was already resolved."))
                    .CatchCancelation(e => Assert.Fail("Promise was canceled when it was already resolved."))
                    .Forget();

                deferred.Resolve();

                Assert.IsFalse(deferred.TryResolve());
                Assert.Throws<InvalidOperationException>(() => deferred.Resolve());
                Assert.IsFalse(deferred.TryReject("Fail value"));
                Assert.Throws<InvalidOperationException>(() => deferred.Reject("Fail value"));

                cancelationSource.Cancel();

                Promise.Manager.HandleCompletes();
                Assert.IsTrue(resolved);

                cancelationSource.Dispose();
            }

            [Test]
            public void MustNotTransitionToAnyOtherState_T()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                var deferred = Promise.NewDeferred<int>(cancelationSource.Token);

                bool resolved = false;

                deferred.Promise
                    .Then(v => { resolved = true; })
                    .Catch(() => Assert.Fail("Promise was rejected when it was already resolved."))
                    .CatchCancelation(e => Assert.Fail("Promise was canceled when it was already resolved."))
                    .Forget();

                deferred.Resolve(1);

                Assert.IsFalse(deferred.TryResolve(1));
                Assert.Throws<InvalidOperationException>(() => deferred.Resolve(1));
                Assert.IsFalse(deferred.TryReject("Fail value"));
                Assert.Throws<InvalidOperationException>(() => deferred.Reject("Fail value"));

                cancelationSource.Cancel();

                Promise.Manager.HandleCompletes();
                Assert.IsTrue(resolved);

                cancelationSource.Dispose();
            }
        }

        public class WhenRejectedAPromise
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
            public void MustNotTransitionToAnyOtherState_void()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                var deferred = Promise.NewDeferred(cancelationSource.Token);

                bool rejected = false;

                deferred.Promise
                    .Then(() => Assert.Fail("Promise was resolved when it was already rejected."))
                    .Catch(() => { rejected = true; })
                    .CatchCancelation(e => Assert.Fail("Promise was canceled when it was already rejected."))
                    .Forget();

                deferred.Reject("Fail Value");

                Assert.IsFalse(deferred.TryResolve());
                Assert.Throws<InvalidOperationException>(() => deferred.Resolve());
                Assert.IsFalse(deferred.TryReject("Fail value"));
                Assert.Throws<InvalidOperationException>(() => deferred.Reject("Fail value"));

                cancelationSource.Cancel();

                Promise.Manager.HandleCompletes();
                Assert.IsTrue(rejected);

                cancelationSource.Dispose();
            }

            [Test]
            public void MustNotTransitionToAnyOtherState_T()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                var deferred = Promise.NewDeferred<int>(cancelationSource.Token);

                bool rejected = false;

                deferred.Promise
                    .Then(() => Assert.Fail("Promise was resolved when it was already rejected."))
                    .Catch(() => { rejected = true; })
                    .CatchCancelation(e => Assert.Fail("Promise was canceled when it was already rejected."))
                    .Forget();

                deferred.Reject("Fail Value");

                Assert.IsFalse(deferred.TryResolve(1));
                Assert.Throws<InvalidOperationException>(() => deferred.Resolve(1));
                Assert.IsFalse(deferred.TryReject("Fail value"));
                Assert.Throws<InvalidOperationException>(() => deferred.Reject("Fail value"));

                cancelationSource.Cancel();

                Promise.Manager.HandleCompletes();
                Assert.IsTrue(rejected);

                cancelationSource.Dispose();
            }
        }

        public class WhenCanceledAPromise
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
            public void MustNotTransitionToAnyOtherState_void()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                var deferred = Promise.NewDeferred(cancelationSource.Token);

                bool canceled = false;

                deferred.Promise
                    .Then(() => Assert.Fail("Promise was resolved when it was already canceled."))
                    .Catch(() => Assert.Fail("Promise was rejected when it was already canceled."))
                    .CatchCancelation(e => { canceled = true; })
                    .Forget();

                cancelationSource.Cancel();

                Assert.IsFalse(deferred.TryResolve());
                Assert.Throws<InvalidOperationException>(() => deferred.Resolve());
                Assert.IsFalse(deferred.TryReject("Fail value"));
                Assert.Throws<InvalidOperationException>(() => deferred.Reject("Fail value"));
                Assert.Throws<InvalidOperationException>(() => cancelationSource.Cancel());

                Promise.Manager.HandleCompletes();

                Assert.IsTrue(canceled);

                cancelationSource.Dispose();
            }

            [Test]
            public void MustNotTransitionToAnyOtherState_T()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                var deferred = Promise.NewDeferred<int>(cancelationSource.Token);

                bool canceled = false;

                deferred.Promise
                    .Then(v => Assert.Fail("Promise was resolved when it was already canceled."))
                    .Catch(() => Assert.Fail("Promise was rejected when it was already canceled."))
                    .CatchCancelation(e => { canceled = true; })
                    .Forget();

                cancelationSource.Cancel();

                Assert.IsFalse(deferred.TryResolve(1));
                Assert.Throws<InvalidOperationException>(() => deferred.Resolve(1));
                Assert.IsFalse(deferred.TryReject("Fail value"));
                Assert.Throws<InvalidOperationException>(() => deferred.Reject("Fail value"));
                Assert.Throws<InvalidOperationException>(() => cancelationSource.Cancel());

                Promise.Manager.HandleCompletes();

                Assert.IsTrue(canceled);

                cancelationSource.Dispose();
            }

            [Test]
            public void MustHaveAReasonWhichMustNotChange_void()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                var deferred = Promise.NewDeferred(cancelationSource.Token);
                var promise = deferred.Promise.Preserve();

                object cancelation = null;
                string expected = "Cancel Value";

                Action rejectAssert = () => Assert.Fail("Promise was rejected when it should have been resolved.");
                Action resolveAssert = () => Assert.Fail("Promise was resolved when it should have been rejected.");

                TestHelper.AddCallbacks<int, object, string>(promise,
                    onResolve: resolveAssert,
                    onReject: failValue => rejectAssert(),
                    onUnknownRejection: rejectAssert);
                promise
                    .CatchCancelation(cancelValue => Assert.AreEqual(expected, cancelation = cancelValue.Value))
                    .Forget();
                cancelationSource.Cancel(expected);
                Promise.Manager.HandleCompletes();

                Assert.AreEqual(expected, cancelation);

                TestHelper.AddCallbacks<int, object, string>(promise,
                    onResolve: resolveAssert,
                    onReject: failValue => rejectAssert(),
                    onUnknownRejection: rejectAssert);
                promise
                    .CatchCancelation(cancelValue => Assert.AreEqual(expected, cancelation = cancelValue.Value))
                    .Forget();

                Assert.Throws<InvalidOperationException>(() =>
                    cancelationSource.Cancel("Different Cancel Value")
                );

                Promise.Manager.HandleCompletes();

                Assert.AreEqual(expected, cancelation);

                promise.Forget();
                cancelationSource.Dispose();
            }

            [Test]
            public void MustHaveAReasonWhichMustNotChange_T()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                var deferred = Promise.NewDeferred<int>(cancelationSource.Token);
                var promise = deferred.Promise.Preserve();

                object cancelation = null;
                string expected = "Cancel Value";

                Action rejectAssert = () => Assert.Fail("Promise was rejected when it should have been resolved.");
                Action resolveAssert = () => Assert.Fail("Promise was resolved when it should have been rejected.");

                TestHelper.AddCallbacks<int, bool, object, string>(promise,
                    onResolve: _ => resolveAssert(),
                    onReject: _ => rejectAssert(),
                    onUnknownRejection: rejectAssert);
                promise
                    .CatchCancelation(cancelValue => Assert.AreEqual(expected, cancelation = cancelValue.Value))
                    .Forget();
                cancelationSource.Cancel(expected);
                Promise.Manager.HandleCompletes();

                Assert.AreEqual(expected, cancelation);

                TestHelper.AddCallbacks<int, bool, object, string>(promise,
                    onResolve: _ => resolveAssert(),
                    onReject: _ => rejectAssert(),
                    onUnknownRejection: rejectAssert);
                promise
                    .CatchCancelation(cancelValue => Assert.AreEqual(expected, cancelation = cancelValue.Value))
                    .Forget();

                Assert.Throws<InvalidOperationException>(() =>
                    cancelationSource.Cancel("Different Cancel Value")
                );

                Promise.Manager.HandleCompletes();

                Assert.AreEqual(expected, cancelation);

                promise.Forget();
                cancelationSource.Dispose();
            }
        }

#if PROMISE_DEBUG
        [Test]
        public void IfOnCanceledIsNullThrow_void()
        {
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred(cancelationSource.Token);

            Assert.Throws<ArgumentNullException>(() =>
            {
                deferred.Promise.CatchCancelation(default(Promise.CanceledAction));
            });

            cancelationSource.Cancel();
            cancelationSource.Dispose();
            deferred.Promise.Forget();
        }

        [Test]
        public void IfOnCanceledIsNullThrow_T()
        {
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred<int>(cancelationSource.Token);

            Assert.Throws<ArgumentNullException>(() =>
            {
                deferred.Promise.CatchCancelation(default(Promise.CanceledAction));
            });

            cancelationSource.Cancel();
            deferred.Promise.Forget();
            cancelationSource.Dispose();
        }
#endif

        public class IfOnCanceledIsAFunction
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
            public void ItMustBeCalledAfterPromiseIsCanceledWithPromisesReasonAsItsFirstArgument()
            {
                string cancelReason = "Cancel value";
                var canceled = false;
                CancelationSource cancelationSource = CancelationSource.New();
                var deferred = Promise.NewDeferred(cancelationSource.Token);
                deferred.Promise
                    .CatchCancelation(r =>
                    {
                        Assert.AreEqual(cancelReason, r.Value);
                        canceled = true;
                    })
                    .Forget();
                cancelationSource.Cancel(cancelReason);
                Promise.Manager.HandleCompletes();

                Assert.True(canceled);

                cancelationSource.Dispose();
            }

            [Test]
            public void ItMustNotBeCalledBeforePromiseIsCanceled()
            {
                string cancelReason = "Cancel value";
                var canceled = false;
                CancelationSource cancelationSource = CancelationSource.New();
                var deferred = Promise.NewDeferred(cancelationSource.Token);
                deferred.Promise
                    .CatchCancelation(r =>
                    {
                        Assert.AreEqual(cancelReason, r.Value);
                        canceled = true;
                    })
                    .Forget();
                Promise.Manager.HandleCompletes();

                Assert.False(canceled);

                cancelationSource.Cancel(cancelReason);
                Promise.Manager.HandleCompletes();

                Assert.True(canceled);

                cancelationSource.Dispose();
            }

            [Test]
            public void ItMustNotBeCalledMoreThanOnce()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                var deferred = Promise.NewDeferred(cancelationSource.Token);
                var cancelCount = 0;
                deferred.Promise
                    .CatchCancelation(r => ++cancelCount)
                    .Forget();
                cancelationSource.Cancel("Cancel value");
                Promise.Manager.HandleCompletes();

                Assert.Throws<InvalidOperationException>(() =>
                    cancelationSource.Cancel("Cancel value")
                );

                Promise.Manager.HandleCompletes();
                Assert.AreEqual(1, cancelCount);

                cancelationSource.Dispose();
            }
        }

        [Test]
        public void OnCanceledMustNotBeCalledUntilTheExecutionContextStackContainsOnlyPlatformCode()
        {
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred(cancelationSource.Token);

            bool canceled = false;
            deferred.Promise
                .CatchCancelation(e => canceled = true)
                .Forget();
            cancelationSource.Cancel("Cancel value");
            Assert.False(canceled);

            Promise.Manager.HandleCompletes();
            Assert.True(canceled);

            cancelationSource.Dispose();
        }

        public class CatchCancelationMayBeCalledMultipleTimesOnTheSamePromise
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
            public void IfWhenPromiseCancelationIsCanceled_AllRespectiveOnCanceledCallbacksMustExecuteInTheOrderOfTheirOriginatingCallsToCatchCancelation()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                var deferred = Promise.NewDeferred(cancelationSource.Token);
                var promise = deferred.Promise.Preserve();

                int counter = 0;

                promise.CatchCancelation(e => Assert.AreEqual(0, counter++)).Forget();
                promise.CatchCancelation(e => Assert.AreEqual(1, counter++)).Forget();
                promise.CatchCancelation(e => Assert.AreEqual(2, counter++)).Forget();

                cancelationSource.Cancel();
                Promise.Manager.HandleCompletes();

                Assert.AreEqual(3, counter);

                cancelationSource.Dispose();
                promise.Forget();
            }
        }

        [Test]
        public void IfPromiseIsCanceled_OnResolveAndOnRejectedMustNotBeInvoked_void()
        {
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred(cancelationSource.Token);

            Action rejectAssert = () => Assert.Fail("Promise was rejected when it should have been canceled.");

            TestHelper.AddCallbacks<int, object, string>(deferred.Promise,
                onResolve: () => Assert.Fail("Promise was resolved when it should have been canceled."),
                onReject: _ => rejectAssert(),
                onUnknownRejection: rejectAssert);

            cancelationSource.Cancel();
            Promise.Manager.HandleCompletes();

            cancelationSource.Dispose();
        }

        [Test]
        public void IfPromiseIsCanceled_OnResolveAndOnRejectedMustNotBeInvoked_T()
        {
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred<int>(cancelationSource.Token);

            Action rejectAssert = () => Assert.Fail("Promise was rejected when it should have been canceled.");

            TestHelper.AddCallbacks<int, bool, object, string>(deferred.Promise,
                onResolve: v => Assert.Fail("Promise was resolved when it should have been canceled."),
                onReject: _ => rejectAssert(),
                onUnknownRejection: rejectAssert);

            cancelationSource.Cancel();
            Promise.Manager.HandleCompletes();

            cancelationSource.Dispose();
        }

        [Test]
        public void CancelationsDoNotPropagateToRoot()
        {
            var deferred = Promise.NewDeferred();

            CancelationSource cancelationSource = CancelationSource.New();

            bool resolved = false;

            deferred.Promise
                .Then(() => resolved = true)
                .Then(_ => Assert.Fail("Promise was resolved when it should have been canceled."), cancelationSource.Token)
                .Finally(cancelationSource.Dispose)
                .Forget();

            cancelationSource.Cancel();
            deferred.Resolve();
            Promise.Manager.HandleCompletes();

            Assert.IsTrue(resolved);
        }

        [Test]
        public void CancelationsPropagateToBranches()
        {
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred(cancelationSource.Token);

            bool invoked = false;

            deferred.Promise
                .Then(() => { })
                .CatchCancelation(e => invoked = true)
                .Forget();

            cancelationSource.Cancel();
            Promise.Manager.HandleCompletes();

            Assert.IsTrue(invoked);

            cancelationSource.Dispose();
        }

        [Test]
        public void IfPromiseIsCanceledAndAPreviousPromiseIsAlsoCanceled_PromiseMustBeCanceledWithTheInitialCancelReason()
        {
            var deferred = Promise.NewDeferred();

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
                .Finally(cancelationSource.Dispose)
                .Forget();

            cancelationSource.Cancel(cancelValue);
            deferred.Resolve();
            Promise.Manager.HandleCompletes();

            Assert.IsTrue(invoked);
        }

        [Test]
        public void APromiseMayBeCanceledWhenItIsPending()
        {
            CancelationSource cancelationSource = CancelationSource.New();
            string cancelValue = "Cancel";
            var deferred = Promise.NewDeferred();

            deferred.Promise
                .Then(() => cancelationSource.Cancel(cancelValue))
                .Then(() => { }, cancelationSource.Token)
                .Then(() => Assert.Fail("Promise was resolved when it should have been canceled."),
                    () => Assert.Fail("Promise was rejected when it should have been canceled."))
                .CatchCancelation(reason => Assert.AreEqual(cancelValue, reason.Value))
                .Finally(cancelationSource.Dispose)
                .Forget();

            deferred.Resolve();
            Promise.Manager.HandleCompletes();
        }

        [Test]
        public void CatchCancelationCaptureValue()
        {
            CancelationSource cancelationSource = CancelationSource.New();
            string cancelValue = "Cancel";
            int captureValue = 100;
            var deferred = Promise.NewDeferred();
            var promise = deferred.Promise.Preserve();

            promise
                .Then(() => { }, cancelationSource.Token)
                .CatchCancelation(captureValue, (cv, _) => Assert.AreEqual(captureValue, cv))
                .Forget();
            promise
                .Then(() => 1f, cancelationSource.Token)
                .CatchCancelation(captureValue, (cv, _) => Assert.AreEqual(captureValue, cv))
                .Forget();
            cancelationSource.Cancel(cancelValue);

            deferred.Resolve();
            Promise.Manager.HandleCompletes();

            cancelationSource.Dispose();
            promise.Forget();
        }

        public class CancelationToken
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
            public void DeferredIsCanceledFromAlreadyCanceledToken()
            {
                string cancelReason = "Cancel value";
                var canceled = false;
                CancelationSource cancelationSource = CancelationSource.New();
                cancelationSource.Cancel(cancelReason);

                var deferred = Promise.NewDeferred(cancelationSource.Token);

                deferred.Promise
                    .CatchCancelation(r =>
                    {
                        Assert.AreEqual(cancelReason, r.Value);
                        canceled = true;
                    })
                    .Forget();
                Promise.Manager.HandleCompletes();

                Assert.True(canceled);

                cancelationSource.Dispose();
            }

            [Test]
            public void OnCanceledIsNotInvokedIfTokenIsCanceled_void0()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                var deferred = Promise.NewDeferred(cancelationSource.Token);

                deferred.Promise
                    .CatchCancelation(_ => Assert.Fail("OnCanceled was invoked."), cancelationSource.Token)
                    .CatchCancelation(1, (cv, _) => Assert.Fail("OnCanceled was invoked."), cancelationSource.Token)
                    .Forget();

                cancelationSource.Cancel();
                Promise.Manager.HandleCompletes();

                cancelationSource.Dispose();
            }

            [Test]
            public void OnCanceledIsNotInvokedIfTokenIsCanceled_T0()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                var deferred = Promise.NewDeferred<int>(cancelationSource.Token);

                deferred.Promise
                    .CatchCancelation(_ => Assert.Fail("OnCanceled was invoked."), cancelationSource.Token)
                    .CatchCancelation(1, (cv, _) => Assert.Fail("OnCanceled was invoked."), cancelationSource.Token)
                    .Forget();

                cancelationSource.Cancel();
                Promise.Manager.HandleCompletes();

                cancelationSource.Dispose();
            }

            [Test]
            public void OnCanceledIsNotInvokedIfTokenIsCanceled_void1()
            {
                CancelationSource deferredCancelationSource = CancelationSource.New();
                CancelationSource catchCancelationSource = CancelationSource.New();
                var deferred = Promise.NewDeferred(deferredCancelationSource.Token);

                deferred.Promise
                    .CatchCancelation(_ => Assert.Fail("OnCanceled was invoked."), catchCancelationSource.Token)
                    .CatchCancelation(1, (cv, _) => Assert.Fail("OnCanceled was invoked."), catchCancelationSource.Token)
                    .Forget();

                catchCancelationSource.Cancel();
                deferredCancelationSource.Cancel();
                Promise.Manager.HandleCompletes();

                catchCancelationSource.Dispose();
                deferredCancelationSource.Dispose();
            }

            [Test]
            public void OnCanceledIsNotInvokedIfTokenIsCanceled_T1()
            {
                CancelationSource deferredCancelationSource = CancelationSource.New();
                CancelationSource catchCancelationSource = CancelationSource.New();
                var deferred = Promise.NewDeferred<int>(deferredCancelationSource.Token);

                deferred.Promise
                    .CatchCancelation(_ => Assert.Fail("OnCanceled was invoked."), catchCancelationSource.Token)
                    .CatchCancelation(1, (cv, _) => Assert.Fail("OnCanceled was invoked."), catchCancelationSource.Token)
                    .Forget();

                catchCancelationSource.Cancel();
                deferredCancelationSource.Cancel();
                Promise.Manager.HandleCompletes();

                catchCancelationSource.Dispose();
                deferredCancelationSource.Dispose();
            }

            [Test]
            public void OnCanceledIsNotInvokedIfTokenIsAlreadyCanceled()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                cancelationSource.Cancel();

                Promise.Canceled()
                    .CatchCancelation(_ => Assert.Fail("OnCanceled was invoked."), cancelationSource.Token)
                    .CatchCancelation(1, (cv, _) => Assert.Fail("OnCanceled was invoked."), cancelationSource.Token)
                    .Forget();
                Promise.Canceled<int>()
                    .CatchCancelation(_ => Assert.Fail("OnCanceled was invoked."), cancelationSource.Token)
                    .CatchCancelation(1, (cv, _) => Assert.Fail("OnCanceled was invoked."), cancelationSource.Token)
                    .Forget();

                Promise.Manager.HandleCompletes();

                cancelationSource.Dispose();
            }

            [Test]
            public void OnCanceledIsNotInvokedIfPromiseIsResolved_void0()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                var deferred = Promise.NewDeferred();

                deferred.Promise
                    .CatchCancelation(_ => Assert.Fail("OnCanceled was invoked."), cancelationSource.Token)
                    .CatchCancelation(1, (cv, _) => Assert.Fail("OnCanceled was invoked."), cancelationSource.Token)
                    .Forget();

                deferred.Resolve();
                cancelationSource.Cancel();
                Promise.Manager.HandleCompletes();

                cancelationSource.Dispose();
            }

            [Test]
            public void OnCanceledIsNotInvokedIfPromiseIsResolved_T0()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                var deferred = Promise.NewDeferred<int>();

                deferred.Promise
                    .CatchCancelation(_ => Assert.Fail("OnCanceled was invoked."), cancelationSource.Token)
                    .CatchCancelation(1, (cv, _) => Assert.Fail("OnCanceled was invoked."), cancelationSource.Token)
                    .Forget();

                deferred.Resolve(1);
                cancelationSource.Cancel();
                Promise.Manager.HandleCompletes();

                cancelationSource.Dispose();
            }

            [Test]
            public void OnCanceledIsNotInvokedIfPromiseIsResolved_void1()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                var deferred = Promise.NewDeferred();

                deferred.Promise
                    .CatchCancelation(_ => Assert.Fail("OnCanceled was invoked."), cancelationSource.Token)
                    .CatchCancelation(1, (cv, _) => Assert.Fail("OnCanceled was invoked."), cancelationSource.Token)
                    .Forget();

                deferred.Resolve();
                Promise.Manager.HandleCompletes();

                cancelationSource.Dispose();
            }

            [Test]
            public void OnCanceledIsNotInvokedIfPromiseIsResolved_T1()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                var deferred = Promise.NewDeferred<int>();

                deferred.Promise
                    .CatchCancelation(_ => Assert.Fail("OnCanceled was invoked."), cancelationSource.Token)
                    .CatchCancelation(1, (cv, _) => Assert.Fail("OnCanceled was invoked."), cancelationSource.Token)
                    .Forget();

                deferred.Resolve(1);
                Promise.Manager.HandleCompletes();

                cancelationSource.Dispose();
            }

            [Test]
            public void OnResolvedIsNotInvokedIfTokenIsCanceled_void()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                var deferred = Promise.NewDeferred();
                var promise = deferred.Promise.Preserve();

                TestHelper.AddResolveCallbacksWithCancelation<float, string>(
                    promise,
                    onResolve: () => Assert.Fail("OnResolved was invoked."),
                    onResolveCapture: _ => Assert.Fail("OnResolved was invoked."),
                    cancelationToken: cancelationSource.Token
                );
                TestHelper.AddCallbacksWithCancelation<float, object, string>(
                    promise,
                    onResolve: () => Assert.Fail("OnResolved was invoked."),
                    onResolveCapture: _ => Assert.Fail("OnResolved was invoked."),
                    cancelationToken: cancelationSource.Token
                );

                cancelationSource.Cancel();
                deferred.Resolve();
                Promise.Manager.HandleCompletes();

                cancelationSource.Dispose();
                promise.Forget();
            }

            [Test]
            public void OnResolvedIsNotInvokedIfTokenIsCanceled_T()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                var deferred = Promise.NewDeferred<int>();
                var promise = deferred.Promise.Preserve();

                TestHelper.AddResolveCallbacksWithCancelation<int, float, string>(
                    promise,
                    onResolve: _ => Assert.Fail("OnResolved was invoked."),
                    onResolveCapture: _ => Assert.Fail("OnResolved was invoked."),
                    cancelationToken: cancelationSource.Token
                );
                TestHelper.AddCallbacksWithCancelation<int, float, object, string>(
                    promise,
                    onResolve: _ => Assert.Fail("OnResolved was invoked."),
                    onResolveCapture: _ => Assert.Fail("OnResolved was invoked."),
                    cancelationToken: cancelationSource.Token
                );

                cancelationSource.Cancel();
                deferred.Resolve(1);
                Promise.Manager.HandleCompletes();

                cancelationSource.Dispose();
                promise.Forget();
            }

            [Test]
            public void OnResolvedIsNotInvokedIfTokenIsAlreadyCanceled_void()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                cancelationSource.Cancel();

                var deferred = Promise.NewDeferred();
                var promise = deferred.Promise.Preserve();

                TestHelper.AddResolveCallbacksWithCancelation<float, string>(
                    promise,
                    onResolve: () => Assert.Fail("OnResolved was invoked."),
                    onResolveCapture: _ => Assert.Fail("OnResolved was invoked."),
                    cancelationToken: cancelationSource.Token
                );
                TestHelper.AddCallbacksWithCancelation<float, object, string>(
                    promise,
                    onResolve: () => Assert.Fail("OnResolved was invoked."),
                    onResolveCapture: _ => Assert.Fail("OnResolved was invoked."),
                    cancelationToken: cancelationSource.Token
                );

                deferred.Resolve();
                Promise.Manager.HandleCompletes();

                cancelationSource.Dispose();
                promise.Forget();
            }

            [Test]
            public void OnResolvedIsNotInvokedIfTokenIsAlreadyCanceled_T()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                cancelationSource.Cancel();

                var deferred = Promise.NewDeferred<int>();
                var promise = deferred.Promise.Preserve();

                TestHelper.AddResolveCallbacksWithCancelation<int, float, string>(
                    promise,
                    onResolve: _ => Assert.Fail("OnResolved was invoked."),
                    onResolveCapture: _ => Assert.Fail("OnResolved was invoked."),
                    cancelationToken: cancelationSource.Token
                );
                TestHelper.AddCallbacksWithCancelation<int, float, object, string>(
                    promise,
                    onResolve: _ => Assert.Fail("OnResolved was invoked."),
                    onResolveCapture: _ => Assert.Fail("OnResolved was invoked."),
                    cancelationToken: cancelationSource.Token
                );

                deferred.Resolve(1);
                Promise.Manager.HandleCompletes();

                cancelationSource.Dispose();
                promise.Forget();
            }

            [Test]
            public void OnRejectedIsNotInvokedIfTokenIsCanceled_void()
            {
                CancelationSource cancelationSource = CancelationSource.New();

                var deferred = Promise.NewDeferred();

                TestHelper.AddCallbacksWithCancelation<float, object, string>(
                    deferred.Promise,
                    onReject: _ => Assert.Fail("OnRejected was invoked."),
                    onUnknownRejection: () => Assert.Fail("OnRejected was invoked."),
                    onRejectCapture: _ => Assert.Fail("OnRejected was invoked."),
                    onUnknownRejectionCapture: _ => Assert.Fail("OnRejected was invoked."),
                    cancelationToken: cancelationSource.Token
                );

                cancelationSource.Cancel();
                deferred.Reject("Reject");
                Promise.Manager.HandleCompletes();

                cancelationSource.Dispose();
            }

            [Test]
            public void OnRejectedIsNotInvokedIfTokenIsCanceled_T()
            {
                CancelationSource cancelationSource = CancelationSource.New();

                var deferred = Promise.NewDeferred<int>();

                TestHelper.AddCallbacksWithCancelation<int, float, object, string>(
                    deferred.Promise,
                    onReject: _ => Assert.Fail("OnRejected was invoked."),
                    onUnknownRejection: () => Assert.Fail("OnRejected was invoked."),
                    onRejectCapture: _ => Assert.Fail("OnRejected was invoked."),
                    onUnknownRejectionCapture: _ => Assert.Fail("OnRejected was invoked."),
                    cancelationToken: cancelationSource.Token
                );

                cancelationSource.Cancel();
                deferred.Reject("Reject");
                Promise.Manager.HandleCompletes();

                cancelationSource.Dispose();
            }

            [Test]
            public void OnRejectedIsNotInvokedIfTokenIsAlreadyCanceled_void()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                cancelationSource.Cancel();

                var deferred = Promise.NewDeferred();

                TestHelper.AddCallbacksWithCancelation<float, object, string>(
                    deferred.Promise,
                    onReject: _ => Assert.Fail("OnRejected was invoked."),
                    onUnknownRejection: () => Assert.Fail("OnRejected was invoked."),
                    onRejectCapture: _ => Assert.Fail("OnRejected was invoked."),
                    onUnknownRejectionCapture: _ => Assert.Fail("OnRejected was invoked."),
                    cancelationToken: cancelationSource.Token
                );

                deferred.Reject("Reject");
                Promise.Manager.HandleCompletes();

                cancelationSource.Dispose();
            }

            [Test]
            public void OnRejectedIsNotInvokedIfTokenIsAlreadyCanceled_T()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                cancelationSource.Cancel();

                var deferred = Promise.NewDeferred<int>();

                TestHelper.AddCallbacksWithCancelation<int, float, object, string>(
                    deferred.Promise,
                    onReject: _ => Assert.Fail("OnRejected was invoked."),
                    onUnknownRejection: () => Assert.Fail("OnRejected was invoked."),
                    onRejectCapture: _ => Assert.Fail("OnRejected was invoked."),
                    onUnknownRejectionCapture: _ => Assert.Fail("OnRejected was invoked."),
                    cancelationToken: cancelationSource.Token
                );

                deferred.Reject("Reject");
                Promise.Manager.HandleCompletes();

                cancelationSource.Dispose();
            }

            [Test]
            public void OnContinueIsNotInvokedIfTokenIsCanceled_void()
            {
                CancelationSource cancelationSource = CancelationSource.New();

                var deferred = Promise.NewDeferred();

                TestHelper.AddContinueCallbacksWithCancelation<float, string>(
                    deferred.Promise,
                    onContinue: _ => Assert.Fail("OnContinue was invoked."),
                    onContinueCapture: (_, __) => Assert.Fail("OnContinue was invoked."),
                    cancelationToken: cancelationSource.Token
                );

                cancelationSource.Cancel();
                deferred.Resolve();
                Promise.Manager.HandleCompletes();

                cancelationSource.Dispose();
            }

            [Test]
            public void OnContinueIsNotInvokedIfTokenIsCanceled_T()
            {
                CancelationSource cancelationSource = CancelationSource.New();

                var deferred = Promise.NewDeferred<int>();

                TestHelper.AddContinueCallbacksWithCancelation<int, float, string>(
                    deferred.Promise,
                    onContinue: _ => Assert.Fail("OnContinue was invoked."),
                    onContinueCapture: (_, __) => Assert.Fail("OnContinue was invoked."),
                    cancelationToken: cancelationSource.Token
                );

                cancelationSource.Cancel();
                deferred.Resolve(1);
                Promise.Manager.HandleCompletes();

                cancelationSource.Dispose();
            }

            [Test]
            public void OnContinueIsNotInvokedIfTokenIsAlreadyCanceled_void()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                cancelationSource.Cancel();

                var deferred = Promise.NewDeferred();

                TestHelper.AddContinueCallbacksWithCancelation<float, string>(
                    deferred.Promise,
                    onContinue: _ => Assert.Fail("OnContinue was invoked."),
                    onContinueCapture: (_, __) => Assert.Fail("OnContinue was invoked."),
                    cancelationToken: cancelationSource.Token
                );

                deferred.Resolve();
                Promise.Manager.HandleCompletes();

                cancelationSource.Dispose();
            }

            [Test]
            public void OnContinueIsNotInvokedIfTokenIsAlreadyCanceled_T()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                cancelationSource.Cancel();

                var deferred = Promise.NewDeferred<int>();

                TestHelper.AddContinueCallbacksWithCancelation<int, float, string>(
                    deferred.Promise,
                    onContinue: _ => Assert.Fail("OnContinue was invoked."),
                    onContinueCapture: (_, __) => Assert.Fail("OnContinue was invoked."),
                    cancelationToken: cancelationSource.Token
                );

                deferred.Resolve(1);
                Promise.Manager.HandleCompletes();

                cancelationSource.Dispose();
            }

            [Test]
            public void PromiseIsCanceledFromToken_void()
            {
                CancelationSource cancelationSource = CancelationSource.New();

                var deferred = Promise.NewDeferred();
                var promise = deferred.Promise.Preserve();

                int cancelCallbacks = 0;

                TestHelper.AddResolveCallbacksWithCancelation<float, string>(
                    promise,
                    cancelationToken: cancelationSource.Token,
                    onCancel: _ => ++cancelCallbacks,
                    onCancelCapture: (_, __) => ++cancelCallbacks
                );
                TestHelper.AddCallbacksWithCancelation<float, object, string>(
                    promise,
                    cancelationToken: cancelationSource.Token,
                    onCancel: _ => ++cancelCallbacks,
                    onCancelCapture: (_, __) => ++cancelCallbacks
                );
                TestHelper.AddContinueCallbacksWithCancelation<float, string>(
                    promise,
                    cancelationToken: cancelationSource.Token,
                    onCancel: _ => ++cancelCallbacks,
                    onCancelCapture: (_, __) => ++cancelCallbacks
                );

                cancelationSource.Cancel();
                deferred.Resolve();
                Promise.Manager.HandleCompletes();

                Assert.AreEqual(
                    TestHelper.cancelVoidCallbacks,
                    cancelCallbacks
                );

                cancelationSource.Dispose();
                promise.Forget();
            }

            [Test]
            public void PromiseIsCanceledFromToken_T()
            {
                CancelationSource cancelationSource = CancelationSource.New();

                var deferred = Promise.NewDeferred<int>();
                var promise = deferred.Promise.Preserve();

                int cancelCallbacks = 0;

                TestHelper.AddResolveCallbacksWithCancelation<int, float, string>(
                    promise,
                    cancelationToken: cancelationSource.Token,
                    onCancel: _ => ++cancelCallbacks,
                    onCancelCapture: (_, __) => ++cancelCallbacks
                );
                TestHelper.AddCallbacksWithCancelation<int, float, object, string>(
                    promise,
                    cancelationToken: cancelationSource.Token,
                    onCancel: _ => ++cancelCallbacks,
                    onCancelCapture: (_, __) => ++cancelCallbacks
                );
                TestHelper.AddContinueCallbacksWithCancelation<int, float, string>(
                    promise,
                    cancelationToken: cancelationSource.Token,
                    onCancel: _ => ++cancelCallbacks,
                    onCancelCapture: (_, __) => ++cancelCallbacks
                );

                cancelationSource.Cancel();
                deferred.Resolve(1);
                Promise.Manager.HandleCompletes();

                Assert.AreEqual(
                    TestHelper.cancelTCallbacks,
                    cancelCallbacks
                );

                cancelationSource.Dispose();
                promise.Forget();
            }

            [Test]
            public void PromiseIsCanceledFromAlreadyCanceledToken_void()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                cancelationSource.Cancel();

                var deferred = Promise.NewDeferred();
                var promise = deferred.Promise.Preserve();

                int cancelCallbacks = 0;

                TestHelper.AddResolveCallbacksWithCancelation<float, string>(
                    promise,
                    cancelationToken: cancelationSource.Token,
                    onCancel: _ => ++cancelCallbacks,
                    onCancelCapture: (_, __) => ++cancelCallbacks
                );
                TestHelper.AddCallbacksWithCancelation<float, object, string>(
                    promise,
                    cancelationToken: cancelationSource.Token,
                    onCancel: _ => ++cancelCallbacks,
                    onCancelCapture: (_, __) => ++cancelCallbacks
                );
                TestHelper.AddContinueCallbacksWithCancelation<float, string>(
                    promise,
                    cancelationToken: cancelationSource.Token,
                    onCancel: _ => ++cancelCallbacks,
                    onCancelCapture: (_, __) => ++cancelCallbacks
                );

                deferred.Resolve();
                Promise.Manager.HandleCompletes();

                Assert.AreEqual(
                    TestHelper.cancelVoidCallbacks,
                    cancelCallbacks
                );

                cancelationSource.Dispose();
                promise.Forget();
            }

            [Test]
            public void PromiseIsCanceledFromAlreadyCanceledToken_T()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                cancelationSource.Cancel();

                var deferred = Promise.NewDeferred<int>();
                var promise = deferred.Promise.Preserve();

                int cancelCallbacks = 0;

                TestHelper.AddResolveCallbacksWithCancelation<int, float, string>(
                    promise,
                    cancelationToken: cancelationSource.Token,
                    onCancel: _ => ++cancelCallbacks,
                    onCancelCapture: (_, __) => ++cancelCallbacks
                );
                TestHelper.AddCallbacksWithCancelation<int, float, object, string>(
                    promise,
                    cancelationToken: cancelationSource.Token,
                    onCancel: _ => ++cancelCallbacks,
                    onCancelCapture: (_, __) => ++cancelCallbacks
                );
                TestHelper.AddContinueCallbacksWithCancelation<int, float, string>(
                    promise,
                    cancelationToken: cancelationSource.Token,
                    onCancel: _ => ++cancelCallbacks,
                    onCancelCapture: (_, __) => ++cancelCallbacks
                );

                deferred.Resolve(1);
                Promise.Manager.HandleCompletes();

                Assert.AreEqual(
                    TestHelper.cancelTCallbacks,
                    cancelCallbacks
                );

                cancelationSource.Dispose();
                promise.Forget();
            }
        }
    }
}