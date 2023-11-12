#if CSHARP_7_3_OR_NEWER

#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using NUnit.Framework;
using Proto.Promises;
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
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ForEachAsync(default(Action<int>), SynchronizationContext.Current));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ForEachAsync(default(Action<int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ForEachAsync(default(Action<int, int>), SynchronizationContext.Current));
                                                             
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ForEachAsync("captured", default(Action<string, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ForEachAsync("captured", default(Action<string, int>), SynchronizationContext.Current));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ForEachAsync("captured", default(Action<string, int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ForEachAsync("captured", default(Action<string, int, int>), SynchronizationContext.Current));
                                                             
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ForEachAsync(default(Func<int, Promise>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ForEachAsync(default(Func<int, Promise>), SynchronizationContext.Current));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ForEachAsync(default(Func<int, int, Promise>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ForEachAsync(default(Func<int, int, Promise>), SynchronizationContext.Current));
                                                             
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ForEachAsync("captured", default(Func<string, int, Promise>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ForEachAsync("captured", default(Func<string, int, Promise>), SynchronizationContext.Current));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ForEachAsync("captured", default(Func<string, int, int, Promise>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ForEachAsync("captured", default(Func<string, int, int, Promise>), SynchronizationContext.Current));

            enumerable.GetAsyncEnumerator().DisposeAsync().Forget();
        }
#endif

        [Test]
        public void ForEachAsync_Simple()
        {
            var sum = 0;
            EnumerableRangeAsync(1, 4)
                .ForEachAsync(x => sum += x)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
            Assert.AreEqual(10, sum);
        }

        [Test]
        public void ForEachAsync_Simple_WithCaptureValue()
        {
            var sum = 0;
            string expected = "captured";
            EnumerableRangeAsync(1, 4)
                .ForEachAsync(expected, (s, x) =>
                {
                    Assert.AreEqual(expected, s);
                    sum += x;
                })
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
            Assert.AreEqual(10, sum);
        }

        [Test]
        public void ForEachAsync_Indexed()
        {
            var sum = 0;
            EnumerableRangeAsync(1, 4)
                .ForEachAsync((x, i) => sum += x * i)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
            Assert.AreEqual(1 * 0 + 2 * 1 + 3 * 2 + 4 * 3, sum);
        }

        [Test]
        public void ForEachAsync_Indexed_WithCaptureValue()
        {
            var sum = 0;
            string expected = "captured";
            EnumerableRangeAsync(1, 4)
                .ForEachAsync(expected, (s, x, i) =>
                {
                    Assert.AreEqual(expected, s);
                    sum += x * i;
                })
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
            Assert.AreEqual(1 * 0 + 2 * 1 + 3 * 2 + 4 * 3, sum);
        }

        [Test]
        public void ForEachAsync_Throws_Action()
        {
            Promise.Run(async () =>
            {
                var ex = new Exception("Bang");
                Exception actual = null;
                try
                {
                    await EnumerableRangeAsync(1, 4)
                        .ForEachAsync(x => { throw ex; });
                }
                catch (Exception e)
                {
                    actual = e;
                }
                Assert.AreEqual(ex, actual);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void ForEachAsync_Indexed_Throws_Action()
        {
            Promise.Run(async () =>
            {
                var ex = new Exception("Bang");
                Exception actual = null;
                try
                {
                    await EnumerableRangeAsync(1, 4)
                        .ForEachAsync((x, i) => { throw ex; });
                }
                catch (Exception e)
                {
                    actual = e;
                }
                Assert.AreEqual(ex, actual);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void ForEachAwaitAsync_Simple()
        {
            var sum = 0;
            EnumerableRangeAsync(1, 4)
                .ForEachAsync(async x => sum += x)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
            Assert.AreEqual(10, sum);
        }

        [Test]
        public void ForEachAwaitAsync_Simple_WithCaptureValue()
        {
            var sum = 0;
            string expected = "captured";
            EnumerableRangeAsync(1, 4)
                .ForEachAsync(expected, async (s, x) =>
                {
                    Assert.AreEqual(expected, s);
                    sum += x;
                })
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
            Assert.AreEqual(10, sum);
        }

        [Test]
        public void ForEachAwaitAsync_Indexed()
        {
            var sum = 0;
            EnumerableRangeAsync(1, 4)
                .ForEachAsync((x, i) => sum += x * i)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
            Assert.AreEqual(1 * 0 + 2 * 1 + 3 * 2 + 4 * 3, sum);
        }

        [Test]
        public void ForEachAwaitAsync_Indexed_WithCaptureValue()
        {
            var sum = 0;
            string expected = "captured";
            EnumerableRangeAsync(1, 4)
                .ForEachAsync(expected, async (s, x, i) =>
                {
                    Assert.AreEqual(expected, s);
                    sum += x * i;
                })
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
            Assert.AreEqual(1 * 0 + 2 * 1 + 3 * 2 + 4 * 3, sum);
        }

        [Test]
        public void ForEachAwaitAsync_Throws_Action()
        {
            Promise.Run(async () =>
            {
                var ex = new Exception("Bang");
                Exception actual = null;
                try
                {
                    await EnumerableRangeAsync(1, 4)
                        .ForEachAsync(x => Promise.Rejected(ex));
                }
                catch (Exception e)
                {
                    actual = e;
                }
                Assert.AreEqual(ex, actual);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void ForEachAwaitAsync_Indexed_Throws_Action()
        {
            Promise.Run(async () =>
            {
                var ex = new Exception("Bang");
                Exception actual = null;
                try
                {
                    await EnumerableRangeAsync(1, 4)
                        .ForEachAsync((x, i) => Promise.Rejected(ex));
                }
                catch (Exception e)
                {
                    actual = e;
                }
                Assert.AreEqual(ex, actual);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }
    }
}

#endif