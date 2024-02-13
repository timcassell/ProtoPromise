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
    public class AverageAsyncTests
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
        public void AverageAsync_Int32_Empty()
        {
            Promise.Run(async () =>
            {
                var ys = new int[0].ToAsyncEnumerable();
                await TestHelper.AssertThrowsAsync<System.InvalidOperationException>(() => ys.AverageAsync());
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AverageAsync_Int32_Many()
        {
            Promise.Run(async () =>
            {
                var xs = new int[] { 2, 3, 5, 7, 11, 13, 17, 19 };
                var ys = xs.ToAsyncEnumerable();
                Assert.AreEqual(xs.Average(), await ys.AverageAsync());
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AverageAsync_Int32_Nullable_Empty()
        {
            Promise.Run(async () =>
            {
                var ys = new int?[0].ToAsyncEnumerable();
                Assert.IsNull(await ys.AverageAsync());
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AverageAsync_Int32_Nullable_Many()
        {
            Promise.Run(async () =>
            {
                var xs = new int?[] { 2, 3, 5, 7, null, 11, 13, 17, null, 19 };
                var ys = xs.ToAsyncEnumerable();
                Assert.AreEqual(xs.Average(), await ys.AverageAsync());
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AverageAsync_Int64_Empty()
        {
            Promise.Run(async () =>
            {
                var ys = new long[0].ToAsyncEnumerable();
                await TestHelper.AssertThrowsAsync<System.InvalidOperationException>(() => ys.AverageAsync());
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AverageAsync_Int64_Many()
        {
            Promise.Run(async () =>
            {
                var xs = new long[] { 2, 3, 5, 7, 11, 13, 17, 19 };
                var ys = xs.ToAsyncEnumerable();
                Assert.AreEqual(xs.Average(), await ys.AverageAsync());
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AverageAsync_Int64_Nullable_Empty()
        {
            Promise.Run(async () =>
            {
                var ys = new long?[0].ToAsyncEnumerable();
                Assert.IsNull(await ys.AverageAsync());
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AverageAsync_Int64_Nullable_Many()
        {
            Promise.Run(async () =>
            {
                var xs = new long?[] { 2, 3, 5, 7, null, 11, 13, 17, null, 19 };
                var ys = xs.ToAsyncEnumerable();
                Assert.AreEqual(xs.Average(), await ys.AverageAsync());
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AverageAsync_Single_Empty()
        {
            Promise.Run(async () =>
            {
                var ys = new float[0].ToAsyncEnumerable();
                await TestHelper.AssertThrowsAsync<System.InvalidOperationException>(() => ys.AverageAsync());
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AverageAsync_Single_Many()
        {
            Promise.Run(async () =>
            {
                var xs = new float[] { 2, 3, 5, 7, 11, 13, 17, 19 };
                var ys = xs.ToAsyncEnumerable();
                Assert.AreEqual(xs.Average(), await ys.AverageAsync());
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AverageAsync_Single_Nullable_Empty()
        {
            Promise.Run(async () =>
            {
                var ys = new float?[0].ToAsyncEnumerable();
                Assert.IsNull(await ys.AverageAsync());
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AverageAsync_Single_Nullable_Many()
        {
            Promise.Run(async () =>
            {
                var xs = new float?[] { 2, 3, 5, 7, null, 11, 13, 17, null, 19 };
                var ys = xs.ToAsyncEnumerable();
                Assert.AreEqual(xs.Average(), await ys.AverageAsync());
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AverageAsync_Double_Empty()
        {
            Promise.Run(async () =>
            {
                var ys = new double[0].ToAsyncEnumerable();
                await TestHelper.AssertThrowsAsync<System.InvalidOperationException>(() => ys.AverageAsync());
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AverageAsync_Double_Many()
        {
            Promise.Run(async () =>
            {
                var xs = new double[] { 2, 3, 5, 7, 11, 13, 17, 19 };
                var ys = xs.ToAsyncEnumerable();
                Assert.AreEqual(xs.Average(), await ys.AverageAsync());
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AverageAsync_Double_Nullable_Empty()
        {
            Promise.Run(async () =>
            {
                var ys = new double?[0].ToAsyncEnumerable();
                Assert.IsNull(await ys.AverageAsync());
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AverageAsync_Double_Nullable_Many()
        {
            Promise.Run(async () =>
            {
                var xs = new double?[] { 2, 3, 5, 7, null, 11, 13, 17, null, 19 };
                var ys = xs.ToAsyncEnumerable();
                Assert.AreEqual(xs.Average(), await ys.AverageAsync());
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AverageAsync_Decimal_Empty()
        {
            Promise.Run(async () =>
            {
                var ys = new decimal[0].ToAsyncEnumerable();
                await TestHelper.AssertThrowsAsync<System.InvalidOperationException>(() => ys.AverageAsync());
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AverageAsync_Decimal_Many()
        {
            Promise.Run(async () =>
            {
                var xs = new decimal[] { 2, 3, 5, 7, 11, 13, 17, 19 };
                var ys = xs.ToAsyncEnumerable();
                Assert.AreEqual(xs.Average(), await ys.AverageAsync());
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AverageAsync_Decimal_Nullable_Empty()
        {
            Promise.Run(async () =>
            {
                var ys = new decimal?[0].ToAsyncEnumerable();
                Assert.IsNull(await ys.AverageAsync());
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AverageAsync_Decimal_Nullable_Many()
        {
            Promise.Run(async () =>
            {
                var xs = new decimal?[] { 2, 3, 5, 7, null, 11, 13, 17, null, 19 };
                var ys = xs.ToAsyncEnumerable();
                Assert.AreEqual(xs.Average(), await ys.AverageAsync());
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AverageAsync_Int32_Cancel()
        {
            Promise.Run(async () =>
            {
                var deferred = Promise.NewDeferred();
                var xs = AsyncEnumerable.Create<int>(async (writer, cancelationToken) =>
                {
                    await deferred.Promise;
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(0);
                    await deferred.Promise;
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(2);
                    await deferred.Promise;
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(4);
                });
                using (var cancelationSource = CancelationSource.New())
                {
                    var ys = xs.AverageAsync(cancelationSource.Token);
                    var def = deferred;
                    deferred = Promise.NewDeferred();
                    def.Resolve();
                    cancelationSource.Cancel();
                    deferred.Resolve();
                    await TestHelper.AssertCanceledAsync(() => ys);
                }
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AverageAsync_Int64_Cancel()
        {
            Promise.Run(async () =>
            {
                var deferred = Promise.NewDeferred();
                var xs = AsyncEnumerable.Create<long>(async (writer, cancelationToken) =>
                {
                    await deferred.Promise;
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(0);
                    await deferred.Promise;
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(2);
                    await deferred.Promise;
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(4);
                });
                using (var cancelationSource = CancelationSource.New())
                {
                    var ys = xs.AverageAsync(cancelationSource.Token);
                    var def = deferred;
                    deferred = Promise.NewDeferred();
                    def.Resolve();
                    cancelationSource.Cancel();
                    deferred.Resolve();
                    await TestHelper.AssertCanceledAsync(() => ys);
                }
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AverageAsync_Single_Cancel()
        {
            Promise.Run(async () =>
            {
                var deferred = Promise.NewDeferred();
                var xs = AsyncEnumerable.Create<float>(async (writer, cancelationToken) =>
                {
                    await deferred.Promise;
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(0);
                    await deferred.Promise;
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(2);
                    await deferred.Promise;
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(4);
                });
                using (var cancelationSource = CancelationSource.New())
                {
                    var ys = xs.AverageAsync(cancelationSource.Token);
                    var def = deferred;
                    deferred = Promise.NewDeferred();
                    def.Resolve();
                    cancelationSource.Cancel();
                    deferred.Resolve();
                    await TestHelper.AssertCanceledAsync(() => ys);
                }
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AverageAsync_Double_Cancel()
        {
            Promise.Run(async () =>
            {
                var deferred = Promise.NewDeferred();
                var xs = AsyncEnumerable.Create<double>(async (writer, cancelationToken) =>
                {
                    await deferred.Promise;
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(0);
                    await deferred.Promise;
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(2);
                    await deferred.Promise;
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(4);
                });
                using (var cancelationSource = CancelationSource.New())
                {
                    var ys = xs.AverageAsync(cancelationSource.Token);
                    var def = deferred;
                    deferred = Promise.NewDeferred();
                    def.Resolve();
                    cancelationSource.Cancel();
                    deferred.Resolve();
                    await TestHelper.AssertCanceledAsync(() => ys);
                }
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AverageAsync_Decimal_Cancel()
        {
            Promise.Run(async () =>
            {
                var deferred = Promise.NewDeferred();
                var xs = AsyncEnumerable.Create<decimal>(async (writer, cancelationToken) =>
                {
                    await deferred.Promise;
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(0);
                    await deferred.Promise;
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(2);
                    await deferred.Promise;
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(4);
                });
                using (var cancelationSource = CancelationSource.New())
                {
                    var ys = xs.AverageAsync(cancelationSource.Token);
                    var def = deferred;
                    deferred = Promise.NewDeferred();
                    def.Resolve();
                    cancelationSource.Cancel();
                    deferred.Resolve();
                    await TestHelper.AssertCanceledAsync(() => ys);
                }
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AverageAsync_Int32_Nullable_Cancel()
        {
            Promise.Run(async () =>
            {
                var deferred = Promise.NewDeferred();
                var xs = AsyncEnumerable.Create<int?>(async (writer, cancelationToken) =>
                {
                    await deferred.Promise;
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(0);
                    await deferred.Promise;
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(2);
                    await deferred.Promise;
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(4);
                });
                using (var cancelationSource = CancelationSource.New())
                {
                    var ys = xs.AverageAsync(cancelationSource.Token);
                    var def = deferred;
                    deferred = Promise.NewDeferred();
                    def.Resolve();
                    cancelationSource.Cancel();
                    deferred.Resolve();
                    await TestHelper.AssertCanceledAsync(() => ys);
                }
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AverageAsync_Int64_Nullable_Cancel()
        {
            Promise.Run(async () =>
            {
                var deferred = Promise.NewDeferred();
                var xs = AsyncEnumerable.Create<long?>(async (writer, cancelationToken) =>
                {
                    await deferred.Promise;
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(0);
                    await deferred.Promise;
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(2);
                    await deferred.Promise;
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(4);
                });
                using (var cancelationSource = CancelationSource.New())
                {
                    var ys = xs.AverageAsync(cancelationSource.Token);
                    var def = deferred;
                    deferred = Promise.NewDeferred();
                    def.Resolve();
                    cancelationSource.Cancel();
                    deferred.Resolve();
                    await TestHelper.AssertCanceledAsync(() => ys);
                }
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AverageAsync_Single_Nullable_Cancel()
        {
            Promise.Run(async () =>
            {
                var deferred = Promise.NewDeferred();
                var xs = AsyncEnumerable.Create<float?>(async (writer, cancelationToken) =>
                {
                    await deferred.Promise;
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(0);
                    await deferred.Promise;
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(2);
                    await deferred.Promise;
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(4);
                });
                using (var cancelationSource = CancelationSource.New())
                {
                    var ys = xs.AverageAsync(cancelationSource.Token);
                    var def = deferred;
                    deferred = Promise.NewDeferred();
                    def.Resolve();
                    cancelationSource.Cancel();
                    deferred.Resolve();
                    await TestHelper.AssertCanceledAsync(() => ys);
                }
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AverageAsync_Double_Nullable_Cancel()
        {
            Promise.Run(async () =>
            {
                var deferred = Promise.NewDeferred();
                var xs = AsyncEnumerable.Create<double?>(async (writer, cancelationToken) =>
                {
                    await deferred.Promise;
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(0);
                    await deferred.Promise;
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(2);
                    await deferred.Promise;
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(4);
                });
                using (var cancelationSource = CancelationSource.New())
                {
                    var ys = xs.AverageAsync(cancelationSource.Token);
                    var def = deferred;
                    deferred = Promise.NewDeferred();
                    def.Resolve();
                    cancelationSource.Cancel();
                    deferred.Resolve();
                    await TestHelper.AssertCanceledAsync(() => ys);
                }
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AverageAsync_Decimal_Nullable_Cancel()
        {
            Promise.Run(async () =>
            {
                var deferred = Promise.NewDeferred();
                var xs = AsyncEnumerable.Create<decimal?>(async (writer, cancelationToken) =>
                {
                    await deferred.Promise;
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(0);
                    await deferred.Promise;
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(2);
                    await deferred.Promise;
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(4);
                });
                using (var cancelationSource = CancelationSource.New())
                {
                    var ys = xs.AverageAsync(cancelationSource.Token);
                    var def = deferred;
                    deferred = Promise.NewDeferred();
                    def.Resolve();
                    cancelationSource.Cancel();
                    deferred.Resolve();
                    await TestHelper.AssertCanceledAsync(() => ys);
                }
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }
    }
}