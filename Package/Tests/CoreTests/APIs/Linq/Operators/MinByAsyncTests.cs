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
    public class MinByAsyncTests
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
        public void MinByAsync_NullArgument()
        {
            var enumerable = AsyncEnumerable.Range(0, 10);
            const string captureValue = "captureValue";

            Assert.Catch<System.ArgumentNullException>(() => enumerable.MinByAsync(default(Func<int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.MinByAsync(x => x, default(IComparer<int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.MinByAsync(default(Func<int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.MinByAsync(async x => x, default(IComparer<int>)));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.MinByAsync(captureValue, default(Func<string, int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.MinByAsync(captureValue, (cv, x) => x, default(IComparer<int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.MinByAsync(captureValue, default(Func<string, int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.MinByAsync(captureValue, async (cv, x) => x, default(IComparer<int>)));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).MinByAsync(default(Func<int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).MinByAsync(x => x, default(IComparer<int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).MinByAsync(default(Func<int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).MinByAsync(async x => x, default(IComparer<int>)));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).MinByAsync(captureValue, default(Func<string, int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).MinByAsync(captureValue, (cv, x) => x, default(IComparer<int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).MinByAsync(captureValue, default(Func<string, int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).MinByAsync(captureValue, async (cv, x) => x, default(IComparer<int>)));

            enumerable.GetAsyncEnumerator().DisposeAsync().Forget();
        }
#endif

        // We test all of the overloads.
        private static Promise<TSource> MinByAsync<TSource, TKey>(AsyncEnumerable<TSource> source,
            bool configured,
            bool async,
            bool captureValue,
            Func<TSource, TKey> keySelector,
            IComparer<TKey> comparer,
            CancelationToken cancelationToken = default)
        {
            if (configured)
            {
                return MinByAsync(source.ConfigureAwait(SynchronizationOption.Foreground).WithCancelation(cancelationToken), async, captureValue, keySelector, comparer);
            }

            const string capturedValue = "captureValue";

            return async
                ? captureValue
                    ? comparer != null
                        ? source.MinByAsync(capturedValue, async (cv, x) => keySelector(x), comparer, cancelationToken)
                        : source.MinByAsync(capturedValue, async (cv, x) => keySelector(x), cancelationToken)
                    : comparer != null
                        ? source.MinByAsync(async x => keySelector(x), comparer, cancelationToken)
                        : source.MinByAsync(async x => keySelector(x), cancelationToken)
                : captureValue
                    ? comparer != null
                        ? source.MinByAsync(capturedValue, (cv, x) => keySelector(x), comparer, cancelationToken)
                        : source.MinByAsync(capturedValue, (cv, x) => keySelector(x), cancelationToken)
                    : comparer != null
                        ? source.MinByAsync(x => keySelector(x), comparer, cancelationToken)
                        : source.MinByAsync(x => keySelector(x), cancelationToken);
        }

        private static Promise<TSource> MinByAsync<TSource, TKey>(ConfiguredAsyncEnumerable<TSource> source,
            bool async,
            bool captureValue,
            Func<TSource, TKey> keySelector,
            IComparer<TKey> comparer)
        {
            const string capturedValue = "captureValue";

            return async
                ? captureValue
                    ? comparer != null
                        ? source.MinByAsync(capturedValue, async (cv, x) => keySelector(x), comparer)
                        : source.MinByAsync(capturedValue, async (cv, x) => keySelector(x))
                    : comparer != null
                        ? source.MinByAsync(async x => keySelector(x), comparer)
                        : source.MinByAsync(async x => keySelector(x))
                : captureValue
                    ? comparer != null
                        ? source.MinByAsync(capturedValue, (cv, x) => keySelector(x), comparer)
                        : source.MinByAsync(capturedValue, (cv, x) => keySelector(x))
                    : comparer != null
                        ? source.MinByAsync(x => keySelector(x), comparer)
                        : source.MinByAsync(x => keySelector(x));
        }

        private static IComparer<T> GetDefaultOrNullComparer<T>(bool defaultComparer)
        {
            return defaultComparer ? Comparer<T>.Default : null;
        }

        [Test]
        public void MinByAsync_Empty_Int32_Throws(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureValue,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var res = MinByAsync(AsyncEnumerable<int>.Empty(), configured, async, captureValue, x => x, GetDefaultOrNullComparer<int>(withComparer));
                await TestHelper.AssertThrowsAsync<System.InvalidOperationException>(() => res);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void MinByAsync_Empty_Object_ReturnsNull(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureValue,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var res = MinByAsync(AsyncEnumerable<object>.Empty(), configured, async, captureValue, x => x, GetDefaultOrNullComparer<object>(withComparer));
                Assert.IsNull(await res);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void MinByAsync_KeySelectorThrows(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureValue,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var ex = new Exception("Bang!");
                var res = MinByAsync(new[] { 42, 44, 43 }.ToAsyncEnumerable(), configured, async, captureValue, x => { throw ex; }, GetDefaultOrNullComparer<int>(withComparer));
                await TestHelper.AssertThrowsAsync(() => res, ex);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void MinByAsync_Single(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureValue,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var res = MinByAsync(AsyncEnumerable.Return(42), configured, async, captureValue, x => x, GetDefaultOrNullComparer<int>(withComparer));
                Assert.AreEqual(42, await res);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void MinByAsync_Many(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureValue,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var res = MinByAsync(new[] { 42, 44, 43 }.ToAsyncEnumerable(), configured, async, captureValue, x => x, GetDefaultOrNullComparer<int>(withComparer));
                Assert.AreEqual(42, await res);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void MinByAsync_Many_KeySelectorAbs(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureValue,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var res = MinByAsync(new[] { 42, -44, 43 }.ToAsyncEnumerable(), configured, async, captureValue, x => Math.Abs(x), GetDefaultOrNullComparer<int>(withComparer));
                Assert.AreEqual(42, await res);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void MinByAsync_Comparer(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureValue)
        {
            Promise.Run(async () =>
            {
                var res = MinByAsync(new[] { 42, -44, 43 }.ToAsyncEnumerable(), configured, async, captureValue, x => x, new AbsComparer());
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
        public void MinByAsync_Cancel(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureValue,
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
                    var res = MinByAsync(xs, configured, async, captureValue, x => x, GetDefaultOrNullComparer<int>(withComparer), cancelationSource.Token);
                    cancelationSource.Cancel();
                    deferred.Resolve();
                    await TestHelper.AssertCanceledAsync(() => res);
                }
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }
    }
}