#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using NUnit.Framework;
using Proto.Promises;
using System;

namespace ProtoPromiseTests.APIs
{
    public class FinallyTests
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

#if PROMISE_DEBUG
        [Test]
        public void IfOnFinallyIsNullThrow_void()
        {
            var deferred = Promise.NewDeferred();
            var promise = deferred.Promise.Preserve();

            Assert.Throws<Proto.Promises.ArgumentNullException>(() =>
            {
                promise.Finally(default(Action));
            });

            deferred.Resolve();

            promise.Forget();
        }

        [Test]
        public void IfOnFinallyIsNullThrow_T()
        {
            var deferred = Promise.NewDeferred<int>();
            var promise = deferred.Promise.Preserve();

            Assert.Throws<Proto.Promises.ArgumentNullException>(() =>
            {
                promise.Finally(default(Action));
            });

            deferred.Resolve(1);

            promise.Forget();
        }
#endif

        [Test]
        public void OnFinallyIsInvokedWhenPromiseIsResolved_void()
        {
            var deferred = Promise.NewDeferred();

            bool invoked = false;

            deferred.Promise
                .Finally(() => invoked = true)
                .Forget();

            deferred.Resolve();

            Assert.IsTrue(invoked);
        }

        [Test]
        public void OnFinallyIsInvokedWhenPromiseIsResolved_T()
        {
            var deferred = Promise.NewDeferred<int>();

            bool invoked = false;

            deferred.Promise
                .Finally(() => invoked = true)
                .Forget();

            deferred.Resolve(1);

            Assert.IsTrue(invoked);
        }

        [Test]
        public void OnFinallyIsInvokedWhenPromiseIsRejected_void()
        {
            var deferred = Promise.NewDeferred();

            bool invoked = false;
            string rejection = "Reject";

            deferred.Promise
                .Finally(() => invoked = true)
                .Catch((string e) => Assert.AreEqual(rejection, e))
                .Forget();

            deferred.Reject(rejection);

            Assert.IsTrue(invoked);
        }

        [Test]
        public void OnFinallyIsInvokedWhenPromiseIsRejected_T()
        {
            var deferred = Promise.NewDeferred<int>();

            bool invoked = false;
            string rejection = "Reject";

            deferred.Promise
                .Finally(() => invoked = true)
                .Catch((string e) => Assert.AreEqual(rejection, e))
                .Forget();

            deferred.Reject(rejection);

            Assert.IsTrue(invoked);
        }

        [Test]
        public void OnFinallyIsInvokedWhenPromiseIsCanceled_void()
        {
            bool repeat = true;
        Repeat:
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred(cancelationSource.Token);

            bool invoked = false;

            deferred.Promise
                .Finally(() => invoked = true)
                .Forget();

            if (repeat)
            {
                cancelationSource.Cancel();
            }
            else
            {
                cancelationSource.Cancel("Cancel");
            }

            Assert.IsTrue(invoked);

            cancelationSource.Dispose();

            if (repeat)
            {
                repeat = false;
                goto Repeat;
            }
        }

        [Test]
        public void OnFinallyIsInvokedWhenPromiseIsCanceled_T()
        {
            bool repeat = true;
        Repeat:
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred<int>(cancelationSource.Token);

            bool invoked = false;

            deferred.Promise
                .Finally(() => invoked = true)
                .Forget();

            if (repeat)
            {
                cancelationSource.Cancel();
            }
            else
            {
                cancelationSource.Cancel("Cancel");
            }

            Assert.IsTrue(invoked);

            cancelationSource.Dispose();

            if (repeat)
            {
                repeat = false;
                goto Repeat;
            }
        }

        [Test]
        public void PromiseIsRejectedWithThrownExceptionWhenOnFinallyThrows_resolved_void()
        {
            var deferred = Promise.NewDeferred();

            bool invoked = false;
            Exception expected = new Exception();

            deferred.Promise
                .Finally(() => { throw expected; })
                .Catch((Exception e) =>
                {
                    Assert.AreEqual(expected, e);
                    invoked = true;
                })
                .Forget();

            deferred.Resolve();

            Assert.IsTrue(invoked);
        }

