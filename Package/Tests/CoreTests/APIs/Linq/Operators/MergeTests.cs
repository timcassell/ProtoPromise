#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using NUnit.Framework;
using Proto.Promises;
using Proto.Promises.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace ProtoPromiseTests.APIs.Linq
{
    public class MergeTests
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

        private static AsyncEnumerable<int> EnumerableRangeAsync(int start, int count, bool yield = true)
        {
            return AsyncEnumerable<int>.Create((start, count, yield), async (cv, writer, cancelationToken) =>
            {
                for (int i = cv.start; i < cv.start + cv.count; i++)
                {
                    if (cv.yield)
                    {
                        await Promise.SwitchToBackgroundAwait(forceAsync: true);
                    }

                    await writer.YieldAsync(i);
                }
            });
        }

        [Test]
        public void AsyncEnumerableMerge_MergesConcurrently_Async()
        {
            var deferred = Promise.NewDeferred();
            var yieldPromise = deferred.Promise.Preserve();

            Func<int, int, AsyncEnumerable<int>> EnumerableRangeAsync = (int start, int count) =>
            {
                return AsyncEnumerable<int>.Create(async (writer, cancelationToken) =>
                {
                    for (int i = start; i < start + count; i++)
                    {
                        if (i > start)
                        {
                            await yieldPromise;
                        }

                        await writer.YieldAsync(i);
                    }
                });
            };

            var enumerablesAsync = AsyncEnumerable.Create<AsyncEnumerable<int>>(async (writer, cancelationToken) =>
            {
                await writer.YieldAsync(EnumerableRangeAsync(0, 1));
                await writer.YieldAsync(EnumerableRangeAsync(0, 2));
                await writer.YieldAsync(EnumerableRangeAsync(0, 3));
                await writer.YieldAsync(EnumerableRangeAsync(0, 4));
            });

            int totalCount = 0;
            int zeroCount = 0;
            int oneCount = 0;
            int twoCount = 0;
            int threeCount = 0;

            var runPromise = AsyncEnumerable.Merge(enumerablesAsync)
                .ForEachAsync(num =>
                {
                    ++totalCount;
                    if (totalCount <= 4)
                    {
                        Assert.AreEqual(0, num);
                        ++zeroCount;
                    }
                    else if (totalCount <= 4 + 3)
                    {
                        ++oneCount;
                    }
                    else if (totalCount <= 4 + 3 + 2)
                    {
                        ++twoCount;
                    }
                    else
                    {
                        ++threeCount;
                    }
                });

            Assert.AreEqual(4, totalCount);
            deferred.Resolve();

            runPromise.WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));

            Assert.AreEqual(zeroCount, 4);
            Assert.AreEqual(oneCount, 3);
            Assert.AreEqual(twoCount, 2);
            Assert.AreEqual(threeCount, 1);
            Assert.AreEqual(4 + 3 + 2 + 1, totalCount);

            yieldPromise.Forget();
        }

        [Test]
        public void AsyncEnumerableMerge_MergesConcurrently_Array_Sync()
        {
            var deferred = Promise.NewDeferred();
            var yieldPromise = deferred.Promise.Preserve();

            Func<int, int, AsyncEnumerable<int>> EnumerableRangeAsync = (int start, int count) =>
            {
                return AsyncEnumerable<int>.Create(async (writer, cancelationToken) =>
                {
                    for (int i = start; i < start + count; i++)
                    {
                        if (i > start)
                        {
                            await yieldPromise;
                        }

                        await writer.YieldAsync(i);
                    }
                });
            };

            var enumerables = new AsyncEnumerable<int>[4]
            {
                EnumerableRangeAsync(0, 1),
                EnumerableRangeAsync(0, 2),
                EnumerableRangeAsync(0, 3),
                EnumerableRangeAsync(0, 4)
            };

            int totalCount = 0;
            int zeroCount = 0;
            int oneCount = 0;
            int twoCount = 0;
            int threeCount = 0;

            var runPromise = AsyncEnumerable.Merge(enumerables)
                .ForEachAsync(num =>
                {
                    ++totalCount;
                    if (totalCount <= 4)
                    {
                        Assert.AreEqual(0, num);
                        ++zeroCount;
                    }
                    else if (totalCount <= 4 + 3)
                    {
                        ++oneCount;
                    }
                    else if (totalCount <= 4 + 3 + 2)
                    {
                        ++twoCount;
                    }
                    else
                    {
                        ++threeCount;
                    }
                });

            Assert.AreEqual(4, totalCount);
            deferred.Resolve();

            runPromise.WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));

            Assert.AreEqual(zeroCount, 4);
            Assert.AreEqual(oneCount, 3);
            Assert.AreEqual(twoCount, 2);
            Assert.AreEqual(threeCount, 1);
            Assert.AreEqual(4 + 3 + 2 + 1, totalCount);

            yieldPromise.Forget();
        }

        // ReadOnlySpan<T> is not available in Unity netstandard2.0, and we can't include nuget package dependencies in Unity packages,
        // so we only include this in the nuget package and netstandard2.1+.
