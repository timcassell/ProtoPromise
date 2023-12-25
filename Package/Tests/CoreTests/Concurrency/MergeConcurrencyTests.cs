#if !UNITY_WEBGL

#if !PROTO_PROMISE_PROGRESS_DISABLE
#define PROMISE_PROGRESS
#else
#undef PROMISE_PROGRESS
#endif

using NUnit.Framework;
using Proto.Promises;
using System;
using System.Collections.Generic;

#pragma warning disable 0618 // Type or member is obsolete

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
            [MaybeValues(CompleteType.Resolve)] CompleteType completeTypeVoid)
        {
            var completer0 = TestHelper.GetCompleterT(completeType0, 1, rejectValue);
            var completerVoid = TestHelper.GetCompleterVoid(completeTypeVoid, rejectValue);

            var deferred0 = default(Promise<int>.Deferred);
            var deferredVoid = default(Promise.Deferred);
            var cancelationSource0 = default(CancelationSource);
            var cancelationSourceVoid = default(CancelationSource);

            List<Action> parallelActions = new List<Action>()
            {
#if PROMISE_PROGRESS
                () => deferred0.TryReportProgress(0.5f),
                () => deferredVoid.TryReportProgress(0.5f),
#endif
                () => completer0(deferred0, cancelationSource0),
                () => completerVoid(deferredVoid, cancelationSourceVoid),
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
                    deferred0 = TestHelper.GetNewDeferredT<int>(completeType0, out cancelationSource0);
                    deferredVoid = TestHelper.GetNewDeferredVoid(completeTypeVoid, out cancelationSourceVoid);
                    helper.Setup();
                },
                // teardown
                () =>
                {
                    helper.Teardown();
                    cancelationSource0.TryDispose();
                    cancelationSourceVoid.TryDispose();
                    Assert.IsTrue(helper.Success);
                },
                parallelActions
            );
        }

        [Test]
        public void DeferredsMayBeCompletedWhileTheirPromisesArePassedToMergeConcurrently_T2(
            [Values] CombineType combineType,
            [Values] CompleteType completeType0,
            [MaybeValues(CompleteType.Resolve)] CompleteType completeType1)
        {
            var completer0 = TestHelper.GetCompleterT(completeType0, 1, rejectValue);
            var completer1 = TestHelper.GetCompleterT(completeType1, 2, rejectValue);

            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
            var cancelationSource0 = default(CancelationSource);
            var cancelationSource1 = default(CancelationSource);

            List<Action> parallelActions = new List<Action>()
            {
#if PROMISE_PROGRESS
                () => deferred0.TryReportProgress(0.5f),
                () => deferred1.TryReportProgress(0.5f),
#endif
                () => completer0(deferred0, cancelationSource0),
                () => completer1(deferred1, cancelationSource1),
            };

            var helper = ParallelCombineTestHelper.Create(
                combineType,
                () => Promise.Merge(deferred0.Promise, deferred1.Promise),
                expectedResolveValue: ValueTuple.Create(1, 2)
            );
            helper.MaybeAddParallelAction(parallelActions);

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsMaybeWithOffsets(
                // setup
                () =>
                {
                    deferred0 = TestHelper.GetNewDeferredT<int>(completeType0, out cancelationSource0);
                    deferred1 = TestHelper.GetNewDeferredT<int>(completeType1, out cancelationSource1);
                    helper.Setup();
                },
                // teardown
                () =>
                {
                    helper.Teardown();
                    cancelationSource0.TryDispose();
                    cancelationSource1.TryDispose();
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
            [MaybeValues(CompleteType.Resolve)] CompleteType completeTypeVoid)
        {
            var completer0 = TestHelper.GetCompleterT(completeType0, 1, rejectValue);
            var completer1 = TestHelper.GetCompleterT(completeType1, 2, rejectValue);
            var completerVoid = TestHelper.GetCompleterVoid(completeTypeVoid, rejectValue);

            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
            var deferredVoid = default(Promise.Deferred);
            var cancelationSource0 = default(CancelationSource);
            var cancelationSource1 = default(CancelationSource);
            var cancelationSourceVoid = default(CancelationSource);

            List<Action> parallelActions = new List<Action>()
            {
#if PROMISE_PROGRESS
                () => deferred0.TryReportProgress(0.5f),
                () => deferred1.TryReportProgress(0.5f),
                () => deferredVoid.TryReportProgress(0.5f),
#endif
                () => completer0(deferred0, cancelationSource0),
                () => completer1(deferred1, cancelationSource1),
                () => completerVoid(deferredVoid, cancelationSourceVoid),
            };

            var helper = ParallelCombineTestHelper.Create(
                combineType,
                () => Promise.Merge(deferred0.Promise, deferred1.Promise, deferredVoid.Promise),
                expectedResolveValue: ValueTuple.Create(1, 2)
            );
            helper.MaybeAddParallelAction(parallelActions);

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsMaybeWithOffsets(
                // setup
                () =>
                {
                    deferred0 = TestHelper.GetNewDeferredT<int>(completeType0, out cancelationSource0);
                    deferred1 = TestHelper.GetNewDeferredT<int>(completeType1, out cancelationSource1);
                    deferredVoid = TestHelper.GetNewDeferredVoid(completeTypeVoid, out cancelationSourceVoid);
                    helper.Setup();
                },
                // teardown
                () =>
                {
                    helper.Teardown();
                    cancelationSource0.TryDispose();
                    cancelationSource1.TryDispose();
                    cancelationSourceVoid.TryDispose();
                    Assert.IsTrue(helper.Success);
                },
                parallelActions
            );
        }

        [Test]
        public void DeferredsMayBeCompletedWhileTheirPromisesArePassedToMergeConcurrently_T3(
            [Values] CombineType combineType,
            [Values] CompleteType completeType0,
            [MaybeValues(CompleteType.Resolve)] CompleteType completeType1,
            [Values(CompleteType.Resolve)] CompleteType completeType2)
        {
            var completer0 = TestHelper.GetCompleterT(completeType0, 1, rejectValue);
            var completer1 = TestHelper.GetCompleterT(completeType1, 2, rejectValue);
            var completer2 = TestHelper.GetCompleterT(completeType2, 3, rejectValue);

            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
            var deferred2 = default(Promise<int>.Deferred);
            var cancelationSource0 = default(CancelationSource);
            var cancelationSource1 = default(CancelationSource);
            var cancelationSource2 = default(CancelationSource);

            List<Action> parallelActions = new List<Action>()
            {
#if PROMISE_PROGRESS
                () => deferred0.TryReportProgress(0.5f),
                () => deferred1.TryReportProgress(0.5f),
                () => deferred2.TryReportProgress(0.5f),
#endif
                () => completer0(deferred0, cancelationSource0),
                () => completer1(deferred1, cancelationSource1),
                () => completer2(deferred2, cancelationSource2),
            };

            var helper = ParallelCombineTestHelper.Create(
                combineType,
                () => Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise),
                expectedResolveValue: ValueTuple.Create(1, 2, 3)
            );
            helper.MaybeAddParallelAction(parallelActions);

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsMaybeWithOffsets(
                // setup
                () =>
                {
                    deferred0 = TestHelper.GetNewDeferredT<int>(completeType0, out cancelationSource0);
                    deferred1 = TestHelper.GetNewDeferredT<int>(completeType1, out cancelationSource1);
                    deferred2 = TestHelper.GetNewDeferredT<int>(completeType2, out cancelationSource2);
                    helper.Setup();
                },
                // teardown
                () =>
                {
                    helper.Teardown();
                    cancelationSource0.TryDispose();
                    cancelationSource1.TryDispose();
                    cancelationSource2.TryDispose();
                    Assert.IsTrue(helper.Success);
                },
                parallelActions
            );
        }

        [Test] // Only generate up to 2 CompleteTypes (more takes too long to test)
        public void DeferredsMayBeCompletedWhileTheirPromisesArePassedToMergeConcurrently_T3void(
            [Values] CombineType combineType,
            [Values] CompleteType completeType0,
            [Values(CompleteType.Resolve)] CompleteType completeType1,
            [Values(CompleteType.Resolve)] CompleteType completeType2,
            [MaybeValues(CompleteType.Resolve)] CompleteType completeTypeVoid)
        {
            var completer0 = TestHelper.GetCompleterT(completeType0, 1, rejectValue);
            var completer1 = TestHelper.GetCompleterT(completeType1, 2, rejectValue);
            var completer2 = TestHelper.GetCompleterT(completeType2, 3, rejectValue);
            var completerVoid = TestHelper.GetCompleterVoid(completeTypeVoid, rejectValue);

            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
            var deferred2 = default(Promise<int>.Deferred);
            var deferredVoid = default(Promise.Deferred);
            var cancelationSource0 = default(CancelationSource);
            var cancelationSource1 = default(CancelationSource);
            var cancelationSource2 = default(CancelationSource);
            var cancelationSourceVoid = default(CancelationSource);

            List<Action> parallelActions = new List<Action>()
            {
#if PROMISE_PROGRESS
                () => deferred0.TryReportProgress(0.5f),
                () => deferred1.TryReportProgress(0.5f),
                () => deferred2.TryReportProgress(0.5f),
                () => deferredVoid.TryReportProgress(0.5f),
#endif
                () => completer0(deferred0, cancelationSource0),
                () => completer1(deferred1, cancelationSource1),
                () => completer2(deferred2, cancelationSource2),
                () => completerVoid(deferredVoid, cancelationSourceVoid),
            };

            var helper = ParallelCombineTestHelper.Create(
                combineType,
                () => Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferredVoid.Promise),
                expectedResolveValue: ValueTuple.Create(1, 2, 3)
            );
            helper.MaybeAddParallelAction(parallelActions);

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsMaybeWithOffsets(
                // setup
                () =>
                {
                    deferred0 = TestHelper.GetNewDeferredT<int>(completeType0, out cancelationSource0);
                    deferred1 = TestHelper.GetNewDeferredT<int>(completeType1, out cancelationSource1);
                    deferred2 = TestHelper.GetNewDeferredT<int>(completeType2, out cancelationSource2);
                    deferredVoid = TestHelper.GetNewDeferredVoid(completeTypeVoid, out cancelationSourceVoid);
                    helper.Setup();
                },
                // teardown
                () =>
                {
                    helper.Teardown();
                    cancelationSource0.TryDispose();
                    cancelationSource1.TryDispose();
                    cancelationSource2.TryDispose();
                    cancelationSourceVoid.TryDispose();
                    Assert.IsTrue(helper.Success);
                },
                parallelActions
            );
        }

        [Test] // Only generate up to 2 CompleteTypes (more takes too long to test)
        public void DeferredsMayBeCompletedWhileTheirPromisesArePassedToMergeConcurrently_T4(
            [Values] CombineType combineType,
            [Values] CompleteType completeType0,
            [MaybeValues(CompleteType.Resolve)] CompleteType completeType1,
            [Values(CompleteType.Resolve)] CompleteType completeType2,
            [Values(CompleteType.Resolve)] CompleteType completeType3)
        {
            var completer0 = TestHelper.GetCompleterT(completeType0, 1, rejectValue);
            var completer1 = TestHelper.GetCompleterT(completeType1, 2, rejectValue);
            var completer2 = TestHelper.GetCompleterT(completeType2, 3, rejectValue);
            var completer3 = TestHelper.GetCompleterT(completeType3, 4, rejectValue);

            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
            var deferred2 = default(Promise<int>.Deferred);
            var deferred3 = default(Promise<int>.Deferred);
            var cancelationSource0 = default(CancelationSource);
            var cancelationSource1 = default(CancelationSource);
            var cancelationSource2 = default(CancelationSource);
            var cancelationSource3 = default(CancelationSource);

            List<Action> parallelActions = new List<Action>()
            {
#if PROMISE_PROGRESS
                () => deferred0.TryReportProgress(0.5f),
                () => deferred1.TryReportProgress(0.5f),
                () => deferred2.TryReportProgress(0.5f),
                () => deferred3.TryReportProgress(0.5f),
#endif
                () => completer0(deferred0, cancelationSource0),
                () => completer1(deferred1, cancelationSource1),
                () => completer2(deferred2, cancelationSource2),
                () => completer3(deferred3, cancelationSource3),
            };

            var helper = ParallelCombineTestHelper.Create(
                combineType,
                () => Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise),
                expectedResolveValue: ValueTuple.Create(1, 2, 3, 4)
            );
            helper.MaybeAddParallelAction(parallelActions);

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsMaybeWithOffsets(
                // setup
                () =>
                {
                    deferred0 = TestHelper.GetNewDeferredT<int>(completeType0, out cancelationSource0);
                    deferred1 = TestHelper.GetNewDeferredT<int>(completeType1, out cancelationSource1);
                    deferred2 = TestHelper.GetNewDeferredT<int>(completeType2, out cancelationSource2);
                    deferred3 = TestHelper.GetNewDeferredT<int>(completeType3, out cancelationSource3);
                    helper.Setup();
                },
                // teardown
                () =>
                {
                    helper.Teardown();
                    cancelationSource0.TryDispose();
                    cancelationSource1.TryDispose();
                    cancelationSource2.TryDispose();
                    cancelationSource3.TryDispose();
                    Assert.IsTrue(helper.Success);
                },
                parallelActions
            );
        }

        [Test] // Only generate up to 2 CompleteTypes (more takes too long to test)
        public void DeferredsMayBeCompletedWhileTheirPromisesArePassedToMergeConcurrently_T4void(
            [Values] CombineType combineType,
            [Values] CompleteType completeType0,
            [Values(CompleteType.Resolve)] CompleteType completeType1,
            [Values(CompleteType.Resolve)] CompleteType completeType2,
            [Values(CompleteType.Resolve)] CompleteType completeType3,
            [MaybeValues(CompleteType.Resolve)] CompleteType completeTypeVoid)
        {
            var completer0 = TestHelper.GetCompleterT(completeType0, 1, rejectValue);
            var completer1 = TestHelper.GetCompleterT(completeType1, 2, rejectValue);
            var completer2 = TestHelper.GetCompleterT(completeType2, 3, rejectValue);
            var completer3 = TestHelper.GetCompleterT(completeType3, 4, rejectValue);
            var completerVoid = TestHelper.GetCompleterVoid(completeTypeVoid, rejectValue);

            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
            var deferred2 = default(Promise<int>.Deferred);
            var deferred3 = default(Promise<int>.Deferred);
            var deferredVoid = default(Promise.Deferred);
            var cancelationSource0 = default(CancelationSource);
            var cancelationSource1 = default(CancelationSource);
            var cancelationSource2 = default(CancelationSource);
            var cancelationSource3 = default(CancelationSource);
            var cancelationSourceVoid = default(CancelationSource);

            List<Action> parallelActions = new List<Action>()
            {
#if PROMISE_PROGRESS
                () => deferred0.TryReportProgress(0.5f),
                () => deferred1.TryReportProgress(0.5f),
                () => deferred2.TryReportProgress(0.5f),
                () => deferred3.TryReportProgress(0.5f),
                () => deferredVoid.TryReportProgress(0.5f),
#endif
                () => completer0(deferred0, cancelationSource0),
                () => completer1(deferred1, cancelationSource1),
                () => completer2(deferred2, cancelationSource2),
                () => completer3(deferred3, cancelationSource3),
                () => completerVoid(deferredVoid, cancelationSourceVoid),
            };

            var helper = ParallelCombineTestHelper.Create(
                combineType,
                () => Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise, deferredVoid.Promise),
                expectedResolveValue: ValueTuple.Create(1, 2, 3, 4)
            );
            helper.MaybeAddParallelAction(parallelActions);

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsMaybeWithOffsets(
                // setup
                () =>
                {
                    deferred0 = TestHelper.GetNewDeferredT<int>(completeType0, out cancelationSource0);
                    deferred1 = TestHelper.GetNewDeferredT<int>(completeType1, out cancelationSource1);
                    deferred2 = TestHelper.GetNewDeferredT<int>(completeType2, out cancelationSource2);
                    deferred3 = TestHelper.GetNewDeferredT<int>(completeType3, out cancelationSource3);
                    deferredVoid = TestHelper.GetNewDeferredVoid(completeTypeVoid, out cancelationSourceVoid);
                    helper.Setup();
                },
                // teardown
                () =>
                {
                    helper.Teardown();
                    cancelationSource0.TryDispose();
                    cancelationSource1.TryDispose();
                    cancelationSource2.TryDispose();
                    cancelationSource3.TryDispose();
                    cancelationSourceVoid.TryDispose();
                    Assert.IsTrue(helper.Success);
                },
                parallelActions
            );
        }

        [Test] // Only generate up to 2 CompleteTypes (more takes too long to test)
        public void DeferredsMayBeCompletedWhileTheirPromisesArePassedToMergeConcurrently_T5(
            [Values] CombineType combineType,
            [Values] CompleteType completeType0,
            [MaybeValues(CompleteType.Resolve)] CompleteType completeType1,
            [Values(CompleteType.Resolve)] CompleteType completeType2,
            [Values(CompleteType.Resolve)] CompleteType completeType3,
            [Values(CompleteType.Resolve)] CompleteType completeType4)
        {
            var completer0 = TestHelper.GetCompleterT(completeType0, 1, rejectValue);
            var completer1 = TestHelper.GetCompleterT(completeType1, 2, rejectValue);
            var completer2 = TestHelper.GetCompleterT(completeType2, 3, rejectValue);
            var completer3 = TestHelper.GetCompleterT(completeType3, 4, rejectValue);
            var completer4 = TestHelper.GetCompleterT(completeType4, 5, rejectValue);

            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
            var deferred2 = default(Promise<int>.Deferred);
            var deferred3 = default(Promise<int>.Deferred);
            var deferred4 = default(Promise<int>.Deferred);
            var cancelationSource0 = default(CancelationSource);
            var cancelationSource1 = default(CancelationSource);
            var cancelationSource2 = default(CancelationSource);
            var cancelationSource3 = default(CancelationSource);
            var cancelationSource4 = default(CancelationSource);

            List<Action> parallelActions = new List<Action>()
            {
#if PROMISE_PROGRESS
                () => deferred0.TryReportProgress(0.5f),
                () => deferred1.TryReportProgress(0.5f),
                () => deferred2.TryReportProgress(0.5f),
                () => deferred3.TryReportProgress(0.5f),
                () => deferred4.TryReportProgress(0.5f),
#endif
                () => completer0(deferred0, cancelationSource0),
                () => completer1(deferred1, cancelationSource1),
                () => completer2(deferred2, cancelationSource2),
                () => completer3(deferred3, cancelationSource3),
                () => completer4(deferred4, cancelationSource4),
            };

            var helper = ParallelCombineTestHelper.Create(
                combineType,
                () => Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise, deferred4.Promise),
                expectedResolveValue: ValueTuple.Create(1, 2, 3, 4, 5)
            );
            helper.MaybeAddParallelAction(parallelActions);

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsMaybeWithOffsets(
                // setup
                () =>
                {
                    deferred0 = TestHelper.GetNewDeferredT<int>(completeType0, out cancelationSource0);
                    deferred1 = TestHelper.GetNewDeferredT<int>(completeType1, out cancelationSource1);
                    deferred2 = TestHelper.GetNewDeferredT<int>(completeType2, out cancelationSource2);
                    deferred3 = TestHelper.GetNewDeferredT<int>(completeType3, out cancelationSource3);
                    deferred4 = TestHelper.GetNewDeferredT<int>(completeType4, out cancelationSource4);
                    helper.Setup();
                },
                // teardown
                () =>
                {
                    helper.Teardown();
                    cancelationSource0.TryDispose();
                    cancelationSource1.TryDispose();
                    cancelationSource2.TryDispose();
                    cancelationSource3.TryDispose();
                    cancelationSource4.TryDispose();
                    Assert.IsTrue(helper.Success);
                },
                parallelActions
            );
        }

        [Test] // Only generate up to 2 CompleteTypes (more takes too long to test)
        public void DeferredsMayBeCompletedWhileTheirPromisesArePassedToMergeConcurrently_T5void(
            [Values] CombineType combineType,
            [Values] CompleteType completeType0,
            [Values(CompleteType.Resolve)] CompleteType completeType1,
            [Values(CompleteType.Resolve)] CompleteType completeType2,
            [Values(CompleteType.Resolve)] CompleteType completeType3,
            [Values(CompleteType.Resolve)] CompleteType completeType4,
            [MaybeValues(CompleteType.Resolve)] CompleteType completeTypeVoid)
        {
            var completer0 = TestHelper.GetCompleterT(completeType0, 1, rejectValue);
            var completer1 = TestHelper.GetCompleterT(completeType1, 2, rejectValue);
            var completer2 = TestHelper.GetCompleterT(completeType2, 3, rejectValue);
            var completer3 = TestHelper.GetCompleterT(completeType3, 4, rejectValue);
            var completer4 = TestHelper.GetCompleterT(completeType4, 5, rejectValue);
            var completerVoid = TestHelper.GetCompleterVoid(completeTypeVoid, rejectValue);

            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
            var deferred2 = default(Promise<int>.Deferred);
            var deferred3 = default(Promise<int>.Deferred);
            var deferred4 = default(Promise<int>.Deferred);
            var deferredVoid = default(Promise.Deferred);
            var cancelationSource0 = default(CancelationSource);
            var cancelationSource1 = default(CancelationSource);
            var cancelationSource2 = default(CancelationSource);
            var cancelationSource3 = default(CancelationSource);
            var cancelationSource4 = default(CancelationSource);
            var cancelationSourceVoid = default(CancelationSource);

            List<Action> parallelActions = new List<Action>()
            {
#if PROMISE_PROGRESS
                () => deferred0.TryReportProgress(0.5f),
                () => deferred1.TryReportProgress(0.5f),
                () => deferred2.TryReportProgress(0.5f),
                () => deferred3.TryReportProgress(0.5f),
                () => deferred4.TryReportProgress(0.5f),
                () => deferredVoid.TryReportProgress(0.5f),
#endif
                () => completer0(deferred0, cancelationSource0),
                () => completer1(deferred1, cancelationSource1),
                () => completer2(deferred2, cancelationSource2),
                () => completer3(deferred3, cancelationSource3),
                () => completer4(deferred4, cancelationSource4),
                () => completerVoid(deferredVoid, cancelationSourceVoid),
            };

            var helper = ParallelCombineTestHelper.Create(
                combineType,
                () => Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise, deferred4.Promise, deferredVoid.Promise),
                expectedResolveValue: ValueTuple.Create(1, 2, 3, 4, 5)
            );
            helper.MaybeAddParallelAction(parallelActions);

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsMaybeWithOffsets(
                // setup
                () =>
                {
                    deferred0 = TestHelper.GetNewDeferredT<int>(completeType0, out cancelationSource0);
                    deferred1 = TestHelper.GetNewDeferredT<int>(completeType1, out cancelationSource1);
                    deferred2 = TestHelper.GetNewDeferredT<int>(completeType2, out cancelationSource2);
                    deferred3 = TestHelper.GetNewDeferredT<int>(completeType3, out cancelationSource3);
                    deferred4 = TestHelper.GetNewDeferredT<int>(completeType4, out cancelationSource4);
                    deferredVoid = TestHelper.GetNewDeferredVoid(completeTypeVoid, out cancelationSourceVoid);
                    helper.Setup();
                },
                // teardown
                () =>
                {
                    helper.Teardown();
                    cancelationSource0.TryDispose();
                    cancelationSource1.TryDispose();
                    cancelationSource2.TryDispose();
                    cancelationSource3.TryDispose();
                    cancelationSource4.TryDispose();
                    cancelationSourceVoid.TryDispose();
                    Assert.IsTrue(helper.Success);
                },
                parallelActions
            );
        }

        [Test] // Only generate up to 2 CompleteTypes (more takes too long to test)
        public void DeferredsMayBeCompletedWhileTheirPromisesArePassedToMergeConcurrently_T6(
            [Values] CombineType combineType,
            [Values] CompleteType completeType0,
            [MaybeValues(CompleteType.Resolve)] CompleteType completeType1,
            [Values(CompleteType.Resolve)] CompleteType completeType2,
            [Values(CompleteType.Resolve)] CompleteType completeType3,
            [Values(CompleteType.Resolve)] CompleteType completeType4,
            [Values(CompleteType.Resolve)] CompleteType completeType5)
        {
            var completer0 = TestHelper.GetCompleterT(completeType0, 1, rejectValue);
            var completer1 = TestHelper.GetCompleterT(completeType1, 2, rejectValue);
            var completer2 = TestHelper.GetCompleterT(completeType2, 3, rejectValue);
            var completer3 = TestHelper.GetCompleterT(completeType3, 4, rejectValue);
            var completer4 = TestHelper.GetCompleterT(completeType4, 5, rejectValue);
            var completer5 = TestHelper.GetCompleterT(completeType5, 6, rejectValue);

            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
            var deferred2 = default(Promise<int>.Deferred);
            var deferred3 = default(Promise<int>.Deferred);
            var deferred4 = default(Promise<int>.Deferred);
            var deferred5 = default(Promise<int>.Deferred);
            var cancelationSource0 = default(CancelationSource);
            var cancelationSource1 = default(CancelationSource);
            var cancelationSource2 = default(CancelationSource);
            var cancelationSource3 = default(CancelationSource);
            var cancelationSource4 = default(CancelationSource);
            var cancelationSource5 = default(CancelationSource);

            List<Action> parallelActions = new List<Action>()
            {
#if PROMISE_PROGRESS
                () => deferred0.TryReportProgress(0.5f),
                () => deferred1.TryReportProgress(0.5f),
                () => deferred2.TryReportProgress(0.5f),
                () => deferred3.TryReportProgress(0.5f),
                () => deferred4.TryReportProgress(0.5f),
                () => deferred5.TryReportProgress(0.5f),
#endif
                () => completer0(deferred0, cancelationSource0),
                () => completer1(deferred1, cancelationSource1),
                () => completer2(deferred2, cancelationSource2),
                () => completer3(deferred3, cancelationSource3),
                () => completer4(deferred4, cancelationSource4),
                () => completer5(deferred5, cancelationSource5),
            };

            var helper = ParallelCombineTestHelper.Create(
                combineType,
                () => Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise, deferred4.Promise, deferred5.Promise),
                expectedResolveValue: ValueTuple.Create(1, 2, 3, 4, 5, 6)
            );
            helper.MaybeAddParallelAction(parallelActions);

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsMaybeWithOffsets(
                // setup
                () =>
                {
                    deferred0 = TestHelper.GetNewDeferredT<int>(completeType0, out cancelationSource0);
                    deferred1 = TestHelper.GetNewDeferredT<int>(completeType1, out cancelationSource1);
                    deferred2 = TestHelper.GetNewDeferredT<int>(completeType2, out cancelationSource2);
                    deferred3 = TestHelper.GetNewDeferredT<int>(completeType3, out cancelationSource3);
                    deferred4 = TestHelper.GetNewDeferredT<int>(completeType4, out cancelationSource4);
                    deferred5 = TestHelper.GetNewDeferredT<int>(completeType5, out cancelationSource5);
                    helper.Setup();
                },
                // teardown
                () =>
                {
                    helper.Teardown();
                    cancelationSource0.TryDispose();
                    cancelationSource1.TryDispose();
                    cancelationSource2.TryDispose();
                    cancelationSource3.TryDispose();
                    cancelationSource4.TryDispose();
                    cancelationSource5.TryDispose();
                    Assert.IsTrue(helper.Success);
                },
                parallelActions
            );
        }

        [Test] // Only generate up to 2 CompleteTypes (more takes too long to test)
        public void DeferredsMayBeCompletedWhileTheirPromisesArePassedToMergeConcurrently_T6void(
            [Values] CombineType combineType,
            [Values] CompleteType completeType0,
            [Values(CompleteType.Resolve)] CompleteType completeType1,
            [Values(CompleteType.Resolve)] CompleteType completeType2,
            [Values(CompleteType.Resolve)] CompleteType completeType3,
            [Values(CompleteType.Resolve)] CompleteType completeType4,
            [Values(CompleteType.Resolve)] CompleteType completeType5,
            [MaybeValues(CompleteType.Resolve)] CompleteType completeTypeVoid)
        {
            var completer0 = TestHelper.GetCompleterT(completeType0, 1, rejectValue);
            var completer1 = TestHelper.GetCompleterT(completeType1, 2, rejectValue);
            var completer2 = TestHelper.GetCompleterT(completeType2, 3, rejectValue);
            var completer3 = TestHelper.GetCompleterT(completeType3, 4, rejectValue);
            var completer4 = TestHelper.GetCompleterT(completeType4, 5, rejectValue);
            var completer5 = TestHelper.GetCompleterT(completeType5, 6, rejectValue);
            var completerVoid = TestHelper.GetCompleterVoid(completeTypeVoid, rejectValue);

            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
            var deferred2 = default(Promise<int>.Deferred);
            var deferred3 = default(Promise<int>.Deferred);
            var deferred4 = default(Promise<int>.Deferred);
            var deferred5 = default(Promise<int>.Deferred);
            var deferredVoid = default(Promise.Deferred);
            var cancelationSource0 = default(CancelationSource);
            var cancelationSource1 = default(CancelationSource);
            var cancelationSource2 = default(CancelationSource);
            var cancelationSource3 = default(CancelationSource);
            var cancelationSource4 = default(CancelationSource);
            var cancelationSource5 = default(CancelationSource);
            var cancelationSourceVoid = default(CancelationSource);

            List<Action> parallelActions = new List<Action>()
            {
#if PROMISE_PROGRESS
                () => deferred0.TryReportProgress(0.5f),
                () => deferred1.TryReportProgress(0.5f),
                () => deferred2.TryReportProgress(0.5f),
                () => deferred3.TryReportProgress(0.5f),
                () => deferred4.TryReportProgress(0.5f),
                () => deferred5.TryReportProgress(0.5f),
                () => deferredVoid.TryReportProgress(0.5f),
#endif
                () => completer0(deferred0, cancelationSource0),
                () => completer1(deferred1, cancelationSource1),
                () => completer2(deferred2, cancelationSource2),
                () => completer3(deferred3, cancelationSource3),
                () => completer4(deferred4, cancelationSource4),
                () => completer5(deferred5, cancelationSource5),
                () => completerVoid(deferredVoid, cancelationSourceVoid),
            };

            var helper = ParallelCombineTestHelper.Create(
                combineType,
                () => Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise, deferred4.Promise, deferred5.Promise, deferredVoid.Promise),
                expectedResolveValue: ValueTuple.Create(1, 2, 3, 4, 5, 6)
            );
            helper.MaybeAddParallelAction(parallelActions);

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsMaybeWithOffsets(
                // setup
                () =>
                {
                    deferred0 = TestHelper.GetNewDeferredT<int>(completeType0, out cancelationSource0);
                    deferred1 = TestHelper.GetNewDeferredT<int>(completeType1, out cancelationSource1);
                    deferred2 = TestHelper.GetNewDeferredT<int>(completeType2, out cancelationSource2);
                    deferred3 = TestHelper.GetNewDeferredT<int>(completeType3, out cancelationSource3);
                    deferred4 = TestHelper.GetNewDeferredT<int>(completeType4, out cancelationSource4);
                    deferred5 = TestHelper.GetNewDeferredT<int>(completeType5, out cancelationSource5);
                    deferredVoid = TestHelper.GetNewDeferredVoid(completeTypeVoid, out cancelationSourceVoid);
                    helper.Setup();
                },
                // teardown
                () =>
                {
                    helper.Teardown();
                    cancelationSource0.TryDispose();
                    cancelationSource1.TryDispose();
                    cancelationSource2.TryDispose();
                    cancelationSource3.TryDispose();
                    cancelationSource4.TryDispose();
                    cancelationSource5.TryDispose();
                    cancelationSourceVoid.TryDispose();
                    Assert.IsTrue(helper.Success);
                },
                parallelActions
            );
        }

        [Test] // Only generate up to 2 CompleteTypes (more takes too long to test)
        public void DeferredsMayBeCompletedWhileTheirPromisesArePassedToMergeConcurrently_T7(
            [Values] CombineType combineType,
            [Values] CompleteType completeType0,
            [MaybeValues(CompleteType.Resolve)] CompleteType completeType1,
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
            var cancelationSource0 = default(CancelationSource);
            var cancelationSource1 = default(CancelationSource);
            var cancelationSource2 = default(CancelationSource);
            var cancelationSource3 = default(CancelationSource);
            var cancelationSource4 = default(CancelationSource);
            var cancelationSource5 = default(CancelationSource);
            var cancelationSource6 = default(CancelationSource);

            List<Action> parallelActions = new List<Action>()
            {
#if PROMISE_PROGRESS
                () => deferred0.TryReportProgress(0.5f),
                () => deferred1.TryReportProgress(0.5f),
                () => deferred2.TryReportProgress(0.5f),
                () => deferred3.TryReportProgress(0.5f),
                () => deferred4.TryReportProgress(0.5f),
                () => deferred5.TryReportProgress(0.5f),
                () => deferred6.TryReportProgress(0.5f),
#endif
                () => completer0(deferred0, cancelationSource0),
                () => completer1(deferred1, cancelationSource1),
                () => completer2(deferred2, cancelationSource2),
                () => completer3(deferred3, cancelationSource3),
                () => completer4(deferred4, cancelationSource4),
                () => completer5(deferred5, cancelationSource5),
                () => completer6(deferred6, cancelationSource6),
            };

            var helper = ParallelCombineTestHelper.Create(
                combineType,
                () => Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise, deferred4.Promise, deferred5.Promise, deferred6.Promise),
                expectedResolveValue: ValueTuple.Create(1, 2, 3, 4, 5, 6, 7)
            );
            helper.MaybeAddParallelAction(parallelActions);

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsMaybeWithOffsets(
                // setup
                () =>
                {
                    deferred0 = TestHelper.GetNewDeferredT<int>(completeType0, out cancelationSource0);
                    deferred1 = TestHelper.GetNewDeferredT<int>(completeType1, out cancelationSource1);
                    deferred2 = TestHelper.GetNewDeferredT<int>(completeType2, out cancelationSource2);
                    deferred3 = TestHelper.GetNewDeferredT<int>(completeType3, out cancelationSource3);
                    deferred4 = TestHelper.GetNewDeferredT<int>(completeType4, out cancelationSource4);
                    deferred5 = TestHelper.GetNewDeferredT<int>(completeType5, out cancelationSource5);
                    deferred6 = TestHelper.GetNewDeferredT<int>(completeType6, out cancelationSource6);
                    helper.Setup();
                },
                // teardown
                () =>
                {
                    helper.Teardown();
                    cancelationSource0.TryDispose();
                    cancelationSource1.TryDispose();
                    cancelationSource2.TryDispose();
                    cancelationSource3.TryDispose();
                    cancelationSource4.TryDispose();
                    cancelationSource5.TryDispose();
                    cancelationSource6.TryDispose();
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
            [MaybeValues(CompleteType.Resolve)] CompleteType completeTypeVoid)
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
            var cancelationSource0 = default(CancelationSource);
            var cancelationSource1 = default(CancelationSource);
            var cancelationSource2 = default(CancelationSource);
            var cancelationSource3 = default(CancelationSource);
            var cancelationSource4 = default(CancelationSource);
            var cancelationSource5 = default(CancelationSource);
            var cancelationSource6 = default(CancelationSource);
            var cancelationSourceVoid = default(CancelationSource);

            List<Action> parallelActions = new List<Action>()
            {
#if PROMISE_PROGRESS
                () => deferred0.TryReportProgress(0.5f),
                () => deferred1.TryReportProgress(0.5f),
                () => deferred2.TryReportProgress(0.5f),
                () => deferred3.TryReportProgress(0.5f),
                () => deferred4.TryReportProgress(0.5f),
                () => deferred5.TryReportProgress(0.5f),
                () => deferred6.TryReportProgress(0.5f),
                () => deferredVoid.TryReportProgress(0.5f),
#endif
                () => completer0(deferred0, cancelationSource0),
                () => completer1(deferred1, cancelationSource1),
                () => completer2(deferred2, cancelationSource2),
                () => completer3(deferred3, cancelationSource3),
                () => completer4(deferred4, cancelationSource4),
                () => completer5(deferred5, cancelationSource5),
                () => completer6(deferred6, cancelationSource6),
                () => completerVoid(deferredVoid, cancelationSourceVoid),
            };

            var helper = ParallelCombineTestHelper.Create(
                combineType,
                () => Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise, deferred4.Promise, deferred5.Promise, deferred6.Promise, deferredVoid.Promise),
                expectedResolveValue: ValueTuple.Create(1, 2, 3, 4, 5, 6, 7)
            );
            helper.MaybeAddParallelAction(parallelActions);

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsMaybeWithOffsets(
                // setup
                () =>
                {
                    deferred0 = TestHelper.GetNewDeferredT<int>(completeType0, out cancelationSource0);
                    deferred1 = TestHelper.GetNewDeferredT<int>(completeType1, out cancelationSource1);
                    deferred2 = TestHelper.GetNewDeferredT<int>(completeType2, out cancelationSource2);
                    deferred3 = TestHelper.GetNewDeferredT<int>(completeType3, out cancelationSource3);
                    deferred4 = TestHelper.GetNewDeferredT<int>(completeType4, out cancelationSource4);
                    deferred5 = TestHelper.GetNewDeferredT<int>(completeType5, out cancelationSource5);
                    deferred6 = TestHelper.GetNewDeferredT<int>(completeType6, out cancelationSource6);
                    deferredVoid = TestHelper.GetNewDeferredVoid(completeTypeVoid, out cancelationSourceVoid);
                    helper.Setup();
                },
                // teardown
                () =>
                {
                    helper.Teardown();
                    cancelationSource0.TryDispose();
                    cancelationSource1.TryDispose();
                    cancelationSource2.TryDispose();
                    cancelationSource3.TryDispose();
                    cancelationSource4.TryDispose();
                    cancelationSource5.TryDispose();
                    cancelationSource6.TryDispose();
                    cancelationSourceVoid.TryDispose();
                    Assert.IsTrue(helper.Success);
                },
                parallelActions
            );
        }
    }
}

#endif // !UNITY_WEBGL