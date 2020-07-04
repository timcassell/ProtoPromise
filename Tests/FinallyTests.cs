#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using System;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Proto.Promises.Tests
{
    public class FinallyTests
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
            Promise.Manager.HandleCompletesAndProgress();
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
            Promise.Manager.HandleCompletesAndProgress();
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
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void OnFinallyIsInvokedWhenPromiseIsCanceled()
        {
            CancelationSource cancelationSource1 = CancelationSource.New();
            var deferred = Promise.NewDeferred(cancelationSource1.Token);
            CancelationSource cancelationSource2 = CancelationSource.New();
            var deferredInt = Promise.NewDeferred<int>(cancelationSource2.Token);

            bool voidFinallyFired = false;
            bool intFinallyFired = false;

            deferred.Promise.Finally(() => voidFinallyFired = true);
            deferredInt.Promise.Finally(() => intFinallyFired = true);

            cancelationSource1.Cancel();
            cancelationSource2.Cancel();

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(true, voidFinallyFired);
            Assert.AreEqual(true, intFinallyFired);

            cancelationSource1.Dispose();
            cancelationSource2.Dispose();
            voidFinallyFired = false;
            intFinallyFired = false;

            cancelationSource1 = CancelationSource.New();
            deferred = Promise.NewDeferred(cancelationSource1.Token);
            cancelationSource2 = CancelationSource.New();
            deferredInt = Promise.NewDeferred<int>(cancelationSource2.Token);

            deferred.Promise.Finally(() => voidFinallyFired = true);
            deferredInt.Promise.Finally(() => intFinallyFired = true);

            cancelationSource1.Cancel("Cancel");
            cancelationSource2.Cancel("Cancel");

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(true, voidFinallyFired);
            Assert.AreEqual(true, intFinallyFired);

            // Clean up.
            cancelationSource1.Dispose();
            cancelationSource2.Dispose();
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }
    }
}