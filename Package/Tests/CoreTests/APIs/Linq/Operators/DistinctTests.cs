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
    public class DistinctTests
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
        public void Distinct_NullArgumentThrows()
        {
            var enumerable = AsyncEnumerable.Return(42);
            var nullComparer = default(IEqualityComparer<int>);

            Assert.Catch<System.ArgumentNullException>(() => enumerable.Distinct(nullComparer));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).Distinct(nullComparer));

            enumerable.GetAsyncEnumerator().DisposeAsync().Forget();
        }

        [Test]
        public void DistinctBy_NullArgumentThrows()
        {
            var enumerable = AsyncEnumerable.Return(42);
            var captureValue = "captureValue";
            var nullComparer = default(IEqualityComparer<int>);

            Assert.Catch<System.ArgumentNullException>(() => enumerable.DistinctBy(default(Func<int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.DistinctBy(x => x, nullComparer));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.DistinctBy(default(Func<int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.DistinctBy(async x => x, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.DistinctBy(captureValue, default(Func<string, int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.DistinctBy(captureValue, (cv, x) => x, nullComparer));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.DistinctBy(captureValue, default(Func<string, int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.DistinctBy(captureValue, async (cv, x) => x, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).DistinctBy(default(Func<int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).DistinctBy(x => x, nullComparer));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).DistinctBy(default(Func<int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).DistinctBy(async x => x, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).DistinctBy(captureValue, default(Func<string, int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).DistinctBy(captureValue, (cv, x) => x, nullComparer));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).DistinctBy(captureValue, default(Func<string, int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).DistinctBy(captureValue, async (cv, x) => x, nullComparer));

            enumerable.GetAsyncEnumerator().DisposeAsync().Forget();
        }
#endif //PROMISE_DEBUG

        // We test all the different overloads.
        private static AsyncEnumerable<TSource> Distinct<TSource>(AsyncEnumerable<TSource> asyncEnumerable,
            bool configured,
            IEqualityComparer<TSource> equalityComparer = null)
        {
            return configured
                ? equalityComparer != null
                    ? asyncEnumerable.ConfigureAwait(SynchronizationOption.Foreground).Distinct(equalityComparer)
                    : asyncEnumerable.ConfigureAwait(SynchronizationOption.Foreground).Distinct()
                : equalityComparer != null
                    ? asyncEnumerable.Distinct(equalityComparer)
                    : asyncEnumerable.Distinct();
        }

        private static AsyncEnumerable<TSource> DistinctBy<TSource, TKey>(AsyncEnumerable<TSource> asyncEnumerable,
            bool configured,
            bool async,
            bool captureValue,
            Func<TSource, TKey> keySelector,
            IEqualityComparer<TKey> equalityComparer = null)
        {
            if (configured)
            {
                return DistinctBy(asyncEnumerable.ConfigureAwait(SynchronizationOption.Foreground), async, captureValue, keySelector, equalityComparer);
            }

            const string valueCapture = "valueCapture";

            if (!captureValue)
            {
                return async
                    ? equalityComparer != null
                        ? asyncEnumerable.DistinctBy(async x => keySelector(x), equalityComparer)
                        : asyncEnumerable.DistinctBy(async x => keySelector(x))
                    : equalityComparer != null
                        ? asyncEnumerable.DistinctBy(keySelector, equalityComparer)
                        : asyncEnumerable.DistinctBy(keySelector);
            }
            else
            {
                return async
                    ? equalityComparer != null
                        ? asyncEnumerable.DistinctBy(valueCapture, async (cv, x) =>
                        {
                            Assert.AreEqual(valueCapture, cv);
                            return keySelector(x);
                        }, equalityComparer)
                        : asyncEnumerable.DistinctBy(valueCapture, async (cv, x) =>
                        {
                            Assert.AreEqual(valueCapture, cv);
                            return keySelector(x);
                        })
                    : equalityComparer != null
                        ? asyncEnumerable.DistinctBy(valueCapture, (cv, x) =>
                        {
                            Assert.AreEqual(valueCapture, cv);
                            return keySelector(x);
                        }, equalityComparer)
                        : asyncEnumerable.DistinctBy(valueCapture, (cv, x) =>
                        {
                            Assert.AreEqual(valueCapture, cv);
                            return keySelector(x);
                        });
            }
        }

        private static AsyncEnumerable<TSource> DistinctBy<TSource, TKey>(ConfiguredAsyncEnumerable<TSource> asyncEnumerable,
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
                        ? asyncEnumerable.DistinctBy(async x => keySelector(x), equalityComparer)
                        : asyncEnumerable.DistinctBy(async x => keySelector(x))
                    : equalityComparer != null
                        ? asyncEnumerable.DistinctBy(keySelector, equalityComparer)
                        : asyncEnumerable.DistinctBy(keySelector);
            }
            else
            {
                return async
                    ? equalityComparer != null
                        ? asyncEnumerable.DistinctBy(valueCapture, async (cv, x) =>
                        {
                            Assert.AreEqual(valueCapture, cv);
                            return keySelector(x);
                        }, equalityComparer)
                        : asyncEnumerable.DistinctBy(valueCapture, async (cv, x) =>
                        {
                            Assert.AreEqual(valueCapture, cv);
                            return keySelector(x);
                        })
                    : equalityComparer != null
                        ? asyncEnumerable.DistinctBy(valueCapture, (cv, x) =>
                        {
                            Assert.AreEqual(valueCapture, cv);
                            return keySelector(x);
                        }, equalityComparer)
                        : asyncEnumerable.DistinctBy(valueCapture, (cv, x) =>
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
        public void Distinct_Empty(
            [Values] bool configured,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var asyncEnumerator = Distinct(AsyncEnumerable.Empty<int>(), configured, GetDefaultOrNullComparer<int>(withComparer)).GetAsyncEnumerator();
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Distinct_Simple(
            [Values] bool configured,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var asyncEnumerator = Distinct(new[] { 1, 2, 1, 3, 5, 2, 1, 4 }.ToAsyncEnumerable(), configured, GetDefaultOrNullComparer<int>(withComparer)).GetAsyncEnumerator();
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
        public void Distinct_Comparer(
            [Values] bool configured)
        {
            Promise.Run(async () =>
            {
                var asyncEnumerator = Distinct(new[] { 1, -2, -1, 3, 5, 2, 1, 4 }.ToAsyncEnumerable(), configured, new Eq()).GetAsyncEnumerator();
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(1, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(-2, asyncEnumerator.Current);
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
        public void Distinct_Source_Throws(
            [Values] bool configured,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var ex = new Exception("Bang!");
                var asyncEnumerator = Distinct(AsyncEnumerable<int>.Rejected(ex), configured, GetDefaultOrNullComparer<int>(withComparer)).GetAsyncEnumerator();
                await TestHelper.AssertThrowsAsync(() => asyncEnumerator.MoveNextAsync(), ex);
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void DistinctBy_Empty(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureValue,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var asyncEnumerator = DistinctBy(AsyncEnumerable.Empty<int>(), configured, async, captureValue, x => x, GetDefaultOrNullComparer<int>(withComparer)).GetAsyncEnumerator();
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void DistinctBy_Simple(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureValue,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var asyncEnumerator = DistinctBy(new[] { 1, 2, 1, 3, 5, 2, 1, 4 }.ToAsyncEnumerable(), configured, async, captureValue, x => x, GetDefaultOrNullComparer<int>(withComparer)).GetAsyncEnumerator();
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
        public void DistinctBy_Comparer(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureValue)
        {
            Promise.Run(async () =>
            {
                var asyncEnumerator = DistinctBy(new[] { 1, -2, -1, 3, 5, 2, 1, 4 }.ToAsyncEnumerable(), configured, async, captureValue, x => x, new Eq()).GetAsyncEnumerator();
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(1, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(-2, asyncEnumerator.Current);
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
        public void DistinctBy_Source_Throws(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureValue,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var ex = new Exception("Bang!");
                var asyncEnumerator = DistinctBy(AsyncEnumerable<int>.Rejected(ex), configured, async, captureValue, x => x, GetDefaultOrNullComparer<int>(withComparer)).GetAsyncEnumerator();
                await TestHelper.AssertThrowsAsync(() => asyncEnumerator.MoveNextAsync(), ex);
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void DistinctBy_KeySelector_Throws(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureValue,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var ex = new Exception("Bang!");
                var asyncEnumerator = DistinctBy(new[] { 1, 2, 1, 3, 5, 2, 1, 4 }.ToAsyncEnumerable(), configured, async, captureValue, x => { throw ex; }, GetDefaultOrNullComparer<int>(withComparer)).GetAsyncEnumerator();
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