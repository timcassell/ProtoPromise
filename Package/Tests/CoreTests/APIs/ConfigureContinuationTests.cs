#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using NUnit.Framework;
using Proto.Promises;
using ProtoPromise.Tests.Concurrency;
using System;
using System.Collections.Generic;
using System.Threading;

namespace ProtoPromise.Tests.APIs
{
    public class ConfigureContinuationTests
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

        private static IEnumerable<TestCaseData> GetArgs(params CompleteType[] completeTypes)
        {
            SynchronizationType[] completeContexts = new[]
            {
                SynchronizationType.Foreground
#if !UNITY_WEBGL
                , SynchronizationType.Background
#endif
            };
            var foregroundOnlyContext = new[] { SynchronizationType.Foreground };

            foreach (CompleteType completeType in completeTypes)
            foreach (SynchronizationType continuationContext in Enum.GetValues(typeof(SynchronizationType)))
            foreach (CompletedContinuationBehavior completedBehavior in Enum.GetValues(typeof(CompletedContinuationBehavior)))
            foreach (bool alreadyComplete in new[] { true, false })
            foreach (SynchronizationType completeContext in alreadyComplete ? foregroundOnlyContext : completeContexts)
            {
                yield return new TestCaseData(completeType, continuationContext, completedBehavior, alreadyComplete, completeContext);
            }
        }

        private static IEnumerable<TestCaseData> GetArgs_ResolveReject()
            => GetArgs(CompleteType.Resolve, CompleteType.Reject);

        private static IEnumerable<TestCaseData> GetArgs_Continue()
            => GetArgs(CompleteType.Resolve, CompleteType.Reject, CompleteType.Cancel);

        private static IEnumerable<TestCaseData> GetArgs_Cancel()
            => GetArgs(CompleteType.Cancel);

        [Test, TestCaseSource(nameof(GetArgs_ResolveReject))]
        public void CallbacksWillBeInvokedOnTheCorrectSynchronizationContext_ResolveReject_void(
            CompleteType completeType,
            SynchronizationType continuationContext,
            CompletedContinuationBehavior completedBehavior,
            bool alreadyComplete,
            SynchronizationType completeContext)
        {
            var foregroundThread = Thread.CurrentThread;
            var threadHelper = new ThreadHelper();

            using (var promiseRetainer = TestHelper.BuildPromise(completeType, alreadyComplete, rejectValue, out var tryCompleter).GetRetainer())
            {
                int invokeCounter = 0;
                int expectedInvokes = 0;

                Action onCallback = () =>
                {
                    if (completedBehavior == CompletedContinuationBehavior.Synchronous && alreadyComplete)
                    {
                        TestHelper.AssertCallbackContext(completeContext, completeContext, foregroundThread);
                    }
                    else
                    {
                        TestHelper.AssertCallbackContext(continuationContext, completeContext, foregroundThread);
                    }
                    Interlocked.Increment(ref invokeCounter);
                };

                if (completeType == CompleteType.Resolve)
                {
                    TestHelper.AddResolveCallbacks<int, string>(promiseRetainer.WaitAsync(),
                        onResolve: () => onCallback(),
                        onCallbackAdded: (ref Promise promise) =>
                        {
                            ++expectedInvokes;
                            promise.Forget();
                        },
                        onCallbackAddedConvert: (ref Promise<int> promise) =>
                        {
                            ++expectedInvokes;
                            promise.Forget();
                        },
                        continuationOptions: TestHelper.GetContinuationOptions(continuationContext, completedBehavior)
                    );
                }
                TestHelper.AddCallbacks<int, object, string>(promiseRetainer.WaitAsync(),
                    onResolve: () => onCallback(),
                    onReject: r => onCallback(),
                    onUnknownRejection: () => onCallback(),
                    onDirectCallbackAdded: (ref Promise promise) =>
                    {
                        ++expectedInvokes;
                        promise = promise.Catch(() => { });
                    },
                    onDirectCallbackAddedConvert: (ref Promise<int> promise) =>
                    {
                        ++expectedInvokes;
                        promise = promise.Catch(() => 2);
                    },
                    onDirectCallbackAddedCatch: (ref Promise promise) =>
                    {
                        if (completeType == CompleteType.Reject)
                        {
                            ++expectedInvokes;
                        }
                    },
                    onAdoptCallbackAdded: (ref Promise promise, AdoptLocation adoptLocation) =>
                    {
                        ++expectedInvokes;
                        promise = promise.Catch(() => { });
                    },
                    onAdoptCallbackAddedConvert: (ref Promise<int> promise, AdoptLocation adoptLocation) =>
                    {
                        promise = promise.Catch(() => 2);
                        ++expectedInvokes;
                    },
                    onAdoptCallbackAddedCatch: (ref Promise promise) =>
                    {
                        if (completeType == CompleteType.Reject)
                        {
                            ++expectedInvokes;
                        }
                    },
                    continuationOptions: TestHelper.GetContinuationOptions(continuationContext, completedBehavior)
                );

                threadHelper.ExecuteSynchronousOrOnThread(tryCompleter, completeContext == SynchronizationType.Foreground);

                TestHelper.ExecuteForegroundCallbacksAndWaitForThreadsToComplete();
                Assert.AreEqual(expectedInvokes, invokeCounter);
            }
        }

