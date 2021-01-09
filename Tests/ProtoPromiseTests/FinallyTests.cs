#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using System;
using NUnit.Framework;

namespace Proto.Promises.Tests
{
    public class FinallyTests
    {
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

            Assert.Throws<ArgumentNullException>(() =>
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

            Assert.Throws<ArgumentNullException>(() =>
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

            Promise.Manager.HandleCompletes();
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

            Promise.Manager.HandleCompletes();
            Assert.IsTrue(invoked);
        }

        [Test]
        public void OnFinallyIsInvokedWhenPromiseIsRejected_void()
        {
            var deferred = Promise.NewDeferred();

            bool invoked = false;

            deferred.Promise
                .Finally(() => invoked = true)
                .Catch(() => { })
                .Forget();

            deferred.Reject("Reject");

            Promise.Manager.HandleCompletes();
            Assert.IsTrue(invoked);
        }

        [Test]
        public void OnFinallyIsInvokedWhenPromiseIsRejected_T()
        {
            var deferred = Promise.NewDeferred<int>();

            bool invoked = false;

            deferred.Promise
                .Finally(() => invoked = true)
                .Catch(() => { })
                .Forget();

            deferred.Reject("Reject");

            Promise.Manager.HandleCompletes();
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

            Promise.Manager.HandleCompletes();
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