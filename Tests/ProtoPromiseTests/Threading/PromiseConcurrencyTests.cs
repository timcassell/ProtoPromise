#if !UNITY_EDITOR || CSHARP_7_OR_LATER

#if !PROTO_PROMISE_PROGRESS_DISABLE
#define PROMISE_PROGRESS
#else
#undef PROMISE_PROGRESS
#endif

using NUnit.Framework;
using System.Threading;

namespace Proto.Promises.Tests.Threading
{
    public class PromiseConcurrencyTests
    {
        [TearDown]
        public void Teardown()
        {
            TestHelper.Cleanup();
        }

#if PROMISE_PROGRESS
        [Test]
        public void PromiseProgressMayBeCalledConcurrently_pending_void()
        {
            int invokedCount = 0;
            var deferred = Promise.NewDeferred();
            var promise = deferred.Promise;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(
                () => promise.Progress(v => { Interlocked.Increment(ref invokedCount); })
            );

            promise.Forget();
            deferred.ReportProgress(0.1f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(ThreadHelper.multiThreadCount, invokedCount);
            deferred.Resolve();
        }

        [Test]
        public void PromiseProgressMayBeCalledConcurrently_resolved_void()
        {
            int invokedCount = 0;
            Promise promise = Promise.Resolved();

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(
                () => promise.Progress(v => { Interlocked.Increment(ref invokedCount); })
            );
            promise.Forget();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(ThreadHelper.multiThreadCount, invokedCount);
        }

        [Test]
        public void PromiseProgressMayBeCalledConcurrently_pending_T()
        {
            int invokedCount = 0;
            var deferred = Promise.NewDeferred<int>();
            var promise = deferred.Promise;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(
                () => promise.Progress(v => { Interlocked.Increment(ref invokedCount); })
            );

            promise.Forget();
            deferred.ReportProgress(0.1f);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(ThreadHelper.multiThreadCount, invokedCount);
            deferred.Resolve(1);
        }

        [Test]
        public void PromiseProgressMayBeCalledConcurrently_resolved_T()
        {
            int invokedCount = 0;
            Promise<int> promise = Promise.Resolved(1);

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(
                () => promise.Progress(v => { Interlocked.Increment(ref invokedCount); })
            );
            promise.Forget();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(ThreadHelper.multiThreadCount, invokedCount);
        }
#endif

        [Test]
        public void PreservedPromiseMayBeAwaitedConcurrently_pending_void()
        {
            int invokedCount = 0;
            var deferred = Promise.NewDeferred();
            var promise = deferred.Promise.Preserve();

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(
                () => promise.Then(() => { Interlocked.Increment(ref invokedCount); }).Forget()
            );
            promise.Forget();
            deferred.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(ThreadHelper.multiThreadCount, invokedCount);
        }

        [Test]
        public void PreservedPromiseMayBeAwaitedConcurrently_resolved_void()
        {
            int invokedCount = 0;
            var promise = Promise.Resolved().Preserve();

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(
                () => promise.Then(() => { Interlocked.Increment(ref invokedCount); }).Forget()
            );
            promise.Forget();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(ThreadHelper.multiThreadCount, invokedCount);
        }

        [Test]
        public void PreservedPromiseMayBeAwaitedConcurrently_pending_T()
        {
            int invokedCount = 0;
            var deferred = Promise.NewDeferred<int>();
            var promise = deferred.Promise.Preserve();

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(
                () => promise.Then(() => { Interlocked.Increment(ref invokedCount); }).Forget()
            );
            promise.Forget();
            deferred.Resolve(1);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(ThreadHelper.multiThreadCount, invokedCount);
        }

        [Test]
        public void PreservedPromiseMayBeAwaitedConcurrently_resolved_T()
        {
            int invokedCount = 0;
            var promise = Promise.Resolved(1).Preserve();

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(
                () => promise.Then(v => { Interlocked.Increment(ref invokedCount); }).Forget()
            );
            promise.Forget();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(ThreadHelper.multiThreadCount, invokedCount);
        }
    }
}

#endif