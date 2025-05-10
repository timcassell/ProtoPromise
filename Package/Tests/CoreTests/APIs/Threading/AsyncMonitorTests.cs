#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using NUnit.Framework;
using Proto.Promises;
using Proto.Promises.Threading;
using System;
using System.Threading;

namespace ProtoPromise.Tests.APIs.Threading
{
#if UNITY_2021_2_OR_NEWER || NETSTANDARD2_1_OR_GREATER || NETCOREAPP
    public class AsyncMonitorTests
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

        public enum CancelationType
        {
            NoToken,
            Default,
            Canceled,
            Pending
        }

        private static CancelationToken GetToken(CancelationSource cancelationSource, CancelationType cancelationType)
        {
            return cancelationType == CancelationType.Canceled ? CancelationToken.Canceled()
                : cancelationType == CancelationType.Pending ? cancelationSource.Token
                : CancelationToken.None;
        }

        [Test]
        public void AsyncMonitor_TryEnter_ReturnsFalseIfLockIsHeld()
        {
            var mutex = new AsyncLock();
            var key = mutex.Lock();
            Assert.False(AsyncMonitor.TryEnter(mutex, out _));
            Assert.False(AsyncMonitor.TryEnter(mutex, out _, CancelationToken.Canceled()));
            key.Dispose();
        }

        [Test]
        public void AsyncMonitor_TryEnter_ReturnsTrueIfLockIsNotHeld([Values] CancelationType cancelationType)
        {
            var mutex = new AsyncLock();
            var cancelationSource = CancelationSource.New();
            AsyncLock.Key key;
            if (cancelationType == CancelationType.NoToken)
            {
                Assert.IsTrue(AsyncMonitor.TryEnter(mutex, out key));
            }
            else
            {
                Assert.IsTrue(AsyncMonitor.TryEnter(mutex, out key, GetToken(cancelationSource, cancelationType)));
            }
            key.Dispose();
            cancelationSource.Dispose();
        }

        [Test]
        public void AsyncMonitor_TryEnterAsync_TrueIfLockIsNotHeld(
            [Values(CancelationType.Default, CancelationType.Canceled, CancelationType.Pending)] CancelationType cancelationType)
        {
            var mutex = new AsyncLock();
            var cancelationSource = CancelationSource.New();
            AsyncMonitor.TryEnterAsync(mutex, GetToken(cancelationSource, cancelationType))
                .Then(tuple =>
                {
                    Assert.True(tuple.didEnter);
                    tuple.key.Dispose();
                })
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
            cancelationSource.Dispose();
        }

        [Test]
        public void AsyncMonitor_TryEnterAsync_FalseIfLockIsHeld(
            [Values(CancelationType.Canceled, CancelationType.Pending)] CancelationType cancelationType)
        {
            var mutex = new AsyncLock();
            var key = mutex.Lock();

            var cancelationSource = CancelationSource.New();
            var promise = AsyncMonitor.TryEnterAsync(mutex, GetToken(cancelationSource, cancelationType))
                .Then(tuple =>
                {
                    Assert.False(tuple.didEnter);
                });
            cancelationSource.Cancel();
            promise.WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
            cancelationSource.Dispose();
            key.Dispose();
        }

        [Test]
        public void AsyncMonitor_TryEnterAsync_TrueIfLockIsReleased(
            [Values(CancelationType.Default, CancelationType.Pending)] CancelationType cancelationType)
        {
            var mutex = new AsyncLock();
            var key = mutex.Lock();

            var cancelationSource = CancelationSource.New();
            var promise = AsyncMonitor.TryEnterAsync(mutex, GetToken(cancelationSource, cancelationType))
                .Then(tuple =>
                {
                    Assert.True(tuple.didEnter);
                    tuple.key.Dispose();
                });
            key.Dispose();
            promise.WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
            cancelationSource.Dispose();
        }

        [Test]
        public void AsyncMonitor_PreCanceled_WaitReturnsFalse()
        {
            var mutex = new AsyncLock();
            using (var key = mutex.Lock())
            {
                Assert.IsFalse(AsyncMonitor.TryWait(key, CancelationToken.Canceled()));
            }
        }

