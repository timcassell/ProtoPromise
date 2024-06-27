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

namespace ProtoPromiseTests.APIs.PromiseGroups
{
    public class PromiseEachGroupTests
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

        [Test]
        public void PromiseEachGroup_UsingInvalidatedGroupThrows_void(
            [Values] CancelationType cancelationType,
            [Values] bool suppressUnobservedRejections)
        {
            var voidPromise = Promise.Resolved();
            Assert.Catch<System.InvalidOperationException>(() => default(PromiseEachGroup).Add(voidPromise));
            Assert.Catch<System.InvalidOperationException>(() => default(PromiseEachGroup).GetAsyncEnumerable());

            using (var cancelationSource = CancelationSource.New())
            {
                var eachGroup1 = cancelationType == CancelationType.None ? PromiseEachGroup.New(out _, suppressUnobservedRejections)
                    : cancelationType == CancelationType.Deferred ? PromiseEachGroup.New(cancelationSource.Token, out _, suppressUnobservedRejections)
                    : PromiseEachGroup.New(CancelationToken.Canceled(), out _, suppressUnobservedRejections);

                var eachGroup2 = eachGroup1.Add(Promise.Resolved());
                Assert.Catch<System.InvalidOperationException>(() => eachGroup1.Add(voidPromise));
                Assert.Catch<System.InvalidOperationException>(() => eachGroup1.GetAsyncEnumerable());

                var eachGroup3 = eachGroup2.Add(Promise.Resolved());
                Assert.Catch<System.InvalidOperationException>(() => eachGroup2.Add(voidPromise));
                Assert.Catch<System.InvalidOperationException>(() => eachGroup2.GetAsyncEnumerable());

                eachGroup3.GetAsyncEnumerable().GetAsyncEnumerator().DisposeAsync().Forget();
                Assert.Catch<System.InvalidOperationException>(() => eachGroup3.Add(voidPromise));
                Assert.Catch<System.InvalidOperationException>(() => eachGroup3.GetAsyncEnumerable());

                voidPromise.Forget();
            }
        }

        [Test]
        public void PromiseEachGroup_UsingInvalidatedGroupThrows_T(
            [Values] CancelationType cancelationType,
            [Values] bool suppressUnobservedRejections)
        {
            var intPromise = Promise.Resolved(42);
            Assert.Catch<System.InvalidOperationException>(() => default(PromiseEachGroup<int>).Add(intPromise));
            Assert.Catch<System.InvalidOperationException>(() => default(PromiseEachGroup<int>).GetAsyncEnumerable());

            using (var cancelationSource = CancelationSource.New())
            {
                var eachGroup1 = cancelationType == CancelationType.None ? PromiseEachGroup<int>.New(out _, suppressUnobservedRejections)
                    : cancelationType == CancelationType.Deferred ? PromiseEachGroup<int>.New(cancelationSource.Token, out _, suppressUnobservedRejections)
                    : PromiseEachGroup<int>.New(CancelationToken.Canceled(), out _, suppressUnobservedRejections);

                var eachGroup2 = eachGroup1.Add(Promise.Resolved(2));
                Assert.Catch<System.InvalidOperationException>(() => eachGroup1.Add(intPromise));
                Assert.Catch<System.InvalidOperationException>(() => eachGroup1.GetAsyncEnumerable());

                var eachGroup3 = eachGroup2.Add(Promise.Resolved(2));
                Assert.Catch<System.InvalidOperationException>(() => eachGroup2.Add(intPromise));
                Assert.Catch<System.InvalidOperationException>(() => eachGroup2.GetAsyncEnumerable());

                eachGroup3.GetAsyncEnumerable().GetAsyncEnumerator().DisposeAsync().Forget();
                Assert.Catch<System.InvalidOperationException>(() => eachGroup3.Add(intPromise));
                Assert.Catch<System.InvalidOperationException>(() => eachGroup3.GetAsyncEnumerable());

                intPromise.Forget();
            }
        }

        private static IEnumerable<TestCaseData> GetArgs() => EachTests.GetArgs();

