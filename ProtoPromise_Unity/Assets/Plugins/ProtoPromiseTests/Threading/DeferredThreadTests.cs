#if !UNITY_WEBGL

#if !PROTO_PROMISE_PROGRESS_DISABLE
#define PROMISE_PROGRESS
#else
#undef PROMISE_PROGRESS
#endif

using NUnit.Framework;
using Proto.Promises;

namespace ProtoPromiseTests.Threading
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

#if PROMISE_PROGRESS
        [Test]
        public void DeferredMayReportProgressOnSeparateThread_void(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType,
            [Values] bool withCancelationToken)
        {
            var cancelationSource = CancelationSource.New();
            var deferred = withCancelationToken
                ? Promise.NewDeferred(cancelationSource.Token)
                : Promise.NewDeferred();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);

            deferred.Promise
                .SubscribeProgress(progressHelper)
                .Forget();

            progressHelper.AssertCurrentProgress(0f);

            progressHelper.PrepareForInvoke();
            new ThreadHelper().ExecuteSingleAction(() => deferred.ReportProgress(0.1f));
            progressHelper.AssertCurrentProgress(0.1f);

            deferred.Resolve();
            cancelationSource.Dispose();
        }

        [Test]
        public void DeferredMayReportProgressOnSeparateThread_T(
            [Values] ProgressType progressType,
            [Values] SynchronizationType synchronizationType,
            [Values] bool withCancelationToken)
        {
            var cancelationSource = CancelationSource.New();
            var deferred = withCancelationToken
                ? Promise.NewDeferred<int>(cancelationSource.Token)
                : Promise.NewDeferred<int>();

            ProgressHelper progressHelper = new ProgressHelper(progressType, synchronizationType);

            deferred.Promise
                .SubscribeProgress(progressHelper)
                .Forget();

            progressHelper.AssertCurrentProgress(0f);

            progressHelper.PrepareForInvoke();
            new ThreadHelper().ExecuteSingleAction(() => deferred.ReportProgress(0.1f));
            progressHelper.AssertCurrentProgress(0.1f);

            deferred.Resolve(1);
            cancelationSource.Dispose();
        }
#endif

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
        public void DeferredMayBeCanceledOnSeparateThread_void0()
        {
            bool invoked = false;
            var cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred(cancelationSource.Token);
            deferred.Promise
                .CatchCancelation(_ => { invoked = true; })
                .Forget();

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteSingleAction(() => cancelationSource.Cancel());

            Assert.IsTrue(invoked);
            cancelationSource.Dispose();
        }

        [Test]
        public void DeferredMayBeCanceledOnSeparateThread_void1()
        {
            bool invoked = false;
            var cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred(cancelationSource.Token);
            deferred.Promise
                .CatchCancelation(_ => { invoked = true; })
                .Forget();

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteSingleAction(() => cancelationSource.Cancel("Cancel"));

            Assert.IsTrue(invoked);
            cancelationSource.Dispose();
        }

        [Test]
        public void DeferredMayBeCanceledOnSeparateThread_T0()
        {
            bool invoked = false;
            var cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred<int>(cancelationSource.Token);
            deferred.Promise
                .CatchCancelation(_ => { invoked = true; })
                .Forget();

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteSingleAction(() => cancelationSource.Cancel());

            Assert.IsTrue(invoked);
            cancelationSource.Dispose();
        }

        [Test]
        public void DeferredMayBeCanceledOnSeparateThread_T1()
        {
            bool invoked = false;
            var cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred<int>(cancelationSource.Token);
            deferred.Promise
                .CatchCancelation(_ => { invoked = true; })
                .Forget();

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteSingleAction(() => cancelationSource.Cancel("Cancel"));

            Assert.IsTrue(invoked);
            cancelationSource.Dispose();
        }
    }
}

#endif