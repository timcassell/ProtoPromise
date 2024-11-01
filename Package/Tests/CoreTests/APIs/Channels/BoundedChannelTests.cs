using NUnit.Framework;
using Proto.Promises;
using Proto.Promises.Channels;
using Proto.Promises.Linq;
using ProtoPromiseTests.Concurrency;
using System;
using System.Threading;

namespace ProtoPromiseTests.APIs.Channels
{
    public class BoundedChannelTests
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
        public void Count_IncrementsDecrementsAsExpected()
        {
            const int Bound = 3;

            var options = new BoundedChannelOptions<int>() { Capacity = Bound, FullMode = BoundedChannelFullMode.Wait };
            var channel = Channel<int>.NewBounded(options);

            for (int iter = 0; iter < 2; iter++)
            {
                for (int i = 0; i < Bound; i++)
                {
                    Assert.AreEqual(i, channel.Reader.Count);

                    Assert.AreEqual(ChannelWriteResult.Success, channel.Writer.WriteAsync(i).WaitForResult().Result);
                    Assert.AreEqual(i + 1, channel.Reader.Count);
                }

                if (iter != 0)
                {
                    Assert.True(channel.Writer.TryClose());

                    while (channel.Reader.ReadAsync().WaitForResult().TryGetItem(out var item))
                    {
                        Assert.AreEqual(Bound - (item + 1), channel.Reader.Count);
                    }
                }
                else
                {
                    for (int i = Bound; i > 0; --i)
                    {
                        Assert.True(channel.Reader.ReadAsync().WaitForResult().TryGetItem(out var item));
                        Assert.AreEqual(Bound - (item + 1), channel.Reader.Count);
                    }
                }

                Assert.AreEqual(0, channel.Reader.Count);
            }

            channel.Dispose();
        }

        [Test]
        public void Write_Read_Many(
            [Values(1, 10, 1000)] int bufferedCapacity)
        {
            var options = new BoundedChannelOptions<int>() { Capacity = bufferedCapacity, FullMode = BoundedChannelFullMode.Wait };
            var channel = Channel<int>.NewBounded(options);

            for (int i = 0; i < bufferedCapacity; i++)
            {
                Assert.AreEqual(ChannelWriteResult.Success, channel.Writer.WriteAsync(i).WaitForResult().Result);
            }
            for (int i = 0; i < bufferedCapacity; i++)
            {
                Assert.True(channel.Reader.ReadAsync().WaitForResult().TryGetItem(out var item));
                Assert.AreEqual(i, item);
            }

            channel.Dispose();
        }

        [Test]
        public void Write_Read_Many_DropOldest(
            [Values(1, 10, 1000)] int bufferedCapacity)
        {
            var options = new BoundedChannelOptions<int>() { Capacity = bufferedCapacity, FullMode = BoundedChannelFullMode.DropOldest };
            var channel = Channel<int>.NewBounded(options);

            for (int i = 0; i < bufferedCapacity; i++)
            {
                Assert.AreEqual(ChannelWriteResult.Success, channel.Writer.WriteAsync(i).WaitForResult().Result);
            }
            for (int i = bufferedCapacity; i < bufferedCapacity * 2; i++)
            {
                Assert.AreEqual(ChannelWriteResult.DroppedItem, channel.Writer.WriteAsync(i).WaitForResult().Result);
            }
            for (int i = bufferedCapacity; i < bufferedCapacity * 2; i++)
            {
                Assert.True(channel.Reader.ReadAsync().WaitForResult().TryGetItem(out var item));
                Assert.AreEqual(i, item);
            }

            channel.Dispose();
        }

        [Test]
        public void Write_Read_Many_DropNewest(
            [Values(1, 10, 1000)] int bufferedCapacity)
        {
            var options = new BoundedChannelOptions<int>() { Capacity = bufferedCapacity, FullMode = BoundedChannelFullMode.DropNewest };
            var channel = Channel<int>.NewBounded(options);

            for (int i = 0; i < bufferedCapacity; i++)
            {
                Assert.AreEqual(ChannelWriteResult.Success, channel.Writer.WriteAsync(i).WaitForResult().Result);
            }
            for (int i = bufferedCapacity; i < bufferedCapacity * 2; i++)
            {
                Assert.AreEqual(ChannelWriteResult.DroppedItem, channel.Writer.WriteAsync(i).WaitForResult().Result);
            }
            for (int i = 0; i < bufferedCapacity - 1; i++)
            {
                Assert.True(channel.Reader.ReadAsync().WaitForResult().TryGetItem(out var item));
                Assert.AreEqual(i, item);
            }

            Assert.True(channel.Reader.ReadAsync().WaitForResult().TryGetItem(out var lastItem));
            Assert.AreEqual(bufferedCapacity * 2 - 1, lastItem);

            channel.Dispose();
        }

