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
            var deferred0 = default(Promise.Deferred);
            var deferred1 = default(Promise.Deferred);
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
            var deferred0 = default(Promise.Deferred);
            var deferred1 = default(Promise.Deferred);
            var deferred2 = default(Promise.Deferred);
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
            var deferred0 = default(Promise.Deferred);
            var deferred1 = default(Promise.Deferred);
            var deferred2 = default(Promise.Deferred);
            var deferred3 = default(Promise.Deferred);
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
            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
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
            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
            var deferred2 = default(Promise<int>.Deferred);
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
            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
            var deferred2 = default(Promise<int>.Deferred);
            var deferred3 = default(Promise<int>.Deferred);
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
        public void DeferredsMayBeResolvedConcurrentlyAfterTheirPromisesArePassedToRace_void0()
        {
            var deferred0 = default(Promise.Deferred);
            var deferred1 = default(Promise.Deferred);
            bool invoked = false;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    deferred0 = Promise.NewDeferred();
                    deferred1 = Promise.NewDeferred();
                    invoked = false;
                    Promise.Race(deferred0.Promise, deferred1.Promise)
                        .Then(() => invoked = true)
                        .Forget();
                },
                // Teardown
                () =>
                {
                    Promise.Manager.HandleCompletes();
                    Assert.IsTrue(invoked);
                },
                // Parallel actions
                () => deferred0.Resolve(),
                () => deferred1.Resolve()
            );
        }

        [Test]
        public void DeferredsMayBeResolvedConcurrentlyAfterTheirPromisesArePassedToRace_void1()
        {
            var deferred0 = default(Promise.Deferred);
            var deferred1 = default(Promise.Deferred);
            var deferred2 = default(Promise.Deferred);
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
                    Promise.Race(deferred0.Promise, deferred1.Promise, deferred2.Promise)
                        .Then(() => invoked = true)
                        .Forget();
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
                () => deferred2.Resolve()
            );
        }

        [Test]
        public void DeferredsMayBeResolvedConcurrentlyAfterTheirPromisesArePassedToRace_void2()
        {
            var deferred0 = default(Promise.Deferred);
            var deferred1 = default(Promise.Deferred);
            var deferred2 = default(Promise.Deferred);
            var deferred3 = default(Promise.Deferred);
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
                    Promise.Race(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise)
                        .Then(() => invoked = true)
                        .Forget();
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
                () => deferred3.Resolve()
            );
        }

        [Test]
        public void DeferredsMayBeResolvedConcurrentlyAfterTheirPromisesArePassedToRace_void3()
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
                    Promise.Race(deferreds.Select(d => d.Promise))
                        .Then(() => invoked = true)
                        .Forget();
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
                () => deferreds[3].Resolve()
            );
        }

        [Test]
        public void DeferredsMayBeResolvedConcurrentlyAfterTheirPromisesArePassedToRace_T0()
        {
            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
            bool invoked = false;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    deferred0 = Promise<int>.NewDeferred();
                    deferred1 = Promise<int>.NewDeferred();
                    invoked = false;
                    Promise.Race(deferred0.Promise, deferred1.Promise)
                        .Then(v => invoked = true) // Result is indeterminable, so don't check it.
                        .Forget();
                },
                // Teardown
                () =>
                {
                    Promise.Manager.HandleCompletes();
                    Assert.IsTrue(invoked);
                },
                // Parallel actions
                () => deferred0.Resolve(1),
                () => deferred1.Resolve(2)
            );
        }

        [Test]
        public void DeferredsMayBeResolvedConcurrentlyAfterTheirPromisesArePassedToRace_T1()
        {
            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
            var deferred2 = default(Promise<int>.Deferred);
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
                    Promise.Race(deferred0.Promise, deferred1.Promise, deferred2.Promise)
                        .Then(v => invoked = true) // Result is indeterminable, so don't check it.
                        .Forget();
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
                () => deferred2.Resolve(3)
            );
        }

        [Test]
        public void DeferredsMayBeResolvedConcurrentlyAfterTheirPromisesArePassedToRace_T2()
        {
            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
            var deferred2 = default(Promise<int>.Deferred);
            var deferred3 = default(Promise<int>.Deferred);
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
                    Promise.Race(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise)
                        .Then(v => invoked = true) // Result is indeterminable, so don't check it.
                        .Forget();
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
                () => deferred3.Resolve(4)
            );
        }

        [Test]
        public void DeferredsMayBeResolvedConcurrentlyAfterTheirPromisesArePassedToRace_T3()
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
                    Promise.Race(deferreds.Select(d => d.Promise))
                        .Then(v => invoked = true) // Result is indeterminable, so don't check it.
                        .Forget();
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
                () => deferreds[3].Resolve(4)
            );
        }

        private System.Action<UnhandledException> prevRejectionHandler;

        private void HandleUncaught(object expected)
        {
            // If a race promise is resolved and another is rejected, the rejection is unhandled. So we need to catch it here and make sure it's what we expect.
            prevRejectionHandler = Promise.Config.UncaughtRejectionHandler;
            Promise.Config.UncaughtRejectionHandler = ex =>
            {
                if (!ex.Value.Equals(expected))
                {
                    throw ex;
                }
            };
        }

        private void ResetRejectionHandler()
        {
            Promise.Config.UncaughtRejectionHandler = prevRejectionHandler;
        }

        [Test]
        public void ADeferredMayBeRejectedWhileItsPromiseIsPassedToRaceConcurrently_void0()
        {
            var deferred0 = default(Promise.Deferred);
            var deferred1 = default(Promise.Deferred);
            bool invoked = false;
            int expected = 1;

            HandleUncaught(expected);
            try
            {
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
            finally
            {
                ResetRejectionHandler();
            }
        }

        [Test]
        public void ADeferredMayBeRejectedWhileItsPromiseIsPassedToRaceConcurrently_void1()
        {
            var deferred0 = default(Promise.Deferred);
            var deferred1 = default(Promise.Deferred);
            var deferred2 = default(Promise.Deferred);
            bool invoked = false;
            int expected = 1;

            HandleUncaught(expected);
            try
            {
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
            finally
            {
                ResetRejectionHandler();
            }
        }

        [Test]
        public void ADeferredMayBeRejectedWhileItsPromiseIsPassedToRaceConcurrently_void2()
        {
            var deferred0 = default(Promise.Deferred);
            var deferred1 = default(Promise.Deferred);
            var deferred2 = default(Promise.Deferred);
            var deferred3 = default(Promise.Deferred);
            bool invoked = false;
            int expected = 1;

            HandleUncaught(expected);
            try
            {
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
            finally
            {
                ResetRejectionHandler();
            }
        }

        [Test]
        public void ADeferredMayBeRejectedWhileItsPromiseIsPassedToRaceConcurrently_void3()
        {
            Promise.Deferred[] deferreds = null;
            bool invoked = false;
            int expected = 1;

            HandleUncaught(expected);
            try
            {
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
            finally
            {
                ResetRejectionHandler();
            }
        }

        [Test]
        public void ADeferredMayBeRejectedWhileItsPromiseIsPassedToRaceConcurrently_T0()
        {
            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
            bool invoked = false;
            int expected = 1;

            HandleUncaught(expected);
            try
            {
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
            finally
            {
                ResetRejectionHandler();
            }
        }

        [Test]
        public void ADeferredMayBeRejectedWhileItsPromiseIsPassedToRaceConcurrently_T1()
        {
            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
            var deferred2 = default(Promise<int>.Deferred);
            bool invoked = false;
            int expected = 1;

            HandleUncaught(expected);
            try
            {
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
            finally
            {
                ResetRejectionHandler();
            }
        }

        [Test]
        public void ADeferredMayBeRejectedWhileItsPromiseIsPassedToRaceConcurrently_T2()
        {
            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
            var deferred2 = default(Promise<int>.Deferred);
            var deferred3 = default(Promise<int>.Deferred);
            bool invoked = false;
            int expected = 1;

            HandleUncaught(expected);
            try
            {
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
            finally
            {
                ResetRejectionHandler();
            }
        }

        [Test]
        public void ADeferredMayBeRejectedWhileItsPromiseIsPassedToRaceConcurrently_T3()
        {
            Promise<int>.Deferred[] deferreds = null;
            bool invoked = false;
            int expected = 1;

            HandleUncaught(expected);
            try
            {
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
            finally
            {
                ResetRejectionHandler();
            }
        }

        [Test]
        public void ADeferredMayBeRejectedConcurrentlyAfterItsPromiseIsPassedToRace_void0()
        {
            var deferred0 = default(Promise.Deferred);
            var deferred1 = default(Promise.Deferred);
            bool invoked = false;
            int expected = 1;

            HandleUncaught(expected);
            try
            {
                var threadHelper = new ThreadHelper();
                threadHelper.ExecuteParallelActionsWithOffsets(false,
                    // Setup
                    () =>
                    {
                        deferred0 = Promise.NewDeferred();
                        deferred1 = Promise.NewDeferred();
                        invoked = false;
                        Promise.Race(deferred0.Promise, deferred1.Promise)
                            .Finally(() => invoked = true) // State is indeterminable, so just make sure promise completes.
                            .Forget();
                    },
                    // Teardown
                    () =>
                    {
                        Promise.Manager.HandleCompletes();
                        Assert.IsTrue(invoked);
                    },
                    // Parallel actions
                    () => deferred0.Resolve(),
                    () => deferred1.Reject(expected)
                );
            }
            finally
            {
                ResetRejectionHandler();
            }
        }

        [Test]
        public void ADeferredMayBeRejectedConcurrentlyAfterItsPromiseIsPassedToRace_void1()
        {
            var deferred0 = default(Promise.Deferred);
            var deferred1 = default(Promise.Deferred);
            var deferred2 = default(Promise.Deferred);
            bool invoked = false;
            int expected = 1;

            HandleUncaught(expected);
            try
            {
                var threadHelper = new ThreadHelper();
                threadHelper.ExecuteParallelActionsWithOffsets(false,
                    // Setup
                    () =>
                    {
                        deferred0 = Promise.NewDeferred();
                        deferred1 = Promise.NewDeferred();
                        deferred2 = Promise.NewDeferred();
                        invoked = false;
                        Promise.Race(deferred0.Promise, deferred1.Promise, deferred2.Promise)
                            .Finally(() => invoked = true) // State is indeterminable, so just make sure promise completes.
                            .Forget();
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
                    () => deferred2.Reject(expected)
                );
            }
            finally
            {
                ResetRejectionHandler();
            }
        }

        [Test]
        public void ADeferredMayBeRejectedConcurrentlyAfterItsPromiseIsPassedToRace_void2()
        {
            var deferred0 = default(Promise.Deferred);
            var deferred1 = default(Promise.Deferred);
            var deferred2 = default(Promise.Deferred);
            var deferred3 = default(Promise.Deferred);
            bool invoked = false;
            int expected = 1;

            HandleUncaught(expected);
            try
            {
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
                        Promise.Race(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise)
                            .Finally(() => invoked = true) // State is indeterminable, so just make sure promise completes.
                            .Forget();
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
                    () => deferred3.Reject(expected)
                );
            }
            finally
            {
                ResetRejectionHandler();
            }
        }

        [Test]
        public void ADeferredMayBeRejectedConcurrentlyAfterItsPromiseIsPassedToRace_void3()
        {
            Promise.Deferred[] deferreds = null;
            bool invoked = false;
            int expected = 1;

            HandleUncaught(expected);
            try
            {
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
                        Promise.Race(deferreds.Select(d => d.Promise))
                            .Finally(() => invoked = true) // State is indeterminable, so just make sure promise completes.
                            .Forget();
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
                    () => deferreds[3].Reject(expected)
                );
            }
            finally
            {
                ResetRejectionHandler();
            }
        }

        [Test]
        public void ADeferredMayBeRejectedConcurrentlyAfterItsPromiseIsPassedToRace_T0()
        {
            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
            bool invoked = false;
            int expected = 1;

            HandleUncaught(expected);
            try
            {
                var threadHelper = new ThreadHelper();
                threadHelper.ExecuteParallelActionsWithOffsets(false,
                    // Setup
                    () =>
                    {
                        deferred0 = Promise<int>.NewDeferred();
                        deferred1 = Promise<int>.NewDeferred();
                        invoked = false;
                        Promise.Race(deferred0.Promise, deferred1.Promise)
                            .Finally(() => invoked = true) // State is indeterminable, so just make sure promise completes.
                            .Forget();
                    },
                    // Teardown
                    () =>
                    {
                        Promise.Manager.HandleCompletes();
                        Assert.IsTrue(invoked);
                    },
                    // Parallel actions
                    () => deferred0.Resolve(1),
                    () => deferred1.Reject(expected)
                );
            }
            finally
            {
                ResetRejectionHandler();
            }
        }

        [Test]
        public void ADeferredMayBeRejectedConcurrentlyAfterItsPromiseIsPassedToRace_T1()
        {
            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
            var deferred2 = default(Promise<int>.Deferred);
            bool invoked = false;
            int expected = 1;

            HandleUncaught(expected);
            try
            {
                var threadHelper = new ThreadHelper();
                threadHelper.ExecuteParallelActionsWithOffsets(false,
                    // Setup
                    () =>
                    {
                        deferred0 = Promise<int>.NewDeferred();
                        deferred1 = Promise<int>.NewDeferred();
                        deferred2 = Promise<int>.NewDeferred();
                        invoked = false;
                        Promise.Race(deferred0.Promise, deferred1.Promise, deferred2.Promise)
                            .Finally(() => invoked = true) // State is indeterminable, so just make sure promise completes.
                            .Forget();
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
                    () => deferred2.Reject(expected)
                );
            }
            finally
            {
                ResetRejectionHandler();
            }
        }

        [Test]
        public void ADeferredMayBeRejectedConcurrentlyAfterItsPromiseIsPassedToRace_T2()
        {
            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
            var deferred2 = default(Promise<int>.Deferred);
            var deferred3 = default(Promise<int>.Deferred);
            bool invoked = false;
            int expected = 1;

            HandleUncaught(expected);
            try
            {
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
                        Promise.Race(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise)
                            .Finally(() => invoked = true) // State is indeterminable, so just make sure promise completes.
                            .Forget();
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
                    () => deferred3.Reject(expected)
                );
            }
            finally
            {
                ResetRejectionHandler();
            }
        }

        [Test]
        public void ADeferredMayBeRejectedConcurrentlyAfterItsPromiseIsPassedToRace_T3()
        {
            Promise<int>.Deferred[] deferreds = null;
            bool invoked = false;
            int expected = 1;

            HandleUncaught(expected);
            try
            {
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
                        Promise.Race(deferreds.Select(d => d.Promise))
                            .Finally(() => invoked = true) // State is indeterminable, so just make sure promise completes.
                            .Forget();
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
                    () => deferreds[3].Reject(expected)
                );
            }
            finally
            {
                ResetRejectionHandler();
            }
        }

        [Test]
        public void ADeferredMayBeCanceledWhileItsPromiseIsPassedToRaceConcurrently_void0()
        {
            var cancelationSource = default(CancelationSource);
            var deferred0 = default(Promise.Deferred);
            var deferred1 = default(Promise.Deferred);
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
            var cancelationSource = default(CancelationSource);
            var deferred0 = default(Promise.Deferred);
            var deferred1 = default(Promise.Deferred);
            var deferred2 = default(Promise.Deferred);
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
                    cancelationSource.Dispose();
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
            var cancelationSource = default(CancelationSource);
            var deferred0 = default(Promise.Deferred);
            var deferred1 = default(Promise.Deferred);
            var deferred2 = default(Promise.Deferred);
            var deferred3 = default(Promise.Deferred);
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
                    cancelationSource.Dispose();
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
            var cancelationSource = default(CancelationSource);
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
                    cancelationSource.Dispose();
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
            var cancelationSource = default(CancelationSource);
            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
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
                    cancelationSource.Dispose();
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
            var cancelationSource = default(CancelationSource);
            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
            var deferred2 = default(Promise<int>.Deferred);
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
                    cancelationSource.Dispose();
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
            var cancelationSource = default(CancelationSource);
            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
            var deferred2 = default(Promise<int>.Deferred);
            var deferred3 = default(Promise<int>.Deferred);
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
                    cancelationSource.Dispose();
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
            var cancelationSource = default(CancelationSource);
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
                    cancelationSource.Dispose();
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

        [Test]
        public void ADeferredMayBeCanceledConcurrentlyAfterItsPromiseIsPassedToRace_void0()
        {
            var cancelationSource = default(CancelationSource);
            var deferred0 = default(Promise.Deferred);
            var deferred1 = default(Promise.Deferred);
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
                    Promise.Race(deferred0.Promise, deferred1.Promise)
                        .Finally(() => invoked = true) // State is indeterminable, so just make sure promise completes.
                        .Forget();
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
                () => cancelationSource.Cancel()
            );
        }

        [Test]
        public void ADeferredMayBeCanceledConcurrentlyAfterItsPromiseIsPassedToRace_void1()
        {
            var cancelationSource = default(CancelationSource);
            var deferred0 = default(Promise.Deferred);
            var deferred1 = default(Promise.Deferred);
            var deferred2 = default(Promise.Deferred);
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
                    Promise.Race(deferred0.Promise, deferred1.Promise, deferred2.Promise)
                        .Finally(() => invoked = true) // State is indeterminable, so just make sure promise completes.
                        .Forget();
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
                () => deferred1.Resolve(),
                () => cancelationSource.Cancel()
            );
        }

        [Test]
        public void ADeferredMayBeCanceledConcurrentlyAfterItsPromiseIsPassedToRace_void2()
        {
            var cancelationSource = default(CancelationSource);
            var deferred0 = default(Promise.Deferred);
            var deferred1 = default(Promise.Deferred);
            var deferred2 = default(Promise.Deferred);
            var deferred3 = default(Promise.Deferred);
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
                    Promise.Race(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise)
                        .Finally(() => invoked = true) // State is indeterminable, so just make sure promise completes.
                        .Forget();
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
                () => deferred1.Resolve(),
                () => deferred2.Resolve(),
                () => cancelationSource.Cancel()
            );
        }

        [Test]
        public void ADeferredMayBeCanceledConcurrentlyAfterItsPromiseIsPassedToRace_void3()
        {
            var cancelationSource = default(CancelationSource);
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
                    Promise.Race(deferreds.Select(d => d.Promise))
                        .Finally(() => invoked = true) // State is indeterminable, so just make sure promise completes.
                        .Forget();
                },
                // Teardown
                () =>
                {
                    cancelationSource.Dispose();
                    Promise.Manager.HandleCompletes();
                    Assert.IsTrue(invoked);
                },
                // Parallel actions
                () => deferreds[0].Resolve(),
                () => deferreds[1].Resolve(),
                () => deferreds[2].Resolve(),
                () => cancelationSource.Cancel()
            );
        }

        [Test]
        public void ADeferredMayBeCanceledConcurrentlyAfterItsPromiseIsPassedToRace_T0()
        {
            var cancelationSource = default(CancelationSource);
            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
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
                    Promise.Race(deferred0.Promise, deferred1.Promise)
                        .Finally(() => invoked = true) // State is indeterminable, so just make sure promise completes.
                        .Forget();
                },
                // Teardown
                () =>
                {
                    cancelationSource.Dispose();
                    Promise.Manager.HandleCompletes();
                    Assert.IsTrue(invoked);
                },
                // Parallel actions
                () => deferred0.Resolve(1),
                () => cancelationSource.Cancel()
            );
        }

        [Test]
        public void ADeferredMayBeCanceledConcurrentlyAfterItsPromiseIsPassedToRace_T1()
        {
            var cancelationSource = default(CancelationSource);
            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
            var deferred2 = default(Promise<int>.Deferred);
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
                    Promise.Race(deferred0.Promise, deferred1.Promise, deferred2.Promise)
                        .Finally(() => invoked = true) // State is indeterminable, so just make sure promise completes.
                        .Forget();
                },
                // Teardown
                () =>
                {
                    cancelationSource.Dispose();
                    Promise.Manager.HandleCompletes();
                    Assert.IsTrue(invoked);
                },
                // Parallel actions
                () => deferred0.Resolve(1),
                () => deferred1.Resolve(2),
                () => cancelationSource.Cancel()
            );
        }

        [Test]
        public void ADeferredMayBeCanceledConcurrentlyAfterItsPromiseIsPassedToRace_T2()
        {
            var cancelationSource = default(CancelationSource);
            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
            var deferred2 = default(Promise<int>.Deferred);
            var deferred3 = default(Promise<int>.Deferred);
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
                    Promise.Race(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise)
                        .Finally(() => invoked = true) // State is indeterminable, so just make sure promise completes.
                        .Forget();
                },
                // Teardown
                () =>
                {
                    cancelationSource.Dispose();
                    Promise.Manager.HandleCompletes();
                    Assert.IsTrue(invoked);
                },
                // Parallel actions
                () => deferred0.Resolve(1),
                () => deferred1.Resolve(2),
                () => deferred2.Resolve(3),
                () => cancelationSource.Cancel()
            );
        }

        [Test]
        public void ADeferredMayBeCanceledConcurrentlyAfterItsPromiseIsPassedToRace_T3()
        {
            var cancelationSource = default(CancelationSource);
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
                    Promise.Race(deferreds.Select(d => d.Promise))
                        .Finally(() => invoked = true) // State is indeterminable, so just make sure promise completes.
                        .Forget();
                },
                // Teardown
                () =>
                {
                    cancelationSource.Dispose();
                    Promise.Manager.HandleCompletes();
                    Assert.IsTrue(invoked);
                },
                // Parallel actions
                () => deferreds[0].Resolve(1),
                () => deferreds[1].Resolve(2),
                () => deferreds[2].Resolve(3),
                () => cancelationSource.Cancel()
            );
        }
    }
}

#endif