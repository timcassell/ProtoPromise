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
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace ProtoPromise.Tests.Concurrency.PromiseGroups
{
    public class PromiseRaceGroupConcurrencyTests
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
        public void DeferredsMayBeCompletedWhileTheirPromisesArePassedToPromiseRaceGroup_AndCancelationTriggeredConcurrently_3_void(
            [Values] bool withCancelation,
            [Values] CombineType combineType,
            [Values] CompleteType completeType1,
            [Values] bool alreadyComplete1,
            [Values(CompleteType.Resolve)] CompleteType completeType2,
            [Values] bool alreadyComplete2,
            [Values] CompleteType completeType3,
            // We need at least 1 promise to be pending.
            [Values(false)] bool alreadyComplete3)
        {
            var tryCompleter1 = default(Action);
            var tryCompleter2 = default(Action);
            var tryCompleter3 = default(Action);
            var promise1 = default(Promise);
            var promise2 = default(Promise);
            var promise3 = default(Promise);
            var cancelationSource = default(CancelationSource);
            var groupCancelationToken = default(CancelationToken);
            var group = default(PromiseRaceGroup);

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
                    .WaitAsync()
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
                        group = PromiseRaceGroup.New(cancelationSource.Token, out groupCancelationToken);
                    }
                    else
                    {
                        group = PromiseRaceGroup.New(out groupCancelationToken);
                    }
                    promise1 = TestHelper.BuildPromise(completeType1, alreadyComplete1, rejectValue, groupCancelationToken, out tryCompleter1);
                    promise2 = TestHelper.BuildPromise(completeType2, alreadyComplete2, rejectValue, groupCancelationToken, out tryCompleter2);
                    promise3 = TestHelper.BuildPromise(completeType3, alreadyComplete3, rejectValue, groupCancelationToken, out tryCompleter3);
                    helper.Setup();
                },
                // teardown
                () =>
                {
                    helper.Teardown();
                    if (withCancelation)
                    {
                        cancelationSource.Dispose();
                    }
                    Assert.IsTrue(helper.Success);
                },
                parallelActions
            );
        }

        [Test]
        public void DeferredsMayBeCompletedWhileTheirPromisesArePassedToPromiseRaceGroup_AndCancelationTriggeredConcurrently_3_T(
            [Values] bool withCancelation,
            [Values] bool withCleanup,
            [Values] CombineType combineType,
            [Values] CompleteType completeType1,
            [Values] bool alreadyComplete1,
            [Values(CompleteType.Resolve)] CompleteType completeType2,
            [Values] bool alreadyComplete2,
            [Values] CompleteType completeType3,
            // We need at least 1 promise to be pending.
            [Values(false)] bool alreadyComplete3)
        {
            var tryCompleter1 = default(Action);
            var tryCompleter2 = default(Action);
            var tryCompleter3 = default(Action);
            var promise1 = default(Promise<int>);
            var promise2 = default(Promise<int>);
            var promise3 = default(Promise<int>);
            var cancelationSource = default(CancelationSource);
            var groupCancelationToken = default(CancelationToken);
            var group = default(PromiseRaceGroup<int>);

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
            if (withCancelation)
            {
                parallelActions.Add(() => cancelationSource.Cancel());
            }

            int resolveCount = 0;
            int cleanupCount = 0;
            long completedFlag = 0;
            Action<int> onCleanup = _ =>
            {
                Assert.AreEqual(0, Interlocked.Read(ref completedFlag));
                Interlocked.Increment(ref cleanupCount);
            };

            var helper = ParallelCombineTestHelper.Create(
                combineType,
                () => group
                    .Add(promise1)
                    .Add(promise2)
                    .Add(promise3)
                    .WaitAsync()
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
                        group = withCleanup
                            ? PromiseRaceGroup<int>.New(cancelationSource.Token, out groupCancelationToken, onCleanup)
                            : PromiseRaceGroup<int>.New(cancelationSource.Token, out groupCancelationToken);
                    }
                    else
                    {
                        group = withCleanup
                            ? PromiseRaceGroup<int>.New(out groupCancelationToken, onCleanup)
                            : PromiseRaceGroup<int>.New(out groupCancelationToken);
                    }
                    promise1 = TestHelper.BuildPromise(completeType1, alreadyComplete1, 1, rejectValue, groupCancelationToken, out tryCompleter1);
                    promise2 = TestHelper.BuildPromise(completeType2, alreadyComplete2, 2, rejectValue, groupCancelationToken, out tryCompleter2);
                    promise3 = TestHelper.BuildPromise(completeType3, alreadyComplete3, 3, rejectValue, groupCancelationToken, out tryCompleter3);
                    completedFlag = 0;
                    if (withCleanup)
                    {
                        resolveCount = 0;
                        cleanupCount = 0;
                        promise1 = promise1.Then(v => { Interlocked.Increment(ref resolveCount); return v; });
                        promise2 = promise2.Then(v => { Interlocked.Increment(ref resolveCount); return v; });
                        promise3 = promise3.Then(v => { Interlocked.Increment(ref resolveCount); return v; });
                    }
                    helper.Setup();
                },
                // teardown
                () =>
                {
                    helper.Teardown();
                    if (withCancelation)
                    {
                        cancelationSource.Dispose();
                    }
                    Assert.IsTrue(helper.Success);
                    if (withCleanup)
                    {
                        if (helper.State != Promise.State.Resolved)
                            Assert.AreEqual(resolveCount, cleanupCount);
                        else
                            Assert.AreEqual(resolveCount - 1, cleanupCount);
                    }
                },
                parallelActions
            );
        }
    }
}

#endif // !UNITY_WEBGL