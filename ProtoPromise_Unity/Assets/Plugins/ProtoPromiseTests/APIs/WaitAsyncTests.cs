#if !PROTO_PROMISE_PROGRESS_DISABLE
#define PROMISE_PROGRESS
#else
#undef PROMISE_PROGRESS
# endif

using NUnit.Framework;
using Proto.Promises;
using ProtoPromiseTests.Threading;
using System;
using System.Collections.Generic;
using System.Threading;

namespace ProtoPromiseTests.APIs
{
    public class WaitAsyncTests
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

        private static IEnumerable<TestCaseData> GetArgs(CompleteType[] completeTypes)
        {
            SynchronizationType[] synchronizationTypes = new SynchronizationType[]
            {
                SynchronizationType.Synchronous,
                //SynchronizationType.Foreground, // Ignore foreground to reduce number of tests, testing explicit is the same.
#if !UNITY_WEBGL
                SynchronizationType.Background,
#endif
                SynchronizationType.Explicit
            };
            SynchronizationType[] reportTypes = new SynchronizationType[]
            {
                SynchronizationType.Foreground
#if !UNITY_WEBGL
                , SynchronizationType.Background
#endif
            };
            SynchronizationType[] foregroundOnlyReportType = new SynchronizationType[] { SynchronizationType.Foreground };
            bool[] alreadyCompletes = new bool[] { true, false };

            CompleteType[] secondCompleteTypes = new CompleteType[] { CompleteType.Resolve }; // Just use a single value to reduce number of tests.

            foreach (CompleteType firstCompleteType in completeTypes)
            foreach (CompleteType secondCompleteType in secondCompleteTypes)
            foreach (bool isFirstComplete in alreadyCompletes)
            foreach (bool isSecondComplete in alreadyCompletes)
            foreach (SynchronizationType firstWaitType in synchronizationTypes)
            foreach (SynchronizationType secondWaitType in synchronizationTypes)
            foreach (SynchronizationType firstReportType in isFirstComplete ? foregroundOnlyReportType : reportTypes)
            {
                var secondReportTypes = !isSecondComplete
                    ? reportTypes
                    : new SynchronizationType[]
                    {
                        firstWaitType == SynchronizationType.Synchronous
                            ? firstReportType
                            : firstWaitType == (SynchronizationType) SynchronizationOption.Background
                                ? (SynchronizationType) SynchronizationOption.Background
                                : SynchronizationType.Foreground
                    };
                foreach (SynchronizationType secondReportType in secondReportTypes)
                {
                    yield return new TestCaseData(firstCompleteType, secondCompleteType, firstWaitType, secondWaitType, firstReportType, secondReportType, isFirstComplete, isSecondComplete);
                }
            }
        }

        private static IEnumerable<TestCaseData> GetArgs_ResolveReject()
        {
            return GetArgs(new CompleteType[]
            {
                CompleteType.Resolve,
                CompleteType.Reject
            });
        }

        private static IEnumerable<TestCaseData> GetArgs_ContinueWith()
        {
            return GetArgs(new CompleteType[]
            {
                CompleteType.Resolve,
                CompleteType.Reject,
                CompleteType.Cancel
            });
        }

        private static IEnumerable<TestCaseData> GetArgs_CancelFinally()
        {
            CompleteType[] completeTypes = new CompleteType[]
            {
                CompleteType.Cancel,
                CompleteType.CancelFromToken
            };
            SynchronizationType[] synchronizationTypes = new SynchronizationType[]
            {
                SynchronizationType.Synchronous,
                SynchronizationType.Foreground,
#if !UNITY_WEBGL
                SynchronizationType.Background,
#endif
                SynchronizationType.Explicit
            };
            SynchronizationType[] reportTypes = new SynchronizationType[]
            {
                SynchronizationType.Foreground
#if !UNITY_WEBGL
                , SynchronizationType.Background
#endif
            };
            SynchronizationType[] foregroundOnlyReportType = new SynchronizationType[] { SynchronizationType.Foreground };
            bool[] alreadyCompletes = new bool[] { true, false };

            foreach (CompleteType firstCompleteType in completeTypes)
            foreach (bool isComplete in alreadyCompletes)
            foreach (SynchronizationType waitType in synchronizationTypes)
            foreach (SynchronizationType reportType in isComplete ? foregroundOnlyReportType : reportTypes)
            {
                yield return new TestCaseData(firstCompleteType, waitType, reportType, isComplete);
            }
        }

        private static void BuildPromise<TReject>(CompleteType completeType, bool isAlreadyComplete, TReject reason, out Promise.Deferred deferred, out CancelationSource cancelationSource, out Promise promise)
        {
            if (!isAlreadyComplete)
            {
                deferred = TestHelper.GetNewDeferredVoid(completeType, out cancelationSource);
                promise = deferred.Promise;
                return;
            }

            deferred = default(Promise.Deferred);
            cancelationSource = default(CancelationSource);
            switch (completeType)
            {
                case CompleteType.Resolve:
                {
                    promise = Promise.Resolved();
                    break;
                }
                case CompleteType.Reject:
                {
                    promise = Promise.Rejected(reason);
                    break;
                }
                default:
                {
                    promise = Promise.Canceled();
                    break;
                }
            }
        }

