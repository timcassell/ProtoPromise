#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using NUnit.Framework;
using Proto.Promises;
using Proto.Promises.Threading;
using ProtoPromiseTests.Concurrency;
using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace ProtoPromiseTests.APIs.Threading
{
#if UNITY_2021_2_OR_NEWER || NETSTANDARD2_1_OR_GREATER || NETCOREAPP
    public class AsyncConditionVariableTests
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
        public void AsyncConditionVariable_PreCanceled_WaitReturnsFalse()
        {
            var mutex = new AsyncLock();
            var condVar = new AsyncConditionVariable();
            using (var key = mutex.Lock())
            {
                Assert.IsFalse(condVar.TryWait(key, CancelationToken.Canceled()));
            }
        }

        [Test]
        public void AsyncConditionVariable_PreCanceled_WaitAsyncYieldsFalse()
        {
            var mutex = new AsyncLock();
            var condVar = new AsyncConditionVariable();
            mutex.LockAsync()
                .Then(key =>
                {
                    return condVar.TryWaitAsync(key, CancelationToken.Canceled())
                        .Then(wasPulsed =>
                        {
                            Assert.IsFalse(wasPulsed);
                            key.Dispose();
                        });
                })
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AsyncConditionVariable_Canceled_WaitAsyncYieldsFalse()
        {
            var mutex = new AsyncLock();
            var condVar = new AsyncConditionVariable();
            var cts = CancelationSource.New();
            var readyDeferred = Promise.NewDeferred();
            var waitPromise = mutex.LockAsync()
                .Then(key =>
                {
                    readyDeferred.Resolve();
                    return condVar.TryWaitAsync(key, cts.Token)
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
                    condVar.Notify(key);
                    key.Dispose();
                    return waitPromise;
                })
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
            cts.Dispose();
        }

        [Test]
        public void AsyncConditionVariable_CanceledTooLate_WaitAsyncYieldsTrue()
        {
            var mutex = new AsyncLock();
            var condVar = new AsyncConditionVariable();
            var cts = CancelationSource.New();
            var readyDeferred = Promise.NewDeferred();
            var waitPromise = mutex.LockAsync()
                .Then(key =>
                {
                    readyDeferred.Resolve();
                    return condVar.TryWaitAsync(key, cts.Token)
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
                    condVar.Notify(key);
                    key.Dispose();
                    return waitPromise;
                })
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
            cts.Dispose();
        }

        [Test]
        public void AsyncConditionVariable_Pulse_DoesNothingWithNoWaiters()
        {
            var mutex = new AsyncLock();
            var condVar = new AsyncConditionVariable();
            mutex.LockAsync()
                .Then(key =>
                {
                    condVar.Notify(key);
                    key.Dispose();
                })
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));

            using (var key = mutex.Lock())
            {
                condVar.Notify(key);
            }
        }

        [Test]
        public void AsyncConditionVariable_PulseAll_DoesNothingWithNoWaiters()
        {
            var mutex = new AsyncLock();
            var condVar = new AsyncConditionVariable();
            mutex.LockAsync()
                .Then(key =>
                {
                    condVar.NotifyAll(key);
                    key.Dispose();
                })
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));

            using (var key = mutex.Lock())
            {
                condVar.NotifyAll(key);
            }
        }

        [Test]
        public void AsyncConditionVariable_UsedWhenAnotherLockIsUsing_Throws()
        {
            var mutex1 = new AsyncLock();
            var mutex2 = new AsyncLock();
            var condVar = new AsyncConditionVariable();
            Promise.Run(async () =>
            {
                using (var key = await mutex1.LockAsync())
                {
                    await condVar.WaitAsync(key);
                }
            }, SynchronizationOption.Synchronous)
                .Forget();

            Promise.Run(async () =>
            {
                using (var key = await mutex2.LockAsync())
                {
                    Assert.Catch<System.InvalidOperationException>(() => condVar.WaitAsync(key));
                    Assert.Catch<System.InvalidOperationException>(() => condVar.Wait(key));
                    Assert.Catch<System.InvalidOperationException>(() => condVar.TryWaitAsync(key, CancelationToken.Canceled()));
                    Assert.Catch<System.InvalidOperationException>(() => condVar.TryWait(key, CancelationToken.Canceled()));
                    Assert.Catch<System.InvalidOperationException>(() => condVar.Notify(key));
                    Assert.Catch<System.InvalidOperationException>(() => condVar.NotifyAll(key));
                }

                using (var key = await mutex1.LockAsync())
                {
                    condVar.Notify(key);
                }
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AsyncConditionVariable_TryWaitAsync_AlreadyCanceled_AnotherLockWaiting_ReturnsFalse_SingleWaiter()
        {
            var mutex = new AsyncLock();
            var condVar = new AsyncConditionVariable();
            Promise.Run(async () =>
            {
                Promise notifyPromise;
                using (var key = await mutex.LockAsync())
                {
                    notifyPromise = Promise.Run(async () =>
                    {
                        using (var key2 = await mutex.LockAsync())
                        {
                            condVar.Notify(key2);
                        }
                    }, SynchronizationOption.Synchronous);

                    var success = await condVar.TryWaitAsync(key, CancelationToken.Canceled());
                    Assert.False(success);
                }
                await notifyPromise;
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AsyncConditionVariable_TryWaitAsync_AlreadyCanceled_AnotherLockWaiting_ReturnsFalse_MultipleWaiters()
        {
            var mutex = new AsyncLock();
            var condVar = new AsyncConditionVariable();
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
                            condVar.NotifyAll(key2);
                        }
                    }, SynchronizationOption.Synchronous);

                    var success = await condVar.TryWaitAsync(key, CancelationToken.Canceled());
                    Assert.False(success);
                }
                await Promise.All(firstLockPromise, secondLockPromise);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

#if !UNITY_WEBGL
        [Test]
        public void AsyncConditionVariable_Canceled_WaitReturnsFalse()
        {
            var mutex = new AsyncLock();
            var condVar = new AsyncConditionVariable();
            var cts = CancelationSource.New();
            var readyDeferred = Promise.NewDeferred();
            var waitPromise = Promise.Run(() =>
            {
                using (var key = mutex.Lock())
                {
                    readyDeferred.Resolve();
                    Assert.IsFalse(condVar.TryWait(key, cts.Token));
                }
            }, SynchronizationOption.Background, forceAsync: true);

            readyDeferred.Promise
                .Then(() => mutex.LockAsync())
                .Then(key =>
                {
                    cts.Cancel();
                    condVar.Notify(key);
                    key.Dispose();
                    return waitPromise;
                })
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
            cts.Dispose();
        }

        [Test]
        public void AsyncConditionVariable_CanceledTooLate_WaitReturnsTrue()
        {
            var mutex = new AsyncLock();
            var condVar = new AsyncConditionVariable();
            var cts = CancelationSource.New();
            var readyDeferred = Promise.NewDeferred();
            var waitPromise = Promise.Run(() =>
            {
                using (var key = mutex.Lock())
                {
                    readyDeferred.Resolve();
                    Assert.IsTrue(condVar.TryWait(key, cts.Token));
                    cts.Cancel();
                }
            }, SynchronizationOption.Background, forceAsync: true);

            readyDeferred.Promise
                .Then(() => mutex.LockAsync())
                .Then(key =>
                {
                    condVar.Notify(key);
                    key.Dispose();
                    return waitPromise;
                })
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
            cts.Dispose();
        }

        [Test]
        public void AsyncConditionVariable_CanceledOutsideLock_WaitReturnsFalse()
        {
            var mutex = new AsyncLock();
            var condVar = new AsyncConditionVariable();
            var cts = CancelationSource.New();
            var readyDeferred = Promise.NewDeferred();
            var waitPromise = Promise.Run(() =>
            {
                using (var key = mutex.Lock())
                {
                    readyDeferred.Resolve();
                    Assert.IsFalse(condVar.TryWait(key, cts.Token));
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
        public void AsyncConditionVariable_CanceledOutsideLock_WaitAsyncYieldsFalse()
        {
            var mutex = new AsyncLock();
            var condVar = new AsyncConditionVariable();
            var cts = CancelationSource.New();
            var readyDeferred = Promise.NewDeferred();
            var waitPromise = mutex.LockAsync()
                .ConfigureContinuation(new ContinuationOptions(SynchronizationOption.Background, CompletedContinuationBehavior.Asynchronous))
                .Then(key =>
                {
                    readyDeferred.Resolve();
                    return condVar.TryWaitAsync(key, cts.Token)
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
        public void AsyncConditionVariable_CanceledTooLateOutsideLock_WaitReturnsTrue()
        {
            var mutex = new AsyncLock();
            var condVar = new AsyncConditionVariable();
            var cts = CancelationSource.New();
            var readyDeferred = Promise.NewDeferred();
            var waitPromise = Promise.Run(() =>
            {
                using (var key = mutex.Lock())
                {
                    readyDeferred.Resolve();
                    Assert.IsTrue(condVar.TryWait(key, cts.Token));
                }
            }, SynchronizationOption.Background, forceAsync: true);

            readyDeferred.Promise
                .ConfigureContinuation(new ContinuationOptions(SynchronizationOption.Background, CompletedContinuationBehavior.Asynchronous))
                .Then(() => mutex.LockAsync())
                .Then(key =>
                {
                    condVar.Notify(key);
                    key.Dispose();
                    cts.Cancel();
                    return waitPromise;
                })
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
            cts.Dispose();
        }

        [Test]
        public void AsyncConditionVariable_CanceledTooLateOutsideLock_WaitAsyncYieldsTrue()
        {
            var mutex = new AsyncLock();
            var condVar = new AsyncConditionVariable();
            var cts = CancelationSource.New();
            var readyDeferred = Promise.NewDeferred();
            var waitPromise = mutex.LockAsync()
                .ConfigureContinuation(new ContinuationOptions(SynchronizationOption.Background, CompletedContinuationBehavior.Asynchronous))
                .Then(key =>
                {
                    readyDeferred.Resolve();
                    return condVar.TryWaitAsync(key, cts.Token)
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
                    condVar.Notify(key);
                    key.Dispose();
                    cts.Cancel();
                    return waitPromise;
                })
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
            cts.Dispose();
        }

        [Test]
        public void AsyncConditionVariable_Pulse_ReleasesOneSyncWaiter()
        {
            var mutex = new AsyncLock();
            var condVar = new AsyncConditionVariable();
            int completed = 0;
            var deferredReady = Promise.NewDeferred();
            var promise = Promise.Run(() =>
            {
                using (var key = mutex.Lock())
                {
                    deferredReady.Resolve();
                    condVar.Wait(key);
                    Interlocked.Increment(ref completed);
                }
            });

            deferredReady.Promise
                .ConfigureContinuation(new ContinuationOptions(SynchronizationOption.Background, CompletedContinuationBehavior.Asynchronous))
                .Then(() =>
                {
                    using (var key = mutex.Lock())
                    {
                        condVar.Notify(key);
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
        public void AsyncConditionVariable_PulseAll_ReleasesAllSyncWaiters()
        {
            var mutex = new AsyncLock();
            var condVar = new AsyncConditionVariable();
            int completed = 0;
            var deferred1Ready = Promise.NewDeferred();
            var deferred2Ready = Promise.NewDeferred();
            var promise1 = Promise.Run(() =>
            {
                using (var key = mutex.Lock())
                {
                    deferred1Ready.Resolve();
                    condVar.Wait(key);
                    Interlocked.Increment(ref completed);
                }
            });
            var promise2 = Promise.Run(() =>
            {
                using (var key = mutex.Lock())
                {
                    deferred2Ready.Resolve();
                    condVar.Wait(key);
                    Interlocked.Increment(ref completed);
                }
            });

            Promise.All(deferred1Ready.Promise, deferred2Ready.Promise)
                .ConfigureContinuation(new ContinuationOptions(SynchronizationOption.Background, CompletedContinuationBehavior.Asynchronous))
                .Then(() =>
                {
                    using (var key = mutex.Lock())
                    {
                        condVar.NotifyAll(key);
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
        public void AsyncConditionVariable_Pulse_ReleasesOneAsyncWaiter_Then()
        {
            var mutex = new AsyncLock();
            var condVar = new AsyncConditionVariable();
            int completed = 0;
            var deferred1Ready = Promise.NewDeferred();
            var deferred2Ready = Promise.NewDeferred();
            var deferred1Complete = Promise.NewDeferred();
            var deferred2Complete = Promise.NewDeferred();
            var promise1 = Promise.Run(() =>
            {
                return mutex.LockAsync()
                    .Then(key =>
                    {
                        var waitPromise = condVar.WaitAsync(key);
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
                return mutex.LockAsync()
                    .Then(key =>
                    {
                        var waitPromise = condVar.WaitAsync(key);
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
                    return mutex.LockAsync()
                        .Then(key =>
                        {
                            condVar.Notify(key);
                            key.Dispose();
                            return Promise.Race(promise1, promise2);
                        })
                        .Then(() =>
                        {
                            Assert.AreEqual(1, completed);
                            return mutex.LockAsync();
                        })
                        .Then(key =>
                        {
                            condVar.Notify(key);
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
        public void AsyncConditionVariable_PulseAll_ReleasesAllAsyncWaiters_Then()
        {
            var mutex = new AsyncLock();
            var condVar = new AsyncConditionVariable();
            int completed = 0;
            var deferred1Ready = Promise.NewDeferred();
            var deferred2Ready = Promise.NewDeferred();
            var promise1 = Promise.Run(() =>
            {
                return mutex.LockAsync()
                    .Then(key =>
                    {
                        var waitPromise = condVar.WaitAsync(key);
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
                return mutex.LockAsync()
                    .Then(key =>
                    {
                        var waitPromise = condVar.WaitAsync(key);
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
                    return mutex.LockAsync()
                        .Then(key =>
                        {
                            condVar.NotifyAll(key);
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
        public void AsyncConditionVariable_Pulse_ReleasesOneAsyncWaiter_AsyncAwait()
        {
            Promise.Run(async () =>
            {
                var mutex = new AsyncLock();
                var condVar = new AsyncConditionVariable();
                int completed = 0;
                var deferred1Ready = Promise.NewDeferred();
                var deferred2Ready = Promise.NewDeferred();
                var deferred1Complete = Promise.NewDeferred();
                var deferred2Complete = Promise.NewDeferred();
                var promise1 = Promise.Run(async () =>
                {
                    using (var key = await mutex.LockAsync())
                    {
                        var waitPromise = condVar.WaitAsync(key);
                        deferred1Ready.Resolve();
                        await waitPromise;
                        Interlocked.Increment(ref completed);
                        deferred1Complete.Resolve();
                    }
                });
                await deferred1Ready.Promise;
                var promise2 = Promise.Run(async () =>
                {
                    using (var key = await mutex.LockAsync())
                    {
                        var waitPromise = condVar.WaitAsync(key);
                        deferred2Ready.Resolve();
                        await waitPromise;
                        Interlocked.Increment(ref completed);
                        deferred2Complete.Resolve();
                    }
                });
                await deferred2Ready.Promise;

                using (var key = await mutex.LockAsync())
                {
                    condVar.Notify(key);
                }
                await Promise.Race(promise1, promise2);
                Assert.AreEqual(1, completed);

                using (var key = await mutex.LockAsync())
                {
                    condVar.Notify(key);
                }
                await Promise.All(deferred1Complete.Promise, deferred2Complete.Promise);
                Assert.AreEqual(2, completed);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AsyncConditionVariable_PulseAll_ReleasesAllAsyncWaiters_AsyncAwait()
        {
            Promise.Run(async () =>
            {
                var mutex = new AsyncLock();
                var condVar = new AsyncConditionVariable();
                int completed = 0;
                var deferred1Ready = Promise.NewDeferred();
                var deferred2Ready = Promise.NewDeferred();
                var promise1 = Promise.Run(async () =>
                {
                    using (var key = await mutex.LockAsync())
                    {
                        var waitPromise = condVar.WaitAsync(key);
                        deferred1Ready.Resolve();
                        await waitPromise;
                        Interlocked.Increment(ref completed);
                    }
                });
                await deferred1Ready.Promise;
                var promise2 = Promise.Run(async () =>
                {
                    using (var key = await mutex.LockAsync())
                    {
                        var waitPromise = condVar.WaitAsync(key);
                        deferred2Ready.Resolve();
                        await waitPromise;
                        Interlocked.Increment(ref completed);
                    }
                });
                await deferred2Ready.Promise;

                using (var key = await mutex.LockAsync())
                {
                    condVar.NotifyAll(key);
                }
                await Promise.All(promise1, promise2);

                Assert.AreEqual(2, completed);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }
#endif // !UNITY_WEBGL

        [Test]
        public void AsyncConditionVariable_WaitAsyncWithContinuationOptions_ContinuesOnConfiguredContext_Then(
            [Values] SynchronizationType continuationContext,
            [Values] CompletedContinuationBehavior completedBehavior,
            [Values(SynchronizationType.Foreground, SynchronizationType.Background)] SynchronizationType invokeContext)
        {
            var foregroundThread = Thread.CurrentThread;
            var mutex = new AsyncLock();
            var condVar = new AsyncConditionVariable();

            var promise = mutex.LockAsync(ContinuationOptions.Synchronous)
                .Then(key =>
                {
                    return condVar.WaitAsync(key, TestHelper.GetContinuationOptions(continuationContext, completedBehavior))
                        .Then(() =>
                        {
                            TestHelper.AssertCallbackContext(continuationContext, invokeContext, foregroundThread);
                        });
                });

            new ThreadHelper().ExecuteSynchronousOrOnThread(
                () =>
                {
                    using (var key = mutex.Lock())
                    {
                        condVar.Notify(key);
                    }
                },
                invokeContext == SynchronizationType.Foreground
            );
            promise.WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AsyncConditionVariable_TryWaitAsyncWithContinuationOptions_ContinuesOnConfiguredContext_Then(
            [Values] SynchronizationType continuationContext,
            [Values] CompletedContinuationBehavior completedBehavior,
            [Values(SynchronizationType.Foreground, SynchronizationType.Background)] SynchronizationType invokeContext)
        {
            var foregroundThread = Thread.CurrentThread;
            var mutex = new AsyncLock();
            var condVar = new AsyncConditionVariable();
            var cancelationSource = CancelationSource.New();

            var promise = mutex.LockAsync(ContinuationOptions.Synchronous)
                .Then(key =>
                {
                    return condVar.TryWaitAsync(key, cancelationSource.Token, TestHelper.GetContinuationOptions(continuationContext, completedBehavior))
                        .Then(success =>
                        {
                            Assert.True(success);
                            TestHelper.AssertCallbackContext(continuationContext, invokeContext, foregroundThread);
                        });
                });

            new ThreadHelper().ExecuteSynchronousOrOnThread(
                () =>
                {
                    using (var key = mutex.Lock())
                    {
                        condVar.Notify(key);
                    }
                },
                invokeContext == SynchronizationType.Foreground
            );
            promise.WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));

            cancelationSource.Dispose();
        }

        [Test]
        public void AsyncConditionVariable_WaitAsyncWithContinuationOptions_ContinuesOnConfiguredContext_await(
            [Values] SynchronizationType continuationContext,
            [Values] CompletedContinuationBehavior completedBehavior,
            [Values(SynchronizationType.Foreground, SynchronizationType.Background)] SynchronizationType invokeContext)
        {
            var foregroundThread = Thread.CurrentThread;
            var mutex = new AsyncLock();
            var condVar = new AsyncConditionVariable();

            var promise = Promise.Run(async () =>
            {
                using (var key = await mutex.LockAsync(ContinuationOptions.Synchronous))
                {
                    await condVar.WaitAsync(key, TestHelper.GetContinuationOptions(continuationContext, completedBehavior));
                    TestHelper.AssertCallbackContext(continuationContext, invokeContext, foregroundThread);
                }
            }, SynchronizationOption.Synchronous);

            new ThreadHelper().ExecuteSynchronousOrOnThread(
                () =>
                {
                    using (var key = mutex.Lock())
                    {
                        condVar.Notify(key);
                    }
                },
                invokeContext == SynchronizationType.Foreground
            );
            promise.WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AsyncConditionVariable_TryWaitAsyncWithContinuationOptions_ContinuesOnConfiguredContext_await(
            [Values] SynchronizationType continuationContext,
            [Values] CompletedContinuationBehavior completedBehavior,
            [Values(SynchronizationType.Foreground, SynchronizationType.Background)] SynchronizationType invokeContext)
        {
            var foregroundThread = Thread.CurrentThread;
            var mutex = new AsyncLock();
            var condVar = new AsyncConditionVariable();
            var cancelationSource = CancelationSource.New();

            var promise = Promise.Run(async () =>
            {
                using (var key = await mutex.LockAsync(ContinuationOptions.Synchronous))
                {
                    var success = await condVar.TryWaitAsync(key, cancelationSource.Token, TestHelper.GetContinuationOptions(continuationContext, completedBehavior));
                    Assert.True(success);
                    TestHelper.AssertCallbackContext(continuationContext, invokeContext, foregroundThread);
                }
            }, SynchronizationOption.Synchronous);

            new ThreadHelper().ExecuteSynchronousOrOnThread(
                () =>
                {
                    using (var key = mutex.Lock())
                    {
                        condVar.Notify(key);
                    }
                },
                invokeContext == SynchronizationType.Foreground
            );
            promise.WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));

            cancelationSource.Dispose();
        }

#if PROMISE_DEBUG
        [Test]
        public void AsyncConditionVariable_ReleaseLock_BeforeWaitAsyncCompletes_Throws()
        {
            var mutex = new AsyncLock();
            var condVar = new AsyncConditionVariable();
            var waitPromise = mutex.LockAsync()
                .Then(key =>
                {
                    var promise = condVar.WaitAsync(key)
                        .Then(() => key);
                    Assert.Catch<System.InvalidOperationException>(key.Dispose);
                    return promise;
                })
                .Then(key => key.Dispose());

            mutex.LockAsync()
                .Then(secondKey =>
                {
                    condVar.Notify(secondKey);
                    secondKey.Dispose();
                })
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));

            waitPromise
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }
#endif

#if PROTO_PROMISE_TEST_GC_ENABLED
        [Test]
        public void AsyncConditionVariable_AbandonedConditionVariableIsReported()
        {
            var currentRejectionHandler = Promise.Config.UncaughtRejectionHandler;
            AbandonedConditionVariableException abandonedConditionVariableException = null;
            Promise.Config.UncaughtRejectionHandler = ex =>
            {
                abandonedConditionVariableException = ex.Value as AbandonedConditionVariableException;
            };

            var mutex = new AsyncLock();
            var key = mutex.Lock();
            WaitAndAbandonConditionVariable(key);

            TestHelper.GcCollectAndWaitForFinalizers();
            TestHelper.ExecuteForegroundCallbacksAndWaitForThreadsToComplete();
            Assert.IsNotNull(abandonedConditionVariableException);
            key.Dispose();
            TestHelper.ExecuteForegroundCallbacksAndWaitForThreadsToComplete();

            Promise.Config.UncaughtRejectionHandler = currentRejectionHandler;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void WaitAndAbandonConditionVariable(AsyncLock.Key key)
        {
            new AsyncConditionVariable().WaitAsync(key).Forget();
        }
#endif // PROTO_PROMISE_TEST_GC_ENABLED
    }
#endif // UNITY_2021_2_OR_NEWER || NETSTANDARD2_1_OR_GREATER || NETCOREAPP
}