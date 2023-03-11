using NUnit.Framework;
using Proto.Promises;
using Proto.Promises.Threading;
using System;
using System.Collections.Generic;
using System.Threading;

namespace ProtoPromiseTests.APIs
{
#if UNITY_2021_2_OR_NEWER || NETSTANDARD2_1_OR_GREATER || NETCOREAPP
    public class AsyncReaderWriterLockTests
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
        public void AsyncReaderWriterLock_Unlocked_PermitsWriterLock_Async()
        {
            var rwl = new AsyncReaderWriterLock();

            Promise.Run(async () =>
            {
                using (await rwl.WriterLockAsync()) { }
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeout(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AsyncReaderWriterLock_Unlocked_PermitsWriterLock_Sync()
        {
            var rwl = new AsyncReaderWriterLock();

            var key = rwl.WriterLock();
            key.Dispose();
        }

        [Test]
        public void AsyncReaderWriterLock_Unlocked_PermitsTryWriterLock_Sync()
        {
            var rwl = new AsyncReaderWriterLock();

            bool entered = rwl.TryEnterWriterLock(out var key);
            Assert.True(entered);
            key.Dispose();
        }

        [Test]
        public void AsyncReaderWriterLock_Unlocked_PermitsMultipleReaderLocks_Async()
        {
            int lockHolders = 0;
            var rwl = new AsyncReaderWriterLock();

            var readerPromise1 = rwl.ReaderLockAsync()
                .Then(key =>
                {
                    Interlocked.Increment(ref lockHolders);
                    return key;
                });
            var readerPromise2 = rwl.ReaderLockAsync()
                .Then(key =>
                {
                    Interlocked.Increment(ref lockHolders);
                    return key;
                });

            Assert.AreEqual(2, lockHolders);

            readerPromise1
                .Then(key => key.Dispose())
                .WaitWithTimeout(TimeSpan.FromSeconds(1));
            readerPromise2
                .Then(key => key.Dispose())
                .WaitWithTimeout(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AsyncReaderWriterLock_Unlocked_PermitsMultipleReaderLocks_Sync()
        {
            var rwl = new AsyncReaderWriterLock();

            var key1 = rwl.ReaderLock();
            var key2 = rwl.ReaderLock();
            key1.Dispose();
            key2.Dispose();
        }

        [Test]
        public void AsyncReaderWriterLock_Unlocked_PermitsMultipleTryReaderLocks_Sync()
        {
            var rwl = new AsyncReaderWriterLock();

            bool entered1 = rwl.TryEnterReaderLock(out var key1);
            bool entered2 = rwl.TryEnterReaderLock(out var key2);
            Assert.True(entered1);
            Assert.True(entered2);
            key1.Dispose();
            key2.Dispose();
        }

        [Test]
        public void AsyncReaderWriterLock_Unlocked_PermitsUpgradeableReaderLock_Async()
        {
            var rwl = new AsyncReaderWriterLock();

            Promise.Run(async () =>
            {
                using (await rwl.UpgradeableReaderLockAsync()) { }
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeout(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AsyncReaderWriterLock_Unlocked_PermitsUpgradeableReaderLock_Sync()
        {
            var rwl = new AsyncReaderWriterLock();

            var key = rwl.UpgradeableReaderLock();
            key.Dispose();
        }

        [Test]
        public void AsyncReaderWriterLock_Unlocked_PermitsTryUpgradeableReaderLock_Sync()
        {
            var rwl = new AsyncReaderWriterLock();

            bool entered = rwl.TryEnterUpgradeableReaderLock(out var key);
            Assert.True(entered);
            key.Dispose();
        }

        [Test]
        public void AsyncReaderWriterLock_UpgradeableReaderLocked_PermitsUpgradeToWriterLock_Async()
        {
            var rwl = new AsyncReaderWriterLock();

            Promise.Run(async () =>
            {
                using (var readerKey = await rwl.UpgradeableReaderLockAsync())
                {
                    using (await rwl.UpgradeToWriterLockAsync(readerKey)) { }
                }
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeout(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AsyncReaderWriterLock_UpgradeableReaderLocked_PermitsUpgradeToWriterLock_Sync()
        {
            var rwl = new AsyncReaderWriterLock();

            var readerKey = rwl.UpgradeableReaderLock();
            var writerKey = rwl.UpgradeToWriterLock(readerKey);
            writerKey.Dispose();
            readerKey.Dispose();
        }

        [Test]
        public void AsyncReaderWriterLock_UpgradeableReaderLocked_PermitsTryUpgradeToWriterLock_Sync()
        {
            var rwl = new AsyncReaderWriterLock();

            var readerKey = rwl.UpgradeableReaderLock();
            bool isUpgraded = rwl.TryUpgradeToWriterLock(readerKey, out var writerKey);
            Assert.True(isUpgraded);
            writerKey.Dispose();
            readerKey.Dispose();
        }

        [Test]
        public void AsyncReaderWriterLock_UpgradeableReaderLocked_PermitsReaderLock_Async()
        {
            var rwl = new AsyncReaderWriterLock();

            Promise.Run(async () =>
            {
                using (await rwl.UpgradeableReaderLockAsync())
                {
                    using (await rwl.ReaderLockAsync()) { }
                }
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeout(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AsyncReaderWriterLock_UpgradeableReaderLocked_PermitsReaderLock_Sync()
        {
            var rwl = new AsyncReaderWriterLock();

            var upgradeableReaderKey = rwl.UpgradeableReaderLock();
            var readerKey = rwl.ReaderLock();
            readerKey.Dispose();
            upgradeableReaderKey.Dispose();
        }

        [Test]
        public void AsyncReaderWriterLock_UpgradeableReaderLocked_PermitsTryReaderLock_Sync()
        {
            var rwl = new AsyncReaderWriterLock();

            var upgradeableReaderKey = rwl.UpgradeableReaderLock();
            bool isUpgraded = rwl.TryEnterReaderLock(out var readerKey);
            Assert.True(isUpgraded);
            readerKey.Dispose();
            upgradeableReaderKey.Dispose();
        }

        [Test]
        public void AsyncReaderWriterLock_WriterKey_DoubleDisposeThrows()
        {
            var rwl = new AsyncReaderWriterLock();

            bool didThrow = false;
            rwl.WriterLockAsync()
                .Then(key =>
                {
                    key.Dispose();
                    key.Dispose();
                })
                .Catch((System.InvalidOperationException e) => didThrow = true)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
            Assert.True(didThrow);
        }

        [Test]
        public void AsyncReaderWriterLock_ReaderKey_DoubleDisposeThrows()
        {
            var rwl = new AsyncReaderWriterLock();

            bool didThrow = false;
            rwl.ReaderLockAsync()
                .Then(key =>
                {
                    key.Dispose();
                    key.Dispose();
                })
                // In DEBUG mode, InvalidOperationException is thrown. In RELEASE mode, it only checks for underflow.
                .Catch((System.InvalidOperationException e) => didThrow = true)
                .Catch((OverflowException e) => didThrow = true)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
            Assert.True(didThrow);
        }

        [Test]
        public void AsyncReaderWriterLock_UpgradeableReaderKey_DoubleDisposeThrows()
        {
            var rwl = new AsyncReaderWriterLock();

            bool didThrow = false;
            rwl.ReaderLockAsync()
                .Then(key =>
                {
                    key.Dispose();
                    key.Dispose();
                })
                // In DEBUG mode, InvalidOperationException is thrown. In RELEASE mode, it only checks for underflow.
                .Catch((System.InvalidOperationException e) => didThrow = true)
                .Catch((OverflowException e) => didThrow = true)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
            Assert.True(didThrow);
        }

        [Test]
        public void AsyncReaderWriterLock_UpgradedWriterKey_DoubleDisposeThrows()
        {
            var rwl = new AsyncReaderWriterLock();

            bool didThrow = false;
            rwl.UpgradeableReaderLockAsync()
                .Then(readerKey =>
                    rwl.UpgradeToWriterLockAsync(readerKey)
                        .Then(writerKey =>
                        {
                            writerKey.Dispose();
                            writerKey.Dispose();
                        })
                        .Catch((System.InvalidOperationException e) => didThrow = true)
                        .Finally(() => readerKey.Dispose())
                )
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
            Assert.True(didThrow);
        }

        [Test]
        public void AsyncReaderWriterLock_PreCanceledWriterLock_Unlocked_SynchronouslyCanceled()
        {
            var rwl = new AsyncReaderWriterLock();
            var token = CancelationToken.Canceled();
            bool canceled = false;

            rwl.WriterLockAsync(token)
                .CatchCancelation(() => canceled = true)
                .Forget();

            Assert.True(canceled);
            Assert.Catch<OperationCanceledException>(() => rwl.WriterLock(token));
        }

        [Test]
        public void AsyncReaderWriterLock_PreCanceledReaderLock_Unlocked_SynchronouslyCanceled()
        {
            var rwl = new AsyncReaderWriterLock();
            var token = CancelationToken.Canceled();
            bool canceled = false;

            rwl.ReaderLockAsync(token)
                .CatchCancelation(() => canceled = true)
                .Forget();

            Assert.True(canceled);
            Assert.Catch<OperationCanceledException>(() => rwl.ReaderLock(token));
        }

        [Test]
        public void AsyncReaderWriterLock_PreCanceledUpgradeableReaderLock_Unlocked_SynchronouslyCanceled()
        {
            var rwl = new AsyncReaderWriterLock();
            var token = CancelationToken.Canceled();
            bool canceled = false;

            rwl.UpgradeableReaderLockAsync(token)
                .CatchCancelation(() => canceled = true)
                .Forget();

            Assert.True(canceled);
            Assert.Catch<OperationCanceledException>(() => rwl.UpgradeableReaderLock(token));
        }

        [Test]
        public void AsyncReaderWriterLock_PreCanceledUpgradeWriterLock_Unlocked_SynchronouslyCanceled()
        {
            var rwl = new AsyncReaderWriterLock();
            var token = CancelationToken.Canceled();
            bool canceled = false;

            Promise.Run(async () =>
            {
                using (var key = await rwl.UpgradeableReaderLockAsync())
                {
                    using (await rwl.UpgradeToWriterLockAsync(key, token)) { }
                }
            }, SynchronizationOption.Synchronous)
                .CatchCancelation(() => canceled = true)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));

            Assert.True(canceled);
            Assert.Catch<OperationCanceledException>(() =>
            {
                using (var key = rwl.UpgradeableReaderLock())
                {
                    using (rwl.UpgradeToWriterLock(key, token)) { }
                }
            });
        }

        [Test]
        public void AsyncReaderWriterLock_PreCanceledWriterLock_Locked_SynchronouslyCancels()
        {
            var rwl = new AsyncReaderWriterLock();

            var lockPromise = rwl.WriterLockAsync();
            var token = CancelationToken.Canceled();

            var promise = rwl.WriterLockAsync(token);

            Promise.State state = Promise.State.Pending;
            promise
                .ContinueWith(r => state = r.State)
                .Forget();
            Assert.AreEqual(Promise.State.Canceled, state);

            lockPromise.WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1)).Dispose();
        }

        [Test]
        public void AsyncReaderWriterLock_PreCanceledReaderLock_Locked_SynchronouslyCancels()
        {
            var rwl = new AsyncReaderWriterLock();

            var lockPromise = rwl.WriterLockAsync();
            var token = CancelationToken.Canceled();

            var promise = rwl.ReaderLockAsync(token);

            Promise.State state = Promise.State.Pending;
            promise
                .ContinueWith(r => state = r.State)
                .Forget();
            Assert.AreEqual(Promise.State.Canceled, state);

            lockPromise.WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1)).Dispose();
        }

        [Test]
        public void AsyncReaderWriterLock_PreCanceledUpgradeableReaderLock_Locked_SynchronouslyCancels()
        {
            var rwl = new AsyncReaderWriterLock();

            var lockPromise = rwl.WriterLockAsync();
            var token = CancelationToken.Canceled();

            var promise = rwl.UpgradeableReaderLockAsync(token);

            Promise.State state = Promise.State.Pending;
            promise
                .ContinueWith(r => state = r.State)
                .Forget();
            Assert.AreEqual(Promise.State.Canceled, state);

            lockPromise.WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1)).Dispose();
        }

        [Test]
        public void AsyncReaderWriterLock_PreCanceledUpgradeWriterLock_ReaderLocked_SynchronouslyCancels()
        {
            var rwl = new AsyncReaderWriterLock();

            var lockPromise = rwl.ReaderLockAsync();
            var token = CancelationToken.Canceled();

            Promise.State state = Promise.State.Pending;
            Promise.Run(async () =>
            {
                using (var key = await rwl.UpgradeableReaderLockAsync())
                {
                    using (await rwl.UpgradeToWriterLockAsync(key, token)) { }
                }
            }, SynchronizationOption.Synchronous)
                .ContinueWith(r => state = r.State)
                .Forget();

            Assert.AreEqual(Promise.State.Canceled, state);

            lockPromise.WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1)).Dispose();
        }

        [Test]
        public void AsyncReaderWriterLock_WriteLocked_PreventsAnotherWriterLockUntilReleased_Async()
        {
            var rwl = new AsyncReaderWriterLock();
            var deferred1Continue = Promise.NewDeferred();

            bool isLocked = false;

            Promise.Run(async () =>
            {
                using (await rwl.WriterLockAsync())
                {
                    isLocked = true;
                    await deferred1Continue.Promise;
                }
            }, SynchronizationOption.Synchronous)
                .Forget();

            TestHelper.ExecuteForegroundCallbacks();
            Assert.True(isLocked);

            bool promise2IsComplete = false;

            Promise.Run(async () =>
            {
                using (await rwl.WriterLockAsync()) { }
                promise2IsComplete = true;
            }, SynchronizationOption.Synchronous)
                .Forget();

            TestHelper.ExecuteForegroundCallbacks();
            Assert.False(promise2IsComplete);

            deferred1Continue.Resolve();
            // Continuations are posted asynchronously to the current context, so we need to make sure it is executed.
            TestHelper.ExecuteForegroundCallbacks();
            Assert.True(promise2IsComplete);
        }

        [Test]
        public void AsyncReaderWriterLock_WriteLocked_PreventsReaderLockUntilReleased_Async()
        {
            var rwl = new AsyncReaderWriterLock();
            var deferred1Continue = Promise.NewDeferred();

            bool isLocked = false;

            Promise.Run(async () =>
            {
                using (await rwl.WriterLockAsync())
                {
                    isLocked = true;
                    await deferred1Continue.Promise;
                }
            }, SynchronizationOption.Synchronous)
                .Forget();

            TestHelper.ExecuteForegroundCallbacks();
            Assert.True(isLocked);

            bool promise2IsComplete = false;

            Promise.Run(async () =>
            {
                using (await rwl.ReaderLockAsync()) { }
                promise2IsComplete = true;
            }, SynchronizationOption.Synchronous)
                .Forget();

            TestHelper.ExecuteForegroundCallbacks();
            Assert.False(promise2IsComplete);

            deferred1Continue.Resolve();
            // Continuations are posted asynchronously to the current context, so we need to make sure it is executed.
            TestHelper.ExecuteForegroundCallbacks();
            Assert.True(promise2IsComplete);
        }

        [Test]
        public void AsyncReaderWriterLock_WriteLocked_PreventsUpgradeableReaderLockUntilReleased_Async()
        {
            var rwl = new AsyncReaderWriterLock();
            var deferred1Continue = Promise.NewDeferred();

            bool isLocked = false;

            Promise.Run(async () =>
            {
                using (await rwl.WriterLockAsync())
                {
                    isLocked = true;
                    await deferred1Continue.Promise;
                }
            }, SynchronizationOption.Synchronous)
                .Forget();

            TestHelper.ExecuteForegroundCallbacks();
            Assert.True(isLocked);

            bool promise2IsComplete = false;

            Promise.Run(async () =>
            {
                using (await rwl.UpgradeableReaderLockAsync()) { }
                promise2IsComplete = true;
            }, SynchronizationOption.Synchronous)
                .Forget();

            TestHelper.ExecuteForegroundCallbacks();
            Assert.False(promise2IsComplete);

            deferred1Continue.Resolve();
            // Continuations are posted asynchronously to the current context, so we need to make sure it is executed.
            TestHelper.ExecuteForegroundCallbacks();
            Assert.True(promise2IsComplete);
        }

        [Test]
        public void AsyncReaderWriterLock_ReadLocked_PreventsWriterLockUntilReleased_Async()
        {
            var rwl = new AsyncReaderWriterLock();
            var deferred1Continue = Promise.NewDeferred();

            bool isReaderLocked = false;

            Promise.Run(async () =>
            {
                using (await rwl.ReaderLockAsync())
                {
                    isReaderLocked = true;
                    await deferred1Continue.Promise;
                }
            }, SynchronizationOption.Synchronous)
                .Forget();

            TestHelper.ExecuteForegroundCallbacks();
            Assert.True(isReaderLocked);

            bool writerIsComplete = false;

            Promise.Run(async () =>
            {
                using (await rwl.WriterLockAsync()) { }
                writerIsComplete = true;
            }, SynchronizationOption.Synchronous)
                .Forget();

            TestHelper.ExecuteForegroundCallbacks();
            Assert.False(writerIsComplete);

            deferred1Continue.Resolve();
            // Continuations are posted asynchronously to the current context, so we need to make sure it is executed.
            TestHelper.ExecuteForegroundCallbacks();
            Assert.True(writerIsComplete);
        }

        [Test]
        public void AsyncReaderWriterLock_ReadLocked_EnterWriterLock_PreventsAnotherReaderLockUntilReleased_Async()
        {
            var rwl = new AsyncReaderWriterLock();
            var deferred1Continue = Promise.NewDeferred();

            bool isReaderLocked = false;

            Promise.Run(async () =>
            {
                using (await rwl.ReaderLockAsync())
                {
                    isReaderLocked = true;
                    await deferred1Continue.Promise;
                }
            }, SynchronizationOption.Synchronous)
                .Forget();

            bool isWriterLocked = false;

            Promise.Run(async () =>
            {
                using (await rwl.WriterLockAsync())
                {
                    isWriterLocked = true;
                }
            }, SynchronizationOption.Synchronous)
                .Forget();

            TestHelper.ExecuteForegroundCallbacks();
            Assert.True(isReaderLocked);
            Assert.False(isWriterLocked);

            bool secondReaderIsComplete = false;

            Promise.Run(async () =>
            {
                using (await rwl.ReaderLockAsync()) { }
                secondReaderIsComplete = true;
            }, SynchronizationOption.Synchronous)
                .Forget();

            TestHelper.ExecuteForegroundCallbacks();
            Assert.False(secondReaderIsComplete);

            deferred1Continue.Resolve();
            // Continuations are posted asynchronously to the current context, so we need to make sure it is executed.
            TestHelper.ExecuteForegroundCallbacks();
            Assert.True(isWriterLocked);
            Assert.True(secondReaderIsComplete);
        }

        [Test]
        public void AsyncReaderWriterLock_ReadLocked_EnterWriterLock_PreventsUpgradeableReaderLockUntilReleased_Async()
        {
            var rwl = new AsyncReaderWriterLock();
            var deferred1Continue = Promise.NewDeferred();

            bool isReaderLocked = false;

            Promise.Run(async () =>
            {
                using (await rwl.ReaderLockAsync())
                {
                    isReaderLocked = true;
                    await deferred1Continue.Promise;
                }
            }, SynchronizationOption.Synchronous)
                .Forget();

            bool isWriterLocked = false;

            Promise.Run(async () =>
            {
                using (await rwl.WriterLockAsync())
                {
                    isWriterLocked = true;
                }
            }, SynchronizationOption.Synchronous)
                .Forget();

            TestHelper.ExecuteForegroundCallbacks();
            Assert.True(isReaderLocked);
            Assert.False(isWriterLocked);

            bool secondReaderIsComplete = false;

            Promise.Run(async () =>
            {
                using (await rwl.UpgradeableReaderLockAsync()) { }
                secondReaderIsComplete = true;
            }, SynchronizationOption.Synchronous)
                .Forget();

            TestHelper.ExecuteForegroundCallbacks();
            Assert.False(secondReaderIsComplete);

            deferred1Continue.Resolve();
            // Continuations are posted asynchronously to the current context, so we need to make sure it is executed.
            TestHelper.ExecuteForegroundCallbacks();
            Assert.True(isWriterLocked);
            Assert.True(secondReaderIsComplete);
        }

        [Test]
        public void AsyncReaderWriterLock_ReadLocked_EnterUpgradeableReaderLock_PreventsWriterLockUntilReleased_Async()
        {
            var rwl = new AsyncReaderWriterLock();
            var deferred1Continue = Promise.NewDeferred();

            bool isReaderLocked = false;

            Promise.Run(async () =>
            {
                using (await rwl.ReaderLockAsync())
                {
                    isReaderLocked = true;
                    await deferred1Continue.Promise;
                }
            }, SynchronizationOption.Synchronous)
                .Forget();

            bool isUpgradeableReaderLocked = false;

            Promise.Run(async () =>
            {
                using (await rwl.UpgradeableReaderLockAsync())
                {
                    isUpgradeableReaderLocked = true;
                }
            }, SynchronizationOption.Synchronous)
                .Forget();

            TestHelper.ExecuteForegroundCallbacks();
            Assert.True(isReaderLocked);
            Assert.True(isUpgradeableReaderLocked);

            bool writerIsComplete = false;

            Promise.Run(async () =>
            {
                using (await rwl.WriterLockAsync()) { }
                writerIsComplete = true;
            }, SynchronizationOption.Synchronous)
                .Forget();

            TestHelper.ExecuteForegroundCallbacks();
            Assert.False(writerIsComplete);

            deferred1Continue.Resolve();
            // Continuations are posted asynchronously to the current context, so we need to make sure it is executed.
            TestHelper.ExecuteForegroundCallbacks();
            Assert.True(writerIsComplete);
        }

        [Test]
        public void AsyncReaderWriterLock_UpgradeableReadLocked_PreventsWriterLockUntilReleased_Async()
        {
            var rwl = new AsyncReaderWriterLock();
            var deferred1Continue = Promise.NewDeferred();

            bool isUpgradeableReaderLocked = false;

            Promise.Run(async () =>
            {
                using (await rwl.UpgradeableReaderLockAsync())
                {
                    isUpgradeableReaderLocked = true;
                    await deferred1Continue.Promise;
                }
            }, SynchronizationOption.Synchronous)
                .Forget();

            TestHelper.ExecuteForegroundCallbacks();
            Assert.True(isUpgradeableReaderLocked);

            bool writerIsComplete = false;

            Promise.Run(async () =>
            {
                var promise = rwl.WriterLockAsync();
                using (await promise) { }
                writerIsComplete = true;
            }, SynchronizationOption.Synchronous)
                .Forget();

            TestHelper.ExecuteForegroundCallbacks();
            Assert.False(writerIsComplete);

            deferred1Continue.Resolve();
            // Continuations are posted asynchronously to the current context, so we need to make sure it is executed.
            TestHelper.ExecuteForegroundCallbacks();
            Assert.True(writerIsComplete);
        }

        [Test]
        public void AsyncReaderWriterLock_UpgradeableReadLocked_PreventsAnotherUpgradeableReaderLockUntilReleased_NoUpgrade_Async()
        {
            var rwl = new AsyncReaderWriterLock();
            var deferred1Continue = Promise.NewDeferred();

            bool isUpgradeableReaderLocked = false;

            Promise.Run(async () =>
            {
                using (await rwl.UpgradeableReaderLockAsync())
                {
                    isUpgradeableReaderLocked = true;
                    await deferred1Continue.Promise;
                }
            }, SynchronizationOption.Synchronous)
                .Forget();

            TestHelper.ExecuteForegroundCallbacks();
            Assert.True(isUpgradeableReaderLocked);

            bool secondUpgradeableReaderIsComplete = false;

            Promise.Run(async () =>
            {
                var promise = rwl.UpgradeableReaderLockAsync();
                using (await promise) { }
                secondUpgradeableReaderIsComplete = true;
            }, SynchronizationOption.Synchronous)
                .Forget();

            TestHelper.ExecuteForegroundCallbacks();
            Assert.False(secondUpgradeableReaderIsComplete);

            deferred1Continue.Resolve();
            // Continuations are posted asynchronously to the current context, so we need to make sure it is executed.
            TestHelper.ExecuteForegroundCallbacks();
            Assert.True(secondUpgradeableReaderIsComplete);
        }

        [Test]
        public void AsyncReaderWriterLock_UpgradeableReadLocked_PreventsAnotherUpgradeableReaderLockUntilReleased_AfterDowngrade_Async()
        {
            var rwl = new AsyncReaderWriterLock();
            var deferred1Continue = Promise.NewDeferred();

            bool isUpgradeableReaderDowngradedLocked = false;

            Promise.Run(async () =>
            {
                using (var key = await rwl.UpgradeableReaderLockAsync())
                {
                    using (await rwl.UpgradeToWriterLockAsync(key)) { }
                    isUpgradeableReaderDowngradedLocked = true;
                    await deferred1Continue.Promise;
                }
            }, SynchronizationOption.Synchronous)
                .Forget();

            TestHelper.ExecuteForegroundCallbacks();
            Assert.True(isUpgradeableReaderDowngradedLocked);

            bool secondUpgradeableReaderIsComplete = false;

            Promise.Run(async () =>
            {
                var promise = rwl.UpgradeableReaderLockAsync();
                using (await promise) { }
                secondUpgradeableReaderIsComplete = true;
            }, SynchronizationOption.Synchronous)
                .Forget();

            TestHelper.ExecuteForegroundCallbacks();
            Assert.False(secondUpgradeableReaderIsComplete);

            deferred1Continue.Resolve();
            // Continuations are posted asynchronously to the current context, so we need to make sure it is executed.
            TestHelper.ExecuteForegroundCallbacks();
            Assert.True(secondUpgradeableReaderIsComplete);
        }

        [Test]
        public void AsyncReaderWriterLock_UpgradedWriterLocked_PreventsAnotherUpgradeableReaderLockUntilUpgradeableReaderLockReleased_Async()
        {
            var rwl = new AsyncReaderWriterLock();
            var deferred1Continue = Promise.NewDeferred();

            bool isUpgradedWriterLocked = false;

            Promise.Run(async () =>
            {
                using (var key = await rwl.UpgradeableReaderLockAsync())
                {
                    isUpgradedWriterLocked = true;
                    using (await rwl.UpgradeToWriterLockAsync(key))
                    {
                        await deferred1Continue.Promise;
                    }
                }
            }, SynchronizationOption.Synchronous)
                .Forget();

            TestHelper.ExecuteForegroundCallbacks();
            Assert.True(isUpgradedWriterLocked);

            bool secondUpgradeableReaderIsComplete = false;

            Promise.Run(async () =>
            {
                var promise = rwl.UpgradeableReaderLockAsync();
                using (await promise) { }
                secondUpgradeableReaderIsComplete = true;
            }, SynchronizationOption.Synchronous)
                .Forget();

            TestHelper.ExecuteForegroundCallbacks();
            Assert.False(secondUpgradeableReaderIsComplete);

            deferred1Continue.Resolve();
            // Continuations are posted asynchronously to the current context, so we need to make sure it is executed.
            TestHelper.ExecuteForegroundCallbacks();
            Assert.True(secondUpgradeableReaderIsComplete);
        }

        [Test]
        public void AsyncReaderWriterLock_UpgradeableReaderLocksTakeLockInOrder_AfterNormalLockWithPendingReaderLock_Async()
        {
            var rwl = new AsyncReaderWriterLock();
            var deferred = Promise.NewDeferred();
            var promise = deferred.Promise.Preserve();

            Promise.Run(async () =>
            {
                using (await rwl.WriterLockAsync())
                {
                    await promise;
                }
            }, SynchronizationOption.Synchronous)
                .Forget();

            bool enteredReadLock = false;

            Promise.Run(async () =>
            {
                using (await rwl.ReaderLockAsync())
                {
                    enteredReadLock = true;
                    await promise;
                }
            }, SynchronizationOption.Synchronous)
                .Forget();

            bool enteredFirstUpgradeableReadLock = false;
            bool exitedFirstUpgradeableReadLock = false;

            Promise.Run(async () =>
            {
                using (await rwl.UpgradeableReaderLockAsync())
                {
                    enteredFirstUpgradeableReadLock = true;
                    await promise;
                    exitedFirstUpgradeableReadLock = true;
                }
            }, SynchronizationOption.Synchronous)
                .Forget();

            Assert.False(enteredReadLock);
            Assert.False(enteredFirstUpgradeableReadLock);

            var d = deferred;
            deferred = Promise.NewDeferred();
            promise.Forget();
            promise = deferred.Promise.Preserve();
            d.Resolve();
            // Continuations are posted asynchronously to the current context, so we need to make sure it is executed.
            TestHelper.ExecuteForegroundCallbacks();

            Assert.True(enteredReadLock);
            Assert.True(enteredFirstUpgradeableReadLock);
            Assert.False(exitedFirstUpgradeableReadLock);

            bool enteredSecondUpgradeableReadLock = false;

            Promise.Run(async () =>
            {
                using (await rwl.UpgradeableReaderLockAsync())
                {
                    enteredSecondUpgradeableReadLock = true;
                }
            }, SynchronizationOption.Synchronous)
                .Forget();

            Assert.False(exitedFirstUpgradeableReadLock);
            Assert.False(enteredSecondUpgradeableReadLock);

            deferred.Resolve();
            promise.Forget();
            // Continuations are posted asynchronously to the current context, so we need to make sure it is executed.
            TestHelper.ExecuteForegroundCallbacks();
        }

        [Test]
        public void AsyncReaderWriterLock_UpgradedWriterLocked_PreventsReaderLockUntilReleased_Async()
        {
            var rwl = new AsyncReaderWriterLock();
            var deferred1Continue = Promise.NewDeferred();

            bool isUpgradedWriterLocked = false;

            Promise.Run(async () =>
            {
                using (var key = await rwl.UpgradeableReaderLockAsync())
                {
                    using (await rwl.UpgradeToWriterLockAsync(key))
                    {
                        isUpgradedWriterLocked = true;
                        await deferred1Continue.Promise;
                    }
                }
            }, SynchronizationOption.Synchronous)
                .Forget();

            TestHelper.ExecuteForegroundCallbacks();
            Assert.True(isUpgradedWriterLocked);

            bool readerIsComplete = false;

            Promise.Run(async () =>
            {
                using (await rwl.ReaderLockAsync()) { }
                readerIsComplete = true;
            }, SynchronizationOption.Synchronous)
                .Forget();

            TestHelper.ExecuteForegroundCallbacks();
            Assert.False(readerIsComplete);

            deferred1Continue.Resolve();
            // Continuations are posted asynchronously to the current context, so we need to make sure it is executed.
            TestHelper.ExecuteForegroundCallbacks();
            Assert.True(readerIsComplete);
        }

        [Test]
        public void AsyncReaderWriterLock_ReadLocked_UpgradeToWriterLock_PreventsAnotherReaderLockUntilReleased_Async()
        {
            var rwl = new AsyncReaderWriterLock();
            var deferred1Continue = Promise.NewDeferred();

            bool isReaderLocked = false;

            Promise.Run(async () =>
            {
                using (await rwl.ReaderLockAsync())
                {
                    isReaderLocked = true;
                    await deferred1Continue.Promise;
                }
            }, SynchronizationOption.Synchronous)
                .Forget();

            bool isUpgradedWriterLocked = false;

            Promise.Run(async () =>
            {
                using (var key = await rwl.UpgradeableReaderLockAsync())
                {
                    using (await rwl.UpgradeToWriterLockAsync(key))
                    {
                        isUpgradedWriterLocked = true;
                    }
                }
            }, SynchronizationOption.Synchronous)
                .Forget();

            TestHelper.ExecuteForegroundCallbacks();
            Assert.True(isReaderLocked);
            Assert.False(isUpgradedWriterLocked);

            bool secondReaderIsComplete = false;

            Promise.Run(async () =>
            {
                using (await rwl.ReaderLockAsync()) { }
                secondReaderIsComplete = true;
            }, SynchronizationOption.Synchronous)
                .Forget();

            TestHelper.ExecuteForegroundCallbacks();
            Assert.False(secondReaderIsComplete);

            deferred1Continue.Resolve();
            // Continuations are posted asynchronously to the current context, so we need to make sure it is executed.
            TestHelper.ExecuteForegroundCallbacks();
            Assert.True(isUpgradedWriterLocked);
            Assert.True(secondReaderIsComplete);
        }

        [Test]
        public void AsyncReaderWriterLock_CanceledWriterLock_CancelsPromise()
        {
            var rwl = new AsyncReaderWriterLock();
            var cts = CancelationSource.New();
            bool canceled = false;

            rwl.WriterLockAsync()
                .Then(key =>
                {
                    var canceledLockPromise = rwl.WriterLockAsync(cts.Token);
                    cts.Cancel();

                    key.Dispose();
                    return canceledLockPromise
                        .CatchCancelation(() => canceled = true);
                })
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));

            cts.Dispose();
            Assert.True(canceled);
        }

        [Test]
        public void AsyncReaderWriterLock_CanceledReaderLock_CancelsPromise()
        {
            var rwl = new AsyncReaderWriterLock();
            var cts = CancelationSource.New();
            bool canceled = false;

            rwl.WriterLockAsync()
                .Then(key =>
                {
                    var canceledLockPromise = rwl.ReaderLockAsync(cts.Token);
                    cts.Cancel();

                    key.Dispose();
                    return canceledLockPromise
                        .CatchCancelation(() => canceled = true);
                })
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));

            cts.Dispose();
            Assert.True(canceled);
        }

        [Test]
        public void AsyncReaderWriterLock_CanceledUpgradeableReaderLock_CancelsPromise()
        {
            var rwl = new AsyncReaderWriterLock();
            var cts = CancelationSource.New();
            bool canceled = false;

            rwl.WriterLockAsync()
                .Then(key =>
                {
                    var canceledLockPromise = rwl.UpgradeableReaderLockAsync(cts.Token);
                    cts.Cancel();

                    key.Dispose();
                    return canceledLockPromise
                        .CatchCancelation(() => canceled = true);
                })
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));

            cts.Dispose();
            Assert.True(canceled);
        }

        [Test]
        public void AsyncReaderWriterLock_CanceledUpgradedWriterLock_CancelsPromise()
        {
            var rwl = new AsyncReaderWriterLock();
            var cts = CancelationSource.New();
            bool canceled = false;

            Promise.Run(async () =>
            {
                Promise canceledPromise;
                using (var upgradeableReaderKey = await rwl.UpgradeableReaderLockAsync())
                {
                    using (await rwl.ReaderLockAsync())
                    {
                        canceledPromise = rwl.UpgradeToWriterLockAsync(upgradeableReaderKey, cts.Token)
                            .CatchCancelation(() => canceled = true);
                        cts.Cancel();
                    }
                }
                await canceledPromise;
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));

            cts.Dispose();
            Assert.True(canceled);
        }

        [Test]
        public void AsyncReaderWriterLock_CanceledTooLate_StillTakesWriterLock_Async()
        {
            var rwl = new AsyncReaderWriterLock();
            var cts = CancelationSource.New();

            Promise<AsyncReaderWriterLock.WriterKey> cancelableLockPromise;
            Promise.Run(async () =>
            {
                using (await rwl.WriterLockAsync())
                {
                    cancelableLockPromise = rwl.WriterLockAsync(cts.Token);
                }

                cts.Cancel();

                Promise.State state = Promise.State.Pending;
                var nextLocker = rwl.WriterLockAsync()
                    .ContinueWith(r =>
                    {
                        state = r.State;
                        r.Result.Dispose();
                    });
                Assert.AreEqual(Promise.State.Pending, state);

                var key = await cancelableLockPromise;
                key.Dispose();
                await nextLocker;
                Assert.AreEqual(Promise.State.Resolved, state);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));

            cts.Dispose();
        }

        [Test]
        public void AsyncReaderWriterLock_CanceledTooLate_StillTakesReaderLock_Async()
        {
            var rwl = new AsyncReaderWriterLock();
            var cts = CancelationSource.New();

            Promise<AsyncReaderWriterLock.ReaderKey> cancelableLockPromise;
            Promise.Run(async () =>
            {
                using (await rwl.WriterLockAsync())
                {
                    cancelableLockPromise = rwl.ReaderLockAsync(cts.Token);
                }

                cts.Cancel();

                Promise.State state = Promise.State.Pending;
                var nextLocker = rwl.WriterLockAsync()
                    .ContinueWith(r =>
                    {
                        state = r.State;
                        r.Result.Dispose();
                    });
                Assert.AreEqual(Promise.State.Pending, state);

                var key = await cancelableLockPromise;
                key.Dispose();
                await nextLocker;
                Assert.AreEqual(Promise.State.Resolved, state);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));

            cts.Dispose();
        }

        [Test]
        public void AsyncReaderWriterLock_CanceledTooLate_StillTakesUpgradeableReaderLock_Async()
        {
            var rwl = new AsyncReaderWriterLock();
            var cts = CancelationSource.New();

            Promise<AsyncReaderWriterLock.UpgradeableReaderKey> cancelableLockPromise;
            Promise.Run(async () =>
            {
                using (await rwl.WriterLockAsync())
                {
                    cancelableLockPromise = rwl.UpgradeableReaderLockAsync(cts.Token);
                }

                cts.Cancel();

                Promise.State state = Promise.State.Pending;
                var nextLocker = rwl.WriterLockAsync()
                    .ContinueWith(r =>
                    {
                        state = r.State;
                        r.Result.Dispose();
                    });
                Assert.AreEqual(Promise.State.Pending, state);

                var key = await cancelableLockPromise;
                key.Dispose();
                await nextLocker;
                Assert.AreEqual(Promise.State.Resolved, state);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));

            cts.Dispose();
        }

        [Test]
        public void AsyncReaderWriterLock_CanceledTooLate_StillTakesUpgradedWriterLock_Async()
        {
            var rwl = new AsyncReaderWriterLock();
            var cts = CancelationSource.New();
            var deferred = Promise.NewDeferred();

            Promise.Run(async () =>
            {
                using (await rwl.ReaderLockAsync())
                {
                    using (var upgradeableReaderKey = await rwl.UpgradeableReaderLockAsync())
                    {
                        var cancelableLockPromise = rwl.UpgradeToWriterLockAsync(upgradeableReaderKey, cts.Token);
                        await deferred.Promise;
                        using (await cancelableLockPromise) { }
                    }
                }
            }, SynchronizationOption.Synchronous)
                .Forget();

            Promise.Run(async () =>
            {
                cts.Cancel();

                Promise.State state = Promise.State.Pending;
                var nextLocker = rwl.WriterLockAsync()
                    .ContinueWith(r =>
                    {
                        state = r.State;
                        r.Result.Dispose();
                    });
                Assert.AreEqual(Promise.State.Pending, state);

                deferred.Resolve();
                await nextLocker;
                Assert.AreEqual(Promise.State.Resolved, state);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));

            cts.Dispose();
        }

        [Test]
        public void AsyncReaderWriterLock_SupportsMultipleAsynchronousLocks()
        {
            // This test will fail if continuations are executed synchronously (promise1 will run in the loop forever, blocking promise2 from completing).

            var AsyncReaderWriterLock = new AsyncReaderWriterLock();
            var cancelationSource = CancelationSource.New();
            var cancelationToken = cancelationSource.Token;

            var promise1 = Promise.Run(async () =>
            {
                while (!cancelationToken.IsCancelationRequested)
                {
                    using (await AsyncReaderWriterLock.WriterLockAsync())
                    {
                        Thread.Sleep(10);
                    }
                }
            }, forceAsync: true);

            var promise2 = Promise.Run(() =>
            {
                using (AsyncReaderWriterLock.WriterLock())
                {
                    Thread.Sleep(1000);
                }
            }, forceAsync: true);

            promise2.WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(10));
            cancelationSource.Cancel();
            promise1.WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
            cancelationSource.Dispose();
        }

        [Test]
        public void AsyncReaderWriterLock_ReleaseWriterLock_ResolvesReaderLockAndUpgradeableReaderLock_Async()
        {
            var rwl = new AsyncReaderWriterLock();
            var cts = CancelationSource.New();
            var writerDeferred = Promise.NewDeferred();
            var readersDeferred = Promise.NewDeferred();
            var preservedPromise = readersDeferred.Promise.Preserve();

            Promise.Run(async () =>
            {
                using (await rwl.WriterLockAsync())
                {
                    await writerDeferred.Promise;
                }
            }, SynchronizationOption.Synchronous)
                .Forget();

            bool isUpgradeableReaderLocked = false;
            bool isReaderLocked = false;

            Promise.Run(async () =>
            {
                using (await rwl.UpgradeableReaderLockAsync())
                {
                    isUpgradeableReaderLocked = true;
                    await preservedPromise;
                }
            }, SynchronizationOption.Synchronous)
                .Forget();

            Promise.Run(async () =>
            {
                using (await rwl.ReaderLockAsync())
                {
                    isReaderLocked = true;
                    await preservedPromise;
                }
            }, SynchronizationOption.Synchronous)
                .Forget();

            TestHelper.ExecuteForegroundCallbacks();
            Assert.False(isUpgradeableReaderLocked);
            Assert.False(isReaderLocked);

            writerDeferred.Resolve();
            // Continuations are posted asynchronously to the current context, so we need to make sure it is executed.
            TestHelper.ExecuteForegroundCallbacks();
            Assert.True(isUpgradeableReaderLocked);
            Assert.True(isReaderLocked);

            readersDeferred.Resolve();
            TestHelper.ExecuteForegroundCallbacks();
            preservedPromise.Forget();
            cts.Dispose();
        }

        [Test]
        public void AsyncReaderWriterLock_ReleaseUpgradedWriterLock_ResolvesReaderLockOnly_Async()
        {
            var rwl = new AsyncReaderWriterLock();
            var cts = CancelationSource.New();
            var writerDeferred = Promise.NewDeferred();
            var readersDeferred = Promise.NewDeferred();
            var preservedPromise = readersDeferred.Promise.Preserve();

            Promise.Run(async () =>
            {
                using (var upgradeableReaderKey = await rwl.UpgradeableReaderLockAsync())
                {
                    using (await rwl.UpgradeToWriterLockAsync(upgradeableReaderKey))
                    {
                        await writerDeferred.Promise;
                    }
                    await preservedPromise;
                }
            }, SynchronizationOption.Synchronous)
                .Forget();

            bool isUpgradeableReaderLocked = false;
            bool isReaderLocked = false;

            Promise.Run(async () =>
            {
                using (await rwl.UpgradeableReaderLockAsync())
                {
                    isUpgradeableReaderLocked = true;
                    await preservedPromise;
                }
            }, SynchronizationOption.Synchronous)
                .Forget();

            Promise.Run(async () =>
            {
                using (await rwl.ReaderLockAsync())
                {
                    isReaderLocked = true;
                    await preservedPromise;
                }
            }, SynchronizationOption.Synchronous)
                .Forget();

            TestHelper.ExecuteForegroundCallbacks();
            Assert.False(isUpgradeableReaderLocked);
            Assert.False(isReaderLocked);

            writerDeferred.Resolve();
            // Continuations are posted asynchronously to the current context, so we need to make sure it is executed.
            TestHelper.ExecuteForegroundCallbacks();
            Assert.False(isUpgradeableReaderLocked);
            Assert.True(isReaderLocked);

            readersDeferred.Resolve();
            TestHelper.ExecuteForegroundCallbacks();
            preservedPromise.Forget();
            cts.Dispose();
        }

        [Test]
        public void AsyncReaderWriterLock_ReleaseUpgradeableReaderLock_BeforeReleaseUpgradedWriterLock_Throws_Async()
        {
            var rwl = new AsyncReaderWriterLock();
            bool didThrow = false;

            Promise.Run(async () =>
            {
                AsyncReaderWriterLock.UpgradeableReaderKey upgradeableReaderKey = default;
                AsyncReaderWriterLock.WriterKey upgradedWriterKey = default;
                try
                {
                    using (upgradeableReaderKey = await rwl.UpgradeableReaderLockAsync())
                    {
                        upgradedWriterKey = await rwl.UpgradeToWriterLockAsync(upgradeableReaderKey);
                    }
                }
                catch (System.InvalidOperationException)
                {
                    didThrow = true;
                }
                // After we verify the bad case, dispose them properly.
                upgradedWriterKey.Dispose();
                upgradeableReaderKey.Dispose();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));

            Assert.True(didThrow);
        }

        [Test]
        public void AsyncReaderWriterLock_ReleaseUpgradeableReaderLock_BeforeUpgradedWriterLockPromiseIsComplete_Throws_Async()
        {
            var rwl = new AsyncReaderWriterLock();
            var deferred = Promise.NewDeferred();
            AsyncReaderWriterLock.UpgradeableReaderKey upgradeableReaderKey = default;
            Promise<AsyncReaderWriterLock.WriterKey> upgradedWriterPromise = default;
            bool didThrow = false;

            Promise.Run(async () =>
            {
                using (await rwl.ReaderLockAsync())
                {
                    await deferred.Promise;
                }
            }, SynchronizationOption.Synchronous)
                .Forget();

            Promise.Run(async () =>
            {
                try
                {
                    using (upgradeableReaderKey = await rwl.UpgradeableReaderLockAsync())
                    {
                        upgradedWriterPromise = rwl.UpgradeToWriterLockAsync(upgradeableReaderKey);
                    }
                }
                catch (System.InvalidOperationException)
                {
                    didThrow = true;
                }
                deferred.Resolve();
            }, SynchronizationOption.Synchronous)
                .Forget();

            Promise.Run(async () =>
            {
                // After we verified the bad case, await and dispose them properly.
                using (await upgradedWriterPromise) { }
                upgradeableReaderKey.Dispose();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));

            Assert.True(didThrow);
        }

        [Test]
        public void AsyncReaderWriterLock_ReaderLocksDoNotStarveWriterLocks_Async()
        {
            // A reader-preferring lock will fail this test, causing the readerRunner to loop forever, never allowing the writer lock access.

            var rwl = new AsyncReaderWriterLock();
            int writerCount = 0;
            const int expectedWriterCount = 10;
            var deferred = Promise.NewDeferred();

            var readerRunner = Promise.Run(async () =>
            {
                // We take the reader lock first, then always take another reader lock before releasing the current.
                var readerLockPromise = rwl.ReaderLockAsync();
                Assert.AreEqual(0, writerCount);

                await deferred.Promise; // Wait for the writer runner to start.

                while (writerCount < expectedWriterCount)
                {
                    var temp = readerLockPromise;
                    using (await temp)
                    {
                        readerLockPromise = rwl.ReaderLockAsync();
                    }
                }

                using (await readerLockPromise) { }
            }, SynchronizationOption.Synchronous);

            var writerRunner = Promise.Run(async () =>
            {
                var writerLockPromise = rwl.WriterLockAsync();
                deferred.Resolve();

                using (await writerLockPromise)
                {
                    ++writerCount;
                }

                while (writerCount < expectedWriterCount)
                {
                    using (await rwl.WriterLockAsync())
                    {
                        ++writerCount;
                    }
                }
            }, SynchronizationOption.Synchronous);

            Promise.All(readerRunner, writerRunner)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
            Assert.AreEqual(expectedWriterCount, writerCount);
        }

        [Test]
        public void AsyncReaderWriterLock_WriterLocksDoNotStarveReaderLocks_Async()
        {
            // A writer-preferring lock will fail this test, causing the writerRunner to loop forever, never allowing the reader lock access.

            var rwl = new AsyncReaderWriterLock();
            int readerCount = 0;
            const int expectedReaderCount = 10;
            var deferred = Promise.NewDeferred();

            var writerRunner = Promise.Run(async () =>
            {
                // We take the writer lock first, then always take another writer lock before releasing the current.
                var writerLockPromise = rwl.WriterLockAsync();
                Assert.AreEqual(0, readerCount);

                await deferred.Promise; // Wait for the reader runner to start.

                while (readerCount < expectedReaderCount)
                {
                    var temp = writerLockPromise;
                    using (await temp)
                    {
                        writerLockPromise = rwl.WriterLockAsync();
                    }
                }

                using (await writerLockPromise) { }
            }, SynchronizationOption.Synchronous);

            var readerRunner = Promise.Run(async () =>
            {
                var readerLockPromise = rwl.ReaderLockAsync();
                deferred.Resolve();

                using (await readerLockPromise)
                {
                    ++readerCount;
                }

                while (readerCount < expectedReaderCount)
                {
                    using (await rwl.ReaderLockAsync())
                    {
                        ++readerCount;
                    }
                }
            }, SynchronizationOption.Synchronous);

            Promise.All(readerRunner, writerRunner)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
            Assert.AreEqual(expectedReaderCount, readerCount);
        }

        public enum ReaderWriterLockType
        {
            Reader,
            Writer,
            Upgradeable
        }

        public enum TakeNextUpgradeablePlace
        {
            BeforeUpgrade,
            InsideWriterLock,
            AfterDowngrade
        }

        private static IEnumerable<TestCaseData> DontStarveLocksCases()
        {
            TakeNextUpgradeablePlace[] upgradeablePlaces = new[]
            {
                TakeNextUpgradeablePlace.BeforeUpgrade,
                TakeNextUpgradeablePlace.InsideWriterLock,
                TakeNextUpgradeablePlace.AfterDowngrade
            };
            SynchronizationOption[] runnerOptions = new[]
            {
                SynchronizationOption.Synchronous,
#if !UNITY_WEBGL
                SynchronizationOption.Background
#endif
            };
            foreach (var upgradeablePlace in upgradeablePlaces)
            foreach (var runnerOption in runnerOptions)
            {
                yield return new TestCaseData(ReaderWriterLockType.Reader, ReaderWriterLockType.Writer, ReaderWriterLockType.Upgradeable, upgradeablePlace, runnerOption);
                yield return new TestCaseData(ReaderWriterLockType.Reader, ReaderWriterLockType.Upgradeable, ReaderWriterLockType.Writer, upgradeablePlace, runnerOption);
                yield return new TestCaseData(ReaderWriterLockType.Writer, ReaderWriterLockType.Reader, ReaderWriterLockType.Upgradeable, upgradeablePlace, runnerOption);
                yield return new TestCaseData(ReaderWriterLockType.Writer, ReaderWriterLockType.Upgradeable, ReaderWriterLockType.Reader, upgradeablePlace, runnerOption);
                yield return new TestCaseData(ReaderWriterLockType.Upgradeable, ReaderWriterLockType.Reader, ReaderWriterLockType.Writer, upgradeablePlace, runnerOption);
                yield return new TestCaseData(ReaderWriterLockType.Upgradeable, ReaderWriterLockType.Writer, ReaderWriterLockType.Reader, upgradeablePlace, runnerOption);
            }
        }

        [Test, TestCaseSource(nameof(DontStarveLocksCases))]
        public void AsyncReaderWriterLock_LocksDoNotStarveOtherLocks_Async(
            ReaderWriterLockType first,
            ReaderWriterLockType second,
            ReaderWriterLockType third,
            TakeNextUpgradeablePlace upgradeablePlace,
            SynchronizationOption runnerOption)
        {
            // A lock that does not balance the types of locks acquired will fail this test (reader-preferred or writer-preferred),
            // causing the favored lock type to loop forever, never allowing the other lock types access.

            var rwl = new AsyncReaderWriterLock();

            int readerCount = 0;
            int writerCount = 0;
            int upgradeableReaderCount = 0;
            int upgradedWriterCount = 0;
            const int expectedCounts = 10;

            var readerStartDeferred = Promise.NewDeferred();
            var writerStartDeferred = Promise.NewDeferred();
            var upgradeableReaderStartDeferred = Promise.NewDeferred();

            var readerReadyDeferred = Promise.NewDeferred();
            var writerReadyDeferred = Promise.NewDeferred();
            var upgradeableReaderReadyDeferred = Promise.NewDeferred();
            var allReadyPromise = Promise.All(readerReadyDeferred.Promise, writerReadyDeferred.Promise, upgradeableReaderReadyDeferred.Promise).Preserve();

            var readerRunner = readerStartDeferred.Promise
                .WaitAsync(runnerOption)
                .Then(async () =>
                {
                    // We take the lock first, then always take another lock before releasing the current.
                    var lockPromise = rwl.ReaderLockAsync();

                    readerReadyDeferred.Resolve();
                    await allReadyPromise.WaitAsync(runnerOption); // Wait for the other runners to start.

                    while (readerCount < expectedCounts || writerCount < expectedCounts || upgradeableReaderCount < expectedCounts || upgradedWriterCount < expectedCounts)
                    {
                        using (await lockPromise)
                        {
                            ++readerCount;
                            lockPromise = rwl.ReaderLockAsync();
                        }
                    }

                    using (await lockPromise) { }
                });

            var writerRunner = writerStartDeferred.Promise
                .WaitAsync(runnerOption)
                .Then(async () =>
                {
                    // We take the lock first, then always take another lock before releasing the current.
                    var lockPromise = rwl.WriterLockAsync();

                    writerReadyDeferred.Resolve();
                    await allReadyPromise.WaitAsync(runnerOption); // Wait for the other runners to start.

                    while (readerCount < expectedCounts || writerCount < expectedCounts || upgradeableReaderCount < expectedCounts || upgradedWriterCount < expectedCounts)
                    {
                        using (await lockPromise)
                        {
                            ++writerCount;
                            lockPromise = rwl.WriterLockAsync();
                        }
                    }

                    using (await lockPromise) { }
                });

            var upgradeableReaderRunner = upgradeableReaderStartDeferred.Promise
                .WaitAsync(runnerOption)
                .Then(async () =>
                {
                    // We take the lock first, then always take another lock before releasing the current.
                    var lockPromise = rwl.UpgradeableReaderLockAsync();

                    upgradeableReaderReadyDeferred.Resolve();
                    await allReadyPromise.WaitAsync(runnerOption); // Wait for the other runners to start.

                    while (readerCount < expectedCounts || writerCount < expectedCounts || upgradeableReaderCount < expectedCounts || upgradedWriterCount < expectedCounts)
                    {
                        using (var key = await lockPromise)
                        {
                            ++upgradeableReaderCount;
                            if (upgradeablePlace == TakeNextUpgradeablePlace.BeforeUpgrade)
                            {
                                lockPromise = rwl.UpgradeableReaderLockAsync();
                            }
                            using (await rwl.UpgradeToWriterLockAsync(key))
                            {
                                ++upgradedWriterCount;
                                if (upgradeablePlace == TakeNextUpgradeablePlace.InsideWriterLock)
                                {
                                    lockPromise = rwl.UpgradeableReaderLockAsync();
                                }
                            }
                            if (upgradeablePlace == TakeNextUpgradeablePlace.AfterDowngrade)
                            {
                                lockPromise = rwl.UpgradeableReaderLockAsync();
                            }
                        }
                    }

                    using (await lockPromise) { }
                });

            StartRunner(first);
            StartRunner(second);
            StartRunner(third);

            Promise.All(readerRunner, writerRunner, upgradeableReaderRunner)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
            allReadyPromise.Forget();

            Assert.GreaterOrEqual(readerCount, expectedCounts);
            Assert.GreaterOrEqual(writerCount, expectedCounts);
            Assert.GreaterOrEqual(upgradeableReaderCount, expectedCounts);
            Assert.GreaterOrEqual(upgradedWriterCount, expectedCounts);

            void StartRunner(ReaderWriterLockType lockType)
            {
                if (lockType == ReaderWriterLockType.Reader)
                {
                    readerStartDeferred.Resolve();
                }
                else if (lockType == ReaderWriterLockType.Writer)
                {
                    writerStartDeferred.Resolve();
                }
                else
                {
                    upgradeableReaderStartDeferred.Resolve();
                }
            }
        }
    }
#endif // UNITY_2021_2_OR_NEWER || NETSTANDARD2_1_OR_GREATER || NETCOREAPP
}