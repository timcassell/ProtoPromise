#if !UNITY_WEBGL

using NUnit.Framework;
using Proto.Promises;
using System;
using System.Collections.Generic;
using System.Threading;

namespace ProtoPromiseTests.Concurrency
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
            TestHelper.Cleanup();
        }

        public enum ContinuationType
        {
            ContinueWith,
            Await
        }

        private static IEnumerable<TestCaseData> GetArgs()
        {
            var waitTypes = new ConfigureAwaitType[]
            {
                ConfigureAwaitType.None,
                ConfigureAwaitType.Synchronous,
                ConfigureAwaitType.Foreground,
#if !UNITY_WEBGL
                ConfigureAwaitType.Background,
#endif
                // ConfigureAwaitType.Explicit // Skip explicit to reduce number of tests.
            };
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

            foreach (var waitType in waitTypes)
            foreach (var waitAsyncPlace in waitAsyncPlaces)
            foreach (var continuePlace in GetContinuePlace(waitAsyncPlace))
            foreach (var continuationType in continuationTypes)
            {
                yield return new TestCaseData(waitType, waitAsyncPlace, continuePlace, true, continuationType);
                yield return new TestCaseData(waitType, waitAsyncPlace, continuePlace, false, continuationType);
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

        private readonly TimeSpan timeout = TimeSpan.FromSeconds(2);

        [Test, TestCaseSource("GetArgs")]
        public void WaitAsyncContinuationWillBeInvokedOnTheCorrectContext_Concurrent_void(
            ConfigureAwaitType waitType,
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
                            // None and Synchronous don't care about the continuation context.
                            if (waitType != ConfigureAwaitType.None && waitType != ConfigureAwaitType.Synchronous)
                            {
                                TestHelper.AssertCallbackContext((SynchronizationType) waitType, SynchronizationType.Background, foregroundThread);
                            }
                            didContinue = true;
                        }
                    }
                }
                else
                {
                    promise
                        .ContinueWith(_ =>
                        {
                            // None and Synchronous don't care about the continuation context.
                            if (waitType != ConfigureAwaitType.None && waitType != ConfigureAwaitType.Synchronous)
                            {
                                TestHelper.AssertCallbackContext((SynchronizationType) waitType, SynchronizationType.Background, foregroundThread);
                            }
                            didContinue = true;
                        })
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
                    promise = promise.ConfigureAwait(waitType, false, cancelationToken);
                    SubscribeContinuation();
                });
            }
            else if (waitAsyncSubscribePlace == ActionPlace.Parallel)
            {
                parallelActions.Add(() => promise = promise.ConfigureAwait(waitType, false, cancelationToken));
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
                        promise = promise.ConfigureAwait(waitType, false, cancelationToken);
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
                        promise = promise.ConfigureAwait(waitType, false, cancelationToken);
                    }
                    if (continuePlace == ActionPlace.InTeardown)
                    {
                        SubscribeContinuation();
                    }

                    TestHelper.ExecuteForegroundCallbacksAndWaitForThreadsToComplete();
                    if (!SpinWait.SpinUntil(() => didContinue, timeout))
                    {
                        Assert.Fail("Timed out after " + timeout + ", didContinue: " + didContinue);
                    }
                    cancelationSource.TryDispose();
                },
                actions: parallelActions.ToArray()
            );
        }

        [Test, TestCaseSource("GetArgs")]
        public void WaitAsyncContinuationWillBeInvokedOnTheCorrectContext_Concurrent_T(
            ConfigureAwaitType waitType,
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
                            // None and Synchronous don't care about the continuation context.
                            if (waitType != ConfigureAwaitType.None && waitType != ConfigureAwaitType.Synchronous)
                            {
                                TestHelper.AssertCallbackContext((SynchronizationType) waitType, SynchronizationType.Background, foregroundThread);
                            }
                            didContinue = true;
                        }
                    }
                }
                else
                {
                    promise
                        .ContinueWith(_ =>
                        {
                            // None and Synchronous don't care about the continuation context.
                            if (waitType != ConfigureAwaitType.None && waitType != ConfigureAwaitType.Synchronous)
                            {
                                TestHelper.AssertCallbackContext((SynchronizationType) waitType, SynchronizationType.Background, foregroundThread);
                            }
                            didContinue = true;
                        })
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
                    promise = promise.ConfigureAwait(waitType, false, cancelationToken);
                    SubscribeContinuation();
                });
            }
            else if (waitAsyncSubscribePlace == ActionPlace.Parallel)
            {
                parallelActions.Add(() => promise = promise.ConfigureAwait(waitType, false, cancelationToken));
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
                        promise = promise.ConfigureAwait(waitType, false, cancelationToken);
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
                        promise = promise.ConfigureAwait(waitType, false, cancelationToken);
                    }
                    if (continuePlace == ActionPlace.InTeardown)
                    {
                        SubscribeContinuation();
                    }

                    TestHelper.ExecuteForegroundCallbacksAndWaitForThreadsToComplete();
                    if (!SpinWait.SpinUntil(() => didContinue, timeout))
                    {
                        Assert.Fail("Timed out after " + timeout + ", didContinue: " + didContinue);
                    }
                    cancelationSource.TryDispose();
                },
                actions: parallelActions.ToArray()
            );
        }
    }
}

#endif // !UNITY_WEBGL