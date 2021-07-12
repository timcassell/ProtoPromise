﻿#if CSHARP_7_OR_LATER

using NUnit.Framework;

namespace Proto.Promises.Tests.Threading
{
    public class MergeConcurrencyTests
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
        public void DeferredsMayBeCompletedWhileTheirPromisesArePassedToMergeConcurrently_T1void(
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
                () =>
                {
                    Promise.Merge(deferred0.Promise, deferredVoid.Promise)
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
        public void DeferredsMayBeCompletedWhileTheirPromisesArePassedToMergeConcurrently_T2(
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
                () =>
                {
                    Promise.Merge(deferred0.Promise, deferred1.Promise)
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
        public void DeferredsMayBeCompletedWhileTheirPromisesArePassedToMergeConcurrently_T2void(
            [Values] CompleteType completeType0,
            [Values] CompleteType completeType1,
            [Values(CompleteType.Resolve)] CompleteType completeTypeVoid)
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
                () =>
                {
                    Promise.Merge(deferred0.Promise, deferred1.Promise, deferredVoid.Promise)
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

        [Test]
        public void DeferredsMayBeCompletedWhileTheirPromisesArePassedToMergeConcurrently_T3(
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
                () =>
                {
                    Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise)
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
        public void DeferredsMayBeCompletedWhileTheirPromisesArePassedToMergeConcurrently_T3void(
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
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    deferred0 = TestHelper.GetNewDeferredT<int>(completeType0, out cancelationSource0);
                    deferred1 = TestHelper.GetNewDeferredT<int>(completeType1, out cancelationSource1);
                    deferred2 = TestHelper.GetNewDeferredT<int>(completeType2, out cancelationSource2);
                    deferredVoid = TestHelper.GetNewDeferredVoid(completeTypeVoid, out cancelationSourceVoid);
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
                () =>
                {
                    Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferredVoid.Promise)
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
        public void DeferredsMayBeCompletedWhileTheirPromisesArePassedToMergeConcurrently_T4(
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
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    deferred0 = TestHelper.GetNewDeferredT<int>(completeType0, out cancelationSource0);
                    deferred1 = TestHelper.GetNewDeferredT<int>(completeType1, out cancelationSource1);
                    deferred2 = TestHelper.GetNewDeferredT<int>(completeType2, out cancelationSource2);
                    deferred3 = TestHelper.GetNewDeferredT<int>(completeType3, out cancelationSource3);
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
                () =>
                {
                    Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise)
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
        public void DeferredsMayBeCompletedWhileTheirPromisesArePassedToMergeConcurrently_T4void(
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
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    deferred0 = TestHelper.GetNewDeferredT<int>(completeType0, out cancelationSource0);
                    deferred1 = TestHelper.GetNewDeferredT<int>(completeType1, out cancelationSource1);
                    deferred2 = TestHelper.GetNewDeferredT<int>(completeType2, out cancelationSource2);
                    deferred3 = TestHelper.GetNewDeferredT<int>(completeType3, out cancelationSource3);
                    deferredVoid = TestHelper.GetNewDeferredVoid(completeTypeVoid, out cancelationSourceVoid);
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
                () =>
                {
                    Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise, deferredVoid.Promise)
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
        public void DeferredsMayBeCompletedWhileTheirPromisesArePassedToMergeConcurrently_T5(
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
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    deferred0 = TestHelper.GetNewDeferredT<int>(completeType0, out cancelationSource0);
                    deferred1 = TestHelper.GetNewDeferredT<int>(completeType1, out cancelationSource1);
                    deferred2 = TestHelper.GetNewDeferredT<int>(completeType2, out cancelationSource2);
                    deferred3 = TestHelper.GetNewDeferredT<int>(completeType3, out cancelationSource3);
                    deferred4 = TestHelper.GetNewDeferredT<int>(completeType4, out cancelationSource4);
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
                () =>
                {
                    Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise, deferred4.Promise)
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
        public void DeferredsMayBeCompletedWhileTheirPromisesArePassedToMergeConcurrently_T5void(
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
            threadHelper.ExecuteParallelActionsWithOffsets(false,
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
                () =>
                {
                    Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise, deferred4.Promise, deferredVoid.Promise)
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
        public void DeferredsMayBeCompletedWhileTheirPromisesArePassedToMergeConcurrently_T6(
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
            threadHelper.ExecuteParallelActionsWithOffsets(false,
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
                () =>
                {
                    Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise, deferred4.Promise, deferred5.Promise)
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
        public void DeferredsMayBeCompletedWhileTheirPromisesArePassedToMergeConcurrently_T6void(
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
            threadHelper.ExecuteParallelActionsWithOffsets(false,
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
                () =>
                {
                    Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise, deferred4.Promise, deferred5.Promise, deferredVoid.Promise)
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
        public void DeferredsMayBeCompletedWhileTheirPromisesArePassedToMergeConcurrently_T7(
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
            threadHelper.ExecuteParallelActionsWithOffsets(false,
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
                () =>
                {
                    Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise, deferred4.Promise, deferred5.Promise, deferred6.Promise)
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
        public void DeferredsMayBeCompletedWhileTheirPromisesArePassedToMergeConcurrently_T7void(
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
            threadHelper.ExecuteParallelActionsWithOffsets(false,
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
                () =>
                {
                    Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise, deferred4.Promise, deferred5.Promise, deferred6.Promise, deferredVoid.Promise)
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
        public void DeferredsMayBeCompletedConcurrentlyAfterTheirPromisesArePassedToMerge_T1void(
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
                () => completerVoid(deferredVoid, cancelationSourceVoid)
            );

            Promise.Config.UncaughtRejectionHandler = currentHandler;
        }

        [Test]
        public void DeferredsMayBeCompletedConcurrentlyAfterTheirPromisesArePassedToMerge_T2(
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
                () => completer1(deferred1, cancelationSource1)
            );

            Promise.Config.UncaughtRejectionHandler = currentHandler;
        }

        [Test] // Only generate up to 2 parameters (more takes too long to test)
        public void DeferredsMayBeCompletedConcurrentlyAfterTheirPromisesArePassedToMerge_T2void(
            [Values] CompleteType completeType0,
            [Values] CompleteType completeType1,
            [Values(CompleteType.Resolve)] CompleteType completeTypeVoid)
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
                () => completerVoid(deferredVoid, cancelationSourceVoid)
            );

            Promise.Config.UncaughtRejectionHandler = currentHandler;
        }

        [Test]
        public void DeferredsMayBeCompletedConcurrentlyAfterTheirPromisesArePassedToMerge_T3(
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
                () => completer2(deferred2, cancelationSource2)
            );

            Promise.Config.UncaughtRejectionHandler = currentHandler;
        }

        [Test] // Only generate up to 2 parameters (more takes too long to test)
        public void DeferredsMayBeCompletedConcurrentlyAfterTheirPromisesArePassedToMerge_T3void(
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
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    deferred0 = TestHelper.GetNewDeferredT<int>(completeType0, out cancelationSource0);
                    deferred1 = TestHelper.GetNewDeferredT<int>(completeType1, out cancelationSource1);
                    deferred2 = TestHelper.GetNewDeferredT<int>(completeType2, out cancelationSource2);
                    deferredVoid = TestHelper.GetNewDeferredVoid(completeTypeVoid, out cancelationSourceVoid);
                    invoked = false;
                    Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferredVoid.Promise)
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
                () => completerVoid(deferredVoid, cancelationSourceVoid)
            );

            Promise.Config.UncaughtRejectionHandler = currentHandler;
        }

        [Test] // Only generate up to 2 parameters (more takes too long to test)
        public void DeferredsMayBeCompletedConcurrentlyAfterTheirPromisesArePassedToMerge_T4(
            [Values] CompleteType completeType0,
            [Values(CompleteType.Resolve)] CompleteType completeType1,
            [Values] CompleteType completeType2,
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
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    deferred0 = TestHelper.GetNewDeferredT<int>(completeType0, out cancelationSource0);
                    deferred1 = TestHelper.GetNewDeferredT<int>(completeType1, out cancelationSource1);
                    deferred2 = TestHelper.GetNewDeferredT<int>(completeType2, out cancelationSource2);
                    deferred3 = TestHelper.GetNewDeferredT<int>(completeType3, out cancelationSource3);
                    invoked = false;
                    Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise)
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
                () => completer3(deferred3, cancelationSource3)
            );

            Promise.Config.UncaughtRejectionHandler = currentHandler;
        }

        [Test] // Only generate up to 2 parameters (more takes too long to test)
        public void DeferredsMayBeCompletedConcurrentlyAfterTheirPromisesArePassedToMerge_T4void(
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
            threadHelper.ExecuteParallelActionsWithOffsets(false,
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
                () => completerVoid(deferredVoid, cancelationSourceVoid)
            );

            Promise.Config.UncaughtRejectionHandler = currentHandler;
        }

        [Test] // Only generate up to 2 parameters (more takes too long to test)
        public void DeferredsMayBeCompletedConcurrentlyAfterTheirPromisesArePassedToMerge_T5(
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
            threadHelper.ExecuteParallelActionsWithOffsets(false,
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
                () => completer4(deferred4, cancelationSource4)
            );

            Promise.Config.UncaughtRejectionHandler = currentHandler;
        }

        [Test] // Only generate up to 2 parameters (more takes too long to test)
        public void DeferredsMayBeCompletedConcurrentlyAfterTheirPromisesArePassedToMerge_T5void(
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
            threadHelper.ExecuteParallelActionsWithOffsets(false,
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
                () => completerVoid(deferredVoid, cancelationSourceVoid)
            );

            Promise.Config.UncaughtRejectionHandler = currentHandler;
        }

        [Test] // Only generate up to 2 parameters (more takes too long to test)
        public void DeferredsMayBeCompletedConcurrentlyAfterTheirPromisesArePassedToMerge_T6(
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
            threadHelper.ExecuteParallelActionsWithOffsets(false,
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
                () => completer5(deferred5, cancelationSource5)
            );

            Promise.Config.UncaughtRejectionHandler = currentHandler;
        }

        [Test] // Only generate up to 2 parameters (more takes too long to test)
        public void DeferredsMayBeCompletedConcurrentlyAfterTheirPromisesArePassedToMerge_T6void(
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
            threadHelper.ExecuteParallelActionsWithOffsets(false,
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
                () => completerVoid(deferredVoid, cancelationSourceVoid)
            );

            Promise.Config.UncaughtRejectionHandler = currentHandler;
        }

        [Test] // Only generate up to 2 parameters (more takes too long to test)
        public void DeferredsMayBeCompletedConcurrentlyAfterTheirPromisesArePassedToMerge_T7(
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
            threadHelper.ExecuteParallelActionsWithOffsets(false,
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
                () => completer6(deferred6, cancelationSource6)
            );

            Promise.Config.UncaughtRejectionHandler = currentHandler;
        }

        [Test] // Only generate up to 2 parameters (more takes too long to test)
        public void DeferredsMayBeCompletedConcurrentlyAfterTheirPromisesArePassedToMerge_T7void(
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
            threadHelper.ExecuteParallelActionsWithOffsets(false,
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
                () => completerVoid(deferredVoid, cancelationSourceVoid)
            );

            Promise.Config.UncaughtRejectionHandler = currentHandler;
        }
    }
}

#endif