        private static void BuildPromise<T, TReject>(CompleteType completeType, bool isAlreadyComplete, T value, TReject reason, out Promise<T>.Deferred deferred, out CancelationSource cancelationSource, out Promise<T> promise)
        {
            if (!isAlreadyComplete)
            {
                deferred = TestHelper.GetNewDeferredT<T>(completeType, out cancelationSource);
                promise = deferred.Promise;
                return;
            }

            deferred = default(Promise<T>.Deferred);
            cancelationSource = default(CancelationSource);
            switch (completeType)
            {
                case CompleteType.Resolve:
                {
                    promise = Promise.Resolved(value);
                    break;
                }
                case CompleteType.Reject:
                {
                    promise = Promise<T>.Rejected(reason);
                    break;
                }
                default:
                {
                    promise = Promise<T>.Canceled();
                    break;
                }
            }
        }

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

        [Test, TestCaseSource("GetArgs_ResolveReject")]
        public void CallbacksWillBeInvokedOnTheCorrectSynchronizationContext_ResolveReject_void(
            CompleteType firstCompleteType,
            CompleteType secondCompleteType,
            SynchronizationType firstWaitType,
            SynchronizationType secondWaitType,
            SynchronizationType firstReportType,
            SynchronizationType secondReportType,
            bool isFirstAlreadyComplete,
            bool isSecondAlreadyComplete)
        {
            Thread foregroundThread = Thread.CurrentThread;
            ThreadHelper threadHelper = new ThreadHelper();

            CancelationSource firstCancelationSource;
            Promise.Deferred firstDeferred;
            Promise firstPromise;
            CancelationSource secondCancelationSource;
            Promise<int>.Deferred secondDeferred;
            Promise<int> secondPromise;

            string rejectReason = "Fail";

            BuildPromise(firstCompleteType, isFirstAlreadyComplete, rejectReason, out firstDeferred, out firstCancelationSource, out firstPromise);
            BuildPromise(secondCompleteType, isSecondAlreadyComplete, 1, rejectReason, out secondDeferred, out secondCancelationSource, out secondPromise);

            firstPromise = firstPromise.Preserve();
            secondPromise = secondPromise.Preserve();

            int firstInvokeCounter = 0;
            int secondInvokeCounter = 0;

            int expectedFirstInvokes = 0;
            int expectedSecondInvokes = 0;

            Action onFirstCallback = () =>
            {
                Interlocked.Increment(ref firstInvokeCounter);
                TestHelper.AssertCallbackContext(firstWaitType, firstReportType, foregroundThread);
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

            if (firstCompleteType == CompleteType.Resolve)
            {
                TestHelper.AddResolveCallbacks<int, string>(firstPromise,
                    onResolve: () => onFirstCallback(),
                    promiseToPromise: p => secondPromise.ConfigureAwait((ConfigureAwaitType) secondWaitType),
                    promiseToPromiseConvert: p => secondPromise.ConfigureAwait((ConfigureAwaitType) secondWaitType),
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
                    configureAwaitType: (ConfigureAwaitType) firstWaitType
                );
            }
            TestHelper.AddCallbacks<int, object, string>(firstPromise,
                onResolve: () => onFirstCallback(),
                onReject: r => onFirstCallback(),
                onUnknownRejection: () => onFirstCallback(),
                promiseToPromise: p => secondPromise.ConfigureAwait((ConfigureAwaitType) secondWaitType),
                promiseToPromiseConvert: p => secondPromise.ConfigureAwait((ConfigureAwaitType) secondWaitType),
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
                    if (firstCompleteType == CompleteType.Reject)
                    {
                        promise = HookupSecondVoid(promise);
                    }
                },
                configureAwaitType: (ConfigureAwaitType) firstWaitType
            );

            threadHelper.ExecuteSynchronousOrOnThread(
                () => TestHelper.GetTryCompleterVoid(firstCompleteType, rejectReason).Invoke(firstDeferred, firstCancelationSource),
                firstReportType == SynchronizationType.Foreground);
            TestHelper.ExecuteForegroundCallbacks();

            if (firstWaitType != (SynchronizationType) SynchronizationOption.Background)
            {
                Assert.AreEqual(expectedFirstInvokes, firstInvokeCounter);
            }
            else
            {
                if (!SpinWait.SpinUntil(() => firstInvokeCounter == expectedFirstInvokes, TimeSpan.FromSeconds(expectedFirstInvokes)))
                {
                    Assert.Fail("Timed out after " + TimeSpan.FromSeconds(expectedFirstInvokes) + ", expectedFirstInvokes: " + expectedFirstInvokes + ", firstInvokeCounter: " + firstInvokeCounter);
                }
            }

            threadHelper.ExecuteSynchronousOrOnThread(
                () => TestHelper.GetTryCompleterT(secondCompleteType, 1, rejectReason).Invoke(secondDeferred, secondCancelationSource),
                secondReportType == SynchronizationType.Foreground);
            TestHelper.ExecuteForegroundCallbacks();

            if (!SpinWait.SpinUntil(() =>
                {
                    // We must execute foreground context on every spin to account for the race condition between firstInvokeCounter being incremented and the configured promise returning in the callback.
                    TestHelper.ExecuteForegroundCallbacks();
                    return secondInvokeCounter == expectedSecondInvokes;
                }, TimeSpan.FromSeconds(expectedSecondInvokes)))
            {
                Assert.Fail("Timed out after " + TimeSpan.FromSeconds(expectedSecondInvokes) + ", expectedSecondInvokes: " + expectedSecondInvokes + ", secondInvokeCounter: " + secondInvokeCounter);
            }

            firstPromise.Forget();
            secondPromise.Forget();
            firstCancelationSource.TryDispose();
            secondCancelationSource.TryDispose();
        }

