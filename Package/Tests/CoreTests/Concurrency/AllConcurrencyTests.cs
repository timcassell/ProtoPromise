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
using System.Linq;

#pragma warning disable 0618 // Type or member is obsolete

namespace ProtoPromiseTests.Concurrency
{
    public class AllConcurrencyTests
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
        public void DeferredsMayBeCompletedAndProgressReportedWhileTheirPromisesArePassedToAllOrProgressSubscribedConcurrently_void0(
            [Values] CombineType combineType,
            [Values] CompleteType completeType0,
            [MaybeValues(CompleteType.Resolve)] CompleteType completeType1)
        {
            var completer0 = TestHelper.GetCompleterVoid(completeType0, rejectValue);
            var completer1 = TestHelper.GetCompleterVoid(completeType1, rejectValue);

            var deferred0 = default(Promise.Deferred);
            var deferred1 = default(Promise.Deferred);
            var cancelationSource0 = default(CancelationSource);
            var cancelationSource1 = default(CancelationSource);

            List<Action> parallelActions = new List<Action>()
            {
#if PROMISE_PROGRESS
                () => deferred0.TryReportProgress(0.5f),
                () => deferred1.TryReportProgress(0.5f),
#endif
                () => completer0(deferred0, cancelationSource0),
                () => completer1(deferred1, cancelationSource1)
            };

            var helper = ParallelCombineTestHelper.Create(
                combineType,
                () => Promise.All(deferred0.Promise, deferred1.Promise)
            );
            helper.MaybeAddParallelAction(parallelActions);

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsMaybeWithOffsets(
                // setup
                () =>
                {
                    deferred0 = TestHelper.GetNewDeferredVoid(completeType0, out cancelationSource0);
                    deferred1 = TestHelper.GetNewDeferredVoid(completeType1, out cancelationSource1);
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
        public void DeferredsMayBeCompletedAndProgressReportedWhileTheirPromisesArePassedToAllOrProgressSubscribedConcurrently_void1(
            [Values] CombineType combineType,
            [Values] CompleteType completeType0,
            [MaybeValues(CompleteType.Resolve)] CompleteType completeType1,
            [Values(CompleteType.Resolve)] CompleteType completeType2)
        {
            var completer0 = TestHelper.GetCompleterVoid(completeType0, rejectValue);
            var completer1 = TestHelper.GetCompleterVoid(completeType1, rejectValue);
            var completer2 = TestHelper.GetCompleterVoid(completeType2, rejectValue);

            var deferred0 = default(Promise.Deferred);
            var deferred1 = default(Promise.Deferred);
            var deferred2 = default(Promise.Deferred);
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
                () => completer2(deferred2, cancelationSource2)
            };

            var helper = ParallelCombineTestHelper.Create(
                combineType,
                () => Promise.All(deferred0.Promise, deferred1.Promise, deferred2.Promise)
            );
            helper.MaybeAddParallelAction(parallelActions);

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsMaybeWithOffsets(
                // setup
                () =>
                {
                    deferred0 = TestHelper.GetNewDeferredVoid(completeType0, out cancelationSource0);
                    deferred1 = TestHelper.GetNewDeferredVoid(completeType1, out cancelationSource1);
                    deferred2 = TestHelper.GetNewDeferredVoid(completeType2, out cancelationSource2);
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
        public void DeferredsMayBeCompletedAndProgressReportedWhileTheirPromisesArePassedToAllOrProgressSubscribedConcurrently_void2(
            [Values] CombineType combineType,
            [Values] CompleteType completeType0,
            [MaybeValues(CompleteType.Resolve)] CompleteType completeType1,
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
                () => completer3(deferred3, cancelationSource3)
            };

            var helper = ParallelCombineTestHelper.Create(
                combineType,
                () => Promise.All(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise)
            );
            helper.MaybeAddParallelAction(parallelActions);

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsMaybeWithOffsets(
                // setup
                () =>
                {
                    deferred0 = TestHelper.GetNewDeferredVoid(completeType0, out cancelationSource0);
                    deferred1 = TestHelper.GetNewDeferredVoid(completeType1, out cancelationSource1);
                    deferred2 = TestHelper.GetNewDeferredVoid(completeType2, out cancelationSource2);
                    deferred3 = TestHelper.GetNewDeferredVoid(completeType3, out cancelationSource3);
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
        public void DeferredsMayBeCompletedAndProgressReportedWhileTheirPromisesArePassedToAllOrProgressSubscribedConcurrently_void3(
            [Values] CombineType combineType,
            [Values] CompleteType completeType0,
            [MaybeValues(CompleteType.Resolve)] CompleteType completeType1,
            [Values(CompleteType.Resolve)] CompleteType completeType2,
            [Values(CompleteType.Resolve)] CompleteType completeType3)
        {
            var completer0 = TestHelper.GetCompleterVoid(completeType0, rejectValue);
            var completer1 = TestHelper.GetCompleterVoid(completeType1, rejectValue);
            var completer2 = TestHelper.GetCompleterVoid(completeType2, rejectValue);
            var completer3 = TestHelper.GetCompleterVoid(completeType3, rejectValue);

            Promise.Deferred[] deferreds = null;
            IEnumerator<Promise> promises = null;
            var cancelationSource0 = default(CancelationSource);
            var cancelationSource1 = default(CancelationSource);
            var cancelationSource2 = default(CancelationSource);
            var cancelationSource3 = default(CancelationSource);

            List<Action> parallelActions = new List<Action>()
            {
#if PROMISE_PROGRESS
                () => deferreds[0].TryReportProgress(0.5f),
                () => deferreds[1].TryReportProgress(0.5f),
                () => deferreds[2].TryReportProgress(0.5f),
                () => deferreds[3].TryReportProgress(0.5f),
#endif
                () => completer0(deferreds[0], cancelationSource0),
                () => completer1(deferreds[1], cancelationSource1),
                () => completer2(deferreds[2], cancelationSource2),
                () => completer3(deferreds[3], cancelationSource3)
            };

            var helper = ParallelCombineTestHelper.Create(
                combineType,
                () => Promise.All(promises)
            );
            helper.MaybeAddParallelAction(parallelActions);

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsMaybeWithOffsets(
                // setup
                () =>
                {
                    deferreds = new Promise.Deferred[]
                    {
                        TestHelper.GetNewDeferredVoid(completeType0, out cancelationSource0),
                        TestHelper.GetNewDeferredVoid(completeType1, out cancelationSource1),
                        TestHelper.GetNewDeferredVoid(completeType2, out cancelationSource2),
                        TestHelper.GetNewDeferredVoid(completeType3, out cancelationSource3)
                    };
                    promises = deferreds.Select(d => d.Promise).GetEnumerator();
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

        [Test]
        public void DeferredsMayBeCompletedAndProgressReportedWhileTheirPromisesArePassedToAllOrProgressSubscribedConcurrently_T0(
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
                () => Promise<int>.All(deferred0.Promise, deferred1.Promise),
                expectedResolveValue: new int[] { 1, 2 }
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
        public void DeferredsMayBeCompletedAndProgressReportedWhileTheirPromisesArePassedToAllOrProgressSubscribedConcurrently_T1(
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
                () => Promise<int>.All(deferred0.Promise, deferred1.Promise, deferred2.Promise),
                expectedResolveValue: new int[3] { 1, 2, 3 }
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
        public void DeferredsMayBeCompletedAndProgressReportedWhileTheirPromisesArePassedToAllOrProgressSubscribedConcurrently_T2(
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
                () => completer3(deferred3, cancelationSource3)
            };

            var helper = ParallelCombineTestHelper.Create(
                combineType,
                () => Promise<int>.All(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise),
                expectedResolveValue: new int[4] { 1, 2, 3, 4 }
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
        public void DeferredsMayBeCompletedAndProgressReportedWhileTheirPromisesArePassedToAllOrProgressSubscribedConcurrently_T3(
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

            Promise<int>.Deferred[] deferreds = null;
            IEnumerator<Promise<int>> promises = null;
            var cancelationSource0 = default(CancelationSource);
            var cancelationSource1 = default(CancelationSource);
            var cancelationSource2 = default(CancelationSource);
            var cancelationSource3 = default(CancelationSource);

            List<Action> parallelActions = new List<Action>()
            {
#if PROMISE_PROGRESS
                () => deferreds[0].TryReportProgress(0.5f),
                () => deferreds[1].TryReportProgress(0.5f),
                () => deferreds[2].TryReportProgress(0.5f),
                () => deferreds[3].TryReportProgress(0.5f),
#endif
                () => completer0(deferreds[0], cancelationSource0),
                () => completer1(deferreds[1], cancelationSource1),
                () => completer2(deferreds[2], cancelationSource2),
                () => completer3(deferreds[3], cancelationSource3)
            };

            var helper = ParallelCombineTestHelper.Create(
                combineType,
                () => Promise<int>.All(promises),
                expectedResolveValue: new int[4] { 1, 2, 3, 4 }
            );
            helper.MaybeAddParallelAction(parallelActions);

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsMaybeWithOffsets(
                // setup
                () =>
                {
                    deferreds = new Promise<int>.Deferred[]
                    {
                        TestHelper.GetNewDeferredT<int>(completeType0, out cancelationSource0),
                        TestHelper.GetNewDeferredT<int>(completeType1, out cancelationSource1),
                        TestHelper.GetNewDeferredT<int>(completeType2, out cancelationSource2),
                        TestHelper.GetNewDeferredT<int>(completeType3, out cancelationSource3)
                    };
                    promises = deferreds.Select(d => d.Promise).GetEnumerator();
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
    }
}

#endif // !UNITY_WEBGL