        [Test, TestCaseSource(nameof(GetArgs))]
        public void PromiseEachGroupResultsAreYieldedInCompletionOrder_void((CompleteType completeType, bool isAlreadyComplete, int completeIndex)[] args)
        {
            Promise.Run(async () =>
            {
                var rejections = new Exception[args.Length];
                var tryCompleters = new Action[args.Length];
                
                var eachGroup = PromiseEachGroup.New(out _);
                for (int i = 0; i < args.Length; ++i)
                {
                    var (completeType, isAlreadyComplete, completeIndex) = args[i];
                    rejections[completeIndex] = new Exception($"Rejected completeIndex: {completeIndex}");
                    var promise = TestHelper.BuildPromise(completeType, isAlreadyComplete, rejections[completeIndex], out tryCompleters[completeIndex]);
                    eachGroup = eachGroup.Add(promise);
                }

                var asyncEnumerator = eachGroup.GetAsyncEnumerable().GetAsyncEnumerator();
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
        public void PromiseEachGroupResultsAreYieldedInCompletionOrder_MoveNextAsyncBeforeComplete_void()
        {
            Promise.Run(async () =>
            {
                var args = new (CompleteType completeType, bool isAlreadyComplete, int completeIndex)[]
                {
                    (CompleteType.Resolve, false, 1),
                    (CompleteType.Resolve, false, 0),
                    (CompleteType.Resolve, false, 3),
                    (CompleteType.Resolve, false, 2),
                };
                var rejections = new Exception[args.Length];
                var tryCompleters = new Action[args.Length];
                
                var eachGroup = PromiseEachGroup.New(out _);
                for (int i = 0; i < args.Length; ++i)
                {
                    var (completeType, isAlreadyComplete, completeIndex) = args[i];
                    rejections[completeIndex] = new Exception($"Rejected completeIndex: {completeIndex}");
                    var promise = TestHelper.BuildPromise(completeType, isAlreadyComplete, rejections[completeIndex], out tryCompleters[completeIndex]);
                    eachGroup = eachGroup.Add(promise);
                }

                var asyncEnumerator = eachGroup.GetAsyncEnumerable().GetAsyncEnumerator();
                foreach (var (completeType, _, completeIndex) in args.OrderBy(x => x.completeIndex))
                {
                    bool movedNext = false;
                    var moveNextPromise = asyncEnumerator.MoveNextAsync()
                        .Finally(() => movedNext = true);
                    Assert.False(movedNext);
                    tryCompleters[completeIndex].Invoke();
                    Assert.True(await moveNextPromise);
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
        public void PromiseEachGroupDisposeEarly_void(
            [Values] bool suppressUnobservedRejections)
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

            bool runComplete = false;
            var runPromise = Promise.Run(async () =>
            {
                var eachGroup = PromiseEachGroup.New(out _, suppressUnobservedRejections);
                for (int i = 0; i < args.Length; ++i)
                {
                    var (completeType, isAlreadyComplete, completeIndex) = args[i];
                    rejections[completeIndex] = new Exception($"Rejected completeIndex: {completeIndex}");
                    var promise = TestHelper.BuildPromise(completeType, isAlreadyComplete, rejections[completeIndex], out tryCompleters[completeIndex]);
                    eachGroup = eachGroup.Add(promise);
                }

                args = args.OrderBy(x => x.completeIndex).ToArray();

                var asyncEnumerator = eachGroup.GetAsyncEnumerable().GetAsyncEnumerator();
                for (int i = 0; i < args.Length / 2; ++i)
                {
                    var (completeType, _, completeIndex) = args[i];
                    tryCompleters[completeIndex].Invoke();
                    Assert.True(await asyncEnumerator.MoveNextAsync());
                    var resultContainer = asyncEnumerator.Current;
                    Assert.AreEqual((Promise.State) completeType, resultContainer.State);
                    if (resultContainer.State == Promise.State.Rejected)
                    {
                        Assert.AreEqual(rejections[i], resultContainer.Reason);
                    }
                }

                bool didThrow = false;
                try
                {
                    await asyncEnumerator.DisposeAsync();
                }
                catch (AggregateException e)
                {
                    didThrow = true;
                    Assert.AreEqual(1, e.InnerExceptions.Count);
                    Assert.AreEqual(rejections[2], e.InnerExceptions[0]);
                }
                Assert.AreNotEqual(suppressUnobservedRejections, didThrow);
            }, SynchronizationOption.Synchronous)
                .Finally(() => runComplete = true);

            Assert.False(runComplete);
            for (int i = args.Length / 2; i < args.Length; ++i)
            {
                tryCompleters[args[i].completeIndex].Invoke();
            }
            Assert.True(runComplete);

            runPromise.WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void PromiseEachGroupDisposeWithoutMoveNext_void(
            [Values] bool suppressUnobservedRejections)
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

            bool runComplete = false;
            var runPromise = Promise.Run(async () =>
            {
                var eachGroup = PromiseEachGroup.New(out _, suppressUnobservedRejections);
                for (int i = 0; i < args.Length; ++i)
                {
                    var (completeType, isAlreadyComplete, completeIndex) = args[i];
                    rejections[completeIndex] = new Exception($"Rejected completeIndex: {completeIndex}");
                    var promise = TestHelper.BuildPromise(completeType, isAlreadyComplete, rejections[completeIndex], out tryCompleters[completeIndex]);
                    eachGroup = eachGroup.Add(promise);
                }

                bool didThrow = false;
                try
                {
                    await eachGroup.GetAsyncEnumerable().GetAsyncEnumerator().DisposeAsync();
                }
                catch (AggregateException e)
                {
                    didThrow = true;
                    Assert.AreEqual(1, e.InnerExceptions.Count);
                    Assert.AreEqual(rejections[2], e.InnerExceptions[0]);
                }
                Assert.AreNotEqual(suppressUnobservedRejections, didThrow);
            }, SynchronizationOption.Synchronous)
                .Finally(() => runComplete = true);

            Assert.False(runComplete);
            foreach (var (_, _, completeIndex) in args.OrderBy(x => x.completeIndex))
            {
                tryCompleters[completeIndex].Invoke();
            }
            Assert.True(runComplete);

            runPromise.WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void PromiseEachGroupCancelIterationEarly_void(
            [Values] bool disposeCancelationSourceEarly,
            [Values] bool suppressUnobservedRejections)
        {
            var cancelationSource = CancelationSource.New();
            var args = new (CompleteType completeType, bool isAlreadyComplete, int completeIndex)[]
            {
                (CompleteType.Resolve, false, 1),
                (CompleteType.Resolve, true, 0),
                (CompleteType.Cancel, false, 3),
                (CompleteType.Reject, false, 2),
            };
            var rejections = new Exception[args.Length];
            var tryCompleters = new Action[args.Length];

            bool runComplete = false;
            var runPromise = Promise.Run(async () =>
            {
                var eachGroup = PromiseEachGroup.New(out _, suppressUnobservedRejections);
                for (int i = 0; i < args.Length; ++i)
                {
                    var (completeType, isAlreadyComplete, completeIndex) = args[i];
                    rejections[completeIndex] = new Exception($"Rejected completeIndex: {completeIndex}");
                    var promise = TestHelper.BuildPromise(completeType, isAlreadyComplete, rejections[completeIndex], out tryCompleters[completeIndex]);
                    eachGroup = eachGroup.Add(promise);
                }

                args = args.OrderBy(x => x.completeIndex).ToArray();

                var asyncEnumerator = eachGroup.GetAsyncEnumerable().WithCancelation(cancelationSource.Token).GetAsyncEnumerator();
                for (int i = 0; i < args.Length / 2; ++i)
                {
                    var (completeType, _, completeIndex) = args[i];
                    tryCompleters[completeIndex].Invoke();
                    Assert.True(await asyncEnumerator.MoveNextAsync());
                    var resultContainer = asyncEnumerator.Current;
                    Assert.AreEqual((Promise.State) completeType, resultContainer.State);
                    if (resultContainer.State == Promise.State.Rejected)
                    {
                        Assert.AreEqual(rejections[i], resultContainer.Reason);
                    }
                }
                cancelationSource.Cancel();
                if (disposeCancelationSourceEarly)
                {
                    cancelationSource.Dispose();
                }
                await TestHelper.AssertCanceledAsync(() => asyncEnumerator.MoveNextAsync());

                bool didThrow = false;
                try
                {
                    await asyncEnumerator.DisposeAsync();
                }
                catch (AggregateException e)
                {
                    didThrow = true;
                    Assert.AreEqual(1, e.InnerExceptions.Count);
                    Assert.AreEqual(rejections[2], e.InnerExceptions[0]);
                }
                Assert.AreNotEqual(suppressUnobservedRejections, didThrow);
            }, SynchronizationOption.Synchronous)
                .Finally(() => runComplete = true);

            Assert.False(runComplete);
            for (int i = args.Length / 2; i < args.Length; ++i)
            {
                tryCompleters[args[i].completeIndex].Invoke();
            }
            Assert.True(runComplete);
            cancelationSource.TryDispose();

            runPromise.WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void PromiseEachGroupIterationCancelationSourceDisposedEarly_void()
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
                
                var eachGroup = PromiseEachGroup.New(out _);
                for (int i = 0; i < args.Length; ++i)
                {
                    var (completeType, isAlreadyComplete, completeIndex) = args[i];
                    rejections[completeIndex] = new Exception($"Rejected completeIndex: {completeIndex}");
                    var promise = TestHelper.BuildPromise(completeType, isAlreadyComplete, rejections[completeIndex], out tryCompleters[completeIndex]);
                    eachGroup = eachGroup.Add(promise);
                }

                args = args.OrderBy(x => x.completeIndex).ToArray();

                var cancelationSource = CancelationSource.New();
                var asyncEnumerator = eachGroup.GetAsyncEnumerable().WithCancelation(cancelationSource.Token).GetAsyncEnumerator();
                for (int i = 0; i < args.Length / 2; ++i)
                {
                    var (completeType, _, completeIndex) = args[i];
                    tryCompleters[completeIndex].Invoke();
                    Assert.True(await asyncEnumerator.MoveNextAsync());
                    var resultContainer = asyncEnumerator.Current;
                    Assert.AreEqual((Promise.State) completeType, resultContainer.State);
                    if (resultContainer.State == Promise.State.Rejected)
                    {
                        Assert.AreEqual(rejections[i], resultContainer.Reason);
                    }
                }
                cancelationSource.Dispose();

                for (int i = args.Length / 2; i < args.Length; ++i)
                {
                    var (completeType, _, completeIndex) = args[i];
                    tryCompleters[completeIndex].Invoke();
                    Assert.True(await asyncEnumerator.MoveNextAsync());
                    var resultContainer = asyncEnumerator.Current;
                    Assert.AreEqual((Promise.State) completeType, resultContainer.State);
                    if (resultContainer.State == Promise.State.Rejected)
                    {
                        Assert.AreEqual(rejections[i], resultContainer.Reason);
                    }
                }
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void PromiseEachGroupIterationCanceledAfterAllCompleteAndBeforeMoveNextAsync_void()
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
                
                var eachGroup = PromiseEachGroup.New(out _);
                for (int i = 0; i < args.Length; ++i)
                {
                    var (completeType, isAlreadyComplete, completeIndex) = args[i];
                    rejections[completeIndex] = new Exception($"Rejected completeIndex: {completeIndex}");
                    var promise = TestHelper.BuildPromise(completeType, isAlreadyComplete, rejections[completeIndex], out tryCompleters[completeIndex]);
                    eachGroup = eachGroup.Add(promise);
                }

                args = args.OrderBy(x => x.completeIndex).ToArray();

                var cancelationSource = CancelationSource.New();
                var asyncEnumerator = eachGroup.GetAsyncEnumerable().WithCancelation(cancelationSource.Token).GetAsyncEnumerator();
                for (int i = 0; i < args.Length; ++i)
                {
                    var (completeType, _, completeIndex) = args[i];
                    tryCompleters[completeIndex].Invoke();
                    Assert.True(await asyncEnumerator.MoveNextAsync());
                    var resultContainer = asyncEnumerator.Current;
                    Assert.AreEqual((Promise.State) completeType, resultContainer.State);
                    if (resultContainer.State == Promise.State.Rejected)
                    {
                        Assert.AreEqual(rejections[i], resultContainer.Reason);
                    }
                }
                cancelationSource.Cancel();
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
                cancelationSource.TryDispose();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void PromiseEachGroupCancelGroupEarly_void(
            [Values] bool disposeCancelationSourceEarly)
        {
            Promise.Run(async () =>
            {
                var args = new (CompleteType completeType, bool isAlreadyComplete, int completeIndex)[]
                {
                    (CompleteType.Resolve, false, 1),
                    (CompleteType.Resolve, true, 0),
                    (CompleteType.Resolve, false, 3),
                    (CompleteType.Reject, false, 2),
                };
                var rejections = new Exception[args.Length];
                var tryCompleters = new Action[args.Length];

                var cancelationSource = CancelationSource.New();
                var eachGroup = PromiseEachGroup.New(cancelationSource.Token, out var groupCancelationToken);
                for (int i = 0; i < args.Length; ++i)
                {
                    var (completeType, isAlreadyComplete, completeIndex) = args[i];
                    rejections[completeIndex] = new Exception($"Rejected completeIndex: {completeIndex}");
                    var promise = TestHelper.BuildPromise(completeType, isAlreadyComplete, rejections[completeIndex], groupCancelationToken, out tryCompleters[completeIndex]);
                    eachGroup = eachGroup.Add(promise);
                }

                args = args.OrderBy(x => x.completeIndex).ToArray();
                var asyncEnumerator = eachGroup.GetAsyncEnumerable().GetAsyncEnumerator();
                for (int i = 0; i < args.Length / 2; ++i)
                {
                    var (completeType, _, completeIndex) = args[i];
                    tryCompleters[completeIndex].Invoke();
                    Assert.True(await asyncEnumerator.MoveNextAsync());
                    var resultContainer = asyncEnumerator.Current;
                    Assert.AreEqual((Promise.State) completeType, resultContainer.State);
                    if (resultContainer.State == Promise.State.Rejected)
                    {
                        Assert.AreEqual(rejections[i], resultContainer.Reason);
                    }
                }
                cancelationSource.Cancel();
                if (disposeCancelationSourceEarly)
                {
                    cancelationSource.Dispose();
                }

                for (int i = args.Length / 2; i < args.Length; ++i)
                {
                    tryCompleters[args[i].completeIndex].Invoke();
                    Assert.True(await asyncEnumerator.MoveNextAsync());
                    Assert.AreEqual(Promise.State.Canceled, asyncEnumerator.Current.State);
                }
                await asyncEnumerator.DisposeAsync();
                cancelationSource.TryDispose();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void PromiseEachGroupGroupCancelationSourceDisposedEarly_void()
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

                var cancelationSource = CancelationSource.New();
                var eachGroup = PromiseEachGroup.New(cancelationSource.Token, out var groupCancelationToken);
                for (int i = 0; i < args.Length; ++i)
                {
                    var (completeType, isAlreadyComplete, completeIndex) = args[i];
                    rejections[completeIndex] = new Exception($"Rejected completeIndex: {completeIndex}");
                    var promise = TestHelper.BuildPromise(completeType, isAlreadyComplete, rejections[completeIndex], groupCancelationToken, out tryCompleters[completeIndex]);
                    eachGroup = eachGroup.Add(promise);
                }

                args = args.OrderBy(x => x.completeIndex).ToArray();
                var asyncEnumerator = eachGroup.GetAsyncEnumerable().WithCancelation(cancelationSource.Token).GetAsyncEnumerator();
                for (int i = 0; i < args.Length / 2; ++i)
                {
                    var (completeType, _, completeIndex) = args[i];
                    tryCompleters[completeIndex].Invoke();
                    Assert.True(await asyncEnumerator.MoveNextAsync());
                    var resultContainer = asyncEnumerator.Current;
                    Assert.AreEqual((Promise.State) completeType, resultContainer.State);
                    if (resultContainer.State == Promise.State.Rejected)
                    {
                        Assert.AreEqual(rejections[i], resultContainer.Reason);
                    }
                }
                cancelationSource.Dispose();

                for (int i = args.Length / 2; i < args.Length; ++i)
                {
                    var (completeType, _, completeIndex) = args[i];
                    tryCompleters[completeIndex].Invoke();
                    Assert.True(await asyncEnumerator.MoveNextAsync());
                    var resultContainer = asyncEnumerator.Current;
                    Assert.AreEqual((Promise.State) completeType, resultContainer.State);
                    if (resultContainer.State == Promise.State.Rejected)
                    {
                        Assert.AreEqual(rejections[i], resultContainer.Reason);
                    }
                }
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void PromiseEachGroupGroupCanceledAfterAllCompleteAndBeforeMoveNextAsync_void()
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

                var cancelationSource = CancelationSource.New();
                var eachGroup = PromiseEachGroup.New(cancelationSource.Token, out var groupCancelationToken);
                for (int i = 0; i < args.Length; ++i)
                {
                    var (completeType, isAlreadyComplete, completeIndex) = args[i];
                    rejections[completeIndex] = new Exception($"Rejected completeIndex: {completeIndex}");
                    var promise = TestHelper.BuildPromise(completeType, isAlreadyComplete, rejections[completeIndex], groupCancelationToken, out tryCompleters[completeIndex]);
                    eachGroup = eachGroup.Add(promise);
                }

                args = args.OrderBy(x => x.completeIndex).ToArray();
                var asyncEnumerator = eachGroup.GetAsyncEnumerable().WithCancelation(cancelationSource.Token).GetAsyncEnumerator();
                for (int i = 0; i < args.Length; ++i)
                {
                    var (completeType, _, completeIndex) = args[i];
                    tryCompleters[completeIndex].Invoke();
                    Assert.True(await asyncEnumerator.MoveNextAsync());
                    var resultContainer = asyncEnumerator.Current;
                    Assert.AreEqual((Promise.State) completeType, resultContainer.State);
                    if (resultContainer.State == Promise.State.Rejected)
                    {
                        Assert.AreEqual(rejections[i], resultContainer.Reason);
                    }
                }
                cancelationSource.Cancel();
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
                cancelationSource.TryDispose();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void PromiseEachGroup2CancelationSources_void()
        {
            Promise.Run(async () =>
            {
                var args = new (CompleteType completeType, bool isAlreadyComplete, int completeIndex)[]
                {
                    (CompleteType.Resolve, false, 1),
                    (CompleteType.Resolve, true, 0),
                    (CompleteType.Resolve, false, 3),
                    (CompleteType.Resolve, false, 2),
                };
                var rejections = new Exception[args.Length];
                var tryCompleters = new Action[args.Length];

                var groupCancelationSource = CancelationSource.New();
                var iterationCancelationSource = CancelationSource.New();
                var eachGroup = PromiseEachGroup.New(groupCancelationSource.Token, out var groupCancelationToken);
                for (int i = 0; i < args.Length; ++i)
                {
                    var (completeType, isAlreadyComplete, completeIndex) = args[i];
                    rejections[completeIndex] = new Exception($"Rejected completeIndex: {completeIndex}");
                    var promise = TestHelper.BuildPromise(completeType, isAlreadyComplete, rejections[completeIndex], groupCancelationToken, out tryCompleters[completeIndex]);
                    eachGroup = eachGroup.Add(promise);
                }

                args = args.OrderBy(x => x.completeIndex).ToArray();
                var asyncEnumerator = eachGroup.GetAsyncEnumerable().WithCancelation(iterationCancelationSource.Token).GetAsyncEnumerator();
                for (int i = 0; i < args.Length - 2; ++i)
                {
                    var (completeType, _, completeIndex) = args[i];
                    tryCompleters[completeIndex].Invoke();
                    Assert.True(await asyncEnumerator.MoveNextAsync());
                    var resultContainer = asyncEnumerator.Current;
                    Assert.AreEqual((Promise.State) completeType, resultContainer.State);
                    if (resultContainer.State == Promise.State.Rejected)
                    {
                        Assert.AreEqual(rejections[i], resultContainer.Reason);
                    }
                }
                groupCancelationSource.Cancel();
                {
                    var (completeType, _, completeIndex) = args[args.Length - 2];
                    tryCompleters[completeIndex].Invoke();
                    Assert.True(await asyncEnumerator.MoveNextAsync());
                    Assert.AreEqual(Promise.State.Canceled, asyncEnumerator.Current.State);
                }
                iterationCancelationSource.Cancel();
                await TestHelper.AssertCanceledAsync(() => asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
                groupCancelationSource.Dispose();
                iterationCancelationSource.Dispose();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void PromiseEachGroup1CancelationSourceUsedTwice_void()
        {
            Promise.Run(async () =>
            {
                var args = new (CompleteType completeType, bool isAlreadyComplete, int completeIndex)[]
                {
                    (CompleteType.Resolve, false, 1),
                    (CompleteType.Resolve, true, 0),
                    (CompleteType.Resolve, false, 3),
                    (CompleteType.Resolve, false, 2),
                };
                var rejections = new Exception[args.Length];
                var tryCompleters = new Action[args.Length];

                var cancelationSource = CancelationSource.New();
                var eachGroup = PromiseEachGroup.New(cancelationSource.Token, out var groupCancelationToken);
                for (int i = 0; i < args.Length; ++i)
                {
                    var (completeType, isAlreadyComplete, completeIndex) = args[i];
                    rejections[completeIndex] = new Exception($"Rejected completeIndex: {completeIndex}");
                    var promise = TestHelper.BuildPromise(completeType, isAlreadyComplete, rejections[completeIndex], groupCancelationToken, out tryCompleters[completeIndex]);
                    eachGroup = eachGroup.Add(promise);
                }

                args = args.OrderBy(x => x.completeIndex).ToArray();
                var asyncEnumerator = eachGroup.GetAsyncEnumerable().WithCancelation(cancelationSource.Token).GetAsyncEnumerator();
                for (int i = 0; i < args.Length / 2; ++i)
                {
                    var (completeType, _, completeIndex) = args[i];
                    tryCompleters[completeIndex].Invoke();
                    Assert.True(await asyncEnumerator.MoveNextAsync());
                    var resultContainer = asyncEnumerator.Current;
                    Assert.AreEqual((Promise.State) completeType, resultContainer.State);
                    if (resultContainer.State == Promise.State.Rejected)
                    {
                        Assert.AreEqual(rejections[i], resultContainer.Reason);
                    }
                }
                cancelationSource.Cancel();
                await TestHelper.AssertCanceledAsync(() => asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
                cancelationSource.Dispose();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test, TestCaseSource(nameof(GetArgs))]
        public void PromiseEachGroupResultsAreYieldedInCompletionOrder_T((CompleteType completeType, bool isAlreadyComplete, int completeIndex)[] args)
        {
            Promise.Run(async () =>
            {
                var rejections = new Exception[args.Length];
                var tryCompleters = new Action[args.Length];
                
                var eachGroup = PromiseEachGroup<int>.New(out _);
                for (int i = 0; i < args.Length; ++i)
                {
                    var (completeType, isAlreadyComplete, completeIndex) = args[i];
                    rejections[completeIndex] = new Exception($"Rejected completeIndex: {completeIndex}");
                    var promise = TestHelper.BuildPromise(completeType, isAlreadyComplete, completeIndex, rejections[completeIndex], out tryCompleters[completeIndex]);
                    eachGroup = eachGroup.Add(promise);
                }

                var asyncEnumerator = eachGroup.GetAsyncEnumerable().GetAsyncEnumerator();
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
        public void PromiseEachGroupResultsAreYieldedInCompletionOrder_MoveNextAsyncBeforeComplete_T()
        {
            Promise.Run(async () =>
            {
                var args = new (CompleteType completeType, bool isAlreadyComplete, int completeIndex)[]
                {
                    (CompleteType.Resolve, false, 1),
                    (CompleteType.Resolve, false, 0),
                    (CompleteType.Resolve, false, 3),
                    (CompleteType.Resolve, false, 2),
                };
                var rejections = new Exception[args.Length];
                var tryCompleters = new Action[args.Length];
                
                var eachGroup = PromiseEachGroup<int>.New(out _);
                for (int i = 0; i < args.Length; ++i)
                {
                    var (completeType, isAlreadyComplete, completeIndex) = args[i];
                    rejections[completeIndex] = new Exception($"Rejected completeIndex: {completeIndex}");
                    var promise = TestHelper.BuildPromise(completeType, isAlreadyComplete, completeIndex, rejections[completeIndex], out tryCompleters[completeIndex]);
                    eachGroup = eachGroup.Add(promise);
                }

                var asyncEnumerator = eachGroup.GetAsyncEnumerable().GetAsyncEnumerator();
                foreach (var (completeType, _, completeIndex) in args.OrderBy(x => x.completeIndex))
                {
                    bool movedNext = false;
                    var moveNextPromise = asyncEnumerator.MoveNextAsync()
                        .Finally(() => movedNext = true);
                    Assert.False(movedNext);
                    tryCompleters[completeIndex].Invoke();
                    Assert.True(await moveNextPromise);
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
        public void PromiseEachGroupDisposeEarly_T(
            [Values] bool suppressUnobservedRejections)
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

            bool runComplete = false;
            var runPromise = Promise.Run(async () =>
            {
                var eachGroup = PromiseEachGroup<int>.New(out _, suppressUnobservedRejections);
                for (int i = 0; i < args.Length; ++i)
                {
                    var (completeType, isAlreadyComplete, completeIndex) = args[i];
                    rejections[completeIndex] = new Exception($"Rejected completeIndex: {completeIndex}");
                    var promise = TestHelper.BuildPromise(completeType, isAlreadyComplete, completeIndex, rejections[completeIndex], out tryCompleters[completeIndex]);
                    eachGroup = eachGroup.Add(promise);
                }

                args = args.OrderBy(x => x.completeIndex).ToArray();

                var asyncEnumerator = eachGroup.GetAsyncEnumerable().GetAsyncEnumerator();
                for (int i = 0; i < args.Length / 2; ++i)
                {
                    var (completeType, _, completeIndex) = args[i];
                    tryCompleters[completeIndex].Invoke();
                    Assert.True(await asyncEnumerator.MoveNextAsync());
                    var resultContainer = asyncEnumerator.Current;
                    Assert.AreEqual((Promise.State) completeType, resultContainer.State);
                    if (resultContainer.State == Promise.State.Resolved)
                    {
                        Assert.AreEqual(i, resultContainer.Value);
                    }
                    else if (resultContainer.State == Promise.State.Rejected)
                    {
                        Assert.AreEqual(rejections[i], resultContainer.Reason);
                    }
                }

                bool didThrow = false;
                try
                {
                    await asyncEnumerator.DisposeAsync();
                }
                catch (AggregateException e)
                {
                    didThrow = true;
                    Assert.AreEqual(1, e.InnerExceptions.Count);
                    Assert.AreEqual(rejections[2], e.InnerExceptions[0]);
                }
                Assert.AreNotEqual(suppressUnobservedRejections, didThrow);
            }, SynchronizationOption.Synchronous)
                .Finally(() => runComplete = true);

            Assert.False(runComplete);
            for (int i = args.Length / 2; i < args.Length; ++i)
            {
                tryCompleters[args[i].completeIndex].Invoke();
            }
            Assert.True(runComplete);

            runPromise.WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void PromiseEachGroupDisposeWithoutMoveNext_T(
            [Values] bool suppressUnobservedRejections)
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

            bool runComplete = false;
            var runPromise = Promise.Run(async () =>
            {
                var eachGroup = PromiseEachGroup<int>.New(out _, suppressUnobservedRejections);
                for (int i = 0; i < args.Length; ++i)
                {
                    var (completeType, isAlreadyComplete, completeIndex) = args[i];
                    rejections[completeIndex] = new Exception($"Rejected completeIndex: {completeIndex}");
                    var promise = TestHelper.BuildPromise(completeType, isAlreadyComplete, completeIndex, rejections[completeIndex], out tryCompleters[completeIndex]);
                    eachGroup = eachGroup.Add(promise);
                }

                bool didThrow = false;
                try
                {
                    await eachGroup.GetAsyncEnumerable().GetAsyncEnumerator().DisposeAsync();
                }
                catch (AggregateException e)
                {
                    didThrow = true;
                    Assert.AreEqual(1, e.InnerExceptions.Count);
                    Assert.AreEqual(rejections[2], e.InnerExceptions[0]);
                }
                Assert.AreNotEqual(suppressUnobservedRejections, didThrow);
            }, SynchronizationOption.Synchronous)
                .Finally(() => runComplete = true);

            Assert.False(runComplete);
            foreach (var (_, _, completeIndex) in args.OrderBy(x => x.completeIndex))
            {
                tryCompleters[completeIndex].Invoke();
            }
            Assert.True(runComplete);

            runPromise.WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void PromiseEachGroupCancelIterationEarly_T(
            [Values] bool disposeCancelationSourceEarly,
            [Values] bool suppressUnobservedRejections)
        {
            var cancelationSource = CancelationSource.New();
            var args = new (CompleteType completeType, bool isAlreadyComplete, int completeIndex)[]
            {
                (CompleteType.Resolve, false, 1),
                (CompleteType.Resolve, true, 0),
                (CompleteType.Cancel, false, 3),
                (CompleteType.Reject, false, 2),
            };
            var rejections = new Exception[args.Length];
            var tryCompleters = new Action[args.Length];

            bool runComplete = false;
            var runPromise = Promise.Run(async () =>
            {
                var eachGroup = PromiseEachGroup<int>.New(out _, suppressUnobservedRejections);
                for (int i = 0; i < args.Length; ++i)
                {
                    var (completeType, isAlreadyComplete, completeIndex) = args[i];
                    rejections[completeIndex] = new Exception($"Rejected completeIndex: {completeIndex}");
                    var promise = TestHelper.BuildPromise(completeType, isAlreadyComplete, completeIndex, rejections[completeIndex], out tryCompleters[completeIndex]);
                    eachGroup = eachGroup.Add(promise);
                }

                args = args.OrderBy(x => x.completeIndex).ToArray();

                var asyncEnumerator = eachGroup.GetAsyncEnumerable().WithCancelation(cancelationSource.Token).GetAsyncEnumerator();
                for (int i = 0; i < args.Length / 2; ++i)
                {
                    var (completeType, _, completeIndex) = args[i];
                    tryCompleters[completeIndex].Invoke();
                    Assert.True(await asyncEnumerator.MoveNextAsync());
                    var resultContainer = asyncEnumerator.Current;
                    Assert.AreEqual((Promise.State) completeType, resultContainer.State);
                    if (resultContainer.State == Promise.State.Resolved)
                    {
                        Assert.AreEqual(i, resultContainer.Value);
                    }
                    else if (resultContainer.State == Promise.State.Rejected)
                    {
                        Assert.AreEqual(rejections[i], resultContainer.Reason);
                    }
                }
                cancelationSource.Cancel();
                if (disposeCancelationSourceEarly)
                {
                    cancelationSource.Dispose();
                }
                await TestHelper.AssertCanceledAsync(() => asyncEnumerator.MoveNextAsync());

                bool didThrow = false;
                try
                {
                    await asyncEnumerator.DisposeAsync();
                }
                catch (AggregateException e)
                {
                    didThrow = true;
                    Assert.AreEqual(1, e.InnerExceptions.Count);
                    Assert.AreEqual(rejections[2], e.InnerExceptions[0]);
                }
                Assert.AreNotEqual(suppressUnobservedRejections, didThrow);
            }, SynchronizationOption.Synchronous)
                .Finally(() => runComplete = true);

            Assert.False(runComplete);
            for (int i = args.Length / 2; i < args.Length; ++i)
            {
                tryCompleters[args[i].completeIndex].Invoke();
            }
            Assert.True(runComplete);
            cancelationSource.TryDispose();

            runPromise.WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void PromiseEachGroupIterationCancelationSourceDisposedEarly_T()
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
                
                var eachGroup = PromiseEachGroup<int>.New(out _);
                for (int i = 0; i < args.Length; ++i)
                {
                    var (completeType, isAlreadyComplete, completeIndex) = args[i];
                    rejections[completeIndex] = new Exception($"Rejected completeIndex: {completeIndex}");
                    var promise = TestHelper.BuildPromise(completeType, isAlreadyComplete, completeIndex, rejections[completeIndex], out tryCompleters[completeIndex]);
                    eachGroup = eachGroup.Add(promise);
                }

                args = args.OrderBy(x => x.completeIndex).ToArray();

                var cancelationSource = CancelationSource.New();
                var asyncEnumerator = eachGroup.GetAsyncEnumerable().WithCancelation(cancelationSource.Token).GetAsyncEnumerator();
                for (int i = 0; i < args.Length / 2; ++i)
                {
                    var (completeType, _, completeIndex) = args[i];
                    tryCompleters[completeIndex].Invoke();
                    Assert.True(await asyncEnumerator.MoveNextAsync());
                    var resultContainer = asyncEnumerator.Current;
                    Assert.AreEqual((Promise.State) completeType, resultContainer.State);
                    if (resultContainer.State == Promise.State.Resolved)
                    {
                        Assert.AreEqual(i, resultContainer.Value);
                    }
                    else if (resultContainer.State == Promise.State.Rejected)
                    {
                        Assert.AreEqual(rejections[i], resultContainer.Reason);
                    }
                }
                cancelationSource.Dispose();

                for (int i = args.Length / 2; i < args.Length; ++i)
                {
                    var (completeType, _, completeIndex) = args[i];
                    tryCompleters[completeIndex].Invoke();
                    Assert.True(await asyncEnumerator.MoveNextAsync());
                    var resultContainer = asyncEnumerator.Current;
                    Assert.AreEqual((Promise.State) completeType, resultContainer.State);
                    if (resultContainer.State == Promise.State.Resolved)
                    {
                        Assert.AreEqual(i, resultContainer.Value);
                    }
                    else if (resultContainer.State == Promise.State.Rejected)
                    {
                        Assert.AreEqual(rejections[i], resultContainer.Reason);
                    }
                }
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void PromiseEachGroupIterationCanceledAfterAllCompleteAndBeforeMoveNextAsync_T()
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
                
                var eachGroup = PromiseEachGroup<int>.New(out _);
                for (int i = 0; i < args.Length; ++i)
                {
                    var (completeType, isAlreadyComplete, completeIndex) = args[i];
                    rejections[completeIndex] = new Exception($"Rejected completeIndex: {completeIndex}");
                    var promise = TestHelper.BuildPromise(completeType, isAlreadyComplete, completeIndex, rejections[completeIndex], out tryCompleters[completeIndex]);
                    eachGroup = eachGroup.Add(promise);
                }

                args = args.OrderBy(x => x.completeIndex).ToArray();

                var cancelationSource = CancelationSource.New();
                var asyncEnumerator = eachGroup.GetAsyncEnumerable().WithCancelation(cancelationSource.Token).GetAsyncEnumerator();
                for (int i = 0; i < args.Length; ++i)
                {
                    var (completeType, _, completeIndex) = args[i];
                    tryCompleters[completeIndex].Invoke();
                    Assert.True(await asyncEnumerator.MoveNextAsync());
                    var resultContainer = asyncEnumerator.Current;
                    Assert.AreEqual((Promise.State) completeType, resultContainer.State);
                    if (resultContainer.State == Promise.State.Resolved)
                    {
                        Assert.AreEqual(i, resultContainer.Value);
                    }
                    else if (resultContainer.State == Promise.State.Rejected)
                    {
                        Assert.AreEqual(rejections[i], resultContainer.Reason);
                    }
                }
                cancelationSource.Cancel();
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
                cancelationSource.TryDispose();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void PromiseEachGroupCancelGroupEarly_T(
            [Values] bool disposeCancelationSourceEarly)
        {
            Promise.Run(async () =>
            {
                var args = new (CompleteType completeType, bool isAlreadyComplete, int completeIndex)[]
                {
                    (CompleteType.Resolve, false, 1),
                    (CompleteType.Resolve, true, 0),
                    (CompleteType.Resolve, false, 3),
                    (CompleteType.Reject, false, 2),
                };
                var rejections = new Exception[args.Length];
                var tryCompleters = new Action[args.Length];

                var cancelationSource = CancelationSource.New();
                var eachGroup = PromiseEachGroup<int>.New(cancelationSource.Token, out var groupCancelationToken);
                for (int i = 0; i < args.Length; ++i)
                {
                    var (completeType, isAlreadyComplete, completeIndex) = args[i];
                    rejections[completeIndex] = new Exception($"Rejected completeIndex: {completeIndex}");
                    var promise = TestHelper.BuildPromise(completeType, isAlreadyComplete, completeIndex, rejections[completeIndex], groupCancelationToken, out tryCompleters[completeIndex]);
                    eachGroup = eachGroup.Add(promise);
                }

                args = args.OrderBy(x => x.completeIndex).ToArray();
                var asyncEnumerator = eachGroup.GetAsyncEnumerable().GetAsyncEnumerator();
                for (int i = 0; i < args.Length / 2; ++i)
                {
                    var (completeType, _, completeIndex) = args[i];
                    tryCompleters[completeIndex].Invoke();
                    Assert.True(await asyncEnumerator.MoveNextAsync());
                    var resultContainer = asyncEnumerator.Current;
                    Assert.AreEqual((Promise.State) completeType, resultContainer.State);
                    if (resultContainer.State == Promise.State.Resolved)
                    {
                        Assert.AreEqual(i, resultContainer.Value);
                    }
                    else if (resultContainer.State == Promise.State.Rejected)
                    {
                        Assert.AreEqual(rejections[i], resultContainer.Reason);
                    }
                }
                cancelationSource.Cancel();
                if (disposeCancelationSourceEarly)
                {
                    cancelationSource.Dispose();
                }

                for (int i = args.Length / 2; i < args.Length; ++i)
                {
                    tryCompleters[args[i].completeIndex].Invoke();
                    Assert.True(await asyncEnumerator.MoveNextAsync());
                    Assert.AreEqual(Promise.State.Canceled, asyncEnumerator.Current.State);
                }
                await asyncEnumerator.DisposeAsync();
                cancelationSource.TryDispose();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void PromiseEachGroupGroupCancelationSourceDisposedEarly_T()
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

                var cancelationSource = CancelationSource.New();
                var eachGroup = PromiseEachGroup<int>.New(cancelationSource.Token, out var groupCancelationToken);
                for (int i = 0; i < args.Length; ++i)
                {
                    var (completeType, isAlreadyComplete, completeIndex) = args[i];
                    rejections[completeIndex] = new Exception($"Rejected completeIndex: {completeIndex}");
                    var promise = TestHelper.BuildPromise(completeType, isAlreadyComplete, completeIndex, rejections[completeIndex], groupCancelationToken, out tryCompleters[completeIndex]);
                    eachGroup = eachGroup.Add(promise);
                }

                args = args.OrderBy(x => x.completeIndex).ToArray();
                var asyncEnumerator = eachGroup.GetAsyncEnumerable().WithCancelation(cancelationSource.Token).GetAsyncEnumerator();
                for (int i = 0; i < args.Length / 2; ++i)
                {
                    var (completeType, _, completeIndex) = args[i];
                    tryCompleters[completeIndex].Invoke();
                    Assert.True(await asyncEnumerator.MoveNextAsync());
                    var resultContainer = asyncEnumerator.Current;
                    Assert.AreEqual((Promise.State) completeType, resultContainer.State);
                    if (resultContainer.State == Promise.State.Resolved)
                    {
                        Assert.AreEqual(i, resultContainer.Value);
                    }
                    else if (resultContainer.State == Promise.State.Rejected)
                    {
                        Assert.AreEqual(rejections[i], resultContainer.Reason);
                    }
                }
                cancelationSource.Dispose();

                for (int i = args.Length / 2; i < args.Length; ++i)
                {
                    var (completeType, _, completeIndex) = args[i];
                    tryCompleters[completeIndex].Invoke();
                    Assert.True(await asyncEnumerator.MoveNextAsync());
                    var resultContainer = asyncEnumerator.Current;
                    Assert.AreEqual((Promise.State) completeType, resultContainer.State);
                    if (resultContainer.State == Promise.State.Resolved)
                    {
                        Assert.AreEqual(i, resultContainer.Value);
                    }
                    else if (resultContainer.State == Promise.State.Rejected)
                    {
                        Assert.AreEqual(rejections[i], resultContainer.Reason);
                    }
                }
                await asyncEnumerator.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void PromiseEachGroupGroupCanceledAfterAllCompleteAndBeforeMoveNextAsync_T()
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

                var cancelationSource = CancelationSource.New();
                var eachGroup = PromiseEachGroup<int>.New(cancelationSource.Token, out var groupCancelationToken);
                for (int i = 0; i < args.Length; ++i)
                {
                    var (completeType, isAlreadyComplete, completeIndex) = args[i];
                    rejections[completeIndex] = new Exception($"Rejected completeIndex: {completeIndex}");
                    var promise = TestHelper.BuildPromise(completeType, isAlreadyComplete, completeIndex, rejections[completeIndex], groupCancelationToken, out tryCompleters[completeIndex]);
                    eachGroup = eachGroup.Add(promise);
                }

                args = args.OrderBy(x => x.completeIndex).ToArray();
                var asyncEnumerator = eachGroup.GetAsyncEnumerable().WithCancelation(cancelationSource.Token).GetAsyncEnumerator();
                for (int i = 0; i < args.Length; ++i)
                {
                    var (completeType, _, completeIndex) = args[i];
                    tryCompleters[completeIndex].Invoke();
                    Assert.True(await asyncEnumerator.MoveNextAsync());
                    var resultContainer = asyncEnumerator.Current;
                    Assert.AreEqual((Promise.State) completeType, resultContainer.State);
                    if (resultContainer.State == Promise.State.Resolved)
                    {
                        Assert.AreEqual(i, resultContainer.Value);
                    }
                    else if (resultContainer.State == Promise.State.Rejected)
                    {
                        Assert.AreEqual(rejections[i], resultContainer.Reason);
                    }
                }
                cancelationSource.Cancel();
                Assert.False(await asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
                cancelationSource.TryDispose();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void PromiseEachGroup2CancelationSources_T()
        {
            Promise.Run(async () =>
            {
                var args = new (CompleteType completeType, bool isAlreadyComplete, int completeIndex)[]
                {
                    (CompleteType.Resolve, false, 1),
                    (CompleteType.Resolve, true, 0),
                    (CompleteType.Resolve, false, 3),
                    (CompleteType.Resolve, false, 2),
                };
                var rejections = new Exception[args.Length];
                var tryCompleters = new Action[args.Length];

                var groupCancelationSource = CancelationSource.New();
                var iterationCancelationSource = CancelationSource.New();
                var eachGroup = PromiseEachGroup<int>.New(groupCancelationSource.Token, out var groupCancelationToken);
                for (int i = 0; i < args.Length; ++i)
                {
                    var (completeType, isAlreadyComplete, completeIndex) = args[i];
                    rejections[completeIndex] = new Exception($"Rejected completeIndex: {completeIndex}");
                    var promise = TestHelper.BuildPromise(completeType, isAlreadyComplete, completeIndex, rejections[completeIndex], groupCancelationToken, out tryCompleters[completeIndex]);
                    eachGroup = eachGroup.Add(promise);
                }

                args = args.OrderBy(x => x.completeIndex).ToArray();
                var asyncEnumerator = eachGroup.GetAsyncEnumerable().WithCancelation(iterationCancelationSource.Token).GetAsyncEnumerator();
                for (int i = 0; i < args.Length - 2; ++i)
                {
                    var (completeType, _, completeIndex) = args[i];
                    tryCompleters[completeIndex].Invoke();
                    Assert.True(await asyncEnumerator.MoveNextAsync());
                    var resultContainer = asyncEnumerator.Current;
                    Assert.AreEqual((Promise.State) completeType, resultContainer.State);
                    if (resultContainer.State == Promise.State.Resolved)
                    {
                        Assert.AreEqual(i, resultContainer.Value);
                    }
                    else if (resultContainer.State == Promise.State.Rejected)
                    {
                        Assert.AreEqual(rejections[i], resultContainer.Reason);
                    }
                }
                groupCancelationSource.Cancel();
                {
                    var (completeType, _, completeIndex) = args[args.Length - 2];
                    tryCompleters[completeIndex].Invoke();
                    Assert.True(await asyncEnumerator.MoveNextAsync());
                    Assert.AreEqual(Promise.State.Canceled, asyncEnumerator.Current.State);
                }
                iterationCancelationSource.Cancel();
                await TestHelper.AssertCanceledAsync(() => asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
                groupCancelationSource.Dispose();
                iterationCancelationSource.Dispose();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void PromiseEachGroup1CancelationSourceUsedTwice_T()
        {
            Promise.Run(async () =>
            {
                var args = new (CompleteType completeType, bool isAlreadyComplete, int completeIndex)[]
                {
                    (CompleteType.Resolve, false, 1),
                    (CompleteType.Resolve, true, 0),
                    (CompleteType.Resolve, false, 3),
                    (CompleteType.Resolve, false, 2),
                };
                var rejections = new Exception[args.Length];
                var tryCompleters = new Action[args.Length];

                var cancelationSource = CancelationSource.New();
                var eachGroup = PromiseEachGroup<int>.New(cancelationSource.Token, out var groupCancelationToken);
                for (int i = 0; i < args.Length; ++i)
                {
                    var (completeType, isAlreadyComplete, completeIndex) = args[i];
                    rejections[completeIndex] = new Exception($"Rejected completeIndex: {completeIndex}");
                    var promise = TestHelper.BuildPromise(completeType, isAlreadyComplete, completeIndex, rejections[completeIndex], groupCancelationToken, out tryCompleters[completeIndex]);
                    eachGroup = eachGroup.Add(promise);
                }

                args = args.OrderBy(x => x.completeIndex).ToArray();
                var asyncEnumerator = eachGroup.GetAsyncEnumerable().WithCancelation(cancelationSource.Token).GetAsyncEnumerator();
                for (int i = 0; i < args.Length / 2; ++i)
                {
                    var (completeType, _, completeIndex) = args[i];
                    tryCompleters[completeIndex].Invoke();
                    Assert.True(await asyncEnumerator.MoveNextAsync());
                    var resultContainer = asyncEnumerator.Current;
                    Assert.AreEqual((Promise.State) completeType, resultContainer.State);
                    if (resultContainer.State == Promise.State.Resolved)
                    {
                        Assert.AreEqual(i, resultContainer.Value);
                    }
                    else if (resultContainer.State == Promise.State.Rejected)
                    {
                        Assert.AreEqual(rejections[i], resultContainer.Reason);
                    }
                }
                cancelationSource.Cancel();
                await TestHelper.AssertCanceledAsync(() => asyncEnumerator.MoveNextAsync());
                await asyncEnumerator.DisposeAsync();
                cancelationSource.Dispose();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void PromiseEachGroup_CancelationCallbackExceptionsArePropagated_void(
            [Values(CancelationType.None, CancelationType.Deferred)] CancelationType iterationCancelationType,
            [Values] CompleteType completeType1,
            [Values] bool alreadyComplete1,
            [Values] CompleteType completeType2,
            [Values] bool suppressUnobservedRejection)
        {
            Promise.Run(async () =>
            {
                var groupCancelationSource = CancelationSource.New();
                var iterationCancelationSource = CancelationSource.New();

                // We don't test a group cancelation source because it propagates the exceptions directly when it's canceled.
                // We need to only test when the each group itself triggers cancelation (via the iteration cancelation source, or via DisposeAsync early).
                var eachGroup = PromiseEachGroup.New(out var groupCancelationToken, suppressUnobservedRejection);

                var cancelationException = new Exception("Error in cancelation!");
                groupCancelationToken.Register(() => { throw cancelationException; });

                var promiseException = new System.InvalidOperationException("Bang!");

                var asyncEnumerator = eachGroup
                    .Add(TestHelper.BuildPromise(completeType1, alreadyComplete1, promiseException, out var tryCompleter1))
                    .Add(TestHelper.BuildPromise(completeType2, false, promiseException, out var tryCompleter2))
                    .GetAsyncEnumerable()
                    .WithCancelation(iterationCancelationType == CancelationType.None ? CancelationToken.None : iterationCancelationSource.Token)
                    .GetAsyncEnumerator();

                tryCompleter1();
                Assert.True(await asyncEnumerator.MoveNextAsync());
                var resultContainer = asyncEnumerator.Current;
                Assert.AreEqual((Promise.State) completeType1, resultContainer.State);
                if (resultContainer.State == Promise.State.Rejected)
                {
                    Assert.AreEqual(promiseException, resultContainer.Reason);
                }

                groupCancelationSource.Cancel();
                iterationCancelationSource.Cancel();

                tryCompleter2();
                if (iterationCancelationType == CancelationType.Deferred)
                {
                    await TestHelper.AssertCanceledAsync(() => asyncEnumerator.MoveNextAsync());
                }

                bool didThrow = false;
                try
                {
                    await asyncEnumerator.DisposeAsync();
                }
                catch (AggregateException e)
                {
                    didThrow = true;

                    int expectedExceptionCount = 1;
                    if (completeType2 == CompleteType.Reject && !suppressUnobservedRejection)
                    {
                        ++expectedExceptionCount;
                    }
                    Assert.AreEqual(expectedExceptionCount, e.InnerExceptions.Count);
                    Assert.IsInstanceOf<AggregateException>(e.InnerExceptions[0]);
                    if (expectedExceptionCount > 1)
                    {
                        Assert.IsInstanceOf<System.InvalidOperationException>(e.InnerExceptions[1]);
                    }
                    Assert.AreEqual(1, e.InnerExceptions[0].UnsafeAs<AggregateException>().InnerExceptions.Count);
                    Assert.AreEqual(cancelationException, e.InnerExceptions[0].UnsafeAs<AggregateException>().InnerExceptions[0]);
                }

                Assert.True(didThrow);

                groupCancelationSource.Dispose();
                iterationCancelationSource.Dispose();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void PromiseEachGroup_CancelationCallbackExceptionsArePropagated_T(
            [Values(CancelationType.None, CancelationType.Deferred)] CancelationType iterationCancelationType,
            [Values] CompleteType completeType1,
            [Values] bool alreadyComplete1,
            [Values] CompleteType completeType2,
            [Values] bool suppressUnobservedRejection)
        {
            Promise.Run(async () =>
            {
                var groupCancelationSource = CancelationSource.New();
                var iterationCancelationSource = CancelationSource.New();

                // We don't test a group cancelation source because it propagates the exceptions directly when it's canceled.
                // We need to only test when the each group itself triggers cancelation (via the iteration cancelation source, or via DisposeAsync early).
                var eachGroup = PromiseEachGroup<int>.New(out var groupCancelationToken, suppressUnobservedRejection);

                var cancelationException = new Exception("Error in cancelation!");
                groupCancelationToken.Register(() => { throw cancelationException; });

                var promiseException = new System.InvalidOperationException("Bang!");

                var asyncEnumerator = eachGroup
                    .Add(TestHelper.BuildPromise(completeType1, alreadyComplete1, 1, promiseException, out var tryCompleter1))
                    .Add(TestHelper.BuildPromise(completeType2, false, 2, promiseException, out var tryCompleter2))
                    .GetAsyncEnumerable()
                    .WithCancelation(iterationCancelationType == CancelationType.None ? CancelationToken.None : iterationCancelationSource.Token)
                    .GetAsyncEnumerator();

                tryCompleter1();
                Assert.True(await asyncEnumerator.MoveNextAsync());
                var resultContainer = asyncEnumerator.Current;
                Assert.AreEqual((Promise.State) completeType1, resultContainer.State);
                if (resultContainer.State == Promise.State.Resolved)
                {
                    Assert.AreEqual(1, resultContainer.Value);
                }
                else if (resultContainer.State == Promise.State.Rejected)
                {
                    Assert.AreEqual(promiseException, resultContainer.Reason);
                }

                groupCancelationSource.Cancel();
                iterationCancelationSource.Cancel();

                tryCompleter2();
                if (iterationCancelationType == CancelationType.Deferred)
                {
                    await TestHelper.AssertCanceledAsync(() => asyncEnumerator.MoveNextAsync());
                }

                bool didThrow = false;
                try
                {
                    await asyncEnumerator.DisposeAsync();
                }
                catch (AggregateException e)
                {
                    didThrow = true;

                    int expectedExceptionCount = 1;
                    if (completeType2 == CompleteType.Reject && !suppressUnobservedRejection)
                    {
                        ++expectedExceptionCount;
                    }
                    Assert.AreEqual(expectedExceptionCount, e.InnerExceptions.Count);
                    Assert.IsInstanceOf<AggregateException>(e.InnerExceptions[0]);
                    if (expectedExceptionCount > 1)
                    {
                        Assert.IsInstanceOf<System.InvalidOperationException>(e.InnerExceptions[1]);
                    }
                    Assert.AreEqual(1, e.InnerExceptions[0].UnsafeAs<AggregateException>().InnerExceptions.Count);
                    Assert.AreEqual(cancelationException, e.InnerExceptions[0].UnsafeAs<AggregateException>().InnerExceptions[0]);
                }

                Assert.True(didThrow);

                groupCancelationSource.Dispose();
                iterationCancelationSource.Dispose();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }
    }
}