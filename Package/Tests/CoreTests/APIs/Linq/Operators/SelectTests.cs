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
    public static class SelectHelper
    {
        // We test all the different overloads.
        public static AsyncEnumerable<TResult> Select<TSource, TResult>(this AsyncEnumerable<TSource> source,
            bool configured,
            bool async,
            Func<TSource, TResult> selector, bool captureValue)
        {
            if (configured)
            {
                return Select(source.ConfigureAwait(SynchronizationOption.Foreground), async, selector, captureValue);
            }

            const string capturedValue = "capturedValue";

            if (!captureValue)
            {
                return async
                    ? source.Select(async x => selector(x))
                    : source.Select(selector);
            }
            else
            {
                return async
                    ? source.Select(capturedValue, async (cv, x) =>
                    {
                        Assert.AreEqual(capturedValue, cv);
                        return selector(x);
                    })
                    : source.Select(capturedValue, (cv, x) =>
                    {
                        Assert.AreEqual(capturedValue, cv);
                        return selector(x);
                    });
            }
        }

        public static AsyncEnumerable<TResult> Select<TSource, TResult>(this in ConfiguredAsyncEnumerable<TSource> source,
            bool async,
            Func<TSource, TResult> selector, bool captureValue)
        {
            const string capturedValue = "capturedValue";

            if (!captureValue)
            {
                return async
                    ? source.Select(async x => selector(x))
                    : source.Select(selector);
            }
            else
            {
                return async
                    ? source.Select(capturedValue, async (cv, x) =>
                    {
                        Assert.AreEqual(capturedValue, cv);
                        return selector(x);
                    })
                    : source.Select(capturedValue, (cv, x) =>
                    {
                        Assert.AreEqual(capturedValue, cv);
                        return selector(x);
                    });
            }
        }

        // We test all the different overloads.
        public static AsyncEnumerable<TResult> Select<TSource, TResult>(this AsyncEnumerable<TSource> source,
            bool configured,
            bool async,
            Func<TSource, int, TResult> selector, bool captureValue)
        {
            if (configured)
            {
                return Select(source.ConfigureAwait(SynchronizationOption.Foreground), async, selector, captureValue);
            }

            const string capturedValue = "capturedValue";

            if (!captureValue)
            {
                return async
                    ? source.Select(async (x, i) => selector(x, i))
                    : source.Select(selector);
            }
            else
            {
                return async
                    ? source.Select(capturedValue, async (cv, x, i) =>
                    {
                        Assert.AreEqual(capturedValue, cv);
                        return selector(x, i);
                    })
                    : source.Select(capturedValue, (cv, x, i) =>
                    {
                        Assert.AreEqual(capturedValue, cv);
                        return selector(x, i);
                    });
            }
        }

        public static AsyncEnumerable<TResult> Select<TSource, TResult>(this in ConfiguredAsyncEnumerable<TSource> source,
            bool async,
            Func<TSource, int, TResult> selector, bool captureValue)
        {
            const string capturedValue = "capturedValue";

            if (!captureValue)
            {
                return async
                    ? source.Select(async (x, i) => selector(x, i))
                    : source.Select(selector);
            }
            else
            {
                return async
                    ? source.Select(capturedValue, async (cv, x, i) =>
                    {
                        Assert.AreEqual(capturedValue, cv);
                        return selector(x, i);
                    })
                    : source.Select(capturedValue, (cv, x, i) =>
                    {
                        Assert.AreEqual(capturedValue, cv);
                        return selector(x, i);
                    });
            }
        }
    }

    public class SelectTests
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
        public void Select_NullArgumentThrows()
        {
            var enumerable = AsyncEnumerable.Return(42);
            var captureValue = "captureValue";

            Assert.Catch<System.ArgumentNullException>(() => enumerable.Select(default(Func<int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.Select(default(Func<int, Promise<int>>)));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.Select(captureValue, default(Func<string, int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.Select(captureValue, default(Func<string, int, Promise<int>>)));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).Select(default(Func<int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).Select(default(Func<int, Promise<int>>)));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).Select(captureValue, default(Func<string, int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).Select(captureValue, default(Func<string, int, Promise<int>>)));

            enumerable.GetAsyncEnumerator().DisposeAsync().Forget();
        }
#endif //PROMISE_DEBUG

        [Test]
        public void Select_Empty(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureValue)
        {
            Promise.Run(async () =>
            {
                var asyncEnumerator = AsyncEnumerable.Empty<int>()
                    .Select(configured, async, x => x, captureValue)
                    .GetAsyncEnumerator();
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Select_Simple(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureValue)
        {
            Promise.Run(async () =>
            {
                var asyncEnumerator = new[] { 0, 1, 2 }.ToAsyncEnumerable()
                    .Select(configured, async, x => (char) ('a' + x), captureValue)
                    .GetAsyncEnumerator();
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual('a', asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual('b', asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual('c', asyncEnumerator.Current);
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Select_Indexed(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureValue)
        {
            Promise.Run(async () =>
            {
                var asyncEnumerator = new[] { 8, 5, 7 }.ToAsyncEnumerable()
                    .Select(configured, async, (x, i) => (char) ('a' + i), captureValue)
                    .GetAsyncEnumerator();
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual('a', asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual('b', asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual('c', asyncEnumerator.Current);
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Select_Throws_Selector(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureValue)
        {
            Promise.Run(async () =>
            {
                var asyncEnumerator = new[] { 0, 1, 2 }.ToAsyncEnumerable()
                    // IL2CPP crashes on integer divide by zero, so throw the exception manually instead.
                    .Select(configured, async, x => { if (x == 0) throw new DivideByZeroException(); return 1 / x; }, captureValue)
                    .GetAsyncEnumerator();
                await TestHelper.AssertThrowsAsync<DivideByZeroException>(() => asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Select_Indexed_Throws_Selector(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureValue)
        {
            Promise.Run(async () =>
            {
                var asyncEnumerator = new[] { 8, 5, 7 }.ToAsyncEnumerable()
                    // IL2CPP crashes on integer divide by zero, so throw the exception manually instead.
                    .Select(configured, async, (x, i) => { if (i == 0) throw new DivideByZeroException(); return 1 / i; }, captureValue)
                    .GetAsyncEnumerator();
                await TestHelper.AssertThrowsAsync<DivideByZeroException>(() => asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Select_Select(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureValue)
        {
            Promise.Run(async () =>
            {
                var asyncEnumerator = new[] { 0, 1, 2 }.ToAsyncEnumerable()
                    .Select(configured, async, i => i + 3, captureValue)
                    .Select(configured, async, x => (char) ('a' + x), captureValue)
                    .GetAsyncEnumerator();
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual('d', asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual('e', asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual('f', asyncEnumerator.Current);
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        // TODO: add these tests when Count and ToList and ToArray extensions are added.
        //[Test]
        //public void Select_IList_Count()
        //{
        //    var xs = ToAsyncEnumerableIList(new[] { 1, 2, 3, 4, 5 });
        //    var ys = xs.Select(x => x * 2);

        //    Assert.Equal(5, await ys.CountAsync());
        //}

        //[Test]
        //public void Select_IList_ToList()
        //{
        //    var xs = ToAsyncEnumerableIList(new[] { 1, 2, 3, 4, 5 });
        //    var ys = xs.Select(x => x * 2);

        //    Assert.Equal(new[] { 2, 4, 6, 8, 10 }, await ys.ToListAsync());
        //}

        //[Test]
        //public void Select_IList_ToArray()
        //{
        //    var xs = ToAsyncEnumerableIList(new[] { 1, 2, 3, 4, 5 });
        //    var ys = xs.Select(x => x * 2);

        //    Assert.Equal(new[] { 2, 4, 6, 8, 10 }, await ys.ToArrayAsync());
        //}
    }
}

#endif