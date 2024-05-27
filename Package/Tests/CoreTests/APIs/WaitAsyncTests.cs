#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using NUnit.Framework;
using Proto.Promises;
using ProtoPromiseTests.Concurrency;
using System;
using System.Collections.Generic;
using System.Threading;

namespace ProtoPromiseTests.APIs
{
    public class WaitAsyncTests
    {
        public enum ConfigureAwaitCancelType
        {
            NoToken,
            WithToken_NoCancel,
            AlreadyCanceled,
            CancelFirst,
            CancelSecond
        }

        const string rejectValue = "Fail";

        [SetUp]
        public void Setup()
        {
            // When a promise is canceled, the previous rejected promise is unhandled, so its rejection is sent to the UncaughtRejectionHandler.
            // So we set the expected uncaught reject value.
            TestHelper.s_expectedUncaughtRejectValue = rejectValue;

            TestHelper.Setup();
        }

        [TearDown]
        public void Teardown()
        {
            TestHelper.Cleanup();

            TestHelper.s_expectedUncaughtRejectValue = null;
        }

        private static IEnumerable<TestCaseData> GetArgs(params CompleteType[] completeTypes)
        {
            var waitTypes = new[]
            {
                SynchronizationType.Synchronous,
                //SynchronizationType.Foreground, // Ignore foreground to reduce number of tests, testing explicit is effectively the same.
#if !UNITY_WEBGL
                SynchronizationType.Background,
#endif
                SynchronizationType.Explicit
            };
            SynchronizationType[] reportTypes = new[]
            {
                SynchronizationType.Foreground
#if !UNITY_WEBGL
                , SynchronizationType.Background
#endif
            };
            var foregroundOnlyReportType = new[] { SynchronizationType.Foreground };
            var alreadyCompletes = new[] { true, false };

            var secondCompleteTypes = new[] { CompleteType.Resolve }; // Just use a single value to reduce number of tests.

            var configureAwaitCancelTypes = new[]
            {
                ConfigureAwaitCancelType.NoToken,
                ConfigureAwaitCancelType.WithToken_NoCancel,
                ConfigureAwaitCancelType.AlreadyCanceled,
                ConfigureAwaitCancelType.CancelFirst,
                ConfigureAwaitCancelType.CancelSecond
            };

            foreach (ConfigureAwaitCancelType configureAwaitCancelType in configureAwaitCancelTypes)
            foreach (CompleteType firstCompleteType in completeTypes)
            foreach (CompleteType secondCompleteType in secondCompleteTypes)
            foreach (bool isFirstComplete in alreadyCompletes)
            foreach (bool isSecondComplete in new[] { false })// alreadyCompletes) // Second always pending to reduce number of tests.
            foreach (SynchronizationType firstWaitType in waitTypes)
            foreach (SynchronizationType secondWaitType in waitTypes)
            foreach (SynchronizationType firstReportType in isFirstComplete ? foregroundOnlyReportType : reportTypes)
            {
                // Only report second on foreground to reduce number of tests.
                var secondReportTypes = foregroundOnlyReportType;
                //var secondReportTypes = !isSecondComplete
                //    ? reportTypes
                //    : new SynchronizationType[]
                //    {
                //        firstWaitType == SynchronizationType.Synchronous
                //            ? firstReportType
                //            : firstWaitType == (SynchronizationType) SynchronizationOption.Background
                //                ? (SynchronizationType) SynchronizationOption.Background
                //                : SynchronizationType.Foreground
                //    };
                foreach (SynchronizationType secondReportType in secondReportTypes)
                {
                    yield return new TestCaseData(firstCompleteType, secondCompleteType, firstWaitType, secondWaitType, firstReportType, secondReportType, configureAwaitCancelType, isFirstComplete, isSecondComplete);
                }
            }
        }

        private static IEnumerable<TestCaseData> GetArgs_ResolveReject()
            => GetArgs(CompleteType.Resolve, CompleteType.Reject);

        private static IEnumerable<TestCaseData> GetArgs_ContinueWith()
            => GetArgs(CompleteType.Resolve, CompleteType.Reject, CompleteType.Cancel);

        private static IEnumerable<TestCaseData> GetArgs_CancelFinally(params CompleteType[] completeTypes)
        {
            var synchronizationTypes = new[]
            {
                SynchronizationType.Synchronous,
                SynchronizationType.Foreground,
#if !UNITY_WEBGL
                SynchronizationType.Background,
#endif
                SynchronizationType.Explicit
            };
            var reportTypes = new[]
            {
                SynchronizationType.Foreground
#if !UNITY_WEBGL
                , SynchronizationType.Background
#endif
            };
            var foregroundOnlyReportType = new[] { SynchronizationType.Foreground };
            var alreadyCompletes = new[] { true, false };

            foreach (CompleteType firstCompleteType in completeTypes)
            foreach (bool isComplete in alreadyCompletes)
            foreach (SynchronizationType waitType in synchronizationTypes)
            foreach (SynchronizationType reportType in isComplete ? foregroundOnlyReportType : reportTypes)
            {
                yield return new TestCaseData(firstCompleteType, waitType, reportType, isComplete);
            }
        }

        private static IEnumerable<TestCaseData> GetArgs_Cancel()
            => GetArgs_CancelFinally(CompleteType.Cancel);

        private static IEnumerable<TestCaseData> GetArgs_Finally()
            => GetArgs_CancelFinally(CompleteType.Resolve, CompleteType.Reject, CompleteType.Cancel);
        private readonly TimeSpan timeout = TimeSpan.FromSeconds(2);

        // promise
        //     .Then(() => otherPromise.WaitAsync(SynchronizationOption))
        //     .Then(() => ...)
        // 
        // The context that the second callback runs on in this case is undefined due to thread race conditions, but we test it here to make sure nothing breaks and the callback is actually still called.
        // The proper way to do it is move the WaitAsync call out like so:
        //
        // promise
        //     .Then(() => otherPromise)
        //     .WaitAsync(SynchronizationOption)
        //     .Then(() => ...)

