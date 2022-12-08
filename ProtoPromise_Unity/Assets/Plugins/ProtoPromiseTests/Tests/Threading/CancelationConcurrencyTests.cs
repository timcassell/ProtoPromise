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
        public void CancelationSourceMayBeDisposedOnlyOnce3()
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
                () =>
                {
                    cancelationSource = CancelationSource.New();
                    cancelationSource.Token.Register(() => { });
                },
                // Teardown
                () => { },
                // Parallel actions
                () => cancelationSource.TryCancel(),
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
                () => cancelationToken.Register(() => Interlocked.Increment(ref invokedCount))
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
                () => cancelationToken.Register(1, cv => Interlocked.Increment(ref invokedCount))
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
                () => registrations.Add(cancelationToken.Register(() => { }))
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
                () => registrations.Add(cancelationToken.Register(1, cv => { }))
            );

            var diffChecker = new HashSet<CancelationRegistration>();
            if (!registrations.All(diffChecker.Add))
            {
                Assert.Fail("cancelationToken.Register returned at least one of the same CancelationRegistration instance.");
            }
            cancelationSource.Dispose();
        }

        [Test]
        public void CancelationTokenMayBeRetainedConcurrentlyWithoutCancel()
        {
            var cancelationSource = CancelationSource.New();
            var cancelationToken = cancelationSource.Token;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(
                () => cancelationToken.TryRetain()
            );
            Assert.IsTrue(cancelationToken.CanBeCanceled);
            cancelationSource.Dispose();
            for (int i = 0; i < ThreadHelper.multiExecutionCount; ++i)
            {
                cancelationToken.Release();
            }
            Assert.IsFalse(cancelationToken.CanBeCanceled);
        }

        [Test]
        public void CancelationTokenMayBeReleasedConcurrentlyWithoutCancel()
        {
            var cancelationSource = CancelationSource.New();
            var cancelationToken = cancelationSource.Token;

            for (int i = 0; i < ThreadHelper.multiExecutionCount; ++i)
            {
                cancelationToken.TryRetain();
            }
            Assert.IsTrue(cancelationToken.CanBeCanceled);
            cancelationSource.Dispose();
            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(
                () => cancelationToken.Release()
            );
            Assert.IsFalse(cancelationToken.CanBeCanceled);
        }

        [Test]
        public void CancelationTokenMayBeRetainedConcurrentlyAfterCanceled()
        {
            var cancelationSource = CancelationSource.New();
            var cancelationToken = cancelationSource.Token;
            cancelationSource.Cancel();

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(
                () => cancelationToken.TryRetain()
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
        public void CancelationTokenMayBeReleasedConcurrentlyAfterCanceled()
        {
            var cancelationSource = CancelationSource.New();
            var cancelationToken = cancelationSource.Token;
            cancelationSource.Cancel();

            for (int i = 0; i < ThreadHelper.multiExecutionCount; ++i)
            {
                cancelationToken.TryRetain();
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
        public void CancelationRegistrationMayOnlyBeUnregisteredOnce_0()
        {
            var cancelationSource = CancelationSource.New();
            var cancelationToken = cancelationSource.Token;
            var registration = cancelationToken.Register(() => { });

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
        public void CancelationRegistrationMayOnlyBeUnregisteredOnce_1()
        {
            var cancelationSource = CancelationSource.New();
            var cancelationToken = cancelationSource.Token;
            var registration = cancelationToken.Register(1, cv => { });

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
        public void CancelationTokenMayBeCanceledAndRegisteredToConcurrently()
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
                 () => cancelationToken.Register(() => { }),
                 () => cancelationToken.Register(1, cv => { })
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
                 () => { try { cancelationToken.Register(() => { }); } catch (Proto.Promises.InvalidOperationException) { } },
                 () => { try { cancelationToken.Register(1, cv => { }); } catch (Proto.Promises.InvalidOperationException) { } }
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
                     cancelationToken.TryRegister(() => { }, out cancelationRegistration);
                 },
                 () =>
                 {
                     CancelationRegistration cancelationRegistration;
                     cancelationToken.TryRegister(1, cv => { }, out cancelationRegistration);
                 }
             );
        }

        [Test]
        public void CancelationTokenMayBeCanceledAndRegistrationUnRegisteredConcurrently()
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
                         cancelationToken.Register(() => { }),
                         cancelationToken.Register(() => { }),
                         cancelationToken.Register(1, cv => { }),
                         cancelationToken.Register(1, cv => { })
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
        public void CancelationTokenMayBeCanceledAndRegistrationDisposedConcurrently()
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
                         cancelationToken.Register(() => { }),
                         cancelationToken.Register(() => { }),
                         cancelationToken.Register(1, cv => { }),
                         cancelationToken.Register(1, cv => { })
                     };
                 },
                 // Teardown
                 () =>
                 {
                     cancelationSource.Dispose();
                 },
                 // Parallel actions
                 () => cancelationSource.TryCancel(),
                 () => cancelationRegistrations[0].Dispose(),
                 () => cancelationRegistrations[1].Dispose(),
                 () => cancelationRegistrations[2].Dispose(),
                 () => cancelationRegistrations[3].Dispose()
             );
        }

        [Test]
        public void CancelationTokenMayBeDisposedAndRegistrationUnRegisteredConcurrently()
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
                         cancelationToken.Register(() => { }),
                         cancelationToken.Register(() => { }),
                         cancelationToken.Register(1, cv => { }),
                         cancelationToken.Register(1, cv => { })
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
        public void CancelationTokenMayBeDisposedAndRegistrationDisposedConcurrently()
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
                         cancelationToken.Register(() => { }),
                         cancelationToken.Register(() => { }),
                         cancelationToken.Register(1, cv => { }),
                         cancelationToken.Register(1, cv => { })
                     };
                 },
                 // Teardown
                 () => { },
                 // Parallel actions
                 () => cancelationSource.TryDispose(),
                 () => cancelationRegistrations[0].Dispose(),
                 () => cancelationRegistrations[1].Dispose(),
                 () => cancelationRegistrations[2].Dispose(),
                 () => cancelationRegistrations[3].Dispose()
             );
        }