#if !UNITY_2018_3_OR_NEWER || UNITY_2021_2_OR_NEWER
        [Test]
        public void AsyncEnumerableMerge_MergesConcurrently_Span_Sync()
        {
            var deferred = Promise.NewDeferred();
            var yieldPromise = deferred.Promise.Preserve();

            Func<int, int, AsyncEnumerable<int>> EnumerableRangeAsync = (int start, int count) =>
            {
                return AsyncEnumerable<int>.Create(async (writer, cancelationToken) =>
                {
                    for (int i = start; i < start + count; i++)
                    {
                        if (i > start)
                        {
                            await yieldPromise;
                        }

                        await writer.YieldAsync(i);
                    }
                });
            };

            ReadOnlySpan<AsyncEnumerable<int>> enumerables = new AsyncEnumerable<int>[4]
            {
                EnumerableRangeAsync(0, 1),
                EnumerableRangeAsync(0, 2),
                EnumerableRangeAsync(0, 3),
                EnumerableRangeAsync(0, 4)
            };

            int totalCount = 0;
            int zeroCount = 0;
            int oneCount = 0;
            int twoCount = 0;
            int threeCount = 0;

            var runPromise = AsyncEnumerable.Merge(enumerables)
                .ForEachAsync(num =>
                {
                    ++totalCount;
                    if (totalCount <= 4)
                    {
                        Assert.AreEqual(0, num);
                        ++zeroCount;
                    }
                    else if (totalCount <= 4 + 3)
                    {
                        ++oneCount;
                    }
                    else if (totalCount <= 4 + 3 + 2)
                    {
                        ++twoCount;
                    }
                    else
                    {
                        ++threeCount;
                    }
                });

            Assert.AreEqual(4, totalCount);
            deferred.Resolve();

            runPromise.WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));

            Assert.AreEqual(zeroCount, 4);
            Assert.AreEqual(oneCount, 3);
            Assert.AreEqual(twoCount, 2);
            Assert.AreEqual(threeCount, 1);
            Assert.AreEqual(4 + 3 + 2 + 1, totalCount);

            yieldPromise.Forget();
        }
