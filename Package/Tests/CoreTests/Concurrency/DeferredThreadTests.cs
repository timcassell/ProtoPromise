#if !UNITY_WEBGL

#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using NUnit.Framework;
using Proto.Promises;

namespace ProtoPromise.Tests.Concurrency
{
    public class DeferredThreadTests
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
        public void DeferredMayResolveOnSeparateThread_void0()
        {
            bool invoked = false;
            var deferred = Promise.NewDeferred();
            deferred.Promise
                .Then(() => { invoked = true; })
                .Forget();

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteSingleAction(() => deferred.Resolve());

            Assert.IsTrue(invoked);
        }

        [Test]
        public void DeferredMayResolveOnSeparateThread_void1()
        {
            bool invoked = false;
            var cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred();
            deferred.Promise
                .Then(() => { invoked = true; })
                .Forget();

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteSingleAction(() => deferred.Resolve());

            Assert.IsTrue(invoked);
            cancelationSource.Dispose();
        }

        [Test]
        public void DeferredMayResolveOnSeparateThread_T0()
        {
            bool invoked = false;
            var deferred = Promise.NewDeferred<int>();
            deferred.Promise
                .Then(v => { invoked = true; })
                .Forget();

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteSingleAction(() => deferred.Resolve(1));

            Assert.IsTrue(invoked);
        }

        [Test]
        public void DeferredMayResolveOnSeparateThread_T1()
        {
            bool invoked = false;
            var cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred<int>();
            deferred.Promise
                .Then(v => { invoked = true; })
                .Forget();

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteSingleAction(() => deferred.Resolve(1));

            Assert.IsTrue(invoked);
            cancelationSource.Dispose();
        }

        [Test]
        public void DeferredMayRejectOnSeparateThread_void0()
        {
            bool invoked = false;
            var deferred = Promise.NewDeferred();
            deferred.Promise
                .Catch(() => { invoked = true; })
                .Forget();

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteSingleAction(() => deferred.Reject("Reject"));

            Assert.IsTrue(invoked);
        }

        [Test]
        public void DeferredMayRejectOnSeparateThread_void1()
        {
            bool invoked = false;
            var cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred();
            deferred.Promise
                .Catch(() => { invoked = true; })
                .Forget();

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteSingleAction(() => deferred.Reject("Reject"));

            Assert.IsTrue(invoked);
            cancelationSource.Dispose();
        }

        [Test]
        public void DeferredMayRejectOnSeparateThread_T0()
        {
            bool invoked = false;
            var deferred = Promise.NewDeferred<int>();
            deferred.Promise
                .Catch(() => { invoked = true; })
                .Forget();

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteSingleAction(() => deferred.Reject("Reject"));

            Assert.IsTrue(invoked);
        }

        [Test]
        public void DeferredMayRejectOnSeparateThread_T1()
        {
            bool invoked = false;
            var cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred<int>();
            deferred.Promise
                .Catch(() => { invoked = true; })
                .Forget();

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteSingleAction(() => deferred.Reject("Reject"));

            Assert.IsTrue(invoked);
            cancelationSource.Dispose();
        }

        [Test]
        public void DeferredMayBeCanceledOnSeparateThread_void()
        {
            bool invoked = false;
            var deferred = Promise.NewDeferred();
            deferred.Promise
                .CatchCancelation(() => { invoked = true; })
                .Forget();

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteSingleAction(() => deferred.Cancel());

            Assert.IsTrue(invoked);
        }

        [Test]
        public void DeferredMayBeCanceledOnSeparateThread_T()
        {
            bool invoked = false;
            var deferred = Promise.NewDeferred<int>();
            deferred.Promise
                .CatchCancelation(() => { invoked = true; })
                .Forget();

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteSingleAction(() => deferred.Cancel());

            Assert.IsTrue(invoked);
        }
    }
}

#endif // !UNITY_WEBGL