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
    public static class SelectManyWithResultSelectorHelper
    {
        // We test all the different overloads.
        public static AsyncEnumerable<TResult> SelectMany<TSource, TCollection, TResult>(this AsyncEnumerable<TSource> source,
            bool configured,
            bool async,
            bool captureCollection,
            Func<TSource, AsyncEnumerable<TCollection>> collectionSelector,
            bool captureResult,
            Func<TSource, TCollection, TResult> resultSelector,
            CancelationToken configuredCancelationToken = default)
        {
            if (configured)
            {
                return SelectMany(source.ConfigureAwait(SynchronizationOption.Foreground).WithCancelation(configuredCancelationToken), async, captureCollection, collectionSelector, captureResult, resultSelector);
            }

            const string capturedCollection = "capturedCollection";
            const string capturedResult = "capturedResult";

            if (!captureCollection)
            {
                if (!captureResult)
                {
                    return async
                        ? source.SelectMany(async (x, _) => collectionSelector(x), async (x, y, _) => resultSelector(x, y))
                        : source.SelectMany(collectionSelector, resultSelector);
                }
                else
                {
                    return async
                        ? source.SelectMany(async (x, _) => collectionSelector(x),
                            capturedResult, async (cv, x, y, _) =>
                            {
                                Assert.AreEqual(capturedResult, cv);
                                return resultSelector(x, y);
                            })
                        : source.SelectMany(collectionSelector,
                            capturedResult, (cv, x, y) =>
                            {
                                Assert.AreEqual(capturedResult, cv);
                                return resultSelector(x, y);
                            });
                }
            }
            else
            {
                if (!captureResult)
                {
                    return async
                        ? source.SelectMany(capturedCollection, async (cv, x, _) =>
                            {
                                Assert.AreEqual(capturedCollection, cv);
                                return collectionSelector(x);
                            }, async (x, y, _) => resultSelector(x, y))
                        : source.SelectMany(capturedCollection, (cv, x) =>
                            {
                                Assert.AreEqual(capturedCollection, cv);
                                return collectionSelector(x);
                            }, resultSelector);
                }
                else
                {
                    return async
                        ? source.SelectMany(capturedCollection, async (cv, x, _) =>
                            {
                                Assert.AreEqual(capturedCollection, cv);
                                return collectionSelector(x);
                            }, capturedResult, async (cv, x, y, _) =>
                            {
                                Assert.AreEqual(capturedResult, cv);
                                return resultSelector(x, y);
                            })
                        : source.SelectMany(capturedCollection, (cv, x) =>
                            {
                                Assert.AreEqual(capturedCollection, cv);
                                return collectionSelector(x);
                            }, capturedResult, (cv, x, y) =>
                            {
                                Assert.AreEqual(capturedResult, cv);
                                return resultSelector(x, y);
                            });
                }
            }
        }

        public static AsyncEnumerable<TResult> SelectMany<TSource, TCollection, TResult>(this in ConfiguredAsyncEnumerable<TSource> source,
            bool async,
            bool captureCollection,
            Func<TSource, AsyncEnumerable<TCollection>> collectionSelector,
            bool captureResult,
            Func<TSource, TCollection, TResult> resultSelector)
        {

            const string capturedCollection = "capturedCollection";
            const string capturedResult = "capturedResult";

            if (!captureCollection)
            {
                if (!captureResult)
                {
                    return async
                        ? source.SelectMany(async (x, _) => collectionSelector(x), async (x, y, _) => resultSelector(x, y))
                        : source.SelectMany(collectionSelector, resultSelector);
                }
                else
                {
                    return async
                        ? source.SelectMany(async (x, _) => collectionSelector(x),
                            capturedResult, async (cv, x, y, _) =>
                            {
                                Assert.AreEqual(capturedResult, cv);
                                return resultSelector(x, y);
                            })
                        : source.SelectMany(collectionSelector,
                            capturedResult, (cv, x, y) =>
                            {
                                Assert.AreEqual(capturedResult, cv);
                                return resultSelector(x, y);
                            });
                }
            }
            else
            {
                if (!captureResult)
                {
                    return async
                        ? source.SelectMany(capturedCollection, async (cv, x, _) =>
                        {
                            Assert.AreEqual(capturedCollection, cv);
                            return collectionSelector(x);
                        }, async (x, y, _) => resultSelector(x, y))
                        : source.SelectMany(capturedCollection, (cv, x) =>
                        {
                            Assert.AreEqual(capturedCollection, cv);
                            return collectionSelector(x);
                        }, resultSelector);
                }
                else
                {
                    return async
                        ? source.SelectMany(capturedCollection, async (cv, x, _) =>
                        {
                            Assert.AreEqual(capturedCollection, cv);
                            return collectionSelector(x);
                        }, capturedResult, async (cv, x, y, _) =>
                        {
                            Assert.AreEqual(capturedResult, cv);
                            return resultSelector(x, y);
                        })
                        : source.SelectMany(capturedCollection, (cv, x) =>
                        {
                            Assert.AreEqual(capturedCollection, cv);
                            return collectionSelector(x);
                        }, capturedResult, (cv, x, y) =>
                        {
                            Assert.AreEqual(capturedResult, cv);
                            return resultSelector(x, y);
                        });
                }
            }
        }
    }

    public class SelectManyWithResultSelectorTests
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

            Assert.Catch<System.ArgumentNullException>(() => enumerable.SelectMany(default(Func<int, AsyncEnumerable<int>>), (x, y) => x * y));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.SelectMany(default(Func<int, CancelationToken, Promise<AsyncEnumerable<int>>>)));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.SelectMany(captureValue, default(Func<string, int, AsyncEnumerable<int>>), (x, y) => x * y));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.SelectMany(captureValue, default(Func<string, int, CancelationToken, Promise<AsyncEnumerable<int>>>), async (x, y, _) => x * y));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).SelectMany(default(Func<int, AsyncEnumerable<int>>), (x, y) => x * y));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).SelectMany(default(Func<int, CancelationToken, Promise<AsyncEnumerable<int>>>), async (x, y, _) => x * y));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).SelectMany(captureValue, default(Func<string, int, AsyncEnumerable<int>>), (x, y) => x * y));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).SelectMany(captureValue, default(Func<string, int, CancelationToken, Promise<AsyncEnumerable<int>>>), async (x, y, _) => x * y));


            Assert.Catch<System.ArgumentNullException>(() => enumerable.SelectMany(x => AsyncEnumerable.Return(42), default(Func<int, int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.SelectMany(async (x, _) => AsyncEnumerable.Return(42), default(Func<int, int, CancelationToken, Promise<int>>)));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.SelectMany(captureValue, (cv, x) => AsyncEnumerable.Return(42), default(Func<int, int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.SelectMany(captureValue, async (cv, x, _) => AsyncEnumerable.Return(42), default(Func<int, int, CancelationToken, Promise<int>>)));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).SelectMany(x => AsyncEnumerable.Return(42), default(Func<int, int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).SelectMany(async (x, _) => AsyncEnumerable.Return(42), default(Func<int, int, CancelationToken, Promise<int>>)));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).SelectMany(captureValue, (cv, x) => AsyncEnumerable.Return(42), default(Func<int, int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).SelectMany(captureValue, async (cv, x, _) => AsyncEnumerable.Return(42), default(Func<int, int, CancelationToken, Promise<int>>)));

            enumerable.GetAsyncEnumerator().DisposeAsync().Forget();
        }
