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

namespace ProtoPromiseTests.APIs.Threading
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

        [Test]
        public void AsyncMonitor_TryEnter_ReturnsTrueIfLockIsNotHeld()
        {
            var mutex = new AsyncLock();
            AsyncLock.Key key;
            Assert.IsTrue(AsyncMonitor.TryEnter(mutex, out key));
            key.Dispose();
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

#if !UNITY_WEBGL
        [Test]
        public void AsyncMonitor_TryEnter_ReturnsFalseIfLockIsHeld()
        {
            var mutex = new AsyncLock();
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
                .WaitAsync(SynchronizationOption.Background, forceAsync: true)
                .Then(() =>
                {
                    AsyncLock.Key key;
                    Assert.IsFalse(AsyncMonitor.TryEnter(mutex, out key));
                    // Allow the other thread to continue.
                    lock (mutex)
                    {
                        Monitor.Pulse(mutex);
                    }
                    return promise;
                })
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
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
                .WaitAsync(SynchronizationOption.Background, forceAsync: true)
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
                .WaitAsync(SynchronizationOption.Background, forceAsync: true)
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
                .WaitAsync(SynchronizationOption.Background, forceAsync: true)
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
                .WaitAsync(SynchronizationOption.Background, forceAsync: true)
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
                .WaitAsync(SynchronizationOption.Background, forceAsync: true)
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
                .WaitAsync(SynchronizationOption.Background, forceAsync: true)
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
                .WaitAsync(SynchronizationOption.Background, forceAsync: true)
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
                .WaitAsync(SynchronizationOption.Background, forceAsync: true)
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
                .WaitAsync(SynchronizationOption.Background, forceAsync: true)
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
                .WaitAsync(SynchronizationOption.Background, forceAsync: true)
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
            Pulse_ReleasesOneAsyncWaiter_AsyncAwait_Core()
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        private async Promise Pulse_ReleasesOneAsyncWaiter_AsyncAwait_Core()
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
        }

        [Test]
        public void AsyncMonitor_PulseAll_ReleasesAllAsyncWaiters_AsyncAwait()
        {
            PulseAll_ReleasesAllAsyncWaiters_AsyncAwait_Core()
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        private async Promise PulseAll_ReleasesAllAsyncWaiters_AsyncAwait_Core()
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