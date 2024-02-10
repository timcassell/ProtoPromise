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
    public class CountByTests
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
        public void CountBy_NullArgumentThrows()
        {
            var enumerable = AsyncEnumerable.Return(42);
            var captureValue = "captureValue";
            var nullComparer = default(IEqualityComparer<int>);

            Assert.Catch<System.ArgumentNullException>(() => enumerable.CountBy(default(Func<int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.CountBy(default(Func<int, int>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.CountBy(x => 0, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.CountBy(captureValue, default(Func<string, int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.CountBy(captureValue, default(Func<string, int, int>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.CountBy(captureValue, (cv, x) => 0, nullComparer));


            Assert.Catch<System.ArgumentNullException>(() => enumerable.CountBy(default(Func<int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.CountBy(default(Func<int, Promise<int>>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.CountBy(x => Promise.Resolved(0), nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.CountBy(captureValue, default(Func<string, int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.CountBy(captureValue, default(Func<string, int, Promise<int>>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.CountBy(captureValue, (cv, x) => Promise.Resolved(0), nullComparer));


            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).CountBy(default(Func<int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).CountBy(default(Func<int, int>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).CountBy(x => 0, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).CountBy(captureValue, default(Func<string, int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).CountBy(captureValue, default(Func<string, int, int>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).CountBy(captureValue, (cv, x) => 0, nullComparer));


            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).CountBy(default(Func<int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).CountBy(default(Func<int, Promise<int>>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).CountBy(x => Promise.Resolved(0), nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).CountBy(captureValue, default(Func<string, int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).CountBy(captureValue, default(Func<string, int, Promise<int>>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).CountBy(captureValue, (cv, x) => Promise.Resolved(0), nullComparer));

            enumerable.GetAsyncEnumerator().DisposeAsync().Forget();
        }
#endif //PROMISE_DEBUG

        // We test all the different overloads.
        private static AsyncEnumerable<KeyValuePair<TKey, int>> CountBy<TSource, TKey>(AsyncEnumerable<TSource> asyncEnumerable,
            bool configured,
            bool async,
            bool captureKey,
            Func<TSource, TKey> keySelector,
            IEqualityComparer<TKey> equalityComparer = null,
            CancelationToken configuredCancelationToken = default)
        {
            if (configured)
            {
                return CountBy(asyncEnumerable.ConfigureAwait(SynchronizationOption.Foreground).WithCancelation(configuredCancelationToken), async, captureKey, keySelector, equalityComparer);
            }

            const string keyCapture = "keyCapture";

            if (!captureKey)
            {
                return async
                    ? equalityComparer != null
                        ? asyncEnumerable.CountBy(async x => keySelector(x), equalityComparer)
                        : asyncEnumerable.CountBy(async x => keySelector(x))
                    : equalityComparer != null
                        ? asyncEnumerable.CountBy(keySelector, equalityComparer)
                        : asyncEnumerable.CountBy(keySelector);
            }
            else
            {
                return async
                    ? equalityComparer != null
                        ? asyncEnumerable.CountBy(keyCapture, async (cv, x) =>
                        {
                            Assert.AreEqual(keyCapture, cv);
                            return keySelector(x);
                        }, equalityComparer)
                        : asyncEnumerable.CountBy(keyCapture, async (cv, x) =>
                        {
                            Assert.AreEqual(keyCapture, cv);
                            return keySelector(x);
                        })
                    : equalityComparer != null
                        ? asyncEnumerable.CountBy(keyCapture, (cv, x) =>
                        {
                            Assert.AreEqual(keyCapture, cv);
                            return keySelector(x);
                        }, equalityComparer)
                        : asyncEnumerable.CountBy(keyCapture, (cv, x) =>
                        {
                            Assert.AreEqual(keyCapture, cv);
                            return keySelector(x);
                        });
            }
        }

        private static AsyncEnumerable<KeyValuePair<TKey, int>> CountBy<TSource, TKey>(ConfiguredAsyncEnumerable<TSource> asyncEnumerable,
            bool async,
            bool captureKey,
            Func<TSource, TKey> keySelector,
            IEqualityComparer<TKey> equalityComparer = null)
        {
            const string keyCapture = "keyCapture";

            if (!captureKey)
            {
                return async
                    ? equalityComparer != null
                        ? asyncEnumerable.CountBy(async x => keySelector(x), equalityComparer)
                        : asyncEnumerable.CountBy(async x => keySelector(x))
                    : equalityComparer != null
                        ? asyncEnumerable.CountBy(keySelector, equalityComparer)
                        : asyncEnumerable.CountBy(keySelector);
            }
            else
            {
                return async
                    ? equalityComparer != null
                        ? asyncEnumerable.CountBy(keyCapture, async (cv, x) =>
                        {
                            Assert.AreEqual(keyCapture, cv);
                            return keySelector(x);
                        }, equalityComparer)
                        : asyncEnumerable.CountBy(keyCapture, async (cv, x) =>
                        {
                            Assert.AreEqual(keyCapture, cv);
                            return keySelector(x);
                        })
                    : equalityComparer != null
                        ? asyncEnumerable.CountBy(keyCapture, (cv, x) =>
                        {
                            Assert.AreEqual(keyCapture, cv);
                            return keySelector(x);
                        }, equalityComparer)
                        : asyncEnumerable.CountBy(keyCapture, (cv, x) =>
                        {
                            Assert.AreEqual(keyCapture, cv);
                            return keySelector(x);
                        });
            }
        }

        private static IEqualityComparer<T> GetDefaultOrNullComparer<T>(bool defaultComparer)
        {
            return defaultComparer ? EqualityComparer<T>.Default : null;
        }

        [Test]
        public void CountBy_Empty(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureKey,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var e = CountBy(AsyncEnumerable.Empty<int>(), configured, async, captureKey, x => x, equalityComparer: GetDefaultOrNullComparer<int>(withComparer)).GetAsyncEnumerator();
                Assert.False(await e.MoveNextAsync());
                await e.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void CountBy_Simple1(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureKey,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var xs = new[] {
                    new { Name = "Bart", Age = 27 },
                    new { Name = "John", Age = 62 },
                    new { Name = "Eric", Age = 27 },
                    new { Name = "Lisa", Age = 14 },
                    new { Name = "Brad", Age = 27 },
                    new { Name = "Lisa", Age = 23 },
                    new { Name = "Eric", Age = 42 },
                };

                var e = CountBy(xs.ToAsyncEnumerable(), configured, async, captureKey, x => x.Age, equalityComparer: GetDefaultOrNullComparer<int>(withComparer)).GetAsyncEnumerator();

                Assert.True(await e.MoveNextAsync());
                Assert.AreEqual(new KeyValuePair<int, int>(27, 3), e.Current);
                Assert.True(await e.MoveNextAsync());
                Assert.AreEqual(new KeyValuePair<int, int>(62, 1), e.Current);
                Assert.True(await e.MoveNextAsync());
                Assert.AreEqual(new KeyValuePair<int, int>(14, 1), e.Current);
                Assert.True(await e.MoveNextAsync());
                Assert.AreEqual(new KeyValuePair<int, int>(23, 1), e.Current);
                Assert.True(await e.MoveNextAsync());
                Assert.AreEqual(new KeyValuePair<int, int>(42, 1), e.Current);
                Assert.False(await e.MoveNextAsync());
                await e.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void CountBy_Simple2(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureKey,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var xs = new[] {
                    new { Name = "Bart", Age = 27 },
                    new { Name = "John", Age = 62 },
                    new { Name = "Eric", Age = 27 },
                    new { Name = "Lisa", Age = 14 },
                    new { Name = "Brad", Age = 27 },
                    new { Name = "Lisa", Age = 23 },
                    new { Name = "Eric", Age = 42 },
                };

                var e = CountBy(xs.ToAsyncEnumerable(), configured, async, captureKey, x => x.Name, equalityComparer: GetDefaultOrNullComparer<string>(withComparer)).GetAsyncEnumerator();

                Assert.True(await e.MoveNextAsync());
                Assert.AreEqual(new KeyValuePair<string, int>("Bart", 1), e.Current);
                Assert.True(await e.MoveNextAsync());
                Assert.AreEqual(new KeyValuePair<string, int>("John", 1), e.Current);
                Assert.True(await e.MoveNextAsync());
                Assert.AreEqual(new KeyValuePair<string, int>("Eric", 2), e.Current);
                Assert.True(await e.MoveNextAsync());
                Assert.AreEqual(new KeyValuePair<string, int>("Lisa", 2), e.Current);
                Assert.True(await e.MoveNextAsync());
                Assert.AreEqual(new KeyValuePair<string, int>("Brad", 1), e.Current);
                Assert.False(await e.MoveNextAsync());
                await e.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void CountBy_Throws_Source(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureKey,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var ex = new Exception("Bang!");
                var e = CountBy(AsyncEnumerable<int>.Rejected(ex), configured, async, captureKey, x => x, equalityComparer: GetDefaultOrNullComparer<int>(withComparer)).GetAsyncEnumerator();
                await TestHelper.AssertThrowsAsync(() => e.MoveNextAsync(), ex);
                await e.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void CountBy_Throws(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureKey,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var ex = new Exception("Bang!");
                var e = CountBy(AsyncEnumerable.Return(42), configured, async, captureKey, x => { throw ex; return x; }, equalityComparer: GetDefaultOrNullComparer<int>(withComparer)).GetAsyncEnumerator();
                await TestHelper.AssertThrowsAsync(() => e.MoveNextAsync(), ex);
                await e.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void CountBy_Comparer_Simple(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureKey)
        {
            Promise.Run(async () =>
            {
                var ys = CountBy(AsyncEnumerable.Range(0, 10), configured, async, captureKey, x => x, new EqMod(3));

                var e = ys.GetAsyncEnumerator();
                Assert.True(await e.MoveNextAsync());
                Assert.AreEqual(new KeyValuePair<int, int>(0, 4), e.Current);
                Assert.True(await e.MoveNextAsync());
                Assert.AreEqual(new KeyValuePair<int, int>(1, 3), e.Current);
                Assert.True(await e.MoveNextAsync());
                Assert.AreEqual(new KeyValuePair<int, int>(2, 3), e.Current);
                Assert.False(await e.MoveNextAsync());
                await e.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void CountBy_Comparer_Count(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureKey)
        {
            Promise.Run(async () =>
            {
                var ys = CountBy(AsyncEnumerable.Range(0, 10), configured, async, captureKey, x => x, new EqMod(3));
                Assert.AreEqual(3, await ys.CountAsync());
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        private sealed class EqMod : IEqualityComparer<int>
        {
            private readonly int _d;

            public EqMod(int d)
            {
                _d = d;
            }

            public bool Equals(int x, int y)
            {
                return EqualityComparer<int>.Default.Equals(x % _d, y % _d);
            }

            public int GetHashCode(int obj)
            {
                return EqualityComparer<int>.Default.GetHashCode(obj % _d);
            }
        }

        public enum ConfiguredType
        {
            NotConfigured,
            Configured,
            ConfiguredWithCancelation
        }

        [Test]
        public void CountBy_Cancel(
            [Values] ConfiguredType configuredType,
            [Values] bool async,
            [Values] bool captureKey,
            [Values] bool withComparer,
            [Values] bool enumeratorToken)
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
                using (var configuredCancelationSource = CancelationSource.New())
                {
                    using (var enumeratorCancelationSource = CancelationSource.New())
                    {
                        var asyncEnumerator = CountBy(xs, configuredType != ConfiguredType.NotConfigured, async, captureKey, x =>
                            {
                                if (x == 2)
                                {
                                    configuredCancelationSource.Cancel();
                                    enumeratorCancelationSource.Cancel();
                                }
                                return x;
                            },
                            equalityComparer: GetDefaultOrNullComparer<int>(withComparer),
                            configuredCancelationToken: configuredType == ConfiguredType.ConfiguredWithCancelation ? configuredCancelationSource.Token : CancelationToken.None)
                            .GetAsyncEnumerator(enumeratorToken ? enumeratorCancelationSource.Token : CancelationToken.None);
                        if (configuredType == ConfiguredType.ConfiguredWithCancelation || enumeratorToken)
                        {
                            await TestHelper.AssertCanceledAsync(() => asyncEnumerator.MoveNextAsync());
                        }
                        await asyncEnumerator.DisposeAsync();
                    }
                }
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }
    }
}

#endif