#if !UNITY_WEBGL

#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using NUnit.Framework;
using Proto.Promises;
using Proto.Promises.Threading;
using System;
using System.Threading;

namespace ProtoPromiseTests.Concurrency.Threading
{
    public class AsyncManualResetEventConcurrencyTests
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
        public void AsyncManualResetEvent_WaitCalledConcurrently_AlreadySet()
        {
            int invokedCount = 0;
            var mre = new AsyncManualResetEvent(true);
            Action parallelAction = () =>
            {
                mre.Wait();
                Interlocked.Increment(ref invokedCount);
            };

            new ThreadHelper().ExecuteParallelActionsWithOffsets(false,
                // setup
                () =>
                {
                    invokedCount = 0;
                },
                // teardown
                () =>
                {
                    Assert.AreEqual(4, invokedCount);
                },
                // parallel actions, repeated to generate offsets
                parallelAction,
                parallelAction,
                parallelAction,
                parallelAction
            );
        }

        [Test]
        public void AsyncManualResetEvent_WaitAndSetCalledConcurrently()
        {
            int invokedCount = 0;
            var mre = new AsyncManualResetEvent(false);
            Action parallelAction = () =>
            {
                mre.Wait();
                Interlocked.Increment(ref invokedCount);
            };

            new ThreadHelper().ExecuteParallelActionsWithOffsets(false,
                // setup
                () =>
                {
                    invokedCount = 0;
                    mre.Reset();
                },
                // teardown
                () =>
                {
                    Assert.AreEqual(4, invokedCount);
                },
                // parallel actions, repeated to generate offsets
                parallelAction,
                parallelAction,
                parallelAction,
                parallelAction,
                () => mre.Set()
            );
        }

        [Test]
        public void AsyncManualResetEvent_WaitAndCancelCalledConcurrently_AlreadySet()
        {
            int invokedCount = 0;
            var mre = new AsyncManualResetEvent(true);
            CancelationSource cancelationSource = default(CancelationSource);
            Action parallelAction = () =>
            {
                mre.TryWait(cancelationSource.Token);
                Interlocked.Increment(ref invokedCount);
            };

            new ThreadHelper().ExecuteParallelActionsWithOffsets(false,
                // setup
                () =>
                {
                    invokedCount = 0;
                    cancelationSource = CancelationSource.New();
                },
                // teardown
                () =>
                {
                    Assert.AreEqual(4, invokedCount);
                    cancelationSource.Dispose();
                },
                // parallel actions, repeated to generate offsets
                parallelAction,
                parallelAction,
                parallelAction,
                parallelAction,
                () => cancelationSource.Cancel()
            );
        }

        [Test]
        public void AsyncManualResetEvent_WaitAndSetAndCancelCalledConcurrently()
        {
            int invokedCount = 0;
            var mre = new AsyncManualResetEvent(false);
            CancelationSource cancelationSource = default(CancelationSource);
            Action parallelAction = () =>
            {
                mre.TryWait(cancelationSource.Token);
                Interlocked.Increment(ref invokedCount);
            };

            new ThreadHelper().ExecuteParallelActionsWithOffsets(false,
                // setup
                () =>
                {
                    invokedCount = 0;
                    cancelationSource = CancelationSource.New();
                    mre.Reset();
                },
                // teardown
                () =>
                {
                    Assert.AreEqual(4, invokedCount);
                    cancelationSource.Dispose();
                },
                // parallel actions, repeated to generate offsets
                parallelAction,
                parallelAction,
                parallelAction,
                parallelAction,
                () => mre.Set(),
                () => cancelationSource.Cancel()
            );
        }

        [Test]
        public void AsyncManualResetEvent_WaitAsyncCalledConcurrently_AlreadySet()
        {
            int invokedCount = 0;
            var mre = new AsyncManualResetEvent(true);
            Action parallelAction = () =>
            {
                mre.WaitAsync()
                    .Then(() => Interlocked.Increment(ref invokedCount))
                    .Forget();
            };

            new ThreadHelper().ExecuteParallelActionsWithOffsets(false,
                // setup
                () =>
                {
                    invokedCount = 0;
                },
                // teardown
                () =>
                {
                    Assert.AreEqual(4, invokedCount);
                },
                // parallel actions, repeated to generate offsets
                parallelAction,
                parallelAction,
                parallelAction,
                parallelAction
            );
        }

        [Test]
        public void AsyncManualResetEvent_WaitAsyncAndSetCalledConcurrently()
        {
            int invokedCount = 0;
            var mre = new AsyncManualResetEvent(false);
            Action parallelAction = () =>
            {
                mre.WaitAsync()
                    .Then(() => Interlocked.Increment(ref invokedCount))
                    .Forget();
            };

            new ThreadHelper().ExecuteParallelActionsWithOffsets(false,
                // setup
                () =>
                {
                    invokedCount = 0;
                    mre.Reset();
                },
                // teardown
                () =>
                {
                    TestHelper.ExecuteForegroundCallbacksAndWaitForThreadsToComplete();
                    Assert.AreEqual(4, invokedCount);
                },
                // parallel actions, repeated to generate offsets
                parallelAction,
                parallelAction,
                parallelAction,
                parallelAction,
                () => mre.Set()
            );
        }

        [Test]
        public void AsyncManualResetEvent_WaitAsyncAndCancelCalledConcurrently_AlreadySet()
        {
            int invokedCount = 0;
            var mre = new AsyncManualResetEvent(true);
            CancelationSource cancelationSource = default(CancelationSource);
            Action parallelAction = () =>
            {
                mre.TryWaitAsync(cancelationSource.Token)
                    .Then(() => Interlocked.Increment(ref invokedCount))
                    .Forget();
            };

            new ThreadHelper().ExecuteParallelActionsWithOffsets(false,
                // setup
                () =>
                {
                    invokedCount = 0;
                    cancelationSource = CancelationSource.New();
                },
                // teardown
                () =>
                {
                    Assert.AreEqual(4, invokedCount);
                    cancelationSource.Dispose();
                },
                // parallel actions, repeated to generate offsets
                parallelAction,
                parallelAction,
                parallelAction,
                parallelAction,
                () => cancelationSource.Cancel()
            );
        }

        [Test]
        public void AsyncManualResetEvent_WaitAsyncAndSetAndCancelCalledConcurrently()
        {
            int invokedCount = 0;
            var mre = new AsyncManualResetEvent(false);
            CancelationSource cancelationSource = default(CancelationSource);
            Action parallelAction = () =>
            {
                mre.TryWaitAsync(cancelationSource.Token)
                    .Then(() => Interlocked.Increment(ref invokedCount))
                    .Forget();
            };

            new ThreadHelper().ExecuteParallelActionsWithOffsets(false,
                // setup
                () =>
                {
                    invokedCount = 0;
                    cancelationSource = CancelationSource.New();
                    mre.Reset();
                },
                // teardown
                () =>
                {
                    TestHelper.ExecuteForegroundCallbacksAndWaitForThreadsToComplete();
                    Assert.AreEqual(4, invokedCount);
                    cancelationSource.Dispose();
                },
                // parallel actions, repeated to generate offsets
                parallelAction,
                parallelAction,
                parallelAction,
                parallelAction,
                () => mre.Set(),
                () => cancelationSource.Cancel()
            );
        }
    }
}

#endif // !UNITY_WEBGL