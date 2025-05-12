#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using NUnit.Framework;
using Proto.Promises;
using Proto.Promises.Threading;
using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace ProtoPromise.Tests.APIs.Threading
{
#if UNITY_2021_2_OR_NEWER || NETSTANDARD2_1_OR_GREATER || NETCOREAPP
    public class AsyncLockTests
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
        public void AsyncLock_Unlocked_SynchronouslyPermitsLock()
        {
            var mutex = new AsyncLock();

            var lockPromise = mutex.LockAsync();

            Assert.True(lockPromise.TryWaitForResult(TimeSpan.FromSeconds(1), out var key));
            key.Dispose();
        }

        [Test]
        public void AsyncLock_DoubleDisposeThrows()
        {
            var mutex = new AsyncLock();

            bool didThrow = false;
            mutex.LockAsync()
                .Then(key =>
                {
                    key.Dispose();
                    key.Dispose();
                })
                .Catch((System.InvalidOperationException e) => didThrow = true)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
            Assert.IsTrue(didThrow);
        }

        [Test]
        public void AsyncLock_PreCanceled_Unlocked_SynchronouslyCanceled()
        {
            var mutex = new AsyncLock();
            var token = CancelationToken.Canceled();
            bool canceled = false;

            mutex.LockAsync(token)
                .CatchCancelation(() => canceled = true)
                .Forget();

            Assert.True(canceled);
            Assert.Catch<OperationCanceledException>(() => mutex.Lock(token));
        }

        [Test]
        public void AsyncLock_PreCanceled_Locked_SynchronouslyCancels()
        {
            var mutex = new AsyncLock();

            var lockPromise = mutex.LockAsync();
            var token = CancelationToken.Canceled();

            var promise = mutex.LockAsync(token);

            Promise.State state = Promise.State.Pending;
            promise
                .ContinueWith(r => state = r.State)
                .Forget();
            Assert.AreEqual(Promise.State.Canceled, state);

            lockPromise.WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1)).Dispose();
        }

        [Test]
        public void AsyncLock_Locked_PreventsLockUntilUnlocked_SingleThread_Then()
        {
            var mutex = new AsyncLock();
            var deferred1Continue = Promise.NewDeferred();

            bool isLocked = false;

            mutex.LockAsync()
                .Then(key =>
                {
                    isLocked = true;
                    return deferred1Continue.Promise.ContinueWith(_ => key);
                })
                .ContinueWith(r => r.Value.Dispose())
                .Forget();

            TestHelper.ExecuteForegroundCallbacks();
            Assert.IsTrue(isLocked);

            bool promise2IsComplete = false;
            mutex.LockAsync()
                .Then(key => key.Dispose())
                .Finally(() => promise2IsComplete = true)
                .Forget();

            TestHelper.ExecuteForegroundCallbacks();
            Assert.IsFalse(promise2IsComplete);

            deferred1Continue.Resolve();
            // Continuations are posted asynchronously to the current context, so we need to make sure it is executed.
            TestHelper.ExecuteForegroundCallbacks();
            Assert.IsTrue(promise2IsComplete);
        }

        [Test]
        public void AsyncLock_CanceledLock_ThrowsException_Then()
        {
            var mutex = new AsyncLock();
            var cts = CancelationSource.New();

            mutex.LockAsync()
                .Then(key =>
                {
                    var canceledLockPromise = mutex.LockAsync(cts.Token);
                    cts.Cancel();

                    Assert.Catch<OperationCanceledException>(() => canceledLockPromise.WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1)));
                    key.Dispose();
                })
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));

            cts.Dispose();
        }

        [Test]
        public void AsyncLock_CanceledTooLate_StillTakesLock_Then()
        {
            var mutex = new AsyncLock();
            var cts = CancelationSource.New();

            var cancelableLockPromise = default(Promise<AsyncLock.Key>);
            mutex.LockAsync()
                .Then(key =>
                {
                    cancelableLockPromise = mutex.LockAsync(cts.Token);
                    key.Dispose();
                })
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));


            cancelableLockPromise
                .Then(key =>
                {
                    cts.Cancel();

                    Promise.State state = Promise.State.Pending;
                    var nextLocker = mutex.LockAsync()
                        .ContinueWith(r =>
                        {
                            state = r.State;
                            r.Value.Dispose();
                        });
                    Assert.AreEqual(Promise.State.Pending, state);

                    key.Dispose();
                    return nextLocker;
                })
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));

            cts.Dispose();
        }

