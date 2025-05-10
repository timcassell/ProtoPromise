#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using NUnit.Framework;
using Proto.Promises;
using System;

namespace ProtoPromise.Tests.APIs
{
    public class ContinuewithTests
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
        public void IfOnContinueIsNullThrow_void()
        {
            var deferred = Promise.NewDeferred();
            var promise = deferred.Promise;

            Assert.Throws<Proto.Promises.ArgumentNullException>(() => promise.ContinueWith(default(Action<Promise.ResultContainer>)));
            Assert.Throws<Proto.Promises.ArgumentNullException>(() => promise.ContinueWith(default(Func<Promise.ResultContainer, bool>)));
            Assert.Throws<Proto.Promises.ArgumentNullException>(() => promise.ContinueWith(default(Func<Promise.ResultContainer, Promise>)));
            Assert.Throws<Proto.Promises.ArgumentNullException>(() => promise.ContinueWith(default(Func<Promise.ResultContainer, Promise<bool>>)));

            deferred.Resolve();
            promise.Forget();
        }

        [Test]
        public void IfOnContinueIsNullThrow_T()
        {
            var deferred = Promise.NewDeferred<int>();
            var promise = deferred.Promise;

            Assert.Throws<Proto.Promises.ArgumentNullException>(() => promise.ContinueWith(default(Action<Promise<int>.ResultContainer>)));
            Assert.Throws<Proto.Promises.ArgumentNullException>(() => promise.ContinueWith(default(Func<Promise<int>.ResultContainer, bool>)));
            Assert.Throws<Proto.Promises.ArgumentNullException>(() => promise.ContinueWith(default(Func<Promise<int>.ResultContainer, Promise>)));
            Assert.Throws<Proto.Promises.ArgumentNullException>(() => promise.ContinueWith(default(Func<Promise<int>.ResultContainer, Promise<bool>>)));

            deferred.Resolve(1);
            promise.Forget();
        }
#endif

        [Test]
        public void OnContinueIsInvokedWhenPromiseIsResolved_void()
        {
            var deferred = Promise.NewDeferred();

            int finallyCount = 0;

            TestHelper.AddContinueCallbacks<int, string>(deferred.Promise,
                onContinue: r => ++finallyCount
            );

            deferred.Resolve();

            Assert.AreEqual(TestHelper.continueVoidCallbacks * 2, finallyCount);
        }

        [Test]
        public void OnContinueIsInvokedWhenPromiseIsResolved_T()
        {
            var deferred = Promise.NewDeferred<int>();

            int finallyCount = 0;

            TestHelper.AddContinueCallbacks<int, int, string>(deferred.Promise,
                onContinue: r => ++finallyCount
            );

            deferred.Resolve(50);

            Assert.AreEqual(TestHelper.continueTCallbacks * 2, finallyCount);
        }

        [Test]
        public void OnContinueStateWhenPromiseIsResolved()
        {
            var deferred = Promise.NewDeferred();

            TestHelper.AddContinueCallbacks<int, string>(deferred.Promise,
                onContinue: r => Assert.AreEqual(r.State, Promise.State.Resolved)
            );

            deferred.Resolve();
        }

        [Test]
        public void OnContinueResultWhenPromiseIsResolved()
        {
            var deferred = Promise.NewDeferred<int>();

            int expected = 50;

            TestHelper.AddContinueCallbacks<int, bool, string>(deferred.Promise,
                onContinue: r =>
                {
                    Assert.AreEqual(r.State, Promise.State.Resolved);
                    Assert.AreEqual(expected, r.Value);
                }
            );

            deferred.Resolve(expected);
        }

        [Test]
        public void OnContinueIsInvokedWhenPromiseIsRejected_void()
        {
            var deferred = Promise.NewDeferred();

            int finallyCount = 0;

            TestHelper.AddContinueCallbacks<int, string>(deferred.Promise,
                onContinue: r => ++finallyCount
            );

            deferred.Reject("Reject");
            Assert.AreEqual(TestHelper.continueVoidCallbacks * 2, finallyCount);
        }

        [Test]
        public void OnContinueIsInvokedWhenPromiseIsRejected_T()
        {
            var deferred = Promise.NewDeferred<int>();

            int finallyCount = 0;

            TestHelper.AddContinueCallbacks<int, int, string>(deferred.Promise,
                onContinue: r => ++finallyCount
            );

            deferred.Reject("Reject");
            Assert.AreEqual(TestHelper.continueTCallbacks * 2, finallyCount);
        }

        [Test]
        public void OnContinueRejectReasonWhenPromiseIsRejected_void()
        {
            var deferred = Promise.NewDeferred();

            string rejection = "Reject";

            TestHelper.AddContinueCallbacks<int, string>(deferred.Promise,
                onContinue: r =>
                {
                    Assert.AreEqual(r.State, Promise.State.Rejected);
                    Assert.AreEqual(rejection, r.Reason);
                }
            );

            deferred.Reject(rejection);
        }

