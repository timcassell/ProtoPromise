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
    public class RaceConcurrencyTests
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
        public void DeferredsMayBeCompletedWhileTheirPromisesArePassedToRaceConcurrently_void0(
            [Values] CombineType combineType,
            [Values] CompleteType completeType0,
            [Values] CompleteType completeType1)
        {
            var completer0 = TestHelper.GetCompleterVoid(completeType0, rejectValue);
            var completer1 = TestHelper.GetCompleterVoid(completeType1, rejectValue);

            var deferred0 = default(Promise.Deferred);
            var deferred1 = default(Promise.Deferred);

            List<Action> parallelActions = new List<Action>()
            {
                () => completer0(deferred0),
                () => completer1(deferred1),
            };

            var helper = ParallelCombineTestHelper.Create(
                combineType,
                () => Promise.Race(deferred0.Promise, deferred1.Promise)
            );
            helper.MaybeAddParallelAction(parallelActions);

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsMaybeWithOffsets(
                // setup
                () =>
                {
                    deferred0 = Promise.NewDeferred();
                    deferred1 = Promise.NewDeferred();
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
        public void DeferredsMayBeCompletedWhileTheirPromisesArePassedToRaceConcurrently_void1(
            [Values] CombineType combineType,
            [Values] CompleteType completeType0,
            [Values] CompleteType completeType1,
            [Values(CompleteType.Resolve)] CompleteType completeType2)
        {
            var completer0 = TestHelper.GetCompleterVoid(completeType0, rejectValue);
            var completer1 = TestHelper.GetCompleterVoid(completeType1, rejectValue);
            var completer2 = TestHelper.GetCompleterVoid(completeType2, rejectValue);

            var deferred0 = default(Promise.Deferred);
            var deferred1 = default(Promise.Deferred);
            var deferred2 = default(Promise.Deferred);

            List<Action> parallelActions = new List<Action>()
            {
                () => completer0(deferred0),
                () => completer1(deferred1),
                () => completer2(deferred2),
            };

            var helper = ParallelCombineTestHelper.Create(
                combineType,
                () => Promise.Race(deferred0.Promise, deferred1.Promise, deferred2.Promise)
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
        public void DeferredsMayBeCompletedWhileTheirPromisesArePassedToRaceConcurrently_void2(
            [Values] CombineType combineType,
            [Values] CompleteType completeType0,
            [Values] CompleteType completeType1,
            [Values(CompleteType.Resolve)] CompleteType completeType2,
            [Values(CompleteType.Resolve)] CompleteType completeType3)
        {
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
                () => Promise.Race(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise)
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
        public void DeferredsMayBeCompletedWhileTheirPromisesArePassedToRaceConcurrently_void3(
            [Values] CombineType combineType,
            [Values] CompleteType completeType0,
            [Values] CompleteType completeType1,
            [Values(CompleteType.Resolve)] CompleteType completeType2,
            [Values(CompleteType.Resolve)] CompleteType completeType3)
        {
            var completer0 = TestHelper.GetCompleterVoid(completeType0, rejectValue);
            var completer1 = TestHelper.GetCompleterVoid(completeType1, rejectValue);
            var completer2 = TestHelper.GetCompleterVoid(completeType2, rejectValue);
            var completer3 = TestHelper.GetCompleterVoid(completeType3, rejectValue);

            Promise.Deferred[] deferreds = null;
            IEnumerator<Promise> promises = null;

            List<Action> parallelActions = new List<Action>()
            {
                () => completer0(deferreds[0]),
                () => completer1(deferreds[1]),
                () => completer2(deferreds[2]),
                () => completer3(deferreds[3])
            };

            var helper = ParallelCombineTestHelper.Create(
                combineType,
                () => Promise.Race(promises)
            );
            helper.MaybeAddParallelAction(parallelActions);

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsMaybeWithOffsets(
                // setup
                () =>
                {
                    deferreds = new Promise.Deferred[]
                    {
                        Promise.NewDeferred(),
                        Promise.NewDeferred(),
                        Promise.NewDeferred(),
                        Promise.NewDeferred()
                    };
                    promises = deferreds.Select(d => d.Promise).GetEnumerator();
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
        public void DeferredsMayBeCompletedWhileTheirPromisesArePassedToRaceConcurrently_T0(
            [Values] CombineType combineType,
            [Values] CompleteType completeType0,
            [Values] CompleteType completeType1)
        {
            var completer0 = TestHelper.GetCompleterT(completeType0, 1, rejectValue);
            var completer1 = TestHelper.GetCompleterT(completeType1, 1, rejectValue);

            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);

            List<Action> parallelActions = new List<Action>()
            {
                () => completer0(deferred0),
                () => completer1(deferred1),
            };

            var helper = ParallelCombineTestHelper.Create(
                combineType,
                () => Promise<int>.Race(deferred0.Promise, deferred1.Promise),
                expectedResolveValue: 1
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
        public void DeferredsMayBeCompletedWhileTheirPromisesArePassedToRaceConcurrently_T1(
            [Values] CombineType combineType,
            [Values] CompleteType completeType0,
            [Values] CompleteType completeType1,
            [Values(CompleteType.Resolve)] CompleteType completeType2)
        {
            var completer0 = TestHelper.GetCompleterT(completeType0, 1, rejectValue);
            var completer1 = TestHelper.GetCompleterT(completeType1, 1, rejectValue);
            var completer2 = TestHelper.GetCompleterT(completeType2, 1, rejectValue);

            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
            var deferred2 = default(Promise<int>.Deferred);

            List<Action> parallelActions = new List<Action>()
            {
                () => completer0(deferred0),
                () => completer1(deferred1),
                () => completer2(deferred2),
            };

            var helper = ParallelCombineTestHelper.Create(
                combineType,
                () => Promise<int>.Race(deferred0.Promise, deferred1.Promise, deferred2.Promise),
                expectedResolveValue: 1
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
        public void DeferredsMayBeCompletedWhileTheirPromisesArePassedToRaceConcurrently_T2(
            [Values] CombineType combineType,
            [Values] CompleteType completeType0,
            [Values] CompleteType completeType1,
            [Values(CompleteType.Resolve)] CompleteType completeType2,
            [Values(CompleteType.Resolve)] CompleteType completeType3)
        {
            var completer0 = TestHelper.GetCompleterT(completeType0, 1, rejectValue);
            var completer1 = TestHelper.GetCompleterT(completeType1, 1, rejectValue);
            var completer2 = TestHelper.GetCompleterT(completeType2, 1, rejectValue);
            var completer3 = TestHelper.GetCompleterT(completeType3, 1, rejectValue);

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
                () => Promise<int>.Race(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise),
                expectedResolveValue: 1
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
        public void DeferredsMayBeCompletedWhileTheirPromisesArePassedToRaceConcurrently_T3(
            [Values] CombineType combineType,
            [Values] CompleteType completeType0,
            [Values] CompleteType completeType1,
            [Values(CompleteType.Resolve)] CompleteType completeType2,
            [Values(CompleteType.Resolve)] CompleteType completeType3)
        {
            var completer0 = TestHelper.GetCompleterT(completeType0, 1, rejectValue);
            var completer1 = TestHelper.GetCompleterT(completeType1, 1, rejectValue);
            var completer2 = TestHelper.GetCompleterT(completeType2, 1, rejectValue);
            var completer3 = TestHelper.GetCompleterT(completeType3, 1, rejectValue);

            Promise<int>.Deferred[] deferreds = null;
            IEnumerator<Promise<int>> promises = null;

            List<Action> parallelActions = new List<Action>()
            {
                () => completer0(deferreds[0]),
                () => completer1(deferreds[1]),
                () => completer2(deferreds[2]),
                () => completer3(deferreds[3])
            };

            var helper = ParallelCombineTestHelper.Create(
                combineType,
                () => Promise<int>.Race(promises),
                expectedResolveValue: 1
            );
            helper.MaybeAddParallelAction(parallelActions);

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsMaybeWithOffsets(
                // setup
                () =>
                {
                    deferreds = new Promise<int>.Deferred[]
                    {
                        Promise<int>.NewDeferred(),
                        Promise<int>.NewDeferred(),
                        Promise<int>.NewDeferred(),
                        Promise<int>.NewDeferred()
                    };
                    promises = deferreds.Select(d => d.Promise).GetEnumerator();
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