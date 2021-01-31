#if CSHARP_7_OR_LATER

#if !PROTO_PROMISE_PROGRESS_DISABLE
#define PROMISE_PROGRESS
#else
#undef PROMISE_PROGRESS
#endif

using NUnit.Framework;
using System.Linq;

namespace Proto.Promises.Tests.Threading
{
    public class RaceConcurrencyTests
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

        [Test]
        public void DeferredsMayBeResolvedWhileTheirPromisesArePassedToRaceConcurrently_void0()
        {
            Promise.Deferred deferred0 = default(Promise.Deferred);
            Promise.Deferred deferred1 = default(Promise.Deferred);
            bool invoked = false;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    deferred0 = Promise.NewDeferred();
                    deferred1 = Promise.NewDeferred();
                    invoked = false;
                },
                // Teardown
                () =>
                {
                    Promise.Manager.HandleCompletes();
                    Assert.IsTrue(invoked);
                },
                // Parallel actions
                () => deferred0.Resolve(),
                () => deferred1.Resolve(),
                () =>
                {
                    Promise.Race(deferred0.Promise, deferred1.Promise)
                        .Then(() => invoked = true)
                        .Forget();
                }
            );
        }

        [Test]
        public void DeferredsMayBeResolvedWhileTheirPromisesArePassedToRaceConcurrently_void1()
        {
            Promise.Deferred deferred0 = default(Promise.Deferred);
            Promise.Deferred deferred1 = default(Promise.Deferred);
            Promise.Deferred deferred2 = default(Promise.Deferred);
            bool invoked = false;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    deferred0 = Promise.NewDeferred();
                    deferred1 = Promise.NewDeferred();
                    deferred2 = Promise.NewDeferred();
                    invoked = false;
                },
                // Teardown
                () =>
                {
                    Promise.Manager.HandleCompletes();
                    Assert.IsTrue(invoked);
                },
                // Parallel actions
                () => deferred0.Resolve(),
                () => deferred1.Resolve(),
                () => deferred2.Resolve(),
                () =>
                {
                    Promise.Race(deferred0.Promise, deferred1.Promise, deferred2.Promise)
                        .Then(() => invoked = true)
                        .Forget();
                }
            );
        }

        [Test]
        public void DeferredsMayBeResolvedWhileTheirPromisesArePassedToRaceConcurrently_void2()
        {
            Promise.Deferred deferred0 = default(Promise.Deferred);
            Promise.Deferred deferred1 = default(Promise.Deferred);
            Promise.Deferred deferred2 = default(Promise.Deferred);
            Promise.Deferred deferred3 = default(Promise.Deferred);
            bool invoked = false;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    deferred0 = Promise.NewDeferred();
                    deferred1 = Promise.NewDeferred();
                    deferred2 = Promise.NewDeferred();
                    deferred3 = Promise.NewDeferred();
                    invoked = false;
                },
                // Teardown
                () =>
                {
                    Promise.Manager.HandleCompletes();
                    Assert.IsTrue(invoked);
                },
                // Parallel actions
                () => deferred0.Resolve(),
                () => deferred1.Resolve(),
                () => deferred2.Resolve(),
                () => deferred3.Resolve(),
                () =>
                {
                    Promise.Race(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise)
                        .Then(() => invoked = true)
                        .Forget();
                }
            );
        }

        [Test]
        public void DeferredsMayBeResolvedWhileTheirPromisesArePassedToRaceConcurrently_void3()
        {
            Promise.Deferred[] deferreds = null;
            bool invoked = false;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    deferreds = new Promise.Deferred[]
                    {
                        Promise.NewDeferred(),
                        Promise.NewDeferred(),
                        Promise.NewDeferred(),
                        Promise.NewDeferred()
                    };
                    invoked = false;
                },
                // Teardown
                () =>
                {
                    Promise.Manager.HandleCompletes();
                    Assert.IsTrue(invoked);
                },
                // Parallel actions
                () => deferreds[0].Resolve(),
                () => deferreds[1].Resolve(),
                () => deferreds[2].Resolve(),
                () => deferreds[3].Resolve(),
                () =>
                {
                    Promise.Race(deferreds.Select(d => d.Promise))
                        .Then(() => invoked = true)
                        .Forget();
                }
            );
        }

        [Test]
        public void DeferredsMayBeResolvedWhileTheirPromisesArePassedToRaceConcurrently_T0()
        {
            Promise<int>.Deferred deferred0 = default(Promise<int>.Deferred);
            Promise<int>.Deferred deferred1 = default(Promise<int>.Deferred);
            bool invoked = false;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    deferred0 = Promise<int>.NewDeferred();
                    deferred1 = Promise<int>.NewDeferred();
                    invoked = false;
                },
                // Teardown
                () =>
                {
                    Promise.Manager.HandleCompletes();
                    Assert.IsTrue(invoked);
                },
                // Parallel actions
                () => deferred0.Resolve(1),
                () => deferred1.Resolve(2),
                () =>
                {
                    Promise.Race(deferred0.Promise, deferred1.Promise)
                        .Then(v => invoked = true) // Result is indeterminable, so don't check it.
                        .Forget();
                }
            );
        }

        [Test]
        public void DeferredsMayBeResolvedWhileTheirPromisesArePassedToRaceConcurrently_T1()
        {
            Promise<int>.Deferred deferred0 = default(Promise<int>.Deferred);
            Promise<int>.Deferred deferred1 = default(Promise<int>.Deferred);
            Promise<int>.Deferred deferred2 = default(Promise<int>.Deferred);
            bool invoked = false;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    deferred0 = Promise<int>.NewDeferred();
                    deferred1 = Promise<int>.NewDeferred();
                    deferred2 = Promise<int>.NewDeferred();
                    invoked = false;
                },
                // Teardown
                () =>
                {
                    Promise.Manager.HandleCompletes();
                    Assert.IsTrue(invoked);
                },
                // Parallel actions
                () => deferred0.Resolve(1),
                () => deferred1.Resolve(2),
                () => deferred2.Resolve(3),
                () =>
                {
                    Promise.Race(deferred0.Promise, deferred1.Promise, deferred2.Promise)
                        .Then(v => invoked = true) // Result is indeterminable, so don't check it.
                        .Forget();
                }
            );
        }

        [Test]
        public void DeferredsMayBeResolvedWhileTheirPromisesArePassedToRaceConcurrently_T2()
        {
            Promise<int>.Deferred deferred0 = default(Promise<int>.Deferred);
            Promise<int>.Deferred deferred1 = default(Promise<int>.Deferred);
            Promise<int>.Deferred deferred2 = default(Promise<int>.Deferred);
            Promise<int>.Deferred deferred3 = default(Promise<int>.Deferred);
            bool invoked = false;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    deferred0 = Promise<int>.NewDeferred();
                    deferred1 = Promise<int>.NewDeferred();
                    deferred2 = Promise<int>.NewDeferred();
                    deferred3 = Promise<int>.NewDeferred();
                    invoked = false;
                },
                // Teardown
                () =>
                {
                    Promise.Manager.HandleCompletes();
                    Assert.IsTrue(invoked);
                },
                // Parallel actions
                () => deferred0.Resolve(1),
                () => deferred1.Resolve(2),
                () => deferred2.Resolve(3),
                () => deferred3.Resolve(4),
                () =>
                {
                    Promise.Race(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise)
                        .Then(v => invoked = true) // Result is indeterminable, so don't check it.
                        .Forget();
                }
            );
        }

        [Test]
        public void DeferredsMayBeResolvedWhileTheirPromisesArePassedToRaceConcurrently_T3()
        {
            Promise<int>.Deferred[] deferreds = null;
            bool invoked = false;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    deferreds = new Promise<int>.Deferred[]
                    {
                        Promise.NewDeferred<int>(),
                        Promise.NewDeferred<int>(),
                        Promise.NewDeferred<int>(),
                        Promise.NewDeferred<int>()
                    };
                    invoked = false;
                },
                // Teardown
                () =>
                {
                    Promise.Manager.HandleCompletes();
                    Assert.IsTrue(invoked);
                },
                // Parallel actions
                () => deferreds[0].Resolve(1),
                () => deferreds[1].Resolve(2),
                () => deferreds[2].Resolve(3),
                () => deferreds[3].Resolve(4),
                () =>
                {
                    Promise.Race(deferreds.Select(d => d.Promise))
                        .Then(v => invoked = true) // Result is indeterminable, so don't check it.
                        .Forget();
                }
            );
        }

        [Test]
        public void ADeferredMayBeRejectedWhileItsPromiseIsPassedToRaceConcurrently_void0()
        {
            Promise.Deferred deferred0 = default(Promise.Deferred);
            Promise.Deferred deferred1 = default(Promise.Deferred);
            bool invoked = false;
            int expected = 1;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    deferred0 = Promise.NewDeferred();
                    deferred1 = Promise.NewDeferred();
                    invoked = false;
                },
                // Teardown
                () =>
                {
                    Promise.Manager.HandleCompletes();
                    Assert.IsTrue(invoked);
                },
                // Parallel actions
                () => deferred0.Resolve(),
                () => deferred1.Reject(expected),
                () =>
                {
                    Promise.Race(deferred0.Promise, deferred1.Promise)
                        .Finally(() => invoked = true) // State is indeterminable, so just make sure promise completes.
                        .Forget();
                }
            );
        }

        [Test]
        public void ADeferredMayBeRejectedWhileItsPromiseIsPassedToRaceConcurrently_void1()
        {
            Promise.Deferred deferred0 = default(Promise.Deferred);
            Promise.Deferred deferred1 = default(Promise.Deferred);
            Promise.Deferred deferred2 = default(Promise.Deferred);
            bool invoked = false;
            int expected = 1;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    deferred0 = Promise.NewDeferred();
                    deferred1 = Promise.NewDeferred();
                    deferred2 = Promise.NewDeferred();
                    invoked = false;
                },
                // Teardown
                () =>
                {
                    Promise.Manager.HandleCompletes();
                    Assert.IsTrue(invoked);
                },
                // Parallel actions
                () => deferred0.Resolve(),
                () => deferred1.Resolve(),
                () => deferred2.Reject(expected),
                () =>
                {
                    Promise.Race(deferred0.Promise, deferred1.Promise, deferred2.Promise)
                        .Finally(() => invoked = true) // State is indeterminable, so just make sure promise completes.
                        .Forget();
                }
            );
        }

        [Test]
        public void ADeferredMayBeRejectedWhileItsPromiseIsPassedToRaceConcurrently_void2()
        {
            Promise.Deferred deferred0 = default(Promise.Deferred);
            Promise.Deferred deferred1 = default(Promise.Deferred);
            Promise.Deferred deferred2 = default(Promise.Deferred);
            Promise.Deferred deferred3 = default(Promise.Deferred);
            bool invoked = false;
            int expected = 1;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    deferred0 = Promise.NewDeferred();
                    deferred1 = Promise.NewDeferred();
                    deferred2 = Promise.NewDeferred();
                    deferred3 = Promise.NewDeferred();
                    invoked = false;
                },
                // Teardown
                () =>
                {
                    Promise.Manager.HandleCompletes();
                    Assert.IsTrue(invoked);
                },
                // Parallel actions
                () => deferred0.Resolve(),
                () => deferred1.Resolve(),
                () => deferred2.Resolve(),
                () => deferred3.Reject(expected),
                () =>
                {
                    Promise.Race(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise)
                        .Finally(() => invoked = true) // State is indeterminable, so just make sure promise completes.
                        .Forget();
                }
            );
        }

        [Test]
        public void ADeferredMayBeRejectedWhileItsPromiseIsPassedToRaceConcurrently_void3()
        {
            Promise.Deferred[] deferreds = null;
            bool invoked = false;
            int expected = 1;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    deferreds = new Promise.Deferred[]
                    {
                        Promise.NewDeferred(),
                        Promise.NewDeferred(),
                        Promise.NewDeferred(),
                        Promise.NewDeferred()
                    };
                    invoked = false;
                },
                // Teardown
                () =>
                {
                    Promise.Manager.HandleCompletes();
                    Assert.IsTrue(invoked);
                },
                // Parallel actions
                () => deferreds[0].Resolve(),
                () => deferreds[1].Resolve(),
                () => deferreds[2].Resolve(),
                () => deferreds[3].Reject(expected),
                () =>
                {
                    Promise.Race(deferreds.Select(d => d.Promise))
                        .Finally(() => invoked = true) // State is indeterminable, so just make sure promise completes.
                        .Forget();
                }
            );
        }

        [Test]
        public void ADeferredMayBeRejectedWhileItsPromiseIsPassedToRaceConcurrently_T0()
        {
            Promise<int>.Deferred deferred0 = default(Promise<int>.Deferred);
            Promise<int>.Deferred deferred1 = default(Promise<int>.Deferred);
            bool invoked = false;
            int expected = 1;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    deferred0 = Promise<int>.NewDeferred();
                    deferred1 = Promise<int>.NewDeferred();
                    invoked = false;
                },
                // Teardown
                () =>
                {
                    Promise.Manager.HandleCompletes();
                    Assert.IsTrue(invoked);
                },
                // Parallel actions
                () => deferred0.Resolve(1),
                () => deferred1.Reject(expected),
                () =>
                {
                    Promise.Race(deferred0.Promise, deferred1.Promise)
                        .Finally(() => invoked = true) // State is indeterminable, so just make sure promise completes.
                        .Forget();
                }
            );
        }

        [Test]
        public void ADeferredMayBeRejectedWhileItsPromiseIsPassedToRaceConcurrently_T1()
        {
            Promise<int>.Deferred deferred0 = default(Promise<int>.Deferred);
            Promise<int>.Deferred deferred1 = default(Promise<int>.Deferred);
            Promise<int>.Deferred deferred2 = default(Promise<int>.Deferred);
            bool invoked = false;
            int expected = 1;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    deferred0 = Promise<int>.NewDeferred();
                    deferred1 = Promise<int>.NewDeferred();
                    deferred2 = Promise<int>.NewDeferred();
                    invoked = false;
                },
                // Teardown
                () =>
                {
                    Promise.Manager.HandleCompletes();
                    Assert.IsTrue(invoked);
                },
                // Parallel actions
                () => deferred0.Resolve(1),
                () => deferred1.Resolve(2),
                () => deferred2.Reject(expected),
                () =>
                {
                    Promise.Race(deferred0.Promise, deferred1.Promise, deferred2.Promise)
                        .Finally(() => invoked = true) // State is indeterminable, so just make sure promise completes.
                        .Forget();
                }
            );
        }

        [Test]
        public void ADeferredMayBeRejectedWhileItsPromiseIsPassedToRaceConcurrently_T2()
        {
            Promise<int>.Deferred deferred0 = default(Promise<int>.Deferred);
            Promise<int>.Deferred deferred1 = default(Promise<int>.Deferred);
            Promise<int>.Deferred deferred2 = default(Promise<int>.Deferred);
            Promise<int>.Deferred deferred3 = default(Promise<int>.Deferred);
            bool invoked = false;
            int expected = 1;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    deferred0 = Promise<int>.NewDeferred();
                    deferred1 = Promise<int>.NewDeferred();
                    deferred2 = Promise<int>.NewDeferred();
                    deferred3 = Promise<int>.NewDeferred();
                    invoked = false;
                },
                // Teardown
                () =>
                {
                    Promise.Manager.HandleCompletes();
                    Assert.IsTrue(invoked);
                },
                // Parallel actions
                () => deferred0.Resolve(1),
                () => deferred1.Resolve(2),
                () => deferred2.Resolve(3),
                () => deferred3.Reject(expected),
                () =>
                {
                    Promise.Race(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise)
                        .Finally(() => invoked = true) // State is indeterminable, so just make sure promise completes.
                        .Forget();
                }
            );
        }

        [Test]
        public void ADeferredMayBeRejectedWhileItsPromiseIsPassedToRaceConcurrently_T3()
        {
            Promise<int>.Deferred[] deferreds = null;
            bool invoked = false;
            int expected = 1;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    deferreds = new Promise<int>.Deferred[]
                    {
                        Promise.NewDeferred<int>(),
                        Promise.NewDeferred<int>(),
                        Promise.NewDeferred<int>(),
                        Promise.NewDeferred<int>()
                    };
                    invoked = false;
                },
                // Teardown
                () =>
                {
                    Promise.Manager.HandleCompletes();
                    Assert.IsTrue(invoked);
                },
                // Parallel actions
                () => deferreds[0].Resolve(1),
                () => deferreds[1].Resolve(2),
                () => deferreds[2].Resolve(3),
                () => deferreds[3].Reject(expected),
                () =>
                {
                    Promise.Race(deferreds.Select(d => d.Promise))
                        .Finally(() => invoked = true) // State is indeterminable, so just make sure promise completes.
                        .Forget();
                }
            );
        }

        [Test]
        public void ADeferredMayBeCanceledWhileItsPromiseIsPassedToRaceConcurrently_void0()
        {
            CancelationSource cancelationSource = default(CancelationSource);
            Promise.Deferred deferred0 = default(Promise.Deferred);
            Promise.Deferred deferred1 = default(Promise.Deferred);
            bool invoked = false;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    cancelationSource = CancelationSource.New();
                    deferred0 = Promise.NewDeferred();
                    deferred1 = Promise.NewDeferred(cancelationSource.Token);
                    invoked = false;
                },
                // Teardown
                () =>
                {
                    cancelationSource.Dispose();
                    Promise.Manager.HandleCompletes();
                    Assert.IsTrue(invoked);
                },
                // Parallel actions
                () => deferred0.Resolve(),
                () => cancelationSource.Cancel(),
                () =>
                {
                    Promise.Race(deferred0.Promise, deferred1.Promise)
                        .Finally(() => invoked = true) // State is indeterminable, so just make sure promise completes.
                        .Forget();
                }
            );
        }

        [Test]
        public void ADeferredMayBeCanceledWhileItsPromiseIsPassedToRaceConcurrently_void1()
        {
            CancelationSource cancelationSource = default(CancelationSource);
            Promise.Deferred deferred0 = default(Promise.Deferred);
            Promise.Deferred deferred1 = default(Promise.Deferred);
            Promise.Deferred deferred2 = default(Promise.Deferred);
            bool invoked = false;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    cancelationSource = CancelationSource.New();
                    deferred0 = Promise.NewDeferred();
                    deferred1 = Promise.NewDeferred();
                    deferred2 = Promise.NewDeferred(cancelationSource.Token);
                    invoked = false;
                },
                // Teardown
                () =>
                {
                    Promise.Manager.HandleCompletes();
                    Assert.IsTrue(invoked);
                },
                // Parallel actions
                () => deferred0.Resolve(),
                () => deferred1.Resolve(),
                () => cancelationSource.Cancel(),
                () =>
                {
                    Promise.Race(deferred0.Promise, deferred1.Promise, deferred2.Promise)
                        .Finally(() => invoked = true) // State is indeterminable, so just make sure promise completes.
                        .Forget();
                }
            );
        }

        [Test]
        public void ADeferredMayBeCanceledWhileItsPromiseIsPassedToRaceConcurrently_void2()
        {
            CancelationSource cancelationSource = default(CancelationSource);
            Promise.Deferred deferred0 = default(Promise.Deferred);
            Promise.Deferred deferred1 = default(Promise.Deferred);
            Promise.Deferred deferred2 = default(Promise.Deferred);
            Promise.Deferred deferred3 = default(Promise.Deferred);
            bool invoked = false;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    cancelationSource = CancelationSource.New();
                    deferred0 = Promise.NewDeferred();
                    deferred1 = Promise.NewDeferred();
                    deferred2 = Promise.NewDeferred();
                    deferred3 = Promise.NewDeferred(cancelationSource.Token);
                    invoked = false;
                },
                // Teardown
                () =>
                {
                    Promise.Manager.HandleCompletes();
                    Assert.IsTrue(invoked);
                },
                // Parallel actions
                () => deferred0.Resolve(),
                () => deferred1.Resolve(),
                () => deferred2.Resolve(),
                () => cancelationSource.Cancel(),
                () =>
                {
                    Promise.Race(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise)
                        .Finally(() => invoked = true) // State is indeterminable, so just make sure promise completes.
                        .Forget();
                }
            );
        }

        [Test]
        public void ADeferredMayBeCanceledWhileItsPromiseIsPassedToRaceConcurrently_void3()
        {
            CancelationSource cancelationSource = default(CancelationSource);
            Promise.Deferred[] deferreds = null;
            bool invoked = false;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    cancelationSource = CancelationSource.New();
                    deferreds = new Promise.Deferred[]
                    {
                        Promise.NewDeferred(),
                        Promise.NewDeferred(),
                        Promise.NewDeferred(),
                        Promise.NewDeferred(cancelationSource.Token)
                    };
                    invoked = false;
                },
                // Teardown
                () =>
                {
                    Promise.Manager.HandleCompletes();
                    Assert.IsTrue(invoked);
                },
                // Parallel actions
                () => deferreds[0].Resolve(),
                () => deferreds[1].Resolve(),
                () => deferreds[2].Resolve(),
                () => cancelationSource.Cancel(),
                () =>
                {
                    Promise.Race(deferreds.Select(d => d.Promise))
                        .Finally(() => invoked = true) // State is indeterminable, so just make sure promise completes.
                        .Forget();
                }
            );
        }

        [Test]
        public void ADeferredMayBeCanceledWhileItsPromiseIsPassedToRaceConcurrently_T0()
        {
            CancelationSource cancelationSource = default(CancelationSource);
            Promise<int>.Deferred deferred0 = default(Promise<int>.Deferred);
            Promise<int>.Deferred deferred1 = default(Promise<int>.Deferred);
            bool invoked = false;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    cancelationSource = CancelationSource.New();
                    deferred0 = Promise<int>.NewDeferred();
                    deferred1 = Promise<int>.NewDeferred(cancelationSource.Token);
                    invoked = false;
                },
                // Teardown
                () =>
                {
                    Promise.Manager.HandleCompletes();
                    Assert.IsTrue(invoked);
                },
                // Parallel actions
                () => deferred0.Resolve(1),
                () => cancelationSource.Cancel(),
                () =>
                {
                    Promise.Race(deferred0.Promise, deferred1.Promise)
                        .Finally(() => invoked = true) // State is indeterminable, so just make sure promise completes.
                        .Forget();
                }
            );
        }

        [Test]
        public void ADeferredMayBeCanceledWhileItsPromiseIsPassedToRaceConcurrently_T1()
        {
            CancelationSource cancelationSource = default(CancelationSource);
            Promise<int>.Deferred deferred0 = default(Promise<int>.Deferred);
            Promise<int>.Deferred deferred1 = default(Promise<int>.Deferred);
            Promise<int>.Deferred deferred2 = default(Promise<int>.Deferred);
            bool invoked = false;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    cancelationSource = CancelationSource.New();
                    deferred0 = Promise<int>.NewDeferred();
                    deferred1 = Promise<int>.NewDeferred();
                    deferred2 = Promise<int>.NewDeferred(cancelationSource.Token);
                    invoked = false;
                },
                // Teardown
                () =>
                {
                    Promise.Manager.HandleCompletes();
                    Assert.IsTrue(invoked);
                },
                // Parallel actions
                () => deferred0.Resolve(1),
                () => deferred1.Resolve(2),
                () => cancelationSource.Cancel(),
                () =>
                {
                    Promise.Race(deferred0.Promise, deferred1.Promise, deferred2.Promise)
                        .Finally(() => invoked = true) // State is indeterminable, so just make sure promise completes.
                        .Forget();
                }
            );
        }

        [Test]
        public void ADeferredMayBeCanceledWhileItsPromiseIsPassedToRaceConcurrently_T2()
        {
            CancelationSource cancelationSource = default(CancelationSource);
            Promise<int>.Deferred deferred0 = default(Promise<int>.Deferred);
            Promise<int>.Deferred deferred1 = default(Promise<int>.Deferred);
            Promise<int>.Deferred deferred2 = default(Promise<int>.Deferred);
            Promise<int>.Deferred deferred3 = default(Promise<int>.Deferred);
            bool invoked = false;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    cancelationSource = CancelationSource.New();
                    deferred0 = Promise<int>.NewDeferred();
                    deferred1 = Promise<int>.NewDeferred();
                    deferred2 = Promise<int>.NewDeferred();
                    deferred3 = Promise<int>.NewDeferred(cancelationSource.Token);
                    invoked = false;
                },
                // Teardown
                () =>
                {
                    Promise.Manager.HandleCompletes();
                    Assert.IsTrue(invoked);
                },
                // Parallel actions
                () => deferred0.Resolve(1),
                () => deferred1.Resolve(2),
                () => deferred2.Resolve(3),
                () => cancelationSource.Cancel(),
                () =>
                {
                    Promise.Race(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise)
                        .Finally(() => invoked = true) // State is indeterminable, so just make sure promise completes.
                        .Forget();
                }
            );
        }

        [Test]
        public void ADeferredMayBeCanceledWhileItsPromiseIsPassedToRaceConcurrently_T3()
        {
            CancelationSource cancelationSource = default(CancelationSource);
            Promise<int>.Deferred[] deferreds = null;
            bool invoked = false;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    cancelationSource = CancelationSource.New();
                    deferreds = new Promise<int>.Deferred[]
                    {
                        Promise.NewDeferred<int>(),
                        Promise.NewDeferred<int>(),
                        Promise.NewDeferred<int>(),
                        Promise.NewDeferred<int>(cancelationSource.Token)
                    };
                    invoked = false;
                },
                // Teardown
                () =>
                {
                    Promise.Manager.HandleCompletes();
                    Assert.IsTrue(invoked);
                },
                // Parallel actions
                () => deferreds[0].Resolve(1),
                () => deferreds[1].Resolve(2),
                () => deferreds[2].Resolve(3),
                () => cancelationSource.Cancel(),
                () =>
                {
                    Promise.Race(deferreds.Select(d => d.Promise))
                        .Finally(() => invoked = true) // State is indeterminable, so just make sure promise completes.
                        .Forget();
                }
            );
        }
    }
}

#endif