        [Test, TestCaseSource("GetArgs_ResolveReject")]
        public void CallbacksWillBeInvokedOnTheCorrectSynchronizationContext_ResolveReject_T(
            CompleteType firstCompleteType,
            CompleteType secondCompleteType,
            SynchronizationType firstWaitType,
            SynchronizationType secondWaitType,
            SynchronizationType firstReportType,
            SynchronizationType secondReportType,
            bool isFirstAlreadyComplete,
            bool isSecondAlreadyComplete)
        {
            Thread foregroundThread = Thread.CurrentThread;
            ThreadHelper threadHelper = new ThreadHelper();

            CancelationSource firstCancelationSource;
            Promise<int>.Deferred firstDeferred;
            Promise<int> firstPromise;
            CancelationSource secondCancelationSource;
            Promise<int>.Deferred secondDeferred;
            Promise<int> secondPromise;

            string rejectReason = "Fail";

            BuildPromise(firstCompleteType, isFirstAlreadyComplete, 1, rejectReason, out firstDeferred, out firstCancelationSource, out firstPromise);
            BuildPromise(secondCompleteType, isSecondAlreadyComplete, 1, rejectReason, out secondDeferred, out secondCancelationSource, out secondPromise);

            firstPromise = firstPromise.Preserve();
            secondPromise = secondPromise.Preserve();

            int firstInvokeCounter = 0;
            int secondInvokeCounter = 0;

            int expectedFirstInvokes = 0;
            int expectedSecondInvokes = 0;

            Action onFirstCallback = () =>
            {
                Interlocked.Increment(ref firstInvokeCounter);
                TestHelper.AssertCallbackContext(firstWaitType, firstReportType, foregroundThread);
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

            if (firstCompleteType == CompleteType.Resolve)
            {
                TestHelper.AddResolveCallbacks<int, int, string>(firstPromise,
                    onResolve: v => onFirstCallback(),
                    promiseToPromise: p => secondPromise.ConfigureAwait((ConfigureAwaitType) secondWaitType),
                    promiseToPromiseConvert: p => secondPromise.ConfigureAwait((ConfigureAwaitType) secondWaitType),
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
                    configureAwaitType: (ConfigureAwaitType) firstWaitType
                );
            }
            TestHelper.AddCallbacks<int, int, object, string>(firstPromise,
                onResolve: v => onFirstCallback(),
                onReject: r => onFirstCallback(),
                onUnknownRejection: () => onFirstCallback(),
                promiseToPromise: p => secondPromise.ConfigureAwait((ConfigureAwaitType) secondWaitType),
                promiseToPromiseConvert: p => secondPromise.ConfigureAwait((ConfigureAwaitType) secondWaitType),
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
                    if (firstCompleteType == CompleteType.Reject)
                    {
                        promise = HookupSecondT(promise);
                    }
                },
                configureAwaitType: (ConfigureAwaitType) firstWaitType
            );

            threadHelper.ExecuteSynchronousOrOnThread(
                () => TestHelper.GetTryCompleterT(firstCompleteType, 1, rejectReason).Invoke(firstDeferred, firstCancelationSource),
                firstReportType == SynchronizationType.Foreground);
            TestHelper.ExecuteForegroundCallbacks();

            if (firstWaitType != (SynchronizationType) SynchronizationOption.Background)
            {
                Assert.AreEqual(expectedFirstInvokes, firstInvokeCounter);
            }
            else
            {
                if (!SpinWait.SpinUntil(() => firstInvokeCounter == expectedFirstInvokes, TimeSpan.FromSeconds(expectedFirstInvokes)))
                {
                    Assert.Fail("Timed out after " + TimeSpan.FromSeconds(expectedFirstInvokes) + ", expectedFirstInvokes: " + expectedFirstInvokes + ", firstInvokeCounter: " + firstInvokeCounter);
                }
            }

            threadHelper.ExecuteSynchronousOrOnThread(
                () => TestHelper.GetTryCompleterT(secondCompleteType, 1, rejectReason).Invoke(secondDeferred, secondCancelationSource),
                secondReportType == SynchronizationType.Foreground);
            TestHelper.ExecuteForegroundCallbacks();

            if (!SpinWait.SpinUntil(() =>
                {
                    // We must execute foreground context on every spin to account for the race condition between firstInvokeCounter being incremented and the configured promise returning in the callback.
                    TestHelper.ExecuteForegroundCallbacks();
                    return secondInvokeCounter == expectedSecondInvokes;
                }, TimeSpan.FromSeconds(expectedSecondInvokes)))
            {
                Assert.Fail("Timed out after " + TimeSpan.FromSeconds(expectedSecondInvokes) + ", expectedSecondInvokes: " + expectedSecondInvokes + ", secondInvokeCounter: " + secondInvokeCounter);
            }

            firstPromise.Forget();
            secondPromise.Forget();
            firstCancelationSource.TryDispose();
            secondCancelationSource.TryDispose();
        }

