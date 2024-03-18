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
    public static class UnionHelper
    {
        // We test all the different overloads.
        public static AsyncEnumerable<TSource> Union<TSource>(this AsyncEnumerable<TSource> firstAsyncEnumerable,
            AsyncEnumerable<TSource> secondAsyncEnumerable,
            bool configured,
            IEqualityComparer<TSource> equalityComparer = null,
            CancelationToken configuredCancelationToken = default)
        {
            return configured
                ? equalityComparer != null
                    ? firstAsyncEnumerable.ConfigureAwait(SynchronizationOption.Foreground).WithCancelation(configuredCancelationToken).Union(secondAsyncEnumerable, equalityComparer)
                    : firstAsyncEnumerable.ConfigureAwait(SynchronizationOption.Foreground).WithCancelation(configuredCancelationToken).Union(secondAsyncEnumerable)
                : equalityComparer != null
                    ? firstAsyncEnumerable.Union(secondAsyncEnumerable, equalityComparer)
                    : firstAsyncEnumerable.Union(secondAsyncEnumerable);
        }

        public static AsyncEnumerable<TSource> UnionBy<TSource, TKey>(this AsyncEnumerable<TSource> firstAsyncEnumerable,
            AsyncEnumerable<TSource> secondAsyncEnumerable,
            bool configured,
            bool async,
            bool captureValue,
            Func<TSource, TKey> keySelector,
            IEqualityComparer<TKey> equalityComparer = null,
            CancelationToken configuredCancelationToken = default)
        {
            if (configured)
            {
                return UnionBy(firstAsyncEnumerable.ConfigureAwait(SynchronizationOption.Foreground).WithCancelation(configuredCancelationToken), secondAsyncEnumerable, async, captureValue, keySelector, equalityComparer);
            }

            const string valueCapture = "valueCapture";

            if (!captureValue)
            {
                return async
                    ? equalityComparer != null
                        ? firstAsyncEnumerable.UnionBy(secondAsyncEnumerable, async x => keySelector(x), equalityComparer)
                        : firstAsyncEnumerable.UnionBy(secondAsyncEnumerable, async x => keySelector(x))
                    : equalityComparer != null
                        ? firstAsyncEnumerable.UnionBy(secondAsyncEnumerable, keySelector, equalityComparer)
                        : firstAsyncEnumerable.UnionBy(secondAsyncEnumerable, keySelector);
            }
            else
            {
                return async
                    ? equalityComparer != null
                        ? firstAsyncEnumerable.UnionBy(secondAsyncEnumerable, valueCapture, async (cv, x) =>
                        {
                            Assert.AreEqual(valueCapture, cv);
                            return keySelector(x);
                        }, equalityComparer)
                        : firstAsyncEnumerable.UnionBy(secondAsyncEnumerable, valueCapture, async (cv, x) =>
                        {
                            Assert.AreEqual(valueCapture, cv);
                            return keySelector(x);
                        })
                    : equalityComparer != null
                        ? firstAsyncEnumerable.UnionBy(secondAsyncEnumerable, valueCapture, (cv, x) =>
                        {
                            Assert.AreEqual(valueCapture, cv);
                            return keySelector(x);
                        }, equalityComparer)
                        : firstAsyncEnumerable.UnionBy(secondAsyncEnumerable, valueCapture, (cv, x) =>
                        {
                            Assert.AreEqual(valueCapture, cv);
                            return keySelector(x);
                        });
            }
        }

        public static AsyncEnumerable<TSource> UnionBy<TSource, TKey>(this ConfiguredAsyncEnumerable<TSource> firstAsyncEnumerable,
            AsyncEnumerable<TSource> secondAsyncEnumerable,
            bool async,
            bool captureValue,
            Func<TSource, TKey> keySelector,
            IEqualityComparer<TKey> equalityComparer = null)
        {
            const string valueCapture = "valueCapture";

            if (!captureValue)
            {
                return async
                    ? equalityComparer != null
                        ? firstAsyncEnumerable.UnionBy(secondAsyncEnumerable, async x => keySelector(x), equalityComparer)
                        : firstAsyncEnumerable.UnionBy(secondAsyncEnumerable, async x => keySelector(x))
                    : equalityComparer != null
                        ? firstAsyncEnumerable.UnionBy(secondAsyncEnumerable, keySelector, equalityComparer)
                        : firstAsyncEnumerable.UnionBy(secondAsyncEnumerable, keySelector);
            }
            else
            {
                return async
                    ? equalityComparer != null
                        ? firstAsyncEnumerable.UnionBy(secondAsyncEnumerable, valueCapture, async (cv, x) =>
                        {
                            Assert.AreEqual(valueCapture, cv);
                            return keySelector(x);
                        }, equalityComparer)
                        : firstAsyncEnumerable.UnionBy(secondAsyncEnumerable, valueCapture, async (cv, x) =>
                        {
                            Assert.AreEqual(valueCapture, cv);
                            return keySelector(x);
                        })
                    : equalityComparer != null
                        ? firstAsyncEnumerable.UnionBy(secondAsyncEnumerable, valueCapture, (cv, x) =>
                        {
                            Assert.AreEqual(valueCapture, cv);
                            return keySelector(x);
                        }, equalityComparer)
                        : firstAsyncEnumerable.UnionBy(secondAsyncEnumerable, valueCapture, (cv, x) =>
                        {
                            Assert.AreEqual(valueCapture, cv);
                            return keySelector(x);
                        });
            }
        }
    }

    public class UnionTests
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
        public void Union_NullArgumentThrows()
        {
            var first = AsyncEnumerable.Return(42);
            var second = AsyncEnumerable.Return(21);
            var nullComparer = default(IEqualityComparer<int>);

            Assert.Catch<System.ArgumentNullException>(() => first.Union(second, nullComparer));
            Assert.Catch<System.ArgumentNullException>(() => first.ConfigureAwait(SynchronizationOption.Synchronous).Union(second, nullComparer));

            first.GetAsyncEnumerator().DisposeAsync().Forget();
            second.GetAsyncEnumerator().DisposeAsync().Forget();
        }

        [Test]
        public void UnionBy_NullArgumentThrows()
        {
            var first = AsyncEnumerable.Return(42);
            var second = AsyncEnumerable.Return(21);
            var captureValue = "captureValue";
            var nullComparer = default(IEqualityComparer<int>);

            Assert.Catch<System.ArgumentNullException>(() => first.UnionBy(second, default(Func<int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => first.UnionBy(second, x => x, nullComparer));
            Assert.Catch<System.ArgumentNullException>(() => first.UnionBy(second, default(Func<int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => first.UnionBy(second, async x => x, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => first.UnionBy(second, captureValue, default(Func<string, int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => first.UnionBy(second, captureValue, (cv, x) => x, nullComparer));
            Assert.Catch<System.ArgumentNullException>(() => first.UnionBy(second, captureValue, default(Func<string, int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => first.UnionBy(second, captureValue, async (cv, x) => x, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => first.ConfigureAwait(SynchronizationOption.Synchronous).UnionBy(second, default(Func<int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => first.ConfigureAwait(SynchronizationOption.Synchronous).UnionBy(second, x => x, nullComparer));
            Assert.Catch<System.ArgumentNullException>(() => first.ConfigureAwait(SynchronizationOption.Synchronous).UnionBy(second, default(Func<int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => first.ConfigureAwait(SynchronizationOption.Synchronous).UnionBy(second, async x => x, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => first.ConfigureAwait(SynchronizationOption.Synchronous).UnionBy(second, captureValue, default(Func<string, int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => first.ConfigureAwait(SynchronizationOption.Synchronous).UnionBy(second, captureValue, (cv, x) => x, nullComparer));
            Assert.Catch<System.ArgumentNullException>(() => first.ConfigureAwait(SynchronizationOption.Synchronous).UnionBy(second, captureValue, default(Func<string, int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => first.ConfigureAwait(SynchronizationOption.Synchronous).UnionBy(second, captureValue, async (cv, x) => x, nullComparer));

            first.GetAsyncEnumerator().DisposeAsync().Forget();
            second.GetAsyncEnumerator().DisposeAsync().Forget();
        }
#endif //PROMISE_DEBUG

        private static IEqualityComparer<T> GetDefaultOrNullComparer<T>(bool defaultComparer)
        {
            return defaultComparer ? EqualityComparer<T>.Default : null;
        }

        [Test]
        public void Union_EmptyFirst(
            [Values] bool configured,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var xs = AsyncEnumerable.Empty<int>();
                var ys = new[] { 3, 5, 1, 4 }.ToAsyncEnumerable();
                var asyncEnumerator = xs.Union(ys, configured, GetDefaultOrNullComparer<int>(withComparer)).GetAsyncEnumerator();
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(3, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(5, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(1, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(4, asyncEnumerator.Current);
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Union_EmptySecond(
            [Values] bool configured,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var xs = new[] { 3, 5, 1, 4 }.ToAsyncEnumerable();
                var ys = AsyncEnumerable.Empty<int>();
                var asyncEnumerator = xs.Union(ys, configured, GetDefaultOrNullComparer<int>(withComparer)).GetAsyncEnumerator();
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(3, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(5, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(1, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(4, asyncEnumerator.Current);
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Union_Simple(
            [Values] bool configured,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var xs = new[] { 1, 2, 3 }.ToAsyncEnumerable();
                var ys = new[] { 3, 5, 1, 4 }.ToAsyncEnumerable();
                var asyncEnumerator = xs.Union(ys, configured, GetDefaultOrNullComparer<int>(withComparer)).GetAsyncEnumerator();
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(1, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(2, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(3, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(5, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(4, asyncEnumerator.Current);
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Union_EqualityComparer(
            [Values] bool configured)
        {
            Promise.Run(async () =>
            {
                var xs = new[] { 1, 2, -3 }.ToAsyncEnumerable();
                var ys = new[] { 3, 5, -1, 4 }.ToAsyncEnumerable();
                var asyncEnumerator = xs.Union(ys, configured, new Eq()).GetAsyncEnumerator();
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(1, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(2, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(-3, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(5, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(4, asyncEnumerator.Current);
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Union_FirstThrows(
            [Values] bool configured)
        {
            Promise.Run(async () =>
            {
                var ex = new Exception("Bang!");
                var xs = AsyncEnumerable<int>.Rejected(ex);
                var ys = new[] { 3, 5, -1, 4 }.ToAsyncEnumerable();
                var asyncEnumerator = xs.Union(ys, configured, new Eq()).GetAsyncEnumerator();
                await TestHelper.AssertThrowsAsync(() => asyncEnumerator.MoveNextAsync(), ex);
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Union_SecondThrows(
            [Values] bool configured)
        {
            Promise.Run(async () =>
            {
                var ex = new Exception("Bang!");
                var xs = new[] { 3, 5, -1, 4 }.ToAsyncEnumerable();
                var ys = AsyncEnumerable<int>.Rejected(ex);
                var asyncEnumerator = xs.Union(ys, configured, new Eq()).GetAsyncEnumerator();
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(3, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(5, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(-1, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(4, asyncEnumerator.Current);
                await TestHelper.AssertThrowsAsync(() => asyncEnumerator.MoveNextAsync(), ex);
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void UnionBy_EmptyFirst(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureValue,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var xs = AsyncEnumerable.Empty<int>();
                var ys = new[] { 3, 5, 1, 4 }.ToAsyncEnumerable();
                var asyncEnumerator = xs.UnionBy(ys, configured, async, captureValue, x => x, GetDefaultOrNullComparer<int>(withComparer)).GetAsyncEnumerator();
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(3, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(5, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(1, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(4, asyncEnumerator.Current);
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void UnionBy_EmptySecond(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureValue,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var xs = new[] { 3, 5, 1, 4 }.ToAsyncEnumerable();
                var ys = AsyncEnumerable.Empty<int>();
                var asyncEnumerator = xs.UnionBy(ys, configured, async, captureValue, x => x, GetDefaultOrNullComparer<int>(withComparer)).GetAsyncEnumerator();
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(3, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(5, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(1, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(4, asyncEnumerator.Current);
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void UnionBy_Simple(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureValue,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var xs = new[] { 1, 2, 3 }.ToAsyncEnumerable();
                var ys = new[] { 3, 5, 1, 4 }.ToAsyncEnumerable();
                var asyncEnumerator = xs.UnionBy(ys, configured, async, captureValue, x => x, GetDefaultOrNullComparer<int>(withComparer)).GetAsyncEnumerator();
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(1, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(2, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(3, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(5, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(4, asyncEnumerator.Current);
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void UnionBy_EqualityComparer(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureValue)
        {
            Promise.Run(async () =>
            {
                var xs = new[] { 1, 2, -3 }.ToAsyncEnumerable();
                var ys = new[] { 3, 5, -1, 4 }.ToAsyncEnumerable();
                var asyncEnumerator = xs.UnionBy(ys, configured, async, captureValue, x => x, new Eq()).GetAsyncEnumerator();
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(1, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(2, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(-3, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(5, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(4, asyncEnumerator.Current);
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void UnionBy_FirstThrows(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureValue,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var ex = new Exception("Bang!");
                var xs = AsyncEnumerable<int>.Rejected(ex);
                var ys = new[] { 3, 5, -1, 4 }.ToAsyncEnumerable();
                var asyncEnumerator = xs.UnionBy(ys, configured, async, captureValue, x => x, GetDefaultOrNullComparer<int>(withComparer)).GetAsyncEnumerator();
                await TestHelper.AssertThrowsAsync(() => asyncEnumerator.MoveNextAsync(), ex);
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void UnionBy_SecondThrows(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureValue,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var ex = new Exception("Bang!");
                var xs = new[] { 3, 5, -1, 4 }.ToAsyncEnumerable();
                var ys = AsyncEnumerable<int>.Rejected(ex);
                var asyncEnumerator = xs.UnionBy(ys, configured, async, captureValue, x => x, GetDefaultOrNullComparer<int>(withComparer)).GetAsyncEnumerator();
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(3, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(5, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(-1, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(4, asyncEnumerator.Current);
                await TestHelper.AssertThrowsAsync(() => asyncEnumerator.MoveNextAsync(), ex);
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Union_3(
            [Values] bool configured1,
            [Values] bool configured2,
            [Values] bool withComparer1,
            [Values] bool withComparer2)
        {
            Promise.Run(async () =>
            {
                var asyncEnumerator = new[] { 1, 2, 3 }.ToAsyncEnumerable()
                    .Union(new[] { 3, 4, 5, 1 }.ToAsyncEnumerable(), configured1, GetDefaultOrNullComparer<int>(withComparer1))
                    .Union(new[] { 1, 5, 2, 6, 7 }.ToAsyncEnumerable(), configured2, GetDefaultOrNullComparer<int>(withComparer2))
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
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Union_UnionedSecond(
            [Values] bool configured1,
            [Values] bool configured2,
            [Values] bool withComparer1,
            [Values] bool withComparer2)
        {
            Promise.Run(async () =>
            {
                var asyncEnumerator = new[] { 1, 2, 3 }.ToAsyncEnumerable()
                    .Union(
                        new[] { 3, 4, 5, 1 }.ToAsyncEnumerable()
                        .Union(new[] { 1, 5, 2, 6, 7 }.ToAsyncEnumerable(), configured1, GetDefaultOrNullComparer<int>(withComparer1)),
                        configured2, GetDefaultOrNullComparer<int>(withComparer2)
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
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Union_UnionedBoth(
            [Values] bool configured1,
            [Values] bool configured2,
            [Values] bool configured3,
            [Values] bool withComparer1,
            [Values] bool withComparer2,
            [Values] bool withComparer3)
        {
            Promise.Run(async () =>
            {
                var asyncEnumerator = new[] { 1, 2 }.ToAsyncEnumerable()
                    .Union(new[] { 3, 4, 1 }.ToAsyncEnumerable(), configured1, GetDefaultOrNullComparer<int>(withComparer1))
                    .Union(
                        new[] { 1, 3, 5, 6 }.ToAsyncEnumerable()
                        .Union(new[] { 7, 4, 6, 2 }.ToAsyncEnumerable(), configured2, GetDefaultOrNullComparer<int>(withComparer2)),
                        configured3, GetDefaultOrNullComparer<int>(withComparer3)
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
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Union_4(
            [Values] bool configured1,
            [Values] bool configured2,
            [Values] bool configured3,
            [Values] bool withComparer1,
            [Values] bool withComparer2,
            [Values] bool withComparer3)
        {
            Promise.Run(async () =>
            {
                var asyncEnumerator = new[] { 1, 2 }.ToAsyncEnumerable()
                    .Union(new[] { 3, 4, 1 }.ToAsyncEnumerable(), configured1, GetDefaultOrNullComparer<int>(withComparer1))
                    .Union(new[] { 1, 3, 5, 6 }.ToAsyncEnumerable(), configured2, GetDefaultOrNullComparer<int>(withComparer2))
                    .Union(new[] { 7, 4, 6, 2 }.ToAsyncEnumerable(), configured3, GetDefaultOrNullComparer<int>(withComparer3))
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
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Union_UnionedBoth3_2(
            [Values] bool configured1,
            [Values] bool configured2,
            [Values] bool configured3,
            [Values] bool configured4,
            [Values] bool withComparer1,
            [Values] bool withComparer2,
            [Values] bool withComparer3,
            [Values] bool withComparer4)
        {
            Promise.Run(async () =>
            {
                var asyncEnumerator = new[] { 1, 2 }.ToAsyncEnumerable()
                    .Union(new[] { 3, 1 }.ToAsyncEnumerable(), configured1, GetDefaultOrNullComparer<int>(withComparer1))
                    .Union(new[] { 2, 4 }.ToAsyncEnumerable(), configured2, GetDefaultOrNullComparer<int>(withComparer2))
                    .Union(
                        new[] { 1, 3, 5, 6 }.ToAsyncEnumerable()
                        .Union(new[] { 7, 4, 6, 2 }.ToAsyncEnumerable(), configured3, GetDefaultOrNullComparer<int>(withComparer3)),
                        configured4, GetDefaultOrNullComparer<int>(withComparer4)
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
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Union_UnionedBoth2_3(
            [Values] bool configured1,
            [Values] bool configured2,
            [Values] bool configured3,
            [Values] bool configured4,
            [Values] bool withComparer1,
            [Values] bool withComparer2,
            [Values] bool withComparer3,
            [Values] bool withComparer4)
        {
            Promise.Run(async () =>
            {
                var asyncEnumerator = new[] { 1, 2 }.ToAsyncEnumerable()
                    .Union(new[] { 3, 4, 1 }.ToAsyncEnumerable(), configured1, GetDefaultOrNullComparer<int>(withComparer1))
                    .Union(
                        new[] { 1, 3, 5, 6 }.ToAsyncEnumerable()
                        .Union(new[] { 7, 4, 6, 2 }.ToAsyncEnumerable(), configured2, GetDefaultOrNullComparer<int>(withComparer2))
                        .Union(new[] { 7, 4, 6, 8 }.ToAsyncEnumerable(), configured3, GetDefaultOrNullComparer<int>(withComparer3)),
                        configured4, GetDefaultOrNullComparer<int>(withComparer4)
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
        public void Union_UnionedBoth3_3(
            [Values] bool configured1,
            [Values] bool configured2,
            [Values] bool configured3,
            [Values] bool configured4,
            [Values] bool configured5,
            [Values] bool withComparer1,
            [Values] bool withComparer2,
            [Values] bool withComparer3,
            [Values] bool withComparer4,
            [Values] bool withComparer5)
        {
            Promise.Run(async () =>
            {
                var asyncEnumerator = new[] { 1, 2 }.ToAsyncEnumerable()
                    .Union(new[] { 3, 1 }.ToAsyncEnumerable(), configured1, GetDefaultOrNullComparer<int>(withComparer1))
                    .Union(new[] { 2, 4 }.ToAsyncEnumerable(), configured2, GetDefaultOrNullComparer<int>(withComparer2))
                    .Union(
                        new[] { 1, 3, 5, 6 }.ToAsyncEnumerable()
                        .Union(new[] { 7, 4, 6, 2 }.ToAsyncEnumerable(), configured3, GetDefaultOrNullComparer<int>(withComparer3))
                        .Union(new[] { 7, 4, 6, 8 }.ToAsyncEnumerable(), configured4, GetDefaultOrNullComparer<int>(withComparer4)),
                        configured5, GetDefaultOrNullComparer<int>(withComparer5)
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
        public void Union_UnionedBoth2_3_1(
            [Values] bool configured1,
            [Values] bool configured2,
            [Values] bool configured3,
            [Values] bool configured4,
            [Values] bool configured5,
            [Values] bool withComparer1,
            [Values] bool withComparer2,
            [Values] bool withComparer3,
            [Values] bool withComparer4,
            [Values] bool withComparer5)
        {
            Promise.Run(async () =>
            {
                var asyncEnumerator = new[] { 1, 2 }.ToAsyncEnumerable()
                    .Union(new[] { 3, 1 }.ToAsyncEnumerable(), configured1, GetDefaultOrNullComparer<int>(withComparer1))
                    .Union(
                        new[] { 2, 4 }.ToAsyncEnumerable()
                        .Union(new[] { 1, 3, 5, 6 }.ToAsyncEnumerable(), configured2, GetDefaultOrNullComparer<int>(withComparer2))
                        .Union(new[] { 7, 4, 6, 2 }.ToAsyncEnumerable(), configured3, GetDefaultOrNullComparer<int>(withComparer3)),
                        configured4, GetDefaultOrNullComparer<int>(withComparer4)
                    )
                    .Union(new[] { 7, 4, 6, 8 }.ToAsyncEnumerable(), configured5, GetDefaultOrNullComparer<int>(withComparer5))
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

        private sealed class Eq : IEqualityComparer<int>
        {
            public bool Equals(int x, int y)
            {
                return EqualityComparer<int>.Default.Equals(Math.Abs(x), Math.Abs(y));
            }

            public int GetHashCode(int obj)
            {
                return EqualityComparer<int>.Default.GetHashCode(Math.Abs(obj));
            }
        }

        public enum ConfiguredType
        {
            NotConfigured,
            Configured,
            ConfiguredWithCancelation
        }

        [Test]
        public void Union_2_Cancel(
            [Values] ConfiguredType configuredType,
            [Values] bool withComparer,
            [Values] bool enumeratorToken,
            [Values(0, 1)] int cancelSequence)
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
                var ys = AsyncEnumerable.Create<int>(async (writer, cancelationToken) =>
                {
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(3);
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(4);
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(5);
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(1);
                });
                using (var configuredCancelationSource = CancelationSource.New())
                {
                    using (var enumeratorCancelationSource = CancelationSource.New())
                    {
                        var asyncEnumerator = xs.Union(ys, configuredType != ConfiguredType.NotConfigured, GetDefaultOrNullComparer<int>(withComparer),
                            configuredType == ConfiguredType.ConfiguredWithCancelation ? configuredCancelationSource.Token : CancelationToken.None)
                            .GetAsyncEnumerator(enumeratorToken ? enumeratorCancelationSource.Token : CancelationToken.None);
                        try
                        {
                            Assert.True(await asyncEnumerator.MoveNextAsync());
                            Assert.AreEqual(1, asyncEnumerator.Current);
                            if (cancelSequence == 0)
                            {
                                configuredCancelationSource.Cancel();
                                enumeratorCancelationSource.Cancel();
                                if (configuredType == ConfiguredType.ConfiguredWithCancelation || enumeratorToken)
                                {
                                    await TestHelper.AssertCanceledAsync(() => asyncEnumerator.MoveNextAsync());
                                }
                                return;
                            }
                            Assert.True(await asyncEnumerator.MoveNextAsync());
                            Assert.AreEqual(2, asyncEnumerator.Current);
                            Assert.True(await asyncEnumerator.MoveNextAsync());
                            Assert.AreEqual(3, asyncEnumerator.Current);
                            Assert.True(await asyncEnumerator.MoveNextAsync());
                            Assert.AreEqual(4, asyncEnumerator.Current);
                            configuredCancelationSource.Cancel();
                            enumeratorCancelationSource.Cancel();
                            if (configuredType == ConfiguredType.ConfiguredWithCancelation || enumeratorToken)
                            {
                                await TestHelper.AssertCanceledAsync(() => asyncEnumerator.MoveNextAsync());
                            }
                        }
                        finally
                        {
                            await asyncEnumerator.DisposeAsync();
                        }
                    }
                }
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Union_3_Cancel(
            [Values] ConfiguredType configured1Type,
            [Values] ConfiguredType configured2Type,
            [Values] bool withComparer1,
            [Values] bool withComparer2,
            [Values] bool enumeratorToken,
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
                    await writer.YieldAsync(3);
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(4);
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(5);
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(1);
                });
                var xs3 = AsyncEnumerable.Create<int>(async (writer, cancelationToken) =>
                {
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(1);
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(5);
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(2);
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(6);
                });
                using (var configuredCancelationSource = CancelationSource.New())
                {
                    using (var enumeratorCancelationSource = CancelationSource.New())
                    {
                        var asyncEnumerator = xs1
                            .Union(xs2, configured1Type != ConfiguredType.NotConfigured, GetDefaultOrNullComparer<int>(withComparer1),
                                configured1Type == ConfiguredType.ConfiguredWithCancelation ? configuredCancelationSource.Token : CancelationToken.None)
                            .Union(xs3, configured2Type != ConfiguredType.NotConfigured, GetDefaultOrNullComparer<int>(withComparer2),
                                configured2Type == ConfiguredType.ConfiguredWithCancelation ? configuredCancelationSource.Token : CancelationToken.None)
                            .GetAsyncEnumerator(enumeratorToken ? enumeratorCancelationSource.Token : CancelationToken.None);
                        try
                        {
                            Assert.True(await asyncEnumerator.MoveNextAsync());
                            Assert.AreEqual(1, asyncEnumerator.Current);
                            if (cancelSequence == 0)
                            {
                                configuredCancelationSource.Cancel();
                                enumeratorCancelationSource.Cancel();
                                if (configured1Type == ConfiguredType.ConfiguredWithCancelation || enumeratorToken)
                                {
                                    await TestHelper.AssertCanceledAsync(() => asyncEnumerator.MoveNextAsync());
                                }
                                return;
                            }
                            Assert.True(await asyncEnumerator.MoveNextAsync());
                            Assert.AreEqual(2, asyncEnumerator.Current);
                            Assert.True(await asyncEnumerator.MoveNextAsync());
                            Assert.AreEqual(3, asyncEnumerator.Current);
                            Assert.True(await asyncEnumerator.MoveNextAsync());
                            Assert.AreEqual(4, asyncEnumerator.Current);
                            if (cancelSequence == 1)
                            {
                                configuredCancelationSource.Cancel();
                                enumeratorCancelationSource.Cancel();
                                if (configured1Type == ConfiguredType.ConfiguredWithCancelation || configured2Type == ConfiguredType.ConfiguredWithCancelation || enumeratorToken)
                                {
                                    await TestHelper.AssertCanceledAsync(() => asyncEnumerator.MoveNextAsync());
                                }
                                return;
                            }
                            Assert.True(await asyncEnumerator.MoveNextAsync());
                            Assert.AreEqual(5, asyncEnumerator.Current);
                            configuredCancelationSource.Cancel();
                            enumeratorCancelationSource.Cancel();
                            if (configured1Type == ConfiguredType.ConfiguredWithCancelation || configured2Type == ConfiguredType.ConfiguredWithCancelation || enumeratorToken)
                            {
                                await TestHelper.AssertCanceledAsync(() => asyncEnumerator.MoveNextAsync());
                            }
                        }
                        finally
                        {
                            await asyncEnumerator.DisposeAsync();
                        }
                    }
                }
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Union_UnionedSecond_Cancel(
            [Values] ConfiguredType configured1Type,
            [Values] ConfiguredType configured2Type,
            [Values] bool withComparer1,
            [Values] bool withComparer2,
            [Values] bool enumeratorToken,
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
                    await writer.YieldAsync(3);
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(4);
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(5);
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(1);
                });
                var xs3 = AsyncEnumerable.Create<int>(async (writer, cancelationToken) =>
                {
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(1);
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(5);
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(2);
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(6);
                });
                using (var configuredCancelationSource = CancelationSource.New())
                {
                    using (var enumeratorCancelationSource = CancelationSource.New())
                    {
                        var asyncEnumerator = xs1
                            .Union(
                                xs2.Union(xs3, configured2Type != ConfiguredType.NotConfigured, GetDefaultOrNullComparer<int>(withComparer2),
                                    configured2Type == ConfiguredType.ConfiguredWithCancelation ? configuredCancelationSource.Token : CancelationToken.None), configured1Type != ConfiguredType.NotConfigured, GetDefaultOrNullComparer<int>(withComparer1),
                                configured1Type == ConfiguredType.ConfiguredWithCancelation ? configuredCancelationSource.Token : CancelationToken.None)
                            
                            .GetAsyncEnumerator(enumeratorToken ? enumeratorCancelationSource.Token : CancelationToken.None);
                        try
                        {
                            Assert.True(await asyncEnumerator.MoveNextAsync());
                            Assert.AreEqual(1, asyncEnumerator.Current);
                            if (cancelSequence == 0)
                            {
                                configuredCancelationSource.Cancel();
                                enumeratorCancelationSource.Cancel();
                                if (configured1Type == ConfiguredType.ConfiguredWithCancelation || enumeratorToken)
                                {
                                    await TestHelper.AssertCanceledAsync(() => asyncEnumerator.MoveNextAsync());
                                }
                                return;
                            }
                            Assert.True(await asyncEnumerator.MoveNextAsync());
                            Assert.AreEqual(2, asyncEnumerator.Current);
                            Assert.True(await asyncEnumerator.MoveNextAsync());
                            Assert.AreEqual(3, asyncEnumerator.Current);
                            Assert.True(await asyncEnumerator.MoveNextAsync());
                            Assert.AreEqual(4, asyncEnumerator.Current);
                            if (cancelSequence == 1)
                            {
                                configuredCancelationSource.Cancel();
                                enumeratorCancelationSource.Cancel();
                                if (configured1Type == ConfiguredType.ConfiguredWithCancelation || configured2Type == ConfiguredType.ConfiguredWithCancelation || enumeratorToken)
                                {
                                    await TestHelper.AssertCanceledAsync(() => asyncEnumerator.MoveNextAsync());
                                }
                                return;
                            }
                            Assert.True(await asyncEnumerator.MoveNextAsync());
                            Assert.AreEqual(5, asyncEnumerator.Current);
                            configuredCancelationSource.Cancel();
                            enumeratorCancelationSource.Cancel();
                            if (configured1Type == ConfiguredType.ConfiguredWithCancelation || configured2Type == ConfiguredType.ConfiguredWithCancelation || enumeratorToken)
                            {
                                await TestHelper.AssertCanceledAsync(() => asyncEnumerator.MoveNextAsync());
                            }
                        }
                        finally
                        {
                            await asyncEnumerator.DisposeAsync();
                        }
                    }
                }
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        private static IEnumerable<TestCaseData> Union_UnionedBoth_Cancel_Args()
        {
            ConfiguredType[] configuredTypes = new ConfiguredType[]
            {
                ConfiguredType.NotConfigured,
                ConfiguredType.Configured,
                ConfiguredType.ConfiguredWithCancelation
            };
            bool[] bools = new bool[] { true, false };
            int[] cancelSequences = new int[] { 0, 1, 2, 3 };

            foreach (var configured1Type in configuredTypes)
            foreach (var configured2Type in configuredTypes)
            foreach (var configured3Type in configuredTypes)
            foreach (var withComparer1 in bools)
            foreach (var withComparer2 in bools)
            foreach (var withComparer3 in bools)
            foreach (var enumeratorToken in bools)
            foreach (var cancelSequence in cancelSequences)
            {
                if (configured1Type == ConfiguredType.ConfiguredWithCancelation
                    || configured2Type == ConfiguredType.ConfiguredWithCancelation
                    || configured3Type == ConfiguredType.ConfiguredWithCancelation
                    || enumeratorToken)
                {
                    if ((cancelSequence == 0 && (!enumeratorToken && configured1Type != ConfiguredType.ConfiguredWithCancelation))
                        || (cancelSequence == 1 && (!enumeratorToken && configured1Type != ConfiguredType.ConfiguredWithCancelation))
                        || (cancelSequence == 2 && (!enumeratorToken && configured2Type != ConfiguredType.ConfiguredWithCancelation && configured3Type != ConfiguredType.ConfiguredWithCancelation))
                        || (cancelSequence == 3 && (!enumeratorToken && configured2Type != ConfiguredType.ConfiguredWithCancelation && configured3Type != ConfiguredType.ConfiguredWithCancelation))
                        )
                    {
                        continue;
                    }
                    yield return new TestCaseData(configured1Type, configured2Type, configured3Type, withComparer1, withComparer2, withComparer3, enumeratorToken, cancelSequence);
                }
            }
        }

        [Test, TestCaseSource(nameof(Union_UnionedBoth_Cancel_Args))]

        public void Union_UnionedBoth_Cancel(
            ConfiguredType configured1Type,
            ConfiguredType configured2Type,
            ConfiguredType configured3Type,
            bool withComparer1,
            bool withComparer2,
            bool withComparer3,
            bool enumeratorToken,
            int cancelSequence)
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
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(1);
                });
                var xs3 = AsyncEnumerable.Create<int>(async (writer, cancelationToken) =>
                {
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(1);
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(3);
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
                    await writer.YieldAsync(4);
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(6);
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(2);
                });
                using (var configuredCancelationSource = CancelationSource.New())
                {
                    using (var enumeratorCancelationSource = CancelationSource.New())
                    {
                        var asyncEnumerator = xs1
                            .Union(xs2, configured1Type != ConfiguredType.NotConfigured, GetDefaultOrNullComparer<int>(withComparer1),
                                configured1Type == ConfiguredType.ConfiguredWithCancelation ? configuredCancelationSource.Token : CancelationToken.None)
                            .Union(
                                xs3.Union(xs4, configured2Type != ConfiguredType.NotConfigured, GetDefaultOrNullComparer<int>(withComparer2),
                                    configured2Type == ConfiguredType.ConfiguredWithCancelation ? configuredCancelationSource.Token : CancelationToken.None), configured3Type != ConfiguredType.NotConfigured, GetDefaultOrNullComparer<int>(withComparer3),
                                configured3Type == ConfiguredType.ConfiguredWithCancelation ? configuredCancelationSource.Token : CancelationToken.None)

                            .GetAsyncEnumerator(enumeratorToken ? enumeratorCancelationSource.Token : CancelationToken.None);
                        try
                        {
                            Assert.True(await asyncEnumerator.MoveNextAsync());
                            Assert.AreEqual(1, asyncEnumerator.Current);
                            if (cancelSequence == 0)
                            {
                                configuredCancelationSource.Cancel();
                                enumeratorCancelationSource.Cancel();
                                await TestHelper.AssertCanceledAsync(() => asyncEnumerator.MoveNextAsync());
                                return;
                            }
                            Assert.True(await asyncEnumerator.MoveNextAsync());
                            Assert.AreEqual(2, asyncEnumerator.Current);
                            Assert.True(await asyncEnumerator.MoveNextAsync());
                            Assert.AreEqual(3, asyncEnumerator.Current);
                            if (cancelSequence == 1)
                            {
                                configuredCancelationSource.Cancel();
                                enumeratorCancelationSource.Cancel();
                                await TestHelper.AssertCanceledAsync(() => asyncEnumerator.MoveNextAsync());
                                return;
                            }
                            Assert.True(await asyncEnumerator.MoveNextAsync());
                            Assert.AreEqual(4, asyncEnumerator.Current);
                            Assert.True(await asyncEnumerator.MoveNextAsync());
                            Assert.AreEqual(5, asyncEnumerator.Current);
                            if (cancelSequence == 2)
                            {
                                configuredCancelationSource.Cancel();
                                enumeratorCancelationSource.Cancel();
                                await TestHelper.AssertCanceledAsync(() => asyncEnumerator.MoveNextAsync());
                                return;
                            }
                            Assert.True(await asyncEnumerator.MoveNextAsync());
                            Assert.AreEqual(6, asyncEnumerator.Current);
                            configuredCancelationSource.Cancel();
                            enumeratorCancelationSource.Cancel();
                            await TestHelper.AssertCanceledAsync(() => asyncEnumerator.MoveNextAsync());
                        }
                        finally
                        {
                            await asyncEnumerator.DisposeAsync();
                        }
                    }
                }
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void UnionBy_Cancel(
            [Values] ConfiguredType configuredType,
            [Values] bool async,
            [Values] bool captureValue,
            [Values] bool withComparer,
            [Values] bool enumeratorToken,
            [Values(0, 1)] int cancelSequence)
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
                var ys = AsyncEnumerable.Create<int>(async (writer, cancelationToken) =>
                {
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(3);
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(4);
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(5);
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(1);
                });
                using (var configuredCancelationSource = CancelationSource.New())
                {
                    using (var enumeratorCancelationSource = CancelationSource.New())
                    {
                        var asyncEnumerator = xs.UnionBy(ys, configuredType != ConfiguredType.NotConfigured, async, captureValue, x => x, GetDefaultOrNullComparer<int>(withComparer),
                            configuredType == ConfiguredType.ConfiguredWithCancelation ? configuredCancelationSource.Token : CancelationToken.None)
                            .GetAsyncEnumerator(enumeratorToken ? enumeratorCancelationSource.Token : CancelationToken.None);
                        try
                        {
                            Assert.True(await asyncEnumerator.MoveNextAsync());
                            Assert.AreEqual(1, asyncEnumerator.Current);
                            if (cancelSequence == 0)
                            {
                                configuredCancelationSource.Cancel();
                                enumeratorCancelationSource.Cancel();
                                if (configuredType == ConfiguredType.ConfiguredWithCancelation || enumeratorToken)
                                {
                                    await TestHelper.AssertCanceledAsync(() => asyncEnumerator.MoveNextAsync());
                                }
                                return;
                            }
                            Assert.True(await asyncEnumerator.MoveNextAsync());
                            Assert.AreEqual(2, asyncEnumerator.Current);
                            Assert.True(await asyncEnumerator.MoveNextAsync());
                            Assert.AreEqual(3, asyncEnumerator.Current);
                            Assert.True(await asyncEnumerator.MoveNextAsync());
                            Assert.AreEqual(4, asyncEnumerator.Current);
                            configuredCancelationSource.Cancel();
                            enumeratorCancelationSource.Cancel();
                            if (configuredType == ConfiguredType.ConfiguredWithCancelation || enumeratorToken)
                            {
                                await TestHelper.AssertCanceledAsync(() => asyncEnumerator.MoveNextAsync());
                            }
                        }
                        finally
                        {
                            await asyncEnumerator.DisposeAsync();
                        }
                    }
                }
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }
    }
}