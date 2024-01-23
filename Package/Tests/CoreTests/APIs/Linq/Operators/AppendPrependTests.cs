#if CSHARP_7_3_OR_NEWER

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
    public class AppendPrependTests
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
        public void Append(
            [Values(0, 1, 2, 3)] int appendCount)
        {
            Promise.Run(async () =>
            {
                var asyncEnumerable = new int[1] { 0 }.ToAsyncEnumerable();
                for (int i = 1; i <= appendCount; ++i)
                {
                    asyncEnumerable = asyncEnumerable.Append(i);
                }
                var asyncEnumerator = asyncEnumerable.GetAsyncEnumerator();
                for (int i = 0; i <= appendCount; ++i)
                {
                    Assert.True(await asyncEnumerator.MoveNextAsync());
                    Assert.AreEqual(i, asyncEnumerator.Current);
                }
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Prepend(
            [Values(0, 1, 2, 3)] int prependCount)
        {
            Promise.Run(async () =>
            {
                var asyncEnumerable = new int[1] { 0 }.ToAsyncEnumerable();
                for (int i = 1; i <= prependCount; ++i)
                {
                    asyncEnumerable = asyncEnumerable.Prepend(i);
                }
                var asyncEnumerator = asyncEnumerable.GetAsyncEnumerator();
                for (int i = prependCount; i >= 0; --i)
                {
                    Assert.True(await asyncEnumerator.MoveNextAsync());
                    Assert.AreEqual(i, asyncEnumerator.Current);
                }
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AppendPrepend(
            [Values(0, 1, 2, 3)] int appendCount,
            [Values(0, 1, 2, 3)] int prependCount)
        {
            Promise.Run(async () =>
            {
                var asyncEnumerable = new int[1] { 0 }.ToAsyncEnumerable();
                for (int i = 1; i <= appendCount; ++i)
                {
                    asyncEnumerable = asyncEnumerable.Append(i);
                }
                for (int i = 1; i <= prependCount; ++i)
                {
                    asyncEnumerable = asyncEnumerable.Prepend(i);
                }
                var asyncEnumerator = asyncEnumerable.GetAsyncEnumerator();
                for (int i = prependCount; i >= 0; --i)
                {
                    Assert.True(await asyncEnumerator.MoveNextAsync());
                    Assert.AreEqual(i, asyncEnumerator.Current);
                }
                for (int i = 1; i <= appendCount; ++i)
                {
                    Assert.True(await asyncEnumerator.MoveNextAsync());
                    Assert.AreEqual(i, asyncEnumerator.Current);
                }
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void PrependAppend(
            [Values(0, 1, 2, 3)] int prependCount,
            [Values(0, 1, 2, 3)] int appendCount)
        {
            Promise.Run(async () =>
            {
                var asyncEnumerable = new int[1] { 0 }.ToAsyncEnumerable();
                for (int i = 1; i <= prependCount; ++i)
                {
                    asyncEnumerable = asyncEnumerable.Prepend(i);
                }
                for (int i = 1; i <= appendCount; ++i)
                {
                    asyncEnumerable = asyncEnumerable.Append(i);
                }
                var asyncEnumerator = asyncEnumerable.GetAsyncEnumerator();
                for (int i = prependCount; i >= 0; --i)
                {
                    Assert.True(await asyncEnumerator.MoveNextAsync());
                    Assert.AreEqual(i, asyncEnumerator.Current);
                }
                for (int i = 1; i <= appendCount; ++i)
                {
                    Assert.True(await asyncEnumerator.MoveNextAsync());
                    Assert.AreEqual(i, asyncEnumerator.Current);
                }
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AppendPrepend_Interleaved(
            [Values(0, 1, 2, 3)] int appendCount,
            [Values(0, 1, 2, 3)] int prependCount)
        {
            Promise.Run(async () =>
            {
                var asyncEnumerable = new int[1] { 0 }.ToAsyncEnumerable();
                int i = 1;
                for (; i <= appendCount && i <= prependCount; ++i)
                {
                    asyncEnumerable = asyncEnumerable
                        .Append(i)
                        .Prepend(i);
                }
                for (int j = i; j <= appendCount; ++j)
                {
                    asyncEnumerable = asyncEnumerable
                        .Append(j);
                }
                for (int j = i; j <= prependCount; ++j)
                {
                    asyncEnumerable = asyncEnumerable
                        .Prepend(j);
                }
                var asyncEnumerator = asyncEnumerable.GetAsyncEnumerator();
                for (i = prependCount; i >= 0; --i)
                {
                    Assert.True(await asyncEnumerator.MoveNextAsync());
                    Assert.AreEqual(i, asyncEnumerator.Current);
                }
                for (i = 1; i <= appendCount; ++i)
                {
                    Assert.True(await asyncEnumerator.MoveNextAsync());
                    Assert.AreEqual(i, asyncEnumerator.Current);
                }
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void PrependAppend_Interleaved(
            [Values(0, 1, 2, 3)] int prependCount,
            [Values(0, 1, 2, 3)] int appendCount)
        {
            Promise.Run(async () =>
            {
                var asyncEnumerable = new int[1] { 0 }.ToAsyncEnumerable();
                int i = 1;
                for (; i <= appendCount && i <= prependCount; ++i)
                {
                    asyncEnumerable = asyncEnumerable
                        .Prepend(i)
                        .Append(i);
                }
                for (int j = i; j <= appendCount; ++j)
                {
                    asyncEnumerable = asyncEnumerable
                        .Append(j);
                }
                for (int j = i; j <= prependCount; ++j)
                {
                    asyncEnumerable = asyncEnumerable
                        .Prepend(j);
                }
                var asyncEnumerator = asyncEnumerable.GetAsyncEnumerator();
                for (i = prependCount; i >= 0; --i)
                {
                    Assert.True(await asyncEnumerator.MoveNextAsync());
                    Assert.AreEqual(i, asyncEnumerator.Current);
                }
                for (i = 1; i <= appendCount; ++i)
                {
                    Assert.True(await asyncEnumerator.MoveNextAsync());
                    Assert.AreEqual(i, asyncEnumerator.Current);
                }
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }
    }
}

#endif