        [Test]
        public void OnContinueRejectReasonWhenPromiseIsRejected_T()
        {
            var deferred = Promise.NewDeferred<int>();

            string rejection = "Reject";

            TestHelper.AddContinueCallbacks<int, int, string>(deferred.Promise,
                onContinue: r =>
                {
                    Assert.AreEqual(r.State, Promise.State.Rejected);
                    Assert.AreEqual(rejection, r.Reason);
                }
            );

            deferred.Reject(rejection);
        }

        [Test]
        public void OnContinueRethrowRejectReasonWhenPromiseIsRejected_void()
        {
            var deferred = Promise.NewDeferred();

            int rejections = 0;
            string rejection = "Reject";

            TestHelper.AddContinueCallbacks<int, string>(deferred.Promise,
                onContinue: r => r.RethrowIfRejected(),
                onCallbackAdded: (ref Promise p) => p.Catch((object e) => { Assert.AreEqual(rejection, e); ++rejections; }).Forget(),
                onCallbackAddedConvert: (ref Promise<int> p) => p.Catch((object e) => { Assert.AreEqual(rejection, e); ++rejections; }).Forget()
            );

            deferred.Reject(rejection);

            Assert.AreEqual(
                TestHelper.continueVoidCallbacks * 2,
                rejections
            );
        }

        [Test]
        public void OnContinueRethrowRejectReasonWhenPromiseIsRejected_T()
        {
            var deferred = Promise.NewDeferred<int>();

            int rejections = 0;
            string rejection = "Reject";

            TestHelper.AddContinueCallbacks<int, int, string>(deferred.Promise,
                onContinue: r => r.RethrowIfRejected(),
                onCallbackAdded: (ref Promise p) => p.Catch((object e) => { Assert.AreEqual(rejection, e); ++rejections; }).Forget(),
                onCallbackAddedConvert: (ref Promise<int> p) => p.Catch((object e) => { Assert.AreEqual(rejection, e); ++rejections; }).Forget()
            );

            deferred.Reject(rejection);

            Assert.AreEqual(
                TestHelper.continueTCallbacks * 2,
                rejections
            );
        }

        [Test]
        public void OnContinueIsInvokedWhenPromiseIsCanceled_void()
        {
            var deferred = Promise.NewDeferred();

            int finallyCount = 0;

            TestHelper.AddContinueCallbacks<int, string>(deferred.Promise,
                onContinue: r => ++finallyCount
            );

            deferred.Cancel();
            Assert.AreEqual(TestHelper.continueVoidCallbacks * 2, finallyCount);
        }

        [Test]
        public void OnContinueIsInvokedWhenPromiseIsCanceled_T()
        {
            var deferred = Promise.NewDeferred<int>();

            int finallyCount = 0;

            TestHelper.AddContinueCallbacks<int, int, string>(deferred.Promise,
                onContinue: r => ++finallyCount
            );

            deferred.Cancel();
            Assert.AreEqual(TestHelper.continueTCallbacks * 2, finallyCount);
        }

        [Test]
        public void OnContinueCancelStateWhenPromiseIsCanceled_void()
        {
            var deferred = Promise.NewDeferred();

            TestHelper.AddContinueCallbacks<int, string>(deferred.Promise,
                onContinue: r =>
                {
                    Assert.AreEqual(r.State, Promise.State.Canceled);
                }
            );

            deferred.Cancel();
        }

        [Test]
        public void OnContinueCancelStateWhenPromiseIsCanceled_T()
        {
            var deferred = Promise.NewDeferred<int>();

            TestHelper.AddContinueCallbacks<int, int, string>(deferred.Promise,
                onContinue: r =>
                {
                    Assert.AreEqual(r.State, Promise.State.Canceled);
                }
            );

            deferred.Cancel();
        }

        [Test]
        public void OnContinueRethrowCancelWhenPromiseIsCanceled_void()
        {
            var deferred = Promise.NewDeferred();

            int cancelCount = 0;

            TestHelper.AddContinueCallbacks<int, string>(deferred.Promise,
                onContinue: r => r.RethrowIfCanceled(),
                onCancel: () => { ++cancelCount; }
            );

            deferred.Cancel();

            Assert.AreEqual(
                TestHelper.continueVoidCallbacks * 2,
                cancelCount
            );
        }

        [Test]
        public void OnContinueRethrowCancelReasonWhenPromiseIsCanceled_T()
        {
            var deferred = Promise.NewDeferred<int>();

            int cancelCount = 0;

            TestHelper.AddContinueCallbacks<int, int, string>(deferred.Promise,
                onContinue: r => r.RethrowIfCanceled(),
                onCancel: () => { ++cancelCount; }
            );

            deferred.Cancel();

            Assert.AreEqual(
                TestHelper.continueTCallbacks * 2,
                cancelCount
            );
        }
    }
}