        [Test, TestCaseSource("GetArgs_ContinueWith")]
        public void CallbacksWillBeInvokedOnTheCorrectSynchronizationContext_ContinueWith_void(
            CompleteType firstCompleteType,
            CompleteType secondCompleteType,
            SynchronizationType firstWaitType,
            SynchronizationType secondWaitType,
            SynchronizationType firstReportType,
            SynchronizationType secondReportType,
            bool isFirstAlreadyComplete,
            bool isSecondAlreadyComplete)
        {
            Thread foregroundThread = Thread.CurrentThread;
            ThreadHelper threadHelper = new ThreadHelper();

            CancelationSource firstCancelationSource;
            Promise.Deferred firstDeferred;
            Promise firstPromise;
            CancelationSource secondCancelationSource;
            Promise<int>.Deferred secondDeferred;
            Promise<int> secondPromise;

            string rejectReason = "Fail";

            BuildPromise(firstCompleteType, isFirstAlreadyComplete, rejectReason, out firstDeferred, out firstCancelationSource, out firstPromise);
            BuildPromise(secondCompleteType, isSecondAlreadyComplete, 1, rejectReason, out secondDeferred, out secondCancelationSource, out secondPromise);

            firstPromise = firstPromise.Preserve();
            secondPromise = secondPromise.Preserve();

            int firstInvokeCounter = 0;
            int secondInvokeCounter = 0;

            int expectedFirstInvokes = 0;
            int expectedSecondInvokes = 0;

            Action onFirstCallback = () =>
            {
                Interlocked.Increment(ref firstInvokeCounter);
                TestHelper.AssertCallbackContext(firstWaitType, firstReportType, foregroundThread);
            };
            Action onSecondCallback = () =>
            {
                // We can't assert the context due to thread race conditions, just make sure the callback is invoked.
                Interlocked.Increment(ref secondInvokeCounter);
            };

            TestHelper.AddContinueCallbacks<int, string>(firstPromise,
                onContinue: _ => onFirstCallback(),
                promiseToPromise: p => secondPromise.ConfigureAwait((ConfigureAwaitType) secondWaitType),
                promiseToPromiseConvert: p => secondPromise.ConfigureAwait((ConfigureAwaitType) secondWaitType),
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
                configureAwaitType: (ConfigureAwaitType) firstWaitType
            );

            threadHelper.ExecuteSynchronousOrOnThread(
                () => TestHelper.GetTryCompleterVoid(firstCompleteType, rejectReason).Invoke(firstDeferred, firstCancelationSource),
                firstReportType == SynchronizationType.Foreground);
            TestHelper.ExecuteForegroundCallbacks();

            if (firstWaitType != (SynchronizationType) SynchronizationOption.Background)
            {
                Assert.AreEqual(expectedFirstInvokes, firstInvokeCounter);
            }
            else
            {
                if (!SpinWait.SpinUntil(() => firstInvokeCounter == expectedFirstInvokes, TimeSpan.FromSeconds(expectedFirstInvokes)))
                {
                    Assert.Fail("Timed out after " + TimeSpan.FromSeconds(expectedFirstInvokes) + ", expectedFirstInvokes: " + expectedFirstInvokes + ", firstInvokeCounter: " + firstInvokeCounter);
                }
            }

            threadHelper.ExecuteSynchronousOrOnThread(
                () => TestHelper.GetTryCompleterT(secondCompleteType, 1, rejectReason).Invoke(secondDeferred, secondCancelationSource),
                secondReportType == SynchronizationType.Foreground);
            TestHelper.ExecuteForegroundCallbacks();

            if (!SpinWait.SpinUntil(() =>
                {
                    // We must execute foreground context on every spin to account for the race condition between firstInvokeCounter being incremented and the configured promise returning in the callback.
                    TestHelper.ExecuteForegroundCallbacks();
                    return secondInvokeCounter == expectedSecondInvokes;
                }, TimeSpan.FromSeconds(expectedSecondInvokes)))
            {
                Assert.Fail("Timed out after " + TimeSpan.FromSeconds(expectedSecondInvokes) + ", expectedSecondInvokes: " + expectedSecondInvokes + ", secondInvokeCounter: " + secondInvokeCounter);
            }

            firstPromise.Forget();
            secondPromise.Forget();
            firstCancelationSource.TryDispose();
            secondCancelationSource.TryDispose();
        }

