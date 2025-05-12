#if !UNITY_WEBGL

#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using NUnit.Framework;
using Proto.Promises;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ProtoPromise.Tests.Concurrency
{
    public class EachConcurrencyTests
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

        [Test] // Only generate up to 2 CompleteTypes (more takes too long to test)
        public void DeferredsMayBeCompletedWhileTheirPromisesArePassedToEachAndConsumedConcurrently_void(
            [Values] CombineType combineType,
            [Values] CompleteType completeType0,
            [Values] CompleteType completeType1,
            [Values(CompleteType.Resolve)] CompleteType completeType2,
            [Values(CompleteType.Resolve)] CompleteType completeType3)
        {
            const string rejectValue = "Reject";

            var completer0 = TestHelper.GetCompleterVoid(completeType0, rejectValue);
            var completer1 = TestHelper.GetCompleterVoid(completeType1, rejectValue);
            var completer2 = TestHelper.GetCompleterVoid(completeType2, rejectValue);
            var completer3 = TestHelper.GetCompleterVoid(completeType3, rejectValue);

            var deferred0 = default(Promise.Deferred);
            var deferred1 = default(Promise.Deferred);
            var deferred2 = default(Promise.Deferred);
            var deferred3 = default(Promise.Deferred);

            List<Action> parallelActions = new List<Action>()
            {
                () => completer0(deferred0),
                () => completer1(deferred1),
                () => completer2(deferred2),
                () => completer3(deferred3)
            };

            var helper = ParallelCombineTestHelper.Create(
                combineType,
                async () =>
                {
                    var asyncEnumerator = Promise.Each(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise)
                        .GetAsyncEnumerator();

                    for (int i = 0; i < 4; ++i)
                    {
                        Assert.True(await asyncEnumerator.MoveNextAsync());
                        var result = asyncEnumerator.Current;
                        // We can't assert the state, because the promises are completed concurrently, so their completion order is indeterminate.
                        if (result.State == Promise.State.Rejected)
                        {
                            Assert.AreEqual(rejectValue, result.Reason);
                        }
                    }
                    Assert.False(await asyncEnumerator.MoveNextAsync());
                    await asyncEnumerator.DisposeAsync();
                }
            );
            helper.MaybeAddParallelAction(parallelActions);

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsMaybeWithOffsets(
                // setup
                () =>
                {
                    deferred0 = Promise.NewDeferred();
                    deferred1 = Promise.NewDeferred();
                    deferred2 = Promise.NewDeferred();
                    deferred3 = Promise.NewDeferred();
                    helper.Setup();
                },
                // teardown
                () =>
                {
                    helper.Teardown();
                    Assert.IsTrue(helper.Success);
                },
                parallelActions
            );
        }

        [Test] // Only generate up to 2 CompleteTypes (more takes too long to test)
        public void DeferredsMayBeCompletedWhileTheirPromisesArePassedToEachAndConsumedConcurrently_WithCancelation_void(
            [Values] CombineType combineType,
            [Values] CompleteType completeType0,
            [Values] CompleteType completeType1,
            [Values(CompleteType.Resolve)] CompleteType completeType2,
            [Values(CompleteType.Resolve)] CompleteType completeType3)
        {
            const string rejectValue = "Reject";

            var completer0 = TestHelper.GetCompleterVoid(completeType0, rejectValue);
            var completer1 = TestHelper.GetCompleterVoid(completeType1, rejectValue);
            var completer2 = TestHelper.GetCompleterVoid(completeType2, rejectValue);
            var completer3 = TestHelper.GetCompleterVoid(completeType3, rejectValue);

            var deferred0 = default(Promise.Deferred);
            var deferred1 = default(Promise.Deferred);
            var deferred2 = default(Promise.Deferred);
            var deferred3 = default(Promise.Deferred);
            var cancelationSource = default(CancelationSource);

            List<Action> parallelActions = new List<Action>()
            {
                () => completer0(deferred0),
                () => completer1(deferred1),
                () => completer2(deferred2),
                () => completer3(deferred3),
                () => cancelationSource.Cancel()
            };

            var helper = ParallelCombineTestHelper.Create(
                combineType,
                async () =>
                {
                    var asyncEnumerator = Promise.Each(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise)
                        .WithCancelation(cancelationSource.Token)
                        .GetAsyncEnumerator();

                    try
                    {
                        // This is canceled concurrently, so we can't know how many elements we can enumerate before that occurs.
                        while (await asyncEnumerator.MoveNextAsync())
                        {
                            var result = asyncEnumerator.Current;
                            // We can't assert the state, because the promises are completed concurrently, so their completion order is indeterminate.
                            if (result.State == Promise.State.Rejected)
                            {
                                Assert.AreEqual(rejectValue, result.Reason);
                            }
                        }
                    }
                    finally
                    {
                        await asyncEnumerator.DisposeAsync();
                    }
                }
            );
            helper.MaybeAddParallelAction(parallelActions);

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsMaybeWithOffsets(
                // setup
                () =>
                {
                    deferred0 = Promise.NewDeferred();
                    deferred1 = Promise.NewDeferred();
                    deferred2 = Promise.NewDeferred();
                    deferred3 = Promise.NewDeferred();
                    cancelationSource = CancelationSource.New();
                    helper.Setup();
                },
                // teardown
                () =>
                {
                    helper.Teardown();
                    cancelationSource.Dispose();
                    Assert.IsTrue(helper.Success);
                },
                parallelActions
            );
        }

        [Test] // Only generate up to 2 CompleteTypes (more takes too long to test)
        public void DeferredsMayBeCompletedWhileTheirPromisesArePassedToEachAndConsumedConcurrently_T(
            [Values] CombineType combineType,
            [Values] CompleteType completeType0,
            [Values] CompleteType completeType1,
            [Values(CompleteType.Resolve)] CompleteType completeType2,
            [Values(CompleteType.Resolve)] CompleteType completeType3)
        {
            const int resolveValue = 42;
            const string rejectValue = "Reject";

            var completer0 = TestHelper.GetCompleterT(completeType0, resolveValue, rejectValue);
            var completer1 = TestHelper.GetCompleterT(completeType1, resolveValue, rejectValue);
            var completer2 = TestHelper.GetCompleterT(completeType2, resolveValue, rejectValue);
            var completer3 = TestHelper.GetCompleterT(completeType3, resolveValue, rejectValue);

            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
            var deferred2 = default(Promise<int>.Deferred);
            var deferred3 = default(Promise<int>.Deferred);

            List<Action> parallelActions = new List<Action>()
            {
                () => completer0(deferred0),
                () => completer1(deferred1),
                () => completer2(deferred2),
                () => completer3(deferred3)
            };

            var helper = ParallelCombineTestHelper.Create(
                combineType,
                async () =>
                {
                    var asyncEnumerator = Promise.Each(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise)
                        .GetAsyncEnumerator();

                    for (int i = 0; i < 4; ++i)
                    {
                        Assert.True(await asyncEnumerator.MoveNextAsync());
                        var result = asyncEnumerator.Current;
                        // We can't assert the state, because the promises are completed concurrently, so their completion order is indeterminate.
                        if (result.State == Promise.State.Resolved)
                        {
                            Assert.AreEqual(resolveValue, result.Value);
                        }
                        else if (result.State == Promise.State.Rejected)
                        {
                            Assert.AreEqual(rejectValue, result.Reason);
                        }
                    }
                    Assert.False(await asyncEnumerator.MoveNextAsync());
                    await asyncEnumerator.DisposeAsync();
                }
            );
            helper.MaybeAddParallelAction(parallelActions);

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsMaybeWithOffsets(
                // setup
                () =>
                {
                    deferred0 = Promise<int>.NewDeferred();
                    deferred1 = Promise<int>.NewDeferred();
                    deferred2 = Promise<int>.NewDeferred();
                    deferred3 = Promise<int>.NewDeferred();
                    helper.Setup();
                },
                // teardown
                () =>
                {
                    helper.Teardown();
                    Assert.IsTrue(helper.Success);
                },
                parallelActions
            );
        }

        [Test] // Only generate up to 2 CompleteTypes (more takes too long to test)
        public void DeferredsMayBeCompletedWhileTheirPromisesArePassedToEachAndConsumedConcurrently_WithCancelation_T(
            [Values] CombineType combineType,
            [Values] CompleteType completeType0,
            [Values] CompleteType completeType1,
            [Values(CompleteType.Resolve)] CompleteType completeType2,
            [Values(CompleteType.Resolve)] CompleteType completeType3)
        {
            const int resolveValue = 42;
            const string rejectValue = "Reject";

            var completer0 = TestHelper.GetCompleterT(completeType0, resolveValue, rejectValue);
            var completer1 = TestHelper.GetCompleterT(completeType1, resolveValue, rejectValue);
            var completer2 = TestHelper.GetCompleterT(completeType2, resolveValue, rejectValue);
            var completer3 = TestHelper.GetCompleterT(completeType3, resolveValue, rejectValue);

            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
            var deferred2 = default(Promise<int>.Deferred);
            var deferred3 = default(Promise<int>.Deferred);
            var cancelationSource = default(CancelationSource);

            List<Action> parallelActions = new List<Action>()
            {
                () => completer0(deferred0),
                () => completer1(deferred1),
                () => completer2(deferred2),
                () => completer3(deferred3),
                () => cancelationSource.Cancel()
            };

            var helper = ParallelCombineTestHelper.Create(
                combineType,
                async () =>
                {
                    var asyncEnumerator = Promise.Each(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise)
                        .WithCancelation(cancelationSource.Token)
                        .GetAsyncEnumerator();

                    try
                    {
                        // This is canceled concurrently, so we can't know how many elements we can enumerate before that occurs.
                        while (await asyncEnumerator.MoveNextAsync())
                        {
                            var result = asyncEnumerator.Current;
                            // We can't assert the state, because the promises are completed concurrently, so their completion order is indeterminate.
                            if (result.State == Promise.State.Resolved)
                            {
                                Assert.AreEqual(resolveValue, result.Value);
                            }
                            else if (result.State == Promise.State.Rejected)
                            {
                                Assert.AreEqual(rejectValue, result.Reason);
                            }
                        }
                    }
                    finally
                    {
                        await asyncEnumerator.DisposeAsync();
                    }
                }
            );
            helper.MaybeAddParallelAction(parallelActions);

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsMaybeWithOffsets(
                // setup
                () =>
                {
                    deferred0 = Promise<int>.NewDeferred();
                    deferred1 = Promise<int>.NewDeferred();
                    deferred2 = Promise<int>.NewDeferred();
                    deferred3 = Promise<int>.NewDeferred();
                    cancelationSource = CancelationSource.New();
                    helper.Setup();
                },
                // teardown
                () =>
                {
                    helper.Teardown();
                    cancelationSource.Dispose();
                    Assert.IsTrue(helper.Success);
                },
                parallelActions
            );
        }
    }
}

#endif // !UNITY_WEBGL