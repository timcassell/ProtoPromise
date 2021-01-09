#if CSHARP_7_OR_LATER

#if !PROTO_PROMISE_PROGRESS_DISABLE
#define PROMISE_PROGRESS
#else
#undef PROMISE_PROGRESS
#endif

#pragma warning disable IDE0062 // Make local function 'static'

using System;
using NUnit.Framework;

namespace Proto.Promises.Tests
{
    public class AwaitTests
    {
        [TearDown]
        public void Teardown()
        {
            TestHelper.Cleanup();
        }

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
            Promise.Manager.HandleCompletes();
            Assert.IsFalse(continued);

            deferred.Resolve();
            Promise.Manager.HandleCompletes();
            Assert.IsTrue(continued);
        }

        [Test]
        public void ResolveAwaitedPromiseReturnsValueAndContinuesExecution()
        {
            var deferred = Promise.NewDeferred<int>();

            int expected = 50;
            bool continued = false;

            async void Func()
            {
                int value = await deferred.Promise;
                Assert.AreEqual(expected, value);
                continued = true;
            }

            Func();
            Promise.Manager.HandleCompletes();
            Assert.IsFalse(continued);

            deferred.Resolve(expected);
            Promise.Manager.HandleCompletes();
            Assert.IsTrue(continued);
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

            Assert.IsFalse(continued);
            Func();
            Assert.IsTrue(continued);

            Promise.Manager.HandleCompletes();
            Assert.IsTrue(continued);
        }

        [Test]
        public void AwaitAlreadyResolvedPromiseReturnsValueAndContinuesExecution()
        {
            int expected = 50;
            bool continued = false;

            async void Func()
            {
                int value = await Promise.Resolved(expected);
                Assert.AreEqual(expected, value);
                continued = true;
            }

            Assert.IsFalse(continued);
            Func();
            Assert.IsTrue(continued);

            Promise.Manager.HandleCompletes();
            Assert.IsTrue(continued);
        }

        [Test]
        public void RejectAwaitedPromiseThrows1()
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
                catch (UnhandledException e)
                {
                    Assert.AreEqual(rejectValue, e.Value);
                    continued = true;
                }
            }

            Func();
            Promise.Manager.HandleCompletes();
            Assert.IsFalse(continued);

            deferred.Reject(rejectValue);
            Promise.Manager.HandleCompletes();
            Assert.IsTrue(continued);
        }

        [Test]
        public void RejectAwaitedPromiseThrows2()
        {
            var deferred = Promise.NewDeferred<int>();

            bool continued = false;
            string rejectValue = "Reject";

            async void Func()
            {
                try
                {
                    int value = await deferred.Promise;
                }
                catch (UnhandledException e)
                {
                    Assert.AreEqual(rejectValue, e.Value);
                    continued = true;
                }
            }

            Func();
            Promise.Manager.HandleCompletes();
            Assert.IsFalse(continued);

            deferred.Reject(rejectValue);
            Promise.Manager.HandleCompletes();
            Assert.IsTrue(continued);
        }

        [Test]
        public void AwaitAlreadyRejectedPromiseThrows1()
        {
            bool continued = false;
            string rejectValue = "Reject";

            async void Func()
            {
                try
                {
                    await Promise.Rejected(rejectValue);
                }
                catch (UnhandledException e)
                {
                    Assert.AreEqual(rejectValue, e.Value);
                    continued = true;
                }
            }

            Assert.IsFalse(continued);
            Func();
            Assert.IsTrue(continued);

            Promise.Manager.HandleCompletes();
            Assert.IsTrue(continued);
        }

        [Test]
        public void AwaitAlreadyRejectedPromiseThrows2()
        {
            bool continued = false;
            string rejectValue = "Reject";

            async void Func()
            {
                try
                {
                    int value = await Promise.Rejected<int, string>(rejectValue);
                }
                catch (UnhandledException e)
                {
                    Assert.AreEqual(rejectValue, e.Value);
                    continued = true;
                }
            }

            Assert.IsFalse(continued);
            Func();
            Assert.IsTrue(continued);

            Promise.Manager.HandleCompletes();
            Assert.IsTrue(continued);
        }

        [Test]
        public void CancelAwaitedPromiseThrowsOperationCanceled1()
        {
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred(cancelationSource.Token);

            bool continued = false;
            string cancelValue = "Cancel";

            async void Func()
            {
                try
                {
                    await deferred.Promise;
                }
                catch (CanceledException e)
                {
                    Assert.AreEqual(cancelValue, e.Value);
                    continued = true;
                }
            }

            Func();
            Promise.Manager.HandleCompletes();
            Assert.IsFalse(continued);

            cancelationSource.Cancel(cancelValue);
            Promise.Manager.HandleCompletes();
            Assert.IsTrue(continued);

            cancelationSource.Dispose();
        }

        [Test]
        public void CancelAwaitedPromiseThrowsOperationCanceled2()
        {
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred<int>(cancelationSource.Token);

            string cancelValue = "Cancel";
            bool continued = false;

            async void Func()
            {
                try
                {
                    int value = await deferred.Promise;
                }
                catch (CanceledException e)
                {
                    Assert.AreEqual(cancelValue, e.Value);
                    continued = true;
                }
            }

            Func();
            Promise.Manager.HandleCompletes();
            Assert.IsFalse(continued);

            cancelationSource.Cancel(cancelValue);
            Promise.Manager.HandleCompletes();
            Assert.IsTrue(continued);

            cancelationSource.Dispose();
        }

        [Test]
        public void AwaitAlreadyCanceledPromiseThrowsOperationCanceled1()
        {
            string cancelValue = "Cancel";
            bool continued = false;

            async void Func()
            {
                try
                {
                    await Promise.Canceled(cancelValue);
                }
                catch (CanceledException e)
                {
                    Assert.AreEqual(cancelValue, e.Value);
                    continued = true;
                }
            }

            Assert.IsFalse(continued);
            Func();
            Assert.IsTrue(continued);

            Promise.Manager.HandleCompletes();
            Assert.IsTrue(continued);
        }

        [Test]
        public void AwaitAlreadyCanceledPromiseThrowsOperationCanceled2()
        {
            string cancelValue = "Cancel";
            bool continued = false;

            async void Func()
            {
                try
                {
                    int value = await Promise.Canceled<int, string>(cancelValue);
                }
                catch (CanceledException e)
                {
                    Assert.AreEqual(cancelValue, e.Value);
                    continued = true;
                }
            }

            Assert.IsFalse(continued);
            Func();
            Assert.IsTrue(continued);

            Promise.Manager.HandleCompletes();
            Assert.IsTrue(continued);
        }
    }
}

#endif