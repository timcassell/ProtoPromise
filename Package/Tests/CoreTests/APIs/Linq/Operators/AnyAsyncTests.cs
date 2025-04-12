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
    public static class AnyHelper
    {
        // We test all the different overloads.
        public static Promise<bool> AnyAsync<TSource>(this AsyncEnumerable<TSource> source,
            bool configured,
            bool async,
            bool captureValue,
            Func<TSource, bool> predicate,
            CancelationToken cancelationToken = default)
        {
            if (configured)
            {
                return AnyAsync(source.ConfigureAwait(SynchronizationOption.Foreground).WithCancelation(cancelationToken), async, predicate, captureValue);
            }

            const string valueCapture = "valueCapture";

            if (!captureValue)
            {
                return async
                    ? source.AnyAsync(async (x, _) => predicate(x), cancelationToken)
                    : source.AnyAsync(predicate, cancelationToken);
            }
            else
            {
                return async
                    ? source.AnyAsync(valueCapture, async (cv, x, _) =>
                    {
                        Assert.AreEqual(valueCapture, cv);
                        return predicate(x);
                    }, cancelationToken)
                    : source.AnyAsync(valueCapture, (cv, x) =>
                    {
                        Assert.AreEqual(valueCapture, cv);
                        return predicate(x);
                    }, cancelationToken);
            }
        }

        public static Promise<bool> AnyAsync<TSource>(this in ConfiguredAsyncEnumerable<TSource> source,
            bool async,
            Func<TSource, bool> predicate, bool captureValue)
        {
            const string valueCapture = "valueCapture";

            if (!captureValue)
            {
                return async
                    ? source.AnyAsync(async (x, _) => predicate(x))
                    : source.AnyAsync(predicate);
            }
            else
            {
                return async
                    ? source.AnyAsync(valueCapture, async (cv, x, _) =>
                    {
                        Assert.AreEqual(valueCapture, cv);
                        return predicate(x);
                    })
                    : source.AnyAsync(valueCapture, (cv, x) =>
                    {
                        Assert.AreEqual(valueCapture, cv);
                        return predicate(x);
                    });
            }
        }
    }

    public class AnyAsyncTests
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
        public void AnyAsync_Null()
        {
            var enumerable = AsyncEnumerable.Range(0, 10);

            Assert.Catch<System.ArgumentNullException>(() => enumerable.AnyAsync(default(Func<int, bool>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AnyAsync("captured", default(Func<string, int, bool>)));
                                                             
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Foreground).AnyAsync(default(Func<int, bool>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Foreground).AnyAsync("captured", default(Func<string, int, bool>)));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.AnyAsync(default(Func<int, CancelationToken, Promise<bool>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AnyAsync("captured", default(Func<string, int, CancelationToken, Promise<bool>>)));
                                                             
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Foreground).AnyAsync(default(Func<int, CancelationToken, Promise<bool>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Foreground).AnyAsync("captured", default(Func<string, int, CancelationToken, Promise<bool>>)));

            enumerable.GetAsyncEnumerator().DisposeAsync().Forget();
        }
#endif

        [Test]
        public void AnyAsync_Empty(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureValue)
        {
            Promise.Run(async () =>
            {
                var res = new int[0].ToAsyncEnumerable()
                    .AnyAsync(configured, async, captureValue, x => x % 2 == 0);
                Assert.False(await res);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AnyAsync_Simple_True(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureValue)
        {
            Promise.Run(async () =>
            {
                var res = new[] { 1, 2, 3, 4 }.ToAsyncEnumerable()
                    .AnyAsync(configured, async, captureValue, x => x % 2 == 0);
                Assert.True(await res);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AnyAsync_Simple_False(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureValue)
        {
            Promise.Run(async () =>
            {
                var res = new[] { 2, 8, 4 }.ToAsyncEnumerable()
                    .AnyAsync(configured, async, captureValue, x => x % 2 != 0);
                Assert.False(await res);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AnyAsync_Throw_Source(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureValue)
        {
            Promise.Run(async () =>
            {
                var ex = new Exception("Bang!");
                var res = AsyncEnumerable<int>.Rejected(ex)
                    .AnyAsync(configured, async, captureValue, x => x % 2 == 0);
                await TestHelper.AssertThrowsAsync(() => res, ex);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AnyAsync_Throw_Predicate(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureValue)
        {
            Promise.Run(async () =>
            {
                var ex = new Exception("Bang!");
                var res = new[] { 2, 8, 4 }.ToAsyncEnumerable()
                    .AnyAsync(configured, async, captureValue, x => { throw ex; });
                await TestHelper.AssertThrowsAsync(() => res, ex);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AnyAsync_NoPredicate_NonEmpty()
        {
            Promise.Run(async () =>
            {
                var res = new[] { 1, 2, 3, 4 }.ToAsyncEnumerable().AnyAsync();
                Assert.True(await res);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AnyAsync_NoPredicate_Empty()
        {
            Promise.Run(async () =>
            {
                var res = new int[0].ToAsyncEnumerable().AnyAsync();
                Assert.False(await res);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AnyAsync_Cancel(
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
                    var ys = xs.AnyAsync(configured, async, captureValue, x =>
                    {
                        if (x == 2)
                        {
                            cancelationSource.Cancel();
                        }
                        return x % 2 != 0;
                    }, cancelationSource.Token);
                    await TestHelper.AssertCanceledAsync(() => ys);
                }
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AnyAsync_NoPredicate_Cancel(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureValue)
        {
            Promise.Run(async () =>
            {
                var deferred = Promise.NewDeferred();
                var xs = AsyncEnumerable.Create<int>(async (writer, cancelationToken) =>
                {
                    await deferred.Promise;
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(1);
                    await deferred.Promise;
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(2);
                    await deferred.Promise;
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(3);
                });
                using (var cancelationSource = CancelationSource.New())
                {
                    var res = xs.AnyAsync(cancelationSource.Token);
                    cancelationSource.Cancel();
                    deferred.Resolve();
                    await TestHelper.AssertCanceledAsync(() => res);
                }
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }
    }
}