        [Test, TestCaseSource(nameof(GetArgs_ResolveReject))]
        public void CallbacksWillBeInvokedOnTheCorrectSynchronizationContext_ResolveReject_T(
            CompleteType completeType,
            SynchronizationType continuationContext,
            CompletedContinuationBehavior completedBehavior,
            bool alreadyComplete,
            SynchronizationType completeContext)
        {
            var foregroundThread = Thread.CurrentThread;
            var threadHelper = new ThreadHelper();

            using (var promiseRetainer = TestHelper.BuildPromise(completeType, alreadyComplete, 1, rejectValue, out var tryCompleter).GetRetainer())
            {
                int invokeCounter = 0;
                int expectedInvokes = 0;

                Action onCallback = () =>
                {
                    if (completedBehavior == CompletedContinuationBehavior.Synchronous && alreadyComplete)
                    {
                        TestHelper.AssertCallbackContext(completeContext, completeContext, foregroundThread);
                    }
                    else
                    {
                        TestHelper.AssertCallbackContext(continuationContext, completeContext, foregroundThread);
                    }
                    Interlocked.Increment(ref invokeCounter);
                };

                if (completeType == CompleteType.Resolve)
                {
                    TestHelper.AddResolveCallbacks<int, int, string>(promiseRetainer.WaitAsync(),
                        onResolve: v => onCallback(),
                        onCallbackAdded: (ref Promise promise) =>
                        {
                            ++expectedInvokes;
                            promise.Forget();
                        },
                        onCallbackAddedConvert: (ref Promise<int> promise) =>
                        {
                            ++expectedInvokes;
                            promise.Forget();
                        },
                        continuationOptions: TestHelper.GetContinuationOptions(continuationContext, completedBehavior)
                    );
                }
                TestHelper.AddCallbacks<int, int, object, string>(promiseRetainer.WaitAsync(),
                    onResolve: v => onCallback(),
                    onReject: r => onCallback(),
                    onUnknownRejection: () => onCallback(),
                    onDirectCallbackAdded: (ref Promise promise) =>
                    {
                        ++expectedInvokes;
                        promise = promise.Catch(() => { });
                    },
                    onDirectCallbackAddedConvert: (ref Promise<int> promise) =>
                    {
                        ++expectedInvokes;
                        promise = promise.Catch(() => 2);
                    },
                    onDirectCallbackAddedT: (ref Promise<int> promise) =>
                    {
                        if (completeType == CompleteType.Reject)
                        {
                            ++expectedInvokes;
                        }
                    },
                    onAdoptCallbackAdded: (ref Promise promise, AdoptLocation adoptLocation) =>
                    {
                        ++expectedInvokes;
                        promise = promise.Catch(() => { });
                    },
                    onAdoptCallbackAddedConvert: (ref Promise<int> promise, AdoptLocation adoptLocation) =>
                    {
                        ++expectedInvokes;
                        promise = promise.Catch(() => 2);
                    },
                    onAdoptCallbackAddedCatch: (ref Promise<int> promise) =>
                    {
                        if (completeType == CompleteType.Reject)
                        {
                            ++expectedInvokes;
                        }
                    },
                    continuationOptions: TestHelper.GetContinuationOptions(continuationContext, completedBehavior)
                );

                threadHelper.ExecuteSynchronousOrOnThread(tryCompleter, completeContext == SynchronizationType.Foreground);

                TestHelper.ExecuteForegroundCallbacksAndWaitForThreadsToComplete();
                Assert.AreEqual(expectedInvokes, invokeCounter);
            }
        }