#endif //PROMISE_DEBUG

        [Test]
        public void SelectMany_Empty(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureCollection,
            [Values] bool captureResult)
        {
            Promise.Run(async () =>
            {
                var asyncEnumerator = AsyncEnumerable.Empty<int>()
                    .SelectMany(configured, async, captureCollection, x => AsyncEnumerable.Range(0, x), captureResult, (x, y) => x * y)
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
            [Values] bool captureCollection,
            [Values] bool captureResult)
        {
            Promise.Run(async () =>
            {
                var asyncEnumerator = new[] { 1, 2, 3 }.ToAsyncEnumerable()
                    .SelectMany(configured, async, captureCollection, x => AsyncEnumerable.Range(0, x), captureResult, (x, y) => x * y)
                    .GetAsyncEnumerator();
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(0, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(0, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(2, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(0, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(3, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(6, asyncEnumerator.Current);
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void SelectMany_Throws_CollectionSelector(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureCollection,
            [Values] bool captureResult)
        {
            Promise.Run(async () =>
            {
                var asyncEnumerator = new[] { -1, 0, 1 }.ToAsyncEnumerable()
                    .SelectMany(configured, async, captureCollection, x => AsyncEnumerable.Range(0, x), captureResult, (x, y) => x * y)
                    .GetAsyncEnumerator();
                await TestHelper.AssertThrowsAsync<System.ArgumentOutOfRangeException>(() => asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void SelectMany_Throws_ResultSelector(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureCollection,
            [Values] bool captureResult)
        {
            Promise.Run(async () =>
            {
                var ex = new Exception("Bang!");
                var asyncEnumerator = new[] { -1, 0, 1 }.ToAsyncEnumerable()
                    .SelectMany(configured, async, captureCollection, x => AsyncEnumerable.Return(42), captureResult, (x, y) => { throw ex; return x * y; })
                    .GetAsyncEnumerator();
                await TestHelper.AssertThrowsAsync(() => asyncEnumerator.MoveNextAsync(), ex);
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void SelectMany_Throws_Source(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureCollection,
            [Values] bool captureResult)
        {
            Promise.Run(async () =>
            {
                var ex = new Exception("Bang!");
                var asyncEnumerator = AsyncEnumerable<int>.Rejected(ex)
                    .SelectMany(configured, async, captureCollection, x => AsyncEnumerable.Range(0, x + 1), captureResult, (x, y) => x * y)
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
            [Values] bool captureCollection,
            [Values] bool captureResult,
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
                            .SelectMany(configuredType != ConfiguredType.NotConfigured, async, captureCollection, x => AsyncEnumerable.Range(0, x), captureResult, (x, y) => x * y,
                                configuredType == ConfiguredType.ConfiguredWithCancelation ? configuredCancelationSource.Token : CancelationToken.None)
                            .GetAsyncEnumerator(enumeratorCancelationSource.Token);
                        Assert.True(await asyncEnumerator.MoveNextAsync());
                        Assert.AreEqual(0, asyncEnumerator.Current);
                        Assert.True(await asyncEnumerator.MoveNextAsync());
                        Assert.AreEqual(0, asyncEnumerator.Current);
                        Assert.True(await asyncEnumerator.MoveNextAsync());
                        Assert.AreEqual(2, asyncEnumerator.Current);
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
            [Values] bool captureCollection,
            [Values] bool captureResult,
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
                            .SelectMany(configuredType != ConfiguredType.NotConfigured, async, captureCollection, x => CreateInner(x), captureResult, (x, y) => x * y,
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