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
    public class SequenceEqualAsyncTests
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
        public void SequenceEqualAsync_NullArgumentThrows()
        {
            var first = AsyncEnumerable.Return(42);
            var second = AsyncEnumerable.Return(42);

            Assert.Catch<System.ArgumentNullException>(() => first.SequenceEqualAsync(second, default(IEqualityComparer<int>)));
            Assert.Catch<System.ArgumentNullException>(() => first.ConfigureAwait(SynchronizationOption.Synchronous).SequenceEqualAsync(second, default(IEqualityComparer<int>)));

            first.GetAsyncEnumerator().DisposeAsync().Forget();
            second.GetAsyncEnumerator().DisposeAsync().Forget();
        }
#endif //PROMISE_DEBUG

        // We test all the different overloads.
        private static Promise<bool> SequenceEqualAsync<TSource>(AsyncEnumerable<TSource> first,
            AsyncEnumerable<TSource> second,
            bool configured,
            IEqualityComparer<TSource> equalityComparer = null,
            CancelationToken cancelationToken = default)
        {
            return configured
                ? equalityComparer != null
                    ? first.ConfigureAwait(SynchronizationOption.Foreground).WithCancelation(cancelationToken).SequenceEqualAsync(second, equalityComparer)
                    : first.ConfigureAwait(SynchronizationOption.Foreground).WithCancelation(cancelationToken).SequenceEqualAsync(second)
                : equalityComparer != null
                    ? first.SequenceEqualAsync(second, equalityComparer, cancelationToken)
                    : first.SequenceEqualAsync(second, cancelationToken);
        }

        private static IEqualityComparer<T> GetDefaultOrNullComparer<T>(bool defaultComparer)
        {
            return defaultComparer ? EqualityComparer<T>.Default : null;
        }

        [Test]
        public void SequenceEqualAsync_Same_Empty(
            [Values] bool configured,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var res = SequenceEqualAsync(AsyncEnumerable.Empty<int>(), AsyncEnumerable.Empty<int>(), configured, GetDefaultOrNullComparer<int>(withComparer));
                Assert.True(await res);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void SequenceEqualAsync_Same_Many(
            [Values] bool configured,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var xs = new[] { 1, 2, 3 };
                var res = SequenceEqualAsync(xs.ToAsyncEnumerable(), xs.ToAsyncEnumerable(), configured, GetDefaultOrNullComparer<int>(withComparer));
                Assert.True(await res);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void SequenceEqualAsync_NotSame_Empty_Many(
            [Values] bool configured,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var xs = new[] { 1, 2, 3 };
                var res = SequenceEqualAsync(AsyncEnumerable.Empty<int>(), xs.ToAsyncEnumerable(), configured, GetDefaultOrNullComparer<int>(withComparer));
                Assert.False(await res);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void SequenceEqualAsync_NotSame_Many_Empty(
            [Values] bool configured,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var xs = new[] { 1, 2, 3 };
                var res = SequenceEqualAsync(xs.ToAsyncEnumerable(), AsyncEnumerable.Empty<int>(), configured, GetDefaultOrNullComparer<int>(withComparer));
                Assert.False(await res);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void SequenceEqualAsync_NotSame_SameLength(
            [Values] bool configured,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var res = SequenceEqualAsync(new[] { 1, 2, 3 }.ToAsyncEnumerable(), new[] { 3, 2, 1 }.ToAsyncEnumerable(), configured, GetDefaultOrNullComparer<int>(withComparer));
                Assert.False(await res);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void SequenceEqualAsync_NotSame_DifferentLength(
            [Values] bool configured,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var res = SequenceEqualAsync(new[] { 1, 2, 3 }.ToAsyncEnumerable(), new[] { 1, 2, 3, 4 }.ToAsyncEnumerable(), configured, GetDefaultOrNullComparer<int>(withComparer));
                Assert.False(await res);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void SequenceEqualAsync_Same_Empty_Comparer(
            [Values] bool configured,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var res = SequenceEqualAsync(AsyncEnumerable.Empty<int>(), AsyncEnumerable.Empty<int>(), configured, new AbsComparer());
                Assert.True(await res);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void SequenceEqualAsync_Same_Many_Comparer1(
            [Values] bool configured,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var xs = new[] { 1, 2, 3 };
                var res = SequenceEqualAsync(xs.ToAsyncEnumerable(), xs.ToAsyncEnumerable(), configured, new AbsComparer());
                Assert.True(await res);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void SequenceEqualAsync_Same_Many_Comparer2(
            [Values] bool configured,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var xs1 = new[] { 1, 2, -3, 4 };
                var xs2 = new[] { 1, -2, 3, 4 };
                var res = SequenceEqualAsync(xs1.ToAsyncEnumerable(), xs2.ToAsyncEnumerable(), configured, new AbsComparer());
                Assert.True(await res);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void SequenceEqualAsync_NotSame_Empty_Many_Comparer(
            [Values] bool configured,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var xs = new[] { 1, 2, 3 };
                var res = SequenceEqualAsync(AsyncEnumerable.Empty<int>(), xs.ToAsyncEnumerable(), configured, new AbsComparer());
                Assert.False(await res);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void SequenceEqualAsync_NotSame_Many_Empty_Comparer(
            [Values] bool configured,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var xs = new[] { 1, 2, 3 };
                var res = SequenceEqualAsync(xs.ToAsyncEnumerable(), AsyncEnumerable.Empty<int>(), configured, new AbsComparer());
                Assert.False(await res);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void SequenceEqualAsync_NotSame_SameLength_Comparer(
            [Values] bool configured,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var res = SequenceEqualAsync(new[] { 1, 2, 3 }.ToAsyncEnumerable(), new[] { 3, 2, 1 }.ToAsyncEnumerable(), configured, new AbsComparer());
                Assert.False(await res);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void SequenceEqualAsync_NotSame_DifferentLength_Comparer(
            [Values] bool configured,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var res = SequenceEqualAsync(new[] { 1, 2, 3 }.ToAsyncEnumerable(), new[] { 1, 2, 3, 4 }.ToAsyncEnumerable(), configured, new AbsComparer());
                Assert.False(await res);
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

        public enum ConfiguredType
        {
            NotConfigured,
            Configured,
            ConfiguredWithCancelation
        }

        [Test]
        public void SequenceEqualAsync_Cancel(
            [Values] ConfiguredType configuredType,
            [Values] bool withComparer,
            [Values] bool cancelFirst)
        {
            Promise.Run(async () =>
            {
                using (var cancelationSource = CancelationSource.New())
                {
                    var xs1 = AsyncEnumerable.Create<int>(async (writer, cancelationToken) =>
                    {
                        cancelationToken.ThrowIfCancelationRequested();
                        if (!cancelFirst)
                        {
                            cancelationSource.Cancel();
                        }
                        await writer.YieldAsync(0);
                        cancelationToken.ThrowIfCancelationRequested();
                        await writer.YieldAsync(1);
                        cancelationToken.ThrowIfCancelationRequested();
                        await writer.YieldAsync(2);
                    });
                    var xs2 = AsyncEnumerable.Create<int>(async (writer, cancelationToken) =>
                    {
                        cancelationToken.ThrowIfCancelationRequested();
                        if (cancelFirst)
                        {
                            cancelationSource.Cancel();
                        }
                        await writer.YieldAsync(0);
                        cancelationToken.ThrowIfCancelationRequested();
                        await writer.YieldAsync(1);
                        cancelationToken.ThrowIfCancelationRequested();
                        await writer.YieldAsync(2);
                    });
                    var res = SequenceEqualAsync(xs1, xs2, configuredType != ConfiguredType.NotConfigured, GetDefaultOrNullComparer<int>(withComparer), cancelationSource.Token);
                    await TestHelper.AssertCanceledAsync(() => res);
                }
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }
    }
}

#endif