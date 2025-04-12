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
    public static class SelectManyNoResultSelectorHelper
    {
        // We test all the different overloads.
        public static AsyncEnumerable<TResult> SelectMany<TSource, TResult>(this AsyncEnumerable<TSource> source,
            bool configured,
            bool async,
            bool captureValue,
            Func<TSource, AsyncEnumerable<TResult>> selector,
            CancelationToken configuredCancelationToken = default)
        {
            if (configured)
            {
                return SelectMany(source.ConfigureAwait(SynchronizationOption.Foreground).WithCancelation(configuredCancelationToken), async, captureValue, selector);
            }

            const string capturedValue = "capturedValue";

            if (!captureValue)
            {
                return async
                    ? source.SelectMany(async (x, _) => selector(x))
                    : source.SelectMany(selector);
            }
            else
            {
                return async
                    ? source.SelectMany(capturedValue, async (cv, x, _) =>
                    {
                        Assert.AreEqual(capturedValue, cv);
                        return selector(x);
                    })
                    : source.SelectMany(capturedValue, (cv, x) =>
                    {
                        Assert.AreEqual(capturedValue, cv);
                        return selector(x);
                    });
            }
        }

        public static AsyncEnumerable<TResult> SelectMany<TSource, TResult>(this in ConfiguredAsyncEnumerable<TSource> source,
            bool async,
            bool captureValue,
            Func<TSource, AsyncEnumerable<TResult>> selector)
        {
            const string capturedValue = "capturedValue";

            if (!captureValue)
            {
                return async
                    ? source.SelectMany(async (x, _) => selector(x))
                    : source.SelectMany(selector);
            }
            else
            {
                return async
                    ? source.SelectMany(capturedValue, async (cv, x, _) =>
                    {
                        Assert.AreEqual(capturedValue, cv);
                        return selector(x);
                    })
                    : source.SelectMany(capturedValue, (cv, x) =>
                    {
                        Assert.AreEqual(capturedValue, cv);
                        return selector(x);
                    });
            }
        }
    }

    public class SelectManyNoResultSelectorTests
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
        public void SelectMany_NullArgumentThrows()
        {
            var enumerable = AsyncEnumerable.Return(42);
            var captureValue = "captureValue";

            Assert.Catch<System.ArgumentNullException>(() => enumerable.SelectMany(default(Func<int, AsyncEnumerable<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.SelectMany(default(Func<int, CancelationToken, Promise<AsyncEnumerable<int>>>)));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.SelectMany(captureValue, default(Func<string, int, AsyncEnumerable<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.SelectMany(captureValue, default(Func<string, int, CancelationToken, Promise<AsyncEnumerable<int>>>)));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).SelectMany(default(Func<int, AsyncEnumerable<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).SelectMany(default(Func<int, CancelationToken, Promise<AsyncEnumerable<int>>>)));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).SelectMany(captureValue, default(Func<string, int, AsyncEnumerable<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).SelectMany(captureValue, default(Func<string, int, CancelationToken, Promise<AsyncEnumerable<int>>>)));

            enumerable.GetAsyncEnumerator().DisposeAsync().Forget();
        }
#endif //PROMISE_DEBUG

        [Test]
        public void SelectMany_Empty(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureValue)
        {
            Promise.Run(async () =>
            {
                var asyncEnumerator = AsyncEnumerable.Empty<int>()
                    .SelectMany(configured, async, captureValue, x => AsyncEnumerable.Range(0, x))
                    .GetAsyncEnumerator();
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void SelectMany_Simple(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureValue)
        {
            Promise.Run(async () =>
            {
                var asyncEnumerator = new[] { 1, 2, 3 }.ToAsyncEnumerable()
                    .SelectMany(configured, async, captureValue, x => AsyncEnumerable.Range(0, x))
                    .GetAsyncEnumerator();
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(0, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(0, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(1, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(0, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(1, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(2, asyncEnumerator.Current);
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void SelectMany_Throws_Selector(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureValue)
        {
            Promise.Run(async () =>
            {
                var asyncEnumerator = new[] { -1, 0, 1 }.ToAsyncEnumerable()
                    .SelectMany(configured, async, captureValue, x => AsyncEnumerable.Range(0, x))
                    .GetAsyncEnumerator();
                await TestHelper.AssertThrowsAsync<System.ArgumentOutOfRangeException>(() => asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void SelectMany_Throws_Source(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureValue)
        {
            Promise.Run(async () =>
            {
                var ex = new Exception("Bang");
                var asyncEnumerator = AsyncEnumerable<int>.Rejected(ex)
                    .SelectMany(configured, async, captureValue, x => AsyncEnumerable.Range(0, x + 1))
                    .GetAsyncEnumerator();
                await TestHelper.AssertThrowsAsync(() => asyncEnumerator.MoveNextAsync(), ex);
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
        public void SelectMany_Cancel(
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
                    await writer.YieldAsync(1);
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(2);
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(3);
                });
                using (var configuredCancelationSource = CancelationSource.New())
                {
                    using (var enumeratorCancelationSource = CancelationSource.New())
                    {
                        var asyncEnumerator = xs
                            .SelectMany(configuredType != ConfiguredType.NotConfigured, async, captureValue, x => AsyncEnumerable.Range(0, x),
                                configuredType == ConfiguredType.ConfiguredWithCancelation ? configuredCancelationSource.Token : CancelationToken.None)
                            .GetAsyncEnumerator(enumeratorCancelationSource.Token);
                        Assert.True(await asyncEnumerator.MoveNextAsync());
                        Assert.AreEqual(0, asyncEnumerator.Current);
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

        [Test]
        public void SelectMany_CancelDeep(
            [Values] ConfiguredType configuredType,
            [Values] bool async,
            [Values] bool captureValue,
            [Values] bool enumeratorToken)
        {
            Promise.Run(async () =>
            {
                var xs = AsyncEnumerable.Create<int>(async (writer, cancelationToken) =>
                {
                    await writer.YieldAsync(1);
                    await writer.YieldAsync(2);
                });
                AsyncEnumerable<int> CreateInner(int count) => AsyncEnumerable<int>.Create(count, async (c, writer, cancelationToken) =>
                {
                    for (int i = 0; i <= c; ++i)
                    {
                        cancelationToken.ThrowIfCancelationRequested();
                        await writer.YieldAsync(i);
                    }
                });

                using (var configuredCancelationSource = CancelationSource.New())
                {
                    using (var enumeratorCancelationSource = CancelationSource.New())
                    {
                        var asyncEnumerator = xs
                            .SelectMany(configuredType != ConfiguredType.NotConfigured, async, captureValue, x => CreateInner(x),
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