        [Test]
        public void AsyncMonitor_PreCanceled_WaitAsyncYieldsFalse()
        {
            var mutex = new AsyncLock();
            mutex.LockAsync()
                .Then(key =>
                {
                    return AsyncMonitor.TryWaitAsync(key, CancelationToken.Canceled())
                        .Then(wasPulsed =>
                        {
                            Assert.IsFalse(wasPulsed);
                            key.Dispose();
                        });
                })
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AsyncMonitor_Canceled_WaitAsyncYieldsFalse()
        {
            var mutex = new AsyncLock();
            var cts = CancelationSource.New();
            var readyDeferred = Promise.NewDeferred();
            var waitPromise = mutex.LockAsync()
                .Then(key =>
                {
                    readyDeferred.Resolve();
                    return AsyncMonitor.TryWaitAsync(key, cts.Token)
                        .Then(wasPulsed =>
                        {
                            Assert.IsFalse(wasPulsed);
                            key.Dispose();
                        });
                });

            readyDeferred.Promise
                .Then(() => mutex.LockAsync())
                .Then(key =>
                {
                    cts.Cancel();
                    AsyncMonitor.Pulse(key);
                    key.Dispose();
                    return waitPromise;
                })
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
            cts.Dispose();
        }

        [Test]
        public void AsyncMonitor_CanceledTooLate_WaitAsyncYieldsTrue()
        {
            var mutex = new AsyncLock();
            var cts = CancelationSource.New();
            var readyDeferred = Promise.NewDeferred();
            var waitPromise = mutex.LockAsync()
                .Then(key =>
                {
                    readyDeferred.Resolve();
                    return AsyncMonitor.TryWaitAsync(key, cts.Token)
                        .Then(wasPulsed =>
                        {
                            Assert.IsTrue(wasPulsed);
                            cts.Cancel();
                            key.Dispose();
                        });
                });

            readyDeferred.Promise
                .Then(() => mutex.LockAsync())
                .Then(key =>
                {
                    AsyncMonitor.Pulse(key);
                    key.Dispose();
                    return waitPromise;
                })
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
            cts.Dispose();
        }

        [Test]
        public void AsyncMonitor_Pulse_DoesNothingWithNoWaiters()
        {
            var mutex = new AsyncLock();
            mutex.LockAsync()
                .Then(key =>
                {
                    AsyncMonitor.Pulse(key);
                    key.Dispose();
                })
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));

