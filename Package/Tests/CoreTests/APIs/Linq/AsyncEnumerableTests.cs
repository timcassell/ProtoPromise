﻿#if NET47_OR_GREATER || NETSTANDARD2_1_OR_GREATER || NETCOREAPP || UNITY_2021_2_OR_NEWER

using NUnit.Framework;
using Proto.Promises;
using Proto.Promises.Linq;
using System;
using System.Threading.Tasks;

namespace ProtoPromiseTests.APIs
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
                .WaitWithTimeout(TimeSpan.FromSeconds(1));
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
                .WaitWithTimeout(TimeSpan.FromSeconds(1));

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
                .WaitWithTimeout(TimeSpan.FromSeconds(1));

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
                .WaitWithTimeout(TimeSpan.FromSeconds(1));

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
                .WaitWithTimeout(TimeSpan.FromSeconds(1));
        }
    }
}

#endif