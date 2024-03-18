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
    public class FirstTests
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
        public void FirstAsync_NullArgument()
        {
            var enumerable = AsyncEnumerable.Range(0, 10);

            Assert.Catch<System.ArgumentNullException>(() => enumerable.FirstAsync(default(Func<int, bool>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.FirstAsync(default(Func<int, Promise<bool>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).FirstAsync(default(Func<int, bool>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).FirstAsync(default(Func<int, Promise<bool>>)));

            enumerable.GetAsyncEnumerator().DisposeAsync().Forget();
        }
#endif

        [Test]
        public void FirstAsync_NoParam_Empty()
        {
            Promise.Run(async () =>
            {
                var res = AsyncEnumerable.Empty<int>().FirstAsync();
                await TestHelper.AssertThrowsAsync<System.InvalidOperationException>(() => res);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void FirstAsync_NoParam_Throw()
        {
            Promise.Run(async () =>
            {
                var ex = new Exception("Bang!");
                var res = AsyncEnumerable<int>.Rejected(ex).FirstAsync();
                await TestHelper.AssertThrowsAsync(() => res, ex);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void FirstAsync_NoParam_Single()
        {
            Promise.Run(async () =>
            {
                var res = AsyncEnumerable.Return(42).FirstAsync();
                Assert.AreEqual(42, await res);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void FirstAsync_NoParam_Many()
        {
            Promise.Run(async () =>
            {
                var res = new[] { 42, 43, 44 }.ToAsyncEnumerable().FirstAsync();
                Assert.AreEqual(42, await res);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        // We test all the different overloads.
        private static Promise<TSource> FirstAsync<TSource>(AsyncEnumerable<TSource> source,
            bool configured,
            bool async,
            bool captureValue,
            Func<TSource, bool> predicate,
            CancelationToken cancelationToken = default)
        {
            if (configured)
            {
                return FirstAsync(source.ConfigureAwait(SynchronizationOption.Foreground).WithCancelation(cancelationToken), async, captureValue, predicate);
            }

            const string valueCapture = "valueCapture";

            if (!captureValue)
            {
                return async
                    ? source.FirstAsync(async x => predicate(x), cancelationToken)
                    : source.FirstAsync(predicate, cancelationToken);
            }
            else
            {
                return async
                    ? source.FirstAsync(valueCapture, async (cv, x) =>
                    {
                        Assert.AreEqual(valueCapture, cv);
                        return predicate(x);
                    }, cancelationToken)
                    : source.FirstAsync(valueCapture, (cv, x) =>
                    {
                        Assert.AreEqual(valueCapture, cv);
                        return predicate(x);
                    }, cancelationToken);
            }
        }

        private static Promise<TSource> FirstAsync<TSource>(ConfiguredAsyncEnumerable<TSource> source,
            bool async,
            bool captureValue,
            Func<TSource, bool> predicate)
        {
            const string valueCapture = "valueCapture";

            if (!captureValue)
            {
                return async
                    ? source.FirstAsync(async x => predicate(x))
                    : source.FirstAsync(predicate);
            }
            else
            {
                return async
                    ? source.FirstAsync(valueCapture, async (cv, x) =>
                    {
                        Assert.AreEqual(valueCapture, cv);
                        return predicate(x);
                    })
                    : source.FirstAsync(valueCapture, (cv, x) =>
                    {
                        Assert.AreEqual(valueCapture, cv);
                        return predicate(x);
                    });
            }
        }

        [Test]
        public void FirstAsync_Predicate_Empty(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureValue)
        {
            Promise.Run(async () =>
            {
                var res = FirstAsync(AsyncEnumerable.Empty<int>(), configured, async, captureValue, x => true);
                await TestHelper.AssertThrowsAsync<System.InvalidOperationException>(() => res);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void FirstAsync_Predicate_Throw(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureValue)
        {
            Promise.Run(async () =>
            {
                var ex = new Exception("Bang!");
                var res = FirstAsync(AsyncEnumerable<int>.Rejected(ex), configured, async, captureValue, x => true);
                await TestHelper.AssertThrowsAsync(() => res, ex);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void FirstAsync_Predicate_Single_None(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureValue)
        {
            Promise.Run(async () =>
            {
                var res = FirstAsync(AsyncEnumerable.Return(42), configured, async, captureValue, x => x % 2 != 0);
                await TestHelper.AssertThrowsAsync<System.InvalidOperationException>(() => res);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void FirstAsync_Predicate_Many_None(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureValue)
        {
            Promise.Run(async () =>
            {
                var res = FirstAsync(new[] { 40, 42, 44 }.ToAsyncEnumerable(), configured, async, captureValue, x => x % 2 != 0);
                await TestHelper.AssertThrowsAsync<System.InvalidOperationException>(() => res);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void FirstAsync_Predicate_Single_Pass(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureValue)
        {
            Promise.Run(async () =>
            {
                var res = FirstAsync(AsyncEnumerable.Return(42), configured, async, captureValue, x => x % 2 == 0);
                Assert.AreEqual(42, await res);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void FirstAsync_Predicate_Many_Pass1(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureValue)
        {
            Promise.Run(async () =>
            {
                var res = FirstAsync(new[] { 42, 43, 44 }.ToAsyncEnumerable(), configured, async, captureValue, x => x % 2 != 0);
                Assert.AreEqual(43, await res);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void FirstAsync_Predicate_Many_Pass2(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureValue)
        {
            Promise.Run(async () =>
            {
                var res = FirstAsync(new[] { 42, 45, 90 }.ToAsyncEnumerable(), configured, async, captureValue, x => x % 2 == 0);
                Assert.AreEqual(42, await res);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void FirstAsync_Predicate_PredicateThrows(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureValue)
        {
            Promise.Run(async () =>
            {
                // IL2CPP crashes on integer divide by zero, so throw the exception manually instead.
                var res = FirstAsync(new[] { 0, 1, 2 }.ToAsyncEnumerable(), configured, async, captureValue, x => { if (x == 0) throw new DivideByZeroException(); return 1 / x > 0; });
                await TestHelper.AssertThrowsAsync<DivideByZeroException>(() => res);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void FirstAsync_NoParam_Cancel()
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
                    var res = xs.FirstAsync(cancelationSource.Token);
                    cancelationSource.Cancel();
                    deferred.Resolve();
                    await TestHelper.AssertCanceledAsync(() => res);
                }
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void FirstAsync_Predicate_Cancel(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureValue)
        {
            Promise.Run(async () =>
            {
                var xs = AsyncEnumerable.Create<int>(async (writer, cancelationToken) =>
                {
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(0);
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(2);
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(4);
                });
                using (var cancelationSource = CancelationSource.New())
                {
                    var res = FirstAsync(xs, configured, async, captureValue, x =>
                    {
                        if (x == 2)
                        {
                            cancelationSource.Cancel();
                        }
                        return x == 4;
                    }, cancelationSource.Token);
                    await TestHelper.AssertCanceledAsync(() => res);
                }
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }
    }
}