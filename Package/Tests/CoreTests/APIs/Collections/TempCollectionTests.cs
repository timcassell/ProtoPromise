#if CSHARP_7_3_OR_NEWER

using System.Linq;
using Proto.Promises;
using NUnit.Framework;
using Proto.Promises.Collections;

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
    }
}

#endif // CSHARP_7_3_OR_NEWER