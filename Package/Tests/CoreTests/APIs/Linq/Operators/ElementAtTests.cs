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
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace ProtoPromiseTests.APIs.Linq
{
    public class ElementAtTests
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
        public void ElementAt_InvalidArgument()
        {
            var enumerable = AsyncEnumerable.Range(0, 10);

            Assert.Catch<System.ArgumentOutOfRangeException>(() => enumerable.ElementAtAsync(-1));

#if NETSTANDARD2_1_OR_GREATER || UNITY_2021_2_OR_NEWER || NETCOREAPP3_0_OR_GREATER
            Assert.Catch<System.ArgumentOutOfRangeException>(() => enumerable.ElementAtAsync(^0));
#endif

            enumerable.GetAsyncEnumerator().DisposeAsync().Forget();
        }
#endif

        [Test]
        public void ElementAtAsync_Empty_Index0()
        {
            Promise.Run(async () =>
            {
                var res = AsyncEnumerable.Empty<int>().ElementAtAsync(0);
                await TestHelper.AssertThrowsAsync<System.ArgumentOutOfRangeException>(() => res);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void ElementAtAsync_Single_Index0()
        {
            Promise.Run(async () =>
            {
                var res = AsyncEnumerable.Return(42).ElementAtAsync(0);
                Assert.AreEqual(42, await res);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void ElementAtAsync_Single_Index1()
        {
            Promise.Run(async () =>
            {
                var res = AsyncEnumerable.Return(42).ElementAtAsync(1);
                await TestHelper.AssertThrowsAsync<System.ArgumentOutOfRangeException>(() => res);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void ElementAtAsync_Many_InRange()
        {
            Promise.Run(async () =>
            {
                var res = new[] { 1, 42, 3 }.ToAsyncEnumerable().ElementAtAsync(1);
                Assert.AreEqual(42, await res);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void ElementAtAsync_Many_OutOfRange()
        {
            Promise.Run(async () =>
            {
                var res = new[] { 1, 42, 3 }.ToAsyncEnumerable().ElementAtAsync(7);
                await TestHelper.AssertThrowsAsync<System.ArgumentOutOfRangeException>(() => res);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void ElementAtAsync_Throws_Source()
        {
            Promise.Run(async () =>
            {
                var ex = new Exception("Bang!");
                var res = AsyncEnumerable<int>.Rejected(ex).ElementAtAsync(15);
                await TestHelper.AssertThrowsAsync(() => res, ex);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void ElementAtOrDefaultAsync_Empty_Index0()
        {
            Promise.Run(async () =>
            {
                var res = AsyncEnumerable.Empty<int>().ElementAtOrDefaultAsync(0);
                Assert.AreEqual(0, await res);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void ElementAtOrDefaultAsync_Single_Index0()
        {
            Promise.Run(async () =>
            {
                var res = AsyncEnumerable.Return(42).ElementAtOrDefaultAsync(0);
                Assert.AreEqual(42, await res);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void ElementAtOrDefaultAsync_Single_Index1()
        {
            Promise.Run(async () =>
            {
                var res = AsyncEnumerable.Return(42).ElementAtOrDefaultAsync(1);
                Assert.AreEqual(0, await res);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void ElementAtOrDefaultAsync_Many_InRange()
        {
            Promise.Run(async () =>
            {
                var res = new[] { 1, 42, 3 }.ToAsyncEnumerable().ElementAtOrDefaultAsync(1);
                Assert.AreEqual(42, await res);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void ElementAtOrDefaultAsync_Many_OutOfRange()
        {
            Promise.Run(async () =>
            {
                var res = new[] { 1, 42, 3 }.ToAsyncEnumerable().ElementAtOrDefaultAsync(7);
                Assert.AreEqual(0, await res);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void ElementAtOrDefaultAsync_Throws_Source()
        {
            Promise.Run(async () =>
            {
                var ex = new Exception("Bang!");
                var res = AsyncEnumerable<int>.Rejected(ex).ElementAtOrDefaultAsync(15);
                await TestHelper.AssertThrowsAsync(() => res, ex);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void ElementAt_Cancel()
        {
            Promise.Run(async () =>
            {
                var deferred = Promise.NewDeferred();
                var xs = AsyncEnumerable.Create<int>(async (writer, cancelationToken) =>
                {
                    await deferred.Promise;
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(1);
                    await deferred.Promise;
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(2);
                    await deferred.Promise;
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(3);
                });
                using (var cancelationSource = CancelationSource.New())
                {
                    var res = xs.ElementAtAsync(2, cancelationSource.Token);
                    cancelationSource.Cancel();
                    deferred.Resolve();
                    await TestHelper.AssertCanceledAsync(() => res);
                }
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void ElementAtOrDefault_Cancel()
        {
            Promise.Run(async () =>
            {
                var deferred = Promise.NewDeferred();
                var xs = AsyncEnumerable.Create<int>(async (writer, cancelationToken) =>
                {
                    await deferred.Promise;
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(1);
                    await deferred.Promise;
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(2);
                    await deferred.Promise;
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(3);
                });
                using (var cancelationSource = CancelationSource.New())
                {
                    var res = xs.ElementAtOrDefaultAsync(2, cancelationSource.Token);
                    cancelationSource.Cancel();
                    deferred.Resolve();
                    await TestHelper.AssertCanceledAsync(() => res);
                }
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }


#if NETSTANDARD2_1_OR_GREATER || UNITY_2021_2_OR_NEWER || NETCOREAPP3_0_OR_GREATER
        [Test]
        public void ElementAtAsync_Empty_SystemIndex0(
            [Values] bool fromEnd)
        {
            Promise.Run(async () =>
            {
                var res = AsyncEnumerable.Empty<int>().ElementAtAsync(fromEnd ? ^1 : 0);
                await TestHelper.AssertThrowsAsync<System.ArgumentOutOfRangeException>(() => res);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void ElementAtAsync_Single_SystemIndex0(
            [Values] bool fromEnd)
        {
            Promise.Run(async () =>
            {
                var res = AsyncEnumerable.Return(42).ElementAtAsync(fromEnd ? ^1 : 0);
                Assert.AreEqual(42, await res);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void ElementAtAsync_Single_SystemIndex1(
            [Values] bool fromEnd)
        {
            Promise.Run(async () =>
            {
                var res = AsyncEnumerable.Return(42).ElementAtAsync(fromEnd ? ^2 : 1);
                await TestHelper.AssertThrowsAsync<System.ArgumentOutOfRangeException>(() => res);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void ElementAtAsync_Many_InRange_SystemIndex(
            [Values] bool fromEnd)
        {
            Promise.Run(async () =>
            {
                var res = new[] { 1, 42, 3 }.ToAsyncEnumerable().ElementAtAsync(fromEnd ? ^2 : 1);
                Assert.AreEqual(42, await res);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void ElementAtAsync_Many_OutOfRange_SystemIndex(
            [Values] bool fromEnd)
        {
            Promise.Run(async () =>
            {
                var res = new[] { 1, 42, 3 }.ToAsyncEnumerable().ElementAtAsync(new System.Index(7, fromEnd));
                await TestHelper.AssertThrowsAsync<System.ArgumentOutOfRangeException>(() => res);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void ElementAtAsync_Throws_Source_SystemIndex(
            [Values] bool fromEnd)
        {
            Promise.Run(async () =>
            {
                var ex = new Exception("Bang!");
                var res = AsyncEnumerable<int>.Rejected(ex).ElementAtAsync(new System.Index(15, fromEnd));
                await TestHelper.AssertThrowsAsync(() => res, ex);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void ElementAtOrDefaultAsync_Empty_SystemIndex0(
            [Values] bool fromEnd)
        {
            Promise.Run(async () =>
            {
                var res = AsyncEnumerable.Empty<int>().ElementAtOrDefaultAsync(fromEnd ? ^1 : 0);
                Assert.AreEqual(0, await res);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void ElementAtOrDefaultAsync_Single_SystemIndex0(
            [Values] bool fromEnd)
        {
            Promise.Run(async () =>
            {
                var res = AsyncEnumerable.Return(42).ElementAtOrDefaultAsync(fromEnd ? ^1 : 0);
                Assert.AreEqual(42, await res);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void ElementAtOrDefaultAsync_Single_SystemIndex1(
            [Values] bool fromEnd)
        {
            Promise.Run(async () =>
            {
                var res = AsyncEnumerable.Return(42).ElementAtOrDefaultAsync(fromEnd ? ^2 : 1);
                Assert.AreEqual(0, await res);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void ElementAtOrDefaultAsync_Many_InRange_SystemIndex(
            [Values] bool fromEnd)
        {
            Promise.Run(async () =>
            {
                var res = new[] { 1, 42, 3 }.ToAsyncEnumerable().ElementAtOrDefaultAsync(fromEnd ? ^2 : 1);
                Assert.AreEqual(42, await res);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void ElementAtOrDefaultAsync_Many_OutOfRange_SystemIndex(
            [Values] bool fromEnd)
        {
            Promise.Run(async () =>
            {
                var res = new[] { 1, 42, 3 }.ToAsyncEnumerable().ElementAtOrDefaultAsync(new System.Index(7, fromEnd));
                Assert.AreEqual(0, await res);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void ElementAtOrDefaultAsync_Throws_Source_SystemIndex(
            [Values] bool fromEnd)
        {
            Promise.Run(async () =>
            {
                var ex = new Exception("Bang!");
                var res = AsyncEnumerable<int>.Rejected(ex).ElementAtOrDefaultAsync(new System.Index(15, fromEnd));
                await TestHelper.AssertThrowsAsync(() => res, ex);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void ElementAt_SystemIndex_Cancel(
            [Values] bool fromEnd)
        {
            Promise.Run(async () =>
            {
                var deferred = Promise.NewDeferred();
                var xs = AsyncEnumerable.Create<int>(async (writer, cancelationToken) =>
                {
                    await deferred.Promise;
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(1);
                    await deferred.Promise;
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(2);
                    await deferred.Promise;
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(3);
                });
                using (var cancelationSource = CancelationSource.New())
                {
                    var res = xs.ElementAtAsync(new System.Index(2, fromEnd), cancelationSource.Token);
                    cancelationSource.Cancel();
                    deferred.Resolve();
                    await TestHelper.AssertCanceledAsync(() => res);
                }
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void ElementAtOrDefault_SystemIndex_Cancel(
            [Values] bool fromEnd)
        {
            Promise.Run(async () =>
            {
                var deferred = Promise.NewDeferred();
                var xs = AsyncEnumerable.Create<int>(async (writer, cancelationToken) =>
                {
                    await deferred.Promise;
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(1);
                    await deferred.Promise;
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(2);
                    await deferred.Promise;
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(3);
                });
                using (var cancelationSource = CancelationSource.New())
                {
                    var res = xs.ElementAtOrDefaultAsync(new System.Index(2, fromEnd), cancelationSource.Token);
                    cancelationSource.Cancel();
                    deferred.Resolve();
                    await TestHelper.AssertCanceledAsync(() => res);
                }
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }
#endif
    }
}

#endif