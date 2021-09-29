#if CSHARP_7_3_OR_NEWER && !UNITY_WEBGL

#if !PROTO_PROMISE_PROGRESS_DISABLE
#define PROMISE_PROGRESS
#else
#undef PROMISE_PROGRESS
#endif

using NUnit.Framework;

namespace Proto.Promises.Tests.Threading
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
        public void DeferredMayReportProgressOnSeparateThread_void0()
        {
            float progress = float.NaN;
            var deferred = Promise.NewDeferred();
            deferred.Promise
                .Progress(v => progress = v)
                .Forget();

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteSingleAction(() => deferred.ReportProgress(0.1f));

            Assert.IsNaN(progress); // Progress isn't reported until manager handles it.
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.1f, progress, TestHelper.progressEpsilon);

            deferred.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
        }

        [Test]
        public void DeferredMayReportProgressOnSeparateThread_void1()
        {
            var cancelationSource = CancelationSource.New();
            float progress = float.NaN;
            var deferred = Promise.NewDeferred(cancelationSource.Token);
            deferred.Promise
                .Progress(v => progress = v)
                .Forget();

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteSingleAction(() => deferred.ReportProgress(0.1f));

            Assert.IsNaN(progress); // Progress isn't reported until manager handles it.
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.1f, progress, TestHelper.progressEpsilon);

            deferred.Resolve();
            cancelationSource.Dispose();
            Promise.Manager.HandleCompletesAndProgress();
        }

        [Test]
        public void DeferredMayReportProgressOnSeparateThread_T0()
        {
            float progress = float.NaN;
            var deferred = Promise.NewDeferred<int>();
            deferred.Promise
                .Progress(v => progress = v)
                .Forget();

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteSingleAction(() => deferred.ReportProgress(0.1f));

            Assert.IsNaN(progress); // Progress isn't reported until manager handles it.
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.1f, progress, TestHelper.progressEpsilon);

            deferred.Resolve(1);
            Promise.Manager.HandleCompletesAndProgress();
        }

        [Test]
        public void DeferredMayReportProgressOnSeparateThread_T1()
        {
            var cancelationSource = CancelationSource.New();
            float progress = float.NaN;
            var deferred = Promise.NewDeferred<int>(cancelationSource.Token);
            deferred.Promise
                .Progress(v => progress = v)
                .Forget();

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteSingleAction(() => deferred.ReportProgress(0.1f));

            Assert.IsNaN(progress); // Progress isn't reported until manager handles it.
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(0.1f, progress, TestHelper.progressEpsilon);

            deferred.Resolve(1);
            cancelationSource.Dispose();
            Promise.Manager.HandleCompletesAndProgress();
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