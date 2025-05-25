#if !UNITY_WEBGL

using NUnit.Framework;
using Proto.Promises;
using Proto.Timers;
using System;
using System.Collections.Generic;
using System.Threading;

namespace ProtoPromise.Tests.Concurrency
{
    public class WaitAsyncConcurrencyTests
    {
        [SetUp]
        public void Setup()
        {
            TestHelper.Setup();
        }

        [TearDown]
        public void Teardown()
        {
            TestHelper.Cleanup(spinForThreadPool: true);
        }

        public enum ContinuationType
        {
            ContinueWith,
            Await
        }

        private static IEnumerable<TestCaseData> GetArgs_CancelationToken()
        {
            var waitAsyncPlaces = new ActionPlace[]
            {
                ActionPlace.InSetup,
                ActionPlace.Parallel,
                ActionPlace.InTeardown
            };

            var continuationTypes = new ContinuationType[]
            {
                ContinuationType.ContinueWith,
                ContinuationType.Await
            };

            foreach (var waitAsyncPlace in waitAsyncPlaces)
            foreach (var continuePlace in GetContinuePlace(waitAsyncPlace))
            foreach (var continuationType in continuationTypes)
            {
                yield return new TestCaseData(waitAsyncPlace, continuePlace, true, continuationType);
                yield return new TestCaseData(waitAsyncPlace, continuePlace, false, continuationType);
            }
        }

        private static IEnumerable<ActionPlace> GetContinuePlace(ActionPlace waitAsyncPlace)
        {
            if (waitAsyncPlace == ActionPlace.InSetup)
            {
                yield return ActionPlace.InSetup;
            }
            if (waitAsyncPlace != ActionPlace.InTeardown)
            {
                yield return ActionPlace.Parallel;
            }
            yield return ActionPlace.InTeardown;
        }

        [Test, TestCaseSource(nameof(GetArgs_CancelationToken))]
        public void WaitAsync_CancelationToken_Concurrent_void(
            ActionPlace waitAsyncSubscribePlace,
            ActionPlace continuePlace,
            bool withCancelation,
            ContinuationType continuationType)
        {
            var foregroundThread = Thread.CurrentThread;
            
            var cancelationSource = default(CancelationSource);
            var cancelationToken = default(CancelationToken);
            var deferred = default(Promise.Deferred);
            var promise = default(Promise);

            bool didContinue = false;

            Action SubscribeContinuation = () =>
            {
                if (continuationType == ContinuationType.Await)
                {
                    Await().Forget();

                    async Promise Await()
                    {
                        try
                        {
                            await promise;
                        }
                        catch (OperationCanceledException) { }
                        finally
                        {
                            didContinue = true;
                        }
                    }
                }
                else
                {
                    promise
                        .ContinueWith(_ => didContinue = true)
                        .Forget();
                }
            };

            var parallelActions = new List<Action>(3)
            {
                () => deferred.Resolve()
            };

            if (withCancelation)
            {
                parallelActions.Add(() => cancelationSource.Cancel());
            }

            if (waitAsyncSubscribePlace == ActionPlace.Parallel && continuePlace == ActionPlace.Parallel)
            {
                parallelActions.Add(() =>
                {
                    promise = promise.WaitAsync(cancelationToken);
                    SubscribeContinuation();
                });
            }
            else if (waitAsyncSubscribePlace == ActionPlace.Parallel)
            {
                parallelActions.Add(() => promise = promise.WaitAsync(cancelationToken));
            }
            else if (continuePlace == ActionPlace.Parallel)
            {
                parallelActions.Add(SubscribeContinuation);
            }
                
            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                setup: () =>
                {
                    didContinue = false;
                    if (withCancelation)
                    {
                        cancelationSource = CancelationSource.New();
                        cancelationToken = cancelationSource.Token;
                    }
                    deferred = Promise.NewDeferred();
                    promise = deferred.Promise;
                    if (waitAsyncSubscribePlace == ActionPlace.InSetup)
                    {
                        promise = promise.WaitAsync(cancelationToken);
                    }
                    if (continuePlace == ActionPlace.InSetup)
                    {
                        SubscribeContinuation();
                    }
                },
                teardown: () =>
                {
                    if (waitAsyncSubscribePlace == ActionPlace.InTeardown)
                    {
                        promise = promise.WaitAsync(cancelationToken);
                    }
                    if (continuePlace == ActionPlace.InTeardown)
                    {
                        SubscribeContinuation();
                    }

                    TestHelper.ExecuteForegroundCallbacksAndWaitForThreadsToComplete();
                    TestHelper.SpinUntil(() => didContinue, TimeSpan.FromSeconds(1), $"didContinue: {didContinue}");
                    if (withCancelation)
                    {
                        cancelationSource.Dispose();
                    }
                },
                actions: parallelActions.ToArray()
            );
        }

