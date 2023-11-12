// Unity 2020.2 added C#8 support.
#if UNITY_2020_2_OR_NEWER || (CSHARP_7_3_OR_NEWER && !UNITY_5_5_OR_NEWER)

using NUnit.Framework;
using Proto.Promises;
using Proto.Promises.Linq;
using System;
using System.Threading;

namespace ProtoPromiseTests.APIs.Linq
{
    public class AsyncEnumerableTests
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
        public void AsyncEnumerableCompletesSynchronouslyWithValues(
            [Values(0, 1, 2, 10)] int yieldCount)
        {
            var enumerable = AsyncEnumerable.Create<int>(async (writer, _) =>
            {
                for (int i = 0; i < yieldCount; i++)
                {
                    await writer.YieldAsync(i);
                }
            });

            Promise.Run(async () =>
            {
                int count = 0;

                await foreach (var item in enumerable)
                {
                    Assert.AreEqual(count, item);
                    ++count;
                }

                Assert.AreEqual(yieldCount, count);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AsyncEnumerableProducesCorrectValues(
            [Values(0, 1, 2, 10)] int yieldCount,
            [Values] bool iteratorIsAsync,
            [Values] bool consumerIsAsync)
        {
            var deferred = Promise.NewDeferred();
            bool runnerIsComplete = false;

            var enumerable = AsyncEnumerable.Create<int>(async (writer, _) =>
            {
                if (iteratorIsAsync)
                {
                    await deferred.Promise;
                }
                for (int i = 0; i < yieldCount; i++)
                {
                    await writer.YieldAsync(i);
                    if (iteratorIsAsync)
                    {
                        await deferred.Promise;
                    }
                }
            });

            int count = 0;

            var runner = Promise.Run(async () =>
            {
                await foreach (var item in enumerable)
                {
                    Assert.AreEqual(count, item);
                    ++count;
                    if (consumerIsAsync)
                    {
                        await deferred.Promise;
                    }
                }
                if (consumerIsAsync)
                {
                    await deferred.Promise;
                }

                Assert.AreEqual(yieldCount, count);
                runnerIsComplete = true;
            }, SynchronizationOption.Synchronous);

            Assert.AreNotEqual(iteratorIsAsync || consumerIsAsync, runnerIsComplete);
            int awaitCount = iteratorIsAsync && consumerIsAsync ? yieldCount * 2
                : iteratorIsAsync || consumerIsAsync ? yieldCount
                : 0;
            for (int i = 0; i < awaitCount; i++)
            {
                var def = deferred;
                deferred = Promise.NewDeferred();
                def.Resolve();
            }

            if (iteratorIsAsync)
            {
                Assert.False(runnerIsComplete);
                var def = deferred;
                deferred = Promise.NewDeferred();
                def.Resolve();
            }
            Assert.AreNotEqual(consumerIsAsync, runnerIsComplete);
            deferred.Resolve();
            Assert.True(runnerIsComplete);

            runner
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));

            if (deferred.IsValid)
            {
                deferred.Promise.Forget();
            }
        }

        [Test]
        public void AsyncEnumerableSynchronousEarlyExit(
            [Values(0, 1, 2, 10)] int yieldCount)
        {
            bool didRunFinallyBlock = false;
            var enumerable = AsyncEnumerable.Create<int>(async (writer, _) =>
            {
                try
                {
                    for (int i = 0; i < yieldCount; i++)
                    {
                        await writer.YieldAsync(i);
                    }
                }
                finally
                {
                    didRunFinallyBlock = true;
                }
            });

            Promise.Run(async () =>
            {
                int count = 0;

                await foreach (var item in enumerable)
                {
                    Assert.AreEqual(count, item);
                    ++count;
                    break;
                }

                Assert.LessOrEqual(count, 1);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));

            Assert.True(didRunFinallyBlock);
        }

        [Test]
        public void AsyncEnumerableAsynchronousEarlyExit(
            [Values(0, 1, 2, 10)] int yieldCount)
        {
            var deferred = Promise.NewDeferred();
            bool didStartFinallyBlock = false;
            bool didCompleteFinallyBlock = false;
            bool runnerIsComplete = false;

            var enumerable = AsyncEnumerable.Create<int>(async (writer, _) =>
            {
                try
                {
                    await deferred.Promise;
                    for (int i = 0; i < yieldCount; i++)
                    {
                        await writer.YieldAsync(i);
                        await deferred.Promise;
                    }
                }
                finally
                {
                    didStartFinallyBlock = true;
                    await deferred.Promise;
                    didCompleteFinallyBlock = true;
                }
            });

            int count = 0;

            var runner = Promise.Run(async () =>
            {
                await foreach (var item in enumerable)
                {
                    Assert.AreEqual(count, item);
                    ++count;
                    break;
                }

                Assert.LessOrEqual(count, 1);
                runnerIsComplete = true;
            }, SynchronizationOption.Synchronous);

            Assert.False(runnerIsComplete);
            Assert.False(didStartFinallyBlock);
            var def = deferred;
            deferred = Promise.NewDeferred();
            def.Resolve();

            Assert.False(runnerIsComplete);
            Assert.False(didCompleteFinallyBlock);
            Assert.True(didStartFinallyBlock);
            deferred.Resolve();
            Assert.True(runnerIsComplete);

            runner
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));

