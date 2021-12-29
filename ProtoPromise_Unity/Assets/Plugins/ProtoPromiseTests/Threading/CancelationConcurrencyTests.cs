#if !UNITY_WEBGL

#pragma warning disable IDE0018 // Inline variable declaration

using NUnit.Framework;
using Proto.Promises;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ProtoPromiseTests.Threading
{
    public class CancelationConcurrencyTests
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
        public void CancelationSourceMayBeCanceledOnlyOnce0()
        {
            var cancelationSource = CancelationSource.New();
            
            int successCount = 0, invalidCount = 0;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(
                () =>
                {
                    try
                    {
                        cancelationSource.Cancel();
                        Interlocked.Increment(ref successCount);
                    }
                    catch (Proto.Promises.InvalidOperationException)
                    {
                        Interlocked.Increment(ref invalidCount);
                    }
                }
            );
            Assert.AreEqual(1, successCount);
            Assert.AreEqual(ThreadHelper.multiExecutionCount - 1, invalidCount);
            cancelationSource.Dispose();
        }

        [Test]
        public void CancelationSourceMayBeCanceledOnlyOnce1()
        {
            var cancelationSource = CancelationSource.New();

            int successCount = 0, invalidCount = 0;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(
                () =>
                {
                    try
                    {
                        cancelationSource.Cancel("Cancel");
                        Interlocked.Increment(ref successCount);
                    }
                    catch (Proto.Promises.InvalidOperationException)
                    {
                        Interlocked.Increment(ref invalidCount);
                    }
                }
            );
            Assert.AreEqual(1, successCount);
            Assert.AreEqual(ThreadHelper.multiExecutionCount - 1, invalidCount);
            cancelationSource.Dispose();
        }

        [Test]
        public void CancelationSourceMayBeCanceledOnlyOnce2()
        {
            var cancelationSource = CancelationSource.New();

            int successCount = 0, invalidCount = 0;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(
                () =>
                {
                    if (cancelationSource.TryCancel())
                    {
                        Interlocked.Increment(ref successCount);
                    }
                    else
                    {
                        Interlocked.Increment(ref invalidCount);
                    }
                }
            );
            Assert.AreEqual(1, successCount);
            Assert.AreEqual(ThreadHelper.multiExecutionCount - 1, invalidCount);
            cancelationSource.Dispose();
        }

        [Test]
        public void CancelationSourceMayBeCanceledOnlyOnce3()
        {
            var cancelationSource = CancelationSource.New();

            int successCount = 0, invalidCount = 0;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(
                () =>
                {
                    if (cancelationSource.TryCancel())
                    {
                        Interlocked.Increment(ref successCount);
                    }
                    else
                    {
                        Interlocked.Increment(ref invalidCount);
                    }
                }
            );
            Assert.AreEqual(1, successCount);
            Assert.AreEqual(ThreadHelper.multiExecutionCount - 1, invalidCount);
            cancelationSource.Dispose();
        }

        [Test]
        public void CancelationSourceMayBeDisposedOnlyOnce0()
        {
            var cancelationSource = CancelationSource.New();

            int successCount = 0, invalidCount = 0;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(
                () =>
                {
                    try
                    {
                        cancelationSource.Dispose();
                        Interlocked.Increment(ref successCount);
                    }
                    catch (Proto.Promises.InvalidOperationException)
                    {
                        Interlocked.Increment(ref invalidCount);
                    }
                }
            );
            Assert.AreEqual(1, successCount);
            Assert.AreEqual(ThreadHelper.multiExecutionCount - 1, invalidCount);
        }

        [Test]
        public void CancelationSourceMayBeDisposedOnlyOnce1()
        {
            var cancelationSource = CancelationSource.New();
            cancelationSource.Cancel();

            int successCount = 0, invalidCount = 0;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(
                () =>
                {
                    try
                    {
                        cancelationSource.Dispose();
                        Interlocked.Increment(ref successCount);
                    }
                    catch (Proto.Promises.InvalidOperationException)
                    {
                        Interlocked.Increment(ref invalidCount);
                    }
                }
            );
            Assert.AreEqual(1, successCount);
            Assert.AreEqual(ThreadHelper.multiExecutionCount - 1, invalidCount);
        }

        [Test]
        public void CancelationSourceMayBeDisposedOnlyOnce2()
        {
            var cancelationSource = CancelationSource.New();
            cancelationSource.Cancel("Cancel");

            int successCount = 0, invalidCount = 0;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(
                () =>
                {
                    try
                    {
                        cancelationSource.Dispose();
                        Interlocked.Increment(ref successCount);
                    }
                    catch (Proto.Promises.InvalidOperationException)
                    {
                        Interlocked.Increment(ref invalidCount);
                    }
                }
            );
            Assert.AreEqual(1, successCount);
            Assert.AreEqual(ThreadHelper.multiExecutionCount - 1, invalidCount);
        }

        [Test]
        public void CancelationSourceMayBeDisposedOnlyOnce3()
        {
            var cancelationSource = CancelationSource.New();

            int successCount = 0, invalidCount = 0;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(
                () =>
                {
                    if (cancelationSource.TryDispose())
                    {
                        Interlocked.Increment(ref successCount);
                    }
                    else
                    {
                        Interlocked.Increment(ref invalidCount);
                    }
                }
            );
            Assert.AreEqual(1, successCount);
            Assert.AreEqual(ThreadHelper.multiExecutionCount - 1, invalidCount);
        }

        [Test]
        public void CancelationSourceMayBeDisposedOnlyOnce4()
        {
            var cancelationSource = CancelationSource.New();
            cancelationSource.Cancel();

            int successCount = 0, invalidCount = 0;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(
                () =>
                {
                    if (cancelationSource.TryDispose())
                    {
                        Interlocked.Increment(ref successCount);
                    }
                    else
                    {
                        Interlocked.Increment(ref invalidCount);
                    }
                }
            );
            Assert.AreEqual(1, successCount);
            Assert.AreEqual(ThreadHelper.multiExecutionCount - 1, invalidCount);
        }

        [Test]
        public void CancelationSourceMayBeDisposedOnlyOnce5()
        {
            var cancelationSource = CancelationSource.New();
            cancelationSource.Cancel("Cancel");

            int successCount = 0, invalidCount = 0;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(
                () =>
                {
                    if (cancelationSource.TryDispose())
                    {
                        Interlocked.Increment(ref successCount);
                    }
                    else
                    {
                        Interlocked.Increment(ref invalidCount);
                    }
                }
            );
            Assert.AreEqual(1, successCount);
            Assert.AreEqual(ThreadHelper.multiExecutionCount - 1, invalidCount);
        }

        [Test]
        public void CancelationSourceMayBeCanceledAndDisposedConcurrently0()
        {
            var cancelationSource = default(CancelationSource);

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(true,
                // Setup
                () => cancelationSource = CancelationSource.New(),
                // Teardown
                () => { },
                // Parallel actions
                () => cancelationSource.TryCancel(),
                () => cancelationSource.TryDispose()
            );
        }

        [Test]
        public void CancelationSourceMayBeCanceledAndDisposedConcurrently1()
        {
            var cancelationSource = default(CancelationSource);

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(true,
                // Setup
                () => cancelationSource = CancelationSource.New(),
                // Teardown
                () => { },
                // Parallel actions
                () => cancelationSource.TryCancel("Cancel"),
                () => cancelationSource.TryDispose()
            );
        }

        [Test]
        public void CancelationSourceMayBeCanceledAndDisposedConcurrently2()
        {
            var cancelationSource = default(CancelationSource);

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(true,
                // Setup
                () =>
                {
                    cancelationSource = CancelationSource.New();
                    cancelationSource.Token.Register(_ => { });
                },
                // Teardown
                () => { },
                // Parallel actions
                () => cancelationSource.TryCancel(),
                () => cancelationSource.TryDispose()
            );
        }

        [Test]
        public void CancelationSourceMayBeCanceledAndDisposedConcurrently3()
        {
            var cancelationSource = default(CancelationSource);

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(true,
                // Setup
                () =>
                {
                    cancelationSource = CancelationSource.New();
                    cancelationSource.Token.Register(_ => { });
                },
                // Teardown
                () => { },
                // Parallel actions
                () => cancelationSource.TryCancel("Cancel"),
                () => cancelationSource.TryDispose()
            );
        }

        [Test]
        public void CancelationTokenMayBeRegisteredToConcurrently0()
        {
            var cancelationSource = CancelationSource.New();
            var cancelationToken = cancelationSource.Token;

            int invokedCount = 0;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(
                () => cancelationToken.Register(_ => Interlocked.Increment(ref invokedCount))
            );
            cancelationSource.Cancel();
            Assert.AreEqual(ThreadHelper.multiExecutionCount, invokedCount);
            cancelationSource.Dispose();
        }

        [Test]
        public void CancelationTokenMayBeRegisteredToConcurrently1()
        {
            var cancelationSource = CancelationSource.New();
            var cancelationToken = cancelationSource.Token;

            int invokedCount = 0;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(
                () => cancelationToken.Register(1, (cv, _) => Interlocked.Increment(ref invokedCount))
            );
            cancelationSource.Cancel();
            Assert.AreEqual(ThreadHelper.multiExecutionCount, invokedCount);
            cancelationSource.Dispose();
        }

        [Test]
        public void CancelationTokenRegisterAlwaysReturnsUnique0()
        {
            var cancelationSource = CancelationSource.New();
            var cancelationToken = cancelationSource.Token;

            var registrations = new ConcurrentBag<CancelationRegistration>();

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(
                () => registrations.Add(cancelationToken.Register(_ => { }))
            );

            var diffChecker = new HashSet<CancelationRegistration>();
            if (!registrations.All(diffChecker.Add))
            {
                Assert.Fail("cancelationToken.Register returned at least one of the same CancelationRegistration instance.");
            }
            cancelationSource.Dispose();
        }

        [Test]
        public void CancelationTokenRegisterAlwaysReturnsUnique1()
        {
            var cancelationSource = CancelationSource.New();
            var cancelationToken = cancelationSource.Token;

            var registrations = new ConcurrentBag<CancelationRegistration>();

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(
                () => registrations.Add(cancelationToken.Register(1, (cv, _) => { }))
            );

            var diffChecker = new HashSet<CancelationRegistration>();
            if (!registrations.All(diffChecker.Add))
            {
                Assert.Fail("cancelationToken.Register returned at least one of the same CancelationRegistration instance.");
            }
            cancelationSource.Dispose();
        }

        [Test]
        public void CancelationTokenMayBeRetainedConcurrently()
        {
            var cancelationSource = CancelationSource.New();
            var cancelationToken = cancelationSource.Token;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(
                () => cancelationToken.Retain()
            );
            cancelationSource.Dispose();
            Assert.IsTrue(cancelationToken.CanBeCanceled);
            for (int i = 0; i < ThreadHelper.multiExecutionCount; ++i)
            {
                cancelationToken.Release();
            }
            Assert.IsFalse(cancelationToken.CanBeCanceled);
        }

        [Test]
        public void CancelationTokenMayBeReleasedConcurrently()
        {
            var cancelationSource = CancelationSource.New();
            var cancelationToken = cancelationSource.Token;

            for (int i = 0; i < ThreadHelper.multiExecutionCount; ++i)
            {
                cancelationToken.Retain();
            }
            cancelationSource.Dispose();
            Assert.IsTrue(cancelationToken.CanBeCanceled);
            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(
                () => cancelationToken.Release()
            );
            Assert.IsFalse(cancelationToken.CanBeCanceled);
        }

        [Test]
        public void CancelationRegistrationMayOnlyBeUnregisteredOnce0()
        {
            var cancelationSource = CancelationSource.New();
            var cancelationToken = cancelationSource.Token;
            var registration = cancelationToken.Register(_ => { });

            int successCount = 0, invalidCount = 0;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(
                () =>
                {
                    try
                    {
                        registration.Unregister();
                        Interlocked.Increment(ref successCount);
                    }
                    catch (Proto.Promises.InvalidOperationException)
                    {
                        Interlocked.Increment(ref invalidCount);
                    }
                }
            );
            Assert.AreEqual(1, successCount);
            Assert.AreEqual(ThreadHelper.multiExecutionCount - 1, invalidCount);
            cancelationSource.Dispose();
        }

        [Test]
        public void CancelationRegistrationMayOnlyBeUnregisteredOnce1()
        {
            var cancelationSource = CancelationSource.New();
            var cancelationToken = cancelationSource.Token;
            var registration = cancelationToken.Register(1, (cv, _) => { });

            int successCount = 0, invalidCount = 0;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(
                () =>
                {
                    try
                    {
                        registration.Unregister();
                        Interlocked.Increment(ref successCount);
                    }
                    catch (Proto.Promises.InvalidOperationException)
                    {
                        Interlocked.Increment(ref invalidCount);
                    }
                }
            );
            Assert.AreEqual(1, successCount);
            Assert.AreEqual(ThreadHelper.multiExecutionCount - 1, invalidCount);
            cancelationSource.Dispose();
        }

        [Test]
        public void CancelationRegistrationMayOnlyBeUnregisteredOnce2()
        {
            var cancelationSource = CancelationSource.New();
            var cancelationToken = cancelationSource.Token;
            var registration = cancelationToken.Register(_ => { });

            int successCount = 0, invalidCount = 0;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(
                () =>
                {
                    if (registration.TryUnregister())
                    {
                        Interlocked.Increment(ref successCount);
                    }
                    else
                    {
                        Interlocked.Increment(ref invalidCount);
                    }
                }
            );
            Assert.AreEqual(1, successCount);
            Assert.AreEqual(ThreadHelper.multiExecutionCount - 1, invalidCount);
            cancelationSource.Dispose();
        }

        [Test]
        public void CancelationRegistrationMayOnlyBeUnregisteredOnce3()
        {
            var cancelationSource = CancelationSource.New();
            var cancelationToken = cancelationSource.Token;
            var registration = cancelationToken.Register(1, (cv, _) => { });

            int successCount = 0, invalidCount = 0;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(
                () =>
                {
                    if (registration.TryUnregister())
                    {
                        Interlocked.Increment(ref successCount);
                    }
                    else
                    {
                        Interlocked.Increment(ref invalidCount);
                    }
                }
            );
            Assert.AreEqual(1, successCount);
            Assert.AreEqual(ThreadHelper.multiExecutionCount - 1, invalidCount);
            cancelationSource.Dispose();
        }

        [Test]
        public void CancelationTokenMayBeCanceledAndRegisteredToConcurrently0()
        {
            var cancelationSource = default(CancelationSource);
            var cancelationToken = default(CancelationToken);

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(true,
                 // Setup
                 () =>
                 {
                     cancelationSource = CancelationSource.New();
                     cancelationToken = cancelationSource.Token;
                 },
                 // Teardown
                 () =>
                 {
                     cancelationSource.Dispose();
                 },
                 // Parallel actions
                 () => cancelationSource.TryCancel(),
                 () => cancelationToken.Register(_ => { }),
                 () => cancelationToken.Register(1, (cv, _) => { })
             );
        }

        [Test]
        public void CancelationTokenMayBeCanceledAndRegisteredToConcurrently1()
        {
            var cancelationSource = default(CancelationSource);
            var cancelationToken = default(CancelationToken);

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(true,
                 // Setup
                 () =>
                 {
                     cancelationSource = CancelationSource.New();
                     cancelationToken = cancelationSource.Token;
                 },
                 // Teardown
                 () =>
                 {
                     cancelationSource.Dispose();
                 },
                 // Parallel actions
                 () => cancelationSource.TryCancel(1),
                 () => cancelationToken.Register(_ => { }),
                 () => cancelationToken.Register(1, (cv, _) => { })
             );
        }

        [Test]
        public void CancelationTokenMayBeDisposedAndRegisteredToConcurrently0()
        {
            var cancelationSource = default(CancelationSource);
            var cancelationToken = default(CancelationToken);

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(true,
                 // Setup
                 () =>
                 {
                     cancelationSource = CancelationSource.New();
                     cancelationToken = cancelationSource.Token;
                 },
                 // Teardown
                 () => { },
                 // Parallel actions
                 () => cancelationSource.TryDispose(),
                 () => { try { cancelationToken.Register(_ => { }); } catch (Proto.Promises.InvalidOperationException) { } },
                 () => { try { cancelationToken.Register(1, (cv, _) => { }); } catch (Proto.Promises.InvalidOperationException) { } }
             );
        }

       [Test]
        public void CancelationTokenMayBeDisposedAndRegisteredToConcurrently1()
        {
            var cancelationSource = default(CancelationSource);
            var cancelationToken = default(CancelationToken);

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                 // Setup
                 () =>
                 {
                     cancelationSource = CancelationSource.New();
                     cancelationToken = cancelationSource.Token;
                 },
                 // Teardown
                 () => { },
                 // Parallel actions
                 () => cancelationSource.TryDispose(),
                 () =>
                 {
                     CancelationRegistration cancelationRegistration;
                     cancelationToken.TryRegister(_ => { }, out cancelationRegistration);
                 },
                 () =>
                 {
                     CancelationRegistration cancelationRegistration;
                     cancelationToken.TryRegister(1, (cv, _) => { }, out cancelationRegistration);
                 }
             );
        }

        [Test]
        public void CancelationTokenMayBeCanceledAndUnRegisteredFromConcurrently0()
        {
            var cancelationSource = default(CancelationSource);
            var cancelationToken = default(CancelationToken);
            var cancelationRegistrations = default(CancelationRegistration[]);

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                 // Setup
                 () =>
                 {
                     cancelationSource = CancelationSource.New();
                     cancelationToken = cancelationSource.Token;
                     cancelationRegistrations = new CancelationRegistration[4]
                     {
                         cancelationToken.Register(_ => { }),
                         cancelationToken.Register(_ => { }),
                         cancelationToken.Register(1, (cv, _) => { }),
                         cancelationToken.Register(1, (cv, _) => { })
                     };
                 },
                 // Teardown
                 () =>
                 {
                     cancelationSource.Dispose();
                 },
                 // Parallel actions
                 () => cancelationSource.TryCancel(),
                 () => cancelationRegistrations[0].TryUnregister(),
                 () => cancelationRegistrations[1].TryUnregister(),
                 () => cancelationRegistrations[2].TryUnregister(),
                 () => cancelationRegistrations[3].TryUnregister()
             );
        }

        [Test]
        public void CancelationTokenMayBeCanceledAndUnRegisteredFromConcurrently1()
        {
            var cancelationSource = default(CancelationSource);
            var cancelationToken = default(CancelationToken);
            var cancelationRegistrations = default(CancelationRegistration[]);

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                 // Setup
                 () =>
                 {
                     cancelationSource = CancelationSource.New();
                     cancelationToken = cancelationSource.Token;
                     cancelationRegistrations = new CancelationRegistration[4]
                     {
                         cancelationToken.Register(_ => { }),
                         cancelationToken.Register(_ => { }),
                         cancelationToken.Register(1, (cv, _) => { }),
                         cancelationToken.Register(1, (cv, _) => { })
                     };
                 },
                 // Teardown
                 () =>
                 {
                     cancelationSource.Dispose();
                 },
                 // Parallel actions
                 () => cancelationSource.TryCancel(1),
                 () => cancelationRegistrations[0].TryUnregister(),
                 () => cancelationRegistrations[1].TryUnregister(),
                 () => cancelationRegistrations[2].TryUnregister(),
                 () => cancelationRegistrations[3].TryUnregister()
             );
        }

        [Test]
        public void CancelationTokenMayBeDisposedAndUnRegisteredFromConcurrently()
        {
            var cancelationSource = default(CancelationSource);
            var cancelationToken = default(CancelationToken);
            var cancelationRegistrations = default(CancelationRegistration[]);

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                 // Setup
                 () =>
                 {
                     cancelationSource = CancelationSource.New();
                     cancelationToken = cancelationSource.Token;
                     cancelationRegistrations = new CancelationRegistration[4]
                     {
                         cancelationToken.Register(_ => { }),
                         cancelationToken.Register(_ => { }),
                         cancelationToken.Register(1, (cv, _) => { }),
                         cancelationToken.Register(1, (cv, _) => { })
                     };
                 },
                 // Teardown
                 () => { },
                 // Parallel actions
                 () => cancelationSource.TryDispose(),
                 () => cancelationRegistrations[0].TryUnregister(),
                 () => cancelationRegistrations[1].TryUnregister(),
                 () => cancelationRegistrations[2].TryUnregister(),
                 () => cancelationRegistrations[3].TryUnregister()
             );
        }

        [Test]
        public void LinkedCancelationSourcesMayBeCanceledConcurrently0()
        {
            var cancelationSource1 = default(CancelationSource);
            var cancelationSource2 = default(CancelationSource);
            var cancelationSource3 = default(CancelationSource);

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    cancelationSource1 = CancelationSource.New();
                    cancelationSource2 = CancelationSource.New();
                    cancelationSource3 = CancelationSource.New(cancelationSource1.Token, cancelationSource2.Token);
                },
                // Teardown
                () =>
                {
                    cancelationSource1.Dispose();
                    cancelationSource2.Dispose();
                    cancelationSource3.Dispose();
                },
                // Parallel actions
                () => cancelationSource1.Cancel(),
                () => cancelationSource2.Cancel(),
                () => cancelationSource3.TryCancel()
            );
        }

        [Test]
        public void LinkedCancelationSourcesMayBeCanceledConcurrently1()
        {
            var cancelationSource1 = default(CancelationSource);
            var cancelationSource2 = default(CancelationSource);
            var cancelationSource3 = default(CancelationSource);

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    cancelationSource1 = CancelationSource.New();
                    cancelationSource2 = CancelationSource.New();
                    cancelationSource3 = CancelationSource.New(cancelationSource1.Token, cancelationSource2.Token);
                },
                // Teardown
                () =>
                {
                    cancelationSource1.Dispose();
                    cancelationSource2.Dispose();
                    cancelationSource3.Dispose();
                },
                // Parallel actions
                () => cancelationSource1.Cancel(),
                () => cancelationSource2.Cancel(1),
                () => cancelationSource3.TryCancel("Cancel")
            );
        }

        [Test]
        public void CancelationSourceLinkedToToken1TwiceMayBeCanceledConcurrently0()
        {
            var cancelationSource1 = default(CancelationSource);
            var cancelationSource2 = default(CancelationSource);

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    cancelationSource1 = CancelationSource.New();
                    cancelationSource2 = CancelationSource.New(cancelationSource1.Token, cancelationSource1.Token);
                },
                // Teardown
                () =>
                {
                    cancelationSource1.Dispose();
                    cancelationSource2.Dispose();
                },
                // Parallel actions
                () => cancelationSource1.Cancel(),
                () => cancelationSource2.TryCancel()
            );
        }

        [Test]
        public void CancelationSourceLinkedToToken1TwiceMayBeCanceledConcurrently1()
        {
            var cancelationSource1 = default(CancelationSource);
            var cancelationSource2 = default(CancelationSource);

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    cancelationSource1 = CancelationSource.New();
                    cancelationSource2 = CancelationSource.New(cancelationSource1.Token, cancelationSource1.Token);
                },
                // Teardown
                () =>
                {
                    cancelationSource1.Dispose();
                    cancelationSource2.Dispose();
                },
                // Parallel actions
                () => cancelationSource1.Cancel(),
                () => cancelationSource2.TryCancel(1)
            );
        }

        [Test]
        public void LinkedCancelationSourcesMayBeDisposedConcurrently()
        {
            var cancelationSource1 = default(CancelationSource);
            var cancelationSource2 = default(CancelationSource);
            var cancelationSource3 = default(CancelationSource);

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    cancelationSource1 = CancelationSource.New();
                    cancelationSource2 = CancelationSource.New();
                    cancelationSource3 = CancelationSource.New(cancelationSource1.Token, cancelationSource2.Token);
                },
                // Teardown
                () => { },
                // Parallel actions
                () => cancelationSource1.Dispose(),
                () => cancelationSource2.Dispose(),
                () => cancelationSource3.Dispose()
            );
        }

        [Test]
        public void CancelationSourceLinkedToToken1TwiceMayBeDisposedConcurrently()
        {
            var cancelationSource1 = default(CancelationSource);
            var cancelationSource2 = default(CancelationSource);

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    cancelationSource1 = CancelationSource.New();
                    cancelationSource2 = CancelationSource.New(cancelationSource1.Token, cancelationSource1.Token);
                },
                // Teardown
                () => { },
                // Parallel actions
                () => cancelationSource1.Dispose(),
                () => cancelationSource2.Dispose()
            );
        }

        [Test]
        public void LinkedCancelationSourcesMayBeCanceledAndDisposedConcurrently0()
        {
            var cancelationSource1 = default(CancelationSource);
            var cancelationSource2 = default(CancelationSource);
            var cancelationSource3 = default(CancelationSource);

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    cancelationSource1 = CancelationSource.New();
                    cancelationSource2 = CancelationSource.New();
                    cancelationSource3 = CancelationSource.New(cancelationSource1.Token, cancelationSource2.Token);
                },
                // Teardown
                () => { },
                // Parallel actions
                () => cancelationSource1.TryCancel(),
                () => cancelationSource1.Dispose(),
                () => cancelationSource2.TryCancel(),
                () => cancelationSource2.Dispose(),
                () => cancelationSource3.TryCancel(),
                () => cancelationSource3.Dispose()
            );
        }

        [Test]
        public void LinkedCancelationSourcesMayBeCanceledAndDisposedConcurrently1()
        {
            var cancelationSource1 = default(CancelationSource);
            var cancelationSource2 = default(CancelationSource);
            var cancelationSource3 = default(CancelationSource);

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    cancelationSource1 = CancelationSource.New();
                    cancelationSource2 = CancelationSource.New();
                    cancelationSource3 = CancelationSource.New(cancelationSource1.Token, cancelationSource2.Token);
                },
                // Teardown
                () => { },
                // Parallel actions
                () => cancelationSource1.TryCancel(),
                () => cancelationSource1.Dispose(),
                () => cancelationSource2.TryCancel(1),
                () => cancelationSource2.Dispose(),
                () => cancelationSource3.TryCancel("Cancel"),
                () => cancelationSource3.Dispose()
            );
        }

        [Test]
        public void CancelationSourceLinkedToToken1TwiceMayBeCanceledAndDisposedConcurrently0()
        {
            var cancelationSource1 = default(CancelationSource);
            var cancelationSource2 = default(CancelationSource);

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    cancelationSource1 = CancelationSource.New();
                    cancelationSource2 = CancelationSource.New(cancelationSource1.Token, cancelationSource1.Token);
                },
                // Teardown
                () => { },
                // Parallel actions
                () => cancelationSource1.TryCancel(),
                () => cancelationSource1.Dispose(),
                () => cancelationSource2.TryCancel(),
                () => cancelationSource2.Dispose()
            );
        }

        [Test]
        public void CancelationSourceLinkedToToken1TwiceMayBeCanceledAndDisposedConcurrently1()
        {
            var cancelationSource1 = default(CancelationSource);
            var cancelationSource2 = default(CancelationSource);

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    cancelationSource1 = CancelationSource.New();
                    cancelationSource2 = CancelationSource.New(cancelationSource1.Token, cancelationSource1.Token);
                },
                // Teardown
                () => { },
                // Parallel actions
                () => cancelationSource1.TryCancel(),
                () => cancelationSource1.Dispose(),
                () => cancelationSource2.TryCancel(1),
                () => cancelationSource2.Dispose()
            );
        }

        [Test]
        public void CancelationSourceMayBeCanceledAndItsTokenLinkedToANewCancelationSourceConcurrently()
        {
            var cancelationSource1 = default(CancelationSource);
            var cancelationSource2 = default(CancelationSource);
            var cancelationSource3 = default(CancelationSource);

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () => cancelationSource1 = CancelationSource.New(),
                // Teardown
                () =>
                {
                    cancelationSource1.Dispose();
                    cancelationSource2.Dispose();
                    cancelationSource3.Dispose();
                },
                // Parallel actions
                () =>
                {
                    var s = CancelationSource.New(); // Increases the contention between cancel and linking the token.
                    cancelationSource1.Cancel();
                    s.Dispose();
                },
                () => cancelationSource2 = CancelationSource.New(cancelationSource1.Token),
                () => cancelationSource3 = CancelationSource.New(cancelationSource1.Token)
            );
        }

        [Test]
        public void CancelationSourceMayBeDisposedAndItsTokenLinkedToANewCancelationSourceConcurrently()
        {
            var cancelationSource1 = default(CancelationSource);
            var cancelationSource2 = default(CancelationSource);
            var cancelationSource3 = default(CancelationSource);

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () => cancelationSource1 = CancelationSource.New(),
                // Teardown
                () =>
                {
                    cancelationSource2.Dispose();
                    cancelationSource3.Dispose();
                },
                // Parallel actions
                () =>
                {
                    var s = CancelationSource.New(); // Increases the contention between dispose and linking the token.
                    cancelationSource1.Dispose();
                    s.Dispose();
                },
                () => cancelationSource2 = CancelationSource.New(cancelationSource1.Token),
                () => cancelationSource3 = CancelationSource.New(cancelationSource1.Token)
            );
        }

        [Test]
        public void CancelationSourcesMayBeCanceledAndTheirTokensLinkedToANewCancelationSourceConcurrently()
        {
            var cancelationSource1 = default(CancelationSource);
            var cancelationSource2 = default(CancelationSource);
            var cancelationSource3 = default(CancelationSource);
            var cancelationSource4 = default(CancelationSource);

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    cancelationSource1 = CancelationSource.New();
                    cancelationSource2 = CancelationSource.New();
                },
                // Teardown
                () =>
                {
                    cancelationSource1.Dispose();
                    cancelationSource2.Dispose();
                    cancelationSource3.Dispose();
                    cancelationSource4.Dispose();
                },
                // Parallel actions
                () =>
                {
                    var s = CancelationSource.New(); // Increases the contention between cancel and linking the tokens.
                    cancelationSource1.Cancel();
                    s.Dispose();
                },
                () =>
                {
                    var s = CancelationSource.New(); // Increases the contention between cancel and linking the tokens.
                    cancelationSource2.Cancel();
                    s.Dispose();
                },
                () => cancelationSource3 = CancelationSource.New(cancelationSource1.Token, cancelationSource2.Token),
                () => cancelationSource4 = CancelationSource.New(cancelationSource1.Token, cancelationSource2.Token)
            );
        }

        [Test]
        public void CancelationSourcesMayBeDisposedAndTheirTokensLinkedToANewCancelationSourceConcurrently()
        {
            var cancelationSource1 = default(CancelationSource);
            var cancelationSource2 = default(CancelationSource);
            var cancelationSource3 = default(CancelationSource);
            var cancelationSource4 = default(CancelationSource);

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    cancelationSource1 = CancelationSource.New();
                    cancelationSource2 = CancelationSource.New();
                },
                // Teardown
                () =>
                {
                    cancelationSource3.Dispose();
                    cancelationSource4.Dispose();
                },
                // Parallel actions
                () =>
                {
                    var s = CancelationSource.New(); // Increases the contention between dispose and linking the tokens.
                    cancelationSource1.Dispose();
                    s.Dispose();
                },
                () =>
                {
                    var s = CancelationSource.New(); // Increases the contention between dispose and linking the tokens.
                    cancelationSource2.Dispose();
                    s.Dispose();
                },
                () => cancelationSource3 = CancelationSource.New(cancelationSource1.Token, cancelationSource2.Token),
                () => cancelationSource4 = CancelationSource.New(cancelationSource1.Token, cancelationSource2.Token)
            );
        }

        [Test]
        public void CanceledTokenMayThrowIfCancelationRequestedConcurrently()
        {
            int invoked = 0;
            var cancelationSource = CancelationSource.New();
            var cancelationToken = cancelationSource.Token;
            cancelationSource.Cancel();

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(() =>
            {
                try
                {
                    cancelationToken.ThrowIfCancelationRequested();
                }
                catch (CanceledException)
                {
                    Interlocked.Increment(ref invoked);
                }
            });
            cancelationSource.Dispose();
            Assert.AreEqual(ThreadHelper.multiExecutionCount, invoked);
        }

        [Test]
        public void DisposedTokenMayThrowIfCancelationRequestedConcurrently()
        {
            int invoked = 0;
            var cancelationSource = CancelationSource.New();
            var cancelationToken = cancelationSource.Token;
            cancelationSource.Dispose();

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(() =>
            {
                cancelationToken.ThrowIfCancelationRequested();
                Interlocked.Increment(ref invoked);
            });
            Assert.AreEqual(ThreadHelper.multiExecutionCount, invoked);
        }

        [Test]
        public void CancelationSourceMayBeCanceledAndItsTokenMayThrowIfCancelationRequestedConcurrently()
        {
            var cancelationSource = default(CancelationSource);
            var cancelationToken = default(CancelationToken);

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    cancelationSource = CancelationSource.New();
                    cancelationToken = cancelationSource.Token;
                },
                // Teardown
                () =>
                {
                    cancelationSource.Dispose();
                },
                // Parallel actions
                () =>
                {
                    try { cancelationToken.ThrowIfCancelationRequested(); } catch { }
                },
                () =>
                {
                    try { cancelationToken.ThrowIfCancelationRequested(); } catch { }
                },
                () => cancelationSource.TryCancel(),
                () => cancelationSource.TryCancel(1)
            );
        }

        [Test]
        public void CancelationSourceMayBeDisposedAndItsTokenMayThrowIfCancelationRequestedConcurrently()
        {
            var cancelationSource = default(CancelationSource);
            var cancelationToken = default(CancelationToken);

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () =>
                {
                    cancelationSource = CancelationSource.New();
                    cancelationToken = cancelationSource.Token;
                },
                // Teardown
                () => { },
                // Parallel actions
                () =>
                {
                    try { cancelationToken.ThrowIfCancelationRequested(); } catch { }
                },
                () =>
                {
                    try { cancelationToken.ThrowIfCancelationRequested(); } catch { }
                },
                () => cancelationSource.Dispose()
            );
        }

        [Test]
        public void CancelationMegaConcurrencyTest()
        {
            // Hammer every public API at the same time to test the stability of the thread-safety.
            var cancelationSource = default(CancelationSource);
            var cancelationToken = default(CancelationToken);
            var cancelationRegistration1 = default(CancelationRegistration);
            var cancelationRegistration2 = default(CancelationRegistration);

            // Can't use ExecuteParallelActionsWithOffsets since 3^20 would take forever.
            new ThreadHelper().ExecuteParallelActions(ThreadHelper.multiExecutionCount,
                 // Setup
                 () =>
                 {
                     cancelationSource = CancelationSource.New();
                     cancelationToken = cancelationSource.Token;
                 },
                 // Teardown
                 () => { },
                 // Parallel actions
                 () => cancelationSource.TryCancel(),
                 () => cancelationSource.TryCancel(1),
                 () => cancelationSource.TryCancel("Cancel"),
                 () => cancelationSource.Dispose(),
                 () => { bool _ = cancelationSource.IsCancelationRequested; },
                 () => { bool _ = cancelationSource.IsValid; },
                 () => { CancelationToken _ = cancelationSource.Token; },
                 () => { bool _ = cancelationToken.IsCancelationRequested; },
                 () => { bool _ = cancelationToken.CanBeCanceled; },
                 () =>
                 {
                     try
                     {
                         object _ = cancelationToken.CancelationValue;
                     }
                     catch (Proto.Promises.InvalidOperationException) { }
                 },
                 () =>
                 {
                     try
                     {
                         Type _ = cancelationToken.CancelationValueType;
                     }
                     catch (Proto.Promises.InvalidOperationException) { }
                 },
                 () =>
                 {
                     try
                     {
                         int _;
                         cancelationToken.TryGetCancelationValueAs(out _);
                     }
                     catch (Proto.Promises.InvalidOperationException) { }
                 },
                 () =>
                 {
                     try
                     {
                         cancelationToken.ThrowIfCancelationRequested();
                     }
                     catch (CanceledException) { }
                 },
                 () => cancelationToken.TryRegister(_ => { }, out cancelationRegistration1),
                 () => cancelationToken.TryRegister(1, (cv, _) => { }, out cancelationRegistration2),
                 () =>
                 {
                     try
                     {
                         cancelationToken.Register(_ => { });
                     }
                     catch (Proto.Promises.InvalidOperationException) { }
                 },
                 () =>
                 {
                     try
                     {
                         cancelationToken.Register(1, (cv, _) => { });
                     }
                     catch (Proto.Promises.InvalidOperationException) { }
                 },
                 () => { bool _ = cancelationRegistration1.IsRegistered; },
                 () => { bool _1, _2; cancelationRegistration1.GetIsRegisteredAndIsCancelationRequested(out _1, out _2); },
                 () => cancelationRegistration1.TryUnregister(),
                 () => { bool _; cancelationRegistration1.TryUnregister(out _); },
                 () => { bool _ = cancelationRegistration2.IsRegistered; },
                 () => { bool _1, _2; cancelationRegistration2.GetIsRegisteredAndIsCancelationRequested(out _1, out _2); },
                 () => cancelationRegistration2.TryUnregister(),
                 () => { bool _; cancelationRegistration2.TryUnregister(out _); }
             );
        }
    }
}

#endif // !UNITY_WEBGL