#if NET6_0_OR_GREATER || UNITY_2021_2_OR_NEWER
        [Test]
        public void CancelationTokenMayBeCanceledAndRegistrationDisposedAsyncConcurrently()
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
                         cancelationToken.Register(() => { }),
                         cancelationToken.Register(() => { }),
                         cancelationToken.Register(1, cv => { }),
                         cancelationToken.Register(1, cv => { })
                     };
                 },
                 // Teardown
                 () =>
                 {
                     cancelationSource.Dispose();
                 },
                 // Parallel actions
                 () => cancelationSource.TryCancel(),
                 () => cancelationRegistrations[0].DisposeAsync().Forget(),
                 () => cancelationRegistrations[1].DisposeAsync().Forget(),
                 () => cancelationRegistrations[2].DisposeAsync().Forget(),
                 () => cancelationRegistrations[3].DisposeAsync().Forget()
             );
        }

        [Test]
        public void CancelationTokenMayBeDisposedAndRegistrationDisposedAsyncConcurrently()
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
                         cancelationToken.Register(() => { }),
                         cancelationToken.Register(() => { }),
                         cancelationToken.Register(1, cv => { }),
                         cancelationToken.Register(1, cv => { })
                     };
                 },
                 // Teardown
                 () => { },
                 // Parallel actions
                 () => cancelationSource.TryDispose(),
                 () => cancelationRegistrations[0].DisposeAsync().Forget(),
                 () => cancelationRegistrations[1].DisposeAsync().Forget(),
                 () => cancelationRegistrations[2].DisposeAsync().Forget(),
                 () => cancelationRegistrations[3].DisposeAsync().Forget()
             );
        }
#endif // NET6_0_OR_GREATER || UNITY_2021_2_OR_NEWER

        [Test]
        public void LinkedCancelationSourcesMayBeCanceledConcurrently()
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
        public void CancelationSourceLinkedToToken1TwiceMayBeCanceledConcurrently()
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
        public void LinkedCancelationSourcesMayBeCanceledAndDisposedConcurrently()
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
        public void CancelationSourceLinkedToToken1TwiceMayBeCanceledAndDisposedConcurrently()
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
                    try { cancelationToken.ThrowIfCancelationRequested(); } catch (CanceledException) { }
                },
                () =>
                {
                    try { cancelationToken.ThrowIfCancelationRequested(); } catch (CanceledException) { }
                },
                () => cancelationSource.TryCancel(),
                () => cancelationSource.TryCancel()
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
                    cancelationSource.Cancel();
                    cancelationToken = cancelationSource.Token;
                },
                // Teardown
                () => { },
                // Parallel actions
                () =>
                {
                    try { cancelationToken.ThrowIfCancelationRequested(); } catch (CanceledException) { }
                },
                () =>
                {
                    try { cancelationToken.ThrowIfCancelationRequested(); } catch (CanceledException) { }
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

            // Can't use ExecuteParallelActionsWithOffsets since 3^24 would take forever.
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
                         cancelationToken.ThrowIfCancelationRequested();
                     }
                     catch (CanceledException) { }
                 },
                 () => cancelationToken.TryRegister(() => { }, out cancelationRegistration1),
                 () => cancelationToken.TryRegister(1, cv => { }, out cancelationRegistration2),
                 () =>
                 {
                     try
                     {
                         cancelationToken.Register(() => { });
                     }
                     catch (Proto.Promises.InvalidOperationException) { }
                 },
                 () =>
                 {
                     try
                     {
                         cancelationToken.Register(1, cv => { });
                     }
                     catch (Proto.Promises.InvalidOperationException) { }
                 },
                 () => { bool _ = cancelationRegistration1.IsRegistered; },
                 () => { bool _1, _2; cancelationRegistration1.GetIsRegisteredAndIsCancelationRequested(out _1, out _2); },
                 () => cancelationRegistration1.TryUnregister(),
                 () => cancelationRegistration1.Dispose(),
                 () => { bool _; cancelationRegistration1.TryUnregister(out _); },
                 () => { bool _ = cancelationRegistration2.IsRegistered; },
                 () => { bool _1, _2; cancelationRegistration2.GetIsRegisteredAndIsCancelationRequested(out _1, out _2); },
                 () => cancelationRegistration2.TryUnregister(),
                 () => cancelationRegistration2.Dispose(),
                 () => { bool _; cancelationRegistration2.TryUnregister(out _); }
#if NET6_0_OR_GREATER || UNITY_2021_2_OR_NEWER
                 , () => cancelationRegistration1.DisposeAsync().Forget(),
                 () => cancelationRegistration2.DisposeAsync().Forget()
#endif
             );
        }
    }
}

#endif // !UNITY_WEBGL