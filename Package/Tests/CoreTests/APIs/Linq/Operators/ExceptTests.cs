#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using NUnit.Framework;
using Proto.Promises;
using Proto.Promises.CompilerServices;
using Proto.Promises.Linq;
using ProtoPromise.Tests.APIs.Collections;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

#pragma warning disable CS0162 // Unreachable code detected
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace ProtoPromise.Tests.APIs.Linq
{
    public class ExceptTests
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
        public void Except_NullArgumentThrows()
        {
            var first = AsyncEnumerable.Return(42);
            var second = AsyncEnumerable.Return(21);
            var nullComparer = default(IEqualityComparer<int>);

            Assert.Catch<System.ArgumentNullException>(() => first.Except(second, nullComparer));
            Assert.Catch<System.ArgumentNullException>(() => first.ConfigureAwait(SynchronizationOption.Synchronous).Except(second, nullComparer));

            first.GetAsyncEnumerator().DisposeAsync().Forget();
            second.GetAsyncEnumerator().DisposeAsync().Forget();
        }

        [Test]
        public void ExceptBy_NullArgumentThrows()
        {
            var first = AsyncEnumerable.Return(42);
            var second = AsyncEnumerable.Return(21);
            var captureValue = "captureValue";
            var nullComparer = default(IEqualityComparer<int>);

            Assert.Catch<System.ArgumentNullException>(() => first.ExceptBy(second, default(Func<int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => first.ExceptBy(second, x => x, nullComparer));
            Assert.Catch<System.ArgumentNullException>(() => first.ExceptBy(second, default(Func<int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => first.ExceptBy(second, async x => x, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => first.ExceptBy(second, captureValue, default(Func<string, int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => first.ExceptBy(second, captureValue, (cv, x) => x, nullComparer));
            Assert.Catch<System.ArgumentNullException>(() => first.ExceptBy(second, captureValue, default(Func<string, int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => first.ExceptBy(second, captureValue, async (cv, x) => x, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => first.ConfigureAwait(SynchronizationOption.Synchronous).ExceptBy(second, default(Func<int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => first.ConfigureAwait(SynchronizationOption.Synchronous).ExceptBy(second, x => x, nullComparer));
            Assert.Catch<System.ArgumentNullException>(() => first.ConfigureAwait(SynchronizationOption.Synchronous).ExceptBy(second, default(Func<int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => first.ConfigureAwait(SynchronizationOption.Synchronous).ExceptBy(second, async x => x, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => first.ConfigureAwait(SynchronizationOption.Synchronous).ExceptBy(second, captureValue, default(Func<string, int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => first.ConfigureAwait(SynchronizationOption.Synchronous).ExceptBy(second, captureValue, (cv, x) => x, nullComparer));
            Assert.Catch<System.ArgumentNullException>(() => first.ConfigureAwait(SynchronizationOption.Synchronous).ExceptBy(second, captureValue, default(Func<string, int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => first.ConfigureAwait(SynchronizationOption.Synchronous).ExceptBy(second, captureValue, async (cv, x) => x, nullComparer));

            first.GetAsyncEnumerator().DisposeAsync().Forget();
            second.GetAsyncEnumerator().DisposeAsync().Forget();
        }
#endif //PROMISE_DEBUG

        // We test all the different overloads.
        private static AsyncEnumerable<TSource> Except<TSource>(AsyncEnumerable<TSource> firstAsyncEnumerable,
            AsyncEnumerable<TSource> secondAsyncEnumerable,
            bool configured,
            IEqualityComparer<TSource> equalityComparer = null,
            CancelationToken configuredCancelationToken = default)
        {
            return configured
                ? equalityComparer != null
                    ? firstAsyncEnumerable.ConfigureAwait(SynchronizationOption.Foreground).WithCancelation(configuredCancelationToken).Except(secondAsyncEnumerable, equalityComparer)
                    : firstAsyncEnumerable.ConfigureAwait(SynchronizationOption.Foreground).WithCancelation(configuredCancelationToken).Except(secondAsyncEnumerable)
                : equalityComparer != null
                    ? firstAsyncEnumerable.Except(secondAsyncEnumerable, equalityComparer)
                    : firstAsyncEnumerable.Except(secondAsyncEnumerable);
        }

        private static AsyncEnumerable<TSource> ExceptBy<TSource, TKey>(AsyncEnumerable<TSource> firstAsyncEnumerable,
            AsyncEnumerable<TSource> secondAsyncEnumerable,
            bool configured,
            bool async,
            bool captureValue,
            Func<TSource, TKey> keySelector,
            IEqualityComparer<TKey> equalityComparer = null,
            CancelationToken configuredCancelationToken = default)
        {
            if (configured)
            {
                return ExceptBy(firstAsyncEnumerable.ConfigureAwait(SynchronizationOption.Foreground).WithCancelation(configuredCancelationToken), secondAsyncEnumerable, async, captureValue, keySelector, equalityComparer);
            }

            const string valueCapture = "valueCapture";

            if (!captureValue)
            {
                return async
                    ? equalityComparer != null
                        ? firstAsyncEnumerable.ExceptBy(secondAsyncEnumerable, async x => keySelector(x), equalityComparer)
                        : firstAsyncEnumerable.ExceptBy(secondAsyncEnumerable, async x => keySelector(x))
                    : equalityComparer != null
                        ? firstAsyncEnumerable.ExceptBy(secondAsyncEnumerable, keySelector, equalityComparer)
                        : firstAsyncEnumerable.ExceptBy(secondAsyncEnumerable, keySelector);
            }
            else
            {
                return async
                    ? equalityComparer != null
                        ? firstAsyncEnumerable.ExceptBy(secondAsyncEnumerable, valueCapture, async (cv, x) =>
                        {
                            Assert.AreEqual(valueCapture, cv);
                            return keySelector(x);
                        }, equalityComparer)
                        : firstAsyncEnumerable.ExceptBy(secondAsyncEnumerable, valueCapture, async (cv, x) =>
                        {
                            Assert.AreEqual(valueCapture, cv);
                            return keySelector(x);
                        })
                    : equalityComparer != null
                        ? firstAsyncEnumerable.ExceptBy(secondAsyncEnumerable, valueCapture, (cv, x) =>
                        {
                            Assert.AreEqual(valueCapture, cv);
                            return keySelector(x);
                        }, equalityComparer)
                        : firstAsyncEnumerable.ExceptBy(secondAsyncEnumerable, valueCapture, (cv, x) =>
                        {
                            Assert.AreEqual(valueCapture, cv);
                            return keySelector(x);
                        });
            }
        }

        private static AsyncEnumerable<TSource> ExceptBy<TSource, TKey>(ConfiguredAsyncEnumerable<TSource> firstAsyncEnumerable,
            AsyncEnumerable<TSource> secondAsyncEnumerable,
            bool async,
            bool captureValue,
            Func<TSource, TKey> keySelector,
            IEqualityComparer<TKey> equalityComparer = null)
        {
            const string valueCapture = "valueCapture";

            if (!captureValue)
            {
                return async
                    ? equalityComparer != null
                        ? firstAsyncEnumerable.ExceptBy(secondAsyncEnumerable, async x => keySelector(x), equalityComparer)
                        : firstAsyncEnumerable.ExceptBy(secondAsyncEnumerable, async x => keySelector(x))
                    : equalityComparer != null
                        ? firstAsyncEnumerable.ExceptBy(secondAsyncEnumerable, keySelector, equalityComparer)
                        : firstAsyncEnumerable.ExceptBy(secondAsyncEnumerable, keySelector);
            }
            else
            {
                return async
                    ? equalityComparer != null
                        ? firstAsyncEnumerable.ExceptBy(secondAsyncEnumerable, valueCapture, async (cv, x) =>
                        {
                            Assert.AreEqual(valueCapture, cv);
                            return keySelector(x);
                        }, equalityComparer)
                        : firstAsyncEnumerable.ExceptBy(secondAsyncEnumerable, valueCapture, async (cv, x) =>
                        {
                            Assert.AreEqual(valueCapture, cv);
                            return keySelector(x);
                        })
                    : equalityComparer != null
                        ? firstAsyncEnumerable.ExceptBy(secondAsyncEnumerable, valueCapture, (cv, x) =>
                        {
                            Assert.AreEqual(valueCapture, cv);
                            return keySelector(x);
                        }, equalityComparer)
                        : firstAsyncEnumerable.ExceptBy(secondAsyncEnumerable, valueCapture, (cv, x) =>
                        {
                            Assert.AreEqual(valueCapture, cv);
                            return keySelector(x);
                        });
            }
        }

        private static IEqualityComparer<T> GetDefaultOrNullComparer<T>(bool defaultComparer)
        {
            return defaultComparer ? EqualityComparer<T>.Default : null;
        }

        [Test]
        public void Except_EmptyFirst(
            [Values] bool configured,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var xs = AsyncEnumerable.Empty<int>();
                var ys = new[] { 3, 5, 1, 4 }.ToAsyncEnumerable();
                var asyncEnumerator = Except(xs, ys, configured, GetDefaultOrNullComparer<int>(withComparer)).GetAsyncEnumerator();
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Except_EmptySecond(
            [Values] bool configured,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var xs = new[] { 3, 5, 1, 4 }.ToAsyncEnumerable();
                var ys = AsyncEnumerable.Empty<int>();
                var asyncEnumerator = Except(xs, ys, configured, GetDefaultOrNullComparer<int>(withComparer)).GetAsyncEnumerator();
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(3, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(5, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(1, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(4, asyncEnumerator.Current);
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Except_Simple(
            [Values] bool configured,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var xs = new[] { 1, 2, 3 }.ToAsyncEnumerable();
                var ys = new[] { 3, 5, 1, 4 }.ToAsyncEnumerable();
                var asyncEnumerator = Except(xs, ys, configured, GetDefaultOrNullComparer<int>(withComparer)).GetAsyncEnumerator();
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(2, asyncEnumerator.Current);
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Except_EqualityComparer()
        {
            Promise.Run(async () =>
            {
                var xs = new[] { 1, 2, -3 }.ToAsyncEnumerable();
                var ys = new[] { 3, 5, -1, 4 }.ToAsyncEnumerable();
                var asyncEnumerator = xs.Except(ys, new Eq()).GetAsyncEnumerator();
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(2, asyncEnumerator.Current);
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Except_FirstThrows()
        {
            Promise.Run(async () =>
            {
                var ex = new Exception("Bang!");
                var xs = AsyncEnumerable<int>.Rejected(ex);
                var ys = new[] { 3, 5, -1, 4 }.ToAsyncEnumerable();
                var asyncEnumerator = xs.Except(ys, new Eq()).GetAsyncEnumerator();
                await TestHelper.AssertThrowsAsync(() => asyncEnumerator.MoveNextAsync(), ex);
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Except_SecondThrows()
        {
            Promise.Run(async () =>
            {
                var ex = new Exception("Bang!");
                var xs = new[] { 3, 5, -1, 4 }.ToAsyncEnumerable();
                var ys = AsyncEnumerable<int>.Rejected(ex);
                var asyncEnumerator = xs.Except(ys, new Eq()).GetAsyncEnumerator();
                await TestHelper.AssertThrowsAsync(() => asyncEnumerator.MoveNextAsync(), ex);
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void ExceptBy_EmptyFirst(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureValue,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var xs = AsyncEnumerable.Empty<int>();
                var ys = new[] { 3, 5, 1, 4 }.ToAsyncEnumerable();
                var asyncEnumerator = ExceptBy(xs, ys, configured, async, captureValue, x => x, GetDefaultOrNullComparer<int>(withComparer)).GetAsyncEnumerator();
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void ExceptBy_EmptySecond(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureValue,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var xs = new[] { 3, 5, 1, 4 }.ToAsyncEnumerable();
                var ys = AsyncEnumerable.Empty<int>();
                var asyncEnumerator = ExceptBy(xs, ys, configured, async, captureValue, x => x, GetDefaultOrNullComparer<int>(withComparer)).GetAsyncEnumerator();
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(3, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(5, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(1, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(4, asyncEnumerator.Current);
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void ExceptBy_Simple(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureValue,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var xs = new[] { 1, 2, 3 }.ToAsyncEnumerable();
                var ys = new[] { 3, 5, 1, 4 }.ToAsyncEnumerable();
                var asyncEnumerator = ExceptBy(xs, ys, configured, async, captureValue, x => x, GetDefaultOrNullComparer<int>(withComparer)).GetAsyncEnumerator();
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(2, asyncEnumerator.Current);
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void ExceptBy_EqualityComparer(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureValue)
        {
            Promise.Run(async () =>
            {
                var xs = new[] { 1, 2, -3 }.ToAsyncEnumerable();
                var ys = new[] { 3, 5, -1, 4 }.ToAsyncEnumerable();
                var asyncEnumerator = ExceptBy(xs, ys, configured, async, captureValue, x => x, new Eq()).GetAsyncEnumerator();
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(2, asyncEnumerator.Current);
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void ExceptBy_FirstThrows(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureValue,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var ex = new Exception("Bang!");
                var xs = AsyncEnumerable<int>.Rejected(ex);
                var ys = new[] { 3, 5, -1, 4 }.ToAsyncEnumerable();
                var asyncEnumerator = ExceptBy(xs, ys, configured, async, captureValue, x => x, GetDefaultOrNullComparer<int>(withComparer)).GetAsyncEnumerator();
                await TestHelper.AssertThrowsAsync(() => asyncEnumerator.MoveNextAsync(), ex);
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void ExceptBy_SecondThrows(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureValue,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var ex = new Exception("Bang!");
                var xs = new[] { 3, 5, -1, 4 }.ToAsyncEnumerable();
                var ys = AsyncEnumerable<int>.Rejected(ex);
                var asyncEnumerator = ExceptBy(xs, ys, configured, async, captureValue, x => x, GetDefaultOrNullComparer<int>(withComparer)).GetAsyncEnumerator();
                await TestHelper.AssertThrowsAsync(() => asyncEnumerator.MoveNextAsync(), ex);
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        private sealed class Eq : IEqualityComparer<int>
        {
            public bool Equals(int x, int y)
            {
                return EqualityComparer<int>.Default.Equals(Math.Abs(x), Math.Abs(y));
            }

            public int GetHashCode(int obj)
            {
                return EqualityComparer<int>.Default.GetHashCode(Math.Abs(obj));
            }
        }

        public enum ConfiguredType
        {
            NotConfigured,
            Configured,
            ConfiguredWithCancelation
        }

        [Test]
        public void Except_Cancel(
            [Values] ConfiguredType configuredType,
            [Values] bool withComparer,
            [Values] bool enumeratorToken,
            [Values] bool cancelFirst)
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
                var ys = AsyncEnumerable.Create<int>(async (writer, cancelationToken) =>
                {
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(3);
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(5);
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(1);
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(4);
                });
                using (var configuredCancelationSource = CancelationSource.New())
                {
                    using (var enumeratorCancelationSource = CancelationSource.New())
                    {
                        var asyncEnumerator = Except(xs, ys, configuredType != ConfiguredType.NotConfigured, GetDefaultOrNullComparer<int>(withComparer),
                            configuredType == ConfiguredType.ConfiguredWithCancelation ? configuredCancelationSource.Token : CancelationToken.None)
                            .GetAsyncEnumerator(enumeratorToken ? enumeratorCancelationSource.Token : CancelationToken.None);
                        if (cancelFirst)
                        {
                            Assert.True(await asyncEnumerator.MoveNextAsync());
                            Assert.AreEqual(2, asyncEnumerator.Current);
                        }
                        configuredCancelationSource.Cancel();
                        enumeratorCancelationSource.Cancel();
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

        [Test]
        public void ExceptBy_Cancel(
            [Values] ConfiguredType configuredType,
            [Values] bool async,
            [Values] bool captureValue,
            [Values] bool withComparer,
            [Values] bool enumeratorToken,
            [Values] bool cancelFirst)
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
                var ys = AsyncEnumerable.Create<int>(async (writer, cancelationToken) =>
                {
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(3);
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(5);
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(1);
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(4);
                });
                using (var configuredCancelationSource = CancelationSource.New())
                {
                    using (var enumeratorCancelationSource = CancelationSource.New())
                    {
                        var asyncEnumerator = ExceptBy(xs, ys, configuredType != ConfiguredType.NotConfigured, async, captureValue, x => x, GetDefaultOrNullComparer<int>(withComparer),
                            configuredType == ConfiguredType.ConfiguredWithCancelation ? configuredCancelationSource.Token : CancelationToken.None)
                            .GetAsyncEnumerator(enumeratorToken ? enumeratorCancelationSource.Token : CancelationToken.None);
                        if (cancelFirst)
                        {
                            Assert.True(await asyncEnumerator.MoveNextAsync());
                            Assert.AreEqual(2, asyncEnumerator.Current);
                        }
                        configuredCancelationSource.Cancel();
                        enumeratorCancelationSource.Cancel();
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