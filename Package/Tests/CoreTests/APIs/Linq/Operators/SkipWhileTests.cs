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
    public static class SkipWhileHelper
    {
        // We test all the different overloads.
        public static AsyncEnumerable<TSource> SkipWhile<TSource>(this AsyncEnumerable<TSource> source,
            bool configured,
            bool async,
            bool captureValue,
            Func<TSource, bool> predicate,
            CancelationToken configuredCancelationToken = default)
        {
            if (configured)
            {
                return SkipWhile(source.ConfigureAwait(SynchronizationOption.Foreground).WithCancelation(configuredCancelationToken), async, captureValue, predicate);
            }

            const string capturedValue = "capturedValue";

            if (!captureValue)
            {
                return async
                    ? source.SkipWhile(async (x, _) => predicate(x))
                    : source.SkipWhile(predicate);
            }
            else
            {
                return async
                    ? source.SkipWhile(capturedValue, async (cv, x, _) =>
                    {
                        Assert.AreEqual(capturedValue, cv);
                        return predicate(x);
                    })
                    : source.SkipWhile(capturedValue, (cv, x) =>
                    {
                        Assert.AreEqual(capturedValue, cv);
                        return predicate(x);
                    });
            }
        }

        public static AsyncEnumerable<TSource> SkipWhile<TSource>(this in ConfiguredAsyncEnumerable<TSource> source,
            bool async,
            bool captureValue,
            Func<TSource, bool> predicate)
        {
            const string capturedValue = "capturedValue";

            if (!captureValue)
            {
                return async
                    ? source.SkipWhile(async (x, _) => predicate(x))
                    : source.SkipWhile(predicate);
            }
            else
            {
                return async
                    ? source.SkipWhile(capturedValue, async (cv, x, _) =>
                    {
                        Assert.AreEqual(capturedValue, cv);
                        return predicate(x);
                    })
                    : source.SkipWhile(capturedValue, (cv, x) =>
                    {
                        Assert.AreEqual(capturedValue, cv);
                        return predicate(x);
                    });
            }
        }
    }

    public class SkipWhileTests
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
        public void SkipWhile_NullArgumentThrows()
        {
            var enumerable = AsyncEnumerable.Return(42);
            var captureValue = "captureValue";

            Assert.Catch<System.ArgumentNullException>(() => enumerable.SkipWhile(default(Func<int, bool>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.SkipWhile(default(Func<int, CancelationToken, Promise<bool>>)));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.SkipWhile(captureValue, default(Func<string, int, bool>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.SkipWhile(captureValue, default(Func<string, int, CancelationToken, Promise<bool>>)));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).SkipWhile(default(Func<int, bool>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).SkipWhile(default(Func<int, CancelationToken, Promise<bool>>)));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).SkipWhile(captureValue, default(Func<string, int, bool>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).SkipWhile(captureValue, default(Func<string, int, CancelationToken, Promise<bool>>)));

            enumerable.GetAsyncEnumerator().DisposeAsync().Forget();
        }
#endif //PROMISE_DEBUG

        [Test]
        public void SkipWhile_Empty(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureValue)
        {
            Promise.Run(async () =>
            {
                var asyncEnumerator = AsyncEnumerable.Empty<int>()
                    .SkipWhile(configured, async, captureValue, x => false)
                    .GetAsyncEnumerator();
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void SkipWhile_Simple(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureValue)
        {
            Promise.Run(async () =>
            {
                var asyncEnumerator = new[] { 1, 2, 3, 4 }.ToAsyncEnumerable()
                    .SkipWhile(configured, async, captureValue, x => x < 3)
                    .GetAsyncEnumerator();
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(3, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(4, asyncEnumerator.Current);
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void SkipWhile_False(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureValue)
        {
            Promise.Run(async () =>
            {
                var asyncEnumerator = new[] { 1, 2, 3, 4 }.ToAsyncEnumerable()
                    .SkipWhile(configured, async, captureValue, x => false)
                    .GetAsyncEnumerator();
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(1, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(2, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(3, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(4, asyncEnumerator.Current);
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void SkipWhile_True(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureValue)
        {
            Promise.Run(async () =>
            {
                var asyncEnumerator = new[] { 1, 2, 3, 4 }.ToAsyncEnumerable()
                    .SkipWhile(configured, async, captureValue, x => true)
                    .GetAsyncEnumerator();
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void SkipWhile_Throws_Source(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureValue)
        {
            Promise.Run(async () =>
            {
                var ex = new Exception("Bang!");
                var asyncEnumerator = AsyncEnumerable<int>.Rejected(ex)
                    .SkipWhile(configured, async, captureValue, x => false)
                    .GetAsyncEnumerator();
                await TestHelper.AssertThrowsAsync(() => asyncEnumerator.MoveNextAsync(), ex);
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void SkipWhile_Throws_Predicate(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureValue)
        {
            Promise.Run(async () =>
            {
                var ex = new Exception("Bang!");
                var asyncEnumerator = new[] { 1, 2, 3, 4 }.ToAsyncEnumerable()
                    .SkipWhile(configured, async, captureValue, x => { throw ex; })
                    .GetAsyncEnumerator();
                await TestHelper.AssertThrowsAsync(() => asyncEnumerator.MoveNextAsync(), ex);
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
        public void SkipWhile_Cancel(
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
                            .SkipWhile(configuredType != ConfiguredType.NotConfigured, async, captureValue, x => false,
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