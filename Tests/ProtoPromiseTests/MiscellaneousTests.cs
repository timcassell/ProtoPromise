using System;
using NUnit.Framework;

namespace Proto.Promises.Tests
{
    public class Miscellaneous
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
        public void AccessingCancelExceptionOrRejectExceptionInOnResolvedDoesNotThrow_void()
        {
            void Test()
            {
                var promise = Promise.Resolved().Preserve();

                TestHelper.AddCallbacks<int, object, string>(promise,
                    onResolve: () =>
                    {
                        Promise.CancelException();
                        Promise.CancelException("Cancel!");
                        Promise.RejectException("Reject!");
                    });

                Promise.Manager.HandleCompletes();

                promise.Forget();
            }

            Test();
            TestHelper.Cleanup();
        }

        [Test]
        public void AccessingCancelExceptionOrRejectExceptionInOnResolvedDoesNotThrow_T()
        {
            void Test()
            {
                var promise = Promise.Resolved(100).Preserve();

                TestHelper.AddCallbacks<int, bool, object, string>(promise,
                    onResolve: v =>
                    {
                        Promise.CancelException();
                        Promise.CancelException("Cancel!");
                        Promise.RejectException("Reject!");
                    });

                Promise.Manager.HandleCompletes();

                promise.Forget();
            }

            Test();
            TestHelper.Cleanup();
        }

        [Test]
        public void AccessingCancelExceptionOrRejectExceptionInOnRejectedDoesNotThrow_void()
        {
            void Test()
            {
                var promise = Promise.Rejected("Reject!").Preserve();

                TestHelper.AddCallbacks<int, string, string>(promise,
                    onReject: (string rej) =>
                    {
                        Promise.CancelException();
                        Promise.CancelException("Cancel!");
                        Promise.RejectException("Reject!");
                    },
                    onUnknownRejection: () =>
                    {
                        Promise.CancelException();
                        Promise.CancelException("Cancel!");
                        Promise.RejectException("Reject!");
                    });

                Promise.Manager.HandleCompletes();

                promise.Forget();
            }

            Test();
            TestHelper.Cleanup();
        }

        [Test]
        public void AccessingCancelExceptionOrRejectExceptionInOnRejectedDoesNotThrow_T()
        {
            void Test()
            {
                var promise = Promise<int>.Rejected("Reject!").Preserve();

                TestHelper.AddCallbacks<int, bool, string, string>(promise,
                    onReject: (string rej) =>
                    {
                        Promise.CancelException();
                        Promise.CancelException("Cancel!");
                        Promise.RejectException("Reject!");
                    },
                    onUnknownRejection: () =>
                    {
                        Promise.CancelException();
                        Promise.CancelException("Cancel!");
                        Promise.RejectException("Reject!");
                    });

                Promise.Manager.HandleCompletes();

                promise.Forget();
            }

            Test();
            TestHelper.Cleanup();
        }

        [Test]
        public void ThrowingRejectExceptionInOnResolvedRejectsThePromiseWithTheGivenValue_void()
        {
            void Test()
            {
                var promise = Promise.Resolved().Preserve();

                int rejectCount = 0;
                string expected = "Reject!";

                Action<Promise> callback = p =>
                {
                    p.Catch((string e) =>
                    {
                        Assert.AreEqual(expected, e);
                        ++rejectCount;
                    }).Forget();
                };

                TestHelper.AddResolveCallbacks<int, string>(promise,
                    onResolve: () => { throw Promise.RejectException(expected); },
                    onCallbackAdded: p => callback(p),
                    onCallbackAddedConvert: p => callback(p)
                );
                TestHelper.AddCallbacks<int, object, string>(promise,
                    onResolve: () => { throw Promise.RejectException(expected); },
                    onCallbackAdded: p => callback(p),
                    onCallbackAddedConvert: p => callback(p)
                );
                TestHelper.AddContinueCallbacks<int, string>(promise,
                    onContinue: _ => { throw Promise.RejectException(expected); },
                    onCallbackAdded: p => callback(p),
                    onCallbackAddedConvert: p => callback(p)
                );

                Promise.Manager.HandleCompletes();

                Assert.AreEqual(
                    (TestHelper.resolveVoidCallbacks + TestHelper.continueVoidCallbacks) * 2,
                    rejectCount
                );

                promise.Forget();
            }

            Test();
            TestHelper.Cleanup();
        }