#if !UNITY_WEBGL
        [Test]
        public void AsyncLock_Locked_PreventsLockUntilUnlocked_Then()
        {
            var mutex = new AsyncLock();
            var deferred1Continue = Promise.NewDeferred();

            bool isLocked = false;

            Promise.Run(() =>
            {
                mutex.LockAsync()
                    .Then(key =>
                    {
                        isLocked = true;
                        return deferred1Continue.Promise.ContinueWith(_ => key);
                    })
                    .ContinueWith(r => r.Value.Dispose())
                    .Forget();
            })
                .Forget();

            TestHelper.SpinUntil(() => isLocked, TimeSpan.FromSeconds(1));

            bool promise2IsComplete = false;
            var promise2 = Promise.Run(() =>
            {
                return mutex.LockAsync()
                    .ContinueWith(r => r.Value.Dispose());
            })
                .Finally(() => promise2IsComplete = true);

            Thread.Sleep(20);

            Assert.IsFalse(promise2IsComplete);
            deferred1Continue.Resolve();
            promise2.WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AsyncLock_Locked_OnlyPermitsOneLockerAtATime_Then()
        {
            var mutex = new AsyncLock();
            var deferred1Continue = Promise.NewDeferred();
            var deferred2Continue = Promise.NewDeferred();

            bool isLocked = false;

            Promise.Run(() =>
            {
                mutex.LockAsync()
                    .Then(key =>
                    {
                        isLocked = true;
                        return deferred1Continue.Promise.ContinueWith(_ => key);
                    })
                    .ContinueWith(r => r.Value.Dispose())
                    .Forget();
            })
                .Forget();

            TestHelper.SpinUntil(() => isLocked, TimeSpan.FromSeconds(1));

            bool deferred2Ready = false;
            bool deferred2HasLock = false;

            var promise2 = Promise.Run(() =>
            {
                var keyPromise = mutex.LockAsync();
                deferred2Ready = true;
                return keyPromise
                    .Then(key =>
                    {
                        deferred2HasLock = true;
                        return deferred2Continue.Promise.ContinueWith(_ => key);
                    })
                    .ContinueWith(r => r.Value.Dispose());
            });

            TestHelper.SpinUntil(() => deferred2Ready, TimeSpan.FromSeconds(1));

            bool promise3Complete = false;
            var promise3 = Promise.Run(() =>
            {
                return mutex.LockAsync()
                    .ContinueWith(r => r.Value.Dispose());
            })
                .Finally(() => promise3Complete = true);

            deferred1Continue.Resolve();

            TestHelper.SpinUntil(() => deferred2HasLock, TimeSpan.FromSeconds(1));

            Assert.IsFalse(promise3Complete);
            deferred2Continue.Resolve();

            promise2.WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
            promise3.WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AsyncLock_CanceledLock_LeavesLockUnlocked_Then()
        {
            var mutex = new AsyncLock();
            var cts = CancelationSource.New();

            mutex.LockAsync()
                .Then(key =>
                {
                    bool triedToEnterLock = false;
                    var promise = Promise.Run(() =>
                    {
                        var lockPromise = mutex.LockAsync(cts.Token);
                        triedToEnterLock = true;
                        return lockPromise;
                    });

                    TestHelper.SpinUntil(() => triedToEnterLock, TimeSpan.FromSeconds(1));
                    cts.Cancel();

                    Assert.Catch<OperationCanceledException>(() => promise.WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1)));
                    key.Dispose();

                    mutex.LockAsync().WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1)).Dispose();
                })
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
                
            cts.Dispose();
        }
