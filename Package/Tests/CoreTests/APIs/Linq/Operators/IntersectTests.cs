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
using ProtoPromiseTests.APIs.Collections;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

#pragma warning disable CS0162 // Unreachable code detected
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace ProtoPromiseTests.APIs.Linq
{
    public class IntersectTests
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
        public void Intersect_NullArgumentThrows()
        {
            var first = AsyncEnumerable.Return(42);
            var second = AsyncEnumerable.Return(21);
            var nullComparer = default(IEqualityComparer<int>);

            Assert.Catch<System.ArgumentNullException>(() => first.Intersect(second, nullComparer));
            Assert.Catch<System.ArgumentNullException>(() => first.ConfigureAwait(SynchronizationOption.Synchronous).Intersect(second, nullComparer));

            first.GetAsyncEnumerator().DisposeAsync().Forget();
            second.GetAsyncEnumerator().DisposeAsync().Forget();
        }

        [Test]
        public void IntersectBy_NullArgumentThrows()
        {
            var first = AsyncEnumerable.Return(42);
            var second = AsyncEnumerable.Return(21);
            var captureValue = "captureValue";
            var nullComparer = default(IEqualityComparer<int>);

            Assert.Catch<System.ArgumentNullException>(() => first.IntersectBy(second, default(Func<int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => first.IntersectBy(second, x => x, nullComparer));
            Assert.Catch<System.ArgumentNullException>(() => first.IntersectBy(second, default(Func<int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => first.IntersectBy(second, async x => x, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => first.IntersectBy(second, captureValue, default(Func<string, int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => first.IntersectBy(second, captureValue, (cv, x) => x, nullComparer));
            Assert.Catch<System.ArgumentNullException>(() => first.IntersectBy(second, captureValue, default(Func<string, int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => first.IntersectBy(second, captureValue, async (cv, x) => x, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => first.ConfigureAwait(SynchronizationOption.Synchronous).IntersectBy(second, default(Func<int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => first.ConfigureAwait(SynchronizationOption.Synchronous).IntersectBy(second, x => x, nullComparer));
            Assert.Catch<System.ArgumentNullException>(() => first.ConfigureAwait(SynchronizationOption.Synchronous).IntersectBy(second, default(Func<int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => first.ConfigureAwait(SynchronizationOption.Synchronous).IntersectBy(second, async x => x, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => first.ConfigureAwait(SynchronizationOption.Synchronous).IntersectBy(second, captureValue, default(Func<string, int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => first.ConfigureAwait(SynchronizationOption.Synchronous).IntersectBy(second, captureValue, (cv, x) => x, nullComparer));
            Assert.Catch<System.ArgumentNullException>(() => first.ConfigureAwait(SynchronizationOption.Synchronous).IntersectBy(second, captureValue, default(Func<string, int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => first.ConfigureAwait(SynchronizationOption.Synchronous).IntersectBy(second, captureValue, async (cv, x) => x, nullComparer));

            first.GetAsyncEnumerator().DisposeAsync().Forget();
            second.GetAsyncEnumerator().DisposeAsync().Forget();
        }
#endif //PROMISE_DEBUG

        // We test all the different overloads.
        private static AsyncEnumerable<TSource> Intersect<TSource>(AsyncEnumerable<TSource> firstAsyncEnumerable,
            AsyncEnumerable<TSource> secondAsyncEnumerable,
            bool configured,
            IEqualityComparer<TSource> equalityComparer = null)
        {
            return configured
                ? equalityComparer != null
                    ? firstAsyncEnumerable.ConfigureAwait(SynchronizationOption.Foreground).Intersect(secondAsyncEnumerable, equalityComparer ?? EqualityComparer<TSource>.Default)
                    : firstAsyncEnumerable.ConfigureAwait(SynchronizationOption.Foreground).Intersect(secondAsyncEnumerable)
                : equalityComparer != null
                    ? firstAsyncEnumerable.Intersect(secondAsyncEnumerable, equalityComparer)
                    : firstAsyncEnumerable.Intersect(secondAsyncEnumerable);
        }

        private static AsyncEnumerable<TSource> IntersectBy<TSource, TKey>(AsyncEnumerable<TSource> firstAsyncEnumerable,
            AsyncEnumerable<TSource> secondAsyncEnumerable,
            bool configured,
            bool async,
            bool captureValue,
            Func<TSource, TKey> keySelector,
            IEqualityComparer<TKey> equalityComparer = null)
        {
            if (configured)
            {
                return IntersectBy(firstAsyncEnumerable.ConfigureAwait(SynchronizationOption.Foreground), secondAsyncEnumerable, async, captureValue, keySelector, equalityComparer);
            }

            const string valueCapture = "valueCapture";

            if (!captureValue)
            {
                return async
                    ? equalityComparer != null
                        ? firstAsyncEnumerable.IntersectBy(secondAsyncEnumerable, async x => keySelector(x), equalityComparer)
                        : firstAsyncEnumerable.IntersectBy(secondAsyncEnumerable, async x => keySelector(x))
                    : equalityComparer != null
                        ? firstAsyncEnumerable.IntersectBy(secondAsyncEnumerable, keySelector, equalityComparer)
                        : firstAsyncEnumerable.IntersectBy(secondAsyncEnumerable, keySelector);
            }
            else
            {
                return async
                    ? equalityComparer != null
                        ? firstAsyncEnumerable.IntersectBy(secondAsyncEnumerable, valueCapture, async (cv, x) =>
                        {
                            Assert.AreEqual(valueCapture, cv);
                            return keySelector(x);
                        }, equalityComparer)
                        : firstAsyncEnumerable.IntersectBy(secondAsyncEnumerable, valueCapture, async (cv, x) =>
                        {
                            Assert.AreEqual(valueCapture, cv);
                            return keySelector(x);
                        })
                    : equalityComparer != null
                        ? firstAsyncEnumerable.IntersectBy(secondAsyncEnumerable, valueCapture, (cv, x) =>
                        {
                            Assert.AreEqual(valueCapture, cv);
                            return keySelector(x);
                        }, equalityComparer)
                        : firstAsyncEnumerable.IntersectBy(secondAsyncEnumerable, valueCapture, (cv, x) =>
                        {
                            Assert.AreEqual(valueCapture, cv);
                            return keySelector(x);
                        });
            }
        }

        private static AsyncEnumerable<TSource> IntersectBy<TSource, TKey>(ConfiguredAsyncEnumerable<TSource> firstAsyncEnumerable,
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
                        ? firstAsyncEnumerable.IntersectBy(secondAsyncEnumerable, async x => keySelector(x), equalityComparer)
                        : firstAsyncEnumerable.IntersectBy(secondAsyncEnumerable, async x => keySelector(x))
                    : equalityComparer != null
                        ? firstAsyncEnumerable.IntersectBy(secondAsyncEnumerable, keySelector, equalityComparer)
                        : firstAsyncEnumerable.IntersectBy(secondAsyncEnumerable, keySelector);
            }
            else
            {
                return async
                    ? equalityComparer != null
                        ? firstAsyncEnumerable.IntersectBy(secondAsyncEnumerable, valueCapture, async (cv, x) =>
                        {
                            Assert.AreEqual(valueCapture, cv);
                            return keySelector(x);
                        }, equalityComparer)
                        : firstAsyncEnumerable.IntersectBy(secondAsyncEnumerable, valueCapture, async (cv, x) =>
                        {
                            Assert.AreEqual(valueCapture, cv);
                            return keySelector(x);
                        })
                    : equalityComparer != null
                        ? firstAsyncEnumerable.IntersectBy(secondAsyncEnumerable, valueCapture, (cv, x) =>
                        {
                            Assert.AreEqual(valueCapture, cv);
                            return keySelector(x);
                        }, equalityComparer)
                        : firstAsyncEnumerable.IntersectBy(secondAsyncEnumerable, valueCapture, (cv, x) =>
                        {
                            Assert.AreEqual(valueCapture, cv);
                            return keySelector(x);
                        });
            }
        }

        private static IEqualityComparer<T> GetDefaultOrNullComparer<T>(bool defaultComparer)
        {
            return defaultComparer ? EqualityComparer<T>.Default : null;
        }

        [Test]
        public void Intersect_EmptyFirst(
            [Values] bool configured,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var xs = AsyncEnumerable.Empty<int>();
                var ys = new[] { 3, 5, 1, 4 }.ToAsyncEnumerable();
                var asyncEnumerator = Intersect(xs, ys, configured, GetDefaultOrNullComparer<int>(withComparer)).GetAsyncEnumerator();
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Intersect_EmptySecond(
            [Values] bool configured,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var xs = new[] { 3, 5, 1, 4 }.ToAsyncEnumerable();
                var ys = AsyncEnumerable.Empty<int>();
                var asyncEnumerator = Intersect(xs, ys, configured, GetDefaultOrNullComparer<int>(withComparer)).GetAsyncEnumerator();
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Intersect_Simple(
            [Values] bool configured,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var xs = new[] { 1, 2, 3 }.ToAsyncEnumerable();
                var ys = new[] { 3, 5, 1, 4 }.ToAsyncEnumerable();
                var asyncEnumerator = Intersect(xs, ys, configured, GetDefaultOrNullComparer<int>(withComparer)).GetAsyncEnumerator();
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(1, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(3, asyncEnumerator.Current);
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Intersect_EqualityComparer()
        {
            Promise.Run(async () =>
            {
                var xs = new[] { 1, 2, -3 }.ToAsyncEnumerable();
                var ys = new[] { 3, 5, -1, 4 }.ToAsyncEnumerable();
                var asyncEnumerator = xs.Intersect(ys, new Eq()).GetAsyncEnumerator();
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(1, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(-3, asyncEnumerator.Current);
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Intersect_FirstThrows()
        {
            Promise.Run(async () =>
            {
                var ex = new Exception("Bang!");
                var xs = AsyncEnumerable<int>.Rejected(ex);
                var ys = new[] { 3, 5, -1, 4 }.ToAsyncEnumerable();
                var asyncEnumerator = xs.Intersect(ys, new Eq()).GetAsyncEnumerator();
                await TestHelper.AssertThrowsAsync(() => asyncEnumerator.MoveNextAsync(), ex);
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Intersect_SecondThrows()
        {
            Promise.Run(async () =>
            {
                var ex = new Exception("Bang!");
                var xs = new[] { 3, 5, -1, 4 }.ToAsyncEnumerable();
                var ys = AsyncEnumerable<int>.Rejected(ex);
                var asyncEnumerator = xs.Intersect(ys, new Eq()).GetAsyncEnumerator();
                await TestHelper.AssertThrowsAsync(() => asyncEnumerator.MoveNextAsync(), ex);
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void IntersectBy_EmptyFirst(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureValue,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var xs = AsyncEnumerable.Empty<int>();
                var ys = new[] { 3, 5, 1, 4 }.ToAsyncEnumerable();
                var asyncEnumerator = IntersectBy(xs, ys, configured, async, captureValue, x => x, GetDefaultOrNullComparer<int>(withComparer)).GetAsyncEnumerator();
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void IntersectBy_EmptySecond(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureValue,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var xs = new[] { 3, 5, 1, 4 }.ToAsyncEnumerable();
                var ys = AsyncEnumerable.Empty<int>();
                var asyncEnumerator = IntersectBy(xs, ys, configured, async, captureValue, x => x, GetDefaultOrNullComparer<int>(withComparer)).GetAsyncEnumerator();
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void IntersectBy_Simple(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureValue,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var xs = new[] { 1, 2, 3 }.ToAsyncEnumerable();
                var ys = new[] { 3, 5, 1, 4 }.ToAsyncEnumerable();
                var asyncEnumerator = IntersectBy(xs, ys, configured, async, captureValue, x => x, GetDefaultOrNullComparer<int>(withComparer)).GetAsyncEnumerator();
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(1, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(3, asyncEnumerator.Current);
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void IntersectBy_EqualityComparer(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureValue)
        {
            Promise.Run(async () =>
            {
                var xs = new[] { 1, 2, -3 }.ToAsyncEnumerable();
                var ys = new[] { 3, 5, -1, 4 }.ToAsyncEnumerable();
                var asyncEnumerator = IntersectBy(xs, ys, configured, async, captureValue, x => x, new Eq()).GetAsyncEnumerator();
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(1, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(-3, asyncEnumerator.Current);
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void IntersectBy_FirstThrows(
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
                var asyncEnumerator = IntersectBy(xs, ys, configured, async, captureValue, x => x, GetDefaultOrNullComparer<int>(withComparer)).GetAsyncEnumerator();
                await TestHelper.AssertThrowsAsync(() => asyncEnumerator.MoveNextAsync(), ex);
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void IntersectBy_SecondThrows(
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
                var asyncEnumerator = IntersectBy(xs, ys, configured, async, captureValue, x => x, GetDefaultOrNullComparer<int>(withComparer)).GetAsyncEnumerator();
                await TestHelper.AssertThrowsAsync(() => asyncEnumerator.MoveNextAsync(), ex);
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