        [Test]
        public void ThrowingRejectExceptionInOnResolvedRejectsThePromiseWithTheGivenValue_T()
        {
            void Test()
            {
                var promise = Promise.Resolved(100).Preserve();

                int rejectCount = 0;
                string expected = "Reject!";

                Action<Promise> callback = p =>
                {
                    p.Catch((string e) =>
                    {
                        Assert.AreEqual(expected, e);
                        ++rejectCount;
                    }).Forget();
                };

                TestHelper.AddResolveCallbacks<int, bool, string>(promise,
                    onResolve: v => { throw Promise.RejectException(expected); },
                    onCallbackAdded: p => callback(p),
                    onCallbackAddedConvert: p => callback(p)
                );
                TestHelper.AddCallbacks<int, bool, object, string>(promise,
                    onResolve: v => { throw Promise.RejectException(expected); },
                    onCallbackAdded: p => callback(p),
                    onCallbackAddedConvert: p => callback(p)
                );
                TestHelper.AddContinueCallbacks<int, bool, string>(promise,
                    onContinue: _ => { throw Promise.RejectException(expected); },
                    onCallbackAdded: p => callback(p),
                    onCallbackAddedConvert: p => callback(p)
                );

                Promise.Manager.HandleCompletes();

                Assert.AreEqual(
                    (TestHelper.resolveTCallbacks + TestHelper.continueTCallbacks) * 2,
                    rejectCount
                );

                promise.Forget();
            }

            Test();
            TestHelper.Cleanup();
        }

        [Test]
        public void ThrowingRejectExceptionInOnRejectedRejectsThePromiseWithTheGivenValue_void()
        {
            void Test()
            {
                var promise = Promise.Rejected("A different reject value.").Preserve();

                int rejectCount = 0;
                string expected = "Reject!";

                Action<Promise> callback = p =>
                {
                    p.Catch((string e) =>
                    {
                        Assert.AreEqual(expected, e);
                        ++rejectCount;
                    }).Forget();
                };

                TestHelper.AddCallbacks<int, object, string>(promise,
                    onReject: v => { throw Promise.RejectException(expected); },
                    onUnknownRejection: () => { throw Promise.RejectException(expected); },
                    onCallbackAdded: p => callback(p),
                    onCallbackAddedConvert: p => callback(p)
                );
                TestHelper.AddContinueCallbacks<int, string>(promise,
                    onContinue: _ => { throw Promise.RejectException(expected); },
                    onCallbackAdded: p => callback(p),
                    onCallbackAddedConvert: p => callback(p)
                );

                Promise.Manager.HandleCompletes();

                Assert.AreEqual(
                    (TestHelper.rejectVoidCallbacks + TestHelper.continueVoidCallbacks) * 2,
                    rejectCount
                );

                promise.Forget();
            }

            Test();
            TestHelper.Cleanup();
        }