        [Test]
        public void Write_Read_Many_DropWrite(
            [Values(1, 10, 1000)] int bufferedCapacity)
        {
            var options = new BoundedChannelOptions<int>() { Capacity = bufferedCapacity, FullMode = BoundedChannelFullMode.DropWrite };
            var channel = Channel<int>.NewBounded(options);

            for (int i = 0; i < bufferedCapacity; i++)
            {
                Assert.AreEqual(ChannelWriteResult.Success, channel.Writer.WriteAsync(i).WaitForResult().Result);
            }
            for (int i = bufferedCapacity; i < bufferedCapacity * 2; i++)
            {
                Assert.AreEqual(ChannelWriteResult.DroppedItem, channel.Writer.WriteAsync(i).WaitForResult().Result);
            }
            for (int i = 0; i < bufferedCapacity; i++)
            {
                Assert.True(channel.Reader.ReadAsync().WaitForResult().TryGetItem(out var item));
                Assert.AreEqual(i, item);
            }

            channel.Dispose();
        }

        [Test]
        public void Write_Read_OneAtATime(
            [Values(1, 10, 100)] int bufferedCapacity)
        {
            var options = new BoundedChannelOptions<int>() { Capacity = bufferedCapacity, FullMode = BoundedChannelFullMode.Wait };
            var channel = Channel<int>.NewBounded(options);

            const int NumItems = 1000;
            for (int i = 0; i < NumItems; i++)
            {
                Assert.AreEqual(ChannelWriteResult.Success, channel.Writer.WriteAsync(i).WaitForResult().Result);
                Assert.True(channel.Reader.ReadAsync().WaitForResult().TryGetItem(out var item));
                Assert.AreEqual(i, item);
            }

            channel.Dispose();
        }

