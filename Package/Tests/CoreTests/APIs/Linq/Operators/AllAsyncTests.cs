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

namespace ProtoPromiseTests.APIs.Linq
{
    public static class AllHelper
    {
        // We test all the different overloads.
        public static Promise<bool> AllAsync<TSource>(this AsyncEnumerable<TSource> source,
            bool configured,
            bool async,
            bool captureValue,
            Func<TSource, bool> predicate,
            CancelationToken cancelationToken = default)
        {
            if (configured)
            {
                return AllAsync(source.ConfigureAwait(SynchronizationOption.Foreground).WithCancelation(cancelationToken), async, captureValue, predicate);
            }

            const string valueCapture = "valueCapture";

            if (!captureValue)
            {
                return async
                    ? source.AllAsync(async x => predicate(x), cancelationToken)
                    : source.AllAsync(predicate, cancelationToken);
            }
            else
            {
                return async
                    ? source.AllAsync(valueCapture, async (cv, x) =>
                    {
                        Assert.AreEqual(valueCapture, cv);
                        return predicate(x);
                    }, cancelationToken)
                    : source.AllAsync(valueCapture, (cv, x) =>
                    {
                        Assert.AreEqual(valueCapture, cv);
                        return predicate(x);
                    }, cancelationToken);
            }
        }

        public static Promise<bool> AllAsync<TSource>(this in ConfiguredAsyncEnumerable<TSource> source,
            bool async,
            bool captureValue,
            Func<TSource, bool> predicate)
        {
            const string valueCapture = "valueCapture";

            if (!captureValue)
            {
                return async
                    ? source.AllAsync(async x => predicate(x))
                    : source.AllAsync(predicate);
            }
            else
            {
                return async
                    ? source.AllAsync(valueCapture, async (cv, x) =>
                    {
                        Assert.AreEqual(valueCapture, cv);
                        return predicate(x);
                    })
                    : source.AllAsync(valueCapture, (cv, x) =>
                    {
                        Assert.AreEqual(valueCapture, cv);
                        return predicate(x);
                    });
            }
        }
    }

    public class AllAsyncTests
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
        public void AllAsync_Null()
        {
            var enumerable = AsyncEnumerable.Range(0, 10);

            Assert.Catch<System.ArgumentNullException>(() => enumerable.AllAsync(default(Func<int, bool>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AllAsync("captured", default(Func<string, int, bool>)));
                                                             
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Foreground).AllAsync(default(Func<int, bool>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Foreground).AllAsync("captured", default(Func<string, int, bool>)));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.AllAsync(default(Func<int, Promise<bool>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AllAsync("captured", default(Func<string, int, Promise<bool>>)));
                                                             
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Foreground).AllAsync(default(Func<int, Promise<bool>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Foreground).AllAsync("captured", default(Func<string, int, Promise<bool>>)));

            enumerable.GetAsyncEnumerator().DisposeAsync().Forget();
        }
#endif

        [Test]
        public void AllAsync_Empty(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureValue)
        {
            Promise.Run(async () =>
            {
                var res = new int[0].ToAsyncEnumerable()
                    .AllAsync(configured, async, captureValue, x => x % 2 == 0);
                Assert.True(await res);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AllAsync_Simple_False(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureValue)
        {
            Promise.Run(async () =>
            {
                var res = new[] { 1, 2, 3, 4 }.ToAsyncEnumerable()
                    .AllAsync(configured, async, captureValue, x => x % 2 == 0);
                Assert.False(await res);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AllAsync_Simple_True(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureValue)
        {
            Promise.Run(async () =>
            {
                var res = new[] { 2, 8, 4 }.ToAsyncEnumerable()
                    .AllAsync(configured, async, captureValue, x => x % 2 == 0);
                Assert.True(await res);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AllAsync_Throw_Source(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureValue)
        {
            Promise.Run(async () =>
            {
                var ex = new Exception("Bang!");
                var res = AsyncEnumerable<int>.Rejected(ex)
                    .AllAsync(configured, async, captureValue, x => x % 2 == 0);
                await TestHelper.AssertThrowsAsync(() => res, ex);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AllAsync_Throw_Predicate(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureValue)
        {
            Promise.Run(async () =>
            {
                var ex = new Exception("Bang!");
                var res = new[] { 2, 8, 4 }.ToAsyncEnumerable()
                    .AllAsync(configured, async, captureValue, x => { throw ex; });
                await TestHelper.AssertThrowsAsync(() => res, ex);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AllAsync_Cancel(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureValue)
        {
            Promise.Run(async () =>
            {
                var xs = AsyncEnumerable.Create<int>(async (writer, cancelationToken) =>
                {
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(0);
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(2);
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(4);
                });
                using (var cancelationSource = CancelationSource.New())
                {
                    var res = xs.AllAsync(configured, async, captureValue, x =>
                    {
                        if (x == 2)
                        {
                            cancelationSource.Cancel();
                        }
                        return x % 2 == 0;
                    }, cancelationSource.Token);
                    await TestHelper.AssertCanceledAsync(() => res);
                }
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }
    }
}