        [Test]
        public void ThrowingRejectExceptionInOnRejectedRejectsThePromiseWithTheGivenValue_T()
        {
            void Test()
            {
                var promise = Promise<int>.Rejected("A different reject value.").Preserve();

                int rejectCount = 0;
                string expected = "Reject!";

                Action<Promise> callback = p =>
                {
                    p.Catch((string e) =>
                    {
                        Assert.AreEqual(expected, e);
                        ++rejectCount;
                    }).Forget();
                };

                TestHelper.AddCallbacks<int, bool, object, string>(promise,
                    onReject: v => { throw Promise.RejectException(expected); },
                    onUnknownRejection: () => { throw Promise.RejectException(expected); },
                    onCallbackAdded: p => callback(p),
                    onCallbackAddedConvert: p => callback(p),
                    onCallbackAddedT: p => callback(p)
                );
                TestHelper.AddContinueCallbacks<int, bool, string>(promise,
                    onContinue: _ => { throw Promise.RejectException(expected); },
                    onCallbackAdded: p => callback(p),
                    onCallbackAddedConvert: p => callback(p)
                );

                Promise.Manager.HandleCompletes();

                Assert.AreEqual(
                    (TestHelper.rejectTCallbacks + TestHelper.continueTCallbacks) * 2,
                    rejectCount
                );

                promise.Forget();
            }

            Test();
            TestHelper.Cleanup();
        }

        [Test]
        public void ThrowingCancelExceptionInOnResolvedCancelsThePromiseWithTheGivenValue_void()
        {
            void Test()
            {
                var promise = Promise.Resolved().Preserve();

                int cancelCount = 0;
                string expected = "Cancel!";

                Action<Promise> callback = p =>
                {
                    p.CatchCancelation(reason =>
                    {
                        Assert.AreEqual(expected, reason.Value);
                        ++cancelCount;
                    });
                };

                TestHelper.AddResolveCallbacks<int, string>(promise,
                    onResolve: () => { throw Promise.CancelException(expected); },
                    onCallbackAdded: p => callback(p),
                    onCallbackAddedConvert: p => callback(p)
                );
                TestHelper.AddCallbacks<int, object, string>(promise,
                    onResolve: () => { throw Promise.CancelException(expected); },
                    onCallbackAdded: p => callback(p),
                    onCallbackAddedConvert: p => callback(p)
                );
                TestHelper.AddContinueCallbacks<int, string>(promise,
                    onContinue: _ => { throw Promise.CancelException(expected); },
                    onCallbackAdded: p => callback(p),
                    onCallbackAddedConvert: p => callback(p)
                );

                Promise.Manager.HandleCompletes();

                Assert.AreEqual(
                    (TestHelper.resolveVoidCallbacks + TestHelper.continueVoidCallbacks) * 2,
                    cancelCount
                );

                promise.Forget();
            }

            Test();
            TestHelper.Cleanup();
        }

        [Test]
        public void ThrowingCancelExceptionInOnResolvedCancelsThePromiseWithTheGivenValue_T()
        {
            void Test()
            {
                var promise = Promise.Resolved(100).Preserve();

                int cancelCount = 0;
                string expected = "Cancel!";

                Action<Promise> callback = p =>
                {
                    p.CatchCancelation(reason =>
                    {
                        Assert.AreEqual(expected, reason.Value);
                        ++cancelCount;
                    });
                };

                TestHelper.AddResolveCallbacks<int, bool, string>(promise,
                    onResolve: v => { throw Promise.CancelException(expected); },
                    onCallbackAdded: p => callback(p),
                    onCallbackAddedConvert: p => callback(p)
                );
                TestHelper.AddCallbacks<int, bool, object, string>(promise,
                    onResolve: v => { throw Promise.CancelException(expected); },
                    onCallbackAdded: p => callback(p),
                    onCallbackAddedConvert: p => callback(p)
                );
                TestHelper.AddContinueCallbacks<int, bool, string>(promise,
                    onContinue: _ => { throw Promise.CancelException(expected); },
                    onCallbackAdded: p => callback(p),
                    onCallbackAddedConvert: p => callback(p)
                );

                Promise.Manager.HandleCompletes();

                Assert.AreEqual(
                    (TestHelper.resolveTCallbacks + TestHelper.continueTCallbacks) * 2,
                    cancelCount
                );

                promise.Forget();
            }

            Test();
            TestHelper.Cleanup();
        }

