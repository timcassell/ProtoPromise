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
            public void MayTransitionToEitherTheFulfilledOrRejectedOrCanceledState_void()
            {
                void Test()
                {
                    string Resolved = "Resolved", Rejected = "Rejected", Canceled = "Canceled";
                    string state = null;

                    var deferred = Promise.NewDeferred();
                    Assert.IsTrue(deferred.IsPending);

                    deferred.Promise
                        .CatchCancelation(_ => state = Canceled)
                        .Then(() => state = Resolved, () => state = Rejected)
                        .Forget();
                    Assert.AreEqual(null, state);

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
                    Assert.AreEqual(null, state);

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
                    Assert.AreEqual(null, state);

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
                    Assert.AreEqual(null, state);

                    cancelationSource.Cancel("Cancel Value");
                    Assert.IsFalse(deferred.IsPending);
                    Promise.Manager.HandleCompletesAndProgress();

                    Assert.AreEqual(Canceled, state);
                    cancelationSource.Dispose();
                }

                Test();
                TestHelper.Cleanup();
            }

            [Test]
            public void MayTransitionToEitherTheFulfilledOrRejectedOrCanceledState_T()
            {
                void Test()
                {
                    string Resolved = "Resolved", Rejected = "Rejected", Canceled = "Canceled";
                    string state = null;

                    var deferred = Promise.NewDeferred<int>();
                    Assert.IsTrue(deferred.IsPending);

                    deferred.Promise
                        .CatchCancelation(_ => state = Canceled)
                        .Then(v => state = Resolved, () => state = Rejected)
                        .Forget();
                    Assert.AreEqual(null, state);

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
                    Assert.AreEqual(null, state);

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
                    Assert.AreEqual(null, state);

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
                    Assert.AreEqual(null, state);

                    cancelationSource.Cancel("Cancel Value");
                    Assert.IsFalse(deferred.IsPending);
                    Promise.Manager.HandleCompletesAndProgress();

                    Assert.AreEqual(Canceled, state);
                    cancelationSource.Dispose();
                }

                Test();
                TestHelper.Cleanup();
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
            public void MustNotTransitionToAnyOtherState_void()
            {
                void Test()
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

                    Assert.AreEqual(false, deferred.TryResolve());
                    Assert.Throws<InvalidOperationException>(() => deferred.Resolve());
                    Assert.AreEqual(false, deferred.TryReject("Fail value"));
                    Assert.Throws<InvalidOperationException>(() => deferred.Reject("Fail value"));

                    cancelationSource.Cancel();

                    Promise.Manager.HandleCompletes();
                    Assert.IsTrue(resolved);

                    cancelationSource.Dispose();
                }

                Test();
                TestHelper.Cleanup();
            }

            [Test]
            public void MustNotTransitionToAnyOtherState_T()
            {
                void Test()
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

                    Assert.AreEqual(false, deferred.TryResolve(1));
                    Assert.Throws<InvalidOperationException>(() => deferred.Resolve(1));
                    Assert.AreEqual(false, deferred.TryReject("Fail value"));
                    Assert.Throws<InvalidOperationException>(() => deferred.Reject("Fail value"));

                    cancelationSource.Cancel();

                    Promise.Manager.HandleCompletes();
                    Assert.IsTrue(resolved);

                    cancelationSource.Dispose();
                }

                Test();
                TestHelper.Cleanup();
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
            public void MustNotTransitionToAnyOtherState_void()
            {
                void Test()
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

                    Assert.AreEqual(false, deferred.TryResolve());
                    Assert.Throws<InvalidOperationException>(() => deferred.Resolve());
                    Assert.AreEqual(false, deferred.TryReject("Fail value"));
                    Assert.Throws<InvalidOperationException>(() => deferred.Reject("Fail value"));

                    cancelationSource.Cancel();

                    Promise.Manager.HandleCompletes();
                    Assert.IsTrue(rejected);

                    cancelationSource.Dispose();
                }

                Test();
                TestHelper.Cleanup();
            }

            [Test]
            public void MustNotTransitionToAnyOtherState_T()
            {
                void Test()
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

                    Assert.AreEqual(false, deferred.TryResolve(1));
                    Assert.Throws<InvalidOperationException>(() => deferred.Resolve(1));
                    Assert.AreEqual(false, deferred.TryReject("Fail value"));
                    Assert.Throws<InvalidOperationException>(() => deferred.Reject("Fail value"));

                    cancelationSource.Cancel();

                    Promise.Manager.HandleCompletes();
                    Assert.IsTrue(rejected);

                    cancelationSource.Dispose();
                }

                Test();
                TestHelper.Cleanup();
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
            public void MustNotTransitionToAnyOtherState_void()
            {
                void Test()
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

                    Assert.AreEqual(false, deferred.TryResolve());
                    Assert.Throws<InvalidOperationException>(() => deferred.Resolve());
                    Assert.AreEqual(false, deferred.TryReject("Fail value"));
                    Assert.Throws<InvalidOperationException>(() => deferred.Reject("Fail value"));
                    Assert.Throws<InvalidOperationException>(() => cancelationSource.Cancel());

                    Promise.Manager.HandleCompletes();

                    Assert.IsTrue(canceled);

                    cancelationSource.Dispose();
                }

                Test();
                TestHelper.Cleanup();
            }

            [Test]
            public void MustNotTransitionToAnyOtherState_T()
            {
                void Test()
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

                    Assert.AreEqual(false, deferred.TryResolve(1));
                    Assert.Throws<InvalidOperationException>(() => deferred.Resolve(1));
                    Assert.AreEqual(false, deferred.TryReject("Fail value"));
                    Assert.Throws<InvalidOperationException>(() => deferred.Reject("Fail value"));
                    Assert.Throws<InvalidOperationException>(() => cancelationSource.Cancel());

                    Promise.Manager.HandleCompletes();

                    Assert.IsTrue(canceled);

                    cancelationSource.Dispose();
                }

                Test();
                TestHelper.Cleanup();
            }

            [Test]
            public void MustHaveAReasonWhichMustNotChange_void()
            {
                void Test()
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
                    promise.CatchCancelation(cancelValue => Assert.AreEqual(expected, cancelation = cancelValue.Value));
                    cancelationSource.Cancel(expected);
                    Promise.Manager.HandleCompletes();

                    Assert.AreEqual(expected, cancelation);

                    TestHelper.AddCallbacks<int, object, string>(promise,
                        onResolve: resolveAssert,
                        onReject: failValue => rejectAssert(),
                        onUnknownRejection: rejectAssert);
                    promise.CatchCancelation(cancelValue => Assert.AreEqual(expected, cancelation = cancelValue.Value));

                    Assert.Throws<InvalidOperationException>(() =>
                        cancelationSource.Cancel("Different Cancel Value")
                    );

                    Promise.Manager.HandleCompletes();

                    Assert.AreEqual(expected, cancelation);

                    promise.Forget();
                    cancelationSource.Dispose();
                }

                Test();
                TestHelper.Cleanup();
            }

            [Test]
            public void MustHaveAReasonWhichMustNotChange_T()
            {
                void Test()
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
                    promise.CatchCancelation(cancelValue => Assert.AreEqual(expected, cancelation = cancelValue.Value));
                    cancelationSource.Cancel(expected);
                    Promise.Manager.HandleCompletes();

                    Assert.AreEqual(expected, cancelation);

                    TestHelper.AddCallbacks<int, bool, object, string>(promise,
                        onResolve: _ => resolveAssert(),
                        onReject: _ => rejectAssert(),
                        onUnknownRejection: rejectAssert);
                    promise.CatchCancelation(cancelValue => Assert.AreEqual(expected, cancelation = cancelValue.Value));

                    Assert.Throws<InvalidOperationException>(() =>
                        cancelationSource.Cancel("Different Cancel Value")
                    );

                    Promise.Manager.HandleCompletes();

                    Assert.AreEqual(expected, cancelation);

                    promise.Forget();
                    cancelationSource.Dispose();
                }

                Test();
                TestHelper.Cleanup();
            }
        }

