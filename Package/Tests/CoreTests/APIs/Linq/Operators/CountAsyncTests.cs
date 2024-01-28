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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace ProtoPromiseTests.APIs.Linq
{
    public class CountAsyncTests
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
        public void CountAsync_NullArgumentThrows()
        {
            var enumerable = AsyncEnumerable.Return(42);
            var captureValue = "captureValue";

            Assert.Catch<System.ArgumentNullException>(() => enumerable.CountAsync(default(Func<int, bool>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.CountAsync(captureValue, default(Func<string, int, bool>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.CountAsync(default(Func<int, Promise<bool>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.CountAsync(captureValue, default(Func<string, int, Promise<bool>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).CountAsync(default(Func<int, bool>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).CountAsync(captureValue, default(Func<string, int, bool>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).CountAsync(default(Func<int, Promise<bool>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).CountAsync(captureValue, default(Func<string, int, Promise<bool>>)));

            enumerable.GetAsyncEnumerator().DisposeAsync().Forget();
        }
#endif //PROMISE_DEBUG

        // We test all the different overloads.
        private static Promise<int> CountAsync(AsyncEnumerable<int> source,
            bool configured,
            bool async,
            bool captureValue,
            Func<int, bool> predicate,
            CancelationToken cancelationToken = default)
        {
            if (configured)
            {
                return CountAsync(source.ConfigureAwait(SynchronizationOption.Foreground).WithCancelation(cancelationToken), async, captureValue, predicate);
            }

            const string capturedValue = "capturedValue";

            if (!captureValue)
            {
                return async
                    ? source.CountAsync(async x => predicate(x), cancelationToken)
                    : source.CountAsync(predicate, cancelationToken);
            }
            else
            {
                return async
                    ? source.CountAsync(capturedValue, async (cv, x) =>
                    {
                        Assert.AreEqual(capturedValue, cv);
                        return predicate(x);
                    }, cancelationToken)
                    : source.CountAsync(capturedValue, (cv, x) =>
                    {
                        Assert.AreEqual(capturedValue, cv);
                        return predicate(x);
                    }, cancelationToken);
            }
        }

        private static Promise<int> CountAsync(ConfiguredAsyncEnumerable<int> source,
            bool async,
            bool captureValue,
            Func<int, bool> predicate)
        {
            const string capturedValue = "capturedValue";

            if (!captureValue)
            {
                return async
                    ? source.CountAsync(async x => predicate(x))
                    : source.CountAsync(predicate);
            }
            else
            {
                return async
                    ? source.CountAsync(capturedValue, async (cv, x) =>
                    {
                        Assert.AreEqual(capturedValue, cv);
                        return predicate(x);
                    })
                    : source.CountAsync(capturedValue, (cv, x) =>
                    {
                        Assert.AreEqual(capturedValue, cv);
                        return predicate(x);
                    });
            }
        }

        [Test]
        public void CountAsync_Simple()
        {
            Promise.Run(async () =>
            {
                Assert.AreEqual(0, await new int[0].ToAsyncEnumerable().CountAsync());
                Assert.AreEqual(3, await new[] { 1, 2, 3 }.ToAsyncEnumerable().CountAsync());
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void CountAsync_Simple_Throws_Source()
        {
            Promise.Run(async () =>
            {
                var ex = new Exception("Bang!");
                await TestHelper.AssertThrowsAsync(() => AsyncEnumerable<int>.Rejected(ex).CountAsync(), ex);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void CountAsync_Predicate(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureValue)
        {
            Promise.Run(async () =>
            {
                Assert.AreEqual(0, await CountAsync(new int[0].ToAsyncEnumerable(), configured, async, captureValue, x => x < 3));
                Assert.AreEqual(2, await CountAsync(new[] { 1, 2, 3 }.ToAsyncEnumerable(), configured, async, captureValue, x => x < 3));
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void CountAsync_Predicate_Throws_Predicate(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureValue)
        {
            Promise.Run(async () =>
            {
                var ex = new Exception("Bang!");
                await TestHelper.AssertThrowsAsync(() => CountAsync(new[] { 1, 2, 3 }.ToAsyncEnumerable(), configured, async, captureValue, x => { throw ex; }), ex);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void CountAsync_Predicate_Throws_Source(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureValue)
        {
            Promise.Run(async () =>
            {
                var ex = new Exception("Bang!");
                await TestHelper.AssertThrowsAsync(() => CountAsync(AsyncEnumerable<int>.Rejected(ex), configured, async, captureValue, x => x < 3), ex);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void CountAsync_Simple_Cancel()
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
                    var res = xs.CountAsync(cancelationSource.Token);
                    var def = deferred;
                    deferred = Promise.NewDeferred();
                    def.Resolve();
                    cancelationSource.Cancel();
                    deferred.Resolve();
                    await TestHelper.AssertCanceledAsync(() => res);
                }
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void CountAsync_Predicate_Cancel(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureValue)
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
                    var res = CountAsync(xs, configured, async, captureValue, x => x < 3, cancelationSource.Token);
                    var def = deferred;
                    deferred = Promise.NewDeferred();
                    def.Resolve();
                    cancelationSource.Cancel();
                    deferred.Resolve();
                    await TestHelper.AssertCanceledAsync(() => res);
                }
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }
    }
}

#endif