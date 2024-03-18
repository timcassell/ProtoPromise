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
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace ProtoPromiseTests.APIs.Linq
{
    public class ToHashSetAsyncTests
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
        public void ToHashSet_Empty()
        {
            Promise.Run(async () =>
            {
                var res = AsyncEnumerable<int>.Empty().ToHashSetAsync();
                Assert.Zero((await res).Count);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void ToHashSet_FromArray()
        {
            Promise.Run(async () =>
            {
                var xs = new[] { 42, 25, 39 };
                var res = xs.ToAsyncEnumerable().ToHashSetAsync();
                CollectionAssert.AreEqual(new HashSet<int>(xs), await res);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void ToHashSet_FromIterator()
        {
            Promise.Run(async () =>
            {
                var xs = AsyncEnumerable<int>.Create(async (writer, cancelationToken) =>
                {
                    await writer.YieldAsync(42);
                    await writer.YieldAsync(25);
                    await writer.YieldAsync(39);
                });
                var res = xs.ToHashSetAsync();
                CollectionAssert.AreEqual(new HashSet<int>() { 42, 25, 39 }, await res);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void ToHashSet_Throw()
        {
            Promise.Run(async () =>
            {
                var ex = new Exception("Bang!");
                var res = AsyncEnumerable<int>.Rejected(ex).ToHashSetAsync();
                await TestHelper.AssertThrowsAsync(() => res, ex);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void ToHashSet_Query()
        {
            Promise.Run(async () =>
            {
                var xs = await AsyncEnumerable.Range(5, 50).Take(10).ToHashSetAsync();
                var ex = new HashSet<int>() { 5, 6, 7, 8, 9, 10, 11, 12, 13, 14 };
                CollectionAssert.AreEqual(ex, xs);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void ToHashSet_Empty_Comparer()
        {
            Promise.Run(async () =>
            {
                var res = AsyncEnumerable<int>.Empty().ToHashSetAsync(new AbsComparer());
                Assert.Zero((await res).Count);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void ToHashSet_FromArray_Comparer()
        {
            Promise.Run(async () =>
            {
                var xs = new[] { -42, 42, 25, 39, -25 };
                var res = xs.ToAsyncEnumerable().ToHashSetAsync(new AbsComparer());
                CollectionAssert.AreEqual(new HashSet<int>(xs, new AbsComparer()), await res);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void ToHashSet_FromIterator_Comparer()
        {
            Promise.Run(async () =>
            {
                var xs = AsyncEnumerable<int>.Create(async (writer, cancelationToken) =>
                {
                    await writer.YieldAsync(-42);
                    await writer.YieldAsync(42);
                    await writer.YieldAsync(25);
                    await writer.YieldAsync(39);
                    await writer.YieldAsync(-25);
                });
                var res = xs.ToHashSetAsync(new AbsComparer());
                CollectionAssert.AreEqual(new HashSet<int>(new AbsComparer()) { -42, 25, 39 }, await res);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void ToHashSet_Throw_Comparer()
        {
            Promise.Run(async () =>
            {
                var ex = new Exception("Bang!");
                var res = AsyncEnumerable<int>.Rejected(ex).ToHashSetAsync(new AbsComparer());
                await TestHelper.AssertThrowsAsync(() => res, ex);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void ToHashSet_Query_Comparer()
        {
            Promise.Run(async () =>
            {
                var xs = await AsyncEnumerable.Range(5, 50).Select(x => x - 10).Take(10).ToHashSetAsync(new AbsComparer());
                var ex = new HashSet<int>(new AbsComparer()) { -5, -4, -3, -2, -1, 0 };
                CollectionAssert.AreEqual(ex, xs);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        private sealed class AbsComparer : IEqualityComparer<int>
        {
            public bool Equals(int x, int y)
                => EqualityComparer<int>.Default.Equals(Math.Abs(x), Math.Abs(y));

            public int GetHashCode(int obj)
                => EqualityComparer<int>.Default.GetHashCode(Math.Abs(obj));
        }

        [Test]
        public void ToHashSet_Cancel(
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var deferred = Promise.NewDeferred();
                var xs = AsyncEnumerable.Create<int>(async (writer, cancelationToken) =>
                {
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(0);
                    await deferred.Promise;
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(2);
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(4);
                });
                using (var cancelationSource = CancelationSource.New())
                {
                    var res = withComparer
                        ? xs.ToHashSetAsync(new AbsComparer(), cancelationSource.Token)
                        : xs.ToHashSetAsync(cancelationSource.Token);
                    cancelationSource.Cancel();
                    deferred.Resolve();
                    await TestHelper.AssertCanceledAsync(() => res);
                }
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }
    }
}