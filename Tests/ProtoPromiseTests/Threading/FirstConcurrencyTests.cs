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
    public class FirstConcurrencyTests
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
        public void DeferredsMayBeResolvedWhileTheirPromisesArePassedToFirstConcurrently_void0()
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
                    Promise.First(deferred0.Promise, deferred1.Promise)
                        .Then(() => invoked = true)
                        .Forget();
                }
            );
        }

        [Test]
        public void DeferredsMayBeResolvedWhileTheirPromisesArePassedToFirstConcurrently_void1()
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
                    Promise.First(deferred0.Promise, deferred1.Promise, deferred2.Promise)
                        .Then(() => invoked = true)
                        .Forget();
                }
            );
        }

        [Test]
        public void DeferredsMayBeResolvedWhileTheirPromisesArePassedToFirstConcurrently_void2()
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
                    Promise.First(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise)
                        .Then(() => invoked = true)
                        .Forget();
                }
            );
        }

        [Test]
        public void DeferredsMayBeResolvedWhileTheirPromisesArePassedToFirstConcurrently_void3()
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
                    Promise.First(deferreds.Select(d => d.Promise))
                        .Then(() => invoked = true)
                        .Forget();
                }
            );
        }

        [Test]
        public void DeferredsMayBeResolvedWhileTheirPromisesArePassedToFirstConcurrently_T0()
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
                    Promise.First(deferred0.Promise, deferred1.Promise)
                        .Then(v => invoked = true) // Result is indeterminable, so don't check it.
                        .Forget();
                }
            );
        }

        [Test]
        public void DeferredsMayBeResolvedWhileTheirPromisesArePassedToFirstConcurrently_T1()
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
                    Promise.First(deferred0.Promise, deferred1.Promise, deferred2.Promise)
                        .Then(v => invoked = true) // Result is indeterminable, so don't check it.
                        .Forget();
                }
            );
        }

        [Test]
        public void DeferredsMayBeResolvedWhileTheirPromisesArePassedToFirstConcurrently_T2()
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
                    Promise.First(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise)
                        .Then(v => invoked = true) // Result is indeterminable, so don't check it.
                        .Forget();
                }
            );
        }

        [Test]
        public void DeferredsMayBeResolvedWhileTheirPromisesArePassedToFirstConcurrently_T3()
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
                    Promise.First(deferreds.Select(d => d.Promise))
                        .Then(v => invoked = true) // Result is indeterminable, so don't check it.
                        .Forget();
                }
            );
        }

        [Test]
        public void DeferredsMayBeResolvedConcurrentlyAfterTheirPromisesArePassedToFirst_void0()
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
                    Promise.First(deferred0.Promise, deferred1.Promise)
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
        public void DeferredsMayBeResolvedConcurrentlyAfterTheirPromisesArePassedToFirst_void1()
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
                    Promise.First(deferred0.Promise, deferred1.Promise, deferred2.Promise)
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
        public void DeferredsMayBeResolvedConcurrentlyAfterTheirPromisesArePassedToFirst_void2()
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
                    Promise.First(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise)
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
        public void DeferredsMayBeResolvedConcurrentlyAfterTheirPromisesArePassedToFirst_void3()
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
                    Promise.First(deferreds.Select(d => d.Promise))
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
        public void DeferredsMayBeResolvedConcurrentlyAfterTheirPromisesArePassedToFirst_T0()
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
                    Promise.First(deferred0.Promise, deferred1.Promise)
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
        public void DeferredsMayBeResolvedConcurrentlyAfterTheirPromisesArePassedToFirst_T1()
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
                    Promise.First(deferred0.Promise, deferred1.Promise, deferred2.Promise)
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
        public void DeferredsMayBeResolvedConcurrentlyAfterTheirPromisesArePassedToFirst_T2()
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
                    Promise.First(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise)
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
        public void DeferredsMayBeResolvedConcurrentlyAfterTheirPromisesArePassedToFirst_T3()
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
                    Promise.First(deferreds.Select(d => d.Promise))
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

        [Test]
        public void ADeferredMayBeRejectedWhileItsPromiseIsPassedToFirstConcurrently_void0()
        {
            var deferred0 = default(Promise.Deferred);
            var deferred1 = default(Promise.Deferred);
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
                    Promise.First(deferred0.Promise, deferred1.Promise)
                        .Finally(() => invoked = true) // State is indeterminable, so just make sure promise completes.
                        .Forget();
                }
            );
        }

        [Test]
        public void ADeferredMayBeRejectedWhileItsPromiseIsPassedToFirstConcurrently_void1()
        {
            var deferred0 = default(Promise.Deferred);
            var deferred1 = default(Promise.Deferred);
            var deferred2 = default(Promise.Deferred);
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
                    Promise.First(deferred0.Promise, deferred1.Promise, deferred2.Promise)
                        .Finally(() => invoked = true) // State is indeterminable, so just make sure promise completes.
                        .Forget();
                }
            );
        }

        [Test]
        public void ADeferredMayBeRejectedWhileItsPromiseIsPassedToFirstConcurrently_void2()
        {
            var deferred0 = default(Promise.Deferred);
            var deferred1 = default(Promise.Deferred);
            var deferred2 = default(Promise.Deferred);
            var deferred3 = default(Promise.Deferred);
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
                    Promise.First(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise)
                        .Finally(() => invoked = true) // State is indeterminable, so just make sure promise completes.
                        .Forget();
                }
            );
        }

        [Test]
        public void ADeferredMayBeRejectedWhileItsPromiseIsPassedToFirstConcurrently_void3()
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
                    Promise.First(deferreds.Select(d => d.Promise))
                        .Finally(() => invoked = true) // State is indeterminable, so just make sure promise completes.
                        .Forget();
                }
            );
        }

        [Test]
        public void ADeferredMayBeRejectedWhileItsPromiseIsPassedToFirstConcurrently_T0()
        {
            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
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
                    Promise.First(deferred0.Promise, deferred1.Promise)
                        .Finally(() => invoked = true) // State is indeterminable, so just make sure promise completes.
                        .Forget();
                }
            );
        }

        [Test]
        public void ADeferredMayBeRejectedWhileItsPromiseIsPassedToFirstConcurrently_T1()
        {
            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
            var deferred2 = default(Promise<int>.Deferred);
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
                    Promise.First(deferred0.Promise, deferred1.Promise, deferred2.Promise)
                        .Finally(() => invoked = true) // State is indeterminable, so just make sure promise completes.
                        .Forget();
                }
            );
        }

        [Test]
        public void ADeferredMayBeRejectedWhileItsPromiseIsPassedToFirstConcurrently_T2()
        {
            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
            var deferred2 = default(Promise<int>.Deferred);
            var deferred3 = default(Promise<int>.Deferred);
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
                    Promise.First(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise)
                        .Finally(() => invoked = true) // State is indeterminable, so just make sure promise completes.
                        .Forget();
                }
            );
        }

        [Test]
        public void ADeferredMayBeRejectedWhileItsPromiseIsPassedToFirstConcurrently_T3()
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
                    Promise.First(deferreds.Select(d => d.Promise))
                        .Finally(() => invoked = true) // State is indeterminable, so just make sure promise completes.
                        .Forget();
                }
            );
        }

        [Test]
        public void ADeferredMayBeRejectedConcurrentlyAfterItsPromiseIsPassedToFirst_void0()
        {
            var deferred0 = default(Promise.Deferred);
            var deferred1 = default(Promise.Deferred);
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
                    Promise.First(deferred0.Promise, deferred1.Promise)
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

        [Test]
        public void ADeferredMayBeRejectedConcurrentlyAfterItsPromiseIsPassedToFirst_void1()
        {
            var deferred0 = default(Promise.Deferred);
            var deferred1 = default(Promise.Deferred);
            var deferred2 = default(Promise.Deferred);
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
                    Promise.First(deferred0.Promise, deferred1.Promise, deferred2.Promise)
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

        [Test]
        public void ADeferredMayBeRejectedConcurrentlyAfterItsPromiseIsPassedToFirst_void2()
        {
            var deferred0 = default(Promise.Deferred);
            var deferred1 = default(Promise.Deferred);
            var deferred2 = default(Promise.Deferred);
            var deferred3 = default(Promise.Deferred);
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
                    Promise.First(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise)
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

        [Test]
        public void ADeferredMayBeRejectedConcurrentlyAfterItsPromiseIsPassedToFirst_void3()
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
                    Promise.First(deferreds.Select(d => d.Promise))
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

        [Test]
        public void ADeferredMayBeRejectedConcurrentlyAfterItsPromiseIsPassedToFirst_T0()
        {
            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
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
                    Promise.First(deferred0.Promise, deferred1.Promise)
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

        [Test]
        public void ADeferredMayBeRejectedConcurrentlyAfterItsPromiseIsPassedToFirst_T1()
        {
            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
            var deferred2 = default(Promise<int>.Deferred);
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
                    Promise.First(deferred0.Promise, deferred1.Promise, deferred2.Promise)
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

        [Test]
        public void ADeferredMayBeRejectedConcurrentlyAfterItsPromiseIsPassedToFirst_T2()
        {
            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
            var deferred2 = default(Promise<int>.Deferred);
            var deferred3 = default(Promise<int>.Deferred);
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
                    Promise.First(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise)
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

        [Test]
        public void ADeferredMayBeRejectedConcurrentlyAfterItsPromiseIsPassedToFirst_T3()
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
                    Promise.First(deferreds.Select(d => d.Promise))
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

        [Test]
        public void ADeferredMayBeCanceledWhileItsPromiseIsPassedToFirstConcurrently_void0()
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
                    Promise.First(deferred0.Promise, deferred1.Promise)
                        .Finally(() => invoked = true) // State is indeterminable, so just make sure promise completes.
                        .Forget();
                }
            );
        }

        [Test]
        public void ADeferredMayBeCanceledWhileItsPromiseIsPassedToFirstConcurrently_void1()
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
                    Promise.First(deferred0.Promise, deferred1.Promise, deferred2.Promise)
                        .Finally(() => invoked = true) // State is indeterminable, so just make sure promise completes.
                        .Forget();
                }
            );
        }

        [Test]
        public void ADeferredMayBeCanceledWhileItsPromiseIsPassedToFirstConcurrently_void2()
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
                    Promise.First(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise)
                        .Finally(() => invoked = true) // State is indeterminable, so just make sure promise completes.
                        .Forget();
                }
            );
        }

        [Test]
        public void ADeferredMayBeCanceledWhileItsPromiseIsPassedToFirstConcurrently_void3()
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
                    Promise.First(deferreds.Select(d => d.Promise))
                        .Finally(() => invoked = true) // State is indeterminable, so just make sure promise completes.
                        .Forget();
                }
            );
        }

        [Test]
        public void ADeferredMayBeCanceledWhileItsPromiseIsPassedToFirstConcurrently_T0()
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
                    Promise.First(deferred0.Promise, deferred1.Promise)
                        .Finally(() => invoked = true) // State is indeterminable, so just make sure promise completes.
                        .Forget();
                }
            );
        }

        [Test]
        public void ADeferredMayBeCanceledWhileItsPromiseIsPassedToFirstConcurrently_T1()
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
                    Promise.First(deferred0.Promise, deferred1.Promise, deferred2.Promise)
                        .Finally(() => invoked = true) // State is indeterminable, so just make sure promise completes.
                        .Forget();
                }
            );
        }

        [Test]
        public void ADeferredMayBeCanceledWhileItsPromiseIsPassedToFirstConcurrently_T2()
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
                    Promise.First(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise)
                        .Finally(() => invoked = true) // State is indeterminable, so just make sure promise completes.
                        .Forget();
                }
            );
        }

        [Test]
        public void ADeferredMayBeCanceledWhileItsPromiseIsPassedToFirstConcurrently_T3()
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
                    Promise.First(deferreds.Select(d => d.Promise))
                        .Finally(() => invoked = true) // State is indeterminable, so just make sure promise completes.
                        .Forget();
                }
            );
        }

        [Test]
        public void ADeferredMayBeCanceledConcurrentlyAfterItsPromiseIsPassedToFirst_void0()
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
                    Promise.First(deferred0.Promise, deferred1.Promise)
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
        public void ADeferredMayBeCanceledConcurrentlyAfterItsPromiseIsPassedToFirst_void1()
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
                    Promise.First(deferred0.Promise, deferred1.Promise, deferred2.Promise)
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
        public void ADeferredMayBeCanceledConcurrentlyAfterItsPromiseIsPassedToFirst_void2()
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
                    Promise.First(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise)
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
        public void ADeferredMayBeCanceledConcurrentlyAfterItsPromiseIsPassedToFirst_void3()
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
                    Promise.First(deferreds.Select(d => d.Promise))
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
        public void ADeferredMayBeCanceledConcurrentlyAfterItsPromiseIsPassedToFirst_T0()
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
                    Promise.First(deferred0.Promise, deferred1.Promise)
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
        public void ADeferredMayBeCanceledConcurrentlyAfterItsPromiseIsPassedToFirst_T1()
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
                    Promise.First(deferred0.Promise, deferred1.Promise, deferred2.Promise)
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
        public void ADeferredMayBeCanceledConcurrentlyAfterItsPromiseIsPassedToFirst_T2()
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
                    Promise.First(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise)
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
        public void ADeferredMayBeCanceledConcurrentlyAfterItsPromiseIsPassedToFirst_T3()
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
                    Promise.First(deferreds.Select(d => d.Promise))
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