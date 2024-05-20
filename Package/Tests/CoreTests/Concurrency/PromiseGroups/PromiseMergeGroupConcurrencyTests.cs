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

namespace ProtoPromiseTests.Concurrency.PromiseGroups
{
    public class PromiseMergeGroupConcurrencyTests
    {
        const string rejectValue = "Fail";

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
        public void DeferredsMayBeCompletedWhileTheirPromisesArePassedToPromiseMergeGroup_AndCancelationTriggeredConcurrently_T_void(
            [Values] bool withCancelation,
            [Values] CombineType combineType,
            [Values(CompleteType.Resolve, CompleteType.Reject, CompleteType.Cancel)] CompleteType completeType1,
            [Values] bool alreadyComplete1,
            [Values(CompleteType.Resolve, CompleteType.Reject, CompleteType.Cancel)] CompleteType completeType2,
            // We need at least 1 promise to be pending.
            [Values(false)] bool alreadyComplete2)
        {
            var tryCompleter1 = TestHelper.GetTryCompleterT(completeType1, 1, rejectValue);
            var tryCompleter2 = TestHelper.GetTryCompleterVoid(completeType2, rejectValue);

            var deferred1 = default(Promise<int>.Deferred);
            var deferred2 = default(Promise.Deferred);
            var promise1 = default(Promise<int>);
            var promise2 = default(Promise);
            var cancelationSource = default(CancelationSource);
            var groupCancelationToken = default(CancelationToken);
            var group = default(PromiseMergeGroup);

            List<Action> parallelActions = new List<Action>();
            if (!alreadyComplete1)
            {
                parallelActions.Add(() => tryCompleter1(deferred1, default));
            }
            if (!alreadyComplete2)
            {
                parallelActions.Add(() => tryCompleter2(deferred2, default));
            }
            if (withCancelation)
            {
                parallelActions.Add(() => cancelationSource.Cancel());
            }

            var helper = ParallelCombineTestHelper.Create(
                combineType,
                () => group
                    .Add(promise1)
                    .Add(promise2)
                    .WaitAsync(),
                expectedResolveValue: 1
            );
            helper.MaybeAddParallelAction(parallelActions);

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsMaybeWithOffsets(
                // setup
                () =>
                {
                    if (withCancelation)
                    {
                        cancelationSource = CancelationSource.New();
                        group = PromiseMergeGroup.New(cancelationSource.Token, out groupCancelationToken);
                    }
                    else
                    {
                        group = PromiseMergeGroup.New(out groupCancelationToken);
                    }
                    promise1 = TestHelper.BuildPromise(completeType1, alreadyComplete1, 1, rejectValue, out deferred1, out _);
                    promise2 = TestHelper.BuildPromise(completeType2, alreadyComplete2, rejectValue, out deferred2, out _);
                    groupCancelationToken.Register(() =>
                    {
                        deferred1.TryCancel();
                        deferred2.TryCancel();
                    });
                    helper.Setup();
                },
                // teardown
                () =>
                {
                    helper.Teardown();
                    cancelationSource.TryDispose();
                    Assert.IsTrue(helper.Success);
                },
                parallelActions
            );
        }

        [Test]
        public void DeferredsMayBeCompletedWhileTheirPromisesArePassedToPromiseMergeGroup_AndCancelationTriggeredConcurrently_void_T_void_T(
            [Values] bool withCancelation,
            [Values] CombineType combineType,
            [Values(CompleteType.Resolve, CompleteType.Reject, CompleteType.Cancel)] CompleteType completeType1,
            [Values] bool alreadyComplete1,
            [Values(CompleteType.Resolve)] CompleteType completeType2,
            // We need at least 1 promise to be pending.
            [Values(false)] bool alreadyComplete2,
            [Values(CompleteType.Resolve)] CompleteType completeType3,
            // Only have up to 2 already complete to reduce test count.
            [Values(false)] bool alreadyComplete3,
            [Values(CompleteType.Resolve, CompleteType.Reject, CompleteType.Cancel)] CompleteType completeType4,
            [Values] bool alreadyComplete4)
        {
            var tryCompleter1 = TestHelper.GetTryCompleterVoid(completeType1, rejectValue);
            var tryCompleter2 = TestHelper.GetTryCompleterT(completeType2, 1, rejectValue);
            var tryCompleter3 = TestHelper.GetTryCompleterVoid(completeType3, rejectValue);
            var tryCompleter4 = TestHelper.GetTryCompleterT(completeType4, 2, rejectValue);

            var deferred1 = default(Promise.Deferred);
            var deferred2 = default(Promise<int>.Deferred);
            var deferred3 = default(Promise.Deferred);
            var deferred4 = default(Promise<int>.Deferred);
            var promise1 = default(Promise);
            var promise2 = default(Promise<int>);
            var promise3 = default(Promise);
            var promise4 = default(Promise<int>);
            var cancelationSource = default(CancelationSource);
            var groupCancelationToken = default(CancelationToken);
            var group = default(PromiseMergeGroup);

            List<Action> parallelActions = new List<Action>();
            if (!alreadyComplete1)
            {
                parallelActions.Add(() => tryCompleter1(deferred1, default));
            }
            if (!alreadyComplete2)
            {
                parallelActions.Add(() => tryCompleter2(deferred2, default));
            }
            if (!alreadyComplete3)
            {
                parallelActions.Add(() => tryCompleter3(deferred3, default));
            }
            if (!alreadyComplete4)
            {
                parallelActions.Add(() => tryCompleter4(deferred4, default));
            }
            if (withCancelation)
            {
                parallelActions.Add(() => cancelationSource.Cancel());
            }

            var helper = ParallelCombineTestHelper.Create(
                combineType,
                () => group
                    .Add(promise1)
                    .Add(promise2)
                    .Add(promise3)
                    .Add(promise4)
                    .WaitAsync(),
                expectedResolveValue: (1, 2)
            );
            helper.MaybeAddParallelAction(parallelActions);

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsMaybeWithOffsets(
                // setup
                () =>
                {
                    if (withCancelation)
                    {
                        cancelationSource = CancelationSource.New();
                        group = PromiseMergeGroup.New(cancelationSource.Token, out groupCancelationToken);
                    }
                    else
                    {
                        group = PromiseMergeGroup.New(out groupCancelationToken);
                    }
                    promise1 = TestHelper.BuildPromise(completeType1, alreadyComplete1, rejectValue, out deferred1, out _);
                    promise2 = TestHelper.BuildPromise(completeType2, alreadyComplete2, 1, rejectValue, out deferred2, out _);
                    promise3 = TestHelper.BuildPromise(completeType3, alreadyComplete3, rejectValue, out deferred3, out _);
                    promise4 = TestHelper.BuildPromise(completeType4, alreadyComplete4, 2, rejectValue, out deferred4, out _);
                    groupCancelationToken.Register(() =>
                    {
                        deferred1.TryCancel();
                        deferred2.TryCancel();
                        deferred3.TryCancel();
                        deferred4.TryCancel();
                    });
                    helper.Setup();
                },
                // teardown
                () =>
                {
                    helper.Teardown();
                    cancelationSource.TryDispose();
                    Assert.IsTrue(helper.Success);
                },
                parallelActions
            );
        }
    }
}

#endif // !UNITY_WEBGL