            Assert.True(didCompleteFinallyBlock);
        }

        [Test]
        public void AsyncEnumerableDisposeAsyncEnumeratorWithoutIterating()
        {
            bool didIterate = false;
            var enumerable = AsyncEnumerable.Create<int>(async (writer, _) =>
            {
                didIterate = true;
                await writer.YieldAsync(42);
            });

            bool runnerIsComplete = false;
            int count = 0;

            var runner = Promise.Run(async () =>
            {
                var enumerator = enumerable.GetAsyncEnumerator();
                await enumerator.DisposeAsync();

                Assert.AreEqual(0, count);
                runnerIsComplete = true;
            }, SynchronizationOption.Synchronous);

            Assert.True(runnerIsComplete);
            Assert.False(didIterate);

            runner
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AsyncEnumerableRespectsCancelationToken(
            [Values] bool iteratorIsAsync,
            [Values] bool consumerIsAsync)
        {
            const int yieldCount = 10;
            var cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred();
            bool runnerIsComplete = false;

            var enumerable = AsyncEnumerable.Create<int>(async (writer, cancelationToken) =>
            {
                cancelationToken.ThrowIfCancelationRequested();
                if (iteratorIsAsync)
                {
                    await deferred.Promise.WaitAsync(cancelationToken);
                }
                for (int i = 0; i < yieldCount; i++)
                {
                    await writer.YieldAsync(i);
                    cancelationToken.ThrowIfCancelationRequested();
                    if (iteratorIsAsync)
                    {
                        await deferred.Promise.WaitAsync(cancelationToken);
                    }
                }
            });

            int count = 0;

            var runner = Promise.Run(async () =>
            {
                try
                {
                    await foreach (var item in enumerable.WithCancelation(cancelationSource.Token))
                    {
                        Assert.AreEqual(count, item);
                        ++count;
                        if (consumerIsAsync)
                        {
                            await deferred.Promise;
                        }
                        if (count == 2)
                        {
                            cancelationSource.Cancel();
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    if (consumerIsAsync)
                    {
                        await deferred.Promise;
                    }
                    Assert.AreEqual(2, count);
                    runnerIsComplete = true;
                }
            }, SynchronizationOption.Synchronous);

            Assert.AreNotEqual(iteratorIsAsync || consumerIsAsync, runnerIsComplete);
            int awaitCount = iteratorIsAsync && consumerIsAsync ? 4
                : iteratorIsAsync || consumerIsAsync ? 2
                : 0;
            for (int i = 0; i < awaitCount; i++)
            {
                var def = deferred;
                deferred = Promise.NewDeferred();
                def.Resolve();
            }

            Assert.AreNotEqual(consumerIsAsync, runnerIsComplete);
            deferred.Resolve();
            Assert.True(runnerIsComplete);

            runner
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));

            if (deferred.IsValid)
            {
                deferred.Promise.Forget();
            }
            cancelationSource.Dispose();
        }

        [Test]
        public void AsyncEnumerableRespectsConfigureAwait(
            [Values(0, 1, 2, 10)] int yieldCount,
            [Values] bool iteratorIsAsync,
            [Values] bool consumerIsAsync,
            [Values] SynchronizationType synchronizationType)
        {
            var foregroundThread = Thread.CurrentThread;

            bool didAwaitDeferred = false;
            var deferred = Promise.NewDeferred();
            bool runnerIsComplete = false;

            var enumerable = AsyncEnumerable.Create<int>(async (writer, _) =>
            {
                if (iteratorIsAsync)
                {
                    var promise = deferred.Promise;
                    didAwaitDeferred = true;
                    await promise;
                }
                if (synchronizationType != SynchronizationType.Synchronous)
                {
                    await Promise.SwitchToContextAwait(synchronizationType == SynchronizationType.Background ? TestHelper._foregroundContext : TestHelper._backgroundContext);
                }
                for (int i = 0; i < yieldCount; i++)
                {
                    await writer.YieldAsync(i);
                    if (iteratorIsAsync)
                    {
                        var promise = deferred.Promise;
                        didAwaitDeferred = true;
                        await promise;
                    }
                    if (synchronizationType != SynchronizationType.Synchronous)
                    {
                        await Promise.SwitchToContextAwait(synchronizationType == SynchronizationType.Background ? TestHelper._foregroundContext : TestHelper._backgroundContext);
                    }
                }
            });

            int count = 0;

            var runner = Promise.Run(async () =>
            {
                var configuredEnumerable = synchronizationType == SynchronizationType.Explicit
                    ? enumerable.ConfigureAwait(TestHelper._foregroundContext)
                    : enumerable.ConfigureAwait((SynchronizationOption) synchronizationType);
                await foreach (var item in configuredEnumerable)
                {
                    TestHelper.AssertCallbackContext(synchronizationType, SynchronizationType.Foreground, foregroundThread);
                    Assert.AreEqual(count, item);
                    ++count;
                    if (consumerIsAsync)
                    {
                        var promise = deferred.Promise;
                        didAwaitDeferred = true;
                        await promise;
                    }
                }
                TestHelper.AssertCallbackContext(synchronizationType, SynchronizationType.Foreground, foregroundThread);
                if (consumerIsAsync)
                {
                    var promise = deferred.Promise;
                    didAwaitDeferred = true;
                    await promise;
                }

                Assert.AreEqual(yieldCount, count);
                runnerIsComplete = true;
            }, SynchronizationOption.Synchronous);

            int awaitCount = iteratorIsAsync && consumerIsAsync ? (yieldCount * 2) + 1
                : iteratorIsAsync || consumerIsAsync ? yieldCount
                : 0;
            for (int i = 0; i < awaitCount; i++)
            {
                if (!SpinWait.SpinUntil(() =>
                {
                    TestHelper.ExecuteForegroundCallbacks();
                    return didAwaitDeferred;
                }, TimeSpan.FromSeconds(1)))
                {
                    throw new TimeoutException();
                }
                didAwaitDeferred = false;
                Assert.False(runnerIsComplete);
                var def = deferred;
                deferred = Promise.NewDeferred();
                def.Resolve();
            }

            if (iteratorIsAsync || consumerIsAsync)
            {
                if (!SpinWait.SpinUntil(() =>
                {
                    TestHelper.ExecuteForegroundCallbacks();
                    return didAwaitDeferred;
                }, TimeSpan.FromSeconds(1)))
                {
                    throw new TimeoutException();
                }
                Assert.False(runnerIsComplete);
            }
            deferred.Resolve();

            runner
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
            Assert.True(runnerIsComplete);

            if (deferred.IsValid)
            {
                deferred.Promise.Forget();
            }
        }
    }
}

#endif