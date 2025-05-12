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
    public static partial class JoinHelper
    {
        // We test all the different overloads.
        public static AsyncEnumerable<(TOuter Outer, TInner Inner)> RightJoin<TOuter, TInner, TKey>(this AsyncEnumerable<TOuter> outer,
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
                return RightJoin(outer.ConfigureAwait(SynchronizationOption.Foreground).WithCancelation(configuredCancelationToken), inner, async, innerKeySelector, captureInnerKey, outerKeySelector, captureOuterKey, equalityComparer);
            }

            const string outerKeyCapture = "outerKeyCapture";
            const string innerKeyCapture = "innerKeyCapture";

            if (!captureInnerKey)
            {
                if (!captureOuterKey)
                {
                    return async
                        ? equalityComparer != null
                            ? outer.RightJoin(inner, async x => outerKeySelector(x), async x => innerKeySelector(x), equalityComparer)
                            : outer.RightJoin(inner, async x => outerKeySelector(x), async x => innerKeySelector(x))
                        : equalityComparer != null
                            ? outer.RightJoin(inner, outerKeySelector, innerKeySelector, equalityComparer)
                            : outer.RightJoin(inner, outerKeySelector, innerKeySelector);
                }
                else
                {
                    return async
                        ? equalityComparer != null
                            ? outer.RightJoin(inner, outerKeyCapture, async (cv, x) =>
                            {
                                Assert.AreEqual(outerKeyCapture, cv);
                                return outerKeySelector(x);
                            }, async x => innerKeySelector(x), equalityComparer)
                            : outer.RightJoin(inner, outerKeyCapture, async (cv, x) =>
                            {
                                Assert.AreEqual(outerKeyCapture, cv);
                                return outerKeySelector(x);
                            }, async x => innerKeySelector(x))
                        : equalityComparer != null
                            ? outer.RightJoin(inner, outerKeyCapture, (cv, x) =>
                            {
                                Assert.AreEqual(outerKeyCapture, cv);
                                return outerKeySelector(x);
                            }, innerKeySelector, equalityComparer)
                            : outer.RightJoin(inner, outerKeyCapture, (cv, x) =>
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
                            ? outer.RightJoin(inner, async x => outerKeySelector(x), innerKeyCapture, async (cv, x) =>
                            {
                                Assert.AreEqual(innerKeyCapture, cv);
                                return innerKeySelector(x);
                            }, equalityComparer)
                            : outer.RightJoin(inner, async x => outerKeySelector(x), innerKeyCapture, async (cv, x) =>
                            {
                                Assert.AreEqual(innerKeyCapture, cv);
                                return innerKeySelector(x);
                            })
                        : equalityComparer != null
                            ? outer.RightJoin(inner, outerKeySelector, innerKeyCapture, (cv, x) =>
                            {
                                Assert.AreEqual(innerKeyCapture, cv);
                                return innerKeySelector(x);
                            }, equalityComparer)
                            : outer.RightJoin(inner, outerKeySelector, innerKeyCapture, (cv, x) =>
                            {
                                Assert.AreEqual(innerKeyCapture, cv);
                                return innerKeySelector(x);
                            });
                }
                else
                {
                    return async
                        ? equalityComparer != null
                            ? outer.RightJoin(inner, outerKeyCapture, async (cv, x) =>
                            {
                                Assert.AreEqual(outerKeyCapture, cv);
                                return outerKeySelector(x);
                            }, innerKeyCapture, async (cv, x) =>
                            {
                                Assert.AreEqual(innerKeyCapture, cv);
                                return innerKeySelector(x);
                            }, equalityComparer)
                            : outer.RightJoin(inner, outerKeyCapture, async (cv, x) =>
                            {
                                Assert.AreEqual(outerKeyCapture, cv);
                                return outerKeySelector(x);
                            }, innerKeyCapture, async (cv, x) =>
                            {
                                Assert.AreEqual(innerKeyCapture, cv);
                                return innerKeySelector(x);
                            })
                        : equalityComparer != null
                            ? outer.RightJoin(inner, outerKeyCapture, (cv, x) =>
                            {
                                Assert.AreEqual(outerKeyCapture, cv);
                                return outerKeySelector(x);
                            }, innerKeyCapture, (cv, x) =>
                            {
                                Assert.AreEqual(innerKeyCapture, cv);
                                return innerKeySelector(x);
                            }, equalityComparer)
                            : outer.RightJoin(inner, outerKeyCapture, (cv, x) =>
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

        public static AsyncEnumerable<(TOuter Outer, TInner Inner)> RightJoin<TOuter, TInner, TKey>(this in ConfiguredAsyncEnumerable<TOuter> outer,
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
                            ? outer.RightJoin(inner, async x => outerKeySelector(x), async x => innerKeySelector(x), equalityComparer)
                            : outer.RightJoin(inner, async x => outerKeySelector(x), async x => innerKeySelector(x))
                        : equalityComparer != null
                            ? outer.RightJoin(inner, outerKeySelector, innerKeySelector, equalityComparer)
                            : outer.RightJoin(inner, outerKeySelector, innerKeySelector);
                }
                else
                {
                    return async
                        ? equalityComparer != null
                            ? outer.RightJoin(inner, outerKeyCapture, async (cv, x) =>
                            {
                                Assert.AreEqual(outerKeyCapture, cv);
                                return outerKeySelector(x);
                            }, async x => innerKeySelector(x), equalityComparer)
                            : outer.RightJoin(inner, outerKeyCapture, async (cv, x) =>
                            {
                                Assert.AreEqual(outerKeyCapture, cv);
                                return outerKeySelector(x);
                            }, async x => innerKeySelector(x))
                        : equalityComparer != null
                            ? outer.RightJoin(inner, outerKeyCapture, (cv, x) =>
                            {
                                Assert.AreEqual(outerKeyCapture, cv);
                                return outerKeySelector(x);
                            }, innerKeySelector, equalityComparer)
                            : outer.RightJoin(inner, outerKeyCapture, (cv, x) =>
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
                            ? outer.RightJoin(inner, async x => outerKeySelector(x), innerKeyCapture, async (cv, x) =>
                            {
                                Assert.AreEqual(innerKeyCapture, cv);
                                return innerKeySelector(x);
                            }, equalityComparer)
                            : outer.RightJoin(inner, async x => outerKeySelector(x), innerKeyCapture, async (cv, x) =>
                            {
                                Assert.AreEqual(innerKeyCapture, cv);
                                return innerKeySelector(x);
                            })
                        : equalityComparer != null
                            ? outer.RightJoin(inner, outerKeySelector, innerKeyCapture, (cv, x) =>
                            {
                                Assert.AreEqual(innerKeyCapture, cv);
                                return innerKeySelector(x);
                            }, equalityComparer)
                            : outer.RightJoin(inner, outerKeySelector, innerKeyCapture, (cv, x) =>
                            {
                                Assert.AreEqual(innerKeyCapture, cv);
                                return innerKeySelector(x);
                            });
                }
                else
                {
                    return async
                        ? equalityComparer != null
                            ? outer.RightJoin(inner, outerKeyCapture, async (cv, x) =>
                            {
                                Assert.AreEqual(outerKeyCapture, cv);
                                return outerKeySelector(x);
                            }, innerKeyCapture, async (cv, x) =>
                            {
                                Assert.AreEqual(innerKeyCapture, cv);
                                return innerKeySelector(x);
                            }, equalityComparer)
                            : outer.RightJoin(inner, outerKeyCapture, async (cv, x) =>
                            {
                                Assert.AreEqual(outerKeyCapture, cv);
                                return outerKeySelector(x);
                            }, innerKeyCapture, async (cv, x) =>
                            {
                                Assert.AreEqual(innerKeyCapture, cv);
                                return innerKeySelector(x);
                            })
                        : equalityComparer != null
                            ? outer.RightJoin(inner, outerKeyCapture, (cv, x) =>
                            {
                                Assert.AreEqual(outerKeyCapture, cv);
                                return outerKeySelector(x);
                            }, innerKeyCapture, (cv, x) =>
                            {
                                Assert.AreEqual(innerKeyCapture, cv);
                                return innerKeySelector(x);
                            }, equalityComparer)
                            : outer.RightJoin(inner, outerKeyCapture, (cv, x) =>
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

    public class RightJoinTests
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
        public void RightJoin_NullArgumentThrows()
        {
            var outer = AsyncEnumerable.Return(42);
            var inner = AsyncEnumerable.Return(1);
            var captureValue = "captureValue";
            var nullComparer = default(IEqualityComparer<int>);

            Assert.Catch<System.ArgumentNullException>(() => outer.RightJoin(inner, x => 0, default(Func<int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => outer.RightJoin(inner, x => 0, default(Func<int, int>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => outer.RightJoin(inner, default(Func<int, int>), x => 0));
            Assert.Catch<System.ArgumentNullException>(() => outer.RightJoin(inner, default(Func<int, int>), x => 0, EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => outer.RightJoin(inner, x => 0, x => 0, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => outer.RightJoin(inner, captureValue, (cv, x) => 0, default(Func<int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => outer.RightJoin(inner, captureValue, (cv, x) => 0, default(Func<int, int>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => outer.RightJoin(inner, captureValue, default(Func<string, int, int>), x => 0));
            Assert.Catch<System.ArgumentNullException>(() => outer.RightJoin(inner, captureValue, default(Func<string, int, int>), x => 0, EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => outer.RightJoin(inner, captureValue, (cv, x) => 0, x => 0, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => outer.RightJoin(inner, x => 0, captureValue, default(Func<string, int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => outer.RightJoin(inner, x => 0, captureValue, default(Func<string, int, int>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => outer.RightJoin(inner, default(Func<int, int>), captureValue, (cv, x) => 0));
            Assert.Catch<System.ArgumentNullException>(() => outer.RightJoin(inner, default(Func<int, int>), captureValue, (cv, x) => 0, EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => outer.RightJoin(inner, x => 0, captureValue, (cv, x) => 0, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => outer.RightJoin(inner, captureValue, (cv, x) => 0, captureValue, default(Func<string, int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => outer.RightJoin(inner, captureValue, (cv, x) => 0, captureValue, default(Func<string, int, int>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => outer.RightJoin(inner, captureValue, default(Func<string, int, int>), captureValue, (cv, x) => 0));
            Assert.Catch<System.ArgumentNullException>(() => outer.RightJoin(inner, captureValue, default(Func<string, int, int>), captureValue, (cv, x) => 0, EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => outer.RightJoin(inner, captureValue, (cv, x) => 0, captureValue, (cv, x) => 0, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => outer.RightJoin(inner, x => Promise.Resolved(0), default(Func<int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => outer.RightJoin(inner, x => Promise.Resolved(0), default(Func<int, Promise<int>>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => outer.RightJoin(inner, default(Func<int, Promise<int>>), x => Promise.Resolved(0)));
            Assert.Catch<System.ArgumentNullException>(() => outer.RightJoin(inner, default(Func<int, Promise<int>>), x => Promise.Resolved(0), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => outer.RightJoin(inner, x => Promise.Resolved(0), x => Promise.Resolved(0), nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => outer.RightJoin(inner, captureValue, (cv, x) => Promise.Resolved(0), default(Func<int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => outer.RightJoin(inner, captureValue, (cv, x) => Promise.Resolved(0), default(Func<int, Promise<int>>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => outer.RightJoin(inner, captureValue, default(Func<string, int, Promise<int>>), x => Promise.Resolved(0)));
            Assert.Catch<System.ArgumentNullException>(() => outer.RightJoin(inner, captureValue, default(Func<string, int, Promise<int>>), x => Promise.Resolved(0), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => outer.RightJoin(inner, captureValue, (cv, x) => Promise.Resolved(0), x => Promise.Resolved(0), nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => outer.RightJoin(inner, x => Promise.Resolved(0), captureValue, default(Func<string, int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => outer.RightJoin(inner, x => Promise.Resolved(0), captureValue, default(Func<string, int, Promise<int>>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => outer.RightJoin(inner, default(Func<int, Promise<int>>), captureValue, (cv, x) => Promise.Resolved(0)));
            Assert.Catch<System.ArgumentNullException>(() => outer.RightJoin(inner, default(Func<int, Promise<int>>), captureValue, (cv, x) => Promise.Resolved(0), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => outer.RightJoin(inner, x => Promise.Resolved(0), captureValue, (cv, x) => Promise.Resolved(0), nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => outer.RightJoin(inner, captureValue, (cv, x) => Promise.Resolved(0), captureValue, default(Func<string, int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => outer.RightJoin(inner, captureValue, (cv, x) => Promise.Resolved(0), captureValue, default(Func<string, int, Promise<int>>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => outer.RightJoin(inner, captureValue, default(Func<string, int, Promise<int>>), captureValue, (cv, x) => Promise.Resolved(0)));
            Assert.Catch<System.ArgumentNullException>(() => outer.RightJoin(inner, captureValue, default(Func<string, int, Promise<int>>), captureValue, (cv, x) => Promise.Resolved(0), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => outer.RightJoin(inner, captureValue, (cv, x) => Promise.Resolved(0), captureValue, (cv, x) => Promise.Resolved(0), nullComparer));


            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).RightJoin(inner, x => 0, default(Func<int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).RightJoin(inner, x => 0, default(Func<int, int>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).RightJoin(inner, default(Func<int, int>), x => 0));
            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).RightJoin(inner, default(Func<int, int>), x => 0, EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).RightJoin(inner, x => 0, x => 0, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).RightJoin(inner, captureValue, (cv, x) => 0, default(Func<int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).RightJoin(inner, captureValue, (cv, x) => 0, default(Func<int, int>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).RightJoin(inner, captureValue, default(Func<string, int, int>), x => 0));
            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).RightJoin(inner, captureValue, default(Func<string, int, int>), x => 0, EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).RightJoin(inner, captureValue, (cv, x) => 0, x => 0, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).RightJoin(inner, x => 0, captureValue, default(Func<string, int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).RightJoin(inner, x => 0, captureValue, default(Func<string, int, int>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).RightJoin(inner, default(Func<int, int>), captureValue, (cv, x) => 0));
            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).RightJoin(inner, default(Func<int, int>), captureValue, (cv, x) => 0, EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).RightJoin(inner, x => 0, captureValue, (cv, x) => 0, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).RightJoin(inner, captureValue, (cv, x) => 0, captureValue, default(Func<string, int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).RightJoin(inner, captureValue, (cv, x) => 0, captureValue, default(Func<string, int, int>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).RightJoin(inner, captureValue, default(Func<string, int, int>), captureValue, (cv, x) => 0));
            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).RightJoin(inner, captureValue, default(Func<string, int, int>), captureValue, (cv, x) => 0, EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).RightJoin(inner, captureValue, (cv, x) => 0, captureValue, (cv, x) => 0, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).RightJoin(inner, x => Promise.Resolved(0), default(Func<int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).RightJoin(inner, x => Promise.Resolved(0), default(Func<int, Promise<int>>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).RightJoin(inner, default(Func<int, Promise<int>>), x => Promise.Resolved(0)));
            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).RightJoin(inner, default(Func<int, Promise<int>>), x => Promise.Resolved(0), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).RightJoin(inner, x => Promise.Resolved(0), x => Promise.Resolved(0), nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).RightJoin(inner, captureValue, (cv, x) => Promise.Resolved(0), default(Func<int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).RightJoin(inner, captureValue, (cv, x) => Promise.Resolved(0), default(Func<int, Promise<int>>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).RightJoin(inner, captureValue, default(Func<string, int, Promise<int>>), x => Promise.Resolved(0)));
            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).RightJoin(inner, captureValue, default(Func<string, int, Promise<int>>), x => Promise.Resolved(0), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).RightJoin(inner, captureValue, (cv, x) => Promise.Resolved(0), x => Promise.Resolved(0), nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).RightJoin(inner, x => Promise.Resolved(0), captureValue, default(Func<string, int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).RightJoin(inner, x => Promise.Resolved(0), captureValue, default(Func<string, int, Promise<int>>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).RightJoin(inner, default(Func<int, Promise<int>>), captureValue, (cv, x) => Promise.Resolved(0)));
            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).RightJoin(inner, default(Func<int, Promise<int>>), captureValue, (cv, x) => Promise.Resolved(0), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).RightJoin(inner, x => Promise.Resolved(0), captureValue, (cv, x) => Promise.Resolved(0), nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).RightJoin(inner, captureValue, (cv, x) => Promise.Resolved(0), captureValue, default(Func<string, int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).RightJoin(inner, captureValue, (cv, x) => Promise.Resolved(0), captureValue, default(Func<string, int, Promise<int>>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).RightJoin(inner, captureValue, default(Func<string, int, Promise<int>>), captureValue, (cv, x) => Promise.Resolved(0)));
            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).RightJoin(inner, captureValue, default(Func<string, int, Promise<int>>), captureValue, (cv, x) => Promise.Resolved(0), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => outer.ConfigureAwait(SynchronizationOption.Synchronous).RightJoin(inner, captureValue, (cv, x) => Promise.Resolved(0), captureValue, (cv, x) => Promise.Resolved(0), nullComparer));

            outer.GetAsyncEnumerator().DisposeAsync().Forget();
            inner.GetAsyncEnumerator().DisposeAsync().Forget();
        }
#endif //PROMISE_DEBUG

        private static IEqualityComparer<T> GetDefaultOrNullComparer<T>(bool defaultComparer)
        {
            return defaultComparer ? EqualityComparer<T>.Default : null;
        }

        public struct CustomerRec
        {
            public string name;
            public int custID;
        }

        public struct OrderRec
        {
            public int orderID;
            public int custID;
            public int total;
        }

        public struct JoinRec
        {
            public string name;
            public int orderID;
            public int total;
        }

        public static JoinRec CreateJoinRec(CustomerRec cr, OrderRec or)
        {
            return new JoinRec { name = cr.name, orderID = or.orderID, total = or.total };
        }

        [Test]
        public void RightJoin_EmptyOuter(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureOuterKey,
            // Reduce number of tests.
            [Values(false)] bool captureInnerKey,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var outer = new CustomerRec[0].ToAsyncEnumerable();
                var inner = new OrderRec[]
                {
                    new OrderRec{ orderID = 45321, custID = 98022, total = 50 },
                    new OrderRec{ orderID = 97865, custID = 32103, total = 25 }
                }.ToAsyncEnumerable();
                JoinRec[] expected = new JoinRec[]
                {
                    new JoinRec{ name = null, orderID = 45321, total = 50 },
                    new JoinRec{ name = null, orderID = 97865, total = 25 }
                };
                var result = await outer
                    .RightJoin(inner, configured, async, e => e.custID, captureOuterKey, e => e.custID, captureInnerKey, equalityComparer: GetDefaultOrNullComparer<int>(withComparer))
                    .Select(tuple => CreateJoinRec(tuple.Outer, tuple.Inner))
                    .ToArrayAsync();
                CollectionAssert.AreEqual(expected, result);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void RightJoin_FirstOuterMatchesLastInnerLastOuterMatchesFirstInnerSameNumberElements(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureOuterKey,
            [Values] bool captureInnerKey,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var outer = new CustomerRec[]
                {
                    new CustomerRec{ name = "Prakash", custID = 98022 },
                    new CustomerRec{ name = "Tim", custID = 99021 },
                    new CustomerRec{ name = "Robert", custID = 99022 }
                }.ToAsyncEnumerable();
                var inner = new OrderRec[]
                {
                    new OrderRec{ orderID = 45321, custID = 99022, total = 50 },
                    new OrderRec{ orderID = 43421, custID = 29022, total = 20 },
                    new OrderRec{ orderID = 95421, custID = 98022, total = 9 }
                }.ToAsyncEnumerable();
                JoinRec[] expected = new JoinRec[]
                {
                    new JoinRec{ name = "Robert", orderID = 45321, total = 50 },
                    new JoinRec{ name = null, orderID = 43421, total = 20 },
                    new JoinRec{ name = "Prakash", orderID = 95421, total = 9 }
                };
                var result = await outer
                    .RightJoin(inner, configured, async, e => e.custID, captureOuterKey, e => e.custID, captureInnerKey, equalityComparer: GetDefaultOrNullComparer<int>(withComparer))
                    .Select(tuple => CreateJoinRec(tuple.Outer, tuple.Inner))
                    .ToArrayAsync();
                CollectionAssert.AreEqual(expected, result);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void RightJoin_NullElements(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureOuterKey,
            [Values] bool captureInnerKey,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var outer = new string[] { null, string.Empty }.ToAsyncEnumerable();
                var inner = new string[] { null, string.Empty }.ToAsyncEnumerable();
                string[] expected = new string[] { null, string.Empty };
                var result = await outer
                    .RightJoin(inner, configured, async, e => e, captureOuterKey, e => e, captureInnerKey, equalityComparer: GetDefaultOrNullComparer<string>(withComparer))
                    .Select(tuple => tuple.Inner)
                    .ToArrayAsync();
                CollectionAssert.AreEqual(expected, result);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void RightJoin_EmptyInner(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureOuterKey,
            // Reduce number of tests.
            [Values(false)] bool captureInnerKey,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var outer = new CustomerRec[]
                {
                    new CustomerRec{ name = "Tim", custID = 43434 },
                    new CustomerRec{ name = "Bob", custID = 34093 }
                }.ToAsyncEnumerable();
                var inner = new OrderRec[0].ToAsyncEnumerable();
                JoinRec[] expected =
                {
                    new JoinRec{ name = "Tim", orderID = 0, total = 0 },
                    new JoinRec{ name = "Bob", orderID = 0, total = 0 }
                };
                var asyncEnumerator = outer
                    .RightJoin(inner, configured, async, e => e.custID, captureOuterKey, e => e.custID, captureInnerKey, equalityComparer: GetDefaultOrNullComparer<int>(withComparer))
                    .GetAsyncEnumerator();
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void RightJoin_SingleElementEachAndMatches(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureOuterKey,
            [Values] bool captureInnerKey,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var outer = new CustomerRec[] { new CustomerRec { name = "Prakash", custID = 98022 } }.ToAsyncEnumerable();
                var inner = new OrderRec[] { new OrderRec { orderID = 45321, custID = 98022, total = 50 } }.ToAsyncEnumerable();
                JoinRec[] expected = new JoinRec[] { new JoinRec { name = "Prakash", orderID = 45321, total = 50 } };
                var result = await outer
                    .RightJoin(inner, configured, async, e => e.custID, captureOuterKey, e => e.custID, captureInnerKey, equalityComparer: GetDefaultOrNullComparer<int>(withComparer))
                    .Select(tuple => CreateJoinRec(tuple.Outer, tuple.Inner))
                    .ToArrayAsync();
                CollectionAssert.AreEqual(expected, result);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void RightJoin_SingleElementEachAndDoesntMatch(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureOuterKey,
            [Values] bool captureInnerKey,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var outer = new CustomerRec[] { new CustomerRec { name = "Prakash", custID = 98922 } }.ToAsyncEnumerable();
                var inner = new OrderRec[] { new OrderRec { orderID = 45321, custID = 98022, total = 50 } }.ToAsyncEnumerable();
                JoinRec[] expected = new JoinRec[] { new JoinRec { name = null, orderID = 45321, total = 50 } };
                var result = await outer
                    .RightJoin(inner, configured, async, e => e.custID, captureOuterKey, e => e.custID, captureInnerKey, equalityComparer: GetDefaultOrNullComparer<int>(withComparer))
                    .Select(tuple => CreateJoinRec(tuple.Outer, tuple.Inner))
                    .ToArrayAsync();
                CollectionAssert.AreEqual(expected, result);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void RightJoin_SelectorsReturnNull(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureOuterKey,
            [Values] bool captureInnerKey,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var outer = new int?[] { null, null }.ToAsyncEnumerable();
                var inner = new int?[] { null, null, null }.ToAsyncEnumerable();
                int?[] expected = new int?[] { null, null, null };
                var result = await outer
                    .RightJoin(inner, configured, async, e => e, captureOuterKey, e => e, captureInnerKey, equalityComparer: GetDefaultOrNullComparer<int?>(withComparer))
                    .Select(tuple => tuple.Outer)
                    .ToArrayAsync();
                CollectionAssert.AreEqual(expected, result);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void RightJoin_InnerSameKeyMoreThanOneElementAndMatches(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureOuterKey,
            [Values] bool captureInnerKey,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var outer = new CustomerRec[]
                {
                    new CustomerRec{ name = "Prakash", custID = 98022 },
                    new CustomerRec{ name = "Tim", custID = 99021 },
                    new CustomerRec{ name = "Robert", custID = 99022 }
                }.ToAsyncEnumerable();
                var inner = new OrderRec[]
                {
                    new OrderRec{ orderID = 45321, custID = 98022, total = 50 },
                    new OrderRec{ orderID = 45421, custID = 98022, total = 10 },
                    new OrderRec{ orderID = 43421, custID = 99022, total = 20 },
                    new OrderRec{ orderID = 85421, custID = 98022, total = 18 },
                    new OrderRec{ orderID = 95421, custID = 99021, total = 9 }
                }.ToAsyncEnumerable();
                JoinRec[] expected = new JoinRec[]
                {
                    new JoinRec{ name = "Prakash", orderID = 45321, total = 50 },
                    new JoinRec{ name = "Prakash", orderID = 45421, total = 10 },
                    new JoinRec{ name = "Robert", orderID = 43421, total = 20 },
                    new JoinRec{ name = "Prakash", orderID = 85421, total = 18 },
                    new JoinRec{ name = "Tim", orderID = 95421, total = 9 }
                };
                var result = await outer
                    .RightJoin(inner, configured, async, e => e.custID, captureOuterKey, e => e.custID, captureInnerKey, equalityComparer: GetDefaultOrNullComparer<int?>(withComparer))
                    .Select(tuple => CreateJoinRec(tuple.Outer, tuple.Inner))
                    .ToArrayAsync();
                CollectionAssert.AreEqual(expected, result);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void RightJoin_OuterSameKeyMoreThanOneElementAndMatches(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureOuterKey,
            [Values] bool captureInnerKey,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var outer = new CustomerRec[]
                {
                    new CustomerRec{ name = "Prakash", custID = 98022 },
                    new CustomerRec{ name = "Bob", custID = 99022 },
                    new CustomerRec{ name = "Tim", custID = 99021 },
                    new CustomerRec{ name = "Robert", custID = 99022 }
                }.ToAsyncEnumerable();
                var inner = new OrderRec[]
                {
                    new OrderRec{ orderID = 45321, custID = 98022, total = 50 },
                    new OrderRec{ orderID = 43421, custID = 99022, total = 20 },
                    new OrderRec{ orderID = 95421, custID = 99021, total = 9 }
                }.ToAsyncEnumerable();
                JoinRec[] expected = new JoinRec[]
                {
                    new JoinRec{ name = "Prakash", orderID = 45321, total = 50 },
                    new JoinRec{ name = "Bob", orderID = 43421, total = 20 },
                    new JoinRec{ name = "Robert", orderID = 43421, total = 20 },
                    new JoinRec{ name = "Tim", orderID = 95421, total = 9 }
                };
                var result = await outer
                    .RightJoin(inner, configured, async, e => e.custID, captureOuterKey, e => e.custID, captureInnerKey, equalityComparer: GetDefaultOrNullComparer<int?>(withComparer))
                    .Select(tuple => CreateJoinRec(tuple.Outer, tuple.Inner))
                    .ToArrayAsync();
                CollectionAssert.AreEqual(expected, result);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void RightJoin_NoMatches(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureOuterKey,
            [Values] bool captureInnerKey,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var outer = new CustomerRec[]
                {
                    new CustomerRec{ name = "Prakash", custID = 98022 },
                    new CustomerRec{ name = "Bob", custID = 99022 },
                    new CustomerRec{ name = "Tim", custID = 99021 },
                    new CustomerRec{ name = "Robert", custID = 99022 }
                }.ToAsyncEnumerable();
                var inner = new OrderRec[]
                {
                    new OrderRec{ orderID = 45321, custID = 18022, total = 50 },
                    new OrderRec{ orderID = 43421, custID = 29022, total = 20 },
                    new OrderRec{ orderID = 95421, custID = 39021, total = 9 }
                }.ToAsyncEnumerable();
                JoinRec[] expected = new JoinRec[]
                {
                    new JoinRec{ name = null, orderID = 45321, total = 50 },
                    new JoinRec{ name = null, orderID = 43421, total = 20 },
                    new JoinRec{ name = null, orderID = 95421, total = 9 }
                };
                var result = await outer
                    .RightJoin(inner, configured, async, e => e.custID, captureOuterKey, e => e.custID, captureInnerKey, equalityComparer: GetDefaultOrNullComparer<int?>(withComparer))
                    .Select(tuple => CreateJoinRec(tuple.Outer, tuple.Inner))
                    .ToArrayAsync();
                CollectionAssert.AreEqual(expected, result);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        public enum ConfiguredType
        {
            NotConfigured,
            Configured,
            ConfiguredWithCancelation
        }

        [Test]
        public void RightJoin_Cancel(
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
                        var asyncEnumerator = xs.RightJoin(ys, configuredType != ConfiguredType.NotConfigured, async, x => x % 3, captureOuterKey, y => y % 3, captureInnerKey, GetDefaultOrNullComparer<int>(withComparer),
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