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
            Promise.Manager.HandleCompletes();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void AccessingCancelExceptionOrRejectExceptionInOnResolvedDoesNotThrow()
        {
            var promise1 = Promise.Resolved();
            var promise2 = Promise.Resolved(100);

            TestHelper.AddCallbacks<object>(promise1, () =>
            {
#if PROMISE_CANCEL
                Promise.CancelException();
                Promise.CancelException("Cancel!");
#endif
                Promise.RejectException("Reject!");
            }, null, null);
            TestHelper.AddCallbacks<int, object>(promise2, v =>
            {
#if PROMISE_CANCEL
                Promise.CancelException();
                Promise.CancelException("Cancel!");
#endif
                Promise.RejectException("Reject!");
            }, null, null);

            Promise.Manager.HandleCompletes();

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletes();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void AccessingCancelExceptionOrRejectExceptionInOnRejectedDoesNotThrow()
        {
            var promise1 = Promise.Rejected("Reject!");
            var promise2 = Promise.Rejected<int, string>("Reject!");

            TestHelper.AddCallbacks(promise1, null, (string rej) =>
            {
#if PROMISE_CANCEL
                Promise.CancelException();
                Promise.CancelException("Cancel!");
#endif
                Promise.RejectException("Reject!");
            }, () =>
            {
#if PROMISE_CANCEL
                Promise.CancelException();
                Promise.CancelException("Cancel!");
#endif
                Promise.RejectException("Reject!");
            });
            TestHelper.AddCallbacks(promise2, null, (string rej) =>
            {
#if PROMISE_CANCEL
                Promise.CancelException();
                Promise.CancelException("Cancel!");
#endif
                Promise.RejectException("Reject!");
            }, () =>
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
            Promise.Manager.HandleCompletes();
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

            TestHelper.AddCatchRejectCallbacks<object, string>(promise1, () =>
            {
                throw Promise.RejectException(expected);
            }, null, () =>
            {
                ++voidRejections;
            }, (string rej) =>
            {
                Assert.AreEqual(expected, rej);
                ++voidRejections;
            });
            TestHelper.AddCatchRejectCallbacks<int, object, string>(promise2, v =>
            {
                throw Promise.RejectException(expected);
            }, null, () =>
            {
                ++intRejections;
            }, (string rej) =>
            {
                Assert.AreEqual(expected, rej);
                ++intRejections;
            });

            Promise.Manager.HandleCompletes();

            Assert.AreEqual(TestHelper.resolveVoidCallbacks, voidRejections);
            Assert.AreEqual(TestHelper.resolveTCallbacks, intRejections);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletes();
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

            TestHelper.AddCatchRejectCallbacks<string, string>(promise1, null, (string rej) =>
            {
                throw Promise.RejectException(expected);
            }, () =>
            {
                throw Promise.RejectException(expected);
            }, (string rej) =>
            {
                Assert.AreEqual(expected, rej);
                ++voidRejections;
            });
            TestHelper.AddCatchRejectCallbacks<int, string, string>(promise2, null, (string rej) =>
            {
                throw Promise.RejectException(expected);
            }, () =>
            {
                throw Promise.RejectException(expected);
            }, (string rej) =>
            {
                Assert.AreEqual(expected, rej);
                ++intRejections;
            });

            Promise.Manager.HandleCompletes();

            Assert.AreEqual(TestHelper.catchRejectVoidCallbacks, voidRejections);
            Assert.AreEqual(TestHelper.catchRejectTCallbacks, intRejections);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletes();
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

            TestHelper.AddCatchCancelCallbacks<object, string>(promise1, () =>
            {
                throw Promise.CancelException(expected);
            }, null, null, cancel =>
            {
                Assert.AreEqual(expected, cancel);
                ++voidCancelations;
            });
            TestHelper.AddCatchCancelCallbacks<int, object, string>(promise2, v =>
            {
                throw Promise.CancelException(expected);
            }, null, null, cancel =>
            {
                Assert.AreEqual(expected, cancel);
                ++intCancelations;
            });

            Promise.Manager.HandleCompletes();

            Assert.AreEqual(TestHelper.resolveVoidCallbacks, voidCancelations);
            Assert.AreEqual(TestHelper.resolveTCallbacks, intCancelations);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletes();
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

            TestHelper.AddCatchCancelCallbacks<string, string>(promise1, null, rej =>
            {
                throw Promise.CancelException(expected);
            }, () =>
            {
                throw Promise.CancelException(expected);
            }, cancel =>
            {
                Assert.AreEqual(expected, cancel);
                ++voidCancelations;
            });
            TestHelper.AddCatchCancelCallbacks<int, string, string>(promise2, null, rej =>
            {
                throw Promise.CancelException(expected);
            }, () =>
            {
                throw Promise.CancelException(expected);
            }, cancel =>
            {
                Assert.AreEqual(expected, cancel);
                ++intCancelations;
            });

            Promise.Manager.HandleCompletes();

            Assert.AreEqual(TestHelper.rejectVoidCallbacks, voidCancelations);
            Assert.AreEqual(TestHelper.rejectTCallbacks, intCancelations);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletes();
            LogAssert.NoUnexpectedReceived();
        }
#endif

        [Test]
        public void AccessingRethrowInNormalCodeThrows()
        {
            Assert.Throws<InvalidOperationException>(() => { var _ = Promise.Rethrow; });

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletes();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void AccessingRethrowInOnResolvedThrows()
        {
            var promise1 = Promise.Resolved();
            var promise2 = Promise.Resolved(100);

            bool caughtVoid = false;
            bool caughtT = false;

            TestHelper.AddCallbacks<InvalidOperationException>(promise1, () =>
            {
                var _ = Promise.Rethrow;
            }, null, null, e =>
            {
                Assert.IsInstanceOf(typeof(InvalidOperationException), e);
                caughtVoid = true;
            });
            TestHelper.AddCallbacks<int, InvalidOperationException>(promise2, v =>
            {
                var _ = Promise.Rethrow;
            }, null, null, e =>
            {
                Assert.IsInstanceOf(typeof(InvalidOperationException), e);
                caughtT = true;
            });

            Promise.Manager.HandleCompletes();

            Assert.AreEqual(true, caughtVoid);
            Assert.AreEqual(true, caughtT);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletes();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void ThrowingRethrowInOnRejectedRejectsThePromiseWithTheSameReason()
        {
            string expected = "Reject!";

            var promise1 = Promise.Rejected(expected);
            var promise2 = Promise.Rejected<int, string>(expected);

            bool caughtVoid = false;
            bool caughtT = false;

            TestHelper.AddCatchRejectCallbacks<string, string>(promise1, null, e =>
            {
                throw Promise.Rethrow;
            }, () =>
            {
                throw Promise.Rethrow;
            }, e =>
            {
                Assert.AreEqual(expected, e);
                caughtVoid = true;
            });
            TestHelper.AddCatchRejectCallbacks<int, string, string>(promise2, null, e =>
            {
                throw Promise.Rethrow;
            }, () =>
            {
                throw Promise.Rethrow;
            }, e =>
            {
                Assert.AreEqual(expected, e);
                caughtT = true;
            });

            Promise.Manager.HandleCompletes();

            Assert.AreEqual(true, caughtVoid);
            Assert.AreEqual(true, caughtT);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletes();
            LogAssert.NoUnexpectedReceived();
        }
    }
}