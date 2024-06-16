#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using System.Linq;
using Proto.Promises;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace ProtoPromiseTests.APIs
{
    public class EachTests
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

        private static IEnumerable<TestCaseData> GetArgs()
        {
            var completeTypes = new CompleteType[]
            {
                CompleteType.Resolve,
                CompleteType.Reject,
                CompleteType.Cancel
            };
            var alreadyCompletes = new bool[] { false, true };
            // Only test a few combinations to keep test counts down.
            var completeIndicesGroups = new int[][]
            {
                new int[] { 0, 1, 2 },
                new int[] { 1, 0, 2 },
                new int[] { 2, 0, 1 },
            };

            var generatedCombos = new List<(CompleteType completeType, bool isAlreadyComplete, int completeIndex)[]>();
            foreach (var completeIndices in completeIndicesGroups)
            {
                generatedCombos.Clear();

                var args = new (CompleteType completeType, bool isAlreadyComplete, int completeIndex)[completeIndices.Length];
                for (int i = 0; i < args.Length; ++i)
                {
                    args[i] = (CompleteType.Resolve, false, completeIndices[i]);
                }
                generatedCombos.Add(args);

                for (int i = 0; i < completeIndices.Length; ++i)
                {
                    for (int j = 0, max = generatedCombos.Count; j < max; j++)
                    {
                        foreach (var completeType in completeTypes)
                        {
                            foreach (var alreadyComplete in alreadyCompletes)
                            {
                                var combo = generatedCombos[j].ToArray();
                                combo[i].completeType = completeType;
                                combo[i].isAlreadyComplete = alreadyComplete;
                                if (!generatedCombos.Any(c => Enumerable.SequenceEqual(c, combo)))
                                {
                                    generatedCombos.Add(combo);
                                }
                            }
                        }
                    }
                }

                foreach (var combo in generatedCombos)
                {
                    // Make sure completeIndex is always correct in the face of already completes.
                    bool alreadyCompleteOutOfOrder = false;
                    bool notAlreadyComplete = false;
                    foreach (var c in combo.OrderBy(x => x.completeIndex))
                    {
                        if (c.isAlreadyComplete && notAlreadyComplete)
                        {
                            alreadyCompleteOutOfOrder = true;
                            break;
                        }
                        if (!c.isAlreadyComplete)
                        {
                            notAlreadyComplete = true;
                        }
                    }
                    if (alreadyCompleteOutOfOrder)
                    {
                        continue;
                    }

                    int lastCompleteIndex = -1;
                    alreadyCompleteOutOfOrder = false;
                    foreach (var c in combo)
                    {
                        if (!c.isAlreadyComplete)
                        {
                            continue;
                        }
                        if (c.completeIndex < lastCompleteIndex)
                        {
                            alreadyCompleteOutOfOrder = true;
                            break;
                        }
                        lastCompleteIndex = c.completeIndex;
                    }
                    if (alreadyCompleteOutOfOrder)
                    {
                        continue;
                    }

                    yield return new TestCaseData((object) combo);
                }
            }
        }

        [Test, TestCaseSource(nameof(GetArgs))]
        public void EachPromiseResultsAreYieldedInCompletionOrder_void((CompleteType completeType, bool isAlreadyComplete, int completeIndex)[] args)
        {
            Promise.Run(async () =>
            {
                var rejections = new Exception[args.Length];
                var tryCompleters = new Action[args.Length];
                var promises = new Promise[args.Length];
                for (int i = 0; i < args.Length; ++i)
                {
                    var (completeType, isAlreadyComplete, completeIndex) = args[i];
                    rejections[completeIndex] = new Exception($"Rejected completeIndex: {completeIndex}");
                    promises[i] = TestHelper.BuildPromise(completeType, isAlreadyComplete, rejections[completeIndex], out tryCompleters[completeIndex]);
                }

                var asyncEnumerator = Promise.Each(promises).GetAsyncEnumerator();
                foreach (var (completeType, _, completeIndex) in args.OrderBy(x => x.completeIndex))
                {
                    tryCompleters[completeIndex].Invoke();
                    Assert.True(await asyncEnumerator.MoveNextAsync());
                    var resultContainer = asyncEnumerator.Current;
                    Assert.AreEqual((Promise.State) completeType, resultContainer.State);
                    if (resultContainer.State == Promise.State.Rejected)
                    {
                        Assert.AreEqual(rejections[completeIndex], resultContainer.Reason);
                    }
                }
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void EachDisposeEarly_void()
        {
            Promise.Run(async () =>
            {
                var args = new (CompleteType completeType, bool isAlreadyComplete, int completeIndex)[]
                {
                    (CompleteType.Resolve, false, 1),
                    (CompleteType.Resolve, true, 0),
                    (CompleteType.Cancel, false, 3),
                    (CompleteType.Reject, false, 2),
                };
                var rejections = new Exception[args.Length];
                var tryCompleters = new Action[args.Length];
                var promises = new Promise[args.Length];
                for (int i = 0; i < args.Length; ++i)
                {
                    var (completeType, isAlreadyComplete, completeIndex) = args[i];
                    rejections[completeIndex] = new Exception($"Rejected completeIndex: {completeIndex}");
                    promises[i] = TestHelper.BuildPromise(completeType, isAlreadyComplete, rejections[completeIndex], out tryCompleters[completeIndex]);
                }

                args = args.OrderBy(x => x.completeIndex).ToArray();

                var asyncEnumerator = Promise.Each(promises).GetAsyncEnumerator();
                for (int i = 0; i < args.Length / 2; ++i)
                {
                    var (completeType, _, completeIndex) = args[i];
                    tryCompleters[completeIndex].Invoke();
                    Assert.True(await asyncEnumerator.MoveNextAsync());
                    var resultContainer = asyncEnumerator.Current;
                    Assert.AreEqual((Promise.State) completeType, resultContainer.State);
                    if (resultContainer.State == Promise.State.Rejected)
                    {
                        Assert.AreEqual(rejections[completeIndex], resultContainer.Reason);
                    }
                }
                await asyncEnumerator.DisposeAsync();

                for (int i = args.Length / 2; i < args.Length; ++i)
                {
                    tryCompleters[i].Invoke();
                }
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void EachCancelEarly_void()
        {
            Promise.Run(async () =>
            {
                var args = new (CompleteType completeType, bool isAlreadyComplete, int completeIndex)[]
                {
                    (CompleteType.Resolve, false, 1),
                    (CompleteType.Resolve, true, 0),
                    (CompleteType.Cancel, false, 3),
                    (CompleteType.Reject, false, 2),
                };
                var rejections = new Exception[args.Length];
                var tryCompleters = new Action[args.Length];
                var promises = new Promise[args.Length];
                for (int i = 0; i < args.Length; ++i)
                {
                    var (completeType, isAlreadyComplete, completeIndex) = args[i];
                    rejections[completeIndex] = new Exception($"Rejected completeIndex: {completeIndex}");
                    promises[i] = TestHelper.BuildPromise(completeType, isAlreadyComplete, rejections[completeIndex], out tryCompleters[completeIndex]);
                }

                args = args.OrderBy(x => x.completeIndex).ToArray();

                var cancelationSource = CancelationSource.New();
                var asyncEnumerator = Promise.Each(promises).WithCancelation(cancelationSource.Token).GetAsyncEnumerator();
                for (int i = 0; i < args.Length / 2; ++i)
                {
                    var (completeType, _, completeIndex) = args[i];
                    tryCompleters[i].Invoke();
                    Assert.True(await asyncEnumerator.MoveNextAsync());
                    var resultContainer = asyncEnumerator.Current;
                    Assert.AreEqual((Promise.State) args[i].completeType, resultContainer.State);
                    if (resultContainer.State == Promise.State.Rejected)
                    {
                        Assert.AreEqual(rejections[i], resultContainer.Reason);
                    }
                }
                cancelationSource.Cancel();
                await TestHelper.AssertCanceledAsync(() => asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();

                for (int i = args.Length / 2; i < args.Length; ++i)
                {
                    tryCompleters[i].Invoke();
                }
                cancelationSource.Dispose();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test, TestCaseSource(nameof(GetArgs))]
        public void EachPromiseResultsAreYieldedInCompletionOrder_T((CompleteType completeType, bool isAlreadyComplete, int completeIndex)[] args)
        {
            Promise.Run(async () =>
            {
                var rejections = new Exception[args.Length];
                var tryCompleters = new Action[args.Length];
                var promises = new Promise<int>[args.Length];
                for (int i = 0; i < args.Length; ++i)
                {
                    var (completeType, isAlreadyComplete, completeIndex) = args[i];
                    rejections[completeIndex] = new Exception($"Rejected completeIndex: {completeIndex}");
                    promises[i] = TestHelper.BuildPromise(completeType, isAlreadyComplete, completeIndex, rejections[completeIndex], out tryCompleters[completeIndex]);
                }

                var asyncEnumerator = Promise.Each(promises).GetAsyncEnumerator();
                foreach (var (completeType, _, completeIndex) in args.OrderBy(x => x.completeIndex))
                {
                    tryCompleters[completeIndex].Invoke();
                    Assert.True(await asyncEnumerator.MoveNextAsync());
                    var resultContainer = asyncEnumerator.Current;
                    Assert.AreEqual((Promise.State) completeType, resultContainer.State);
                    if (resultContainer.State == Promise.State.Resolved)
                    {
                        Assert.AreEqual(completeIndex, resultContainer.Value);
                    }
                    else if (resultContainer.State == Promise.State.Rejected)
                    {
                        Assert.AreEqual(rejections[completeIndex], resultContainer.Reason);
                    }
                }
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void EachDisposeEarly_T()
        {
            Promise.Run(async () =>
            {
                var args = new (CompleteType completeType, bool isAlreadyComplete, int completeIndex)[]
                {
                    (CompleteType.Resolve, false, 1),
                    (CompleteType.Resolve, true, 0),
                    (CompleteType.Cancel, false, 3),
                    (CompleteType.Reject, false, 2),
                };
                var rejections = new Exception[args.Length];
                var tryCompleters = new Action[args.Length];
                var promises = new Promise<int>[args.Length];
                for (int i = 0; i < args.Length; ++i)
                {
                    var (completeType, isAlreadyComplete, completeIndex) = args[i];
                    rejections[completeIndex] = new Exception($"Rejected completeIndex: {completeIndex}");
                    promises[i] = TestHelper.BuildPromise(completeType, isAlreadyComplete, completeIndex, rejections[completeIndex], out tryCompleters[completeIndex]);
                }

                args = args.OrderBy(x => x.completeIndex).ToArray();

                var asyncEnumerator = Promise.Each(promises).GetAsyncEnumerator();
                for (int i = 0; i < args.Length / 2; ++i)
                {
                    var (completeType, _, completeIndex) = args[i];
                    tryCompleters[completeIndex].Invoke();
                    Assert.True(await asyncEnumerator.MoveNextAsync());
                    var resultContainer = asyncEnumerator.Current;
                    Assert.AreEqual((Promise.State) completeType, resultContainer.State);
                    if (resultContainer.State == Promise.State.Resolved)
                    {
                        Assert.AreEqual(completeIndex, resultContainer.Value);
                    }
                    else if (resultContainer.State == Promise.State.Rejected)
                    {
                        Assert.AreEqual(rejections[completeIndex], resultContainer.Reason);
                    }
                }
                await asyncEnumerator.DisposeAsync();

                for (int i = args.Length / 2; i < args.Length; ++i)
                {
                    tryCompleters[i].Invoke();
                }
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void EachCancelEarly_T()
        {
            Promise.Run(async () =>
            {
                var args = new (CompleteType completeType, bool isAlreadyComplete, int completeIndex)[]
                {
                    (CompleteType.Resolve, false, 1),
                    (CompleteType.Resolve, true, 0),
                    (CompleteType.Cancel, false, 3),
                    (CompleteType.Reject, false, 2),
                };
                var rejections = new Exception[args.Length];
                var tryCompleters = new Action[args.Length];
                var promises = new Promise<int>[args.Length];
                for (int i = 0; i < args.Length; ++i)
                {
                    var (completeType, isAlreadyComplete, completeIndex) = args[i];
                    rejections[completeIndex] = new Exception($"Rejected completeIndex: {completeIndex}");
                    promises[i] = TestHelper.BuildPromise(completeType, isAlreadyComplete, completeIndex, rejections[completeIndex], out tryCompleters[completeIndex]);
                }

                args = args.OrderBy(x => x.completeIndex).ToArray();

                var cancelationSource = CancelationSource.New();
                var asyncEnumerator = Promise.Each(promises).WithCancelation(cancelationSource.Token).GetAsyncEnumerator();
                for (int i = 0; i < args.Length / 2; ++i)
                {
                    var (completeType, _, completeIndex) = args[i];
                    tryCompleters[i].Invoke();
                    Assert.True(await asyncEnumerator.MoveNextAsync());
                    var resultContainer = asyncEnumerator.Current;
                    Assert.AreEqual((Promise.State) args[i].completeType, resultContainer.State);
                    if (resultContainer.State == Promise.State.Resolved)
                    {
                        Assert.AreEqual(completeIndex, resultContainer.Value);
                    }
                    else if (resultContainer.State == Promise.State.Rejected)
                    {
                        Assert.AreEqual(rejections[completeIndex], resultContainer.Reason);
                    }
                }
                cancelationSource.Cancel();
                await TestHelper.AssertCanceledAsync(() => asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();

                for (int i = args.Length / 2; i < args.Length; ++i)
                {
                    tryCompleters[i].Invoke();
                }
                cancelationSource.Dispose();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }
    }
}