#endif // !UNITY_2018_3_OR_NEWER || UNITY_2021_2_OR_NEWER

        [Test]
        public void AsyncEnumerableMerge_MergesConcurrently_2()
        {
            var deferred = Promise.NewDeferred();
            var yieldPromise = deferred.Promise.Preserve();

            Func<int, int, AsyncEnumerable<int>> EnumerableRangeAsync = (int start, int count) =>
            {
                return AsyncEnumerable<int>.Create(async (writer, cancelationToken) =>
                {
                    for (int i = start; i < start + count; i++)
                    {
                        if (i > start)
                        {
                            await yieldPromise;
                        }

                        await writer.YieldAsync(i);
                    }
                });
            };

            int totalCount = 0;
            int zeroCount = 0;
            int oneCount = 0;

            var runPromise = AsyncEnumerable.Merge(
                EnumerableRangeAsync(0, 1),
                EnumerableRangeAsync(0, 2)
            )
                .ForEachAsync(num =>
                {
                    ++totalCount;
                    if (totalCount <= 2)
                    {
                        Assert.AreEqual(0, num);
                        ++zeroCount;
                    }
                    else
                    {
                        ++oneCount;
                    }
                });

            Assert.AreEqual(2, totalCount);
            deferred.Resolve();

            runPromise.WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));

            Assert.AreEqual(zeroCount, 2);
            Assert.AreEqual(oneCount, 1);
            Assert.AreEqual(2 + 1, totalCount);

            yieldPromise.Forget();
        }

        [Test]
        public void AsyncEnumerableMerge_MergesConcurrently_3()
        {
            var deferred = Promise.NewDeferred();
            var yieldPromise = deferred.Promise.Preserve();

            Func<int, int, AsyncEnumerable<int>> EnumerableRangeAsync = (int start, int count) =>
            {
                return AsyncEnumerable<int>.Create(async (writer, cancelationToken) =>
                {
                    for (int i = start; i < start + count; i++)
                    {
                        if (i > start)
                        {
                            await yieldPromise;
                        }

                        await writer.YieldAsync(i);
                    }
                });
            };

            int totalCount = 0;
            int zeroCount = 0;
            int oneCount = 0;
            int twoCount = 0;

            var runPromise = AsyncEnumerable.Merge(
                EnumerableRangeAsync(0, 1),
                EnumerableRangeAsync(0, 2),
                EnumerableRangeAsync(0, 3)
            )
                .ForEachAsync(num =>
                {
                    ++totalCount;
                    if (totalCount <= 3)
                    {
                        Assert.AreEqual(0, num);
                        ++zeroCount;
                    }
                    else if (totalCount <= 3 + 2)
                    {
                        ++oneCount;
                    }
                    else
                    {
                        ++twoCount;
                    }
                });

            Assert.AreEqual(3, totalCount);
            deferred.Resolve();

            runPromise.WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));

            Assert.AreEqual(zeroCount, 3);
            Assert.AreEqual(oneCount, 2);
            Assert.AreEqual(twoCount, 1);
            Assert.AreEqual(3 + 2 + 1, totalCount);

            yieldPromise.Forget();
        }

        [Test]
        public void AsyncEnumerableMerge_MergesConcurrently_4()
        {
            var deferred = Promise.NewDeferred();
            var yieldPromise = deferred.Promise.Preserve();

            Func<int, int, AsyncEnumerable<int>> EnumerableRangeAsync = (int start, int count) =>
            {
                return AsyncEnumerable<int>.Create(async (writer, cancelationToken) =>
                {
                    for (int i = start; i < start + count; i++)
                    {
                        if (i > start)
                        {
                            await yieldPromise;
                        }

                        await writer.YieldAsync(i);
                    }
                });
            };

            int totalCount = 0;
            int zeroCount = 0;
            int oneCount = 0;
            int twoCount = 0;
            int threeCount = 0;

            var runPromise = AsyncEnumerable.Merge(
                EnumerableRangeAsync(0, 1),
                EnumerableRangeAsync(0, 2),
                EnumerableRangeAsync(0, 3),
                EnumerableRangeAsync(0, 4)
            )
                .ForEachAsync(num =>
                {
                    ++totalCount;
                    if (totalCount <= 4)
                    {
                        Assert.AreEqual(0, num);
                        ++zeroCount;
                    }
                    else if (totalCount <= 4 + 3)
                    {
                        ++oneCount;
                    }
                    else if (totalCount <= 4 + 3 + 2)
                    {
                        ++twoCount;
                    }
                    else
                    {
                        ++threeCount;
                    }
                });

            Assert.AreEqual(4, totalCount);
            deferred.Resolve();

            runPromise.WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));

            Assert.AreEqual(zeroCount, 4);
            Assert.AreEqual(oneCount, 3);
            Assert.AreEqual(twoCount, 2);
            Assert.AreEqual(threeCount, 1);
            Assert.AreEqual(4 + 3 + 2 + 1, totalCount);

            yieldPromise.Forget();
        }

        [Test]
        public void AsyncEnumerableMerge_CanceledAsyncEnumerable_NotifiesOthersAndCancelsConsumer()
        {
            var deferred = Promise.NewDeferred();
            var yieldPromise = deferred.Promise.Preserve();

            int notifiedCount = 0;

            Func<AsyncEnumerable<int>> ObserveCancelation = () => AsyncEnumerable<int>.Create(async (writer, cancelationToken) =>
            {
                await yieldPromise;

                if (cancelationToken.IsCancelationRequested)
                {
                    ++notifiedCount;
                }
            });

            Func<int, int, AsyncEnumerable<int>> EnumerableRangeAsyncWithCancelation = (int start, int count) => AsyncEnumerable<int>.Create(async (writer, cancelationToken) =>
            {
                for (int i = start; i < start + count; i++)
                {
                    cancelationToken.ThrowIfCancelationRequested();

                    if (i > start)
                    {
                        throw Promise.CancelException();
                    }

                    await writer.YieldAsync(i);
                }
            });

            Promise.Run(async () =>
            {
                var asyncEnumerator = AsyncEnumerable.Merge(
                    ObserveCancelation(),
                    EnumerableRangeAsyncWithCancelation(0, 2),
                    ObserveCancelation(),
                    ObserveCancelation()
                )
                    .GetAsyncEnumerator();
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(0, asyncEnumerator.Current);
                var moveNextPromise = asyncEnumerator.MoveNextAsync();
                deferred.Resolve();
                await TestHelper.AssertCanceledAsync(() => moveNextPromise);

                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));

            Assert.AreEqual(3, notifiedCount);

            yieldPromise.Forget();
        }

        [Test]
        public void AsyncEnumerableMerge_RejectedAsyncEnumerable_NotifiesOthersAndRejectsConsumer()
        {
            var deferred = Promise.NewDeferred();
            var yieldPromise = deferred.Promise.Preserve();

            Exception expectedException = new Exception("expected");
            int notifiedCount = 0;

            Func<AsyncEnumerable<int>> ObserveCancelation = () => AsyncEnumerable<int>.Create(async (writer, cancelationToken) =>
            {
                await yieldPromise;

                if (cancelationToken.IsCancelationRequested)
                {
                    ++notifiedCount;
                }
            });

            Func<int, int, AsyncEnumerable<int>> EnumerableRangeAsyncWithCancelation = (int start, int count) => AsyncEnumerable<int>.Create(async (writer, cancelationToken) =>
            {
                for (int i = start; i < start + count; i++)
                {
                    cancelationToken.ThrowIfCancelationRequested();

                    if (i > start)
                    {
                        throw expectedException;
                    }

                    await writer.YieldAsync(i);
                }
            });

            Promise.Run(async () =>
            {
                var asyncEnumerator = AsyncEnumerable.Merge(
                    ObserveCancelation(),
                    EnumerableRangeAsyncWithCancelation(0, 2),
                    ObserveCancelation(),
                    ObserveCancelation()
                )
                    .GetAsyncEnumerator();
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(0, asyncEnumerator.Current);
                var moveNextPromise = asyncEnumerator.MoveNextAsync();
                deferred.Resolve();

                bool rejected = false;
                try
                {
                    await moveNextPromise;
                }
                catch (System.AggregateException e)
                {
                    Assert.AreEqual(1, e.InnerExceptions.Count);
                    Assert.AreEqual(expectedException, e.InnerException);
                    rejected = true;
                }
                Assert.True(rejected);

                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));

            Assert.AreEqual(3, notifiedCount);

            yieldPromise.Forget();
        }

        [Test]
        public void AsyncEnumerableMerge_CancelFromSource()
        {
            Func<int, int, AsyncEnumerable<int>> EnumerableRangeAsync = (int start, int count) =>
            {
                return AsyncEnumerable<int>.Create(async (writer, cancelationToken) =>
                {
                    for (int i = start; i < start + count; i++)
                    {
                        cancelationToken.ThrowIfCancelationRequested();
                        await writer.YieldAsync(i);
                    }
                });
            };

            Promise.Run(async () =>
            {
                using (var cancelationSource = CancelationSource.New())
                {
                    var asyncEnumerator = AsyncEnumerable.Merge(
                        EnumerableRangeAsync(0, 2),
                        EnumerableRangeAsync(0, 3),
                        EnumerableRangeAsync(0, 4)
                    )
                        .GetAsyncEnumerator(cancelationSource.Token);
                    Assert.True(await asyncEnumerator.MoveNextAsync());
                    Assert.AreEqual(0, asyncEnumerator.Current);
                    cancelationSource.Cancel();
                    await TestHelper.AssertCanceledAsync(() => asyncEnumerator.MoveNextAsync());
                    await asyncEnumerator.DisposeAsync();
                }
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AsyncEnumerableMerge_Sync_DisposeWithoutMoveNext()
        {
            Promise.Run(async () =>
            {
                var asyncEnumerator = AsyncEnumerable.Merge(
                    AsyncEnumerable.Range(0, 2),
                    AsyncEnumerable.Range(0, 3),
                    AsyncEnumerable.Range(0, 4)
                )
                    .GetAsyncEnumerator();
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AsyncEnumerableMerge_Sync_EarlyDispose()
        {
            Promise.Run(async () =>
            {
                var asyncEnumerator = AsyncEnumerable.Merge(
                    AsyncEnumerable.Range(0, 2),
                    AsyncEnumerable.Range(0, 3),
                    AsyncEnumerable.Range(0, 4)
                )
                    .GetAsyncEnumerator();
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(0, asyncEnumerator.Current);
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AsyncEnumerableMerge_Async_DisposeWithoutMoveNext()
        {
            Promise.Run(async () =>
            {
                var enumerablesAsync = AsyncEnumerable.Create<AsyncEnumerable<int>>(async (writer, cancelationToken) =>
                {
                    await writer.YieldAsync(AsyncEnumerable.Range(0, 2));
                    await writer.YieldAsync(AsyncEnumerable.Range(0, 3));
                    await writer.YieldAsync(AsyncEnumerable.Range(0, 4));
                });
                var asyncEnumerator = AsyncEnumerable.Merge(enumerablesAsync).GetAsyncEnumerator();
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AsyncEnumerableMerge_Async_EarlyDispose()
        {
            Promise.Run(async () =>
            {
                var enumerablesAsync = AsyncEnumerable.Create<AsyncEnumerable<int>>(async (writer, cancelationToken) =>
                {
                    await writer.YieldAsync(AsyncEnumerable.Range(0, 2));
                    await writer.YieldAsync(AsyncEnumerable.Range(0, 3));
                    await writer.YieldAsync(AsyncEnumerable.Range(0, 4));
                });
                var asyncEnumerator = AsyncEnumerable.Merge(enumerablesAsync).GetAsyncEnumerator();
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(0, asyncEnumerator.Current);
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        private static AsyncEnumerable<int> RangeWithCancelationTokenCallbackThrow(int count, bool throwInIterator) => AsyncEnumerable.Create<int>(async (writer, cancelationToken) =>
        {
            cancelationToken.Register(() => throw new Exception("Error in cancelation!"));
            for (int i = 0; i < count; ++i)
            {
                await writer.YieldAsync(i);
                if (throwInIterator)
                {
                    throw new System.InvalidOperationException("Error in async iterator body!");
                }
            }
        });

        [Test]
        public void AsyncEnumerableMerge_Sync_CancelationCallbackExceptionsArePropagated()
        {
            Promise.Run(async () =>
            {
                var asyncEnumerator = AsyncEnumerable.Merge(
                    RangeWithCancelationTokenCallbackThrow(2, false),
                    RangeWithCancelationTokenCallbackThrow(3, false),
                    RangeWithCancelationTokenCallbackThrow(4, true)
                )
                    .GetAsyncEnumerator();
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(0, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(0, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(0, asyncEnumerator.Current);

                bool didThrow = false;
                try
                {
                    await asyncEnumerator.MoveNextAsync();
                }
                catch (AggregateException e)
                {
                    didThrow = true;
                    e = e.Flatten();
                    Assert.AreEqual(4, e.InnerExceptions.Count);
                    Assert.AreEqual(1, e.InnerExceptions.Count(x => x is System.InvalidOperationException));
                }
                Assert.True(didThrow);

                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AsyncEnumerableMerge_Async_CancelationCallbackExceptionsArePropagated()
        {
            Promise.Run(async () =>
            {
                var enumerablesAsync = AsyncEnumerable.Create<AsyncEnumerable<int>>(async (writer, cancelationToken) =>
                {
                    await writer.YieldAsync(RangeWithCancelationTokenCallbackThrow(2, false));
                    await writer.YieldAsync(RangeWithCancelationTokenCallbackThrow(3, false));
                    await writer.YieldAsync(RangeWithCancelationTokenCallbackThrow(4, true));
                });
                var asyncEnumerator = AsyncEnumerable.Merge(enumerablesAsync).GetAsyncEnumerator();
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(0, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(0, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(0, asyncEnumerator.Current);

                bool didThrow = false;
                try
                {
                    await asyncEnumerator.MoveNextAsync();
                }
                catch (AggregateException e)
                {
                    didThrow = true;
                    e = e.Flatten();
                    Assert.AreEqual(4, e.InnerExceptions.Count);
                    Assert.AreEqual(1, e.InnerExceptions.Count(x => x is System.InvalidOperationException));
                }
                Assert.True(didThrow);

                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }
    }
}