#if CSHARP_7_3_OR_NEWER

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
            IEqualityComparer<TSource> equalityComparer = null)
        {
            return configured
                ? equalityComparer != null
                    ? firstAsyncEnumerable.ConfigureAwait(SynchronizationOption.Foreground).Union(secondAsyncEnumerable, equalityComparer)
                    : firstAsyncEnumerable.ConfigureAwait(SynchronizationOption.Foreground).Union(secondAsyncEnumerable)
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
            IEqualityComparer<TKey> equalityComparer = null)
        {
            if (configured)
            {
                return UnionBy(firstAsyncEnumerable.ConfigureAwait(SynchronizationOption.Foreground), secondAsyncEnumerable, async, captureValue, keySelector, equalityComparer);
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
    }
}

#endif