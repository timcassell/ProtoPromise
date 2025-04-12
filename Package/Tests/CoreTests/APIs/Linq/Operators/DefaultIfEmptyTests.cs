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

namespace ProtoPromiseTests.APIs.Linq
{
    public class DefaultIfEmptyTests
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

#if PROMISE_DEBUG
        [Test]
        public void DefaultIfEmpty_NullArguments()
        {
            var enumerable = AsyncEnumerable.Range(0, 10);

            Assert.Catch<System.ArgumentNullException>(() => enumerable.DefaultIfEmpty(default(Func<CancelationToken, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.DefaultIfEmpty("capture", default(Func<string, CancelationToken, Promise<int>>)));

            enumerable.GetAsyncEnumerator().DisposeAsync().Forget();
        }
#endif

        [Test]
        public void DefaultIfEmpty_Empty()
        {
            Promise.Run(async () =>
            {
                var asyncEnumerator = AsyncEnumerable.Empty<int>().DefaultIfEmpty().GetAsyncEnumerator();
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(0, asyncEnumerator.Current);
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void DefaultIfEmpty_Value_Empty()
        {
            Promise.Run(async () =>
            {
                var asyncEnumerator = AsyncEnumerable.Empty<int>().DefaultIfEmpty(42).GetAsyncEnumerator();
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(42, asyncEnumerator.Current);
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void DefaultIfEmpty_Single()
        {
            Promise.Run(async () =>
            {
                var asyncEnumerator = AsyncEnumerable.Return(42).DefaultIfEmpty().GetAsyncEnumerator();
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(42, asyncEnumerator.Current);
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void DefaultIfEmpty_Value_Single()
        {
            Promise.Run(async () =>
            {
                var asyncEnumerator = AsyncEnumerable.Return(42).DefaultIfEmpty(21).GetAsyncEnumerator();
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(42, asyncEnumerator.Current);
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void DefaultIfEmpty_Many()
        {
            Promise.Run(async () =>
            {
                var asyncEnumerator = new[] { 1, 2, 3, 4 }.ToAsyncEnumerable().DefaultIfEmpty().GetAsyncEnumerator();
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(1, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(2, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(3, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(4, asyncEnumerator.Current);
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void DefaultIfEmpty_Value_Many()
        {
            Promise.Run(async () =>
            {
                var asyncEnumerator = new[] { 1, 2, 3, 4 }.ToAsyncEnumerable().DefaultIfEmpty(21).GetAsyncEnumerator();
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(1, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(2, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(3, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(4, asyncEnumerator.Current);
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void DefaultIfEmpty_Throws_Source()
        {
            Promise.Run(async () =>
            {
                var ex = new Exception("Bang!");
                var asyncEnumerator = AsyncEnumerable<int>.Rejected(ex).DefaultIfEmpty().GetAsyncEnumerator();
                await TestHelper.AssertThrowsAsync(() => asyncEnumerator.MoveNextAsync(), ex);
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void DefaultIfEmpty_Value_Throws_Source()
        {
            Promise.Run(async () =>
            {
                var ex = new Exception("Bang!");
                var asyncEnumerator = AsyncEnumerable<int>.Rejected(ex).DefaultIfEmpty(42).GetAsyncEnumerator();
                await TestHelper.AssertThrowsAsync(() => asyncEnumerator.MoveNextAsync(), ex);
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        private static AsyncEnumerable<T> DefaultIfEmpty<T>(AsyncEnumerable<T> source, bool captureValue, Func<T> retriever)
        {
            const string capturedValue = "capturedValue";

            return captureValue
                ? source.DefaultIfEmpty(capturedValue, (cv, _) =>
                {
                    Assert.AreEqual(capturedValue, cv);
                    return Promise.Resolved(retriever.Invoke());
                })
                : source.DefaultIfEmpty(_ => Promise.Resolved(retriever.Invoke()));
        }

        [Test]
        public void DefaultIfEmpty_Retriever_Empty(
            [Values] bool captureValue)
        {
            Promise.Run(async () =>
            {
                var asyncEnumerator = DefaultIfEmpty(AsyncEnumerable.Empty<int>(), captureValue, () => 42).GetAsyncEnumerator();
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(42, asyncEnumerator.Current);
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void DefaultIfEmpty_Retriever_Single(
            [Values] bool captureValue)
        {
            Promise.Run(async () =>
            {
                var asyncEnumerator = DefaultIfEmpty(AsyncEnumerable.Return(42), captureValue, () => 21).GetAsyncEnumerator();
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(42, asyncEnumerator.Current);
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void DefaultIfEmpty_Retriever_Many(
            [Values] bool captureValue)
        {
            Promise.Run(async () =>
            {
                var asyncEnumerator = DefaultIfEmpty(new[] { 1, 2, 3, 4 }.ToAsyncEnumerable(), captureValue, () => 21).GetAsyncEnumerator();
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(1, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(2, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(3, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(4, asyncEnumerator.Current);
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void DefaultIfEmpty_Retriever_Throws_Source(
            [Values] bool captureValue)
        {
            Promise.Run(async () =>
            {
                var ex = new Exception("Bang!");
                var asyncEnumerator = DefaultIfEmpty(AsyncEnumerable<int>.Rejected(ex), captureValue, () => 21).GetAsyncEnumerator();
                await TestHelper.AssertThrowsAsync(() => asyncEnumerator.MoveNextAsync(), ex);
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void DefaultIfEmpty_Retriever_Throws(
            [Values] bool captureValue)
        {
            Promise.Run(async () =>
            {
                var ex = new Exception("Bang!");
                var asyncEnumerator = DefaultIfEmpty(AsyncEnumerable<int>.Empty(), captureValue, () => { throw ex; }).GetAsyncEnumerator();
                await TestHelper.AssertThrowsAsync(() => asyncEnumerator.MoveNextAsync(), ex);
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void DefaultIfEmpty_Many_Cancel()
        {
            Promise.Run(async () =>
            {
                var xs = AsyncEnumerable.Create<int>(async (writer, cancelationToken) =>
                {
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(1);
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(2);
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(3);
                });
                using (var cancelationSource = CancelationSource.New())
                {
                    var asyncEnumerator = xs.DefaultIfEmpty().GetAsyncEnumerator(cancelationSource.Token);
                    Assert.True(await asyncEnumerator.MoveNextAsync());
                    Assert.AreEqual(1, asyncEnumerator.Current);
                    Assert.True(await asyncEnumerator.MoveNextAsync());
                    Assert.AreEqual(2, asyncEnumerator.Current);
                    cancelationSource.Cancel();
                    await TestHelper.AssertCanceledAsync(() => asyncEnumerator.MoveNextAsync());
                    await asyncEnumerator.DisposeAsync();
                }
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void DefaultIfEmpty_Value_Many_Cancel()
        {
            Promise.Run(async () =>
            {
                var xs = AsyncEnumerable.Create<int>(async (writer, cancelationToken) =>
                {
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(1);
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(2);
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(3);
                });
                using (var cancelationSource = CancelationSource.New())
                {
                    var asyncEnumerator = xs.DefaultIfEmpty(42).GetAsyncEnumerator(cancelationSource.Token);
                    Assert.True(await asyncEnumerator.MoveNextAsync());
                    Assert.AreEqual(1, asyncEnumerator.Current);
                    Assert.True(await asyncEnumerator.MoveNextAsync());
                    Assert.AreEqual(2, asyncEnumerator.Current);
                    cancelationSource.Cancel();
                    await TestHelper.AssertCanceledAsync(() => asyncEnumerator.MoveNextAsync());
                    await asyncEnumerator.DisposeAsync();
                }
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void DefaultIfEmpty_Retriever_Many_Cancel(
            [Values] bool captureValue)
        {
            Promise.Run(async () =>
            {
                var xs = AsyncEnumerable.Create<int>(async (writer, cancelationToken) =>
                {
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(1);
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(2);
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(3);
                });
                using (var cancelationSource = CancelationSource.New())
                {
                    var asyncEnumerator = DefaultIfEmpty(xs, captureValue, () => 42).GetAsyncEnumerator(cancelationSource.Token);
                    Assert.True(await asyncEnumerator.MoveNextAsync());
                    Assert.AreEqual(1, asyncEnumerator.Current);
                    Assert.True(await asyncEnumerator.MoveNextAsync());
                    Assert.AreEqual(2, asyncEnumerator.Current);
                    cancelationSource.Cancel();
                    await TestHelper.AssertCanceledAsync(() => asyncEnumerator.MoveNextAsync());
                    await asyncEnumerator.DisposeAsync();
                }
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }
    }
}