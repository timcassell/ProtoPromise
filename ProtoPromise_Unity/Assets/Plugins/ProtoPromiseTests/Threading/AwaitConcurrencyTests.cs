#if !UNITY_WEBGL && CSHARP_7_3_OR_NEWER

#if !PROTO_PROMISE_PROGRESS_DISABLE
#define PROMISE_PROGRESS
#else
#undef PROMISE_PROGRESS
#endif

using NUnit.Framework;
using Proto.Promises;
using System;
using System.Threading;

namespace ProtoPromiseTests.Threading
{
    public class AwaitConcurrencyTests
    {
        const string rejectValue = "Fail";

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
        public void PreservedPromiseMayBeAwaitedConcurrently_PendingThenComplete_void(
            [Values] CompleteType completeType)
        {
            int invokedCount = 0;
            var deferred = TestHelper.GetNewDeferredVoid(completeType, out var cancelationSource);
            var promise = deferred.Promise.Preserve();

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(
                () =>
                {
                    Await();

                    async void Await()
                    {
                        try
                        {
                            await promise;
                            Interlocked.Increment(ref invokedCount);
                        }
                        catch (UnhandledException)
                        {
                            Interlocked.Increment(ref invokedCount);
                        }
                        catch (CanceledException)
                        {
                            Interlocked.Increment(ref invokedCount);
                        }
                    }
                }
            );

            promise.Forget();
            TestHelper.GetTryCompleterVoid(completeType, rejectValue).Invoke(deferred, cancelationSource);
            cancelationSource.TryDispose();
            Assert.AreEqual(ThreadHelper.multiExecutionCount, invokedCount);
        }

        [Test]
        public void PreservedPromiseMayBeAwaitedConcurrently_AlreadyComplete_void(
            [Values(CompleteType.Resolve, CompleteType.Reject, CompleteType.Cancel)] CompleteType completeType)
        {
            int invokedCount = 0;
            var promise = completeType == CompleteType.Resolve
                ? Promise.Resolved().Preserve()
                : completeType == CompleteType.Reject
                ? Promise.Rejected(rejectValue).Preserve()
                : Promise.Canceled().Preserve();

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(
                () =>
                {
                    Await();

                    async void Await()
                    {
                        try
                        {
                            await promise;
                            Interlocked.Increment(ref invokedCount);
                        }
                        catch (UnhandledException)
                        {
                            Interlocked.Increment(ref invokedCount);
                        }
                        catch (CanceledException)
                        {
                            Interlocked.Increment(ref invokedCount);
                        }
                    }
                }
            );

            promise.Forget();
            Assert.AreEqual(ThreadHelper.multiExecutionCount, invokedCount);
        }

        [Test]
        public void PreservedPromiseMayBeAwaitedConcurrently_PendingThenComplete_T(
            [Values] CompleteType completeType)
        {
            int invokedCount = 0;
            var deferred = TestHelper.GetNewDeferredT<int>(completeType, out var cancelationSource);
            var promise = deferred.Promise.Preserve();

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(
                () =>
                {
                    Await();

                    async void Await()
                    {
                        try
                        {
                            await promise;
                            Interlocked.Increment(ref invokedCount);
                        }
                        catch (UnhandledException)
                        {
                            Interlocked.Increment(ref invokedCount);
                        }
                        catch (CanceledException)
                        {
                            Interlocked.Increment(ref invokedCount);
                        }
                    }
                }
            );

            promise.Forget();
            TestHelper.GetTryCompleterT(completeType, 1, rejectValue).Invoke(deferred, cancelationSource);
            cancelationSource.TryDispose();
            Assert.AreEqual(ThreadHelper.multiExecutionCount, invokedCount);
        }

        [Test]
        public void PreservedPromiseMayBeAwaitedConcurrently_AlreadyComplete_T(
            [Values(CompleteType.Resolve, CompleteType.Reject, CompleteType.Cancel)] CompleteType completeType)
        {
            int invokedCount = 0;
            var promise = completeType == CompleteType.Resolve
                ? Promise<int>.Resolved(1).Preserve()
                : completeType == CompleteType.Reject
                ? Promise<int>.Rejected(rejectValue).Preserve()
                : Promise<int>.Canceled().Preserve();

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(
                () =>
                {
                    Await();

                    async void Await()
                    {
                        try
                        {
                            await promise;
                            Interlocked.Increment(ref invokedCount);
                        }
                        catch (UnhandledException)
                        {
                            Interlocked.Increment(ref invokedCount);
                        }
                        catch (CanceledException)
                        {
                            Interlocked.Increment(ref invokedCount);
                        }
                    }
                }
            );

            promise.Forget();
            Assert.AreEqual(ThreadHelper.multiExecutionCount, invokedCount);
        }