        [Test]
        public void ThrowingCancelExceptionInOnRejectedCancelsThePromiseWithTheGivenValue_void()
        {
            void Test()
            {
                var promise = Promise.Rejected("Rejected").Preserve();

                int cancelCount = 0;
                string expected = "Cancel!";

                Action<Promise> callback = p =>
                {
                    p.CatchCancelation(reason =>
                    {
                        Assert.AreEqual(expected, reason.Value);
                        ++cancelCount;
                    });
                };

                TestHelper.AddCallbacks<int, object, string>(promise,
                    onReject: v => { throw Promise.CancelException(expected); },
                    onUnknownRejection: () => { throw Promise.CancelException(expected); },
                    onCallbackAdded: p => callback(p),
                    onCallbackAddedConvert: p => callback(p)
                );
                TestHelper.AddContinueCallbacks<int, string>(promise,
                    onContinue: _ => { throw Promise.CancelException(expected); },
                    onCallbackAdded: p => callback(p),
                    onCallbackAddedConvert: p => callback(p)
                );

                Promise.Manager.HandleCompletes();

                Assert.AreEqual(
                    (TestHelper.rejectVoidCallbacks + TestHelper.continueVoidCallbacks) * 2,
                    cancelCount
                );

                promise.Forget();
            }

            Test();
            TestHelper.Cleanup();
        }

        [Test]
        public void ThrowingCancelExceptionInOnRejectedCancelsThePromiseWithTheGivenValue_T()
        {
            void Test()
            {
                var promise = Promise<int>.Rejected("Rejected").Preserve();

                int cancelCount = 0;
                string expected = "Cancel!";

                Action<Promise> callback = p =>
                {
                    p.CatchCancelation(reason =>
                    {
                        Assert.AreEqual(expected, reason.Value);
                        ++cancelCount;
                    });
                };

                TestHelper.AddCallbacks<int, bool, object, string>(promise,
                    onReject: v => { throw Promise.CancelException(expected); },
                    onUnknownRejection: () => { throw Promise.CancelException(expected); },
                    onCallbackAdded: p => callback(p),
                    onCallbackAddedConvert: p => callback(p),
                    onCallbackAddedT: p => callback(p)
                );
                TestHelper.AddContinueCallbacks<int, int, string>(promise,
                    onContinue: _ => { throw Promise.CancelException(expected); },
                    onCallbackAdded: p => callback(p),
                    onCallbackAddedConvert: p => callback(p)
                );

                Promise.Manager.HandleCompletes();

                Assert.AreEqual(
                    (TestHelper.rejectTCallbacks + TestHelper.continueTCallbacks) * 2,
                    cancelCount
                );

                promise.Forget();
            }

            Test();
            TestHelper.Cleanup();
        }

        [Test]
        public void AccessingRethrowInNormalCodeThrows()
        {
            void Test()
            {
                Assert.Throws<InvalidOperationException>(() => { var _ = Promise.Rethrow; });
            }

            Test();
            TestHelper.Cleanup();
        }

        [Test]
        public void AccessingRethrowInOnResolvedThrows_void()
        {
            void Test()
            {
                var promise = Promise.Resolved().Preserve();

                int errorCount = 0;

                Action<Promise> callback = p =>
                {
                    p.Catch((object e) =>
                    {
                        Assert.IsInstanceOf(typeof(InvalidOperationException), e);
                        ++errorCount;
                    }).Forget();
                };

                TestHelper.AddResolveCallbacks<int, string>(promise,
                    onResolve: () => { var _ = Promise.Rethrow; },
                    onCallbackAdded: p => callback(p),
                    onCallbackAddedConvert: p => callback(p)
                );
                TestHelper.AddCallbacks<int, object, string>(promise,
                    onResolve: () => { var _ = Promise.Rethrow; },
                    onCallbackAdded: p => callback(p),
                    onCallbackAddedConvert: p => callback(p)
                );
                TestHelper.AddContinueCallbacks<int, string>(promise,
                    onContinue: _ => { var __ = Promise.Rethrow; },
                    onCallbackAdded: p => callback(p),
                    onCallbackAddedConvert: p => callback(p)
                );

                Promise.Manager.HandleCompletes();

                Assert.AreEqual(
                    (TestHelper.resolveVoidCallbacks + TestHelper.continueVoidCallbacks) * 2,
                    errorCount
                );

                promise.Forget();
            }

            Test();
            TestHelper.Cleanup();
        }