        [Test, TestCaseSource(nameof(GetArgs_Continue))]
        public void CallbacksWillBeInvokedOnTheCorrectSynchronizationContext_ContinueWith_void(
            CompleteType completeType,
            SynchronizationType continuationContext,
            CompletedContinuationBehavior completedBehavior,
            bool alreadyComplete,
            SynchronizationType completeContext)
        {
            var foregroundThread = Thread.CurrentThread;
            var threadHelper = new ThreadHelper();

            using (var promiseRetainer = TestHelper.BuildPromise(completeType, alreadyComplete, rejectValue, out var tryCompleter)
                .GetRetainer())
            {
                int invokeCounter = 0;
                int expectedInvokes = 0;

                Action onCallback = () =>
                {
                    if (completedBehavior == CompletedContinuationBehavior.Synchronous && alreadyComplete)
                    {
                        TestHelper.AssertCallbackContext(completeContext, completeContext, foregroundThread);
                    }
                    else
                    {
                        TestHelper.AssertCallbackContext(continuationContext, completeContext, foregroundThread);
                    }
                    Interlocked.Increment(ref invokeCounter);
                };

                TestHelper.AddContinueCallbacks<int, string>(promiseRetainer.WaitAsync(),
                    onContinue: _ => onCallback(),
                    onCallbackAdded: (ref Promise promise) =>
                    {
                        ++expectedInvokes;
                        promise.Forget();
                    },
                    onCallbackAddedConvert: (ref Promise<int> promise) =>
                    {
                        ++expectedInvokes;
                        promise.Forget();
                    },
                    continuationOptions: TestHelper.GetContinuationOptions(continuationContext, completedBehavior)
                );

                threadHelper.ExecuteSynchronousOrOnThread(tryCompleter, completeContext == SynchronizationType.Foreground);

                TestHelper.ExecuteForegroundCallbacksAndWaitForThreadsToComplete();
                Assert.AreEqual(expectedInvokes, invokeCounter);
            }
        }

        [Test, TestCaseSource(nameof(GetArgs_Continue))]
        public void CallbacksWillBeInvokedOnTheCorrectSynchronizationContext_ContinueWith_T(
            CompleteType completeType,
            SynchronizationType continuationContext,
            CompletedContinuationBehavior completedBehavior,
            bool alreadyComplete,
            SynchronizationType completeContext)
        {
            var foregroundThread = Thread.CurrentThread;
            var threadHelper = new ThreadHelper();

            using (var promiseRetainer = TestHelper.BuildPromise(completeType, alreadyComplete, 1, rejectValue, out var tryCompleter)
                .GetRetainer())
            {
                int invokeCounter = 0;
                int expectedInvokes = 0;

                Action onCallback = () =>
                {
                    if (completedBehavior == CompletedContinuationBehavior.Synchronous && alreadyComplete)
                    {
                        TestHelper.AssertCallbackContext(completeContext, completeContext, foregroundThread);
                    }
                    else
                    {
                        TestHelper.AssertCallbackContext(continuationContext, completeContext, foregroundThread);
                    }
                    Interlocked.Increment(ref invokeCounter);
                };

                TestHelper.AddContinueCallbacks<int, int, string>(promiseRetainer.WaitAsync(),
                    onContinue: _ => onCallback(),
                    onCallbackAdded: (ref Promise promise) =>
                    {
                        ++expectedInvokes;
                        promise.Forget();
                    },
                    onCallbackAddedConvert: (ref Promise<int> promise) =>
                    {
                        ++expectedInvokes;
                        promise.Forget();
                    },
                    continuationOptions: TestHelper.GetContinuationOptions(continuationContext, completedBehavior)
                );

                threadHelper.ExecuteSynchronousOrOnThread(tryCompleter, completeContext == SynchronizationType.Foreground);

                TestHelper.ExecuteForegroundCallbacksAndWaitForThreadsToComplete();
                Assert.AreEqual(expectedInvokes, invokeCounter);
            }
        }

