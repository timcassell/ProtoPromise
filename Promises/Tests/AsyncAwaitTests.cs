#if CSHARP_7_OR_LATER

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
    public class AsyncAwaitTests
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
            Assert.AreEqual(false, continued);

            deferred.Resolve(expected);
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
        public void CancelAwaitedPromiseThrowsOperationCanceled1()
        {
            var deferred = Promise.NewDeferred();

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
            Assert.AreEqual(false, continued);

            deferred.Cancel(cancelValue);
            Promise.Manager.HandleCompletes();
            Assert.AreEqual(true, continued);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void CancelAwaitedPromiseThrowsOperationCanceled2()
        {
            var deferred = Promise.NewDeferred<int>();

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
            Assert.AreEqual(false, continued);

            deferred.Cancel(cancelValue);
            Promise.Manager.HandleCompletes();
            Assert.AreEqual(true, continued);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
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
        public void AsyncPromiseIsResolvedFromPromise()
        {
            var deferred = Promise.NewDeferred();

            bool resolved = false;

            async Promise Func()
            {
                await deferred.Promise;
            }

            Func()
                .Then(() => resolved = true);
            Promise.Manager.HandleCompletes();
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
        public void AsyncPromiseIsResolvedFromPromiseWithValue()
        {
            var deferred = Promise.NewDeferred<int>();

            int expected = 50;
            bool resolved = false;

            async Promise<int> Func()
            {
                return await deferred.Promise;
            }

            Func()
                .Then(value =>
                {
                    Assert.AreEqual(expected, value);
                    resolved = true;
                });
            Promise.Manager.HandleCompletes();
            Assert.AreEqual(false, resolved);

            deferred.Resolve(expected);
            Promise.Manager.HandleCompletes();
            Assert.AreEqual(true, resolved);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void AsyncPromiseIsResolvedWithoutAwait()
        {
            bool resolved = false;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
            async Promise Func()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
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

        [Test]
        public void AsyncPromiseIsResolvedWithValueWithoutAwait()
        {
            int expected = 50;
            bool resolved = false;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
            async Promise<int> Func()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
            {
                return expected;
            }

            Func()
                .Then(value =>
                {
                    Assert.AreEqual(expected, value);
                    resolved = true;
                });
            Assert.AreEqual(false, resolved);

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(true, resolved);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void AsyncPromiseIsRejectedFromPromise1()
        {
            var deferred = Promise.NewDeferred();

            NullReferenceException expected = new NullReferenceException();
            bool rejected = false;

            async Promise Func()
            {
                await deferred.Promise;
            }

            Func()
                .Catch((NullReferenceException e) =>
                {
                    Assert.AreEqual(expected, e);
                    rejected = true;
                });
            Promise.Manager.HandleCompletes();
            Assert.AreEqual(false, rejected);

            deferred.Reject(expected);
            Promise.Manager.HandleCompletes();
            Assert.AreEqual(true, rejected);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void AsyncPromiseIsRejectedFromPromise2()
        {
            var deferred = Promise.NewDeferred<int>();

            NullReferenceException expected = new NullReferenceException();
            bool rejected = false;

            async Promise<int> Func()
            {
                return await deferred.Promise;
            }

            Func()
                .Catch((NullReferenceException e) =>
                {
                    Assert.AreEqual(expected, e);
                    rejected = true;
                });
            Promise.Manager.HandleCompletes();
            Assert.AreEqual(false, rejected);

            deferred.Reject(expected);
            Promise.Manager.HandleCompletes();
            Assert.AreEqual(true, rejected);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void AsyncPromiseIsRejectedFromThrow1()
        {
            NullReferenceException expected = new NullReferenceException();
            bool rejected = false;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
            async Promise Func()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
            {
                throw expected;
            }

            Func()
                .Catch((NullReferenceException e) =>
                {
                    Assert.AreEqual(expected, e);
                    rejected = true;
                });
            Assert.AreEqual(false, rejected);

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(true, rejected);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void AsyncPromiseIsRejectedFromThrow2()
        {
            NullReferenceException expected = new NullReferenceException();
            bool rejected = false;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
            async Promise<int> Func()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
            {
                throw expected;
            }

            Func()
                .Catch((NullReferenceException e) =>
                {
                    Assert.AreEqual(expected, e);
                    rejected = true;
                });
            Assert.AreEqual(false, rejected);

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(true, rejected);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void AsyncPromiseIsCanceledFromPromise1()
        {
            var deferred = Promise.NewDeferred();

            string expected = "Cancel";
            bool canceled = false;

            async Promise Func()
            {
                await deferred.Promise;
            }

            Func()
                .CatchCancelation(e =>
                {
                    Assert.AreEqual(expected, e.Value);
                    canceled = true;
                });
            Promise.Manager.HandleCompletes();
            Assert.AreEqual(false, canceled);

            deferred.Cancel(expected);
            Promise.Manager.HandleCompletes();
            Assert.AreEqual(true, canceled);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void AsyncPromiseIsCanceledFromPromise2()
        {
            var deferred = Promise.NewDeferred<int>();

            string expected = "Cancel";
            bool canceled = false;

            async Promise<int> Func()
            {
                return await deferred.Promise;
            }

            Func()
                .CatchCancelation(e =>
                {
                    Assert.AreEqual(expected, e.Value);
                    canceled = true;
                });
            Promise.Manager.HandleCompletes();
            Assert.AreEqual(false, canceled);

            deferred.Cancel(expected);
            Promise.Manager.HandleCompletes();
            Assert.AreEqual(true, canceled);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void AsyncPromiseIsCanceledFromThrow1()
        {
            bool canceled = false;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
            async Promise Func()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
            {
                throw new OperationCanceledException();
            }

            Func()
                .CatchCancelation(e =>
                {
                    Assert.AreEqual(null, e.ValueType);
                    canceled = true;
                });
            Assert.AreEqual(false, canceled);

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(true, canceled);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void AsyncPromiseIsCanceledFromThrow2()
        {
            bool canceled = false;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
            async Promise<int> Func()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
            {
                throw new OperationCanceledException();
            }

            Func()
                .CatchCancelation(e =>
                {
                    Assert.AreEqual(null, e.ValueType);
                    canceled = true;
                });
            Assert.AreEqual(false, canceled);

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(true, canceled);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void AsyncPromiseIsCanceledFromThrow3()
        {
            string expected = "Cancel";
            bool canceled = false;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
            async Promise Func()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
            {
                throw Promise.CancelException(expected);
            }

            Func()
                .CatchCancelation(e =>
                {
                    Assert.AreEqual(expected, e.Value);
                    canceled = true;
                });
            Assert.AreEqual(false, canceled);

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(true, canceled);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void AsyncPromiseIsCanceledFromThrow4()
        {
            string expected = "Cancel";
            bool canceled = false;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
            async Promise<int> Func()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
            {
                throw Promise.CancelException(expected);
            }

            Func()
                .CatchCancelation(e =>
                {
                    Assert.AreEqual(expected, e.Value);
                    canceled = true;
                });
            Assert.AreEqual(false, canceled);

            Promise.Manager.HandleCompletes();
            Assert.AreEqual(true, canceled);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void AsyncPromiseCanMultipleAwait1()
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred();

            bool continued1 = false;
            bool continued2 = false;
            bool resolved = false;

            async Promise Func()
            {
                await deferred1.Promise;
                continued1 = true;
                await deferred2.Promise;
                continued2 = true;
            }

            Func()
                .Then(() => resolved = true);
            Promise.Manager.HandleCompletes();
            Assert.AreEqual(false, continued1);
            Assert.AreEqual(false, continued2);
            Assert.AreEqual(false, resolved);

            deferred1.Resolve();
            Promise.Manager.HandleCompletes();
            Assert.AreEqual(true, continued1);
            Assert.AreEqual(false, continued2);
            Assert.AreEqual(false, resolved);

            deferred2.Resolve();
            Promise.Manager.HandleCompletes();
            Assert.AreEqual(true, continued1);
            Assert.AreEqual(true, continued2);
            Assert.AreEqual(true, resolved);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void AsyncPromiseCanMultipleAwait2()
        {
            var deferred1 = Promise.NewDeferred();
            var deferred2 = Promise.NewDeferred<string>();

            bool continued1 = false;
            bool continued2 = false;
            bool resolved = false;
            int expected = 50;

            async Promise<int> Func()
            {
                await deferred1.Promise;
                continued1 = true;
                var value = await deferred2.Promise;
                continued2 = true;
                return expected;
            }

            Func()
                .Then(value =>
                {
                    Assert.AreEqual(expected, value);
                    resolved = true;
                });
            Promise.Manager.HandleCompletes();
            Assert.AreEqual(false, continued1);
            Assert.AreEqual(false, continued2);
            Assert.AreEqual(false, resolved);

            deferred1.Resolve();
            Promise.Manager.HandleCompletes();
            Assert.AreEqual(true, continued1);
            Assert.AreEqual(false, continued2);
            Assert.AreEqual(false, resolved);

            deferred2.Resolve("Some string");
            Promise.Manager.HandleCompletes();
            Assert.AreEqual(true, continued1);
            Assert.AreEqual(true, continued2);
            Assert.AreEqual(true, resolved);

            // Clean up.
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
            LogAssert.NoUnexpectedReceived();
        }
    }
}

#endif