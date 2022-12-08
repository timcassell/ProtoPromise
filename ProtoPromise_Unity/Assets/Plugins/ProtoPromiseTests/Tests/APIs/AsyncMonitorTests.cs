using NUnit.Framework;
using Proto.Promises;
using Proto.Promises.Threading;
using System;
using System.Threading;

namespace ProtoPromiseTests.APIs
{
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
        public void AsyncMonitor_TryEnter_ReturnsFalseIfLockIsHeld()
        {
            var mutex = new AsyncLock();
            var deferredReady = Promise.NewDeferred();
            var promise = Promise.Run(() =>
            {
                using (var key = AsyncMonitor.Enter(mutex))
                {
                    deferredReady.Resolve();
                    // Hold onto the lock until the other thread tries to enter.
                    lock (mutex)
                    {
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
                .WaitWithTimeout(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AsyncMonitor_PreCanceled_WaitReturnsFalse()
        {
            var mutex = new AsyncLock();
            using (var key = mutex.Lock())
            {
                Assert.IsFalse(AsyncMonitor.Wait(key, CancelationToken.Canceled()));
            }
        }

        [Test]
        public void AsyncMonitor_PreCanceled_WaitAsyncYieldsFalse()
        {
            var mutex = new AsyncLock();
            var promise = mutex.LockAsync()
                .Then(key =>
                {
                    var waitPromise = AsyncMonitor.WaitAsync(key, CancelationToken.Canceled());
                    key.Dispose();
                    return waitPromise;
                })
                .Then(wasPulsed => Assert.IsFalse(wasPulsed));

            TestHelper.ExecuteForegroundCallbacks();
            promise.WaitWithTimeout(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AsyncMonitor_Canceled_WaitReturnsFalse()
        {
            var mutex = new AsyncLock();
            var cts = CancelationSource.New();
            var readyDeferred = Promise.NewDeferred();
            var promise = Promise.Run(() =>
            {
                using (var key = mutex.Lock())
                {
                    readyDeferred.Resolve();
                    Assert.IsFalse(AsyncMonitor.Wait(key, cts.Token));
                }
            }, SynchronizationOption.Background, forceAsync: true);

            readyDeferred.Promise
                .Then(() => mutex.LockAsync())
                .Then(key =>
                {
                    cts.Cancel();
                    AsyncMonitor.Pulse(key);
                    key.Dispose();
                    return promise;
                })
                .WaitWithTimeout(TimeSpan.FromSeconds(1));
            cts.Dispose();
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
                    return AsyncMonitor.WaitAsync(key, cts.Token)
                        .Then(wasPulsed =>
                        {
                            Assert.IsFalse(wasPulsed);
                            key.Dispose();
                        });
                });

            var pulsePromise = readyDeferred.Promise
                .Then(() => mutex.LockAsync())
                .Then(key =>
                {
                    cts.Cancel();
                    AsyncMonitor.Pulse(key);
                    key.Dispose();
                    return waitPromise;
                });

            TestHelper.ExecuteForegroundCallbacks();
            pulsePromise.WaitWithTimeout(TimeSpan.FromSeconds(1));
            cts.Dispose();
        }

        [Test]
        public void AsyncMonitor_CanceledTooLate_WaitReturnsTrue()
        {
            var mutex = new AsyncLock();
            var cts = CancelationSource.New();
            var readyDeferred = Promise.NewDeferred();
            var promise = Promise.Run(() =>
            {
                using (var key = mutex.Lock())
                {
                    readyDeferred.Resolve();
                    Assert.IsTrue(AsyncMonitor.Wait(key, cts.Token));
                    cts.Cancel();
                }
            }, SynchronizationOption.Background, forceAsync: true);

            readyDeferred.Promise
                .Then(() => mutex.LockAsync())
                .Then(key =>
                {
                    AsyncMonitor.Pulse(key);
                    key.Dispose();
                    return promise;
                })
                .WaitWithTimeout(TimeSpan.FromSeconds(1));
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
                    return AsyncMonitor.WaitAsync(key, cts.Token)
                        .Then(wasPulsed =>
                        {
                            Assert.IsTrue(wasPulsed);
                            cts.Cancel();
                            key.Dispose();
                        });
                });

            var pulsePromise = readyDeferred.Promise
                .Then(() => mutex.LockAsync())
                .Then(key =>
                {
                    AsyncMonitor.Pulse(key);
                    key.Dispose();
                    return waitPromise;
                });

            TestHelper.ExecuteForegroundCallbacks();
            pulsePromise.WaitWithTimeout(TimeSpan.FromSeconds(1));
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
                    Assert.IsTrue(AsyncMonitor.Wait(key));
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
                .WaitWithTimeout(TimeSpan.FromSeconds(1));
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
                    Assert.IsTrue(AsyncMonitor.Wait(key));
                    Interlocked.Increment(ref completed);
                }
            });
            var promise2 = Promise.Run(() =>
            {
                using (var key = AsyncMonitor.Enter(mutex))
                {
                    deferred2Ready.Resolve();
                    Assert.IsTrue(AsyncMonitor.Wait(key));
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
                .WaitWithTimeout(TimeSpan.FromSeconds(1));
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
                        return waitPromise.ContinueWith((Promise<bool>.ContinueFunc<AsyncLock.Key>) (r =>
                        {
                            Assert.IsTrue(r.Result);
                            return key;
                        }));
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
                        return waitPromise.ContinueWith((Promise<bool>.ContinueFunc<AsyncLock.Key>) (r =>
                        {
                            Assert.IsTrue(r.Result);
                            return key;
                        }));
                    })
                    .Then(key =>
                    {
                        Interlocked.Increment(ref completed);
                        deferred2Complete.Resolve();
                        key.Dispose();
                    });
            });
            Promise.All(deferred1Ready.Promise, deferred2Ready.Promise)
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
                .WaitWithTimeout(TimeSpan.FromSeconds(1));
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
                        return waitPromise.ContinueWith((Promise<bool>.ContinueFunc<AsyncLock.Key>) (r =>
                        {
                            Assert.IsTrue(r.Result);
                            return key;
                        }));
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
                        return waitPromise.ContinueWith((Promise<bool>.ContinueFunc<AsyncLock.Key>) (r =>
                        {
                            Assert.IsTrue(r.Result);
                            return key;
                        }));
                    })
                    .Then(key =>
                    {
                        Interlocked.Increment(ref completed);
                        key.Dispose();
                    });
            });
            Promise.All(deferred1Ready.Promise, deferred2Ready.Promise)
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
                .WaitWithTimeout(TimeSpan.FromSeconds(1));
        }

#if CSHARP_7_3_OR_NEWER
        [Test]
        public void AsyncMonitor_Pulse_ReleasesOneAsyncWaiter_AsyncAwait()
        {
            Pulse_ReleasesOneAsyncWaiter_AsyncAwait_Core().WaitWithTimeout(TimeSpan.FromSeconds(1));
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
                    Assert.IsTrue(await waitPromise);
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
                    Assert.IsTrue(await waitPromise);
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
            PulseAll_ReleasesAllAsyncWaiters_AsyncAwait_Core().WaitWithTimeout(TimeSpan.FromSeconds(1));
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
                    Assert.IsTrue(await waitPromise);
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
                    Assert.IsTrue(await waitPromise);
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
#endif // CSHARP_7_3_OR_NEWER
    }
}