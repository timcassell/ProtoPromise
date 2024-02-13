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

        [Test]
        public void Skip_Simple_Cancel()
        {
            Promise.Run(async () =>
            {
                var asyncEnumerable = AsyncEnumerable.Create<int>(async (writer, cancelationToken) =>
                {
                    foreach (var x in new[] { -2, -1, 0, 1, 2, 3 })
                    {
                        cancelationToken.ThrowIfCancelationRequested();
                        await writer.YieldAsync(x);
                    }
                });
                using (var cancelationSource = CancelationSource.New())
                {
                    var asyncEnumerator = asyncEnumerable.Skip(2).GetAsyncEnumerator(cancelationSource.Token);
                    Assert.True(await asyncEnumerator.MoveNextAsync());
                    Assert.AreEqual(0, asyncEnumerator.Current);
                    Assert.True(await asyncEnumerator.MoveNextAsync());
                    Assert.AreEqual(1, asyncEnumerator.Current);
                    cancelationSource.Cancel();
                    await TestHelper.AssertCanceledAsync(() => asyncEnumerator.MoveNextAsync());
                    await asyncEnumerator.DisposeAsync();
                }
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void SkipLast_Cancel()
        {
            Promise.Run(async () =>
            {
                var asyncEnumerable = AsyncEnumerable.Create<int>(async (writer, cancelationToken) =>
                {
                    foreach (var x in new[] { -2, -1, 0, 1, 2, 3 })
                    {
                        cancelationToken.ThrowIfCancelationRequested();
                        await writer.YieldAsync(x);
                    }
                });
                using (var cancelationSource = CancelationSource.New())
                {
                    var asyncEnumerator = asyncEnumerable.SkipLast(2).GetAsyncEnumerator(cancelationSource.Token);
                    Assert.True(await asyncEnumerator.MoveNextAsync());
                    Assert.AreEqual(-2, asyncEnumerator.Current);
                    Assert.True(await asyncEnumerator.MoveNextAsync());
                    Assert.AreEqual(-1, asyncEnumerator.Current);
                    cancelationSource.Cancel();
                    await TestHelper.AssertCanceledAsync(() => asyncEnumerator.MoveNextAsync());
                    await asyncEnumerator.DisposeAsync();
                }
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Take_Simple_Cancel()
        {
            Promise.Run(async () =>
            {
                var asyncEnumerable = AsyncEnumerable.Create<int>(async (writer, cancelationToken) =>
                {
                    foreach (var x in new[] { -2, -1, 0, 1, 2, 3 })
                    {
                        cancelationToken.ThrowIfCancelationRequested();
                        await writer.YieldAsync(x);
                    }
                });
                using (var cancelationSource = CancelationSource.New())
                {
                    var asyncEnumerator = asyncEnumerable.Take(4).GetAsyncEnumerator(cancelationSource.Token);
                    Assert.True(await asyncEnumerator.MoveNextAsync());
                    Assert.AreEqual(-2, asyncEnumerator.Current);
                    Assert.True(await asyncEnumerator.MoveNextAsync());
                    Assert.AreEqual(-1, asyncEnumerator.Current);
                    cancelationSource.Cancel();
                    await TestHelper.AssertCanceledAsync(() => asyncEnumerator.MoveNextAsync());
                    await asyncEnumerator.DisposeAsync();
                }
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void TakeLast_Simple_Cancel()
        {
            Promise.Run(async () =>
            {
                var asyncEnumerable = AsyncEnumerable.Create<int>(async (writer, cancelationToken) =>
                {
                    foreach (var x in new[] { -2, -1, 0, 1, 2, 3 })
                    {
                        cancelationToken.ThrowIfCancelationRequested();
                        await writer.YieldAsync(x);
                    }
                });
                using (var cancelationSource = CancelationSource.New())
                {
                    var asyncEnumerator = asyncEnumerable.TakeLast(4).GetAsyncEnumerator(cancelationSource.Token);
                    // TakeLast results are queued before they are yielded, so we need to cancel before starting iteration.
                    cancelationSource.Cancel();
                    await TestHelper.AssertCanceledAsync(() => asyncEnumerator.MoveNextAsync());
                    await asyncEnumerator.DisposeAsync();
                }
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

#if NETSTANDARD2_1_OR_GREATER || UNITY_2021_2_OR_NEWER || NETCOREAPP3_0_OR_GREATER // System.Range is available in netcorapp3.0 and netstandard2.1.
        public static IEnumerable<TestCaseData> TakeRangeEmptyArgs()
        {
            yield return new TestCaseData(0..0);
            yield return new TestCaseData(0..5);
            yield return new TestCaseData(^5..5);
            yield return new TestCaseData(0..^0);
            yield return new TestCaseData(^5..^0);
        }

        [Test, TestCaseSource(nameof(TakeRangeEmptyArgs))]
        public void Take_Range_EmptySource(Range range)
        {
            Promise.Run(async () =>
            {
                var xs = new int[0];
                var asyncEnumerator = xs.ToAsyncEnumerable().Take(range).GetAsyncEnumerator();
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test, TestCaseSource(nameof(TakeRangeEmptyArgs))]
        public void Take_Range_SourceThrows(Range range)
        {
            Promise.Run(async () =>
            {
                var ex = new Exception("Bang");
                var asyncEnumerator = AsyncEnumerable<int>.Rejected(ex).Take(range).GetAsyncEnumerator();
                await TestHelper.AssertThrowsAsync(() => asyncEnumerator.MoveNextAsync(), ex);
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        public static IEnumerable<TestCaseData> TakeRangeCancelArgs()
        {
            yield return new TestCaseData(1..5);
            yield return new TestCaseData(1..^2);
            yield return new TestCaseData(^5..5);
            yield return new TestCaseData(^5..^2);
        }

        [Test, TestCaseSource(nameof(TakeRangeCancelArgs))]
        public void Take_Range_Cancel(Range range)
        {
            Promise.Run(async () =>
            {
                var asyncEnumerable = AsyncEnumerable.Create<int>(async (writer, cancelationToken) =>
                {
                    foreach (var x in new[] { -2, -1, 0, 1, 2, 3 })
                    {
                        cancelationToken.ThrowIfCancelationRequested();
                        await writer.YieldAsync(x);
                    }
                });
                using (var cancelationSource = CancelationSource.New())
                {
                    var asyncEnumerator = asyncEnumerable.Take(range).GetAsyncEnumerator(cancelationSource.Token);
                    // If the results will be queued before yielded, we need to cancel before starting iteration.
                    if (!range.Start.IsFromEnd)
                    {
                        Assert.True(await asyncEnumerator.MoveNextAsync());
                        Assert.AreEqual(-1, asyncEnumerator.Current);
                        Assert.True(await asyncEnumerator.MoveNextAsync());
                        Assert.AreEqual(0, asyncEnumerator.Current);
                    }
                    cancelationSource.Cancel();
                    await TestHelper.AssertCanceledAsync(() => asyncEnumerator.MoveNextAsync());
                    await asyncEnumerator.DisposeAsync();
                }
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }
#endif // NETSTANDARD2_1_OR_GREATER || UNITY_2021_2_OR_NEWER || NETCOREAPP3_0_OR_GREATER

#if NET6_0_OR_GREATER // System.Linq's Take(Range) was added in net6.0
        public static IEnumerable<TestCaseData> TakeRangeArgs1()
        {
            yield return new TestCaseData(0..4);
            yield return new TestCaseData(2..4);
            yield return new TestCaseData(4..0);
            yield return new TestCaseData(0..^0);
            yield return new TestCaseData(0..^5);
            yield return new TestCaseData(^0..^5);
            yield return new TestCaseData(^5..^0);
            yield return new TestCaseData(^5..^2);
            yield return new TestCaseData(^5..0);
            yield return new TestCaseData(^5..4);
        }

        [Test, TestCaseSource(nameof(TakeRangeArgs1))]
        public void Take_Range_1(Range range)
        {
            Promise.Run(async () =>
            {
                var xs = new[] { -2, -1, 0, 1, 2, 3 };
                var asyncEnumerator = xs.ToAsyncEnumerable().Take(range).GetAsyncEnumerator();
                foreach (var x in xs.Take(range))
                {
                    Assert.True(await asyncEnumerator.MoveNextAsync());
                    Assert.AreEqual(x, asyncEnumerator.Current);
                }
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        public static IEnumerable<TestCaseData> TakeRangeArgs2()
        {
            IEnumerable<Range> GetSmallRangeArgs()
            {
                yield return 2..1;
                yield return 0..2;
                yield return 2..5;
                yield return ^2..^0;
                yield return ^5..^2;
                yield return ^2..2;
                yield return 0..^0;
                yield return 0..^2;
            }

            foreach (var range1 in GetSmallRangeArgs())
            {
                foreach (var range2 in GetSmallRangeArgs())
                {
                    yield return new TestCaseData(range1, range2);
                }
            }
        }


        [Test, TestCaseSource(nameof(TakeRangeArgs2))]
        public void Take_Range_2(Range range1, Range range2)
        {
            Promise.Run(async () =>
            {
                var xs = new[] { -2, -1, 0, 1, 2, 3 };
                var asyncEnumerator = xs.ToAsyncEnumerable().Take(range1).Take(range2).GetAsyncEnumerator();
                foreach (var x in xs.Take(range1).Take(range2))
                {
                    Assert.True(await asyncEnumerator.MoveNextAsync());
                    Assert.AreEqual(x, asyncEnumerator.Current);
                }
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }
#endif // NET6_0_OR_GREATER
    }
}