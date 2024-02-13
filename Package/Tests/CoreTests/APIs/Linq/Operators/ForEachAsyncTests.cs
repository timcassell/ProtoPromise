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
using System.Runtime.CompilerServices;
using System.Threading;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace ProtoPromiseTests.APIs.Linq
{
    public class ForEachAsyncTests
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

        private static AsyncEnumerable<int> EnumerableRangeAsync(int start, int count, bool yield = true)
        {
            return AsyncEnumerable<int>.Create((start, count, yield), async (cv, writer, cancelationToken) =>
            {
                for (int i = cv.start; i < cv.start + cv.count; i++)
                {
                    if (cv.yield)
                    {
                        await Promise.SwitchToBackgroundAwait(forceAsync: true);
                    }

                    await writer.YieldAsync(i);
                }
            });
        }

#if PROMISE_DEBUG
        [Test]
        public void ForEachAsync_Null()
        {
            var enumerable = EnumerableRangeAsync(0, 10);

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ForEachAsync(default(Action<int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Foreground).ForEachAsync(default(Action<int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ForEachAsync(default(Action<int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Foreground).ForEachAsync(default(Action<int, int>)));
                                                             
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ForEachAsync("captured", default(Action<string, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Foreground).ForEachAsync("captured", default(Action<string, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ForEachAsync("captured", default(Action<string, int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Foreground).ForEachAsync("captured", default(Action<string, int, int>)));
                                                             
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ForEachAsync(default(Func<int, Promise>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Foreground).ForEachAsync(default(Func<int, Promise>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ForEachAsync(default(Func<int, int, Promise>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Foreground).ForEachAsync(default(Func<int, int, Promise>)));
                                                             
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ForEachAsync("captured", default(Func<string, int, Promise>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Foreground).ForEachAsync("captured", default(Func<string, int, Promise>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ForEachAsync("captured", default(Func<string, int, int, Promise>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Foreground).ForEachAsync("captured", default(Func<string, int, int, Promise>)));

            enumerable.GetAsyncEnumerator().DisposeAsync().Forget();
        }
#endif

        // We test all the different overloads.
        private static Promise ForEachAsync<TSource>(AsyncEnumerable<TSource> source,
            bool configured,
            bool async,
            bool captureValue,
            Action<TSource> action,
            CancelationToken cancelationToken = default)
        {
            if (configured)
            {
                return ForEachAsync(source.ConfigureAwait(SynchronizationOption.Foreground).WithCancelation(cancelationToken), async, captureValue, action);
            }

            const string valueCapture = "valueCapture";

            if (!captureValue)
            {
                return async
                    ? source.ForEachAsync(async x => action(x), cancelationToken)
                    : source.ForEachAsync(action, cancelationToken);
            }
            else
            {
                return async
                    ? source.ForEachAsync(valueCapture, async (cv, x) =>
                    {
                        Assert.AreEqual(valueCapture, cv);
                        action(x);
                    }, cancelationToken)
                    : source.ForEachAsync(valueCapture, (cv, x) =>
                    {
                        Assert.AreEqual(valueCapture, cv);
                        action(x);
                    }, cancelationToken);
            }
        }

        private static Promise ForEachAsync<TSource>(ConfiguredAsyncEnumerable<TSource> source,
            bool async,
            bool captureValue,
            Action<TSource> action)
        {
            const string valueCapture = "valueCapture";

            if (!captureValue)
            {
                return async
                    ? source.ForEachAsync(async x => action(x))
                    : source.ForEachAsync(action);
            }
            else
            {
                return async
                    ? source.ForEachAsync(valueCapture, async (cv, x) =>
                    {
                        Assert.AreEqual(valueCapture, cv);
                        action(x);
                    })
                    : source.ForEachAsync(valueCapture, (cv, x) =>
                    {
                        Assert.AreEqual(valueCapture, cv);
                        action(x);
                    });
            }
        }

        // We test all the different overloads.
        private static Promise ForEachAsync<TSource>(AsyncEnumerable<TSource> source,
            bool configured,
            bool async,
            bool captureValue,
            Action<TSource, int> action,
            CancelationToken cancelationToken = default)
        {
            if (configured)
            {
                return ForEachAsync(source.ConfigureAwait(SynchronizationOption.Foreground).WithCancelation(cancelationToken), async, captureValue, action);
            }

            const string valueCapture = "valueCapture";

            if (!captureValue)
            {
                return async
                    ? source.ForEachAsync(async (x, i) => action(x, i), cancelationToken)
                    : source.ForEachAsync(action, cancelationToken);
            }
            else
            {
                return async
                    ? source.ForEachAsync(valueCapture, async (cv, x, i) =>
                    {
                        Assert.AreEqual(valueCapture, cv);
                        action(x, i);
                    }, cancelationToken)
                    : source.ForEachAsync(valueCapture, (cv, x, i) =>
                    {
                        Assert.AreEqual(valueCapture, cv);
                        action(x, i);
                    }, cancelationToken);
            }
        }

        private static Promise ForEachAsync<TSource>(ConfiguredAsyncEnumerable<TSource> source,
            bool async,
            bool captureValue,
            Action<TSource, int> action)
        {
            const string valueCapture = "valueCapture";

            if (!captureValue)
            {
                return async
                    ? source.ForEachAsync(async (x, i) => action(x, i))
                    : source.ForEachAsync(action);
            }
            else
            {
                return async
                    ? source.ForEachAsync(valueCapture, async (cv, x, i) =>
                    {
                        Assert.AreEqual(valueCapture, cv);
                        action(x, i);
                    })
                    : source.ForEachAsync(valueCapture, (cv, x, i) =>
                    {
                        Assert.AreEqual(valueCapture, cv);
                        action(x, i);
                    });
            }
        }

        [Test]
        public void ForEachAsync_Simple(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureValue)
        {
            var sum = 0;
            ForEachAsync(EnumerableRangeAsync(1, 4), configured, async, captureValue, x => sum += x)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
            Assert.AreEqual(10, sum);
        }

        [Test]
        public void ForEachAsync_Indexed(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureValue)
        {
            var sum = 0;
            ForEachAsync(EnumerableRangeAsync(1, 4), configured, async, captureValue, (x, i) => sum += x * i)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
            Assert.AreEqual(1 * 0 + 2 * 1 + 3 * 2 + 4 * 3, sum);
        }

        [Test]
        public void ForEachAsync_Throws_Action(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureValue)
        {
            Promise.Run(async () =>
            {
                var ex = new Exception("Bang");
                var res = ForEachAsync(EnumerableRangeAsync(1, 4), configured, async, captureValue, x => { throw ex; });
                await TestHelper.AssertThrowsAsync(() => res, ex);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void ForEachAsync_Indexed_Throws_Action(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureValue)
        {
            Promise.Run(async () =>
            {
                var ex = new Exception("Bang");
                var res = ForEachAsync(EnumerableRangeAsync(1, 4), configured, async, captureValue, (x, i) => { throw ex; });
                await TestHelper.AssertThrowsAsync(() => res, ex);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void ForEachAsync_Simple_Cancel(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureValue)
        {
            Promise.Run(async () =>
            {
                var xs = AsyncEnumerable.Create<int>(async (writer, cancelationToken) =>
                {
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(1);
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(2);
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(3);
                });
                using (var cancelationSource = CancelationSource.New())
                {
                    var res = ForEachAsync(xs, configured, async, captureValue, x =>
                    {
                        if (x == 2)
                        {
                            cancelationSource.Cancel();
                        }
                    }, cancelationSource.Token);
                    await TestHelper.AssertCanceledAsync(() => res);
                }
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void ForEachAsync_Indexed_Cancel(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureValue)
        {
            Promise.Run(async () =>
            {
                var xs = AsyncEnumerable.Create<int>(async (writer, cancelationToken) =>
                {
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(1);
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(2);
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(3);
                });
                using (var cancelationSource = CancelationSource.New())
                {
                    var res = ForEachAsync(xs, configured, async, captureValue, (x, i) =>
                    {
                        if (x == 2)
                        {
                            cancelationSource.Cancel();
                        }
                    }, cancelationSource.Token);
                    await TestHelper.AssertCanceledAsync(() => res);
                }
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }
    }
}