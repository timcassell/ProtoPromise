#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using NUnit.Framework;
using Proto.Promises;
using Proto.Promises.CompilerServices;
using Proto.Promises.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace ProtoPromise.Tests.APIs.Linq
{
    public class ZipTests
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
        public void Zip_Tuple2_EqualLength()
        {
            Promise.Run(async () =>
            {
                var xs = new[] { 1, 2, 3 }.ToAsyncEnumerable();
                var ys = new[] { 4, 5, 6 }.ToAsyncEnumerable();
                var asyncEnumerator = xs.Zip(ys).GetAsyncEnumerator();
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual((1, 4), asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual((2, 5), asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual((3, 6), asyncEnumerator.Current);
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Zip_Tuple2_LeftShorter()
        {
            Promise.Run(async () =>
            {
                var xs = new[] { 1, 2, 3 }.ToAsyncEnumerable();
                var ys = new[] { 4, 5, 6, 7 }.ToAsyncEnumerable();
                var asyncEnumerator = xs.Zip(ys).GetAsyncEnumerator();
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual((1, 4), asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual((2, 5), asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual((3, 6), asyncEnumerator.Current);
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Zip_Tuple2_RightShorter()
        {
            Promise.Run(async () =>
            {
                var xs = new[] { 1, 2, 3, 4 }.ToAsyncEnumerable();
                var ys = new[] { 4, 5, 6 }.ToAsyncEnumerable();
                var asyncEnumerator = xs.Zip(ys).GetAsyncEnumerator();
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual((1, 4), asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual((2, 5), asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual((3, 6), asyncEnumerator.Current);
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Zip_Tuple2_Throws_Right()
        {
            Promise.Run(async () =>
            {
                var ex = new Exception("Bang!");
                var xs = new[] { 1, 2, 3, 4 }.ToAsyncEnumerable();
                var ys = AsyncEnumerable<int>.Rejected(ex);
                var asyncEnumerator = xs.Zip(ys).GetAsyncEnumerator();
                await TestHelper.AssertThrowsAsync(() => asyncEnumerator.MoveNextAsync(), ex);
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Zip_Tuple2_Throws_Left()
        {
            Promise.Run(async () =>
            {
                var ex = new Exception("Bang!");
                var xs = AsyncEnumerable<int>.Rejected(ex);
                var ys = new[] { 1, 2, 3, 4 }.ToAsyncEnumerable();
                var asyncEnumerator = xs.Zip(ys).GetAsyncEnumerator();
                await TestHelper.AssertThrowsAsync(() => asyncEnumerator.MoveNextAsync(), ex);
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Zip_Tuple3_EqualLength()
        {
            Promise.Run(async () =>
            {
                var xs1 = new[] { 1, 2, 3 }.ToAsyncEnumerable();
                var xs2 = new[] { 4, 5, 6 }.ToAsyncEnumerable();
                var xs3 = new[] { 7, 8, 9 }.ToAsyncEnumerable();
                var asyncEnumerator = xs1.Zip(xs2, xs3).GetAsyncEnumerator();
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual((1, 4, 7), asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual((2, 5, 8), asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual((3, 6, 9), asyncEnumerator.Current);
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Zip_Tuple3_LeftShorter()
        {
            Promise.Run(async () =>
            {
                var xs1 = new[] { 1, 2 }.ToAsyncEnumerable();
                var xs2 = new[] { 4, 5, 6 }.ToAsyncEnumerable();
                var xs3 = new[] { 7, 8, 9 }.ToAsyncEnumerable();
                var asyncEnumerator = xs1.Zip(xs2, xs3).GetAsyncEnumerator();
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual((1, 4, 7), asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual((2, 5, 8), asyncEnumerator.Current);
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Zip_Tuple3_MiddleShorter()
        {
            Promise.Run(async () =>
            {
                var xs1 = new[] { 1, 2, 3 }.ToAsyncEnumerable();
                var xs2 = new[] { 4, 5 }.ToAsyncEnumerable();
                var xs3 = new[] { 7, 8, 9 }.ToAsyncEnumerable();
                var asyncEnumerator = xs1.Zip(xs2, xs3).GetAsyncEnumerator();
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual((1, 4, 7), asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual((2, 5, 8), asyncEnumerator.Current);
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Zip_Tuple3_RightShorter()
        {
            Promise.Run(async () =>
            {
                var xs1 = new[] { 1, 2, 3 }.ToAsyncEnumerable();
                var xs2 = new[] { 4, 5, 6 }.ToAsyncEnumerable();
                var xs3 = new[] { 7, 8 }.ToAsyncEnumerable();
                var asyncEnumerator = xs1.Zip(xs2, xs3).GetAsyncEnumerator();
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual((1, 4, 7), asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual((2, 5, 8), asyncEnumerator.Current);
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Zip_Tuple3_Throws_Right()
        {
            Promise.Run(async () =>
            {
                var ex = new Exception("Bang!");
                var xs1 = new[] { 1, 2, 3 }.ToAsyncEnumerable();
                var xs2 = new[] { 4, 5, 6 }.ToAsyncEnumerable();
                var xs3 = AsyncEnumerable<int>.Rejected(ex);
                var asyncEnumerator = xs1.Zip(xs2, xs3).GetAsyncEnumerator();
                await TestHelper.AssertThrowsAsync(() => asyncEnumerator.MoveNextAsync(), ex);
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Zip_Tuple3_Throws_Middle()
        {
            Promise.Run(async () =>
            {
                var ex = new Exception("Bang!");
                var xs1 = new[] { 1, 2, 3 }.ToAsyncEnumerable();
                var xs2 = AsyncEnumerable<int>.Rejected(ex);
                var xs3 = new[] { 4, 5, 6 }.ToAsyncEnumerable();
                var asyncEnumerator = xs1.Zip(xs2, xs3).GetAsyncEnumerator();
                await TestHelper.AssertThrowsAsync(() => asyncEnumerator.MoveNextAsync(), ex);
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Zip_Tuple3_Throws_Left()
        {
            Promise.Run(async () =>
            {
                var ex = new Exception("Bang!");
                var xs1 = AsyncEnumerable<int>.Rejected(ex);
                var xs2 = new[] { 1, 2, 3 }.ToAsyncEnumerable();
                var xs3 = new[] { 4, 5, 6 }.ToAsyncEnumerable();
                var asyncEnumerator = xs1.Zip(xs2, xs3).GetAsyncEnumerator();
                await TestHelper.AssertThrowsAsync(() => asyncEnumerator.MoveNextAsync(), ex);
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        public enum ConfiguredType
        {
            NotConfigured,
            Configured,
            ConfiguredWithCancelation
        }

        [Test]
        public void Zip_2_Cancel(
            [Values(0, 1)] int cancelSequence)
        {
            Promise.Run(async () =>
            {
                var xs1 = AsyncEnumerable.Create<int>(async (writer, cancelationToken) =>
                {
                    await writer.YieldAsync(1);
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(2);
                    await writer.YieldAsync(3);
                });
                var xs2 = AsyncEnumerable.Create<int>(async (writer, cancelationToken) =>
                {
                    await writer.YieldAsync(4);
                    await writer.YieldAsync(5);
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(6);
                });
                using (var cancelationSource = CancelationSource.New())
                {
                    var asyncEnumerator = xs1.Zip(xs2).GetAsyncEnumerator(cancelationSource.Token);
                    try
                    {
                        Assert.True(await asyncEnumerator.MoveNextAsync());
                        Assert.AreEqual((1, 4), asyncEnumerator.Current);
                        if (cancelSequence != 0)
                        {
                            Assert.True(await asyncEnumerator.MoveNextAsync());
                            Assert.AreEqual((2, 5), asyncEnumerator.Current);
                        }
                        cancelationSource.Cancel();
                        await TestHelper.AssertCanceledAsync(() => asyncEnumerator.MoveNextAsync());
                    }
                    finally
                    {
                        await asyncEnumerator.DisposeAsync();
                    }
                }
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Zip_3_Cancel(
            [Values(0, 1, 2)] int cancelSequence)
        {
            Promise.Run(async () =>
            {
                var xs1 = AsyncEnumerable.Create<int>(async (writer, cancelationToken) =>
                {
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(1);
                    await writer.YieldAsync(2);
                    await writer.YieldAsync(3);
                });
                var xs2 = AsyncEnumerable.Create<int>(async (writer, cancelationToken) =>
                {
                    await writer.YieldAsync(4);
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(5);
                    await writer.YieldAsync(6);
                });
                var xs3 = AsyncEnumerable.Create<int>(async (writer, cancelationToken) =>
                {
                    await writer.YieldAsync(7);
                    await writer.YieldAsync(8);
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(9);
                });
                using (var cancelationSource = CancelationSource.New())
                {
                    var asyncEnumerator = xs1.Zip(xs2, xs3).GetAsyncEnumerator(cancelationSource.Token);
                    try
                    {
                        if (cancelSequence != 0)
                        {
                            Assert.True(await asyncEnumerator.MoveNextAsync());
                            Assert.AreEqual((1, 4, 7), asyncEnumerator.Current);
                            if (cancelSequence != 1)
                            {
                                Assert.True(await asyncEnumerator.MoveNextAsync());
                                Assert.AreEqual((2, 5, 8), asyncEnumerator.Current);
                            }
                        }
                        cancelationSource.Cancel();
                        await TestHelper.AssertCanceledAsync(() => asyncEnumerator.MoveNextAsync());
                    }
                    finally
                    {
                        await asyncEnumerator.DisposeAsync();
                    }
                }
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }
    }
}