        [Test, TestCaseSource(nameof(GetArgs_Cancel))]
        public void CallbacksWillBeInvokedOnTheCorrectSynchronizationContext_CatchCancelation_void(
            CompleteType completeType,
            SynchronizationType continuationContext,
            CompletedContinuationBehavior completedBehavior,
            bool alreadyComplete,
            SynchronizationType completeContext)
        {
            var foregroundThread = Thread.CurrentThread;
            var threadHelper = new ThreadHelper();

            var continuationOptions = TestHelper.GetContinuationOptions(continuationContext, completedBehavior);

            using (var promiseRetainer = TestHelper.BuildPromise(completeType, alreadyComplete, rejectValue, out var tryCompleter)
                .GetRetainer())
            {
                int invokeCounter = 0;
                int expectedInvokes = 0;

                foreach (var p in TestHelper.GetTestablePromises(promiseRetainer))
                {
                    ++expectedInvokes;
                    p.ConfigureContinuation(continuationOptions)
                        .CatchCancelation(() =>
                        {
                            if (completedBehavior == CompletedContinuationBehavior.Synchronous && alreadyComplete)
                            {
                                TestHelper.AssertCallbackContext(completeContext, completeContext, foregroundThread);
                            }
                            else
                            {
                                TestHelper.AssertCallbackContext(continuationContext, completeContext, foregroundThread);
                            }
                            Interlocked.Increment(ref invokeCounter);
                        })
                        .Forget();
                }
                foreach (var p in TestHelper.GetTestablePromises(promiseRetainer))
                {
                    ++expectedInvokes;
                    p.ConfigureContinuation(continuationOptions)
                        .CatchCancelation(1, cv =>
                        {
                            if (completedBehavior == CompletedContinuationBehavior.Synchronous && alreadyComplete)
                            {
                                TestHelper.AssertCallbackContext(completeContext, completeContext, foregroundThread);
                            }
                            else
                            {
                                TestHelper.AssertCallbackContext(continuationContext, completeContext, foregroundThread);
                            }
                            Interlocked.Increment(ref invokeCounter);
                        })
                        .Forget();
                }

                threadHelper.ExecuteSynchronousOrOnThread(tryCompleter, completeContext == SynchronizationType.Foreground);

                TestHelper.ExecuteForegroundCallbacksAndWaitForThreadsToComplete();
                Assert.AreEqual(expectedInvokes, invokeCounter);
            }
        }

        [Test, TestCaseSource(nameof(GetArgs_Cancel))]
        public void CallbacksWillBeInvokedOnTheCorrectSynchronizationContext_CatchCancelation_T(
            CompleteType completeType,
            SynchronizationType continuationContext,
            CompletedContinuationBehavior completedBehavior,
            bool alreadyComplete,
            SynchronizationType completeContext)
        {
            var foregroundThread = Thread.CurrentThread;
            var threadHelper = new ThreadHelper();

            var continuationOptions = TestHelper.GetContinuationOptions(continuationContext, completedBehavior);

            using (var promiseRetainer = TestHelper.BuildPromise(completeType, alreadyComplete, 1, rejectValue, out var tryCompleter)
                .GetRetainer())
            {
                int invokeCounter = 0;
                int expectedInvokes = 0;

                foreach (var p in TestHelper.GetTestablePromises(promiseRetainer))
                {
                    ++expectedInvokes;
                    p.ConfigureContinuation(continuationOptions)
                        .CatchCancelation(() =>
                        {
                            if (completedBehavior == CompletedContinuationBehavior.Synchronous && alreadyComplete)
                            {
                                TestHelper.AssertCallbackContext(completeContext, completeContext, foregroundThread);
                            }
                            else
                            {
                                TestHelper.AssertCallbackContext(continuationContext, completeContext, foregroundThread);
                            }
                            Interlocked.Increment(ref invokeCounter);
                        })
                        .Forget();
                }

                threadHelper.ExecuteSynchronousOrOnThread(tryCompleter, completeContext == SynchronizationType.Foreground);

                TestHelper.ExecuteForegroundCallbacksAndWaitForThreadsToComplete();
                Assert.AreEqual(expectedInvokes, invokeCounter);
            }
        }

