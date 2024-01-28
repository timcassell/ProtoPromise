#if CSHARP_7_3_OR_NEWER

#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using NUnit.Framework;
using Proto.Promises;
using Proto.Promises.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace ProtoPromiseTests.APIs.Linq
{
    public class ChunkTests
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
        public void Chunk_ThrowsWhenSizeIsNonPositive()
        {
            var enumerable = AsyncEnumerable.Range(0, 10);

            Assert.Catch<System.ArgumentOutOfRangeException>(() => enumerable.Chunk(0));
            Assert.Catch<System.ArgumentOutOfRangeException>(() => enumerable.Chunk(-1));
            Assert.Catch<System.ArgumentOutOfRangeException>(() => enumerable.Chunk(0, true));
            Assert.Catch<System.ArgumentOutOfRangeException>(() => enumerable.Chunk(-1, true));

            enumerable.GetAsyncEnumerator().DisposeAsync().Forget();
        }

        [Test]
        public void Chunk_Lazy(
            [Values] bool allowSameStorage)
        {
            Promise.Run(async () =>
            {
                var chunksEnumerator = AsyncEnumerable.Range(0, 100)
                    .Chunk(5, allowSameStorage)
                    .GetAsyncEnumerator();
                Assert.True(await chunksEnumerator.MoveNextAsync());
                CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 4 }, chunksEnumerator.Current);
                await chunksEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Chunk_Even(
            [Values] bool allowSameStorage)
        {
            Promise.Run(async () =>
            {
                var chunksEnumerator = new[] { 9999, 0, 888, -1, 66, -777, 1, 2, -12345 }.ToAsyncEnumerable()
                    .Chunk(3, allowSameStorage)
                    .GetAsyncEnumerator();
                Assert.True(await chunksEnumerator.MoveNextAsync());
                CollectionAssert.AreEqual(new[] { 9999, 0, 888 }, chunksEnumerator.Current);
                Assert.True(await chunksEnumerator.MoveNextAsync());
                CollectionAssert.AreEqual(new[] { -1, 66, -777 }, chunksEnumerator.Current);
                Assert.True(await chunksEnumerator.MoveNextAsync());
                CollectionAssert.AreEqual(new[] { 1, 2, -12345 }, chunksEnumerator.Current);
                Assert.False(await chunksEnumerator.MoveNextAsync());
                await chunksEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Chunk_Uneven(
            [Values] bool allowSameStorage)
        {
            Promise.Run(async () =>
            {
                var chunksEnumerator = new[] { 9999, 0, 888, -1, 66, -777, 1, 2 }.ToAsyncEnumerable()
                    .Chunk(3, allowSameStorage)
                    .GetAsyncEnumerator();
                Assert.True(await chunksEnumerator.MoveNextAsync());
                CollectionAssert.AreEqual(new[] { 9999, 0, 888 }, chunksEnumerator.Current);
                Assert.True(await chunksEnumerator.MoveNextAsync());
                CollectionAssert.AreEqual(new[] { -1, 66, -777 }, chunksEnumerator.Current);
                Assert.True(await chunksEnumerator.MoveNextAsync());
                CollectionAssert.AreEqual(new[] { 1, 2 }, chunksEnumerator.Current);
                Assert.False(await chunksEnumerator.MoveNextAsync());
                await chunksEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Chunk_SourceSmallerThanMaxSize(
            [Values] bool allowSameStorage)
        {
            Promise.Run(async () =>
            {
                var chunksEnumerator = new[] { 9999, 0 }.ToAsyncEnumerable()
                    .Chunk(3, allowSameStorage)
                    .GetAsyncEnumerator();
                Assert.True(await chunksEnumerator.MoveNextAsync());
                CollectionAssert.AreEqual(new[] { 9999, 0 }, chunksEnumerator.Current);
                Assert.False(await chunksEnumerator.MoveNextAsync());
                await chunksEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Chunk_Empty(
            [Values] bool allowSameStorage)
        {
            Promise.Run(async () =>
            {
                var chunksEnumerator = AsyncEnumerable<int>.Empty()
                    .Chunk(3, allowSameStorage)
                    .GetAsyncEnumerator();
                Assert.False(await chunksEnumerator.MoveNextAsync());
                await chunksEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Chunk_DoesNotPrematurelyAllocateHugeArray(
            [Values] bool allowSameStorage)
        {
            Promise.Run(async () =>
            {
                var chunksEnumerator = AsyncEnumerable.Range(0, 10)
                    .Chunk(int.MaxValue, allowSameStorage)
                    .GetAsyncEnumerator();
                Assert.True(await chunksEnumerator.MoveNextAsync());
                CollectionAssert.AreEqual(Enumerable.Range(0, 10), chunksEnumerator.Current);
                Assert.False(await chunksEnumerator.MoveNextAsync());
                await chunksEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Chunk_Cancel(
            [Values] bool allowSameStorage)
        {
            Promise.Run(async () =>
            {
                var xs = AsyncEnumerable.Create<int>(async (writer, cancelationToken) =>
                {
                    foreach (var num in new[] { 9999, 0, 888, -1, 66, -777, 1, 2, -12345 })
                    {
                        cancelationToken.ThrowIfCancelationRequested();
                        await writer.YieldAsync(num);
                    }
                });
                using (var cancelationSource = CancelationSource.New())
                {
                    var chunksEnumerator = xs
                        .Chunk(3, allowSameStorage)
                        .GetAsyncEnumerator(cancelationSource.Token);
                    Assert.True(await chunksEnumerator.MoveNextAsync());
                    CollectionAssert.AreEqual(new[] { 9999, 0, 888 }, chunksEnumerator.Current);
                    Assert.True(await chunksEnumerator.MoveNextAsync());
                    CollectionAssert.AreEqual(new[] { -1, 66, -777 }, chunksEnumerator.Current);
                    cancelationSource.Cancel();
                    await TestHelper.AssertCanceledAsync(() => chunksEnumerator.MoveNextAsync());
                    await chunksEnumerator.DisposeAsync();
                }
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }
    }
}

#endif