        [Test, TestCaseSource("GetArgs_ContinueWith")]
        public void CallbacksWillBeInvokedOnTheCorrectSynchronizationContext_ContinueWith_T(
            CompleteType firstCompleteType,
            CompleteType secondCompleteType,
            SynchronizationType firstWaitType,
            SynchronizationType secondWaitType,
            SynchronizationType firstReportType,
            SynchronizationType secondReportType,
            bool isFirstAlreadyComplete,
            bool isSecondAlreadyComplete)
        {
            Thread foregroundThread = Thread.CurrentThread;
            ThreadHelper threadHelper = new ThreadHelper();

            CancelationSource firstCancelationSource;
            Promise<int>.Deferred firstDeferred;
            Promise<int> firstPromise;
            CancelationSource secondCancelationSource;
            Promise<int>.Deferred secondDeferred;
            Promise<int> secondPromise;

            string rejectReason = "Fail";

            BuildPromise(firstCompleteType, isFirstAlreadyComplete, 1, rejectReason, out firstDeferred, out firstCancelationSource, out firstPromise);
            BuildPromise(secondCompleteType, isSecondAlreadyComplete, 1, rejectReason, out secondDeferred, out secondCancelationSource, out secondPromise);

            firstPromise = firstPromise.Preserve();
            secondPromise = secondPromise.Preserve();

            int firstInvokeCounter = 0;
            int secondInvokeCounter = 0;

            int expectedFirstInvokes = 0;
            int expectedSecondInvokes = 0;

            Action onFirstCallback = () =>
            {
                Interlocked.Increment(ref firstInvokeCounter);
                TestHelper.AssertCallbackContext(firstWaitType, firstReportType, foregroundThread);
            };
            Action onSecondCallback = () =>
            {
                // We can't assert the context due to thread race conditions, just make sure the callback is invoked.
                Interlocked.Increment(ref secondInvokeCounter);
            };

            TestHelper.AddContinueCallbacks<int, int, string>(firstPromise,
                onContinue: _ => onFirstCallback(),
                promiseToPromise: p => secondPromise.ConfigureAwait((ConfigureAwaitType) secondWaitType),
                promiseToPromiseConvert: p => secondPromise.ConfigureAwait((ConfigureAwaitType) secondWaitType),
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
                configureAwaitType: (ConfigureAwaitType) firstWaitType
            );

            threadHelper.ExecuteSynchronousOrOnThread(
                () => TestHelper.GetTryCompleterT(firstCompleteType, 1, rejectReason).Invoke(firstDeferred, firstCancelationSource),
                firstReportType == SynchronizationType.Foreground);
            TestHelper.ExecuteForegroundCallbacks();

            if (firstWaitType != (SynchronizationType) SynchronizationOption.Background)
            {
                Assert.AreEqual(expectedFirstInvokes, firstInvokeCounter);
            }
            else
            {
                if (!SpinWait.SpinUntil(() => firstInvokeCounter == expectedFirstInvokes, TimeSpan.FromSeconds(expectedFirstInvokes)))
                {
                    Assert.Fail("Timed out after " + TimeSpan.FromSeconds(expectedFirstInvokes) + ", expectedFirstInvokes: " + expectedFirstInvokes + ", firstInvokeCounter: " + firstInvokeCounter);
                }
            }

            threadHelper.ExecuteSynchronousOrOnThread(
                () => TestHelper.GetTryCompleterT(secondCompleteType, 1, rejectReason).Invoke(secondDeferred, secondCancelationSource),
                secondReportType == SynchronizationType.Foreground);
            TestHelper.ExecuteForegroundCallbacks();

            if (!SpinWait.SpinUntil(() =>
                {
                    // We must execute foreground context on every spin to account for the race condition between firstInvokeCounter being incremented and the configured promise returning in the callback.
                    TestHelper.ExecuteForegroundCallbacks();
                    return secondInvokeCounter == expectedSecondInvokes;
                }, TimeSpan.FromSeconds(expectedSecondInvokes)))
            {
                Assert.Fail("Timed out after " + TimeSpan.FromSeconds(expectedSecondInvokes) + ", expectedSecondInvokes: " + expectedSecondInvokes + ", secondInvokeCounter: " + secondInvokeCounter);
            }

            firstPromise.Forget();
            secondPromise.Forget();
            firstCancelationSource.TryDispose();
            secondCancelationSource.TryDispose();
        }

        [Test, TestCaseSource("GetArgs_CancelFinally")]
        public void CallbacksWillBeInvokedOnTheCorrectSynchronizationContext_Cancel_void(
            CompleteType completeType,
            SynchronizationType waitType,
            SynchronizationType reportType,
            bool isAlreadyComplete)
        {
            Thread foregroundThread = Thread.CurrentThread;
            ThreadHelper threadHelper = new ThreadHelper();

            CancelationSource cancelationSource;
            Promise.Deferred deferred;
            Promise promise;

            string rejectReason = "Fail";

            BuildPromise(completeType, isAlreadyComplete, rejectReason, out deferred, out cancelationSource, out promise);

            promise = promise.Preserve();

            int invokeCounter = 0;
            int expectedInvokes = 0;

            foreach (var p in TestHelper.GetTestablePromises(promise))
            {
                ++expectedInvokes;
                p.ConfigureAwait((ConfigureAwaitType) waitType)
                    .CatchCancelation(_ =>
                    {
                        Interlocked.Increment(ref invokeCounter);
                        TestHelper.AssertCallbackContext(waitType, reportType, foregroundThread);
                    })
                    .Forget();
            }
            foreach (var p in TestHelper.GetTestablePromises(promise))
            {
                ++expectedInvokes;
                p.ConfigureAwait((ConfigureAwaitType) waitType)
                    .CatchCancelation(1, (cv, reason) =>
                    {
                        Interlocked.Increment(ref invokeCounter);
                        TestHelper.AssertCallbackContext(waitType, reportType, foregroundThread);
                    })
                    .Forget();
            }

            threadHelper.ExecuteSynchronousOrOnThread(
                () => TestHelper.GetTryCompleterVoid(completeType, rejectReason).Invoke(deferred, cancelationSource),
                reportType == SynchronizationType.Foreground);
            TestHelper.ExecuteForegroundCallbacks();

            if (waitType != (SynchronizationType) SynchronizationOption.Background)
            {
                Assert.AreEqual(expectedInvokes, invokeCounter);
            }
            else
            {
                if (!SpinWait.SpinUntil(() => invokeCounter == expectedInvokes, TimeSpan.FromSeconds(expectedInvokes)))
                {
                    Assert.Fail("Timed out after " + TimeSpan.FromSeconds(expectedInvokes) + ", expectedInvokes: " + expectedInvokes + ", firstInvokeCounter: " + invokeCounter);
                }
            }

            promise.Forget();
            cancelationSource.TryDispose();
        }

