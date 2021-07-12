#if !PROTO_PROMISE_PROGRESS_DISABLE
#define PROMISE_PROGRESS
#else
#undef PROMISE_PROGRESS
#endif

#if CSHARP_7_OR_LATER
#if PROMISE_PROGRESS

using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace Proto.Promises.Tests.Threading
{
    public class MergeProgressConcurrencyTests
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
        public void DeferredsPassedToMergeMayBeCompletedWhileProgressIsSubscribedConcurrently_T1void(
            [Values] CompleteType completeType0,
            [Values] CompleteType completeTypeVoid)
        {
            // When 2 or more promises are rejected, the remaining rejects are sent to the UncaughtRejectionHandler.
            // So we need to suppress that here and make sure it's correct.
            var currentHandler = Promise.Config.UncaughtRejectionHandler;
            Promise.Config.UncaughtRejectionHandler = e => Assert.AreEqual(rejectValue, e.Value);

            var completer0 = TestHelper.GetCompleterT(completeType0, 1, rejectValue);
            var completerVoid = TestHelper.GetCompleterVoid(completeTypeVoid, rejectValue);

            var deferred0 = default(Promise<int>.Deferred);
            var deferredVoid = default(Promise.Deferred);
            var cancelationSource0 = default(CancelationSource);
            var cancelationSourceVoid = default(CancelationSource);
            var allPromise = default(Promise<int>);
            bool invoked = false;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    deferred0 = TestHelper.GetNewDeferredT<int>(completeType0, out cancelationSource0);
                    deferredVoid = TestHelper.GetNewDeferredVoid(completeTypeVoid, out cancelationSourceVoid);
                    allPromise = Promise.Merge(deferred0.Promise, deferredVoid.Promise);
                    invoked = false;
                },
                // Teardown
                () =>
                {
                    cancelationSource0.TryDispose();
                    cancelationSourceVoid.TryDispose();
                    Promise.Manager.HandleCompletes();
                    Assert.IsTrue(invoked);
                },
                // Parallel actions
                () => completer0(deferred0, cancelationSource0),
                () => completerVoid(deferredVoid, cancelationSourceVoid),
                () => deferred0.TryReportProgress(0.5f),
                () => deferredVoid.TryReportProgress(0.5f),
                () =>
                {
                    allPromise
                        .Progress(v => { }) // Callback might not be invoked if the promise is canceled/rejected.
                        .ContinueWith(r =>
                        {
                            if (r.State == Promise.State.Resolved)
                            {
                                Assert.AreEqual(1, r.Result);
                            }
                            invoked = true;
                        })
                        .Forget();
                }
            );

            Promise.Config.UncaughtRejectionHandler = currentHandler;
        }

        [Test]
        public void DeferredsPassedToMergeMayBeCompletedWhileProgressIsSubscribedConcurrently_T2(
            [Values] CompleteType completeType0,
            [Values] CompleteType completeType1)
        {
            // When 2 or more promises are rejected, the remaining rejects are sent to the UncaughtRejectionHandler.
            // So we need to suppress that here and make sure it's correct.
            var currentHandler = Promise.Config.UncaughtRejectionHandler;
            Promise.Config.UncaughtRejectionHandler = e => Assert.AreEqual(rejectValue, e.Value);

            var completer0 = TestHelper.GetCompleterT(completeType0, 1, rejectValue);
            var completer1 = TestHelper.GetCompleterT(completeType1, 2, rejectValue);

            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
            var cancelationSource0 = default(CancelationSource);
            var cancelationSource1 = default(CancelationSource);
            var allPromise = default(Promise<System.ValueTuple<int, int>>);
            bool invoked = false;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    deferred0 = TestHelper.GetNewDeferredT<int>(completeType0, out cancelationSource0);
                    deferred1 = TestHelper.GetNewDeferredT<int>(completeType1, out cancelationSource1);
                    allPromise = Promise.Merge(deferred0.Promise, deferred1.Promise);
                    invoked = false;
                },
                // Teardown
                () =>
                {
                    cancelationSource0.TryDispose();
                    cancelationSource1.TryDispose();
                    Promise.Manager.HandleCompletes();
                    Assert.IsTrue(invoked);
                },
                // Parallel actions
                () => completer0(deferred0, cancelationSource0),
                () => completer1(deferred1, cancelationSource1),
                () => deferred0.TryReportProgress(0.5f),
                () => deferred1.TryReportProgress(0.5f),
                () =>
                {
                    allPromise
                        .Progress(v => { }) // Callback might not be invoked if the promise is canceled/rejected.
                        .ContinueWith(r =>
                        {
                            if (r.State == Promise.State.Resolved)
                            {
                                var cv = r.Result;
                                Assert.AreEqual(1, cv.Item1);
                                Assert.AreEqual(2, cv.Item2);
                            }
                            invoked = true;
                        })
                        .Forget();
                }
            );

            Promise.Config.UncaughtRejectionHandler = currentHandler;
        }

        [Test] // Only generate up to 2 parameters (more takes too long to test)
        public void DeferredsPassedToMergeMayBeCompletedWhileProgressIsSubscribedConcurrently_T2void(
            [Values] CompleteType completeType0,
            [Values(CompleteType.Resolve)] CompleteType completeType1,
            [Values] CompleteType completeTypeVoid)
        {
            // When 2 or more promises are rejected, the remaining rejects are sent to the UncaughtRejectionHandler.
            // So we need to suppress that here and make sure it's correct.
            var currentHandler = Promise.Config.UncaughtRejectionHandler;
            Promise.Config.UncaughtRejectionHandler = e => Assert.AreEqual(rejectValue, e.Value);

            var completer0 = TestHelper.GetCompleterT(completeType0, 1, rejectValue);
            var completer1 = TestHelper.GetCompleterT(completeType1, 2, rejectValue);
            var completerVoid = TestHelper.GetCompleterVoid(completeTypeVoid, rejectValue);

            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
            var deferredVoid = default(Promise.Deferred);
            var cancelationSource0 = default(CancelationSource);
            var cancelationSource1 = default(CancelationSource);
            var cancelationSourceVoid = default(CancelationSource);
            var allPromise = default(Promise<System.ValueTuple<int, int>>);
            bool invoked = false;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    deferred0 = TestHelper.GetNewDeferredT<int>(completeType0, out cancelationSource0);
                    deferred1 = TestHelper.GetNewDeferredT<int>(completeType1, out cancelationSource1);
                    deferredVoid = TestHelper.GetNewDeferredVoid(completeTypeVoid, out cancelationSourceVoid);
                    allPromise = Promise.Merge(deferred0.Promise, deferred1.Promise, deferredVoid.Promise);
                    invoked = false;
                },
                // Teardown
                () =>
                {
                    cancelationSource0.TryDispose();
                    cancelationSource1.TryDispose();
                    cancelationSourceVoid.TryDispose();
                    cancelationSourceVoid.TryDispose();
                    Promise.Manager.HandleCompletes();
                    Assert.IsTrue(invoked);
                },
                // Parallel actions
                () => completer0(deferred0, cancelationSource0),
                () => completer1(deferred1, cancelationSource1),
                () => completerVoid(deferredVoid, cancelationSourceVoid),
                () => deferred0.TryReportProgress(0.5f),
                () => deferred1.TryReportProgress(0.5f),
                () => deferredVoid.TryReportProgress(0.5f),
                () =>
                {
                    allPromise
                        .Progress(v => { }) // Callback might not be invoked if the promise is canceled/rejected.
                        .ContinueWith(r =>
                        {
                            if (r.State == Promise.State.Resolved)
                            {
                                var cv = r.Result;
                                Assert.AreEqual(1, cv.Item1);
                                Assert.AreEqual(2, cv.Item2);
                            }
                            invoked = true;
                        })
                        .Forget();
                }
            );

            Promise.Config.UncaughtRejectionHandler = currentHandler;
        }

        [Test] // Only generate up to 2 parameters (more takes too long to test)
        public void DeferredsPassedToMergeMayBeCompletedWhileProgressIsSubscribedConcurrently_T3(
            [Values] CompleteType completeType0,
            [Values] CompleteType completeType1,
            [Values(CompleteType.Resolve)] CompleteType completeType2)
        {
            // When 2 or more promises are rejected, the remaining rejects are sent to the UncaughtRejectionHandler.
            // So we need to suppress that here and make sure it's correct.
            var currentHandler = Promise.Config.UncaughtRejectionHandler;
            Promise.Config.UncaughtRejectionHandler = e => Assert.AreEqual(rejectValue, e.Value);

            var completer0 = TestHelper.GetCompleterT(completeType0, 1, rejectValue);
            var completer1 = TestHelper.GetCompleterT(completeType1, 2, rejectValue);
            var completer2 = TestHelper.GetCompleterT(completeType2, 3, rejectValue);

            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
            var deferred2 = default(Promise<int>.Deferred);
            var cancelationSource0 = default(CancelationSource);
            var cancelationSource1 = default(CancelationSource);
            var cancelationSource2 = default(CancelationSource);
            var allPromise = default(Promise<System.ValueTuple<int, int, int>>);
            bool invoked = false;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    deferred0 = TestHelper.GetNewDeferredT<int>(completeType0, out cancelationSource0);
                    deferred1 = TestHelper.GetNewDeferredT<int>(completeType1, out cancelationSource1);
                    deferred2 = TestHelper.GetNewDeferredT<int>(completeType2, out cancelationSource2);
                    allPromise = Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise);
                    invoked = false;
                },
                // Teardown
                () =>
                {
                    cancelationSource0.TryDispose();
                    cancelationSource1.TryDispose();
                    cancelationSource2.TryDispose();
                    Promise.Manager.HandleCompletes();
                    Assert.IsTrue(invoked);
                },
                // Parallel actions
                () => completer0(deferred0, cancelationSource0),
                () => completer1(deferred1, cancelationSource1),
                () => completer2(deferred2, cancelationSource2),
                () => deferred0.TryReportProgress(0.5f),
                () => deferred1.TryReportProgress(0.5f),
                () => deferred2.TryReportProgress(0.5f),
                () =>
                {
                    allPromise
                        .Progress(v => { }) // Callback might not be invoked if the promise is canceled/rejected.
                        .ContinueWith(r =>
                        {
                            if (r.State == Promise.State.Resolved)
                            {
                                var cv = r.Result;
                                Assert.AreEqual(1, cv.Item1);
                                Assert.AreEqual(2, cv.Item2);
                                Assert.AreEqual(3, cv.Item3);
                            }
                            invoked = true;
                        })
                        .Forget();
                }
            );

            Promise.Config.UncaughtRejectionHandler = currentHandler;
        }

        [Test] // Only generate up to 2 parameters (more takes too long to test)
        public void DeferredsPassedToMergeMayBeCompletedWhileProgressIsSubscribedConcurrently_T3void(
            [Values] CompleteType completeType0,
            [Values(CompleteType.Resolve)] CompleteType completeType1,
            [Values(CompleteType.Resolve)] CompleteType completeType2,
            [Values] CompleteType completeTypeVoid)
        {
            // When 2 or more promises are rejected, the remaining rejects are sent to the UncaughtRejectionHandler.
            // So we need to suppress that here and make sure it's correct.
            var currentHandler = Promise.Config.UncaughtRejectionHandler;
            Promise.Config.UncaughtRejectionHandler = e => Assert.AreEqual(rejectValue, e.Value);

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
            var allPromise = default(Promise<System.ValueTuple<int, int, int>>);
            bool invoked = false;

            var threadHelper = new ThreadHelper();
            // Don't use WithOffsets because there are too many actions.
            threadHelper.ExecuteParallelActions(ThreadHelper.multiExecutionCount,
                // Setup
                () =>
                {
                    deferred0 = TestHelper.GetNewDeferredT<int>(completeType0, out cancelationSource0);
                    deferred1 = TestHelper.GetNewDeferredT<int>(completeType1, out cancelationSource1);
                    deferred2 = TestHelper.GetNewDeferredT<int>(completeType2, out cancelationSource2);
                    deferredVoid = TestHelper.GetNewDeferredVoid(completeTypeVoid, out cancelationSourceVoid);
                    allPromise = Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferredVoid.Promise);
                    invoked = false;
                },
                // Teardown
                () =>
                {
                    cancelationSource0.TryDispose();
                    cancelationSource1.TryDispose();
                    cancelationSource2.TryDispose();
                    cancelationSourceVoid.TryDispose();
                    Promise.Manager.HandleCompletes();
                    Assert.IsTrue(invoked);
                },
                // Parallel actions
                () => completer0(deferred0, cancelationSource0),
                () => completer1(deferred1, cancelationSource1),
                () => completer2(deferred2, cancelationSource2),
                () => completerVoid(deferredVoid, cancelationSourceVoid),
                () => deferred0.TryReportProgress(0.5f),
                () => deferred1.TryReportProgress(0.5f),
                () => deferred2.TryReportProgress(0.5f),
                () => deferredVoid.TryReportProgress(0.5f),
                () =>
                {
                    allPromise
                        .Progress(v => { }) // Callback might not be invoked if the promise is canceled/rejected.
                        .ContinueWith(r =>
                        {
                            if (r.State == Promise.State.Resolved)
                            {
                                var cv = r.Result;
                                Assert.AreEqual(1, cv.Item1);
                                Assert.AreEqual(2, cv.Item2);
                                Assert.AreEqual(3, cv.Item3);
                            }
                            invoked = true;
                        })
                        .Forget();
                }
            );

            Promise.Config.UncaughtRejectionHandler = currentHandler;
        }

        [Test] // Only generate up to 2 parameters (more takes too long to test)
        public void DeferredsPassedToMergeMayBeCompletedWhileProgressIsSubscribedConcurrently_T4(
            [Values] CompleteType completeType0,
            [Values] CompleteType completeType1,
            [Values(CompleteType.Resolve)] CompleteType completeType2,
            [Values(CompleteType.Resolve)] CompleteType completeType3)
        {
            // When 2 or more promises are rejected, the remaining rejects are sent to the UncaughtRejectionHandler.
            // So we need to suppress that here and make sure it's correct.
            var currentHandler = Promise.Config.UncaughtRejectionHandler;
            Promise.Config.UncaughtRejectionHandler = e => Assert.AreEqual(rejectValue, e.Value);

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
            var allPromise = default(Promise<System.ValueTuple<int, int, int, int>>);
            bool invoked = false;

            var threadHelper = new ThreadHelper();
            // Don't use WithOffsets because there are too many actions.
            threadHelper.ExecuteParallelActions(ThreadHelper.multiExecutionCount,
                // Setup
                () =>
                {
                    deferred0 = TestHelper.GetNewDeferredT<int>(completeType0, out cancelationSource0);
                    deferred1 = TestHelper.GetNewDeferredT<int>(completeType1, out cancelationSource1);
                    deferred2 = TestHelper.GetNewDeferredT<int>(completeType2, out cancelationSource2);
                    deferred3 = TestHelper.GetNewDeferredT<int>(completeType3, out cancelationSource3);
                    allPromise = Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise);
                    invoked = false;
                },
                // Teardown
                () =>
                {
                    cancelationSource0.TryDispose();
                    cancelationSource1.TryDispose();
                    cancelationSource2.TryDispose();
                    cancelationSource3.TryDispose();
                    Promise.Manager.HandleCompletes();
                    Assert.IsTrue(invoked);
                },
                // Parallel actions
                () => completer0(deferred0, cancelationSource0),
                () => completer1(deferred1, cancelationSource1),
                () => completer2(deferred2, cancelationSource2),
                () => completer3(deferred3, cancelationSource3),
                () => deferred0.TryReportProgress(0.5f),
                () => deferred1.TryReportProgress(0.5f),
                () => deferred2.TryReportProgress(0.5f),
                () => deferred3.TryReportProgress(0.5f),
                () =>
                {
                    allPromise
                        .Progress(v => { }) // Callback might not be invoked if the promise is canceled/rejected.
                        .ContinueWith(r =>
                        {
                            if (r.State == Promise.State.Resolved)
                            {
                                var cv = r.Result;
                                Assert.AreEqual(1, cv.Item1);
                                Assert.AreEqual(2, cv.Item2);
                                Assert.AreEqual(3, cv.Item3);
                                Assert.AreEqual(4, cv.Item4);
                            }
                            invoked = true;
                        })
                        .Forget();
                }
            );

            Promise.Config.UncaughtRejectionHandler = currentHandler;
        }

        [Test] // Only generate up to 2 parameters (more takes too long to test)
        public void DeferredsPassedToMergeMayBeCompletedWhileProgressIsSubscribedConcurrently_T4void(
            [Values] CompleteType completeType0,
            [Values(CompleteType.Resolve)] CompleteType completeType1,
            [Values(CompleteType.Resolve)] CompleteType completeType2,
            [Values(CompleteType.Resolve)] CompleteType completeType3,
            [Values] CompleteType completeTypeVoid)
        {
            // When 2 or more promises are rejected, the remaining rejects are sent to the UncaughtRejectionHandler.
            // So we need to suppress that here and make sure it's correct.
            var currentHandler = Promise.Config.UncaughtRejectionHandler;
            Promise.Config.UncaughtRejectionHandler = e => Assert.AreEqual(rejectValue, e.Value);

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
            var allPromise = default(Promise<System.ValueTuple<int, int, int, int>>);
            bool invoked = false;

            var threadHelper = new ThreadHelper();
            // Don't use WithOffsets because there are too many actions.
            threadHelper.ExecuteParallelActions(ThreadHelper.multiExecutionCount,
                // Setup
                () =>
                {
                    deferred0 = TestHelper.GetNewDeferredT<int>(completeType0, out cancelationSource0);
                    deferred1 = TestHelper.GetNewDeferredT<int>(completeType1, out cancelationSource1);
                    deferred2 = TestHelper.GetNewDeferredT<int>(completeType2, out cancelationSource2);
                    deferred3 = TestHelper.GetNewDeferredT<int>(completeType3, out cancelationSource3);
                    deferredVoid = TestHelper.GetNewDeferredVoid(completeTypeVoid, out cancelationSourceVoid);
                    allPromise = Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise, deferredVoid.Promise);
                    invoked = false;
                },
                // Teardown
                () =>
                {
                    cancelationSource0.TryDispose();
                    cancelationSource1.TryDispose();
                    cancelationSource2.TryDispose();
                    cancelationSource3.TryDispose();
                    cancelationSourceVoid.TryDispose();
                    Promise.Manager.HandleCompletes();
                    Assert.IsTrue(invoked);
                },
                // Parallel actions
                () => completer0(deferred0, cancelationSource0),
                () => completer1(deferred1, cancelationSource1),
                () => completer2(deferred2, cancelationSource2),
                () => completer3(deferred3, cancelationSource3),
                () => completerVoid(deferredVoid, cancelationSourceVoid),
                () => deferred0.TryReportProgress(0.5f),
                () => deferred1.TryReportProgress(0.5f),
                () => deferred2.TryReportProgress(0.5f),
                () => deferred3.TryReportProgress(0.5f),
                () => deferredVoid.TryReportProgress(0.5f),
                () =>
                {
                    allPromise
                        .Progress(v => { }) // Callback might not be invoked if the promise is canceled/rejected.
                        .ContinueWith(r =>
                        {
                            if (r.State == Promise.State.Resolved)
                            {
                                var cv = r.Result;
                                Assert.AreEqual(1, cv.Item1);
                                Assert.AreEqual(2, cv.Item2);
                                Assert.AreEqual(3, cv.Item3);
                                Assert.AreEqual(4, cv.Item4);
                            }
                            invoked = true;
                        })
                        .Forget();
                }
            );

            Promise.Config.UncaughtRejectionHandler = currentHandler;
        }

        [Test] // Only generate up to 2 parameters (more takes too long to test)
        public void DeferredsPassedToMergeMayBeCompletedWhileProgressIsSubscribedConcurrently_T5(
            [Values] CompleteType completeType0,
            [Values] CompleteType completeType1,
            [Values(CompleteType.Resolve)] CompleteType completeType2,
            [Values(CompleteType.Resolve)] CompleteType completeType3,
            [Values(CompleteType.Resolve)] CompleteType completeType4)
        {
            // When 2 or more promises are rejected, the remaining rejects are sent to the UncaughtRejectionHandler.
            // So we need to suppress that here and make sure it's correct.
            var currentHandler = Promise.Config.UncaughtRejectionHandler;
            Promise.Config.UncaughtRejectionHandler = e => Assert.AreEqual(rejectValue, e.Value);

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
            var allPromise = default(Promise<System.ValueTuple<int, int, int, int, int>>);
            bool invoked = false;

            var threadHelper = new ThreadHelper();
            // Don't use WithOffsets because there are too many actions.
            threadHelper.ExecuteParallelActions(ThreadHelper.multiExecutionCount,
                // Setup
                () =>
                {
                    deferred0 = TestHelper.GetNewDeferredT<int>(completeType0, out cancelationSource0);
                    deferred1 = TestHelper.GetNewDeferredT<int>(completeType1, out cancelationSource1);
                    deferred2 = TestHelper.GetNewDeferredT<int>(completeType2, out cancelationSource2);
                    deferred3 = TestHelper.GetNewDeferredT<int>(completeType3, out cancelationSource3);
                    deferred4 = TestHelper.GetNewDeferredT<int>(completeType4, out cancelationSource4);
                    allPromise = Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise, deferred4.Promise);
                    invoked = false;
                },
                // Teardown
                () =>
                {
                    cancelationSource0.TryDispose();
                    cancelationSource1.TryDispose();
                    cancelationSource2.TryDispose();
                    cancelationSource3.TryDispose();
                    cancelationSource4.TryDispose();
                    Promise.Manager.HandleCompletes();
                    Assert.IsTrue(invoked);
                },
                // Parallel actions
                () => completer0(deferred0, cancelationSource0),
                () => completer1(deferred1, cancelationSource1),
                () => completer2(deferred2, cancelationSource2),
                () => completer3(deferred3, cancelationSource3),
                () => completer4(deferred4, cancelationSource4),
                () => deferred0.TryReportProgress(0.5f),
                () => deferred1.TryReportProgress(0.5f),
                () => deferred2.TryReportProgress(0.5f),
                () => deferred3.TryReportProgress(0.5f),
                () => deferred4.TryReportProgress(0.5f),
                () =>
                {
                    allPromise
                        .Progress(v => { }) // Callback might not be invoked if the promise is canceled/rejected.
                        .ContinueWith(r =>
                        {
                            if (r.State == Promise.State.Resolved)
                            {
                                var cv = r.Result;
                                Assert.AreEqual(1, cv.Item1);
                                Assert.AreEqual(2, cv.Item2);
                                Assert.AreEqual(3, cv.Item3);
                                Assert.AreEqual(4, cv.Item4);
                                Assert.AreEqual(5, cv.Item5);
                            }
                            invoked = true;
                        })
                        .Forget();
                }
            );

            Promise.Config.UncaughtRejectionHandler = currentHandler;
        }

        [Test] // Only generate up to 2 parameters (more takes too long to test)
        public void DeferredsPassedToMergeMayBeCompletedWhileProgressIsSubscribedConcurrently_T5void(
            [Values] CompleteType completeType0,
            [Values(CompleteType.Resolve)] CompleteType completeType1,
            [Values(CompleteType.Resolve)] CompleteType completeType2,
            [Values(CompleteType.Resolve)] CompleteType completeType3,
            [Values(CompleteType.Resolve)] CompleteType completeType4,
            [Values] CompleteType completeTypeVoid)
        {
            // When 2 or more promises are rejected, the remaining rejects are sent to the UncaughtRejectionHandler.
            // So we need to suppress that here and make sure it's correct.
            var currentHandler = Promise.Config.UncaughtRejectionHandler;
            Promise.Config.UncaughtRejectionHandler = e => Assert.AreEqual(rejectValue, e.Value);

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
            var allPromise = default(Promise<System.ValueTuple<int, int, int, int, int>>);
            bool invoked = false;

            var threadHelper = new ThreadHelper();
            // Don't use WithOffsets because there are too many actions.
            threadHelper.ExecuteParallelActions(ThreadHelper.multiExecutionCount,
                // Setup
                () =>
                {
                    deferred0 = TestHelper.GetNewDeferredT<int>(completeType0, out cancelationSource0);
                    deferred1 = TestHelper.GetNewDeferredT<int>(completeType1, out cancelationSource1);
                    deferred2 = TestHelper.GetNewDeferredT<int>(completeType2, out cancelationSource2);
                    deferred3 = TestHelper.GetNewDeferredT<int>(completeType3, out cancelationSource3);
                    deferred4 = TestHelper.GetNewDeferredT<int>(completeType4, out cancelationSource4);
                    deferredVoid = TestHelper.GetNewDeferredVoid(completeTypeVoid, out cancelationSourceVoid);
                    allPromise = Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise, deferred4.Promise, deferredVoid.Promise);
                    invoked = false;
                },
                // Teardown
                () =>
                {
                    cancelationSource0.TryDispose();
                    cancelationSource1.TryDispose();
                    cancelationSource2.TryDispose();
                    cancelationSource3.TryDispose();
                    cancelationSource4.TryDispose();
                    cancelationSourceVoid.TryDispose();
                    Promise.Manager.HandleCompletes();
                    Assert.IsTrue(invoked);
                },
                // Parallel actions
                () => completer0(deferred0, cancelationSource0),
                () => completer1(deferred1, cancelationSource1),
                () => completer2(deferred2, cancelationSource2),
                () => completer3(deferred3, cancelationSource3),
                () => completer4(deferred4, cancelationSource4),
                () => completerVoid(deferredVoid, cancelationSourceVoid),
                () => deferred0.TryReportProgress(0.5f),
                () => deferred1.TryReportProgress(0.5f),
                () => deferred2.TryReportProgress(0.5f),
                () => deferred3.TryReportProgress(0.5f),
                () => deferred4.TryReportProgress(0.5f),
                () => deferredVoid.TryReportProgress(0.5f),
                () =>
                {
                    allPromise
                        .Progress(v => { }) // Callback might not be invoked if the promise is canceled/rejected.
                        .ContinueWith(r =>
                        {
                            if (r.State == Promise.State.Resolved)
                            {
                                var cv = r.Result;
                                Assert.AreEqual(1, cv.Item1);
                                Assert.AreEqual(2, cv.Item2);
                                Assert.AreEqual(3, cv.Item3);
                                Assert.AreEqual(4, cv.Item4);
                                Assert.AreEqual(5, cv.Item5);
                            }
                            invoked = true;
                        })
                        .Forget();
                }
            );

            Promise.Config.UncaughtRejectionHandler = currentHandler;
        }

        [Test] // Only generate up to 2 parameters (more takes too long to test)
        public void DeferredsPassedToMergeMayBeCompletedWhileProgressIsSubscribedConcurrently_T6(
            [Values] CompleteType completeType0,
            [Values] CompleteType completeType1,
            [Values(CompleteType.Resolve)] CompleteType completeType2,
            [Values(CompleteType.Resolve)] CompleteType completeType3,
            [Values(CompleteType.Resolve)] CompleteType completeType4,
            [Values(CompleteType.Resolve)] CompleteType completeType5)
        {
            // When 2 or more promises are rejected, the remaining rejects are sent to the UncaughtRejectionHandler.
            // So we need to suppress that here and make sure it's correct.
            var currentHandler = Promise.Config.UncaughtRejectionHandler;
            Promise.Config.UncaughtRejectionHandler = e => Assert.AreEqual(rejectValue, e.Value);

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
            var allPromise = default(Promise<System.ValueTuple<int, int, int, int, int, int>>);
            bool invoked = false;

            var threadHelper = new ThreadHelper();
            // Don't use WithOffsets because there are too many actions.
            threadHelper.ExecuteParallelActions(ThreadHelper.multiExecutionCount,
                // Setup
                () =>
                {
                    deferred0 = TestHelper.GetNewDeferredT<int>(completeType0, out cancelationSource0);
                    deferred1 = TestHelper.GetNewDeferredT<int>(completeType1, out cancelationSource1);
                    deferred2 = TestHelper.GetNewDeferredT<int>(completeType2, out cancelationSource2);
                    deferred3 = TestHelper.GetNewDeferredT<int>(completeType3, out cancelationSource3);
                    deferred4 = TestHelper.GetNewDeferredT<int>(completeType4, out cancelationSource4);
                    deferred5 = TestHelper.GetNewDeferredT<int>(completeType5, out cancelationSource5);
                    allPromise = Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise, deferred4.Promise, deferred5.Promise);
                    invoked = false;
                },
                // Teardown
                () =>
                {
                    cancelationSource0.TryDispose();
                    cancelationSource1.TryDispose();
                    cancelationSource2.TryDispose();
                    cancelationSource3.TryDispose();
                    cancelationSource4.TryDispose();
                    cancelationSource5.TryDispose();
                    Promise.Manager.HandleCompletes();
                    Assert.IsTrue(invoked);
                },
                // Parallel actions
                () => completer0(deferred0, cancelationSource0),
                () => completer1(deferred1, cancelationSource1),
                () => completer2(deferred2, cancelationSource2),
                () => completer3(deferred3, cancelationSource3),
                () => completer4(deferred4, cancelationSource4),
                () => completer5(deferred5, cancelationSource5),
                () => deferred0.TryReportProgress(0.5f),
                () => deferred1.TryReportProgress(0.5f),
                () => deferred2.TryReportProgress(0.5f),
                () => deferred3.TryReportProgress(0.5f),
                () => deferred4.TryReportProgress(0.5f),
                () => deferred5.TryReportProgress(0.5f),
                () =>
                {
                    allPromise
                        .Progress(v => { }) // Callback might not be invoked if the promise is canceled/rejected.
                        .ContinueWith(r =>
                        {
                            if (r.State == Promise.State.Resolved)
                            {
                                var cv = r.Result;
                                Assert.AreEqual(1, cv.Item1);
                                Assert.AreEqual(2, cv.Item2);
                                Assert.AreEqual(3, cv.Item3);
                                Assert.AreEqual(4, cv.Item4);
                                Assert.AreEqual(5, cv.Item5);
                                Assert.AreEqual(6, cv.Item6);
                            }
                            invoked = true;
                        })
                        .Forget();
                }
            );

            Promise.Config.UncaughtRejectionHandler = currentHandler;
        }

        [Test] // Only generate up to 2 parameters (more takes too long to test)
        public void DeferredsPassedToMergeMayBeCompletedWhileProgressIsSubscribedConcurrently_T6void(
            [Values] CompleteType completeType0,
            [Values(CompleteType.Resolve)] CompleteType completeType1,
            [Values(CompleteType.Resolve)] CompleteType completeType2,
            [Values(CompleteType.Resolve)] CompleteType completeType3,
            [Values(CompleteType.Resolve)] CompleteType completeType4,
            [Values(CompleteType.Resolve)] CompleteType completeType5,
            [Values] CompleteType completeTypeVoid)
        {
            // When 2 or more promises are rejected, the remaining rejects are sent to the UncaughtRejectionHandler.
            // So we need to suppress that here and make sure it's correct.
            var currentHandler = Promise.Config.UncaughtRejectionHandler;
            Promise.Config.UncaughtRejectionHandler = e => Assert.AreEqual(rejectValue, e.Value);

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
            var allPromise = default(Promise<System.ValueTuple<int, int, int, int, int, int>>);
            bool invoked = false;

            var threadHelper = new ThreadHelper();
            // Don't use WithOffsets because there are too many actions.
            threadHelper.ExecuteParallelActions(ThreadHelper.multiExecutionCount,
                // Setup
                () =>
                {
                    deferred0 = TestHelper.GetNewDeferredT<int>(completeType0, out cancelationSource0);
                    deferred1 = TestHelper.GetNewDeferredT<int>(completeType1, out cancelationSource1);
                    deferred2 = TestHelper.GetNewDeferredT<int>(completeType2, out cancelationSource2);
                    deferred3 = TestHelper.GetNewDeferredT<int>(completeType3, out cancelationSource3);
                    deferred4 = TestHelper.GetNewDeferredT<int>(completeType4, out cancelationSource4);
                    deferred5 = TestHelper.GetNewDeferredT<int>(completeType5, out cancelationSource5);
                    deferredVoid = TestHelper.GetNewDeferredVoid(completeTypeVoid, out cancelationSourceVoid);
                    allPromise = Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise, deferred4.Promise, deferred5.Promise, deferredVoid.Promise);
                    invoked = false;
                },
                // Teardown
                () =>
                {
                    cancelationSource0.TryDispose();
                    cancelationSource1.TryDispose();
                    cancelationSource2.TryDispose();
                    cancelationSource3.TryDispose();
                    cancelationSource4.TryDispose();
                    cancelationSource5.TryDispose();
                    cancelationSourceVoid.TryDispose();
                    Promise.Manager.HandleCompletes();
                    Assert.IsTrue(invoked);
                },
                // Parallel actions
                () => completer0(deferred0, cancelationSource0),
                () => completer1(deferred1, cancelationSource1),
                () => completer2(deferred2, cancelationSource2),
                () => completer3(deferred3, cancelationSource3),
                () => completer4(deferred4, cancelationSource4),
                () => completer5(deferred5, cancelationSource5),
                () => completerVoid(deferredVoid, cancelationSourceVoid),
                () => deferred0.TryReportProgress(0.5f),
                () => deferred1.TryReportProgress(0.5f),
                () => deferred2.TryReportProgress(0.5f),
                () => deferred3.TryReportProgress(0.5f),
                () => deferred4.TryReportProgress(0.5f),
                () => deferred5.TryReportProgress(0.5f),
                () => deferredVoid.TryReportProgress(0.5f),
                () =>
                {
                    allPromise
                        .Progress(v => { }) // Callback might not be invoked if the promise is canceled/rejected.
                        .ContinueWith(r =>
                        {
                            if (r.State == Promise.State.Resolved)
                            {
                                var cv = r.Result;
                                Assert.AreEqual(1, cv.Item1);
                                Assert.AreEqual(2, cv.Item2);
                                Assert.AreEqual(3, cv.Item3);
                                Assert.AreEqual(4, cv.Item4);
                                Assert.AreEqual(5, cv.Item5);
                                Assert.AreEqual(6, cv.Item6);
                            }
                            invoked = true;
                        })
                        .Forget();
                }
            );

            Promise.Config.UncaughtRejectionHandler = currentHandler;
        }

        [Test] // Only generate up to 2 parameters (more takes too long to test)
        public void DeferredsPassedToMergeMayBeCompletedWhileProgressIsSubscribedConcurrently_T7(
            [Values] CompleteType completeType0,
            [Values] CompleteType completeType1,
            [Values(CompleteType.Resolve)] CompleteType completeType2,
            [Values(CompleteType.Resolve)] CompleteType completeType3,
            [Values(CompleteType.Resolve)] CompleteType completeType4,
            [Values(CompleteType.Resolve)] CompleteType completeType5,
            [Values(CompleteType.Resolve)] CompleteType completeType6)
        {
            // When 2 or more promises are rejected, the remaining rejects are sent to the UncaughtRejectionHandler.
            // So we need to suppress that here and make sure it's correct.
            var currentHandler = Promise.Config.UncaughtRejectionHandler;
            Promise.Config.UncaughtRejectionHandler = e => Assert.AreEqual(rejectValue, e.Value);

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
            var allPromise = default(Promise<System.ValueTuple<int, int, int, int, int, int, int>>);
            bool invoked = false;

            var threadHelper = new ThreadHelper();
            // Don't use WithOffsets because there are too many actions.
            threadHelper.ExecuteParallelActions(ThreadHelper.multiExecutionCount,
                // Setup
                () =>
                {
                    deferred0 = TestHelper.GetNewDeferredT<int>(completeType0, out cancelationSource0);
                    deferred1 = TestHelper.GetNewDeferredT<int>(completeType1, out cancelationSource1);
                    deferred2 = TestHelper.GetNewDeferredT<int>(completeType2, out cancelationSource2);
                    deferred3 = TestHelper.GetNewDeferredT<int>(completeType3, out cancelationSource3);
                    deferred4 = TestHelper.GetNewDeferredT<int>(completeType4, out cancelationSource4);
                    deferred5 = TestHelper.GetNewDeferredT<int>(completeType5, out cancelationSource5);
                    deferred6 = TestHelper.GetNewDeferredT<int>(completeType6, out cancelationSource6);
                    allPromise = Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise, deferred4.Promise, deferred5.Promise, deferred6.Promise);
                    invoked = false;
                },
                // Teardown
                () =>
                {
                    cancelationSource0.TryDispose();
                    cancelationSource1.TryDispose();
                    cancelationSource2.TryDispose();
                    cancelationSource3.TryDispose();
                    cancelationSource4.TryDispose();
                    cancelationSource5.TryDispose();
                    cancelationSource6.TryDispose();
                    Promise.Manager.HandleCompletes();
                    Assert.IsTrue(invoked);
                },
                // Parallel actions
                () => completer0(deferred0, cancelationSource0),
                () => completer1(deferred1, cancelationSource1),
                () => completer2(deferred2, cancelationSource2),
                () => completer3(deferred3, cancelationSource3),
                () => completer4(deferred4, cancelationSource4),
                () => completer5(deferred5, cancelationSource5),
                () => completer6(deferred6, cancelationSource6),
                () => deferred0.TryReportProgress(0.5f),
                () => deferred1.TryReportProgress(0.5f),
                () => deferred2.TryReportProgress(0.5f),
                () => deferred3.TryReportProgress(0.5f),
                () => deferred4.TryReportProgress(0.5f),
                () => deferred5.TryReportProgress(0.5f),
                () => deferred6.TryReportProgress(0.5f),
                () =>
                {
                    allPromise
                        .Progress(v => { }) // Callback might not be invoked if the promise is canceled/rejected.
                        .ContinueWith(r =>
                        {
                            if (r.State == Promise.State.Resolved)
                            {
                                var cv = r.Result;
                                Assert.AreEqual(1, cv.Item1);
                                Assert.AreEqual(2, cv.Item2);
                                Assert.AreEqual(3, cv.Item3);
                                Assert.AreEqual(4, cv.Item4);
                                Assert.AreEqual(5, cv.Item5);
                                Assert.AreEqual(6, cv.Item6);
                                Assert.AreEqual(7, cv.Item7);
                            }
                            invoked = true;
                        })
                        .Forget();
                }
            );

            Promise.Config.UncaughtRejectionHandler = currentHandler;
        }

        [Test] // Only generate up to 2 parameters (more takes too long to test)
        public void DeferredsPassedToMergeMayBeCompletedWhileProgressIsSubscribedConcurrently_T7void(
            [Values] CompleteType completeType0,
            [Values(CompleteType.Resolve)] CompleteType completeType1,
            [Values(CompleteType.Resolve)] CompleteType completeType2,
            [Values(CompleteType.Resolve)] CompleteType completeType3,
            [Values(CompleteType.Resolve)] CompleteType completeType4,
            [Values(CompleteType.Resolve)] CompleteType completeType5,
            [Values(CompleteType.Resolve)] CompleteType completeType6,
            [Values] CompleteType completeTypeVoid)
        {
            // When 2 or more promises are rejected, the remaining rejects are sent to the UncaughtRejectionHandler.
            // So we need to suppress that here and make sure it's correct.
            var currentHandler = Promise.Config.UncaughtRejectionHandler;
            Promise.Config.UncaughtRejectionHandler = e => Assert.AreEqual(rejectValue, e.Value);

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
            var allPromise = default(Promise<System.ValueTuple<int, int, int, int, int, int, int>>);
            bool invoked = false;

            var threadHelper = new ThreadHelper();
            // Don't use WithOffsets because there are too many actions.
            threadHelper.ExecuteParallelActions(ThreadHelper.multiExecutionCount,
                // Setup
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
                    allPromise = Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise, deferred4.Promise, deferred5.Promise, deferred6.Promise, deferredVoid.Promise);
                    invoked = false;
                },
                // Teardown
                () =>
                {
                    cancelationSource0.TryDispose();
                    cancelationSource1.TryDispose();
                    cancelationSource2.TryDispose();
                    cancelationSource3.TryDispose();
                    cancelationSource4.TryDispose();
                    cancelationSource5.TryDispose();
                    cancelationSource6.TryDispose();
                    cancelationSourceVoid.TryDispose();
                    Promise.Manager.HandleCompletes();
                    Assert.IsTrue(invoked);
                },
                // Parallel actions
                () => completer0(deferred0, cancelationSource0),
                () => completer1(deferred1, cancelationSource1),
                () => completer2(deferred2, cancelationSource2),
                () => completer3(deferred3, cancelationSource3),
                () => completer4(deferred4, cancelationSource4),
                () => completer5(deferred5, cancelationSource5),
                () => completer6(deferred6, cancelationSource6),
                () => completerVoid(deferredVoid, cancelationSourceVoid),
                () => deferred0.TryReportProgress(0.5f),
                () => deferred1.TryReportProgress(0.5f),
                () => deferred2.TryReportProgress(0.5f),
                () => deferred3.TryReportProgress(0.5f),
                () => deferred4.TryReportProgress(0.5f),
                () => deferred5.TryReportProgress(0.5f),
                () => deferred6.TryReportProgress(0.5f),
                () => deferredVoid.TryReportProgress(0.5f),
                () =>
                {
                    allPromise
                        .Progress(v => { }) // Callback might not be invoked if the promise is canceled/rejected.
                        .ContinueWith(r =>
                        {
                            if (r.State == Promise.State.Resolved)
                            {
                                var cv = r.Result;
                                Assert.AreEqual(1, cv.Item1);
                                Assert.AreEqual(2, cv.Item2);
                                Assert.AreEqual(3, cv.Item3);
                                Assert.AreEqual(4, cv.Item4);
                                Assert.AreEqual(5, cv.Item5);
                                Assert.AreEqual(6, cv.Item6);
                                Assert.AreEqual(7, cv.Item7);
                            }
                            invoked = true;
                        })
                        .Forget();
                }
            );

            Promise.Config.UncaughtRejectionHandler = currentHandler;
        }

        [Test]
        public void DeferredsPassedToMergeMayBeCompletedConcurrentlyAfterProgressIsSubscribed_T1void(
            [Values] CompleteType completeType0,
            [Values] CompleteType completeTypeVoid)
        {
            // When 2 or more promises are rejected, the remaining rejects are sent to the UncaughtRejectionHandler.
            // So we need to suppress that here and make sure it's correct.
            var currentHandler = Promise.Config.UncaughtRejectionHandler;
            Promise.Config.UncaughtRejectionHandler = e => Assert.AreEqual(rejectValue, e.Value);

            var completer0 = TestHelper.GetCompleterT(completeType0, 1, rejectValue);
            var completerVoid = TestHelper.GetCompleterVoid(completeTypeVoid, rejectValue);

            var deferred0 = default(Promise<int>.Deferred);
            var deferredVoid = default(Promise.Deferred);
            var cancelationSource0 = default(CancelationSource);
            var cancelationSourceVoid = default(CancelationSource);
            bool invoked = false;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    deferred0 = TestHelper.GetNewDeferredT<int>(completeType0, out cancelationSource0);
                    deferredVoid = TestHelper.GetNewDeferredVoid(completeTypeVoid, out cancelationSourceVoid);
                    invoked = false;
                    Promise.Merge(deferred0.Promise, deferredVoid.Promise)
                        .Progress(v => { }) // Callback might not be invoked if the promise is canceled/rejected.
                        .ContinueWith(r =>
                        {
                            if (r.State == Promise.State.Resolved)
                            {
                                Assert.AreEqual(1, r.Result);
                            }
                            invoked = true;
                        })
                        .Forget();
                },
                // Teardown
                () =>
                {
                    cancelationSource0.TryDispose();
                    cancelationSourceVoid.TryDispose();
                    Promise.Manager.HandleCompletes();
                    Assert.IsTrue(invoked);
                },
                // Parallel actions
                () => completer0(deferred0, cancelationSource0),
                () => completerVoid(deferredVoid, cancelationSourceVoid),
                () => deferred0.TryReportProgress(0.5f),
                () => deferredVoid.TryReportProgress(0.5f)
            );
        }

        [Test]
        public void DeferredsPassedToMergeMayBeCompletedConcurrentlyAfterProgressIsSubscribed_T2(
            [Values] CompleteType completeType0,
            [Values] CompleteType completeType1)
        {
            // When 2 or more promises are rejected, the remaining rejects are sent to the UncaughtRejectionHandler.
            // So we need to suppress that here and make sure it's correct.
            var currentHandler = Promise.Config.UncaughtRejectionHandler;
            Promise.Config.UncaughtRejectionHandler = e => Assert.AreEqual(rejectValue, e.Value);

            var completer0 = TestHelper.GetCompleterT(completeType0, 1, rejectValue);
            var completer1 = TestHelper.GetCompleterT(completeType1, 2, rejectValue);

            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
            var cancelationSource0 = default(CancelationSource);
            var cancelationSource1 = default(CancelationSource);
            bool invoked = false;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    deferred0 = TestHelper.GetNewDeferredT<int>(completeType0, out cancelationSource0);
                    deferred1 = TestHelper.GetNewDeferredT<int>(completeType1, out cancelationSource1);
                    invoked = false;
                    Promise.Merge(deferred0.Promise, deferred1.Promise)
                        .Progress(v => { }) // Callback might not be invoked if the promise is canceled/rejected.
                        .ContinueWith(r =>
                        {
                            if (r.State == Promise.State.Resolved)
                            {
                                var cv = r.Result;
                                Assert.AreEqual(1, cv.Item1);
                                Assert.AreEqual(2, cv.Item2);
                            }
                            invoked = true;
                        })
                        .Forget();
                },
                // Teardown
                () =>
                {
                    cancelationSource0.TryDispose();
                    cancelationSource1.TryDispose();
                    Promise.Manager.HandleCompletes();
                    Assert.IsTrue(invoked);
                },
                // Parallel actions
                () => completer0(deferred0, cancelationSource0),
                () => completer1(deferred1, cancelationSource1),
                () => deferred0.TryReportProgress(0.5f),
                () => deferred1.TryReportProgress(0.5f)
            );

            Promise.Config.UncaughtRejectionHandler = currentHandler;
        }

        [Test] // Only generate up to 2 parameters (more takes too long to test)
        public void DeferredsPassedToMergeMayBeCompletedConcurrentlyAfterProgressIsSubscribed_T2void(
            [Values] CompleteType completeType0,
            [Values(CompleteType.Resolve)] CompleteType completeType1,
            [Values] CompleteType completeTypeVoid)
        {
            // When 2 or more promises are rejected, the remaining rejects are sent to the UncaughtRejectionHandler.
            // So we need to suppress that here and make sure it's correct.
            var currentHandler = Promise.Config.UncaughtRejectionHandler;
            Promise.Config.UncaughtRejectionHandler = e => Assert.AreEqual(rejectValue, e.Value);

            var completer0 = TestHelper.GetCompleterT(completeType0, 1, rejectValue);
            var completer1 = TestHelper.GetCompleterT(completeType1, 2, rejectValue);
            var completerVoid = TestHelper.GetCompleterVoid(completeTypeVoid, rejectValue);

            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
            var deferredVoid = default(Promise.Deferred);
            var cancelationSource0 = default(CancelationSource);
            var cancelationSource1 = default(CancelationSource);
            var cancelationSourceVoid = default(CancelationSource);
            bool invoked = false;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    deferred0 = TestHelper.GetNewDeferredT<int>(completeType0, out cancelationSource0);
                    deferred1 = TestHelper.GetNewDeferredT<int>(completeType1, out cancelationSource1);
                    deferredVoid = TestHelper.GetNewDeferredVoid(completeTypeVoid, out cancelationSourceVoid);
                    invoked = false;
                    Promise.Merge(deferred0.Promise, deferred1.Promise, deferredVoid.Promise)
                        .Progress(v => { }) // Callback might not be invoked if the promise is canceled/rejected.
                        .ContinueWith(r =>
                        {
                            if (r.State == Promise.State.Resolved)
                            {
                                var cv = r.Result;
                                Assert.AreEqual(1, cv.Item1);
                                Assert.AreEqual(2, cv.Item2);
                            }
                            invoked = true;
                        })
                        .Forget();
                },
                // Teardown
                () =>
                {
                    cancelationSource0.TryDispose();
                    cancelationSource1.TryDispose();
                    cancelationSourceVoid.TryDispose();
                    cancelationSourceVoid.TryDispose();
                    Promise.Manager.HandleCompletes();
                    Assert.IsTrue(invoked);
                },
                // Parallel actions
                () => completer0(deferred0, cancelationSource0),
                () => completer1(deferred1, cancelationSource1),
                () => completerVoid(deferredVoid, cancelationSourceVoid),
                () => deferred0.TryReportProgress(0.5f),
                () => deferred1.TryReportProgress(0.5f),
                () => deferredVoid.TryReportProgress(0.5f)
            );

            Promise.Config.UncaughtRejectionHandler = currentHandler;
        }

        [Test] // Only generate up to 2 parameters (more takes too long to test)
        public void DeferredsPassedToMergeMayBeCompletedConcurrentlyAfterProgressIsSubscribed_T3(
            [Values] CompleteType completeType0,
            [Values] CompleteType completeType1,
            [Values(CompleteType.Resolve)] CompleteType completeType2)
        {
            // When 2 or more promises are rejected, the remaining rejects are sent to the UncaughtRejectionHandler.
            // So we need to suppress that here and make sure it's correct.
            var currentHandler = Promise.Config.UncaughtRejectionHandler;
            Promise.Config.UncaughtRejectionHandler = e => Assert.AreEqual(rejectValue, e.Value);

            var completer0 = TestHelper.GetCompleterT(completeType0, 1, rejectValue);
            var completer1 = TestHelper.GetCompleterT(completeType1, 2, rejectValue);
            var completer2 = TestHelper.GetCompleterT(completeType2, 3, rejectValue);

            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
            var deferred2 = default(Promise<int>.Deferred);
            var cancelationSource0 = default(CancelationSource);
            var cancelationSource1 = default(CancelationSource);
            var cancelationSource2 = default(CancelationSource);
            bool invoked = false;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    deferred0 = TestHelper.GetNewDeferredT<int>(completeType0, out cancelationSource0);
                    deferred1 = TestHelper.GetNewDeferredT<int>(completeType1, out cancelationSource1);
                    deferred2 = TestHelper.GetNewDeferredT<int>(completeType2, out cancelationSource2);
                    invoked = false;
                    Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise)
                        .Progress(v => { }) // Callback might not be invoked if the promise is canceled/rejected.
                        .ContinueWith(r =>
                        {
                            if (r.State == Promise.State.Resolved)
                            {
                                var cv = r.Result;
                                Assert.AreEqual(1, cv.Item1);
                                Assert.AreEqual(2, cv.Item2);
                                Assert.AreEqual(3, cv.Item3);
                            }
                            invoked = true;
                        })
                        .Forget();
                },
                // Teardown
                () =>
                {
                    cancelationSource0.TryDispose();
                    cancelationSource1.TryDispose();
                    cancelationSource2.TryDispose();
                    Promise.Manager.HandleCompletes();
                    Assert.IsTrue(invoked);
                },
                // Parallel actions
                () => completer0(deferred0, cancelationSource0),
                () => completer1(deferred1, cancelationSource1),
                () => completer2(deferred2, cancelationSource2),
                () => deferred0.TryReportProgress(0.5f),
                () => deferred1.TryReportProgress(0.5f),
                () => deferred2.TryReportProgress(0.5f)
            );

            Promise.Config.UncaughtRejectionHandler = currentHandler;
        }

        [Test] // Only generate up to 2 parameters (more takes too long to test)
        public void DeferredsPassedToMergeMayBeCompletedConcurrentlyAfterProgressIsSubscribed_T3void(
            [Values] CompleteType completeType0,
            [Values(CompleteType.Resolve)] CompleteType completeType1,
            [Values(CompleteType.Resolve)] CompleteType completeType2,
            [Values] CompleteType completeTypeVoid)
        {
            // When 2 or more promises are rejected, the remaining rejects are sent to the UncaughtRejectionHandler.
            // So we need to suppress that here and make sure it's correct.
            var currentHandler = Promise.Config.UncaughtRejectionHandler;
            Promise.Config.UncaughtRejectionHandler = e => Assert.AreEqual(rejectValue, e.Value);

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
            bool invoked = false;

            var threadHelper = new ThreadHelper();
            // Don't use WithOffsets because there are too many actions.
            threadHelper.ExecuteParallelActions(ThreadHelper.multiExecutionCount,
                // Setup
                () =>
                {
                    deferred0 = TestHelper.GetNewDeferredT<int>(completeType0, out cancelationSource0);
                    deferred1 = TestHelper.GetNewDeferredT<int>(completeType1, out cancelationSource1);
                    deferred2 = TestHelper.GetNewDeferredT<int>(completeType2, out cancelationSource2);
                    deferredVoid = TestHelper.GetNewDeferredVoid(completeTypeVoid, out cancelationSourceVoid);
                    invoked = false;
                    Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferredVoid.Promise)
                        .Progress(v => { }) // Callback might not be invoked if the promise is canceled/rejected.
                        .ContinueWith(r =>
                        {
                            if (r.State == Promise.State.Resolved)
                            {
                                var cv = r.Result;
                                Assert.AreEqual(1, cv.Item1);
                                Assert.AreEqual(2, cv.Item2);
                                Assert.AreEqual(3, cv.Item3);
                            }
                            invoked = true;
                        })
                        .Forget();
                },
                // Teardown
                () =>
                {
                    cancelationSource0.TryDispose();
                    cancelationSource1.TryDispose();
                    cancelationSource2.TryDispose();
                    cancelationSourceVoid.TryDispose();
                    Promise.Manager.HandleCompletes();
                    Assert.IsTrue(invoked);
                },
                // Parallel actions
                () => completer0(deferred0, cancelationSource0),
                () => completer1(deferred1, cancelationSource1),
                () => completer2(deferred2, cancelationSource2),
                () => completerVoid(deferredVoid, cancelationSourceVoid),
                () => deferred0.TryReportProgress(0.5f),
                () => deferred1.TryReportProgress(0.5f),
                () => deferred2.TryReportProgress(0.5f),
                () => deferredVoid.TryReportProgress(0.5f)
            );

            Promise.Config.UncaughtRejectionHandler = currentHandler;
        }

        [Test] // Only generate up to 2 parameters (more takes too long to test)
        public void DeferredsPassedToMergeMayBeCompletedConcurrentlyAfterProgressIsSubscribed_T4(
            [Values] CompleteType completeType0,
            [Values] CompleteType completeType1,
            [Values(CompleteType.Resolve)] CompleteType completeType2,
            [Values(CompleteType.Resolve)] CompleteType completeType3)
        {
            // When 2 or more promises are rejected, the remaining rejects are sent to the UncaughtRejectionHandler.
            // So we need to suppress that here and make sure it's correct.
            var currentHandler = Promise.Config.UncaughtRejectionHandler;
            Promise.Config.UncaughtRejectionHandler = e => Assert.AreEqual(rejectValue, e.Value);

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
            bool invoked = false;

            var threadHelper = new ThreadHelper();
            // Don't use WithOffsets because there are too many actions.
            threadHelper.ExecuteParallelActions(ThreadHelper.multiExecutionCount,
                // Setup
                () =>
                {
                    deferred0 = TestHelper.GetNewDeferredT<int>(completeType0, out cancelationSource0);
                    deferred1 = TestHelper.GetNewDeferredT<int>(completeType1, out cancelationSource1);
                    deferred2 = TestHelper.GetNewDeferredT<int>(completeType2, out cancelationSource2);
                    deferred3 = TestHelper.GetNewDeferredT<int>(completeType3, out cancelationSource3);
                    invoked = false;
                    Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise)
                        .Progress(v => { }) // Callback might not be invoked if the promise is canceled/rejected.
                        .ContinueWith(r =>
                        {
                            if (r.State == Promise.State.Resolved)
                            {
                                var cv = r.Result;
                                Assert.AreEqual(1, cv.Item1);
                                Assert.AreEqual(2, cv.Item2);
                                Assert.AreEqual(3, cv.Item3);
                                Assert.AreEqual(4, cv.Item4);
                            }
                            invoked = true;
                        })
                        .Forget();
                },
                // Teardown
                () =>
                {
                    cancelationSource0.TryDispose();
                    cancelationSource1.TryDispose();
                    cancelationSource2.TryDispose();
                    cancelationSource3.TryDispose();
                    Promise.Manager.HandleCompletes();
                    Assert.IsTrue(invoked);
                },
                // Parallel actions
                () => completer0(deferred0, cancelationSource0),
                () => completer1(deferred1, cancelationSource1),
                () => completer2(deferred2, cancelationSource2),
                () => completer3(deferred3, cancelationSource3),
                () => deferred0.TryReportProgress(0.5f),
                () => deferred1.TryReportProgress(0.5f),
                () => deferred2.TryReportProgress(0.5f),
                () => deferred3.TryReportProgress(0.5f)
            );

            Promise.Config.UncaughtRejectionHandler = currentHandler;
        }

        [Test] // Only generate up to 2 parameters (more takes too long to test)
        public void DeferredsPassedToMergeMayBeCompletedConcurrentlyAfterProgressIsSubscribed_T4void(
            [Values] CompleteType completeType0,
            [Values(CompleteType.Resolve)] CompleteType completeType1,
            [Values(CompleteType.Resolve)] CompleteType completeType2,
            [Values(CompleteType.Resolve)] CompleteType completeType3,
            [Values] CompleteType completeTypeVoid)
        {
            // When 2 or more promises are rejected, the remaining rejects are sent to the UncaughtRejectionHandler.
            // So we need to suppress that here and make sure it's correct.
            var currentHandler = Promise.Config.UncaughtRejectionHandler;
            Promise.Config.UncaughtRejectionHandler = e => Assert.AreEqual(rejectValue, e.Value);

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
            bool invoked = false;

            var threadHelper = new ThreadHelper();
            // Don't use WithOffsets because there are too many actions.
            threadHelper.ExecuteParallelActions(ThreadHelper.multiExecutionCount,
                // Setup
                () =>
                {
                    deferred0 = TestHelper.GetNewDeferredT<int>(completeType0, out cancelationSource0);
                    deferred1 = TestHelper.GetNewDeferredT<int>(completeType1, out cancelationSource1);
                    deferred2 = TestHelper.GetNewDeferredT<int>(completeType2, out cancelationSource2);
                    deferred3 = TestHelper.GetNewDeferredT<int>(completeType3, out cancelationSource3);
                    deferredVoid = TestHelper.GetNewDeferredVoid(completeTypeVoid, out cancelationSourceVoid);
                    invoked = false;
                    Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise, deferredVoid.Promise)
                        .Progress(v => { }) // Callback might not be invoked if the promise is canceled/rejected.
                        .ContinueWith(r =>
                        {
                            if (r.State == Promise.State.Resolved)
                            {
                                var cv = r.Result;
                                Assert.AreEqual(1, cv.Item1);
                                Assert.AreEqual(2, cv.Item2);
                                Assert.AreEqual(3, cv.Item3);
                                Assert.AreEqual(4, cv.Item4);
                            }
                            invoked = true;
                        })
                        .Forget();
                },
                // Teardown
                () =>
                {
                    cancelationSource0.TryDispose();
                    cancelationSource1.TryDispose();
                    cancelationSource2.TryDispose();
                    cancelationSource3.TryDispose();
                    cancelationSourceVoid.TryDispose();
                    Promise.Manager.HandleCompletes();
                    Assert.IsTrue(invoked);
                },
                // Parallel actions
                () => completer0(deferred0, cancelationSource0),
                () => completer1(deferred1, cancelationSource1),
                () => completer2(deferred2, cancelationSource2),
                () => completer3(deferred3, cancelationSource3),
                () => completerVoid(deferredVoid, cancelationSourceVoid),
                () => deferred0.TryReportProgress(0.5f),
                () => deferred1.TryReportProgress(0.5f),
                () => deferred2.TryReportProgress(0.5f),
                () => deferred3.TryReportProgress(0.5f),
                () => deferredVoid.TryReportProgress(0.5f)
            );

            Promise.Config.UncaughtRejectionHandler = currentHandler;
        }

        [Test] // Only generate up to 2 parameters (more takes too long to test)
        public void DeferredsPassedToMergeMayBeCompletedConcurrentlyAfterProgressIsSubscribed_T5(
            [Values] CompleteType completeType0,
            [Values] CompleteType completeType1,
            [Values(CompleteType.Resolve)] CompleteType completeType2,
            [Values(CompleteType.Resolve)] CompleteType completeType3,
            [Values(CompleteType.Resolve)] CompleteType completeType4)
        {
            // When 2 or more promises are rejected, the remaining rejects are sent to the UncaughtRejectionHandler.
            // So we need to suppress that here and make sure it's correct.
            var currentHandler = Promise.Config.UncaughtRejectionHandler;
            Promise.Config.UncaughtRejectionHandler = e => Assert.AreEqual(rejectValue, e.Value);

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
            bool invoked = false;

            var threadHelper = new ThreadHelper();
            // Don't use WithOffsets because there are too many actions.
            threadHelper.ExecuteParallelActions(ThreadHelper.multiExecutionCount,
                // Setup
                () =>
                {
                    deferred0 = TestHelper.GetNewDeferredT<int>(completeType0, out cancelationSource0);
                    deferred1 = TestHelper.GetNewDeferredT<int>(completeType1, out cancelationSource1);
                    deferred2 = TestHelper.GetNewDeferredT<int>(completeType2, out cancelationSource2);
                    deferred3 = TestHelper.GetNewDeferredT<int>(completeType3, out cancelationSource3);
                    deferred4 = TestHelper.GetNewDeferredT<int>(completeType4, out cancelationSource4);
                    invoked = false;
                    Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise, deferred4.Promise)
                        .Progress(v => { }) // Callback might not be invoked if the promise is canceled/rejected.
                        .ContinueWith(r =>
                        {
                            if (r.State == Promise.State.Resolved)
                            {
                                var cv = r.Result;
                                Assert.AreEqual(1, cv.Item1);
                                Assert.AreEqual(2, cv.Item2);
                                Assert.AreEqual(3, cv.Item3);
                                Assert.AreEqual(4, cv.Item4);
                                Assert.AreEqual(5, cv.Item5);
                            }
                            invoked = true;
                        })
                        .Forget();
                },
                // Teardown
                () =>
                {
                    cancelationSource0.TryDispose();
                    cancelationSource1.TryDispose();
                    cancelationSource2.TryDispose();
                    cancelationSource3.TryDispose();
                    cancelationSource4.TryDispose();
                    Promise.Manager.HandleCompletes();
                    Assert.IsTrue(invoked);
                },
                // Parallel actions
                () => completer0(deferred0, cancelationSource0),
                () => completer1(deferred1, cancelationSource1),
                () => completer2(deferred2, cancelationSource2),
                () => completer3(deferred3, cancelationSource3),
                () => completer4(deferred4, cancelationSource4),
                () => deferred0.TryReportProgress(0.5f),
                () => deferred1.TryReportProgress(0.5f),
                () => deferred2.TryReportProgress(0.5f),
                () => deferred3.TryReportProgress(0.5f),
                () => deferred4.TryReportProgress(0.5f)
            );

            Promise.Config.UncaughtRejectionHandler = currentHandler;
        }

        [Test] // Only generate up to 2 parameters (more takes too long to test)
        public void DeferredsPassedToMergeMayBeCompletedConcurrentlyAfterProgressIsSubscribed_T5void(
            [Values] CompleteType completeType0,
            [Values(CompleteType.Resolve)] CompleteType completeType1,
            [Values(CompleteType.Resolve)] CompleteType completeType2,
            [Values(CompleteType.Resolve)] CompleteType completeType3,
            [Values(CompleteType.Resolve)] CompleteType completeType4,
            [Values] CompleteType completeTypeVoid)
        {
            // When 2 or more promises are rejected, the remaining rejects are sent to the UncaughtRejectionHandler.
            // So we need to suppress that here and make sure it's correct.
            var currentHandler = Promise.Config.UncaughtRejectionHandler;
            Promise.Config.UncaughtRejectionHandler = e => Assert.AreEqual(rejectValue, e.Value);

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
            bool invoked = false;

            var threadHelper = new ThreadHelper();
            // Don't use WithOffsets because there are too many actions.
            threadHelper.ExecuteParallelActions(ThreadHelper.multiExecutionCount,
                // Setup
                () =>
                {
                    deferred0 = TestHelper.GetNewDeferredT<int>(completeType0, out cancelationSource0);
                    deferred1 = TestHelper.GetNewDeferredT<int>(completeType1, out cancelationSource1);
                    deferred2 = TestHelper.GetNewDeferredT<int>(completeType2, out cancelationSource2);
                    deferred3 = TestHelper.GetNewDeferredT<int>(completeType3, out cancelationSource3);
                    deferred4 = TestHelper.GetNewDeferredT<int>(completeType4, out cancelationSource4);
                    deferredVoid = TestHelper.GetNewDeferredVoid(completeTypeVoid, out cancelationSourceVoid);
                    invoked = false;
                    Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise, deferred4.Promise, deferredVoid.Promise)
                        .Progress(v => { }) // Callback might not be invoked if the promise is canceled/rejected.
                        .ContinueWith(r =>
                        {
                            if (r.State == Promise.State.Resolved)
                            {
                                var cv = r.Result;
                                Assert.AreEqual(1, cv.Item1);
                                Assert.AreEqual(2, cv.Item2);
                                Assert.AreEqual(3, cv.Item3);
                                Assert.AreEqual(4, cv.Item4);
                                Assert.AreEqual(5, cv.Item5);
                            }
                            invoked = true;
                        })
                        .Forget();
                },
                // Teardown
                () =>
                {
                    cancelationSource0.TryDispose();
                    cancelationSource1.TryDispose();
                    cancelationSource2.TryDispose();
                    cancelationSource3.TryDispose();
                    cancelationSource4.TryDispose();
                    cancelationSourceVoid.TryDispose();
                    Promise.Manager.HandleCompletes();
                    Assert.IsTrue(invoked);
                },
                // Parallel actions
                () => completer0(deferred0, cancelationSource0),
                () => completer1(deferred1, cancelationSource1),
                () => completer2(deferred2, cancelationSource2),
                () => completer3(deferred3, cancelationSource3),
                () => completer4(deferred4, cancelationSource4),
                () => completerVoid(deferredVoid, cancelationSourceVoid),
                () => deferred0.TryReportProgress(0.5f),
                () => deferred1.TryReportProgress(0.5f),
                () => deferred2.TryReportProgress(0.5f),
                () => deferred3.TryReportProgress(0.5f),
                () => deferred4.TryReportProgress(0.5f),
                () => deferredVoid.TryReportProgress(0.5f)
            );

            Promise.Config.UncaughtRejectionHandler = currentHandler;
        }

        [Test] // Only generate up to 2 parameters (more takes too long to test)
        public void DeferredsPassedToMergeMayBeCompletedConcurrentlyAfterProgressIsSubscribed_T6(
            [Values] CompleteType completeType0,
            [Values] CompleteType completeType1,
            [Values(CompleteType.Resolve)] CompleteType completeType2,
            [Values(CompleteType.Resolve)] CompleteType completeType3,
            [Values(CompleteType.Resolve)] CompleteType completeType4,
            [Values(CompleteType.Resolve)] CompleteType completeType5)
        {
            // When 2 or more promises are rejected, the remaining rejects are sent to the UncaughtRejectionHandler.
            // So we need to suppress that here and make sure it's correct.
            var currentHandler = Promise.Config.UncaughtRejectionHandler;
            Promise.Config.UncaughtRejectionHandler = e => Assert.AreEqual(rejectValue, e.Value);

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
            bool invoked = false;

            var threadHelper = new ThreadHelper();
            // Don't use WithOffsets because there are too many actions.
            threadHelper.ExecuteParallelActions(ThreadHelper.multiExecutionCount,
                // Setup
                () =>
                {
                    deferred0 = TestHelper.GetNewDeferredT<int>(completeType0, out cancelationSource0);
                    deferred1 = TestHelper.GetNewDeferredT<int>(completeType1, out cancelationSource1);
                    deferred2 = TestHelper.GetNewDeferredT<int>(completeType2, out cancelationSource2);
                    deferred3 = TestHelper.GetNewDeferredT<int>(completeType3, out cancelationSource3);
                    deferred4 = TestHelper.GetNewDeferredT<int>(completeType4, out cancelationSource4);
                    deferred5 = TestHelper.GetNewDeferredT<int>(completeType5, out cancelationSource5);
                    invoked = false;
                    Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise, deferred4.Promise, deferred5.Promise)
                        .Progress(v => { }) // Callback might not be invoked if the promise is canceled/rejected.
                        .ContinueWith(r =>
                        {
                            if (r.State == Promise.State.Resolved)
                            {
                                var cv = r.Result;
                                Assert.AreEqual(1, cv.Item1);
                                Assert.AreEqual(2, cv.Item2);
                                Assert.AreEqual(3, cv.Item3);
                                Assert.AreEqual(4, cv.Item4);
                                Assert.AreEqual(5, cv.Item5);
                                Assert.AreEqual(6, cv.Item6);
                            }
                            invoked = true;
                        })
                        .Forget();
                },
                // Teardown
                () =>
                {
                    cancelationSource0.TryDispose();
                    cancelationSource1.TryDispose();
                    cancelationSource2.TryDispose();
                    cancelationSource3.TryDispose();
                    cancelationSource4.TryDispose();
                    cancelationSource5.TryDispose();
                    Promise.Manager.HandleCompletes();
                    Assert.IsTrue(invoked);
                },
                // Parallel actions
                () => completer0(deferred0, cancelationSource0),
                () => completer1(deferred1, cancelationSource1),
                () => completer2(deferred2, cancelationSource2),
                () => completer3(deferred3, cancelationSource3),
                () => completer4(deferred4, cancelationSource4),
                () => completer5(deferred5, cancelationSource5),
                () => deferred0.TryReportProgress(0.5f),
                () => deferred1.TryReportProgress(0.5f),
                () => deferred2.TryReportProgress(0.5f),
                () => deferred3.TryReportProgress(0.5f),
                () => deferred4.TryReportProgress(0.5f),
                () => deferred5.TryReportProgress(0.5f)
            );

            Promise.Config.UncaughtRejectionHandler = currentHandler;
        }

        [Test] // Only generate up to 2 parameters (more takes too long to test)
        public void DeferredsPassedToMergeMayBeCompletedConcurrentlyAfterProgressIsSubscribed_T6void(
            [Values] CompleteType completeType0,
            [Values(CompleteType.Resolve)] CompleteType completeType1,
            [Values(CompleteType.Resolve)] CompleteType completeType2,
            [Values(CompleteType.Resolve)] CompleteType completeType3,
            [Values(CompleteType.Resolve)] CompleteType completeType4,
            [Values(CompleteType.Resolve)] CompleteType completeType5,
            [Values] CompleteType completeTypeVoid)
        {
            // When 2 or more promises are rejected, the remaining rejects are sent to the UncaughtRejectionHandler.
            // So we need to suppress that here and make sure it's correct.
            var currentHandler = Promise.Config.UncaughtRejectionHandler;
            Promise.Config.UncaughtRejectionHandler = e => Assert.AreEqual(rejectValue, e.Value);

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
            bool invoked = false;

            var threadHelper = new ThreadHelper();
            // Don't use WithOffsets because there are too many actions.
            threadHelper.ExecuteParallelActions(ThreadHelper.multiExecutionCount,
                // Setup
                () =>
                {
                    deferred0 = TestHelper.GetNewDeferredT<int>(completeType0, out cancelationSource0);
                    deferred1 = TestHelper.GetNewDeferredT<int>(completeType1, out cancelationSource1);
                    deferred2 = TestHelper.GetNewDeferredT<int>(completeType2, out cancelationSource2);
                    deferred3 = TestHelper.GetNewDeferredT<int>(completeType3, out cancelationSource3);
                    deferred4 = TestHelper.GetNewDeferredT<int>(completeType4, out cancelationSource4);
                    deferred5 = TestHelper.GetNewDeferredT<int>(completeType5, out cancelationSource5);
                    deferredVoid = TestHelper.GetNewDeferredVoid(completeTypeVoid, out cancelationSourceVoid);
                    invoked = false;
                    Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise, deferred4.Promise, deferred5.Promise, deferredVoid.Promise)
                        .Progress(v => { }) // Callback might not be invoked if the promise is canceled/rejected.
                        .ContinueWith(r =>
                        {
                            if (r.State == Promise.State.Resolved)
                            {
                                var cv = r.Result;
                                Assert.AreEqual(1, cv.Item1);
                                Assert.AreEqual(2, cv.Item2);
                                Assert.AreEqual(3, cv.Item3);
                                Assert.AreEqual(4, cv.Item4);
                                Assert.AreEqual(5, cv.Item5);
                                Assert.AreEqual(6, cv.Item6);
                            }
                            invoked = true;
                        })
                        .Forget();
                },
                // Teardown
                () =>
                {
                    cancelationSource0.TryDispose();
                    cancelationSource1.TryDispose();
                    cancelationSource2.TryDispose();
                    cancelationSource3.TryDispose();
                    cancelationSource4.TryDispose();
                    cancelationSource5.TryDispose();
                    cancelationSourceVoid.TryDispose();
                    Promise.Manager.HandleCompletes();
                    Assert.IsTrue(invoked);
                },
                // Parallel actions
                () => completer0(deferred0, cancelationSource0),
                () => completer1(deferred1, cancelationSource1),
                () => completer2(deferred2, cancelationSource2),
                () => completer3(deferred3, cancelationSource3),
                () => completer4(deferred4, cancelationSource4),
                () => completer5(deferred5, cancelationSource5),
                () => completerVoid(deferredVoid, cancelationSourceVoid),
                () => deferred0.TryReportProgress(0.5f),
                () => deferred1.TryReportProgress(0.5f),
                () => deferred2.TryReportProgress(0.5f),
                () => deferred3.TryReportProgress(0.5f),
                () => deferred4.TryReportProgress(0.5f),
                () => deferred5.TryReportProgress(0.5f),
                () => deferredVoid.TryReportProgress(0.5f)
            );

            Promise.Config.UncaughtRejectionHandler = currentHandler;
        }

        [Test] // Only generate up to 2 parameters (more takes too long to test)
        public void DeferredsPassedToMergeMayBeCompletedConcurrentlyAfterProgressIsSubscribed_T7(
            [Values] CompleteType completeType0,
            [Values] CompleteType completeType1,
            [Values(CompleteType.Resolve)] CompleteType completeType2,
            [Values(CompleteType.Resolve)] CompleteType completeType3,
            [Values(CompleteType.Resolve)] CompleteType completeType4,
            [Values(CompleteType.Resolve)] CompleteType completeType5,
            [Values(CompleteType.Resolve)] CompleteType completeType6)
        {
            // When 2 or more promises are rejected, the remaining rejects are sent to the UncaughtRejectionHandler.
            // So we need to suppress that here and make sure it's correct.
            var currentHandler = Promise.Config.UncaughtRejectionHandler;
            Promise.Config.UncaughtRejectionHandler = e => Assert.AreEqual(rejectValue, e.Value);

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
            bool invoked = false;

            var threadHelper = new ThreadHelper();
            // Don't use WithOffsets because there are too many actions.
            threadHelper.ExecuteParallelActions(ThreadHelper.multiExecutionCount,
                // Setup
                () =>
                {
                    deferred0 = TestHelper.GetNewDeferredT<int>(completeType0, out cancelationSource0);
                    deferred1 = TestHelper.GetNewDeferredT<int>(completeType1, out cancelationSource1);
                    deferred2 = TestHelper.GetNewDeferredT<int>(completeType2, out cancelationSource2);
                    deferred3 = TestHelper.GetNewDeferredT<int>(completeType3, out cancelationSource3);
                    deferred4 = TestHelper.GetNewDeferredT<int>(completeType4, out cancelationSource4);
                    deferred5 = TestHelper.GetNewDeferredT<int>(completeType5, out cancelationSource5);
                    deferred6 = TestHelper.GetNewDeferredT<int>(completeType6, out cancelationSource6);
                    invoked = false;
                    Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise, deferred4.Promise, deferred5.Promise, deferred6.Promise)
                        .Progress(v => { }) // Callback might not be invoked if the promise is canceled/rejected.
                        .ContinueWith(r =>
                        {
                            if (r.State == Promise.State.Resolved)
                            {
                                var cv = r.Result;
                                Assert.AreEqual(1, cv.Item1);
                                Assert.AreEqual(2, cv.Item2);
                                Assert.AreEqual(3, cv.Item3);
                                Assert.AreEqual(4, cv.Item4);
                                Assert.AreEqual(5, cv.Item5);
                                Assert.AreEqual(6, cv.Item6);
                                Assert.AreEqual(7, cv.Item7);
                            }
                            invoked = true;
                        })
                        .Forget();
                },
                // Teardown
                () =>
                {
                    cancelationSource0.TryDispose();
                    cancelationSource1.TryDispose();
                    cancelationSource2.TryDispose();
                    cancelationSource3.TryDispose();
                    cancelationSource4.TryDispose();
                    cancelationSource5.TryDispose();
                    cancelationSource6.TryDispose();
                    Promise.Manager.HandleCompletes();
                    Assert.IsTrue(invoked);
                },
                // Parallel actions
                () => completer0(deferred0, cancelationSource0),
                () => completer1(deferred1, cancelationSource1),
                () => completer2(deferred2, cancelationSource2),
                () => completer3(deferred3, cancelationSource3),
                () => completer4(deferred4, cancelationSource4),
                () => completer5(deferred5, cancelationSource5),
                () => completer6(deferred6, cancelationSource6),
                () => deferred0.TryReportProgress(0.5f),
                () => deferred1.TryReportProgress(0.5f),
                () => deferred2.TryReportProgress(0.5f),
                () => deferred3.TryReportProgress(0.5f),
                () => deferred4.TryReportProgress(0.5f),
                () => deferred5.TryReportProgress(0.5f),
                () => deferred6.TryReportProgress(0.5f)
            );

            Promise.Config.UncaughtRejectionHandler = currentHandler;
        }

        [Test] // Only generate up to 2 parameters (more takes too long to test)
        public void DeferredsPassedToMergeMayBeCompletedConcurrentlyAfterProgressIsSubscribed_T7void(
            [Values] CompleteType completeType0,
            [Values(CompleteType.Resolve)] CompleteType completeType1,
            [Values(CompleteType.Resolve)] CompleteType completeType2,
            [Values(CompleteType.Resolve)] CompleteType completeType3,
            [Values(CompleteType.Resolve)] CompleteType completeType4,
            [Values(CompleteType.Resolve)] CompleteType completeType5,
            [Values(CompleteType.Resolve)] CompleteType completeType6,
            [Values] CompleteType completeTypeVoid)
        {
            // When 2 or more promises are rejected, the remaining rejects are sent to the UncaughtRejectionHandler.
            // So we need to suppress that here and make sure it's correct.
            var currentHandler = Promise.Config.UncaughtRejectionHandler;
            Promise.Config.UncaughtRejectionHandler = e => Assert.AreEqual(rejectValue, e.Value);

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
            bool invoked = false;

            var threadHelper = new ThreadHelper();
            // Don't use WithOffsets because there are too many actions.
            threadHelper.ExecuteParallelActions(ThreadHelper.multiExecutionCount,
                // Setup
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
                    invoked = false;
                    Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise, deferred4.Promise, deferred5.Promise, deferred6.Promise, deferredVoid.Promise)
                        .Progress(v => { }) // Callback might not be invoked if the promise is canceled/rejected.
                        .ContinueWith(r =>
                        {
                            if (r.State == Promise.State.Resolved)
                            {
                                var cv = r.Result;
                                Assert.AreEqual(1, cv.Item1);
                                Assert.AreEqual(2, cv.Item2);
                                Assert.AreEqual(3, cv.Item3);
                                Assert.AreEqual(4, cv.Item4);
                                Assert.AreEqual(5, cv.Item5);
                                Assert.AreEqual(6, cv.Item6);
                                Assert.AreEqual(7, cv.Item7);
                            }
                            invoked = true;
                        })
                        .Forget();
                },
                // Teardown
                () =>
                {
                    cancelationSource0.TryDispose();
                    cancelationSource1.TryDispose();
                    cancelationSource2.TryDispose();
                    cancelationSource3.TryDispose();
                    cancelationSource4.TryDispose();
                    cancelationSource5.TryDispose();
                    cancelationSource6.TryDispose();
                    cancelationSourceVoid.TryDispose();
                    Promise.Manager.HandleCompletes();
                    Assert.IsTrue(invoked);
                },
                // Parallel actions
                () => completer0(deferred0, cancelationSource0),
                () => completer1(deferred1, cancelationSource1),
                () => completer2(deferred2, cancelationSource2),
                () => completer3(deferred3, cancelationSource3),
                () => completer4(deferred4, cancelationSource4),
                () => completer5(deferred5, cancelationSource5),
                () => completer6(deferred6, cancelationSource6),
                () => completerVoid(deferredVoid, cancelationSourceVoid),
                () => deferred0.TryReportProgress(0.5f),
                () => deferred1.TryReportProgress(0.5f),
                () => deferred2.TryReportProgress(0.5f),
                () => deferred3.TryReportProgress(0.5f),
                () => deferred4.TryReportProgress(0.5f),
                () => deferred5.TryReportProgress(0.5f),
                () => deferred6.TryReportProgress(0.5f),
                () => deferredVoid.TryReportProgress(0.5f)
            );

            Promise.Config.UncaughtRejectionHandler = currentHandler;
        }
    }
}

#endif // PROMISE_PROGRESS
#endif // CSHARP_7_OR_LATER