        [Test, TestCaseSource(nameof(GetArgs_ResolveReject))]
        public void CallbacksWillBeInvokedOnTheCorrectSynchronizationContext_ResolveReject_void(
            CompleteType firstCompleteType,
            CompleteType secondCompleteType,
            SynchronizationType firstWaitType,
            SynchronizationType secondWaitType,
            SynchronizationType firstReportType,
            SynchronizationType secondReportType,
            ConfigureAwaitCancelType configureAwaitCancelType,
            bool isFirstAlreadyComplete,
            bool isSecondAlreadyComplete)
        {
            var foregroundThread = Thread.CurrentThread;
            var threadHelper = new ThreadHelper();

            var configureAwaitCancelationSource1 = CancelationSource.New();
            var configureAwaitCancelationToken1 = configureAwaitCancelType == ConfigureAwaitCancelType.NoToken ? default(CancelationToken)
                : configureAwaitCancelType == ConfigureAwaitCancelType.AlreadyCanceled ? CancelationToken.Canceled()
                : configureAwaitCancelationSource1.Token;
            var configureAwaitCancelationSource2 = CancelationSource.New();
            var configureAwaitCancelationToken2 = configureAwaitCancelType == ConfigureAwaitCancelType.NoToken ? default(CancelationToken)
                : configureAwaitCancelType == ConfigureAwaitCancelType.AlreadyCanceled ? CancelationToken.Canceled()
                : configureAwaitCancelationSource2.Token;

            Promise firstPromise = TestHelper.BuildPromise(firstCompleteType, isFirstAlreadyComplete, rejectValue, out var tryCompleter1);
            Promise<int> secondPromise = TestHelper.BuildPromise(secondCompleteType, isSecondAlreadyComplete, 1, rejectValue, out var tryCompleter2);

            firstPromise = firstPromise.Preserve();
            secondPromise = secondPromise.Preserve();

            int firstInvokeCounter = 0;
            int secondInvokeCounter = 0;

            int expectedFirstInvokes = 0;
            int expectedSecondInvokes = 0;

            Action onFirstCallback = () =>
            {
                TestHelper.AssertCallbackContext(firstWaitType, configureAwaitCancelType == ConfigureAwaitCancelType.AlreadyCanceled ? firstWaitType : firstReportType, foregroundThread);
                Interlocked.Increment(ref firstInvokeCounter);
            };
            Action onSecondCallback = () =>
            {
                // We can't assert the context due to thread race conditions, just make sure the callback is invoked.
                Interlocked.Increment(ref secondInvokeCounter);
            };

            Func<Promise, Promise> HookupSecondVoid = promise =>
            {
                ++expectedSecondInvokes;
                return promise.ContinueWith(_ => onSecondCallback());
            };

            Func<Promise<int>, Promise<int>> HookupSecondT = promise =>
            {
                ++expectedSecondInvokes;
                return promise.ContinueWith(_ => { onSecondCallback(); return 2; });
            };

            bool isFirstCancelExpected = configureAwaitCancelType == ConfigureAwaitCancelType.AlreadyCanceled
                || (configureAwaitCancelType == ConfigureAwaitCancelType.CancelFirst && (!isFirstAlreadyComplete || firstWaitType != SynchronizationType.Synchronous));

            if (firstCompleteType == CompleteType.Resolve)
            {
                TestHelper.AddResolveCallbacksWithCancelation<int, string>(firstPromise,
                    onResolve: () => onFirstCallback(),
                    promiseToPromise: p => secondPromise.ConfigureAwait((ConfigureAwaitType) secondWaitType, false, configureAwaitCancelationToken2),
                    promiseToPromiseConvert: p => secondPromise.ConfigureAwait((ConfigureAwaitType) secondWaitType, false, configureAwaitCancelationToken2),
                    onCallbackAdded: (ref Promise promise) =>
                    {
                        ++expectedFirstInvokes;
                        promise.Forget();
                    },
                    onCallbackAddedConvert: (ref Promise<int> promise) =>
                    {
                        ++expectedFirstInvokes;
                        promise.Forget();
                    },
                    onAdoptCallbackAdded: (ref Promise promise) =>
                    {
                        promise = HookupSecondVoid(promise);
                    },
                    onAdoptCallbackAddedConvert: (ref Promise<int> promise) =>
                    {
                        promise = HookupSecondT(promise);
                    },
                    onCancel: () =>
                    {
                        if (isFirstCancelExpected)
                        {
                            // Don't assert the context due to a race condition between the cancelation propagating on another thread before the CatchCancelation is hooked up.
                            Interlocked.Increment(ref firstInvokeCounter);
                        }
                    },
                    configureAwaitType: (ConfigureAwaitType) firstWaitType,
                    waitAsyncCancelationToken: configureAwaitCancelationToken1,
                    configureAwaitForceAsync: true
                );
            }
            TestHelper.AddCallbacksWithCancelation<int, object, string>(firstPromise,
                onResolve: () => onFirstCallback(),
                onReject: r => onFirstCallback(),
                onUnknownRejection: () => onFirstCallback(),
                promiseToPromise: p => secondPromise.ConfigureAwait((ConfigureAwaitType) secondWaitType, false, configureAwaitCancelationToken2),
                promiseToPromiseConvert: p => secondPromise.ConfigureAwait((ConfigureAwaitType) secondWaitType, false, configureAwaitCancelationToken2),
                onDirectCallbackAdded: (ref Promise promise) =>
                {
                    ++expectedFirstInvokes;
                    promise = promise.Catch(() => { });
                },
                onDirectCallbackAddedConvert: (ref Promise<int> promise) =>
                {
                    ++expectedFirstInvokes;
                    promise = promise.Catch(() => 2);
                },
                onDirectCallbackAddedCatch: (ref Promise promise) =>
                {
                    if (firstCompleteType != CompleteType.Resolve)
                    {
                        ++expectedFirstInvokes;
                    }
                    // Don't expect cancelation invoke if it will continue on background, as that introduces a race condition.
                    else if (isFirstCancelExpected && firstWaitType != TestHelper.backgroundType)
                    {
                        ++expectedFirstInvokes;
                    }
                },
                onAdoptCallbackAdded: (ref Promise promise, AdoptLocation adoptLocation) =>
                {
                    ++expectedFirstInvokes;
                    if (adoptLocation == AdoptLocation.Both || (CompleteType) adoptLocation == firstCompleteType)
                    {
                        promise = HookupSecondVoid(promise);
                    }
                },
                onAdoptCallbackAddedConvert: (ref Promise<int> promise, AdoptLocation adoptLocation) =>
                {
                    ++expectedFirstInvokes;
                    if (adoptLocation == AdoptLocation.Both || (CompleteType) adoptLocation == firstCompleteType)
                    {
                        promise = HookupSecondT(promise);
                    }
                },
                onAdoptCallbackAddedCatch: (ref Promise promise) =>
                {
                    if (firstCompleteType != CompleteType.Resolve)
                    {
                        ++expectedFirstInvokes;
                    }
                    // Don't expect cancelation invoke if it will continue on background, as that introduces a race condition.
                    else if (isFirstCancelExpected && firstWaitType != TestHelper.backgroundType)
                    {
                        ++expectedFirstInvokes;
                    }
                    if (firstCompleteType == CompleteType.Reject)
                    {
                        promise = HookupSecondVoid(promise);
                    }
                },
                onCancel: () =>
                {
                    if (isFirstCancelExpected)
                    {
                        // Don't assert the context due to a race condition between the cancelation propagating on another thread before the CatchCancelation is hooked up.
                        Interlocked.Increment(ref firstInvokeCounter);
                    }
                },
                configureAwaitType: (ConfigureAwaitType) firstWaitType,
                waitAsyncCancelationToken: configureAwaitCancelationToken1,
                configureAwaitForceAsync: true
            );

            threadHelper.ExecuteSynchronousOrOnThread(
                () =>
                {
                    if (configureAwaitCancelType == ConfigureAwaitCancelType.CancelFirst)
                    {
                        configureAwaitCancelationSource1.Cancel();
                    }
                    tryCompleter1();
                },
                firstReportType == SynchronizationType.Foreground);

            TestHelper.ExecuteForegroundCallbacksAndWaitForThreadsToComplete();
            if (firstWaitType != (SynchronizationType) SynchronizationOption.Background)
            {
                Assert.AreEqual(expectedFirstInvokes, firstInvokeCounter);
            }
            else
            {
                // We check >= instead of == because of race conditions
                Assert.GreaterOrEqual(firstInvokeCounter, expectedFirstInvokes);
            }

            threadHelper.ExecuteSynchronousOrOnThread(
                () =>
                {
                    if (configureAwaitCancelType == ConfigureAwaitCancelType.CancelSecond)
                    {
                        configureAwaitCancelationSource2.Cancel();
                    }
                    tryCompleter2();
                },
                secondReportType == SynchronizationType.Foreground);
            TestHelper.ExecuteForegroundCallbacks();

            // We must execute foreground context on every spin to account for the race condition between firstInvokeCounter being incremented and the configured promise returning in the callback.
            TestHelper.SpinUntilWhileExecutingForegroundContext(() => secondInvokeCounter == expectedSecondInvokes, timeout,
                $"expectedSecondInvokes: {expectedSecondInvokes}, secondInvokeCounter: {secondInvokeCounter}");

            // Fix a race condition that causes forget to be called before ConfigureAwait.
            TestHelper._backgroundContext.WaitForAllThreadsToComplete();
            TestHelper.ExecuteForegroundCallbacks();

            firstPromise.Forget();
            secondPromise.Forget();
            configureAwaitCancelationSource1.TryDispose();
            configureAwaitCancelationSource2.TryDispose();
        }