        [Test, TestCaseSource(nameof(GetArgs_Continue))]
        public void CallbacksWillBeInvokedOnTheCorrectSynchronizationContext_Finally_void(
            CompleteType completeType,
            SynchronizationType continuationContext,
            CompletedContinuationBehavior completedBehavior,
            bool alreadyComplete,
            SynchronizationType completeContext)
        {
            var foregroundThread = Thread.CurrentThread;
            var threadHelper = new ThreadHelper();

            var continuationOptions = TestHelper.GetContinuationOptions(continuationContext, completedBehavior);

            using (var promiseRetainer = TestHelper.BuildPromise(completeType, alreadyComplete, rejectValue, out var tryCompleter)
                .GetRetainer())
            {
                int invokeCounter = 0;
                int expectedInvokes = 0;

                foreach (var p in TestHelper.GetTestablePromises(promiseRetainer))
                {
                    ++expectedInvokes;
                    p.ConfigureContinuation(continuationOptions)
                        .Finally(() =>
                        {
                            if (completedBehavior == CompletedContinuationBehavior.Synchronous && alreadyComplete)
                            {
                                TestHelper.AssertCallbackContext(completeContext, completeContext, foregroundThread);
                            }
                            else
                            {
                                TestHelper.AssertCallbackContext(continuationContext, completeContext, foregroundThread);
                            }
                            Interlocked.Increment(ref invokeCounter);
                        })
                        .Catch(() => { })
                        .Forget();
                }

                threadHelper.ExecuteSynchronousOrOnThread(tryCompleter, completeContext == SynchronizationType.Foreground);

                TestHelper.ExecuteForegroundCallbacksAndWaitForThreadsToComplete();
                Assert.AreEqual(expectedInvokes, invokeCounter);
            }
        }

        [Test, TestCaseSource(nameof(GetArgs_Continue))]
        public void CallbacksWillBeInvokedOnTheCorrectSynchronizationContext_Finally_T(
            CompleteType completeType,
            SynchronizationType continuationContext,
            CompletedContinuationBehavior completedBehavior,
            bool alreadyComplete,
            SynchronizationType completeContext)
        {
            var foregroundThread = Thread.CurrentThread;
            var threadHelper = new ThreadHelper();

            var continuationOptions = TestHelper.GetContinuationOptions(continuationContext, completedBehavior);

            using (var promiseRetainer = TestHelper.BuildPromise(completeType, alreadyComplete, 1, rejectValue, out var tryCompleter)
                .GetRetainer())
            {
                int invokeCounter = 0;
                int expectedInvokes = 0;

                foreach (var p in TestHelper.GetTestablePromises(promiseRetainer))
                {
                    ++expectedInvokes;
                    p.ConfigureContinuation(continuationOptions)
                        .Finally(() =>
                        {
                            if (completedBehavior == CompletedContinuationBehavior.Synchronous && alreadyComplete)
                            {
                                TestHelper.AssertCallbackContext(completeContext, completeContext, foregroundThread);
                            }
                            else
                            {
                                TestHelper.AssertCallbackContext(continuationContext, completeContext, foregroundThread);
                            }
                            Interlocked.Increment(ref invokeCounter);
                        })
                        .Catch(() => { })
                        .Forget();
                }

                threadHelper.ExecuteSynchronousOrOnThread(tryCompleter, completeContext == SynchronizationType.Foreground);

                TestHelper.ExecuteForegroundCallbacksAndWaitForThreadsToComplete();
                Assert.AreEqual(expectedInvokes, invokeCounter);
            }
        }