        [Test]
        public void PromiseIsRejectedWithThrownExceptionWhenOnFinallyThrows_resolved_T()
        {
            var deferred = Promise.NewDeferred<int>();

            bool invoked = false;
            Exception expected = new Exception();

            deferred.Promise
                .Finally(() => { throw expected; })
                .Catch((Exception e) =>
                {
                    Assert.AreEqual(expected, e);
                    invoked = true;
                })
                .Forget();

            deferred.Resolve(1);

            Assert.IsTrue(invoked);
        }

        [Test]
        public void PromiseIsRejectedWithThrownExceptionWhenOnFinallyThrows_rejected_void()
        {
            var deferred = Promise.NewDeferred();

            bool invoked = false;
            string rejectValue = "Reject";
            Exception expected = new Exception();

            deferred.Promise
                .Finally(() => { throw expected; })
                .Catch((Exception e) =>
                {
                    Assert.AreEqual(expected, e);
                    invoked = true;
                })
                .Forget();

            // When the exception thrown in onFinally overwrites the current rejection, the current rejection gets sent to the UncaughtRejectionHandler.
            // So we need to suppress that here and make sure it actually gets sent to it.
            var currentHandler = Promise.Config.UncaughtRejectionHandler;
            bool uncaughtHandled = false;
            Promise.Config.UncaughtRejectionHandler = e =>
            {
                Assert.AreEqual(rejectValue, e.Value);
                uncaughtHandled = true;
            };

            deferred.Reject(rejectValue);

            Assert.IsTrue(invoked);
            Assert.IsTrue(uncaughtHandled);
            Promise.Config.UncaughtRejectionHandler = currentHandler;
        }

        [Test]
        public void PromiseIsRejectedWithThrownExceptionWhenOnFinallyThrows_rejected_T()
        {
            var deferred = Promise.NewDeferred<int>();

            bool invoked = false;
            string rejectValue = "Reject";
            Exception expected = new Exception();

            deferred.Promise
                .Finally(() => { throw expected; })
                .Catch((Exception e) =>
                {
                    Assert.AreEqual(expected, e);
                    invoked = true;
                })
                .Forget();

            // When the exception thrown in onFinally overwrites the current rejection, the current rejection gets sent to the UncaughtRejectionHandler.
            // So we need to suppress that here and make sure it actually gets sent to it.
            var currentHandler = Promise.Config.UncaughtRejectionHandler;
            bool uncaughtHandled = false;
            Promise.Config.UncaughtRejectionHandler = e =>
            {
                Assert.AreEqual(rejectValue, e.Value);
                uncaughtHandled = true;
            };

            deferred.Reject(rejectValue);

            Assert.IsTrue(invoked);
            Assert.IsTrue(uncaughtHandled);
            Promise.Config.UncaughtRejectionHandler = currentHandler;
        }

        [Test]
        public void PromiseIsRejectedWithThrownExceptionWhenOnFinallyThrows_canceled_void()
        {
            bool repeat = true;
            Exception expected = new Exception();
        Repeat:
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred(cancelationSource.Token);

            bool invoked = false;

            deferred.Promise
                .Finally(() => { throw expected; })
                .Catch((Exception e) =>
                {
                    Assert.AreEqual(expected, e);
                    invoked = true;
                })
                .Forget();

            if (repeat)
            {
                cancelationSource.Cancel();
            }
            else
            {
                cancelationSource.Cancel("Cancel");
            }

            Assert.IsTrue(invoked);

            cancelationSource.Dispose();

            if (repeat)
            {
                repeat = false;
                goto Repeat;
            }
        }

        [Test]
        public void PromiseIsRejectedWithThrownExceptionWhenOnFinallyThrows_canceled_T()
        {
            bool repeat = true;
            Exception expected = new Exception();
        Repeat:
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred<int>(cancelationSource.Token);

            bool invoked = false;

            deferred.Promise
                .Finally(() => { throw expected; })
                .Catch((Exception e) =>
                {
                    Assert.AreEqual(expected, e);
                    invoked = true;
                })
                .Forget();

            if (repeat)
            {
                cancelationSource.Cancel();
            }
            else
            {
                cancelationSource.Cancel("Cancel");
            }

            Assert.IsTrue(invoked);

            cancelationSource.Dispose();

            if (repeat)
            {
                repeat = false;
                goto Repeat;
            }
        }
    }
}