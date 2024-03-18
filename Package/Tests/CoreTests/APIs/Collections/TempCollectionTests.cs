#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using System.Linq;
using Proto.Promises;
using NUnit.Framework;
using Proto.Promises.Collections;
using Proto.Promises.Linq;
using System;

namespace ProtoPromiseTests.APIs.Collections
{
    public class TempCollectionTests
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

        [Test]
        public void TempCollectionBuilder()
        {
            var builder = new TempCollectionBuilder<int>(0);

            Assert.AreEqual(0, builder.View.Count);
            builder.Add(1);
            Assert.AreEqual(1, builder.View.Count);
            Assert.AreEqual(1, builder.View[0]);
            builder.Dispose();
            builder = new TempCollectionBuilder<int>(0);
            Assert.AreEqual(0, builder.View.Count);
            for (int i = 0; i < 100; i++)
            {
                builder.Add(i);
            }
            Assert.AreEqual(100, builder.View.Count);
            for (int i = 0; i < 100; i++)
            {
                Assert.AreEqual(i, builder.View[i]);
            }
            builder.Dispose();

            Promise.Manager.ClearObjectPool();
        }

#if NETCOREAPP || NETSTANDARD2_1_OR_GREATER || UNITY_2021_2_OR_NEWER
        [Test]
        public void TempCollection_Span()
        {
            var builder = new TempCollectionBuilder<int>(0);

            Assert.AreEqual(0, builder.View.Span.Length);
            builder.Add(1);
            Assert.AreEqual(1, builder.View.Span.Length);
            Assert.AreEqual(1, builder.View.Span[0]);
            builder.Dispose();
            builder = new TempCollectionBuilder<int>(0);
            Assert.AreEqual(0, builder.View.Span.Length);
            for (int i = 0; i < 100; i++)
            {
                builder.Add(i);
            }
            Assert.AreEqual(100, builder.View.Span.Length);
            for (int i = 0; i < 100; i++)
            {
                Assert.AreEqual(i, builder.View.Span[i]);
            }
            builder.Dispose();
        }
#endif // NETCOREAPP || NETSTANDARD2_1_OR_GREATER || UNITY_2021_2_OR_NEWER

        [Test]
        public void TempCollection_Enumerator(
            [Values(0, 1, 2, 10)] int count)
        {
            var builder = new TempCollectionBuilder<int>(0);

            for (int i = 0; i < count; i++)
            {
                builder.Add(i);
            }

            int iteratedCount = 0;
            foreach (var item in builder.View)
            {
                Assert.AreEqual(iteratedCount, item);
                ++iteratedCount;
            }

            Assert.AreEqual(count, iteratedCount);
            builder.Dispose();
        }

        [Test]
        public void TempCollection_ToArray(
            [Values(0, 1, 2, 10)] int count)
        {
            var builder = new TempCollectionBuilder<int>(0);

            for (int i = 0; i < count; i++)
            {
                builder.Add(i);
            }

            CollectionAssert.AreEqual(Enumerable.Range(0, count), builder.View.ToArray());
            builder.Dispose();
        }

        [Test]
        public void TempCollection_ToList(
            [Values(0, 1, 2, 10)] int count)
        {
            var builder = new TempCollectionBuilder<int>(0);

            for (int i = 0; i < count; i++)
            {
                builder.Add(i);
            }

            CollectionAssert.AreEqual(Enumerable.Range(0, count), builder.View.ToList());
            builder.Dispose();
        }

#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
        [Test]
        public void TempCollectionIsInvalidAfterDisposed(
            [Values(0, 1, 2, 10)] int count)
        {
            var builder = new TempCollectionBuilder<int>(0);

            for (int i = 0; i < count; i++)
            {
                builder.Add(i);
            }

            var tempCollection = builder.View;

            CollectionAssert.AreEqual(Enumerable.Range(0, count), tempCollection.ToList());
            builder.Dispose();

            AssertIsInvalid(tempCollection);
        }
#endif // PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE

        public static void AssertIsInvalid<T>(TempCollection<T> tempCollection)
        {
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
            Assert.Catch<System.InvalidOperationException>(() => { var _ = tempCollection.Count; });
            Assert.Catch<System.InvalidOperationException>(() => { var _ = tempCollection[0]; });
            Assert.Catch<System.InvalidOperationException>(() => { var _ = tempCollection.GetEnumerator(); });
            Assert.Catch<System.InvalidOperationException>(() => { var _ = tempCollection.ToArray(); });
            Assert.Catch<System.InvalidOperationException>(() => { var _ = tempCollection.ToList(); });
            Assert.Catch<System.InvalidOperationException>(() =>
            {
                T[] array = new T[1];
                tempCollection.CopyTo(array, 0);
            });
#if NETCOREAPP || NETSTANDARD2_1_OR_GREATER || UNITY_2021_2_OR_NEWER
            Assert.Catch<System.InvalidOperationException>(() => { var _ = tempCollection.Span; });
#endif
#endif // PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
        }

        // We test that a TempCollection is valid when percolated through multiple AsyncEnumerables,
        // and is invalidated when the AsyncEnumerator has MoveNextAsync or DisposeAsync called.
        [Test]
        public void TempCollection_IsValidUntilDisposedAsync(
            [Values(0, 1, 2, 10)] int tempCollectionSize,
            [Values(0, 1, 2, 10)] int tempCollectionCount)
        {
            var tempCollectionEnumerable = AsyncEnumerable.Create<TempCollection<int>>(async (writer, cancelationToken) =>
            {
                for (int i = 0; i < tempCollectionCount; ++i)
                {
                    using (var builder = new TempCollectionBuilder<int>(0))
                    {
                        for (int j = 0; j < tempCollectionCount; j++)
                        {
                            builder.Add(j);
                        }

                        await writer.YieldAsync(builder.View);
                    }
                }
            });

            var cachedCollection = default(TempCollection<int>);
            int counter = 0;

            AsyncEnumerable.Create<TempCollection<int>>(async (writer, cancelationToken) =>
            {
                var asyncEnumerator = tempCollectionEnumerable.GetAsyncEnumerator();
                try
                {
                    while (await asyncEnumerator.MoveNextAsync())
                    {
                        await writer.YieldAsync(asyncEnumerator.Current);
                    }
                }
                finally
                {
                    await asyncEnumerator.DisposeAsync();
                }
            })
                .ForEachAsync(tempCollection =>
                {
                    AssertIsInvalid(cachedCollection);
                    cachedCollection = tempCollection;
                    CollectionAssert.AreEqual(Enumerable.Range(0, tempCollectionCount), tempCollection.ToList());
                    if (++counter == 5)
                    {
                        throw Promise.CancelException();
                    }
                })
                .CatchCancelation(() => { })
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));

            AssertIsInvalid(cachedCollection);
            Assert.AreEqual(Math.Min(tempCollectionCount, 5), counter);
        }
    }
}