        [Test]
        public void ForceAsync_ContinuationWillBeInvokedProperly_void(
            [Values(SynchronizationType.Foreground, SynchronizationType.Explicit
#if !UNITY_WEBGL
                , SynchronizationType.Background
#endif
            )] SynchronizationType continuationContext,
            [Values] CompletedContinuationBehavior completedBehavior,
            [Values] bool alreadyComplete)
        {
            var foregroundThread = Thread.CurrentThread;
            var continuationOptions = TestHelper.GetContinuationOptions(continuationContext, completedBehavior);

            // Lock helps us assert that the callback is not invoked synchronously if completedBehavior is Asynchronous.
            var lockObj = new object();
            bool didInvoke = false;

            Action action = () =>
            {
                lock (lockObj)
                {
                    TestHelper.BuildPromise(CompleteType.Resolve, alreadyComplete, rejectValue, out var tryCompleter)
                        .ConfigureContinuation(continuationOptions)
                        .Then(() =>
                        {
                            TestHelper.AssertCallbackContext(continuationContext, continuationContext, foregroundThread);
                            lock (lockObj)
                            {
                                didInvoke = true;
                            }
                        })
                        .Forget();
                    tryCompleter();
                    // Implementation detail - if the promise is not already complete, the continuation will always be invoked asynchronously.
                    // This is a change from previous behavior when ContinuationOptions were added, where it used to compare the provided context to the current context before invoking.
                    Assert.AreNotEqual(completedBehavior == CompletedContinuationBehavior.Asynchronous || !alreadyComplete, didInvoke);
                }
            };

            if (continuationContext == SynchronizationType.Explicit)
            {
                Promise.Run(action, TestHelper._foregroundContext).Forget();
            }
            else
            {
                Promise.Run(action, (SynchronizationOption) continuationContext).Forget();
            }

            TestHelper.ExecuteForegroundCallbacksAndWaitForThreadsToComplete();

            TestHelper.SpinUntil(() => didInvoke, TimeSpan.FromSeconds(1), $"didInvoke: {didInvoke}");
        }

        [Test]
        public void ForceAsync_ContinuationWillBeInvokedProperly_T(
            [Values(SynchronizationType.Foreground, SynchronizationType.Explicit
#if !UNITY_WEBGL
                , SynchronizationType.Background
#endif
            )] SynchronizationType continuationContext,
            [Values] CompletedContinuationBehavior completedBehavior,
            [Values] bool alreadyComplete)
        {
            var foregroundThread = Thread.CurrentThread;
            var continuationOptions = TestHelper.GetContinuationOptions(continuationContext, completedBehavior);

            // Lock helps us assert that the callback is not invoked synchronously if completedBehavior is Asynchronous.
            var lockObj = new object();
            bool didInvoke = false;

            Action action = () =>
            {
                lock (lockObj)
                {
                    TestHelper.BuildPromise(CompleteType.Resolve, alreadyComplete, 1, rejectValue, out var tryCompleter)
                        .ConfigureContinuation(continuationOptions)
                        .ContinueWith(_ =>
                        {
                            TestHelper.AssertCallbackContext(continuationContext, continuationContext, foregroundThread);
                            lock (lockObj)
                            {
                                didInvoke = true;
                            }
                        })
                        .Forget();
                    tryCompleter();
                    // Implementation detail - if the promise is not already complete, the continuation will always be invoked asynchronously.
                    // This is a change from previous behavior when ContinuationOptions were added, where it used to compare the provided context to the current context before invoking.
                    Assert.AreNotEqual(completedBehavior == CompletedContinuationBehavior.Asynchronous || !alreadyComplete, didInvoke);
                }
            };

            if (continuationContext == SynchronizationType.Explicit)
            {
                Promise.Run(action, TestHelper._foregroundContext).Forget();
            }
            else
            {
                Promise.Run(action, (SynchronizationOption) continuationContext).Forget();
            }

            TestHelper.ExecuteForegroundCallbacksAndWaitForThreadsToComplete();

            TestHelper.SpinUntil(() => didInvoke, TimeSpan.FromSeconds(1), $"didInvoke: {didInvoke}");
        }
    }
}