        [Test, TestCaseSource(nameof(GetArgs_ResolveReject))]
        public void CallbacksWillBeInvokedOnTheCorrectSynchronizationContext_ResolveReject_T(
            CompleteType firstCompleteType,
            CompleteType secondCompleteType,
            SynchronizationType firstWaitType,
            SynchronizationType secondWaitType,
            SynchronizationType firstReportType,
            SynchronizationType secondReportType,
            ConfigureAwaitCancelType configureAwaitCancelType,
            bool isFirstAlreadyComplete,
            bool isSecondAlreadyComplete)
        {
            var foregroundThread = Thread.CurrentThread;
            var threadHelper = new ThreadHelper();

            var configureAwaitCancelationSource1 = CancelationSource.New();
            var configureAwaitCancelationToken1 = configureAwaitCancelType == ConfigureAwaitCancelType.NoToken ? default(CancelationToken)
                : configureAwaitCancelType == ConfigureAwaitCancelType.AlreadyCanceled ? CancelationToken.Canceled()
                : configureAwaitCancelationSource1.Token;
            var configureAwaitCancelationSource2 = CancelationSource.New();
            var configureAwaitCancelationToken2 = configureAwaitCancelType == ConfigureAwaitCancelType.NoToken ? default(CancelationToken)
                : configureAwaitCancelType == ConfigureAwaitCancelType.AlreadyCanceled ? CancelationToken.Canceled()
                : configureAwaitCancelationSource2.Token;

            Promise<int> firstPromise = TestHelper.BuildPromise(firstCompleteType, isFirstAlreadyComplete, 1, rejectValue, out var tryCompleter1);
            Promise<int> secondPromise = TestHelper.BuildPromise(secondCompleteType, isSecondAlreadyComplete, 1, rejectValue, out var tryCompleter2);

            firstPromise = firstPromise.Preserve();
            secondPromise = secondPromise.Preserve();

            int firstInvokeCounter = 0;
            int secondInvokeCounter = 0;

            int expectedFirstInvokes = 0;
            int expectedSecondInvokes = 0;

            Action onFirstCallback = () =>
            {
                TestHelper.AssertCallbackContext(firstWaitType, configureAwaitCancelType == ConfigureAwaitCancelType.AlreadyCanceled ? firstWaitType : firstReportType, foregroundThread);
                Interlocked.Increment(ref firstInvokeCounter);
            };
            Action onSecondCallback = () =>
            {
                // We can't assert the context due to thread race conditions, just make sure the callback is invoked.
                Interlocked.Increment(ref secondInvokeCounter);
            };

            Func<Promise, Promise> HookupSecondVoid = promise =>
            {
                ++expectedSecondInvokes;
                return promise.ContinueWith(_ => onSecondCallback());
            };

            Func<Promise<int>, Promise<int>> HookupSecondT = promise =>
            {
                ++expectedSecondInvokes;
                return promise.ContinueWith(_ => { onSecondCallback(); return 2; });
            };

            bool isFirstCancelExpected = configureAwaitCancelType == ConfigureAwaitCancelType.AlreadyCanceled
                || (configureAwaitCancelType == ConfigureAwaitCancelType.CancelFirst && (!isFirstAlreadyComplete || firstWaitType != SynchronizationType.Synchronous));

            if (firstCompleteType == CompleteType.Resolve)
            {
                TestHelper.AddResolveCallbacksWithCancelation<int, int, string>(firstPromise,
                    onResolve: v => onFirstCallback(),
                    promiseToPromise: p => secondPromise.ConfigureAwait((ConfigureAwaitType) secondWaitType, false, configureAwaitCancelationToken2),
                    promiseToPromiseConvert: p => secondPromise.ConfigureAwait((ConfigureAwaitType) secondWaitType, false, configureAwaitCancelationToken2),
                    onCallbackAdded: (ref Promise promise) =>
                    {
                        ++expectedFirstInvokes;
                        promise.Forget();
                    },
                    onCallbackAddedConvert: (ref Promise<int> promise) =>
                    {
                        ++expectedFirstInvokes;
                        promise.Forget();
                    },
                    onAdoptCallbackAdded: (ref Promise promise) =>
                    {
                        promise = HookupSecondVoid(promise);
                    },
                    onAdoptCallbackAddedConvert: (ref Promise<int> promise) =>
                    {
                        promise = HookupSecondT(promise);
                    },
                    onCancel: () =>
                    {
                        if (isFirstCancelExpected)
                        {
                            // Don't assert the context due to a race condition between the cancelation propagating on another thread before the CatchCancelation is hooked up.
                            Interlocked.Increment(ref firstInvokeCounter);
                        }
                    },
                    configureAwaitType: (ConfigureAwaitType) firstWaitType,
                    waitAsyncCancelationToken: configureAwaitCancelationToken1,
                    configureAwaitForceAsync: true
                );
            }
            TestHelper.AddCallbacksWithCancelation<int, int, object, string>(firstPromise,
                onResolve: v => onFirstCallback(),
                onReject: r => onFirstCallback(),
                onUnknownRejection: () => onFirstCallback(),
                promiseToPromise: p => secondPromise.ConfigureAwait((ConfigureAwaitType) secondWaitType, false, configureAwaitCancelationToken2),
                promiseToPromiseConvert: p => secondPromise.ConfigureAwait((ConfigureAwaitType) secondWaitType, false, configureAwaitCancelationToken2),
                onDirectCallbackAdded: (ref Promise promise) =>
                {
                    ++expectedFirstInvokes;
                    promise = promise.Catch(() => { });
                },
                onDirectCallbackAddedConvert: (ref Promise<int> promise) =>
                {
                    ++expectedFirstInvokes;
                    promise = promise.Catch(() => 2);
                },
                onDirectCallbackAddedT: (ref Promise<int> promise) =>
                {
                    if (firstCompleteType != CompleteType.Resolve)
                    {
                        ++expectedFirstInvokes;
                    }
                    // Don't expect cancelation invoke if it will continue on background, as that introduces a race condition.
                    else if (isFirstCancelExpected && firstWaitType != TestHelper.backgroundType)
                    {
                        ++expectedFirstInvokes;
                    }
                },
                onAdoptCallbackAdded: (ref Promise promise, AdoptLocation adoptLocation) =>
                {
                    ++expectedFirstInvokes;
                    if (adoptLocation == AdoptLocation.Both || (CompleteType) adoptLocation == firstCompleteType)
                    {
                        promise = HookupSecondVoid(promise);
                    }
                },
                onAdoptCallbackAddedConvert: (ref Promise<int> promise, AdoptLocation adoptLocation) =>
                {
                    ++expectedFirstInvokes;
                    if (adoptLocation == AdoptLocation.Both || (CompleteType) adoptLocation == firstCompleteType)
                    {
                        promise = HookupSecondT(promise);
                    }
                },
                onAdoptCallbackAddedT: (ref Promise<int> promise) =>
                {
                    if (firstCompleteType != CompleteType.Resolve)
                    {
                        ++expectedFirstInvokes;
                    }
                    // Don't expect cancelation invoke if it will continue on background, as that introduces a race condition.
                    else if (isFirstCancelExpected && firstWaitType != TestHelper.backgroundType)
                    {
                        ++expectedFirstInvokes;
                    }
                    if (firstCompleteType == CompleteType.Reject)
                    {
                        promise = HookupSecondT(promise);
                    }
                },
                onCancel: () =>
                {
                    if (isFirstCancelExpected)
                    {
                        // Don't assert the context due to a race condition between the cancelation propagating on another thread before the CatchCancelation is hooked up.
                        Interlocked.Increment(ref firstInvokeCounter);
                    }
                },
                configureAwaitType: (ConfigureAwaitType) firstWaitType,
                waitAsyncCancelationToken: configureAwaitCancelationToken1,
                configureAwaitForceAsync: true
            );

            threadHelper.ExecuteSynchronousOrOnThread(
                () =>
                {
                    if (configureAwaitCancelType == ConfigureAwaitCancelType.CancelFirst)
                    {
                        configureAwaitCancelationSource1.Cancel();
                    }
                    tryCompleter1();
                },
                firstReportType == SynchronizationType.Foreground);

            TestHelper.ExecuteForegroundCallbacksAndWaitForThreadsToComplete();
            if (firstWaitType != (SynchronizationType) SynchronizationOption.Background)
            {
                Assert.AreEqual(expectedFirstInvokes, firstInvokeCounter);
            }
            else
            {
                // We check >= instead of == because of race conditions
                Assert.GreaterOrEqual(firstInvokeCounter, expectedFirstInvokes);
            }

            threadHelper.ExecuteSynchronousOrOnThread(
                () =>
                {
                    if (configureAwaitCancelType == ConfigureAwaitCancelType.CancelSecond)
                    {
                        configureAwaitCancelationSource2.Cancel();
                    }
                    tryCompleter2();
                },
                secondReportType == SynchronizationType.Foreground);
            TestHelper.ExecuteForegroundCallbacks();

            // We must execute foreground context on every spin to account for the race condition between firstInvokeCounter being incremented and the configured promise returning in the callback.
            TestHelper.SpinUntilWhileExecutingForegroundContext(() => secondInvokeCounter == expectedSecondInvokes, timeout,
                $"expectedSecondInvokes: {expectedSecondInvokes}, secondInvokeCounter: {secondInvokeCounter}");

            // Fix a race condition that causes forget to be called before ConfigureAwait.
            TestHelper._backgroundContext.WaitForAllThreadsToComplete();
            TestHelper.ExecuteForegroundCallbacks();

            firstPromise.Forget();
            secondPromise.Forget();
            configureAwaitCancelationSource1.TryDispose();
            configureAwaitCancelationSource2.TryDispose();
        }

