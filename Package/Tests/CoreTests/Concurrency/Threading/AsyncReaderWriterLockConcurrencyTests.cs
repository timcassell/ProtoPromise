#if !UNITY_WEBGL

using NUnit.Framework;
using Proto.Promises;
using Proto.Promises.Threading;
using System;
using System.Collections.Generic;
using System.Threading;

namespace ProtoPromiseTests.Concurrency.Threading
{
#if UNITY_2021_2_OR_NEWER || NETSTANDARD2_1_OR_GREATER || NETCOREAPP
    public class AsyncReaderWriterLockConcurrencyTests
    {
        // 6 is better, but 5 lets the tests complete in a reasonable amount of time.
        private const int ConcurrentRunnerCount = 5;

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

        private static IEnumerable<TestCaseData> GetLockTypeCounts()
        {
            // Dirty way to get a combination for readers, writers, and upgradeables that are async, sync, or try lock entries.
            for (int numReaders = 0; numReaders < ConcurrentRunnerCount; ++numReaders)
            for (int numWriters = 0; numWriters < ConcurrentRunnerCount; ++numWriters)
            for (int numUpgradeables = 0; numUpgradeables < ConcurrentRunnerCount; ++numUpgradeables)
                        
            for (int numReadersAsync = 0; numReadersAsync < ConcurrentRunnerCount; ++numReadersAsync)
            for (int numWritersAsync = 0; numWritersAsync < ConcurrentRunnerCount; ++numWritersAsync)
            for (int numUpgradeablesAsync = 0; numUpgradeablesAsync < ConcurrentRunnerCount; ++numUpgradeablesAsync)
                                    
            for (int numReadersTry = 0; numReadersTry < ConcurrentRunnerCount; ++numReadersTry)
            for (int numWritersTry = 0; numWritersTry < ConcurrentRunnerCount; ++numWritersTry)
            for (int numUpgradeablesTry = 0; numUpgradeablesTry < ConcurrentRunnerCount; ++numUpgradeablesTry)
            {
                // Ideally, we would not restrain the number of any type of lock,
                // but this is necessary in order to reduce the number of tests to not take too long.
                int totalReaders = numReaders + numReadersAsync + numReadersTry;
                int totalWriters = numWriters + numWritersAsync + numWritersTry;
                int totalUpgradeables = numUpgradeables + numUpgradeablesAsync + numUpgradeablesTry;
                
                int total = totalReaders + totalWriters + totalUpgradeables;

                if (total == ConcurrentRunnerCount && (totalWriters + totalUpgradeables) <= 3)
                {
                    yield return new TestCaseData(
                        numReaders,         numReadersAsync,        numReadersTry,
                        numWriters,         numWritersAsync,        numWritersTry,
                        numUpgradeables,    numUpgradeablesAsync,   numUpgradeablesTry
                    );
                }
            }
        }