        [Test, TestCaseSource(nameof(GetArgs_CancelationToken))]
        public void WaitAsync_CancelationToken_Concurrent_T(
            ActionPlace waitAsyncSubscribePlace,
            ActionPlace continuePlace,
            bool withCancelation,
            ContinuationType continuationType)
        {
            var foregroundThread = Thread.CurrentThread;

            var cancelationSource = default(CancelationSource);
            var cancelationToken = default(CancelationToken);
            var deferred = default(Promise<int>.Deferred);
            var promise = default(Promise<int>);

            bool didContinue = false;

            Action SubscribeContinuation = () =>
            {
                if (continuationType == ContinuationType.Await)
                {
                    Await().Forget();

                    async Promise Await()
                    {
                        try
                        {
                            _ = await promise;
                        }
                        catch (OperationCanceledException) { }
                        finally
                        {
                            didContinue = true;
                        }
                    }
                }
                else
                {
                    promise
                        .ContinueWith(_ => didContinue = true)
                        .Forget();
                }
            };

            var parallelActions = new List<Action>(3)
            {
                () => deferred.Resolve(1)
            };

            if (withCancelation)
            {
                parallelActions.Add(() => cancelationSource.Cancel());
            }

            if (waitAsyncSubscribePlace == ActionPlace.Parallel && continuePlace == ActionPlace.Parallel)
            {
                parallelActions.Add(() =>
                {
                    promise = promise.WaitAsync(cancelationToken);
                    SubscribeContinuation();
                });
            }
            else if (waitAsyncSubscribePlace == ActionPlace.Parallel)
            {
                parallelActions.Add(() => promise = promise.WaitAsync(cancelationToken));
            }
            else if (continuePlace == ActionPlace.Parallel)
            {
                parallelActions.Add(SubscribeContinuation);
            }

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                setup: () =>
                {
                    didContinue = false;
                    if (withCancelation)
                    {
                        cancelationSource = CancelationSource.New();
                        cancelationToken = cancelationSource.Token;
                    }
                    deferred = Promise<int>.NewDeferred();
                    promise = deferred.Promise;
                    if (waitAsyncSubscribePlace == ActionPlace.InSetup)
                    {
                        promise = promise.WaitAsync(cancelationToken);
                    }
                    if (continuePlace == ActionPlace.InSetup)
                    {
                        SubscribeContinuation();
                    }
                },
                teardown: () =>
                {
                    if (waitAsyncSubscribePlace == ActionPlace.InTeardown)
                    {
                        promise = promise.WaitAsync(cancelationToken);
                    }
                    if (continuePlace == ActionPlace.InTeardown)
                    {
                        SubscribeContinuation();
                    }

                    TestHelper.ExecuteForegroundCallbacksAndWaitForThreadsToComplete();
                    TestHelper.SpinUntil(() => didContinue, TimeSpan.FromSeconds(1), $"didContinue: {didContinue}");
                    if (withCancelation)
                    {
                        cancelationSource.Dispose();
                    }
                },
                actions: parallelActions.ToArray()
            );
        }

        [Test]
        public void WaitAsync_Timeout_Concurrent_void(
            [Values(0, 1, -1)] int milliseconds,
            [Values] ContinuationType continuationType)
        {
            var foregroundThread = Thread.CurrentThread;
            var timeout = TimeSpan.FromMilliseconds(milliseconds);
            
            var deferred = default(Promise.Deferred);
            bool didContinue = false;

            var parallelActions = new Action[]
            {
                () => deferred.Resolve(),
                () =>
                {
                    var promise = deferred.Promise.WaitAsync(timeout);
                    if (continuationType == ContinuationType.Await)
                    {
                        Await().Forget();

                        async Promise Await()
                        {
                            try
                            {
                                await promise;
                            }
                            catch (TimeoutException) { }
                            finally
                            {
                                didContinue = true;
                            }
                        }
                    }
                    else
                    {
                        promise
                            .ContinueWith(_ => didContinue = true)
                            .Forget();
                    }
                }
            };

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                setup: () =>
                {
                    didContinue = false;
                    deferred = Promise.NewDeferred();
                },
                teardown: () =>
                {
                    TestHelper.ExecuteForegroundCallbacksAndWaitForThreadsToComplete();
                    TestHelper.SpinUntil(() => didContinue, TimeSpan.FromSeconds(1), $"didContinue: {didContinue}");
                },
                actions: parallelActions
            );
        }

        [Test]
        public void WaitAsync_Timeout_Concurrent_T(
            [Values(0, 1, -1)] int milliseconds,
            [Values] ContinuationType continuationType)
        {
            var foregroundThread = Thread.CurrentThread;
            var timeout = TimeSpan.FromMilliseconds(milliseconds);

            var deferred = default(Promise<int>.Deferred);
            bool didContinue = false;

            var parallelActions = new Action[]
            {
                () => deferred.Resolve(1),
                () =>
                {
                    var promise = deferred.Promise.WaitAsync(timeout);
                    if (continuationType == ContinuationType.Await)
                    {
                        Await().Forget();

                        async Promise Await()
                        {
                            try
                            {
                                await promise;
                            }
                            catch (TimeoutException) { }
                            finally
                            {
                                didContinue = true;
                            }
                        }
                    }
                    else
                    {
                        promise
                            .ContinueWith(_ => didContinue = true)
                            .Forget();
                    }
                }
            };

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                setup: () =>
                {
                    didContinue = false;
                    deferred = Promise<int>.NewDeferred();
                },
                teardown: () =>
                {
                    TestHelper.ExecuteForegroundCallbacksAndWaitForThreadsToComplete();
                    TestHelper.SpinUntil(() => didContinue, TimeSpan.FromSeconds(1), $"didContinue: {didContinue}");
                },
                actions: parallelActions
            );
        }

        private static IEnumerable<TestCaseData> GetArgs_WaitAsync_TimeoutFactory()
        {
            foreach (int milliseconds in new int[] { 0, 1, -1 })
            foreach (ContinuationType continuationType in Enum.GetValues(typeof(ContinuationType)))
            foreach (FakeConcurrentTimerType fakeTimerType in Enum.GetValues(typeof(FakeConcurrentTimerType)))
            foreach (ActionPlace actionPlace in new ActionPlace[] { ActionPlace.InSetup, ActionPlace.Parallel })
            {
                // These timer types rely on the timer being created, so we skip these combinations.
                if ((fakeTimerType == FakeConcurrentTimerType.Invoke || fakeTimerType == FakeConcurrentTimerType.DisposeWhenInvoked)
                    // If the time is 0 or -1, WaitAsync skips creating the timer.
                    // If WaitAsync is called in parallel with cancelations and deferred, the timer might not be created.
                    && (milliseconds < 1 || actionPlace == ActionPlace.Parallel))
                {
                    continue;
                }
                yield return new TestCaseData(milliseconds, continuationType, fakeTimerType, actionPlace);
            }
        }

        [Test, TestCaseSource(nameof(GetArgs_WaitAsync_TimeoutFactory))]
        public void WaitAsync_TimeoutFactory_Concurrent_void(
            int milliseconds,
            ContinuationType continuationType,
            FakeConcurrentTimerType fakeTimerType,
            ActionPlace actionPlace)
        {
            var foregroundThread = Thread.CurrentThread;
            var timeout = TimeSpan.FromMilliseconds(milliseconds);
            var fakeTimerFactory = FakeConcurrentTimerFactory.Create(fakeTimerType);

            var deferred = default(Promise.Deferred);
            bool didContinue = false;

            void AwaitPromise()
            {
                var promise = deferred.Promise.WaitAsync(timeout, fakeTimerFactory);
                if (continuationType == ContinuationType.Await)
                {
                    Await().Forget();

                    async Promise Await()
                    {
                        try
                        {
                            await promise;
                        }
                        catch (TimeoutException) { }
                        catch (OperationCanceledException) { }
                        finally
                        {
                            didContinue = true;
                        }
                    }
                }
                else
                {
                    promise
                        .ContinueWith(_ => didContinue = true)
                        .Forget();
                }
            }

            var parallelActions = new List<Action>(3)
            {
                () => deferred.Resolve(),
                fakeTimerFactory.Invoke
            };

            if (actionPlace == ActionPlace.Parallel)
            {
                parallelActions.Add(AwaitPromise);
            }

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                setup: () =>
                {
                    didContinue = false;
                    deferred = Promise.NewDeferred();
                    if (actionPlace == ActionPlace.InSetup)
                    {
                        AwaitPromise();
                    }
                },
                teardown: () =>
                {
                    TestHelper.ExecuteForegroundCallbacksAndWaitForThreadsToComplete();
                    TestHelper.SpinUntil(() => didContinue, TimeSpan.FromSeconds(1), $"didContinue: {didContinue}");
                },
                actions: parallelActions.ToArray()
            );
        }

        [Test, TestCaseSource(nameof(GetArgs_WaitAsync_TimeoutFactory))]
        public void WaitAsync_TimeoutFactory_Concurrent_T(
            int milliseconds,
            ContinuationType continuationType,
            FakeConcurrentTimerType fakeTimerType,
            ActionPlace actionPlace)
        {
            var foregroundThread = Thread.CurrentThread;
            var timeout = TimeSpan.FromMilliseconds(milliseconds);
            var fakeTimerFactory = FakeConcurrentTimerFactory.Create(fakeTimerType);

            var deferred = default(Promise<int>.Deferred);
            bool didContinue = false;

            void AwaitPromise()
            {
                var promise = deferred.Promise.WaitAsync(timeout, fakeTimerFactory);
                if (continuationType == ContinuationType.Await)
                {
                    Await().Forget();

                    async Promise Await()
                    {
                        try
                        {
                            await promise;
                        }
                        catch (TimeoutException) { }
                        catch (OperationCanceledException) { }
                        finally
                        {
                            didContinue = true;
                        }
                    }
                }
                else
                {
                    promise
                        .ContinueWith(_ => didContinue = true)
                        .Forget();
                }
            }

            var parallelActions = new List<Action>(3)
            {
                () => deferred.Resolve(1),
                fakeTimerFactory.Invoke
            };

            if (actionPlace == ActionPlace.Parallel)
            {
                parallelActions.Add(AwaitPromise);
            }

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                setup: () =>
                {
                    didContinue = false;
                    deferred = Promise<int>.NewDeferred();
                    if (actionPlace == ActionPlace.InSetup)
                    {
                        AwaitPromise();
                    }
                },
                teardown: () =>
                {
                    TestHelper.ExecuteForegroundCallbacksAndWaitForThreadsToComplete();
                    TestHelper.SpinUntil(() => didContinue, TimeSpan.FromSeconds(1), $"didContinue: {didContinue}");
                },
                actions: parallelActions.ToArray()
            );
        }

        [Test]
        public void WaitAsync_Timeout_CancelationToken_Concurrent_void(
            [Values(0, 1, -1)] int milliseconds,
            [Values] bool withCancelation,
            [Values] ContinuationType continuationType)
        {
            var foregroundThread = Thread.CurrentThread;
            var timeout = TimeSpan.FromMilliseconds(milliseconds);

            var cancelationSource = default(CancelationSource);
            var cancelationToken = default(CancelationToken);
            var deferred = default(Promise.Deferred);
            bool didContinue = false;

            var parallelActions = new List<Action>(3)
            {
                () => deferred.Resolve(),
                () =>
                {
                    var promise = deferred.Promise.WaitAsync(timeout, cancelationToken);
                    if (continuationType == ContinuationType.Await)
                    {
                        Await().Forget();

                        async Promise Await()
                        {
                            try
                            {
                                await promise;
                            }
                            catch (TimeoutException) { }
                            catch (OperationCanceledException) { }
                            finally
                            {
                                didContinue = true;
                            }
                        }
                    }
                    else
                    {
                        promise
                            .ContinueWith(_ => didContinue = true)
                            .Forget();
                    }
                }
            };

            if (withCancelation)
            {
                parallelActions.Add(() => cancelationSource.Cancel());
            }

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                setup: () =>
                {
                    didContinue = false;
                    if (withCancelation)
                    {
                        cancelationSource = CancelationSource.New();
                        cancelationToken = cancelationSource.Token;
                    }
                    deferred = Promise.NewDeferred();
                },
                teardown: () =>
                {
                    TestHelper.ExecuteForegroundCallbacksAndWaitForThreadsToComplete();
                    TestHelper.SpinUntil(() => didContinue, TimeSpan.FromSeconds(1), $"didContinue: {didContinue}");
                    if (withCancelation)
                    {
                        cancelationSource.Dispose();
                    }
                },
                actions: parallelActions.ToArray()
            );
        }

        [Test]
        public void WaitAsync_Timeout_CancelationToken_Concurrent_T(
            [Values(0, 1, -1)] int milliseconds,
            [Values] bool withCancelation,
            [Values] ContinuationType continuationType)
        {
            var foregroundThread = Thread.CurrentThread;
            var timeout = TimeSpan.FromMilliseconds(milliseconds);

            var cancelationSource = default(CancelationSource);
            var cancelationToken = default(CancelationToken);
            var deferred = default(Promise<int>.Deferred);
            bool didContinue = false;

            var parallelActions = new List<Action>(3)
            {
                () => deferred.Resolve(1),
                () =>
                {
                    var promise = deferred.Promise.WaitAsync(timeout, cancelationToken);
                    if (continuationType == ContinuationType.Await)
                    {
                        Await().Forget();

                        async Promise Await()
                        {
                            try
                            {
                                await promise;
                            }
                            catch (TimeoutException) { }
                            catch (OperationCanceledException) { }
                            finally
                            {
                                didContinue = true;
                            }
                        }
                    }
                    else
                    {
                        promise
                            .ContinueWith(_ => didContinue = true)
                            .Forget();
                    }
                }
            };

            if (withCancelation)
            {
                parallelActions.Add(() => cancelationSource.Cancel());
            }

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                setup: () =>
                {
                    didContinue = false;
                    if (withCancelation)
                    {
                        cancelationSource = CancelationSource.New();
                        cancelationToken = cancelationSource.Token;
                    }
                    deferred = Promise<int>.NewDeferred();
                },
                teardown: () =>
                {
                    TestHelper.ExecuteForegroundCallbacksAndWaitForThreadsToComplete();
                    TestHelper.SpinUntil(() => didContinue, TimeSpan.FromSeconds(1), $"didContinue: {didContinue}");
                    if (withCancelation)
                    {
                        cancelationSource.Dispose();
                    }
                },
                actions: parallelActions.ToArray()
            );
        }

        private static IEnumerable<TestCaseData> GetArgs_WaitAsync_TimeoutFactory_CancelationToken()
        {
            foreach (int milliseconds in new int[] { 0, 1, -1 })
            foreach (bool withCancelation in new bool[] { true, false })
            foreach (ContinuationType continuationType in Enum.GetValues(typeof(ContinuationType)))
            foreach (FakeConcurrentTimerType fakeTimerType in Enum.GetValues(typeof(FakeConcurrentTimerType)))
            foreach (ActionPlace actionPlace in new ActionPlace[] { ActionPlace.InSetup, ActionPlace.Parallel })
            {
                // These timer types rely on the timer being created, so we skip these combinations.
                if ((fakeTimerType == FakeConcurrentTimerType.Invoke || fakeTimerType == FakeConcurrentTimerType.DisposeWhenInvoked)
                    // If the time is 0 or -1, WaitAsync skips creating the timer.
                    // If WaitAsync is called in parallel with cancelations and deferred, the timer might not be created.
                    && (milliseconds < 1 || actionPlace == ActionPlace.Parallel))
                {
                    continue;
                }
                yield return new TestCaseData(milliseconds, withCancelation, continuationType, fakeTimerType, actionPlace);
            }
        }

        [Test, TestCaseSource(nameof(GetArgs_WaitAsync_TimeoutFactory_CancelationToken))]
        public void WaitAsync_TimeoutFactory_CancelationToken_Concurrent_void(
            int milliseconds,
            bool withCancelation,
            ContinuationType continuationType,
            FakeConcurrentTimerType fakeTimerType,
            ActionPlace actionPlace)
        {
            var foregroundThread = Thread.CurrentThread;
            var timeout = TimeSpan.FromMilliseconds(milliseconds);
            var fakeTimerFactory = FakeConcurrentTimerFactory.Create(fakeTimerType);

            var cancelationSource = default(CancelationSource);
            var cancelationToken = default(CancelationToken);
            var deferred = default(Promise.Deferred);
            bool didContinue = false;

            void AwaitPromise()
            {
                var promise = deferred.Promise.WaitAsync(timeout, fakeTimerFactory, cancelationToken);
                if (continuationType == ContinuationType.Await)
                {
                    Await().Forget();

                    async Promise Await()
                    {
                        try
                        {
                            await promise;
                        }
                        catch (TimeoutException) { }
                        catch (OperationCanceledException) { }
                        finally
                        {
                            didContinue = true;
                        }
                    }
                }
                else
                {
                    promise
                        .ContinueWith(_ => didContinue = true)
                        .Forget();
                }
            }


            var parallelActions = new List<Action>(4)
            {
                () => deferred.Resolve(),
                fakeTimerFactory.Invoke
            };

            if (actionPlace == ActionPlace.Parallel)
            {
                parallelActions.Add(AwaitPromise);
            }
            if (withCancelation)
            {
                parallelActions.Add(() => cancelationSource.Cancel());
            }

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                setup: () =>
                {
                    didContinue = false;
                    if (withCancelation)
                    {
                        cancelationSource = CancelationSource.New();
                        cancelationToken = cancelationSource.Token;
                    }
                    deferred = Promise.NewDeferred();
                    if (actionPlace == ActionPlace.InSetup)
                    {
                        AwaitPromise();
                    }
                },
                teardown: () =>
                {
                    TestHelper.ExecuteForegroundCallbacksAndWaitForThreadsToComplete();
                    TestHelper.SpinUntil(() => didContinue, TimeSpan.FromSeconds(1), $"didContinue: {didContinue}");
                    if (withCancelation)
                    {
                        cancelationSource.Dispose();
                    }
                },
                actions: parallelActions.ToArray()
            );
        }

        [Test, TestCaseSource(nameof(GetArgs_WaitAsync_TimeoutFactory_CancelationToken))]
        public void WaitAsync_TimeoutFactory_CancelationToken_Concurrent_T(
            int milliseconds,
            bool withCancelation,
            ContinuationType continuationType,
            FakeConcurrentTimerType fakeTimerType,
            ActionPlace actionPlace)
        {
            var foregroundThread = Thread.CurrentThread;
            var timeout = TimeSpan.FromMilliseconds(milliseconds);
            var fakeTimerFactory = FakeConcurrentTimerFactory.Create(fakeTimerType);

            var cancelationSource = default(CancelationSource);
            var cancelationToken = default(CancelationToken);
            var deferred = default(Promise<int>.Deferred);
            bool didContinue = false;

            void AwaitPromise()
            {
                var promise = deferred.Promise.WaitAsync(timeout, fakeTimerFactory, cancelationToken);
                if (continuationType == ContinuationType.Await)
                {
                    Await().Forget();

                    async Promise Await()
                    {
                        try
                        {
                            await promise;
                        }
                        catch (TimeoutException) { }
                        catch (OperationCanceledException) { }
                        finally
                        {
                            didContinue = true;
                        }
                    }
                }
                else
                {
                    promise
                        .ContinueWith(_ => didContinue = true)
                        .Forget();
                }
            }


            var parallelActions = new List<Action>(4)
            {
                () => deferred.Resolve(1),
                fakeTimerFactory.Invoke
            };

            if (actionPlace == ActionPlace.Parallel)
            {
                parallelActions.Add(AwaitPromise);
            }
            if (withCancelation)
            {
                parallelActions.Add(() => cancelationSource.Cancel());
            }

            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(false,
                setup: () =>
                {
                    didContinue = false;
                    if (withCancelation)
                    {
                        cancelationSource = CancelationSource.New();
                        cancelationToken = cancelationSource.Token;
                    }
                    deferred = Promise<int>.NewDeferred();
                    if (actionPlace == ActionPlace.InSetup)
                    {
                        AwaitPromise();
                    }
                },
                teardown: () =>
                {
                    TestHelper.ExecuteForegroundCallbacksAndWaitForThreadsToComplete();
                    TestHelper.SpinUntil(() => didContinue, TimeSpan.FromSeconds(1), $"didContinue: {didContinue}");
                    if (withCancelation)
                    {
                        cancelationSource.Dispose();
                    }
                },
                actions: parallelActions.ToArray()
            );
        }
    }
}

#endif // !UNITY_WEBGL