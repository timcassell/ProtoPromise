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
    public class MergeConcurrencyTests
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
        public void DeferredsMayBeResolvedWhileTheirPromisesArePassedToMergeConcurrently_T1void()
        {
            var deferred0 = default(Promise<int>.Deferred);
            var deferredVoid = default(Promise.Deferred);
            bool invoked = false;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    deferred0 = Promise.NewDeferred<int>();
                    deferredVoid = Promise.NewDeferred();
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
                () => deferredVoid.Resolve(),
                () =>
                {
                    Promise.Merge(deferred0.Promise, deferredVoid.Promise)
                        .Then(v =>
                        {
                            Assert.AreEqual(1, v);
                            invoked = true;
                        })
                        .Forget();
                }
            );
        }

        [Test]
        public void DeferredsMayBeResolvedWhileTheirPromisesArePassedToMergeConcurrently_T2()
        {
            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
            bool invoked = false;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    deferred0 = Promise.NewDeferred<int>();
                    deferred1 = Promise.NewDeferred<int>();
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
                    Promise.Merge(deferred0.Promise, deferred1.Promise)
                        .Then(cv =>
                        {
                            Assert.AreEqual(1, cv.Item1);
                            Assert.AreEqual(2, cv.Item2);
                            invoked = true;
                        })
                        .Forget();
                }
            );
        }

        [Test]
        public void DeferredsMayBeResolvedWhileTheirPromisesArePassedToMergeConcurrently_T2void()
        {
            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
            var deferredVoid = default(Promise.Deferred);
            bool invoked = false;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    deferred0 = Promise.NewDeferred<int>();
                    deferred1 = Promise.NewDeferred<int>();
                    deferredVoid = Promise.NewDeferred();
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
                () => deferredVoid.Resolve(),
                () =>
                {
                    Promise.Merge(deferred0.Promise, deferred1.Promise, deferredVoid.Promise)
                        .Then(cv =>
                        {
                            Assert.AreEqual(1, cv.Item1);
                            Assert.AreEqual(2, cv.Item2);
                            invoked = true;
                        })
                        .Forget();
                }
            );
        }

        [Test]
        public void DeferredsMayBeResolvedWhileTheirPromisesArePassedToMergeConcurrently_T3()
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
                    deferred0 = Promise.NewDeferred<int>();
                    deferred1 = Promise.NewDeferred<int>();
                    deferred2 = Promise.NewDeferred<int>();
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
                    Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise)
                        .Then(cv =>
                        {
                            Assert.AreEqual(1, cv.Item1);
                            Assert.AreEqual(2, cv.Item2);
                            Assert.AreEqual(3, cv.Item3);
                            invoked = true;
                        })
                        .Forget();
                }
            );
        }

        [Test]
        public void DeferredsMayBeResolvedWhileTheirPromisesArePassedToMergeConcurrently_T3void()
        {
            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
            var deferred2 = default(Promise<int>.Deferred);
            var deferredVoid = default(Promise.Deferred);
            bool invoked = false;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    deferred0 = Promise.NewDeferred<int>();
                    deferred1 = Promise.NewDeferred<int>();
                    deferred2 = Promise.NewDeferred<int>();
                    deferredVoid = Promise.NewDeferred();
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
                () => deferredVoid.Resolve(),
                () =>
                {
                    Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferredVoid.Promise)
                        .Then(cv =>
                        {
                            Assert.AreEqual(1, cv.Item1);
                            Assert.AreEqual(2, cv.Item2);
                            Assert.AreEqual(3, cv.Item3);
                            invoked = true;
                        })
                        .Forget();
                }
            );
        }

        [Test]
        public void DeferredsMayBeResolvedWhileTheirPromisesArePassedToMergeConcurrently_T4()
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
                    deferred0 = Promise.NewDeferred<int>();
                    deferred1 = Promise.NewDeferred<int>();
                    deferred2 = Promise.NewDeferred<int>();
                    deferred3 = Promise.NewDeferred<int>();
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
                    Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise)
                        .Then(cv =>
                        {
                            Assert.AreEqual(1, cv.Item1);
                            Assert.AreEqual(2, cv.Item2);
                            Assert.AreEqual(3, cv.Item3);
                            Assert.AreEqual(4, cv.Item4);
                            invoked = true;
                        })
                        .Forget();
                }
            );
        }

        [Test]
        public void DeferredsMayBeResolvedWhileTheirPromisesArePassedToMergeConcurrently_T4void()
        {
            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
            var deferred2 = default(Promise<int>.Deferred);
            var deferred3 = default(Promise<int>.Deferred);
            var deferredVoid = default(Promise.Deferred);
            bool invoked = false;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    deferred0 = Promise.NewDeferred<int>();
                    deferred1 = Promise.NewDeferred<int>();
                    deferred2 = Promise.NewDeferred<int>();
                    deferred3 = Promise.NewDeferred<int>();
                    deferredVoid = Promise.NewDeferred();
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
                () => deferredVoid.Resolve(),
                () =>
                {
                    Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise, deferredVoid.Promise)
                        .Then(cv =>
                        {
                            Assert.AreEqual(1, cv.Item1);
                            Assert.AreEqual(2, cv.Item2);
                            Assert.AreEqual(3, cv.Item3);
                            Assert.AreEqual(4, cv.Item4);
                            invoked = true;
                        })
                        .Forget();
                }
            );
        }

        [Test]
        public void DeferredsMayBeResolvedWhileTheirPromisesArePassedToMergeConcurrently_T5()
        {
            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
            var deferred2 = default(Promise<int>.Deferred);
            var deferred3 = default(Promise<int>.Deferred);
            var deferred4 = default(Promise<int>.Deferred);
            bool invoked = false;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    deferred0 = Promise.NewDeferred<int>();
                    deferred1 = Promise.NewDeferred<int>();
                    deferred2 = Promise.NewDeferred<int>();
                    deferred3 = Promise.NewDeferred<int>();
                    deferred4 = Promise.NewDeferred<int>();
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
                () => deferred4.Resolve(5),
                () =>
                {
                    Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise, deferred4.Promise)
                        .Then(cv =>
                        {
                            Assert.AreEqual(1, cv.Item1);
                            Assert.AreEqual(2, cv.Item2);
                            Assert.AreEqual(3, cv.Item3);
                            Assert.AreEqual(4, cv.Item4);
                            Assert.AreEqual(5, cv.Item5);
                            invoked = true;
                        })
                        .Forget();
                }
            );
        }

        [Test]
        public void DeferredsMayBeResolvedWhileTheirPromisesArePassedToMergeConcurrently_T5void()
        {
            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
            var deferred2 = default(Promise<int>.Deferred);
            var deferred3 = default(Promise<int>.Deferred);
            var deferred4 = default(Promise<int>.Deferred);
            var deferredVoid = default(Promise.Deferred);
            bool invoked = false;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    deferred0 = Promise.NewDeferred<int>();
                    deferred1 = Promise.NewDeferred<int>();
                    deferred2 = Promise.NewDeferred<int>();
                    deferred3 = Promise.NewDeferred<int>();
                    deferred4 = Promise.NewDeferred<int>();
                    deferredVoid = Promise.NewDeferred();
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
                () => deferred4.Resolve(5),
                () => deferredVoid.Resolve(),
                () =>
                {
                    Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise, deferred4.Promise, deferredVoid.Promise)
                        .Then(cv =>
                        {
                            Assert.AreEqual(1, cv.Item1);
                            Assert.AreEqual(2, cv.Item2);
                            Assert.AreEqual(3, cv.Item3);
                            Assert.AreEqual(4, cv.Item4);
                            Assert.AreEqual(5, cv.Item5);
                            invoked = true;
                        })
                        .Forget();
                }
            );
        }

        [Test]
        public void DeferredsMayBeResolvedWhileTheirPromisesArePassedToMergeConcurrently_T6()
        {
            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
            var deferred2 = default(Promise<int>.Deferred);
            var deferred3 = default(Promise<int>.Deferred);
            var deferred4 = default(Promise<int>.Deferred);
            var deferred5 = default(Promise<int>.Deferred);
            bool invoked = false;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    deferred0 = Promise.NewDeferred<int>();
                    deferred1 = Promise.NewDeferred<int>();
                    deferred2 = Promise.NewDeferred<int>();
                    deferred3 = Promise.NewDeferred<int>();
                    deferred4 = Promise.NewDeferred<int>();
                    deferred5 = Promise.NewDeferred<int>();
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
                () => deferred4.Resolve(5),
                () => deferred5.Resolve(6),
                () =>
                {
                    Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise, deferred4.Promise, deferred5.Promise)
                        .Then(cv =>
                        {
                            Assert.AreEqual(1, cv.Item1);
                            Assert.AreEqual(2, cv.Item2);
                            Assert.AreEqual(3, cv.Item3);
                            Assert.AreEqual(4, cv.Item4);
                            Assert.AreEqual(5, cv.Item5);
                            Assert.AreEqual(6, cv.Item6);
                            invoked = true;
                        })
                        .Forget();
                }
            );
        }

        [Test]
        public void DeferredsMayBeResolvedWhileTheirPromisesArePassedToMergeConcurrently_T6void()
        {
            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
            var deferred2 = default(Promise<int>.Deferred);
            var deferred3 = default(Promise<int>.Deferred);
            var deferred4 = default(Promise<int>.Deferred);
            var deferred5 = default(Promise<int>.Deferred);
            var deferredVoid = default(Promise.Deferred);
            bool invoked = false;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    deferred0 = Promise.NewDeferred<int>();
                    deferred1 = Promise.NewDeferred<int>();
                    deferred2 = Promise.NewDeferred<int>();
                    deferred3 = Promise.NewDeferred<int>();
                    deferred4 = Promise.NewDeferred<int>();
                    deferred5 = Promise.NewDeferred<int>();
                    deferredVoid = Promise.NewDeferred();
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
                () => deferred4.Resolve(5),
                () => deferred5.Resolve(6),
                () => deferredVoid.Resolve(),
                () =>
                {
                    Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise, deferred4.Promise, deferred5.Promise, deferredVoid.Promise)
                        .Then(cv =>
                        {
                            Assert.AreEqual(1, cv.Item1);
                            Assert.AreEqual(2, cv.Item2);
                            Assert.AreEqual(3, cv.Item3);
                            Assert.AreEqual(4, cv.Item4);
                            Assert.AreEqual(5, cv.Item5);
                            Assert.AreEqual(6, cv.Item6);
                            invoked = true;
                        })
                        .Forget();
                }
            );
        }

        [Test]
        public void DeferredsMayBeResolvedWhileTheirPromisesArePassedToMergeConcurrently_T7()
        {
            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
            var deferred2 = default(Promise<int>.Deferred);
            var deferred3 = default(Promise<int>.Deferred);
            var deferred4 = default(Promise<int>.Deferred);
            var deferred5 = default(Promise<int>.Deferred);
            var deferred6 = default(Promise<int>.Deferred);
            bool invoked = false;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    deferred0 = Promise.NewDeferred<int>();
                    deferred1 = Promise.NewDeferred<int>();
                    deferred2 = Promise.NewDeferred<int>();
                    deferred3 = Promise.NewDeferred<int>();
                    deferred4 = Promise.NewDeferred<int>();
                    deferred5 = Promise.NewDeferred<int>();
                    deferred6 = Promise.NewDeferred<int>();
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
                () => deferred4.Resolve(5),
                () => deferred5.Resolve(6),
                () => deferred6.Resolve(7),
                () =>
                {
                    Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise, deferred4.Promise, deferred5.Promise, deferred6.Promise)
                        .Then(cv =>
                        {
                            Assert.AreEqual(1, cv.Item1);
                            Assert.AreEqual(2, cv.Item2);
                            Assert.AreEqual(3, cv.Item3);
                            Assert.AreEqual(4, cv.Item4);
                            Assert.AreEqual(5, cv.Item5);
                            Assert.AreEqual(6, cv.Item6);
                            Assert.AreEqual(7, cv.Item7);
                            invoked = true;
                        })
                        .Forget();
                }
            );
        }

        [Test]
        public void DeferredsMayBeResolvedWhileTheirPromisesArePassedToMergeConcurrently_T7void()
        {
            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
            var deferred2 = default(Promise<int>.Deferred);
            var deferred3 = default(Promise<int>.Deferred);
            var deferred4 = default(Promise<int>.Deferred);
            var deferred5 = default(Promise<int>.Deferred);
            var deferred6 = default(Promise<int>.Deferred);
            var deferredVoid = default(Promise.Deferred);
            bool invoked = false;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    deferred0 = Promise.NewDeferred<int>();
                    deferred1 = Promise.NewDeferred<int>();
                    deferred2 = Promise.NewDeferred<int>();
                    deferred3 = Promise.NewDeferred<int>();
                    deferred4 = Promise.NewDeferred<int>();
                    deferred5 = Promise.NewDeferred<int>();
                    deferred6 = Promise.NewDeferred<int>();
                    deferredVoid = Promise.NewDeferred();
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
                () => deferred4.Resolve(5),
                () => deferred5.Resolve(6),
                () => deferred6.Resolve(7),
                () => deferredVoid.Resolve(),
                () =>
                {
                    Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise, deferred4.Promise, deferred5.Promise, deferred6.Promise, deferredVoid.Promise)
                        .Then(cv =>
                        {
                            Assert.AreEqual(1, cv.Item1);
                            Assert.AreEqual(2, cv.Item2);
                            Assert.AreEqual(3, cv.Item3);
                            Assert.AreEqual(4, cv.Item4);
                            Assert.AreEqual(5, cv.Item5);
                            Assert.AreEqual(6, cv.Item6);
                            Assert.AreEqual(7, cv.Item7);
                            invoked = true;
                        })
                        .Forget();
                }
            );
        }

        [Test]
        public void DeferredsMayBeResolvedConcurrentlyAfterTheirPromisesArePassedToMerge_T1void()
        {
            var deferred0 = default(Promise<int>.Deferred);
            var deferredVoid = default(Promise.Deferred);
            bool invoked = false;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    deferred0 = Promise.NewDeferred<int>();
                    deferredVoid = Promise.NewDeferred();
                    invoked = false;
                    Promise.Merge(deferred0.Promise, deferredVoid.Promise)
                        .Then(v =>
                        {
                            Assert.AreEqual(1, v);
                            invoked = true;
                        })
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
                () => deferredVoid.Resolve()
            );
        }

        [Test]
        public void DeferredsMayBeResolvedConcurrentlyAfterTheirPromisesArePassedToMerge_T2()
        {
            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
            bool invoked = false;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    deferred0 = Promise.NewDeferred<int>();
                    deferred1 = Promise.NewDeferred<int>();
                    invoked = false;
                    Promise.Merge(deferred0.Promise, deferred1.Promise)
                        .Then(cv =>
                        {
                            Assert.AreEqual(1, cv.Item1);
                            Assert.AreEqual(2, cv.Item2);
                            invoked = true;
                        })
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
        public void DeferredsMayBeResolvedConcurrentlyAfterTheirPromisesArePassedToMerge_T2void()
        {
            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
            var deferredVoid = default(Promise.Deferred);
            bool invoked = false;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    deferred0 = Promise.NewDeferred<int>();
                    deferred1 = Promise.NewDeferred<int>();
                    deferredVoid = Promise.NewDeferred();
                    invoked = false;
                    Promise.Merge(deferred0.Promise, deferred1.Promise, deferredVoid.Promise)
                        .Then(cv =>
                        {
                            Assert.AreEqual(1, cv.Item1);
                            Assert.AreEqual(2, cv.Item2);
                            invoked = true;
                        })
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
                () => deferredVoid.Resolve()
            );
        }

        [Test]
        public void DeferredsMayBeResolvedConcurrentlyAfterTheirPromisesArePassedToMerge_T3()
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
                    deferred0 = Promise.NewDeferred<int>();
                    deferred1 = Promise.NewDeferred<int>();
                    deferred2 = Promise.NewDeferred<int>();
                    invoked = false;
                    Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise)
                        .Then(cv =>
                        {
                            Assert.AreEqual(1, cv.Item1);
                            Assert.AreEqual(2, cv.Item2);
                            Assert.AreEqual(3, cv.Item3);
                            invoked = true;
                        })
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
        public void DeferredsMayBeResolvedConcurrentlyAfterTheirPromisesArePassedToMerge_T3void()
        {
            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
            var deferred2 = default(Promise<int>.Deferred);
            var deferredVoid = default(Promise.Deferred);
            bool invoked = false;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    deferred0 = Promise.NewDeferred<int>();
                    deferred1 = Promise.NewDeferred<int>();
                    deferred2 = Promise.NewDeferred<int>();
                    deferredVoid = Promise.NewDeferred();
                    invoked = false;
                    Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferredVoid.Promise)
                        .Then(cv =>
                        {
                            Assert.AreEqual(1, cv.Item1);
                            Assert.AreEqual(2, cv.Item2);
                            Assert.AreEqual(3, cv.Item3);
                            invoked = true;
                        })
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
                () => deferredVoid.Resolve()
            );
        }

        [Test]
        public void DeferredsMayBeResolvedConcurrentlyAfterTheirPromisesArePassedToMerge_T4()
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
                    deferred0 = Promise.NewDeferred<int>();
                    deferred1 = Promise.NewDeferred<int>();
                    deferred2 = Promise.NewDeferred<int>();
                    deferred3 = Promise.NewDeferred<int>();
                    invoked = false;
                    Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise)
                        .Then(cv =>
                        {
                            Assert.AreEqual(1, cv.Item1);
                            Assert.AreEqual(2, cv.Item2);
                            Assert.AreEqual(3, cv.Item3);
                            Assert.AreEqual(4, cv.Item4);
                            invoked = true;
                        })
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
        public void DeferredsMayBeResolvedConcurrentlyAfterTheirPromisesArePassedToMerge_T4void()
        {
            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
            var deferred2 = default(Promise<int>.Deferred);
            var deferred3 = default(Promise<int>.Deferred);
            var deferredVoid = default(Promise.Deferred);
            bool invoked = false;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    deferred0 = Promise.NewDeferred<int>();
                    deferred1 = Promise.NewDeferred<int>();
                    deferred2 = Promise.NewDeferred<int>();
                    deferred3 = Promise.NewDeferred<int>();
                    deferredVoid = Promise.NewDeferred();
                    invoked = false;
                    Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise, deferredVoid.Promise)
                        .Then(cv =>
                        {
                            Assert.AreEqual(1, cv.Item1);
                            Assert.AreEqual(2, cv.Item2);
                            Assert.AreEqual(3, cv.Item3);
                            Assert.AreEqual(4, cv.Item4);
                            invoked = true;
                        })
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
                () => deferred3.Resolve(4),
                () => deferredVoid.Resolve()
            );
        }

        [Test]
        public void DeferredsMayBeResolvedConcurrentlyAfterTheirPromisesArePassedToMerge_T5()
        {
            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
            var deferred2 = default(Promise<int>.Deferred);
            var deferred3 = default(Promise<int>.Deferred);
            var deferred4 = default(Promise<int>.Deferred);
            bool invoked = false;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    deferred0 = Promise.NewDeferred<int>();
                    deferred1 = Promise.NewDeferred<int>();
                    deferred2 = Promise.NewDeferred<int>();
                    deferred3 = Promise.NewDeferred<int>();
                    deferred4 = Promise.NewDeferred<int>();
                    invoked = false;
                    Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise, deferred4.Promise)
                        .Then(cv =>
                        {
                            Assert.AreEqual(1, cv.Item1);
                            Assert.AreEqual(2, cv.Item2);
                            Assert.AreEqual(3, cv.Item3);
                            Assert.AreEqual(4, cv.Item4);
                            Assert.AreEqual(5, cv.Item5);
                            invoked = true;
                        })
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
                () => deferred3.Resolve(4),
                () => deferred4.Resolve(5)
            );
        }

        [Test]
        public void DeferredsMayBeResolvedConcurrentlyAfterTheirPromisesArePassedToMerge_T5void()
        {
            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
            var deferred2 = default(Promise<int>.Deferred);
            var deferred3 = default(Promise<int>.Deferred);
            var deferred4 = default(Promise<int>.Deferred);
            var deferredVoid = default(Promise.Deferred);
            bool invoked = false;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    deferred0 = Promise.NewDeferred<int>();
                    deferred1 = Promise.NewDeferred<int>();
                    deferred2 = Promise.NewDeferred<int>();
                    deferred3 = Promise.NewDeferred<int>();
                    deferred4 = Promise.NewDeferred<int>();
                    deferredVoid = Promise.NewDeferred();
                    invoked = false;
                    Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise, deferred4.Promise, deferredVoid.Promise)
                        .Then(cv =>
                        {
                            Assert.AreEqual(1, cv.Item1);
                            Assert.AreEqual(2, cv.Item2);
                            Assert.AreEqual(3, cv.Item3);
                            Assert.AreEqual(4, cv.Item4);
                            Assert.AreEqual(5, cv.Item5);
                            invoked = true;
                        })
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
                () => deferred3.Resolve(4),
                () => deferred4.Resolve(5),
                () => deferredVoid.Resolve()
            );
        }

        [Test]
        public void DeferredsMayBeResolvedConcurrentlyAfterTheirPromisesArePassedToMerge_T6()
        {
            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
            var deferred2 = default(Promise<int>.Deferred);
            var deferred3 = default(Promise<int>.Deferred);
            var deferred4 = default(Promise<int>.Deferred);
            var deferred5 = default(Promise<int>.Deferred);
            bool invoked = false;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    deferred0 = Promise.NewDeferred<int>();
                    deferred1 = Promise.NewDeferred<int>();
                    deferred2 = Promise.NewDeferred<int>();
                    deferred3 = Promise.NewDeferred<int>();
                    deferred4 = Promise.NewDeferred<int>();
                    deferred5 = Promise.NewDeferred<int>();
                    invoked = false;
                    Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise, deferred4.Promise, deferred5.Promise)
                        .Then(cv =>
                        {
                            Assert.AreEqual(1, cv.Item1);
                            Assert.AreEqual(2, cv.Item2);
                            Assert.AreEqual(3, cv.Item3);
                            Assert.AreEqual(4, cv.Item4);
                            Assert.AreEqual(5, cv.Item5);
                            Assert.AreEqual(6, cv.Item6);
                            invoked = true;
                        })
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
                () => deferred3.Resolve(4),
                () => deferred4.Resolve(5),
                () => deferred5.Resolve(6)
            );
        }

        [Test]
        public void DeferredsMayBeResolvedConcurrentlyAfterTheirPromisesArePassedToMerge_T6void()
        {
            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
            var deferred2 = default(Promise<int>.Deferred);
            var deferred3 = default(Promise<int>.Deferred);
            var deferred4 = default(Promise<int>.Deferred);
            var deferred5 = default(Promise<int>.Deferred);
            var deferredVoid = default(Promise.Deferred);
            bool invoked = false;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    deferred0 = Promise.NewDeferred<int>();
                    deferred1 = Promise.NewDeferred<int>();
                    deferred2 = Promise.NewDeferred<int>();
                    deferred3 = Promise.NewDeferred<int>();
                    deferred4 = Promise.NewDeferred<int>();
                    deferred5 = Promise.NewDeferred<int>();
                    deferredVoid = Promise.NewDeferred();
                    invoked = false;
                    Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise, deferred4.Promise, deferred5.Promise, deferredVoid.Promise)
                        .Then(cv =>
                        {
                            Assert.AreEqual(1, cv.Item1);
                            Assert.AreEqual(2, cv.Item2);
                            Assert.AreEqual(3, cv.Item3);
                            Assert.AreEqual(4, cv.Item4);
                            Assert.AreEqual(5, cv.Item5);
                            Assert.AreEqual(6, cv.Item6);
                            invoked = true;
                        })
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
                () => deferred3.Resolve(4),
                () => deferred4.Resolve(5),
                () => deferred5.Resolve(6),
                () => deferredVoid.Resolve()
            );
        }

        [Test]
        public void DeferredsMayBeResolvedConcurrentlyAfterTheirPromisesArePassedToMerge_T7()
        {
            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
            var deferred2 = default(Promise<int>.Deferred);
            var deferred3 = default(Promise<int>.Deferred);
            var deferred4 = default(Promise<int>.Deferred);
            var deferred5 = default(Promise<int>.Deferred);
            var deferred6 = default(Promise<int>.Deferred);
            bool invoked = false;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    deferred0 = Promise.NewDeferred<int>();
                    deferred1 = Promise.NewDeferred<int>();
                    deferred2 = Promise.NewDeferred<int>();
                    deferred3 = Promise.NewDeferred<int>();
                    deferred4 = Promise.NewDeferred<int>();
                    deferred5 = Promise.NewDeferred<int>();
                    deferred6 = Promise.NewDeferred<int>();
                    invoked = false;
                    Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise, deferred4.Promise, deferred5.Promise, deferred6.Promise)
                        .Then(cv =>
                        {
                            Assert.AreEqual(1, cv.Item1);
                            Assert.AreEqual(2, cv.Item2);
                            Assert.AreEqual(3, cv.Item3);
                            Assert.AreEqual(4, cv.Item4);
                            Assert.AreEqual(5, cv.Item5);
                            Assert.AreEqual(6, cv.Item6);
                            Assert.AreEqual(7, cv.Item7);
                            invoked = true;
                        })
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
                () => deferred3.Resolve(4),
                () => deferred4.Resolve(5),
                () => deferred5.Resolve(6),
                () => deferred6.Resolve(7)
            );
        }

        [Test]
        public void DeferredsMayBeResolvedConcurrentlyAfterTheirPromisesArePassedToMerge_T7void()
        {
            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
            var deferred2 = default(Promise<int>.Deferred);
            var deferred3 = default(Promise<int>.Deferred);
            var deferred4 = default(Promise<int>.Deferred);
            var deferred5 = default(Promise<int>.Deferred);
            var deferred6 = default(Promise<int>.Deferred);
            var deferredVoid = default(Promise.Deferred);
            bool invoked = false;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    deferred0 = Promise.NewDeferred<int>();
                    deferred1 = Promise.NewDeferred<int>();
                    deferred2 = Promise.NewDeferred<int>();
                    deferred3 = Promise.NewDeferred<int>();
                    deferred4 = Promise.NewDeferred<int>();
                    deferred5 = Promise.NewDeferred<int>();
                    deferred6 = Promise.NewDeferred<int>();
                    deferredVoid = Promise.NewDeferred();
                    invoked = false;
                    Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise, deferred4.Promise, deferred5.Promise, deferred6.Promise, deferredVoid.Promise)
                        .Then(cv =>
                        {
                            Assert.AreEqual(1, cv.Item1);
                            Assert.AreEqual(2, cv.Item2);
                            Assert.AreEqual(3, cv.Item3);
                            Assert.AreEqual(4, cv.Item4);
                            Assert.AreEqual(5, cv.Item5);
                            Assert.AreEqual(6, cv.Item6);
                            Assert.AreEqual(7, cv.Item7);
                            invoked = true;
                        })
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
                () => deferred3.Resolve(4),
                () => deferred4.Resolve(5),
                () => deferred5.Resolve(6),
                () => deferred6.Resolve(7),
                () => deferredVoid.Resolve()
            );
        }

        [Test]
        public void ADeferredMayBeRejectedWhileItsPromiseIsPassedToMergeConcurrently_T1void()
        {
            var deferred0 = default(Promise<int>.Deferred);
            var deferredVoid = default(Promise.Deferred);
            bool invoked = false;
            int expected = 1;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    deferred0 = Promise.NewDeferred<int>();
                    deferredVoid = Promise.NewDeferred();
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
                () => deferredVoid.Reject(expected),
                () =>
                {
                    Promise.Merge(deferred0.Promise, deferredVoid.Promise)
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
        public void ADeferredMayBeRejectedWhileItsPromiseIsPassedToMergeConcurrently_T2()
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
                    deferred0 = Promise.NewDeferred<int>();
                    deferred1 = Promise.NewDeferred<int>();
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
                    Promise.Merge(deferred0.Promise, deferred1.Promise)
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
        public void ADeferredMayBeRejectedWhileItsPromiseIsPassedToMergeConcurrently_T2void()
        {
            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
            var deferredVoid = default(Promise.Deferred);
            bool invoked = false;
            int expected = 1;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    deferred0 = Promise.NewDeferred<int>();
                    deferred1 = Promise.NewDeferred<int>();
                    deferredVoid = Promise.NewDeferred();
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
                () => deferred1.Resolve(1),
                () => deferredVoid.Reject(expected),
                () =>
                {
                    Promise.Merge(deferred0.Promise, deferred1.Promise, deferredVoid.Promise)
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
        public void ADeferredMayBeRejectedWhileItsPromiseIsPassedToMergeConcurrently_T3()
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
                    deferred0 = Promise.NewDeferred<int>();
                    deferred1 = Promise.NewDeferred<int>();
                    deferred2 = Promise.NewDeferred<int>();
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
                    Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise)
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
        public void ADeferredMayBeRejectedWhileItsPromiseIsPassedToMergeConcurrently_T3void()
        {
            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
            var deferred2 = default(Promise<int>.Deferred);
            var deferredVoid = default(Promise.Deferred);
            bool invoked = false;
            int expected = 1;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    deferred0 = Promise.NewDeferred<int>();
                    deferred1 = Promise.NewDeferred<int>();
                    deferred2 = Promise.NewDeferred<int>();
                    deferredVoid = Promise.NewDeferred();
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
                () => deferredVoid.Reject(expected),
                () =>
                {
                    Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferredVoid.Promise)
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
        public void ADeferredMayBeRejectedWhileItsPromiseIsPassedToMergeConcurrently_T4()
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
                    deferred0 = Promise.NewDeferred<int>();
                    deferred1 = Promise.NewDeferred<int>();
                    deferred2 = Promise.NewDeferred<int>();
                    deferred3 = Promise.NewDeferred<int>();
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
                    Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise)
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
        public void ADeferredMayBeRejectedWhileItsPromiseIsPassedToMergeConcurrently_T4void()
        {
            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
            var deferred2 = default(Promise<int>.Deferred);
            var deferred3 = default(Promise<int>.Deferred);
            var deferredVoid = default(Promise.Deferred);
            bool invoked = false;
            int expected = 1;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    deferred0 = Promise.NewDeferred<int>();
                    deferred1 = Promise.NewDeferred<int>();
                    deferred2 = Promise.NewDeferred<int>();
                    deferred3 = Promise.NewDeferred<int>();
                    deferredVoid = Promise.NewDeferred();
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
                () => deferredVoid.Reject(expected),
                () =>
                {
                    Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise, deferredVoid.Promise)
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
        public void ADeferredMayBeRejectedWhileItsPromiseIsPassedToMergeConcurrently_T5()
        {
            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
            var deferred2 = default(Promise<int>.Deferred);
            var deferred3 = default(Promise<int>.Deferred);
            var deferred4 = default(Promise<int>.Deferred);
            bool invoked = false;
            int expected = 1;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    deferred0 = Promise.NewDeferred<int>();
                    deferred1 = Promise.NewDeferred<int>();
                    deferred2 = Promise.NewDeferred<int>();
                    deferred3 = Promise.NewDeferred<int>();
                    deferred4 = Promise.NewDeferred<int>();
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
                () => deferred4.Reject(expected),
                () =>
                {
                    Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise, deferred4.Promise)
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
        public void ADeferredMayBeRejectedWhileItsPromiseIsPassedToMergeConcurrently_T5void()
        {
            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
            var deferred2 = default(Promise<int>.Deferred);
            var deferred3 = default(Promise<int>.Deferred);
            var deferred4 = default(Promise<int>.Deferred);
            var deferredVoid = default(Promise.Deferred);
            bool invoked = false;
            int expected = 1;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    deferred0 = Promise.NewDeferred<int>();
                    deferred1 = Promise.NewDeferred<int>();
                    deferred2 = Promise.NewDeferred<int>();
                    deferred3 = Promise.NewDeferred<int>();
                    deferred4 = Promise.NewDeferred<int>();
                    deferredVoid = Promise.NewDeferred();
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
                () => deferred4.Resolve(5),
                () => deferredVoid.Reject(expected),
                () =>
                {
                    Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise, deferred4.Promise, deferredVoid.Promise)
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
        public void ADeferredMayBeRejectedWhileItsPromiseIsPassedToMergeConcurrently_T6()
        {
            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
            var deferred2 = default(Promise<int>.Deferred);
            var deferred3 = default(Promise<int>.Deferred);
            var deferred4 = default(Promise<int>.Deferred);
            var deferred5 = default(Promise<int>.Deferred);
            bool invoked = false;
            int expected = 1;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    deferred0 = Promise.NewDeferred<int>();
                    deferred1 = Promise.NewDeferred<int>();
                    deferred2 = Promise.NewDeferred<int>();
                    deferred3 = Promise.NewDeferred<int>();
                    deferred4 = Promise.NewDeferred<int>();
                    deferred5 = Promise.NewDeferred<int>();
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
                () => deferred4.Resolve(5),
                () => deferred5.Reject(expected),
                () =>
                {
                    Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise, deferred4.Promise, deferred5.Promise)
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
        public void ADeferredMayBeRejectedWhileItsPromiseIsPassedToMergeConcurrently_T6void()
        {
            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
            var deferred2 = default(Promise<int>.Deferred);
            var deferred3 = default(Promise<int>.Deferred);
            var deferred4 = default(Promise<int>.Deferred);
            var deferred5 = default(Promise<int>.Deferred);
            var deferredVoid = default(Promise.Deferred);
            bool invoked = false;
            int expected = 1;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    deferred0 = Promise.NewDeferred<int>();
                    deferred1 = Promise.NewDeferred<int>();
                    deferred2 = Promise.NewDeferred<int>();
                    deferred3 = Promise.NewDeferred<int>();
                    deferred4 = Promise.NewDeferred<int>();
                    deferred5 = Promise.NewDeferred<int>();
                    deferredVoid = Promise.NewDeferred();
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
                () => deferred4.Resolve(5),
                () => deferred5.Resolve(6),
                () => deferredVoid.Reject(expected),
                () =>
                {
                    Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise, deferred4.Promise, deferred5.Promise, deferredVoid.Promise)
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
        public void ADeferredMayBeRejectedWhileItsPromiseIsPassedToMergeConcurrently_T7()
        {
            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
            var deferred2 = default(Promise<int>.Deferred);
            var deferred3 = default(Promise<int>.Deferred);
            var deferred4 = default(Promise<int>.Deferred);
            var deferred5 = default(Promise<int>.Deferred);
            var deferred6 = default(Promise<int>.Deferred);
            bool invoked = false;
            int expected = 1;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    deferred0 = Promise.NewDeferred<int>();
                    deferred1 = Promise.NewDeferred<int>();
                    deferred2 = Promise.NewDeferred<int>();
                    deferred3 = Promise.NewDeferred<int>();
                    deferred4 = Promise.NewDeferred<int>();
                    deferred5 = Promise.NewDeferred<int>();
                    deferred6 = Promise.NewDeferred<int>();
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
                () => deferred4.Resolve(5),
                () => deferred5.Resolve(6),
                () => deferred6.Reject(expected),
                () =>
                {
                    Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise, deferred4.Promise, deferred5.Promise, deferred6.Promise)
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
        public void ADeferredMayBeRejectedWhileItsPromiseIsPassedToMergeConcurrently_T7void()
        {
            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
            var deferred2 = default(Promise<int>.Deferred);
            var deferred3 = default(Promise<int>.Deferred);
            var deferred4 = default(Promise<int>.Deferred);
            var deferred5 = default(Promise<int>.Deferred);
            var deferred6 = default(Promise<int>.Deferred);
            var deferredVoid = default(Promise.Deferred);
            bool invoked = false;
            int expected = 1;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    deferred0 = Promise.NewDeferred<int>();
                    deferred1 = Promise.NewDeferred<int>();
                    deferred2 = Promise.NewDeferred<int>();
                    deferred3 = Promise.NewDeferred<int>();
                    deferred4 = Promise.NewDeferred<int>();
                    deferred5 = Promise.NewDeferred<int>();
                    deferred6 = Promise.NewDeferred<int>();
                    deferredVoid = Promise.NewDeferred();
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
                () => deferred4.Resolve(5),
                () => deferred5.Resolve(6),
                () => deferred6.Resolve(7),
                () => deferredVoid.Reject(expected),
                () =>
                {
                    Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise, deferred4.Promise, deferred5.Promise, deferred6.Promise, deferredVoid.Promise)
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
        public void ADeferredMayBeRejectedConcurrentlyAfterItsPromiseIsPassedToMerge_T1void()
        {
            var deferred0 = default(Promise<int>.Deferred);
            var deferredVoid = default(Promise.Deferred);
            bool invoked = false;
            int expected = 1;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    deferred0 = Promise.NewDeferred<int>();
                    deferredVoid = Promise.NewDeferred();
                    invoked = false;
                    Promise.Merge(deferred0.Promise, deferredVoid.Promise)
                        .Catch((object s) =>
                        {
                            Assert.AreEqual(expected, s);
                            invoked = true;
                        })
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
                () => deferredVoid.Reject(expected)
            );
        }

        [Test]
        public void ADeferredMayBeRejectedConcurrentlyAfterItsPromiseIsPassedToMerge_T2()
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
                    deferred0 = Promise.NewDeferred<int>();
                    deferred1 = Promise.NewDeferred<int>();
                    invoked = false;
                    Promise.Merge(deferred0.Promise, deferred1.Promise)
                        .Catch((object s) =>
                        {
                            Assert.AreEqual(expected, s);
                            invoked = true;
                        })
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
        public void ADeferredMayBeRejectedConcurrentlyAfterItsPromiseIsPassedToMerge_T2void()
        {
            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
            var deferredVoid = default(Promise.Deferred);
            bool invoked = false;
            int expected = 1;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    deferred0 = Promise.NewDeferred<int>();
                    deferred1 = Promise.NewDeferred<int>();
                    deferredVoid = Promise.NewDeferred();
                    invoked = false;
                    Promise.Merge(deferred0.Promise, deferred1.Promise, deferredVoid.Promise)
                        .Catch((object s) =>
                        {
                            Assert.AreEqual(expected, s);
                            invoked = true;
                        })
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
                () => deferred1.Resolve(1),
                () => deferredVoid.Reject(expected)
            );
        }

        [Test]
        public void ADeferredMayBeRejectedConcurrentlyAfterItsPromiseIsPassedToMerge_T3()
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
                    deferred0 = Promise.NewDeferred<int>();
                    deferred1 = Promise.NewDeferred<int>();
                    deferred2 = Promise.NewDeferred<int>();
                    invoked = false;
                    Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise)
                        .Catch((object s) =>
                        {
                            Assert.AreEqual(expected, s);
                            invoked = true;
                        })
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
        public void ADeferredMayBeRejectedConcurrentlyAfterItsPromiseIsPassedToMerge_T3void()
        {
            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
            var deferred2 = default(Promise<int>.Deferred);
            var deferredVoid = default(Promise.Deferred);
            bool invoked = false;
            int expected = 1;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    deferred0 = Promise.NewDeferred<int>();
                    deferred1 = Promise.NewDeferred<int>();
                    deferred2 = Promise.NewDeferred<int>();
                    deferredVoid = Promise.NewDeferred();
                    invoked = false;
                    Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferredVoid.Promise)
                        .Catch((object s) =>
                        {
                            Assert.AreEqual(expected, s);
                            invoked = true;
                        })
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
                () => deferredVoid.Reject(expected)
            );
        }

        [Test]
        public void ADeferredMayBeRejectedConcurrentlyAfterItsPromiseIsPassedToMerge_T4()
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
                    deferred0 = Promise.NewDeferred<int>();
                    deferred1 = Promise.NewDeferred<int>();
                    deferred2 = Promise.NewDeferred<int>();
                    deferred3 = Promise.NewDeferred<int>();
                    invoked = false;
                    Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise)
                        .Catch((object s) =>
                        {
                            Assert.AreEqual(expected, s);
                            invoked = true;
                        })
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
        public void ADeferredMayBeRejectedConcurrentlyAfterItsPromiseIsPassedToMerge_T4void()
        {
            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
            var deferred2 = default(Promise<int>.Deferred);
            var deferred3 = default(Promise<int>.Deferred);
            var deferredVoid = default(Promise.Deferred);
            bool invoked = false;
            int expected = 1;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    deferred0 = Promise.NewDeferred<int>();
                    deferred1 = Promise.NewDeferred<int>();
                    deferred2 = Promise.NewDeferred<int>();
                    deferred3 = Promise.NewDeferred<int>();
                    deferredVoid = Promise.NewDeferred();
                    invoked = false;
                    Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise, deferredVoid.Promise)
                        .Catch((object s) =>
                        {
                            Assert.AreEqual(expected, s);
                            invoked = true;
                        })
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
                () => deferred3.Resolve(4),
                () => deferredVoid.Reject(expected)
            );
        }

        [Test]
        public void ADeferredMayBeRejectedConcurrentlyAfterItsPromiseIsPassedToMerge_T5()
        {
            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
            var deferred2 = default(Promise<int>.Deferred);
            var deferred3 = default(Promise<int>.Deferred);
            var deferred4 = default(Promise<int>.Deferred);
            bool invoked = false;
            int expected = 1;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    deferred0 = Promise.NewDeferred<int>();
                    deferred1 = Promise.NewDeferred<int>();
                    deferred2 = Promise.NewDeferred<int>();
                    deferred3 = Promise.NewDeferred<int>();
                    deferred4 = Promise.NewDeferred<int>();
                    invoked = false;
                    Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise, deferred4.Promise)
                        .Catch((object s) =>
                        {
                            Assert.AreEqual(expected, s);
                            invoked = true;
                        })
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
                () => deferred3.Resolve(4),
                () => deferred4.Reject(expected)
            );
        }

        [Test]
        public void ADeferredMayBeRejectedConcurrentlyAfterItsPromiseIsPassedToMerge_T5void()
        {
            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
            var deferred2 = default(Promise<int>.Deferred);
            var deferred3 = default(Promise<int>.Deferred);
            var deferred4 = default(Promise<int>.Deferred);
            var deferredVoid = default(Promise.Deferred);
            bool invoked = false;
            int expected = 1;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    deferred0 = Promise.NewDeferred<int>();
                    deferred1 = Promise.NewDeferred<int>();
                    deferred2 = Promise.NewDeferred<int>();
                    deferred3 = Promise.NewDeferred<int>();
                    deferred4 = Promise.NewDeferred<int>();
                    deferredVoid = Promise.NewDeferred();
                    invoked = false;
                    Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise, deferred4.Promise, deferredVoid.Promise)
                        .Catch((object s) =>
                        {
                            Assert.AreEqual(expected, s);
                            invoked = true;
                        })
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
                () => deferred3.Resolve(4),
                () => deferred4.Resolve(5),
                () => deferredVoid.Reject(expected)
            );
        }

        [Test]
        public void ADeferredMayBeRejectedConcurrentlyAfterItsPromiseIsPassedToMerge_T6()
        {
            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
            var deferred2 = default(Promise<int>.Deferred);
            var deferred3 = default(Promise<int>.Deferred);
            var deferred4 = default(Promise<int>.Deferred);
            var deferred5 = default(Promise<int>.Deferred);
            bool invoked = false;
            int expected = 1;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    deferred0 = Promise.NewDeferred<int>();
                    deferred1 = Promise.NewDeferred<int>();
                    deferred2 = Promise.NewDeferred<int>();
                    deferred3 = Promise.NewDeferred<int>();
                    deferred4 = Promise.NewDeferred<int>();
                    deferred5 = Promise.NewDeferred<int>();
                    invoked = false;
                    Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise, deferred4.Promise, deferred5.Promise)
                        .Catch((object s) =>
                        {
                            Assert.AreEqual(expected, s);
                            invoked = true;
                        })
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
                () => deferred3.Resolve(4),
                () => deferred4.Resolve(5),
                () => deferred5.Reject(expected)
            );
        }

        [Test]
        public void ADeferredMayBeRejectedConcurrentlyAfterItsPromiseIsPassedToMerge_T6void()
        {
            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
            var deferred2 = default(Promise<int>.Deferred);
            var deferred3 = default(Promise<int>.Deferred);
            var deferred4 = default(Promise<int>.Deferred);
            var deferred5 = default(Promise<int>.Deferred);
            var deferredVoid = default(Promise.Deferred);
            bool invoked = false;
            int expected = 1;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    deferred0 = Promise.NewDeferred<int>();
                    deferred1 = Promise.NewDeferred<int>();
                    deferred2 = Promise.NewDeferred<int>();
                    deferred3 = Promise.NewDeferred<int>();
                    deferred4 = Promise.NewDeferred<int>();
                    deferred5 = Promise.NewDeferred<int>();
                    deferredVoid = Promise.NewDeferred();
                    invoked = false;
                    Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise, deferred4.Promise, deferred5.Promise, deferredVoid.Promise)
                        .Catch((object s) =>
                        {
                            Assert.AreEqual(expected, s);
                            invoked = true;
                        })
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
                () => deferred3.Resolve(4),
                () => deferred4.Resolve(5),
                () => deferred5.Resolve(6),
                () => deferredVoid.Reject(expected)
            );
        }

        [Test]
        public void ADeferredMayBeRejectedConcurrentlyAfterItsPromiseIsPassedToMerge_T7()
        {
            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
            var deferred2 = default(Promise<int>.Deferred);
            var deferred3 = default(Promise<int>.Deferred);
            var deferred4 = default(Promise<int>.Deferred);
            var deferred5 = default(Promise<int>.Deferred);
            var deferred6 = default(Promise<int>.Deferred);
            bool invoked = false;
            int expected = 1;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    deferred0 = Promise.NewDeferred<int>();
                    deferred1 = Promise.NewDeferred<int>();
                    deferred2 = Promise.NewDeferred<int>();
                    deferred3 = Promise.NewDeferred<int>();
                    deferred4 = Promise.NewDeferred<int>();
                    deferred5 = Promise.NewDeferred<int>();
                    deferred6 = Promise.NewDeferred<int>();
                    invoked = false;
                    Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise, deferred4.Promise, deferred5.Promise, deferred6.Promise)
                        .Catch((object s) =>
                        {
                            Assert.AreEqual(expected, s);
                            invoked = true;
                        })
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
                () => deferred3.Resolve(4),
                () => deferred4.Resolve(5),
                () => deferred5.Resolve(6),
                () => deferred6.Reject(expected)
            );
        }

        [Test]
        public void ADeferredMayBeRejectedConcurrentlyAfterItsPromiseIsPassedToMerge_T7void()
        {
            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
            var deferred2 = default(Promise<int>.Deferred);
            var deferred3 = default(Promise<int>.Deferred);
            var deferred4 = default(Promise<int>.Deferred);
            var deferred5 = default(Promise<int>.Deferred);
            var deferred6 = default(Promise<int>.Deferred);
            var deferredVoid = default(Promise.Deferred);
            bool invoked = false;
            int expected = 1;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    deferred0 = Promise.NewDeferred<int>();
                    deferred1 = Promise.NewDeferred<int>();
                    deferred2 = Promise.NewDeferred<int>();
                    deferred3 = Promise.NewDeferred<int>();
                    deferred4 = Promise.NewDeferred<int>();
                    deferred5 = Promise.NewDeferred<int>();
                    deferred6 = Promise.NewDeferred<int>();
                    deferredVoid = Promise.NewDeferred();
                    invoked = false;
                    Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise, deferred4.Promise, deferred5.Promise, deferred6.Promise, deferredVoid.Promise)
                        .Catch((object s) =>
                        {
                            Assert.AreEqual(expected, s);
                            invoked = true;
                        })
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
                () => deferred3.Resolve(4),
                () => deferred4.Resolve(5),
                () => deferred5.Resolve(6),
                () => deferred6.Resolve(7),
                () => deferredVoid.Reject(expected)
            );
        }

        [Test]
        public void ADeferredMayBeCanceledWhileItsPromiseIsPassedToMergeConcurrently_T1void()
        {
            var cancelationSource = default(CancelationSource);
            var deferred0 = default(Promise<int>.Deferred);
            var deferredVoid = default(Promise.Deferred);
            bool invoked = false;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    cancelationSource = CancelationSource.New();
                    deferred0 = Promise.NewDeferred<int>();
                    deferredVoid = Promise.NewDeferred(cancelationSource.Token);
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
                    Promise.Merge(deferred0.Promise, deferredVoid.Promise)
                        .CatchCancelation(_ => invoked = true)
                        .Forget();
                }
            );
        }

        [Test]
        public void ADeferredMayBeCanceledWhileItsPromiseIsPassedToMergeConcurrently_T2()
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
                    deferred0 = Promise.NewDeferred<int>();
                    deferred1 = Promise.NewDeferred<int>(cancelationSource.Token);
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
                    Promise.Merge(deferred0.Promise, deferred1.Promise)
                        .CatchCancelation(_ => invoked = true)
                        .Forget();
                }
            );
        }

        [Test]
        public void ADeferredMayBeCanceledWhileItsPromiseIsPassedToMergeConcurrently_T2void()
        {
            var cancelationSource = default(CancelationSource);
            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
            var deferredVoid = default(Promise.Deferred);
            bool invoked = false;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    cancelationSource = CancelationSource.New();
                    deferred0 = Promise.NewDeferred<int>();
                    deferred1 = Promise.NewDeferred<int>();
                    deferredVoid = Promise.NewDeferred(cancelationSource.Token);
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
                () => deferred1.Resolve(1),
                () => cancelationSource.Cancel(),
                () =>
                {
                    Promise.Merge(deferred0.Promise, deferred1.Promise, deferredVoid.Promise)
                        .CatchCancelation(_ => invoked = true)
                        .Forget();
                }
            );
        }

        [Test]
        public void ADeferredMayBeCanceledWhileItsPromiseIsPassedToMergeConcurrently_T3()
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
                    deferred0 = Promise.NewDeferred<int>();
                    deferred1 = Promise.NewDeferred<int>();
                    deferred2 = Promise.NewDeferred<int>(cancelationSource.Token);
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
                    Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise)
                        .CatchCancelation(_ => invoked = true)
                        .Forget();
                }
            );
        }

        [Test]
        public void ADeferredMayBeCanceledWhileItsPromiseIsPassedToMergeConcurrently_T3void()
        {
            var cancelationSource = default(CancelationSource);
            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
            var deferred2 = default(Promise<int>.Deferred);
            var deferredVoid = default(Promise.Deferred);
            bool invoked = false;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    cancelationSource = CancelationSource.New();
                    deferred0 = Promise.NewDeferred<int>();
                    deferred1 = Promise.NewDeferred<int>();
                    deferred2 = Promise.NewDeferred<int>();
                    deferredVoid = Promise.NewDeferred(cancelationSource.Token);
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
                    Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferredVoid.Promise)
                        .CatchCancelation(_ => invoked = true)
                        .Forget();
                }
            );
        }

        [Test]
        public void ADeferredMayBeCanceledWhileItsPromiseIsPassedToMergeConcurrently_T4()
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
                    deferred0 = Promise.NewDeferred<int>();
                    deferred1 = Promise.NewDeferred<int>();
                    deferred2 = Promise.NewDeferred<int>();
                    deferred3 = Promise.NewDeferred<int>(cancelationSource.Token);
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
                    Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise)
                        .CatchCancelation(_ => invoked = true)
                        .Forget();
                }
            );
        }

        [Test]
        public void ADeferredMayBeCanceledWhileItsPromiseIsPassedToMergeConcurrently_T4void()
        {
            var cancelationSource = default(CancelationSource);
            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
            var deferred2 = default(Promise<int>.Deferred);
            var deferred3 = default(Promise<int>.Deferred);
            var deferredVoid = default(Promise.Deferred);
            bool invoked = false;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    cancelationSource = CancelationSource.New();
                    deferred0 = Promise.NewDeferred<int>();
                    deferred1 = Promise.NewDeferred<int>();
                    deferred2 = Promise.NewDeferred<int>();
                    deferred3 = Promise.NewDeferred<int>();
                    deferredVoid = Promise.NewDeferred(cancelationSource.Token);
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
                () => deferred3.Resolve(4),
                () => cancelationSource.Cancel(),
                () =>
                {
                    Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise, deferredVoid.Promise)
                        .CatchCancelation(_ => invoked = true)
                        .Forget();
                }
            );
        }

        [Test]
        public void ADeferredMayBeCanceledWhileItsPromiseIsPassedToMergeConcurrently_T5()
        {
            var cancelationSource = default(CancelationSource);
            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
            var deferred2 = default(Promise<int>.Deferred);
            var deferred3 = default(Promise<int>.Deferred);
            var deferred4 = default(Promise<int>.Deferred);
            bool invoked = false;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    cancelationSource = CancelationSource.New();
                    deferred0 = Promise.NewDeferred<int>();
                    deferred1 = Promise.NewDeferred<int>();
                    deferred2 = Promise.NewDeferred<int>();
                    deferred3 = Promise.NewDeferred<int>();
                    deferred4 = Promise.NewDeferred<int>(cancelationSource.Token);
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
                () => deferred3.Resolve(4),
                () => cancelationSource.Cancel(),
                () =>
                {
                    Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise, deferred4.Promise)
                        .CatchCancelation(_ => invoked = true)
                        .Forget();
                }
            );
        }

        [Test]
        public void ADeferredMayBeCanceledWhileItsPromiseIsPassedToMergeConcurrently_T5void()
        {
            var cancelationSource = default(CancelationSource);
            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
            var deferred2 = default(Promise<int>.Deferred);
            var deferred3 = default(Promise<int>.Deferred);
            var deferred4 = default(Promise<int>.Deferred);
            var deferredVoid = default(Promise.Deferred);
            bool invoked = false;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    cancelationSource = CancelationSource.New();
                    deferred0 = Promise.NewDeferred<int>();
                    deferred1 = Promise.NewDeferred<int>();
                    deferred2 = Promise.NewDeferred<int>();
                    deferred3 = Promise.NewDeferred<int>();
                    deferred4 = Promise.NewDeferred<int>();
                    deferredVoid = Promise.NewDeferred(cancelationSource.Token);
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
                () => deferred3.Resolve(4),
                () => deferred4.Resolve(5),
                () => cancelationSource.Cancel(),
                () =>
                {
                    Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise, deferred4.Promise, deferredVoid.Promise)
                        .CatchCancelation(_ => invoked = true)
                        .Forget();
                }
            );
        }

        [Test]
        public void ADeferredMayBeCanceledWhileItsPromiseIsPassedToMergeConcurrently_T6()
        {
            var cancelationSource = default(CancelationSource);
            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
            var deferred2 = default(Promise<int>.Deferred);
            var deferred3 = default(Promise<int>.Deferred);
            var deferred4 = default(Promise<int>.Deferred);
            var deferred5 = default(Promise<int>.Deferred);
            bool invoked = false;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    cancelationSource = CancelationSource.New();
                    deferred0 = Promise.NewDeferred<int>();
                    deferred1 = Promise.NewDeferred<int>();
                    deferred2 = Promise.NewDeferred<int>();
                    deferred3 = Promise.NewDeferred<int>();
                    deferred4 = Promise.NewDeferred<int>();
                    deferred5 = Promise.NewDeferred<int>(cancelationSource.Token);
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
                () => deferred3.Resolve(4),
                () => deferred4.Resolve(5),
                () => cancelationSource.Cancel(),
                () =>
                {
                    Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise, deferred4.Promise, deferred5.Promise)
                        .CatchCancelation(_ => invoked = true)
                        .Forget();
                }
            );
        }

        [Test]
        public void ADeferredMayBeCanceledWhileItsPromiseIsPassedToMergeConcurrently_T6void()
        {
            var cancelationSource = default(CancelationSource);
            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
            var deferred2 = default(Promise<int>.Deferred);
            var deferred3 = default(Promise<int>.Deferred);
            var deferred4 = default(Promise<int>.Deferred);
            var deferred5 = default(Promise<int>.Deferred);
            var deferredVoid = default(Promise.Deferred);
            bool invoked = false;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    cancelationSource = CancelationSource.New();
                    deferred0 = Promise.NewDeferred<int>();
                    deferred1 = Promise.NewDeferred<int>();
                    deferred2 = Promise.NewDeferred<int>();
                    deferred3 = Promise.NewDeferred<int>();
                    deferred4 = Promise.NewDeferred<int>();
                    deferred5 = Promise.NewDeferred<int>();
                    deferredVoid = Promise.NewDeferred(cancelationSource.Token);
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
                () => deferred3.Resolve(4),
                () => deferred4.Resolve(5),
                () => deferred5.Resolve(6),
                () => cancelationSource.Cancel(),
                () =>
                {
                    Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise, deferred4.Promise, deferred5.Promise, deferredVoid.Promise)
                        .CatchCancelation(_ => invoked = true)
                        .Forget();
                }
            );
        }

        [Test]
        public void ADeferredMayBeCanceledWhileItsPromiseIsPassedToMergeConcurrently_T7()
        {
            var cancelationSource = default(CancelationSource);
            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
            var deferred2 = default(Promise<int>.Deferred);
            var deferred3 = default(Promise<int>.Deferred);
            var deferred4 = default(Promise<int>.Deferred);
            var deferred5 = default(Promise<int>.Deferred);
            var deferred6 = default(Promise<int>.Deferred);
            bool invoked = false;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    cancelationSource = CancelationSource.New();
                    deferred0 = Promise.NewDeferred<int>();
                    deferred1 = Promise.NewDeferred<int>();
                    deferred2 = Promise.NewDeferred<int>();
                    deferred3 = Promise.NewDeferred<int>();
                    deferred4 = Promise.NewDeferred<int>();
                    deferred5 = Promise.NewDeferred<int>();
                    deferred6 = Promise.NewDeferred<int>(cancelationSource.Token);
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
                () => deferred3.Resolve(4),
                () => deferred4.Resolve(5),
                () => deferred5.Resolve(6),
                () => cancelationSource.Cancel(),
                () =>
                {
                    Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise, deferred4.Promise, deferred5.Promise, deferred6.Promise)
                        .CatchCancelation(_ => invoked = true)
                        .Forget();
                }
            );
        }

        [Test]
        public void ADeferredMayBeCanceledWhileItsPromiseIsPassedToMergeConcurrently_T7void()
        {
            var cancelationSource = default(CancelationSource);
            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
            var deferred2 = default(Promise<int>.Deferred);
            var deferred3 = default(Promise<int>.Deferred);
            var deferred4 = default(Promise<int>.Deferred);
            var deferred5 = default(Promise<int>.Deferred);
            var deferred6 = default(Promise<int>.Deferred);
            var deferredVoid = default(Promise.Deferred);
            bool invoked = false;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    cancelationSource = CancelationSource.New();
                    deferred0 = Promise.NewDeferred<int>();
                    deferred1 = Promise.NewDeferred<int>();
                    deferred2 = Promise.NewDeferred<int>();
                    deferred3 = Promise.NewDeferred<int>();
                    deferred4 = Promise.NewDeferred<int>();
                    deferred5 = Promise.NewDeferred<int>();
                    deferred6 = Promise.NewDeferred<int>();
                    deferredVoid = Promise.NewDeferred(cancelationSource.Token);
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
                () => deferred3.Resolve(4),
                () => deferred4.Resolve(5),
                () => deferred5.Resolve(6),
                () => deferred6.Resolve(7),
                () => cancelationSource.Cancel(),
                () =>
                {
                    Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise, deferred4.Promise, deferred5.Promise, deferred6.Promise, deferredVoid.Promise)
                        .CatchCancelation(_ => invoked = true)
                        .Forget();
                }
            );
        }

        [Test]
        public void ADeferredMayBeCanceledConcurrentlyAfterItsPromiseIsPassedToMerge_T1void()
        {
            var cancelationSource = default(CancelationSource);
            var deferred0 = default(Promise<int>.Deferred);
            var deferredVoid = default(Promise.Deferred);
            bool invoked = false;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    cancelationSource = CancelationSource.New();
                    deferred0 = Promise.NewDeferred<int>();
                    deferredVoid = Promise.NewDeferred(cancelationSource.Token);
                    invoked = false;
                    Promise.Merge(deferred0.Promise, deferredVoid.Promise)
                        .CatchCancelation(_ => invoked = true)
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
        public void ADeferredMayBeCanceledConcurrentlyAfterItsPromiseIsPassedToMerge_T2()
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
                    deferred0 = Promise.NewDeferred<int>();
                    deferred1 = Promise.NewDeferred<int>(cancelationSource.Token);
                    invoked = false;
                    Promise.Merge(deferred0.Promise, deferred1.Promise)
                        .CatchCancelation(_ => invoked = true)
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
        public void ADeferredMayBeCanceledConcurrentlyAfterItsPromiseIsPassedToMerge_T2void()
        {
            var cancelationSource = default(CancelationSource);
            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
            var deferredVoid = default(Promise.Deferred);
            bool invoked = false;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    cancelationSource = CancelationSource.New();
                    deferred0 = Promise.NewDeferred<int>();
                    deferred1 = Promise.NewDeferred<int>();
                    deferredVoid = Promise.NewDeferred(cancelationSource.Token);
                    invoked = false;
                    Promise.Merge(deferred0.Promise, deferred1.Promise, deferredVoid.Promise)
                        .CatchCancelation(_ => invoked = true)
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
                () => deferred1.Resolve(1),
                () => cancelationSource.Cancel()
            );
        }

        [Test]
        public void ADeferredMayBeCanceledConcurrentlyAfterItsPromiseIsPassedToMerge_T3()
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
                    deferred0 = Promise.NewDeferred<int>();
                    deferred1 = Promise.NewDeferred<int>();
                    deferred2 = Promise.NewDeferred<int>(cancelationSource.Token);
                    invoked = false;
                    Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise)
                        .CatchCancelation(_ => invoked = true)
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
        public void ADeferredMayBeCanceledConcurrentlyAfterItsPromiseIsPassedToMerge_T3void()
        {
            var cancelationSource = default(CancelationSource);
            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
            var deferred2 = default(Promise<int>.Deferred);
            var deferredVoid = default(Promise.Deferred);
            bool invoked = false;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    cancelationSource = CancelationSource.New();
                    deferred0 = Promise.NewDeferred<int>();
                    deferred1 = Promise.NewDeferred<int>();
                    deferred2 = Promise.NewDeferred<int>();
                    deferredVoid = Promise.NewDeferred(cancelationSource.Token);
                    invoked = false;
                    Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferredVoid.Promise)
                        .CatchCancelation(_ => invoked = true)
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
        public void ADeferredMayBeCanceledConcurrentlyAfterItsPromiseIsPassedToMerge_T4()
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
                    deferred0 = Promise.NewDeferred<int>();
                    deferred1 = Promise.NewDeferred<int>();
                    deferred2 = Promise.NewDeferred<int>();
                    deferred3 = Promise.NewDeferred<int>(cancelationSource.Token);
                    invoked = false;
                    Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise)
                        .CatchCancelation(_ => invoked = true)
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
        public void ADeferredMayBeCanceledConcurrentlyAfterItsPromiseIsPassedToMerge_T4void()
        {
            var cancelationSource = default(CancelationSource);
            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
            var deferred2 = default(Promise<int>.Deferred);
            var deferred3 = default(Promise<int>.Deferred);
            var deferredVoid = default(Promise.Deferred);
            bool invoked = false;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    cancelationSource = CancelationSource.New();
                    deferred0 = Promise.NewDeferred<int>();
                    deferred1 = Promise.NewDeferred<int>();
                    deferred2 = Promise.NewDeferred<int>();
                    deferred3 = Promise.NewDeferred<int>();
                    deferredVoid = Promise.NewDeferred(cancelationSource.Token);
                    invoked = false;
                    Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise, deferredVoid.Promise)
                        .CatchCancelation(_ => invoked = true)
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
                () => deferred3.Resolve(4),
                () => cancelationSource.Cancel()
            );
        }

        [Test]
        public void ADeferredMayBeCanceledConcurrentlyAfterItsPromiseIsPassedToMerge_T5()
        {
            var cancelationSource = default(CancelationSource);
            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
            var deferred2 = default(Promise<int>.Deferred);
            var deferred3 = default(Promise<int>.Deferred);
            var deferred4 = default(Promise<int>.Deferred);
            bool invoked = false;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    cancelationSource = CancelationSource.New();
                    deferred0 = Promise.NewDeferred<int>();
                    deferred1 = Promise.NewDeferred<int>();
                    deferred2 = Promise.NewDeferred<int>();
                    deferred3 = Promise.NewDeferred<int>();
                    deferred4 = Promise.NewDeferred<int>(cancelationSource.Token);
                    invoked = false;
                    Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise, deferred4.Promise)
                        .CatchCancelation(_ => invoked = true)
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
                () => deferred3.Resolve(4),
                () => cancelationSource.Cancel()
            );
        }

        [Test]
        public void ADeferredMayBeCanceledConcurrentlyAfterItsPromiseIsPassedToMerge_T5void()
        {
            var cancelationSource = default(CancelationSource);
            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
            var deferred2 = default(Promise<int>.Deferred);
            var deferred3 = default(Promise<int>.Deferred);
            var deferred4 = default(Promise<int>.Deferred);
            var deferredVoid = default(Promise.Deferred);
            bool invoked = false;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    cancelationSource = CancelationSource.New();
                    deferred0 = Promise.NewDeferred<int>();
                    deferred1 = Promise.NewDeferred<int>();
                    deferred2 = Promise.NewDeferred<int>();
                    deferred3 = Promise.NewDeferred<int>();
                    deferred4 = Promise.NewDeferred<int>();
                    deferredVoid = Promise.NewDeferred(cancelationSource.Token);
                    invoked = false;
                    Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise, deferred4.Promise, deferredVoid.Promise)
                        .CatchCancelation(_ => invoked = true)
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
                () => deferred3.Resolve(4),
                () => deferred4.Resolve(5),
                () => cancelationSource.Cancel()
            );
        }

        [Test]
        public void ADeferredMayBeCanceledConcurrentlyAfterItsPromiseIsPassedToMerge_T6()
        {
            var cancelationSource = default(CancelationSource);
            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
            var deferred2 = default(Promise<int>.Deferred);
            var deferred3 = default(Promise<int>.Deferred);
            var deferred4 = default(Promise<int>.Deferred);
            var deferred5 = default(Promise<int>.Deferred);
            bool invoked = false;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    cancelationSource = CancelationSource.New();
                    deferred0 = Promise.NewDeferred<int>();
                    deferred1 = Promise.NewDeferred<int>();
                    deferred2 = Promise.NewDeferred<int>();
                    deferred3 = Promise.NewDeferred<int>();
                    deferred4 = Promise.NewDeferred<int>();
                    deferred5 = Promise.NewDeferred<int>(cancelationSource.Token);
                    invoked = false;
                    Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise, deferred4.Promise, deferred5.Promise)
                        .CatchCancelation(_ => invoked = true)
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
                () => deferred3.Resolve(4),
                () => deferred4.Resolve(5),
                () => cancelationSource.Cancel()
            );
        }

        [Test]
        public void ADeferredMayBeCanceledConcurrentlyAfterItsPromiseIsPassedToMerge_T6void()
        {
            var cancelationSource = default(CancelationSource);
            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
            var deferred2 = default(Promise<int>.Deferred);
            var deferred3 = default(Promise<int>.Deferred);
            var deferred4 = default(Promise<int>.Deferred);
            var deferred5 = default(Promise<int>.Deferred);
            var deferredVoid = default(Promise.Deferred);
            bool invoked = false;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    cancelationSource = CancelationSource.New();
                    deferred0 = Promise.NewDeferred<int>();
                    deferred1 = Promise.NewDeferred<int>();
                    deferred2 = Promise.NewDeferred<int>();
                    deferred3 = Promise.NewDeferred<int>();
                    deferred4 = Promise.NewDeferred<int>();
                    deferred5 = Promise.NewDeferred<int>();
                    deferredVoid = Promise.NewDeferred(cancelationSource.Token);
                    invoked = false;
                    Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise, deferred4.Promise, deferred5.Promise, deferredVoid.Promise)
                        .CatchCancelation(_ => invoked = true)
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
                () => deferred3.Resolve(4),
                () => deferred4.Resolve(5),
                () => deferred5.Resolve(6),
                () => cancelationSource.Cancel()
            );
        }

        [Test]
        public void ADeferredMayBeCanceledConcurrentlyAfterItsPromiseIsPassedToMerge_T7()
        {
            var cancelationSource = default(CancelationSource);
            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
            var deferred2 = default(Promise<int>.Deferred);
            var deferred3 = default(Promise<int>.Deferred);
            var deferred4 = default(Promise<int>.Deferred);
            var deferred5 = default(Promise<int>.Deferred);
            var deferred6 = default(Promise<int>.Deferred);
            bool invoked = false;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    cancelationSource = CancelationSource.New();
                    deferred0 = Promise.NewDeferred<int>();
                    deferred1 = Promise.NewDeferred<int>();
                    deferred2 = Promise.NewDeferred<int>();
                    deferred3 = Promise.NewDeferred<int>();
                    deferred4 = Promise.NewDeferred<int>();
                    deferred5 = Promise.NewDeferred<int>();
                    deferred6 = Promise.NewDeferred<int>(cancelationSource.Token);
                    invoked = false;
                    Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise, deferred4.Promise, deferred5.Promise, deferred6.Promise)
                        .CatchCancelation(_ => invoked = true)
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
                () => deferred3.Resolve(4),
                () => deferred4.Resolve(5),
                () => deferred5.Resolve(6),
                () => cancelationSource.Cancel()
            );
        }

        [Test]
        public void ADeferredMayBeCanceledConcurrentlyAfterItsPromiseIsPassedToMerge_T7void()
        {
            var cancelationSource = default(CancelationSource);
            var deferred0 = default(Promise<int>.Deferred);
            var deferred1 = default(Promise<int>.Deferred);
            var deferred2 = default(Promise<int>.Deferred);
            var deferred3 = default(Promise<int>.Deferred);
            var deferred4 = default(Promise<int>.Deferred);
            var deferred5 = default(Promise<int>.Deferred);
            var deferred6 = default(Promise<int>.Deferred);
            var deferredVoid = default(Promise.Deferred);
            bool invoked = false;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    cancelationSource = CancelationSource.New();
                    deferred0 = Promise.NewDeferred<int>();
                    deferred1 = Promise.NewDeferred<int>();
                    deferred2 = Promise.NewDeferred<int>();
                    deferred3 = Promise.NewDeferred<int>();
                    deferred4 = Promise.NewDeferred<int>();
                    deferred5 = Promise.NewDeferred<int>();
                    deferred6 = Promise.NewDeferred<int>();
                    deferredVoid = Promise.NewDeferred(cancelationSource.Token);
                    invoked = false;
                    Promise.Merge(deferred0.Promise, deferred1.Promise, deferred2.Promise, deferred3.Promise, deferred4.Promise, deferred5.Promise, deferred6.Promise, deferredVoid.Promise)
                        .CatchCancelation(_ => invoked = true)
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
                () => deferred3.Resolve(4),
                () => deferred4.Resolve(5),
                () => deferred5.Resolve(6),
                () => deferred6.Resolve(7),
                () => cancelationSource.Cancel()
            );
        }
    }
}

#endif