        [Test]
        public void AccessingRethrowInOnResolvedThrows_T()
        {
            void Test()
            {
                var promise = Promise.Resolved(100).Preserve();

                int errorCount = 0;

                Action<Promise> callback = p =>
                {
                    p.Catch((object e) =>
                    {
                        Assert.IsInstanceOf(typeof(InvalidOperationException), e);
                        ++errorCount;
                    }).Forget();
                };

                TestHelper.AddResolveCallbacks<int, bool, string>(promise,
                    onResolve: v => { var _ = Promise.Rethrow; },
                    onCallbackAdded: p => callback(p),
                    onCallbackAddedConvert: p => callback(p)
                );
                TestHelper.AddCallbacks<int, bool, object, string>(promise,
                    onResolve: v => { var _ = Promise.Rethrow; },
                    onCallbackAdded: p => callback(p),
                    onCallbackAddedConvert: p => callback(p)
                );
                TestHelper.AddContinueCallbacks<int, bool, string>(promise,
                    onContinue: _ => { var __ = Promise.Rethrow; },
                    onCallbackAdded: p => callback(p),
                    onCallbackAddedConvert: p => callback(p)
                );

                Promise.Manager.HandleCompletes();

                Assert.AreEqual(
                    (TestHelper.resolveTCallbacks + TestHelper.continueTCallbacks) * 2,
                    errorCount
                );

                promise.Forget();
            }

            Test();
            TestHelper.Cleanup();
        }

        [Test]
        public void ThrowingRethrowInOnRejectedRejectsThePromiseWithTheSameReason_void()
        {
            void Test()
            {
                string expected = "Reject!";

                var promise = Promise.Rejected(expected).Preserve();

                int rejectCount = 0;

                Action<Promise> callback = p =>
                {
                    p.Catch((string e) =>
                    {
                        Assert.AreEqual(expected, e);
                        ++rejectCount;
                    }).Forget();
                };

                TestHelper.AddCallbacks<int, object, string>(promise,
                    onReject: _ => { throw Promise.Rethrow; },
                    onUnknownRejection: () => { throw Promise.Rethrow; },
                    onCallbackAdded: p => callback(p),
                    onCallbackAddedConvert: p => callback(p)
                );

                Promise.Manager.HandleCompletes();

                Assert.AreEqual(
                    TestHelper.rejectVoidCallbacks * 2,
                    rejectCount
                );

                promise.Forget();
            }

            Test();
            TestHelper.Cleanup();
        }

        [Test]
        public void ThrowingRethrowInOnRejectedRejectsThePromiseWithTheSameReason_T()
        {
            void Test()
            {
                string expected = "Reject!";

                var promise = Promise<int>.Rejected(expected).Preserve();

                int rejectCount = 0;

                Action<Promise> callback = p =>
                {
                    p.Catch((string e) =>
                    {
                        Assert.AreEqual(expected, e);
                        ++rejectCount;
                    }).Forget();
                };

                TestHelper.AddCallbacks<int, bool, object, string>(promise,
                    onReject: _ => { throw Promise.Rethrow; },
                    onUnknownRejection: () => { throw Promise.Rethrow; },
                    onCallbackAdded: p => callback(p),
                    onCallbackAddedConvert: p => callback(p),
                    onCallbackAddedT: p => callback(p)
                );

                Promise.Manager.HandleCompletes();

                Assert.AreEqual(
                    TestHelper.rejectTCallbacks * 2,
                    rejectCount
                );

                promise.Forget();
            }

            Test();
            TestHelper.Cleanup();
        }

