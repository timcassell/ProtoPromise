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
            Func<TSource, bool> predicate, bool captureValue)
        {
            if (configured)
            {
                return AllAsync(source.ConfigureAwait(SynchronizationOption.Foreground), async, predicate, captureValue);
            }

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

        public static Promise<bool> AllAsync<TSource>(this ConfiguredAsyncEnumerable<TSource> source,
            bool async,
            Func<TSource, bool> predicate, bool captureValue)
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
                    .AllAsync(configured, async, x => x % 2 == 0, captureValue);
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
                    .AllAsync(configured, async, x => x % 2 == 0, captureValue);
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
                    .AllAsync(configured, async, x => x % 2 == 0, captureValue);
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
                    .AllAsync(configured, async, x => x % 2 == 0, captureValue);
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
                    .AllAsync(configured, async, x => { throw ex; }, captureValue);
                await TestHelper.AssertThrowsAsync(() => res, ex);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }
    }
}

#endif