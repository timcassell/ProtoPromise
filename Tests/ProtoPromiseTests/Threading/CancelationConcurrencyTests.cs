#if CSHARP_7_OR_LATER

using NUnit.Framework;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Proto.Promises.Tests.Threading
{
    public class CancelationConcurrencyTests
    {
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
                    catch (InvalidOperationException)
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
                    catch (InvalidOperationException)
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
                    catch (InvalidOperationException)
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
                    catch (InvalidOperationException)
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
                    catch (InvalidOperationException)
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
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () => cancelationSource = CancelationSource.New(),
                // Teardown
                () => { },
                // Parallel actions
                () => cancelationSource.TryCancel(),
                () => cancelationSource.Dispose()
            );
        }

        [Test]
        public void CancelationSourceMayBeCanceledAndDisposedConcurrently1()
        {
            var cancelationSource = default(CancelationSource);

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                // Setup
                () => cancelationSource = CancelationSource.New(),
                // Teardown
                () => { },
                // Parallel actions
                () => cancelationSource.TryCancel("Cancel"),
                () => cancelationSource.Dispose()
            );
        }

        [Test]
        public void CancelationSourceMayBeCanceledAndDisposedConcurrently2()
        {
            var cancelationSource = default(CancelationSource);

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
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
                () => cancelationSource.Dispose()
            );
        }

        [Test]
        public void CancelationSourceMayBeCanceledAndDisposedConcurrently3()
        {
            var cancelationSource = default(CancelationSource);

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
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
                () => cancelationSource.Dispose()
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
                    catch (InvalidOperationException)
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
                    catch (InvalidOperationException)
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
                () => cancelationSource1.Cancel(),
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
                () => cancelationSource1.Dispose(),
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
                () => cancelationSource1.Cancel(),
                () => cancelationSource2.Cancel(),
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
                () => cancelationSource1.Dispose(),
                () => cancelationSource2.Dispose(),
                () => cancelationSource3 = CancelationSource.New(cancelationSource1.Token, cancelationSource2.Token),
                () => cancelationSource4 = CancelationSource.New(cancelationSource1.Token, cancelationSource2.Token)
            );
        }
    }
}

#endif