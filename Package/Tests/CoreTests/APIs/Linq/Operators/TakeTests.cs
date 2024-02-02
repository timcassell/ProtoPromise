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
    public class TakeTests
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
        public void Take_Simple_EmptySource()
        {
            Promise.Run(async () =>
            {
                var asyncEnumerator = AsyncEnumerable<int>.Empty().Take(0).GetAsyncEnumerator();
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
                
                asyncEnumerator = AsyncEnumerable<int>.Empty().Take(2).GetAsyncEnumerator();
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Take_Simple(
            [Values(-1, 0, 1, 2, 3, 4, 5)] int takeCount)
        {
            Promise.Run(async () =>
            {
                var xs = new[] { 1, 2, 3, 4 };
                var asyncEnumerator = xs.ToAsyncEnumerable().Take(takeCount).GetAsyncEnumerator();
                for (int i = 0; i < xs.Length && i < takeCount; ++i)
                {
                    Assert.True(await asyncEnumerator.MoveNextAsync());
                    Assert.AreEqual(xs[i], asyncEnumerator.Current);
                }
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Take_Throws_Source()
        {
            Promise.Run(async () =>
            {
                var ex = new Exception("Bang");
                var asyncEnumerator = AsyncEnumerable<int>.Rejected(ex).Take(2).GetAsyncEnumerator();
                await TestHelper.AssertThrowsAsync(() => asyncEnumerator.MoveNextAsync(), ex);
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Take_Simple_TakeTake(
            [Values(-1, 0, 1, 3, 7)] int takeCount1,
            [Values(-1, 0, 2, 4, 6)] int takeCount2)
        {
            Promise.Run(async () =>
            {
                var xs = new[] { -2, -1, 0, 1, 2, 3 };
                var asyncEnumerator = xs.ToAsyncEnumerable().Take(takeCount1).Take(takeCount2).GetAsyncEnumerator();
                int actualTakeCount = takeCount1 <= 0 ? 0 : Math.Max(0, Math.Min(takeCount1, takeCount2));
                for (int i = 0; i < xs.Length && i < actualTakeCount; ++i)
                {
                    Assert.True(await asyncEnumerator.MoveNextAsync());
                    Assert.AreEqual(xs[i], asyncEnumerator.Current);
                }
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void TakeTake_Simple_TakeSkip(
            [Values(-1, 0, 1, 3, 7)] int takeCount,
            [Values(-1, 0, 1, 3, 6)] int skipCount)
        {
            Promise.Run(async () =>
            {
                var xs = new[] { -2, -1, 0, 1, 2, 3 };
                var asyncEnumerator = xs.ToAsyncEnumerable().Take(takeCount).Skip(skipCount).GetAsyncEnumerator();
                for (int i = Math.Max(0, skipCount), j = 0; i < xs.Length && j < takeCount - Math.Max(0, skipCount); ++i, ++j)
                {
                    Assert.True(await asyncEnumerator.MoveNextAsync());
                    Assert.AreEqual(xs[i], asyncEnumerator.Current);
                }
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }
    }
}

#endif