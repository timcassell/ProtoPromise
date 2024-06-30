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

namespace ProtoPromiseTests.Concurrency
{
    public class MergeConcurrencyTests
    {
        const string rejectValue = "Fail";

        [SetUp]
        public void Setup()
        {
            // When 2 or more promises are rejected, the remaining rejects are sent to the UncaughtRejectionHandler.
            // So we set the expected uncaught reject value.
            TestHelper.s_expectedUncaughtRejectValue = rejectValue;

            TestHelper.Setup();
        }

        [TearDown]
        public void Teardown()
        {
            TestHelper.Cleanup();

            TestHelper.s_expectedUncaughtRejectValue = null;
        }

        [Test]
        public void DeferredsMayBeCompletedWhileTheirPromisesArePassedToMergeConcurrently_T1void(
            [Values] CombineType combineType,
            [Values] CompleteType completeType0,
            [Values] CompleteType completeTypeVoid)
        {
            var completer0 = TestHelper.GetCompleterT(completeType0, 1, rejectValue);
            var completerVoid = TestHelper.GetCompleterVoid(completeTypeVoid, rejectValue);

            var deferred0 = default(Promise<int>.Deferred);
            var deferredVoid = default(Promise.Deferred);

            List<Action> parallelActions = new List<Action>()
            {
                () => completer0(deferred0),
                () => completerVoid(deferredVoid),
            };

            var helper = ParallelCombineTestHelper.Create(
                combineType,
                () => Promise.Merge(deferred0.Promise, deferredVoid.Promise),
                expectedResolveValue: 1
            );
            helper.MaybeAddParallelAction(parallelActions);

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsMaybeWithOffsets(
                // setup
                () =>
                {
                    deferred0 = Promise<int>.NewDeferred();
                    deferredVoid = Promise.NewDeferred();
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

        [Test]
        public void DeferredsMayBeCompletedWhileTheirPromisesArePassedToMergeConcurrently_T2(
            [Values] CombineType combineType,
            [Values] CompleteType completeType0,
            [Values] CompleteType completeType1)
        {
            var completer0 = TestHelper.GetCompleterT(completeType0, 1, rejectValue);
            var completer1 = TestHelper.GetCompleterT(completeType1, 2, rejectValue);

            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);

            List<Action> parallelActions = new List<Action>()
            {
                () => completer0(deferred0),
                () => completer1(deferred1),
            };

            var helper = ParallelCombineTestHelper.Create(
                combineType,
                () => Promise.Merge(deferred0.Promise, deferred1.Promise),
                expectedResolveValue: (1, 2)
            );
            helper.MaybeAddParallelAction(parallelActions);

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsMaybeWithOffsets(
                // setup
                () =>
                {
                    deferred0 = Promise<int>.NewDeferred();
                    deferred1 = Promise<int>.NewDeferred();
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
        public void DeferredsMayBeCompletedWhileTheirPromisesArePassedToMergeConcurrently_T2void(
            [Values] CombineType combineType,
            [Values] CompleteType completeType0,
            [Values(CompleteType.Resolve)] CompleteType completeType1,
            [Values] CompleteType completeTypeVoid)
        {
            var completer0 = TestHelper.GetCompleterT(completeType0, 1, rejectValue);
            var completer1 = TestHelper.GetCompleterT(completeType1, 2, rejectValue);
            var completerVoid = TestHelper.GetCompleterVoid(completeTypeVoid, rejectValue);

            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
            var deferredVoid = default(Promise.Deferred);

            List<Action> parallelActions = new List<Action>()
            {
                () => completer0(deferred0),
                () => completer1(deferred1),
                () => completerVoid(deferredVoid),
            };

            var helper = ParallelCombineTestHelper.Create(
                combineType,
                () => Promise.Merge(deferred0.Promise, deferred1.Promise, deferredVoid.Promise),
                expectedResolveValue: (1, 2)
            );
            helper.MaybeAddParallelAction(parallelActions);

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsMaybeWithOffsets(
                // setup
                () =>
                {
                    deferred0 = Promise<int>.NewDeferred();
                    deferred1 = Promise<int>.NewDeferred();
                    deferredVoid = Promise.NewDeferred();
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

        // We don't test T3 - T6 to reduce number of tests. The implementation is basically the same for all of them anyway.

        [Test] // Only generate up to 2 CompleteTypes (more takes too long to test)
        public void DeferredsMayBeCompletedWhileTheirPromisesArePassedToMergeConcurrently_T7(
            [Values] CombineType combineType,
            [Values] CompleteType completeType0,
            [Values] CompleteType completeType1,
            [Values(CompleteType.Resolve)] CompleteType completeType2,
            [Values(CompleteType.Resolve)] CompleteType completeType3,
            [Values(CompleteType.Resolve)] CompleteType completeType4,
            [Values(CompleteType.Resolve)] CompleteType completeType5,
            [Values(CompleteType.Resolve)] CompleteType completeType6)
        {
            var completer0 = TestHelper.GetCompleterT(completeType0, 1, rejectValue);
            var completer1 = TestHelper.GetCompleterT(completeType1, 2, rejectValue);
            var completer2 = TestHelper.GetCompleterT(completeType2, 3, rejectValue);
            var completer3 = TestHelper.GetCompleterT(completeType3, 4, rejectValue);
            var completer4 = TestHelper.GetCompleterT(completeType4, 5, rejectValue);
            var completer5 = TestHelper.GetCompleterT(completeType5, 6, rejectValue);
            var completer6 = TestHelper.GetCompleterT(completeType6, 7, rejectValue);

            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
            var deferred2 = default(Promise<int>.Deferred);
            var deferred3 = default(Promise<int>.Deferred);
            var deferred4 = default(Promise<int>.Deferred);
            var deferred5 = default(Promise<int>.Deferred);
            var deferred6 = default(Promise<int>.Deferred);

            List<Action> parallelActions = new List<Action>()
            {
                () => completer0(deferred0),
                () => completer1(deferred1),
                () => completer2(deferred2),
                () => completer3(deferred3),
                () => completer4(deferred4),
                () => completer5(deferred5),
                () => completer6(deferred6),
            };

            var helper = ParallelCombineTestHelper.Create(
                combineType,
                () => Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise, deferred4.Promise, deferred5.Promise, deferred6.Promise),
                expectedResolveValue: (1, 2, 3, 4, 5, 6, 7)
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
                    deferred4 = Promise<int>.NewDeferred();
                    deferred5 = Promise<int>.NewDeferred();
                    deferred6 = Promise<int>.NewDeferred();
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
        public void DeferredsMayBeCompletedWhileTheirPromisesArePassedToMergeConcurrently_T7void(
            [Values] CombineType combineType,
            [Values] CompleteType completeType0,
            [Values(CompleteType.Resolve)] CompleteType completeType1,
            [Values(CompleteType.Resolve)] CompleteType completeType2,
            [Values(CompleteType.Resolve)] CompleteType completeType3,
            [Values(CompleteType.Resolve)] CompleteType completeType4,
            [Values(CompleteType.Resolve)] CompleteType completeType5,
            [Values(CompleteType.Resolve)] CompleteType completeType6,
            [Values] CompleteType completeTypeVoid)
        {
            var completer0 = TestHelper.GetCompleterT(completeType0, 1, rejectValue);
            var completer1 = TestHelper.GetCompleterT(completeType1, 2, rejectValue);
            var completer2 = TestHelper.GetCompleterT(completeType2, 3, rejectValue);
            var completer3 = TestHelper.GetCompleterT(completeType3, 4, rejectValue);
            var completer4 = TestHelper.GetCompleterT(completeType4, 5, rejectValue);
            var completer5 = TestHelper.GetCompleterT(completeType5, 6, rejectValue);
            var completer6 = TestHelper.GetCompleterT(completeType6, 7, rejectValue);
            var completerVoid = TestHelper.GetCompleterVoid(completeTypeVoid, rejectValue);

            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
            var deferred2 = default(Promise<int>.Deferred);
            var deferred3 = default(Promise<int>.Deferred);
            var deferred4 = default(Promise<int>.Deferred);
            var deferred5 = default(Promise<int>.Deferred);
            var deferred6 = default(Promise<int>.Deferred);
            var deferredVoid = default(Promise.Deferred);

            List<Action> parallelActions = new List<Action>()
            {
                () => completer0(deferred0),
                () => completer1(deferred1),
                () => completer2(deferred2),
                () => completer3(deferred3),
                () => completer4(deferred4),
                () => completer5(deferred5),
                () => completer6(deferred6),
                () => completerVoid(deferredVoid),
            };

            var helper = ParallelCombineTestHelper.Create(
                combineType,
                () => Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise, deferred4.Promise, deferred5.Promise, deferred6.Promise, deferredVoid.Promise),
                expectedResolveValue: (1, 2, 3, 4, 5, 6, 7)
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
                    deferred4 = Promise<int>.NewDeferred();
                    deferred5 = Promise<int>.NewDeferred();
                    deferred6 = Promise<int>.NewDeferred();
                    deferredVoid = Promise.NewDeferred();
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
    }
}

#endif // !UNITY_WEBGL