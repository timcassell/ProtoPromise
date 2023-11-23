#if CSHARP_7_3_OR_NEWER

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
                        Assert.AreEqual(1, num);
                        ++oneCount;
                    }
                    else if (totalCount <= 4 + 3 + 2)
                    {
                        Assert.AreEqual(2, num);
                        ++twoCount;
                    }
                    else
                    {
                        Assert.AreEqual(3, num);
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
        public void AsyncEnumerableMerge_MergesConcurrently_Sync()
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
                        Assert.AreEqual(1, num);
                        ++oneCount;
                    }
                    else if (totalCount <= 4 + 3 + 2)
                    {
                        Assert.AreEqual(2, num);
                        ++twoCount;
                    }
                    else
                    {
                        Assert.AreEqual(3, num);
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

            Func<int, int, AsyncEnumerable<int>> EnumerableRangeAsync = (int start, int count) =>
            {
                return AsyncEnumerable<int>.Create(async (writer, cancelationToken) =>
                {
                    for (int i = start; i < start + count; i++)
                    {
                        if (cancelationToken.IsCancelationRequested)
                        {
                            ++notifiedCount;
                            cancelationToken.ThrowIfCancelationRequested();
                        }

                        if (i > start)
                        {
                            await yieldPromise;
                        }

                        await writer.YieldAsync(i);
                    }
                });
            };

            Func<int, int, AsyncEnumerable<int>> EnumerableRangeAsyncWithCancelation = (int start, int count) =>
            {
                return AsyncEnumerable<int>.Create(async (writer, cancelationToken) =>
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
            };

            var enumerables = new AsyncEnumerable<int>[4]
            {
                EnumerableRangeAsync(0, 1),
                EnumerableRangeAsyncWithCancelation(0, 2),
                EnumerableRangeAsync(0, 3),
                EnumerableRangeAsync(0, 4)
            };

            int totalCount = 0;
            int zeroCount = 0;
            int oneCount = 0;
            int twoCount = 0;
            int threeCount = 0;

            bool canceled = false;

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
                        Assert.AreEqual(1, num);
                        ++oneCount;
                    }
                    else if (totalCount <= 4 + 3 + 2)
                    {
                        Assert.AreEqual(2, num);
                        ++twoCount;
                    }
                    else
                    {
                        Assert.AreEqual(3, num);
                        ++threeCount;
                    }
                })
                .CatchCancelation(() => canceled = true);

            Assert.AreEqual(4, totalCount);
            deferred.Resolve();

            runPromise.WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));

            Assert.True(canceled);
            Assert.AreEqual(2, notifiedCount);

            Assert.AreEqual(zeroCount, 4);
            Assert.AreEqual(oneCount, 0);
            Assert.AreEqual(twoCount, 0);
            Assert.AreEqual(threeCount, 0);
            Assert.AreEqual(4, totalCount);

            yieldPromise.Forget();
        }

        [Test]
        public void AsyncEnumerableMerge_RejectedAsyncEnumerable_NotifiesOthersAndRejectsConsumer()
        {
            var deferred = Promise.NewDeferred();
            var yieldPromise = deferred.Promise.Preserve();

            Exception expectedException = new Exception("expected");
            int notifiedCount = 0;

            Func<int, int, AsyncEnumerable<int>> EnumerableRangeAsync = (int start, int count) =>
            {
                return AsyncEnumerable<int>.Create(async (writer, cancelationToken) =>
                {
                    for (int i = start; i < start + count; i++)
                    {
                        if (cancelationToken.IsCancelationRequested)
                        {
                            ++notifiedCount;
                            cancelationToken.ThrowIfCancelationRequested();
                        }

                        if (i > start)
                        {
                            await yieldPromise;
                        }

                        await writer.YieldAsync(i);
                    }
                });
            };

            Func<int, int, AsyncEnumerable<int>> EnumerableRangeAsyncWithCancelation = (int start, int count) =>
            {
                return AsyncEnumerable<int>.Create(async (writer, cancelationToken) =>
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
            };

            var enumerables = new AsyncEnumerable<int>[4]
            {
                EnumerableRangeAsync(0, 1),
                EnumerableRangeAsyncWithCancelation(0, 2),
                EnumerableRangeAsync(0, 3),
                EnumerableRangeAsync(0, 4)
            };

            int totalCount = 0;
            int zeroCount = 0;
            int oneCount = 0;
            int twoCount = 0;
            int threeCount = 0;

            bool rejected = false;

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
                        Assert.AreEqual(1, num);
                        ++oneCount;
                    }
                    else if (totalCount <= 4 + 3 + 2)
                    {
                        Assert.AreEqual(2, num);
                        ++twoCount;
                    }
                    else
                    {
                        Assert.AreEqual(3, num);
                        ++threeCount;
                    }
                })
                .Catch((System.AggregateException e) =>
                {
                    Assert.AreEqual(1, e.InnerExceptions.Count);
                    Assert.AreEqual(expectedException, e.InnerException);
                    rejected = true;
                });

            Assert.AreEqual(4, totalCount);
            deferred.Resolve();

            runPromise.WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));

            Assert.True(rejected);
            Assert.AreEqual(2, notifiedCount);

            Assert.AreEqual(zeroCount, 4);
            Assert.AreEqual(oneCount, 0);
            Assert.AreEqual(twoCount, 0);
            Assert.AreEqual(threeCount, 0);
            Assert.AreEqual(4, totalCount);

            yieldPromise.Forget();
        }
    }
}

#endif