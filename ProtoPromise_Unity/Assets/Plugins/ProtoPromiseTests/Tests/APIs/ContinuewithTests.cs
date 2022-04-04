#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using NUnit.Framework;
using Proto.Promises;

namespace ProtoPromiseTests.APIs
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
            var promise = deferred.Promise.Preserve();

            Assert.Throws<ArgumentNullException>(() =>
            {
                promise.ContinueWith(default(Promise.ContinueAction));
            });
            Assert.Throws<ArgumentNullException>(() =>
            {
                promise.ContinueWith(default(Promise.ContinueFunc<int>));
            });
            Assert.Throws<ArgumentNullException>(() =>
            {
                promise.ContinueWith(default(Promise.ContinueFunc<Promise>));
            });
            Assert.Throws<ArgumentNullException>(() =>
            {
                promise.ContinueWith(default(Promise.ContinueFunc<Promise<int>>));
            });
            deferred.Resolve();
            promise.Forget();
        }

        [Test]
        public void IfOnContinueIsNullThrow_T()
        {
            var deferred = Promise.NewDeferred<int>();
            var promise = deferred.Promise.Preserve();

            Assert.Throws<ArgumentNullException>(() =>
            {
                promise.ContinueWith(default(Promise<int>.ContinueAction));
            });
            Assert.Throws<ArgumentNullException>(() =>
            {
                promise.ContinueWith(default(Promise<int>.ContinueFunc<int>));
            });
            Assert.Throws<ArgumentNullException>(() =>
            {
                promise.ContinueWith(default(Promise<int>.ContinueFunc<Promise>));
            });
            Assert.Throws<ArgumentNullException>(() =>
            {
                promise.ContinueWith(default(Promise<int>.ContinueFunc<Promise<int>>));
            });
            deferred.Resolve(1);
            promise.Forget();
        }
