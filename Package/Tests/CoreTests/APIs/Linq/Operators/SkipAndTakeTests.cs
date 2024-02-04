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
    public class SkipAndTakeTests
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

        public enum SkipOrTakeType
        {
            SkipSimple,
            TakeSimple,
#if NETCOREAPP || UNITY_2021_2_OR_NEWER // System.Linq's SkipLast and TakeLast were added in netstandard2.1 and netcoreapp2.0.
            SkipLastSimple,
            TakeLastSimple
#endif
        }

        private static void SkipOrTake<T>(ref AsyncEnumerable<T> asyncSource, SkipOrTakeType skipOrTakeType, int count)
        {
            if (skipOrTakeType == SkipOrTakeType.SkipSimple)
            {
                asyncSource = asyncSource.Skip(count);
            }
            else if (skipOrTakeType == SkipOrTakeType.TakeSimple)
            {
                asyncSource = asyncSource.Take(count);
            }
#if NETCOREAPP || UNITY_2021_2_OR_NEWER
            else if (skipOrTakeType == SkipOrTakeType.SkipLastSimple)
            {
                asyncSource = asyncSource.SkipLast(count);
            }
            else
            {
                asyncSource = asyncSource.TakeLast(count);
            }
#endif
        }

        private static void SkipOrTake<T>(ref IEnumerable<T> syncSource, SkipOrTakeType skipOrTakeType, int count)
        {
            if (skipOrTakeType == SkipOrTakeType.SkipSimple)
            {
                syncSource = syncSource.Skip(count);
            }
            else if (skipOrTakeType == SkipOrTakeType.TakeSimple)
            {
                syncSource = syncSource.Take(count);
            }
#if NETCOREAPP || UNITY_2021_2_OR_NEWER
            else if (skipOrTakeType == SkipOrTakeType.SkipLastSimple)
            {
                syncSource = syncSource.SkipLast(count);
            }
            else
            {
                syncSource = syncSource.TakeLast(count);
            }
#endif
        }

        [Test]
        public void SkipTake_Simple_EmptySource(
            [Values] SkipOrTakeType skipOrTakeType,
            [Values(-1, 0, 1, 3, 7)] int count)
        {
            Promise.Run(async () =>
            {
                var xs = new int[0];
                var asyncEnumerable = xs.ToAsyncEnumerable();
                SkipOrTake(ref asyncEnumerable, skipOrTakeType, count);
                var asyncEnumerator = asyncEnumerable.GetAsyncEnumerator();
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void SkipTake_Simple_SourceThrows(
            [Values] SkipOrTakeType skipOrTakeType,
            [Values(-1, 0, 1)] int count)
        {
            Promise.Run(async () =>
            {
                var ex = new Exception("Bang");
                var asyncEnumerable = AsyncEnumerable<int>.Rejected(ex);
                SkipOrTake(ref asyncEnumerable, skipOrTakeType, count);
                var asyncEnumerator = asyncEnumerable.GetAsyncEnumerator();
                await TestHelper.AssertThrowsAsync(() => asyncEnumerator.MoveNextAsync(), ex);
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void SkipTake_Simple_1(
            [Values] SkipOrTakeType skipOrTakeType,
            [Values(-1, 0, 1, 3, 7)] int count)
        {
            Promise.Run(async () =>
            {
                var xs = new[] { -2, -1, 0, 1, 2, 3 };
                IEnumerable<int> enumerable = xs;
                SkipOrTake(ref enumerable, skipOrTakeType, count);
                var asyncEnumerable = xs.ToAsyncEnumerable();
                SkipOrTake(ref asyncEnumerable, skipOrTakeType, count);
                var asyncEnumerator = asyncEnumerable.GetAsyncEnumerator();
                foreach (var x in enumerable)
                {
                    Assert.True(await asyncEnumerator.MoveNextAsync());
                    Assert.AreEqual(x, asyncEnumerator.Current);
                }
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void SkipTake_Simple_2(
            [Values] SkipOrTakeType skipOrTakeType1,
            [Values(0, 1, 3, 7)] int count1,
            [Values] SkipOrTakeType skipOrTakeType2,
            [Values(0, 2, 4, 6)] int count2)
        {
            Promise.Run(async () =>
            {
                var xs = new[] { -2, -1, 0, 1, 2, 3 };
                IEnumerable<int> enumerable = xs;
                SkipOrTake(ref enumerable, skipOrTakeType1, count1);
                SkipOrTake(ref enumerable, skipOrTakeType2, count2);
                var asyncEnumerable = xs.ToAsyncEnumerable();
                SkipOrTake(ref asyncEnumerable, skipOrTakeType1, count1);
                SkipOrTake(ref asyncEnumerable, skipOrTakeType2, count2);
                var asyncEnumerator = asyncEnumerable.GetAsyncEnumerator();
                foreach (var x in enumerable)
                {
                    Assert.True(await asyncEnumerator.MoveNextAsync());
                    Assert.AreEqual(x, asyncEnumerator.Current);
                }
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void SkipTake_Simple_3(
            [Values] SkipOrTakeType skipOrTakeType1,
            [Values(0, 1, 2, 7)] int count1,
            [Values] SkipOrTakeType skipOrTakeType2,
            [Values(0, 1, 7)] int count2,
            [Values] SkipOrTakeType skipOrTakeType3,
            [Values(1, 7)] int count3)
        {
            Promise.Run(async () =>
            {
                var xs = new[] { -2, -1, 0, 1, 2, 3 };
                IEnumerable<int> enumerable = xs;
                SkipOrTake(ref enumerable, skipOrTakeType1, count1);
                SkipOrTake(ref enumerable, skipOrTakeType2, count2);
                SkipOrTake(ref enumerable, skipOrTakeType3, count3);
                var asyncEnumerable = xs.ToAsyncEnumerable();
                SkipOrTake(ref asyncEnumerable, skipOrTakeType1, count1);
                SkipOrTake(ref asyncEnumerable, skipOrTakeType2, count2);
                SkipOrTake(ref asyncEnumerable, skipOrTakeType3, count3);
                var asyncEnumerator = asyncEnumerable.GetAsyncEnumerator();
                foreach (var x in enumerable)
                {
                    Assert.True(await asyncEnumerator.MoveNextAsync());
                    Assert.AreEqual(x, asyncEnumerator.Current);
                }
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }
    }
}

#endif