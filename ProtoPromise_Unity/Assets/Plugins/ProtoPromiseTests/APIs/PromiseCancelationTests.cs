#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using NUnit.Framework;
using Proto.Promises;
using System;

namespace ProtoPromiseTests.APIs
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
                Assert.IsTrue(deferred.IsValidAndPending);

                deferred.Promise
                    .CatchCancelation(() => state = Canceled)
                    .Then(() => state = Resolved, () => state = Rejected)
                    .Forget();
                Assert.IsNull(state);

                deferred.Resolve();
                Assert.IsFalse(deferred.IsValidAndPending);

                Assert.AreEqual(Resolved, state);

                state = null;
                deferred = Promise.NewDeferred();
                Assert.IsTrue(deferred.IsValidAndPending);

                deferred.Promise
                    .CatchCancelation(() => state = Canceled)
                    .Then(() => state = Resolved, () => state = Rejected)
                    .Forget();
                Assert.IsNull(state);

                deferred.Reject("Fail Value");
                Assert.IsFalse(deferred.IsValidAndPending);

                state = null;
                CancelationSource cancelationSource = CancelationSource.New();
                deferred = Promise.NewDeferred(cancelationSource.Token);
                Assert.IsTrue(deferred.IsValidAndPending);

                deferred.Promise
                    .CatchCancelation(() => state = Canceled)
                    .Then(() => state = Resolved, () => state = Rejected)
                    .Forget();
                Assert.IsNull(state);

                cancelationSource.Cancel();
                Assert.IsFalse(deferred.IsValidAndPending);

                Assert.AreEqual(Canceled, state);
                cancelationSource.Dispose();

                state = null;
                cancelationSource = CancelationSource.New();
                deferred = Promise.NewDeferred(cancelationSource.Token);
                Assert.IsTrue(deferred.IsValidAndPending);

                deferred.Promise
                    .CatchCancelation(() => state = Canceled)
                    .Then(() => state = Resolved, () => state = Rejected)
                    .Forget();
                Assert.IsNull(state);

                cancelationSource.Cancel();
                Assert.IsFalse(deferred.IsValidAndPending);

                Assert.AreEqual(Canceled, state);
                cancelationSource.Dispose();
            }

            [Test]
            public void MayTransitionToEitherTheFulfilledOrRejectedOrCanceledState_T()
            {
                string Resolved = "Resolved", Rejected = "Rejected", Canceled = "Canceled";
                string state = null;

                var deferred = Promise.NewDeferred<int>();
                Assert.IsTrue(deferred.IsValidAndPending);

                deferred.Promise
                    .CatchCancelation(() => state = Canceled)
                    .Then(v => state = Resolved, () => state = Rejected)
                    .Forget();
                Assert.IsNull(state);

                deferred.Resolve(1);
                Assert.IsFalse(deferred.IsValidAndPending);

                Assert.AreEqual(Resolved, state);

                state = null;
                deferred = Promise.NewDeferred<int>();
                Assert.IsTrue(deferred.IsValidAndPending);

                deferred.Promise
                    .CatchCancelation(() => state = Canceled)
                    .Then(v => state = Resolved, () => state = Rejected)
                    .Forget();
                Assert.IsNull(state);

                deferred.Reject("Fail Value");
                Assert.IsFalse(deferred.IsValidAndPending);

                state = null;
                CancelationSource cancelationSource = CancelationSource.New();
                deferred = Promise.NewDeferred<int>(cancelationSource.Token);
                Assert.IsTrue(deferred.IsValidAndPending);

                deferred.Promise
                    .CatchCancelation(() => state = Canceled)
                    .Then(v => state = Resolved, () => state = Rejected)
                    .Forget();
                Assert.IsNull(state);

                cancelationSource.Cancel();
                Assert.IsFalse(deferred.IsValidAndPending);

                Assert.AreEqual(Canceled, state);
                cancelationSource.Dispose();

                state = null;
                cancelationSource = CancelationSource.New();
                deferred = Promise.NewDeferred<int>(cancelationSource.Token);
                Assert.IsTrue(deferred.IsValidAndPending);

                deferred.Promise
                    .CatchCancelation(() => state = Canceled)
                    .Then(v => state = Resolved, () => state = Rejected)
                    .Forget();
                Assert.IsNull(state);

                cancelationSource.Cancel();
                Assert.IsFalse(deferred.IsValidAndPending);

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
                    .CatchCancelation(() => Assert.Fail("Promise was canceled when it was already resolved."))
                    .Forget();

                deferred.Resolve();

                Assert.IsFalse(deferred.TryResolve());
                Assert.Throws<Proto.Promises.InvalidOperationException>(() => deferred.Resolve());
                Assert.IsFalse(deferred.TryReject("Fail value"));
                Assert.Throws<Proto.Promises.InvalidOperationException>(() => deferred.Reject("Fail value"));

                cancelationSource.Cancel();

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
                    .CatchCancelation(() => Assert.Fail("Promise was canceled when it was already resolved."))
                    .Forget();

                deferred.Resolve(1);

                Assert.IsFalse(deferred.TryResolve(1));
                Assert.Throws<Proto.Promises.InvalidOperationException>(() => deferred.Resolve(1));
                Assert.IsFalse(deferred.TryReject("Fail value"));
                Assert.Throws<Proto.Promises.InvalidOperationException>(() => deferred.Reject("Fail value"));

                cancelationSource.Cancel();

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
                    .CatchCancelation(() => Assert.Fail("Promise was canceled when it was already rejected."))
                    .Forget();

                deferred.Reject("Fail Value");

                Assert.IsFalse(deferred.TryResolve());
                Assert.Throws<Proto.Promises.InvalidOperationException>(() => deferred.Resolve());
                Assert.IsFalse(deferred.TryReject("Fail value"));
                Assert.Throws<Proto.Promises.InvalidOperationException>(() => deferred.Reject("Fail value"));

                cancelationSource.Cancel();

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
                    .CatchCancelation(() => Assert.Fail("Promise was canceled when it was already rejected."))
                    .Forget();

                deferred.Reject("Fail Value");

                Assert.IsFalse(deferred.TryResolve(1));
                Assert.Throws<Proto.Promises.InvalidOperationException>(() => deferred.Resolve(1));
                Assert.IsFalse(deferred.TryReject("Fail value"));
                Assert.Throws<Proto.Promises.InvalidOperationException>(() => deferred.Reject("Fail value"));

                cancelationSource.Cancel();

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
                    .CatchCancelation(() => { canceled = true; })
                    .Forget();

                cancelationSource.Cancel();

                Assert.IsFalse(deferred.TryResolve());
                Assert.Throws<Proto.Promises.InvalidOperationException>(() => deferred.Resolve());
                Assert.IsFalse(deferred.TryReject("Fail value"));
                Assert.Throws<Proto.Promises.InvalidOperationException>(() => deferred.Reject("Fail value"));
                Assert.Throws<Proto.Promises.InvalidOperationException>(() => cancelationSource.Cancel());

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
                    .CatchCancelation(() => { canceled = true; })
                    .Forget();

                cancelationSource.Cancel();

                Assert.IsFalse(deferred.TryResolve(1));
                Assert.Throws<Proto.Promises.InvalidOperationException>(() => deferred.Resolve(1));
                Assert.IsFalse(deferred.TryReject("Fail value"));
                Assert.Throws<Proto.Promises.InvalidOperationException>(() => deferred.Reject("Fail value"));
                Assert.Throws<Proto.Promises.InvalidOperationException>(() => cancelationSource.Cancel());

                Assert.IsTrue(canceled);

                cancelationSource.Dispose();
            }
        }

