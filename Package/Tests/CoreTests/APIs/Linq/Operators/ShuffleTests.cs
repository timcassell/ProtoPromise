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
using System.Runtime.CompilerServices;
using System.Threading;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace ProtoPromise.Tests.APIs.Linq
{
    public class ShuffleTests
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

        public static IEnumerable<object[]> VariousInputs()
        {
            yield return new object[] { new int[0] };
            yield return new object[] { new int[] { 1 } };
            yield return new object[] { new int[] { 2, 4, 8 } };
            yield return new object[] { new int[] { -1, 2, 5, 6, 7, 8 } };
        }

        [Test, TestCaseSource(nameof(VariousInputs))]
        public void VariousValues_ContainsAllInputValues(int[] values)
        {
            Promise.Run(async () =>
            {
                int[] shuffled = await values.ToAsyncEnumerable().Shuffle().ToArrayAsync();
                Array.Sort(shuffled);
                Assert.AreEqual(values, shuffled);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void ToArrayAsync_ElementsAreRandomized()
        {
            Promise.Run(async () =>
            {
                // The chance that shuffling a thousand elements produces the same order twice is infinitesimal.
                // Repeat just in case.
                for (int i = 0; ; ++i)
                {
                    int length = 1000;
                    int[] first = await AsyncEnumerable.Range(0, length).Shuffle().ToArrayAsync();
                    int[] second = await AsyncEnumerable.Range(0, length).Shuffle().ToArrayAsync();
                    Assert.AreEqual(length, first.Length);
                    Assert.AreEqual(length, second.Length);
                    try
                    {
                        CollectionAssert.AreNotEqual(first, second);
                        break;
                    }
                    catch
                    {
                        if (i > 10)
                        {
                            throw;
                        }
                    }
                }
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Cancelation_Cancels()
        {
            Promise.Run(async () =>
            {
                var asyncEnumerable = AsyncEnumerable<int>.Create(async (writer, cancelationToken) =>
                {
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(1);
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(2);
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(3);
                });
                await TestHelper.AssertCanceledAsync(async () =>
                {
                    var asyncEnumerator = asyncEnumerable.GetAsyncEnumerator(CancelationToken.Canceled());
                    try
                    {
                        while (await asyncEnumerator.MoveNextAsync())
                        {
                            _ = asyncEnumerator.Current;
                        }
                    }
                    finally
                    {
                        await asyncEnumerator.DisposeAsync();
                    }
                });
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }
    }
}