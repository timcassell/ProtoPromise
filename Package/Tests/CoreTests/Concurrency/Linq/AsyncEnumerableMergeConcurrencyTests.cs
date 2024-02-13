#if !UNITY_WEBGL

using NUnit.Framework;
using Proto.Promises;
using Proto.Promises.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ProtoPromiseTests.Concurrency.Linq
{
    public class AsyncEnumerableMergeConcurrencyTests
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
        public void AsyncEnumerableMerge_ConcurrentYields_Sync()
        {
            var enumerables = new AsyncEnumerable<int>[Environment.ProcessorCount];
            var barrier = new Barrier(enumerables.Length);

            for (int i = 0; i < enumerables.Length; ++i)
            {
                enumerables[i] = AsyncEnumerable.Create<int>(async (writer, cancelationToken) =>
                {
                    for (int j = 0; j < 10; ++j)
                    {
                        await Promise.SwitchToBackgroundAwait();

                        // Make all threads yield at the same time.
                        barrier.SignalAndWait();

                        await writer.YieldAsync(j);
                    }
                });
            }

            int totalCount = 0;
            AsyncEnumerable.Merge(enumerables)
                .ForEachAsync(num => ++totalCount)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(enumerables.Length));

            Assert.AreEqual(enumerables.Length * 10, totalCount);
        }

        [Test]
        public void AsyncEnumerableMerge_ConcurrentYields_Async()
        {
            int enumerableCount = Environment.ProcessorCount - 1;

            var barrier = new Barrier(0);

            Func<AsyncEnumerable<int>> EnumerableFunc = () =>
            {
                return AsyncEnumerable.Create<int>(async (writer, cancelationToken) =>
                {
                    barrier.AddParticipant();
                    for (int j = 0; j < 10; ++j)
                    {
                        await Promise.SwitchToBackgroundAwait();

                        // Make all threads yield at the same time.
                        barrier.SignalAndWait();

                        await writer.YieldAsync(j);
                    }
                    barrier.RemoveParticipant();
                });
            };

            var enumerablesAsync = AsyncEnumerable.Create<AsyncEnumerable<int>>(async (writer, cancelationToken) =>
            {
                barrier.AddParticipant();
                for (int i = 0; i < enumerableCount; ++i)
                {
                    await Promise.SwitchToBackgroundAwait();

                    // Make all threads yield at the same time.
                    barrier.SignalAndWait();

                    await writer.YieldAsync(EnumerableFunc());
                }
                barrier.RemoveParticipant();
            });

            int totalCount = 0;
            AsyncEnumerable.Merge(enumerablesAsync)
                .ForEachAsync(num => ++totalCount)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(enumerableCount + 1));

            Assert.AreEqual(enumerableCount * 10, totalCount);
        }
    }
}

#endif // !UNITY_WEBGL