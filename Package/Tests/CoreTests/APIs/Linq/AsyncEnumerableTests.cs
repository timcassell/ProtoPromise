#if NET47_OR_GREATER || NETSTANDARD2_1_OR_GREATER || NETCOREAPP || UNITY_2021_2_OR_NEWER

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
        public void AsyncEnumerableCompletesAsynchronouslyWithValues(
            [Values(0, 1, 2, 10)] int yieldCount)
        {
            var deferred = Promise.NewDeferred();
            bool runnerIsComplete = false;

            var enumerable = AsyncEnumerable.Create<int>(async (writer, _) =>
            {
                await deferred.Promise;
                for (int i = 0; i < yieldCount; i++)
                {
                    await writer.YieldAsync(i);
                    await deferred.Promise;
                }
            });

            int count = 0;

            var runner = Promise.Run(async () =>
            {
                await foreach (var item in enumerable)
                {
                    Assert.AreEqual(count, item);
                    ++count;
                }

                Assert.AreEqual(yieldCount, count);
                runnerIsComplete = true;
            }, SynchronizationOption.Synchronous);

            Assert.False(runnerIsComplete);
            int iterations = yieldCount == 0
                ? yieldCount - 1
                : yieldCount;
            for (int i = 0; i < iterations; i++)
            {
                var def = deferred;
                deferred = Promise.NewDeferred();
                def.Resolve();
            }

            Assert.False(runnerIsComplete);
            deferred.Resolve();
            Assert.True(runnerIsComplete);

            runner
                .WaitWithTimeout(TimeSpan.FromSeconds(1));
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
    }
}

#endif