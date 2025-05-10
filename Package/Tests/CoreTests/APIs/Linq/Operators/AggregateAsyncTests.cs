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
using System.Runtime.CompilerServices;
using System.Threading;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace ProtoPromise.Tests.APIs.Linq
{
    public static class AggregateHelper
    {
        // We test all the different overloads.
        public static Promise<TSource> AggregateAsync<TSource>(this AsyncEnumerable<TSource> source,
            bool configured,
            bool async,
            bool captureValue,
            Func<TSource, TSource, TSource> accumulator,
            CancelationToken cancelationToken = default)
        {
            if (configured)
            {
                return AggregateAsync(source.ConfigureAwait(SynchronizationOption.Foreground).WithCancelation(cancelationToken), async, captureValue, accumulator);
            }

            const string valueCapture = "valueCapture";

            if (!captureValue)
            {
                return async
                    ? source.AggregateAsync(async (acc, x) => accumulator(acc, x), cancelationToken)
                    : source.AggregateAsync(accumulator, cancelationToken);
            }
            else
            {
                return async
                    ? source.AggregateAsync(valueCapture, async (cv, acc, x) =>
                    {
                        Assert.AreEqual(valueCapture, cv);
                        return accumulator(acc, x);
                    }, cancelationToken)
                    : source.AggregateAsync(valueCapture, (cv, acc, x) =>
                    {
                        Assert.AreEqual(valueCapture, cv);
                        return accumulator(acc, x);
                    }, cancelationToken);
            }
        }

        public static Promise<TSource> AggregateAsync<TSource>(this in ConfiguredAsyncEnumerable<TSource> source,
            bool async,
            bool captureValue,
            Func<TSource, TSource, TSource> accumulator)
        {
            const string valueCapture = "valueCapture";

            if (!captureValue)
            {
                return async
                    ? source.AggregateAsync(async (acc, x) => accumulator(acc, x))
                    : source.AggregateAsync(accumulator);
            }
            else
            {
                return async
                    ? source.AggregateAsync(valueCapture, async (cv, acc, x) =>
                    {
                        Assert.AreEqual(valueCapture, cv);
                        return accumulator(acc, x);
                    })
                    : source.AggregateAsync(valueCapture, (cv, acc, x) =>
                    {
                        Assert.AreEqual(valueCapture, cv);
                        return accumulator(acc, x);
                    });
            }
        }

        public static Promise<TAccumulate> AggregateAsync<TSource, TAccumulate>(this AsyncEnumerable<TSource> source,
            bool configured,
            bool async,
            TAccumulate seed,
            bool captureValue,
            Func<TAccumulate, TSource, TAccumulate> accumulator,
            CancelationToken cancelationToken = default)
        {
            if (configured)
            {
                return AggregateAsync(source.ConfigureAwait(SynchronizationOption.Foreground).WithCancelation(cancelationToken), async, seed, captureValue, accumulator);
            }

            const string valueCapture = "valueCapture";

            if (!captureValue)
            {
                return async
                    ? source.AggregateAsync(seed, async (acc, x) => accumulator(acc, x), cancelationToken)
                    : source.AggregateAsync(seed, accumulator, cancelationToken);
            }
            else
            {
                return async
                    ? source.AggregateAsync(valueCapture, seed, async (cv, acc, x) =>
                    {
                        Assert.AreEqual(valueCapture, cv);
                        return accumulator(acc, x);
                    }, cancelationToken)
                    : source.AggregateAsync(valueCapture, seed, (cv, acc, x) =>
                    {
                        Assert.AreEqual(valueCapture, cv);
                        return accumulator(acc, x);
                    }, cancelationToken);
            }
        }

        public static Promise<TAccumulate> AggregateAsync<TSource, TAccumulate>(this in ConfiguredAsyncEnumerable<TSource> source,
            bool async,
            TAccumulate seed,
            bool captureValue,
            Func<TAccumulate, TSource, TAccumulate> accumulator)
        {
            const string valueCapture = "valueCapture";

            if (!captureValue)
            {
                return async
                    ? source.AggregateAsync(seed, async (acc, x) => accumulator(acc, x))
                    : source.AggregateAsync(seed, accumulator);
            }
            else
            {
                return async
                    ? source.AggregateAsync(valueCapture, seed, async (cv, acc, x) =>
                    {
                        Assert.AreEqual(valueCapture, cv);
                        return accumulator(acc, x);
                    })
                    : source.AggregateAsync(valueCapture, seed, (cv, acc, x) =>
                    {
                        Assert.AreEqual(valueCapture, cv);
                        return accumulator(acc, x);
                    });
            }
        }
    }

    public class AggregateAsyncTests
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
        public void AggregateAsync_Null()
        {
            var enumerable = AsyncEnumerable.Range(0, 10);

            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateAsync(default(Func<int, int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateAsync("captured", default(Func<string, int, int, int>)));
                                                             
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Foreground).AggregateAsync(default(Func<int, int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Foreground).AggregateAsync("captured", default(Func<string, int, int, int>)));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateAsync(default(Func<int, int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateAsync("captured", default(Func<string, int, int, Promise<int>>)));
                                                             
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Foreground).AggregateAsync(default(Func<int, int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Foreground).AggregateAsync("captured", default(Func<string, int, int, Promise<int>>)));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateAsync(42, default(Func<int, int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateAsync("captured", 42, default(Func<string, int, int, int>)));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Foreground).AggregateAsync(42, default(Func<int, int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Foreground).AggregateAsync("captured", 42, default(Func<string, int, int, int>)));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateAsync(42, default(Func<int, int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateAsync("captured", 42, default(Func<string, int, int, Promise<int>>)));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Foreground).AggregateAsync(42, default(Func<int, int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Foreground).AggregateAsync("captured", 42, default(Func<string, int, int, Promise<int>>)));

            enumerable.GetAsyncEnumerator().DisposeAsync().Forget();
        }
#endif

        [Test]
        public void AggregateAsync_Simple(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureValue)
        {
            Promise.Run(async () =>
            {
                var xs = new[] { 1, 2, 3, 4 }.ToAsyncEnumerable();
                var ys = xs.AggregateAsync(configured, async, captureValue, (x, y) => x * y);
                Assert.AreEqual(24, await ys);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AggregateAsync_Empty(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureValue)
        {
            Promise.Run(async () =>
            {
                var xs = new int[0].ToAsyncEnumerable();
                var ys = xs.AggregateAsync(configured, async, captureValue, (x, y) => x * y);
                await TestHelper.AssertThrowsAsync<System.InvalidOperationException>(() => ys);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AggregateAsync_Throw_Source(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureValue)
        {
            Promise.Run(async () =>
            {
                var ex = new Exception("Bang!");
                var xs = AsyncEnumerable<int>.Rejected(ex);
                var ys = xs.AggregateAsync(configured, async, captureValue, (x, y) => x * y);
                await TestHelper.AssertThrowsAsync(() => ys, ex);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AggregateAsync_Throw_Selector(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureValue)
        {
            Promise.Run(async () =>
            {
                var ex = new Exception("Bang!");
                var xs = new[] { 1, 2, 3, 4 }.ToAsyncEnumerable();
                var ys = xs.AggregateAsync(configured, async, captureValue, (x, y) => { throw ex; });
                await TestHelper.AssertThrowsAsync(() => ys, ex);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AggregateAsync_Seed_Simple(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureValue)
        {
            Promise.Run(async () =>
            {
                var xs = new[] { 1, 2, 3, 4 }.ToAsyncEnumerable();
                var ys = xs.AggregateAsync(configured, async, 1, captureValue, (x, y) => x * y);
                Assert.AreEqual(24, await ys);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AggregateAsync_Seed_Empty(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureValue)
        {
            Promise.Run(async () =>
            {
                var xs = new int[0].ToAsyncEnumerable();
                var ys = xs.AggregateAsync(configured, async, 1, captureValue, (x, y) => x * y);
                Assert.AreEqual(1, await ys);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AggregateAsync_Seed_Throw_Source(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureValue)
        {
            Promise.Run(async () =>
            {
                var ex = new Exception("Bang!");
                var xs = AsyncEnumerable<int>.Rejected(ex);
                var ys = xs.AggregateAsync(configured, async, 1, captureValue, (x, y) => x * y);
                await TestHelper.AssertThrowsAsync(() => ys, ex);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AggregateAsync_Seed_Throw_Selector(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureValue)
        {
            Promise.Run(async () =>
            {
                var ex = new Exception("Bang!");
                var xs = new[] { 1, 2, 3, 4 }.ToAsyncEnumerable();
                var ys = xs.AggregateAsync(configured, async, 1, captureValue, (x, y) => { throw ex; });
                await TestHelper.AssertThrowsAsync(() => ys, ex);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AggregateAsync_Cancel(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureValue)
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
                using (var cancelationSource = CancelationSource.New())
                {
                    var ys = xs.AggregateAsync(configured, async, 1, captureValue, (acc, x) =>
                    {
                        if (x == 2)
                        {
                            cancelationSource.Cancel();
                        }
                        return acc * x;
                    }, cancelationSource.Token);
                    await TestHelper.AssertCanceledAsync(() => ys);
                }
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }
    }
}