#if CSHARP_7_OR_LATER

#if !PROTO_PROMISE_PROGRESS_DISABLE
#define PROMISE_PROGRESS
#else
#undef PROMISE_PROGRESS
#endif

using NUnit.Framework;
using System.Threading;

namespace Proto.Promises.Tests.Threading
{
    public class AwaitConcurrencyTests
    {
        [TearDown]
        public void Teardown()
        {
            TestHelper.Cleanup();
        }

        [Test]
        public void PreservedPromiseMayBeAwaitedConcurrently_PendingThenResolved_void()
        {
            int invokedCount = 0;
            var deferred = Promise.NewDeferred();
            var promise = deferred.Promise.Preserve();

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(
                () =>
                {
                    Await();

                    async void Await()
                    {
                        await promise;
                        Interlocked.Increment(ref invokedCount);
                    }
                }
            );

            promise.Forget();
            deferred.Resolve();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(ThreadHelper.multiExecutionCount, invokedCount);
        }

        [Test]
        public void PreservedPromiseMayBeAwaitedConcurrently_Resolved_void()
        {
            int invokedCount = 0;
            var promise = Promise.Resolved().Preserve();

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(
                () =>
                {
                    Await();

                    async void Await()
                    {
                        await promise;
                        Interlocked.Increment(ref invokedCount);
                    }
                }
            );

            promise.Forget();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(ThreadHelper.multiExecutionCount, invokedCount);
        }

        [Test]
        public void PreservedPromiseMayBeAwaitedConcurrently_PendingThenResolved_T()
        {
            int invokedCount = 0;
            var deferred = Promise.NewDeferred<int>();
            var promise = deferred.Promise.Preserve();

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(
                () =>
                {
                    Await();

                    async void Await()
                    {
                        _ = await promise;
                        Interlocked.Increment(ref invokedCount);
                    }
                }
            );

            promise.Forget();
            deferred.Resolve(1);
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(ThreadHelper.multiExecutionCount, invokedCount);
        }

        [Test]
        public void PreservedPromiseMayBeAwaitedConcurrently_Resolved_T()
        {
            int invokedCount = 0;
            var promise = Promise.Resolved(1).Preserve();

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(
                () =>
                {
                    Await();

                    async void Await()
                    {
                        _ = await promise;
                        Interlocked.Increment(ref invokedCount);
                    }
                }
            );

            promise.Forget();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(ThreadHelper.multiExecutionCount, invokedCount);
        }

        [Test]
        public void PreservedPromiseMayBeAwaitedConcurrently_PendingThenRejected_void()
        {
            int invokedCount = 0;
            var deferred = Promise.NewDeferred();
            var promise = deferred.Promise.Preserve();

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(
                () =>
                {
                    Await();

                    async void Await()
                    {
                        try
                        {
                            await promise;
                        }
                        catch (UnhandledException)
                        {
                            Interlocked.Increment(ref invokedCount);
                        }
                    }
                }
            );

            promise.Forget();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(ThreadHelper.multiExecutionCount, invokedCount);
        }

        [Test]
        public void PreservedPromiseMayBeAwaitedConcurrently_Rejected_void()
        {
            int invokedCount = 0;
            var promise = Promise.Rejected("Reject").Preserve();

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(
                () =>
                {
                    Await();

                    async void Await()
                    {
                        try
                        {
                            await promise;
                        }
                        catch (UnhandledException)
                        {
                            Interlocked.Increment(ref invokedCount);
                        }
                    }
                }
            );

            promise.Forget();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(ThreadHelper.multiExecutionCount, invokedCount);
        }

        [Test]
        public void PreservedPromiseMayBeAwaitedConcurrently_PendingThenRejected_T()
        {
            int invokedCount = 0;
            var deferred = Promise.NewDeferred<int>();
            var promise = deferred.Promise.Preserve();

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(
                () =>
                {
                    Await();

                    async void Await()
                    {
                        try
                        {
                            _ = await promise;
                        }
                        catch (UnhandledException)
                        {
                            Interlocked.Increment(ref invokedCount);
                        }
                    }
                }
            );

            promise.Forget();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(ThreadHelper.multiExecutionCount, invokedCount);
        }

        [Test]
        public void PreservedPromiseMayBeAwaitedConcurrently_Rejected_T()
        {
            int invokedCount = 0;
            var promise = Promise<int>.Rejected("Reject").Preserve();

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(
                () =>
                {
                    Await();

                    async void Await()
                    {
                        try
                        {
                            _ = await promise;
                        }
                        catch (UnhandledException)
                        {
                            Interlocked.Increment(ref invokedCount);
                        }
                    }
                }
            );

            promise.Forget();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(ThreadHelper.multiExecutionCount, invokedCount);
        }

        [Test]
        public void PreservedPromiseMayBeAwaitedConcurrently_PendingThenCanceled_void()
        {
            int invokedCount = 0;
            var cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred(cancelationSource.Token);
            var promise = deferred.Promise.Preserve();

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(
                () =>
                {
                    Await();

                    async void Await()
                    {
                        try
                        {
                            await promise;
                        }
                        catch (CanceledException)
                        {
                            Interlocked.Increment(ref invokedCount);
                        }
                    }
                }
            );

            promise.Forget();
            cancelationSource.Cancel();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(ThreadHelper.multiExecutionCount, invokedCount);
        }

        [Test]
        public void PreservedPromiseMayBeAwaitedConcurrently_Canceled_void()
        {
            int invokedCount = 0;
            var promise = Promise.Canceled().Preserve();

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(
                () =>
                {
                    Await();

                    async void Await()
                    {
                        try
                        {
                            await promise;
                        }
                        catch (CanceledException)
                        {
                            Interlocked.Increment(ref invokedCount);
                        }
                    }
                }
            );

            promise.Forget();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(ThreadHelper.multiExecutionCount, invokedCount);
        }

        [Test]
        public void PreservedPromiseMayBeAwaitedConcurrently_PendingThenCanceled_T()
        {
            int invokedCount = 0;
            var cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred<int>(cancelationSource.Token);
            var promise = deferred.Promise.Preserve();

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(
                () =>
                {
                    Await();

                    async void Await()
                    {
                        try
                        {
                            _ = await promise;
                        }
                        catch (CanceledException)
                        {
                            Interlocked.Increment(ref invokedCount);
                        }
                    }
                }
            );

            promise.Forget();
            cancelationSource.Cancel();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(ThreadHelper.multiExecutionCount, invokedCount);
        }

        [Test]
        public void PreservedPromiseMayBeAwaitedConcurrently_Canceled_T()
        {
            int invokedCount = 0;
            var promise = Promise<int>.Canceled().Preserve();

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(
                () =>
                {
                    Await();

                    async void Await()
                    {
                        try
                        {
                            _ = await promise;
                        }
                        catch (CanceledException)
                        {
                            Interlocked.Increment(ref invokedCount);
                        }
                    }
                }
            );

            promise.Forget();
            Promise.Manager.HandleCompletesAndProgress();
            Assert.AreEqual(ThreadHelper.multiExecutionCount, invokedCount);
        }
    }
}

#endif