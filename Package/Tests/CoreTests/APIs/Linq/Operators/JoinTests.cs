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
    public static class JoinHelper
    {
        // We test all the different overloads.
        public static AsyncEnumerable<(TOuter Outer, TInner Inner)> Join<TOuter, TInner, TKey>(this AsyncEnumerable<TOuter> outer,
            AsyncEnumerable<TInner> inner,
            bool configured,
            bool async,
            Func<TOuter, TKey> outerKeySelector, bool captureOuterKey,
            Func<TInner, TKey> innerKeySelector, bool captureInnerKey,
            IEqualityComparer<TKey> equalityComparer = null,
            CancelationToken configuredCancelationToken = default)
        {
            if (configured)
            {
                return Join(outer.ConfigureAwait(SynchronizationOption.Foreground).WithCancelation(configuredCancelationToken), inner, async, innerKeySelector, captureInnerKey, outerKeySelector, captureOuterKey, equalityComparer);
            }

            const string outerKeyCapture = "outerKeyCapture";
            const string innerKeyCapture = "innerKeyCapture";

            if (!captureInnerKey)
            {
                if (!captureOuterKey)
                {
                    return async
                        ? equalityComparer != null
                            ? outer.Join(inner, async x => outerKeySelector(x), async x => innerKeySelector(x), equalityComparer)
                            : outer.Join(inner, async x => outerKeySelector(x), async x => innerKeySelector(x))
                        : equalityComparer != null
                            ? outer.Join(inner, outerKeySelector, innerKeySelector, equalityComparer)
                            : outer.Join(inner, outerKeySelector, innerKeySelector);
                }
                else
                {
                    return async
                        ? equalityComparer != null
                            ? outer.Join(inner, outerKeyCapture, async (cv, x) =>
                            {
                                Assert.AreEqual(outerKeyCapture, cv);
                                return outerKeySelector(x);
                            }, async x => innerKeySelector(x), equalityComparer)
                            : outer.Join(inner, outerKeyCapture, async (cv, x) =>
                            {
                                Assert.AreEqual(outerKeyCapture, cv);
                                return outerKeySelector(x);
                            }, async x => innerKeySelector(x))
                        : equalityComparer != null
                            ? outer.Join(inner, outerKeyCapture, (cv, x) =>
                            {
                                Assert.AreEqual(outerKeyCapture, cv);
                                return outerKeySelector(x);
                            }, innerKeySelector, equalityComparer)
                            : outer.Join(inner, outerKeyCapture, (cv, x) =>
                            {
                                Assert.AreEqual(outerKeyCapture, cv);
                                return outerKeySelector(x);
                            }, innerKeySelector);
                }
            }
            else
            {
                if (!captureOuterKey)
                {
                    return async
                        ? equalityComparer != null
                            ? outer.Join(inner, async x => outerKeySelector(x), innerKeyCapture, async (cv, x) =>
                            {
                                Assert.AreEqual(innerKeyCapture, cv);
                                return innerKeySelector(x);
                            }, equalityComparer)
                            : outer.Join(inner, async x => outerKeySelector(x), innerKeyCapture, async (cv, x) =>
                            {
                                Assert.AreEqual(innerKeyCapture, cv);
                                return innerKeySelector(x);
                            })
                        : equalityComparer != null
                            ? outer.Join(inner, outerKeySelector, innerKeyCapture, (cv, x) =>
                            {
                                Assert.AreEqual(innerKeyCapture, cv);
                                return innerKeySelector(x);
                            }, equalityComparer)
                            : outer.Join(inner, outerKeySelector, innerKeyCapture, (cv, x) =>
                            {
                                Assert.AreEqual(innerKeyCapture, cv);
                                return innerKeySelector(x);
                            });
                }
                else
                {
                    return async
                        ? equalityComparer != null
                            ? outer.Join(inner, outerKeyCapture, async (cv, x) =>
                            {
                                Assert.AreEqual(outerKeyCapture, cv);
                                return outerKeySelector(x);
                            }, innerKeyCapture, async (cv, x) =>
                            {
                                Assert.AreEqual(innerKeyCapture, cv);
                                return innerKeySelector(x);
                            }, equalityComparer)
                            : outer.Join(inner, outerKeyCapture, async (cv, x) =>
                            {
                                Assert.AreEqual(outerKeyCapture, cv);
                                return outerKeySelector(x);
                            }, innerKeyCapture, async (cv, x) =>
                            {
                                Assert.AreEqual(innerKeyCapture, cv);
                                return innerKeySelector(x);
                            })
                        : equalityComparer != null
                            ? outer.Join(inner, outerKeyCapture, (cv, x) =>
                            {
                                Assert.AreEqual(outerKeyCapture, cv);
                                return outerKeySelector(x);
                            }, innerKeyCapture, (cv, x) =>
                            {
                                Assert.AreEqual(innerKeyCapture, cv);
                                return innerKeySelector(x);
                            }, equalityComparer)
                            : outer.Join(inner, outerKeyCapture, (cv, x) =>
                            {
                                Assert.AreEqual(outerKeyCapture, cv);
                                return outerKeySelector(x);
                            }, innerKeyCapture, (cv, x) =>
                            {
                                Assert.AreEqual(innerKeyCapture, cv);
                                return innerKeySelector(x);
                            });
                }
            }
        }

        public static AsyncEnumerable<(TOuter Outer, TInner Inner)> Join<TOuter, TInner, TKey>(this in ConfiguredAsyncEnumerable<TOuter> outer,
            AsyncEnumerable<TInner> inner,
            bool async,
            Func<TInner, TKey> innerKeySelector, bool captureInnerKey,
            Func<TOuter, TKey> outerKeySelector, bool captureOuterKey,
            IEqualityComparer<TKey> equalityComparer)
        {
            const string outerKeyCapture = "outerKeyCapture";
            const string innerKeyCapture = "innerKeyCapture";

            if (!captureInnerKey)
            {
                if (!captureOuterKey)
                {
                    return async
                        ? equalityComparer != null
                            ? outer.Join(inner, async x => outerKeySelector(x), async x => innerKeySelector(x), equalityComparer)
                            : outer.Join(inner, async x => outerKeySelector(x), async x => innerKeySelector(x))
                        : equalityComparer != null
                            ? outer.Join(inner, outerKeySelector, innerKeySelector, equalityComparer)
                            : outer.Join(inner, outerKeySelector, innerKeySelector);
                }
                else
                {
                    return async
                        ? equalityComparer != null
                            ? outer.Join(inner, outerKeyCapture, async (cv, x) =>
                            {
                                Assert.AreEqual(outerKeyCapture, cv);
                                return outerKeySelector(x);
                            }, async x => innerKeySelector(x), equalityComparer)
                            : outer.Join(inner, outerKeyCapture, async (cv, x) =>
                            {
                                Assert.AreEqual(outerKeyCapture, cv);
                                return outerKeySelector(x);
                            }, async x => innerKeySelector(x))
                        : equalityComparer != null
                            ? outer.Join(inner, outerKeyCapture, (cv, x) =>
                            {
                                Assert.AreEqual(outerKeyCapture, cv);
                                return outerKeySelector(x);
                            }, innerKeySelector, equalityComparer)
                            : outer.Join(inner, outerKeyCapture, (cv, x) =>
                            {
                                Assert.AreEqual(outerKeyCapture, cv);
                                return outerKeySelector(x);
                            }, innerKeySelector);
                }
            }
            else
            {
                if (!captureOuterKey)
                {
                    return async
                        ? equalityComparer != null
                            ? outer.Join(inner, async x => outerKeySelector(x), innerKeyCapture, async (cv, x) =>
                            {
                                Assert.AreEqual(innerKeyCapture, cv);
                                return innerKeySelector(x);
                            }, equalityComparer)
                            : outer.Join(inner, async x => outerKeySelector(x), innerKeyCapture, async (cv, x) =>
                            {
                                Assert.AreEqual(innerKeyCapture, cv);
                                return innerKeySelector(x);
                            })
                        : equalityComparer != null
                            ? outer.Join(inner, outerKeySelector, innerKeyCapture, (cv, x) =>
                            {
                                Assert.AreEqual(innerKeyCapture, cv);
                                return innerKeySelector(x);
                            }, equalityComparer)
                            : outer.Join(inner, outerKeySelector, innerKeyCapture, (cv, x) =>
                            {
                                Assert.AreEqual(innerKeyCapture, cv);
                                return innerKeySelector(x);
                            });
                }
                else
                {
                    return async
                        ? equalityComparer != null
                            ? outer.Join(inner, outerKeyCapture, async (cv, x) =>
                            {
                                Assert.AreEqual(outerKeyCapture, cv);
                                return outerKeySelector(x);
                            }, innerKeyCapture, async (cv, x) =>
                            {
                                Assert.AreEqual(innerKeyCapture, cv);
                                return innerKeySelector(x);
                            }, equalityComparer)
                            : outer.Join(inner, outerKeyCapture, async (cv, x) =>
                            {
                                Assert.AreEqual(outerKeyCapture, cv);
                                return outerKeySelector(x);
                            }, innerKeyCapture, async (cv, x) =>
                            {
                                Assert.AreEqual(innerKeyCapture, cv);
                                return innerKeySelector(x);
                            })
                        : equalityComparer != null
                            ? outer.Join(inner, outerKeyCapture, (cv, x) =>
                            {
                                Assert.AreEqual(outerKeyCapture, cv);
                                return outerKeySelector(x);
                            }, innerKeyCapture, (cv, x) =>
                            {
                                Assert.AreEqual(innerKeyCapture, cv);
                                return innerKeySelector(x);
                            }, equalityComparer)
                            : outer.Join(inner, outerKeyCapture, (cv, x) =>
                            {
                                Assert.AreEqual(outerKeyCapture, cv);
                                return outerKeySelector(x);
                            }, innerKeyCapture, (cv, x) =>
                            {
                                Assert.AreEqual(innerKeyCapture, cv);
                                return innerKeySelector(x);
                            });
                }
            }
        }
    }

    public class JoinTests
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
        public void Join_NullArgumentThrows()
        {
            var outer = AsyncEnumerable.Return(42);
            var inner = AsyncEnumerable.Return(1);
            var captureValue = "captureValue";
            var nullComparer = default(IEqualityComparer<int>);

            Assert.Catch<System.ArgumentNullException>(() => outer.Join(inner, x => 0, default(Func<int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => outer.Join(inner, x => 0, default(Func<int, int>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => outer.Join(inner, default(Func<int, int>), x => 0));
            Assert.Catch<System.ArgumentNullException>(() => outer.Join(inner, default(Func<int, int>), x => 0, EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => outer.Join(inner, x => 0, x => 0, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => outer.Join(inner, captureValue, (cv, x) => 0, default(Func<int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => outer.Join(inner, captureValue, (cv, x) => 0, default(Func<int, int>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => outer.Join(inner, captureValue, default(Func<string, int, int>), x => 0));
            Assert.Catch<System.ArgumentNullException>(() => outer.Join(inner, captureValue, default(Func<string, int, int>), x => 0, EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => outer.Join(inner, captureValue, (cv, x) => 0, x => 0, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => outer.Join(inner, x => 0, captureValue, default(Func<string, int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => outer.Join(inner, x => 0, captureValue, default(Func<string, int, int>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => outer.Join(inner, default(Func<int, int>), captureValue, (cv, x) => 0));
            Assert.Catch<System.ArgumentNullException>(() => outer.Join(inner, default(Func<int, int>), captureValue, (cv, x) => 0, EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => outer.Join(inner, x => 0, captureValue, (cv, x) => 0, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => outer.Join(inner, captureValue, (cv, x) => 0, captureValue, default(Func<string, int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => outer.Join(inner, captureValue, (cv, x) => 0, captureValue, default(Func<string, int, int>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => outer.Join(inner, captureValue, default(Func<string, int, int>), captureValue, (cv, x) => 0));
            Assert.Catch<System.ArgumentNullException>(() => outer.Join(inner, captureValue, default(Func<string, int, int>), captureValue, (cv, x) => 0, EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => outer.Join(inner, captureValue, (cv, x) => 0, captureValue, (cv, x) => 0, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => outer.Join(inner, x => Promise.Resolved(0), default(Func<int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => outer.Join(inner, x => Promise.Resolved(0), default(Func<int, Promise<int>>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => outer.Join(inner, default(Func<int, Promise<int>>), x => Promise.Resolved(0)));
            Assert.Catch<System.ArgumentNullException>(() => outer.Join(inner, default(Func<int, Promise<int>>), x => Promise.Resolved(0), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => outer.Join(inner, x => Promise.Resolved(0), x => Promise.Resolved(0), nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => outer.Join(inner, captureValue, (cv, x) => Promise.Resolved(0), default(Func<int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => outer.Join(inner, captureValue, (cv, x) => Promise.Resolved(0), default(Func<int, Promise<int>>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => outer.Join(inner, captureValue, default(Func<string, int, Promise<int>>), x => Promise.Resolved(0)));
            Assert.Catch<System.ArgumentNullException>(() => outer.Join(inner, captureValue, default(Func<string, int, Promise<int>>), x => Promise.Resolved(0), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => outer.Join(inner, captureValue, (cv, x) => Promise.Resolved(0), x => Promise.Resolved(0), nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => outer.Join(inner, x => Promise.Resolved(0), captureValue, default(Func<string, int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => outer.Join(inner, x => Promise.Resolved(0), captureValue, default(Func<string, int, Promise<int>>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => outer.Join(inner, default(Func<int, Promise<int>>), captureValue, (cv, x) => Promise.Resolved(0)));
            Assert.Catch<System.ArgumentNullException>(() => outer.Join(inner, default(Func<int, Promise<int>>), captureValue, (cv, x) => Promise.Resolved(0), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => outer.Join(inner, x => Promise.Resolved(0), captureValue, (cv, x) => Promise.Resolved(0), nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => outer.Join(inner, captureValue, (cv, x) => Promise.Resolved(0), captureValue, default(Func<string, int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => outer.Join(inner, captureValue, (cv, x) => Promise.Resolved(0), captureValue, default(Func<string, int, Promise<int>>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => outer.Join(inner, captureValue, default(Func<string, int, Promise<int>>), captureValue, (cv, x) => Promise.Resolved(0)));
            Assert.Catch<System.ArgumentNullException>(() => outer.Join(inner, captureValue, default(Func<string, int, Promise<int>>), captureValue, (cv, x) => Promise.Resolved(0), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => outer.Join(inner, captureValue, (cv, x) => Promise.Resolved(0), captureValue, (cv, x) => Promise.Resolved(0), nullComparer));


            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).Join(inner, x => 0, default(Func<int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).Join(inner, x => 0, default(Func<int, int>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).Join(inner, default(Func<int, int>), x => 0));
            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).Join(inner, default(Func<int, int>), x => 0, EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).Join(inner, x => 0, x => 0, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).Join(inner, captureValue, (cv, x) => 0, default(Func<int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).Join(inner, captureValue, (cv, x) => 0, default(Func<int, int>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).Join(inner, captureValue, default(Func<string, int, int>), x => 0));
            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).Join(inner, captureValue, default(Func<string, int, int>), x => 0, EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).Join(inner, captureValue, (cv, x) => 0, x => 0, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).Join(inner, x => 0, captureValue, default(Func<string, int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).Join(inner, x => 0, captureValue, default(Func<string, int, int>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).Join(inner, default(Func<int, int>), captureValue, (cv, x) => 0));
            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).Join(inner, default(Func<int, int>), captureValue, (cv, x) => 0, EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).Join(inner, x => 0, captureValue, (cv, x) => 0, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).Join(inner, captureValue, (cv, x) => 0, captureValue, default(Func<string, int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).Join(inner, captureValue, (cv, x) => 0, captureValue, default(Func<string, int, int>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).Join(inner, captureValue, default(Func<string, int, int>), captureValue, (cv, x) => 0));
            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).Join(inner, captureValue, default(Func<string, int, int>), captureValue, (cv, x) => 0, EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).Join(inner, captureValue, (cv, x) => 0, captureValue, (cv, x) => 0, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).Join(inner, x => Promise.Resolved(0), default(Func<int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).Join(inner, x => Promise.Resolved(0), default(Func<int, Promise<int>>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).Join(inner, default(Func<int, Promise<int>>), x => Promise.Resolved(0)));
            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).Join(inner, default(Func<int, Promise<int>>), x => Promise.Resolved(0), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).Join(inner, x => Promise.Resolved(0), x => Promise.Resolved(0), nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).Join(inner, captureValue, (cv, x) => Promise.Resolved(0), default(Func<int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).Join(inner, captureValue, (cv, x) => Promise.Resolved(0), default(Func<int, Promise<int>>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).Join(inner, captureValue, default(Func<string, int, Promise<int>>), x => Promise.Resolved(0)));
            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).Join(inner, captureValue, default(Func<string, int, Promise<int>>), x => Promise.Resolved(0), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).Join(inner, captureValue, (cv, x) => Promise.Resolved(0), x => Promise.Resolved(0), nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).Join(inner, x => Promise.Resolved(0), captureValue, default(Func<string, int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).Join(inner, x => Promise.Resolved(0), captureValue, default(Func<string, int, Promise<int>>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).Join(inner, default(Func<int, Promise<int>>), captureValue, (cv, x) => Promise.Resolved(0)));
            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).Join(inner, default(Func<int, Promise<int>>), captureValue, (cv, x) => Promise.Resolved(0), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).Join(inner, x => Promise.Resolved(0), captureValue, (cv, x) => Promise.Resolved(0), nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).Join(inner, captureValue, (cv, x) => Promise.Resolved(0), captureValue, default(Func<string, int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).Join(inner, captureValue, (cv, x) => Promise.Resolved(0), captureValue, default(Func<string, int, Promise<int>>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).Join(inner, captureValue, default(Func<string, int, Promise<int>>), captureValue, (cv, x) => Promise.Resolved(0)));
            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).Join(inner, captureValue, default(Func<string, int, Promise<int>>), captureValue, (cv, x) => Promise.Resolved(0), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).Join(inner, captureValue, (cv, x) => Promise.Resolved(0), captureValue, (cv, x) => Promise.Resolved(0), nullComparer));

            outer.GetAsyncEnumerator().DisposeAsync().Forget();
            inner.GetAsyncEnumerator().DisposeAsync().Forget();
        }
#endif //PROMISE_DEBUG

        private static IEqualityComparer<T> GetDefaultOrNullComparer<T>(bool defaultComparer)
        {
            return defaultComparer ? EqualityComparer<T>.Default : null;
        }

        [Test]
        public void Join_EmptyOuter(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureOuterKey,
            [Values] bool captureInnerKey,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var xs = new[] { 0, 1, 2 }.ToAsyncEnumerable();
                var asyncEnumerator = new int[0].ToAsyncEnumerable()
                    .Join(xs, configured, async, x => x, captureOuterKey, x => x, captureInnerKey, equalityComparer: GetDefaultOrNullComparer<int>(withComparer))
                    .GetAsyncEnumerator();
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Join_EmptyInner(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureOuterKey,
            [Values] bool captureInnerKey,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var asyncEnumerator = new[] { 0, 1, 2 }.ToAsyncEnumerable()
                    .Join(new int[0].ToAsyncEnumerable(), configured, async, x => x, captureOuterKey, x => x, captureInnerKey, equalityComparer: GetDefaultOrNullComparer<int>(withComparer))
                    .GetAsyncEnumerator();
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Join1(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureOuterKey,
            [Values] bool captureInnerKey,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var xs = new[] { 0, 1, 2 }.ToAsyncEnumerable();
                var ys = new[] { 3, 6, 4 }.ToAsyncEnumerable();
                var asyncEnumerator = xs
                    .Join(ys, configured, async, x => x % 3, captureOuterKey, y => y % 3, captureInnerKey, equalityComparer: GetDefaultOrNullComparer<int>(withComparer))
                    .GetAsyncEnumerator();
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual((0, 3), asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual((0, 6), asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual((1, 4), asyncEnumerator.Current);
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Join2(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureOuterKey,
            [Values] bool captureInnerKey,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var xs = new[] { 3, 6, 4 }.ToAsyncEnumerable();
                var ys = new[] { 0, 1, 2 }.ToAsyncEnumerable();
                var asyncEnumerator = xs
                    .Join(ys, configured, async, x => x % 3, captureOuterKey, y => y % 3, captureInnerKey, equalityComparer: GetDefaultOrNullComparer<int>(withComparer))
                    .GetAsyncEnumerator();
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual((3, 0), asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual((6, 0), asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual((4, 1), asyncEnumerator.Current);
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Join3(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureOuterKey,
            [Values] bool captureInnerKey,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var xs = new[] { 0, 1, 2 }.ToAsyncEnumerable();
                var ys = new[] { 3, 6 }.ToAsyncEnumerable();
                var asyncEnumerator = xs
                    .Join(ys, configured, async, x => x % 3, captureOuterKey, y => y % 3, captureInnerKey, equalityComparer: GetDefaultOrNullComparer<int>(withComparer))
                    .GetAsyncEnumerator();
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual((0, 3), asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual((0, 6), asyncEnumerator.Current);
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Join4(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureOuterKey,
            [Values] bool captureInnerKey,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var xs = new[] { 3, 6 }.ToAsyncEnumerable();
                var ys = new[] { 0, 1, 2 }.ToAsyncEnumerable();
                var asyncEnumerator = xs
                    .Join(ys, configured, async, x => x % 3, captureOuterKey, y => y % 3, captureInnerKey, equalityComparer: GetDefaultOrNullComparer<int>(withComparer))
                    .GetAsyncEnumerator();
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual((3, 0), asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual((6, 0), asyncEnumerator.Current);
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Join5Async(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureOuterKey,
            [Values] bool captureInnerKey,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var ex = new Exception("Bang!");
                var xs = AsyncEnumerable<int>.Rejected(ex);
                var ys = new[] { 0, 1, 2 }.ToAsyncEnumerable();
                var asyncEnumerator = xs
                    .Join(ys, configured, async, x => x % 3, captureOuterKey, y => y % 3, captureInnerKey, equalityComparer: GetDefaultOrNullComparer<int>(withComparer))
                    .GetAsyncEnumerator();
                await TestHelper.AssertThrowsAsync(() => asyncEnumerator.MoveNextAsync(), ex);
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Join6Async(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureOuterKey,
            [Values] bool captureInnerKey,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var ex = new Exception("Bang!");
                var xs = new[] { 0, 1, 2 }.ToAsyncEnumerable();
                var ys = AsyncEnumerable<int>.Rejected(ex);
                var asyncEnumerator = xs
                    .Join(ys, configured, async, x => x % 3, captureOuterKey, y => y % 3, captureInnerKey, equalityComparer: GetDefaultOrNullComparer<int>(withComparer))
                    .GetAsyncEnumerator();
                await TestHelper.AssertThrowsAsync(() => asyncEnumerator.MoveNextAsync(), ex);
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Join7Async(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureOuterKey,
            [Values] bool captureInnerKey,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var ex = new Exception("Bang!");
                var xs = new[] { 0, 1, 2 }.ToAsyncEnumerable();
                var ys = new[] { 0, 1, 2 }.ToAsyncEnumerable();
                var asyncEnumerator = xs
                    .Join(ys, configured, async, x => { throw ex; }, captureOuterKey, y => y, captureInnerKey, equalityComparer: GetDefaultOrNullComparer<int>(withComparer))
                    .GetAsyncEnumerator();
                await TestHelper.AssertThrowsAsync(() => asyncEnumerator.MoveNextAsync(), ex);
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Join8Async(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureOuterKey,
            [Values] bool captureInnerKey,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var ex = new Exception("Bang!");
                var xs = new[] { 0, 1, 2 }.ToAsyncEnumerable();
                var ys = new[] { 0, 1, 2 }.ToAsyncEnumerable();
                var asyncEnumerator = xs
                    .Join(ys, configured, async, x => x, captureOuterKey, y => { throw ex; }, captureInnerKey, equalityComparer: GetDefaultOrNullComparer<int>(withComparer))
                    .GetAsyncEnumerator();
                await TestHelper.AssertThrowsAsync(() => asyncEnumerator.MoveNextAsync(), ex);
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Join11(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureOuterKey,
            [Values] bool captureInnerKey,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var customers = new List<Customer>
                {
                    new Customer { CustomerId = "ALFKI" },
                    new Customer { CustomerId = "ANANT" },
                    new Customer { CustomerId = "FISSA" },
                };

                var orders = new List<Order>
                {
                    new Order { OrderId = 1, CustomerId = "ALFKI"},
                    new Order { OrderId = 2, CustomerId = "ALFKI"},
                    new Order { OrderId = 3, CustomerId = "ALFKI"},
                    new Order { OrderId = 4, CustomerId = "FISSA"},
                    new Order { OrderId = 5, CustomerId = "FISSA"},
                    new Order { OrderId = 6, CustomerId = "FISSA"},
                };

                var asyncEnumerator = customers.ToAsyncEnumerable()
                    .Join(orders.ToAsyncEnumerable(), configured, async, c => c.CustomerId, captureOuterKey, o => o.CustomerId, captureInnerKey, equalityComparer: GetDefaultOrNullComparer<string>(withComparer))
                    .Select(x => new CustomerOrder { CustomerId = x.Outer.CustomerId, OrderId = x.Inner.OrderId })
                    .GetAsyncEnumerator();
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(new CustomerOrder { CustomerId = "ALFKI", OrderId = 1 }, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(new CustomerOrder { CustomerId = "ALFKI", OrderId = 2 }, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(new CustomerOrder { CustomerId = "ALFKI", OrderId = 3 }, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(new CustomerOrder { CustomerId = "FISSA", OrderId = 4 }, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(new CustomerOrder { CustomerId = "FISSA", OrderId = 5 }, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(new CustomerOrder { CustomerId = "FISSA", OrderId = 6 }, asyncEnumerator.Current);
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Join12(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureOuterKey,
            [Values] bool captureInnerKey,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var customers = new List<Customer>
                {
                    new Customer { CustomerId = "ANANT" },
                    new Customer { CustomerId = "ALFKI" },
                    new Customer { CustomerId = "FISSA" }
                };

                var orders = new List<Order>
                {
                    new Order { OrderId = 1, CustomerId = "ALFKI"},
                    new Order { OrderId = 2, CustomerId = "ALFKI"},
                    new Order { OrderId = 3, CustomerId = "ALFKI"},
                    new Order { OrderId = 4, CustomerId = "FISSA"},
                    new Order { OrderId = 5, CustomerId = "FISSA"},
                    new Order { OrderId = 6, CustomerId = "FISSA"},
                };

                var asyncEnumerator = customers.ToAsyncEnumerable()
                    .Join(orders.ToAsyncEnumerable(), configured, async, c => c.CustomerId, captureOuterKey, o => o.CustomerId, captureInnerKey, equalityComparer: GetDefaultOrNullComparer<string>(withComparer))
                    .Select(x => new CustomerOrder { CustomerId = x.Outer.CustomerId, OrderId = x.Inner.OrderId })
                    .GetAsyncEnumerator();
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(new CustomerOrder { CustomerId = "ALFKI", OrderId = 1 }, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(new CustomerOrder { CustomerId = "ALFKI", OrderId = 2 }, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(new CustomerOrder { CustomerId = "ALFKI", OrderId = 3 }, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(new CustomerOrder { CustomerId = "FISSA", OrderId = 4 }, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(new CustomerOrder { CustomerId = "FISSA", OrderId = 5 }, asyncEnumerator.Current);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(new CustomerOrder { CustomerId = "FISSA", OrderId = 6 }, asyncEnumerator.Current);
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        public class Customer
        {
            public string CustomerId { get; set; }
        }

        public class Order
        {
            public int OrderId { get; set; }
            public string CustomerId { get; set; }
        }

        public class CustomerOrder : IEquatable<CustomerOrder>
        {
            public int OrderId { get; set; }
            public string CustomerId { get; set; }

            public bool Equals(CustomerOrder other)
            {
                if (other is null) return false;
                if (ReferenceEquals(this, other)) return true;
                return OrderId == other.OrderId && string.Equals(CustomerId, other.CustomerId);
            }

            public override bool Equals(object obj)
            {
                if (obj is null) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != GetType()) return false;
                return Equals((CustomerOrder) obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (OrderId * 397) ^ (CustomerId != null ? CustomerId.GetHashCode() : 0);
                }
            }

            public static bool operator ==(CustomerOrder left, CustomerOrder right)
            {
                return Equals(left, right);
            }

            public static bool operator !=(CustomerOrder left, CustomerOrder right)
            {
                return !Equals(left, right);
            }
        }

        public enum ConfiguredType
        {
            NotConfigured,
            Configured,
            ConfiguredWithCancelation
        }

        [Test]
        public void Join_Cancel(
            [Values] ConfiguredType configuredType,
            [Values] bool async,
            [Values] bool captureOuterKey,
            [Values] bool captureInnerKey,
            [Values] bool withComparer,
            [Values] bool enumeratorToken,
            [Values] bool cancelFirst)
        {
            Promise.Run(async () =>
            {
                var xs = AsyncEnumerable.Create<int>(async (writer, cancelationToken) =>
                {
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(0);
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(1);
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(2);
                });
                var ys = AsyncEnumerable.Create<int>(async (writer, cancelationToken) =>
                {
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(3);
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(6);
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(4);
                });
                using (var configuredCancelationSource = CancelationSource.New())
                {
                    using (var enumeratorCancelationSource = CancelationSource.New())
                    {
                        var asyncEnumerator = xs.Join(ys, configuredType != ConfiguredType.NotConfigured, async, x => x % 3, captureOuterKey, y => y % 3, captureInnerKey, GetDefaultOrNullComparer<int>(withComparer),
                            configuredType == ConfiguredType.ConfiguredWithCancelation ? configuredCancelationSource.Token : CancelationToken.None)
                            .GetAsyncEnumerator(enumeratorToken ? enumeratorCancelationSource.Token : CancelationToken.None);
                        if (cancelFirst)
                        {
                            Assert.True(await asyncEnumerator.MoveNextAsync());
                            Assert.AreEqual((0, 3), asyncEnumerator.Current);
                            Assert.True(await asyncEnumerator.MoveNextAsync());
                            Assert.AreEqual((0, 6), asyncEnumerator.Current);
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