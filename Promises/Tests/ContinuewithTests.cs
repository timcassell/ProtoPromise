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

using System;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Proto.Promises.Tests
{
    public class ContinuewithTests
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
        [Test]
        public void IfOnContinueIsNullThrow()
        {
            var deferred = Promise.NewDeferred();
            var deferredInt = Promise.NewDeferred<int>();

            Assert.Throws<ArgumentNullException>(() =>
            {
                deferred.Promise.ContinueWith(default(Action<Promise.ResultContainer>));
            });
            Assert.Throws<ArgumentNullException>(() =>
            {
                deferred.Promise.ContinueWith(default(Func<Promise.ResultContainer, int>));
            });
            Assert.Throws<ArgumentNullException>(() =>
            {
                deferred.Promise.ContinueWith(default(Func<Promise.ResultContainer, Promise>));
            });
            Assert.Throws<ArgumentNullException>(() =>
            {
                deferred.Promise.ContinueWith(default(Func<Promise.ResultContainer, Promise<int>>));
            });
            Assert.Throws<ArgumentNullException>(() =>
            {
                deferredInt.Promise.ContinueWith(default(Action<Promise<int>.ResultContainer>));
            });
            Assert.Throws<ArgumentNullException>(() =>
            {
                deferredInt.Promise.ContinueWith(default(Func<Promise<int>.ResultContainer, int>));
            });
            Assert.Throws<ArgumentNullException>(() =>
            {
                deferredInt.Promise.ContinueWith(default(Func<Promise<int>.ResultContainer, Promise>));
            });
            Assert.Throws<ArgumentNullException>(() =>
            {
                deferredInt.Promise.ContinueWith(default(Func<Promise<int>.ResultContainer, Promise<int>>));
            });
            deferred.Resolve();
            deferredInt.Resolve(0);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }
#endif

        [Test]
        public void OnContinueIsInvokedWhenPromiseIsResolved()
        {
            var deferred = Promise.NewDeferred();
            var deferredInt = Promise.NewDeferred<int>();

            int voidFinallyFired = 0;
            int intFinallyFired = 0;

            TestHelper.AddContinueCallbacks<int, string>(deferred.Promise,
                onContinue: r => ++voidFinallyFired
            );
            TestHelper.AddContinueCallbacks<int, int, string>(deferredInt.Promise,
                onContinue: r => ++intFinallyFired
            );

            deferred.Resolve();
            deferredInt.Resolve(50);

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(TestHelper.continueVoidCallbacks, voidFinallyFired);
            Assert.AreEqual(TestHelper.continueTCallbacks, intFinallyFired);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void OnContinueResultWhenPromiseIsResolved()
        {
            var deferred = Promise.NewDeferred();
            var deferredInt = Promise.NewDeferred<int>();

            int expected = 50;

            TestHelper.AddContinueCallbacks<int, string>(deferred.Promise,
                onContinue: r => Assert.AreEqual(r.State, Promise.State.Resolved)
            );
            TestHelper.AddContinueCallbacks<int, int, string>(deferredInt.Promise,
                onContinue: r =>
                {
                    Assert.AreEqual(r.State, Promise.State.Resolved);
                    Assert.AreEqual(expected, r.Result);
                }
            );

            deferred.Resolve();
            deferredInt.Resolve(expected);

            Promise.Manager.HandleCompletes();

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void OnContinueIsInvokedWhenPromiseIsRejected()
        {
            var deferred = Promise.NewDeferred();
            var deferredInt = Promise.NewDeferred<int>();

            int voidFinallyFired = 0;
            int intFinallyFired = 0;

            TestHelper.AddContinueCallbacks<int, string>(deferred.Promise,
                onContinue: r => ++voidFinallyFired
            );
            TestHelper.AddContinueCallbacks<int, int, string>(deferredInt.Promise,
                onContinue: r => ++intFinallyFired
            );
            deferred.Promise.Catch(() => { });
            deferredInt.Promise.Catch(() => { });

            deferred.Reject("Reject");
            deferredInt.Reject("Reject");


            Promise.Manager.HandleCompletes();
            Assert.AreEqual(TestHelper.continueVoidCallbacks, voidFinallyFired);
            Assert.AreEqual(TestHelper.continueTCallbacks, intFinallyFired);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void OnContinueRejectReasonWhenPromiseIsRejected()
        {
            var deferred = Promise.NewDeferred();
            var deferredInt = Promise.NewDeferred<int>();

            string rejection = "Reject";

            TestHelper.AddContinueCallbacks<int, string>(deferred.Promise,
                onContinue: r =>
                {
                    Assert.AreEqual(r.State, Promise.State.Rejected);
                    Assert.AreEqual(rejection, r.RejectContainer.Value);
                }
            );
            TestHelper.AddContinueCallbacks<int, int, string>(deferredInt.Promise,
                onContinue: r =>
                {
                    Assert.AreEqual(r.State, Promise.State.Rejected);
                    Assert.AreEqual(rejection, r.RejectContainer.Value);
                }
            );

            deferred.Reject(rejection);
            deferredInt.Reject(rejection);

            Promise.Manager.HandleCompletes();

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void OnContinueRethrowRejectReasonWhenPromiseIsRejected()
        {
            var deferred = Promise.NewDeferred();
            var deferredInt = Promise.NewDeferred<int>();

            int rejections = 0;
            string rejection = "Reject";

            Promise.ResultContainer voidContainer = default(Promise.ResultContainer);
            Promise<int>.ResultContainer intContainer = default(Promise<int>.ResultContainer);

            TestHelper.AddContinueCallbacks<int, string>(deferred.Promise,
                onContinue: r => voidContainer = r,
                promiseToVoid: p => { p.Catch((object e) => { Assert.AreEqual(rejection, e); ++rejections; }); voidContainer.RethrowIfRejected(); },
                promiseToConvert: p => { p.Catch((object e) => { Assert.AreEqual(rejection, e); ++rejections; }); voidContainer.RethrowIfRejected(); return 0; },
                promiseToPromise: p => { p.Catch((object e) => { Assert.AreEqual(rejection, e); ++rejections; }); voidContainer.RethrowIfRejected(); return null; },
                promiseToPromiseConvert: p => { p.Catch((object e) => { Assert.AreEqual(rejection, e); ++rejections; }); voidContainer.RethrowIfRejected(); return null; }
            );
            TestHelper.AddContinueCallbacks<int, int, string>(deferredInt.Promise,
                onContinue: r => intContainer = r,
                promiseToVoid: p => { p.Catch((object e) => { Assert.AreEqual(rejection, e); ++rejections; }); intContainer.RethrowIfRejected(); },
                promiseToConvert: p => { p.Catch((object e) => { Assert.AreEqual(rejection, e); ++rejections; }); intContainer.RethrowIfRejected(); return 0; },
                promiseToPromise: p => { p.Catch((object e) => { Assert.AreEqual(rejection, e); ++rejections; }); intContainer.RethrowIfRejected(); return null; },
                promiseToPromiseConvert: p => { p.Catch((object e) => { Assert.AreEqual(rejection, e); ++rejections; }); intContainer.RethrowIfRejected(); return null; }
            );

            deferred.Reject(rejection);
            deferredInt.Reject(rejection);

            Promise.Manager.HandleCompletes();

            Assert.AreEqual(TestHelper.continueVoidCallbacks + TestHelper.continueTCallbacks,
                rejections);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

#if PROMISE_CANCEL
        [Test]
        public void OnContinueIsInvokedWhenPromiseIsCanceled()
        {
            var deferred = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();

            int voidFinallyFired = 0;
            int intFinallyFired = 0;

            TestHelper.AddContinueCallbacks<int, string>(deferred.Promise,
                onContinue: r => ++voidFinallyFired
            );
            TestHelper.AddContinueCallbacks<int, string>(deferred2.Promise,
                onContinue: r => ++intFinallyFired
            );

            deferred.Cancel();
            deferred2.Cancel("Cancel");

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(TestHelper.continueVoidCallbacks, voidFinallyFired);
            Assert.AreEqual(TestHelper.continueTCallbacks, intFinallyFired);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void OnContinueCancelReasonWhenPromiseIsCanceled0()
        {
            var deferred = Promise.NewDeferred();
            var deferredInt = Promise.NewDeferred<int>();

            string cancelation = "Cancel";

            TestHelper.AddContinueCallbacks<int, string>(deferred.Promise,
                onContinue: r =>
                {
                    Assert.AreEqual(r.State, Promise.State.Canceled);
                    Assert.AreEqual(cancelation, r.CancelContainer.Value);
                }
            );
            TestHelper.AddContinueCallbacks<int, int, string>(deferredInt.Promise,
                onContinue: r =>
                {
                    Assert.AreEqual(r.State, Promise.State.Canceled);
                    Assert.AreEqual(cancelation, r.CancelContainer.Value);
                }
            );

            deferred.Cancel(cancelation);
            deferredInt.Cancel(cancelation);

            Promise.Manager.HandleCompletes();

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void OnContinueCancelReasonWhenPromiseIsCanceled1()
        {
            var deferred = Promise.NewDeferred();
            var deferredInt = Promise.NewDeferred<int>();

            TestHelper.AddContinueCallbacks<int, string>(deferred.Promise,
                onContinue: r =>
                {
                    Assert.AreEqual(r.State, Promise.State.Canceled);
                    Assert.AreEqual(null, r.CancelContainer.ValueType);
                }
            );
            TestHelper.AddContinueCallbacks<int, int, string>(deferredInt.Promise,
                onContinue: r =>
                {
                    Assert.AreEqual(r.State, Promise.State.Canceled);
                    Assert.AreEqual(null, r.CancelContainer.ValueType);
                }
            );

            deferred.Cancel();
            deferredInt.Cancel();

            Promise.Manager.HandleCompletes();

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void OnContinueRethrowCancelReasonWhenPromiseIsCanceled0()
        {
            var deferred = Promise.NewDeferred();
            var deferredInt = Promise.NewDeferred<int>();

            int cancelations = 0;
            string cancelation = "Cancel";

            Promise.ResultContainer voidContainer = default(Promise.ResultContainer);
            Promise<int>.ResultContainer intContainer = default(Promise<int>.ResultContainer);

            TestHelper.AddContinueCallbacks<int, string>(deferred.Promise,
                onContinue: r => voidContainer = r,
                promiseToVoid: p => { p.CatchCancelation(e => { Assert.AreEqual(cancelation, e.Value); ++cancelations; }); voidContainer.RethrowIfCanceled(); },
                promiseToConvert: p => { p.CatchCancelation(e => { Assert.AreEqual(cancelation, e.Value); ++cancelations; }); voidContainer.RethrowIfCanceled(); return 0; },
                promiseToPromise: p => { p.CatchCancelation(e => { Assert.AreEqual(cancelation, e.Value); ++cancelations; }); voidContainer.RethrowIfCanceled(); return null; },
                promiseToPromiseConvert: p => { p.CatchCancelation(e => { Assert.AreEqual(cancelation, e.Value); ++cancelations; }); voidContainer.RethrowIfCanceled(); return null; }
            );
            TestHelper.AddContinueCallbacks<int, int, string>(deferredInt.Promise,
                onContinue: r => intContainer = r,
                promiseToVoid: p => { p.CatchCancelation(e => { Assert.AreEqual(cancelation, e.Value); ++cancelations; }); intContainer.RethrowIfCanceled(); },
                promiseToConvert: p => { p.CatchCancelation(e => { Assert.AreEqual(cancelation, e.Value); ++cancelations; }); intContainer.RethrowIfCanceled(); return 0; },
                promiseToPromise: p => { p.CatchCancelation(e => { Assert.AreEqual(cancelation, e.Value); ++cancelations; }); intContainer.RethrowIfCanceled(); return null; },
                promiseToPromiseConvert: p => { p.CatchCancelation(e => { Assert.AreEqual(cancelation, e.Value); ++cancelations; }); intContainer.RethrowIfCanceled(); return null; }
            );

            deferred.Cancel(cancelation);
            deferredInt.Cancel(cancelation);

            Promise.Manager.HandleCompletes();

            Assert.AreEqual(TestHelper.continueVoidCallbacks + TestHelper.continueTCallbacks,
                cancelations);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void OnContinueRethrowCancelReasonWhenPromiseIsCanceled1()
        {
            var deferred = Promise.NewDeferred();
            var deferredInt = Promise.NewDeferred<int>();

            int cancelations = 0;

            Promise.ResultContainer voidContainer = default(Promise.ResultContainer);
            Promise<int>.ResultContainer intContainer = default(Promise<int>.ResultContainer);

            TestHelper.AddContinueCallbacks<int, string>(deferred.Promise,
                onContinue: r => voidContainer = r,
                promiseToVoid: p => { p.CatchCancelation(e => { Assert.AreEqual(null, e.ValueType); ++cancelations; }); voidContainer.RethrowIfCanceled(); },
                promiseToConvert: p => { p.CatchCancelation(e => { Assert.AreEqual(null, e.ValueType); ++cancelations; }); voidContainer.RethrowIfCanceled(); return 0; },
                promiseToPromise: p => { p.CatchCancelation(e => { Assert.AreEqual(null, e.ValueType); ++cancelations; }); voidContainer.RethrowIfCanceled(); return null; },
                promiseToPromiseConvert: p => { p.CatchCancelation(e => { Assert.AreEqual(null, e.ValueType); ++cancelations; }); voidContainer.RethrowIfCanceled(); return null; }
            );
            TestHelper.AddContinueCallbacks<int, int, string>(deferredInt.Promise,
                onContinue: r => intContainer = r,
                promiseToVoid: p => { p.CatchCancelation(e => { Assert.AreEqual(null, e.ValueType); ++cancelations; }); intContainer.RethrowIfCanceled(); },
                promiseToConvert: p => { p.CatchCancelation(e => { Assert.AreEqual(null, e.ValueType); ++cancelations; }); intContainer.RethrowIfCanceled(); return 0; },
                promiseToPromise: p => { p.CatchCancelation(e => { Assert.AreEqual(null, e.ValueType); ++cancelations; }); intContainer.RethrowIfCanceled(); return null; },
                promiseToPromiseConvert: p => { p.CatchCancelation(e => { Assert.AreEqual(null, e.ValueType); ++cancelations; }); intContainer.RethrowIfCanceled(); return null; }
            );

            deferred.Cancel();
            deferredInt.Cancel();

            Promise.Manager.HandleCompletes();

            Assert.AreEqual(TestHelper.continueVoidCallbacks + TestHelper.continueTCallbacks,
                cancelations);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }
#endif
    }
}