﻿#if NET47_OR_GREATER || NETSTANDARD2_1_OR_GREATER || NETCOREAPP || UNITY_2021_2_OR_NEWER

using NUnit.Framework;
using Proto.Promises;
using Proto.Promises.Linq;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace ProtoPromiseTests.APIs
{
    public class AsyncEnumerableExtensionsTests
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
        public void IAsyncEnumerableCompletesSynchronouslyWithValues(
            [Values(0, 1, 2, 10)] int yieldCount)
        {
            async IAsyncEnumerable<int> GetEnumerable()
            {
                for (int i = 0; i < yieldCount; i++)
                {
                    yield return i;
                }
            };
            var enumerable = GetEnumerable().ToAsyncEnumerable();

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
                .WaitWithTimeout(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void IAsyncEnumerableProducesCorrectValues(
            [Values(0, 1, 2, 10)] int yieldCount,
            [Values] bool iteratorIsAsync,
            [Values] bool consumerIsAsync)
        {
            var deferred = Promise.NewDeferred();
            bool runnerIsComplete = false;

            async IAsyncEnumerable<int> GetEnumerable()
            {
                if (iteratorIsAsync)
                {
                    await deferred.Promise;
                }
                for (int i = 0; i < yieldCount; i++)
                {
                    yield return i;
                    if (iteratorIsAsync)
                    {
                        await deferred.Promise;
                    }
                }
            };
            var enumerable = GetEnumerable().ToAsyncEnumerable();

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
                .WaitWithTimeout(TimeSpan.FromSeconds(1));

            if (deferred.IsValid)
            {
                deferred.Promise.Forget();
            }
        }

        [Test]
        public void IAsyncEnumerableSynchronousEarlyExit(
            [Values(0, 1, 2, 10)] int yieldCount)
        {
            bool didRunFinallyBlock = false;
            async IAsyncEnumerable<int> GetEnumerable()
            {
                try
                {
                    for (int i = 0; i < yieldCount; i++)
                    {
                        yield return i;
                    }
                }
                finally
                {
                    didRunFinallyBlock = true;
                }
            };
            var enumerable = GetEnumerable().ToAsyncEnumerable();

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
                .WaitWithTimeout(TimeSpan.FromSeconds(1));

            Assert.True(didRunFinallyBlock);
        }

        [Test]
        public void IAsyncEnumerableAsynchronousEarlyExit(
            [Values(0, 1, 2, 10)] int yieldCount)
        {
            var deferred = Promise.NewDeferred();
            bool didStartFinallyBlock = false;
            bool didCompleteFinallyBlock = false;
            bool runnerIsComplete = false;

            async IAsyncEnumerable<int> GetEnumerable()
            {
                try
                {
                    await deferred.Promise;
                    for (int i = 0; i < yieldCount; i++)
                    {
                        yield return i;
                        await deferred.Promise;
                    }
                }
                finally
                {
                    didStartFinallyBlock = true;
                    await deferred.Promise;
                    didCompleteFinallyBlock = true;
                }
            };
            var enumerable = GetEnumerable().ToAsyncEnumerable();

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
                .WaitWithTimeout(TimeSpan.FromSeconds(1));

            Assert.True(didCompleteFinallyBlock);
        }

        [Test]
        public void IAsyncEnumerableDisposeAsyncEnumeratorWithoutIterating()
        {
            bool didIterate = false;
            async IAsyncEnumerable<int> GetEnumerable()
            {
                didIterate = true;
                yield return 42;
            };
            var enumerable = GetEnumerable().ToAsyncEnumerable();

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
                .WaitWithTimeout(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void IAsyncEnumerableRespectsCancelationToken(
            [Values] bool iteratorIsAsync,
            [Values] bool consumerIsAsync)
        {
            const int yieldCount = 10;
            var cancelationSource = CancelationSource.New();
            var deferred = Promise.NewDeferred();
            bool runnerIsComplete = false;

            async IAsyncEnumerable<int> GetEnumerable([EnumeratorCancellation] CancellationToken cancelationToken = default)
            {
                cancelationToken.ThrowIfCancellationRequested();
                if (iteratorIsAsync)
                {
                    await deferred.Promise.WaitAsync(cancelationToken.ToCancelationToken());
                }
                for (int i = 0; i < yieldCount; i++)
                {
                    yield return i;
                    cancelationToken.ThrowIfCancellationRequested();
                    if (iteratorIsAsync)
                    {
                        await deferred.Promise.WaitAsync(cancelationToken.ToCancelationToken());
                    }
                }
            };
            var enumerable = GetEnumerable().ToAsyncEnumerable();

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
                .WaitWithTimeout(TimeSpan.FromSeconds(1));

            if (deferred.IsValid)
            {
                deferred.Promise.Forget();
            }
            cancelationSource.Dispose();
        }

        [Test]
        public void IAsyncEnumerableRespectsConfigureAwait(
            [Values(0, 1, 2, 10)] int yieldCount,
            [Values] bool iteratorIsAsync,
            [Values] bool consumerIsAsync,
            [Values] SynchronizationType synchronizationType)
        {
            var foregroundThread = Thread.CurrentThread;

            bool didAwaitDeferred = false;
            var deferred = Promise.NewDeferred();
            bool runnerIsComplete = false;

            async IAsyncEnumerable<int> GetEnumerable()
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
                    yield return i;
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
            };
            var enumerable = GetEnumerable().ToAsyncEnumerable();

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
            else
            {
                if (!SpinWait.SpinUntil(() =>
                {
                    TestHelper.ExecuteForegroundCallbacks();
                    return runnerIsComplete;
                }, TimeSpan.FromSeconds(1)))
                {
                    throw new TimeoutException();
                }
            }
            int awaitCount = iteratorIsAsync && consumerIsAsync ? yieldCount * 2
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
                var def = deferred;
                deferred = Promise.NewDeferred();
                def.Resolve();
            }

            if (iteratorIsAsync)
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
            if (consumerIsAsync)
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
            else
            {
                if (!SpinWait.SpinUntil(() =>
                {
                    TestHelper.ExecuteForegroundCallbacks();
                    return runnerIsComplete;
                }, TimeSpan.FromSeconds(1)))
                {
                    throw new TimeoutException();
                }
            }
            deferred.Resolve();
            Assert.True(runnerIsComplete);

            runner
                .WaitWithTimeout(TimeSpan.FromSeconds(1));

            if (deferred.IsValid)
            {
                deferred.Promise.Forget();
            }
        }
    }
}

#endif