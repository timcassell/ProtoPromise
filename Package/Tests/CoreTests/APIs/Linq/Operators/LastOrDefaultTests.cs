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
    public class LastOrDefaultTests
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
        public void LastOrDefaultAsync_NullArgument()
        {
            var enumerable = AsyncEnumerable.Range(0, 10);
            string captureValue = "captureValue";

            Assert.Catch<System.ArgumentNullException>(() => enumerable.LastOrDefaultAsync(default(Func<int, bool>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.LastOrDefaultAsync(default(Func<int, CancelationToken, Promise<bool>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.LastOrDefaultAsync(captureValue, default(Func<string, int, bool>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.LastOrDefaultAsync(captureValue, default(Func<string, int, CancelationToken, Promise<bool>>)));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.LastOrDefaultAsync(default(Func<int, bool>), 42));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.LastOrDefaultAsync(default(Func<int, CancelationToken, Promise<bool>>), 42));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.LastOrDefaultAsync(captureValue, default(Func<string, int, bool>), 42));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.LastOrDefaultAsync(captureValue, default(Func<string, int, CancelationToken, Promise<bool>>), 42));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).LastOrDefaultAsync(default(Func<int, bool>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).LastOrDefaultAsync(default(Func<int, CancelationToken, Promise<bool>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).LastOrDefaultAsync(captureValue, default(Func<string, int, bool>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).LastOrDefaultAsync(captureValue, default(Func<string, int, CancelationToken, Promise<bool>>)));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).LastOrDefaultAsync(default(Func<int, bool>), 42));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).LastOrDefaultAsync(default(Func<int, CancelationToken, Promise<bool>>), 42));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).LastOrDefaultAsync(captureValue, default(Func<string, int, bool>), 42));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).LastOrDefaultAsync(captureValue, default(Func<string, int, CancelationToken, Promise<bool>>), 42));

            enumerable.GetAsyncEnumerator().DisposeAsync().Forget();
        }
