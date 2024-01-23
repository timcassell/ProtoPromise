#if CSHARP_7_3_OR_NEWER

#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using NUnit.Framework;
using Proto.Promises;
using Proto.Promises.CompilerServices;
using Proto.Promises.Collections;
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
    public static class GroupJoinHelper
    {
        // We test all the different overloads.
        public static AsyncEnumerable<(TOuter Outer, TempCollection<TInner> InnerElements)> GroupJoin<TOuter, TInner, TKey>(this AsyncEnumerable<TOuter> outer,
            AsyncEnumerable<TInner> inner,
            bool configured,
            bool async,
            Func<TOuter, TKey> outerKeySelector, bool captureOuterKey,
            Func<TInner, TKey> innerKeySelector, bool captureInnerKey,
            IEqualityComparer<TKey> equalityComparer = null)
        {
            if (configured)
            {
                return GroupJoin(outer.ConfigureAwait(SynchronizationOption.Foreground), inner, async, innerKeySelector, captureInnerKey, outerKeySelector, captureOuterKey, equalityComparer);
            }

            const string outerKeyCapture = "outerKeyCapture";
            const string innerKeyCapture = "innerKeyCapture";

            if (!captureInnerKey)
            {
                if (!captureOuterKey)
                {
                    return async
                        ? equalityComparer != null
                            ? outer.GroupJoin(inner, async x => outerKeySelector(x), async x => innerKeySelector(x), equalityComparer)
                            : outer.GroupJoin(inner, async x => outerKeySelector(x), async x => innerKeySelector(x))
                        : equalityComparer != null
                            ? outer.GroupJoin(inner, outerKeySelector, innerKeySelector, equalityComparer)
                            : outer.GroupJoin(inner, outerKeySelector, innerKeySelector);
                }
                else
                {
                    return async
                        ? equalityComparer != null
                            ? outer.GroupJoin(inner, outerKeyCapture, async (cv, x) =>
                            {
                                Assert.AreEqual(outerKeyCapture, cv);
                                return outerKeySelector(x);
                            }, async x => innerKeySelector(x), equalityComparer)
                            : outer.GroupJoin(inner, outerKeyCapture, async (cv, x) =>
                            {
                                Assert.AreEqual(outerKeyCapture, cv);
                                return outerKeySelector(x);
                            }, async x => innerKeySelector(x))
                        : equalityComparer != null
                            ? outer.GroupJoin(inner, outerKeyCapture, (cv, x) =>
                            {
                                Assert.AreEqual(outerKeyCapture, cv);
                                return outerKeySelector(x);
                            }, innerKeySelector, equalityComparer)
                            : outer.GroupJoin(inner, outerKeyCapture, (cv, x) =>
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
                            ? outer.GroupJoin(inner, async x => outerKeySelector(x), innerKeyCapture, async (cv, x) =>
                            {
                                Assert.AreEqual(innerKeyCapture, cv);
                                return innerKeySelector(x);
                            }, equalityComparer)
                            : outer.GroupJoin(inner, async x => outerKeySelector(x), innerKeyCapture, async (cv, x) =>
                            {
                                Assert.AreEqual(innerKeyCapture, cv);
                                return innerKeySelector(x);
                            })
                        : equalityComparer != null
                            ? outer.GroupJoin(inner, outerKeySelector, innerKeyCapture, (cv, x) =>
                            {
                                Assert.AreEqual(innerKeyCapture, cv);
                                return innerKeySelector(x);
                            }, equalityComparer)
                            : outer.GroupJoin(inner, outerKeySelector, innerKeyCapture, (cv, x) =>
                            {
                                Assert.AreEqual(innerKeyCapture, cv);
                                return innerKeySelector(x);
                            });
                }
                else
                {
                    return async
                        ? equalityComparer != null
                            ? outer.GroupJoin(inner, outerKeyCapture, async (cv, x) =>
                            {
                                Assert.AreEqual(outerKeyCapture, cv);
                                return outerKeySelector(x);
                            }, innerKeyCapture, async (cv, x) =>
                            {
                                Assert.AreEqual(innerKeyCapture, cv);
                                return innerKeySelector(x);
                            }, equalityComparer)
                            : outer.GroupJoin(inner, outerKeyCapture, async (cv, x) =>
                            {
                                Assert.AreEqual(outerKeyCapture, cv);
                                return outerKeySelector(x);
                            }, innerKeyCapture, async (cv, x) =>
                            {
                                Assert.AreEqual(innerKeyCapture, cv);
                                return innerKeySelector(x);
                            })
                        : equalityComparer != null
                            ? outer.GroupJoin(inner, outerKeyCapture, (cv, x) =>
                            {
                                Assert.AreEqual(outerKeyCapture, cv);
                                return outerKeySelector(x);
                            }, innerKeyCapture, (cv, x) =>
                            {
                                Assert.AreEqual(innerKeyCapture, cv);
                                return innerKeySelector(x);
                            }, equalityComparer)
                            : outer.GroupJoin(inner, outerKeyCapture, (cv, x) =>
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

        public static AsyncEnumerable<(TOuter Outer, TempCollection<TInner> InnerElements)> GroupJoin<TOuter, TInner, TKey>(this ConfiguredAsyncEnumerable<TOuter> outer,
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
                            ? outer.GroupJoin(inner, async x => outerKeySelector(x), async x => innerKeySelector(x), equalityComparer)
                            : outer.GroupJoin(inner, async x => outerKeySelector(x), async x => innerKeySelector(x))
                        : equalityComparer != null
                            ? outer.GroupJoin(inner, outerKeySelector, innerKeySelector, equalityComparer)
                            : outer.GroupJoin(inner, outerKeySelector, innerKeySelector);
                }
                else
                {
                    return async
                        ? equalityComparer != null
                            ? outer.GroupJoin(inner, outerKeyCapture, async (cv, x) =>
                            {
                                Assert.AreEqual(outerKeyCapture, cv);
                                return outerKeySelector(x);
                            }, async x => innerKeySelector(x), equalityComparer)
                            : outer.GroupJoin(inner, outerKeyCapture, async (cv, x) =>
                            {
                                Assert.AreEqual(outerKeyCapture, cv);
                                return outerKeySelector(x);
                            }, async x => innerKeySelector(x))
                        : equalityComparer != null
                            ? outer.GroupJoin(inner, outerKeyCapture, (cv, x) =>
                            {
                                Assert.AreEqual(outerKeyCapture, cv);
                                return outerKeySelector(x);
                            }, innerKeySelector, equalityComparer)
                            : outer.GroupJoin(inner, outerKeyCapture, (cv, x) =>
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
                            ? outer.GroupJoin(inner, async x => outerKeySelector(x), innerKeyCapture, async (cv, x) =>
                            {
                                Assert.AreEqual(innerKeyCapture, cv);
                                return innerKeySelector(x);
                            }, equalityComparer)
                            : outer.GroupJoin(inner, async x => outerKeySelector(x), innerKeyCapture, async (cv, x) =>
                            {
                                Assert.AreEqual(innerKeyCapture, cv);
                                return innerKeySelector(x);
                            })
                        : equalityComparer != null
                            ? outer.GroupJoin(inner, outerKeySelector, innerKeyCapture, (cv, x) =>
                            {
                                Assert.AreEqual(innerKeyCapture, cv);
                                return innerKeySelector(x);
                            }, equalityComparer)
                            : outer.GroupJoin(inner, outerKeySelector, innerKeyCapture, (cv, x) =>
                            {
                                Assert.AreEqual(innerKeyCapture, cv);
                                return innerKeySelector(x);
                            });
                }
                else
                {
                    return async
                        ? equalityComparer != null
                            ? outer.GroupJoin(inner, outerKeyCapture, async (cv, x) =>
                            {
                                Assert.AreEqual(outerKeyCapture, cv);
                                return outerKeySelector(x);
                            }, innerKeyCapture, async (cv, x) =>
                            {
                                Assert.AreEqual(innerKeyCapture, cv);
                                return innerKeySelector(x);
                            }, equalityComparer)
                            : outer.GroupJoin(inner, outerKeyCapture, async (cv, x) =>
                            {
                                Assert.AreEqual(outerKeyCapture, cv);
                                return outerKeySelector(x);
                            }, innerKeyCapture, async (cv, x) =>
                            {
                                Assert.AreEqual(innerKeyCapture, cv);
                                return innerKeySelector(x);
                            })
                        : equalityComparer != null
                            ? outer.GroupJoin(inner, outerKeyCapture, (cv, x) =>
                            {
                                Assert.AreEqual(outerKeyCapture, cv);
                                return outerKeySelector(x);
                            }, innerKeyCapture, (cv, x) =>
                            {
                                Assert.AreEqual(innerKeyCapture, cv);
                                return innerKeySelector(x);
                            }, equalityComparer)
                            : outer.GroupJoin(inner, outerKeyCapture, (cv, x) =>
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

    public class GroupJoinTests
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
        public void GroupJoin_NullArgumentThrows()
        {
            var outer = AsyncEnumerable.Return(42);
            var inner = AsyncEnumerable.Return(1);
            var captureValue = "captureValue";
            var nullComparer = default(IEqualityComparer<int>);

            Assert.Catch<System.ArgumentNullException>(() => outer.GroupJoin(inner, x => 0, default(Func<int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => outer.GroupJoin(inner, x => 0, default(Func<int, int>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => outer.GroupJoin(inner, default(Func<int, int>), x => 0));
            Assert.Catch<System.ArgumentNullException>(() => outer.GroupJoin(inner, default(Func<int, int>), x => 0, EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => outer.GroupJoin(inner, x => 0, x => 0, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => outer.GroupJoin(inner, captureValue, (cv, x) => 0, default(Func<int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => outer.GroupJoin(inner, captureValue, (cv, x) => 0, default(Func<int, int>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => outer.GroupJoin(inner, captureValue, default(Func<string, int, int>), x => 0));
            Assert.Catch<System.ArgumentNullException>(() => outer.GroupJoin(inner, captureValue, default(Func<string, int, int>), x => 0, EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => outer.GroupJoin(inner, captureValue, (cv, x) => 0, x => 0, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => outer.GroupJoin(inner, x => 0, captureValue, default(Func<string, int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => outer.GroupJoin(inner, x => 0, captureValue, default(Func<string, int, int>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => outer.GroupJoin(inner, default(Func<int, int>), captureValue, (cv, x) => 0));
            Assert.Catch<System.ArgumentNullException>(() => outer.GroupJoin(inner, default(Func<int, int>), captureValue, (cv, x) => 0, EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => outer.GroupJoin(inner, x => 0, captureValue, (cv, x) => 0, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => outer.GroupJoin(inner, captureValue, (cv, x) => 0, captureValue, default(Func<string, int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => outer.GroupJoin(inner, captureValue, (cv, x) => 0, captureValue, default(Func<string, int, int>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => outer.GroupJoin(inner, captureValue, default(Func<string, int, int>), captureValue, (cv, x) => 0));
            Assert.Catch<System.ArgumentNullException>(() => outer.GroupJoin(inner, captureValue, default(Func<string, int, int>), captureValue, (cv, x) => 0, EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => outer.GroupJoin(inner, captureValue, (cv, x) => 0, captureValue, (cv, x) => 0, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => outer.GroupJoin(inner, x => Promise.Resolved(0), default(Func<int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => outer.GroupJoin(inner, x => Promise.Resolved(0), default(Func<int, Promise<int>>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => outer.GroupJoin(inner, default(Func<int, Promise<int>>), x => Promise.Resolved(0)));
            Assert.Catch<System.ArgumentNullException>(() => outer.GroupJoin(inner, default(Func<int, Promise<int>>), x => Promise.Resolved(0), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => outer.GroupJoin(inner, x => Promise.Resolved(0), x => Promise.Resolved(0), nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => outer.GroupJoin(inner, captureValue, (cv, x) => Promise.Resolved(0), default(Func<int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => outer.GroupJoin(inner, captureValue, (cv, x) => Promise.Resolved(0), default(Func<int, Promise<int>>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => outer.GroupJoin(inner, captureValue, default(Func<string, int, Promise<int>>), x => Promise.Resolved(0)));
            Assert.Catch<System.ArgumentNullException>(() => outer.GroupJoin(inner, captureValue, default(Func<string, int, Promise<int>>), x => Promise.Resolved(0), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => outer.GroupJoin(inner, captureValue, (cv, x) => Promise.Resolved(0), x => Promise.Resolved(0), nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => outer.GroupJoin(inner, x => Promise.Resolved(0), captureValue, default(Func<string, int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => outer.GroupJoin(inner, x => Promise.Resolved(0), captureValue, default(Func<string, int, Promise<int>>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => outer.GroupJoin(inner, default(Func<int, Promise<int>>), captureValue, (cv, x) => Promise.Resolved(0)));
            Assert.Catch<System.ArgumentNullException>(() => outer.GroupJoin(inner, default(Func<int, Promise<int>>), captureValue, (cv, x) => Promise.Resolved(0), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => outer.GroupJoin(inner, x => Promise.Resolved(0), captureValue, (cv, x) => Promise.Resolved(0), nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => outer.GroupJoin(inner, captureValue, (cv, x) => Promise.Resolved(0), captureValue, default(Func<string, int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => outer.GroupJoin(inner, captureValue, (cv, x) => Promise.Resolved(0), captureValue, default(Func<string, int, Promise<int>>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => outer.GroupJoin(inner, captureValue, default(Func<string, int, Promise<int>>), captureValue, (cv, x) => Promise.Resolved(0)));
            Assert.Catch<System.ArgumentNullException>(() => outer.GroupJoin(inner, captureValue, default(Func<string, int, Promise<int>>), captureValue, (cv, x) => Promise.Resolved(0), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => outer.GroupJoin(inner, captureValue, (cv, x) => Promise.Resolved(0), captureValue, (cv, x) => Promise.Resolved(0), nullComparer));


            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).GroupJoin(inner, x => 0, default(Func<int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).GroupJoin(inner, x => 0, default(Func<int, int>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).GroupJoin(inner, default(Func<int, int>), x => 0));
            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).GroupJoin(inner, default(Func<int, int>), x => 0, EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).GroupJoin(inner, x => 0, x => 0, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).GroupJoin(inner, captureValue, (cv, x) => 0, default(Func<int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).GroupJoin(inner, captureValue, (cv, x) => 0, default(Func<int, int>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).GroupJoin(inner, captureValue, default(Func<string, int, int>), x => 0));
            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).GroupJoin(inner, captureValue, default(Func<string, int, int>), x => 0, EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).GroupJoin(inner, captureValue, (cv, x) => 0, x => 0, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).GroupJoin(inner, x => 0, captureValue, default(Func<string, int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).GroupJoin(inner, x => 0, captureValue, default(Func<string, int, int>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).GroupJoin(inner, default(Func<int, int>), captureValue, (cv, x) => 0));
            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).GroupJoin(inner, default(Func<int, int>), captureValue, (cv, x) => 0, EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).GroupJoin(inner, x => 0, captureValue, (cv, x) => 0, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).GroupJoin(inner, captureValue, (cv, x) => 0, captureValue, default(Func<string, int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).GroupJoin(inner, captureValue, (cv, x) => 0, captureValue, default(Func<string, int, int>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).GroupJoin(inner, captureValue, default(Func<string, int, int>), captureValue, (cv, x) => 0));
            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).GroupJoin(inner, captureValue, default(Func<string, int, int>), captureValue, (cv, x) => 0, EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).GroupJoin(inner, captureValue, (cv, x) => 0, captureValue, (cv, x) => 0, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).GroupJoin(inner, x => Promise.Resolved(0), default(Func<int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).GroupJoin(inner, x => Promise.Resolved(0), default(Func<int, Promise<int>>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).GroupJoin(inner, default(Func<int, Promise<int>>), x => Promise.Resolved(0)));
            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).GroupJoin(inner, default(Func<int, Promise<int>>), x => Promise.Resolved(0), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).GroupJoin(inner, x => Promise.Resolved(0), x => Promise.Resolved(0), nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).GroupJoin(inner, captureValue, (cv, x) => Promise.Resolved(0), default(Func<int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).GroupJoin(inner, captureValue, (cv, x) => Promise.Resolved(0), default(Func<int, Promise<int>>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).GroupJoin(inner, captureValue, default(Func<string, int, Promise<int>>), x => Promise.Resolved(0)));
            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).GroupJoin(inner, captureValue, default(Func<string, int, Promise<int>>), x => Promise.Resolved(0), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).GroupJoin(inner, captureValue, (cv, x) => Promise.Resolved(0), x => Promise.Resolved(0), nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).GroupJoin(inner, x => Promise.Resolved(0), captureValue, default(Func<string, int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).GroupJoin(inner, x => Promise.Resolved(0), captureValue, default(Func<string, int, Promise<int>>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).GroupJoin(inner, default(Func<int, Promise<int>>), captureValue, (cv, x) => Promise.Resolved(0)));
            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).GroupJoin(inner, default(Func<int, Promise<int>>), captureValue, (cv, x) => Promise.Resolved(0), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).GroupJoin(inner, x => Promise.Resolved(0), captureValue, (cv, x) => Promise.Resolved(0), nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).GroupJoin(inner, captureValue, (cv, x) => Promise.Resolved(0), captureValue, default(Func<string, int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).GroupJoin(inner, captureValue, (cv, x) => Promise.Resolved(0), captureValue, default(Func<string, int, Promise<int>>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).GroupJoin(inner, captureValue, default(Func<string, int, Promise<int>>), captureValue, (cv, x) => Promise.Resolved(0)));
            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).GroupJoin(inner, captureValue, default(Func<string, int, Promise<int>>), captureValue, (cv, x) => Promise.Resolved(0), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).GroupJoin(inner, captureValue, (cv, x) => Promise.Resolved(0), captureValue, (cv, x) => Promise.Resolved(0), nullComparer));

            outer.GetAsyncEnumerator().DisposeAsync().Forget();
            inner.GetAsyncEnumerator().DisposeAsync().Forget();
        }
#endif //PROMISE_DEBUG

        private static IEqualityComparer<T> GetDefaultOrNullComparer<T>(bool defaultComparer)
        {
            return defaultComparer ? EqualityComparer<T>.Default : null;
        }

        [Test]
        public void GroupJoin_EmptyBoth(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureOuterKey,
            [Values] bool captureInnerKey,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var xs = AsyncEnumerable.Empty<int>();
                var asyncEnumerator = new int[0].ToAsyncEnumerable()
                    .GroupJoin(xs, configured, async, x => x, captureOuterKey, x => x, captureInnerKey, equalityComparer: GetDefaultOrNullComparer<int>(withComparer))
                    .GetAsyncEnumerator();
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void GroupJoin_EmptyOuter(
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
                    .GroupJoin(xs, configured, async, x => x, captureOuterKey, x => x, captureInnerKey, equalityComparer: GetDefaultOrNullComparer<int>(withComparer))
                    .GetAsyncEnumerator();
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void GroupJoin_EmptyInner(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureOuterKey,
            [Values] bool captureInnerKey,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var asyncEnumerator = new[] { 0, 1, 2 }.ToAsyncEnumerable()
                    .GroupJoin(new int[0].ToAsyncEnumerable(), configured, async, x => x, captureOuterKey, x => x, captureInnerKey, equalityComparer: GetDefaultOrNullComparer<int>(withComparer))
                    .GetAsyncEnumerator();
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(0, asyncEnumerator.Current.Outer);
                CollectionAssert.IsEmpty(asyncEnumerator.Current.InnerElements);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(1, asyncEnumerator.Current.Outer);
                CollectionAssert.IsEmpty(asyncEnumerator.Current.InnerElements);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(2, asyncEnumerator.Current.Outer);
                CollectionAssert.IsEmpty(asyncEnumerator.Current.InnerElements);
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void GroupJoin1(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureOuterKey,
            [Values] bool captureInnerKey,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var xs = new[] { 0, 1, 2 }.ToAsyncEnumerable();
                var ys = new[] { 4, 7, 6, 2, 3, 4, 8, 9 }.ToAsyncEnumerable();
                var asyncEnumerator = xs
                    .GroupJoin(ys, configured, async, x => x % 3, captureOuterKey, y => y % 3, captureInnerKey, equalityComparer: GetDefaultOrNullComparer<int>(withComparer))
                    .GetAsyncEnumerator();
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(0, asyncEnumerator.Current.Outer);
                CollectionAssert.AreEqual(new[] { 6, 3, 9 }, asyncEnumerator.Current.InnerElements);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(1, asyncEnumerator.Current.Outer);
                CollectionAssert.AreEqual(new[] { 4, 7, 4 }, asyncEnumerator.Current.InnerElements);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(2, asyncEnumerator.Current.Outer);
                CollectionAssert.AreEqual(new[] { 2, 8 }, asyncEnumerator.Current.InnerElements);
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void GroupJoin2(
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
                    .GroupJoin(ys, configured, async, x => x % 3, captureOuterKey, y => y % 3, captureInnerKey, equalityComparer: GetDefaultOrNullComparer<int>(withComparer))
                    .GetAsyncEnumerator();
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(0, asyncEnumerator.Current.Outer);
                CollectionAssert.AreEqual(new[] { 3, 6 }, asyncEnumerator.Current.InnerElements);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(1, asyncEnumerator.Current.Outer);
                CollectionAssert.AreEqual(new[] { 4 }, asyncEnumerator.Current.InnerElements);
                Assert.True(await asyncEnumerator.MoveNextAsync());
                Assert.AreEqual(2, asyncEnumerator.Current.Outer);
                CollectionAssert.IsEmpty(asyncEnumerator.Current.InnerElements);
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void GroupJoin3Async(
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
                var ys = new[] { 3, 6, 4 }.ToAsyncEnumerable();
                var asyncEnumerator = xs
                    .GroupJoin(ys, configured, async, x => x % 3, captureOuterKey, y => y % 3, captureInnerKey, equalityComparer: GetDefaultOrNullComparer<int>(withComparer))
                    .GetAsyncEnumerator();
                await TestHelper.AssertThrowsAsync(() => asyncEnumerator.MoveNextAsync(), ex);
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void GroupJoin4Async(
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
                    .GroupJoin(ys, configured, async, x => x % 3, captureOuterKey, y => y % 3, captureInnerKey, equalityComparer: GetDefaultOrNullComparer<int>(withComparer))
                    .GetAsyncEnumerator();
                await TestHelper.AssertThrowsAsync(() => asyncEnumerator.MoveNextAsync(), ex);
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void GroupJoin5Async(
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
                var ys = new[] { 3, 6, 4 }.ToAsyncEnumerable();
                var asyncEnumerator = xs
                    .GroupJoin(ys, configured, async, x => { throw ex; }, captureOuterKey, y => y % 3, captureInnerKey, equalityComparer: GetDefaultOrNullComparer<int>(withComparer))
                    .GetAsyncEnumerator();
                await TestHelper.AssertThrowsAsync(() => asyncEnumerator.MoveNextAsync(), ex);
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void GroupJoin6Async(
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
                var ys = new[] { 3, 6, 4 }.ToAsyncEnumerable();
                var asyncEnumerator = xs
                    .GroupJoin(ys, configured, async, x => x % 3, captureOuterKey, y => { throw ex; }, captureInnerKey, equalityComparer: GetDefaultOrNullComparer<int>(withComparer))
                    .GetAsyncEnumerator();
                await TestHelper.AssertThrowsAsync(() => asyncEnumerator.MoveNextAsync(), ex);
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }
    }
}

#endif