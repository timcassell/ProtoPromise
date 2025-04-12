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
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

#pragma warning disable CS0162 // Unreachable code detected
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace ProtoPromiseTests.APIs.Linq
{
    public class ToDictionaryAsyncTests
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
        public void ToDictionaryAsync_NullArgument()
        {
            var enumerable = AsyncEnumerable.Return(42);
            var captureValue = "captureValue";
            var nullComparer = default(IEqualityComparer<int>);

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToDictionaryAsync(default(Func<int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToDictionaryAsync(default(Func<int, int>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToDictionaryAsync(x => 0, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToDictionaryAsync(captureValue, default(Func<string, int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToDictionaryAsync(captureValue, default(Func<string, int, int>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToDictionaryAsync(captureValue, (cv, x) => 0, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToDictionaryAsync(default(Func<int, int>), x => 0));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToDictionaryAsync(x => 0, default(Func<int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToDictionaryAsync(default(Func<int, int>), x => 0, EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToDictionaryAsync(x => 0, default(Func<int, int>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToDictionaryAsync(x => 0, x => 0, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToDictionaryAsync(captureValue, default(Func<string, int, int>), x => 0));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToDictionaryAsync(captureValue, (cv, x) => 0, default(Func<int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToDictionaryAsync(captureValue, default(Func<string, int, int>), x => 0, EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToDictionaryAsync(captureValue, (cv, x) => 0, default(Func<int, int>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToDictionaryAsync(captureValue, (cv, x) => 0, x => 0, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToDictionaryAsync(default(Func<int, int>), captureValue, (cv, x) => 0));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToDictionaryAsync(x => 0, captureValue, default(Func<string, int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToDictionaryAsync(default(Func<int, int>), captureValue, (cv, x) => 0, EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToDictionaryAsync(x => 0, captureValue, default(Func<string, int, int>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToDictionaryAsync(x => 0, captureValue, (cv, x) => 0, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToDictionaryAsync(captureValue, default(Func<string, int, int>), captureValue, (cv, x) => 0));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToDictionaryAsync(captureValue, (cv, x) => 0, captureValue, default(Func<string, int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToDictionaryAsync(captureValue, default(Func<string, int, int>), captureValue, (cv, x) => 0, EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToDictionaryAsync(captureValue, (cv, x) => 0, captureValue, default(Func<string, int, int>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToDictionaryAsync(captureValue, (cv, x) => 0, captureValue, (cv, x) => 0, nullComparer));


            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToDictionaryAsync(default(Func<int, CancelationToken, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToDictionaryAsync(default(Func<int, CancelationToken, Promise<int>>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToDictionaryAsync((x, _) => Promise.Resolved(0), nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToDictionaryAsync(captureValue, default(Func<string, int, CancelationToken, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToDictionaryAsync(captureValue, default(Func<string, int, CancelationToken, Promise<int>>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToDictionaryAsync(captureValue, (cv, x, _) => Promise.Resolved(0), nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToDictionaryAsync(default(Func<int, CancelationToken, Promise<int>>), (int x, CancelationToken _) => Promise.Resolved(0)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToDictionaryAsync((x, _) => Promise.Resolved(0), default(Func<int, CancelationToken, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToDictionaryAsync(default(Func<int, CancelationToken, Promise<int>>), (x, _) => Promise.Resolved(0), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToDictionaryAsync((x, _) => Promise.Resolved(0), default(Func<int, CancelationToken, Promise<int>>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToDictionaryAsync((x, _) => Promise.Resolved(0), (x, _) => Promise.Resolved(0), nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToDictionaryAsync(captureValue, default(Func<string, int, CancelationToken, Promise<int>>), (x, _) => Promise.Resolved(0)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToDictionaryAsync(captureValue, (cv, x, _) => Promise.Resolved(0), default(Func<int, CancelationToken, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToDictionaryAsync(captureValue, default(Func<string, int, CancelationToken, Promise<int>>), (x, _) => Promise.Resolved(0), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToDictionaryAsync(captureValue, (cv, x, _) => Promise.Resolved(0), default(Func<int, CancelationToken, Promise<int>>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToDictionaryAsync(captureValue, (cv, x, _) => Promise.Resolved(0), (x, _) => Promise.Resolved(0), nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToDictionaryAsync(default(Func<int, CancelationToken, Promise<int>>), captureValue, (cv, x, _) => Promise.Resolved(0)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToDictionaryAsync((x, _) => Promise.Resolved(0), captureValue, default(Func<string, int, CancelationToken, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToDictionaryAsync(default(Func<int, CancelationToken, Promise<int>>), captureValue, (cv, x, _) => Promise.Resolved(0), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToDictionaryAsync((x, _) => Promise.Resolved(0), captureValue, default(Func<string, int, CancelationToken, Promise<int>>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToDictionaryAsync((x, _) => Promise.Resolved(0), captureValue, (cv, x, _) => Promise.Resolved(0), nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToDictionaryAsync(captureValue, default(Func<string, int, CancelationToken, Promise<int>>), captureValue, (cv, x, _) => Promise.Resolved(0)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToDictionaryAsync(captureValue, (cv, x, _) => Promise.Resolved(0), captureValue, default(Func<string, int, CancelationToken, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToDictionaryAsync(captureValue, default(Func<string, int, CancelationToken, Promise<int>>), captureValue, (cv, x, _) => Promise.Resolved(0), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToDictionaryAsync(captureValue, (cv, x, _) => Promise.Resolved(0), captureValue, default(Func<string, int, CancelationToken, Promise<int>>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ToDictionaryAsync(captureValue, (cv, x, _) => Promise.Resolved(0), captureValue, (cv, x, _) => Promise.Resolved(0), nullComparer));


            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToDictionaryAsync(default(Func<int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToDictionaryAsync(default(Func<int, int>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToDictionaryAsync(x => 0, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToDictionaryAsync(captureValue, default(Func<string, int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToDictionaryAsync(captureValue, default(Func<string, int, int>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToDictionaryAsync(captureValue, (cv, x) => 0, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToDictionaryAsync(default(Func<int, int>), x => 0));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToDictionaryAsync(x => 0, default(Func<int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToDictionaryAsync(default(Func<int, int>), x => 0, EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToDictionaryAsync(x => 0, default(Func<int, int>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToDictionaryAsync(x => 0, x => 0, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToDictionaryAsync(captureValue, default(Func<string, int, int>), x => 0));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToDictionaryAsync(captureValue, (cv, x) => 0, default(Func<int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToDictionaryAsync(captureValue, default(Func<string, int, int>), x => 0, EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToDictionaryAsync(captureValue, (cv, x) => 0, default(Func<int, int>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToDictionaryAsync(captureValue, (cv, x) => 0, x => 0, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToDictionaryAsync(default(Func<int, int>), captureValue, (cv, x) => 0));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToDictionaryAsync(x => 0, captureValue, default(Func<string, int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToDictionaryAsync(default(Func<int, int>), captureValue, (cv, x) => 0, EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToDictionaryAsync(x => 0, captureValue, default(Func<string, int, int>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToDictionaryAsync(x => 0, captureValue, (cv, x) => 0, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToDictionaryAsync(captureValue, default(Func<string, int, int>), captureValue, (cv, x) => 0));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToDictionaryAsync(captureValue, (cv, x) => 0, captureValue, default(Func<string, int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToDictionaryAsync(captureValue, default(Func<string, int, int>), captureValue, (cv, x) => 0, EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToDictionaryAsync(captureValue, (cv, x) => 0, captureValue, default(Func<string, int, int>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToDictionaryAsync(captureValue, (cv, x) => 0, captureValue, (cv, x) => 0, nullComparer));


            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToDictionaryAsync(default(Func<int, CancelationToken, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToDictionaryAsync(default(Func<int, CancelationToken, Promise<int>>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToDictionaryAsync((x, _) => Promise.Resolved(0), nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToDictionaryAsync(captureValue, default(Func<string, int, CancelationToken, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToDictionaryAsync(captureValue, default(Func<string, int, CancelationToken, Promise<int>>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToDictionaryAsync(captureValue, (cv, x, _) => Promise.Resolved(0), nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToDictionaryAsync(default(Func<int, CancelationToken, Promise<int>>), (int x, CancelationToken _) => Promise.Resolved(0)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToDictionaryAsync((x, _) => Promise.Resolved(0), default(Func<int, CancelationToken, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToDictionaryAsync(default(Func<int, CancelationToken, Promise<int>>), (x, _) => Promise.Resolved(0), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToDictionaryAsync((x, _) => Promise.Resolved(0), default(Func<int, CancelationToken, Promise<int>>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToDictionaryAsync((x, _) => Promise.Resolved(0), (x, _) => Promise.Resolved(0), nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToDictionaryAsync(captureValue, default(Func<string, int, CancelationToken, Promise<int>>), (x, _) => Promise.Resolved(0)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToDictionaryAsync(captureValue, (cv, x, _) => Promise.Resolved(0), default(Func<int, CancelationToken, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToDictionaryAsync(captureValue, default(Func<string, int, CancelationToken, Promise<int>>), (x, _) => Promise.Resolved(0), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToDictionaryAsync(captureValue, (cv, x, _) => Promise.Resolved(0), default(Func<int, CancelationToken, Promise<int>>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToDictionaryAsync(captureValue, (cv, x, _) => Promise.Resolved(0), (x, _) => Promise.Resolved(0), nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToDictionaryAsync(default(Func<int, CancelationToken, Promise<int>>), captureValue, (cv, x, _) => Promise.Resolved(0)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToDictionaryAsync((x, _) => Promise.Resolved(0), captureValue, default(Func<string, int, CancelationToken, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToDictionaryAsync(default(Func<int, CancelationToken, Promise<int>>), captureValue, (cv, x, _) => Promise.Resolved(0), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToDictionaryAsync((x, _) => Promise.Resolved(0), captureValue, default(Func<string, int, CancelationToken, Promise<int>>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToDictionaryAsync((x, _) => Promise.Resolved(0), captureValue, (cv, x, _) => Promise.Resolved(0), nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToDictionaryAsync(captureValue, default(Func<string, int, CancelationToken, Promise<int>>), captureValue, (cv, x, _) => Promise.Resolved(0)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToDictionaryAsync(captureValue, (cv, x, _) => Promise.Resolved(0), captureValue, default(Func<string, int, CancelationToken, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToDictionaryAsync(captureValue, default(Func<string, int, CancelationToken, Promise<int>>), captureValue, (cv, x, _) => Promise.Resolved(0), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToDictionaryAsync(captureValue, (cv, x, _) => Promise.Resolved(0), captureValue, default(Func<string, int, CancelationToken, Promise<int>>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ToDictionaryAsync(captureValue, (cv, x, _) => Promise.Resolved(0), captureValue, (cv, x, _) => Promise.Resolved(0), nullComparer));

            enumerable.GetAsyncEnumerator().DisposeAsync().Forget();
        }
#endif

        // We test all of the overloads.
        private static Promise<Dictionary<TKey, TSource>> ToDictionaryAsync<TSource, TKey>(AsyncEnumerable<TSource> source,
            bool configured,
            bool asyncKey,
            bool captureKey,
            Func<TSource, TKey> keySelector,
            IEqualityComparer<TKey> comparer = null,
            CancelationToken cancelationToken = default)
        {
            if (configured)
            {
                return ToDictionaryAsync(source.ConfigureAwait(SynchronizationOption.Foreground).WithCancelation(cancelationToken), asyncKey, captureKey, keySelector, comparer);
            }

            const string captureKeyValue = "captureKeyValue";

            return asyncKey
                ? captureKey
                    ? source.ToDictionaryAsync(captureKeyValue, async (cv, x, _) =>
                    {
                        Assert.AreEqual(captureKeyValue, cv);
                        return keySelector(x);
                    }, comparer, cancelationToken)
                    : source.ToDictionaryAsync(async (x, _) => keySelector(x), comparer, cancelationToken)
                : captureKey
                    ? source.ToDictionaryAsync(captureKeyValue, (cv, x) =>
                    {
                        Assert.AreEqual(captureKeyValue, cv);
                        return keySelector(x);
                    }, comparer, cancelationToken)
                    : source.ToDictionaryAsync(keySelector, comparer, cancelationToken);
        }

        private static Promise<Dictionary<TKey, TSource>> ToDictionaryAsync<TSource, TKey>(ConfiguredAsyncEnumerable<TSource> source,
            bool async,
            bool captureKey,
            Func<TSource, TKey> keySelector,
            IEqualityComparer<TKey> comparer)
        {
            const string captureKeyValue = "captureKeyValue";

            return async
                ? captureKey
                    ? source.ToDictionaryAsync(captureKeyValue, async (cv, x, _) =>
                    {
                        Assert.AreEqual(captureKeyValue, cv);
                        return keySelector(x);
                    }, comparer)
                    : source.ToDictionaryAsync(async (x, _) => keySelector(x), comparer)
                : captureKey
                    ? source.ToDictionaryAsync(captureKeyValue, (cv, x) =>
                    {
                        Assert.AreEqual(captureKeyValue, cv);
                        return keySelector(x);
                    }, comparer)
                    : source.ToDictionaryAsync(keySelector, comparer);
        }

        private static Promise<Dictionary<TKey, TElement>> ToDictionaryAsync<TSource, TKey, TElement>(AsyncEnumerable<TSource> source,
            bool configured,
            bool asyncKey,
            bool captureKey,
            Func<TSource, TKey> keySelector,
            bool asyncElement,
            bool captureElement,
            Func<TSource, TElement> elementSelector,
            IEqualityComparer<TKey> comparer = null,
            CancelationToken cancelationToken = default)
        {
            if (configured)
            {
                return ToDictionaryAsync(source.ConfigureAwait(SynchronizationOption.Foreground).WithCancelation(cancelationToken),
                    asyncKey, captureKey, keySelector, asyncElement, captureElement, elementSelector, comparer);
            }

            const string captureKeyValue = "captureKeyValue";
            const string captureElementValue = "captureElementValue";

            return asyncKey
                ? captureKey
                    ? asyncElement
                        ? captureElement
                            ? source.ToDictionaryAsync(captureKeyValue, async (cv, x, _) =>
                                {
                                    Assert.AreEqual(captureKeyValue, cv);
                                    return keySelector(x);
                                },
                                captureElementValue, async (cv, x, _) =>
                                {
                                    Assert.AreEqual(captureElementValue, cv);
                                    return elementSelector(x);
                                }, comparer, cancelationToken)
                            : source.ToDictionaryAsync(captureKeyValue, async (cv, x, _) =>
                                {
                                    Assert.AreEqual(captureKeyValue, cv);
                                    return keySelector(x);
                                },
                                captureElementValue, async (cv, x, _) =>
                                {
                                    Assert.AreEqual(captureElementValue, cv);
                                    return elementSelector(x);
                                }, comparer, cancelationToken)
                        : captureElement
                            ? source.ToDictionaryAsync(captureKeyValue, async (cv, x, _) =>
                                {
                                    Assert.AreEqual(captureKeyValue, cv);
                                    return keySelector(x);
                                },
                                captureElementValue, async (cv, x, _) =>
                                {
                                    Assert.AreEqual(captureElementValue, cv);
                                    return elementSelector(x);
                                }, comparer, cancelationToken)
                            : source.ToDictionaryAsync(captureKeyValue, async (cv, x, _) =>
                                {
                                    Assert.AreEqual(captureKeyValue, cv);
                                    return keySelector(x);
                                }, async (x, _) => elementSelector(x), comparer, cancelationToken)
                    : asyncElement
                        ? captureElement
                            ? source.ToDictionaryAsync(async (x, _) => keySelector(x),
                                captureElementValue, async (cv, x, _) =>
                                {
                                    Assert.AreEqual(captureElementValue, cv);
                                    return elementSelector(x);
                                }, comparer, cancelationToken)
                            : source.ToDictionaryAsync(async (x, _) => keySelector(x),
                                captureElementValue, async (cv, x, _) =>
                                {
                                    Assert.AreEqual(captureElementValue, cv);
                                    return elementSelector(x);
                                }, comparer, cancelationToken)
                        : captureElement
                            ? source.ToDictionaryAsync(async (x, _) => keySelector(x),
                                captureElementValue, async (cv, x, _) =>
                                {
                                    Assert.AreEqual(captureElementValue, cv);
                                    return elementSelector(x);
                                }, comparer, cancelationToken)
                            : source.ToDictionaryAsync(async (x, _) => keySelector(x), async (x, _) => elementSelector(x), comparer, cancelationToken)
                : captureKey
                    ? asyncElement
                        ? captureElement
                            ? source.ToDictionaryAsync(captureKeyValue, (cv, x) =>
                                {
                                    Assert.AreEqual(captureKeyValue, cv);
                                    return keySelector(x);
                                },
                                captureElementValue, (cv, x) =>
                                {
                                    Assert.AreEqual(captureElementValue, cv);
                                    return elementSelector(x);
                                }, comparer, cancelationToken)
                            : source.ToDictionaryAsync(captureKeyValue, (cv, x) =>
                                {
                                    Assert.AreEqual(captureKeyValue, cv);
                                    return keySelector(x);
                                },
                                captureElementValue, (cv, x) =>
                                {
                                    Assert.AreEqual(captureElementValue, cv);
                                    return elementSelector(x);
                                }, comparer, cancelationToken)
                        : captureElement
                            ? source.ToDictionaryAsync(captureKeyValue, (cv, x) =>
                                {
                                    Assert.AreEqual(captureKeyValue, cv);
                                    return keySelector(x);
                                },
                                captureElementValue, (cv, x) =>
                                {
                                    Assert.AreEqual(captureElementValue, cv);
                                    return elementSelector(x);
                                }, comparer, cancelationToken)
                            : source.ToDictionaryAsync(captureKeyValue, (cv, x) =>
                                {
                                    Assert.AreEqual(captureKeyValue, cv);
                                    return keySelector(x);
                                }, elementSelector, comparer, cancelationToken)
                    : asyncElement
                        ? captureElement
                            ? source.ToDictionaryAsync(keySelector,
                                captureElementValue, (cv, x) =>
                                {
                                    Assert.AreEqual(captureElementValue, cv);
                                    return elementSelector(x);
                                }, comparer, cancelationToken)
                            : source.ToDictionaryAsync(keySelector,
                                captureElementValue, (cv, x) =>
                                {
                                    Assert.AreEqual(captureElementValue, cv);
                                    return elementSelector(x);
                                }, comparer, cancelationToken)
                        : captureElement
                            ? source.ToDictionaryAsync(keySelector,
                                captureElementValue, async (cv, x, _) =>
                                {
                                    Assert.AreEqual(captureElementValue, cv);
                                    return elementSelector(x);
                                }, comparer, cancelationToken)
                            : source.ToDictionaryAsync(keySelector, async (x, _) => elementSelector(x), comparer, cancelationToken);
        }

        private static Promise<Dictionary<TKey, TElement>> ToDictionaryAsync<TSource, TKey, TElement>(ConfiguredAsyncEnumerable<TSource> source,
            bool asyncKey,
            bool captureKey,
            Func<TSource, TKey> keySelector,
            bool asyncElement,
            bool captureElement,
            Func<TSource, TElement> elementSelector,
            IEqualityComparer<TKey> comparer)
        {
            const string captureKeyValue = "captureKeyValue";
            const string captureElementValue = "captureElementValue";

            return asyncKey
                ? captureKey
                    ? asyncElement
                        ? captureElement
                            ? source.ToDictionaryAsync(captureKeyValue, async (cv, x, _) =>
                                {
                                    Assert.AreEqual(captureKeyValue, cv);
                                    return keySelector(x);
                                },
                                captureElementValue, async (cv, x, _) =>
                                {
                                    Assert.AreEqual(captureElementValue, cv);
                                    return elementSelector(x);
                                }, comparer)
                            : source.ToDictionaryAsync(captureKeyValue, async (cv, x, _) =>
                                {
                                    Assert.AreEqual(captureKeyValue, cv);
                                    return keySelector(x);
                                },
                                captureElementValue, async (cv, x, _) =>
                                {
                                    Assert.AreEqual(captureElementValue, cv);
                                    return elementSelector(x);
                                }, comparer)
                        : captureElement
                            ? source.ToDictionaryAsync(captureKeyValue, async (cv, x, _) =>
                                {
                                    Assert.AreEqual(captureKeyValue, cv);
                                    return keySelector(x);
                                },
                                captureElementValue, async (cv, x, _) =>
                                {
                                    Assert.AreEqual(captureElementValue, cv);
                                    return elementSelector(x);
                                }, comparer)
                            : source.ToDictionaryAsync(captureKeyValue, async (cv, x, _) =>
                                {
                                    Assert.AreEqual(captureKeyValue, cv);
                                    return keySelector(x);
                                }, async (x, _) => elementSelector(x), comparer)
                    : asyncElement
                        ? captureElement
                            ? source.ToDictionaryAsync(async (x, _) => keySelector(x),
                                captureElementValue, async (cv, x, _) =>
                                {
                                    Assert.AreEqual(captureElementValue, cv);
                                    return elementSelector(x);
                                }, comparer)
                            : source.ToDictionaryAsync(async (x, _) => keySelector(x),
                                captureElementValue, async (cv, x, _) =>
                                {
                                    Assert.AreEqual(captureElementValue, cv);
                                    return elementSelector(x);
                                }, comparer)
                        : captureElement
                            ? source.ToDictionaryAsync(async (x, _) => keySelector(x),
                                captureElementValue, async (cv, x, _) =>
                                {
                                    Assert.AreEqual(captureElementValue, cv);
                                    return elementSelector(x);
                                }, comparer)
                            : source.ToDictionaryAsync(async (x, _) => keySelector(x), async (x, _) => elementSelector(x), comparer)
                : captureKey
                    ? asyncElement
                        ? captureElement
                            ? source.ToDictionaryAsync(captureKeyValue, (cv, x) =>
                                {
                                    Assert.AreEqual(captureKeyValue, cv);
                                    return keySelector(x);
                                },
                                captureElementValue, (cv, x) =>
                                {
                                    Assert.AreEqual(captureElementValue, cv);
                                    return elementSelector(x);
                                }, comparer)
                            : source.ToDictionaryAsync(captureKeyValue, (cv, x) =>
                                {
                                    Assert.AreEqual(captureKeyValue, cv);
                                    return keySelector(x);
                                },
                                captureElementValue, (cv, x) =>
                                {
                                    Assert.AreEqual(captureElementValue, cv);
                                    return elementSelector(x);
                                }, comparer)
                        : captureElement
                            ? source.ToDictionaryAsync(captureKeyValue, (cv, x) =>
                                {
                                    Assert.AreEqual(captureKeyValue, cv);
                                    return keySelector(x);
                                },
                                captureElementValue, (cv, x) =>
                                {
                                    Assert.AreEqual(captureElementValue, cv);
                                    return elementSelector(x);
                                }, comparer)
                            : source.ToDictionaryAsync(captureKeyValue, (cv, x) =>
                                {
                                    Assert.AreEqual(captureKeyValue, cv);
                                    return keySelector(x);
                                }, elementSelector, comparer)
                    : asyncElement
                        ? captureElement
                            ? source.ToDictionaryAsync(keySelector,
                                captureElementValue, (cv, x) =>
                                {
                                    Assert.AreEqual(captureElementValue, cv);
                                    return elementSelector(x);
                                }, comparer)
                            : source.ToDictionaryAsync(keySelector,
                                captureElementValue, (cv, x) =>
                                {
                                    Assert.AreEqual(captureElementValue, cv);
                                    return elementSelector(x);
                                }, comparer)
                        : captureElement
                            ? source.ToDictionaryAsync(keySelector,
                                captureElementValue, async (cv, x, _) =>
                                {
                                    Assert.AreEqual(captureElementValue, cv);
                                    return elementSelector(x);
                                }, comparer)
                            : source.ToDictionaryAsync(keySelector, async (x, _) => elementSelector(x), comparer);
        }

        [Test]
        public void ToDictionary_Empty_KeySelector(
            [Values] bool configured,
            [Values] bool asyncKey,
            [Values] bool captureKey)
        {
            Promise.Run(async () =>
            {
                var res = ToDictionaryAsync(AsyncEnumerable<int>.Empty(), configured, asyncKey, captureKey, x => x);
                Assert.Zero((await res).Count);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void ToDictionary_FromArray_KeySelector(
            [Values] bool configured,
            [Values] bool asyncKey,
            [Values] bool captureKey)
        {
            Promise.Run(async () =>
            {
                var xs = new[] { 42, 25, 39 };
                var res = ToDictionaryAsync(xs.ToAsyncEnumerable(), configured, asyncKey, captureKey, x => x);
                CollectionAssert.AreEqual(xs.ToDictionary(x => x), await res);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void ToDictionary_FromArray_KeySelector_SameKeyThrows(
            [Values] bool configured,
            [Values] bool asyncKey,
            [Values] bool captureKey)
        {
            Promise.Run(async () =>
            {
                var xs = new[] { 42, 25, 39 };
                var res = ToDictionaryAsync(xs.ToAsyncEnumerable(), configured, asyncKey, captureKey, x => x % 2);
                await TestHelper.AssertThrowsAsync<ArgumentException>(() => res);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void ToDictionary_SourceThrows_KeySelector(
            [Values] bool configured,
            [Values] bool asyncKey,
            [Values] bool captureKey)
        {
            Promise.Run(async () =>
            {
                var ex = new Exception("Bang!");
                var res = ToDictionaryAsync(AsyncEnumerable<int>.Rejected(ex), configured, asyncKey, captureKey, x => x % 2);
                await TestHelper.AssertThrowsAsync(() => res, ex);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void ToDictionary_KeySelectorThrows(
            [Values] bool configured,
            [Values] bool asyncKey,
            [Values] bool captureKey)
        {
            Promise.Run(async () =>
            {
                var ex = new Exception("Bang!");
                var xs = new[] { 42, 25, 39 };
                var res = ToDictionaryAsync(xs.ToAsyncEnumerable(), configured, asyncKey, captureKey, x => { throw ex; return x; });
                await TestHelper.AssertThrowsAsync(() => res, ex);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void ToDictionary_FromArray_KeySelector_Comparer(
            [Values] bool configured,
            [Values] bool asyncKey,
            [Values] bool captureKey)
        {
            Promise.Run(async () =>
            {
                var xs = new[] { -42, 41, 25, 39, -24 };
                var res = ToDictionaryAsync(xs.ToAsyncEnumerable(), configured, asyncKey, captureKey, x => x, new AbsComparer());
                CollectionAssert.AreEqual(xs.ToDictionary(x => x, new AbsComparer()), await res);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void ToDictionary_Empty_KeySelector_ElementSelector(
            [Values] bool configured,
            [Values] bool asyncKey,
            [Values] bool captureKey,
            [Values] bool asyncElement,
            [Values] bool captureElement)
        {
            Promise.Run(async () =>
            {
                var res = ToDictionaryAsync(AsyncEnumerable<int>.Empty(), configured, asyncKey, captureKey, x => x, asyncElement, captureElement, x => x);
                Assert.Zero((await res).Count);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void ToDictionary_FromArray_KeySelector_ElementSelector(
            [Values] bool configured,
            [Values] bool asyncKey,
            [Values] bool captureKey,
            [Values] bool asyncElement,
            [Values] bool captureElement)
        {
            Promise.Run(async () =>
            {
                var xs = new[] { 42, 25, 39 };
                var res = ToDictionaryAsync(xs.ToAsyncEnumerable(), configured, asyncKey, captureKey, x => x, asyncElement, captureElement, x => x);
                CollectionAssert.AreEqual(xs.ToDictionary(x => x), await res);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void ToDictionary_FromArray_KeySelector_ElementSelector_SameKeyThrows(
            [Values] bool configured,
            [Values] bool asyncKey,
            [Values] bool captureKey,
            [Values] bool asyncElement,
            [Values] bool captureElement)
        {
            Promise.Run(async () =>
            {
                var xs = new[] { 42, 25, 39 };
                var res = ToDictionaryAsync(xs.ToAsyncEnumerable(), configured, asyncKey, captureKey, x => x % 2, asyncElement, captureElement, x => x);
                await TestHelper.AssertThrowsAsync<ArgumentException>(() => res);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void ToDictionary_ThrowSource_KeySelector_ElementSelector(
            [Values] bool configured,
            [Values] bool asyncKey,
            [Values] bool captureKey,
            [Values] bool asyncElement,
            [Values] bool captureElement)
        {
            Promise.Run(async () =>
            {
                var ex = new Exception("Bang!");
                var res = ToDictionaryAsync(AsyncEnumerable<int>.Rejected(ex), configured, asyncKey, captureKey, x => x % 2, asyncElement, captureElement, x => x);
                await TestHelper.AssertThrowsAsync(() => res, ex);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void ToDictionary_KeySelectorThrows_ElementSelector(
            [Values] bool configured,
            [Values] bool asyncKey,
            [Values] bool captureKey,
            [Values] bool asyncElement,
            [Values] bool captureElement)
        {
            Promise.Run(async () =>
            {
                var ex = new Exception("Bang!");
                var xs = new[] { 42, 25, 39 };
                var res = ToDictionaryAsync(xs.ToAsyncEnumerable(), configured, asyncKey, captureKey, x => { throw ex; return x; }, asyncElement, captureElement, x => x);
                await TestHelper.AssertThrowsAsync(() => res, ex);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void ToDictionary_KeySelector_ElementSelectorThrows(
            [Values] bool configured,
            [Values] bool asyncKey,
            [Values] bool captureKey,
            [Values] bool asyncElement,
            [Values] bool captureElement)
        {
            Promise.Run(async () =>
            {
                var ex = new Exception("Bang!");
                var xs = new[] { 42, 25, 39 };
                var res = ToDictionaryAsync(xs.ToAsyncEnumerable(), configured, asyncKey, captureKey, x => x, asyncElement, captureElement, x => { throw ex; return x; });
                await TestHelper.AssertThrowsAsync(() => res, ex);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void ToDictionary_FromArray_KeySelector_ElementSelector_Comparer(
            [Values] bool configured,
            [Values] bool asyncKey,
            [Values] bool captureKey,
            [Values] bool asyncElement,
            [Values] bool captureElement)
        {
            Promise.Run(async () =>
            {
                var xs = new[] { -42, 41, 25, 39, -24 };
                var res = ToDictionaryAsync(xs.ToAsyncEnumerable(), configured, asyncKey, captureKey, x => x, asyncElement, captureElement, x => x, new AbsComparer());
                CollectionAssert.AreEqual(xs.ToDictionary(x => x, new AbsComparer()), await res);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        private sealed class AbsComparer : IEqualityComparer<int>
        {
            public bool Equals(int x, int y)
                => EqualityComparer<int>.Default.Equals(Math.Abs(x), Math.Abs(y));

            public int GetHashCode(int obj)
                => EqualityComparer<int>.Default.GetHashCode(Math.Abs(obj));
        }

        [Test]
        public void ToDictionary_KeySelector_Cancel(
            [Values] bool configured,
            [Values] bool asyncKey,
            [Values] bool captureKey)
        {
            Promise.Run(async () =>
            {
                var deferred = Promise.NewDeferred();
                var xs = AsyncEnumerable.Create<int>(async (writer, cancelationToken) =>
                {
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(0);
                    await deferred.Promise;
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(2);
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(4);
                });
                using (var cancelationSource = CancelationSource.New())
                {
                    var res = ToDictionaryAsync(xs, configured, asyncKey, captureKey, x => x, null, cancelationSource.Token);
                    cancelationSource.Cancel();
                    deferred.Resolve();
                    await TestHelper.AssertCanceledAsync(() => res);
                }
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void ToDictionary_KeySelector_ElementSelector_Cancel(
            [Values] bool configured,
            [Values] bool asyncKey,
            [Values] bool captureKey,
            [Values] bool asyncElement,
            [Values] bool captureElement)
        {
            Promise.Run(async () =>
            {
                var deferred = Promise.NewDeferred();
                var xs = AsyncEnumerable.Create<int>(async (writer, cancelationToken) =>
                {
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(0);
                    await deferred.Promise;
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(2);
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(4);
                });
                using (var cancelationSource = CancelationSource.New())
                {
                    var res = ToDictionaryAsync(xs, configured, asyncKey, captureKey, x => x, asyncElement, captureElement, x => x, null, cancelationSource.Token);
                    cancelationSource.Cancel();
                    deferred.Resolve();
                    await TestHelper.AssertCanceledAsync(() => res);
                }
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }
    }
}