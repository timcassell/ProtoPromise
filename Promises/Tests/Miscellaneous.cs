#if !PROTO_PROMISE_CANCEL_DISABLE
#define PROMISE_CANCEL
#else
#undef PROMISE_CANCEL
#endif

using System;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Proto.Promises.Tests
{
    public class Miscellaneous
    {
        [Test]
        public void AccessingCancelExceptionOrRejectExceptionInNormalCodeThrows()
        {
#if PROMISE_CANCEL
            Assert.Throws<InvalidOperationException>(() => Promise.CancelException());
            Assert.Throws<InvalidOperationException>(() => Promise.CancelException("Cancel!"));
#endif
            Assert.Throws<InvalidOperationException>(() => Promise.RejectException("Reject!"));

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void AccessingCancelExceptionOrRejectExceptionInOnResolvedDoesNotThrow()
        {
            var promise1 = Promise.Resolved();
            var promise2 = Promise.Resolved(100);

            TestHelper.AddCallbacks<int, object, string>(promise1,
                onResolve: () =>
                {
#if PROMISE_CANCEL
                    Promise.CancelException();
                    Promise.CancelException("Cancel!");
#endif
                    Promise.RejectException("Reject!");
                });
            TestHelper.AddCallbacks<int, bool, object, string>(promise2,
                onResolve: v =>
                {
#if PROMISE_CANCEL
                    Promise.CancelException();
                    Promise.CancelException("Cancel!");
#endif
                    Promise.RejectException("Reject!");
                });

            Promise.Manager.HandleCompletes();

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void AccessingCancelExceptionOrRejectExceptionInOnRejectedDoesNotThrow()
        {
            var promise1 = Promise.Rejected("Reject!");
            var promise2 = Promise.Rejected<int, string>("Reject!");

            TestHelper.AddCallbacks<int, string, string>(promise1,
                onReject: (string rej) =>
                {
#if PROMISE_CANCEL
                    Promise.CancelException();
                    Promise.CancelException("Cancel!");
#endif
                    Promise.RejectException("Reject!");
                },
                onUnknownRejection: () =>
                {
#if PROMISE_CANCEL
                    Promise.CancelException();
                    Promise.CancelException("Cancel!");
#endif
                    Promise.RejectException("Reject!");
                });
            TestHelper.AddCallbacks<int, bool, string, string>(promise2,
                onReject: (string rej) =>
                {
#if PROMISE_CANCEL
                    Promise.CancelException();
                    Promise.CancelException("Cancel!");
#endif
                    Promise.RejectException("Reject!");
                },
                onUnknownRejection: () =>
                {
#if PROMISE_CANCEL
                    Promise.CancelException();
                    Promise.CancelException("Cancel!");
#endif
                    Promise.RejectException("Reject!");
                });

            Promise.Manager.HandleCompletes();

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void ThrowingRejectExceptionInOnResolvedRejectsThePromiseWithTheGivenValue()
        {
            var promise1 = Promise.Resolved();
            var promise2 = Promise.Resolved(100);

            int voidRejections = 0;
            int intRejections = 0;
            string expected = "Reject!";

            Action<Promise> callback = p =>
            {
                p.Catch((string e) =>
                {
                    Assert.AreEqual(expected, e);
                    ++voidRejections;
                });
                throw Promise.RejectException(expected);
            };
            Action<Promise> callbackT = p =>
            {
                p.Catch((string e) =>
                {
                    Assert.AreEqual(expected, e);
                    ++intRejections;
                });
                throw Promise.RejectException(expected);
            };

            TestHelper.AddResolveCallbacks<int, string>(promise1,
                promiseToVoid: callback,
                promiseToConvert: p => { callback(p); return 0; },
                promiseToPromise: p => { callback(p); return Promise.Resolved(); },
                promiseToPromiseConvert: p => { callback(p); return Promise.Resolved(0); }
            );
            TestHelper.AddCallbacks<int, object, string>(promise1,
                promiseToVoid: callback,
                promiseToConvert: p => { callback(p); return 0; },
                promiseToPromise: p => { callback(p); return Promise.Resolved(); },
                promiseToPromiseConvert: p => { callback(p); return Promise.Resolved(0); }
            );
            TestHelper.AddCompleteCallbacks<int, string>(promise1,
                promiseToVoid: callback,
                promiseToConvert: p => { callback(p); return 0; },
                promiseToPromise: p => { callback(p); return Promise.Resolved(); },
                promiseToPromiseConvert: p => { callback(p); return Promise.Resolved(0); }
            );

            TestHelper.AddResolveCallbacks<int, string>(promise2,
                promiseToVoid: callbackT,
                promiseToConvert: p => { callbackT(p); return 0; },
                promiseToPromise: p => { callbackT(p); return Promise.Resolved(); },
                promiseToPromiseConvert: p => { callbackT(p); return Promise.Resolved(0); }
            );
            TestHelper.AddCallbacks<int, bool, object, string>(promise2,
                promiseToVoid: callbackT,
                promiseToConvert: p => { callbackT(p); return false; },
                promiseToPromise: p => { callbackT(p); return Promise.Resolved(); },
                promiseToPromiseConvert: p => { callbackT(p); return Promise.Resolved(false); }
            );
            TestHelper.AddCompleteCallbacks<int, string>(promise2,
                promiseToVoid: callbackT,
                promiseToConvert: p => { callbackT(p); return 0; },
                promiseToPromise: p => { callbackT(p); return Promise.Resolved(); },
                promiseToPromiseConvert: p => { callbackT(p); return Promise.Resolved(0); }
            );

            Promise.Manager.HandleCompletes();

            Assert.AreEqual(TestHelper.resolveVoidVoidCallbacks + TestHelper.resolveVoidConvertCallbacks +
                TestHelper.resolveVoidPromiseVoidCallbacks + TestHelper.resolveVoidPromiseConvertCallbacks +
                TestHelper.completeVoidCallbacks + TestHelper.completeConvertCallbacks +
                TestHelper.completePromiseVoidCallbacks + TestHelper.completePromiseConvertCallbacks,
                voidRejections);
            Assert.AreEqual(TestHelper.resolveTVoidCallbacks + TestHelper.resolveTConvertCallbacks +
                TestHelper.resolveTPromiseVoidCallbacks + TestHelper.resolveTPromiseConvertCallbacks +
                TestHelper.completeVoidCallbacks + TestHelper.completeConvertCallbacks +
                TestHelper.completePromiseVoidCallbacks + TestHelper.completePromiseConvertCallbacks,
                intRejections);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void ThrowingRejectExceptionInOnRejectedRejectsThePromiseWithTheGivenValue()
        {
            var promise1 = Promise.Rejected("A different reject value.");
            var promise2 = Promise.Rejected<int, string>("A different reject value.");

            int voidRejections = 0;
            int intRejections = 0;
            string expected = "Reject!";

            Action<Promise> callback = p =>
            {
                p.Catch((string e) =>
                {
                    Assert.AreEqual(expected, e);
                    ++voidRejections;
                });
                throw Promise.RejectException(expected);
            };
            Action<Promise> callbackT = p =>
            {
                p.Catch((string e) =>
                {
                    Assert.AreEqual(expected, e);
                    ++intRejections;
                });
                throw Promise.RejectException(expected);
            };

            TestHelper.AddCallbacks<int, object, string>(promise1,
                promiseToVoid: callback,
                promiseToConvert: p => { callback(p); return 0; },
                promiseToPromise: p => { callback(p); return Promise.Resolved(); },
                promiseToPromiseConvert: p => { callback(p); return Promise.Resolved(0); }
            );
            TestHelper.AddCompleteCallbacks<int, string>(promise1,
                promiseToVoid: callback,
                promiseToConvert: p => { callback(p); return 0; },
                promiseToPromise: p => { callback(p); return Promise.Resolved(); },
                promiseToPromiseConvert: p => { callback(p); return Promise.Resolved(0); }
            );

            TestHelper.AddCallbacks<int, bool, object, string>(promise2,
                promiseToVoid: callbackT,
                promiseToConvert: p => { callbackT(p); return false; },
                promiseToPromise: p => { callbackT(p); return Promise.Resolved(); },
                promiseToPromiseConvert: p => { callbackT(p); return Promise.Resolved(false); }
            );
            TestHelper.AddCompleteCallbacks<int, string>(promise2,
                promiseToVoid: callbackT,
                promiseToConvert: p => { callbackT(p); return 0; },
                promiseToPromise: p => { callbackT(p); return Promise.Resolved(); },
                promiseToPromiseConvert: p => { callbackT(p); return Promise.Resolved(0); }
            );

            Promise.Manager.HandleCompletes();

            Assert.AreEqual(TestHelper.rejectVoidVoidCallbacks + TestHelper.rejectVoidConvertCallbacks +
                TestHelper.rejectVoidPromiseVoidCallbacks + TestHelper.rejectVoidPromiseConvertCallbacks +
                TestHelper.completeVoidCallbacks + TestHelper.completeConvertCallbacks +
                TestHelper.completePromiseVoidCallbacks + TestHelper.completePromiseConvertCallbacks,
                voidRejections);
            Assert.AreEqual(TestHelper.rejectTVoidCallbacks + TestHelper.rejectTConvertCallbacks +
                TestHelper.rejectTPromiseVoidCallbacks + TestHelper.rejectTPromiseConvertCallbacks +
                TestHelper.completeVoidCallbacks + TestHelper.completeConvertCallbacks +
                TestHelper.completePromiseVoidCallbacks + TestHelper.completePromiseConvertCallbacks,
                intRejections);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

#if PROMISE_CANCEL
        [Test]
        public void ThrowingCancelExceptionInOnResolvedCancelsThePromiseWithTheGivenValue()
        {
            var promise1 = Promise.Resolved();
            var promise2 = Promise.Resolved(100);

            int voidCancelations = 0;
            int intCancelations = 0;
            string expected = "Cancel!";

            Action<Promise> callback = p =>
            {
                p.CatchCancelation((string e) =>
                {
                    Assert.AreEqual(expected, e);
                    ++voidCancelations;
                });
                throw Promise.CancelException(expected);
            };
            Action<Promise> callbackT = p =>
            {
                p.CatchCancelation((string e) =>
                {
                    Assert.AreEqual(expected, e);
                    ++intCancelations;
                });
                throw Promise.CancelException(expected);
            };

            TestHelper.AddResolveCallbacks<int, string>(promise1,
                promiseToVoid: callback,
                promiseToConvert: p => { callback(p); return 0; },
                promiseToPromise: p => { callback(p); return Promise.Resolved(); },
                promiseToPromiseConvert: p => { callback(p); return Promise.Resolved(0); }
            );
            TestHelper.AddCallbacks<int, object, string>(promise1,
                promiseToVoid: callback,
                promiseToConvert: p => { callback(p); return 0; },
                promiseToPromise: p => { callback(p); return Promise.Resolved(); },
                promiseToPromiseConvert: p => { callback(p); return Promise.Resolved(0); }
            );
            TestHelper.AddCompleteCallbacks<int, string>(promise1,
                promiseToVoid: callback,
                promiseToConvert: p => { callback(p); return 0; },
                promiseToPromise: p => { callback(p); return Promise.Resolved(); },
                promiseToPromiseConvert: p => { callback(p); return Promise.Resolved(0); }
            );

            TestHelper.AddResolveCallbacks<int, string>(promise2,
                promiseToVoid: callbackT,
                promiseToConvert: p => { callbackT(p); return 0; },
                promiseToPromise: p => { callbackT(p); return Promise.Resolved(); },
                promiseToPromiseConvert: p => { callbackT(p); return Promise.Resolved(0); }
            );
            TestHelper.AddCallbacks<int, bool, object, string>(promise2,
                promiseToVoid: callbackT,
                promiseToConvert: p => { callbackT(p); return false; },
                promiseToPromise: p => { callbackT(p); return Promise.Resolved(); },
                promiseToPromiseConvert: p => { callbackT(p); return Promise.Resolved(false); }
            );
            TestHelper.AddCompleteCallbacks<int, string>(promise2,
                promiseToVoid: callbackT,
                promiseToConvert: p => { callbackT(p); return 0; },
                promiseToPromise: p => { callbackT(p); return Promise.Resolved(); },
                promiseToPromiseConvert: p => { callbackT(p); return Promise.Resolved(0); }
            );

            Promise.Manager.HandleCompletes();

            Assert.AreEqual(TestHelper.resolveVoidVoidCallbacks + TestHelper.resolveVoidConvertCallbacks +
                TestHelper.resolveVoidPromiseVoidCallbacks + TestHelper.resolveVoidPromiseConvertCallbacks +
                TestHelper.completeVoidCallbacks + TestHelper.completeConvertCallbacks +
                TestHelper.completePromiseVoidCallbacks + TestHelper.completePromiseConvertCallbacks,
                voidCancelations);
            Assert.AreEqual(TestHelper.resolveTVoidCallbacks + TestHelper.resolveTConvertCallbacks +
                TestHelper.resolveTPromiseVoidCallbacks + TestHelper.resolveTPromiseConvertCallbacks +
                TestHelper.completeVoidCallbacks + TestHelper.completeConvertCallbacks +
                TestHelper.completePromiseVoidCallbacks + TestHelper.completePromiseConvertCallbacks,
                intCancelations);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void ThrowingCancelExceptionInOnRejectedCancelsThePromiseWithTheGivenValue()
        {
            var promise1 = Promise.Rejected("Rejected");
            var promise2 = Promise.Rejected<int, string>("Rejected");

            int voidCancelations = 0;
            int intCancelations = 0;
            string expected = "Cancel!";

            Action<Promise> callback = p =>
            {
                p.CatchCancelation((string e) =>
                {
                    Assert.AreEqual(expected, e);
                    ++voidCancelations;
                });
                throw Promise.CancelException(expected);
            };
            Action<Promise> callbackT = p =>
            {
                p.CatchCancelation((string e) =>
                {
                    Assert.AreEqual(expected, e);
                    ++intCancelations;
                });
                throw Promise.CancelException(expected);
            };

            TestHelper.AddCallbacks<int, object, string>(promise1,
                promiseToVoid: callback,
                promiseToConvert: p => { callback(p); return 0; },
                promiseToPromise: p => { callback(p); return Promise.Resolved(); },
                promiseToPromiseConvert: p => { callback(p); return Promise.Resolved(0); }
            );
            TestHelper.AddCompleteCallbacks<int, string>(promise1,
                promiseToVoid: callback,
                promiseToConvert: p => { callback(p); return 0; },
                promiseToPromise: p => { callback(p); return Promise.Resolved(); },
                promiseToPromiseConvert: p => { callback(p); return Promise.Resolved(0); }
            );

            TestHelper.AddCallbacks<int, bool, object, string>(promise2,
                promiseToVoid: callbackT,
                promiseToConvert: p => { callbackT(p); return false; },
                promiseToPromise: p => { callbackT(p); return Promise.Resolved(); },
                promiseToPromiseConvert: p => { callbackT(p); return Promise.Resolved(false); }
            );
            TestHelper.AddCompleteCallbacks<int, string>(promise2,
                promiseToVoid: callbackT,
                promiseToConvert: p => { callbackT(p); return 0; },
                promiseToPromise: p => { callbackT(p); return Promise.Resolved(); },
                promiseToPromiseConvert: p => { callbackT(p); return Promise.Resolved(0); }
            );

            Promise.Manager.HandleCompletes();

            Assert.AreEqual(TestHelper.rejectVoidVoidCallbacks + TestHelper.rejectVoidConvertCallbacks +
                TestHelper.rejectVoidPromiseVoidCallbacks + TestHelper.rejectVoidPromiseConvertCallbacks +
                TestHelper.completeVoidCallbacks + TestHelper.completeConvertCallbacks +
                TestHelper.completePromiseVoidCallbacks + TestHelper.completePromiseConvertCallbacks,
                voidCancelations);
            Assert.AreEqual(TestHelper.rejectTVoidCallbacks + TestHelper.rejectTConvertCallbacks +
                TestHelper.rejectTPromiseVoidCallbacks + TestHelper.rejectTPromiseConvertCallbacks +
                TestHelper.completeVoidCallbacks + TestHelper.completeConvertCallbacks +
                TestHelper.completePromiseVoidCallbacks + TestHelper.completePromiseConvertCallbacks,
                intCancelations);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }
#endif

        [Test]
        public void AccessingRethrowInNormalCodeThrows()
        {
            Assert.Throws<InvalidOperationException>(() => { var _ = Promise.Rethrow; });

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void AccessingRethrowInOnResolvedThrows()
        {
            var promise1 = Promise.Resolved();
            var promise2 = Promise.Resolved(100);

            int voidErrors = 0;
            int intErrors = 0;

            Action<Promise> callback = p =>
            {
                p.Catch((object e) =>
                {
                    Assert.IsInstanceOf(typeof(InvalidOperationException), e);
                    ++voidErrors;
                });
                var _ = Promise.Rethrow;
            };
            Action<Promise> callbackT = p =>
            {
                p.Catch((object e) =>
                {
                    Assert.IsInstanceOf(typeof(InvalidOperationException), e);
                    ++intErrors;
                });
                var _ = Promise.Rethrow;
            };

            TestHelper.AddResolveCallbacks<int, string>(promise1,
                promiseToVoid: callback,
                promiseToConvert: p => { callback(p); return 0; },
                promiseToPromise: p => { callback(p); return Promise.Resolved(); },
                promiseToPromiseConvert: p => { callback(p); return Promise.Resolved(0); }
            );
            TestHelper.AddCallbacks<int, object, string>(promise1,
                promiseToVoid: callback,
                promiseToConvert: p => { callback(p); return 0; },
                promiseToPromise: p => { callback(p); return Promise.Resolved(); },
                promiseToPromiseConvert: p => { callback(p); return Promise.Resolved(0); }
            );
            TestHelper.AddCompleteCallbacks<int, string>(promise1,
                promiseToVoid: callback,
                promiseToConvert: p => { callback(p); return 0; },
                promiseToPromise: p => { callback(p); return Promise.Resolved(); },
                promiseToPromiseConvert: p => { callback(p); return Promise.Resolved(0); }
            );

            TestHelper.AddResolveCallbacks<int, string>(promise2,
                promiseToVoid: callbackT,
                promiseToConvert: p => { callbackT(p); return 0; },
                promiseToPromise: p => { callbackT(p); return Promise.Resolved(); },
                promiseToPromiseConvert: p => { callbackT(p); return Promise.Resolved(0); }
            );
            TestHelper.AddCallbacks<int, bool, object, string>(promise2,
                promiseToVoid: callbackT,
                promiseToConvert: p => { callbackT(p); return false; },
                promiseToPromise: p => { callbackT(p); return Promise.Resolved(); },
                promiseToPromiseConvert: p => { callbackT(p); return Promise.Resolved(false); }
            );
            TestHelper.AddCompleteCallbacks<int, string>(promise2,
                promiseToVoid: callbackT,
                promiseToConvert: p => { callbackT(p); return 0; },
                promiseToPromise: p => { callbackT(p); return Promise.Resolved(); },
                promiseToPromiseConvert: p => { callbackT(p); return Promise.Resolved(0); }
            );

            Promise.Manager.HandleCompletes();

            Assert.AreEqual(TestHelper.resolveVoidVoidCallbacks + TestHelper.resolveVoidConvertCallbacks +
                TestHelper.resolveVoidPromiseVoidCallbacks + TestHelper.resolveVoidPromiseConvertCallbacks +
                TestHelper.completeVoidCallbacks + TestHelper.completeConvertCallbacks +
                TestHelper.completePromiseVoidCallbacks + TestHelper.completePromiseConvertCallbacks,
                voidErrors);
            Assert.AreEqual(TestHelper.resolveTVoidCallbacks + TestHelper.resolveTConvertCallbacks +
                TestHelper.resolveTPromiseVoidCallbacks + TestHelper.resolveTPromiseConvertCallbacks +
                TestHelper.completeVoidCallbacks + TestHelper.completeConvertCallbacks +
                TestHelper.completePromiseVoidCallbacks + TestHelper.completePromiseConvertCallbacks,
                intErrors);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void ThrowingRethrowInOnRejectedRejectsThePromiseWithTheSameReason()
        {
            string expected = "Reject!";

            var promise1 = Promise.Rejected(expected);
            var promise2 = Promise.Rejected<int, string>(expected);

            int voidRejections = 0;
            int intRejections = 0;

            Action<Promise> callback = p =>
            {
                p.Catch((string e) =>
                {
                    Assert.AreEqual(expected, e);
                    ++voidRejections;
                });
                throw Promise.Rethrow;
            };
            Action<Promise> callbackT = p =>
            {
                p.Catch((string e) =>
                {
                    Assert.AreEqual(expected, e);
                    ++intRejections;
                });
                throw Promise.Rethrow;
            };

            TestHelper.AddCallbacks<int, object, string>(promise1,
                promiseToVoid: callback,
                promiseToConvert: p => { callback(p); return 0; },
                promiseToPromise: p => { callback(p); return Promise.Resolved(); },
                promiseToPromiseConvert: p => { callback(p); return Promise.Resolved(0); }
            );
            TestHelper.AddCompleteCallbacks<int, string>(promise1,
                promiseToVoid: callback,
                promiseToConvert: p => { callback(p); return 0; },
                promiseToPromise: p => { callback(p); return Promise.Resolved(); },
                promiseToPromiseConvert: p => { callback(p); return Promise.Resolved(0); }
            );

            TestHelper.AddCallbacks<int, bool, object, string>(promise2,
                promiseToVoid: callbackT,
                promiseToConvert: p => { callbackT(p); return false; },
                promiseToPromise: p => { callbackT(p); return Promise.Resolved(); },
                promiseToPromiseConvert: p => { callbackT(p); return Promise.Resolved(false); }
            );
            TestHelper.AddCompleteCallbacks<int, string>(promise2,
                promiseToVoid: callbackT,
                promiseToConvert: p => { callbackT(p); return 0; },
                promiseToPromise: p => { callbackT(p); return Promise.Resolved(); },
                promiseToPromiseConvert: p => { callbackT(p); return Promise.Resolved(0); }
            );

            Promise.Manager.HandleCompletes();

            Assert.AreEqual(TestHelper.rejectVoidVoidCallbacks + TestHelper.rejectVoidConvertCallbacks +
                TestHelper.rejectVoidPromiseVoidCallbacks + TestHelper.rejectVoidPromiseConvertCallbacks +
                TestHelper.completeVoidCallbacks + TestHelper.completeConvertCallbacks +
                TestHelper.completePromiseVoidCallbacks + TestHelper.completePromiseConvertCallbacks,
                voidRejections);
            Assert.AreEqual(TestHelper.rejectTVoidCallbacks + TestHelper.rejectTConvertCallbacks +
                TestHelper.rejectTPromiseVoidCallbacks + TestHelper.rejectTPromiseConvertCallbacks +
                TestHelper.completeVoidCallbacks + TestHelper.completeConvertCallbacks +
                TestHelper.completePromiseVoidCallbacks + TestHelper.completePromiseConvertCallbacks,
                intRejections);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void PromiseResolvedIsResolved()
        {
            var promise = Promise.Resolved();
            bool resolved = false;

            promise
                .Then(() => resolved = true)
#if PROMISE_CANCEL
                .CatchCancelation(() => Assert.Fail("Promise was canceled when it should have been resolved"))
#endif
                ;

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(true, resolved);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void PromiseResolvedIsResolvedWithTheGivenValue()
        {
            int expected = 100;
            var promise = Promise.Resolved(expected);
            bool resolved = false;

            promise
                .Then(val =>
                {
                    Assert.AreEqual(expected, val);
                    resolved = true;
                })
#if PROMISE_CANCEL
                .CatchCancelation(() => Assert.Fail("Promise was canceled when it should have been resolved"))
#endif
                ;

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(true, resolved);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void PromiseRejectedIsRejectedWithTheGivenReason0()
        {
            string expected = "Reject";
            var promise = Promise.Rejected(expected);
            bool rejected = false;

            promise
                .Then(() => Assert.Fail("Promise was resolved when it should have been rejected"))
                .Catch((object reason) =>
                {
                    Assert.AreEqual(expected, reason);
                    rejected = true;
                })
#if PROMISE_CANCEL
                .CatchCancelation(() => Assert.Fail("Promise was canceled when it should have been rejected"))
#endif
                ;

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(true, rejected);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void PromiseRejectedIsRejectedWithTheGivenReason1()
        {
            string expected = "Reject";
            var promise = Promise.Rejected<int, string>(expected);
            bool rejected = false;

            promise
                .Then(() => Assert.Fail("Promise was resolved when it should have been rejected"))
                .Catch((object reason) =>
                {
                    Assert.AreEqual(expected, reason);
                    rejected = true;
                })
#if PROMISE_CANCEL
                .CatchCancelation(() => Assert.Fail("Promise was canceled when it should have been rejected"))
#endif
                ;

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(true, rejected);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

#if PROMISE_CANCEL
        [Test]
        public void PromiseCanceledIsCanceledWithTheGivenReason0()
        {
            string expected = "Cancel";
            var promise = Promise.Canceled(expected);
            bool canceled = false;

            promise
                .Then(() => Assert.Fail("Promise was resolved when it should have been canceled"))
                .Catch(() => Assert.Fail("Promise was rejected when it should have been canceled"))
                .CatchCancelation((object reason) =>
                {
                    Assert.AreEqual(expected, reason);
                    canceled = true;
                });

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(true, canceled);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void PromiseCanceledIsCanceledWithTheGivenReason1()
        {
            string expected = "Cancel";
            var promise = Promise.Canceled<int, string>(expected);
            bool canceled = false;

            promise
                .Then(() => Assert.Fail("Promise was resolved when it should have been canceled"))
                .Catch(() => Assert.Fail("Promise was rejected when it should have been canceled"))
                .CatchCancelation((object reason) =>
                {
                    Assert.AreEqual(expected, reason);
                    canceled = true;
                });

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(true, canceled);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }
#endif
    }
}