        [Test, TestCaseSource(nameof(GetArgs_ContinueWith))]
        public void CallbacksWillBeInvokedOnTheCorrectSynchronizationContext_ContinueWith_void(
            CompleteType firstCompleteType,
            CompleteType secondCompleteType,
            SynchronizationType firstWaitType,
            SynchronizationType secondWaitType,
            SynchronizationType firstReportType,
            SynchronizationType secondReportType,
            ConfigureAwaitCancelType configureAwaitCancelType,
            bool isFirstAlreadyComplete,
            bool isSecondAlreadyComplete)
        {
            var foregroundThread = Thread.CurrentThread;
            var threadHelper = new ThreadHelper();

            var configureAwaitCancelationSource1 = CancelationSource.New();
            var configureAwaitCancelationToken1 = configureAwaitCancelType == ConfigureAwaitCancelType.NoToken ? default(CancelationToken)
                : configureAwaitCancelType == ConfigureAwaitCancelType.AlreadyCanceled ? CancelationToken.Canceled()
                : configureAwaitCancelationSource1.Token;
            var configureAwaitCancelationSource2 = CancelationSource.New();
            var configureAwaitCancelationToken2 = configureAwaitCancelType == ConfigureAwaitCancelType.NoToken ? default(CancelationToken)
                : configureAwaitCancelType == ConfigureAwaitCancelType.AlreadyCanceled ? CancelationToken.Canceled()
                : configureAwaitCancelationSource2.Token;

            Promise firstPromise = TestHelper.BuildPromise(firstCompleteType, isFirstAlreadyComplete, rejectValue, out var tryCompleter1);
            Promise<int> secondPromise = TestHelper.BuildPromise(secondCompleteType, isSecondAlreadyComplete, 1, rejectValue, out var tryCompleter2);

            firstPromise = firstPromise.Preserve();
            secondPromise = secondPromise.Preserve();

            int firstInvokeCounter = 0;
            int secondInvokeCounter = 0;

            int expectedFirstInvokes = 0;
            int expectedSecondInvokes = 0;

            Action onFirstCallback = () =>
            {
                TestHelper.AssertCallbackContext(firstWaitType, configureAwaitCancelType == ConfigureAwaitCancelType.AlreadyCanceled ? firstWaitType : firstReportType, foregroundThread);
                Interlocked.Increment(ref firstInvokeCounter);
            };
            Action onSecondCallback = () =>
            {
                // We can't assert the context due to thread race conditions, just make sure the callback is invoked.
                Interlocked.Increment(ref secondInvokeCounter);
            };

            TestHelper.AddContinueCallbacksWithCancelation<int, string>(firstPromise,
                onContinue: _ => onFirstCallback(),
                promiseToPromise: p => secondPromise.ConfigureAwait((ConfigureAwaitType) secondWaitType, false, configureAwaitCancelationToken2),
                promiseToPromiseConvert: p => secondPromise.ConfigureAwait((ConfigureAwaitType) secondWaitType, false, configureAwaitCancelationToken2),
                onCallbackAdded: (ref Promise promise) =>
                {
                    ++expectedFirstInvokes;
                    promise.Forget();
                },
                onCallbackAddedConvert: (ref Promise<int> promise) =>
                {
                    ++expectedFirstInvokes;
                    promise.Forget();
                },
                onAdoptCallbackAdded: (ref Promise promise) =>
                {
                    ++expectedSecondInvokes;
                    promise = promise.ContinueWith(_ => onSecondCallback());
                },
                onAdoptCallbackAddedConvert: (ref Promise<int> promise) =>
                {
                    ++expectedSecondInvokes;
                    promise = promise.ContinueWith(_ => { onSecondCallback(); return 2; });
                },
                configureAwaitType: (ConfigureAwaitType) firstWaitType,
                waitAsyncCancelationToken: configureAwaitCancelationToken1,
                configureAwaitForceAsync: true
            );

            threadHelper.ExecuteSynchronousOrOnThread(
                () =>
                {
                    if (configureAwaitCancelType == ConfigureAwaitCancelType.CancelFirst)
                    {
                        configureAwaitCancelationSource1.Cancel();
                    }
                    tryCompleter1();
                },
                firstReportType == SynchronizationType.Foreground);

            TestHelper.ExecuteForegroundCallbacksAndWaitForThreadsToComplete();
            Assert.AreEqual(expectedFirstInvokes, firstInvokeCounter);

            threadHelper.ExecuteSynchronousOrOnThread(
                () =>
                {
                    if (configureAwaitCancelType == ConfigureAwaitCancelType.CancelSecond)
                    {
                        configureAwaitCancelationSource2.Cancel();
                    }
                    tryCompleter2();
                },
                secondReportType == SynchronizationType.Foreground);
            TestHelper.ExecuteForegroundCallbacks();

            // We must execute foreground context on every spin to account for the race condition between firstInvokeCounter being incremented and the configured promise returning in the callback.
            TestHelper.SpinUntilWhileExecutingForegroundContext(() => secondInvokeCounter == expectedSecondInvokes, timeout,
                $"expectedSecondInvokes: {expectedSecondInvokes}, secondInvokeCounter: {secondInvokeCounter}");

            firstPromise.Forget();
            secondPromise.Forget();
            configureAwaitCancelationSource1.TryDispose();
            configureAwaitCancelationSource2.TryDispose();
        }

        [Test, TestCaseSource(nameof(GetArgs_ContinueWith))]
        public void CallbacksWillBeInvokedOnTheCorrectSynchronizationContext_ContinueWith_T(
            CompleteType firstCompleteType,
            CompleteType secondCompleteType,
            SynchronizationType firstWaitType,
            SynchronizationType secondWaitType,
            SynchronizationType firstReportType,
            SynchronizationType secondReportType,
            ConfigureAwaitCancelType configureAwaitCancelType,
            bool isFirstAlreadyComplete,
            bool isSecondAlreadyComplete)
        {
            var foregroundThread = Thread.CurrentThread;
            var threadHelper = new ThreadHelper();

            var configureAwaitCancelationSource1 = CancelationSource.New();
            var configureAwaitCancelationToken1 = configureAwaitCancelType == ConfigureAwaitCancelType.NoToken ? default(CancelationToken)
                : configureAwaitCancelType == ConfigureAwaitCancelType.AlreadyCanceled ? CancelationToken.Canceled()
                : configureAwaitCancelationSource1.Token;
            var configureAwaitCancelationSource2 = CancelationSource.New();
            var configureAwaitCancelationToken2 = configureAwaitCancelType == ConfigureAwaitCancelType.NoToken ? default(CancelationToken)
                : configureAwaitCancelType == ConfigureAwaitCancelType.AlreadyCanceled ? CancelationToken.Canceled()
                : configureAwaitCancelationSource2.Token;

            Promise<int> firstPromise = TestHelper.BuildPromise(firstCompleteType, isFirstAlreadyComplete, 1, rejectValue, out var tryCompleter1);
            Promise<int> secondPromise = TestHelper.BuildPromise(secondCompleteType, isSecondAlreadyComplete, 1, rejectValue, out var tryCompleter2);

            firstPromise = firstPromise.Preserve();
            secondPromise = secondPromise.Preserve();

            int firstInvokeCounter = 0;
            int secondInvokeCounter = 0;

            int expectedFirstInvokes = 0;
            int expectedSecondInvokes = 0;

            Action onFirstCallback = () =>
            {
                TestHelper.AssertCallbackContext(firstWaitType, configureAwaitCancelType == ConfigureAwaitCancelType.AlreadyCanceled ? firstWaitType : firstReportType, foregroundThread);
                Interlocked.Increment(ref firstInvokeCounter);
            };
            Action onSecondCallback = () =>
            {
                // We can't assert the context due to thread race conditions, just make sure the callback is invoked.
                Interlocked.Increment(ref secondInvokeCounter);
            };

            TestHelper.AddContinueCallbacksWithCancelation<int, int, string>(firstPromise,
                onContinue: _ => onFirstCallback(),
                promiseToPromise: p => secondPromise.ConfigureAwait((ConfigureAwaitType) secondWaitType, false, configureAwaitCancelationToken2),
                promiseToPromiseConvert: p => secondPromise.ConfigureAwait((ConfigureAwaitType) secondWaitType, false, configureAwaitCancelationToken2),
                onCallbackAdded: (ref Promise promise) =>
                {
                    ++expectedFirstInvokes;
                    promise.Forget();
                },
                onCallbackAddedConvert: (ref Promise<int> promise) =>
                {
                    ++expectedFirstInvokes;
                    promise.Forget();
                },
                onAdoptCallbackAdded: (ref Promise promise) =>
                {
                    ++expectedSecondInvokes;
                    promise = promise.ContinueWith(_ => onSecondCallback());
                },
                onAdoptCallbackAddedConvert: (ref Promise<int> promise) =>
                {
                    ++expectedSecondInvokes;
                    promise = promise.ContinueWith(_ => { onSecondCallback(); return 2; });
                },
                configureAwaitType: (ConfigureAwaitType) firstWaitType,
                waitAsyncCancelationToken: configureAwaitCancelationToken1,
                configureAwaitForceAsync: true
            );

            threadHelper.ExecuteSynchronousOrOnThread(
                () =>
                {
                    if (configureAwaitCancelType == ConfigureAwaitCancelType.CancelFirst)
                    {
                        configureAwaitCancelationSource1.Cancel();
                    }
                    tryCompleter1();
                },
                firstReportType == SynchronizationType.Foreground);

            TestHelper.ExecuteForegroundCallbacksAndWaitForThreadsToComplete();
            Assert.AreEqual(expectedFirstInvokes, firstInvokeCounter);

            threadHelper.ExecuteSynchronousOrOnThread(
                () =>
                {
                    if (configureAwaitCancelType == ConfigureAwaitCancelType.CancelSecond)
                    {
                        configureAwaitCancelationSource2.Cancel();
                    }
                    tryCompleter2();
                },
                secondReportType == SynchronizationType.Foreground);
            TestHelper.ExecuteForegroundCallbacks();

            // We must execute foreground context on every spin to account for the race condition between firstInvokeCounter being incremented and the configured promise returning in the callback.
            TestHelper.SpinUntilWhileExecutingForegroundContext(() => secondInvokeCounter == expectedSecondInvokes, timeout,
                $"expectedSecondInvokes: {expectedSecondInvokes}, secondInvokeCounter: {secondInvokeCounter}");

            firstPromise.Forget();
            secondPromise.Forget();
            configureAwaitCancelationSource1.TryDispose();
            configureAwaitCancelationSource2.TryDispose();
        }