#endif // !UNITY_WEBGL

        [Test]
        public void AsyncLock_CanceledLock_ThrowsException_AsyncAwait()
        {
            Promise.Run(async () =>
            {
                var mutex = new AsyncLock();
                var cts = CancelationSource.New();

                var key = await mutex.LockAsync();
                var canceledLockPromise = mutex.LockAsync(cts.Token);
                cts.Cancel();

                // Continuations are posted asynchronously to the current context, so we need to make sure it is executed.
                TestHelper.ExecuteForegroundCallbacks();
                Assert.Catch<OperationCanceledException>(() => canceledLockPromise.WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1)));
                key.Dispose();
                cts.Dispose();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AsyncLock_CanceledTooLate_StillTakesLock_AsyncAwait()
        {
            Promise.Run(async () =>
            {
                var mutex = new AsyncLock();
                var cts = CancelationSource.New();

                Promise<AsyncLock.Key> cancelableLockPromise;
                using (await mutex.LockAsync())
                {
                    cancelableLockPromise = mutex.LockAsync(cts.Token);
                }

                cts.Cancel();

                Promise.State state = Promise.State.Pending;
                var nextLocker = mutex.LockAsync()
                    .ContinueWith(r =>
                    {
                        state = r.State;
                        r.Value.Dispose();
                    });
                Assert.AreEqual(Promise.State.Pending, state);

                var key = await cancelableLockPromise;
                key.Dispose();
                await nextLocker;
                cts.Dispose();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

#if !UNITY_WEBGL
        [Test]
        public void AsyncLock_Locked_PreventsLockUntilUnlocked_SingleThread_AsyncAwait()
        {
            var mutex = new AsyncLock();
            var deferred1Continue = Promise.NewDeferred();

            bool isLocked = false;

            Promise.Run(async () =>
            {
                using (await mutex.LockAsync())
                {
                    isLocked = true;
                    await deferred1Continue.Promise;
                }
            }, SynchronizationOption.Synchronous)
                .Forget();

            TestHelper.ExecuteForegroundCallbacks();
            Assert.IsTrue(isLocked);

            bool promise2IsComplete = false;
            Promise.Run(async () =>
            {
                using (await mutex.LockAsync()) { }
            }, SynchronizationOption.Synchronous)
                .Finally(() => promise2IsComplete = true)
                .Forget();

            TestHelper.ExecuteForegroundCallbacks();
            Assert.IsFalse(promise2IsComplete);

            deferred1Continue.Resolve();
            // Continuations are posted asynchronously to the current context, so we need to make sure it is executed.
            TestHelper.ExecuteForegroundCallbacks();
            Assert.IsTrue(promise2IsComplete);
        }

        [Test]
        public void AsyncLock_Locked_PreventsLockUntilUnlocked_AsyncAwait()
        {
            Promise.Run(async () =>
            {
                var mutex = new AsyncLock();
                var deferred1HasLock = Promise.NewDeferred();
                var deferred1Continue = Promise.NewDeferred();

                Promise.Run(async () =>
                {
                    using (await mutex.LockAsync())
                    {
                        deferred1HasLock.Resolve();
                        await deferred1Continue.Promise;
                    }
                })
                    .Forget();
                await deferred1HasLock.Promise;

                bool promise2IsComplete = false;
                var promise2 = Promise.Run(async () =>
                {
                    using (await mutex.LockAsync()) { }
                })
                    .Finally(() => promise2IsComplete = true);

                Assert.IsFalse(promise2IsComplete);
                deferred1Continue.Resolve();
                await promise2;
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AsyncLock_Locked_OnlyPermitsOneLockerAtATime_AsyncAwait()
        {
            Promise.Run(async () =>
            {
                var mutex = new AsyncLock();
                var deferred1HasLock = Promise.NewDeferred();
                var deferred1Continue = Promise.NewDeferred();
                var deferred2Ready = Promise.NewDeferred();
                var deferred2HasLock = Promise.NewDeferred();
                var deferred2Continue = Promise.NewDeferred();

                Promise.Run(async () =>
                {
                    using (await mutex.LockAsync())
                    {
                        deferred1HasLock.Resolve();
                        await deferred1Continue.Promise;
                    }
                })
                    .Forget();
                await deferred1HasLock.Promise;

                var promise2 = Promise.Run(async () =>
                {
                    var key = mutex.LockAsync();
                    deferred2Ready.Resolve();
                    using (await key)
                    {
                        deferred2HasLock.Resolve();
                        await deferred2Continue.Promise;
                    }
                });
                await deferred2Ready.Promise;

                bool promise3Complete = false;
                var promise3 = Promise.Run(async () =>
                {
                    using (await mutex.LockAsync()) { }
                })
                    .Finally(() => promise3Complete = true);

                deferred1Continue.Resolve();
                await deferred2HasLock.Promise;

                Assert.IsFalse(promise3Complete);
                deferred2Continue.Resolve();
                await promise2;
                await promise3;
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AsyncLock_CanceledLock_LeavesLockUnlocked_AsyncAwait()
        {
            Promise.Run(async () =>
            {
                var mutex = new AsyncLock();
                var cts = CancelationSource.New();

                var unlock = await mutex.LockAsync();
                bool triedToEnterLock = false;
                var promise = Promise.Run(async () =>
                {
                    var lockPromise = mutex.LockAsync(cts.Token);
                    triedToEnterLock = true;
                    await lockPromise;
                });

                TestHelper.SpinUntil(() => triedToEnterLock, TimeSpan.FromSeconds(1));
                cts.Cancel();

                Assert.Catch<OperationCanceledException>(() => promise.WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1)));
                unlock.Dispose();

                mutex.LockAsync().WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1)).Dispose();
                cts.Dispose();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AsyncLock_SupportsMultipleAsynchronousLocks_AsyncAwait()
        {
            // This test will fail if continuations are executed synchronously (promise1 will run in the loop forever, blocking promise2 from completing).

            var asyncLock = new AsyncLock();
            var cancelationSource = CancelationSource.New();
            var cancelationToken = cancelationSource.Token;
            var promise1 = Promise.Run(async () =>
            {
                while (!cancelationToken.IsCancelationRequested)
                {
                    using (await asyncLock.LockAsync())
                    {
                        Thread.Sleep(10);
                    }
                }
            }, forceAsync: true);
            var promise2 = Promise.Run(() =>
            {
                using (asyncLock.Lock())
                {
                    Thread.Sleep(1000);
                }
            }, forceAsync: true);

            promise2.WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(10));
            cancelationSource.Cancel();
            promise1.WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
            cancelationSource.Dispose();
        }
#endif // !UNITY_WEBGL

        [Test]
        public void AsyncLock_LockAsync_ContinuesOnConfiguredContext_Then(
            [Values] bool continueOnCapturedContext)
        {
            var foregroundThread = Thread.CurrentThread;
            var asyncLock = new AsyncLock();

            var initialKey = asyncLock.Lock();

            bool isExecuting = false;
            var promise = asyncLock.LockAsync(continueOnCapturedContext)
                .Then(key =>
                {
                    Assert.AreNotEqual(continueOnCapturedContext, isExecuting);
                    key.Dispose();
                });

            isExecuting = true;
            initialKey.Dispose();
            isExecuting = false;
            promise.WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AsyncLock_LockAsync_ContinuesOnConfiguredContext_await(
            [Values] bool continueOnCapturedContext)
        {
            var foregroundThread = Thread.CurrentThread;
            var asyncLock = new AsyncLock();
            
            var initialKey = asyncLock.Lock();

            bool isExecuting = false;
            var promise = Promise.Run(async () =>
            {
                using (await asyncLock.LockAsync(continueOnCapturedContext))
                {
                    Assert.AreNotEqual(continueOnCapturedContext, isExecuting);
                }
            }, SynchronizationOption.Synchronous);

            isExecuting = true;
            initialKey.Dispose();
            isExecuting = false;
            promise.WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

#if PROTO_PROMISE_TEST_GC_ENABLED
        [Test]
        public void AsyncMonitor_AbandonedLockIsReported()
        {
            var currentRejectionHandler = Promise.Config.UncaughtRejectionHandler;
            AbandonedLockException abandonedLockException = null;
            Promise.Config.UncaughtRejectionHandler = ex =>
            {
                abandonedLockException = ex.Value as AbandonedLockException;
            };

            var mutex = new AsyncLock();
            EnterAndAbandonLock(mutex);

            TestHelper.GcCollectAndWaitForFinalizers();
            Assert.IsNotNull(abandonedLockException);
            Assert.Throws<AbandonedLockException>(() => mutex.Lock());
            Assert.Throws<AbandonedLockException>(() => mutex.LockAsync());
            Assert.Throws<AbandonedLockException>(() => AsyncMonitor.Enter(mutex));
            Assert.Throws<AbandonedLockException>(() => AsyncMonitor.EnterAsync(mutex));
            Assert.Throws<AbandonedLockException>(() => AsyncMonitor.TryEnter(mutex, out _));
            Assert.Throws<AbandonedLockException>(() => AsyncMonitor.TryEnter(mutex, out _, CancelationToken.None));
            Assert.Throws<AbandonedLockException>(() => AsyncMonitor.TryEnterAsync(mutex, CancelationToken.None));

            Promise.Config.UncaughtRejectionHandler = currentRejectionHandler;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void EnterAndAbandonLock(AsyncLock mutex)
        {
            mutex.Lock();
        }
#endif // PROTO_PROMISE_TEST_GC_ENABLED
    }
#endif // UNITY_2021_2_OR_NEWER || NETSTANDARD2_1_OR_GREATER || NETCOREAPP
}