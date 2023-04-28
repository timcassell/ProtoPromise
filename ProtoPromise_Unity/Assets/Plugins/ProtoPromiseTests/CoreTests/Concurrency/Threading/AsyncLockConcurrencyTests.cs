#if !UNITY_WEBGL

using NUnit.Framework;
using Proto.Promises;
using Proto.Promises.Threading;
using System;
using System.Threading;

namespace ProtoPromiseTests.Concurrency.Threading
{
#if UNITY_2021_2_OR_NEWER || NETSTANDARD2_1_OR_GREATER || NETCOREAPP
    public class AsyncLockConcurrencyTests
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
        public void AsyncLock_EnteredConcurrenctly_OnlyAllowsSingleLocker(
            [Values] bool delayCancel,
            [Values] bool tryFirst,
            [Values] bool trySecond)
        {
            var mutex = new AsyncLock();
            var cancelationSource = default(CancelationSource);
            int enteredCount = 0;
            int exitedCount = 0;
            int expectedInvokes = ThreadHelper.GetExpandCount(4) * 4;
            Action<bool> syncLockAction = observeCancelation =>
            {
                try
                {
                    using (observeCancelation ? mutex.Lock(cancelationSource.Token) : mutex.Lock())
                    {
                        Assert.AreEqual(1, Interlocked.Increment(ref enteredCount));
                        Thread.Sleep(10);
                        Assert.AreEqual(0, Interlocked.Decrement(ref enteredCount));
                    }
                }
                catch (CanceledException) { }
                finally
                {
                    Interlocked.Increment(ref exitedCount);
                }
            };
            Action<bool> asyncLockAction = observeCancelation =>
            {
                (observeCancelation ? mutex.LockAsync(cancelationSource.Token) : mutex.LockAsync())
                    .Then(key =>
                    {
                        Assert.AreEqual(1, Interlocked.Increment(ref enteredCount));
                        Thread.Sleep(10);
                        Assert.AreEqual(0, Interlocked.Decrement(ref enteredCount));
                        key.Dispose();
                    })
                    .Finally(() =>
                    {
                        Interlocked.Increment(ref exitedCount);
                    })
                    .Forget();
            };
            Action<bool> syncTryLockAction = observeCancelation =>
            {
                bool lockTaken = observeCancelation
                    ? AsyncMonitor.TryEnter(mutex, out var key)
                    : AsyncMonitor.TryEnter(mutex, out key, cancelationSource.Token);
                if (lockTaken)
                {
                    using (key)
                    {
                        Assert.AreEqual(1, Interlocked.Increment(ref enteredCount));
                        Thread.Sleep(10);
                        Assert.AreEqual(0, Interlocked.Decrement(ref enteredCount));
                    }
                }
                Interlocked.Increment(ref exitedCount);
            };
            Action<bool> asyncTryLockAction = observeCancelation =>
            {
                (AsyncMonitor.TryEnterAsync(mutex, observeCancelation ? cancelationSource.Token : CancelationToken.None))
                    .Then(tuple =>
                    {
                        if (tuple.didEnter)
                        {
                            using (tuple.key)
                            {
                                Assert.AreEqual(1, Interlocked.Increment(ref enteredCount));
                                Thread.Sleep(10);
                                Assert.AreEqual(0, Interlocked.Decrement(ref enteredCount));
                            }
                        }
                        Interlocked.Increment(ref exitedCount);
                    })
                    .Forget();
            };

            new ThreadHelper().ExecuteParallelActionsWithOffsets(false,
                // setup
                () =>
                {
                    exitedCount = 0;
                    cancelationSource = CancelationSource.New();
                },
                // teardown
                () =>
                {
                    while (exitedCount < expectedInvokes)
                    {
                        TestHelper.ExecuteForegroundCallbacksAndWaitForThreadsToComplete();
                    }
                    cancelationSource.Dispose();
                },
                // parallel actions
                tryFirst ? () => syncTryLockAction(false) : () => syncLockAction(false),
                trySecond ? () => syncTryLockAction(true) : () => syncLockAction(true),
                tryFirst ? () => asyncTryLockAction(false) : () => asyncLockAction(false),
                trySecond ? () => asyncTryLockAction(true) : () => asyncLockAction(true),
                () =>
                {
                    if (delayCancel)
                    {
                        Thread.Sleep(10);
                    }
                    cancelationSource.Cancel();
                });
        }

        [Test]
        public void AsyncLock_OnlyAllowsSingleLocker_WithWait([Values] bool delayCancel)
        {
            var mutex = new AsyncLock();
            var cancelationSource = default(CancelationSource);
            int enteredCount = 0;
            int exitedCount = 0;
            int expectedInvokes = ThreadHelper.GetExpandCount(4) * 4;
            Action<bool> syncLockAction = observeCancelation =>
            {
                using (var key = mutex.Lock())
                {
                    Assert.AreEqual(1, Interlocked.Increment(ref enteredCount));
                    Thread.Sleep(10);
                    Assert.AreEqual(0, Interlocked.Decrement(ref enteredCount));

                    if (observeCancelation)
                    {
                        AsyncMonitor.TryWait(key, cancelationSource.Token);
                    }
                    else
                    {
                        AsyncMonitor.Wait(key);
                    }

                    Assert.AreEqual(1, Interlocked.Increment(ref enteredCount));
                    Thread.Sleep(10);
                    Assert.AreEqual(0, Interlocked.Decrement(ref enteredCount));
                }
                Interlocked.Increment(ref exitedCount);
            };
            Action<bool> asyncLockAction = observeCancelation =>
            {
                Promise.Run(async () =>
                {
                    using (var key = await mutex.LockAsync())
                    {
                        Assert.AreEqual(1, Interlocked.Increment(ref enteredCount));
                        Thread.Sleep(10);
                        Assert.AreEqual(0, Interlocked.Decrement(ref enteredCount));

                        if (observeCancelation)
                        {
                            await AsyncMonitor.TryWaitAsync(key, cancelationSource.Token);
                        }
                        else
                        {
                            await AsyncMonitor.WaitAsync(key);
                        }

                        Assert.AreEqual(1, Interlocked.Increment(ref enteredCount));
                        Thread.Sleep(10);
                        Assert.AreEqual(0, Interlocked.Decrement(ref enteredCount));
                    }
                    Interlocked.Increment(ref exitedCount);
                }, SynchronizationOption.Synchronous)
                    .Forget();
            };

            new ThreadHelper().ExecuteParallelActionsWithOffsets(false,
                // setup
                () =>
                {
                    exitedCount = 0;
                    cancelationSource = CancelationSource.New();
                },
                // teardown
                () =>
                {
                    cancelationSource.Dispose();
                },
                // parallel actions
                () => syncLockAction(false),
                () => syncLockAction(true),
                () => asyncLockAction(false),
                () => asyncLockAction(true),
                () =>
                {
                    if (!delayCancel)
                    {
                        cancelationSource.Cancel();
                    }
                    // We pulse until all actions are complete.
                    while (exitedCount < expectedInvokes)
                    {
                        using (var key = mutex.Lock())
                        {
                            AsyncMonitor.Pulse(key);
                        }
                        if (delayCancel && exitedCount != 0)
                        {
                            cancelationSource.TryCancel();
                        }
                    }
                });
        }
    }
#endif // UNITY_2021_2_OR_NEWER || NETSTANDARD2_1_OR_GREATER || NETCOREAPP
}

#endif // !UNITY_WEBGL