        [Test, TestCaseSource(nameof(GetLockTypeCounts))]
        public void AsyncReaderWriterLock_EnteredConcurrenctly_LocksProperly(
            int numReaders,         int numReadersAsync,        int numReadersTry,
            int numWriters,         int numWritersAsync,        int numWritersTry,
            int numUpgradeables,    int numUpgradeablesAsync,   int numUpgradeablesTry)
        {
            var rwl = new AsyncReaderWriterLock();

            int lockCount = 0; // 0 for unlocked, -1 for writer locked, otherwise the count of readers.
            int upgradeableLockCount = 0;
            int lockEnteredCount = 0;

            void InsideReaderLock()
            {
                Assert.Greater(Interlocked.Increment(ref lockCount), -1);
                Interlocked.Increment(ref lockEnteredCount);
                Interlocked.Decrement(ref lockCount);
            }

            void InsideWriterLock()
            {
                Assert.AreEqual(0, Interlocked.CompareExchange(ref lockCount, -1, 0));
                Interlocked.Increment(ref lockEnteredCount);
                Assert.AreEqual(-1, Interlocked.CompareExchange(ref lockCount, 0, -1));
            }

            async Promise InsideUpgradeableLock(AsyncReaderWriterLock.UpgradeableReaderKey key, int upgradeType)
            {
                var upgradeableCount = Interlocked.Increment(ref upgradeableLockCount);
                Assert.AreEqual(1, upgradeableCount);

                InsideReaderLock();
                if (upgradeType == 0)
                {
                    using (rwl.UpgradeToWriterLock(key))
                    {
                        InsideWriterLock();
                    }
                }
                else if (upgradeType == 1)
                {
                    using (await rwl.UpgradeToWriterLockAsync(key))
                    {
                        InsideWriterLock();
                    }
                }
                else
                {
                    AsyncReaderWriterLock.WriterKey writerKey = default;
                    SpinWait.SpinUntil(() => rwl.TryUpgradeToWriterLock(key, out writerKey));
                    using (writerKey)
                    {
                        InsideWriterLock();
                    }
                }
                InsideReaderLock();

                Assert.AreEqual(0, Interlocked.Decrement(ref upgradeableLockCount));
            }

            List<Action> actions = new List<Action>(6);

            // Using Promise.Run even for synchronous locks in order to try to have the same amount of overhead to maximize contention.
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
            for (int i = 0; i < numReaders; i++)
            {
                actions.Add(() =>
                {
                    Promise.Run(async () =>
                    {
                        using (rwl.ReaderLock())
                        {
                            InsideReaderLock();
                        }
                    }, SynchronizationOption.Synchronous)
                        .WaitWithTimeout(TimeSpan.FromSeconds(1));
                });
            }
            for (int i = 0; i < numReadersAsync; i++)
            {
                actions.Add(() =>
                {
                    Promise.Run(async () =>
                    {
                        using (await rwl.ReaderLockAsync())
                        {
                            InsideReaderLock();
                        }
                    }, SynchronizationOption.Synchronous)
                        .WaitWithTimeout(TimeSpan.FromSeconds(1));
                });
            }
            for (int i = 0; i < numReadersTry; i++)
            {
                actions.Add(() =>
                {
                    Promise.Run(async () =>
                    {
                        AsyncReaderWriterLock.ReaderKey key = default;
                        SpinWait.SpinUntil(() => rwl.TryEnterReaderLock(out key));
                        using (key)
                        {
                            InsideReaderLock();
                        }
                    }, SynchronizationOption.Synchronous)
                        .WaitWithTimeout(TimeSpan.FromSeconds(1));
                });
            }

            for (int i = 0; i < numWriters; i++)
            {
                actions.Add(() =>
                {
                    Promise.Run(async () =>
                    {
                        using (rwl.WriterLock())
                        {
                            InsideWriterLock();
                        }
                    }, SynchronizationOption.Synchronous)
                        .WaitWithTimeout(TimeSpan.FromSeconds(1));
                });
            }
            for (int i = 0; i < numWritersAsync; i++)
            {
                actions.Add(() =>
                {
                    Promise.Run(async () =>
                    {
                        using (await rwl.WriterLockAsync())
                        {
                            InsideWriterLock();
                        }
                    }, SynchronizationOption.Synchronous)
                        .WaitWithTimeout(TimeSpan.FromSeconds(1));
                });
            }
            for (int i = 0; i < numWritersTry; i++)
            {
                actions.Add(() =>
                {
                    Promise.Run(async () =>
                    {
                        AsyncReaderWriterLock.WriterKey key = default;
                        SpinWait.SpinUntil(() => rwl.TryEnterWriterLock(out key));
                        using (key)
                        {
                            InsideWriterLock();
                        }
                    }, SynchronizationOption.Synchronous)
                        .WaitWithTimeout(TimeSpan.FromSeconds(1));
                });
            }

            int upgradeCounter = 0;
            for (int i = 0; i < numUpgradeables; i++)
            {
                int upgradeType = ++upgradeCounter % 3;
                actions.Add(() =>
                {
                    Promise.Run(async () =>
                    {
                        using (var key = rwl.UpgradeableReaderLock())
                        {
                            await InsideUpgradeableLock(key, upgradeType);
                        }
                    }, SynchronizationOption.Synchronous)
                        .WaitWithTimeout(TimeSpan.FromSeconds(1));
                });
            }
            for (int i = 0; i < numUpgradeablesAsync; i++)
            {
                int upgradeType = ++upgradeCounter % 3;
                actions.Add(() =>
                {
                    Promise.Run(async () =>
                    {
                        using (var key = await rwl.UpgradeableReaderLockAsync())
                        {
                            await InsideUpgradeableLock(key, upgradeType);
                        }
                    }, SynchronizationOption.Synchronous)
                        .WaitWithTimeout(TimeSpan.FromSeconds(1));
                });
            }
            for (int i = 0; i < numUpgradeablesTry; i++)
            {
                int upgradeType = ++upgradeCounter % 3;
                actions.Add(() =>
                {
                    Promise.Run(async () =>
                    {
                        AsyncReaderWriterLock.UpgradeableReaderKey key = default;
                        SpinWait.SpinUntil(() => rwl.TryEnterUpgradeableReaderLock(out key));
                        using (key)
                        {
                            await InsideUpgradeableLock(key, upgradeType);
                        }
                    }, SynchronizationOption.Synchronous)
                        .WaitWithTimeout(TimeSpan.FromSeconds(1));
                });
            }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

            int expectedLocks = (ConcurrentRunnerCount + (numUpgradeables + numUpgradeablesAsync + numUpgradeablesTry) * 2) * ThreadHelper.GetExpandCount(ConcurrentRunnerCount);

            new ThreadHelper().ExecuteParallelActionsWithOffsets(true, // Repeat the parallel actions for as many available hardware threads.
                // setup
                () =>
                {
                    lockEnteredCount = 0;
                },
                // teardown
                () =>
                {
                    Assert.AreEqual(expectedLocks, lockEnteredCount);
                },
                // parallel actions
                actions.ToArray()
            );
        }
    }
#endif // UNITY_2021_2_OR_NEWER || NETSTANDARD2_1_OR_GREATER || NETCOREAPP
}

#endif // !UNITY_WEBGL