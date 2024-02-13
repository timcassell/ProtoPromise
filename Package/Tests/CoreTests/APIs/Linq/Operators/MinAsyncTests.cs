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

namespace ProtoPromiseTests.APIs.Linq
{
    public class MinAsyncTests
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
        public void MinAsync_NullArgument()
        {
            var enumerable = AsyncEnumerable.Range(0, 10);

            Assert.Catch<System.ArgumentNullException>(() => enumerable.MinAsync(default(IComparer<int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).MinAsync(default(IComparer<int>)));

            enumerable.GetAsyncEnumerator().DisposeAsync().Forget();
        }
#endif

        // We test all of the overloads.
        private static Promise<TSource> MinAsync<TSource>(AsyncEnumerable<TSource> source,
            bool configured,
            IComparer<TSource> comparer,
            CancelationToken cancelationToken = default)
        {
            return configured
                ? comparer != null
                    ? source.ConfigureAwait(SynchronizationOption.Foreground).WithCancelation(cancelationToken).MinAsync(comparer)
                    : source.ConfigureAwait(SynchronizationOption.Foreground).WithCancelation(cancelationToken).MinAsync()
                : comparer != null
                    ? source.MinAsync(comparer, cancelationToken)
                    : source.MinAsync(cancelationToken);
        }

        private static IComparer<T> GetDefaultOrNullComparer<T>(bool defaultComparer)
        {
            return defaultComparer ? Comparer<T>.Default : null;
        }

        [Test]
        public void MinAsync_Empty_Int32_Throws(
            [Values] bool configured,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var res = MinAsync(AsyncEnumerable<int>.Empty(), configured, GetDefaultOrNullComparer<int>(withComparer));
                await TestHelper.AssertThrowsAsync<System.InvalidOperationException>(() => res);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void MinAsync_Empty_Object_ReturnsNull(
            [Values] bool configured,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var res = MinAsync(AsyncEnumerable<object>.Empty(), configured, GetDefaultOrNullComparer<object>(withComparer));
                Assert.IsNull(await res);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void MinAsync_Single(
            [Values] bool configured,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var res = MinAsync(AsyncEnumerable.Return(42), configured, GetDefaultOrNullComparer<int>(withComparer));
                Assert.AreEqual(42, await res);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void MinAsync_Many(
            [Values] bool configured,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var res = MinAsync(new[] { 42, 44, 43 }.ToAsyncEnumerable(), configured, GetDefaultOrNullComparer<int>(withComparer));
                Assert.AreEqual(42, await res);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void MinAsync_Comparer(
            [Values] bool configured)
        {
            Promise.Run(async () =>
            {
                var res = MinAsync(new[] { 42, -44, 43 }.ToAsyncEnumerable(), configured, new AbsComparer());
                Assert.AreEqual(42, await res);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        private class AbsComparer : IComparer<int>
        {
            public int Compare(int x, int y)
                => Math.Abs(x).CompareTo(Math.Abs(y));
        }

        [Test]
        public void MinAsync_Cancel(
            [Values] bool configured,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var deferred = Promise.NewDeferred();
                var xs = AsyncEnumerable.Create<int>(async (writer, cancelationToken) =>
                {
                    await deferred.Promise;
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(1);
                    await deferred.Promise;
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(2);
                    await deferred.Promise;
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(3);
                });
                using (var cancelationSource = CancelationSource.New())
                {
                    var res = MinAsync(xs, configured, GetDefaultOrNullComparer<int>(withComparer), cancelationSource.Token);
                    cancelationSource.Cancel();
                    deferred.Resolve();
                    await TestHelper.AssertCanceledAsync(() => res);
                }
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }
    }
}