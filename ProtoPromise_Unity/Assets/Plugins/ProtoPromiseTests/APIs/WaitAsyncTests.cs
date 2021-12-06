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

        private static IEnumerable<TestCaseData> GetArgs()
        {
            CompleteType[] completeTypes = (CompleteType[]) Enum.GetValues(typeof(CompleteType));
            SynchronizationType[] noBackgroundSynchronizationTypes = new SynchronizationType[]
            {
                SynchronizationType.Synchronous,
                SynchronizationType.Foreground,
                SynchronizationType.Explicit
            };
            SynchronizationType[] synchronizationTypes =
#if UNITY_WEBGL
                noBackgroundSynchronizationTypes;
#else
                (SynchronizationType[]) Enum.GetValues(typeof(SynchronizationType));
#endif
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
            foreach (CompleteType secondCompleteType in completeTypes)
            {
                bool isFirstCancel = firstCompleteType == CompleteType.Cancel || firstCompleteType == CompleteType.CancelFromToken;
                foreach (bool isFirstComplete in alreadyCompletes)
                // If first promise is canceled and already complete, we cannot guarantee the catch cancelation callback will be executed in a background thread, so ignore it.
                // (The adopting promise may complete on the background thread before the callback is hooked up.)
                foreach (SynchronizationType firstWaitType in isFirstCancel && isFirstComplete ? noBackgroundSynchronizationTypes : synchronizationTypes)
                foreach (SynchronizationType secondWaitType in synchronizationTypes)
                foreach (SynchronizationType firstReportType in isFirstComplete ? foregroundOnlyReportType : reportTypes)
                foreach (SynchronizationType secondReportType in reportTypes)
                {
                    // If second promise is already complete, we cannot guarantee the continuation will be executed in a background thread, so only use pending.
                    // (The adopting promise may complete on the background thread before the continuation is hooked up.)
                    yield return new TestCaseData(firstCompleteType, secondCompleteType, firstWaitType, secondWaitType, firstReportType, secondReportType, isFirstComplete, false);
                }
                // If the first is canceled, we don't need to test all of the second.
                if (isFirstCancel)
                {
                    break;
                }
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

        [Test, TestCaseSource("GetArgs")]
        public void CallbacksWillBeInvokedOnTheCorrectSynchronizationContext_void(
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
                Interlocked.Increment(ref secondInvokeCounter);
                TestHelper.AssertCallbackContext(secondWaitType, secondReportType, foregroundThread);
            };

            if (firstCompleteType == CompleteType.Resolve)
            {
                TestHelper.AddResolveCallbacks<int, string>(firstPromise,
                    onResolve: () => onFirstCallback(),
                    onCancel: _ =>
                    {
                        if (firstCompleteType == CompleteType.Cancel || firstCompleteType == CompleteType.CancelFromToken)
                        {
                            onFirstCallback();
                        }
                    },
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
                        if (firstCompleteType != CompleteType.Cancel && firstCompleteType != CompleteType.CancelFromToken)
                        {
                            ++expectedSecondInvokes;
                            promise = promise.ContinueWith(_ => onSecondCallback.Invoke());
                        }
                    },
                    onAdoptCallbackAddedConvert: (ref Promise<int> promise) =>
                    {
                        if (firstCompleteType != CompleteType.Cancel && firstCompleteType != CompleteType.CancelFromToken)
                        {
                            ++expectedSecondInvokes;
                            promise = promise.ContinueWith(_ => { onSecondCallback.Invoke(); return 2; });
                        }
                    },
                    configureAwaitType: (ConfigureAwaitType) firstWaitType
                );
            }
            TestHelper.AddCallbacks<int, object, string>(firstPromise,
                onResolve: () => onFirstCallback(),
                onReject: r => onFirstCallback(),
                onUnknownRejection: () => onFirstCallback(),
                onCancel: _ =>
                {
                    if (firstCompleteType == CompleteType.Cancel || firstCompleteType == CompleteType.CancelFromToken)
                    {
                        onFirstCallback();
                    }
                },
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
                    if (firstCompleteType != CompleteType.Cancel && firstCompleteType != CompleteType.CancelFromToken
                        && (adoptLocation == AdoptLocation.Both || (CompleteType) adoptLocation == firstCompleteType))
                    {
                        ++expectedSecondInvokes;
                        promise = promise.ContinueWith(_ => { onSecondCallback.Invoke(); return 2; });
                    }
                },
                onAdoptCallbackAddedConvert: (ref Promise<int> promise, AdoptLocation adoptLocation) =>
                {
                    ++expectedFirstInvokes;
                    if (firstCompleteType != CompleteType.Cancel && firstCompleteType != CompleteType.CancelFromToken
                        && (adoptLocation == AdoptLocation.Both || (CompleteType) adoptLocation == firstCompleteType))
                    {
                        ++expectedSecondInvokes;
                        promise = promise.ContinueWith(_ => { onSecondCallback.Invoke(); return 2; });
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
                        ++expectedSecondInvokes;
                        promise = promise.ContinueWith(_ => onSecondCallback.Invoke());
                    }
                },
                configureAwaitType: (ConfigureAwaitType) firstWaitType
            );
            TestHelper.AddContinueCallbacks<int, string>(firstPromise,
                onContinue: _ => onFirstCallback(),
                onCancel: _ =>
                {
                    if (firstCompleteType == CompleteType.Cancel || firstCompleteType == CompleteType.CancelFromToken)
                    {
                        onFirstCallback();
                    }
                },
                promiseToPromise: p => secondPromise.ConfigureAwait((ConfigureAwaitType) secondWaitType),
                promiseToPromiseConvert: p => secondPromise.ConfigureAwait((ConfigureAwaitType) secondWaitType),
                onCallbackAdded: (ref Promise promise) =>
                {
                    ++expectedFirstInvokes;
                    promise.Catch(() => { }).Forget();
                },
                onCallbackAddedConvert: (ref Promise<int> promise) =>
                {
                    ++expectedFirstInvokes;
                    promise.Catch(() => { }).Forget();
                },
                onAdoptCallbackAdded: (ref Promise promise) =>
                {
                    if (firstCompleteType != CompleteType.Cancel && firstCompleteType != CompleteType.CancelFromToken)
                    {
                        ++expectedSecondInvokes;
                        promise = promise.ContinueWith(_ => onSecondCallback.Invoke());
                    }
                },
                onAdoptCallbackAddedConvert: (ref Promise<int> promise) =>
                {
                    if (firstCompleteType != CompleteType.Cancel && firstCompleteType != CompleteType.CancelFromToken)
                    {
                        ++expectedSecondInvokes;
                        promise = promise.ContinueWith(_ => { onSecondCallback.Invoke(); return 2; });
                    }
                },
                configureAwaitType: (ConfigureAwaitType) firstWaitType
            );

            threadHelper.ExecuteSynchronousOrOnThread(
                () => TestHelper.GetTryCompleterVoid(firstCompleteType, rejectReason).Invoke(firstDeferred, firstCancelationSource),
                firstReportType == SynchronizationType.Foreground);
            TestHelper.ExecuteForegroundCallbacks();

            if (firstWaitType != SynchronizationType.Background)
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

            if (secondWaitType != SynchronizationType.Background)
            {
                Assert.AreEqual(expectedSecondInvokes, secondInvokeCounter);
            }
            else
            {
                if (!SpinWait.SpinUntil(() => secondInvokeCounter == expectedSecondInvokes, TimeSpan.FromSeconds(expectedSecondInvokes)))
                {
                    Assert.Fail("Timed out after " + TimeSpan.FromSeconds(expectedSecondInvokes) + ", expectedSecondInvokes: " + expectedSecondInvokes + ", secondInvokeCounter: " + secondInvokeCounter);
                }
            }

            firstPromise.Forget();
            secondPromise.Forget();
            firstCancelationSource.TryDispose();
            secondCancelationSource.TryDispose();
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

        [Test, TestCaseSource("GetArgs")]
        public void CallbacksWillBeInvokedOnTheCorrectSynchronizationContext_T(
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
                Interlocked.Increment(ref secondInvokeCounter);
                TestHelper.AssertCallbackContext(secondWaitType, secondReportType, foregroundThread);
            };

            if (firstCompleteType == CompleteType.Resolve)
            {
                TestHelper.AddResolveCallbacks<int, int, string>(firstPromise,
                    onResolve: v => onFirstCallback(),
                    onCancel: _ =>
                    {
                        if (firstCompleteType == CompleteType.Cancel || firstCompleteType == CompleteType.CancelFromToken)
                        {
                            onFirstCallback();
                        }
                    },
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
                        if (firstCompleteType != CompleteType.Cancel && firstCompleteType != CompleteType.CancelFromToken)
                        {
                            ++expectedSecondInvokes;
                            promise = promise.ContinueWith(_ => onSecondCallback.Invoke());
                        }
                    },
                    onAdoptCallbackAddedConvert: (ref Promise<int> promise) =>
                    {
                        if (firstCompleteType != CompleteType.Cancel && firstCompleteType != CompleteType.CancelFromToken)
                        {
                            ++expectedSecondInvokes;
                            promise = promise.ContinueWith(_ => { onSecondCallback.Invoke(); return 2; });
                        }
                    },
                    configureAwaitType: (ConfigureAwaitType) firstWaitType
                );
            }
            TestHelper.AddCallbacks<int, int, object, string>(firstPromise,
                onResolve: v => onFirstCallback(),
                onReject: r => onFirstCallback(),
                onUnknownRejection: () => onFirstCallback(),
                onCancel: _ =>
                {
                    if (firstCompleteType == CompleteType.Cancel || firstCompleteType == CompleteType.CancelFromToken)
                    {
                        onFirstCallback();
                    }
                },
                promiseToPromise: p => secondPromise.ConfigureAwait((ConfigureAwaitType) secondWaitType),
                promiseToPromiseConvert: p => secondPromise.ConfigureAwait((ConfigureAwaitType) secondWaitType),
                promiseToPromiseT: p => secondPromise.ConfigureAwait((ConfigureAwaitType) secondWaitType),
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
                    if (firstCompleteType != CompleteType.Cancel && firstCompleteType != CompleteType.CancelFromToken
                        && (adoptLocation == AdoptLocation.Both || (CompleteType) adoptLocation == firstCompleteType))
                    {
                        ++expectedSecondInvokes;
                        promise = promise.ContinueWith(_ => { onSecondCallback.Invoke(); return 2; });
                    }
                },
                onAdoptCallbackAddedConvert: (ref Promise<int> promise, AdoptLocation adoptLocation) =>
                {
                    ++expectedFirstInvokes;
                    if (firstCompleteType != CompleteType.Cancel && firstCompleteType != CompleteType.CancelFromToken
                        && (adoptLocation == AdoptLocation.Both || (CompleteType) adoptLocation == firstCompleteType))
                    {
                        ++expectedSecondInvokes;
                        promise = promise.ContinueWith(_ => { onSecondCallback.Invoke(); return 2; });
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
                        ++expectedSecondInvokes;
                        promise = promise.ContinueWith(_ => { onSecondCallback.Invoke(); return 2; });
                    }
                },
                configureAwaitType: (ConfigureAwaitType) firstWaitType
            );
            TestHelper.AddContinueCallbacks<int, int, string>(firstPromise,
                onContinue: _ => onFirstCallback(),
                onCancel: _ =>
                {
                    if (firstCompleteType == CompleteType.Cancel || firstCompleteType == CompleteType.CancelFromToken)
                    {
                        onFirstCallback();
                    }
                },
                promiseToPromise: p => secondPromise.ConfigureAwait((ConfigureAwaitType) secondWaitType),
                promiseToPromiseConvert: p => secondPromise.ConfigureAwait((ConfigureAwaitType) secondWaitType),
                onCallbackAdded: (ref Promise promise) =>
                {
                    ++expectedFirstInvokes;
                    promise.Catch(() => { }).Forget();
                },
                onCallbackAddedConvert: (ref Promise<int> promise) =>
                {
                    ++expectedFirstInvokes;
                    promise.Catch(() => { }).Forget();
                },
                onAdoptCallbackAdded: (ref Promise promise) =>
                {
                    if (firstCompleteType != CompleteType.Cancel && firstCompleteType != CompleteType.CancelFromToken)
                    {
                        ++expectedSecondInvokes;
                        promise = promise.ContinueWith(_ => onSecondCallback.Invoke());
                    }
                },
                onAdoptCallbackAddedConvert: (ref Promise<int> promise) =>
                {
                    if (firstCompleteType != CompleteType.Cancel && firstCompleteType != CompleteType.CancelFromToken)
                    {
                        ++expectedSecondInvokes;
                        promise = promise.ContinueWith(_ => { onSecondCallback.Invoke(); return 2; });
                    }
                },
                configureAwaitType: (ConfigureAwaitType) firstWaitType
            );

            threadHelper.ExecuteSynchronousOrOnThread(
                () => TestHelper.GetTryCompleterT(firstCompleteType, 1, rejectReason).Invoke(firstDeferred, firstCancelationSource),
                firstReportType == SynchronizationType.Foreground);
            TestHelper.ExecuteForegroundCallbacks();

            if (firstWaitType != SynchronizationType.Background)
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

            if (secondWaitType != SynchronizationType.Background)
            {
                Assert.AreEqual(expectedSecondInvokes, secondInvokeCounter);
            }
            else
            {
                if (!SpinWait.SpinUntil(() => secondInvokeCounter == expectedSecondInvokes, TimeSpan.FromSeconds(expectedSecondInvokes)))
                {
                    Assert.Fail("Timed out after " + TimeSpan.FromSeconds(expectedSecondInvokes) + ", expectedSecondInvokes: " + expectedSecondInvokes + ", secondInvokeCounter: " + secondInvokeCounter);
                }
            }

            firstPromise.Forget();
            secondPromise.Forget();
            firstCancelationSource.TryDispose();
            secondCancelationSource.TryDispose();
        }
    }
}