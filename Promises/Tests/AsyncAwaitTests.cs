#if CSHARP_7_OR_LATER

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
using Proto.Promises.Await;

namespace Proto.Promises.Tests
{
    public class AsyncAwaitTests
    {
        [Test]
        public void ResolveAwaitedPromiseContinuesExecution()
        {
            var deferred = Promise.NewDeferred();

            bool continued = false;

            async void Func()
            {
                await deferred.Promise;
                continued = true;
            }

            Func();
            Assert.AreEqual(false, continued);

            deferred.Resolve();
            Promise.Manager.HandleCompletes();
            Assert.AreEqual(true, continued);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void AwaitAlreadyResolvedPromiseContinuesExecution()
        {
            bool continued = false;

            async void Func()
            {
                await Promise.Resolved();
                continued = true;
            }

            Assert.AreEqual(false, continued);
            Func();
            Assert.AreEqual(true, continued);

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(true, continued);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void RejectAwaitedPromiseThrows()
        {
            var deferred = Promise.NewDeferred();

            bool continued = false;
            string rejectValue = "Reject";

            async void Func()
            {
                try
                {
                    await deferred.Promise;
                }
                catch (Promise.UnhandledException e)
                {
                    Assert.AreEqual(rejectValue, e.Value);
                    continued = true;
                }
            }

            Func();
            Assert.AreEqual(false, continued);

            deferred.Reject(rejectValue);
            Promise.Manager.HandleCompletes();
            Assert.AreEqual(true, continued);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void AwaitAlreadyRejectedPromiseThrows()
        {
            bool continued = false;
            string rejectValue = "Reject";

            async void Func()
            {
                try
                {
                    await Promise.Rejected(rejectValue);
                }
                catch (Promise.UnhandledException e)
                {
                    Assert.AreEqual(rejectValue, e.Value);
                    continued = true;
                }
            }

            Assert.AreEqual(false, continued);
            Func();
            Assert.AreEqual(true, continued);

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(true, continued);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

#if PROMISE_CANCEL
        [Test]
        public void CancelAwaitedPromiseThrowsOperationCanceled()
        {
            var deferred = Promise.NewDeferred();

            bool continued = false;

            async void Func()
            {
                try
                {
                    await deferred.Promise;
                }
                catch (OperationCanceledException)
                {
                    continued = true;
                }
            }

            Func();
            Assert.AreEqual(false, continued);

            deferred.Cancel();
            Promise.Manager.HandleCompletes();
            Assert.AreEqual(true, continued);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void AwaitAlreadyCanceledPromiseThrowsOperationCanceled()
        {
            bool continued = false;

            async void Func()
            {
                try
                {
                    await Promise.Canceled();
                }
                catch (OperationCanceledException)
                {
                    continued = true;
                }
            }

            Assert.AreEqual(false, continued);
            Func();
            Assert.AreEqual(true, continued);

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(true, continued);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }
#endif

        [Test]
        public void AsyncPromiseIsResolved()
        {
            var deferred = Promise.NewDeferred();

            bool resolved = false;

            async Promise Func()
            {
                await deferred.Promise;
            }

            Func()
                .Then(() => resolved = true);
            Assert.AreEqual(false, resolved);

            deferred.Resolve();
            Promise.Manager.HandleCompletes();
            Assert.AreEqual(true, resolved);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void AsyncPromiseIsAlreadyResolved()
        {
            bool resolved = false;

            async Promise Func()
            {
                return;
            }

            Func()
                .Then(() => resolved = true);
            Assert.AreEqual(false, resolved);

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(true, resolved);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }
    }
}

#endif