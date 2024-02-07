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
    public static class WhereHelper
    {
        // We test all the different overloads.
        public static AsyncEnumerable<TSource> Where<TSource>(this AsyncEnumerable<TSource> source,
            bool configured,
            bool async,
            bool captureValue,
            Func<TSource, bool> predicate,
            CancelationToken configuredCancelationToken = default)
        {
            if (configured)
            {
                return Where(source.ConfigureAwait(SynchronizationOption.Foreground).WithCancelation(configuredCancelationToken), async, captureValue, predicate);
            }

            const string capturedValue = "capturedValue";

            if (!captureValue)
            {
                return async
                    ? source.Where(async x => predicate(x))
                    : source.Where(predicate);
            }
            else
            {
                return async
                    ? source.Where(capturedValue, async (cv, x) =>
                    {
                        Assert.AreEqual(capturedValue, cv);
                        return predicate(x);
                    })
                    : source.Where(capturedValue, (cv, x) =>
                    {
                        Assert.AreEqual(capturedValue, cv);
                        return predicate(x);
                    });
            }
        }

        public static AsyncEnumerable<TSource> Where<TSource>(this in ConfiguredAsyncEnumerable<TSource> source,
            bool async,
            bool captureValue,
            Func<TSource, bool> predicate)
        {
            const string capturedValue = "capturedValue";

            if (!captureValue)
            {
                return async
                    ? source.Where(async x => predicate(x))
                    : source.Where(predicate);
            }
            else
            {
                return async
                    ? source.Where(capturedValue, async (cv, x) =>
                    {
                        Assert.AreEqual(capturedValue, cv);
                        return predicate(x);
                    })
                    : source.Where(capturedValue, (cv, x) =>
                    {
                        Assert.AreEqual(capturedValue, cv);
                        return predicate(x);
                    });
            }
        }

        // We test all the different overloads.
        public static AsyncEnumerable<TSource> Where<TSource>(this AsyncEnumerable<TSource> source,
            bool configured,
            bool async,
            bool captureValue,
            Func<TSource, int, bool> predicate,
            CancelationToken configuredCancelationToken = default)
        {
            if (configured)
            {
                return Where(source.ConfigureAwait(SynchronizationOption.Foreground).WithCancelation(configuredCancelationToken), async, captureValue, predicate);
            }

            const string capturedValue = "capturedValue";

            if (!captureValue)
            {
                return async
                    ? source.Where(async (x, i) => predicate(x, i))
                    : source.Where(predicate);
            }
            else
            {
                return async
                    ? source.Where(capturedValue, async (cv, x, i) =>
                    {
                        Assert.AreEqual(capturedValue, cv);
                        return predicate(x, i);
                    })
                    : source.Where(capturedValue, (cv, x, i) =>
                    {
                        Assert.AreEqual(capturedValue, cv);
                        return predicate(x, i);
                    });
            }
        }

        public static AsyncEnumerable<TSource> Where<TSource>(this in ConfiguredAsyncEnumerable<TSource> source,
            bool async,
            bool captureValue,
            Func<TSource, int, bool> predicate)
        {
            const string capturedValue = "capturedValue";

            if (!captureValue)
            {
                return async
                    ? source.Where(async (x, i) => predicate(x, i))
                    : source.Where(predicate);
            }
            else
            {
                return async
                    ? source.Where(capturedValue, async (cv, x, i) =>
                    {
                        Assert.AreEqual(capturedValue, cv);
                        return predicate(x, i);
                    })
                    : source.Where(capturedValue, (cv, x, i) =>
                    {
                        Assert.AreEqual(capturedValue, cv);
                        return predicate(x, i);
                    });
            }
        }
    }

    public class WhereTests
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
        public void Where_NullArgumentThrows()
        {
            var enumerable = AsyncEnumerable.Return(42);
            var captureValue = "captureValue";

            Assert.Catch<System.ArgumentNullException>(() => enumerable.Where(default(Func<int, bool>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.Where(default(Func<int, Promise<bool>>)));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.Where(captureValue, default(Func<string, int, bool>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.Where(captureValue, default(Func<string, int, Promise<bool>>)));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).Where(default(Func<int, bool>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).Where(default(Func<int, Promise<bool>>)));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).Where(captureValue, default(Func<string, int, bool>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).Where(captureValue, default(Func<string, int, Promise<bool>>)));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.Where(default(Func<int, int, bool>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.Where(default(Func<int, int, Promise<bool>>)));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.Where(captureValue, default(Func<string, int, int, bool>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.Where(captureValue, default(Func<string, int, int, Promise<bool>>)));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).Where(default(Func<int, int, bool>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).Where(default(Func<int, int, Promise<bool>>)));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).Where(captureValue, default(Func<string, int, int, bool>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).Where(captureValue, default(Func<string, int, int, Promise<bool>>)));

            enumerable.GetAsyncEnumerator().DisposeAsync().Forget();
        }
#endif //PROMISE_DEBUG

        [Test]
        public void Where_Empty(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureValue)
        {
            Promise.Run(async () =>
            {
                var asyncEnumerator = AsyncEnumerable.Empty<int>()
                    .Where(configured, async, captureValue, x => true)
                    .GetAsyncEnumerator();
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Where_Simple(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureValue)
        {
            Promise.Run(async () =>
            {
                var asyncEnumerator = new[] { 0, 1, 2 }.ToAsyncEnumerable()
                    .Where(configured, async, captureValue, x => x != 1)
                    .GetAsyncEnumerator();
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(0, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(2, asyncEnumerator.Current);
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Where_Indexed(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureValue)
        {
            Promise.Run(async () =>
            {
                var asyncEnumerator = new[] { 8, 5, 7 }.ToAsyncEnumerable()
                    .Where(configured, async, captureValue, (x, i) => i != 1)
                    .GetAsyncEnumerator();
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(8, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(7, asyncEnumerator.Current);
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Where_Throws(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureValue)
        {
            Promise.Run(async () =>
            {
                var asyncEnumerator = new[] { 0, 1, 2 }.ToAsyncEnumerable()
                    .Where(configured, async, captureValue, x => { if (x == 0) throw new DivideByZeroException(); return true; })
                    .GetAsyncEnumerator();
                await TestHelper.AssertThrowsAsync<DivideByZeroException>(() => asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Where_Indexed_Throws(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureValue)
        {
            Promise.Run(async () =>
            {
                var asyncEnumerator = new[] { 8, 5, 7 }.ToAsyncEnumerable()
                    .Where(configured, async, captureValue, (x, i) => { if (i == 0) throw new DivideByZeroException(); return true; })
                    .GetAsyncEnumerator();
                await TestHelper.AssertThrowsAsync<DivideByZeroException>(() => asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Where_Where(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureValue)
        {
            Promise.Run(async () =>
            {
                var asyncEnumerator = new[] { 0, 1, 2, 3, 4, 5 }.ToAsyncEnumerable()
                    .Where(configured, async, captureValue, x => x % 2 == 0)
                    .Where(configured, async, captureValue, x => x != 2)
                    .GetAsyncEnumerator();
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(0, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(4, asyncEnumerator.Current);
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        public enum ConfiguredType
        {
            NotConfigured,
            Configured,
            ConfiguredWithCancelation
        }

        [Test]
        public void Where_Cancel(
            [Values] ConfiguredType configuredType,
            [Values] bool async,
            [Values] bool captureValue,
            [Values] bool enumeratorToken)
        {
            Promise.Run(async () =>
            {
                var xs = AsyncEnumerable.Create<int>(async (writer, cancelationToken) =>
                {
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(0);
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(1);
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(2);
                });
                using (var configuredCancelationSource = CancelationSource.New())
                {
                    using (var enumeratorCancelationSource = CancelationSource.New())
                    {
                        var asyncEnumerator = xs
                            .Where(configuredType != ConfiguredType.NotConfigured, captureValue, async, x => true,
                                configuredType == ConfiguredType.ConfiguredWithCancelation ? configuredCancelationSource.Token : CancelationToken.None)
                            .GetAsyncEnumerator(enumeratorCancelationSource.Token);
                        Assert.True(await asyncEnumerator.MoveNextAsync());
                        Assert.AreEqual(0, asyncEnumerator.Current);
                        Assert.True(await asyncEnumerator.MoveNextAsync());
                        Assert.AreEqual(1, asyncEnumerator.Current);
                        configuredCancelationSource.Cancel();
                        enumeratorCancelationSource.Cancel();
                        if (configuredType == ConfiguredType.ConfiguredWithCancelation || enumeratorToken)
                        {
                            await TestHelper.AssertCanceledAsync(() => asyncEnumerator.MoveNextAsync());
                        }
                        await asyncEnumerator.DisposeAsync();
                    }
                }
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }
    }
}

#endif