#if PROMISE_DEBUG
        [Test]
        public void IfOnCanceledIsNullThrow_void()
        {
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred(cancelationSource.Token);

            Assert.Throws<Proto.Promises.ArgumentNullException>(() =>
            {
                deferred.Promise.CatchCancelation(default(Action));
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

            Assert.Throws<Proto.Promises.ArgumentNullException>(() =>
            {
                deferred.Promise.CatchCancelation(default(Action));
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
            public void ItMustBeCalledAfterPromiseIsCanceled()
            {
                var canceled = false;
                CancelationSource cancelationSource = CancelationSource.New();
                var deferred = Promise.NewDeferred(cancelationSource.Token);
                deferred.Promise
                    .CatchCancelation(() =>
                    {
                        canceled = true;
                    })
                    .Forget();
                cancelationSource.Cancel();

                Assert.True(canceled);

                cancelationSource.Dispose();
            }

            [Test]
            public void ItMustNotBeCalledBeforePromiseIsCanceled()
            {
                var canceled = false;
                CancelationSource cancelationSource = CancelationSource.New();
                var deferred = Promise.NewDeferred(cancelationSource.Token);
                deferred.Promise
                    .CatchCancelation(() =>
                    {
                        canceled = true;
                    })
                    .Forget();

                Assert.False(canceled);

                cancelationSource.Cancel();

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
                    .CatchCancelation(() => ++cancelCount)
                    .Forget();
                cancelationSource.Cancel();

                Assert.Throws<Proto.Promises.InvalidOperationException>(() =>
                    cancelationSource.Cancel()
                );

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
                .WaitAsync(SynchronizationOption.Foreground)
                .CatchCancelation(() => canceled = true)
                .Forget();
            cancelationSource.Cancel();
            Assert.False(canceled);

            TestHelper.ExecuteForegroundCallbacks();
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

                promise.CatchCancelation(() => Assert.AreEqual(0, counter++)).Forget();
                promise.CatchCancelation(() => Assert.AreEqual(1, counter++)).Forget();
                promise.CatchCancelation(() => Assert.AreEqual(2, counter++)).Forget();

                cancelationSource.Cancel();

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
                .CatchCancelation(() => invoked = true)
                .Forget();

            cancelationSource.Cancel();

            Assert.IsTrue(invoked);

            cancelationSource.Dispose();
        }

        [Test]
        public void IfPromiseIsCanceledAndAPreviousPromiseIsAlsoCanceled_onCanceledMustBeCalled()
        {
            var deferred = Promise.NewDeferred();

            bool invoked = false;

            CancelationSource cancelationSource = CancelationSource.New();

            deferred.Promise
                .Then(() => { }, cancelationSource.Token)
                .CatchCancelation(() =>
                {
                    invoked = true;
                })
                .Finally(cancelationSource.Dispose)
                .Forget();

            cancelationSource.Cancel();
            deferred.Resolve();

            Assert.IsTrue(invoked);
        }

        [Test]
        public void APromiseMayBeCanceledWhenItIsPending()
        {
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred();

            bool canceled = false;

            deferred.Promise
                .Then(() => cancelationSource.Cancel())
                .Then(() => { }, cancelationSource.Token)
                .Then(() => Assert.Fail("Promise was resolved when it should have been canceled."),
                    () => Assert.Fail("Promise was rejected when it should have been canceled."))
                .CatchCancelation(() => canceled = true)
                .Finally(cancelationSource.Dispose)
                .Forget();

            deferred.Resolve();
            Assert.IsTrue(canceled);
        }

        [Test]
        public void CatchCancelationCaptureValue()
        {
            CancelationSource cancelationSource = CancelationSource.New();
            int captureValue = 100;
            var deferred = Promise.NewDeferred();
            var promise = deferred.Promise.Preserve();

            promise
                .Then(() => { }, cancelationSource.Token)
                .CatchCancelation(captureValue, cv => Assert.AreEqual(captureValue, cv))
                .Forget();
            promise
                .Then(() => 1f, cancelationSource.Token)
                .CatchCancelation(captureValue, cv => Assert.AreEqual(captureValue, cv))
                .Forget();
            cancelationSource.Cancel();

            deferred.Resolve();

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
                var canceled = false;
                CancelationSource cancelationSource = CancelationSource.New();
                cancelationSource.Cancel();

                var deferred = Promise.NewDeferred(cancelationSource.Token);

                deferred.Promise
                    .CatchCancelation(() =>
                    {
                        canceled = true;
                    })
                    .Forget();

                Assert.True(canceled);

                cancelationSource.Dispose();
            }

            [Test]
            public void OnCanceledIsNotInvokedIfTokenIsCanceled_void0()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                var deferred = Promise.NewDeferred(cancelationSource.Token);

                deferred.Promise
                    .CatchCancelation(() => Assert.Fail("OnCanceled was invoked."), cancelationSource.Token)
                    .CatchCancelation(1, cv => Assert.Fail("OnCanceled was invoked."), cancelationSource.Token)
                    .Forget();

                cancelationSource.Cancel();

                cancelationSource.Dispose();
            }

            [Test]
            public void OnCanceledIsNotInvokedIfTokenIsCanceled_T0()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                var deferred = Promise.NewDeferred<int>(cancelationSource.Token);

                deferred.Promise
                    .CatchCancelation(() => Assert.Fail("OnCanceled was invoked."), cancelationSource.Token)
                    .CatchCancelation(1, cv => Assert.Fail("OnCanceled was invoked."), cancelationSource.Token)
                    .Forget();

                cancelationSource.Cancel();

                cancelationSource.Dispose();
            }

            [Test]
            public void OnCanceledIsNotInvokedIfTokenIsCanceled_void1()
            {
                CancelationSource deferredCancelationSource = CancelationSource.New();
                CancelationSource catchCancelationSource = CancelationSource.New();
                var deferred = Promise.NewDeferred(deferredCancelationSource.Token);

                deferred.Promise
                    .CatchCancelation(() => Assert.Fail("OnCanceled was invoked."), catchCancelationSource.Token)
                    .CatchCancelation(1, cv => Assert.Fail("OnCanceled was invoked."), catchCancelationSource.Token)
                    .Forget();

                catchCancelationSource.Cancel();
                deferredCancelationSource.Cancel();

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
                    .CatchCancelation(() => Assert.Fail("OnCanceled was invoked."), catchCancelationSource.Token)
                    .CatchCancelation(1, cv => Assert.Fail("OnCanceled was invoked."), catchCancelationSource.Token)
                    .Forget();

                catchCancelationSource.Cancel();
                deferredCancelationSource.Cancel();

                catchCancelationSource.Dispose();
                deferredCancelationSource.Dispose();
            }

            [Test]
            public void OnCanceledIsNotInvokedIfTokenIsAlreadyCanceled()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                cancelationSource.Cancel();

                Promise.Canceled()
                    .CatchCancelation(() => Assert.Fail("OnCanceled was invoked."), cancelationSource.Token)
                    .CatchCancelation(1, cv => Assert.Fail("OnCanceled was invoked."), cancelationSource.Token)
                    .Forget();
                Promise.Canceled<int>()
                    .CatchCancelation(() => Assert.Fail("OnCanceled was invoked."), cancelationSource.Token)
                    .CatchCancelation(1, cv => Assert.Fail("OnCanceled was invoked."), cancelationSource.Token)
                    .Forget();

                cancelationSource.Dispose();
            }

            [Test]
            public void OnCanceledIsNotInvokedIfPromiseIsResolved_void0()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                var deferred = Promise.NewDeferred();

                deferred.Promise
                    .CatchCancelation(() => Assert.Fail("OnCanceled was invoked."), cancelationSource.Token)
                    .CatchCancelation(1, cv => Assert.Fail("OnCanceled was invoked."), cancelationSource.Token)
                    .Forget();

                deferred.Resolve();
                cancelationSource.Cancel();

                cancelationSource.Dispose();
            }

            [Test]
            public void OnCanceledIsNotInvokedIfPromiseIsResolved_T0()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                var deferred = Promise.NewDeferred<int>();

                deferred.Promise
                    .CatchCancelation(() => Assert.Fail("OnCanceled was invoked."), cancelationSource.Token)
                    .CatchCancelation(1, cv => Assert.Fail("OnCanceled was invoked."), cancelationSource.Token)
                    .Forget();

                deferred.Resolve(1);
                cancelationSource.Cancel();

                cancelationSource.Dispose();
            }

            [Test]
            public void OnCanceledIsNotInvokedIfPromiseIsResolved_void1()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                var deferred = Promise.NewDeferred();

                deferred.Promise
                    .CatchCancelation(() => Assert.Fail("OnCanceled was invoked."), cancelationSource.Token)
                    .CatchCancelation(1, cv => Assert.Fail("OnCanceled was invoked."), cancelationSource.Token)
                    .Forget();

                deferred.Resolve();

                cancelationSource.Dispose();
            }

            [Test]
            public void OnCanceledIsNotInvokedIfPromiseIsResolved_T1()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                var deferred = Promise.NewDeferred<int>();

                deferred.Promise
                    .CatchCancelation(() => Assert.Fail("OnCanceled was invoked."), cancelationSource.Token)
                    .CatchCancelation(1, cv => Assert.Fail("OnCanceled was invoked."), cancelationSource.Token)
                    .Forget();

                deferred.Resolve(1);

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
                    onCancel: () => ++cancelCallbacks,
                    onCancelCapture: _ => ++cancelCallbacks
                );
                TestHelper.AddCallbacksWithCancelation<float, object, string>(
                    promise,
                    cancelationToken: cancelationSource.Token,
                    onCancel: () => ++cancelCallbacks,
                    onCancelCapture: _ => ++cancelCallbacks
                );
                TestHelper.AddContinueCallbacksWithCancelation<float, string>(
                    promise,
                    cancelationToken: cancelationSource.Token,
                    onCancel: () => ++cancelCallbacks,
                    onCancelCapture: _ => ++cancelCallbacks
                );

                cancelationSource.Cancel();
                deferred.Resolve();

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
                    onCancel: () => ++cancelCallbacks,
                    onCancelCapture: _ => ++cancelCallbacks
                );
                TestHelper.AddCallbacksWithCancelation<int, float, object, string>(
                    promise,
                    cancelationToken: cancelationSource.Token,
                    onCancel: () => ++cancelCallbacks,
                    onCancelCapture: _ => ++cancelCallbacks
                );
                TestHelper.AddContinueCallbacksWithCancelation<int, float, string>(
                    promise,
                    cancelationToken: cancelationSource.Token,
                    onCancel: () => ++cancelCallbacks,
                    onCancelCapture: _ => ++cancelCallbacks
                );

                cancelationSource.Cancel();
                deferred.Resolve(1);

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
                    onCancel: () => ++cancelCallbacks,
                    onCancelCapture: _ => ++cancelCallbacks
                );
                TestHelper.AddCallbacksWithCancelation<float, object, string>(
                    promise,
                    cancelationToken: cancelationSource.Token,
                    onCancel: () => ++cancelCallbacks,
                    onCancelCapture: _ => ++cancelCallbacks
                );
                TestHelper.AddContinueCallbacksWithCancelation<float, string>(
                    promise,
                    cancelationToken: cancelationSource.Token,
                    onCancel: () => ++cancelCallbacks,
                    onCancelCapture: _ => ++cancelCallbacks
                );

                deferred.Resolve();

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
                    onCancel: () => ++cancelCallbacks,
                    onCancelCapture: _ => ++cancelCallbacks
                );
                TestHelper.AddCallbacksWithCancelation<int, float, object, string>(
                    promise,
                    cancelationToken: cancelationSource.Token,
                    onCancel: () => ++cancelCallbacks,
                    onCancelCapture: _ => ++cancelCallbacks
                );
                TestHelper.AddContinueCallbacksWithCancelation<int, float, string>(
                    promise,
                    cancelationToken: cancelationSource.Token,
                    onCancel: () => ++cancelCallbacks,
                    onCancelCapture: _ => ++cancelCallbacks
                );

                deferred.Resolve(1);

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