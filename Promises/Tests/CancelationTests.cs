#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif
#if !PROTO_PROMISE_CANCEL_DISABLE
#define PROMISE_CANCEL
#else
#undef PROMISE_CANCEL
#endif

#if PROMISE_CANCEL
using System;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Proto.Promises.Tests
{
    public class CancelationTests
    {
        public class WhenPendingAPromise
        {
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

                deferred = Promise.NewDeferred();

                Assert.AreEqual(Promise.State.Pending, deferred.State);

                deferred.Cancel();

                Assert.AreEqual(Promise.State.Canceled, deferred.State);

                Assert.Throws<AggregateException>(Promise.Manager.HandleCompletes);

                // Clean up.
                GC.Collect();
                Promise.Manager.HandleCompletes();
                LogAssert.NoUnexpectedReceived();
            }
        }

        public class WhenFulfilledAPromise
        {
            [Test]
            public void MustNotTransitionToAnyOtherState()
            {
                var deferred = Promise.NewDeferred();
                var deferredInt = Promise.NewDeferred<int>();

                Assert.AreEqual(Promise.State.Pending, deferred.State);
                Assert.AreEqual(Promise.State.Pending, deferredInt.State);

                int resolved = 0;

                deferred.Promise
                    .Then(() => { ++resolved; })
                    .Catch(() => Assert.Fail("Promise was rejected when it was already resolved."))
                    .CatchCancelation(() => Assert.Fail("Promise was canceled when it was already resolved."));
                deferredInt.Promise
                    .Then(v => { ++resolved; })
                    .Catch(() => Assert.Fail("Promise was rejected when it was already resolved."))
                    .CatchCancelation(() => Assert.Fail("Promise was canceled when it was already resolved."));

                deferred.Resolve();
                deferredInt.Resolve(0);

                LogAssert.Expect(UnityEngine.LogType.Warning, "Deferred.Reject - Deferred is not in the pending state.");
                deferred.Reject("Fail Value");
                LogAssert.Expect(UnityEngine.LogType.Warning, "Deferred.Reject - Deferred is not in the pending state.");
                deferredInt.Reject("Fail Value");

                LogAssert.Expect(UnityEngine.LogType.Warning, "Deferred.Cancel - Deferred is not in the pending state.");
                deferred.Cancel();
                LogAssert.Expect(UnityEngine.LogType.Warning, "Deferred.Cancel - Deferred is not in the pending state.");
                deferredInt.Cancel();

                deferred.Promise.Cancel();
                deferredInt.Promise.Cancel();

                Assert.AreEqual(Promise.State.Resolved, deferred.State);
                Assert.AreEqual(Promise.State.Resolved, deferredInt.State);

                Assert.Throws<AggregateException>(Promise.Manager.HandleCompletes);

                Assert.AreEqual(2, resolved);

                // Clean up.
                GC.Collect();
                Promise.Manager.HandleCompletes();
                LogAssert.NoUnexpectedReceived();
            }
        }

        public class WhenRejectedAPromise
        {
            [Test]
            public void MustNotTransitionToAnyOtherState()
            {
                var deferred = Promise.NewDeferred();
                var deferredInt = Promise.NewDeferred<int>();

                Assert.AreEqual(Promise.State.Pending, deferred.State);
                Assert.AreEqual(Promise.State.Pending, deferredInt.State);

                int rejected = 0;

                deferred.Promise
                    .Then(() => Assert.Fail("Promise was resolved when it was already rejected."))
                    .Catch(() => { ++rejected; })
                    .CatchCancelation(() => Assert.Fail("Promise was canceled when it was already rejected."));
                deferredInt.Promise
                    .Then(() => Assert.Fail("Promise was resolved when it was already rejected."))
                    .Catch(() => { ++rejected; })
                    .CatchCancelation(() => Assert.Fail("Promise was canceled when it was already rejected."));

                deferred.Reject("Fail Value");
                deferredInt.Reject("Fail Value");

                LogAssert.Expect(UnityEngine.LogType.Warning, "Deferred.Resolve - Deferred is not in the pending state.");
                deferred.Resolve();
                LogAssert.Expect(UnityEngine.LogType.Warning, "Deferred.Resolve - Deferred is not in the pending state.");
                deferredInt.Resolve(0);

                LogAssert.Expect(UnityEngine.LogType.Warning, "Deferred.Cancel - Deferred is not in the pending state.");
                deferred.Cancel();
                LogAssert.Expect(UnityEngine.LogType.Warning, "Deferred.Cancel - Deferred is not in the pending state.");
                deferredInt.Cancel();

                deferred.Promise.Cancel();
                deferredInt.Promise.Cancel();

                Assert.AreEqual(Promise.State.Rejected, deferred.State);
                Assert.AreEqual(Promise.State.Rejected, deferredInt.State);

                Promise.Manager.HandleCompletes();

                Assert.AreEqual(2, rejected);

                // Clean up.
                GC.Collect();
                Promise.Manager.HandleCompletes();
                LogAssert.NoUnexpectedReceived();
            }
        }

        public class WhenCanceledAPromise
        {
            [Test]
            public void MustNotTransitionToAnyOtherState()
            {
                var deferred = Promise.NewDeferred();
                var deferredInt = Promise.NewDeferred<int>();

                Assert.AreEqual(Promise.State.Pending, deferred.State);
                Assert.AreEqual(Promise.State.Pending, deferredInt.State);

                int canceled = 0;

                deferred.Promise
                    .Then(() => Assert.Fail("Promise was resolved when it was already canceled."))
                    .Catch(() => Assert.Fail("Promise was rejected when it was already canceled."))
                    .CatchCancelation(() => { ++canceled; });
                deferredInt.Promise
                    .Then(v => Assert.Fail("Promise was resolved when it was already canceled."))
                    .Catch(() => Assert.Fail("Promise was rejected when it was already canceled."))
                    .CatchCancelation(() => { ++canceled; });

                deferred.Cancel("Cancel Value");
                deferredInt.Cancel("Cancel Value");

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
                GC.Collect();
                Promise.Manager.HandleCompletes();
                LogAssert.NoUnexpectedReceived();
            }

            [Test]
            public void MustHaveAReasonWhichMustNotChange_0()
            {
                var deferred = Promise.NewDeferred();

                Assert.AreEqual(Promise.State.Pending, deferred.State);

                deferred.Retain();
                string cancelation = null;
                string expected = "Cancel Value";

                Action rejectAssert = () => Assert.Fail("Promise was rejected when it should have been resolved.");
                Action resolveAssert = () => Assert.Fail("Promise was resolved when it should have been rejected.");

                TestHelper.AddCallbacks<object>(deferred.Promise, resolveAssert, failValue => rejectAssert());
                deferred.Promise.CatchCancelation<string>(cancelValue => { cancelation = cancelValue; Assert.AreEqual(expected, cancelValue); });
                deferred.Cancel(expected);
                Promise.Manager.HandleCompletes();

                Assert.AreEqual(expected, cancelation);

                TestHelper.AddCallbacks<object>(deferred.Promise, resolveAssert, failValue => rejectAssert());
                deferred.Promise.CatchCancelation<string>(cancelValue => { cancelation = cancelValue; Assert.AreEqual(expected, cancelValue); });

                LogAssert.Expect(UnityEngine.LogType.Warning, "Deferred.Cancel - Deferred is not in the pending state.");

                deferred.Cancel("Different Cancel Value");
                deferred.Release();
                Promise.Manager.HandleCompletes();

                Assert.AreEqual(expected, cancelation);

                // Clean up.
                GC.Collect();
                Promise.Manager.HandleCompletes();
                LogAssert.NoUnexpectedReceived();
            }

            [Test]
            public void MustHaveAReasonWhichMustNotChange_1()
            {
                var deferred = Promise.NewDeferred<int>();

                Assert.AreEqual(Promise.State.Pending, deferred.State);

                deferred.Retain();
                string cancelation = null;
                string expected = "Cancel Value";

                Action rejectAssert = () => Assert.Fail("Promise was rejected when it should have been resolved.");
                Action resolveAssert = () => Assert.Fail("Promise was resolved when it should have been rejected.");

                TestHelper.AddCallbacks<object>(deferred.Promise, resolveAssert, failValue => rejectAssert());
                deferred.Promise.CatchCancelation<string>(cancelValue => Assert.AreEqual(expected, cancelation = cancelValue));
                deferred.Cancel(expected);
                Promise.Manager.HandleCompletes();

                Assert.AreEqual(expected, cancelation);

                TestHelper.AddCallbacks<object>(deferred.Promise, resolveAssert, failValue => rejectAssert());
                deferred.Promise.CatchCancelation<string>(cancelValue => Assert.AreEqual(expected, cancelation = cancelValue));

                LogAssert.Expect(UnityEngine.LogType.Warning, "Deferred.Cancel - Deferred is not in the pending state.");

                deferred.Cancel("Different Cancel Value");
                deferred.Release();
                Promise.Manager.HandleCompletes();

                Assert.AreEqual(expected, cancelation);

                // Clean up.
                GC.Collect();
                Promise.Manager.HandleCompletes();
                LogAssert.NoUnexpectedReceived();
            }
        }

#if PROMISE_DEBUG
        [Test]
        public void IfOnCanceledIsNullThrow()
        {
            var deferred = Promise.NewDeferred();

            Assert.AreEqual(Promise.State.Pending, deferred.State);

            Assert.Throws<ArgumentNullException>(() =>
            {
                deferred.Promise.CatchCancelation(default(Action));
            });
            Assert.Throws<ArgumentNullException>(() =>
            {
                deferred.Promise.CatchCancelation(default(Action<int>));
            });

            deferred.Cancel();

            var deferredInt = Promise.NewDeferred<int>();
            Assert.AreEqual(Promise.State.Pending, deferredInt.State);

            Assert.Throws<ArgumentNullException>(() =>
            {
                deferredInt.Promise.CatchCancelation(default(Action));
            });
            Assert.Throws<ArgumentNullException>(() =>
            {
                deferredInt.Promise.CatchCancelation(default(Action<Exception>));
            });

            deferredInt.Cancel(0);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletes();
            LogAssert.NoUnexpectedReceived();
        }
#endif

        public class IfOnCanceledIsAFunction
        {
            [Test]
            public void ItMustBeCalledAfterPromiseIsCanceledWithPromisesReasonAsItsFirstArgument()
            {
                string cancelReason = "Cancel value";
                var canceled = false;
                var deferred = Promise.NewDeferred();
                Assert.AreEqual(Promise.State.Pending, deferred.State);
                deferred.Promise.CatchCancelation<string>(r =>
                {
                    Assert.AreEqual(cancelReason, r);
                    canceled = true;
                });
                deferred.Cancel(cancelReason);
                Promise.Manager.HandleCompletes();

                Assert.True(canceled);

                // Clean up.
                GC.Collect();
                Promise.Manager.HandleCompletes();
                LogAssert.NoUnexpectedReceived();
            }

            [Test]
            public void ItMustNotBeCalledBeforePromiseIsCanceled()
            {
                string cancelReason = "Cancel value";
                var canceled = false;
                var deferred = Promise.NewDeferred();
                Assert.AreEqual(Promise.State.Pending, deferred.State);
                deferred.Promise.CatchCancelation(() =>
                {
                    canceled = true;
                });
                deferred.Promise.CatchCancelation<string>(r =>
                {
                    Assert.AreEqual(cancelReason, r);
                    canceled = true;
                });
                Promise.Manager.HandleCompletes();

                Assert.False(canceled);

                deferred.Cancel(cancelReason);
                Promise.Manager.HandleCompletes();

                Assert.True(canceled);

                // Clean up.
                GC.Collect();
                Promise.Manager.HandleCompletes();
                LogAssert.NoUnexpectedReceived();
            }

            [Test]
            public void ItMustNotBeCalledMoreThanOnce()
            {
                var deferred = Promise.NewDeferred();
                Assert.AreEqual(Promise.State.Pending, deferred.State);
                deferred.Retain();
                var cancelCount = 0;
                deferred.Promise.CatchCancelation(() => ++cancelCount);
                deferred.Promise.CatchCancelation<string>(r => ++cancelCount);
                deferred.Cancel("Cancel value");
                Promise.Manager.HandleCompletes();
                LogAssert.Expect(UnityEngine.LogType.Warning, "Deferred.Cancel - Deferred is not in the pending state.");
                deferred.Cancel("Cancel value");
                deferred.Release();
                Promise.Manager.HandleCompletes();

                Assert.AreEqual(2, cancelCount);

                // Clean up.
                GC.Collect();
                Promise.Manager.HandleCompletes();
                LogAssert.NoUnexpectedReceived();
            }
        }

        [Test]
        public void OnCanceledMustNotBeCalledUntilTheExecutionContextStackContainsOnlyPlatformCode()
        {
            var deferred = Promise.NewDeferred();
            Assert.AreEqual(Promise.State.Pending, deferred.State);

            bool canceled = false;
            deferred.Promise.CatchCancelation(() => canceled = true);
            deferred.Cancel("Cancel value");
            Assert.False(canceled);


            Promise.Manager.HandleCompletes();
            Assert.True(canceled);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletes();
            LogAssert.NoUnexpectedReceived();
        }

        public class CatchCancelationMayBeCalledMultipleTimesOnTheSameIPotentialCancelation
        {
            [Test]
            public void IfWhenIPotentialCancelationIsCanceledAllRespectiveOnCanceledCallbacksMustExecuteInTheOrderOfTheirOriginatingCallsToCatchCancelation()
            {
                var deferred = Promise.NewDeferred();
                Assert.AreEqual(Promise.State.Pending, deferred.State);

                int counter = 0;

                deferred.Promise.CatchCancelation(() => Assert.AreEqual(0, counter++));
                deferred.Promise.CatchCancelation(() => Assert.AreEqual(1, counter++));
                deferred.Promise.CatchCancelation(() => Assert.AreEqual(2, counter++));

                deferred.Cancel();
                Promise.Manager.HandleCompletes();

                Assert.AreEqual(3, counter);

                // Clean up.
                GC.Collect();
                Promise.Manager.HandleCompletes();
                LogAssert.NoUnexpectedReceived();
            }
        }

        public class CatchCancelationMayReturnAnIPotentialCancelation
        {
            [Test]
            public void IfIPotentialCancelationIsCanceledAndItsReasonIsNotCompatibleWithOnCanceledItMustNotBeInvoked()
            {
                var deferred = Promise.NewDeferred();
                Assert.AreEqual(Promise.State.Pending, deferred.State);

                deferred.Promise.CatchCancelation((string v) => Assert.Fail("onCanceled was invoked with a string when the promise was canceled with an integer"));

                deferred.Cancel(100);
                Promise.Manager.HandleCompletes();

                // Clean up.
                GC.Collect();
                Promise.Manager.HandleCompletes();
                LogAssert.NoUnexpectedReceived();
            }

            [Test]
            public void IfIPotentialCancelationIsCanceledAndItsReasonIsNotCompatibleWithOnCanceledTheReturnedIPotentialCancelationMustBeCanceledWithTheSameReason()
            {
                var deferred = Promise.NewDeferred();
                Assert.AreEqual(Promise.State.Pending, deferred.State);

                int expected = 100;

                deferred.Promise
                    .CatchCancelation((string v) => Assert.Fail("onCanceled was invoked with a string when the promise was canceled with an integer"))
                    .CatchCancelation((int v) => Assert.AreEqual(expected, v));

                deferred.Cancel(expected);
                Promise.Manager.HandleCompletes();

                // Clean up.
                GC.Collect();
                Promise.Manager.HandleCompletes();
                LogAssert.NoUnexpectedReceived();
            }
        }

        [Test]
        public void IfPromiseIsCanceledOnResolveAndOnRejectedMustNotBeInvoked()
        {
            var deferred = Promise.NewDeferred();
            Assert.AreEqual(Promise.State.Pending, deferred.State);
            var deferredInt = Promise.NewDeferred<int>();
            Assert.AreEqual(Promise.State.Pending, deferredInt.State);

            Action resolveAssert = () => Assert.Fail("Promise was resolved when it should have been canceled.");
            Action rejectAssert = () => Assert.Fail("Promise was rejected when it should have been canceled.");

            TestHelper.AddCallbacks(deferred.Promise, resolveAssert, (object o) => rejectAssert(), rejectAssert);
            TestHelper.AddCallbacks(deferredInt.Promise, v => resolveAssert(), (object o) => rejectAssert(), rejectAssert);

            deferred.Cancel();
            deferredInt.Cancel();
            Promise.Manager.HandleCompletes();

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletes();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void CancelationsDoNotPropagateToRoot()
        {
            var deferred = Promise.NewDeferred();
            Assert.AreEqual(Promise.State.Pending, deferred.State);

            bool resolved = false;

            ICancelableAny cancelable = deferred.Promise
                .Then(() => resolved = true)
                .Then(() => Assert.Fail("Promise was resolved when it should have been canceled."));

            cancelable.Cancel();
            deferred.Resolve();
            Promise.Manager.HandleCompletes();

            Assert.AreEqual(true, resolved);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletes();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void CancelationsPropagateToBranches()
        {
            var deferred = Promise.NewDeferred();
            Assert.AreEqual(Promise.State.Pending, deferred.State);

            bool invoked = false;

            deferred.Promise
                .Then(() => { })
                .CatchCancelation(() => invoked = true);

            deferred.Cancel();
            Promise.Manager.HandleCompletes();

            Assert.AreEqual(true, invoked);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletes();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void IfPromiseIsCanceledAndAPreviousPromiseIsAlsoCanceledPromiseMustBeCanceledWithTheInitialCancelReason()
        {
            var deferred = Promise.NewDeferred();
            Assert.AreEqual(Promise.State.Pending, deferred.State);

            bool invoked = false;

            var cancelable = deferred.Promise
                .ThenDuplicate();
            cancelable
                .CatchCancelation<int>(val => Assert.Fail("Promise was canceled with an integer when it should have been canceled with a string."))
                .CatchCancelation<string>(val => invoked = true);

            cancelable.Cancel("Cancel val");
            deferred.Cancel(100);
            Promise.Manager.HandleCompletes();

            Assert.AreEqual(true, invoked);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletes();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void APromiseMayBeCanceledWhenItIsPending0()
        {
            string cancelValue = "Cancel";
            Action<Promise> validate = promise =>
            {
                promise
                    .Then(() => Assert.Fail("Promise was resolved when it should have been canceled."),
                        () => Assert.Fail("Promise was rejected when it should have been canceled."))
                    .CatchCancelation<string>(val => Assert.AreEqual(cancelValue, val));
                Promise.Manager.HandleCompletes();
            };

            Promise cancelPromise = null;
            Action cancel = () => cancelPromise.Cancel(cancelValue);

            cancelPromise = Promise.Resolved().Then(() => cancel());
            validate(cancelPromise);
            cancelPromise = Promise.Resolved().Then(() => { cancel(); return 0; });
            validate(cancelPromise);
            cancelPromise = Promise.Resolved().Then(() => { cancel(); return Promise.Resolved(); });
            validate(cancelPromise);
            cancelPromise = Promise.Resolved().Then(() => { cancel(); return Promise.Resolved(0); });
            validate(cancelPromise);

            cancelPromise = Promise.Resolved().Then(() => cancel(), () => { });
            validate(cancelPromise);
            cancelPromise = Promise.Resolved().Then(() => cancel(), (string failValue) => { });
            validate(cancelPromise);

            cancelPromise = Promise.Resolved().Then(() => { cancel(); return 0; }, () => 0);
            validate(cancelPromise);
            cancelPromise = Promise.Resolved().Then(() => { cancel(); return 0; }, (string failValue) => 0);
            validate(cancelPromise);

            cancelPromise = Promise.Resolved().Then(() => { cancel(); return Promise.Resolved(); }, () => Promise.Resolved());
            validate(cancelPromise);
            cancelPromise = Promise.Resolved().Then(() => { cancel(); return Promise.Resolved(); }, (string failValue) => Promise.Resolved());
            validate(cancelPromise);

            cancelPromise = Promise.Resolved().Then(() => { cancel(); return Promise.Resolved(0); }, () => Promise.Resolved(0));
            validate(cancelPromise);
            cancelPromise = Promise.Resolved().Then(() => { cancel(); return Promise.Resolved(0); }, (string failValue) => Promise.Resolved(0));
            validate(cancelPromise);

            cancelPromise = Promise.Rejected("Reject").Then(() => { }, () => cancel());
            validate(cancelPromise);
            cancelPromise = Promise.Rejected("Reject").Then(() => { }, (string failValue) => cancel());
            validate(cancelPromise);

            cancelPromise = Promise.Rejected("Reject").Then(() => 0, () => { cancel(); return 0; });
            validate(cancelPromise);
            cancelPromise = Promise.Rejected("Reject").Then(() => 0, (string failValue) => { cancel(); return 0; });
            validate(cancelPromise);

            cancelPromise = Promise.Rejected("Reject").Then(() => Promise.Resolved(), () => { cancel(); return Promise.Resolved(); });
            validate(cancelPromise);
            cancelPromise = Promise.Rejected("Reject").Then(() => Promise.Resolved(), (string failValue) => { cancel(); return Promise.Resolved(); });
            validate(cancelPromise);

            cancelPromise = Promise.Rejected("Reject").Then(() => Promise.Resolved(0), () => { cancel(); return Promise.Resolved(0); });
            validate(cancelPromise);
            cancelPromise = Promise.Rejected("Reject").Then(() => Promise.Resolved(0), (string failValue) => { cancel(); return Promise.Resolved(0); });
            validate(cancelPromise);

            cancelPromise = Promise.Rejected("Reject").Catch(() => cancel());
            validate(cancelPromise);
            cancelPromise = Promise.Rejected("Reject").Catch((string failValue) => cancel());
            validate(cancelPromise);

            cancelPromise = Promise.Rejected("Reject").Catch(() => { cancel(); return Promise.Resolved(); });
            validate(cancelPromise);
            cancelPromise = Promise.Rejected("Reject").Catch((string failValue) => { cancel(); return Promise.Resolved(); });
            validate(cancelPromise);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletes();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void APromiseMayBeCanceledWhenItIsPending1()
        {
            string cancelValue = "Cancel";
            Action<Promise> validate = promise =>
            {
                promise
                    .Then(() => Assert.Fail("Promise was resolved when it should have been canceled."),
                        () => Assert.Fail("Promise was rejected when it should have been canceled."))
                    .CatchCancelation<string>(val => Assert.AreEqual(cancelValue, val));
                Promise.Manager.HandleCompletes();
            };

            Promise cancelPromise = null;
            Action cancel = () => cancelPromise.Cancel(cancelValue);

            cancelPromise = Promise.Resolved(0).Then(_ => cancel());
            validate(cancelPromise);
            cancelPromise = Promise.Resolved(0).Then(_ => { cancel(); return 0; });
            validate(cancelPromise);
            cancelPromise = Promise.Resolved(0).Then(_ => { cancel(); return Promise.Resolved(); });
            validate(cancelPromise);
            cancelPromise = Promise.Resolved(0).Then(_ => { cancel(); return Promise.Resolved(0); });
            validate(cancelPromise);

            cancelPromise = Promise.Resolved(0).Then(_ => cancel(), () => { });
            validate(cancelPromise);
            cancelPromise = Promise.Resolved(0).Then(_ => cancel(), (string failValue) => { });
            validate(cancelPromise);

            cancelPromise = Promise.Resolved(0).Then(_ => { cancel(); return 0; }, () => 0);
            validate(cancelPromise);
            cancelPromise = Promise.Resolved(0).Then(_ => { cancel(); return 0; }, (string failValue) => 0);
            validate(cancelPromise);

            cancelPromise = Promise.Resolved(0).Then(_ => { cancel(); return Promise.Resolved(); }, () => Promise.Resolved());
            validate(cancelPromise);
            cancelPromise = Promise.Resolved(0).Then(_ => { cancel(); return Promise.Resolved(); }, (string failValue) => Promise.Resolved());
            validate(cancelPromise);

            cancelPromise = Promise.Resolved(0).Then(_ => { cancel(); return Promise.Resolved(0); }, () => Promise.Resolved(0));
            validate(cancelPromise);
            cancelPromise = Promise.Resolved(0).Then(_ => { cancel(); return Promise.Resolved(0); }, (string failValue) => Promise.Resolved(0));
            validate(cancelPromise);

            cancelPromise = Promise.Rejected<int, string>("Reject").Then(_ => { }, () => cancel());
            validate(cancelPromise);
            cancelPromise = Promise.Rejected<int, string>("Reject").Then(_ => { }, (string failValue) => cancel());
            validate(cancelPromise);

            cancelPromise = Promise.Rejected<int, string>("Reject").Then(_ => 0, () => { cancel(); return 0; });
            validate(cancelPromise);
            cancelPromise = Promise.Rejected<int, string>("Reject").Then(_ => 0, (string failValue) => { cancel(); return 0; });
            validate(cancelPromise);

            cancelPromise = Promise.Rejected<int, string>("Reject").Then(_ => Promise.Resolved(), () => { cancel(); return Promise.Resolved(); });
            validate(cancelPromise);
            cancelPromise = Promise.Rejected<int, string>("Reject").Then(_ => Promise.Resolved(), (string failValue) => { cancel(); return Promise.Resolved(); });
            validate(cancelPromise);

            cancelPromise = Promise.Rejected<int, string>("Reject").Then(_ => Promise.Resolved(0), () => { cancel(); return Promise.Resolved(0); });
            validate(cancelPromise);
            cancelPromise = Promise.Rejected<int, string>("Reject").Then(_ => Promise.Resolved(0), (string failValue) => { cancel(); return Promise.Resolved(0); });
            validate(cancelPromise);

            cancelPromise = Promise.Rejected<int, string>("Reject").Catch(() => cancel());
            validate(cancelPromise);
            cancelPromise = Promise.Rejected<int, string>("Reject").Catch((string failValue) => cancel());
            validate(cancelPromise);

            cancelPromise = Promise.Rejected<int, string>("Reject").Catch(() => { cancel(); return Promise.Resolved(); });
            validate(cancelPromise);
            cancelPromise = Promise.Rejected<int, string>("Reject").Catch((string failValue) => { cancel(); return Promise.Resolved(); });
            validate(cancelPromise);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletes();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void APromiseMayBeCanceledWhenItIsPending2()
        {
            var deferred = Promise.NewDeferred();
            Assert.AreEqual(Promise.State.Pending, deferred.State);
            var deferredInt = Promise.NewDeferred<int>();
            Assert.AreEqual(Promise.State.Pending, deferred.State);

            string cancelValue = "Cancel";
            Action<Promise> validate = promise =>
            {
                promise
                    .Then(() => Assert.Fail("Promise was resolved when it should have been canceled."),
                        () => Assert.Fail("Promise was rejected when it should have been canceled."))
                    .CatchCancelation<string>(val => Assert.AreEqual(cancelValue, val));
                Promise.Manager.HandleCompletes();
            };

            deferred.Promise.Cancel(cancelValue);
            Assert.AreEqual(Promise.State.Pending, deferred.State);
            deferred.Resolve();

            validate(deferred.Promise);

            deferredInt.Promise.Cancel(cancelValue);
            Assert.AreEqual(Promise.State.Pending, deferredInt.State);
            deferredInt.Resolve(100);

            validate(deferredInt.Promise);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletes();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void APromiseMayBeCanceledWhenItIsPending3()
        {
            var deferred = Promise.NewDeferred();
            Assert.AreEqual(Promise.State.Pending, deferred.State);
            var deferredInt = Promise.NewDeferred<int>();
            Assert.AreEqual(Promise.State.Pending, deferred.State);

            string cancelValue = "Cancel";
            Action<Promise> validate = promise =>
            {
                promise
                    .Then(() => Assert.Fail("Promise was resolved when it should have been canceled."),
                        () => Assert.Fail("Promise was rejected when it should have been canceled."))
                    .CatchCancelation<string>(val => Assert.AreEqual(cancelValue, val));
                // Rejecting a deferred whose promise is not pending adds that rejection to the unhandled rejections stack.
                Assert.Throws<AggregateException>(Promise.Manager.HandleCompletes);
            };

            deferred.Promise.Cancel(cancelValue);
            Assert.AreEqual(Promise.State.Pending, deferred.State);
            deferred.Reject("Reject");

            validate(deferred.Promise);

            deferredInt.Promise.Cancel(cancelValue);
            Assert.AreEqual(Promise.State.Pending, deferredInt.State);
            deferredInt.Reject("Reject");

            validate(deferredInt.Promise);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletes();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void APromiseMayBeCanceledWhenItIsPending4()
        {
            var deferred = Promise.NewDeferred();
            Assert.AreEqual(Promise.State.Pending, deferred.State);
            var deferredInt = Promise.NewDeferred<int>();
            Assert.AreEqual(Promise.State.Pending, deferred.State);

            string cancelValue = "Cancel";
            Action<Promise> validate = promise =>
            {
                promise
                    .Then(() => Assert.Fail("Promise was resolved when it should have been canceled."),
                        () => Assert.Fail("Promise was rejected when it should have been canceled."))
                    .CatchCancelation<string>(val => Assert.AreEqual(cancelValue, val));
                Promise.Manager.HandleCompletes();
            };

            deferred.Promise.Cancel(cancelValue);
            Assert.AreEqual(Promise.State.Pending, deferred.State);
            deferred.Cancel("Different Cancel");

            validate(deferred.Promise);

            deferredInt.Promise.Cancel(cancelValue);
            Assert.AreEqual(Promise.State.Pending, deferredInt.State);
            deferredInt.Cancel("Different Cancel");

            validate(deferredInt.Promise);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletes();
            LogAssert.NoUnexpectedReceived();
        }
    }
}
#endif