#endif

        [Test]
        public void LastOrDefaultAsync_NoParam_Empty()
        {
            Promise.Run(async () =>
            {
                var res = AsyncEnumerable.Empty<int>().LastOrDefaultAsync();
                Assert.AreEqual(0, await res);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void LastOrDefaultAsync_NoParam_Throw()
        {
            Promise.Run(async () =>
            {
                var ex = new Exception("Bang!");
                var res = AsyncEnumerable<int>.Rejected(ex).LastOrDefaultAsync();
                await TestHelper.AssertThrowsAsync(() => res, ex);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void LastOrDefaultAsync_NoParam_Single()
        {
            Promise.Run(async () =>
            {
                var res = AsyncEnumerable.Return(42).LastOrDefaultAsync();
                Assert.AreEqual(42, await res);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void LastOrDefaultAsync_NoParam_Many()
        {
            Promise.Run(async () =>
            {
                var res = new[] { 42, 43, 44 }.ToAsyncEnumerable().LastOrDefaultAsync();
                Assert.AreEqual(44, await res);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void LastOrDefaultAsync_DefaultParam_Empty()
        {
            Promise.Run(async () =>
            {
                var res = AsyncEnumerable.Empty<int>().LastOrDefaultAsync(42);
                Assert.AreEqual(42, await res);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void LastOrDefaultAsync_DefaultParam_Throw()
        {
            Promise.Run(async () =>
            {
                var ex = new Exception("Bang!");
                var res = AsyncEnumerable<int>.Rejected(ex).LastOrDefaultAsync(42);
                await TestHelper.AssertThrowsAsync(() => res, ex);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void LastOrDefaultAsync_DefaultParam_Single()
        {
            Promise.Run(async () =>
            {
                var res = AsyncEnumerable.Return(42).LastOrDefaultAsync(1);
                Assert.AreEqual(42, await res);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void LastOrDefaultAsync_DefaultParam_Many()
        {
            Promise.Run(async () =>
            {
                var res = new[] { 42, 43, 44 }.ToAsyncEnumerable().LastOrDefaultAsync(1);
                Assert.AreEqual(44, await res);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        // We test all the different overloads.
        private static Promise<TSource> LastOrDefaultAsync<TSource>(AsyncEnumerable<TSource> source,
            bool configured,
            bool async,
            bool captureValue,
            Func<TSource, bool> predicate,
            bool withDefaultParam,
            TSource defaultParam,
            CancelationToken cancelationToken = default)
        {
            if (configured)
            {
                return LastOrDefaultAsync(source.ConfigureAwait(SynchronizationOption.Foreground).WithCancelation(cancelationToken), async, captureValue, predicate, withDefaultParam, defaultParam);
            }

            const string valueCapture = "valueCapture";

            if (!captureValue)
            {
                return async
                    ? withDefaultParam
                        ? source.LastOrDefaultAsync(async (x, _) => predicate(x), defaultParam, cancelationToken)
                        : source.LastOrDefaultAsync(async (x, _) => predicate(x), cancelationToken)
                    : withDefaultParam
                        ? source.LastOrDefaultAsync(predicate, defaultParam, cancelationToken)
                        : source.LastOrDefaultAsync(predicate, cancelationToken);
            }
            else
            {
                return async
                    ? withDefaultParam
                        ? source.LastOrDefaultAsync(valueCapture, async (cv, x, _) =>
                        {
                            Assert.AreEqual(valueCapture, cv);
                            return predicate(x);
                        }, defaultParam, cancelationToken)
                        : source.LastOrDefaultAsync(valueCapture, async (cv, x, _) =>
                        {
                            Assert.AreEqual(valueCapture, cv);
                            return predicate(x);
                        }, cancelationToken)
                    : withDefaultParam
                        ? source.LastOrDefaultAsync(valueCapture, (cv, x) =>
                        {
                            Assert.AreEqual(valueCapture, cv);
                            return predicate(x);
                        }, defaultParam, cancelationToken)
                        : source.LastOrDefaultAsync(valueCapture, (cv, x) =>
                        {
                            Assert.AreEqual(valueCapture, cv);
                            return predicate(x);
                        }, cancelationToken);
            }
        }

        private static Promise<TSource> LastOrDefaultAsync<TSource>(ConfiguredAsyncEnumerable<TSource> source,
            bool async,
            bool captureValue,
            Func<TSource, bool> predicate,
            bool withDefaultParam,
            TSource defaultParam)
        {
            const string valueCapture = "valueCapture";

            if (!captureValue)
            {
                return async
                    ? withDefaultParam
                        ? source.LastOrDefaultAsync(async (x, _) => predicate(x), defaultParam)
                        : source.LastOrDefaultAsync(async (x, _) => predicate(x))
                    : withDefaultParam
                        ? source.LastOrDefaultAsync(predicate, defaultParam)
                        : source.LastOrDefaultAsync(predicate);
            }
            else
            {
                return async
                    ? withDefaultParam
                        ? source.LastOrDefaultAsync(valueCapture, async (cv, x, _) =>
                        {
                            Assert.AreEqual(valueCapture, cv);
                            return predicate(x);
                        }, defaultParam)
                        : source.LastOrDefaultAsync(valueCapture, async (cv, x, _) =>
                        {
                            Assert.AreEqual(valueCapture, cv);
                            return predicate(x);
                        })
                    : withDefaultParam
                        ? source.LastOrDefaultAsync(valueCapture, (cv, x) =>
                        {
                            Assert.AreEqual(valueCapture, cv);
                            return predicate(x);
                        }, defaultParam)
                        : source.LastOrDefaultAsync(valueCapture, (cv, x) =>
                        {
                            Assert.AreEqual(valueCapture, cv);
                            return predicate(x);
                        });
            }
        }

        [Test]
        public void LastOrDefaultAsync_Predicate_Empty(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureValue,
            [Values] bool withDefaultParam)
        {
            Promise.Run(async () =>
            {
                int expected = withDefaultParam ? 42 : 0;
                var res = LastOrDefaultAsync(AsyncEnumerable.Empty<int>(), configured, async, captureValue, x => true, withDefaultParam, expected);
                Assert.AreEqual(expected, await res);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void LastOrDefaultAsync_Predicate_Throw(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureValue,
            [Values] bool withDefaultParam)
        {
            Promise.Run(async () =>
            {
                var ex = new Exception("Bang!");
                var res = LastOrDefaultAsync(AsyncEnumerable<int>.Rejected(ex), configured, async, captureValue, x => true, withDefaultParam, 42);
                await TestHelper.AssertThrowsAsync(() => res, ex);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void LastOrDefaultAsync_Predicate_Single_None(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureValue,
            [Values] bool withDefaultParam)
        {
            Promise.Run(async () =>
            {
                int expected = withDefaultParam ? 42 : 0;
                var res = LastOrDefaultAsync(AsyncEnumerable.Return(42), configured, async, captureValue, x => x % 2 != 0, withDefaultParam, expected);
                Assert.AreEqual(expected, await res);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void LastOrDefaultAsync_Predicate_Many_None(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureValue,
            [Values] bool withDefaultParam)
        {
            Promise.Run(async () =>
            {
                int expected = withDefaultParam ? 42 : 0;
                var res = LastOrDefaultAsync(new[] { 40, 42, 44 }.ToAsyncEnumerable(), configured, async, captureValue, x => x % 2 != 0, withDefaultParam, expected);
                Assert.AreEqual(expected, await res);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void LastOrDefaultAsync_Predicate_Single_Pass(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureValue,
            [Values] bool withDefaultParam)
        {
            Promise.Run(async () =>
            {
                var res = LastOrDefaultAsync(AsyncEnumerable.Return(42), configured, async, captureValue, x => x % 2 == 0, withDefaultParam, 1);
                Assert.AreEqual(42, await res);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void LastOrDefaultAsync_Predicate_Many_Pass1(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureValue,
            [Values] bool withDefaultParam)
        {
            Promise.Run(async () =>
            {
                var res = LastOrDefaultAsync(new[] { 42, 43, 44 }.ToAsyncEnumerable(), configured, async, captureValue, x => x % 2 != 0, withDefaultParam, 1);
                Assert.AreEqual(43, await res);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void LastOrDefaultAsync_Predicate_Many_Pass2(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureValue,
            [Values] bool withDefaultParam)
        {
            Promise.Run(async () =>
            {
                var res = LastOrDefaultAsync(new[] { 42, 45, 90 }.ToAsyncEnumerable(), configured, async, captureValue, x => x % 2 == 0, withDefaultParam, 1);
                Assert.AreEqual(90, await res);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void LastOrDefaultAsync_Predicate_PredicateThrows(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureValue,
            [Values] bool withDefaultParam)
        {
            Promise.Run(async () =>
            {
                // IL2CPP crashes on integer divide by zero, so throw the exception manually instead.
                var res = LastOrDefaultAsync(new[] { 0, 1, 2 }.ToAsyncEnumerable(), configured, async, captureValue, x => { if (x == 0) throw new DivideByZeroException(); return 1 / x > 0; }, withDefaultParam, 42);
                await TestHelper.AssertThrowsAsync<DivideByZeroException>(() => res);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void LastOrDefaultAsync_NoParam_Cancel()
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
                    var res = xs.LastOrDefaultAsync(cancelationSource.Token);
                    cancelationSource.Cancel();
                    deferred.Resolve();
                    await TestHelper.AssertCanceledAsync(() => res);
                }
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void LastOrDefaultAsync_DefaultParam_Cancel()
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
                    var res = xs.LastOrDefaultAsync(42, cancelationSource.Token);
                    cancelationSource.Cancel();
                    deferred.Resolve();
                    await TestHelper.AssertCanceledAsync(() => res);
                }
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void LastOrDefaultAsync_Predicate_Cancel(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureValue,
            [Values] bool withDefaultParam)
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
                    var res = LastOrDefaultAsync(xs, configured, async, captureValue, x =>
                    {
                        if (x == 2)
                        {
                            cancelationSource.Cancel();
                        }
                        return x == 6;
                    }, withDefaultParam, 42, cancelationSource.Token);
                    await TestHelper.AssertCanceledAsync(() => res);
                }
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }
    }
}