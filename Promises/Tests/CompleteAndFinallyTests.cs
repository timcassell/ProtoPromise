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
    public class CompleteAndFinallyTests
    {
#if PROMISE_DEBUG
        [Test]
        public void IfOnFinallyIsNullThrow()
        {
            var deferred = Promise.NewDeferred();
            var deferredInt = Promise.NewDeferred<int>();

            Assert.Throws<ArgumentNullException>(() =>
            {
                deferred.Promise.Finally(default(Action));
            });
            Assert.Throws<ArgumentNullException>(() =>
            {
                deferredInt.Promise.Finally(default(Action));
            });

            deferred.Resolve();
            deferredInt.Resolve(0);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletes();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void IfOnCompleteIsNullThrow()
        {
            var deferred = Promise.NewDeferred();
            var deferredInt = Promise.NewDeferred<int>();

            Assert.Throws<ArgumentNullException>(() =>
            {
                deferred.Promise.Complete(default(Action));
            });
            Assert.Throws<ArgumentNullException>(() =>
            {
                deferred.Promise.Complete(default(Func<int>));
            });
            Assert.Throws<ArgumentNullException>(() =>
            {
                deferred.Promise.Complete(default(Func<Promise>));
            });
            Assert.Throws<ArgumentNullException>(() =>
            {
                deferred.Promise.Complete(default(Func<Promise<int>>));
            });
            Assert.Throws<ArgumentNullException>(() =>
            {
                deferredInt.Promise.Complete(default(Action));
            });
            Assert.Throws<ArgumentNullException>(() =>
            {
                deferredInt.Promise.Complete(default(Func<int>));
            });
            Assert.Throws<ArgumentNullException>(() =>
            {
                deferredInt.Promise.Complete(default(Func<Promise>));
            });
            Assert.Throws<ArgumentNullException>(() =>
            {
                deferredInt.Promise.Complete(default(Func<Promise<int>>));
            });
            deferred.Resolve();
            deferredInt.Resolve(0);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletes();
            LogAssert.NoUnexpectedReceived();
        }
#endif

        [Test]
        public void OnFinallyIsInvokedWhenPromiseIsResolved()
        {
            var deferred = Promise.NewDeferred();
            var deferredInt = Promise.NewDeferred<int>();

            bool voidFinallyFired = false;
            bool intFinallyFired = false;

            deferred.Promise.Finally(() => voidFinallyFired = true);
            deferredInt.Promise.Finally(() => intFinallyFired = true);

            deferred.Resolve();
            deferredInt.Resolve(0);

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(true, voidFinallyFired);
            Assert.AreEqual(true, intFinallyFired);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletes();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void OnFinallyIsInvokedWhenPromiseIsRejected()
        {
            var deferred = Promise.NewDeferred();
            var deferredInt = Promise.NewDeferred<int>();

            bool voidFinallyFired = false;
            bool intFinallyFired = false;

            deferred.Promise
                .Finally(() => voidFinallyFired = true)
                .Catch(() => { });
            deferredInt.Promise
                .Finally(() => intFinallyFired = true)
                .Catch(() => { });

            deferred.Reject("Reject");
            deferredInt.Reject("Reject");

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(true, voidFinallyFired);
            Assert.AreEqual(true, intFinallyFired);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletes();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void OnFinallyIsInvokedWhenPromiseIsCanceled()
        {
            var deferred = Promise.NewDeferred();
            var deferredInt = Promise.NewDeferred<int>();

            bool voidFinallyFired = false;
            bool intFinallyFired = false;

            deferred.Promise.Finally(() => voidFinallyFired = true);
            deferredInt.Promise.Finally(() => intFinallyFired = true);

            deferred.Cancel();
            deferredInt.Cancel();

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(true, voidFinallyFired);
            Assert.AreEqual(true, intFinallyFired);

            deferred = Promise.NewDeferred();
            deferredInt = Promise.NewDeferred<int>();

            deferred.Promise.Finally(() => voidFinallyFired = true);
            deferredInt.Promise.Finally(() => voidFinallyFired = intFinallyFired);

            deferred.Cancel("Cancel");
            deferredInt.Cancel("Cancel");

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(true, voidFinallyFired);
            Assert.AreEqual(true, intFinallyFired);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletes();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void OnCompleteIsInvokedWhenPromiseIsResolved()
        {
            var deferred = Promise.NewDeferred();

            int voidFinallyFired = 0;

            TestHelper.AddCompleteCallbacks<int, string>(deferred.Promise,
                onComplete: () => ++voidFinallyFired
            );

            deferred.Resolve();

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(TestHelper.completeCallbacks, voidFinallyFired);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletes();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void OnCompleteIsInvokedWhenPromiseIsRejected()
        {
            var deferred = Promise.NewDeferred();

            int voidFinallyFired = 0;

            TestHelper.AddCompleteCallbacks<int, string>(deferred.Promise,
                onComplete: () => ++voidFinallyFired
            );
            deferred.Promise.Catch(() => { });

            deferred.Reject("Reject");


            Promise.Manager.HandleCompletes();
            Assert.AreEqual(TestHelper.completeCallbacks, voidFinallyFired);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletes();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void OnCompleteIsNotInvokedWhenPromiseIsCanceled()
        {
            var deferred = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();

            int voidFinallyFired = 0;

            TestHelper.AddCompleteCallbacks<int, string>(deferred.Promise,
                onComplete: () => ++voidFinallyFired
            );
            TestHelper.AddCompleteCallbacks<int, string>(deferred2.Promise,
                onComplete: () => ++voidFinallyFired
            );

            deferred.Cancel();
            deferred2.Cancel("Cancel");

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(0, voidFinallyFired);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletes();
            LogAssert.NoUnexpectedReceived();
        }
    }
}
#endif