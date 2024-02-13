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
    public class GroupByTests
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
        public void GroupBy_NullArgumentThrows()
        {
            var enumerable = AsyncEnumerable.Return(42);
            var captureValue = "captureValue";
            var nullComparer = default(IEqualityComparer<int>);

            Assert.Catch<System.ArgumentNullException>(() => enumerable.GroupBy(default(Func<int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.GroupBy(default(Func<int, int>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.GroupBy(x => 0, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.GroupBy(captureValue, default(Func<string, int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.GroupBy(captureValue, default(Func<string, int, int>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.GroupBy(captureValue, (cv, x) => 0, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.GroupBy(default(Func<int, int>), x => 0));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.GroupBy(x => 0, default(Func<int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.GroupBy(default(Func<int, int>), x => 0, EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.GroupBy(x => 0, default(Func<int, int>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.GroupBy(x => 0, x => 0, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.GroupBy(captureValue, default(Func<string, int, int>), x => 0));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.GroupBy(captureValue, (cv, x) => 0, default(Func<int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.GroupBy(captureValue, default(Func<string, int, int>), x => 0, EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.GroupBy(captureValue, (cv, x) => 0, default(Func<int, int>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.GroupBy(captureValue, (cv, x) => 0, x => 0, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.GroupBy(default(Func<int, int>), captureValue, (cv, x) => 0));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.GroupBy(x => 0, captureValue, default(Func<string, int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.GroupBy(default(Func<int, int>), captureValue, (cv, x) => 0, EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.GroupBy(x => 0, captureValue, default(Func<string, int, int>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.GroupBy(x => 0, captureValue, (cv, x) => 0, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.GroupBy(captureValue, default(Func<string, int, int>), captureValue, (cv, x) => 0));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.GroupBy(captureValue, (cv, x) => 0, captureValue, default(Func<string, int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.GroupBy(captureValue, default(Func<string, int, int>), captureValue, (cv, x) => 0, EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.GroupBy(captureValue, (cv, x) => 0, captureValue, default(Func<string, int, int>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.GroupBy(captureValue, (cv, x) => 0, captureValue, (cv, x) => 0, nullComparer));


            Assert.Catch<System.ArgumentNullException>(() => enumerable.GroupBy(default(Func<int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.GroupBy(default(Func<int, Promise<int>>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.GroupBy(x => Promise.Resolved(0), nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.GroupBy(captureValue, default(Func<string, int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.GroupBy(captureValue, default(Func<string, int, Promise<int>>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.GroupBy(captureValue, (cv, x) => Promise.Resolved(0), nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.GroupBy(default(Func<int, Promise<int>>), x => Promise.Resolved(0)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.GroupBy(x => Promise.Resolved(0), default(Func<int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.GroupBy(default(Func<int, Promise<int>>), x => Promise.Resolved(0), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.GroupBy(x => Promise.Resolved(0), default(Func<int, Promise<int>>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.GroupBy(x => Promise.Resolved(0), x => Promise.Resolved(0), nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.GroupBy(captureValue, default(Func<string, int, Promise<int>>), x => Promise.Resolved(0)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.GroupBy(captureValue, (cv, x) => Promise.Resolved(0), default(Func<int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.GroupBy(captureValue, default(Func<string, int, Promise<int>>), x => Promise.Resolved(0), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.GroupBy(captureValue, (cv, x) => Promise.Resolved(0), default(Func<int, Promise<int>>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.GroupBy(captureValue, (cv, x) => Promise.Resolved(0), x => Promise.Resolved(0), nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.GroupBy(default(Func<int, Promise<int>>), captureValue, (cv, x) => Promise.Resolved(0)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.GroupBy(x => Promise.Resolved(0), captureValue, default(Func<string, int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.GroupBy(default(Func<int, Promise<int>>), captureValue, (cv, x) => Promise.Resolved(0), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.GroupBy(x => Promise.Resolved(0), captureValue, default(Func<string, int, Promise<int>>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.GroupBy(x => Promise.Resolved(0), captureValue, (cv, x) => Promise.Resolved(0), nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.GroupBy(captureValue, default(Func<string, int, Promise<int>>), captureValue, (cv, x) => Promise.Resolved(0)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.GroupBy(captureValue, (cv, x) => Promise.Resolved(0), captureValue, default(Func<string, int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.GroupBy(captureValue, default(Func<string, int, Promise<int>>), captureValue, (cv, x) => Promise.Resolved(0), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.GroupBy(captureValue, (cv, x) => Promise.Resolved(0), captureValue, default(Func<string, int, Promise<int>>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.GroupBy(captureValue, (cv, x) => Promise.Resolved(0), captureValue, (cv, x) => Promise.Resolved(0), nullComparer));


            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).GroupBy(default(Func<int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).GroupBy(default(Func<int, int>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).GroupBy(x => 0, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).GroupBy(captureValue, default(Func<string, int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).GroupBy(captureValue, default(Func<string, int, int>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).GroupBy(captureValue, (cv, x) => 0, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).GroupBy(default(Func<int, int>), x => 0));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).GroupBy(x => 0, default(Func<int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).GroupBy(default(Func<int, int>), x => 0, EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).GroupBy(x => 0, default(Func<int, int>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).GroupBy(x => 0, x => 0, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).GroupBy(captureValue, default(Func<string, int, int>), x => 0));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).GroupBy(captureValue, (cv, x) => 0, default(Func<int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).GroupBy(captureValue, default(Func<string, int, int>), x => 0, EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).GroupBy(captureValue, (cv, x) => 0, default(Func<int, int>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).GroupBy(captureValue, (cv, x) => 0, x => 0, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).GroupBy(default(Func<int, int>), captureValue, (cv, x) => 0));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).GroupBy(x => 0, captureValue, default(Func<string, int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).GroupBy(default(Func<int, int>), captureValue, (cv, x) => 0, EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).GroupBy(x => 0, captureValue, default(Func<string, int, int>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).GroupBy(x => 0, captureValue, (cv, x) => 0, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).GroupBy(captureValue, default(Func<string, int, int>), captureValue, (cv, x) => 0));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).GroupBy(captureValue, (cv, x) => 0, captureValue, default(Func<string, int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).GroupBy(captureValue, default(Func<string, int, int>), captureValue, (cv, x) => 0, EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).GroupBy(captureValue, (cv, x) => 0, captureValue, default(Func<string, int, int>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).GroupBy(captureValue, (cv, x) => 0, captureValue, (cv, x) => 0, nullComparer));


            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).GroupBy(default(Func<int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).GroupBy(default(Func<int, Promise<int>>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).GroupBy(x => Promise.Resolved(0), nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).GroupBy(captureValue, default(Func<string, int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).GroupBy(captureValue, default(Func<string, int, Promise<int>>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).GroupBy(captureValue, (cv, x) => Promise.Resolved(0), nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).GroupBy(default(Func<int, Promise<int>>), x => Promise.Resolved(0)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).GroupBy(x => Promise.Resolved(0), default(Func<int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).GroupBy(default(Func<int, Promise<int>>), x => Promise.Resolved(0), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).GroupBy(x => Promise.Resolved(0), default(Func<int, Promise<int>>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).GroupBy(x => Promise.Resolved(0), x => Promise.Resolved(0), nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).GroupBy(captureValue, default(Func<string, int, Promise<int>>), x => Promise.Resolved(0)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).GroupBy(captureValue, (cv, x) => Promise.Resolved(0), default(Func<int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).GroupBy(captureValue, default(Func<string, int, Promise<int>>), x => Promise.Resolved(0), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).GroupBy(captureValue, (cv, x) => Promise.Resolved(0), default(Func<int, Promise<int>>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).GroupBy(captureValue, (cv, x) => Promise.Resolved(0), x => Promise.Resolved(0), nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).GroupBy(default(Func<int, Promise<int>>), captureValue, (cv, x) => Promise.Resolved(0)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).GroupBy(x => Promise.Resolved(0), captureValue, default(Func<string, int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).GroupBy(default(Func<int, Promise<int>>), captureValue, (cv, x) => Promise.Resolved(0), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).GroupBy(x => Promise.Resolved(0), captureValue, default(Func<string, int, Promise<int>>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).GroupBy(x => Promise.Resolved(0), captureValue, (cv, x) => Promise.Resolved(0), nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).GroupBy(captureValue, default(Func<string, int, Promise<int>>), captureValue, (cv, x) => Promise.Resolved(0)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).GroupBy(captureValue, (cv, x) => Promise.Resolved(0), captureValue, default(Func<string, int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).GroupBy(captureValue, default(Func<string, int, Promise<int>>), captureValue, (cv, x) => Promise.Resolved(0), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).GroupBy(captureValue, (cv, x) => Promise.Resolved(0), captureValue, default(Func<string, int, Promise<int>>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).GroupBy(captureValue, (cv, x) => Promise.Resolved(0), captureValue, (cv, x) => Promise.Resolved(0), nullComparer));

            enumerable.GetAsyncEnumerator().DisposeAsync().Forget();
        }
#endif //PROMISE_DEBUG

        // We test all the different overloads.
        private static AsyncEnumerable<Grouping<TKey, TSource>> GroupBy<TSource, TKey>(AsyncEnumerable<TSource> asyncEnumerable,
            bool configured,
            bool async,
            Func<TSource, TKey> keySelector, bool captureKey,
            IEqualityComparer<TKey> equalityComparer = null,
            CancelationToken configuredCancelationToken = default)
        {
            if (configured)
            {
                return GroupBy(asyncEnumerable.ConfigureAwait(SynchronizationOption.Foreground).WithCancelation(configuredCancelationToken), async, keySelector, captureKey, equalityComparer);
            }

            const string keyCapture = "keyCapture";

            if (!captureKey)
            {
                return async
                    ? equalityComparer != null
                        ? asyncEnumerable.GroupBy(async x => keySelector(x), equalityComparer)
                        : asyncEnumerable.GroupBy(async x => keySelector(x))
                    : equalityComparer != null
                        ? asyncEnumerable.GroupBy(keySelector, equalityComparer)
                        : asyncEnumerable.GroupBy(keySelector);
            }
            else
            {
                return async
                    ? equalityComparer != null
                        ? asyncEnumerable.GroupBy(keyCapture, async (cv, x) =>
                        {
                            Assert.AreEqual(keyCapture, cv);
                            return keySelector(x);
                        }, equalityComparer)
                        : asyncEnumerable.GroupBy(keyCapture, async (cv, x) =>
                        {
                            Assert.AreEqual(keyCapture, cv);
                            return keySelector(x);
                        })
                    : equalityComparer != null
                        ? asyncEnumerable.GroupBy(keyCapture, (cv, x) =>
                        {
                            Assert.AreEqual(keyCapture, cv);
                            return keySelector(x);
                        }, equalityComparer)
                        : asyncEnumerable.GroupBy(keyCapture, (cv, x) =>
                        {
                            Assert.AreEqual(keyCapture, cv);
                            return keySelector(x);
                        });
            }
        }

        private static AsyncEnumerable<Grouping<TKey, TElement>> GroupBy<TSource, TKey, TElement>(AsyncEnumerable<TSource> asyncEnumerable,
            bool configured,
            bool async,
            Func<TSource, TKey> keySelector, bool captureKey,
            Func<TSource, TElement> elementSelector, bool captureElement,
            IEqualityComparer<TKey> equalityComparer = null,
            CancelationToken configuredCancelationToken = default)
        {
            if (configured)
            {
                return GroupBy(asyncEnumerable.ConfigureAwait(SynchronizationOption.Foreground).WithCancelation(configuredCancelationToken), async, keySelector, captureKey, elementSelector, captureElement, equalityComparer);
            }

            const string keyCapture = "keyCapture";
            const string elementCapture = "elementCapture";

            if (!captureKey)
            {
                if (!captureElement)
                {
                    return async
                        ? equalityComparer != null
                            ? asyncEnumerable.GroupBy(async x => keySelector(x), async x => elementSelector(x), equalityComparer)
                            : asyncEnumerable.GroupBy(async x => keySelector(x), async x => elementSelector(x))
                        : equalityComparer != null
                            ? asyncEnumerable.GroupBy(keySelector, elementSelector, equalityComparer)
                            : asyncEnumerable.GroupBy(keySelector, elementSelector);
                }
                else
                {
                    return async
                        ? equalityComparer != null
                            ? asyncEnumerable.GroupBy(async x => keySelector(x),
                            elementCapture, async (cv, x) =>
                            {
                                Assert.AreEqual(elementCapture, cv);
                                return elementSelector(x);
                            }, equalityComparer)
                            : asyncEnumerable.GroupBy(async x => keySelector(x),
                            elementCapture, async (cv, x) =>
                            {
                                Assert.AreEqual(elementCapture, cv);
                                return elementSelector(x);
                            })
                        : equalityComparer != null
                            ? asyncEnumerable.GroupBy(x => keySelector(x),
                            elementCapture, (cv, x) =>
                            {
                                Assert.AreEqual(elementCapture, cv);
                                return elementSelector(x);
                            }, equalityComparer)
                            : asyncEnumerable.GroupBy(x => keySelector(x),
                            elementCapture, (cv, x) =>
                            {
                                Assert.AreEqual(elementCapture, cv);
                                return elementSelector(x);
                            });
                }
            }
            else
            {
                if (!captureElement)
                {
                    return async
                        ? equalityComparer != null
                            ? asyncEnumerable.GroupBy(keyCapture, async (cv, x) =>
                            {
                                Assert.AreEqual(keyCapture, cv);
                                return keySelector(x);
                            }, async x => elementSelector(x), equalityComparer)
                            : asyncEnumerable.GroupBy(keyCapture, async (cv, x) =>
                            {
                                Assert.AreEqual(keyCapture, cv);
                                return keySelector(x);
                            }, async x => elementSelector(x))
                        : equalityComparer != null
                            ? asyncEnumerable.GroupBy(keyCapture, (cv, x) =>
                            {
                                Assert.AreEqual(keyCapture, cv);
                                return keySelector(x);
                            }, elementSelector, equalityComparer)
                            : asyncEnumerable.GroupBy(keyCapture, (cv, x) =>
                            {
                                Assert.AreEqual(keyCapture, cv);
                                return keySelector(x);
                            }, elementSelector);
                }
                else
                {
                    return async
                        ? equalityComparer != null
                            ? asyncEnumerable.GroupBy(keyCapture, async (cv, x) =>
                            {
                                Assert.AreEqual(keyCapture, cv);
                                return keySelector(x);
                            },
                            elementCapture, async (cv, x) =>
                            {
                                Assert.AreEqual(elementCapture, cv);
                                return elementSelector(x);
                            }, equalityComparer)
                            : asyncEnumerable.GroupBy(keyCapture, async (cv, x) =>
                            {
                                Assert.AreEqual(keyCapture, cv);
                                return keySelector(x);
                            },
                            elementCapture, async (cv, x) =>
                            {
                                Assert.AreEqual(elementCapture, cv);
                                return elementSelector(x);
                            })
                        : equalityComparer != null
                            ? asyncEnumerable.GroupBy(keyCapture, (cv, x) =>
                            {
                                Assert.AreEqual(keyCapture, cv);
                                return keySelector(x);
                            },
                            elementCapture, (cv, x) =>
                            {
                                Assert.AreEqual(elementCapture, cv);
                                return elementSelector(x);
                            }, equalityComparer)
                            : asyncEnumerable.GroupBy(keyCapture, (cv, x) =>
                            {
                                Assert.AreEqual(keyCapture, cv);
                                return keySelector(x);
                            },
                            elementCapture, (cv, x) =>
                            {
                                Assert.AreEqual(elementCapture, cv);
                                return elementSelector(x);
                            });
                }
            }
        }

        private static AsyncEnumerable<Grouping<TKey, TSource>> GroupBy<TSource, TKey>(ConfiguredAsyncEnumerable<TSource> asyncEnumerable,
            bool async,
            Func<TSource, TKey> keySelector, bool captureKey,
            IEqualityComparer<TKey> equalityComparer = null)
        {
            const string keyCapture = "keyCapture";

            if (!captureKey)
            {
                return async
                    ? equalityComparer != null
                        ? asyncEnumerable.GroupBy(async x => keySelector(x), equalityComparer)
                        : asyncEnumerable.GroupBy(async x => keySelector(x))
                    : equalityComparer != null
                        ? asyncEnumerable.GroupBy(keySelector, equalityComparer)
                        : asyncEnumerable.GroupBy(keySelector);
            }
            else
            {
                return async
                    ? equalityComparer != null
                        ? asyncEnumerable.GroupBy(keyCapture, async (cv, x) =>
                        {
                            Assert.AreEqual(keyCapture, cv);
                            return keySelector(x);
                        }, equalityComparer)
                        : asyncEnumerable.GroupBy(keyCapture, async (cv, x) =>
                        {
                            Assert.AreEqual(keyCapture, cv);
                            return keySelector(x);
                        })
                    : equalityComparer != null
                        ? asyncEnumerable.GroupBy(keyCapture, (cv, x) =>
                        {
                            Assert.AreEqual(keyCapture, cv);
                            return keySelector(x);
                        }, equalityComparer)
                        : asyncEnumerable.GroupBy(keyCapture, (cv, x) =>
                        {
                            Assert.AreEqual(keyCapture, cv);
                            return keySelector(x);
                        });
            }
        }

        private static AsyncEnumerable<Grouping<TKey, TElement>> GroupBy<TSource, TKey, TElement>(ConfiguredAsyncEnumerable<TSource> asyncEnumerable,
            bool async,
            Func<TSource, TKey> keySelector, bool captureKey,
            Func<TSource, TElement> elementSelector, bool captureElement,
            IEqualityComparer<TKey> equalityComparer = null)
        {
            const string keyCapture = "keyCapture";
            const string elementCapture = "elementCapture";

            if (!captureKey)
            {
                if (!captureElement)
                {
                    return async
                        ? equalityComparer != null
                            ? asyncEnumerable.GroupBy(async x => keySelector(x), async x => elementSelector(x), equalityComparer)
                            : asyncEnumerable.GroupBy(async x => keySelector(x), async x => elementSelector(x))
                        : equalityComparer != null
                            ? asyncEnumerable.GroupBy(keySelector, elementSelector, equalityComparer)
                            : asyncEnumerable.GroupBy(keySelector, elementSelector);
                }
                else
                {
                    return async
                        ? equalityComparer != null
                            ? asyncEnumerable.GroupBy(async x => keySelector(x),
                            elementCapture, async (cv, x) =>
                            {
                                Assert.AreEqual(elementCapture, cv);
                                return elementSelector(x);
                            }, equalityComparer)
                            : asyncEnumerable.GroupBy(async x => keySelector(x),
                            elementCapture, async (cv, x) =>
                            {
                                Assert.AreEqual(elementCapture, cv);
                                return elementSelector(x);
                            })
                        : equalityComparer != null
                            ? asyncEnumerable.GroupBy(x => keySelector(x),
                            elementCapture, (cv, x) =>
                            {
                                Assert.AreEqual(elementCapture, cv);
                                return elementSelector(x);
                            }, equalityComparer)
                            : asyncEnumerable.GroupBy(x => keySelector(x),
                            elementCapture, (cv, x) =>
                            {
                                Assert.AreEqual(elementCapture, cv);
                                return elementSelector(x);
                            });
                }
            }
            else
            {
                if (!captureElement)
                {
                    return async
                        ? equalityComparer != null
                            ? asyncEnumerable.GroupBy(keyCapture, async (cv, x) =>
                            {
                                Assert.AreEqual(keyCapture, cv);
                                return keySelector(x);
                            }, async x => elementSelector(x), equalityComparer)
                            : asyncEnumerable.GroupBy(keyCapture, async (cv, x) =>
                            {
                                Assert.AreEqual(keyCapture, cv);
                                return keySelector(x);
                            }, async x => elementSelector(x))
                        : equalityComparer != null
                            ? asyncEnumerable.GroupBy(keyCapture, (cv, x) =>
                            {
                                Assert.AreEqual(keyCapture, cv);
                                return keySelector(x);
                            }, elementSelector, equalityComparer)
                            : asyncEnumerable.GroupBy(keyCapture, (cv, x) =>
                            {
                                Assert.AreEqual(keyCapture, cv);
                                return keySelector(x);
                            }, elementSelector);
                }
                else
                {
                    return async
                        ? equalityComparer != null
                            ? asyncEnumerable.GroupBy(keyCapture, async (cv, x) =>
                            {
                                Assert.AreEqual(keyCapture, cv);
                                return keySelector(x);
                            },
                            elementCapture, async (cv, x) =>
                            {
                                Assert.AreEqual(elementCapture, cv);
                                return elementSelector(x);
                            }, equalityComparer)
                            : asyncEnumerable.GroupBy(keyCapture, async (cv, x) =>
                            {
                                Assert.AreEqual(keyCapture, cv);
                                return keySelector(x);
                            },
                            elementCapture, async (cv, x) =>
                            {
                                Assert.AreEqual(elementCapture, cv);
                                return elementSelector(x);
                            })
                        : equalityComparer != null
                            ? asyncEnumerable.GroupBy(keyCapture, (cv, x) =>
                            {
                                Assert.AreEqual(keyCapture, cv);
                                return keySelector(x);
                            },
                            elementCapture, (cv, x) =>
                            {
                                Assert.AreEqual(elementCapture, cv);
                                return elementSelector(x);
                            }, equalityComparer)
                            : asyncEnumerable.GroupBy(keyCapture, (cv, x) =>
                            {
                                Assert.AreEqual(keyCapture, cv);
                                return keySelector(x);
                            },
                            elementCapture, (cv, x) =>
                            {
                                Assert.AreEqual(elementCapture, cv);
                                return elementSelector(x);
                            });
                }
            }
        }

        private static IEqualityComparer<T> GetDefaultOrNullComparer<T>(bool defaultComparer)
        {
            return defaultComparer ? EqualityComparer<T>.Default : null;
        }

        [Test]
        public void GroupBy_KeySelector_Simple1(
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

                var e = GroupBy(xs.ToAsyncEnumerable(), configured, async, x => x.Age / 10, captureKey, equalityComparer: GetDefaultOrNullComparer<int>(withComparer)).GetAsyncEnumerator();

                Assert.True(await e.MoveNextAsync());
                Assert.AreEqual(2, e.Current.Key);
                var g1 = e.Current.Elements;
                Assert.AreEqual(4, g1.Count);
                Assert.AreEqual(xs[0], g1[0]);
                Assert.AreEqual(xs[2], g1[1]);
                Assert.AreEqual(xs[4], g1[2]);
                Assert.AreEqual(xs[5], g1[3]);

                Assert.True(await e.MoveNextAsync());
                Assert.AreEqual(6, e.Current.Key);
                var g2 = e.Current.Elements;
                Assert.AreEqual(1, g2.Count);
                Assert.AreEqual(xs[1], g2[0]);

                Assert.True(await e.MoveNextAsync());
                Assert.AreEqual(1, e.Current.Key);
                var g3 = e.Current.Elements;
                Assert.AreEqual(1, g3.Count);
                Assert.AreEqual(xs[3], g3[0]);

                Assert.True(await e.MoveNextAsync());
                Assert.AreEqual(4, e.Current.Key);
                var g4 = e.Current.Elements;
                Assert.AreEqual(1, g4.Count);
                Assert.AreEqual(xs[6], g4[0]);

                Assert.False(await e.MoveNextAsync());
                await e.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void GroupBy_KeySelector_Simple2_TempCollectionIsStillValidAfterMoveNextAsyncUntilDisposeAsync(
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

                var e = GroupBy(xs.ToAsyncEnumerable(), configured, async, x => x.Age / 10, captureKey, equalityComparer: GetDefaultOrNullComparer<int>(withComparer)).GetAsyncEnumerator();
                Assert.True(await e.MoveNextAsync());
                var g1 = e.Current;

                Assert.True(await e.MoveNextAsync());
                var g2 = e.Current;

                Assert.True(await e.MoveNextAsync());
                var g3 = e.Current;

                Assert.True(await e.MoveNextAsync());
                var g4 = e.Current;

                Assert.AreEqual(2, g1.Key);
                Assert.AreEqual(xs[0], g1.Elements[0]);
                Assert.AreEqual(xs[2], g1.Elements[1]);
                Assert.AreEqual(xs[4], g1.Elements[2]);
                Assert.AreEqual(xs[5], g1.Elements[3]);

                Assert.AreEqual(6, g2.Key);
                Assert.AreEqual(1, g2.Elements.Count);
                Assert.AreEqual(xs[1], g2.Elements[0]);

                Assert.AreEqual(1, g3.Key);
                Assert.AreEqual(1, g3.Elements.Count);
                Assert.AreEqual(xs[3], g3.Elements[0]);

                Assert.AreEqual(4, g4.Key);
                Assert.AreEqual(1, g4.Elements.Count);
                Assert.AreEqual(xs[6], g4.Elements[0]);

                Assert.False(await e.MoveNextAsync());
                await e.DisposeAsync();

                TempCollectionTests.AssertIsInvalid(g1.Elements);
                TempCollectionTests.AssertIsInvalid(g2.Elements);
                TempCollectionTests.AssertIsInvalid(g3.Elements);
                TempCollectionTests.AssertIsInvalid(g4.Elements);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void GroupBy_KeySelector_Simple2_TempCollectionToArrayIsPersistedAfterMoveNextAndDisposeAsync(
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

                var e = GroupBy(xs.ToAsyncEnumerable(), configured, async, x => x.Age / 10, captureKey, equalityComparer: GetDefaultOrNullComparer<int>(withComparer)).GetAsyncEnumerator();
                Assert.True(await e.MoveNextAsync());
                var g1k = e.Current.Key;
                var g1a = e.Current.Elements.ToArray();

                Assert.True(await e.MoveNextAsync());
                var g2k = e.Current.Key;
                var g2a = e.Current.Elements.ToArray();

                Assert.True(await e.MoveNextAsync());
                var g3k = e.Current.Key;
                var g3a = e.Current.Elements.ToArray();

                Assert.True(await e.MoveNextAsync());
                var g4k = e.Current.Key;
                var g4a = e.Current.Elements.ToArray();

                Assert.False(await e.MoveNextAsync());
                await e.DisposeAsync();

                Assert.AreEqual(2, g1k);
                Assert.AreEqual(4, g1a.Length);
                Assert.AreEqual(xs[0], g1a[0]);
                Assert.AreEqual(xs[2], g1a[1]);
                Assert.AreEqual(xs[4], g1a[2]);
                Assert.AreEqual(xs[5], g1a[3]);

                Assert.AreEqual(6, g2k);
                Assert.AreEqual(1, g2a.Length);
                Assert.AreEqual(xs[1], g2a[0]);

                Assert.AreEqual(1, g3k);
                Assert.AreEqual(1, g3a.Length);
                Assert.AreEqual(xs[3], g3a[0]);

                Assert.AreEqual(4, g4k);
                Assert.AreEqual(1, g4a.Length);
                Assert.AreEqual(xs[6], g4a[0]);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void GroupBy_KeySelector_Empty(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureKey,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var e = GroupBy(AsyncEnumerable.Empty<int>(), configured, async, x => x, captureKey, equalityComparer: GetDefaultOrNullComparer<int>(withComparer)).GetAsyncEnumerator();
                Assert.False(await e.MoveNextAsync());
                await e.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void GroupBy_KeySelector_Throws_Source1(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureKey)
        {
            Promise.Run(async () =>
            {
                var ex = new Exception("Bang!");
                var e = GroupBy(AsyncEnumerable<int>.Rejected(ex), configured, async, x => x, captureKey).GetAsyncEnumerator();
                await TestHelper.AssertThrowsAsync(() => e.MoveNextAsync(), ex);
                await e.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void GroupBy_KeySelector_Throws_Source2(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureKey,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var ex = new Exception("Bang!");
                var e = GroupBy(GetXs(ex).ToAsyncEnumerable(), configured, async, x => x, captureKey, equalityComparer: GetDefaultOrNullComparer<int>(withComparer)).GetAsyncEnumerator();
                await TestHelper.AssertThrowsAsync(() => e.MoveNextAsync(), ex);
                await e.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        private static IEnumerable<int> GetXs(Exception ex)
        {
            yield return 42;
            yield return 43;
            throw ex;
        }

        [Test]
        public void GroupBy_KeySelector_Throws_KeySelector1(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureKey,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var ex = new Exception("Bang!");
                var e = GroupBy(AsyncEnumerable.Return(42), configured, async, x => { throw ex; return x; }, captureKey, equalityComparer: GetDefaultOrNullComparer<int>(withComparer)).GetAsyncEnumerator();
                await TestHelper.AssertThrowsAsync(() => e.MoveNextAsync(), ex);
                await e.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void GroupBy_KeySelector_Throws_KeySelector2(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureKey,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var ex = new Exception("Bang!");
                var e = GroupBy(new[] { 1, 2, 3 }.ToAsyncEnumerable(), configured, async, x => { if (x == 3) throw ex; return x; }, captureKey, equalityComparer: GetDefaultOrNullComparer<int>(withComparer)).GetAsyncEnumerator();
                await TestHelper.AssertThrowsAsync(() => e.MoveNextAsync(), ex);
                await e.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void GroupBy_KeySelector_Comparer_Simple(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureKey)
        {
            Promise.Run(async () =>
            {
                var ys = GroupBy(AsyncEnumerable.Range(0, 10), configured, async, x => x, captureKey, new EqMod(3));

                var e = ys.GetAsyncEnumerator();

                Assert.True(await e.MoveNextAsync());
                var g1 = e.Current;
                Assert.AreEqual(0, g1.Key);
                var g1e = g1.Elements;
                Assert.AreEqual(4, g1e.Count);
                Assert.AreEqual(0, g1e[0]);
                Assert.AreEqual(3, g1e[1]);
                Assert.AreEqual(6, g1e[2]);
                Assert.AreEqual(9, g1e[3]);

                Assert.True(await e.MoveNextAsync());
                var g2 = e.Current;
                Assert.AreEqual(1, g2.Key);
                var g2e = g2.Elements;
                Assert.AreEqual(3, g2e.Count);
                Assert.AreEqual(1, g2e[0]);
                Assert.AreEqual(4, g2e[1]);
                Assert.AreEqual(7, g2e[2]);

                Assert.True(await e.MoveNextAsync());
                var g3 = e.Current;
                Assert.AreEqual(2, g3.Key);
                var g3e = g3.Elements;
                Assert.AreEqual(3, g3e.Count);
                Assert.AreEqual(2, g3e[0]);
                Assert.AreEqual(5, g3e[1]);
                Assert.AreEqual(8, g3e[2]);

                Assert.False(await e.MoveNextAsync());
                await e.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void GroupBy_KeySelector_Comparer_Count(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureKey)
        {
            Promise.Run(async () =>
            {
                var ys = GroupBy(AsyncEnumerable.Range(0, 10), configured, async, x => x, captureKey, new EqMod(3));
                Assert.AreEqual(3, await ys.CountAsync());
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void GroupBy_KeySelector_ElementSelector_Simple(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureKey,
            [Values] bool captureElement,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var e = GroupBy(AsyncEnumerable.Range(0, 10), configured, async, x => x % 3, captureKey, x => (char) ('a' + x), captureElement, equalityComparer: GetDefaultOrNullComparer<int>(withComparer)).GetAsyncEnumerator();

                Assert.True(await e.MoveNextAsync());
                var g1 = e.Current;
                Assert.AreEqual(0, g1.Key);
                var g1e = e.Current.Elements;
                Assert.AreEqual(4, g1e.Count);
                Assert.AreEqual('a', g1e[0]);
                Assert.AreEqual('d', g1e[1]);
                Assert.AreEqual('g', g1e[2]);
                Assert.AreEqual('j', g1e[3]);

                Assert.True(await e.MoveNextAsync());
                var g2 = e.Current;
                Assert.AreEqual(1, g2.Key);
                var g2e = e.Current.Elements;
                Assert.AreEqual(3, g2e.Count);
                Assert.AreEqual('b', g2e[0]);
                Assert.AreEqual('e', g2e[1]);
                Assert.AreEqual('h', g2e[2]);

                Assert.True(await e.MoveNextAsync());
                var g3 = e.Current;
                Assert.AreEqual(2, g3.Key);
                var g3e = e.Current.Elements;
                Assert.AreEqual(3, g3e.Count);
                Assert.AreEqual('c', g3e[0]);
                Assert.AreEqual('f', g3e[1]);
                Assert.AreEqual('i', g3e[2]);

                Assert.False(await e.MoveNextAsync());
                await e.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void GroupBy_KeySelector_ElementSelector_Count(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureKey,
            [Values] bool captureElement,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var ys = GroupBy(AsyncEnumerable.Range(0, 10), configured, async, x => x % 3, captureKey, x => (char) ('a' + x), captureElement, equalityComparer: GetDefaultOrNullComparer<int>(withComparer));
                Assert.AreEqual(3, await ys.CountAsync());
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void GroupBy_KeySelector_ElementSelector_Comparer_Simple1(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureKey,
            [Values] bool captureElement)
        {
            Promise.Run(async () =>
            {
                var e = GroupBy(AsyncEnumerable.Range(0, 10), configured, async, x => x, captureKey, x => (char) ('a' + x), captureElement, new EqMod(3)).GetAsyncEnumerator();

                Assert.True(await e.MoveNextAsync());
                var g1 = e.Current;
                Assert.AreEqual(0, g1.Key);
                var g1e = e.Current.Elements;
                Assert.AreEqual(4, g1e.Count);
                Assert.AreEqual('a', g1e[0]);
                Assert.AreEqual('d', g1e[1]);
                Assert.AreEqual('g', g1e[2]);
                Assert.AreEqual('j', g1e[3]);

                Assert.True(await e.MoveNextAsync());
                var g2 = e.Current;
                Assert.AreEqual(1, g2.Key);
                var g2e = e.Current.Elements;
                Assert.AreEqual(3, g2e.Count);
                Assert.AreEqual('b', g2e[0]);
                Assert.AreEqual('e', g2e[1]);
                Assert.AreEqual('h', g2e[2]);

                Assert.True(await e.MoveNextAsync());
                var g3 = e.Current;
                Assert.AreEqual(2, g3.Key);
                var g3e = e.Current.Elements;
                Assert.AreEqual(3, g3e.Count);
                Assert.AreEqual('c', g3e[0]);
                Assert.AreEqual('f', g3e[1]);
                Assert.AreEqual('i', g3e[2]);

                Assert.False(await e.MoveNextAsync());
                await e.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void GroupBy_KeySelector_ElementSelector_Comparer_TempCollectionIsInvalidatedAfterDisposeAsync(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureKey,
            [Values] bool captureElement)
        {
            Promise.Run(async () =>
            {
                var e = GroupBy(AsyncEnumerable.Range(0, 10), configured, async, x => x, captureKey, x => (char) ('a' + x), captureElement, new EqMod(3)).GetAsyncEnumerator();

                Assert.True(await e.MoveNextAsync());
                var g1 = e.Current;
                Assert.AreEqual(0, g1.Key);
                var g1e = e.Current.Elements;
                Assert.AreEqual(4, g1e.Count);
                Assert.AreEqual('a', g1e[0]);
                Assert.AreEqual('d', g1e[1]);
                Assert.AreEqual('g', g1e[2]);
                Assert.AreEqual('j', g1e[3]);

                await e.DisposeAsync();

                TempCollectionTests.AssertIsInvalid(g1e);
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
        public void GroupBy_KeySelector_Cancel(
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
                        var asyncEnumerator = GroupBy(xs, configuredType != ConfiguredType.NotConfigured, async, x =>
                            {
                                if (x == 2)
                                {
                                    configuredCancelationSource.Cancel();
                                    enumeratorCancelationSource.Cancel();
                                }
                                return x;
                            }, captureKey,
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

        [Test]
        public void GroupBy_KeySelector_ElementSelector_Cancel(
            [Values] ConfiguredType configuredType,
            [Values] bool async,
            [Values] bool captureKey,
            [Values] bool captureElement,
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
                        var asyncEnumerator = GroupBy(xs, configuredType != ConfiguredType.NotConfigured, async, x =>
                            {
                                if (x == 2)
                                {
                                    configuredCancelationSource.Cancel();
                                    enumeratorCancelationSource.Cancel();
                                }
                                return x;
                            }, captureKey, x => (char) ('a' + x), captureElement,
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