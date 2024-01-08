#if CSHARP_7_3_OR_NEWER

#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using NUnit.Framework;
using Proto.Promises;
using Proto.Promises.Async.CompilerServices;
using Proto.Promises.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace ProtoPromiseTests.APIs.Linq
{
    public class ToLookupAsyncTests
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
        public void ToLookupAsync_NullArgumentThrows()
        {
            var enumerable = AsyncEnumerable.Return(42);
            var captureValue = "captureValue";
            var nullComparer = default(IEqualityComparer<int>);

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToLookupAsync(default(Func<int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToLookupAsync(default(Func<int, int>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToLookupAsync(x => 0, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToLookupAsync(captureValue, default(Func<string, int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToLookupAsync(captureValue, default(Func<string, int, int>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToLookupAsync(captureValue, (cv, x) => 0, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToLookupAsync(default(Func<int, int>), x => 0));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToLookupAsync(x => 0, default(Func<int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToLookupAsync(default(Func<int, int>), x => 0, EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToLookupAsync(x => 0, default(Func<int, int>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToLookupAsync(x => 0, x => 0, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToLookupAsync(captureValue, default(Func<string, int, int>), x => 0));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToLookupAsync(captureValue, (cv, x) => 0, default(Func<int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToLookupAsync(captureValue, default(Func<string, int, int>), x => 0, EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToLookupAsync(captureValue, (cv, x) => 0, default(Func<int, int>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToLookupAsync(captureValue, (cv, x) => 0, x => 0, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToLookupAsync(default(Func<int, int>), captureValue, (cv, x) => 0));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToLookupAsync(x => 0, captureValue, default(Func<string, int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToLookupAsync(default(Func<int, int>), captureValue, (cv, x) => 0, EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToLookupAsync(x => 0, captureValue, default(Func<string, int, int>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToLookupAsync(x => 0, captureValue, (cv, x) => 0, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToLookupAsync(captureValue, default(Func<string, int, int>), captureValue, (cv, x) => 0));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToLookupAsync(captureValue, (cv, x) => 0, captureValue, default(Func<string, int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToLookupAsync(captureValue, default(Func<string, int, int>), captureValue, (cv, x) => 0, EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToLookupAsync(captureValue, (cv, x) => 0, captureValue, default(Func<string, int, int>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToLookupAsync(captureValue, (cv, x) => 0, captureValue, (cv, x) => 0, nullComparer));


            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToLookupAsync(default(Func<int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToLookupAsync(default(Func<int, Promise<int>>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToLookupAsync(x => Promise.Resolved(0), nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToLookupAsync(captureValue, default(Func<string, int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToLookupAsync(captureValue, default(Func<string, int, Promise<int>>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToLookupAsync(captureValue, (cv, x) => Promise.Resolved(0), nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToLookupAsync(default(Func<int, Promise<int>>), x => Promise.Resolved(0)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToLookupAsync(x => Promise.Resolved(0), default(Func<int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToLookupAsync(default(Func<int, Promise<int>>), x => Promise.Resolved(0), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToLookupAsync(x => Promise.Resolved(0), default(Func<int, Promise<int>>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToLookupAsync(x => Promise.Resolved(0), x => Promise.Resolved(0), nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToLookupAsync(captureValue, default(Func<string, int, Promise<int>>), x => Promise.Resolved(0)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToLookupAsync(captureValue, (cv, x) => Promise.Resolved(0), default(Func<int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToLookupAsync(captureValue, default(Func<string, int, Promise<int>>), x => Promise.Resolved(0), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToLookupAsync(captureValue, (cv, x) => Promise.Resolved(0), default(Func<int, Promise<int>>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToLookupAsync(captureValue, (cv, x) => Promise.Resolved(0), x => Promise.Resolved(0), nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToLookupAsync(default(Func<int, Promise<int>>), captureValue, (cv, x) => Promise.Resolved(0)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToLookupAsync(x => Promise.Resolved(0), captureValue, default(Func<string, int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToLookupAsync(default(Func<int, Promise<int>>), captureValue, (cv, x) => Promise.Resolved(0), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToLookupAsync(x => Promise.Resolved(0), captureValue, default(Func<string, int, Promise<int>>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToLookupAsync(x => Promise.Resolved(0), captureValue, (cv, x) => Promise.Resolved(0), nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToLookupAsync(captureValue, default(Func<string, int, Promise<int>>), captureValue, (cv, x) => Promise.Resolved(0)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToLookupAsync(captureValue, (cv, x) => Promise.Resolved(0), captureValue, default(Func<string, int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToLookupAsync(captureValue, default(Func<string, int, Promise<int>>), captureValue, (cv, x) => Promise.Resolved(0), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToLookupAsync(captureValue, (cv, x) => Promise.Resolved(0), captureValue, default(Func<string, int, Promise<int>>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToLookupAsync(captureValue, (cv, x) => Promise.Resolved(0), captureValue, (cv, x) => Promise.Resolved(0), nullComparer));


            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToLookupAsync(default(Func<int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToLookupAsync(default(Func<int, int>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToLookupAsync(x => 0, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToLookupAsync(captureValue, default(Func<string, int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToLookupAsync(captureValue, default(Func<string, int, int>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToLookupAsync(captureValue, (cv, x) => 0, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToLookupAsync(default(Func<int, int>), x => 0));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToLookupAsync(x => 0, default(Func<int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToLookupAsync(default(Func<int, int>), x => 0, EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToLookupAsync(x => 0, default(Func<int, int>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToLookupAsync(x => 0, x => 0, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToLookupAsync(captureValue, default(Func<string, int, int>), x => 0));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToLookupAsync(captureValue, (cv, x) => 0, default(Func<int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToLookupAsync(captureValue, default(Func<string, int, int>), x => 0, EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToLookupAsync(captureValue, (cv, x) => 0, default(Func<int, int>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToLookupAsync(captureValue, (cv, x) => 0, x => 0, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToLookupAsync(default(Func<int, int>), captureValue, (cv, x) => 0));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToLookupAsync(x => 0, captureValue, default(Func<string, int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToLookupAsync(default(Func<int, int>), captureValue, (cv, x) => 0, EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToLookupAsync(x => 0, captureValue, default(Func<string, int, int>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToLookupAsync(x => 0, captureValue, (cv, x) => 0, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToLookupAsync(captureValue, default(Func<string, int, int>), captureValue, (cv, x) => 0));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToLookupAsync(captureValue, (cv, x) => 0, captureValue, default(Func<string, int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToLookupAsync(captureValue, default(Func<string, int, int>), captureValue, (cv, x) => 0, EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToLookupAsync(captureValue, (cv, x) => 0, captureValue, default(Func<string, int, int>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToLookupAsync(captureValue, (cv, x) => 0, captureValue, (cv, x) => 0, nullComparer));


            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToLookupAsync(default(Func<int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToLookupAsync(default(Func<int, Promise<int>>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToLookupAsync(x => Promise.Resolved(0), nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToLookupAsync(captureValue, default(Func<string, int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToLookupAsync(captureValue, default(Func<string, int, Promise<int>>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToLookupAsync(captureValue, (cv, x) => Promise.Resolved(0), nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToLookupAsync(default(Func<int, Promise<int>>), x => Promise.Resolved(0)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToLookupAsync(x => Promise.Resolved(0), default(Func<int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToLookupAsync(default(Func<int, Promise<int>>), x => Promise.Resolved(0), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToLookupAsync(x => Promise.Resolved(0), default(Func<int, Promise<int>>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToLookupAsync(x => Promise.Resolved(0), x => Promise.Resolved(0), nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToLookupAsync(captureValue, default(Func<string, int, Promise<int>>), x => Promise.Resolved(0)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToLookupAsync(captureValue, (cv, x) => Promise.Resolved(0), default(Func<int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToLookupAsync(captureValue, default(Func<string, int, Promise<int>>), x => Promise.Resolved(0), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToLookupAsync(captureValue, (cv, x) => Promise.Resolved(0), default(Func<int, Promise<int>>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToLookupAsync(captureValue, (cv, x) => Promise.Resolved(0), x => Promise.Resolved(0), nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToLookupAsync(default(Func<int, Promise<int>>), captureValue, (cv, x) => Promise.Resolved(0)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToLookupAsync(x => Promise.Resolved(0), captureValue, default(Func<string, int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToLookupAsync(default(Func<int, Promise<int>>), captureValue, (cv, x) => Promise.Resolved(0), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToLookupAsync(x => Promise.Resolved(0), captureValue, default(Func<string, int, Promise<int>>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToLookupAsync(x => Promise.Resolved(0), captureValue, (cv, x) => Promise.Resolved(0), nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToLookupAsync(captureValue, default(Func<string, int, Promise<int>>), captureValue, (cv, x) => Promise.Resolved(0)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToLookupAsync(captureValue, (cv, x) => Promise.Resolved(0), captureValue, default(Func<string, int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToLookupAsync(captureValue, default(Func<string, int, Promise<int>>), captureValue, (cv, x) => Promise.Resolved(0), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToLookupAsync(captureValue, (cv, x) => Promise.Resolved(0), captureValue, default(Func<string, int, Promise<int>>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToLookupAsync(captureValue, (cv, x) => Promise.Resolved(0), captureValue, (cv, x) => Promise.Resolved(0), nullComparer));

            enumerable.GetAsyncEnumerator().DisposeAsync().Forget();
        }
#endif //PROMISE_DEBUG

        // We test all the different overloads.
        private static Promise<ILookup<int, int>> ToLookupAsync(AsyncEnumerable<int> asyncEnumerable,
            bool configured,
            bool async,
            Func<int, int> keySelector, bool captureKey,
            Func<int, int> elementSelector = null, bool captureElement = false,
            IEqualityComparer<int> equalityComparer = null)
        {
            if (configured)
            {
                return ToLookupAsync(asyncEnumerable.ConfigureAwait(SynchronizationOption.Foreground), async, keySelector, captureKey, elementSelector, captureElement, equalityComparer);
            }

            const string keyCapture = "keyCapture";
            const string elementCapture = "elementCapture";

            if (elementSelector == null)
            {
                if (!captureKey)
                {
                    return async
                        ? equalityComparer != null
                            ? asyncEnumerable.ToLookupAsync(async x => keySelector(x), equalityComparer)
                            : asyncEnumerable.ToLookupAsync(async x => keySelector(x))
                        : equalityComparer != null
                            ? asyncEnumerable.ToLookupAsync(keySelector, equalityComparer)
                            : asyncEnumerable.ToLookupAsync(keySelector);
                }
                else
                {
                    return async
                        ? equalityComparer != null
                            ? asyncEnumerable.ToLookupAsync(keyCapture, async (cv, x) =>
                            {
                                Assert.AreEqual(keyCapture, cv);
                                return keySelector(x);
                            }, equalityComparer)
                            : asyncEnumerable.ToLookupAsync(keyCapture, async (cv, x) =>
                            {
                                Assert.AreEqual(keyCapture, cv);
                                return keySelector(x);
                            })
                        : equalityComparer != null
                            ? asyncEnumerable.ToLookupAsync(keyCapture, (cv, x) =>
                            {
                                Assert.AreEqual(keyCapture, cv);
                                return keySelector(x);
                            }, equalityComparer)
                            : asyncEnumerable.ToLookupAsync(keyCapture, (cv, x) =>
                            {
                                Assert.AreEqual(keyCapture, cv);
                                return keySelector(x);
                            });
                }
            }
            else
            {
                if (!captureKey)
                {
                    if (!captureElement)
                    {
                        return async
                            ? equalityComparer != null
                                ? asyncEnumerable.ToLookupAsync(async x => keySelector(x), async x => elementSelector(x), equalityComparer)
                                : asyncEnumerable.ToLookupAsync(async x => keySelector(x), async x => elementSelector(x))
                            : equalityComparer != null
                                ? asyncEnumerable.ToLookupAsync(keySelector, elementSelector, equalityComparer)
                                : asyncEnumerable.ToLookupAsync(keySelector, elementSelector);
                    }
                    else
                    {
                        return async
                            ? equalityComparer != null
                                ? asyncEnumerable.ToLookupAsync(async x => keySelector(x),
                                elementCapture, async (cv, x) =>
                                {
                                    Assert.AreEqual(elementCapture, cv);
                                    return elementSelector(x);
                                }, equalityComparer)
                                : asyncEnumerable.ToLookupAsync(async x => keySelector(x),
                                elementCapture, async (cv, x) =>
                                {
                                    Assert.AreEqual(elementCapture, cv);
                                    return elementSelector(x);
                                })
                            : equalityComparer != null
                                ? asyncEnumerable.ToLookupAsync(x => keySelector(x),
                                elementCapture, (cv, x) =>
                                {
                                    Assert.AreEqual(elementCapture, cv);
                                    return elementSelector(x);
                                }, equalityComparer)
                                : asyncEnumerable.ToLookupAsync(x => keySelector(x),
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
                                ? asyncEnumerable.ToLookupAsync(keyCapture, async (cv, x) =>
                                {
                                    Assert.AreEqual(keyCapture, cv);
                                    return keySelector(x);
                                }, async x => elementSelector(x), equalityComparer)
                                : asyncEnumerable.ToLookupAsync(keyCapture, async (cv, x) =>
                                {
                                    Assert.AreEqual(keyCapture, cv);
                                    return keySelector(x);
                                }, async x => elementSelector(x))
                            : equalityComparer != null
                                ? asyncEnumerable.ToLookupAsync(keyCapture, (cv, x) =>
                                {
                                    Assert.AreEqual(keyCapture, cv);
                                    return keySelector(x);
                                }, elementSelector, equalityComparer)
                                : asyncEnumerable.ToLookupAsync(keyCapture, (cv, x) =>
                                {
                                    Assert.AreEqual(keyCapture, cv);
                                    return keySelector(x);
                                }, elementSelector);
                    }
                    else
                    {
                        return async
                            ? equalityComparer != null
                                ? asyncEnumerable.ToLookupAsync(keyCapture, async (cv, x) =>
                                {
                                    Assert.AreEqual(keyCapture, cv);
                                    return keySelector(x);
                                },
                                elementCapture, async (cv, x) =>
                                {
                                    Assert.AreEqual(elementCapture, cv);
                                    return elementSelector(x);
                                }, equalityComparer)
                                : asyncEnumerable.ToLookupAsync(keyCapture, async (cv, x) =>
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
                                ? asyncEnumerable.ToLookupAsync(keyCapture, (cv, x) =>
                                {
                                    Assert.AreEqual(keyCapture, cv);
                                    return keySelector(x);
                                },
                                elementCapture, (cv, x) =>
                                {
                                    Assert.AreEqual(elementCapture, cv);
                                    return elementSelector(x);
                                }, equalityComparer)
                                : asyncEnumerable.ToLookupAsync(keyCapture, (cv, x) =>
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
        }
        private static Promise<ILookup<int, int>> ToLookupAsync(ConfiguredAsyncEnumerable<int> asyncEnumerable,
            bool async,
            Func<int, int> keySelector, bool captureKey,
            Func<int, int> elementSelector, bool captureElement,
            IEqualityComparer<int> equalityComparer)
        {
            const string keyCapture = "keyCapture";
            const string elementCapture = "elementCapture";

            if (elementSelector == null)
            {
                if (!captureKey)
                {
                    return async
                        ? equalityComparer != null
                            ? asyncEnumerable.ToLookupAsync(async x => keySelector(x), equalityComparer)
                            : asyncEnumerable.ToLookupAsync(async x => keySelector(x))
                        : equalityComparer != null
                            ? asyncEnumerable.ToLookupAsync(keySelector, equalityComparer)
                            : asyncEnumerable.ToLookupAsync(keySelector);
                }
                else
                {
                    return async
                        ? equalityComparer != null
                            ? asyncEnumerable.ToLookupAsync(keyCapture, async (cv, x) =>
                            {
                                Assert.AreEqual(keyCapture, cv);
                                return keySelector(x);
                            }, equalityComparer)
                            : asyncEnumerable.ToLookupAsync(keyCapture, async (cv, x) =>
                            {
                                Assert.AreEqual(keyCapture, cv);
                                return keySelector(x);
                            })
                        : equalityComparer != null
                            ? asyncEnumerable.ToLookupAsync(keyCapture, (cv, x) =>
                            {
                                Assert.AreEqual(keyCapture, cv);
                                return keySelector(x);
                            }, equalityComparer)
                            : asyncEnumerable.ToLookupAsync(keyCapture, (cv, x) =>
                            {
                                Assert.AreEqual(keyCapture, cv);
                                return keySelector(x);
                            });
                }
            }
            else
            {
                if (!captureKey)
                {
                    if (!captureElement)
                    {
                        return async
                            ? equalityComparer != null
                                ? asyncEnumerable.ToLookupAsync(async x => keySelector(x), async x => elementSelector(x), equalityComparer)
                                : asyncEnumerable.ToLookupAsync(async x => keySelector(x), async x => elementSelector(x))
                            : equalityComparer != null
                                ? asyncEnumerable.ToLookupAsync(keySelector, elementSelector, equalityComparer)
                                : asyncEnumerable.ToLookupAsync(keySelector, elementSelector);
                    }
                    else
                    {
                        return async
                            ? equalityComparer != null
                                ? asyncEnumerable.ToLookupAsync(async x => keySelector(x),
                                elementCapture, async (cv, x) =>
                                {
                                    Assert.AreEqual(elementCapture, cv);
                                    return elementSelector(x);
                                }, equalityComparer)
                                : asyncEnumerable.ToLookupAsync(async x => keySelector(x),
                                elementCapture, async (cv, x) =>
                                {
                                    Assert.AreEqual(elementCapture, cv);
                                    return elementSelector(x);
                                })
                            : equalityComparer != null
                                ? asyncEnumerable.ToLookupAsync(x => keySelector(x),
                                elementCapture, (cv, x) =>
                                {
                                    Assert.AreEqual(elementCapture, cv);
                                    return elementSelector(x);
                                }, equalityComparer)
                                : asyncEnumerable.ToLookupAsync(x => keySelector(x),
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
                                ? asyncEnumerable.ToLookupAsync(keyCapture, async (cv, x) =>
                                {
                                    Assert.AreEqual(keyCapture, cv);
                                    return keySelector(x);
                                }, async x => elementSelector(x), equalityComparer)
                                : asyncEnumerable.ToLookupAsync(keyCapture, async (cv, x) =>
                                {
                                    Assert.AreEqual(keyCapture, cv);
                                    return keySelector(x);
                                }, async x => elementSelector(x))
                            : equalityComparer != null
                                ? asyncEnumerable.ToLookupAsync(keyCapture, (cv, x) =>
                                {
                                    Assert.AreEqual(keyCapture, cv);
                                    return keySelector(x);
                                }, elementSelector, equalityComparer)
                                : asyncEnumerable.ToLookupAsync(keyCapture, (cv, x) =>
                                {
                                    Assert.AreEqual(keyCapture, cv);
                                    return keySelector(x);
                                }, elementSelector);
                    }
                    else
                    {
                        return async
                            ? equalityComparer != null
                                ? asyncEnumerable.ToLookupAsync(keyCapture, async (cv, x) =>
                                {
                                    Assert.AreEqual(keyCapture, cv);
                                    return keySelector(x);
                                },
                                elementCapture, async (cv, x) =>
                                {
                                    Assert.AreEqual(elementCapture, cv);
                                    return elementSelector(x);
                                }, equalityComparer)
                                : asyncEnumerable.ToLookupAsync(keyCapture, async (cv, x) =>
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
                                ? asyncEnumerable.ToLookupAsync(keyCapture, (cv, x) =>
                                {
                                    Assert.AreEqual(keyCapture, cv);
                                    return keySelector(x);
                                },
                                elementCapture, (cv, x) =>
                                {
                                    Assert.AreEqual(elementCapture, cv);
                                    return elementSelector(x);
                                }, equalityComparer)
                                : asyncEnumerable.ToLookupAsync(keyCapture, (cv, x) =>
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
        }

        private static IEqualityComparer<T> GetDefaultOrNullComparer<T>(bool defaultComparer)
        {
            return defaultComparer ? EqualityComparer<T>.Default : null;
        }

        [Test]
        public void ToLookup1Async(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureKey,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var xs = new[] { 1, 4 }.ToAsyncEnumerable();
                var res = await ToLookupAsync(xs, configured, async, x => x % 2, captureKey, equalityComparer: GetDefaultOrNullComparer<int>(withComparer));
                Assert.True(res.Contains(0));
                Assert.True(res.Contains(1));
                CollectionAssert.Contains(res[0], 4);
                CollectionAssert.Contains(res[1], 1);
                Assert.AreEqual(2, res.Count);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void ToLookup2Async(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureKey,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var xs = new[] { 1, 4, 2 }.ToAsyncEnumerable();
                var res = await ToLookupAsync(xs, configured, async, x => x % 2, captureKey, equalityComparer: GetDefaultOrNullComparer<int>(withComparer));
                Assert.True(res.Contains(0));
                Assert.True(res.Contains(1));
                CollectionAssert.Contains(res[0], 4);
                CollectionAssert.Contains(res[0], 2);
                CollectionAssert.Contains(res[1], 1);
                Assert.AreEqual(2, res.Count);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void ToLookup3Async(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureKey,
            [Values] bool captureElement,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var xs = new[] { 1, 4 }.ToAsyncEnumerable();
                var res = await ToLookupAsync(xs, configured, async, x => x % 2, captureKey, x => x + 1, captureElement, equalityComparer: GetDefaultOrNullComparer<int>(withComparer));
                Assert.True(res.Contains(0));
                Assert.True(res.Contains(1));
                CollectionAssert.Contains(res[0], 5);
                CollectionAssert.Contains(res[1], 2);
                Assert.AreEqual(2, res.Count);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void ToLookup4Async(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureKey,
            [Values] bool captureElement,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var xs = new[] { 1, 4, 2 }.ToAsyncEnumerable();
                var res = await ToLookupAsync(xs, configured, async, x => x % 2, captureKey, x => x + 1, captureElement, equalityComparer: GetDefaultOrNullComparer<int>(withComparer));
                Assert.True(res.Contains(0));
                Assert.True(res.Contains(1));
                CollectionAssert.Contains(res[0], 5);
                CollectionAssert.Contains(res[0], 3);
                CollectionAssert.Contains(res[1], 2);
                Assert.AreEqual(2, res.Count);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void ToLookup5Async(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureKey)
        {
            Promise.Run(async () =>
            {
                var xs = new[] { 1, 4 }.ToAsyncEnumerable();
                var res = await ToLookupAsync(xs, configured, async, x => x % 2, captureKey, equalityComparer: new Eq());
                Assert.True(res.Contains(0));
                Assert.True(res.Contains(1));
                CollectionAssert.Contains(res[0], 4);
                CollectionAssert.Contains(res[1], 1);
                Assert.AreEqual(2, res.Count);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void ToLookup6Async(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureKey)
        {
            Promise.Run(async () =>
            {
                var xs = new[] { 1, 4, 2 }.ToAsyncEnumerable();
                var res = await ToLookupAsync(xs, configured, async, x => x % 2, captureKey, equalityComparer: new Eq());
                Assert.True(res.Contains(0));
                Assert.True(res.Contains(1));
                CollectionAssert.Contains(res[0], 4);
                CollectionAssert.Contains(res[0], 2);
                CollectionAssert.Contains(res[1], 1);
                Assert.AreEqual(2, res.Count);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void ToLookup7Async(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureKey,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var xs = new[] { 1, 4, 2 }.ToAsyncEnumerable();
                var res = await ToLookupAsync(xs, configured, async, x => x % 2, captureKey, equalityComparer: GetDefaultOrNullComparer<int>(withComparer));
                foreach (var g in res)
                    Assert.True(g.Key == 0 || g.Key == 1);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void ToLookup8Async(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureKey,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var xs = new[] { 1, 4, 2 }.ToAsyncEnumerable();
                var res = await ToLookupAsync(xs, configured, async, x => x % 2, captureKey, equalityComparer: GetDefaultOrNullComparer<int>(withComparer));
#pragma warning disable IDE0007 // Use implicit type
                foreach (IGrouping<int, int> g in (IEnumerable) res)
                {
                    Assert.NotNull(g);
                    Assert.True(g.Key == 0 || g.Key == 1);
                }
#pragma warning restore IDE0007 // Use implicit type
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void ToLookup9Async(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureKey,
            [Values] bool captureElement)
        {
            Promise.Run(async () =>
            {
                var xs = new[] { 1, 4, 2 }.ToAsyncEnumerable();
                var res = await ToLookupAsync(xs, configured, async, x => x % 2, captureKey, x => x, captureElement, new Eq());
                Assert.True(res.Contains(0));
                Assert.True(res.Contains(1));
                CollectionAssert.Contains(res[0], 4);
                CollectionAssert.Contains(res[0], 2);
                CollectionAssert.Contains(res[1], 1);
                Assert.AreEqual(2, res.Count);
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
    }
}

#endif