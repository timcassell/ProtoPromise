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
    public class ConfigureAwaitTests
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

        private static IEnumerable<TestCaseData> GetArgs()
        {
            SynchronizationType[] completeContexts = new[]
            {
                SynchronizationType.Foreground
#if !UNITY_WEBGL
                , SynchronizationType.Background
#endif
            };
            var foregroundOnlyContext = new[] { SynchronizationType.Foreground };

            foreach (CompleteType completeType in Enum.GetValues(typeof(CompleteType)))
            foreach (SynchronizationType continuationContext in Enum.GetValues(typeof(SynchronizationType)))
            foreach (CompletedContinuationBehavior completedBehavior in Enum.GetValues(typeof(CompletedContinuationBehavior)))
            foreach (bool alreadyComplete in new[] { true, false })
            foreach (SynchronizationType completeContext in alreadyComplete ? foregroundOnlyContext : completeContexts)
            foreach (bool asyncFlowExecutionContext in new[] { true, false })
            {
                yield return new TestCaseData(completeType, continuationContext, completedBehavior, alreadyComplete, completeContext, asyncFlowExecutionContext);
            }
        }

        [Test, TestCaseSource(nameof(GetArgs))]
        public void ContinuationsWillBeInvokedOnTheCorrectSynchronizationContext_void(
            CompleteType completeType,
            SynchronizationType continuationContext,
            CompletedContinuationBehavior completedBehavior,
            bool alreadyComplete,
            SynchronizationType completeContext,
            bool asyncFlowExecutionContext)
        {
            // Normally AsyncFlowExecutionContextEnabled cannot be disabled after it is enabled, but we need to test both configurations.
            Promise.Config.s_asyncFlowExecutionContextEnabled = asyncFlowExecutionContext;

            var foregroundThread = Thread.CurrentThread;
            var threadHelper = new ThreadHelper();

            var continuationOptions = TestHelper.GetContinuationOptions(continuationContext, completedBehavior);

            using (var promiseRetainer = TestHelper.BuildPromise(completeType, alreadyComplete, rejectValue, out var tryCompleter).GetRetainer())
            {
                int invokeCounter = 0;
                int expectedInvokes = 0;

                foreach (var promise in TestHelper.GetTestablePromises(promiseRetainer))
                {
                    ++expectedInvokes;
                    RunAsync(promise).Forget();
                }

                async Promise RunAsync(Promise promise)
                {
                    try
                    {
                        await promise.ConfigureAwait(continuationOptions);
                    }
                    catch { }
                    finally
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
                    }
                }

                threadHelper.ExecuteSynchronousOrOnThread(tryCompleter, completeContext == SynchronizationType.Foreground);

                TestHelper.ExecuteForegroundCallbacksAndWaitForThreadsToComplete();
                Assert.AreEqual(expectedInvokes, invokeCounter);
            }
        }

        [Test, TestCaseSource(nameof(GetArgs))]
        public void ContinuationsWillBeInvokedOnTheCorrectSynchronizationContext_T(
            CompleteType completeType,
            SynchronizationType continuationContext,
            CompletedContinuationBehavior completedBehavior,
            bool alreadyComplete,
            SynchronizationType completeContext,
            bool asyncFlowExecutionContext)
        {
            // Normally AsyncFlowExecutionContextEnabled cannot be disabled after it is enabled, but we need to test both configurations.
            Promise.Config.s_asyncFlowExecutionContextEnabled = asyncFlowExecutionContext;

            var foregroundThread = Thread.CurrentThread;
            var threadHelper = new ThreadHelper();

            var continuationOptions = TestHelper.GetContinuationOptions(continuationContext, completedBehavior);

            using (var promiseRetainer = TestHelper.BuildPromise(completeType, alreadyComplete, 1, rejectValue, out var tryCompleter).GetRetainer())
            {
                int invokeCounter = 0;
                int expectedInvokes = 0;

                foreach (var promise in TestHelper.GetTestablePromises(promiseRetainer))
                {
                    ++expectedInvokes;
                    RunAsync(promise).Forget();
                }

                async Promise RunAsync(Promise<int> promise)
                {
                    try
                    {
                        _ = await promise.ConfigureAwait(continuationOptions);
                    }
                    catch { }
                    finally
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
                    }
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
            [Values] bool alreadyComplete,
            [Values] bool asyncFlowExecutionContext)
        {
            // Normally AsyncFlowExecutionContextEnabled cannot be disabled after it is enabled, but we need to test both configurations.
            Promise.Config.s_asyncFlowExecutionContextEnabled = asyncFlowExecutionContext;

            var foregroundThread = Thread.CurrentThread;
            var continuationOptions = TestHelper.GetContinuationOptions(continuationContext, completedBehavior);

            // Lock helps us assert that the callback is not invoked synchronously if completedBehavior is Asynchronous.
            var lockObj = new object();
            bool didInvoke = false;

            Action action = () =>
            {
                lock (lockObj)
                {
                    var promise = TestHelper.BuildPromise(CompleteType.Resolve, alreadyComplete, rejectValue, out var tryCompleter);

                    Await().Forget();

                    async Promise Await()
                    {
                        await promise.ConfigureAwait(continuationOptions);

                        TestHelper.AssertCallbackContext(continuationContext, continuationContext, foregroundThread);
                        lock (lockObj)
                        {
                            didInvoke = true;
                        }
                    }

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
            [Values] bool alreadyComplete,
            [Values] bool asyncFlowExecutionContext)
        {
            // Normally AsyncFlowExecutionContextEnabled cannot be disabled after it is enabled, but we need to test both configurations.
            Promise.Config.s_asyncFlowExecutionContextEnabled = asyncFlowExecutionContext;

            var foregroundThread = Thread.CurrentThread;
            var continuationOptions = TestHelper.GetContinuationOptions(continuationContext, completedBehavior);

            // Lock helps us assert that the callback is not invoked synchronously if completedBehavior is Asynchronous.
            var lockObj = new object();
            bool didInvoke = false;

            Action action = () =>
            {
                lock (lockObj)
                {
                    var promise = TestHelper.BuildPromise(CompleteType.Resolve, alreadyComplete, 1, rejectValue, out var tryCompleter);

                    Await().Forget();

                    async Promise Await()
                    {
                        _ = await promise.ConfigureAwait(continuationOptions);

                        TestHelper.AssertCallbackContext(continuationContext, continuationContext, foregroundThread);
                        lock (lockObj)
                        {
                            didInvoke = true;
                        }
                    }

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