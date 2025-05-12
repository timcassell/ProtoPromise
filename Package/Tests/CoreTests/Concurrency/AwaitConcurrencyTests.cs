#if !UNITY_WEBGL

#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using NUnit.Framework;
using Proto.Promises;
using System;
using System.Threading;

namespace ProtoPromise.Tests.Concurrency
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
            var deferred = Promise.NewDeferred();
            var promiseRetainer = deferred.Promise.GetRetainer();

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(
                () =>
                {
                    Await().Forget();

                    async Promise Await()
                    {
                        try
                        {
                            await promiseRetainer;
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

            promiseRetainer.Dispose();
            TestHelper.GetCompleterVoid(completeType, rejectValue).Invoke(deferred);
            Assert.AreEqual(ThreadHelper.multiExecutionCount, invokedCount);
        }

        [Test]
        public void PreservedPromiseMayBeAwaitedConcurrently_AlreadyComplete_void(
            [Values] CompleteType completeType)
        {
            int invokedCount = 0;
            var promiseRetainer = completeType == CompleteType.Resolve
                ? Promise.Resolved().GetRetainer()
                : completeType == CompleteType.Reject
                ? Promise.Rejected(rejectValue).GetRetainer()
                : Promise.Canceled().GetRetainer();

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(
                () =>
                {
                    Await().Forget();

                    async Promise Await()
                    {
                        try
                        {
                            await promiseRetainer;
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

            promiseRetainer.Dispose();
            Assert.AreEqual(ThreadHelper.multiExecutionCount, invokedCount);
        }

        [Test]
        public void PreservedPromiseMayBeAwaitedConcurrently_PendingThenComplete_T(
            [Values] CompleteType completeType)
        {
            int invokedCount = 0;
            var deferred = Promise<int>.NewDeferred();
            var promiseRetainer = deferred.Promise.GetRetainer();

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(
                () =>
                {
                    Await();

                    async void Await()
                    {
                        try
                        {
                            await promiseRetainer;
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

            promiseRetainer.Dispose();
            TestHelper.GetCompleterT(completeType, 1, rejectValue).Invoke(deferred);
            Assert.AreEqual(ThreadHelper.multiExecutionCount, invokedCount);
        }

        [Test]
        public void PreservedPromiseMayBeAwaitedConcurrently_AlreadyComplete_T(
            [Values] CompleteType completeType)
        {
            int invokedCount = 0;
            var promiseRetainer = completeType == CompleteType.Resolve
                ? Promise<int>.Resolved(1).GetRetainer()
                : completeType == CompleteType.Reject
                ? Promise<int>.Rejected(rejectValue).GetRetainer()
                : Promise<int>.Canceled().GetRetainer();

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteMultiActionParallel(
                () =>
                {
                    Await();

                    async void Await()
                    {
                        try
                        {
                            await promiseRetainer;
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

            promiseRetainer.Dispose();
            Assert.AreEqual(ThreadHelper.multiExecutionCount, invokedCount);
        }

        [Test]
        public void PromiseMayBeCompletedAndAwaitedConcurrently_void(
            [Values] CompleteType completeType)
        {
            var deferred = default(Promise.Deferred);
            var promise = default(Promise);
            var completer = TestHelper.GetCompleterVoid(completeType, rejectValue);

            Promise.State result = Promise.State.Pending;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsetsAndSetup(
                setup: () =>
                {
                    result = Promise.State.Pending;
                    deferred = Promise.NewDeferred();
                    promise = deferred.Promise;
                },
                parallelActionsSetup: new Action<Action>[]
                {
                    barrierAction =>
                    {
                        Await().Forget();

                        async Promise Await()
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
                    () => completer(deferred)
                },
                teardown: () =>
                {
                    Assert.AreNotEqual(Promise.State.Pending, result);
                    switch (completeType)
                    {
                        case CompleteType.Resolve:
                            Assert.AreEqual(Promise.State.Resolved, result);
                            break;
                        case CompleteType.Reject:
                            Assert.AreEqual(Promise.State.Rejected, result);
                            break;
                        default:
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
            var completer = TestHelper.GetCompleterT(completeType, 1, rejectValue);

            Promise.State result = Promise.State.Pending;

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsetsAndSetup(
                setup: () =>
                {
                    result = Promise.State.Pending;
                    deferred = Promise<int>.NewDeferred();
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
                    () => completer(deferred)
                },
                teardown: () =>
                {
                    Assert.AreNotEqual(Promise.State.Pending, result);
                    switch (completeType)
                    {
                        case CompleteType.Resolve:
                            Assert.AreEqual(Promise.State.Resolved, result);
                            break;
                        case CompleteType.Reject:
                            Assert.AreEqual(Promise.State.Rejected, result);
                            break;
                        default:
                            Assert.AreEqual(Promise.State.Canceled, result);
                            break;
                    }
                }
            );
        }
    }
}

#endif // !UNITY_WEBGL