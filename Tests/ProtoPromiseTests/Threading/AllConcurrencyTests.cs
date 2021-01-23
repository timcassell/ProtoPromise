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
    public class AllConcurrencyTests
    {
        [TearDown]
        public void Teardown()
        {
            TestHelper.Cleanup();
        }

        [Test]
        public void DeferredsMayBeResolvedWhileTheirPromisesArePassedToAllConcurrently_void0()
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
                    Promise.All(deferred0.Promise, deferred1.Promise)
                        .Then(() => invoked = true)
                        .Forget();
                }
            );
        }

        [Test]
        public void DeferredsMayBeResolvedWhileTheirPromisesArePassedToAllConcurrently_void1()
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
                    Promise.All(deferred0.Promise, deferred1.Promise, deferred2.Promise)
                        .Then(() => invoked = true)
                        .Forget();
                }
            );
        }

        [Test]
        public void DeferredsMayBeResolvedWhileTheirPromisesArePassedToAllConcurrently_void2()
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
                    Promise.All(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise)
                        .Then(() => invoked = true)
                        .Forget();
                }
            );
        }

        [Test]
        public void DeferredsMayBeResolvedWhileTheirPromisesArePassedToAllConcurrently_void3()
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
                    Promise.All(deferreds.Select(d => d.Promise))
                        .Then(() => invoked = true)
                        .Forget();
                }
            );
        }

        [Test]
        public void DeferredsMayBeResolvedWhileTheirPromisesArePassedToAllConcurrently_T0()
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
                    Promise.All(deferred0.Promise, deferred1.Promise)
                        .Then(v =>
                        {
                            Assert.AreEqual(1, v[0]);
                            Assert.AreEqual(2, v[1]);
                            invoked = true;
                        })
                        .Forget();
                }
            );
        }

        [Test]
        public void DeferredsMayBeResolvedWhileTheirPromisesArePassedToAllConcurrently_T1()
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
                    Promise.All(deferred0.Promise, deferred1.Promise, deferred2.Promise)
                        .Then(v =>
                        {
                            Assert.AreEqual(1, v[0]);
                            Assert.AreEqual(2, v[1]);
                            Assert.AreEqual(3, v[2]);
                            invoked = true;
                        })
                        .Forget();
                }
            );
        }

        [Test]
        public void DeferredsMayBeResolvedWhileTheirPromisesArePassedToAllConcurrently_T2()
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
                    Promise.All(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise)
                        .Then(v =>
                        {
                            Assert.AreEqual(1, v[0]);
                            Assert.AreEqual(2, v[1]);
                            Assert.AreEqual(3, v[2]);
                            Assert.AreEqual(4, v[3]);
                            invoked = true;
                        })
                        .Forget();
                }
            );
        }

        [Test]
        public void DeferredsMayBeResolvedWhileTheirPromisesArePassedToAllConcurrently_T3()
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
                    Promise.All(deferreds.Select(d => d.Promise))
                        .Then(v =>
                        {
                            Assert.AreEqual(1, v[0]);
                            Assert.AreEqual(2, v[1]);
                            Assert.AreEqual(3, v[2]);
                            Assert.AreEqual(4, v[3]);
                            invoked = true;
                        })
                        .Forget();
                }
            );
        }

        [Test]
        public void ADeferredMayBeRejectedWhileItsPromiseIsPassedToAllConcurrently_void0()
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
                    Promise.All(deferred0.Promise, deferred1.Promise)
                        .Catch((object s) =>
                        {
                            Assert.AreEqual(expected, s);
                            invoked = true;
                        })
                        .Forget();
                }
            );
        }

        [Test]
        public void ADeferredMayBeRejectedWhileItsPromiseIsPassedToAllConcurrently_void1()
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
                    Promise.All(deferred0.Promise, deferred1.Promise, deferred2.Promise)
                        .Catch((object s) =>
                        {
                            Assert.AreEqual(expected, s);
                            invoked = true;
                        })
                        .Forget();
                }
            );
        }

        [Test]
        public void ADeferredMayBeRejectedWhileItsPromiseIsPassedToAllConcurrently_void2()
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
                    Promise.All(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise)
                        .Catch((object s) =>
                        {
                            Assert.AreEqual(expected, s);
                            invoked = true;
                        })
                        .Forget();
                }
            );
        }

        [Test]
        public void ADeferredMayBeRejectedWhileItsPromiseIsPassedToAllConcurrently_void3()
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
                    Promise.All(deferreds.Select(d => d.Promise))
                        .Catch((object s) =>
                        {
                            Assert.AreEqual(expected, s);
                            invoked = true;
                        })
                        .Forget();
                }
            );
        }

        [Test]
        public void ADeferredMayBeRejectedWhileItsPromiseIsPassedToAllConcurrently_T0()
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
                    Promise.All(deferred0.Promise, deferred1.Promise)
                        .Catch((object s) =>
                        {
                            Assert.AreEqual(expected, s);
                            invoked = true;
                        })
                        .Forget();
                }
            );
        }

        [Test]
        public void ADeferredMayBeRejectedWhileItsPromiseIsPassedToAllConcurrently_T1()
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
                    Promise.All(deferred0.Promise, deferred1.Promise, deferred2.Promise)
                        .Catch((object s) =>
                        {
                            Assert.AreEqual(expected, s);
                            invoked = true;
                        })
                        .Forget();
                }
            );
        }

        [Test]
        public void ADeferredMayBeRejectedWhileItsPromiseIsPassedToAllConcurrently_T2()
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
                    Promise.All(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise)
                        .Catch((object s) =>
                        {
                            Assert.AreEqual(expected, s);
                            invoked = true;
                        })
                        .Forget();
                }
            );
        }

        [Test]
        public void ADeferredMayBeRejectedWhileItsPromiseIsPassedToAllConcurrently_T3()
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
                    Promise.All(deferreds.Select(d => d.Promise))
                        .Catch((object s) =>
                        {
                            Assert.AreEqual(expected, s);
                            invoked = true;
                        })
                        .Forget();
                }
            );
        }

        [Test]
        public void ADeferredMayBeCanceledWhileItsPromiseIsPassedToAllConcurrently_void0()
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
                    Promise.All(deferred0.Promise, deferred1.Promise)
                        .CatchCancelation(_ => invoked = true)
                        .Forget();
                }
            );
        }

        [Test]
        public void ADeferredMayBeCanceledWhileItsPromiseIsPassedToAllConcurrently_void1()
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
                    Promise.All(deferred0.Promise, deferred1.Promise, deferred2.Promise)
                        .CatchCancelation(_ => invoked = true)
                        .Forget();
                }
            );
        }

        [Test]
        public void ADeferredMayBeCanceledWhileItsPromiseIsPassedToAllConcurrently_void2()
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
                    Promise.All(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise)
                        .CatchCancelation(_ => invoked = true)
                        .Forget();
                }
            );
        }

        [Test]
        public void ADeferredMayBeCanceledWhileItsPromiseIsPassedToAllConcurrently_void3()
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
                    Promise.All(deferreds.Select(d => d.Promise))
                        .CatchCancelation(_ => invoked = true)
                        .Forget();
                }
            );
        }

        [Test]
        public void ADeferredMayBeCanceledWhileItsPromiseIsPassedToAllConcurrently_T0()
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
                    Promise.All(deferred0.Promise, deferred1.Promise)
                        .CatchCancelation(_ => invoked = true)
                        .Forget();
                }
            );
        }

        [Test]
        public void ADeferredMayBeCanceledWhileItsPromiseIsPassedToAllConcurrently_T1()
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
                    Promise.All(deferred0.Promise, deferred1.Promise, deferred2.Promise)
                        .CatchCancelation(_ => invoked = true)
                        .Forget();
                }
            );
        }

        [Test]
        public void ADeferredMayBeCanceledWhileItsPromiseIsPassedToAllConcurrently_T2()
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
                    Promise.All(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise)
                        .CatchCancelation(_ => invoked = true)
                        .Forget();
                }
            );
        }

        [Test]
        public void ADeferredMayBeCanceledWhileItsPromiseIsPassedToAllConcurrently_T3()
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
                    Promise.All(deferreds.Select(d => d.Promise))
                        .CatchCancelation(_ => invoked = true)
                        .Forget();
                }
            );
        }
    }
}

#endif