            using (var key = mutex.Lock())
            {
                AsyncMonitor.Pulse(key);
            }
        }

        [Test]
        public void AsyncMonitor_PulseAll_DoesNothingWithNoWaiters()
        {
            var mutex = new AsyncLock();
            mutex.LockAsync()
                .Then(key =>
                {
                    AsyncMonitor.PulseAll(key);
                    key.Dispose();
                })
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));

            using (var key = mutex.Lock())
            {
                AsyncMonitor.PulseAll(key);
            }
        }

        [Test]
        public void AsyncMonitor_TryWaitAsync_AlreadyCanceled_AnotherLockWaiting_ReturnsFalse_SingleWaiter()
        {
            var mutex = new AsyncLock();
            Promise.Run(async () =>
            {
                Promise notifyPromise;
                using (var key = await mutex.LockAsync())
                {
                    notifyPromise = Promise.Run(async () =>
                    {
                        using (var key2 = await mutex.LockAsync())
                        {
                            AsyncMonitor.Pulse(key2);
                        }
                    }, SynchronizationOption.Synchronous);

                    var success = await AsyncMonitor.TryWaitAsync(key, CancelationToken.Canceled());
                    Assert.False(success);
                }
                await notifyPromise;
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AsyncMonitor_TryWaitAsync_AlreadyCanceled_AnotherLockWaiting_ReturnsFalse_MultipleWaiters()
        {
            var mutex = new AsyncLock();
            Promise.Run(async () =>
            {
                Promise firstLockPromise;
                Promise secondLockPromise;
                using (var key = await mutex.LockAsync())
                {
                    firstLockPromise = Promise.Run(async () =>
                    {
                        using (await mutex.LockAsync())
                        {
                        }
                    }, SynchronizationOption.Synchronous);

                    secondLockPromise = Promise.Run(async () =>
                    {
                        using (var key2 = await mutex.LockAsync())
                        {
                            AsyncMonitor.PulseAll(key2);
                        }
                    }, SynchronizationOption.Synchronous);

                    var success = await AsyncMonitor.TryWaitAsync(key, CancelationToken.Canceled());
                    Assert.False(success);
                }
                await Promise.All(firstLockPromise, secondLockPromise);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

#if !UNITY_WEBGL
        [Test]
        public void AsyncMonitor_TryEnter_ReturnsFalseIfLockIsHeld(
            [Values(CancelationType.NoToken, CancelationType.Canceled, CancelationType.Pending)] CancelationType cancelationType)
        {
            var mutex = new AsyncLock();
            var cancelationSource = CancelationSource.New();
            var deferredReady = Promise.NewDeferred();
            bool didWait = false;

            var promise = Promise.Run(() =>
            {
                using (var key = AsyncMonitor.Enter(mutex))
                {
                    // Hold onto the lock until the other thread tries to enter.
                    lock (mutex)
                    {
                        deferredReady.Resolve();
                        Monitor.Wait(mutex);
                    }
                    // Sleep a bit to make sure the other thread is waiting on TryEnter.
                    Thread.Sleep(100);
                    cancelationSource.Cancel();
                    // Make sure we don't exit the lock until the other thread returned from TryEnter.
                    TestHelper.SpinUntil(() => didWait, TimeSpan.FromSeconds(1));
                }
            });

            deferredReady.Promise
                .ConfigureContinuation(new ContinuationOptions(SynchronizationOption.Background, CompletedContinuationBehavior.Asynchronous))
                .Then(() =>
                {
                    AsyncLock.Key key;
                    if (cancelationType == CancelationType.NoToken)
                    {
                        Assert.False(AsyncMonitor.TryEnter(mutex, out key));
                        // Allow the other thread to continue.
                        lock (mutex)
                        {
                            Monitor.Pulse(mutex);
                        }
                    }
                    else
                    {
                        // Allow the other thread to continue.
                        lock (mutex)
                        {
                            Monitor.Pulse(mutex);
                        }
                        Assert.False(AsyncMonitor.TryEnter(mutex, out key, GetToken(cancelationSource, cancelationType)));
                    }
                    didWait = true;
                    return promise;
                })
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
            cancelationSource.Dispose();
        }
        [Test]
        public void AsyncMonitor_TryEnter_ReturnsTrueIfLockIsReleased(
            [Values(CancelationType.Default, CancelationType.Pending)] CancelationType cancelationType)
        {
            var mutex = new AsyncLock();
            var cancelationSource = CancelationSource.New();
            var deferredReady = Promise.NewDeferred();

            var promise = Promise.Run(() =>
            {
                using (var key = AsyncMonitor.Enter(mutex))
                {
                    // Hold onto the lock until the other thread tries to enter.
                    lock (mutex)
                    {
                        deferredReady.Resolve();
                        Monitor.Wait(mutex);
                    }
                }
            });

            deferredReady.Promise
                .ConfigureContinuation(new ContinuationOptions(SynchronizationOption.Background, CompletedContinuationBehavior.Asynchronous))
                .Then(() =>
                {
                    // Allow the other thread to continue.
                    lock (mutex)
                    {
                        Monitor.Pulse(mutex);
                    }
                    Assert.True(AsyncMonitor.TryEnter(mutex, out var key, GetToken(cancelationSource, cancelationType)));
                    key.Dispose();
                    return promise;
                })
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
            cancelationSource.Dispose();
        }

        [Test]
        public void AsyncMonitor_TryEnter_ReturnsFalseIfTokenIsCanceledWhileLockIsHeld()
        {
            var mutex = new AsyncLock();
            var cancelationSource = CancelationSource.New();
            var deferredReady = Promise.NewDeferred();
            bool didWait = false;

            var promise = Promise.Run(() =>
            {
                using (var key = AsyncMonitor.Enter(mutex))
                {
                    // Hold onto the lock until the other thread tries to enter.
                    lock (mutex)
                    {
                        deferredReady.Resolve();
                        Monitor.Wait(mutex);
                    }
                    // Sleep a bit to make sure the other thread is waiting on TryEnter.
                    Thread.Sleep(100);
                    cancelationSource.Cancel();
                    // Make sure we don't exit the lock until the other thread returned from TryEnter.
                    TestHelper.SpinUntil(() => didWait, TimeSpan.FromSeconds(1));
                }
            });

            deferredReady.Promise
                .ConfigureContinuation(new ContinuationOptions(SynchronizationOption.Background, CompletedContinuationBehavior.Asynchronous))
                .Then(() =>
                {
                    // Allow the other thread to continue.
                    lock (mutex)
                    {
                        Monitor.Pulse(mutex);
                    }
                    Assert.False(AsyncMonitor.TryEnter(mutex, out var key, cancelationSource.Token));
                    didWait = true;
                    return promise;
                })
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
            cancelationSource.Dispose();
        }

        [Test]
        public void AsyncMonitor_Canceled_WaitReturnsFalse()
        {
            var mutex = new AsyncLock();
            var cts = CancelationSource.New();
            var readyDeferred = Promise.NewDeferred();
            var waitPromise = Promise.Run(() =>
            {
                using (var key = mutex.Lock())
                {
                    readyDeferred.Resolve();
                    Assert.IsFalse(AsyncMonitor.TryWait(key, cts.Token));
                }
            }, SynchronizationOption.Background, forceAsync: true);

            readyDeferred.Promise
                .Then(() => mutex.LockAsync())
                .Then(key =>
                {
                    cts.Cancel();
                    AsyncMonitor.Pulse(key);
                    key.Dispose();
                    return waitPromise;
                })
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
            cts.Dispose();
        }

        [Test]
        public void AsyncMonitor_CanceledTooLate_WaitReturnsTrue()
        {
            var mutex = new AsyncLock();
            var cts = CancelationSource.New();
            var readyDeferred = Promise.NewDeferred();
            var waitPromise = Promise.Run(() =>
            {
                using (var key = mutex.Lock())
                {
                    readyDeferred.Resolve();
                    Assert.IsTrue(AsyncMonitor.TryWait(key, cts.Token));
                    cts.Cancel();
                }
            }, SynchronizationOption.Background, forceAsync: true);

            readyDeferred.Promise
                .Then(() => mutex.LockAsync())
                .Then(key =>
                {
                    AsyncMonitor.Pulse(key);
                    key.Dispose();
                    return waitPromise;
                })
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
            cts.Dispose();
        }

        [Test]
        public void AsyncMonitor_CanceledOutsideLock_WaitReturnsFalse()
        {
            var mutex = new AsyncLock();
            var cts = CancelationSource.New();
            var readyDeferred = Promise.NewDeferred();
            var waitPromise = Promise.Run(() =>
            {
                using (var key = mutex.Lock())
                {
                    readyDeferred.Resolve();
                    Assert.IsFalse(AsyncMonitor.TryWait(key, cts.Token));
                }
            }, SynchronizationOption.Background, forceAsync: true);

            readyDeferred.Promise
                .ConfigureContinuation(new ContinuationOptions(SynchronizationOption.Background, CompletedContinuationBehavior.Asynchronous))
                .Then(() =>
                {
                    Thread.Sleep(TimeSpan.FromMilliseconds(100)); // Give a little extra time for the Wait.
                    cts.Cancel();
                    return waitPromise;
                })
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(2));
            cts.Dispose();
        }

        [Test]
        public void AsyncMonitor_CanceledOutsideLock_WaitAsyncYieldsFalse()
        {
            var mutex = new AsyncLock();
            var cts = CancelationSource.New();
            var readyDeferred = Promise.NewDeferred();
            var waitPromise = mutex.LockAsync()
                .ConfigureContinuation(new ContinuationOptions(SynchronizationOption.Background, CompletedContinuationBehavior.Asynchronous))
                .Then(key =>
                {
                    readyDeferred.Resolve();
                    return AsyncMonitor.TryWaitAsync(key, cts.Token)
                        .Then(wasPulsed =>
                        {
                            Assert.IsFalse(wasPulsed);
                            key.Dispose();
                        });
                });

            readyDeferred.Promise
                .ConfigureContinuation(new ContinuationOptions(SynchronizationOption.Background, CompletedContinuationBehavior.Asynchronous))
                .Then(() =>
                {
                    Thread.Sleep(TimeSpan.FromMilliseconds(100)); // Give a little extra time for the Wait.
                    cts.Cancel();
                    return waitPromise;
                })
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(2));
            cts.Dispose();
        }

        [Test]
        public void AsyncMonitor_CanceledTooLateOutsideLock_WaitReturnsTrue()
        {
            var mutex = new AsyncLock();
            var cts = CancelationSource.New();
            var readyDeferred = Promise.NewDeferred();
            var waitPromise = Promise.Run(() =>
            {
                using (var key = mutex.Lock())
                {
                    readyDeferred.Resolve();
                    Assert.IsTrue(AsyncMonitor.TryWait(key, cts.Token));
                }
            }, SynchronizationOption.Background, forceAsync: true);

            readyDeferred.Promise
                .ConfigureContinuation(new ContinuationOptions(SynchronizationOption.Background, CompletedContinuationBehavior.Asynchronous))
                .Then(() => mutex.LockAsync())
                .Then(key =>
                {
                    AsyncMonitor.Pulse(key);
                    key.Dispose();
                    cts.Cancel();
                    return waitPromise;
                })
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
            cts.Dispose();
        }

        [Test]
        public void AsyncMonitor_CanceledTooLateOutsideLock_WaitAsyncYieldsTrue()
        {
            var mutex = new AsyncLock();
            var cts = CancelationSource.New();
            var readyDeferred = Promise.NewDeferred();
            var waitPromise = mutex.LockAsync()
                .ConfigureContinuation(new ContinuationOptions(SynchronizationOption.Background, CompletedContinuationBehavior.Asynchronous))
                .Then(key =>
                {
                    readyDeferred.Resolve();
                    return AsyncMonitor.TryWaitAsync(key, cts.Token)
                        .Then(wasPulsed =>
                        {
                            Assert.IsTrue(wasPulsed);
                            key.Dispose();
                        });
                });

            readyDeferred.Promise
                .ConfigureContinuation(new ContinuationOptions(SynchronizationOption.Background, CompletedContinuationBehavior.Asynchronous))
                .Then(() => mutex.LockAsync())
                .Then(key =>
                {
                    AsyncMonitor.Pulse(key);
                    key.Dispose();
                    cts.Cancel();
                    return waitPromise;
                })
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
            cts.Dispose();
        }

        [Test]
        public void AsyncMonitor_Pulse_ReleasesOneSyncWaiter()
        {
            var mutex = new AsyncLock();
            int completed = 0;
            var deferredReady = Promise.NewDeferred();
            var promise = Promise.Run(() =>
            {
                using (var key = AsyncMonitor.Enter(mutex))
                {
                    deferredReady.Resolve();
                    AsyncMonitor.Wait(key);
                    Interlocked.Increment(ref completed);
                }
            });

            deferredReady.Promise
                .ConfigureContinuation(new ContinuationOptions(SynchronizationOption.Background, CompletedContinuationBehavior.Asynchronous))
                .Then(() =>
                {
                    using (var key = AsyncMonitor.Enter(mutex))
                    {
                        AsyncMonitor.Pulse(key);
                    }
                    return promise;
                })
                .Then(() =>
                {
                    Assert.AreEqual(1, completed);
                })
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AsyncMonitor_PulseAll_ReleasesAllSyncWaiters()
        {
            var mutex = new AsyncLock();
            int completed = 0;
            var deferred1Ready = Promise.NewDeferred();
            var deferred2Ready = Promise.NewDeferred();
            var promise1 = Promise.Run(() =>
            {
                using (var key = AsyncMonitor.Enter(mutex))
                {
                    deferred1Ready.Resolve();
                    AsyncMonitor.Wait(key);
                    Interlocked.Increment(ref completed);
                }
            });
            var promise2 = Promise.Run(() =>
            {
                using (var key = AsyncMonitor.Enter(mutex))
                {
                    deferred2Ready.Resolve();
                    AsyncMonitor.Wait(key);
                    Interlocked.Increment(ref completed);
                }
            });

            Promise.All(deferred1Ready.Promise, deferred2Ready.Promise)
                .ConfigureContinuation(new ContinuationOptions(SynchronizationOption.Background, CompletedContinuationBehavior.Asynchronous))
                .Then(() =>
                {
                    using (var key = AsyncMonitor.Enter(mutex))
                    {
                        AsyncMonitor.PulseAll(key);
                    }
                    return Promise.All(promise1, promise2);
                })
                .Then(() =>
                {
                    Assert.AreEqual(2, completed);
                })
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AsyncMonitor_Pulse_ReleasesOneAsyncWaiter_Then()
        {
            var mutex = new AsyncLock();
            int completed = 0;
            var deferred1Ready = Promise.NewDeferred();
            var deferred2Ready = Promise.NewDeferred();
            var deferred1Complete = Promise.NewDeferred();
            var deferred2Complete = Promise.NewDeferred();
            var promise1 = Promise.Run(() =>
            {
                return AsyncMonitor.EnterAsync(mutex)
                    .Then(key =>
                    {
                        var waitPromise = AsyncMonitor.WaitAsync(key);
                        deferred1Ready.Resolve();
                        return waitPromise
                            .Then(() => key);
                    })
                    .Then(key =>
                    {
                        Interlocked.Increment(ref completed);
                        deferred1Complete.Resolve();
                        key.Dispose();
                    });
            });
            var promise2 = Promise.Run(() =>
            {
                return AsyncMonitor.EnterAsync(mutex)
                    .Then(key =>
                    {
                        var waitPromise = AsyncMonitor.WaitAsync(key);
                        deferred2Ready.Resolve();
                        return waitPromise
                            .Then(() => key);
                    })
                    .Then(key =>
                    {
                        Interlocked.Increment(ref completed);
                        deferred2Complete.Resolve();
                        key.Dispose();
                    });
            });
            Promise.All(deferred1Ready.Promise, deferred2Ready.Promise)
                .ConfigureContinuation(new ContinuationOptions(SynchronizationOption.Background, CompletedContinuationBehavior.Asynchronous))
                .Then(() =>
                {
                    return AsyncMonitor.EnterAsync(mutex)
                        .Then(key =>
                        {
                            AsyncMonitor.Pulse(key);
                            key.Dispose();
                            return Promise.Race(promise1, promise2);
                        })
                        .Then(() =>
                        {
                            Assert.AreEqual(1, completed);
                            return AsyncMonitor.EnterAsync(mutex);
                        })
                        .Then(key =>
                        {
                            AsyncMonitor.Pulse(key);
                            key.Dispose();
                            return Promise.All(deferred1Complete.Promise, deferred2Complete.Promise);
                        });
                })
                .Then(() =>
                {
                    Assert.AreEqual(2, completed);
                })
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AsyncMonitor_PulseAll_ReleasesAllAsyncWaiters_Then()
        {
            var mutex = new AsyncLock();
            int completed = 0;
            var deferred1Ready = Promise.NewDeferred();
            var deferred2Ready = Promise.NewDeferred();
            var promise1 = Promise.Run(() =>
            {
                return AsyncMonitor.EnterAsync(mutex)
                    .Then(key =>
                    {
                        var waitPromise = AsyncMonitor.WaitAsync(key);
                        deferred1Ready.Resolve();
                        return waitPromise
                            .Then(() => key);
                    })
                    .Then(key =>
                    {
                        Interlocked.Increment(ref completed);
                        key.Dispose();
                    });
            });
            var promise2 = Promise.Run(() =>
            {
                return AsyncMonitor.EnterAsync(mutex)
                    .Then(key =>
                    {
                        var waitPromise = AsyncMonitor.WaitAsync(key);
                        deferred2Ready.Resolve();
                        return waitPromise
                            .Then(() => key);
                    })
                    .Then(key =>
                    {
                        Interlocked.Increment(ref completed);
                        key.Dispose();
                    });
            });
            Promise.All(deferred1Ready.Promise, deferred2Ready.Promise)
                .ConfigureContinuation(new ContinuationOptions(SynchronizationOption.Background, CompletedContinuationBehavior.Asynchronous))
                .Then(() =>
                {
                    return AsyncMonitor.EnterAsync(mutex)
                        .Then(key =>
                        {
                            AsyncMonitor.PulseAll(key);
                            key.Dispose();
                            return Promise.All(promise1, promise2);
                        });
                })
                .Then(() =>
                {
                    Assert.AreEqual(2, completed);
                })
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AsyncMonitor_Pulse_ReleasesOneAsyncWaiter_AsyncAwait()
        {
            Promise.Run(async () =>
            {
                var mutex = new AsyncLock();
                int completed = 0;
                var deferred1Ready = Promise.NewDeferred();
                var deferred2Ready = Promise.NewDeferred();
                var deferred1Complete = Promise.NewDeferred();
                var deferred2Complete = Promise.NewDeferred();
                var promise1 = Promise.Run(async () =>
                {
                    using (var key = await AsyncMonitor.EnterAsync(mutex))
                    {
                        var waitPromise = AsyncMonitor.WaitAsync(key);
                        deferred1Ready.Resolve();
                        await waitPromise;
                        Interlocked.Increment(ref completed);
                        deferred1Complete.Resolve();
                    }
                });
                await deferred1Ready.Promise;
                var promise2 = Promise.Run(async () =>
                {
                    using (var key = await AsyncMonitor.EnterAsync(mutex))
                    {
                        var waitPromise = AsyncMonitor.WaitAsync(key);
                        deferred2Ready.Resolve();
                        await waitPromise;
                        Interlocked.Increment(ref completed);
                        deferred2Complete.Resolve();
                    }
                });
                await deferred2Ready.Promise;

                using (var key = await AsyncMonitor.EnterAsync(mutex))
                {
                    AsyncMonitor.Pulse(key);
                }
                await Promise.Race(promise1, promise2);
                Assert.AreEqual(1, completed);

                using (var key = await AsyncMonitor.EnterAsync(mutex))
                {
                    AsyncMonitor.Pulse(key);
                }
                await Promise.All(deferred1Complete.Promise, deferred2Complete.Promise);
                Assert.AreEqual(2, completed);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AsyncMonitor_PulseAll_ReleasesAllAsyncWaiters_AsyncAwait()
        {
            Promise.Run(async () =>
            {
                var mutex = new AsyncLock();
                int completed = 0;
                var deferred1Ready = Promise.NewDeferred();
                var deferred2Ready = Promise.NewDeferred();
                var promise1 = Promise.Run(async () =>
                {
                    using (var key = await AsyncMonitor.EnterAsync(mutex))
                    {
                        var waitPromise = AsyncMonitor.WaitAsync(key);
                        deferred1Ready.Resolve();
                        await waitPromise;
                        Interlocked.Increment(ref completed);
                    }
                });
                await deferred1Ready.Promise;
                var promise2 = Promise.Run(async () =>
                {
                    using (var key = await AsyncMonitor.EnterAsync(mutex))
                    {
                        var waitPromise = AsyncMonitor.WaitAsync(key);
                        deferred2Ready.Resolve();
                        await waitPromise;
                        Interlocked.Increment(ref completed);
                    }
                });
                await deferred2Ready.Promise;

                using (var key = await AsyncMonitor.EnterAsync(mutex))
                {
                    AsyncMonitor.PulseAll(key);
                }
                await Promise.All(promise1, promise2);

                Assert.AreEqual(2, completed);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }
#endif // !UNITY_WEBGL

#if PROMISE_DEBUG
        [Test]
        public void AsyncMonitor_ReleaseLock_BeforeWaitAsyncCompletes_Throws()
        {
            var mutex = new AsyncLock();
            var waitPromise = mutex.LockAsync()
                .Then(key =>
                {
                    var promise = AsyncMonitor.WaitAsync(key)
                        .Then(() => key);
                    Assert.Catch<System.InvalidOperationException>(key.Dispose);
                    return promise;
                })
                .Then(key => key.Dispose());

            mutex.LockAsync()
                .Then(secondKey =>
                {
                    AsyncMonitor.Pulse(secondKey);
                    secondKey.Dispose();
                })
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));

            waitPromise
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }
#endif
    }
#endif // UNITY_2021_2_OR_NEWER || NETSTANDARD2_1_OR_GREATER || NETCOREAPP
}