        [Test, TestCaseSource("GetArgs_CancelFinally")]
        public void CallbacksWillBeInvokedOnTheCorrectSynchronizationContext_Cancel_T(
            CompleteType completeType,
            SynchronizationType waitType,
            SynchronizationType reportType,
            bool isAlreadyComplete)
        {
            Thread foregroundThread = Thread.CurrentThread;
            ThreadHelper threadHelper = new ThreadHelper();

            CancelationSource cancelationSource;
            Promise<int>.Deferred deferred;
            Promise<int> promise;

            string rejectReason = "Fail";

            BuildPromise(completeType, isAlreadyComplete, 1, rejectReason, out deferred, out cancelationSource, out promise);

            promise = promise.Preserve();

            int invokeCounter = 0;
            int expectedInvokes = 0;

            foreach (var p in TestHelper.GetTestablePromises(promise))
            {
                ++expectedInvokes;
                p.ConfigureAwait((ConfigureAwaitType) waitType)
                    .CatchCancelation(_ =>
                    {
                        Interlocked.Increment(ref invokeCounter);
                        TestHelper.AssertCallbackContext(waitType, reportType, foregroundThread);
                    })
                    .Forget();
            }
            foreach (var p in TestHelper.GetTestablePromises(promise))
            {
                ++expectedInvokes;
                p.ConfigureAwait((ConfigureAwaitType) waitType)
                    .CatchCancelation(1, (cv, reason) =>
                    {
                        Interlocked.Increment(ref invokeCounter);
                        TestHelper.AssertCallbackContext(waitType, reportType, foregroundThread);
                    })
                    .Forget();
            }

            threadHelper.ExecuteSynchronousOrOnThread(
                () => TestHelper.GetTryCompleterT(completeType, 1, rejectReason).Invoke(deferred, cancelationSource),
                reportType == SynchronizationType.Foreground);
            TestHelper.ExecuteForegroundCallbacks();

            if (waitType != (SynchronizationType) SynchronizationOption.Background)
            {
                Assert.AreEqual(expectedInvokes, invokeCounter);
            }
            else
            {
                if (!SpinWait.SpinUntil(() => invokeCounter == expectedInvokes, TimeSpan.FromSeconds(expectedInvokes)))
                {
                    Assert.Fail("Timed out after " + TimeSpan.FromSeconds(expectedInvokes) + ", expectedInvokes: " + expectedInvokes + ", firstInvokeCounter: " + invokeCounter);
                }
            }

            promise.Forget();
            cancelationSource.TryDispose();
        }

        [Test, TestCaseSource("GetArgs_CancelFinally")]
        public void CallbacksWillBeInvokedOnTheCorrectSynchronizationContext_Finally_void(
            CompleteType completeType,
            SynchronizationType waitType,
            SynchronizationType reportType,
            bool isAlreadyComplete)
        {
            Thread foregroundThread = Thread.CurrentThread;
            ThreadHelper threadHelper = new ThreadHelper();

            CancelationSource cancelationSource;
            Promise.Deferred deferred;
            Promise promise;

            string rejectReason = "Fail";

            BuildPromise(completeType, isAlreadyComplete, rejectReason, out deferred, out cancelationSource, out promise);

            promise = promise.Preserve();

            int invokeCounter = 0;
            int expectedInvokes = 0;

            foreach (var p in TestHelper.GetTestablePromises(promise))
            {
                ++expectedInvokes;
                p.ConfigureAwait((ConfigureAwaitType) waitType)
                    .Finally(() =>
                    {
                        Interlocked.Increment(ref invokeCounter);
                        TestHelper.AssertCallbackContext(waitType, reportType, foregroundThread);
                    })
                    .Forget();
            }
            foreach (var p in TestHelper.GetTestablePromises(promise))
            {
                ++expectedInvokes;
                p.ConfigureAwait((ConfigureAwaitType) waitType)
                    .Finally(1, cv =>
                    {
                        Interlocked.Increment(ref invokeCounter);
                        TestHelper.AssertCallbackContext(waitType, reportType, foregroundThread);
                    })
                    .Forget();
            }

            threadHelper.ExecuteSynchronousOrOnThread(
                () => TestHelper.GetTryCompleterVoid(completeType, rejectReason).Invoke(deferred, cancelationSource),
                reportType == SynchronizationType.Foreground);
            TestHelper.ExecuteForegroundCallbacks();

            if (waitType != (SynchronizationType) SynchronizationOption.Background)
            {
                Assert.AreEqual(expectedInvokes, invokeCounter);
            }
            else
            {
                if (!SpinWait.SpinUntil(() => invokeCounter == expectedInvokes, TimeSpan.FromSeconds(expectedInvokes)))
                {
                    Assert.Fail("Timed out after " + TimeSpan.FromSeconds(expectedInvokes) + ", expectedInvokes: " + expectedInvokes + ", firstInvokeCounter: " + invokeCounter);
                }
            }

            promise.Forget();
            cancelationSource.TryDispose();
        }