        [Test, TestCaseSource(nameof(GetArgs_Cancel))]
        public void CallbacksWillBeInvokedOnTheCorrectSynchronizationContext_Cancel_void(
            CompleteType completeType,
            SynchronizationType waitType,
            SynchronizationType reportType,
            bool isAlreadyComplete)
        {
            var foregroundThread = Thread.CurrentThread;
            var threadHelper = new ThreadHelper();

            Promise promise = TestHelper.BuildPromise(completeType, isAlreadyComplete, rejectValue, out var tryCompleter);

            promise = promise.Preserve();

            int invokeCounter = 0;
            int expectedInvokes = 0;

            foreach (var p in TestHelper.GetTestablePromises(promise))
            {
                ++expectedInvokes;
                p.ConfigureAwait((ConfigureAwaitType) waitType)
                    .CatchCancelation(() =>
                    {
                        TestHelper.AssertCallbackContext(waitType, reportType, foregroundThread);
                        Interlocked.Increment(ref invokeCounter);
                    })
                    .Forget();
            }
            foreach (var p in TestHelper.GetTestablePromises(promise))
            {
                ++expectedInvokes;
                p.ConfigureAwait((ConfigureAwaitType) waitType)
                    .CatchCancelation(1, cv =>
                    {
                        TestHelper.AssertCallbackContext(waitType, reportType, foregroundThread);
                        Interlocked.Increment(ref invokeCounter);
                    })
                    .Forget();
            }

            threadHelper.ExecuteSynchronousOrOnThread(
                tryCompleter,
                reportType == SynchronizationType.Foreground);

            TestHelper.ExecuteForegroundCallbacksAndWaitForThreadsToComplete();
            Assert.AreEqual(expectedInvokes, invokeCounter);

            promise.Forget();
        }

        [Test, TestCaseSource(nameof(GetArgs_Cancel))]
        public void CallbacksWillBeInvokedOnTheCorrectSynchronizationContext_Cancel_T(
            CompleteType completeType,
            SynchronizationType waitType,
            SynchronizationType reportType,
            bool isAlreadyComplete)
        {
            var foregroundThread = Thread.CurrentThread;
            var threadHelper = new ThreadHelper();

            Promise<int> promise = TestHelper.BuildPromise(completeType, isAlreadyComplete, 1, rejectValue, out var tryCompleter);

            promise = promise.Preserve();

            int invokeCounter = 0;
            int expectedInvokes = 0;

            foreach (var p in TestHelper.GetTestablePromises(promise))
            {
                ++expectedInvokes;
                p.ConfigureAwait((ConfigureAwaitType) waitType)
                    .CatchCancelation(() =>
                    {
                        TestHelper.AssertCallbackContext(waitType, reportType, foregroundThread);
                        Interlocked.Increment(ref invokeCounter);
                    })
                    .Forget();
            }
            foreach (var p in TestHelper.GetTestablePromises(promise))
            {
                ++expectedInvokes;
                p.ConfigureAwait((ConfigureAwaitType) waitType)
                    .CatchCancelation(1, cv =>
                    {
                        TestHelper.AssertCallbackContext(waitType, reportType, foregroundThread);
                        Interlocked.Increment(ref invokeCounter);
                    })
                    .Forget();
            }

            threadHelper.ExecuteSynchronousOrOnThread(
                tryCompleter,
                reportType == SynchronizationType.Foreground);

            TestHelper.ExecuteForegroundCallbacksAndWaitForThreadsToComplete();
            Assert.AreEqual(expectedInvokes, invokeCounter);

            promise.Forget();
        }

        [Test, TestCaseSource(nameof(GetArgs_Finally))]
        public void CallbacksWillBeInvokedOnTheCorrectSynchronizationContext_Finally_void(
            CompleteType completeType,
            SynchronizationType waitType,
            SynchronizationType reportType,
            bool isAlreadyComplete)
        {
            var foregroundThread = Thread.CurrentThread;
            var threadHelper = new ThreadHelper();

            Promise promise = TestHelper.BuildPromise(completeType, isAlreadyComplete, rejectValue, out var tryCompleter);

            promise = promise.Preserve();

            int invokeCounter = 0;
            int expectedInvokes = 0;

            foreach (var p in TestHelper.GetTestablePromises(promise))
            {
                ++expectedInvokes;
                p.ConfigureAwait((ConfigureAwaitType) waitType)
                    .Finally(() =>
                    {
                        TestHelper.AssertCallbackContext(waitType, reportType, foregroundThread);
                        Interlocked.Increment(ref invokeCounter);
                    })
                    .Catch(() => { })
                    .Forget();
            }
            foreach (var p in TestHelper.GetTestablePromises(promise))
            {
                ++expectedInvokes;
                p.ConfigureAwait((ConfigureAwaitType) waitType)
                    .Finally(1, cv =>
                    {
                        TestHelper.AssertCallbackContext(waitType, reportType, foregroundThread);
                        Interlocked.Increment(ref invokeCounter);
                    })
                    .Catch(() => { })
                    .Forget();
            }

            threadHelper.ExecuteSynchronousOrOnThread(
                tryCompleter,
                reportType == SynchronizationType.Foreground);

            TestHelper.ExecuteForegroundCallbacksAndWaitForThreadsToComplete();
            Assert.AreEqual(expectedInvokes, invokeCounter);

            promise.Forget();
        }

