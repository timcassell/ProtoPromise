#if CSHARP_7_3_OR_NEWER

#if !PROTO_PROMISE_PROGRESS_DISABLE
#define PROMISE_PROGRESS
#else
#undef PROMISE_PROGRESS
#endif

using NUnit.Framework;
using Proto.Promises;

namespace ProtoPromiseTests.APIs
{
    public class AwaitTests
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
            Assert.IsFalse(continued);

            deferred.Resolve();
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
            Assert.IsFalse(continued);

            deferred.Resolve(expected);
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
            Assert.IsFalse(continued);

            deferred.Reject(rejectValue);
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
            Assert.IsFalse(continued);

            deferred.Reject(rejectValue);
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
            Assert.IsFalse(continued);

            cancelationSource.Cancel(cancelValue);
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
            Assert.IsFalse(continued);

            cancelationSource.Cancel(cancelValue);
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

            Assert.IsTrue(continued);
        }

        [Test]
        public void ResolveDoubleAwaitedPromiseContinuesExecution()
        {
            var deferred = Promise.NewDeferred();
            var promise = deferred.Promise.Preserve();

            int continuedCount = 0;

            async void Func()
            {
                await promise;
                ++continuedCount;
            }

            Func();
            Func();
            promise.Forget();
            Assert.AreEqual(0, continuedCount);

            deferred.Resolve();
            Assert.AreEqual(2, continuedCount);
        }

        [Test]
        public void ResolveDoubleAwaitedPromiseReturnsValueAndContinuesExecution()
        {
            var deferred = Promise.NewDeferred<int>();
            var promise = deferred.Promise.Preserve();

            int expected = 50;
            int continuedCount = 0;

            async void Func()
            {
                int value = await promise;
                Assert.AreEqual(expected, value);
                ++continuedCount;
            }

            Func();
            Func();
            promise.Forget();
            Assert.AreEqual(0, continuedCount);

            deferred.Resolve(expected);
            Assert.AreEqual(2, continuedCount);
        }

        [Test]
        public void DoubleAwaitAlreadyResolvedPromiseContinuesExecution()
        {
            var promise = Promise.Resolved().Preserve();
            int continuedCount = 0;

            async void Func()
            {
                await promise;
                ++continuedCount;
            }

            Assert.AreEqual(0, continuedCount);
            Func();
            Func();
            promise.Forget();
            Assert.AreEqual(2, continuedCount);

            Assert.AreEqual(2, continuedCount);
        }

        [Test]
        public void DoubleAwaitAlreadyResolvedPromiseReturnsValueAndContinuesExecution()
        {
            int expected = 50;
            var promise = Promise.Resolved(expected).Preserve();
            int continuedCount = 0;

            async void Func()
            {
                int value = await promise;
                Assert.AreEqual(expected, value);
                ++continuedCount;
            }

            Assert.AreEqual(0, continuedCount);
            Func();
            Func();
            promise.Forget();
            Assert.AreEqual(2, continuedCount);

            Assert.AreEqual(2, continuedCount);
        }

        [Test]
        public void RejectDoubleAwaitedPromiseThrows_void()
        {
            var deferred = Promise.NewDeferred();
            var promise = deferred.Promise.Preserve();

            string rejectValue = "Reject";
            int continuedCount = 0;

            async void Func()
            {
                try
                {
                    await promise;
                }
                catch (UnhandledException e)
                {
                    Assert.AreEqual(rejectValue, e.Value);
                    ++continuedCount;
                }
            }

            Func();
            Func();
            promise.Forget();
            Assert.AreEqual(0, continuedCount);

            deferred.Reject(rejectValue);
            Assert.AreEqual(2, continuedCount);
        }

        [Test]
        public void RejectDoubleAwaitedPromiseThrows_T()
        {
            var deferred = Promise.NewDeferred<int>();
            var promise = deferred.Promise.Preserve();

            string rejectValue = "Reject";
            int continuedCount = 0;

            async void Func()
            {
                try
                {
                    int value = await promise;
                }
                catch (UnhandledException e)
                {
                    Assert.AreEqual(rejectValue, e.Value);
                    ++continuedCount;
                }
            }

            Func();
            Func();
            promise.Forget();
            Assert.AreEqual(0, continuedCount);

            deferred.Reject(rejectValue);
            Assert.AreEqual(2, continuedCount);
        }

        [Test]
        public void DoubleAwaitAlreadyRejectedPromiseThrows_void()
        {
            string rejectValue = "Reject";
            var promise = Promise.Rejected(rejectValue).Preserve();
            int continuedCount = 0;

            async void Func()
            {
                try
                {
                    await promise;
                }
                catch (UnhandledException e)
                {
                    Assert.AreEqual(rejectValue, e.Value);
                    ++continuedCount;
                }
            }

            Assert.AreEqual(0, continuedCount);
            Func();
            Func();
            promise.Forget();
            Assert.AreEqual(2, continuedCount);

            Assert.AreEqual(2, continuedCount);
        }

        [Test]
        public void DoubleAwaitAlreadyRejectedPromiseThrows_T()
        {
            string rejectValue = "Reject";
            var promise = Promise<int>.Rejected(rejectValue).Preserve();
            int continuedCount = 0;

            async void Func()
            {
                try
                {
                    int value = await promise;
                }
                catch (UnhandledException e)
                {
                    Assert.AreEqual(rejectValue, e.Value);
                    ++continuedCount;
                }
            }

            Assert.AreEqual(0, continuedCount);
            Func();
            Func();
            promise.Forget();
            Assert.AreEqual(2, continuedCount);

            Assert.AreEqual(2, continuedCount);
        }

        [Test]
        public void CancelDoubleAwaitedPromiseThrowsOperationCanceled_void()
        {
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred(cancelationSource.Token);
            var promise = deferred.Promise.Preserve();

            string cancelValue = "Cancel";
            int continuedCount = 0;

            async void Func()
            {
                try
                {
                    await promise;
                }
                catch (CanceledException e)
                {
                    Assert.AreEqual(cancelValue, e.Value);
                    ++continuedCount;
                }
            }

            Func();
            Func();
            promise.Forget();
            Assert.AreEqual(0, continuedCount);

            cancelationSource.Cancel(cancelValue);
            Assert.AreEqual(2, continuedCount);

            cancelationSource.Dispose();
        }

        [Test]
        public void CancelDoubleAwaitedPromiseThrowsOperationCanceled_T()
        {
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred<int>(cancelationSource.Token);
            var promise = deferred.Promise.Preserve();

            string cancelValue = "Cancel";
            int continuedCount = 0;

            async void Func()
            {
                try
                {
                    int value = await promise;
                }
                catch (CanceledException e)
                {
                    Assert.AreEqual(cancelValue, e.Value);
                    ++continuedCount;
                }
            }

            Func();
            Func();
            promise.Forget();
            Assert.AreEqual(0, continuedCount);

            cancelationSource.Cancel(cancelValue);
            Assert.AreEqual(2, continuedCount);

            cancelationSource.Dispose();
        }

        [Test]
        public void DoubleAwaitAlreadyCanceledPromiseThrowsOperationCanceled_void()
        {
            string cancelValue = "Cancel";
            var promise = Promise.Canceled(cancelValue).Preserve();
            int continuedCount = 0;

            async void Func()
            {
                try
                {
                    await promise;
                }
                catch (CanceledException e)
                {
                    Assert.AreEqual(cancelValue, e.Value);
                    ++continuedCount;
                }
            }

            Assert.AreEqual(0, continuedCount);
            Func();
            Func();
            promise.Forget();
            Assert.AreEqual(2, continuedCount);

            Assert.AreEqual(2, continuedCount);
        }

        [Test]
        public void DoubleAwaitAlreadyCanceledPromiseThrowsOperationCanceled_T()
        {
            string cancelValue = "Cancel";
            var promise = Promise<int>.Canceled(cancelValue).Preserve();
            int continuedCount = 0;

            async void Func()
            {
                try
                {
                    int value = await promise;
                }
                catch (CanceledException e)
                {
                    Assert.AreEqual(cancelValue, e.Value);
                    ++continuedCount;
                }
            }

            Assert.AreEqual(0, continuedCount);
            Func();
            Func();
            promise.Forget();
            Assert.AreEqual(2, continuedCount);

            Assert.AreEqual(2, continuedCount);
        }
    }
}

#endif