        [Test]
        public void PromiseMayBeCompletedAndAwaitedConcurrently_void(
            [Values] CompleteType completeType)
        {
            var deferred = default(Promise.Deferred);
            var promise = default(Promise);
            var cancelationSource = default(CancelationSource);
            var tryCompleter = TestHelper.GetTryCompleterVoid(completeType, rejectValue);

            Promise.State result = Promise.State.Pending;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsetsAndSetup(
                setup: () =>
                {
                    result = Promise.State.Pending;
                    deferred = TestHelper.GetNewDeferredVoid(completeType, out cancelationSource);
                    promise = deferred.Promise;
                },
                parallelActionsSetup: new Action<Action>[]
                {
                    barrierAction =>
                    {
                        Await();

                        async void Await()
                        {
                            try
                            {
                                barrierAction.Invoke();
                                await promise;
                                result = Promise.State.Resolved;
                            }
                            catch (UnhandledException)
                            {
                                result = Promise.State.Rejected;
                            }
                            catch (CanceledException)
                            {
                                result = Promise.State.Canceled;
                            }
                        }
                    }
                },
                parallelActions: new Action[]
                {
                    () => tryCompleter(deferred, cancelationSource)
                },
                teardown: () =>
                {
                    cancelationSource.TryDispose();

                    Assert.AreNotEqual(Promise.State.Pending, result);
                    switch (completeType)
                    {
                        case CompleteType.Resolve:
                            Assert.AreEqual(Promise.State.Resolved, result);
                            break;
                        case CompleteType.Reject:
                            Assert.AreEqual(Promise.State.Rejected, result);
                            break;
                        case CompleteType.Cancel:
                        case CompleteType.CancelFromToken:
                            Assert.AreEqual(Promise.State.Canceled, result);
                            break;
                    }
                }
            );
        }

        [Test]
        public void PromiseMayBeCompletedAndAwaitedConcurrently_T(
            [Values] CompleteType completeType)
        {
            var deferred = default(Promise<int>.Deferred);
            var promise = default(Promise<int>);
            var cancelationSource = default(CancelationSource);
            var tryCompleter = TestHelper.GetTryCompleterT(completeType, 1, rejectValue);

            Promise.State result = Promise.State.Pending;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsetsAndSetup(
                setup: () =>
                {
                    result = Promise.State.Pending;
                    deferred = TestHelper.GetNewDeferredT<int>(completeType, out cancelationSource);
                    promise = deferred.Promise;
                },
                parallelActionsSetup: new Action<Action>[]
                {
                    barrierAction =>
                    {
                        Await();

                        async void Await()
                        {
                            try
                            {
                                barrierAction.Invoke();
                                await promise;
                                result = Promise.State.Resolved;
                            }
                            catch (UnhandledException)
                            {
                                result = Promise.State.Rejected;
                            }
                            catch (CanceledException)
                            {
                                result = Promise.State.Canceled;
                            }
                        }
                    }
                },
                parallelActions: new Action[]
                {
                    () => tryCompleter(deferred, cancelationSource)
                },
                teardown: () =>
                {
                    cancelationSource.TryDispose();

                    Assert.AreNotEqual(Promise.State.Pending, result);
                    switch (completeType)
                    {
                        case CompleteType.Resolve:
                            Assert.AreEqual(Promise.State.Resolved, result);
                            break;
                        case CompleteType.Reject:
                            Assert.AreEqual(Promise.State.Rejected, result);
                            break;
                        case CompleteType.Cancel:
                        case CompleteType.CancelFromToken:
                            Assert.AreEqual(Promise.State.Canceled, result);
                            break;
                    }
                }
            );
        }
    }
}

#endif // !UNITY_WEBGL && CSHARP_7_3_OR_NEWER