        [Test, TestCaseSource(nameof(GetArgs_Finally))]
        public void CallbacksWillBeInvokedOnTheCorrectSynchronizationContext_Finally_T(
            CompleteType completeType,
            SynchronizationType waitType,
            SynchronizationType reportType,
            bool isAlreadyComplete)
        {
            var foregroundThread = Thread.CurrentThread;
            var threadHelper = new ThreadHelper();

            Promise<int> promise = TestHelper.BuildPromise(completeType, isAlreadyComplete, 1, rejectValue, out var tryCompleter);

            promise = promise.Preserve();

            int invokeCounter = 0;
            int expectedInvokes = 0;

            foreach (var p in TestHelper.GetTestablePromises(promise))
            {
                ++expectedInvokes;
                p.ConfigureAwait((ConfigureAwaitType) waitType)
                    .Finally(() =>
                    {
                        TestHelper.AssertCallbackContext(waitType, reportType, foregroundThread);
                        Interlocked.Increment(ref invokeCounter);
                    })
                    .Catch(() => { })
                    .Forget();
            }
            foreach (var p in TestHelper.GetTestablePromises(promise))
            {
                ++expectedInvokes;
                p.ConfigureAwait((ConfigureAwaitType) waitType)
                    .Finally(1, cv =>
                    {
                        TestHelper.AssertCallbackContext(waitType, reportType, foregroundThread);
                        Interlocked.Increment(ref invokeCounter);
                    })
                    .Catch(() => { })
                    .Forget();
            }

            threadHelper.ExecuteSynchronousOrOnThread(
                tryCompleter,
                reportType == SynchronizationType.Foreground);

            TestHelper.ExecuteForegroundCallbacksAndWaitForThreadsToComplete();
            Assert.AreEqual(expectedInvokes, invokeCounter);

            promise.Forget();
        }

        [Test]
        public void WaitAsyncForceAsync_CallbacksWillBeInvokedProperly_Then_void(
            [Values(ConfigureAwaitType.Foreground, ConfigureAwaitType.Explicit
#if !UNITY_WEBGL
                , ConfigureAwaitType.Background
#endif
            )] ConfigureAwaitType waitType,
            [Values] bool forceAsync,
            [Values] bool isAlreadyComplete)
        {
            var foregroundThread = Thread.CurrentThread;

            // We're testing an implementation detail that if forceAsync is false, the continuation is invoked synchronously if the current context is the same.
            // It may still be executed asynchronously if forceAsync is false. But if it's true, it is guaranteed to be invoked asynchronously.
            // Lock helps us assert that the callback is not invoked synchronously if forceAsync is true.
            var lockObj = new object();
            bool didInvoke = false;

            Action action = () =>
            {
                lock (lockObj)
                {
                    TestHelper.BuildPromise(CompleteType.Resolve, isAlreadyComplete, rejectValue, out var tryCompleter)
                        .ConfigureAwait(waitType, forceAsync)
                        .ContinueWith(_ =>
                        {
                            TestHelper.AssertCallbackContext((SynchronizationType) waitType, (SynchronizationType) waitType, foregroundThread);
                            lock (lockObj)
                            {
                                didInvoke = true;
                            }
                        })
                        .Forget();
                    tryCompleter();
                    Assert.AreNotEqual(forceAsync, didInvoke);
                }
            };

            if (waitType == ConfigureAwaitType.Explicit)
            {
                Promise.Run(action, TestHelper._foregroundContext).Forget();
            }
            else
            {
                Promise.Run(action, (SynchronizationOption) waitType).Forget();
            }

            TestHelper.ExecuteForegroundCallbacksAndWaitForThreadsToComplete();

            TestHelper.SpinUntil(() => didInvoke, timeout, $"didInvoke: {didInvoke}");
        }

        [Test]
        public void WaitAsyncForceAsync_CallbacksWillBeInvokedProperly_Then_T(
            [Values(ConfigureAwaitType.Foreground, ConfigureAwaitType.Explicit
#if !UNITY_WEBGL
                , ConfigureAwaitType.Background
#endif
            )] ConfigureAwaitType waitType,
            [Values] bool forceAsync,
            [Values] bool isAlreadyComplete)
        {
            var foregroundThread = Thread.CurrentThread;

            // We're testing an implementation detail that if forceAsync is false, the continuation is invoked synchronously if the current context is the same.
            // It may still be executed asynchronously if forceAsync is false. But if it's true, it is guaranteed to be invoked asynchronously.
            // Lock helps us assert that the callback is not invoked synchronously if forceAsync is true.
            var lockObj = new object();
            bool didInvoke = false;

            Action action = () =>
            {
                lock (lockObj)
                {
                    TestHelper.BuildPromise(CompleteType.Resolve, isAlreadyComplete, 1, rejectValue, out var tryCompleter)
                        .ConfigureAwait(waitType, forceAsync)
                        .ContinueWith(_ =>
                        {
                            TestHelper.AssertCallbackContext((SynchronizationType) waitType, (SynchronizationType) waitType, foregroundThread);
                            lock (lockObj)
                            {
                                didInvoke = true;
                            }
                        })
                        .Forget();
                    tryCompleter();
                    Assert.AreNotEqual(forceAsync, didInvoke);
                }
            };

            if (waitType == ConfigureAwaitType.Explicit)
            {
                Promise.Run(action, TestHelper._foregroundContext).Forget();
            }
            else
            {
                Promise.Run(action, (SynchronizationOption) waitType).Forget();
            }

            TestHelper.ExecuteForegroundCallbacksAndWaitForThreadsToComplete();

            TestHelper.SpinUntil(() => didInvoke, timeout, $"didInvoke: {didInvoke}");
        }

        [Test, TestCaseSource(nameof(GetArgs_ContinueWith))]
        public void CallbacksWillBeInvokedOnTheCorrectSynchronizationContext_Await_void(
            CompleteType firstCompleteType,
            CompleteType secondCompleteType,
            SynchronizationType firstWaitType,
            SynchronizationType secondWaitType,
            SynchronizationType firstReportType,
            SynchronizationType secondReportType,
            ConfigureAwaitCancelType configureAwaitCancelType,
            bool isFirstAlreadyComplete,
            bool isSecondAlreadyComplete)
        {
            var foregroundThread = Thread.CurrentThread;
            var threadHelper = new ThreadHelper();

            var configureAwaitCancelationSource1 = CancelationSource.New();
            var configureAwaitCancelationToken1 = configureAwaitCancelType == ConfigureAwaitCancelType.NoToken ? default(CancelationToken)
                : configureAwaitCancelType == ConfigureAwaitCancelType.AlreadyCanceled ? CancelationToken.Canceled()
                : configureAwaitCancelationSource1.Token;
            var configureAwaitCancelationSource2 = CancelationSource.New();
            var configureAwaitCancelationToken2 = configureAwaitCancelType == ConfigureAwaitCancelType.NoToken ? default(CancelationToken)
                : configureAwaitCancelType == ConfigureAwaitCancelType.AlreadyCanceled ? CancelationToken.Canceled()
                : configureAwaitCancelationSource2.Token;

            Promise firstPromise = TestHelper.BuildPromise(firstCompleteType, isFirstAlreadyComplete, rejectValue, out var tryCompleter1);
            Promise secondPromise = TestHelper.BuildPromise(secondCompleteType, isSecondAlreadyComplete, rejectValue, out var tryCompleter2);

            firstPromise = firstPromise.Preserve();
            secondPromise = secondPromise.Preserve();

            int firstInvokeCounter = 0;
            int secondInvokeCounter = 0;

            int expectedInvokes = 0;
            bool hasRaceCondition = firstWaitType == (SynchronizationType) SynchronizationOption.Background && secondWaitType == SynchronizationType.Synchronous && !isSecondAlreadyComplete;

            foreach (var p1 in TestHelper.GetTestablePromises(firstPromise))
            {
                ++expectedInvokes;
                RunAsync(p1, secondPromise).Forget();
            }
            foreach (var p2 in TestHelper.GetTestablePromises(secondPromise))
            {
                ++expectedInvokes;
                RunAsync(firstPromise, p2).Forget();
            }

            async Promise RunAsync(Promise p1, Promise p2)
            {
                try
                {
                    await p1.ConfigureAwait((ConfigureAwaitType) firstWaitType, true, configureAwaitCancelationToken1);
                }
                catch { }
                finally
                {
                    TestHelper.AssertCallbackContext(firstWaitType, configureAwaitCancelType == ConfigureAwaitCancelType.AlreadyCanceled ? firstWaitType : firstReportType, foregroundThread);
                    Interlocked.Increment(ref firstInvokeCounter);
                }

                try
                {
                    await p2.ConfigureAwait((ConfigureAwaitType) secondWaitType, false, configureAwaitCancelationToken2);
                }
                catch { }
                finally
                {
                    // If there's a race condition, p2 could be completed on a separate thread before it's awaited, causing the assert to fail.
                    // This only matters for SynchronizationOption.Synchronous, for which the caller does not care on what context it executes.
                    if (!hasRaceCondition)
                    {
                        TestHelper.AssertCallbackContext(secondWaitType, configureAwaitCancelType == ConfigureAwaitCancelType.AlreadyCanceled ? secondWaitType : secondReportType, foregroundThread);
                    }
                    Interlocked.Increment(ref secondInvokeCounter);
                }
            }

            threadHelper.ExecuteSynchronousOrOnThread(
                () =>
                {
                    if (configureAwaitCancelType == ConfigureAwaitCancelType.CancelFirst)
                    {
                        configureAwaitCancelationSource1.Cancel();
                    }
                    tryCompleter1();
                },
                firstReportType == SynchronizationType.Foreground);

            TestHelper.ExecuteForegroundCallbacksAndWaitForThreadsToComplete();
            Assert.AreEqual(expectedInvokes, firstInvokeCounter);

            threadHelper.ExecuteSynchronousOrOnThread(
                () =>
                {
                    if (configureAwaitCancelType == ConfigureAwaitCancelType.CancelSecond)
                    {
                        configureAwaitCancelationSource2.Cancel();
                    }
                    tryCompleter2();
                },
                secondReportType == SynchronizationType.Foreground);
            TestHelper.ExecuteForegroundCallbacks();

            // We must execute foreground context on every spin to account for the race condition between firstInvokeCounter being incremented and the configured promise being awaited.
            TestHelper.SpinUntilWhileExecutingForegroundContext(() => secondInvokeCounter == expectedInvokes, timeout,
                $"expectedInvokes: {expectedInvokes}, secondInvokeCounter: {secondInvokeCounter}");

            firstPromise.Forget();
            secondPromise.Forget();
            configureAwaitCancelationSource1.TryDispose();
            configureAwaitCancelationSource2.TryDispose();
        }