#if PROMISE_DEBUG
        [Test]
        public void IfOnCanceledIsNullThrow_void()
        {
            void Test()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                var deferred = Promise.NewDeferred(cancelationSource.Token);
                var promise = deferred.Promise.Preserve();

                Assert.Throws<ArgumentNullException>(() =>
                {
                    promise.CatchCancelation(default(Promise.CanceledAction));
                });

                cancelationSource.Cancel();
                cancelationSource.Dispose();
                promise.Forget();
            }

            Test();
            TestHelper.Cleanup();
        }

        [Test]
        public void IfOnCanceledIsNullThrow_T()
        {
            void Test()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                var deferred = Promise.NewDeferred<int>(cancelationSource.Token);
                var promise = deferred.Promise.Preserve();

                Assert.Throws<ArgumentNullException>(() =>
                {
                    promise.CatchCancelation(default(Promise.CanceledAction));
                });

                cancelationSource.Cancel();
                cancelationSource.Dispose();
                promise.Forget();
            }

            Test();
            TestHelper.Cleanup();
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
                void Test()
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

                Test();
                TestHelper.Cleanup();
            }

            [Test]
            public void ItMustNotBeCalledBeforePromiseIsCanceled()
            {
                void Test()
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

                Test();
                TestHelper.Cleanup();
            }

            [Test]
            public void ItMustNotBeCalledMoreThanOnce()
            {
                void Test()
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

                Test();
                TestHelper.Cleanup();
            }
        }

        [Test]
        public void OnCanceledMustNotBeCalledUntilTheExecutionContextStackContainsOnlyPlatformCode()
        {
            void Test()
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

            Test();
            TestHelper.Cleanup();
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
            public void IfWhenPromiseCancelationIsCanceled_AllRespectiveOnCanceledCallbacksMustExecuteInTheOrderOfTheirOriginatingCallsToCatchCancelation()
            {
                void Test()
                {
                    CancelationSource cancelationSource = CancelationSource.New();
                    var deferred = Promise.NewDeferred(cancelationSource.Token);
                    var promise = deferred.Promise.Preserve();

                    int counter = 0;

                    promise.CatchCancelation(e => Assert.AreEqual(0, counter++));
                    promise.CatchCancelation(e => Assert.AreEqual(1, counter++));
                    promise.CatchCancelation(e => Assert.AreEqual(2, counter++));

                    cancelationSource.Cancel();
                    Promise.Manager.HandleCompletes();

                    Assert.AreEqual(3, counter);

                    cancelationSource.Dispose();
                    promise.Forget();
                }

                Test();
                TestHelper.Cleanup();
            }
        }

        [Test]
        public void IfPromiseIsCanceled_OnResolveAndOnRejectedMustNotBeInvoked_void()
        {
            void Test()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                var deferred = Promise.NewDeferred(cancelationSource.Token);
                    var promise = deferred.Promise.Preserve();

                Action rejectAssert = () => Assert.Fail("Promise was rejected when it should have been canceled.");

                TestHelper.AddCallbacks<int, object, string>(promise,
                    onResolve: () => Assert.Fail("Promise was resolved when it should have been canceled."),
                    onReject: _ => rejectAssert(),
                    onUnknownRejection: rejectAssert);

                cancelationSource.Cancel();
                Promise.Manager.HandleCompletes();

                cancelationSource.Dispose();
                    promise.Forget();
            }

            Test();
            TestHelper.Cleanup();
        }

        [Test]
        public void IfPromiseIsCanceled_OnResolveAndOnRejectedMustNotBeInvoked_T()
        {
            void Test()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                var deferred = Promise.NewDeferred<int>(cancelationSource.Token);
                var promise = deferred.Promise.Preserve();

                Action rejectAssert = () => Assert.Fail("Promise was rejected when it should have been canceled.");

                TestHelper.AddCallbacks<int, bool, object, string>(promise,
                    onResolve: v => Assert.Fail("Promise was resolved when it should have been canceled."),
                    onReject: _ => rejectAssert(),
                    onUnknownRejection: rejectAssert);

                cancelationSource.Cancel();
                Promise.Manager.HandleCompletes();

                cancelationSource.Dispose();
                promise.Forget();
            }

            Test();
            TestHelper.Cleanup();
        }

        [Test]
        public void CancelationsDoNotPropagateToRoot()
        {
            void Test()
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

                Assert.AreEqual(true, resolved);

            }

            Test();
            TestHelper.Cleanup();
        }

        [Test]
        public void CancelationsPropagateToBranches()
        {
            void Test()
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

                Assert.AreEqual(true, invoked);

                cancelationSource.Dispose();
            }

            Test();
            TestHelper.Cleanup();
        }

        [Test]
        public void IfPromiseIsCanceledAndAPreviousPromiseIsAlsoCanceled_PromiseMustBeCanceledWithTheInitialCancelReason()
        {
            void Test()
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

                Assert.AreEqual(true, invoked);
            }

            Test();
            TestHelper.Cleanup();
        }

        [Test]
        public void APromiseMayBeCanceledWhenItIsPending()
        {
            void Test()
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

            Test();
            TestHelper.Cleanup();
        }

        [Test]
        public void CatchCancelationCaptureValue()
        {
            void Test()
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

            Test();
            TestHelper.Cleanup();
        }

        public class CancelationToken
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
            public void DeferredIsCanceledFromAlreadyCanceledToken()
            {
                void Test()
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

                Test();
                TestHelper.Cleanup();
            }

            [Test]
            public void OnCanceledIsNotInvokedIfTokenIsCanceled_void()
            {
                void Test()
                {
                    CancelationSource cancelationSource = CancelationSource.New();
                    var deferred = Promise.NewDeferred(cancelationSource.Token);
                    var promise = deferred.Promise.Preserve();

                    promise
                        .CatchCancelation(_ => Assert.Fail("OnCanceled was invoked."), cancelationSource.Token)
                        .CatchCancelation(1, (cv, _) => Assert.Fail("OnCanceled was invoked."), cancelationSource.Token);

                    cancelationSource.Cancel();
                    Promise.Manager.HandleCompletes();

                    cancelationSource.Dispose();
                    promise.Forget();
                }

                Test();
                TestHelper.Cleanup();
            }

            [Test]
            public void OnCanceledIsNotInvokedIfTokenIsCanceled_T()
            {
                void Test()
                {
                    CancelationSource cancelationSource = CancelationSource.New();
                    var deferred = Promise.NewDeferred<int>(cancelationSource.Token);
                    var promise = deferred.Promise.Preserve();

                    promise
                        .CatchCancelation(_ => Assert.Fail("OnCanceled was invoked."), cancelationSource.Token)
                        .CatchCancelation(1, (cv, _) => Assert.Fail("OnCanceled was invoked."), cancelationSource.Token);

                    cancelationSource.Cancel();
                    Promise.Manager.HandleCompletes();

                    cancelationSource.Dispose();
                    promise.Forget();
                }

                Test();
                TestHelper.Cleanup();
            }

            [Test]
            public void OnCanceledIsNotInvokedIfTokenIsAlreadyCanceled()
            {
                void Test()
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

                Test();
                TestHelper.Cleanup();
            }

            [Test]
            public void OnResolvedIsNotInvokedIfTokenIsCanceled_void()
            {
                void Test()
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

                Test();
                TestHelper.Cleanup();
            }

            [Test]
            public void OnResolvedIsNotInvokedIfTokenIsCanceled_T()
            {
                void Test()
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

                Test();
                TestHelper.Cleanup();
            }

            [Test]
            public void OnResolvedIsNotInvokedIfTokenIsAlreadyCanceled_void()
            {
                void Test()
                {
                    CancelationSource cancelationSource = CancelationSource.New();
                    cancelationSource.Cancel();

                    var deferred = Promise.NewDeferred();
                    var deferredInt = Promise.NewDeferred<int>();
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

                Test();
                TestHelper.Cleanup();
            }

            [Test]
            public void OnResolvedIsNotInvokedIfTokenIsAlreadyCanceled_T()
            {
                void Test()
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

                Test();
                TestHelper.Cleanup();
            }

            [Test]
            public void OnRejectedIsNotInvokedIfTokenIsCanceled_void()
            {
                void Test()
                {
                    CancelationSource cancelationSource = CancelationSource.New();

                    var deferred = Promise.NewDeferred();
                    var promise = deferred.Promise.Preserve();

                    TestHelper.AddCallbacksWithCancelation<float, object, string>(
                        promise,
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
                    promise.Forget();
                }

                Test();
                TestHelper.Cleanup();
            }

            [Test]
            public void OnRejectedIsNotInvokedIfTokenIsCanceled_T()
            {
                void Test()
                {
                    CancelationSource cancelationSource = CancelationSource.New();

                    var deferred = Promise.NewDeferred<int>();
                    var promise = deferred.Promise.Preserve();

                    TestHelper.AddCallbacksWithCancelation<int, float, object, string>(
                        promise,
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
                    promise.Forget();
                }

                Test();
                TestHelper.Cleanup();
            }

            [Test]
            public void OnRejectedIsNotInvokedIfTokenIsAlreadyCanceled_void()
            {
                Promise.Config.DebugCausalityTracer = Promise.TraceLevel.All;
                void Test()
                {
                    CancelationSource cancelationSource = CancelationSource.New();
                    cancelationSource.Cancel();

                    var deferred = Promise.NewDeferred();
                    var promise = deferred.Promise.Preserve();

                    TestHelper.AddCallbacksWithCancelation<float, object, string>(
                        promise,
                        onReject: _ => Assert.Fail("OnRejected was invoked."),
                        onUnknownRejection: () => Assert.Fail("OnRejected was invoked."),
                        onRejectCapture: _ => Assert.Fail("OnRejected was invoked."),
                        onUnknownRejectionCapture: _ => Assert.Fail("OnRejected was invoked."),
                        cancelationToken: cancelationSource.Token
                    );
                    
                    deferred.Reject("Reject");
                    Promise.Manager.HandleCompletes();

                    cancelationSource.Dispose();
                    promise.Forget();
                }

                Test();
                TestHelper.Cleanup();
            }

            [Test]
            public void OnRejectedIsNotInvokedIfTokenIsAlreadyCanceled_T()
            {
                void Test()
                {
                    CancelationSource cancelationSource = CancelationSource.New();
                    cancelationSource.Cancel();

                    var deferred = Promise.NewDeferred<int>();
                    var promise = deferred.Promise.Preserve();

                    TestHelper.AddCallbacksWithCancelation<int, float, object, string>(
                        promise,
                        onReject: _ => Assert.Fail("OnRejected was invoked."),
                        onUnknownRejection: () => Assert.Fail("OnRejected was invoked."),
                        onRejectCapture: _ => Assert.Fail("OnRejected was invoked."),
                        onUnknownRejectionCapture: _ => Assert.Fail("OnRejected was invoked."),
                        cancelationToken: cancelationSource.Token
                    );

                    deferred.Reject("Reject");
                    Promise.Manager.HandleCompletes();

                    cancelationSource.Dispose();
                    promise.Forget();
                }

                Test();
                TestHelper.Cleanup();
            }

            [Test]
            public void OnContinueIsNotInvokedIfTokenIsCanceled_void()
            {
                void Test()
                {
                    CancelationSource cancelationSource = CancelationSource.New();

                    var deferred = Promise.NewDeferred();
                    var promise = deferred.Promise.Preserve();

                    TestHelper.AddContinueCallbacksWithCancelation<float, string>(
                        promise,
                        onContinue: _ => Assert.Fail("OnContinue was invoked."),
                        onContinueCapture: (_, __) => Assert.Fail("OnContinue was invoked."),
                        cancelationToken: cancelationSource.Token
                    );

                    cancelationSource.Cancel();
                    deferred.Resolve();
                    Promise.Manager.HandleCompletes();

                    cancelationSource.Dispose();
                    promise.Forget();
                }

                Test();
                TestHelper.Cleanup();
            }

            [Test]
            public void OnContinueIsNotInvokedIfTokenIsCanceled_T()
            {
                void Test()
                {
                    CancelationSource cancelationSource = CancelationSource.New();

                    var deferred = Promise.NewDeferred<int>();
                    var promise = deferred.Promise.Preserve();

                    TestHelper.AddContinueCallbacksWithCancelation<int, float, string>(
                        promise,
                        onContinue: _ => Assert.Fail("OnContinue was invoked."),
                        onContinueCapture: (_, __) => Assert.Fail("OnContinue was invoked."),
                        cancelationToken: cancelationSource.Token
                    );

                    cancelationSource.Cancel();
                    deferred.Resolve(1);
                    Promise.Manager.HandleCompletes();

                    cancelationSource.Dispose();
                    promise.Forget();
                }

                Test();
                TestHelper.Cleanup();
            }

            [Test]
            public void OnContinueIsNotInvokedIfTokenIsAlreadyCanceled_void()
            {
                void Test()
                {
                    CancelationSource cancelationSource = CancelationSource.New();
                    cancelationSource.Cancel();

                    var deferred = Promise.NewDeferred();
                    var promise = deferred.Promise.Preserve();

                    TestHelper.AddContinueCallbacksWithCancelation<float, string>(
                        promise,
                        onContinue: _ => Assert.Fail("OnContinue was invoked."),
                        onContinueCapture: (_, __) => Assert.Fail("OnContinue was invoked."),
                        cancelationToken: cancelationSource.Token
                    );

                    deferred.Resolve();
                    Promise.Manager.HandleCompletes();

                    cancelationSource.Dispose();
                    promise.Forget();
                }

                Test();
                TestHelper.Cleanup();
            }

            [Test]
            public void OnContinueIsNotInvokedIfTokenIsAlreadyCanceled_T()
            {
                void Test()
                {
                    CancelationSource cancelationSource = CancelationSource.New();
                    cancelationSource.Cancel();

                    var deferred = Promise.NewDeferred<int>();
                    var promise = deferred.Promise.Preserve();

                    TestHelper.AddContinueCallbacksWithCancelation<int, float, string>(
                        promise,
                        onContinue: _ => Assert.Fail("OnContinue was invoked."),
                        onContinueCapture: (_, __) => Assert.Fail("OnContinue was invoked."),
                        cancelationToken: cancelationSource.Token
                    );

                    deferred.Resolve(1);
                    Promise.Manager.HandleCompletes();

                    cancelationSource.Dispose();
                    promise.Forget();
                }

                Test();
                TestHelper.Cleanup();
            }

            [Test]
            public void PromiseIsCanceledFromToken_void()
            {
                void Test()
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

                Test();
                TestHelper.Cleanup();
            }

            [Test]
            public void PromiseIsCanceledFromToken_T()
            {
                void Test()
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

                Test();
                TestHelper.Cleanup();
            }

            [Test]
            public void PromiseIsCanceledFromAlreadyCanceledToken_void()
            {
                void Test()
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

                Test();
                TestHelper.Cleanup();
            }

            [Test]
            public void PromiseIsCanceledFromAlreadyCanceledToken_T()
            {
                void Test()
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

                Test();
                TestHelper.Cleanup();
            }
        }
    }
}