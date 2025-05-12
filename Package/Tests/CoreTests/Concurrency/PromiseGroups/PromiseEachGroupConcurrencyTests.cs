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

namespace ProtoPromise.Tests.Concurrency.PromiseGroups
{
    public class PromiseEachGroupConcurrencyTests
    {
        [Flags]
        public enum EachCancelationType
        {
            None = 0,
            Group = 1 << 0,
            Iteration = 1 << 1,
            Both = Group | Iteration
        }

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
        public void DeferredsMayBeCompletedWhileTheirPromisesArePassedToPromiseEachGroup_AndCancelationTriggeredConcurrently_void(
            [Values] EachCancelationType cancelationType,
            [Values] CombineType combineType,
            [Values] CompleteType completeType1,
            [Values] bool alreadyComplete1,
            [Values(CompleteType.Resolve)] CompleteType completeType2,
            [Values] bool alreadyComplete2,
            [Values] CompleteType completeType3,
            [Values(false)] bool alreadyComplete3)
        {
            const string rejectValue = "Reject";

            var tryCompleter1 = default(Action);
            var tryCompleter2 = default(Action);
            var tryCompleter3 = default(Action);
            var promise1 = default(Promise);
            var promise2 = default(Promise);
            var promise3 = default(Promise);
            var groupCancelationSource = default(CancelationSource);
            var iterationCancelationSource = default(CancelationSource);
            var iterationCancelationToken = default(CancelationToken);
            var group = default(PromiseEachGroup);

            List<Action> parallelActions = new List<Action>();
            if (!alreadyComplete1)
            {
                parallelActions.Add(() => tryCompleter1());
            }
            if (!alreadyComplete2)
            {
                parallelActions.Add(() => tryCompleter2());
            }
            if (!alreadyComplete3)
            {
                parallelActions.Add(() => tryCompleter3());
            }
            if (cancelationType.HasFlag(EachCancelationType.Group))
            {
                parallelActions.Add(() => groupCancelationSource.Cancel());
            }
            if (cancelationType.HasFlag(EachCancelationType.Iteration))
            {
                parallelActions.Add(() => iterationCancelationSource.Cancel());
            }

            var helper = ParallelCombineTestHelper.Create(
                combineType,
                async () =>
                {
                    var asyncEnumerator = group
                        .Add(promise1)
                        .Add(promise2)
                        .Add(promise3)
                        .GetAsyncEnumerable(suppressUnobservedRejections: true)
                        .WithCancelation(iterationCancelationToken)
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
                    CancelationToken groupCancelationToken;
                    if (cancelationType.HasFlag(EachCancelationType.Group))
                    {
                        groupCancelationSource = CancelationSource.New();
                        group = PromiseEachGroup.New(groupCancelationSource.Token, out groupCancelationToken);
                    }
                    else
                    {
                        group = PromiseEachGroup.New(out groupCancelationToken);
                    }
                    promise1 = TestHelper.BuildPromise(completeType1, alreadyComplete1, rejectValue, groupCancelationToken, out tryCompleter1);
                    promise2 = TestHelper.BuildPromise(completeType2, alreadyComplete2, rejectValue, groupCancelationToken, out tryCompleter2);
                    promise3 = TestHelper.BuildPromise(completeType3, alreadyComplete3, rejectValue, groupCancelationToken, out tryCompleter3);
                    if (cancelationType.HasFlag(EachCancelationType.Iteration))
                    {
                        iterationCancelationSource = CancelationSource.New();
                        iterationCancelationToken = iterationCancelationSource.Token;
                    }
                    helper.Setup();
                },
                // teardown
                () =>
                {
                    helper.Teardown();
                    if (cancelationType.HasFlag(EachCancelationType.Group))
                    {
                        groupCancelationSource.Dispose();
                    }
                    if (cancelationType.HasFlag(EachCancelationType.Iteration))
                    {
                        iterationCancelationSource.Dispose();
                    }
                    Assert.IsTrue(helper.Success);
                },
                parallelActions
            );
        }

        [Test]
        public void DeferredsMayBeCompletedWhileTheirPromisesArePassedToPromiseEachGroup_AndCancelationTriggeredConcurrently_T(
            [Values] EachCancelationType cancelationType,
            [Values] CombineType combineType,
            [Values] CompleteType completeType1,
            [Values] bool alreadyComplete1,
            [Values(CompleteType.Resolve)] CompleteType completeType2,
            [Values] bool alreadyComplete2,
            [Values] CompleteType completeType3,
            [Values(false)] bool alreadyComplete3)
        {
            const string rejectValue = "Reject";

            var tryCompleter1 = default(Action);
            var tryCompleter2 = default(Action);
            var tryCompleter3 = default(Action);
            var promise1 = default(Promise<int>);
            var promise2 = default(Promise<int>);
            var promise3 = default(Promise<int>);
            var groupCancelationSource = default(CancelationSource);
            var iterationCancelationSource = default(CancelationSource);
            var iterationCancelationToken = default(CancelationToken);
            var group = default(PromiseEachGroup<int>);

            List<Action> parallelActions = new List<Action>();
            if (!alreadyComplete1)
            {
                parallelActions.Add(() => tryCompleter1());
            }
            if (!alreadyComplete2)
            {
                parallelActions.Add(() => tryCompleter2());
            }
            if (!alreadyComplete3)
            {
                parallelActions.Add(() => tryCompleter3());
            }
            if (cancelationType.HasFlag(EachCancelationType.Group))
            {
                parallelActions.Add(() => groupCancelationSource.Cancel());
            }
            if (cancelationType.HasFlag(EachCancelationType.Iteration))
            {
                parallelActions.Add(() => iterationCancelationSource.Cancel());
            }

            var helper = ParallelCombineTestHelper.Create(
                combineType,
                async () =>
                {
                    var asyncEnumerator = group
                        .Add(promise1)
                        .Add(promise2)
                        .Add(promise3)
                        .GetAsyncEnumerable(suppressUnobservedRejections: true)
                        .WithCancelation(iterationCancelationToken)
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
                    CancelationToken groupCancelationToken;
                    if (cancelationType.HasFlag(EachCancelationType.Group))
                    {
                        groupCancelationSource = CancelationSource.New();
                        group = PromiseEachGroup<int>.New(groupCancelationSource.Token, out groupCancelationToken);
                    }
                    else
                    {
                        group = PromiseEachGroup<int>.New(out groupCancelationToken);
                    }
                    promise1 = TestHelper.BuildPromise(completeType1, alreadyComplete1, 1, rejectValue, groupCancelationToken, out tryCompleter1);
                    promise2 = TestHelper.BuildPromise(completeType2, alreadyComplete2, 2, rejectValue, groupCancelationToken, out tryCompleter2);
                    promise3 = TestHelper.BuildPromise(completeType3, alreadyComplete3, 3, rejectValue, groupCancelationToken, out tryCompleter3);
                    if (cancelationType.HasFlag(EachCancelationType.Iteration))
                    {
                        iterationCancelationSource = CancelationSource.New();
                        iterationCancelationToken = iterationCancelationSource.Token;
                    }
                    helper.Setup();
                },
                // teardown
                () =>
                {
                    helper.Teardown();
                    if (cancelationType.HasFlag(EachCancelationType.Group))
                    {
                        groupCancelationSource.Dispose();
                    }
                    if (cancelationType.HasFlag(EachCancelationType.Iteration))
                    {
                        iterationCancelationSource.Dispose();
                    }
                    Assert.IsTrue(helper.Success);
                },
                parallelActions
            );
        }
    }
}

#endif // !UNITY_WEBGL