        [Test, TestCaseSource("GetArgs_CancelFinally")]
        public void CallbacksWillBeInvokedOnTheCorrectSynchronizationContext_Finally_T(
            CompleteType completeType,
            SynchronizationType waitType,
            SynchronizationType reportType,
            bool isAlreadyComplete)
        {
            Thread foregroundThread = Thread.CurrentThread;
            ThreadHelper threadHelper = new ThreadHelper();

            CancelationSource cancelationSource;
            Promise<int>.Deferred deferred;
            Promise<int> promise;

            string rejectReason = "Fail";

            BuildPromise(completeType, isAlreadyComplete, 1, rejectReason, out deferred, out cancelationSource, out promise);

            promise = promise.Preserve();

            int invokeCounter = 0;
            int expectedInvokes = 0;

            foreach (var p in TestHelper.GetTestablePromises(promise))
            {
                ++expectedInvokes;
                p.ConfigureAwait((ConfigureAwaitType) waitType)
                    .Finally(() =>
                    {
                        Interlocked.Increment(ref invokeCounter);
                        TestHelper.AssertCallbackContext(waitType, reportType, foregroundThread);
                    })
                    .Forget();
            }
            foreach (var p in TestHelper.GetTestablePromises(promise))
            {
                ++expectedInvokes;
                p.ConfigureAwait((ConfigureAwaitType) waitType)
                    .Finally(1, cv =>
                    {
                        Interlocked.Increment(ref invokeCounter);
                        TestHelper.AssertCallbackContext(waitType, reportType, foregroundThread);
                    })
                    .Forget();
            }

            threadHelper.ExecuteSynchronousOrOnThread(
                () => TestHelper.GetTryCompleterT(completeType, 1, rejectReason).Invoke(deferred, cancelationSource),
                reportType == SynchronizationType.Foreground);
            TestHelper.ExecuteForegroundCallbacks();

            if (waitType != (SynchronizationType) SynchronizationOption.Background)
            {
                Assert.AreEqual(expectedInvokes, invokeCounter);
            }
            else
            {
                if (!SpinWait.SpinUntil(() => invokeCounter == expectedInvokes, TimeSpan.FromSeconds(expectedInvokes)))
                {
                    Assert.Fail("Timed out after " + TimeSpan.FromSeconds(expectedInvokes) + ", expectedInvokes: " + expectedInvokes + ", firstInvokeCounter: " + invokeCounter);
                }
            }

            promise.Forget();
            cancelationSource.TryDispose();
        }

#if CSHARP_7_3_OR_NEWER
        [Test, TestCaseSource("GetArgs_ContinueWith")]
        public void CallbacksWillBeInvokedOnTheCorrectSynchronizationContext_Await_void(
            CompleteType firstCompleteType,
            CompleteType secondCompleteType,
            SynchronizationType firstWaitType,
            SynchronizationType secondWaitType,
            SynchronizationType firstReportType,
            SynchronizationType secondReportType,
            bool isFirstAlreadyComplete,
            bool isSecondAlreadyComplete)
        {
            Thread foregroundThread = Thread.CurrentThread;
            ThreadHelper threadHelper = new ThreadHelper();

            CancelationSource firstCancelationSource;
            Promise.Deferred firstDeferred;
            Promise firstPromise;
            CancelationSource secondCancelationSource;
            Promise.Deferred secondDeferred;
            Promise secondPromise;

            string rejectReason = "Fail";

            BuildPromise(firstCompleteType, isFirstAlreadyComplete, rejectReason, out firstDeferred, out firstCancelationSource, out firstPromise);
            BuildPromise(secondCompleteType, isSecondAlreadyComplete, rejectReason, out secondDeferred, out secondCancelationSource, out secondPromise);

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
                    await p1.ConfigureAwait((ConfigureAwaitType) firstWaitType);
                }
                catch { }
                finally
                {
                    Interlocked.Increment(ref firstInvokeCounter);
                    TestHelper.AssertCallbackContext(firstWaitType, firstReportType, foregroundThread);
                }

