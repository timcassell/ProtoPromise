﻿#if CSHARP_7_3_OR_NEWER

#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using NUnit.Framework;
using Proto.Promises;
using Proto.Promises.Async.CompilerServices;
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
    public static class OrderHelper
    {
        // We test all the different overloads.
        public static OrderedAsyncEnumerable<TSource> OrderBy<TSource, TKey>(this AsyncEnumerable<TSource> asyncEnumerable,
            bool configured,
            bool async,
            Func<TSource, TKey> keySelector, bool captureKey,
            IComparer<TKey> comparer = null)
        {
            if (configured)
            {
                return OrderBy(asyncEnumerable.ConfigureAwait(SynchronizationOption.Foreground), async, keySelector, captureKey, comparer);
            }

            const string keyCapture = "keyCapture";

            if (!captureKey)
            {
                return async
                    ? asyncEnumerable.OrderBy(async x => keySelector(x), comparer)
                    : asyncEnumerable.OrderBy(keySelector, comparer);
            }
            else
            {
                return async
                    ? asyncEnumerable.OrderBy(keyCapture, async (cv, x) =>
                    {
                        Assert.AreEqual(keyCapture, cv);
                        return keySelector(x);
                    }, comparer)
                    : asyncEnumerable.OrderBy(keyCapture, (cv, x) =>
                    {
                        Assert.AreEqual(keyCapture, cv);
                        return keySelector(x);
                    }, comparer);
            }
        }

        public static OrderedAsyncEnumerable<TSource> OrderBy<TSource, TKey>(this ConfiguredAsyncEnumerable<TSource> asyncEnumerable,
            bool async,
            Func<TSource, TKey> keySelector, bool captureKey,
            IComparer<TKey> comparer)
        {
            const string keyCapture = "keyCapture";

            if (!captureKey)
            {
                return async
                    ? asyncEnumerable.OrderBy(async x => keySelector(x), comparer)
                    : asyncEnumerable.OrderBy(keySelector, comparer);
            }
            else
            {
                return async
                    ? asyncEnumerable.OrderBy(keyCapture, async (cv, x) =>
                    {
                        Assert.AreEqual(keyCapture, cv);
                        return keySelector(x);
                    }, comparer)
                    : asyncEnumerable.OrderBy(keyCapture, (cv, x) =>
                    {
                        Assert.AreEqual(keyCapture, cv);
                        return keySelector(x);
                    }, comparer);
            }
        }

        public static OrderedAsyncEnumerable<TSource> OrderByDescending<TSource, TKey>(this AsyncEnumerable<TSource> asyncEnumerable,
            bool configured,
            bool async,
            Func<TSource, TKey> keySelector, bool captureKey,
            IComparer<TKey> comparer = null)
        {
            if (configured)
            {
                return OrderByDescending(asyncEnumerable.ConfigureAwait(SynchronizationOption.Foreground), async, keySelector, captureKey, comparer);
            }

            const string keyCapture = "keyCapture";

            if (!captureKey)
            {
                return async
                    ? asyncEnumerable.OrderByDescending(async x => keySelector(x), comparer)
                    : asyncEnumerable.OrderByDescending(keySelector, comparer);
            }
            else
            {
                return async
                    ? asyncEnumerable.OrderByDescending(keyCapture, async (cv, x) =>
                    {
                        Assert.AreEqual(keyCapture, cv);
                        return keySelector(x);
                    }, comparer)
                    : asyncEnumerable.OrderByDescending(keyCapture, (cv, x) =>
                    {
                        Assert.AreEqual(keyCapture, cv);
                        return keySelector(x);
                    }, comparer);
            }
        }

        public static OrderedAsyncEnumerable<TSource> OrderByDescending<TSource, TKey>(this ConfiguredAsyncEnumerable<TSource> asyncEnumerable,
            bool async,
            Func<TSource, TKey> keySelector, bool captureKey,
            IComparer<TKey> comparer)
        {
            const string keyCapture = "keyCapture";

            if (!captureKey)
            {
                return async
                    ? asyncEnumerable.OrderByDescending(async x => keySelector(x), comparer)
                    : asyncEnumerable.OrderByDescending(keySelector, comparer);
            }
            else
            {
                return async
                    ? asyncEnumerable.OrderByDescending(keyCapture, async (cv, x) =>
                    {
                        Assert.AreEqual(keyCapture, cv);
                        return keySelector(x);
                    }, comparer)
                    : asyncEnumerable.OrderByDescending(keyCapture, (cv, x) =>
                    {
                        Assert.AreEqual(keyCapture, cv);
                        return keySelector(x);
                    }, comparer);
            }
        }

        public static OrderedAsyncEnumerable<TSource> ThenBy<TSource, TKey>(this OrderedAsyncEnumerable<TSource> orderedAsyncEnumerable,
            bool async,
            Func<TSource, TKey> keySelector, bool captureKey,
            IComparer<TKey> comparer = null)
        {
            const string keyCapture = "keyCapture";

            if (!captureKey)
            {
                return async
                    ? orderedAsyncEnumerable.ThenBy(async x => keySelector(x), comparer)
                    : orderedAsyncEnumerable.ThenBy(keySelector, comparer);
            }
            else
            {
                return async
                    ? orderedAsyncEnumerable.ThenBy(keyCapture, async (cv, x) =>
                    {
                        Assert.AreEqual(keyCapture, cv);
                        return keySelector(x);
                    }, comparer)
                    : orderedAsyncEnumerable.ThenBy(keyCapture, (cv, x) =>
                    {
                        Assert.AreEqual(keyCapture, cv);
                        return keySelector(x);
                    }, comparer);
            }
        }

        public static OrderedAsyncEnumerable<TSource> ThenByDescending<TSource, TKey>(this OrderedAsyncEnumerable<TSource> orderedAsyncEnumerable,
            bool async,
            Func<TSource, TKey> keySelector, bool captureKey,
            IComparer<TKey> comparer = null)
        {
            const string keyCapture = "keyCapture";

            if (!captureKey)
            {
                return async
                    ? orderedAsyncEnumerable.ThenByDescending(async x => keySelector(x), comparer)
                    : orderedAsyncEnumerable.ThenByDescending(keySelector, comparer);
            }
            else
            {
                return async
                    ? orderedAsyncEnumerable.ThenByDescending(keyCapture, async (cv, x) =>
                    {
                        Assert.AreEqual(keyCapture, cv);
                        return keySelector(x);
                    }, comparer)
                    : orderedAsyncEnumerable.ThenByDescending(keyCapture, (cv, x) =>
                    {
                        Assert.AreEqual(keyCapture, cv);
                        return keySelector(x);
                    }, comparer);
            }
        }

        public static OrderedAsyncEnumerable<TSource> Order<TSource>(this AsyncEnumerable<TSource> asyncEnumerable,
            bool configured,
            IComparer<TSource> comparer = null)
        {
            return configured
                ? asyncEnumerable.ConfigureAwait(SynchronizationOption.Foreground).Order(comparer)
                : asyncEnumerable.Order(comparer);
        }

        public static OrderedAsyncEnumerable<TSource> OrderDescending<TSource>(this AsyncEnumerable<TSource> asyncEnumerable,
            bool configured,
            IComparer<TSource> comparer = null)
        {
            return configured
                ? asyncEnumerable.ConfigureAwait(SynchronizationOption.Foreground).OrderDescending(comparer)
                : asyncEnumerable.OrderDescending(comparer);
        }
    }

    public class OrderByTests
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
        public void OrderBy_NullArgumentThrows()
        {
            var enumerable = AsyncEnumerable.Return(42);
            var captureValue = "captureValue";

            Assert.Catch<System.ArgumentNullException>(() => enumerable.OrderBy(default(Func<int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.OrderBy(default(Func<int, int>), Comparer<int>.Default));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.OrderBy(captureValue, default(Func<string, int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.OrderBy(captureValue, default(Func<string, int, int>), Comparer<int>.Default));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.OrderBy(default(Func<int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.OrderBy(default(Func<int, Promise<int>>), Comparer<int>.Default));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.OrderBy(captureValue, default(Func<string, int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.OrderBy(captureValue, default(Func<string, int, Promise<int>>), Comparer<int>.Default));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).OrderBy(default(Func<int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).OrderBy(default(Func<int, int>), Comparer<int>.Default));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).OrderBy(captureValue, default(Func<string, int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).OrderBy(captureValue, default(Func<string, int, int>), Comparer<int>.Default));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).OrderBy(default(Func<int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).OrderBy(default(Func<int, Promise<int>>), Comparer<int>.Default));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).OrderBy(captureValue, default(Func<string, int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).OrderBy(captureValue, default(Func<string, int, Promise<int>>), Comparer<int>.Default));

            enumerable.GetAsyncEnumerator().DisposeAsync().Forget();
        }

        [Test]
        public void OrderByDescending_NullArgumentThrows()
        {
            var enumerable = AsyncEnumerable.Return(42);
            var captureValue = "captureValue";

            Assert.Catch<System.ArgumentNullException>(() => enumerable.OrderByDescending(default(Func<int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.OrderByDescending(default(Func<int, int>), Comparer<int>.Default));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.OrderByDescending(captureValue, default(Func<string, int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.OrderByDescending(captureValue, default(Func<string, int, int>), Comparer<int>.Default));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.OrderByDescending(default(Func<int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.OrderByDescending(default(Func<int, Promise<int>>), Comparer<int>.Default));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.OrderByDescending(captureValue, default(Func<string, int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.OrderByDescending(captureValue, default(Func<string, int, Promise<int>>), Comparer<int>.Default));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).OrderByDescending(default(Func<int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).OrderByDescending(default(Func<int, int>), Comparer<int>.Default));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).OrderByDescending(captureValue, default(Func<string, int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).OrderByDescending(captureValue, default(Func<string, int, int>), Comparer<int>.Default));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).OrderByDescending(default(Func<int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).OrderByDescending(default(Func<int, Promise<int>>), Comparer<int>.Default));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).OrderByDescending(captureValue, default(Func<string, int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).OrderByDescending(captureValue, default(Func<string, int, Promise<int>>), Comparer<int>.Default));

            enumerable.GetAsyncEnumerator().DisposeAsync().Forget();
        }

        [Test]
        public void ThenBy_NullArgumentThrows()
        {
            var orderedEnumerable = AsyncEnumerable.Return(42).Order();
            var captureValue = "captureValue";

            Assert.Catch<System.ArgumentNullException>(() => orderedEnumerable.ThenBy(default(Func<int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => orderedEnumerable.ThenBy(default(Func<int, int>), Comparer<int>.Default));

            Assert.Catch<System.ArgumentNullException>(() => orderedEnumerable.ThenBy(captureValue, default(Func<string, int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => orderedEnumerable.ThenBy(captureValue, default(Func<string, int, int>), Comparer<int>.Default));

            Assert.Catch<System.ArgumentNullException>(() => orderedEnumerable.ThenBy(default(Func<int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => orderedEnumerable.ThenBy(default(Func<int, Promise<int>>), Comparer<int>.Default));

            Assert.Catch<System.ArgumentNullException>(() => orderedEnumerable.ThenBy(captureValue, default(Func<string, int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => orderedEnumerable.ThenBy(captureValue, default(Func<string, int, Promise<int>>), Comparer<int>.Default));

            var enumerator = orderedEnumerable.GetAsyncEnumerator();
            enumerator.MoveNextAsync().Forget();
            enumerator.DisposeAsync().Forget();
        }

        [Test]
        public void ThenByDescending_NullArgumentThrows()
        {
            var orderedEnumerable = AsyncEnumerable.Return(42).Order();
            var captureValue = "captureValue";

            Assert.Catch<System.ArgumentNullException>(() => orderedEnumerable.ThenByDescending(default(Func<int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => orderedEnumerable.ThenByDescending(default(Func<int, int>), Comparer<int>.Default));

            Assert.Catch<System.ArgumentNullException>(() => orderedEnumerable.ThenByDescending(captureValue, default(Func<string, int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => orderedEnumerable.ThenByDescending(captureValue, default(Func<string, int, int>), Comparer<int>.Default));

            Assert.Catch<System.ArgumentNullException>(() => orderedEnumerable.ThenByDescending(default(Func<int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => orderedEnumerable.ThenByDescending(default(Func<int, Promise<int>>), Comparer<int>.Default));

            Assert.Catch<System.ArgumentNullException>(() => orderedEnumerable.ThenByDescending(captureValue, default(Func<string, int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => orderedEnumerable.ThenByDescending(captureValue, default(Func<string, int, Promise<int>>), Comparer<int>.Default));

            var enumerator = orderedEnumerable.GetAsyncEnumerator();
            enumerator.MoveNextAsync().Forget();
            enumerator.DisposeAsync().Forget();
        }
#endif //PROMISE_DEBUG

        [Test]
        public void OrderBy_Empty(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureKey)
        {
            Promise.Run(async () =>
            {
                var asyncEnumerator = AsyncEnumerable.Empty<int>()
                    .OrderBy(configured, async, x => x, captureKey)
                    .GetAsyncEnumerator();
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void OrderBy1(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureKey)
        {
            Promise.Run(async () =>
            {
                var asyncEnumerator = new[] { 2, 6, 1, 5, 7, 8, 9, 3, 4, 0 }
                    .ToAsyncEnumerable()
                    .OrderBy(configured, async, x => x, captureKey)
                    .GetAsyncEnumerator();
                for (var i = 0; i < 10; i++)
                {
                    Assert.True(await asyncEnumerator.MoveNextAsync());
                    Assert.AreEqual(i, asyncEnumerator.Current);
                }
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void OrderBy2(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureKey)
        {
            Promise.Run(async () =>
            {
                var ex = new Exception("Bang!");
                var asyncEnumerator = new[] { 2, 6, 1, 5, 7, 8, 9, 3, 4, 0 }
                    .ToAsyncEnumerable()
                    .OrderBy(configured, async, x => { throw ex; return x; }, captureKey)
                    .GetAsyncEnumerator();
                await TestHelper.AssertThrowsAsync(() => asyncEnumerator.MoveNextAsync(), ex);
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void ThenBy1(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureKey)
        {
            Promise.Run(async () =>
            {
                var asyncEnumerator = new[] { 2, 6, 1, 5, 7, 8, 9, 3, 4, 0 }
                    .ToAsyncEnumerable()
                    .OrderBy(x => 0)
                    .ThenBy(async, x => x, captureKey)
                    .GetAsyncEnumerator();
                for (var i = 0; i < 10; i++)
                {
                    Assert.True(await asyncEnumerator.MoveNextAsync());
                    Assert.AreEqual(i, asyncEnumerator.Current);
                }
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void ThenBy2(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureKey)
        {
            Promise.Run(async () =>
            {
                var ex = new Exception("Bang!");
                var asyncEnumerator = new[] { 2, 6, 1, 5, 7, 8, 9, 3, 4, 0 }
                    .ToAsyncEnumerable()
                    .OrderBy(x => x)
                    .ThenBy(async, x => { throw ex; return x; }, captureKey)
                    .GetAsyncEnumerator();
                await TestHelper.AssertThrowsAsync(() => asyncEnumerator.MoveNextAsync(), ex);
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void OrderByDescending1(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureKey)
        {
            Promise.Run(async () =>
            {
                var asyncEnumerator = new[] { 2, 6, 1, 5, 7, 8, 9, 3, 4, 0 }
                    .ToAsyncEnumerable()
                    .OrderByDescending(configured, async, x => x, captureKey)
                    .GetAsyncEnumerator();
                for (var i = 9; i >= 0; i--)
                {
                    Assert.True(await asyncEnumerator.MoveNextAsync());
                    Assert.AreEqual(i, asyncEnumerator.Current);
                }
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void OrderByDescending2(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureKey)
        {
            Promise.Run(async () =>
            {
                var ex = new Exception("Bang!");
                var asyncEnumerator = new[] { 2, 6, 1, 5, 7, 8, 9, 3, 4, 0 }
                    .ToAsyncEnumerable()
                    .OrderByDescending(configured, async, x => { throw ex; return x; }, captureKey)
                    .GetAsyncEnumerator();
                await TestHelper.AssertThrowsAsync(() => asyncEnumerator.MoveNextAsync(), ex);
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void ThenByDescending1(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureKey)
        {
            Promise.Run(async () =>
            {
                var asyncEnumerator = new[] { 2, 6, 1, 5, 7, 8, 9, 3, 4, 0 }
                    .ToAsyncEnumerable()
                    .OrderBy(x => 0)
                    .ThenByDescending(async, x => x, captureKey)
                    .GetAsyncEnumerator();
                for (var i = 9; i >= 0; i--)
                    {
                    Assert.True(await asyncEnumerator.MoveNextAsync());
                    Assert.AreEqual(i, asyncEnumerator.Current);
                }
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void ThenByDescending2(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureKey)
        {
            Promise.Run(async () =>
            {
                var ex = new Exception("Bang!");
                var asyncEnumerator = new[] { 2, 6, 1, 5, 7, 8, 9, 3, 4, 0 }
                    .ToAsyncEnumerable()
                    .OrderBy(x => x)
                    .ThenByDescending(async, x => { throw ex; return x; }, captureKey)
                    .GetAsyncEnumerator();
                await TestHelper.AssertThrowsAsync(() => asyncEnumerator.MoveNextAsync(), ex);
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void OrderByThenBy(
            [Values] bool configured,
            [Values] bool asyncOrderBy,
            [Values] bool captureKeyOrderBy,
            [Values] bool asyncThenBy,
            [Values] bool captureKeyThenBy)
        {
            Promise.Run(async () =>
            {
                var xs = new[] {
                    new { Name = "Bart", Age = 27 },
                    new { Name = "John", Age = 62 },
                    new { Name = "Eric", Age = 27 },
                    new { Name = "Lisa", Age = 14 },
                    new { Name = "Brad", Age = 27 },
                    new { Name = "Lisa", Age = 23 },
                    new { Name = "Eric", Age = 42 },
                };

                var ys = xs.ToAsyncEnumerable();

                var ress = xs.OrderBy(x => x.Name).ThenBy(x => x.Age).ToArray();
                var resa = ys.OrderBy(configured, asyncOrderBy, x => x.Name, captureKeyOrderBy).ThenBy(asyncThenBy, x => x.Age, captureKeyThenBy).GetAsyncEnumerator();

                Assert.True(await resa.MoveNextAsync());
                Assert.AreEqual(ress[0], resa.Current);
                Assert.True(await resa.MoveNextAsync());
                Assert.AreEqual(ress[1], resa.Current);
                Assert.True(await resa.MoveNextAsync());
                Assert.AreEqual(ress[2], resa.Current);
                Assert.True(await resa.MoveNextAsync());
                Assert.AreEqual(ress[3], resa.Current);
                Assert.True(await resa.MoveNextAsync());
                Assert.AreEqual(ress[4], resa.Current);
                Assert.True(await resa.MoveNextAsync());
                Assert.AreEqual(ress[5], resa.Current);
                Assert.True(await resa.MoveNextAsync());
                Assert.AreEqual(ress[6], resa.Current);

                Assert.False(await resa.MoveNextAsync());
                await resa.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void OrderByThenByDescending(
            [Values] bool configured,
            [Values] bool asyncOrderBy,
            [Values] bool captureKeyOrderBy,
            [Values] bool asyncThenBy,
            [Values] bool captureKeyThenBy)
        {
            Promise.Run(async () =>
            {
                var xs = new[] {
                    new { Name = "Bart", Age = 27 },
                    new { Name = "John", Age = 62 },
                    new { Name = "Eric", Age = 27 },
                    new { Name = "Lisa", Age = 14 },
                    new { Name = "Brad", Age = 27 },
                    new { Name = "Lisa", Age = 23 },
                    new { Name = "Eric", Age = 42 },
                };

                var ys = xs.ToAsyncEnumerable();

                var ress = xs.OrderBy(x => x.Name).ThenByDescending(x => x.Age).ToArray();
                var resa = ys.OrderBy(configured, asyncOrderBy, x => x.Name, captureKeyOrderBy).ThenByDescending(asyncThenBy, x => x.Age, captureKeyThenBy).GetAsyncEnumerator();

                Assert.True(await resa.MoveNextAsync());
                Assert.AreEqual(ress[0], resa.Current);
                Assert.True(await resa.MoveNextAsync());
                Assert.AreEqual(ress[1], resa.Current);
                Assert.True(await resa.MoveNextAsync());
                Assert.AreEqual(ress[2], resa.Current);
                Assert.True(await resa.MoveNextAsync());
                Assert.AreEqual(ress[3], resa.Current);
                Assert.True(await resa.MoveNextAsync());
                Assert.AreEqual(ress[4], resa.Current);
                Assert.True(await resa.MoveNextAsync());
                Assert.AreEqual(ress[5], resa.Current);
                Assert.True(await resa.MoveNextAsync());
                Assert.AreEqual(ress[6], resa.Current);

                Assert.False(await resa.MoveNextAsync());
                await resa.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void OrderByDescendingThenBy(
            [Values] bool configured,
            [Values] bool asyncOrderBy,
            [Values] bool captureKeyOrderBy,
            [Values] bool asyncThenBy,
            [Values] bool captureKeyThenBy)
        {
            Promise.Run(async () =>
            {
                var xs = new[] {
                    new { Name = "Bart", Age = 27 },
                    new { Name = "John", Age = 62 },
                    new { Name = "Eric", Age = 27 },
                    new { Name = "Lisa", Age = 14 },
                    new { Name = "Brad", Age = 27 },
                    new { Name = "Lisa", Age = 23 },
                    new { Name = "Eric", Age = 42 },
                };

                var ys = xs.ToAsyncEnumerable();

                var ress = xs.OrderByDescending(x => x.Name).ThenBy(x => x.Age).ToArray();
                var resa = ys.OrderByDescending(configured, asyncOrderBy, x => x.Name, captureKeyOrderBy).ThenBy(asyncThenBy, x => x.Age, captureKeyThenBy).GetAsyncEnumerator();

                Assert.True(await resa.MoveNextAsync());
                Assert.AreEqual(ress[0], resa.Current);
                Assert.True(await resa.MoveNextAsync());
                Assert.AreEqual(ress[1], resa.Current);
                Assert.True(await resa.MoveNextAsync());
                Assert.AreEqual(ress[2], resa.Current);
                Assert.True(await resa.MoveNextAsync());
                Assert.AreEqual(ress[3], resa.Current);
                Assert.True(await resa.MoveNextAsync());
                Assert.AreEqual(ress[4], resa.Current);
                Assert.True(await resa.MoveNextAsync());
                Assert.AreEqual(ress[5], resa.Current);
                Assert.True(await resa.MoveNextAsync());
                Assert.AreEqual(ress[6], resa.Current);

                Assert.False(await resa.MoveNextAsync());
                await resa.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void OrderByDescendingThenByDescending(
            [Values] bool configured,
            [Values] bool asyncOrderBy,
            [Values] bool captureKeyOrderBy,
            [Values] bool asyncThenBy,
            [Values] bool captureKeyThenBy)
        {
            Promise.Run(async () =>
            {
                var xs = new[] {
                    new { Name = "Bart", Age = 27 },
                    new { Name = "John", Age = 62 },
                    new { Name = "Eric", Age = 27 },
                    new { Name = "Lisa", Age = 14 },
                    new { Name = "Brad", Age = 27 },
                    new { Name = "Lisa", Age = 23 },
                    new { Name = "Eric", Age = 42 },
                };

                var ys = xs.ToAsyncEnumerable();

                var ress = xs.OrderByDescending(x => x.Name).ThenByDescending(x => x.Age).ToArray();
                var resa = ys.OrderByDescending(configured, asyncOrderBy, x => x.Name, captureKeyOrderBy).ThenByDescending(asyncThenBy, x => x.Age, captureKeyThenBy).GetAsyncEnumerator();

                Assert.True(await resa.MoveNextAsync());
                Assert.AreEqual(ress[0], resa.Current);
                Assert.True(await resa.MoveNextAsync());
                Assert.AreEqual(ress[1], resa.Current);
                Assert.True(await resa.MoveNextAsync());
                Assert.AreEqual(ress[2], resa.Current);
                Assert.True(await resa.MoveNextAsync());
                Assert.AreEqual(ress[3], resa.Current);
                Assert.True(await resa.MoveNextAsync());
                Assert.AreEqual(ress[4], resa.Current);
                Assert.True(await resa.MoveNextAsync());
                Assert.AreEqual(ress[5], resa.Current);
                Assert.True(await resa.MoveNextAsync());
                Assert.AreEqual(ress[6], resa.Current);

                Assert.False(await resa.MoveNextAsync());
                await resa.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Order_Empty(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureKey)
        {
            Promise.Run(async () =>
            {
                var asyncEnumerator = AsyncEnumerable.Empty<int>()
                    .Order()
                    .GetAsyncEnumerator();
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Order(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureKey)
        {
            Promise.Run(async () =>
            {
                var asyncEnumerator = new[] { 2, 6, 1, 5, 7, 8, 9, 3, 4, 0 }
                    .ToAsyncEnumerable()
                    .Order()
                    .GetAsyncEnumerator();
                for (var i = 0; i < 10; i++)
                {
                    Assert.True(await asyncEnumerator.MoveNextAsync());
                    Assert.AreEqual(i, asyncEnumerator.Current);
                }
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void OrderDescending(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureKey)
        {
            Promise.Run(async () =>
            {
                var asyncEnumerator = new[] { 2, 6, 1, 5, 7, 8, 9, 3, 4, 0 }
                    .ToAsyncEnumerable()
                    .OrderDescending()
                    .GetAsyncEnumerator();
                for (var i = 9; i >= 0; i--)
                {
                    Assert.True(await asyncEnumerator.MoveNextAsync());
                    Assert.AreEqual(i, asyncEnumerator.Current);
                }
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        private class Person
        {
            public string Name;
            public int Age;
        }

        private class PersonFirstNameComparer : IComparer<Person>
        {
            public int Compare(Person x, Person y)
            {
                return x.Name.CompareTo(y.Name);
            }
        }

        [Test]
        public void OrderThenBy(
            [Values] bool configured,
            [Values] bool asyncThenBy,
            [Values] bool captureKeyThenBy)
        {
            Promise.Run(async () =>
            {
                var xs = new[] {
                    new Person() { Name = "Bart", Age = 27 },
                    new Person() { Name = "John", Age = 62 },
                    new Person() { Name = "Eric", Age = 27 },
                    new Person() { Name = "Lisa", Age = 14 },
                    new Person() { Name = "Brad", Age = 27 },
                    new Person() { Name = "Lisa", Age = 23 },
                    new Person() { Name = "Eric", Age = 42 },
                };

                var ys = xs.ToAsyncEnumerable();

                var ress = xs.OrderBy(x => x.Name).ThenBy(x => x.Age).ToArray();
                var resa = ys.Order(configured, new PersonFirstNameComparer()).ThenBy(asyncThenBy, x => x.Age, captureKeyThenBy).GetAsyncEnumerator();

                Assert.True(await resa.MoveNextAsync());
                Assert.AreEqual(ress[0], resa.Current);
                Assert.True(await resa.MoveNextAsync());
                Assert.AreEqual(ress[1], resa.Current);
                Assert.True(await resa.MoveNextAsync());
                Assert.AreEqual(ress[2], resa.Current);
                Assert.True(await resa.MoveNextAsync());
                Assert.AreEqual(ress[3], resa.Current);
                Assert.True(await resa.MoveNextAsync());
                Assert.AreEqual(ress[4], resa.Current);
                Assert.True(await resa.MoveNextAsync());
                Assert.AreEqual(ress[5], resa.Current);
                Assert.True(await resa.MoveNextAsync());
                Assert.AreEqual(ress[6], resa.Current);

                Assert.False(await resa.MoveNextAsync());
                await resa.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void OrderThenByDescending(
            [Values] bool configured,
            [Values] bool asyncThenBy,
            [Values] bool captureKeyThenBy)
        {
            Promise.Run(async () =>
            {
                var xs = new[] {
                    new Person() { Name = "Bart", Age = 27 },
                    new Person() { Name = "John", Age = 62 },
                    new Person() { Name = "Eric", Age = 27 },
                    new Person() { Name = "Lisa", Age = 14 },
                    new Person() { Name = "Brad", Age = 27 },
                    new Person() { Name = "Lisa", Age = 23 },
                    new Person() { Name = "Eric", Age = 42 },
                };

                var ys = xs.ToAsyncEnumerable();

                var ress = xs.OrderBy(x => x.Name).ThenByDescending(x => x.Age).ToArray();
                var resa = ys.Order(configured, new PersonFirstNameComparer()).ThenByDescending(asyncThenBy, x => x.Age, captureKeyThenBy).GetAsyncEnumerator();

                Assert.True(await resa.MoveNextAsync());
                Assert.AreEqual(ress[0], resa.Current);
                Assert.True(await resa.MoveNextAsync());
                Assert.AreEqual(ress[1], resa.Current);
                Assert.True(await resa.MoveNextAsync());
                Assert.AreEqual(ress[2], resa.Current);
                Assert.True(await resa.MoveNextAsync());
                Assert.AreEqual(ress[3], resa.Current);
                Assert.True(await resa.MoveNextAsync());
                Assert.AreEqual(ress[4], resa.Current);
                Assert.True(await resa.MoveNextAsync());
                Assert.AreEqual(ress[5], resa.Current);
                Assert.True(await resa.MoveNextAsync());
                Assert.AreEqual(ress[6], resa.Current);

                Assert.False(await resa.MoveNextAsync());
                await resa.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void OrderDescendingThenBy(
            [Values] bool configured,
            [Values] bool asyncThenBy,
            [Values] bool captureKeyThenBy)
        {
            Promise.Run(async () =>
            {
                var xs = new[] {
                    new Person() { Name = "Bart", Age = 27 },
                    new Person() { Name = "John", Age = 62 },
                    new Person() { Name = "Eric", Age = 27 },
                    new Person() { Name = "Lisa", Age = 14 },
                    new Person() { Name = "Brad", Age = 27 },
                    new Person() { Name = "Lisa", Age = 23 },
                    new Person() { Name = "Eric", Age = 42 },
                };

                var ys = xs.ToAsyncEnumerable();

                var ress = xs.OrderByDescending(x => x.Name).ThenBy(x => x.Age).ToArray();
                var resa = ys.OrderDescending(configured, new PersonFirstNameComparer()).ThenBy(asyncThenBy, x => x.Age, captureKeyThenBy).GetAsyncEnumerator();

                Assert.True(await resa.MoveNextAsync());
                Assert.AreEqual(ress[0], resa.Current);
                Assert.True(await resa.MoveNextAsync());
                Assert.AreEqual(ress[1], resa.Current);
                Assert.True(await resa.MoveNextAsync());
                Assert.AreEqual(ress[2], resa.Current);
                Assert.True(await resa.MoveNextAsync());
                Assert.AreEqual(ress[3], resa.Current);
                Assert.True(await resa.MoveNextAsync());
                Assert.AreEqual(ress[4], resa.Current);
                Assert.True(await resa.MoveNextAsync());
                Assert.AreEqual(ress[5], resa.Current);
                Assert.True(await resa.MoveNextAsync());
                Assert.AreEqual(ress[6], resa.Current);

                Assert.False(await resa.MoveNextAsync());
                await resa.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void OrderDescendingThenByDescending(
            [Values] bool configured,
            [Values] bool asyncThenBy,
            [Values] bool captureKeyThenBy)
        {
            Promise.Run(async () =>
            {
                var xs = new[] {
                    new Person() { Name = "Bart", Age = 27 },
                    new Person() { Name = "John", Age = 62 },
                    new Person() { Name = "Eric", Age = 27 },
                    new Person() { Name = "Lisa", Age = 14 },
                    new Person() { Name = "Brad", Age = 27 },
                    new Person() { Name = "Lisa", Age = 23 },
                    new Person() { Name = "Eric", Age = 42 },
                };

                var ys = xs.ToAsyncEnumerable();

                var ress = xs.OrderByDescending(x => x.Name).ThenByDescending(x => x.Age).ToArray();
                var resa = ys.OrderDescending(configured, new PersonFirstNameComparer()).ThenByDescending(asyncThenBy, x => x.Age, captureKeyThenBy).GetAsyncEnumerator();

                Assert.True(await resa.MoveNextAsync());
                Assert.AreEqual(ress[0], resa.Current);
                Assert.True(await resa.MoveNextAsync());
                Assert.AreEqual(ress[1], resa.Current);
                Assert.True(await resa.MoveNextAsync());
                Assert.AreEqual(ress[2], resa.Current);
                Assert.True(await resa.MoveNextAsync());
                Assert.AreEqual(ress[3], resa.Current);
                Assert.True(await resa.MoveNextAsync());
                Assert.AreEqual(ress[4], resa.Current);
                Assert.True(await resa.MoveNextAsync());
                Assert.AreEqual(ress[5], resa.Current);
                Assert.True(await resa.MoveNextAsync());
                Assert.AreEqual(ress[6], resa.Current);

                Assert.False(await resa.MoveNextAsync());
                await resa.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }
    }
}

#endif