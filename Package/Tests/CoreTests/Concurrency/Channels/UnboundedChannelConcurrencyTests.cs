#if !UNITY_WEBGL

#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using NUnit.Framework;
using Proto.Promises;
using Proto.Promises.Channels;
using Proto.Promises.Linq;
using System;
using System.Collections.Generic;
using System.Threading;

namespace ProtoPromiseTests.Concurrency.Channels
{
    public class UnboundedChannelConcurrencyTests
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

        private static IEnumerable<TestCaseData> GetArgs()
        {
            foreach (int writerCount in new int[] { 1, 2 })
            foreach (int readerCount in new int[] { 0, 1, 2 })
            foreach (int waitToWriteCount in new int[] { 0, 1, 2 })
            foreach (int waitToReadCount in new int[] { 0, 1, 2 })
            foreach (int peekerCount in new int[] { 0, 1, 2 })
            {
                if (readerCount > writerCount) continue;
                yield return new TestCaseData(writerCount, readerCount, waitToWriteCount, waitToReadCount, peekerCount);
            }
        }

        [Test, TestCaseSource(nameof(GetArgs))]
        public void Write_Read_Peek_Concurrent(int writerCount, int readerCount, int waitToWriteCount, int waitToReadCount, int peekerCount)
        {
            const int NumWrites = 100;

            var channel = Channel<int>.NewUnbounded();
            var readersAndWriters = new Stack<Promise>();
            var waitsAndPeeks = new Stack<Promise>();
            // Make all background threads start at the same time.
            var barrier = new Barrier(writerCount + readerCount + peekerCount + waitToWriteCount + waitToReadCount);

            bool isClosed = false;

            for (int i = 0; i < writerCount; ++i)
            {
                readersAndWriters.Push(Promise.Run(async () =>
                {
                    barrier.SignalAndWait();
                    for (int j = 0; j < NumWrites; ++j)
                    {
                        Assert.AreEqual(ChannelWriteResult.Success, (await channel.Writer.WriteAsync(j)).Result);
                    }
                }, SynchronizationOption.Background));
            }
            for (int i = 0; i < readerCount; ++i)
            {
                readersAndWriters.Push(Promise.Run(async () =>
                {
                    barrier.SignalAndWait();
                    for (int j = 0; j < NumWrites; ++j)
                    {
                        Assert.True((await channel.Reader.ReadAsync()).TryGetItem(out var item));
                        // With more than 1 reader and/or writer, we can't assert the read value.
                        if (readerCount == 1 && writerCount == 1)
                        {
                            Assert.AreEqual(j, item);
                        }
                    }
                }, SynchronizationOption.Background));
            }
            for (int i = 0; i < waitToWriteCount; ++i)
            {
                waitsAndPeeks.Push(Promise.Run(async () =>
                {
                    barrier.SignalAndWait();
                    while (!isClosed && await channel.Writer.WaitToWriteAsync())
                    {
                        // WaitToWriteAsync never waits for UnboundedChannel, so we yield the thread.
                        Thread.Yield();
                    }
                }, SynchronizationOption.Background));
            }
            for (int i = 0; i < waitToReadCount; ++i)
            {
                waitsAndPeeks.Push(Promise.Run(async () =>
                {
                    barrier.SignalAndWait();
                    while (!isClosed && await channel.Reader.WaitToReadAsync())
                    {
                    }
                }, SynchronizationOption.Background));
            }
            for (int i = 0; i < peekerCount; ++i)
            {
                waitsAndPeeks.Push(Promise.Run(() =>
                {
                    barrier.SignalAndWait();
                    while (!isClosed && channel.Reader.TryPeek().Result != ChannelPeekResult.Closed)
                    {
                        // We can't assert the peeked value.
                        Thread.Yield();
                    }
                }, SynchronizationOption.Background));
            }

            Promise.All(readersAndWriters)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(NumWrites * (writerCount + readerCount)));
            
            Assert.True(channel.Writer.TryClose());
            isClosed = true;

            Promise.All(waitsAndPeeks)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(3));

            channel.Dispose();
        }
    }
}

#endif