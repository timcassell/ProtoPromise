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
    public class ConcatTests
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
        public void Concat_Simple()
        {
            Promise.Run(async () =>
            {
                var asyncEnumerator = new[] { 1, 2, 3 }.ToAsyncEnumerable()
                    .Concat(new[] { 4, 5, 6 }.ToAsyncEnumerable())
                    .GetAsyncEnumerator();
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(1, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(2, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(3, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(4, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(5, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(6, asyncEnumerator.Current);
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Concat_Throw_Second()
        {
            Promise.Run(async () =>
            {
                var ex = new Exception("Bang");
                var asyncEnumerator = new[] { 1, 2, 3 }.ToAsyncEnumerable()
                    .Concat(AsyncEnumerable<int>.Rejected(ex))
                    .GetAsyncEnumerator();
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(1, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(2, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(3, asyncEnumerator.Current);
                await TestHelper.AssertThrowsAsync(() => asyncEnumerator.MoveNextAsync(), ex);
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Concat_Throw_First()
        {
            Promise.Run(async () =>
            {
                var ex = new Exception("Bang");
                var asyncEnumerator = AsyncEnumerable<int>.Rejected(ex)
                    .Concat(new[] { 1, 2, 3 }.ToAsyncEnumerable())
                    .GetAsyncEnumerator();
                await TestHelper.AssertThrowsAsync(() => asyncEnumerator.MoveNextAsync(), ex);
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Concat_3()
        {
            Promise.Run(async () =>
            {
                var asyncEnumerator = new[] { 1, 2, 3 }.ToAsyncEnumerable()
                    .Concat(new[] { 4, 5 }.ToAsyncEnumerable())
                    .Concat(new[] { 6, 7, 8 }.ToAsyncEnumerable())
                    .GetAsyncEnumerator();
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(1, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(2, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(3, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(4, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(5, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(6, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(7, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(8, asyncEnumerator.Current);
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Concat_ConcatedSecond()
        {
            Promise.Run(async () =>
            {
                var asyncEnumerator = new[] { 1, 2, 3 }.ToAsyncEnumerable()
                    .Concat(
                        new[] { 4, 5 }.ToAsyncEnumerable()
                        .Concat(new[] { 6, 7, 8 }.ToAsyncEnumerable())
                    )
                    .GetAsyncEnumerator();
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(1, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(2, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(3, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(4, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(5, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(6, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(7, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(8, asyncEnumerator.Current);
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Concat_ConcatedBoth()
        {
            Promise.Run(async () =>
            {
                var asyncEnumerator = new[] { 1, 2 }.ToAsyncEnumerable()
                    .Concat(new[] { 3, 4 }.ToAsyncEnumerable())
                    .Concat(
                        new[] { 5, 6 }.ToAsyncEnumerable()
                        .Concat(new[] { 7, 8 }.ToAsyncEnumerable())
                    )
                    .GetAsyncEnumerator();
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(1, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(2, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(3, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(4, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(5, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(6, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(7, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(8, asyncEnumerator.Current);
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Concat_4()
        {
            Promise.Run(async () =>
            {
                var asyncEnumerator = new[] { 1, 2 }.ToAsyncEnumerable()
                    .Concat(new[] { 3, 4 }.ToAsyncEnumerable())
                    .Concat(new[] { 5, 6 }.ToAsyncEnumerable())
                    .Concat(new[] { 7, 8 }.ToAsyncEnumerable())
                    .GetAsyncEnumerator();
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(1, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(2, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(3, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(4, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(5, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(6, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(7, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(8, asyncEnumerator.Current);
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Concat_ConcatedBoth3_2()
        {
            Promise.Run(async () =>
            {
                var asyncEnumerator = new[] { 1, 2 }.ToAsyncEnumerable()
                    .Concat(new[] { 3 }.ToAsyncEnumerable())
                    .Concat(new[] { 4 }.ToAsyncEnumerable())
                    .Concat(
                        new[] { 5, 6 }.ToAsyncEnumerable()
                        .Concat(new[] { 7, 8 }.ToAsyncEnumerable())
                    )
                    .GetAsyncEnumerator();
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(1, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(2, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(3, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(4, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(5, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(6, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(7, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(8, asyncEnumerator.Current);
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Concat_ConcatedBoth2_3()
        {
            Promise.Run(async () =>
            {
                var asyncEnumerator = new[] { 1, 2 }.ToAsyncEnumerable()
                    .Concat(new[] { 3, 4 }.ToAsyncEnumerable())
                    .Concat(
                        new[] { 5, 6 }.ToAsyncEnumerable()
                        .Concat(new[] { 7 }.ToAsyncEnumerable())
                        .Concat(new[] { 8 }.ToAsyncEnumerable())
                    )
                    .GetAsyncEnumerator();
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(1, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(2, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(3, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(4, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(5, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(6, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(7, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(8, asyncEnumerator.Current);
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Concat_ConcatedBoth3_3()
        {
            Promise.Run(async () =>
            {
                var asyncEnumerator = new[] { 1, 2 }.ToAsyncEnumerable()
                    .Concat(new[] { 3 }.ToAsyncEnumerable())
                    .Concat(new[] { 4 }.ToAsyncEnumerable())
                    .Concat(
                        new[] { 5, 6 }.ToAsyncEnumerable()
                        .Concat(new[] { 7 }.ToAsyncEnumerable())
                        .Concat(new[] { 8 }.ToAsyncEnumerable())
                    )
                    .GetAsyncEnumerator();
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(1, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(2, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(3, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(4, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(5, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(6, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(7, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(8, asyncEnumerator.Current);
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Concat_ConcatedBoth2_3_1()
        {
            Promise.Run(async () =>
            {
                var asyncEnumerator = new[] { 1, 2 }.ToAsyncEnumerable()
                    .Concat(new[] { 3 }.ToAsyncEnumerable())
                    .Concat(
                        new[] { 4, 5 }.ToAsyncEnumerable()
                        .Concat(new[] { 6 }.ToAsyncEnumerable())
                        .Concat(new[] { 7 }.ToAsyncEnumerable())
                    )
                    .Concat(new[] { 8 }.ToAsyncEnumerable())
                    .GetAsyncEnumerator();
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(1, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(2, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(3, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(4, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(5, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(6, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(7, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(8, asyncEnumerator.Current);
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Concat_Simple_Cancel(
            [Values(0, 1)] int cancelSequence)
        {
            Promise.Run(async () =>
            {
                var xs1 = AsyncEnumerable.Create<int>(async (writer, cancelationToken) =>
                {
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(1);
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(2);
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(3);
                });
                var xs2 = AsyncEnumerable.Create<int>(async (writer, cancelationToken) =>
                {
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(4);
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(5);
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(6);
                });
                using (var cancelationSource = CancelationSource.New())
                {
                    var asyncEnumerator = xs1
                        .Concat(xs2)
                        .GetAsyncEnumerator(cancelationSource.Token);
                    try
                    {
                        Assert.True(await asyncEnumerator.MoveNextAsync());
                        Assert.AreEqual(1, asyncEnumerator.Current);
                        Assert.True(await asyncEnumerator.MoveNextAsync());
                        Assert.AreEqual(2, asyncEnumerator.Current);
                        if (cancelSequence == 0)
                        {
                            cancelationSource.Cancel();
                            return;
                        }
                        Assert.True(await asyncEnumerator.MoveNextAsync());
                        Assert.AreEqual(3, asyncEnumerator.Current);
                        Assert.True(await asyncEnumerator.MoveNextAsync());
                        Assert.AreEqual(4, asyncEnumerator.Current);
                        Assert.True(await asyncEnumerator.MoveNextAsync());
                        Assert.AreEqual(5, asyncEnumerator.Current);
                        cancelationSource.Cancel();
                    }
                    finally
                    {
                        await TestHelper.AssertCanceledAsync(() => asyncEnumerator.MoveNextAsync());
                        await asyncEnumerator.DisposeAsync();
                    }
                }
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Concat_3_Cancel(
            [Values(0, 1, 2)] int cancelSequence)
        {
            Promise.Run(async () =>
            {
                var xs1 = AsyncEnumerable.Create<int>(async (writer, cancelationToken) =>
                {
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(1);
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(2);
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(3);
                });
                var xs2 = AsyncEnumerable.Create<int>(async (writer, cancelationToken) =>
                {
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(4);
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(5);
                });
                var xs3 = AsyncEnumerable.Create<int>(async (writer, cancelationToken) =>
                {
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(6);
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(7);
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(8);
                });
                using (var cancelationSource = CancelationSource.New())
                {
                    var asyncEnumerator = xs1
                        .Concat(xs2)
                        .Concat(xs3)
                        .GetAsyncEnumerator(cancelationSource.Token);
                    try
                    {
                        Assert.True(await asyncEnumerator.MoveNextAsync());
                        Assert.AreEqual(1, asyncEnumerator.Current);
                        Assert.True(await asyncEnumerator.MoveNextAsync());
                        Assert.AreEqual(2, asyncEnumerator.Current);
                        if (cancelSequence == 0)
                        {
                            cancelationSource.Cancel();
                            return;
                        }
                        Assert.True(await asyncEnumerator.MoveNextAsync());
                        Assert.AreEqual(3, asyncEnumerator.Current);
                        Assert.True(await asyncEnumerator.MoveNextAsync());
                        Assert.AreEqual(4, asyncEnumerator.Current);
                        if (cancelSequence == 1)
                        {
                            cancelationSource.Cancel();
                            return;
                        }
                        Assert.True(await asyncEnumerator.MoveNextAsync());
                        Assert.AreEqual(5, asyncEnumerator.Current);
                        Assert.True(await asyncEnumerator.MoveNextAsync());
                        Assert.AreEqual(6, asyncEnumerator.Current);
                        cancelationSource.Cancel();
                    }
                    finally
                    {
                        await TestHelper.AssertCanceledAsync(() => asyncEnumerator.MoveNextAsync());
                        await asyncEnumerator.DisposeAsync();
                    }
                }
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Concat_ConcatedSecond_Cancel(
            [Values(0, 1, 2)] int cancelSequence)
        {
            Promise.Run(async () =>
            {
                var xs1 = AsyncEnumerable.Create<int>(async (writer, cancelationToken) =>
                {
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(1);
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(2);
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(3);
                });
                var xs2 = AsyncEnumerable.Create<int>(async (writer, cancelationToken) =>
                {
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(4);
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(5);
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(6);
                });
                var xs3 = AsyncEnumerable.Create<int>(async (writer, cancelationToken) =>
                {
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(7);
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(8);
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(9);
                });
                using (var cancelationSource = CancelationSource.New())
                {
                    var asyncEnumerator = xs1
                        .Concat(
                            xs2
                            .Concat(xs3)
                        )
                        .GetAsyncEnumerator(cancelationSource.Token);
                    try
                    {
                        Assert.True(await asyncEnumerator.MoveNextAsync());
                        Assert.AreEqual(1, asyncEnumerator.Current);
                        Assert.True(await asyncEnumerator.MoveNextAsync());
                        Assert.AreEqual(2, asyncEnumerator.Current);
                        if (cancelSequence == 0)
                        {
                            cancelationSource.Cancel();
                            return;
                        }
                        Assert.True(await asyncEnumerator.MoveNextAsync());
                        Assert.AreEqual(3, asyncEnumerator.Current);
                        Assert.True(await asyncEnumerator.MoveNextAsync());
                        Assert.AreEqual(4, asyncEnumerator.Current);
                        if (cancelSequence == 1)
                        {
                            cancelationSource.Cancel();
                            return;
                        }
                        Assert.True(await asyncEnumerator.MoveNextAsync());
                        Assert.AreEqual(5, asyncEnumerator.Current);
                        Assert.True(await asyncEnumerator.MoveNextAsync());
                        Assert.AreEqual(6, asyncEnumerator.Current);
                        Assert.True(await asyncEnumerator.MoveNextAsync());
                        Assert.AreEqual(7, asyncEnumerator.Current);
                        cancelationSource.Cancel();
                    }
                    finally
                    {
                        await TestHelper.AssertCanceledAsync(() => asyncEnumerator.MoveNextAsync());
                        await asyncEnumerator.DisposeAsync();
                    }
                }
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Concat_ConcatedBoth_Cancel(
            [Values(0, 1, 2, 3)] int cancelSequence)
        {
            Promise.Run(async () =>
            {
                var xs1 = AsyncEnumerable.Create<int>(async (writer, cancelationToken) =>
                {
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(1);
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(2);
                });
                var xs2 = AsyncEnumerable.Create<int>(async (writer, cancelationToken) =>
                {
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(3);
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(4);
                });
                var xs3 = AsyncEnumerable.Create<int>(async (writer, cancelationToken) =>
                {
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(5);
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(6);
                });
                var xs4 = AsyncEnumerable.Create<int>(async (writer, cancelationToken) =>
                {
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(7);
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(8);
                });
                using (var cancelationSource = CancelationSource.New())
                {
                    var asyncEnumerator = xs1
                        .Concat(xs2)
                        .Concat(
                            xs3
                            .Concat(xs4)
                        )
                        .GetAsyncEnumerator(cancelationSource.Token);
                    try
                    {
                        Assert.True(await asyncEnumerator.MoveNextAsync());
                        Assert.AreEqual(1, asyncEnumerator.Current);
                        if (cancelSequence == 0)
                        {
                            cancelationSource.Cancel();
                            return;
                        }
                        Assert.True(await asyncEnumerator.MoveNextAsync());
                        Assert.AreEqual(2, asyncEnumerator.Current);
                        Assert.True(await asyncEnumerator.MoveNextAsync());
                        Assert.AreEqual(3, asyncEnumerator.Current);
                        if (cancelSequence == 1)
                        {
                            cancelationSource.Cancel();
                            return;
                        }
                        Assert.True(await asyncEnumerator.MoveNextAsync());
                        Assert.AreEqual(4, asyncEnumerator.Current);
                        Assert.True(await asyncEnumerator.MoveNextAsync());
                        Assert.AreEqual(5, asyncEnumerator.Current);
                        if (cancelSequence == 2)
                        {
                            cancelationSource.Cancel();
                            return;
                        }
                        Assert.True(await asyncEnumerator.MoveNextAsync());
                        Assert.AreEqual(6, asyncEnumerator.Current);
                        Assert.True(await asyncEnumerator.MoveNextAsync());
                        Assert.AreEqual(7, asyncEnumerator.Current);
                        cancelationSource.Cancel();
                    }
                    finally
                    {
                        await TestHelper.AssertCanceledAsync(() => asyncEnumerator.MoveNextAsync());
                        await asyncEnumerator.DisposeAsync();
                    }
                }
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Concat_4_Cancel(
            [Values(0, 1, 2, 3)] int cancelSequence)
        {
            Promise.Run(async () =>
            {
                var xs1 = AsyncEnumerable.Create<int>(async (writer, cancelationToken) =>
                {
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(1);
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(2);
                });
                var xs2 = AsyncEnumerable.Create<int>(async (writer, cancelationToken) =>
                {
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(3);
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(4);
                });
                var xs3 = AsyncEnumerable.Create<int>(async (writer, cancelationToken) =>
                {
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(5);
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(6);
                });
                var xs4 = AsyncEnumerable.Create<int>(async (writer, cancelationToken) =>
                {
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(7);
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(8);
                });
                using (var cancelationSource = CancelationSource.New())
                {
                    var asyncEnumerator = xs1
                        .Concat(xs2)
                        .Concat(xs3)
                        .Concat(xs4)
                        .GetAsyncEnumerator(cancelationSource.Token);
                    try
                    {
                        Assert.True(await asyncEnumerator.MoveNextAsync());
                        Assert.AreEqual(1, asyncEnumerator.Current);
                        if (cancelSequence == 0)
                        {
                            cancelationSource.Cancel();
                            return;
                        }
                        Assert.True(await asyncEnumerator.MoveNextAsync());
                        Assert.AreEqual(2, asyncEnumerator.Current);
                        Assert.True(await asyncEnumerator.MoveNextAsync());
                        Assert.AreEqual(3, asyncEnumerator.Current);
                        if (cancelSequence == 1)
                        {
                            cancelationSource.Cancel();
                            return;
                        }
                        Assert.True(await asyncEnumerator.MoveNextAsync());
                        Assert.AreEqual(4, asyncEnumerator.Current);
                        Assert.True(await asyncEnumerator.MoveNextAsync());
                        Assert.AreEqual(5, asyncEnumerator.Current);
                        if (cancelSequence == 2)
                        {
                            cancelationSource.Cancel();
                            return;
                        }
                        Assert.True(await asyncEnumerator.MoveNextAsync());
                        Assert.AreEqual(6, asyncEnumerator.Current);
                        Assert.True(await asyncEnumerator.MoveNextAsync());
                        Assert.AreEqual(7, asyncEnumerator.Current);
                        cancelationSource.Cancel();
                    }
                    finally
                    {
                        await TestHelper.AssertCanceledAsync(() => asyncEnumerator.MoveNextAsync());
                        await asyncEnumerator.DisposeAsync();
                    }
                }
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Concat_ConcatedBoth3_2_Cancel(
            [Values(0, 1, 2, 3, 4)] int cancelSequence)
        {
            Promise.Run(async () =>
            {
                var xs1 = AsyncEnumerable.Create<int>(async (writer, cancelationToken) =>
                {
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(1);
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(2);
                });
                var xs2 = AsyncEnumerable.Create<int>(async (writer, cancelationToken) =>
                {
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(3);
                });
                var xs3 = AsyncEnumerable.Create<int>(async (writer, cancelationToken) =>
                {
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(4);
                });
                var xs4 = AsyncEnumerable.Create<int>(async (writer, cancelationToken) =>
                {
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(5);
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(6);
                });
                var xs5 = AsyncEnumerable.Create<int>(async (writer, cancelationToken) =>
                {
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(7);
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(8);
                });
                using (var cancelationSource = CancelationSource.New())
                {
                    var asyncEnumerator = xs1
                        .Concat(xs2)
                        .Concat(xs3)
                        .Concat(
                            xs4
                            .Concat(xs5)
                        )
                        .GetAsyncEnumerator(cancelationSource.Token);
                    try
                    {
                        Assert.True(await asyncEnumerator.MoveNextAsync());
                        Assert.AreEqual(1, asyncEnumerator.Current);
                        if (cancelSequence == 0)
                        {
                            cancelationSource.Cancel();
                            return;
                        }
                        Assert.True(await asyncEnumerator.MoveNextAsync());
                        Assert.AreEqual(2, asyncEnumerator.Current);
                        if (cancelSequence == 1)
                        {
                            cancelationSource.Cancel();
                            return;
                        }
                        Assert.True(await asyncEnumerator.MoveNextAsync());
                        Assert.AreEqual(3, asyncEnumerator.Current);
                        if (cancelSequence == 2)
                        {
                            cancelationSource.Cancel();
                            return;
                        }
                        Assert.True(await asyncEnumerator.MoveNextAsync());
                        Assert.AreEqual(4, asyncEnumerator.Current);
                        Assert.True(await asyncEnumerator.MoveNextAsync());
                        Assert.AreEqual(5, asyncEnumerator.Current);
                        if (cancelSequence == 3)
                        {
                            cancelationSource.Cancel();
                            return;
                        }
                        Assert.True(await asyncEnumerator.MoveNextAsync());
                        Assert.AreEqual(6, asyncEnumerator.Current);
                        Assert.True(await asyncEnumerator.MoveNextAsync());
                        Assert.AreEqual(7, asyncEnumerator.Current);
                        cancelationSource.Cancel();
                    }
                    finally
                    {
                        await TestHelper.AssertCanceledAsync(() => asyncEnumerator.MoveNextAsync());
                        await asyncEnumerator.DisposeAsync();
                    }
                }
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Concat_ConcatedBoth2_3_Cancel(
            [Values(0, 1, 2, 3, 4)] int cancelSequence)
        {
            Promise.Run(async () =>
            {
                var xs1 = AsyncEnumerable.Create<int>(async (writer, cancelationToken) =>
                {
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(1);
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(2);
                });
                var xs2 = AsyncEnumerable.Create<int>(async (writer, cancelationToken) =>
                {
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(3);
                });
                var xs3 = AsyncEnumerable.Create<int>(async (writer, cancelationToken) =>
                {
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(4);
                });
                var xs4 = AsyncEnumerable.Create<int>(async (writer, cancelationToken) =>
                {
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(5);
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(6);
                });
                var xs5 = AsyncEnumerable.Create<int>(async (writer, cancelationToken) =>
                {
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(7);
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(8);
                });
                using (var cancelationSource = CancelationSource.New())
                {
                    var asyncEnumerator = xs1
                        .Concat(xs2)
                        .Concat(
                            xs3
                            .Concat(xs4)
                            .Concat(xs5)
                        )
                        .GetAsyncEnumerator(cancelationSource.Token);
                    try
                    {
                        Assert.True(await asyncEnumerator.MoveNextAsync());
                        Assert.AreEqual(1, asyncEnumerator.Current);
                        if (cancelSequence == 0)
                        {
                            cancelationSource.Cancel();
                            return;
                        }
                        Assert.True(await asyncEnumerator.MoveNextAsync());
                        Assert.AreEqual(2, asyncEnumerator.Current);
                        if (cancelSequence == 1)
                        {
                            cancelationSource.Cancel();
                            return;
                        }
                        Assert.True(await asyncEnumerator.MoveNextAsync());
                        Assert.AreEqual(3, asyncEnumerator.Current);
                        if (cancelSequence == 2)
                        {
                            cancelationSource.Cancel();
                            return;
                        }
                        Assert.True(await asyncEnumerator.MoveNextAsync());
                        Assert.AreEqual(4, asyncEnumerator.Current);
                        Assert.True(await asyncEnumerator.MoveNextAsync());
                        Assert.AreEqual(5, asyncEnumerator.Current);
                        if (cancelSequence == 3)
                        {
                            cancelationSource.Cancel();
                            return;
                        }
                        Assert.True(await asyncEnumerator.MoveNextAsync());
                        Assert.AreEqual(6, asyncEnumerator.Current);
                        Assert.True(await asyncEnumerator.MoveNextAsync());
                        Assert.AreEqual(7, asyncEnumerator.Current);
                        cancelationSource.Cancel();
                    }
                    finally
                    {
                        await TestHelper.AssertCanceledAsync(() => asyncEnumerator.MoveNextAsync());
                        await asyncEnumerator.DisposeAsync();
                    }
                }
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Concat_ConcatedBoth3_3_Cancel(
            [Values(0, 1, 2, 3, 4, 5)] int cancelSequence)
        {
            Promise.Run(async () =>
            {
                var xs1 = AsyncEnumerable.Create<int>(async (writer, cancelationToken) =>
                {
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(1);
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(2);
                });
                var xs2 = AsyncEnumerable.Create<int>(async (writer, cancelationToken) =>
                {
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(3);
                });
                var xs3 = AsyncEnumerable.Create<int>(async (writer, cancelationToken) =>
                {
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(4);
                });
                var xs4 = AsyncEnumerable.Create<int>(async (writer, cancelationToken) =>
                {
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(5);
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(6);
                });
                var xs5 = AsyncEnumerable.Create<int>(async (writer, cancelationToken) =>
                {
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(7);
                });
                var xs6 = AsyncEnumerable.Create<int>(async (writer, cancelationToken) =>
                {
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(8);
                });
                using (var cancelationSource = CancelationSource.New())
                {
                    var asyncEnumerator = xs1
                    .Concat(xs2)
                    .Concat(xs3)
                    .Concat(
                        xs4
                        .Concat(xs5)
                        .Concat(xs6)
                    )
                        .GetAsyncEnumerator(cancelationSource.Token);
                    try
                    {
                        Assert.True(await asyncEnumerator.MoveNextAsync());
                        Assert.AreEqual(1, asyncEnumerator.Current);
                        if (cancelSequence == 0)
                        {
                            cancelationSource.Cancel();
                            return;
                        }
                        Assert.True(await asyncEnumerator.MoveNextAsync());
                        Assert.AreEqual(2, asyncEnumerator.Current);
                        if (cancelSequence == 1)
                        {
                            cancelationSource.Cancel();
                            return;
                        }
                        Assert.True(await asyncEnumerator.MoveNextAsync());
                        Assert.AreEqual(3, asyncEnumerator.Current);
                        if (cancelSequence == 2)
                        {
                            cancelationSource.Cancel();
                            return;
                        }
                        Assert.True(await asyncEnumerator.MoveNextAsync());
                        Assert.AreEqual(4, asyncEnumerator.Current);
                        Assert.True(await asyncEnumerator.MoveNextAsync());
                        Assert.AreEqual(5, asyncEnumerator.Current);
                        if (cancelSequence == 3)
                        {
                            cancelationSource.Cancel();
                            return;
                        }
                        Assert.True(await asyncEnumerator.MoveNextAsync());
                        Assert.AreEqual(6, asyncEnumerator.Current);
                        if (cancelSequence == 4)
                        {
                            cancelationSource.Cancel();
                            return;
                        }
                        Assert.True(await asyncEnumerator.MoveNextAsync());
                        Assert.AreEqual(7, asyncEnumerator.Current);
                        cancelationSource.Cancel();
                    }
                    finally
                    {
                        await TestHelper.AssertCanceledAsync(() => asyncEnumerator.MoveNextAsync());
                        await asyncEnumerator.DisposeAsync();
                    }
                }
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Concat_ConcatedBoth2_3_1_Cancel(
            [Values(0, 1, 2, 3, 4, 5)] int cancelSequence)
        {
            Promise.Run(async () =>
            {
                var xs1 = AsyncEnumerable.Create<int>(async (writer, cancelationToken) =>
                {
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(1);
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(2);
                });
                var xs2 = AsyncEnumerable.Create<int>(async (writer, cancelationToken) =>
                {
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(3);
                });
                var xs3 = AsyncEnumerable.Create<int>(async (writer, cancelationToken) =>
                {
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(4);
                });
                var xs4 = AsyncEnumerable.Create<int>(async (writer, cancelationToken) =>
                {
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(5);
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(6);
                });
                var xs5 = AsyncEnumerable.Create<int>(async (writer, cancelationToken) =>
                {
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(7);
                });
                var xs6 = AsyncEnumerable.Create<int>(async (writer, cancelationToken) =>
                {
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(8);
                });
                using (var cancelationSource = CancelationSource.New())
                {
                    var asyncEnumerator = xs1
                        .Concat(xs2)
                        .Concat(
                            xs3
                            .Concat(xs4)
                            .Concat(xs5)
                        )
                        .Concat(xs6)
                        .GetAsyncEnumerator(cancelationSource.Token);
                    try
                    {
                        Assert.True(await asyncEnumerator.MoveNextAsync());
                        Assert.AreEqual(1, asyncEnumerator.Current);
                        if (cancelSequence == 0)
                        {
                            cancelationSource.Cancel();
                            return;
                        }
                        Assert.True(await asyncEnumerator.MoveNextAsync());
                        Assert.AreEqual(2, asyncEnumerator.Current);
                        if (cancelSequence == 1)
                        {
                            cancelationSource.Cancel();
                            return;
                        }
                        Assert.True(await asyncEnumerator.MoveNextAsync());
                        Assert.AreEqual(3, asyncEnumerator.Current);
                        if (cancelSequence == 2)
                        {
                            cancelationSource.Cancel();
                            return;
                        }
                        Assert.True(await asyncEnumerator.MoveNextAsync());
                        Assert.AreEqual(4, asyncEnumerator.Current);
                        Assert.True(await asyncEnumerator.MoveNextAsync());
                        Assert.AreEqual(5, asyncEnumerator.Current);
                        if (cancelSequence == 3)
                        {
                            cancelationSource.Cancel();
                            return;
                        }
                        Assert.True(await asyncEnumerator.MoveNextAsync());
                        Assert.AreEqual(6, asyncEnumerator.Current);
                        if (cancelSequence == 4)
                        {
                            cancelationSource.Cancel();
                            return;
                        }
                        Assert.True(await asyncEnumerator.MoveNextAsync());
                        Assert.AreEqual(7, asyncEnumerator.Current);
                        cancelationSource.Cancel();
                    }
                    finally
                    {
                        await TestHelper.AssertCanceledAsync(() => asyncEnumerator.MoveNextAsync());
                        await asyncEnumerator.DisposeAsync();
                    }
                }
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }
    }
}

#endif