#endif

        [Test]
        public void OnContinueIsInvokedWhenPromiseIsResolved_void()
        {
            var deferred = Promise.NewDeferred();
            var promise = deferred.Promise.Preserve();

            int finallyCount = 0;

            TestHelper.AddContinueCallbacks<int, string>(promise,
                onContinue: r => ++finallyCount
            );

            deferred.Resolve();

            Assert.AreEqual(TestHelper.continueVoidCallbacks * 2, finallyCount);

            promise.Forget();
        }

        [Test]
        public void OnContinueIsInvokedWhenPromiseIsResolved_T()
        {
            var deferred = Promise.NewDeferred<int>();
            var promise = deferred.Promise.Preserve();

            int finallyCount = 0;

            TestHelper.AddContinueCallbacks<int, int, string>(promise,
                onContinue: r => ++finallyCount
            );

            deferred.Resolve(50);

            Assert.AreEqual(TestHelper.continueTCallbacks * 2, finallyCount);

            promise.Forget();
        }

        [Test]
        public void OnContinueStateWhenPromiseIsResolved()
        {
            var deferred = Promise.NewDeferred();
            var promise = deferred.Promise.Preserve();

            TestHelper.AddContinueCallbacks<int, string>(promise,
                onContinue: r => Assert.AreEqual(r.State, Promise.State.Resolved)
            );

            deferred.Resolve();

            promise.Forget();
        }

        [Test]
        public void OnContinueResultWhenPromiseIsResolved()
        {
            var deferred = Promise.NewDeferred<int>();
            var promise = deferred.Promise.Preserve();

            int expected = 50;

            TestHelper.AddContinueCallbacks<int, bool, string>(promise,
                onContinue: r =>
                {
                    Assert.AreEqual(r.State, Promise.State.Resolved);
                    Assert.AreEqual(expected, r.Result);
                }
            );

            deferred.Resolve(expected);

            promise.Forget();
        }

        [Test]
        public void OnContinueIsInvokedWhenPromiseIsRejected_void()
        {
            var deferred = Promise.NewDeferred();
            var promise = deferred.Promise.Preserve();

            int finallyCount = 0;

            TestHelper.AddContinueCallbacks<int, string>(promise,
                onContinue: r => ++finallyCount
            );

            deferred.Reject("Reject");
            Assert.AreEqual(TestHelper.continueVoidCallbacks * 2, finallyCount);

            promise.Forget();
        }

        [Test]
        public void OnContinueIsInvokedWhenPromiseIsRejected_T()
        {
            var deferred = Promise.NewDeferred<int>();
            var promise = deferred.Promise.Preserve();

            int finallyCount = 0;

            TestHelper.AddContinueCallbacks<int, int, string>(promise,
                onContinue: r => ++finallyCount
            );

            deferred.Reject("Reject");
            Assert.AreEqual(TestHelper.continueTCallbacks * 2, finallyCount);

            promise.Forget();
        }

        [Test]
        public void OnContinueRejectReasonWhenPromiseIsRejected_void()
        {
            var deferred = Promise.NewDeferred();
            var promise = deferred.Promise.Preserve();

            string rejection = "Reject";

            TestHelper.AddContinueCallbacks<int, string>(promise,
                onContinue: r =>
                {
                    Assert.AreEqual(r.State, Promise.State.Rejected);
                    Assert.AreEqual(rejection, r.RejectContainer.Value);
                }
            );

            deferred.Reject(rejection);

            promise.Forget();
        }

        [Test]
        public void OnContinueRejectReasonWhenPromiseIsRejected_T()
        {
            var deferred = Promise.NewDeferred<int>();
            var promise = deferred.Promise.Preserve();

            string rejection = "Reject";

            TestHelper.AddContinueCallbacks<int, int, string>(promise,
                onContinue: r =>
                {
                    Assert.AreEqual(r.State, Promise.State.Rejected);
                    Assert.AreEqual(rejection, r.RejectContainer.Value);
                }
            );

            deferred.Reject(rejection);

            promise.Forget();
        }

        [Test]
        public void OnContinueRethrowRejectReasonWhenPromiseIsRejected_void()
        {
            var deferred = Promise.NewDeferred();
            var promise = deferred.Promise.Preserve();

            int rejections = 0;
            string rejection = "Reject";

            TestHelper.AddContinueCallbacks<int, string>(promise,
                onContinue: r => r.RethrowIfRejected(),
                onCallbackAdded: (ref Promise p) => p.Catch((object e) => { Assert.AreEqual(rejection, e); ++rejections; }).Forget(),
                onCallbackAddedConvert: (ref Promise<int> p) => p.Catch((object e) => { Assert.AreEqual(rejection, e); ++rejections; }).Forget()
            );

            deferred.Reject(rejection);

            Assert.AreEqual(
                TestHelper.continueVoidCallbacks * 2,
                rejections
            );

            promise.Forget();
        }

        [Test]
        public void OnContinueRethrowRejectReasonWhenPromiseIsRejected_T()
        {
            var deferred = Promise.NewDeferred<int>();
            var promise = deferred.Promise.Preserve();

            int rejections = 0;
            string rejection = "Reject";

            TestHelper.AddContinueCallbacks<int, int, string>(promise,
                onContinue: r => r.RethrowIfRejected(),
                onCallbackAdded: (ref Promise p) => p.Catch((object e) => { Assert.AreEqual(rejection, e); ++rejections; }).Forget(),
                onCallbackAddedConvert: (ref Promise<int> p) => p.Catch((object e) => { Assert.AreEqual(rejection, e); ++rejections; }).Forget()
            );

            deferred.Reject(rejection);

            Assert.AreEqual(
                TestHelper.continueTCallbacks * 2,
                rejections
            );

            promise.Forget();
        }

        [Test]
        public void OnContinueIsInvokedWhenPromiseIsCanceled_void()
        {
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred(cancelationSource.Token);
            var promise = deferred.Promise.Preserve();

            int finallyCount = 0;

            TestHelper.AddContinueCallbacks<int, string>(promise,
                onContinue: r => ++finallyCount
            );

            cancelationSource.Cancel();
            Assert.AreEqual(TestHelper.continueVoidCallbacks * 2, finallyCount);

            cancelationSource.Dispose();
            promise.Forget();
        }

        [Test]
        public void OnContinueIsInvokedWhenPromiseIsCanceled_T()
        {
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred<int>(cancelationSource.Token);
            var promise = deferred.Promise.Preserve();

            int finallyCount = 0;

            TestHelper.AddContinueCallbacks<int, int, string>(promise,
                onContinue: r => ++finallyCount
            );

            cancelationSource.Cancel();
            Assert.AreEqual(TestHelper.continueTCallbacks * 2, finallyCount);

            cancelationSource.Dispose();
            promise.Forget();
        }

        [Test]
        public void OnContinueCancelStateWhenPromiseIsCanceled_void()
        {
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred(cancelationSource.Token);
            var promise = deferred.Promise.Preserve();

            TestHelper.AddContinueCallbacks<int, string>(promise,
                onContinue: r =>
                {
                    Assert.AreEqual(r.State, Promise.State.Canceled);
                }
            );

            cancelationSource.Cancel();

            cancelationSource.Dispose();
            promise.Forget();
        }

        [Test]
        public void OnContinueCancelStateWhenPromiseIsCanceled_T()
        {
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred<int>(cancelationSource.Token);
            var promise = deferred.Promise.Preserve();

            TestHelper.AddContinueCallbacks<int, int, string>(promise,
                onContinue: r =>
                {
                    Assert.AreEqual(r.State, Promise.State.Canceled);
                }
            );

            cancelationSource.Cancel();

            cancelationSource.Dispose();
            promise.Forget();
        }

        [Test]
        public void OnContinueRethrowCancelWhenPromiseIsCanceled_void()
        {
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred(cancelationSource.Token);
            var promise = deferred.Promise.Preserve();

            int cancelCount = 0;

            TestHelper.AddContinueCallbacks<int, string>(promise,
                onContinue: r => r.RethrowIfCanceled(),
                onCancel: () => { ++cancelCount; }
            );

            cancelationSource.Cancel();

            Assert.AreEqual(
                TestHelper.continueVoidCallbacks * 2,
                cancelCount
            );

            cancelationSource.Dispose();
            promise.Forget();
        }

        [Test]
        public void OnContinueRethrowCancelReasonWhenPromiseIsCanceled_T()
        {
            CancelationSource cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred<int>(cancelationSource.Token);
            var promise = deferred.Promise.Preserve();

            int cancelCount = 0;

            TestHelper.AddContinueCallbacks<int, int, string>(promise,
                onContinue: r => r.RethrowIfCanceled(),
                onCancel: () => { ++cancelCount; }
            );

            cancelationSource.Cancel();

            Assert.AreEqual(
                TestHelper.continueTCallbacks * 2,
                cancelCount
            );

            cancelationSource.Dispose();
            promise.Forget();
        }
    }
}