        [Test]
        public void WriteAsync_AfterFullThenRead()
        {
            Promise.Run(async () =>
            {
                var options = new BoundedChannelOptions<int>() { Capacity = 1, FullMode = BoundedChannelFullMode.Wait };
                var channel = Channel<int>.NewBounded(options);

                Assert.AreEqual(ChannelWriteResult.Success, (await channel.Writer.WriteAsync(1)).Result);

                bool writeIsPending = true;
                var writePromise = channel.Writer.WriteAsync(2)
                    .Finally(() => writeIsPending = false);

                Assert.True(writeIsPending);

                Assert.True((await channel.Reader.ReadAsync()).TryGetItem(out var item));
                Assert.AreEqual(1, item);

                await writePromise;
                Assert.False(writeIsPending);

                channel.Dispose();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void WriteMany_ThenClose_SuccessfullyReadAll()
        {
            Promise.Run(async () =>
            {
                var options = new BoundedChannelOptions<int>() { Capacity = 3, FullMode = BoundedChannelFullMode.Wait };
                var channel = Channel<int>.NewBounded(options);

                for (int i = 0; i < 3; i++)
                {
                    Assert.AreEqual(ChannelWriteResult.Success, (await channel.Writer.WriteAsync(i)).Result);
                }

                Assert.True(channel.Writer.TryClose());
                Assert.AreEqual(ChannelWriteResult.Closed, (await channel.Writer.WriteAsync(11)).Result);

                for (int i = 0; i < 3; i++)
                {
                    Assert.True((await channel.Reader.ReadAsync()).TryGetItem(out var item));
                    Assert.AreEqual(i, item);
                }

                Assert.AreEqual(ChannelReadResult.Closed, (await channel.Reader.ReadAsync()).Result);

                channel.Dispose();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void CloseAfterEmpty_ReadsAreClosed()
        {
            Promise.Run(async () =>
            {
                var options = new BoundedChannelOptions<int>() { Capacity = 3, FullMode = BoundedChannelFullMode.Wait };
                var channel = Channel<int>.NewBounded(options);

                var readPromise = channel.Reader.ReadAsync();
                var waitToReadPromise = channel.Reader.WaitToReadAsync();

                Assert.True(channel.Writer.TryClose());

                Assert.AreEqual(ChannelReadResult.Closed, (await readPromise).Result);
                Assert.False(await waitToReadPromise);

                Assert.AreEqual(ChannelReadResult.Closed, (await channel.Reader.ReadAsync()).Result);
                Assert.False(await channel.Reader.WaitToReadAsync());
                Assert.AreEqual(ChannelReadResult.Closed, channel.Reader.TryRead().Result);
                Assert.AreEqual(ChannelPeekResult.Closed, channel.Reader.TryPeek().Result);

                channel.Dispose();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void TryCloseTwice_ReturnsTrueThenFalse()
        {
            var options = new BoundedChannelOptions<int>() { Capacity = 3, FullMode = BoundedChannelFullMode.Wait };
            var channel = Channel<int>.NewBounded(options);

            Assert.True(channel.Writer.TryClose());
            Assert.False(channel.Writer.TryClose());

            channel.Dispose();
        }

        [Test]
        public void TryReject_Propagates()
        {
            Promise.Run(async () =>
            {
                var options = new BoundedChannelOptions<int>() { Capacity = 3, FullMode = BoundedChannelFullMode.Wait };
                var channel = Channel<int>.NewBounded(options);

                var readPromise = channel.Reader.ReadAsync();
                var waitToReadPromise = channel.Reader.WaitToReadAsync();

                Exception expectedException = new FormatException();
                Assert.True(channel.Writer.TryReject(expectedException));

                await TestHelper.AssertThrowsAsync(() => readPromise, expectedException);
                await TestHelper.AssertThrowsAsync(() => waitToReadPromise, expectedException);
                await TestHelper.AssertThrowsAsync(() => channel.Reader.ReadAsync(), expectedException);
                await TestHelper.AssertThrowsAsync(() => channel.Reader.WaitToReadAsync(), expectedException);
                await TestHelper.AssertThrowsAsync(() => channel.Writer.WriteAsync(1), expectedException);
                await TestHelper.AssertThrowsAsync(() => channel.Writer.WaitToWriteAsync(), expectedException);
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
                await TestHelper.AssertThrowsAsync(async () => channel.Reader.TryPeek(), expectedException);
                await TestHelper.AssertThrowsAsync(async () => channel.Reader.TryRead(), expectedException);
                await TestHelper.AssertThrowsAsync(async () => channel.Writer.TryWrite(1), expectedException);
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

                channel.Dispose();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void TryCancel_Propagates()
        {
            Promise.Run(async () =>
            {
                var options = new BoundedChannelOptions<int>() { Capacity = 3, FullMode = BoundedChannelFullMode.Wait };
                var channel = Channel<int>.NewBounded(options);

                var readPromise = channel.Reader.ReadAsync();
                var waitToReadPromise = channel.Reader.WaitToReadAsync();

                Assert.True(channel.Writer.TryCancel());

                await TestHelper.AssertCanceledAsync(() => readPromise);
                await TestHelper.AssertCanceledAsync(() => waitToReadPromise);
                await TestHelper.AssertCanceledAsync(() => channel.Reader.ReadAsync());
                await TestHelper.AssertCanceledAsync(() => channel.Reader.WaitToReadAsync());
                await TestHelper.AssertCanceledAsync(() => channel.Writer.WriteAsync(1));
                await TestHelper.AssertCanceledAsync(() => channel.Writer.WaitToWriteAsync());
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
                await TestHelper.AssertCanceledAsync(async () => channel.Reader.TryPeek());
                await TestHelper.AssertCanceledAsync(async () => channel.Reader.TryRead());
                await TestHelper.AssertCanceledAsync(async () => channel.Writer.TryWrite(1));
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

                channel.Dispose();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void PingPong_Success(
            [Values(SynchronizationOption.Synchronous, SynchronizationOption.Background)] SynchronizationOption synchronizationOption)
        {
            var options = new BoundedChannelOptions<int>() { Capacity = 3, FullMode = BoundedChannelFullMode.Wait };
            var channel1 = Channel<int>.NewBounded(options);
            var channel2 = Channel<int>.NewBounded(options);

            const int NumItems = 100;
            Promise.All(
                Promise.Run(async () =>
                {
                    for (int i = 0; i < NumItems; i++)
                    {
                        Assert.True((await channel1.Reader.ReadAsync()).TryGetItem(out var item));
                        Assert.AreEqual(i, item);
                        Assert.AreEqual(ChannelWriteResult.Success, (await channel2.Writer.WriteAsync(i)).Result);
                    }
                }, synchronizationOption),
                Promise.Run(async () =>
                {
                    for (int i = 0; i < NumItems; i++)
                    {
                        Assert.AreEqual(ChannelWriteResult.Success, (await channel1.Writer.WriteAsync(i)).Result);
                        Assert.True((await channel2.Reader.ReadAsync()).TryGetItem(out var item));
                    }
                }, synchronizationOption)
            )
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(NumItems * 2));

            channel1.Dispose();
            channel2.Dispose();
        }

        [Test]
        public void Peek_SucceedsWhenDataAvailable()
        {
            Promise.Run(async () =>
            {
                var options = new BoundedChannelOptions<int>() { Capacity = 3, FullMode = BoundedChannelFullMode.Wait };
                var channel = Channel<int>.NewBounded(options);

                // Peek before items are available.
                Assert.AreEqual(ChannelPeekResult.Empty, channel.Reader.TryPeek().Result);

                // Write a value
                Assert.AreEqual(ChannelWriteResult.Success, (await channel.Writer.WriteAsync(42)).Result);

                // Can peek at the written value
                Assert.True(channel.Reader.TryPeek().TryGetItem(out int peekedResult));
                Assert.AreEqual(42, peekedResult);

                // Can still read out that value
                Assert.True((await channel.Reader.ReadAsync()).TryGetItem(out int readResult));
                Assert.AreEqual(42, readResult);

                // Peeking has to wait for another item.
                Assert.False(channel.Reader.TryPeek().TryGetItem(out peekedResult));
                Assert.AreEqual(ChannelPeekResult.Empty, channel.Reader.TryPeek().Result);
                var waitToReadPromise = channel.Reader.WaitToReadAsync();

                // Write another value
                Assert.AreEqual(ChannelWriteResult.Success, (await channel.Writer.WriteAsync(84)).Result);

                // Peek is successful
                Assert.True(await waitToReadPromise);
                Assert.True(channel.Reader.TryPeek().TryGetItem(out peekedResult));
                Assert.AreEqual(84, peekedResult);

                // Can still read out that value
                Assert.True((await channel.Reader.ReadAsync()).TryGetItem(out readResult));
                Assert.AreEqual(84, readResult);

                // Now we read and wait to read at the same time before an item is available.
                waitToReadPromise = channel.Reader.WaitToReadAsync();
                var readPromise = channel.Reader.ReadAsync();

                // Write another value
                Assert.AreEqual(ChannelWriteResult.Success, (await channel.Writer.WriteAsync(101)).Result);

                // Close the channel
                channel.Writer.TryClose();

                // Read is successful
                Assert.True((await readPromise).TryGetItem(out readResult));
                Assert.AreEqual(101, readResult);

                // WaitToRead is false
                Assert.False(await waitToReadPromise);

                // No more items and channel is closed, peek fails.
                Assert.AreEqual(ChannelPeekResult.Closed, channel.Reader.TryPeek().Result);

                channel.Dispose();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Cancel_Write(
            [Values] bool alreadyCanceled)
        {
            Promise.Run(async () =>
            {
                var options = new BoundedChannelOptions<int>() { Capacity = 3, FullMode = BoundedChannelFullMode.Wait };
                var channel = Channel<int>.NewBounded(options);
                var cancelationSource = CancelationSource.New();

                // Fill the channel to capacity.
                Assert.AreEqual(ChannelWriteResult.Success, (await channel.Writer.WriteAsync(1)).Result);
                Assert.AreEqual(ChannelWriteResult.Success, (await channel.Writer.WriteAsync(2)).Result);
                Assert.AreEqual(ChannelWriteResult.Success, (await channel.Writer.WriteAsync(3)).Result);

                var writePromise = channel.Writer.WriteAsync(42, alreadyCanceled ? CancelationToken.Canceled() : cancelationSource.Token);
                cancelationSource.Cancel();
                
                await TestHelper.AssertCanceledAsync(() => writePromise);

                cancelationSource.Dispose();
                channel.Dispose();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Cancel_WaitToWriteAsync(
            [Values] bool alreadyCanceled)
        {
            Promise.Run(async () =>
            {
                var options = new BoundedChannelOptions<int>() { Capacity = 3, FullMode = BoundedChannelFullMode.Wait };
                var channel = Channel<int>.NewBounded(options);
                var cancelationSource = CancelationSource.New();

                // Fill the channel to capacity.
                Assert.AreEqual(ChannelWriteResult.Success, (await channel.Writer.WriteAsync(1)).Result);
                Assert.AreEqual(ChannelWriteResult.Success, (await channel.Writer.WriteAsync(2)).Result);
                Assert.AreEqual(ChannelWriteResult.Success, (await channel.Writer.WriteAsync(3)).Result);

                var waitToWritePromise = channel.Writer.WaitToWriteAsync(alreadyCanceled ? CancelationToken.Canceled() : cancelationSource.Token);
                cancelationSource.Cancel();

                await TestHelper.AssertCanceledAsync(() => waitToWritePromise);

                cancelationSource.Dispose();
                channel.Dispose();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Cancel_Read(
            [Values] bool alreadyCanceled)
        {
            Promise.Run(async () =>
            {
                var options = new BoundedChannelOptions<int>() { Capacity = 3, FullMode = BoundedChannelFullMode.Wait };
                var channel = Channel<int>.NewBounded(options);
                var cancelationSource = CancelationSource.New();

                var readPromise = channel.Reader.ReadAsync(alreadyCanceled ? CancelationToken.Canceled() : cancelationSource.Token);
                cancelationSource.Cancel();

                await TestHelper.AssertCanceledAsync(() => readPromise);

                channel.Dispose();
                cancelationSource.Dispose();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Cancel_WaitToReadAsync(
            [Values] bool alreadyCanceled)
        {
            Promise.Run(async () =>
            {
                var options = new BoundedChannelOptions<int>() { Capacity = 3, FullMode = BoundedChannelFullMode.Wait };
                var channel = Channel<int>.NewBounded(options);
                var cancelationSource = CancelationSource.New();

                var waitToReadPromise = channel.Reader.WaitToReadAsync(alreadyCanceled ? CancelationToken.Canceled() : cancelationSource.Token);
                cancelationSource.Cancel();

                await TestHelper.AssertCanceledAsync(() => waitToReadPromise);

                channel.Dispose();
                cancelationSource.Dispose();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void ReadAllAsync_MoveNextAsyncAfterClosed_ReturnsFalse(
            [Values] bool completeWhilePending)
        {
            Promise.Run(async () =>
            {
                var options = new BoundedChannelOptions<int>() { Capacity = 3, FullMode = BoundedChannelFullMode.Wait };
                var channel = Channel<int>.NewBounded(options);
                var enumerator = channel.Reader.ReadAllAsync().GetAsyncEnumerator();

                if (completeWhilePending)
                {
                    var moveNextPromise = enumerator.MoveNextAsync();
                    Assert.True(channel.Writer.TryClose());
                    Assert.False(await moveNextPromise);
                }
                else
                {
                    Assert.True(channel.Writer.TryClose());
                    Assert.False(await enumerator.MoveNextAsync());
                }

                await enumerator.DisposeAsync();

                channel.Dispose();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void ReadAllAsync_AvailableData()
        {
            Promise.Run(async () =>
            {
                var options = new BoundedChannelOptions<int>() { Capacity = 3, FullMode = BoundedChannelFullMode.Wait };
                var channel = Channel<int>.NewBounded(options);
                var enumerator = channel.Reader.ReadAllAsync().GetAsyncEnumerator();

                for (int i = 100; i < 110; i++)
                {
                    Assert.AreEqual(ChannelWriteResult.Success, (await channel.Writer.WriteAsync(i)).Result);
                    Assert.True(await enumerator.MoveNextAsync());
                    Assert.AreEqual(i, enumerator.Current);
                }

                await enumerator.DisposeAsync();

                channel.Dispose();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void ReadAllAsync_UnavailableData()
        {
            Promise.Run(async () =>
            {
                var options = new BoundedChannelOptions<int>() { Capacity = 3, FullMode = BoundedChannelFullMode.Wait };
                var channel = Channel<int>.NewBounded(options);
                var enumerator = channel.Reader.ReadAllAsync().GetAsyncEnumerator();

                for (int i = 100; i < 110; i++)
                {
                    var moveNextPromise = enumerator.MoveNextAsync();
                    Assert.AreEqual(ChannelWriteResult.Success, (await channel.Writer.WriteAsync(i)).Result);
                    Assert.True(await moveNextPromise);
                    Assert.AreEqual(i, enumerator.Current);
                }

                await enumerator.DisposeAsync();

                channel.Dispose();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void ReadAllAsync_ProducerConsumer_ConsumesAllData(
            [Values(0, 1, 128)] int items,
            [Values(SynchronizationOption.Synchronous, SynchronizationOption.Background)] SynchronizationOption synchronizationOption)
        {
            var options = new BoundedChannelOptions<int>() { Capacity = 3, FullMode = BoundedChannelFullMode.Wait };
            var channel = Channel<int>.NewBounded(options);

            int producedTotal = 0, consumedTotal = 0;
            Promise.All(
                Promise.Run(async () =>
                {
                    for (int i = 0; i < items; i++)
                    {
                        Assert.AreEqual(ChannelWriteResult.Success, (await channel.Writer.WriteAsync(i)).Result);
                        producedTotal += i;
                    }
                    Assert.True(channel.Writer.TryClose());
                }, synchronizationOption),
                Promise.Run(async () =>
                {
                    var enumerator = channel.Reader.ReadAllAsync().GetAsyncEnumerator();
                    try
                    {
                        while (await enumerator.MoveNextAsync())
                        {
                            consumedTotal += enumerator.Current;
                        }
                    }
                    finally
                    {
                        await enumerator.DisposeAsync();
                    }
                }, synchronizationOption)
            )
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds((items * 2) + 1));

            channel.Dispose();

            Assert.AreEqual(producedTotal, consumedTotal);
        }

        [Test]
        public void ReadAllAsync_DualConcurrentEnumeration_AllItemsEnumerated()
        {
            Promise.Run(async () =>
            {
                var options = new BoundedChannelOptions<int>() { Capacity = 3, FullMode = BoundedChannelFullMode.Wait };
                var channel = Channel<int>.NewBounded(options);
                var enumerator1 = channel.Reader.ReadAllAsync().GetAsyncEnumerator();
                var enumerator2 = channel.Reader.ReadAllAsync().GetAsyncEnumerator();

                Promise<bool> moveNextPromise1, moveNextPromise2;
                int producerTotal = 0, consumerTotal = 0;
                for (int i = 0; i < 10; i++)
                {
                    moveNextPromise1 = enumerator1.MoveNextAsync();
                    moveNextPromise2 = enumerator2.MoveNextAsync();

                    Assert.AreEqual(ChannelWriteResult.Success, (await channel.Writer.WriteAsync(i)).Result);
                    producerTotal += i;
                    Assert.AreEqual(ChannelWriteResult.Success, (await channel.Writer.WriteAsync(i * 2)).Result);
                    producerTotal += i * 2;

                    Assert.True(await moveNextPromise1);
                    Assert.True(await moveNextPromise2);
                    consumerTotal += enumerator1.Current;
                    consumerTotal += enumerator2.Current;
                }

                moveNextPromise1 = enumerator1.MoveNextAsync();
                moveNextPromise2 = enumerator2.MoveNextAsync();
                Assert.True(channel.Writer.TryClose());
                Assert.False(await moveNextPromise1);
                Assert.False(await moveNextPromise2);

                Assert.AreEqual(producerTotal, consumerTotal);

                await enumerator1.DisposeAsync();
                await enumerator2.DisposeAsync();
                channel.Dispose();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void ReadAllAsync_CanceledBeforeMoveNextAsync_Throws(
            [Values] bool dataAvailable)
        {
            Promise.Run(async () =>
            {
                var options = new BoundedChannelOptions<int>() { Capacity = 3, FullMode = BoundedChannelFullMode.Wait };
                var channel = Channel<int>.NewBounded(options);
                if (dataAvailable)
                {
                    Assert.AreEqual(ChannelWriteResult.Success, (await channel.Writer.WriteAsync(42)).Result);
                }

                var cancelationSource = CancelationSource.New();

                var enumerator = channel.Reader.ReadAllAsync().WithCancelation(cancelationSource.Token).GetAsyncEnumerator();
                cancelationSource.Cancel();

                await TestHelper.AssertCanceledAsync(() => enumerator.MoveNextAsync());

                await enumerator.DisposeAsync();
                cancelationSource.Dispose();
                channel.Dispose();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void ReadAllAsync_CanceledAfterMoveNextAsync_Throws()
        {
            Promise.Run(async () =>
            {
                var options = new BoundedChannelOptions<int>() { Capacity = 3, FullMode = BoundedChannelFullMode.Wait };
                var channel = Channel<int>.NewBounded(options);

                var cancelationSource = CancelationSource.New();

                var enumerator = channel.Reader.ReadAllAsync().WithCancelation(cancelationSource.Token).GetAsyncEnumerator();
                var moveNextPromise = enumerator.MoveNextAsync();
                cancelationSource.Cancel();

                await TestHelper.AssertCanceledAsync(() => moveNextPromise);

                await enumerator.DisposeAsync();
                cancelationSource.Dispose();
                channel.Dispose();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void WriteAsync_ContinuesOnConfiguredContext_await(
            [Values] SynchronizationType continuationContext,
            [Values] CompletedContinuationBehavior completedBehavior,
            [Values(SynchronizationType.Foreground
#if !UNITY_WEBGL
            , SynchronizationType.Background
#endif
            )] SynchronizationType invokeContext,
            [Values] bool withCancelationToken)
        {
            var foregroundThread = Thread.CurrentThread;

            var options = new BoundedChannelOptions<int>() { Capacity = 1, FullMode = BoundedChannelFullMode.Wait };
            var channel = Channel<int>.NewBounded(options);
            var cancelationSource = CancelationSource.New();

            Assert.AreEqual(ChannelWriteResult.Success, channel.Writer.TryWrite(1).Result);

            var promise = Promise.Run(async () =>
            {
                if (withCancelationToken)
                {
                    var writeResult = await channel.Writer.WriteAsync(2, cancelationSource.Token, TestHelper.GetContinuationOptions(continuationContext, completedBehavior));
                    Assert.AreEqual(ChannelWriteResult.Success, writeResult.Result);
                }
                else
                {
                    var writeResult = await channel.Writer.WriteAsync(2, TestHelper.GetContinuationOptions(continuationContext, completedBehavior));
                    Assert.AreEqual(ChannelWriteResult.Success, writeResult.Result);
                }
                TestHelper.AssertCallbackContext(continuationContext, invokeContext, foregroundThread);
            }, SynchronizationOption.Synchronous);

            new ThreadHelper().ExecuteSynchronousOrOnThread(
                () => channel.Reader.TryRead(),
                invokeContext == SynchronizationType.Foreground
            );
            promise.WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
            cancelationSource.Dispose();
            channel.Dispose();
        }

        [Test]
        public void WaitToWriteAsync_ContinuesOnConfiguredContext_await(
            [Values] SynchronizationType continuationContext,
            [Values] CompletedContinuationBehavior completedBehavior,
            [Values(SynchronizationType.Foreground
#if !UNITY_WEBGL
            , SynchronizationType.Background
#endif
            )] SynchronizationType invokeContext,
            [Values] bool withCancelationToken)
        {
            var foregroundThread = Thread.CurrentThread;

            var options = new BoundedChannelOptions<int>() { Capacity = 1, FullMode = BoundedChannelFullMode.Wait };
            var channel = Channel<int>.NewBounded(options);
            var cancelationSource = CancelationSource.New();

            Assert.AreEqual(ChannelWriteResult.Success, channel.Writer.TryWrite(1).Result);

            var promise = Promise.Run(async () =>
            {
                if (withCancelationToken)
                {
                    var canWrite = await channel.Writer.WaitToWriteAsync(cancelationSource.Token, TestHelper.GetContinuationOptions(continuationContext, completedBehavior));
                    Assert.True(canWrite);
                }
                else
                {
                    var canWrite = await channel.Writer.WaitToWriteAsync(TestHelper.GetContinuationOptions(continuationContext, completedBehavior));
                    Assert.True(canWrite);
                }
                TestHelper.AssertCallbackContext(continuationContext, invokeContext, foregroundThread);
            }, SynchronizationOption.Synchronous);

            new ThreadHelper().ExecuteSynchronousOrOnThread(
                () => channel.Reader.TryRead(),
                invokeContext == SynchronizationType.Foreground
            );
            promise.WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
            cancelationSource.Dispose();
            channel.Dispose();
        }

        [Test]
        public void ReadAsync_ContinuesOnConfiguredContext_await(
            [Values] SynchronizationType continuationContext,
            [Values] CompletedContinuationBehavior completedBehavior,
            [Values(SynchronizationType.Foreground
#if !UNITY_WEBGL
            , SynchronizationType.Background
#endif
            )] SynchronizationType invokeContext,
            [Values] bool withCancelationToken)
        {
            var foregroundThread = Thread.CurrentThread;

            var options = new BoundedChannelOptions<int>() { Capacity = 1, FullMode = BoundedChannelFullMode.Wait };
            var channel = Channel<int>.NewBounded(options);
            var cancelationSource = CancelationSource.New();

            var promise = Promise.Run(async () =>
            {
                if (withCancelationToken)
                {
                    var readResult = await channel.Reader.ReadAsync(cancelationSource.Token, TestHelper.GetContinuationOptions(continuationContext, completedBehavior));
                    Assert.True(readResult.TryGetItem(out var item));
                    Assert.AreEqual(1, item);
                }
                else
                {
                    var readResult = await channel.Reader.ReadAsync(TestHelper.GetContinuationOptions(continuationContext, completedBehavior));
                    Assert.True(readResult.TryGetItem(out var item));
                    Assert.AreEqual(1, item);
                }
                TestHelper.AssertCallbackContext(continuationContext, invokeContext, foregroundThread);
            }, SynchronizationOption.Synchronous);

            new ThreadHelper().ExecuteSynchronousOrOnThread(
                () => channel.Writer.TryWrite(1),
                invokeContext == SynchronizationType.Foreground
            );
            promise.WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
            cancelationSource.Dispose();
            channel.Dispose();
        }

        [Test]
        public void WaitToReadAsync_ContinuesOnConfiguredContext_await(
            [Values] SynchronizationType continuationContext,
            [Values] CompletedContinuationBehavior completedBehavior,
            [Values(SynchronizationType.Foreground
#if !UNITY_WEBGL
            , SynchronizationType.Background
#endif
            )] SynchronizationType invokeContext,
            [Values] bool withCancelationToken)
        {
            var foregroundThread = Thread.CurrentThread;

            var options = new BoundedChannelOptions<int>() { Capacity = 1, FullMode = BoundedChannelFullMode.Wait };
            var channel = Channel<int>.NewBounded(options);
            var cancelationSource = CancelationSource.New();

            var promise = Promise.Run(async () =>
            {
                if (withCancelationToken)
                {
                    var canRead = await channel.Reader.WaitToReadAsync(cancelationSource.Token, TestHelper.GetContinuationOptions(continuationContext, completedBehavior));
                    Assert.True(canRead);
                }
                else
                {
                    var canRead = await channel.Reader.WaitToReadAsync(TestHelper.GetContinuationOptions(continuationContext, completedBehavior));
                    Assert.True(canRead);
                }
                TestHelper.AssertCallbackContext(continuationContext, invokeContext, foregroundThread);
            }, SynchronizationOption.Synchronous);

            new ThreadHelper().ExecuteSynchronousOrOnThread(
                () => channel.Writer.TryWrite(1),
                invokeContext == SynchronizationType.Foreground
            );
            promise.WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
            cancelationSource.Dispose();
            channel.Dispose();
        }
    }
}