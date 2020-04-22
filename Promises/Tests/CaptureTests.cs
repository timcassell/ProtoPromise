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
#if !PROTO_PROMISE_PROGRESS_DISABLE
#define PROMISE_PROGRESS
#else
#undef PROMISE_PROGRESS
#endif

using System;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Proto.Promises.Tests
{
    public class CaptureTests
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

#if PROMISE_DEBUG

#if PROMISE_PROGRESS
        [Test]
        public void IfOnProgressIsNullThrow()
        {
            var deferred = Promise.NewDeferred();

            Assert.AreEqual(Promise.State.Pending, deferred.State);

            Assert.Throws<ArgumentNullException>(() =>
            {
                deferred.Promise.Progress(100, default(Action<int, float>));
            });

            deferred.Resolve();

            var deferredInt = Promise.NewDeferred<int>();
            Assert.AreEqual(Promise.State.Pending, deferredInt.State);

            Assert.Throws<ArgumentNullException>(() =>
            {
                deferredInt.Promise.Progress(100, default(Action<int, float>));
            });

            deferredInt.Resolve(0);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }
#endif

#if PROMISE_CANCEL
        [Test]
        public void IfOnCanceledIsNullThrow()
        {
            var deferred = Promise.NewDeferred();

            Assert.AreEqual(Promise.State.Pending, deferred.State);

            Assert.Throws<ArgumentNullException>(() =>
            {
                deferred.Promise.CatchCancelation(100, default(Action<int, Promise.ReasonContainer>));
            });

            deferred.Cancel();

            var deferredInt = Promise.NewDeferred<int>();
            Assert.AreEqual(Promise.State.Pending, deferredInt.State);

            Assert.Throws<ArgumentNullException>(() =>
            {
                deferredInt.Promise.CatchCancelation(100, default(Action<int, Promise.ReasonContainer>));
            });

            deferredInt.Cancel();

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }
#endif

        [Test]
        public void IfOnFinallyIsNullThrow()
        {
            var deferred = Promise.NewDeferred();
            var deferredInt = Promise.NewDeferred<int>();

            Assert.Throws<ArgumentNullException>(() =>
            {
                deferred.Promise.Finally(100, default(Action<int>));
            });
            Assert.Throws<ArgumentNullException>(() =>
            {
                deferredInt.Promise.Finally(100, default(Action<int>));
            });

            deferred.Resolve();
            deferredInt.Resolve(0);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void IfOnContinueIsNullThrow()
        {
            var deferred = Promise.NewDeferred();
            var deferredInt = Promise.NewDeferred<int>();

            Assert.Throws<ArgumentNullException>(() =>
            {
                deferred.Promise.ContinueWith(100, default(Action<int, Promise.ResultContainer>));
            });
            Assert.Throws<ArgumentNullException>(() =>
            {
                deferred.Promise.ContinueWith(100, default(Func<int, Promise.ResultContainer, bool>));
            });
            Assert.Throws<ArgumentNullException>(() =>
            {
                deferred.Promise.ContinueWith(100, default(Func<int, Promise.ResultContainer, Promise>));
            });
            Assert.Throws<ArgumentNullException>(() =>
            {
                deferred.Promise.ContinueWith(100, default(Func<int, Promise.ResultContainer, Promise<bool>>));
            });
            Assert.Throws<ArgumentNullException>(() =>
            {
                deferredInt.Promise.ContinueWith(100, default(Action<int, Promise<int>.ResultContainer>));
            });
            Assert.Throws<ArgumentNullException>(() =>
            {
                deferredInt.Promise.ContinueWith(100, default(Func<int, Promise<int>.ResultContainer, bool>));
            });
            Assert.Throws<ArgumentNullException>(() =>
            {
                deferredInt.Promise.ContinueWith(100, default(Func<int, Promise<int>.ResultContainer, Promise>));
            });
            Assert.Throws<ArgumentNullException>(() =>
            {
                deferredInt.Promise.ContinueWith(100, default(Func<int, Promise<int>.ResultContainer, Promise<bool>>));
            });
            deferred.Resolve();
            deferredInt.Resolve(0);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void IfOnFulfilledIsNullThrow()
        {
            var deferred = Promise.NewDeferred();

            Assert.AreEqual(Promise.State.Pending, deferred.State);

            var promise = deferred.Promise;

            Assert.Throws<ArgumentNullException>(() =>
            {
                promise.Then(100, default(Action<int>));
            });
            Assert.Throws<ArgumentNullException>(() =>
            {
                promise.Then(100, default(Func<int, bool>));
            });
            Assert.Throws<ArgumentNullException>(() =>
            {
                promise.Then(100, default(Func<int, Promise>));
            });
            Assert.Throws<ArgumentNullException>(() =>
            {
                promise.Then(100, default(Func<int, Promise<bool>>));
            });


            Assert.Throws<ArgumentNullException>(() =>
            {
                promise.Then(100, default(Action<int>), () => { });
            });
            Assert.Throws<ArgumentNullException>(() =>
            {
                promise.Then(100, default(Action<int>), (string failValue) => { });
            });

            Assert.Throws<ArgumentNullException>(() =>
            {
                promise.Then(100, default(Func<int, bool>), () => default(bool));
            });
            Assert.Throws<ArgumentNullException>(() =>
            {
                promise.Then(100, default(Func<int, bool>), (string failValue) => default(bool));
            });

            Assert.Throws<ArgumentNullException>(() =>
            {
                promise.Then(100, default(Func<int, Promise>), () => default(Promise));
            });
            Assert.Throws<ArgumentNullException>(() =>
            {
                promise.Then(100, default(Func<int, Promise>), (string failValue) => default(Promise));
            });

            Assert.Throws<ArgumentNullException>(() =>
            {
                promise.Then(100, default(Func<int, Promise<bool>>), () => default(Promise<bool>));
            });
            Assert.Throws<ArgumentNullException>(() =>
            {
                promise.Then(100, default(Func<int, Promise<bool>>), (string failValue) => default(Promise<bool>));
            });


            Assert.Throws<ArgumentNullException>(() =>
            {
                promise.Then(100, default(Action<int>), () => default(Promise));
            });
            Assert.Throws<ArgumentNullException>(() =>
            {
                promise.Then(100, default(Action<int>), (string failValue) => default(Promise));
            });

            Assert.Throws<ArgumentNullException>(() =>
            {
                promise.Then(100, default(Func<int, bool>), () => default(Promise<bool>));
            });
            Assert.Throws<ArgumentNullException>(() =>
            {
                promise.Then(100, default(Func<int, bool>), (string failValue) => default(Promise<bool>));
            });

            Assert.Throws<ArgumentNullException>(() =>
            {
                promise.Then(100, default(Func<int, Promise>), () => { });
            });
            Assert.Throws<ArgumentNullException>(() =>
            {
                promise.Then(100, default(Func<int, Promise>), (string failValue) => { });
            });

            Assert.Throws<ArgumentNullException>(() =>
            {
                promise.Then(100, default(Func<int, Promise<bool>>), () => default(bool));
            });
            Assert.Throws<ArgumentNullException>(() =>
            {
                promise.Then(100, default(Func<int, Promise<bool>>), (string failValue) => default(bool));
            });

            deferred.Resolve();

            var deferredInt = Promise.NewDeferred<int>();

            Assert.AreEqual(Promise.State.Pending, deferredInt.State);

            var promiseInt = deferredInt.Promise;

            Assert.Throws<ArgumentNullException>(() =>
            {
                promiseInt.Then(true, default(Action<bool, int>));
            });
            Assert.Throws<ArgumentNullException>(() =>
            {
                promiseInt.Then(true, default(Func<bool, int, int>));
            });
            Assert.Throws<ArgumentNullException>(() =>
            {
                promiseInt.Then(true, default(Func<bool, int, Promise>));
            });
            Assert.Throws<ArgumentNullException>(() =>
            {
                promiseInt.Then(true, default(Func<bool, int, Promise<int>>));
            });


            Assert.Throws<ArgumentNullException>(() =>
            {
                promiseInt.Then(true, default(Action<bool, int>), () => { });
            });
            Assert.Throws<ArgumentNullException>(() =>
            {
                promiseInt.Then(true, default(Action<bool, int>), (string failValue) => { });
            });

            Assert.Throws<ArgumentNullException>(() =>
            {
                promiseInt.Then(true, default(Func<bool, int, int>), () => default(int));
            });
            Assert.Throws<ArgumentNullException>(() =>
            {
                promiseInt.Then(true, default(Func<bool, int, int>), (string failValue) => default(int));
            });

            Assert.Throws<ArgumentNullException>(() =>
            {
                promiseInt.Then(true, default(Func<bool, int, Promise>), () => default(Promise));
            });
            Assert.Throws<ArgumentNullException>(() =>
            {
                promiseInt.Then(true, default(Func<bool, int, Promise>), (string failValue) => default(Promise));
            });

            Assert.Throws<ArgumentNullException>(() =>
            {
                promiseInt.Then(true, default(Func<bool, int, Promise<int>>), () => default(Promise<int>));
            });
            Assert.Throws<ArgumentNullException>(() =>
            {
                promiseInt.Then(true, default(Func<bool, int, Promise<int>>), (string failValue) => default(Promise<int>));
            });


            Assert.Throws<ArgumentNullException>(() =>
            {
                promiseInt.Then(true, default(Action<bool, int>), () => default(Promise));
            });
            Assert.Throws<ArgumentNullException>(() =>
            {
                promiseInt.Then(true, default(Action<bool, int>), (string failValue) => default(Promise));
            });

            Assert.Throws<ArgumentNullException>(() =>
            {
                promiseInt.Then(true, default(Func<bool, int, int>), () => default(Promise<int>));
            });
            Assert.Throws<ArgumentNullException>(() =>
            {
                promiseInt.Then(true, default(Func<bool, int, int>), (string failValue) => default(Promise<int>));
            });

            Assert.Throws<ArgumentNullException>(() =>
            {
                promiseInt.Then(true, default(Func<bool, int, Promise>), () => { });
            });
            Assert.Throws<ArgumentNullException>(() =>
            {
                promiseInt.Then(true, default(Func<bool, int, Promise>), (string failValue) => { });
            });

            Assert.Throws<ArgumentNullException>(() =>
            {
                promiseInt.Then(true, default(Func<bool, int, Promise<int>>), () => default(int));
            });
            Assert.Throws<ArgumentNullException>(() =>
            {
                promiseInt.Then(true, default(Func<bool, int, Promise<int>>), (string failValue) => default(int));
            });

            deferredInt.Resolve(0);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void IfOnRejectedIsNullThrow()
        {
            var deferred = Promise.NewDeferred();

            Assert.AreEqual(Promise.State.Pending, deferred.State);

            var promise = deferred.Promise;

            Assert.Throws<ArgumentNullException>(() =>
            {
                promise.Catch(100, default(Action<int>));
            });
            Assert.Throws<ArgumentNullException>(() =>
            {
                promise.Catch(100, default(Action<int, string>));
            });

            Assert.Throws<ArgumentNullException>(() =>
            {
                promise.Catch(100, default(Func<int, Promise>));
            });
            Assert.Throws<ArgumentNullException>(() =>
            {
                promise.Catch(100, default(Func<int, string, Promise>));
            });


            Assert.Throws<ArgumentNullException>(() =>
            {
                promise.Then(() => { }, 100, default(Action<int>));
            });
            Assert.Throws<ArgumentNullException>(() =>
            {
                promise.Then(() => { }, 100, default(Action<int, string>));
            });

            Assert.Throws<ArgumentNullException>(() =>
            {
                promise.Then(() => default(Promise), 100, default(Func<int, Promise>));
            });
            Assert.Throws<ArgumentNullException>(() =>
            {
                promise.Then(() => default(Promise), 100, default(Func<int, string, Promise>));
            });

            Assert.Throws<ArgumentNullException>(() =>
            {
                promise.Then(() => "string", 100, default(Func<int, string>));
            });
            Assert.Throws<ArgumentNullException>(() =>
            {
                promise.Then(() => "string", 100, default(Func<int, Exception, string>));
            });

            Assert.Throws<ArgumentNullException>(() =>
            {
                promise.Then(() => default(Promise<string>), 100, default(Func<int, Promise<string>>));
            });
            Assert.Throws<ArgumentNullException>(() =>
            {
                promise.Then(() => default(Promise<string>), 100, default(Func<int, Exception, Promise<string>>));
            });


            Assert.Throws<ArgumentNullException>(() =>
            {
                promise.Then(() => default(Promise), 100, default(Action<int>));
            });
            Assert.Throws<ArgumentNullException>(() =>
            {
                promise.Then(() => default(Promise), 100, default(Action<int, string>));
            });

            Assert.Throws<ArgumentNullException>(() =>
            {
                promise.Then(() => { }, 100, default(Func<int, Promise>));
            });
            Assert.Throws<ArgumentNullException>(() =>
            {
                promise.Then(() => { }, 100, default(Func<int, string, Promise>));
            });

            Assert.Throws<ArgumentNullException>(() =>
            {
                promise.Then(() => default(Promise<string>), 100, default(Func<int, string>));
            });
            Assert.Throws<ArgumentNullException>(() =>
            {
                promise.Then(() => default(Promise<string>), 100, default(Func<int, Exception, string>));
            });

            Assert.Throws<ArgumentNullException>(() =>
            {
                promise.Then(() => "string", 100, default(Func<int, Promise<string>>));
            });
            Assert.Throws<ArgumentNullException>(() =>
            {
                promise.Then(() => "string", 100, default(Func<int, Exception, Promise<string>>));
            });

            deferred.Resolve();

            var deferredInt = Promise.NewDeferred<int>();

            Assert.AreEqual(Promise.State.Pending, deferredInt.State);

            var promiseInt = deferredInt.Promise;

            Assert.Throws<ArgumentNullException>(() =>
            {
                promiseInt.Catch(true, default(Func<bool, int>));
            });
            Assert.Throws<ArgumentNullException>(() =>
            {
                promiseInt.Catch(true, default(Func<bool, string, int>));
            });

            Assert.Throws<ArgumentNullException>(() =>
            {
                promiseInt.Catch(true, default(Func<bool, Promise<int>>));
            });
            Assert.Throws<ArgumentNullException>(() =>
            {
                promiseInt.Catch(true, default(Func<bool, string, Promise<int>>));
            });


            Assert.Throws<ArgumentNullException>(() =>
            {
                promiseInt.Then((int x) => { }, true, default(Action<bool>));
            });
            Assert.Throws<ArgumentNullException>(() =>
            {
                promiseInt.Then((int x) => { }, true, default(Action<bool, string>));
            });

            Assert.Throws<ArgumentNullException>(() =>
            {
                promiseInt.Then((int x) => default(Promise), true, default(Func<bool, Promise>));
            });
            Assert.Throws<ArgumentNullException>(() =>
            {
                promiseInt.Then((int x) => default(Promise), true, default(Func<bool, string, Promise>));
            });

            Assert.Throws<ArgumentNullException>(() =>
            {
                promiseInt.Then((int x) => "string", true, default(Func<bool, string>));
            });
            Assert.Throws<ArgumentNullException>(() =>
            {
                promiseInt.Then((int x) => "string", true, default(Func<bool, Exception, string>));
            });

            Assert.Throws<ArgumentNullException>(() =>
            {
                promiseInt.Then((int x) => default(Promise<string>), true, default(Func<bool, Promise<string>>));
            });
            Assert.Throws<ArgumentNullException>(() =>
            {
                promiseInt.Then((int x) => default(Promise<string>), true, default(Func<bool, Exception, Promise<string>>));
            });


            Assert.Throws<ArgumentNullException>(() =>
            {
                promiseInt.Then((int x) => default(Promise), true, default(Action<bool>));
            });
            Assert.Throws<ArgumentNullException>(() =>
            {
                promiseInt.Then((int x) => default(Promise), true, default(Action<bool, string>));
            });

            Assert.Throws<ArgumentNullException>(() =>
            {
                promiseInt.Then((int x) => { }, true, default(Func<bool, Promise>));
            });
            Assert.Throws<ArgumentNullException>(() =>
            {
                promiseInt.Then((int x) => { }, true, default(Func<bool, string, Promise>));
            });

            Assert.Throws<ArgumentNullException>(() =>
            {
                promiseInt.Then((int x) => default(Promise<string>), true, default(Func<bool, string>));
            });
            Assert.Throws<ArgumentNullException>(() =>
            {
                promiseInt.Then((int x) => default(Promise<string>), true, default(Func<bool, Exception, string>));
            });

            Assert.Throws<ArgumentNullException>(() =>
            {
                promiseInt.Then((int x) => "string", true, default(Func<bool, Promise<string>>));
            });
            Assert.Throws<ArgumentNullException>(() =>
            {
                promiseInt.Then((int x) => "string", true, default(Func<bool, Exception, Promise<string>>));
            });

            deferredInt.Resolve(0);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }
#endif

#if PROMISE_PROGRESS
        [Test]
        public void OnProgressWillBeInvokedWithCapturedValue()
        {
            var deferred = Promise.NewDeferred();

            string expected = "expected";
            bool invoked = false;

            deferred.Promise
                .Progress(expected, (cv, progress) =>
                {
                    Assert.AreEqual(expected, cv);
                    invoked = true;
                });

            deferred.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            deferred.Resolve();
            Promise.Manager.HandleCompletesAndProgress();

            var deferredInt = Promise.NewDeferred<int>();

            deferredInt.Promise
                .Progress(expected, (cv, progress) =>
                {
                    Assert.AreEqual(expected, cv);
                    invoked = true;
                });

            deferredInt.ReportProgress(0.5f);
            Promise.Manager.HandleCompletesAndProgress();
            deferredInt.Resolve(0);
            Promise.Manager.HandleCompletesAndProgress();

            Assert.AreEqual(true, invoked);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }
#endif

#if PROMISE_CANCEL
        [Test]
        public void OnCanceledWillBeInvokedWithCapturedValue()
        {
            var deferred = Promise.NewDeferred();

            string expected = "expected";
            int cancelValue = 50;
            bool invoked = false;

            deferred.Promise
                .CatchCancelation(expected, (cv, reason) =>
                {
                    Assert.AreEqual(expected, cv);
                    Assert.AreEqual(cancelValue, reason.Value);
                    invoked = true;
                });

            deferred.Cancel(50);

            var deferredInt = Promise.NewDeferred<int>();

            deferredInt.Promise
                .CatchCancelation(expected, (cv, reason) =>
                {
                    Assert.AreEqual(expected, cv);
                    Assert.AreEqual(cancelValue, reason.Value);
                    invoked = true;
                });

            deferredInt.Cancel(cancelValue);

            Promise.Manager.HandleCompletes();

            Assert.AreEqual(true, invoked);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }
#endif

        [Test]
        public void OnFinallyWillBeInvokedWithCapturedValue0()
        {
            var deferred = Promise.NewDeferred();
            var deferredInt = Promise.NewDeferred<int>();

            string expected = "expected";
            bool invoked = false;

            deferred.Promise
                .Finally(expected, cv =>
                {
                    Assert.AreEqual(expected, cv);
                    invoked = true;
                });
            deferredInt.Promise
                .Finally(expected, cv =>
                {
                    Assert.AreEqual(expected, cv);
                    invoked = true;
                });

            deferred.Resolve();
            deferredInt.Resolve(0);

            Promise.Manager.HandleCompletes();

            Assert.AreEqual(true, invoked);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void OnFinallyWillBeInvokedWithCapturedValue1()
        {
            var deferred = Promise.NewDeferred();
            var deferredInt = Promise.NewDeferred<int>();

            string expected = "expected";
            bool invoked = false;

            deferred.Promise
                .Finally(expected, cv =>
                {
                    Assert.AreEqual(expected, cv);
                    invoked = true;
                })
                .Catch(() => { });
            deferredInt.Promise
                .Finally(expected, cv =>
                {
                    Assert.AreEqual(expected, cv);
                    invoked = true;
                })
                .Catch(() => { });

            deferred.Reject("Reject");
            deferredInt.Reject("Reject");

            Promise.Manager.HandleCompletes();

            Assert.AreEqual(true, invoked);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

#if PROMISE_CANCEL
        [Test]
        public void OnFinallyWillBeInvokedWithCapturedValue2()
        {
            var deferred = Promise.NewDeferred();
            var deferredInt = Promise.NewDeferred<int>();

            string expected = "expected";
            bool invoked = false;

            deferred.Promise
                .Finally(expected, cv =>
                {
                    Assert.AreEqual(expected, cv);
                    invoked = true;
                });
            deferredInt.Promise
                .Finally(expected, cv =>
                {
                    Assert.AreEqual(expected, cv);
                    invoked = true;
                });

            deferred.Cancel();
            deferredInt.Cancel();

            deferred = Promise.NewDeferred();
            deferredInt = Promise.NewDeferred<int>();

            deferred.Promise
                .Finally(expected, cv =>
                {
                    Assert.AreEqual(expected, cv);
                    invoked = true;
                });
            deferredInt.Promise
                .Finally(expected, cv =>
                {
                    Assert.AreEqual(expected, cv);
                    invoked = true;
                });

            deferred.Cancel("Cancel");
            deferredInt.Cancel("Cancel");

            Promise.Manager.HandleCompletes();

            Assert.AreEqual(true, invoked);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }
#endif

        [Test]
        public void OnContinueWillBeInvokedWithCapturedValue0()
        {
            var deferred = Promise.NewDeferred();

            string expected = "expected";
            bool invoked = false;

            TestHelper.AddContinueCallbacks<int, string>(deferred.Promise,
                captureValue: expected,
                onContinueCapture: (cv, r) =>
                {
                    Assert.AreEqual(expected, cv);
                    invoked = true;
                }
            );

            deferred.Resolve();
            Promise.Manager.HandleCompletes();

            Assert.AreEqual(true, invoked);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void OnContinueWillBeInvokedWithCapturedValue1()
        {
            var deferred = Promise.NewDeferred();

            string expected = "expected";
            bool invoked = false;

            TestHelper.AddContinueCallbacks<int, string>(deferred.Promise,
                captureValue: expected,
                onContinueCapture: (cv, r) =>
                {
                    Assert.AreEqual(expected, cv);
                    invoked = true;
                }
            );

            deferred.Reject("Reject");
            Promise.Manager.HandleCompletes();

            Assert.AreEqual(true, invoked);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void OnContinueWillBeInvokedWithCapturedValue2()
        {
            var deferred = Promise.NewDeferred<int>();

            string expected = "expected";
            bool invoked = false;

            TestHelper.AddContinueCallbacks<int, int, string>(deferred.Promise,
                captureValue: expected,
                onContinueCapture: (cv, r) =>
                {
                    Assert.AreEqual(expected, cv);
                    invoked = true;
                }
            );

            deferred.Resolve(50);
            Promise.Manager.HandleCompletes();

            Assert.AreEqual(true, invoked);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void OnContinueWillBeInvokedWithCapturedValue3()
        {
            var deferred = Promise.NewDeferred<int>();

            string expected = "expected";
            bool invoked = false;

            TestHelper.AddContinueCallbacks<int, int, string>(deferred.Promise,
                captureValue: expected,
                onContinueCapture: (cv, r) =>
                {
                    Assert.AreEqual(expected, cv);
                    invoked = true;
                }
            );

            deferred.Reject("Reject");
            Promise.Manager.HandleCompletes();

            Assert.AreEqual(true, invoked);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void OnResolvedWillBeInvokedWithCapturedValue0()
        {
            var deferred = Promise.NewDeferred();

            string expected = "expected";
            bool invoked = false;

            TestHelper.AddResolveCallbacks<int, string>(deferred.Promise,
                captureValue: expected,
                onResolveCapture: cv =>
                {
                    Assert.AreEqual(expected, cv);
                    invoked = true;
                }
            );
            TestHelper.AddCallbacks<int, object, string>(deferred.Promise,
                captureValue: expected,
                onResolveCapture: cv =>
                {
                    Assert.AreEqual(expected, cv);
                    invoked = true;
                }
            );

            deferred.Resolve();
            Promise.Manager.HandleCompletes();

            Assert.AreEqual(true, invoked);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void OnResolvedWillBeInvokedWithCapturedValue1()
        {
            var deferred = Promise.NewDeferred<int>();

            string expected = "expected";
            bool invoked = false;

            TestHelper.AddResolveCallbacks<int, bool, string>(deferred.Promise,
                captureValue: expected,
                onResolveCapture: cv =>
                {
                    Assert.AreEqual(expected, cv);
                    invoked = true;
                }
            );
            TestHelper.AddCallbacks<int, bool, object, string>(deferred.Promise,
                captureValue: expected,
                onResolveCapture: cv =>
                {
                    Assert.AreEqual(expected, cv);
                    invoked = true;
                }
            );

            deferred.Resolve(50);
            Promise.Manager.HandleCompletes();

            Assert.AreEqual(true, invoked);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void OnRejectedWillBeInvokedWithCapturedValue0()
        {
            var deferred = Promise.NewDeferred();

            string expected = "expected";
            bool invoked = false;

            TestHelper.AddCallbacks<int, object, string>(deferred.Promise,
                captureValue: expected,
                onRejectCapture: cv =>
                {
                    Assert.AreEqual(expected, cv);
                    invoked = true;
                },
                onUnknownRejectionCapture: cv =>
                {
                    Assert.AreEqual(expected, cv);
                    invoked = true;
                }
            );

            deferred.Reject("Reject");
            Promise.Manager.HandleCompletes();

            Assert.AreEqual(true, invoked);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void OnRejectedWillBeInvokedWithCapturedValue1()
        {
            var deferred = Promise.NewDeferred<int>();

            string expected = "expected";
            bool invoked = false;

            TestHelper.AddCallbacks<int, bool, object, string>(deferred.Promise,
                captureValue: expected,
                onResolveCapture: cv =>
                {
                    Assert.AreEqual(expected, cv);
                    invoked = true;
                },
                onUnknownRejectionCapture: cv =>
                {
                    Assert.AreEqual(expected, cv);
                    invoked = true;
                }
            );

            deferred.Reject("Reject");
            Promise.Manager.HandleCompletes();

            Assert.AreEqual(true, invoked);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }
    }
}