        [Test, TestCaseSource(nameof(GetArgs_ContinueWith))]
        public void CallbacksWillBeInvokedOnTheCorrectSynchronizationContext_Await_T(
            CompleteType firstCompleteType,
            CompleteType secondCompleteType,
            SynchronizationType firstWaitType,
            SynchronizationType secondWaitType,
            SynchronizationType firstReportType,
            SynchronizationType secondReportType,
            ConfigureAwaitCancelType configureAwaitCancelType,
            bool isFirstAlreadyComplete,
            bool isSecondAlreadyComplete)
        {
            var foregroundThread = Thread.CurrentThread;
            var threadHelper = new ThreadHelper();

            var configureAwaitCancelationSource1 = CancelationSource.New();
            var configureAwaitCancelationToken1 = configureAwaitCancelType == ConfigureAwaitCancelType.NoToken ? default(CancelationToken)
                : configureAwaitCancelType == ConfigureAwaitCancelType.AlreadyCanceled ? CancelationToken.Canceled()
                : configureAwaitCancelationSource1.Token;
            var configureAwaitCancelationSource2 = CancelationSource.New();
            var configureAwaitCancelationToken2 = configureAwaitCancelType == ConfigureAwaitCancelType.NoToken ? default(CancelationToken)
                : configureAwaitCancelType == ConfigureAwaitCancelType.AlreadyCanceled ? CancelationToken.Canceled()
                : configureAwaitCancelationSource2.Token;

            Promise<int> firstPromise = TestHelper.BuildPromise(firstCompleteType, isFirstAlreadyComplete, 1, rejectValue, out var tryCompleter1);
            Promise<int> secondPromise = TestHelper.BuildPromise(secondCompleteType, isSecondAlreadyComplete, 1, rejectValue, out var tryCompleter2);

            firstPromise = firstPromise.Preserve();
            secondPromise = secondPromise.Preserve();

            int firstInvokeCounter = 0;
            int secondInvokeCounter = 0;

            int expectedInvokes = 0;
            bool hasRaceCondition = firstWaitType == (SynchronizationType) SynchronizationOption.Background && secondWaitType == SynchronizationType.Synchronous && !isSecondAlreadyComplete;

            foreach (var p1 in TestHelper.GetTestablePromises(firstPromise))
            {
                ++expectedInvokes;
                RunAsync(p1, secondPromise).Forget();
            }
            foreach (var p2 in TestHelper.GetTestablePromises(secondPromise))
            {
                ++expectedInvokes;
                RunAsync(firstPromise, p2).Forget();
            }

            async Promise RunAsync(Promise<int> p1, Promise<int> p2)
            {
                try
                {
                    _ = await p1.ConfigureAwait((ConfigureAwaitType) firstWaitType, true, configureAwaitCancelationToken1);
                }
                catch { }
                finally
                {
                    TestHelper.AssertCallbackContext(firstWaitType, configureAwaitCancelType == ConfigureAwaitCancelType.AlreadyCanceled ? firstWaitType : firstReportType, foregroundThread);
                    Interlocked.Increment(ref firstInvokeCounter);
                }

                try
                {
                    _ = await p2.ConfigureAwait((ConfigureAwaitType) secondWaitType, false, configureAwaitCancelationToken2);
                }
                catch { }
                finally
                {
                    // If there's a race condition, p2 could be completed on a separate thread before it's awaited, causing the assert to fail.
                    // This only matters for SynchronizationOption.Synchronous, for which the caller does not care on what context it executes.
                    if (!hasRaceCondition)
                    {
                        TestHelper.AssertCallbackContext(secondWaitType, configureAwaitCancelType == ConfigureAwaitCancelType.AlreadyCanceled ? secondWaitType : secondReportType, foregroundThread);
                    }
                    Interlocked.Increment(ref secondInvokeCounter);
                }
            }

            threadHelper.ExecuteSynchronousOrOnThread(
                () =>
                {
                    if (configureAwaitCancelType == ConfigureAwaitCancelType.CancelFirst)
                    {
                        configureAwaitCancelationSource1.Cancel();
                    }
                    tryCompleter1();
                },
                firstReportType == SynchronizationType.Foreground);

            TestHelper.ExecuteForegroundCallbacksAndWaitForThreadsToComplete();
            Assert.AreEqual(expectedInvokes, firstInvokeCounter);

            threadHelper.ExecuteSynchronousOrOnThread(
                () =>
                {
                    if (configureAwaitCancelType == ConfigureAwaitCancelType.CancelSecond)
                    {
                        configureAwaitCancelationSource2.Cancel();
                    }
                    tryCompleter2();
                },
                secondReportType == SynchronizationType.Foreground);
            TestHelper.ExecuteForegroundCallbacks();

            // We must execute foreground context on every spin to account for the race condition between firstInvokeCounter being incremented and the configured promise being awaited.
            TestHelper.SpinUntilWhileExecutingForegroundContext(() => secondInvokeCounter == expectedInvokes, timeout,
                $"expectedInvokes: {expectedInvokes}, secondInvokeCounter: {secondInvokeCounter}");

            firstPromise.Forget();
            secondPromise.Forget();
            configureAwaitCancelationSource1.TryDispose();
            configureAwaitCancelationSource2.TryDispose();
        }

        [Test]
        public void WaitAsyncForceAsync_CallbacksWillBeInvokedProperly_await_void(
            [Values(ConfigureAwaitType.Foreground, ConfigureAwaitType.Explicit
#if !UNITY_WEBGL
                , ConfigureAwaitType.Background
#endif
            )] ConfigureAwaitType waitType,
            [Values] bool forceAsync,
            [Values] bool isAlreadyComplete)
        {
            var foregroundThread = Thread.CurrentThread;

            // We're testing an implementation detail that if forceAsync is false, the continuation is invoked synchronously if the current context is the same.
            // It may still be executed asynchronously if forceAsync is false. But if it's true, it is guaranteed to be invoked asynchronously.
            // Lock helps us assert that the callback is not invoked synchronously if forceAsync is true.
            var lockObj = new object();
            bool didInvoke = false;

            Action action = () =>
            {
                lock (lockObj)
                {
                    var promise = TestHelper.BuildPromise(CompleteType.Resolve, isAlreadyComplete, rejectValue, out var tryCompleter);

                    Await().Forget();

                    async Promise Await()
                    {
                        await promise.ConfigureAwait(waitType, forceAsync);

                        TestHelper.AssertCallbackContext((SynchronizationType) waitType, (SynchronizationType) waitType, foregroundThread);
                        lock (lockObj)
                        {
                            didInvoke = true;
                        }
                    }

                    tryCompleter();
                    Assert.AreNotEqual(forceAsync, didInvoke);
                }
            };

            if (waitType == ConfigureAwaitType.Explicit)
            {
                Promise.Run(action, TestHelper._foregroundContext).Forget();
            }
            else
            {
                Promise.Run(action, (SynchronizationOption) waitType).Forget();
            }

            TestHelper.ExecuteForegroundCallbacksAndWaitForThreadsToComplete();

            TestHelper.SpinUntil(() => didInvoke, timeout, $"didInvoke: {didInvoke}");
        }

