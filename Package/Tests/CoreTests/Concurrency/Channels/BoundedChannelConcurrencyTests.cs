﻿#if !UNITY_WEBGL

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
    public class BoundedChannelConcurrencyTests
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
            foreach (int peekerCount in new int[] { 0, 1, 2 })
            foreach (int capacity in new int[] { 1, 10, 100 })
            foreach (BoundedChannelFullMode fullMode in Enum.GetValues(typeof(BoundedChannelFullMode)))
            {
                if (readerCount > writerCount) continue;
                if (fullMode == BoundedChannelFullMode.Wait && readerCount == 0) continue;

                yield return new TestCaseData(writerCount, readerCount, peekerCount, capacity, fullMode);
            }
        }

        [Test, TestCaseSource(nameof(GetArgs))]
        public void Write_Read_Peek_Concurrent(int writerCount, int readerCount, int peekerCount, int capacity, BoundedChannelFullMode fullMode)
        {
            const int NumWrites = 100;

            var channel = Channel<int>.NewBounded(new BoundedChannelOptions<int>() { Capacity = capacity, FullMode = fullMode });
            var writers= new Stack<Promise>();
            var readers = new Stack<Promise>();
            var peekers = new Stack<Promise>();
            // Make all background threads start at the same time.
            var barrier = new Barrier(1 + writerCount + readerCount + peekerCount);

            for (int i = 0; i < writerCount; ++i)
            {
                writers.Push(Promise.Run(async () =>
                {
                    barrier.SignalAndWait();
                    for (int j = 0; j < NumWrites; ++j)
                    {
                        var writeResult = await channel.Writer.WriteAsync(j);
                        if (writeResult.Result == ChannelWriteResult.Closed)
                        {
                            // More than 1 writer was running, and one of the other writers closed the channel.
                            return;
                        }
                    }
                    if (writerCount > 0)
                    {
                        // If multiple writer threads are racing, we don't assert the result.
                        channel.Writer.TryClose();
                    }
                    else
                    {
                        Assert.True(channel.Writer.TryClose());
                    }
                }, SynchronizationOption.Background));
            }
            for (int i = 0; i < readerCount; ++i)
            {
                readers.Push(Promise.Run(async () =>
                {
                    barrier.SignalAndWait();
                    // Some items may be dropped, so we read until there are no more items, rather than a fixed number of reads.
                    while ((await channel.Reader.ReadAsync()).TryGetItem(out var item))
                    {
                        // We can't assert the read value.
                    }
                }, SynchronizationOption.Background));
            }
            for (int i = 0; i < peekerCount; ++i)
            {
                peekers.Push(Promise.Run(async () =>
                {
                    Exception ex = null;
                    try
                    {
                        barrier.SignalAndWait();
                        while ((await channel.Reader.PeekAsync()).TryGetItem(out var item))
                        {
                            // We can't assert the read value.
                        }
                    }
                    catch (Exception e)
                    {
                        ex = e;
                    }
                    if (readerCount == 0)
                    {
                        Assert.IsInstanceOf<ObjectDisposedException>(ex);
                    }
                }, SynchronizationOption.Background));
            }

            barrier.SignalAndWait();
            Promise.All(writers)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(NumWrites * writerCount));
            Promise.All(readers)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(NumWrites * readerCount));
            if (readerCount == 0)
            {
                channel.Dispose();
            }
            Promise.All(peekers)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(peekerCount));
            // Double dispose is a no-op.
            channel.Dispose();
        }
    }
}

#endif