        [Test]
        public void PromiseResolvedIsResolved()
        {
            void Test()
            {
                bool resolved = false;

                Promise.Resolved()
                    .CatchCancelation(r => Assert.Fail("Promise was canceled when it should have been resolved"))
                    .Then(() => resolved = true, () => Assert.Fail("Promise was rejected when it should have been resolved"))
                    .Forget();

                Promise.Manager.HandleCompletes();
                Assert.AreEqual(true, resolved);
            }

            Test();
            TestHelper.Cleanup();
        }

        [Test]
        public void PromiseResolvedIsResolvedWithTheGivenValue()
        {
            void Test()
            {
                int expected = 100;
                bool resolved = false;

                Promise.Resolved(expected)
                    .CatchCancelation(r => Assert.Fail("Promise was canceled when it should have been resolved"))
                    .Then(val =>
                    {
                        Assert.AreEqual(expected, val);
                        resolved = true;
                    }, () => Assert.Fail("Promise was rejected when it should have been resolved"))
                    .Forget();

                Promise.Manager.HandleCompletes();
                Assert.AreEqual(true, resolved);
            }

            Test();
            TestHelper.Cleanup();
        }

        [Test]
        public void PromiseRejectedIsRejectedWithTheGivenReason_void()
        {
            void Test()
            {
                string expected = "Reject";
                bool rejected = false;

                Promise.Rejected(expected)
                    .CatchCancelation(r => Assert.Fail("Promise was canceled when it should have been rejected"))
                    .Then(() => Assert.Fail("Promise was resolved when it should have been rejected"),
                    (object reason) =>
                    {
                        Assert.AreEqual(expected, reason);
                        rejected = true;
                    })
                    .Forget();

                Promise.Manager.HandleCompletes();
                Assert.AreEqual(true, rejected);
            }

            Test();
            TestHelper.Cleanup();
        }

        [Test]
        public void PromiseRejectedIsRejectedWithTheGivenReason_T()
        {
            void Test()
            {
                string expected = "Reject";
                bool rejected = false;

                Promise<int>.Rejected(expected)
                    .CatchCancelation(r => Assert.Fail("Promise was canceled when it should have been rejected"))
                    .Then(() => Assert.Fail("Promise was resolved when it should have been rejected"),
                    (object reason) =>
                    {
                        Assert.AreEqual(expected, reason);
                        rejected = true;
                    })
                    .Forget();

                Promise.Manager.HandleCompletes();
                Assert.AreEqual(true, rejected);
            }

            Test();
            TestHelper.Cleanup();
        }

        [Test]
        public void PromiseCanceledIsCanceledWithTheGivenReason_void()
        {
            void Test()
            {
                string expected = "Cancel";
                bool canceled = false;

                Promise.Canceled(expected)
                    .CatchCancelation(reason =>
                    {
                        Assert.AreEqual(expected, reason.Value);
                        canceled = true;
                    })
                    .Then(() => Assert.Fail("Promise was resolved when it should have been canceled"), () => Assert.Fail("Promise was rejected when it should have been canceled"))
                    .Forget();

                Promise.Manager.HandleCompletes();
                Assert.AreEqual(true, canceled);
            }

            Test();
            TestHelper.Cleanup();
        }

        [Test]
        public void PromiseCanceledIsCanceledWithTheGivenReason_T()
        {
            void Test()
            {
                string expected = "Cancel";
                bool canceled = false;

                Promise<int>.Canceled(expected)
                    .CatchCancelation(reason =>
                    {
                        Assert.AreEqual(expected, reason.Value);
                        canceled = true;
                    })
                    .Then(() => Assert.Fail("Promise was resolved when it should have been canceled"), () => Assert.Fail("Promise was rejected when it should have been canceled"))
                    .Forget();

                Promise.Manager.HandleCompletes();
                Assert.AreEqual(true, canceled);
            }

            Test();
            TestHelper.Cleanup();
        }
    }
}