                try
                {
                    await p2.ConfigureAwait((ConfigureAwaitType) secondWaitType);
                }
                catch { }
                finally
                {
                    Interlocked.Increment(ref secondInvokeCounter);
                    // If there's a race condition, p2 could be completed on a separate thread before it's awaited, causing the assert to fail.
                    // This only matters for SynchronizationOption.Synchronous, for which the caller does not care on what context it executes.
                    if (!hasRaceCondition)
                    {
                        TestHelper.AssertCallbackContext(secondWaitType, secondReportType, foregroundThread);
                    }
                }
            }

            threadHelper.ExecuteSynchronousOrOnThread(
                () => TestHelper.GetTryCompleterVoid(firstCompleteType, rejectReason).Invoke(firstDeferred, firstCancelationSource),
                firstReportType == SynchronizationType.Foreground);
            TestHelper.ExecuteForegroundCallbacks();

            if (firstWaitType != (SynchronizationType) SynchronizationOption.Background)
            {
                Assert.AreEqual(expectedInvokes, firstInvokeCounter);
            }
            else
            {
                if (!SpinWait.SpinUntil(() => firstInvokeCounter == expectedInvokes, TimeSpan.FromSeconds(expectedInvokes)))
                {
                    Assert.Fail("Timed out after " + TimeSpan.FromSeconds(expectedInvokes) + ", expectedInvokes: " + expectedInvokes + ", firstInvokeCounter: " + firstInvokeCounter);
                }
            }

            threadHelper.ExecuteSynchronousOrOnThread(
                () => TestHelper.GetTryCompleterVoid(secondCompleteType, rejectReason).Invoke(secondDeferred, secondCancelationSource),
                secondReportType == SynchronizationType.Foreground);
            TestHelper.ExecuteForegroundCallbacks();

            if (!SpinWait.SpinUntil(() =>
                {
                    // We must execute foreground context on every spin to account for the race condition between firstInvokeCounter being incremented and the configured promise being awaited.
                    TestHelper.ExecuteForegroundCallbacks();
                    return secondInvokeCounter == expectedInvokes;
                }, TimeSpan.FromSeconds(expectedInvokes)))
            {
                Assert.Fail("Timed out after " + TimeSpan.FromSeconds(expectedInvokes) + ", expectedInvokes: " + expectedInvokes + ", secondInvokeCounter: " + secondInvokeCounter);
            }

            firstPromise.Forget();
            secondPromise.Forget();
            firstCancelationSource.TryDispose();
            secondCancelationSource.TryDispose();
        }

        [Test, TestCaseSource("GetArgs_ContinueWith")]
        public void CallbacksWillBeInvokedOnTheCorrectSynchronizationContext_Await_T(
            CompleteType firstCompleteType,
            CompleteType secondCompleteType,
            SynchronizationType firstWaitType,
            SynchronizationType secondWaitType,
            SynchronizationType firstReportType,
            SynchronizationType secondReportType,
            bool isFirstAlreadyComplete,
            bool isSecondAlreadyComplete)
        {
            Thread foregroundThread = Thread.CurrentThread;
            ThreadHelper threadHelper = new ThreadHelper();

            CancelationSource firstCancelationSource;
            Promise<int>.Deferred firstDeferred;
            Promise<int> firstPromise;
            CancelationSource secondCancelationSource;
            Promise<int>.Deferred secondDeferred;
            Promise<int> secondPromise;

            string rejectReason = "Fail";

            BuildPromise(firstCompleteType, isFirstAlreadyComplete, 1, rejectReason, out firstDeferred, out firstCancelationSource, out firstPromise);
            BuildPromise(secondCompleteType, isSecondAlreadyComplete, 1, rejectReason, out secondDeferred, out secondCancelationSource, out secondPromise);

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
                    _ = await p1.ConfigureAwait((ConfigureAwaitType) firstWaitType);
                }
                catch { }
                finally
                {
                    Interlocked.Increment(ref firstInvokeCounter);
                    TestHelper.AssertCallbackContext(firstWaitType, firstReportType, foregroundThread);
                }

                try
                {
                    _ = await p2.ConfigureAwait((ConfigureAwaitType) secondWaitType);
                }
                catch { }
                finally
                {
                    Interlocked.Increment(ref secondInvokeCounter);
                    // If there's a race condition, p2 could be completed on a separate thread before it's awaited, causing the assert to fail.
                    // This only matters for SynchronizationOption.Synchronous, for which the caller does not care on what context it executes.
                    if (!hasRaceCondition)
                    {
                        TestHelper.AssertCallbackContext(secondWaitType, secondReportType, foregroundThread);
                    }
                }
            }

            threadHelper.ExecuteSynchronousOrOnThread(
                () => TestHelper.GetTryCompleterT(firstCompleteType, 1, rejectReason).Invoke(firstDeferred, firstCancelationSource),
                firstReportType == SynchronizationType.Foreground);
            TestHelper.ExecuteForegroundCallbacks();

            if (firstWaitType != (SynchronizationType) SynchronizationOption.Background)
            {
                Assert.AreEqual(expectedInvokes, firstInvokeCounter);
            }
            else
            {
                if (!SpinWait.SpinUntil(() => firstInvokeCounter == expectedInvokes, TimeSpan.FromSeconds(expectedInvokes)))
                {
                    Assert.Fail("Timed out after " + TimeSpan.FromSeconds(expectedInvokes) + ", expectedInvokes: " + expectedInvokes + ", firstInvokeCounter: " + firstInvokeCounter);
                }
            }

            threadHelper.ExecuteSynchronousOrOnThread(
                () => TestHelper.GetTryCompleterT(secondCompleteType, 1, rejectReason).Invoke(secondDeferred, secondCancelationSource),
                secondReportType == SynchronizationType.Foreground);
            TestHelper.ExecuteForegroundCallbacks();

            if (!SpinWait.SpinUntil(() =>
                {
                    // We must execute foreground context on every spin to account for the race condition between firstInvokeCounter being incremented and the configured promise being awaited.
                    TestHelper.ExecuteForegroundCallbacks();
                    return secondInvokeCounter == expectedInvokes;
                }, TimeSpan.FromSeconds(expectedInvokes)))
            {
                Assert.Fail("Timed out after " + TimeSpan.FromSeconds(expectedInvokes) + ", expectedInvokes: " + expectedInvokes + ", secondInvokeCounter: " + secondInvokeCounter);
            }

            firstPromise.Forget();
            secondPromise.Forget();
            firstCancelationSource.TryDispose();
            secondCancelationSource.TryDispose();
        }
#endif
    }
}