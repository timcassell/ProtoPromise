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
    public class AggregateBySingleSeedTests
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
        public void AggregateBy_NullArgumentThrows()
        {
            var enumerable = AsyncEnumerable.Return(42);
            var captureValue = "captureValue";
            var nullComparer = default(IEqualityComparer<int>);

            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(default(Func<int, int>), 0, (acc, x) => acc));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(default(Func<int, int>), 0, (acc, x) => acc, EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(x => 0, 0, (acc, x) => acc, nullComparer));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(captureValue, default(Func<string, int, int>), 0, (acc, x) => acc));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(captureValue, default(Func<string, int, int>), 0, (acc, x) => acc, EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(captureValue, (cv, x) => 0, 0, (acc, x) => acc, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(default(Func<int, Promise<int>>), 0, async (acc, x) => acc));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(default(Func<int, Promise<int>>), 0, async (acc, x) => acc, EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(x => Promise.Resolved(0), 0, async (acc, x) => acc, nullComparer));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(captureValue, default(Func<string, int, Promise<int>>), 0, async (acc, x) => acc));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(captureValue, default(Func<string, int, Promise<int>>), 0, async (acc, x) => acc, EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(captureValue, (cv, x) => Promise.Resolved(0), 0, async (acc, x) => acc, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(x => 0, 0, default(Func<int, int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(x => 0, 0, default(Func<int, int, int>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(x => 0, 0, (acc, x) => acc, nullComparer));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(captureValue, (cv, x) => 0, 0, default(Func<int, int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(captureValue, (cv, x) => 0, 0, default(Func<int, int, int>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(captureValue, (cv, x) => 0, 0, (acc, x) => acc, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(async x => 0, 0, default(Func<int, int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(async x => 0, 0, default(Func<int, int, Promise<int>>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(async x => 0, 0, async (acc, x) => acc, nullComparer));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(captureValue, async (cv, x) => 0, 0, default(Func<int, int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(captureValue, async (cv, x) => 0, 0, default(Func<int, int, Promise<int>>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(captureValue, async (cv, x) => 0, 0, async (acc, x) => acc, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(default(Func<int, int>), 0, captureValue, (cv, acc, x) => acc));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(default(Func<int, int>), 0, captureValue, (cv, acc, x) => acc, EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(x => 0, 0, captureValue, (cv, acc, x) => acc, nullComparer));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(captureValue, default(Func<string, int, int>), 0, captureValue, (cv, acc, x) => acc));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(captureValue, default(Func<string, int, int>), 0, captureValue, (cv, acc, x) => acc, EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(captureValue, (cv, x) => 0, 0, captureValue, (cv, acc, x) => acc, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(default(Func<int, Promise<int>>), 0, captureValue, async (cv, acc, x) => acc));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(default(Func<int, Promise<int>>), 0, captureValue, async (cv, acc, x) => acc, EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(x => Promise.Resolved(0), 0, captureValue, async (cv, acc, x) => acc, nullComparer));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(captureValue, default(Func<string, int, Promise<int>>), 0, captureValue, async (cv, acc, x) => acc));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(captureValue, default(Func<string, int, Promise<int>>), 0, captureValue, async (cv, acc, x) => acc, EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(captureValue, (cv, x) => Promise.Resolved(0), 0, captureValue, async (cv, acc, x) => acc, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(x => 0, 0, captureValue, default(Func<string, int, int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(x => 0, 0, captureValue, default(Func<string, int, int, int>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(x => 0, 0, captureValue, (cv, acc, x) => acc, nullComparer));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(captureValue, (cv, x) => 0, 0, captureValue, default(Func<string, int, int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(captureValue, (cv, x) => 0, 0, captureValue, default(Func<string, int, int, int>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(captureValue, (cv, x) => 0, 0, captureValue, (cv, acc, x) => acc, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(async x => 0, 0, captureValue, default(Func<string, int, int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(async x => 0, 0, captureValue, default(Func<string, int, int, Promise<int>>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(async x => 0, 0, captureValue, async (cv, acc, x) => acc, nullComparer));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(captureValue, async (cv, x) => 0, 0, captureValue, default(Func<string, int, int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(captureValue, async (cv, x) => 0, 0, captureValue, default(Func<string, int, int, Promise<int>>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(captureValue, async (cv, x) => 0, 0, captureValue, async (cv, acc, x) => acc, nullComparer));


            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(default(Func<int, int>), 0, (acc, x) => acc));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(default(Func<int, int>), 0, (acc, x) => acc, EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(x => 0, 0, (acc, x) => acc, nullComparer));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(captureValue, default(Func<string, int, int>), 0, (acc, x) => acc));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(captureValue, default(Func<string, int, int>), 0, (acc, x) => acc, EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(captureValue, (cv, x) => 0, 0, (acc, x) => acc, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(default(Func<int, Promise<int>>), 0, async (acc, x) => acc));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(default(Func<int, Promise<int>>), 0, async (acc, x) => acc, EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(x => Promise.Resolved(0), 0, async (acc, x) => acc, nullComparer));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(captureValue, default(Func<string, int, Promise<int>>), 0, async (acc, x) => acc));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(captureValue, default(Func<string, int, Promise<int>>), 0, async (acc, x) => acc, EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(captureValue, (cv, x) => Promise.Resolved(0), 0, async (acc, x) => acc, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(x => 0, 0, default(Func<int, int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(x => 0, 0, default(Func<int, int, int>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(x => 0, 0, (acc, x) => acc, nullComparer));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(captureValue, (cv, x) => 0, 0, default(Func<int, int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(captureValue, (cv, x) => 0, 0, default(Func<int, int, int>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(captureValue, (cv, x) => 0, 0, (acc, x) => acc, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(async x => 0, 0, default(Func<int, int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(async x => 0, 0, default(Func<int, int, Promise<int>>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(async x => 0, 0, async (acc, x) => acc, nullComparer));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(captureValue, async (cv, x) => 0, 0, default(Func<int, int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(captureValue, async (cv, x) => 0, 0, default(Func<int, int, Promise<int>>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(captureValue, async (cv, x) => 0, 0, async (acc, x) => acc, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(default(Func<int, int>), 0, captureValue, (cv, acc, x) => acc));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(default(Func<int, int>), 0, captureValue, (cv, acc, x) => acc, EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(x => 0, 0, captureValue, (cv, acc, x) => acc, nullComparer));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(captureValue, default(Func<string, int, int>), 0, captureValue, (cv, acc, x) => acc));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(captureValue, default(Func<string, int, int>), 0, captureValue, (cv, acc, x) => acc, EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(captureValue, (cv, x) => 0, 0, captureValue, (cv, acc, x) => acc, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(default(Func<int, Promise<int>>), 0, captureValue, async (cv, acc, x) => acc));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(default(Func<int, Promise<int>>), 0, captureValue, async (cv, acc, x) => acc, EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(x => Promise.Resolved(0), 0, captureValue, async (cv, acc, x) => acc, nullComparer));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(captureValue, default(Func<string, int, Promise<int>>), 0, captureValue, async (cv, acc, x) => acc));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(captureValue, default(Func<string, int, Promise<int>>), 0, captureValue, async (cv, acc, x) => acc, EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(captureValue, (cv, x) => Promise.Resolved(0), 0, captureValue, async (cv, acc, x) => acc, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(x => 0, 0, captureValue, default(Func<string, int, int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(x => 0, 0, captureValue, default(Func<string, int, int, int>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(x => 0, 0, captureValue, (cv, acc, x) => acc, nullComparer));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(captureValue, (cv, x) => 0, 0, captureValue, default(Func<string, int, int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(captureValue, (cv, x) => 0, 0, captureValue, default(Func<string, int, int, int>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(captureValue, (cv, x) => 0, 0, captureValue, (cv, acc, x) => acc, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(async x => 0, 0, captureValue, default(Func<string, int, int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(async x => 0, 0, captureValue, default(Func<string, int, int, Promise<int>>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(async x => 0, 0, captureValue, async (cv, acc, x) => acc, nullComparer));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(captureValue, async (cv, x) => 0, 0, captureValue, default(Func<string, int, int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(captureValue, async (cv, x) => 0, 0, captureValue, default(Func<string, int, int, Promise<int>>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(captureValue, async (cv, x) => 0, 0, captureValue, async (cv, acc, x) => acc, nullComparer));

            enumerable.GetAsyncEnumerator().DisposeAsync().Forget();
        }
#endif //PROMISE_DEBUG

        // We test all the different overloads.
        private static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate>(AsyncEnumerable<TSource> asyncEnumerable,
            bool configured,
            bool async,
            bool captureKey,
            Func<TSource, TKey> keySelector,
            TAccumulate seed,
            bool captureAccumulate,
            Func<TAccumulate, TSource, TAccumulate> accumulator,
            IEqualityComparer<TKey> equalityComparer = null,
            CancelationToken configuredCancelationToken = default)
        {
            if (configured)
            {
                return AggregateBy(asyncEnumerable.ConfigureAwait(SynchronizationOption.Foreground)
                    .WithCancelation(configuredCancelationToken), async, captureKey, keySelector, seed, captureAccumulate, accumulator, equalityComparer);
            }

            const string keyCapture = "keyCapture";
            const string accumulateCapture = "accumulateCapture";

            if (!captureKey)
            {
                if (!captureAccumulate)
                {
                    return async
                        ? equalityComparer != null
                            ? asyncEnumerable.AggregateBy(async x => keySelector(x), seed, async (acc, x) => accumulator(acc, x), equalityComparer)
                            : asyncEnumerable.AggregateBy(async x => keySelector(x), seed, async (acc, x) => accumulator(acc, x))
                        : equalityComparer != null
                            ? asyncEnumerable.AggregateBy(keySelector, seed, accumulator, equalityComparer)
                            : asyncEnumerable.AggregateBy(keySelector, seed, accumulator);
                }
                else
                {
                    return async
                        ? equalityComparer != null
                            ? asyncEnumerable.AggregateBy(async x => keySelector(x), seed, accumulateCapture, async (cv, acc, x) =>
                            {
                                Assert.AreEqual(accumulateCapture, cv);
                                return accumulator(acc, x);
                            }, equalityComparer)
                            : asyncEnumerable.AggregateBy(async x => keySelector(x), seed, accumulateCapture, async (cv, acc, x) =>
                            {
                                Assert.AreEqual(accumulateCapture, cv);
                                return accumulator(acc, x);
                            })
                        : equalityComparer != null
                            ? asyncEnumerable.AggregateBy(keySelector, seed, accumulateCapture, (cv, acc, x) =>
                            {
                                Assert.AreEqual(accumulateCapture, cv);
                                return accumulator(acc, x);
                            }, equalityComparer)
                            : asyncEnumerable.AggregateBy(keySelector, seed, accumulateCapture, (cv, acc, x) =>
                            {
                                Assert.AreEqual(accumulateCapture, cv);
                                return accumulator(acc, x);
                            });
                }
            }
            else
            {
                if (!captureAccumulate)
                {
                    return async
                        ? equalityComparer != null
                            ? asyncEnumerable.AggregateBy(keyCapture, async (cv, x) =>
                            {
                                Assert.AreEqual(keyCapture, cv);
                                return keySelector(x);
                            }, seed, async (acc, x) => accumulator(acc, x), equalityComparer)
                            : asyncEnumerable.AggregateBy(keyCapture, async (cv, x) =>
                            {
                                Assert.AreEqual(keyCapture, cv);
                                return keySelector(x);
                            }, seed, async (acc, x) => accumulator(acc, x))
                        : equalityComparer != null
                            ? asyncEnumerable.AggregateBy(keyCapture, (cv, x) =>
                            {
                                Assert.AreEqual(keyCapture, cv);
                                return keySelector(x);
                            }, seed, accumulator, equalityComparer)
                            : asyncEnumerable.AggregateBy(keyCapture, (cv, x) =>
                            {
                                Assert.AreEqual(keyCapture, cv);
                                return keySelector(x);
                            }, seed, accumulator);
                }
                else
                {
                    return async
                        ? equalityComparer != null
                            ? asyncEnumerable.AggregateBy(keyCapture, async (cv, x) =>
                            {
                                Assert.AreEqual(keyCapture, cv);
                                return keySelector(x);
                            }, seed, accumulateCapture, async (cv, acc, x) =>
                            {
                                Assert.AreEqual(accumulateCapture, cv);
                                return accumulator(acc, x);
                            }, equalityComparer)
                            : asyncEnumerable.AggregateBy(keyCapture, async (cv, x) =>
                            {
                                Assert.AreEqual(keyCapture, cv);
                                return keySelector(x);
                            }, seed, accumulateCapture, async (cv, acc, x) =>
                            {
                                Assert.AreEqual(accumulateCapture, cv);
                                return accumulator(acc, x);
                            })
                        : equalityComparer != null
                            ? asyncEnumerable.AggregateBy(keyCapture, (cv, x) =>
                            {
                                Assert.AreEqual(keyCapture, cv);
                                return keySelector(x);
                            }, seed, accumulateCapture, (cv, acc, x) =>
                            {
                                Assert.AreEqual(accumulateCapture, cv);
                                return accumulator(acc, x);
                            }, equalityComparer)
                            : asyncEnumerable.AggregateBy(keyCapture, (cv, x) =>
                            {
                                Assert.AreEqual(keyCapture, cv);
                                return keySelector(x);
                            }, seed, accumulateCapture, (cv, acc, x) =>
                            {
                                Assert.AreEqual(accumulateCapture, cv);
                                return accumulator(acc, x);
                            });
                }
            }
        }

        private static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate>(ConfiguredAsyncEnumerable<TSource> asyncEnumerable,
            bool async,
            bool captureKey,
            Func<TSource, TKey> keySelector,
            TAccumulate seed,
            bool captureAccumulate,
            Func<TAccumulate, TSource, TAccumulate> accumulator,
            IEqualityComparer<TKey> equalityComparer = null)
        {
            const string keyCapture = "keyCapture";
            const string accumulateCapture = "accumulateCapture";

            if (!captureKey)
            {
                if (!captureAccumulate)
                {
                    return async
                        ? equalityComparer != null
                            ? asyncEnumerable.AggregateBy(async x => keySelector(x), seed, async (acc, x) => accumulator(acc, x), equalityComparer)
                            : asyncEnumerable.AggregateBy(async x => keySelector(x), seed, async (acc, x) => accumulator(acc, x))
                        : equalityComparer != null
                            ? asyncEnumerable.AggregateBy(keySelector, seed, accumulator, equalityComparer)
                            : asyncEnumerable.AggregateBy(keySelector, seed, accumulator);
                }
                else
                {
                    return async
                        ? equalityComparer != null
                            ? asyncEnumerable.AggregateBy(async x => keySelector(x), seed, accumulateCapture, async (cv, acc, x) =>
                            {
                                Assert.AreEqual(accumulateCapture, cv);
                                return accumulator(acc, x);
                            }, equalityComparer)
                            : asyncEnumerable.AggregateBy(async x => keySelector(x), seed, accumulateCapture, async (cv, acc, x) =>
                            {
                                Assert.AreEqual(accumulateCapture, cv);
                                return accumulator(acc, x);
                            })
                        : equalityComparer != null
                            ? asyncEnumerable.AggregateBy(keySelector, seed, accumulateCapture, (cv, acc, x) =>
                            {
                                Assert.AreEqual(accumulateCapture, cv);
                                return accumulator(acc, x);
                            }, equalityComparer)
                            : asyncEnumerable.AggregateBy(keySelector, seed, accumulateCapture, (cv, acc, x) =>
                            {
                                Assert.AreEqual(accumulateCapture, cv);
                                return accumulator(acc, x);
                            });
                }
            }
            else
            {
                if (!captureAccumulate)
                {
                    return async
                        ? equalityComparer != null
                            ? asyncEnumerable.AggregateBy(keyCapture, async (cv, x) =>
                            {
                                Assert.AreEqual(keyCapture, cv);
                                return keySelector(x);
                            }, seed, async (acc, x) => accumulator(acc, x), equalityComparer)
                            : asyncEnumerable.AggregateBy(keyCapture, async (cv, x) =>
                            {
                                Assert.AreEqual(keyCapture, cv);
                                return keySelector(x);
                            }, seed, async (acc, x) => accumulator(acc, x))
                        : equalityComparer != null
                            ? asyncEnumerable.AggregateBy(keyCapture, (cv, x) =>
                            {
                                Assert.AreEqual(keyCapture, cv);
                                return keySelector(x);
                            }, seed, accumulator, equalityComparer)
                            : asyncEnumerable.AggregateBy(keyCapture, (cv, x) =>
                            {
                                Assert.AreEqual(keyCapture, cv);
                                return keySelector(x);
                            }, seed, accumulator);
                }
                else
                {
                    return async
                        ? equalityComparer != null
                            ? asyncEnumerable.AggregateBy(keyCapture, async (cv, x) =>
                            {
                                Assert.AreEqual(keyCapture, cv);
                                return keySelector(x);
                            }, seed, accumulateCapture, async (cv, acc, x) =>
                            {
                                Assert.AreEqual(accumulateCapture, cv);
                                return accumulator(acc, x);
                            }, equalityComparer)
                            : asyncEnumerable.AggregateBy(keyCapture, async (cv, x) =>
                            {
                                Assert.AreEqual(keyCapture, cv);
                                return keySelector(x);
                            }, seed, accumulateCapture, async (cv, acc, x) =>
                            {
                                Assert.AreEqual(accumulateCapture, cv);
                                return accumulator(acc, x);
                            })
                        : equalityComparer != null
                            ? asyncEnumerable.AggregateBy(keyCapture, (cv, x) =>
                            {
                                Assert.AreEqual(keyCapture, cv);
                                return keySelector(x);
                            }, seed, accumulateCapture, (cv, acc, x) =>
                            {
                                Assert.AreEqual(accumulateCapture, cv);
                                return accumulator(acc, x);
                            }, equalityComparer)
                            : asyncEnumerable.AggregateBy(keyCapture, (cv, x) =>
                            {
                                Assert.AreEqual(keyCapture, cv);
                                return keySelector(x);
                            }, seed, accumulateCapture, (cv, acc, x) =>
                            {
                                Assert.AreEqual(accumulateCapture, cv);
                                return accumulator(acc, x);
                            });
                }
            }
        }

        private static IEqualityComparer<T> GetDefaultOrNullComparer<T>(bool defaultComparer)
        {
            return defaultComparer ? EqualityComparer<T>.Default : null;
        }

        [Test]
        public void AggregateBy_Empty(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureKey,
            [Values] bool captureAccumulate,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var e = AggregateBy(AsyncEnumerable.Empty<int>(), configured, async,
                    captureKey, x => x,
                    0,
                    captureAccumulate, (acc, x) => acc,
                    equalityComparer: GetDefaultOrNullComparer<int>(withComparer)).GetAsyncEnumerator();
                Assert.False(await e.MoveNextAsync());
                await e.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AggregateBy_Expected1(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureKey,
            [Values] bool captureAccumulate,
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

                var e = AggregateBy(xs.ToAsyncEnumerable(), configured, async,
                    captureKey, x => x.Name,
                    0,
                    captureAccumulate, (acc, x) => acc + x.Age,
                    equalityComparer: GetDefaultOrNullComparer<string>(withComparer)).GetAsyncEnumerator();

                Assert.True(await e.MoveNextAsync());
                Assert.AreEqual(new KeyValuePair<string, int>("Bart", 27), e.Current);
                Assert.True(await e.MoveNextAsync());
                Assert.AreEqual(new KeyValuePair<string, int>("John", 62), e.Current);
                Assert.True(await e.MoveNextAsync());
                Assert.AreEqual(new KeyValuePair<string, int>("Eric", 27 + 42), e.Current);
                Assert.True(await e.MoveNextAsync());
                Assert.AreEqual(new KeyValuePair<string, int>("Lisa", 14 + 23), e.Current);
                Assert.True(await e.MoveNextAsync());
                Assert.AreEqual(new KeyValuePair<string, int>("Brad", 27), e.Current);
                Assert.False(await e.MoveNextAsync());
                await e.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AggregateBy_Expected2(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureKey,
            [Values] bool captureAccumulate,
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

                var e = AggregateBy(xs.ToAsyncEnumerable(), configured, async,
                    captureKey, x => x.Age,
                    "",
                    captureAccumulate, (acc, x) => acc + x.Name,
                    equalityComparer: GetDefaultOrNullComparer<int>(withComparer)).GetAsyncEnumerator();

                Assert.True(await e.MoveNextAsync());
                Assert.AreEqual(new KeyValuePair<int, string>(27, "Bart" + "Eric" + "Brad"), e.Current);
                Assert.True(await e.MoveNextAsync());
                Assert.AreEqual(new KeyValuePair<int, string>(62, "John"), e.Current);
                Assert.True(await e.MoveNextAsync());
                Assert.AreEqual(new KeyValuePair<int, string>(14, "Lisa"), e.Current);
                Assert.True(await e.MoveNextAsync());
                Assert.AreEqual(new KeyValuePair<int, string>(23, "Lisa"), e.Current);
                Assert.True(await e.MoveNextAsync());
                Assert.AreEqual(new KeyValuePair<int, string>(42, "Eric"), e.Current);
                Assert.False(await e.MoveNextAsync());
                await e.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AggregateBy_Throws_Source(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureKey,
            [Values] bool captureAccumulate,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var ex = new Exception("Bang!");
                var e = AggregateBy(AsyncEnumerable<int>.Rejected(ex), configured, async,
                    captureKey, x => x,
                    0,
                    captureAccumulate, (acc, x) => acc + x,
                    equalityComparer: GetDefaultOrNullComparer<int>(withComparer)).GetAsyncEnumerator();
                await TestHelper.AssertThrowsAsync(() => e.MoveNextAsync(), ex);
                await e.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AggregateBy_KeySelectorThrows(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureKey,
            [Values] bool captureAccumulate,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var ex = new Exception("Bang!");
                var e = AggregateBy(AsyncEnumerable.Return(42), configured, async,
                    captureKey, x => { throw ex; return x; },
                    0,
                    captureAccumulate, (acc, x) => acc + x,
                    equalityComparer: GetDefaultOrNullComparer<int>(withComparer)).GetAsyncEnumerator();
                await TestHelper.AssertThrowsAsync(() => e.MoveNextAsync(), ex);
                await e.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AggregateBy_AccumulatorThrows(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureKey,
            [Values] bool captureAccumulate,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var ex = new Exception("Bang!");
                var e = AggregateBy(AsyncEnumerable.Return(42), configured, async,
                    captureKey, x => x,
                    0,
                    captureAccumulate, (acc, x) => { throw ex; return acc + x; },
                    equalityComparer: GetDefaultOrNullComparer<int>(withComparer)).GetAsyncEnumerator();
                await TestHelper.AssertThrowsAsync(() => e.MoveNextAsync(), ex);
                await e.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AggregateBy_Comparer_Simple(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureKey,
            [Values] bool captureAccumulate)
        {
            Promise.Run(async () =>
            {
                var e = AggregateBy(AsyncEnumerable.Range(0, 10), configured, async,
                    captureKey, x => x,
                    0,
                    captureAccumulate, (acc, x) => acc + x,
                    equalityComparer: new EqMod(3)).GetAsyncEnumerator();
                Assert.True(await e.MoveNextAsync());
                Assert.AreEqual(new KeyValuePair<int, int>(0, 0 + 3 + 6 + 9), e.Current);
                Assert.True(await e.MoveNextAsync());
                Assert.AreEqual(new KeyValuePair<int, int>(1, 1 + 4 + 7), e.Current);
                Assert.True(await e.MoveNextAsync());
                Assert.AreEqual(new KeyValuePair<int, int>(2, 2 + 5 + 8), e.Current);
                Assert.False(await e.MoveNextAsync());
                await e.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AggregateBy_Comparer_Count(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureKey,
            [Values] bool captureAccumulate)
        {
            Promise.Run(async () =>
            {
                var ys = AggregateBy(AsyncEnumerable.Range(0, 10), configured, async,
                    captureKey, x => x,
                    0,
                    captureAccumulate, (acc, x) => acc + x,
                    equalityComparer: new EqMod(3));
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
        public void AggregateBy_Cancel(
            [Values] ConfiguredType configuredType,
            [Values] bool async,
            [Values] bool captureKey,
            [Values] bool captureAccumulate,
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
                        var asyncEnumerator = AggregateBy(xs, configuredType != ConfiguredType.NotConfigured, async,
                            captureKey, x =>
                            {
                                if (x == 2)
                                {
                                    configuredCancelationSource.Cancel();
                                    enumeratorCancelationSource.Cancel();
                                }
                                return x;
                            },
                            0,
                            captureAccumulate, (acc, x) => acc + x,
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