        [Test]
        public void WaitAsyncForceAsync_CallbacksWillBeInvokedProperly_await_T(
            [Values(ConfigureAwaitType.Foreground, ConfigureAwaitType.Explicit
#if !UNITY_WEBGL
                , ConfigureAwaitType.Background
#endif
            )] ConfigureAwaitType waitType,
            [Values] bool forceAsync,
            [Values] bool isAlreadyComplete)
        {
            var foregroundThread = Thread.CurrentThread;

            // We're testing an implementation detail that if forceAsync is false, the continuation is invoked synchronously if the current context is the same.
            // It may still be executed asynchronously if forceAsync is false. But if it's true, it is guaranteed to be invoked asynchronously.
            // Lock helps us assert that the callback is not invoked synchronously if forceAsync is true.
            var lockObj = new object();
            bool didInvoke = false;

            Action action = () =>
            {
                lock (lockObj)
                {
                    var promise = TestHelper.BuildPromise(CompleteType.Resolve, isAlreadyComplete, 1, rejectValue, out var tryCompleter);

                    Await().Forget();

                    async Promise Await()
                    {
                        _ = await promise.ConfigureAwait(waitType, forceAsync);

                        TestHelper.AssertCallbackContext((SynchronizationType) waitType, (SynchronizationType) waitType, foregroundThread);
                        lock (lockObj)
                        {
                            didInvoke = true;
                        }
                    }

                    tryCompleter();
                    Assert.AreNotEqual(forceAsync, didInvoke);
                }
            };

            if (waitType == ConfigureAwaitType.Explicit)
            {
                Promise.Run(action, TestHelper._foregroundContext).Forget();
            }
            else
            {
                Promise.Run(action, (SynchronizationOption) waitType).Forget();
            }

            TestHelper.ExecuteForegroundCallbacksAndWaitForThreadsToComplete();

            TestHelper.SpinUntil(() => didInvoke, timeout, $"didInvoke: {didInvoke}");
        }

        [Test]
        public void WaitAsyncWithCancelationTokenWillBeCompletedProperly_void(
            [Values] CompleteType completeType,
            [Values(ConfigureAwaitCancelType.NoToken, ConfigureAwaitCancelType.WithToken_NoCancel, ConfigureAwaitCancelType.AlreadyCanceled, ConfigureAwaitCancelType.CancelFirst)] ConfigureAwaitCancelType waitAsyncCancelType,
            [Values] bool isAlreadyComplete)
        {
            var foregroundThread = Thread.CurrentThread;

            var waitAsyncCancelationSource = CancelationSource.New();
            var waitAsyncCancelationToken = waitAsyncCancelType == ConfigureAwaitCancelType.NoToken ? default(CancelationToken)
                : waitAsyncCancelType == ConfigureAwaitCancelType.AlreadyCanceled ? CancelationToken.Canceled()
                : waitAsyncCancelationSource.Token;

            Promise promise = TestHelper.BuildPromise(completeType, isAlreadyComplete, rejectValue, out var tryCompleter);

            promise = promise.Preserve();

            int invokeCounter = 0;
            int expectedInvokes = 0;
            bool cancelationExpected = waitAsyncCancelType == ConfigureAwaitCancelType.AlreadyCanceled || (waitAsyncCancelType == ConfigureAwaitCancelType.CancelFirst && !isAlreadyComplete);
            var expectedCompleteState = cancelationExpected ? Promise.State.Canceled : (Promise.State) completeType;

            foreach (var p in TestHelper.GetTestablePromises(promise))
            {
                ++expectedInvokes;
                p.WaitAsync(waitAsyncCancelationToken)
                    .ContinueWith(container =>
                    {
                        Assert.AreEqual(expectedCompleteState, container.State);
                        Interlocked.Increment(ref invokeCounter);
                    })
                    .Forget();
            }

            foreach (var p in TestHelper.GetTestablePromises(promise))
            {
                ++expectedInvokes;
                RunAsync(p).Forget();
            }

            async Promise RunAsync(Promise p)
            {
                var actualCompleteState = Promise.State.Pending;
                try
                {
                    await p.WaitAsync(waitAsyncCancelationToken);
                    actualCompleteState = Promise.State.Resolved;
                }
                catch (OperationCanceledException)
                {
                    actualCompleteState = Promise.State.Canceled;
                }
                catch (Exception)
                {
                    actualCompleteState = Promise.State.Rejected;
                }

                Assert.AreEqual(expectedCompleteState, actualCompleteState);
                Interlocked.Increment(ref invokeCounter);
            }

            if (waitAsyncCancelType == ConfigureAwaitCancelType.CancelFirst)
            {
                waitAsyncCancelationSource.Cancel();
            }
            tryCompleter();

            Assert.AreEqual(expectedInvokes, invokeCounter);

            promise.Forget();
            waitAsyncCancelationSource.TryDispose();
        }

        [Test]
        public void WaitAsyncWithCancelationTokenWillBeCompletedProperly_T(
            [Values] CompleteType completeType,
            [Values(ConfigureAwaitCancelType.NoToken, ConfigureAwaitCancelType.WithToken_NoCancel, ConfigureAwaitCancelType.AlreadyCanceled, ConfigureAwaitCancelType.CancelFirst)] ConfigureAwaitCancelType waitAsyncCancelType,
            [Values] bool isAlreadyComplete)
        {
            var foregroundThread = Thread.CurrentThread;

            var waitAsyncCancelationSource = CancelationSource.New();
            var waitAsyncCancelationToken = waitAsyncCancelType == ConfigureAwaitCancelType.NoToken ? default(CancelationToken)
                : waitAsyncCancelType == ConfigureAwaitCancelType.AlreadyCanceled ? CancelationToken.Canceled()
                : waitAsyncCancelationSource.Token;

            Promise<int> promise = TestHelper.BuildPromise(completeType, isAlreadyComplete, 1, rejectValue, out var tryCompleter);

            promise = promise.Preserve();

            int invokeCounter = 0;
            int expectedInvokes = 0;
            bool cancelationExpected = waitAsyncCancelType == ConfigureAwaitCancelType.AlreadyCanceled || (waitAsyncCancelType == ConfigureAwaitCancelType.CancelFirst && !isAlreadyComplete);
            var expectedCompleteState = cancelationExpected ? Promise.State.Canceled : (Promise.State) completeType;

            foreach (var p in TestHelper.GetTestablePromises(promise))
            {
                ++expectedInvokes;
                p.WaitAsync(waitAsyncCancelationToken)
                    .ContinueWith(container =>
                    {
                        Assert.AreEqual(expectedCompleteState, container.State);
                        Interlocked.Increment(ref invokeCounter);
                    })
                    .Forget();
            }

            foreach (var p in TestHelper.GetTestablePromises(promise))
            {
                ++expectedInvokes;
                RunAsync(p).Forget();
            }

            async Promise RunAsync(Promise<int> p)
            {
                var actualCompleteState = Promise.State.Pending;
                try
                {
                    _ = await p.WaitAsync(waitAsyncCancelationToken);
                    actualCompleteState = Promise.State.Resolved;
                }
                catch (OperationCanceledException)
                {
                    actualCompleteState = Promise.State.Canceled;
                }
                catch (Exception)
                {
                    actualCompleteState = Promise.State.Rejected;
                }

                Assert.AreEqual(expectedCompleteState, actualCompleteState);
                Interlocked.Increment(ref invokeCounter);
            }

            if (waitAsyncCancelType == ConfigureAwaitCancelType.CancelFirst)
            {
                waitAsyncCancelationSource.Cancel();
            }
            tryCompleter();

            Assert.AreEqual(expectedInvokes, invokeCounter);

            promise.Forget();
            waitAsyncCancelationSource.TryDispose();
        }
    }
}