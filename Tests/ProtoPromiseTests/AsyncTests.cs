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
    public class AsyncTests
    {
        [SetUp]
        public void Setup()
        {
            TestHelper.Setup();
        }

        [TearDown]
        public void Teardown()
        {
            TestHelper.Cleanup();
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
                .Then(() => resolved = true)
                .Forget();
            Promise.Manager.HandleCompletes();
            Assert.IsFalse(resolved);

            deferred.Resolve();
            Promise.Manager.HandleCompletes();
            Assert.IsTrue(resolved);
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
                })
                .Forget();
            Promise.Manager.HandleCompletes();
            Assert.IsFalse(resolved);

            deferred.Resolve(expected);
            Promise.Manager.HandleCompletes();
            Assert.IsTrue(resolved);
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
                .Then(() => resolved = true)
                .Forget();
            Assert.IsFalse(resolved);

            Promise.Manager.HandleCompletes();
            Assert.IsTrue(resolved);
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
                })
                .Forget();
            Assert.IsFalse(resolved);

            Promise.Manager.HandleCompletes();
            Assert.IsTrue(resolved);
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
                })
                .Forget();
            Promise.Manager.HandleCompletes();
            Assert.IsFalse(rejected);

            deferred.Reject(expected);
            Promise.Manager.HandleCompletes();
            Assert.IsTrue(rejected);
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
                })
                .Forget();
            Promise.Manager.HandleCompletes();
            Assert.IsFalse(rejected);

            deferred.Reject(expected);
            Promise.Manager.HandleCompletes();
            Assert.IsTrue(rejected);
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
                })
                .Forget();
            Assert.IsFalse(rejected);

            Promise.Manager.HandleCompletes();
            Assert.IsTrue(rejected);
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
                })
                .Forget();
            Assert.IsFalse(rejected);

            Promise.Manager.HandleCompletes();
            Assert.IsTrue(rejected);
        }

        [Test]
        public void AsyncPromiseIsRejectedFromThrow3()
        {
            NullReferenceException expected = new NullReferenceException();
            bool rejected = false;

            async Promise Func()
            {
                await Promise.Resolved();
                throw expected;
            }

            Func()
                .Catch((NullReferenceException e) =>
                {
                    Assert.AreEqual(expected, e);
                    rejected = true;
                })
                .Forget();
            Assert.IsFalse(rejected);

            Promise.Manager.HandleCompletes();
            Assert.IsTrue(rejected);
        }

        [Test]
        public void AsyncPromiseIsRejectedFromThrow4()
        {
            NullReferenceException expected = new NullReferenceException();
            bool rejected = false;

            async Promise<int> Func()
            {
                await Promise.Resolved();
                throw expected;
            }

            Func()
                .Catch((NullReferenceException e) =>
                {
                    Assert.AreEqual(expected, e);
                    rejected = true;
                })
                .Forget();
            Assert.IsFalse(rejected);

            Promise.Manager.HandleCompletes();
            Assert.IsTrue(rejected);
        }

        [Test]
        public void AsyncPromiseIsCanceledFromPromise1()
        {
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred(cancelationSource.Token);

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
                })
                .Forget();
            Promise.Manager.HandleCompletes();
            Assert.IsFalse(canceled);

            cancelationSource.Cancel(expected);
            Promise.Manager.HandleCompletes();
            Assert.IsTrue(canceled);

            cancelationSource.Dispose();
        }

        [Test]
        public void AsyncPromiseIsCanceledFromPromise2()
        {
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred<int>(cancelationSource.Token);

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
                })
                .Forget();
            Promise.Manager.HandleCompletes();
            Assert.IsFalse(canceled);

            cancelationSource.Cancel(expected);
            Promise.Manager.HandleCompletes();
            Assert.IsTrue(canceled);

            cancelationSource.Dispose();
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
                    Assert.IsNull(e.ValueType);
                    canceled = true;
                })
                .Forget();
            Assert.IsFalse(canceled);

            Promise.Manager.HandleCompletes();
            Assert.IsTrue(canceled);
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
                    Assert.IsNull(e.ValueType);
                    canceled = true;
                })
                .Forget();
            Assert.IsFalse(canceled);

            Promise.Manager.HandleCompletes();
            Assert.IsTrue(canceled);
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
                })
                .Forget();
            Assert.IsFalse(canceled);

            Promise.Manager.HandleCompletes();
            Assert.IsTrue(canceled);
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
                })
                .Forget();
            Assert.IsFalse(canceled);

            Promise.Manager.HandleCompletes();
            Assert.IsTrue(canceled);
        }

        [Test]
        public void AsyncPromiseIsCanceledFromThrow5()
        {
            bool canceled = false;

            async Promise Func()
            {
                await Promise.Resolved();
                throw new OperationCanceledException();
            }

            Func()
                .CatchCancelation(e =>
                {
                    Assert.IsNull(e.ValueType);
                    canceled = true;
                })
                .Forget();
            Assert.IsFalse(canceled);

            Promise.Manager.HandleCompletes();
            Assert.IsTrue(canceled);
        }

        [Test]
        public void AsyncPromiseIsCanceledFromThrow6()
        {
            bool canceled = false;

            async Promise<int> Func()
            {
                await Promise.Resolved();
                throw new OperationCanceledException();
            }

            Func()
                .CatchCancelation(e =>
                {
                    Assert.IsNull(e.ValueType);
                    canceled = true;
                })
                .Forget();
            Assert.IsFalse(canceled);

            Promise.Manager.HandleCompletes();
            Assert.IsTrue(canceled);
        }

        [Test]
        public void AsyncPromiseIsCanceledFromThrow7()
        {
            string expected = "Cancel";
            bool canceled = false;

            async Promise Func()
            {
                await Promise.Resolved();
                throw Promise.CancelException(expected);
            }

            Func()
                .CatchCancelation(e =>
                {
                    Assert.AreEqual(expected, e.Value);
                    canceled = true;
                })
                .Forget();
            Assert.IsFalse(canceled);

            Promise.Manager.HandleCompletes();
            Assert.IsTrue(canceled);
        }

        [Test]
        public void AsyncPromiseIsCanceledFromThrow8()
        {
            string expected = "Cancel";
            bool canceled = false;

            async Promise<int> Func()
            {
                await Promise.Resolved();
                throw Promise.CancelException(expected);
            }

            Func()
                .CatchCancelation(e =>
                {
                    Assert.AreEqual(expected, e.Value);
                    canceled = true;
                })
                .Forget();
            Assert.IsFalse(canceled);

            Promise.Manager.HandleCompletes();
            Assert.IsTrue(canceled);
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
                .Then(() => resolved = true)
                .Forget();
            Promise.Manager.HandleCompletes();
            Assert.IsFalse(continued1);
            Assert.IsFalse(continued2);
            Assert.IsFalse(resolved);

            deferred1.Resolve();
            Promise.Manager.HandleCompletes();
            Assert.IsTrue(continued1);
            Assert.IsFalse(continued2);
            Assert.IsFalse(resolved);

            deferred2.Resolve();
            Promise.Manager.HandleCompletes();
            Assert.IsTrue(continued1);
            Assert.IsTrue(continued2);
            Assert.IsTrue(resolved);
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
                })
                .Forget();
            Promise.Manager.HandleCompletes();
            Assert.IsFalse(continued1);
            Assert.IsFalse(continued2);
            Assert.IsFalse(resolved);

            deferred1.Resolve();
            Promise.Manager.HandleCompletes();
            Assert.IsTrue(continued1);
            Assert.IsFalse(continued2);
            Assert.IsFalse(resolved);

            deferred2.Resolve("Some string");
            Promise.Manager.HandleCompletes();
            Assert.IsTrue(continued1);
            Assert.IsTrue(continued2);
            Assert.IsTrue(resolved);
        }
    }
}

#endif