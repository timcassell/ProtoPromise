#if UNITY_5_5 || NET_2_0 || NET_2_0_SUBSET
#define NET_LEGACY
#endif

#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif
#if !PROTO_PROMISE_PROGRESS_DISABLE
#define PROMISE_PROGRESS
#else
#undef PROMISE_PROGRESS
#endif

#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable CS0420 // A reference to a volatile field will not be treated as volatile
#pragma warning disable CS0618 // Type or member is obsolete

using NUnit.Framework;
using Proto.Promises;
using Proto.Promises.Threading;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace ProtoPromiseTests
{
    public enum CompleteType : byte
    {
        // Explicit numbers for easy comparison with Promise.State
        Resolve = 1,
        Reject = 2,
        Cancel = 3,
        CancelFromToken,
    }

    public enum SynchronizationType
    {
        Synchronous = 0,
        Foreground = 1,
#if !UNITY_WEBGL // WebGL doesn't support threads.
        Background = 2,
#endif
        Explicit = 3
    }

    public enum ConfigureAwaitType
    {
        None = -1,
        Synchronous = 0,
        Foreground = 1,
#if !UNITY_WEBGL // WebGL doesn't support threads.
        Background = 2,
#endif
        Explicit = 3
    }

    public enum AdoptLocation
    {
        Resolve,
        Reject,
        Both
    }

    public delegate void TestAction<T>(ref T value);
    public delegate void TestAction<T1, T2>(ref T1 value1, T2 value2);

    // These help test all Then/Catch/ContinueWith methods at once.
    public static class TestHelper
    {
        public const SynchronizationType backgroundType = (SynchronizationType) 2;

        public static readonly PromiseSynchronizationContext _foregroundContext = new PromiseSynchronizationContext();
        public static readonly BackgroundSynchronizationContext _backgroundContext = new BackgroundSynchronizationContext();
        private static readonly List<Exception> _uncaughtExceptions = new List<Exception>();
        public static object s_expectedUncaughtRejectValue;

        private static Stopwatch _stopwatch;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Setup()
        {
            if (Promise.Config.ForegroundContext != _foregroundContext)
            {
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                Internal.TrackObjectsForRelease();
#endif

                // Set the foreground context to execute foreground promise callbacks.
                Promise.Config.ForegroundContext = _foregroundContext;
                // Used instead of ThreadPool, because ThreadPool has issues in old runtime, causing tests to fail.
                // This also allows us to wait for all background threads to complete for validation purposes.
                Promise.Config.BackgroundContext = _backgroundContext;
                // Set uncaught rejection handler.
                Promise.Config.UncaughtRejectionHandler = e =>
                {
                    lock (_uncaughtExceptions)
                    {
                        if (s_expectedUncaughtRejectValue != null)
                        {
                            try
                            {
                                AssertRejection(s_expectedUncaughtRejectValue, e.Value);
                            }
                            catch (Exception ex)
                            {
                                _uncaughtExceptions.Add(ex);
                            }
                            return;
                        }
                        _uncaughtExceptions.Add(e);
                    }
                };
#if PROTO_PROMISE_POOL_DISABLE // Are we testing the pool or not? (used for command-line testing)
                Promise.Config.ObjectPoolingEnabled = false;
#else
                Promise.Config.ObjectPoolingEnabled = true;
#endif
                Promise.Config.DebugCausalityTracer = Promise.TraceLevel.None; // Disabled because it makes the tests slow.

                _stopwatch = Stopwatch.StartNew();
            }

            TestContext.Progress.WriteLine("Begin time: " + _stopwatch.Elapsed.ToString() + ", test: " + TestContext.CurrentContext.Test.FullName);
        }

        public static void AssertRejection(object expected, object actual)
        {
#if ENABLE_IL2CPP && !NET_LEGACY && !UNITY_2022_1_OR_NEWER
            // ExceptionDispatchInfo.Throw() generates a new Exception object instead of throwing the original object in IL2CPP, causing the Assert to fail.
            // This was fixed in Unity 2022. To avoid the tests failing, we instead have to check the object's type and message.
            if (expected is Exception)
            {
                Exception ex = (Exception) expected;
                Assert.AreEqual(ex.GetType(), actual.GetType());
                Exception actualEx = (Exception) actual;
                Assert.AreEqual(ex.Message, actualEx.Message);
                return;
            }
#endif
            Assert.AreEqual(expected, actual);
        }

        public static void Cleanup()
        {
            WaitForAllThreadsToCompleteAndGcCollect();

            Exception[] exceptions;
            lock (_uncaughtExceptions)
            {
                exceptions = _uncaughtExceptions.ToArray();
                _uncaughtExceptions.Clear();
            }
            if (exceptions.Length > 0)
            {
#if true // Set to false to throw all uncaught rejections, set to true to only throw 1 exception to avoid overloading the test error output.
                throw exceptions[0];
#else
                throw new AggregateException(exceptions);
#endif
            }

#if PROMISE_DEBUG
            Internal.AssertAllObjectsReleased();
#endif

            TestContext.Progress.WriteLine("Success time: " + _stopwatch.Elapsed.ToString() + ", test: " + TestContext.CurrentContext.Test.FullName);
        }

        private static void WaitForAllThreadsToCompleteAndGcCollect()
        {
            _backgroundContext.WaitForAllThreadsToComplete();

            ExecuteForegroundCallbacks();
            GcCollectAndWaitForFinalizers();
        }

        public static void GcCollectAndWaitForFinalizers()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        public static void ExecuteForegroundCallbacks()
        {
            _foregroundContext.Execute();
        }

        public static void ExecuteForegroundCallbacksAndWaitForThreadsToComplete()
        {
            _foregroundContext.Execute();
            _backgroundContext.WaitForAllThreadsToComplete();
        }

        public static float Lerp(float a, float b, float t)
        {
            return a + (b - a) * t;
        }

        public static Action<Promise.Deferred, CancelationSource> GetCompleterVoid(CompleteType completeType, string rejectValue)
        {
            switch (completeType)
            {
                case CompleteType.Resolve:
                    return (deferred, _) => deferred.Resolve();
                case CompleteType.Reject:
                    return (deferred, _) => deferred.Reject(rejectValue);
                case CompleteType.Cancel:
                    return (deferred, _) => deferred.Cancel();
                case CompleteType.CancelFromToken:
                    return (_, cancelationSource) => cancelationSource.Cancel();
            }
            throw new Exception();
        }

        public static Action<Promise<T>.Deferred, CancelationSource> GetCompleterT<T>(CompleteType completeType, T resolveValue, string rejectValue)
        {
            switch (completeType)
            {
                case CompleteType.Resolve:
                    return (deferred, _) => deferred.Resolve(resolveValue);
                case CompleteType.Reject:
                    return (deferred, _) => deferred.Reject(rejectValue);
                case CompleteType.Cancel:
                    return (deferred, _) => deferred.Cancel();
                case CompleteType.CancelFromToken:
                    return (_, cancelationSource) => cancelationSource.Cancel();
            }
            throw new Exception();
        }

        public static Action<Promise.Deferred, CancelationSource> GetTryCompleterVoid(CompleteType completeType, string rejectValue)
        {
            switch (completeType)
            {
                case CompleteType.Resolve:
                    return (deferred, _) => deferred.TryResolve();
                case CompleteType.Reject:
                    return (deferred, _) => deferred.TryReject(rejectValue);
                case CompleteType.Cancel:
                    return (deferred, _) => deferred.TryCancel();
                case CompleteType.CancelFromToken:
                    return (_, cancelationSource) => cancelationSource.TryCancel();
            }
            throw new Exception();
        }

        public static Action<Promise<T>.Deferred, CancelationSource> GetTryCompleterT<T>(CompleteType completeType, T resolveValue, string rejectValue)
        {
            switch (completeType)
            {
                case CompleteType.Resolve:
                    return (deferred, _) => deferred.TryResolve(resolveValue);
                case CompleteType.Reject:
                    return (deferred, _) => deferred.TryReject(rejectValue);
                case CompleteType.Cancel:
                    return (deferred, _) => deferred.TryCancel();
                case CompleteType.CancelFromToken:
                    return (_, cancelationSource) => cancelationSource.TryCancel();
            }
            throw new Exception();
        }

        public static Promise.Deferred GetNewDeferredVoid(CompleteType completeType, out CancelationSource cancelationSource)
        {
            if (completeType == CompleteType.CancelFromToken)
            {
                cancelationSource = CancelationSource.New();
                return Promise.NewDeferred(cancelationSource.Token);
            }
            cancelationSource = default(CancelationSource);
            return Promise.NewDeferred();
        }

        public static Promise<T>.Deferred GetNewDeferredT<T>(CompleteType completeType, out CancelationSource cancelationSource)
        {
            if (completeType == CompleteType.CancelFromToken)
            {
                cancelationSource = CancelationSource.New();
                return Promise<T>.NewDeferred(cancelationSource.Token);
            }
            cancelationSource = default(CancelationSource);
            return Promise<T>.NewDeferred();
        }

        public static Promise BuildPromise<TReject>(CompleteType completeType, bool isAlreadyComplete, TReject reason, out Promise.Deferred deferred, out CancelationSource cancelationSource)
        {
            if (!isAlreadyComplete)
            {
                deferred = GetNewDeferredVoid(completeType, out cancelationSource);
                return deferred.Promise;
            }

            deferred = default(Promise.Deferred);
            cancelationSource = default(CancelationSource);
            switch (completeType)
            {
                case CompleteType.Resolve:
                {
                    return Promise.Resolved();
                }
                case CompleteType.Reject:
                {
                    return Promise.Rejected(reason);
                }
                default:
                {
                    return Promise.Canceled();
                }
            }
        }

        public static Promise<T> BuildPromise<T, TReject>(CompleteType completeType, bool isAlreadyComplete, T value, TReject reason, out Promise<T>.Deferred deferred, out CancelationSource cancelationSource)
        {
            if (!isAlreadyComplete)
            {
                deferred = GetNewDeferredT<T>(completeType, out cancelationSource);
                return deferred.Promise;
            }

            deferred = default(Promise<T>.Deferred);
            cancelationSource = default(CancelationSource);
            switch (completeType)
            {
                case CompleteType.Resolve:
                {
                    return Promise.Resolved(value);
                }
                case CompleteType.Reject:
                {
                    return Promise<T>.Rejected(reason);
                }
                default:
                {
                    return Promise<T>.Canceled();
                }
            }
        }

        public static Promise ThenDuplicate(this Promise promise, CancelationToken cancelationToken = default(CancelationToken))
        {
            return promise.Then(() => { }, cancelationToken);
        }

        public static Promise<T> ThenDuplicate<T>(this Promise<T> promise, CancelationToken cancelationToken = default(CancelationToken))
        {
            return promise.Then(v => v, cancelationToken);
        }

        public static Promise SubscribeProgress(this Promise promise, ProgressHelper progressHelper, CancelationToken cancelationToken = default(CancelationToken))
        {
            return progressHelper.Subscribe(promise, cancelationToken);
        }

        public static Promise<T> SubscribeProgress<T>(this Promise<T> promise, ProgressHelper progressHelper, CancelationToken cancelationToken = default(CancelationToken))
        {
            return progressHelper.Subscribe(promise, cancelationToken);
        }

        public static Promise SubscribeProgressAndAssert(this Promise promise, ProgressHelper progressHelper, float expectedValue, CancelationToken cancelationToken = default(CancelationToken), TimeSpan timeout = default(TimeSpan))
        {
            return progressHelper.SubscribeAndAssertCurrentProgress(promise, expectedValue, cancelationToken, timeout);
        }

        public static Promise<T> SubscribeProgressAndAssert<T>(this Promise<T> promise, ProgressHelper progressHelper, float expectedValue, CancelationToken cancelationToken = default(CancelationToken), TimeSpan timeout = default(TimeSpan))
        {
            return progressHelper.SubscribeAndAssertCurrentProgress(promise, expectedValue, cancelationToken, timeout);
        }

        public static Promise ConfigureAwait(this Promise promise, ConfigureAwaitType configureType, bool forceAsync = false, CancelationToken cancelationToken = default(CancelationToken))
        {
            if (configureType == ConfigureAwaitType.None)
            {
                return promise.WaitAsync(cancelationToken);
            }
            if (configureType == ConfigureAwaitType.Explicit)
            {
                return promise.WaitAsync(_foregroundContext, forceAsync, cancelationToken);
            }
            return promise.WaitAsync((SynchronizationOption) configureType, forceAsync, cancelationToken);
        }

        public static Promise<T> ConfigureAwait<T>(this Promise<T> promise, ConfigureAwaitType configureType, bool forceAsync = false, CancelationToken cancelationToken = default(CancelationToken))
        {
            if (configureType == ConfigureAwaitType.None)
            {
                return promise.WaitAsync(cancelationToken);
            }
            if (configureType == ConfigureAwaitType.Explicit)
            {
                return promise.WaitAsync(_foregroundContext, forceAsync, cancelationToken);
            }
            return promise.WaitAsync((SynchronizationOption) configureType, forceAsync, cancelationToken);
        }

        public static void WaitWithTimeout(this Promise promise, TimeSpan timeout)
        {
            if (!promise.Wait(timeout))
            {
                throw new TimeoutException("Promise.Wait timed out after " + timeout);
            }
        }

        public static void AssertCallbackContext(SynchronizationType expectedContext, SynchronizationType invokeContext, Thread foregroundThread)
        {
            switch (expectedContext)
            {
                case SynchronizationType.Foreground:
                case SynchronizationType.Explicit:
                {
                    Assert.AreEqual(foregroundThread, Thread.CurrentThread);
                    return;
                }
                case backgroundType:
                {
                    Assert.AreNotEqual(foregroundThread, Thread.CurrentThread);
                    Assert.IsTrue(Thread.CurrentThread.IsBackground);
                    return;
                }
                case SynchronizationType.Synchronous:
                {
                    if (invokeContext == SynchronizationType.Foreground || invokeContext == SynchronizationType.Explicit)
                    {
                        goto case SynchronizationType.Foreground;
                    }
                    if (invokeContext == backgroundType)
                    {
                        goto case backgroundType;
                    }
                    return;
                }
            }
            throw new Exception("Unexpected callback contexts, expectedContext: " + expectedContext + ", invokeContext: " + invokeContext);
        }

        public static readonly float progressEpsilon = Promise.Config.ProgressPrecision;

        public const int callbacksMultiplier = 3
#if CSHARP_7_3_OR_NEWER
            + 1
#endif
#if PROMISE_PROGRESS
            + 1
#endif
            ;

        public const int resolveVoidCallbacks = 72 * callbacksMultiplier;
        public const int resolveTCallbacks = 72 * callbacksMultiplier;
        public const int rejectVoidCallbacks = 72 * callbacksMultiplier;
        public const int rejectTCallbacks = 72 * callbacksMultiplier;

        public const int resolveOnlyVoidCallbacks = 8 * callbacksMultiplier;
        public const int resolveOnlyTCallbacks = 8 * callbacksMultiplier;

        public const int resolveVoidVoidCallbacks = 18 * callbacksMultiplier;
        public const int resolveVoidConvertCallbacks = 18 * callbacksMultiplier;
        public const int resolveTVoidCallbacks = 18 * callbacksMultiplier;
        public const int resolveTConvertCallbacks = 18 * callbacksMultiplier;
        public const int rejectVoidVoidCallbacks = 20 * callbacksMultiplier;
        public const int rejectVoidConvertCallbacks = 16 * callbacksMultiplier;
        public const int rejectTVoidCallbacks = 16 * callbacksMultiplier;
        public const int rejectTConvertCallbacks = 16 * callbacksMultiplier;
        public const int rejectTTCallbacks = 4 * callbacksMultiplier;

        public const int resolveVoidPromiseVoidCallbacks = 18 * callbacksMultiplier;
        public const int resolveVoidPromiseConvertCallbacks = 18 * callbacksMultiplier;
        public const int resolveTPromiseVoidCallbacks = 18 * callbacksMultiplier;
        public const int resolveTPromiseConvertCallbacks = 18 * callbacksMultiplier;
        public const int rejectVoidPromiseVoidCallbacks = 20 * callbacksMultiplier;
        public const int rejectVoidPromiseConvertCallbacks = 16 * callbacksMultiplier;
        public const int rejectTPromiseVoidCallbacks = 16 * callbacksMultiplier;
        public const int rejectTPromiseConvertCallbacks = 16 * callbacksMultiplier;
        public const int rejectTPromiseTCallbacks = 4 * callbacksMultiplier;

        public const int rejectVoidKnownCallbacks = 36 * callbacksMultiplier;
        public const int rejectTKnownCallbacks = 36 * callbacksMultiplier;

        public const int continueVoidCallbacks = 8 * callbacksMultiplier;
        public const int continueVoidVoidCallbacks = 2 * callbacksMultiplier;
        public const int continueVoidConvertCallbacks = 2 * callbacksMultiplier;
        public const int continueVoidPromiseVoidCallbacks = 2 * callbacksMultiplier;
        public const int continueVoidPromiseConvertCallbacks = 2 * callbacksMultiplier;

        public const int continueTCallbacks = 8 * callbacksMultiplier;
        public const int continueTVoidCallbacks = 2 * callbacksMultiplier;
        public const int continueTConvertCallbacks = 2 * callbacksMultiplier;
        public const int continueTPromiseVoidCallbacks = 2 * callbacksMultiplier;
        public const int continueTPromiseConvertCallbacks = 2 * callbacksMultiplier;

        public const int cancelVoidCallbacks = (72 + 8 + 8) * callbacksMultiplier;
        public const int cancelTCallbacks = (72 + 8 + 8) * callbacksMultiplier;

        public const int onCancelCallbacks = 4 * callbacksMultiplier;

        public static IEnumerable<Promise> GetTestablePromises(Promise preservedPromise)
        {
            // This helps to test several different kinds of promises to make sure they all work with the same API.
            yield return preservedPromise;
            yield return preservedPromise.Duplicate();
            var deferred = Promise.NewDeferred();
            preservedPromise
                .ContinueWith(deferred, (d, result) =>
                {
                    if (result.State == Promise.State.Resolved)
                    {
                        d.Resolve();
                    }
                    else if (result.State == Promise.State.Canceled)
                    {
                        d.Cancel();
                    }
                    else
                    {
                        d.Reject(result.RejectReason);
                    }
                })
                .Forget();
            yield return deferred.Promise;
#if CSHARP_7_3_OR_NEWER
            yield return Await(preservedPromise);
#endif
#if PROMISE_PROGRESS
            yield return preservedPromise.Progress(v => { }, SynchronizationOption.Synchronous);
#endif
        }

        public static IEnumerable<Promise<T>> GetTestablePromises<T>(Promise<T> preservedPromise)
        {
            // This helps to test several different kinds of promises to make sure they all work with the same API.
            yield return preservedPromise;
            yield return preservedPromise.Duplicate();
            var deferred = Promise.NewDeferred<T>();
            preservedPromise
                .ContinueWith(deferred, (d, result) =>
                {
                    if (result.State == Promise.State.Resolved)
                    {
                        d.Resolve(result.Result);
                    }
                    else if (result.State == Promise.State.Canceled)
                    {
                        d.Cancel();
                    }
                    else
                    {
                        d.Reject(result.RejectReason);
                    }
                })
                .Forget();
            yield return deferred.Promise;
#if CSHARP_7_3_OR_NEWER
            yield return Await(preservedPromise);
#endif
#if PROMISE_PROGRESS
            yield return preservedPromise.Progress(v => { }, SynchronizationOption.Synchronous);
#endif
        }

#if CSHARP_7_3_OR_NEWER
        private static async Promise Await(Promise promise)
        {
            await promise;
        }

        private static async Promise<T> Await<T>(Promise<T> promise)
        {
            return await promise;
        }
#endif

        public static void AddResolveCallbacks<TConvert, TCapture>(Promise promise,
            Action onResolve = null, TConvert convertValue = default(TConvert),
            Action<TCapture> onResolveCapture = null, TCapture captureValue = default(TCapture),
            Func<Promise, Promise> promiseToPromise = null, Func<Promise<TConvert>, Promise<TConvert>> promiseToPromiseConvert = null,
            TestAction<Promise> onCallbackAdded = null, TestAction<Promise<TConvert>> onCallbackAddedConvert = null,
            Action onCancel = null,
            TestAction<Promise> onAdoptCallbackAdded = null, TestAction<Promise<TConvert>> onAdoptCallbackAddedConvert = null,
            ConfigureAwaitType configureAwaitType = ConfigureAwaitType.None, bool configureAwaitForceAsync = false)
        {
            promise = promise.Preserve();
            promise.Catch(() => { }).Forget(); // Suppress any rejections from the preserved promise.

            AddResolveCallbacksWithCancelation(
                promise,
                onResolve, convertValue,
                onResolveCapture, captureValue,
                promiseToPromise, promiseToPromiseConvert,
                onCallbackAdded, onCallbackAddedConvert,
                default(CancelationToken), default(CancelationToken),
                onCancel,
                onAdoptCallbackAdded, onAdoptCallbackAddedConvert,
                configureAwaitType
            );

            CancelationSource cancelationSource = CancelationSource.New();
            AddResolveCallbacksWithCancelation(
                promise,
                onResolve, convertValue,
                onResolveCapture, captureValue,
                promiseToPromise, promiseToPromiseConvert,
                onCallbackAdded, onCallbackAddedConvert,
                cancelationSource.Token, cancelationSource.Token,
                onCancel,
                onAdoptCallbackAdded, onAdoptCallbackAddedConvert,
                configureAwaitType
            );
            cancelationSource.Dispose();

            promise.Forget();
        }

        public static void AddResolveCallbacksWithCancelation<TConvert, TCapture>(Promise promise,
            Action onResolve = null, TConvert convertValue = default(TConvert),
            Action<TCapture> onResolveCapture = null, TCapture captureValue = default(TCapture),
            Func<Promise, Promise> promiseToPromise = null, Func<Promise<TConvert>, Promise<TConvert>> promiseToPromiseConvert = null,
            TestAction<Promise> onCallbackAdded = null, TestAction<Promise<TConvert>> onCallbackAddedConvert = null,
            CancelationToken cancelationToken = default(CancelationToken), CancelationToken waitAsyncCancelationToken = default(CancelationToken),
            Action onCancel = null,
            TestAction<Promise> onAdoptCallbackAdded = null, TestAction<Promise<TConvert>> onAdoptCallbackAddedConvert = null,
            ConfigureAwaitType configureAwaitType = ConfigureAwaitType.None, bool configureAwaitForceAsync = false)
        {
            promise = promise.Preserve();
            promise.Catch(() => { }).Forget(); // Suppress any rejections from the preserved promise.

            // Add empty delegates so no need for null check.
            onResolve += () => { };
            onResolveCapture += _ => { };
            if (promiseToPromise == null)
            {
                promiseToPromise = _ => Promise.Resolved();
            }
            if (promiseToPromiseConvert == null)
            {
                promiseToPromiseConvert = _ => Promise.Resolved(convertValue);
            }
            if (onCallbackAdded == null)
            {
                onCallbackAdded = (ref Promise p) => p.Forget();
            }
            if (onCallbackAddedConvert == null)
            {
                onCallbackAddedConvert = (ref Promise<TConvert> p) => p.Forget();
            }
            if (onAdoptCallbackAdded == null)
            {
                onAdoptCallbackAdded = (ref Promise p) => { };
            }
            if (onAdoptCallbackAddedConvert == null)
            {
                onAdoptCallbackAddedConvert = (ref Promise<TConvert> p) => { };
            }
            onCancel += () => { };

            foreach (var p in GetTestablePromises(promise))
            {
                Promise p1 = default(Promise);
                p1 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(() => { onResolve(); }, cancelationToken)
                    .CatchCancelation(onCancel);
                onCallbackAdded(ref p1);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise<TConvert> p2 = default(Promise<TConvert>);
                p2 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(() => { onResolve(); return convertValue; }, cancelationToken)
                    .CatchCancelation(() => { onCancel(); return convertValue; });
                onCallbackAddedConvert(ref p2);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise p3 = default(Promise);
                p3 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(() => { onResolve(); return promiseToPromise(p3); }, cancelationToken)
                    .CatchCancelation(onCancel);
                onAdoptCallbackAdded(ref p3);
                onCallbackAdded(ref p3);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise<TConvert> p4 = default(Promise<TConvert>);
                p4 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(() => { onResolve(); return promiseToPromiseConvert(p4); }, cancelationToken)
                    .CatchCancelation(() => { onCancel(); return convertValue; });
                onAdoptCallbackAddedConvert(ref p4);
                onCallbackAddedConvert(ref p4);
            }

            foreach (var p in GetTestablePromises(promise))
            {
                Promise p5 = default(Promise);
                p5 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(captureValue, cv => { onResolveCapture(cv); onResolve(); }, cancelationToken)
                    .CatchCancelation(onCancel);
                onCallbackAdded(ref p5);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise<TConvert> p6 = default(Promise<TConvert>);
                p6 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return convertValue; }, cancelationToken)
                    .CatchCancelation(() => { onCancel(); return convertValue; });
                onCallbackAddedConvert(ref p6);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise p7 = default(Promise);
                p7 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return promiseToPromise(p7); }, cancelationToken)
                    .CatchCancelation(onCancel);
                onAdoptCallbackAdded(ref p7);
                onCallbackAdded(ref p7);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise<TConvert> p8 = default(Promise<TConvert>);
                p8 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return promiseToPromiseConvert(p8); }, cancelationToken)
                    .CatchCancelation(() => { onCancel(); return convertValue; });
                onAdoptCallbackAddedConvert(ref p8);
                onCallbackAddedConvert(ref p8);
            }

            promise.Forget();
        }

        public static void AddResolveCallbacks<T, TConvert, TCapture>(Promise<T> promise,
            Action<T> onResolve = null, TConvert convertValue = default(TConvert),
            Action<TCapture> onResolveCapture = null, TCapture captureValue = default(TCapture),
            Func<Promise, Promise> promiseToPromise = null, Func<Promise<TConvert>, Promise<TConvert>> promiseToPromiseConvert = null,
            TestAction<Promise> onCallbackAdded = null, TestAction<Promise<TConvert>> onCallbackAddedConvert = null,
            Action onCancel = null,
            TestAction<Promise> onAdoptCallbackAdded = null, TestAction<Promise<TConvert>> onAdoptCallbackAddedConvert = null,
            ConfigureAwaitType configureAwaitType = ConfigureAwaitType.None, bool configureAwaitForceAsync = false)
        {
            promise = promise.Preserve();
            promise.Catch(() => { }).Forget(); // Suppress any rejections from the preserved promise.

            AddResolveCallbacksWithCancelation(
                promise,
                onResolve, convertValue,
                onResolveCapture, captureValue,
                promiseToPromise, promiseToPromiseConvert,
                onCallbackAdded, onCallbackAddedConvert,
                default(CancelationToken), default(CancelationToken),
                onCancel,
                onAdoptCallbackAdded, onAdoptCallbackAddedConvert,
                configureAwaitType
            );

            CancelationSource cancelationSource = CancelationSource.New();
            AddResolveCallbacksWithCancelation(
                promise,
                onResolve, convertValue,
                onResolveCapture, captureValue,
                promiseToPromise, promiseToPromiseConvert,
                onCallbackAdded, onCallbackAddedConvert,
                cancelationSource.Token, cancelationSource.Token,
                onCancel,
                onAdoptCallbackAdded, onAdoptCallbackAddedConvert,
                configureAwaitType
            );
            cancelationSource.Dispose();

            promise.Forget();
        }

        public static void AddResolveCallbacksWithCancelation<T, TConvert, TCapture>(Promise<T> promise,
            Action<T> onResolve = null, TConvert convertValue = default(TConvert),
            Action<TCapture> onResolveCapture = null, TCapture captureValue = default(TCapture),
            Func<Promise, Promise> promiseToPromise = null, Func<Promise<TConvert>, Promise<TConvert>> promiseToPromiseConvert = null,
            TestAction<Promise> onCallbackAdded = null, TestAction<Promise<TConvert>> onCallbackAddedConvert = null,
            CancelationToken cancelationToken = default(CancelationToken), CancelationToken waitAsyncCancelationToken = default(CancelationToken),
            Action onCancel = null,
            TestAction<Promise> onAdoptCallbackAdded = null, TestAction<Promise<TConvert>> onAdoptCallbackAddedConvert = null,
            ConfigureAwaitType configureAwaitType = ConfigureAwaitType.None, bool configureAwaitForceAsync = false)
        {
            promise = promise.Preserve();
            promise.Catch(() => { }).Forget(); // Suppress any rejections from the preserved promise.

            // Add empty delegates so no need for null check.
            onResolve += _ => { };
            onResolveCapture += _ => { };
            if (promiseToPromise == null)
            {
                promiseToPromise = _ => Promise.Resolved();
            }
            if (promiseToPromiseConvert == null)
            {
                promiseToPromiseConvert = _ => Promise.Resolved(convertValue);
            }
            if (onCallbackAdded == null)
            {
                onCallbackAdded = (ref Promise p) => p.Forget();
            }
            if (onCallbackAddedConvert == null)
            {
                onCallbackAddedConvert = (ref Promise<TConvert> p) => p.Forget();
            }
            if (onAdoptCallbackAdded == null)
            {
                onAdoptCallbackAdded = (ref Promise p) => { };
            }
            if (onAdoptCallbackAddedConvert == null)
            {
                onAdoptCallbackAddedConvert = (ref Promise<TConvert> p) => { };
            }
            onCancel += () => { };

            foreach (var p in GetTestablePromises(promise))
            {
                Promise p1 = default(Promise);
                p1 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(x => { onResolve(x); }, cancelationToken)
                    .CatchCancelation(onCancel);
                onCallbackAdded(ref p1);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise<TConvert> p2 = default(Promise<TConvert>);
                p2 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(x => { onResolve(x); return convertValue; }, cancelationToken)
                    .CatchCancelation(() => { onCancel(); return convertValue; });
                onCallbackAddedConvert(ref p2);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise p3 = default(Promise);
                p3 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(x => { onResolve(x); return promiseToPromise(p3); }, cancelationToken)
                    .CatchCancelation(onCancel);
                onAdoptCallbackAdded(ref p3);
                onCallbackAdded(ref p3);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise<TConvert> p4 = default(Promise<TConvert>);
                p4 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(x => { onResolve(x); return promiseToPromiseConvert(p4); }, cancelationToken)
                    .CatchCancelation(() => { onCancel(); return convertValue; });
                onAdoptCallbackAddedConvert(ref p4);
                onCallbackAddedConvert(ref p4);
            }

            foreach (var p in GetTestablePromises(promise))
            {
                Promise p5 = default(Promise);
                p5 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); }, cancelationToken)
                    .CatchCancelation(onCancel);
                onCallbackAdded(ref p5);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise<TConvert> p6 = default(Promise<TConvert>);
                p6 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return convertValue; }, cancelationToken)
                    .CatchCancelation(() => { onCancel(); return convertValue; });
                onCallbackAddedConvert(ref p6);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise p7 = default(Promise);
                p7 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return promiseToPromise(p7); }, cancelationToken)
                    .CatchCancelation(onCancel);
                onAdoptCallbackAdded(ref p7);
                onCallbackAdded(ref p7);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise<TConvert> p8 = default(Promise<TConvert>);
                p8 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return promiseToPromiseConvert(p8); }, cancelationToken)
                    .CatchCancelation(() => { onCancel(); return convertValue; });
                onAdoptCallbackAddedConvert(ref p8);
                onCallbackAddedConvert(ref p8);
            }

            promise.Forget();
        }

        public static void AddCallbacks<TConvert, TReject, TCapture>(Promise promise,
            Action onResolve = null, Action<TReject> onReject = null, Action onUnknownRejection = null, TConvert convertValue = default(TConvert),
            Action<TCapture> onResolveCapture = null, Action<TCapture> onRejectCapture = null, Action<TCapture> onUnknownRejectionCapture = null, TCapture captureValue = default(TCapture),
            Func<Promise, Promise> promiseToPromise = null, Func<Promise<TConvert>, Promise<TConvert>> promiseToPromiseConvert = null,
            TestAction<Promise> onCallbackAdded = null, TestAction<Promise<TConvert>> onCallbackAddedConvert = null,
            Action onCancel = null,
            TestAction<Promise> onDirectCallbackAdded = null, TestAction<Promise<TConvert>> onDirectCallbackAddedConvert = null, TestAction<Promise> onDirectCallbackAddedCatch = null,
            TestAction<Promise, AdoptLocation> onAdoptCallbackAdded = null, TestAction<Promise<TConvert>, AdoptLocation> onAdoptCallbackAddedConvert = null, TestAction<Promise> onAdoptCallbackAddedCatch = null,
            ConfigureAwaitType configureAwaitType = ConfigureAwaitType.None, bool configureAwaitForceAsync = false)
        {
            promise = promise.Preserve();
            promise.Catch(() => { }).Forget(); // Suppress any rejections from the preserved promise.

            AddCallbacksWithCancelation(
                promise,
                onResolve, onReject, onUnknownRejection, convertValue,
                onResolveCapture, onRejectCapture, onUnknownRejectionCapture, captureValue,
                promiseToPromise, promiseToPromiseConvert,
                onCallbackAdded, onCallbackAddedConvert,
                default(CancelationToken), default(CancelationToken),
                onCancel,
                onDirectCallbackAdded, onDirectCallbackAddedConvert, onDirectCallbackAddedCatch,
                onAdoptCallbackAdded, onAdoptCallbackAddedConvert, onAdoptCallbackAddedCatch,
                configureAwaitType
            );

            CancelationSource cancelationSource = CancelationSource.New();
            AddCallbacksWithCancelation(
                promise,
                onResolve, onReject, onUnknownRejection, convertValue,
                onResolveCapture, onRejectCapture, onUnknownRejectionCapture, captureValue,
                promiseToPromise, promiseToPromiseConvert,
                onCallbackAdded, onCallbackAddedConvert,
                cancelationSource.Token, cancelationSource.Token,
                onCancel,
                onDirectCallbackAdded, onDirectCallbackAddedConvert, onDirectCallbackAddedCatch,
                onAdoptCallbackAdded, onAdoptCallbackAddedConvert, onAdoptCallbackAddedCatch,
                configureAwaitType
            );
            cancelationSource.Dispose();

            promise.Forget();
        }

        public static void AddCallbacksWithCancelation<TConvert, TReject, TCapture>(Promise promise,
            Action onResolve = null, Action<TReject> onReject = null, Action onUnknownRejection = null, TConvert convertValue = default(TConvert),
            Action<TCapture> onResolveCapture = null, Action<TCapture> onRejectCapture = null, Action<TCapture> onUnknownRejectionCapture = null, TCapture captureValue = default(TCapture),
            Func<Promise, Promise> promiseToPromise = null, Func<Promise<TConvert>, Promise<TConvert>> promiseToPromiseConvert = null,
            TestAction<Promise> onCallbackAdded = null, TestAction<Promise<TConvert>> onCallbackAddedConvert = null,
            CancelationToken cancelationToken = default(CancelationToken), CancelationToken waitAsyncCancelationToken = default(CancelationToken),
            Action onCancel = null,
            TestAction<Promise> onDirectCallbackAdded = null, TestAction<Promise<TConvert>> onDirectCallbackAddedConvert = null, TestAction<Promise> onDirectCallbackAddedCatch = null,
            TestAction<Promise, AdoptLocation> onAdoptCallbackAdded = null, TestAction<Promise<TConvert>, AdoptLocation> onAdoptCallbackAddedConvert = null, TestAction<Promise> onAdoptCallbackAddedCatch = null,
            ConfigureAwaitType configureAwaitType = ConfigureAwaitType.None, bool configureAwaitForceAsync = false)
        {
            promise = promise.Preserve();
            promise.Catch(() => { }).Forget(); // Suppress any rejections from the preserved promise.

            // Add empty delegates so no need for null check.
            onResolve += () => { };
            onReject += s => { };
            onUnknownRejection += () => { };
            onResolveCapture += _ => { };
            onRejectCapture += _ => { };
            onUnknownRejectionCapture += _ => { };
            if (promiseToPromise == null)
            {
                promiseToPromise = _ => Promise.Resolved();
            }
            if (promiseToPromiseConvert == null)
            {
                promiseToPromiseConvert = _ => Promise.Resolved(convertValue);
            }
            if (onCallbackAdded == null)
            {
                onCallbackAdded = (ref Promise p) => p.Forget();
            }
            if (onCallbackAddedConvert == null)
            {
                onCallbackAddedConvert = (ref Promise<TConvert> p) => p.Forget();
            }
            if (onDirectCallbackAdded == null)
            {
                onDirectCallbackAdded = (ref Promise p) => { };
            }
            if (onDirectCallbackAddedConvert == null)
            {
                onDirectCallbackAddedConvert = (ref Promise<TConvert> p) => { };
            }
            if (onDirectCallbackAddedCatch == null)
            {
                onDirectCallbackAddedCatch = (ref Promise p) => { };
            }
            if (onAdoptCallbackAdded == null)
            {
                onAdoptCallbackAdded = (ref Promise p, AdoptLocation _) => { };
            }
            if (onAdoptCallbackAddedConvert == null)
            {
                onAdoptCallbackAddedConvert = (ref Promise<TConvert> p, AdoptLocation _) => { };
            }
            if (onAdoptCallbackAddedCatch == null)
            {
                onAdoptCallbackAddedCatch = (ref Promise p) => { };
            }
            onCancel += () => { };

            foreach (var p in GetTestablePromises(promise))
            {
                Promise p1 = default(Promise);
                p1 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(() => { onResolve(); }, () => { onUnknownRejection(); }, cancelationToken)
                    .CatchCancelation(onCancel);
                onDirectCallbackAdded(ref p1);
                onCallbackAdded(ref p1);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise p2 = default(Promise);
                p2 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(() => { onResolve(); }, (TReject failValue) => { onReject(failValue); }, cancelationToken)
                    .CatchCancelation(onCancel);
                onDirectCallbackAdded(ref p2);
                onCallbackAdded(ref p2);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise p3 = default(Promise);
                p3 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(() => { onResolve(); }, () => { onUnknownRejection(); return promiseToPromise(p3); }, cancelationToken)
                    .CatchCancelation(onCancel);
                onAdoptCallbackAdded(ref p3, AdoptLocation.Reject);
                onCallbackAdded(ref p3);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise p4 = default(Promise);
                p4 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(() => { onResolve(); }, (TReject failValue) => { onReject(failValue); return promiseToPromise(p4); }, cancelationToken)
                    .CatchCancelation(onCancel);
                onAdoptCallbackAdded(ref p4, AdoptLocation.Reject);
                onCallbackAdded(ref p4);
            }

            foreach (var p in GetTestablePromises(promise))
            {
                Promise<TConvert> p5 = default(Promise<TConvert>);
                p5 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(() => { onResolve(); return convertValue; }, () => { onUnknownRejection(); return convertValue; }, cancelationToken)
                    .CatchCancelation(() => { onCancel(); return convertValue; });
                onDirectCallbackAddedConvert(ref p5);
                onCallbackAddedConvert(ref p5);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise<TConvert> p6 = default(Promise<TConvert>);
                p6 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(() => { onResolve(); return convertValue; }, (TReject failValue) => { onReject(failValue); return convertValue; }, cancelationToken)
                    .CatchCancelation(() => { onCancel(); return convertValue; });
                onDirectCallbackAddedConvert(ref p6);
                onCallbackAddedConvert(ref p6);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise<TConvert> p7 = default(Promise<TConvert>);
                p7 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(() => { onResolve(); return convertValue; }, () => { onUnknownRejection(); return promiseToPromiseConvert(p7); }, cancelationToken)
                    .CatchCancelation(() => { onCancel(); return convertValue; });
                onAdoptCallbackAddedConvert(ref p7, AdoptLocation.Reject);
                onCallbackAddedConvert(ref p7);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise<TConvert> p8 = default(Promise<TConvert>);
                p8 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(() => { onResolve(); return convertValue; }, (TReject failValue) => { onReject(failValue); return promiseToPromiseConvert(p8); }, cancelationToken)
                    .CatchCancelation(() => { onCancel(); return convertValue; });
                onAdoptCallbackAddedConvert(ref p8, AdoptLocation.Reject);
                onCallbackAddedConvert(ref p8);
            }

            foreach (var p in GetTestablePromises(promise))
            {
                Promise p9 = default(Promise);
                p9 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(() => { onResolve(); return promiseToPromise(p9); }, () => { onUnknownRejection(); return promiseToPromise(p9); }, cancelationToken)
                    .CatchCancelation(onCancel);
                onAdoptCallbackAdded(ref p9, AdoptLocation.Both);
                onCallbackAdded(ref p9);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise p10 = default(Promise);
                p10 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(() => { onResolve(); return promiseToPromise(p10); }, (TReject failValue) => { onReject(failValue); return promiseToPromise(p10); }, cancelationToken)
                    .CatchCancelation(onCancel);
                onAdoptCallbackAdded(ref p10, AdoptLocation.Both);
                onCallbackAdded(ref p10);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise p11 = default(Promise);
                p11 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(() => { onResolve(); return promiseToPromise(p11); }, () => { onUnknownRejection(); }, cancelationToken)
                    .CatchCancelation(onCancel);
                onAdoptCallbackAdded(ref p11, AdoptLocation.Resolve);
                onCallbackAdded(ref p11);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise p12 = default(Promise);
                p12 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(() => { onResolve(); return promiseToPromise(p12); }, (TReject failValue) => { onReject(failValue); }, cancelationToken)
                    .CatchCancelation(onCancel);
                onAdoptCallbackAdded(ref p12, AdoptLocation.Resolve);
                onCallbackAdded(ref p12);
            }

            foreach (var p in GetTestablePromises(promise))
            {
                Promise<TConvert> p13 = default(Promise<TConvert>);
                p13 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(() => { onResolve(); return promiseToPromiseConvert(p13); }, () => { onUnknownRejection(); return promiseToPromiseConvert(p13); }, cancelationToken)
                    .CatchCancelation(() => { onCancel(); return convertValue; });
                onAdoptCallbackAddedConvert(ref p13, AdoptLocation.Both);
                onCallbackAddedConvert(ref p13);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise<TConvert> p14 = default(Promise<TConvert>);
                p14 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(() => { onResolve(); return promiseToPromiseConvert(p14); }, (TReject failValue) => { onReject(failValue); return promiseToPromiseConvert(p14); }, cancelationToken)
                    .CatchCancelation(() => { onCancel(); return convertValue; });
                onAdoptCallbackAddedConvert(ref p14, AdoptLocation.Both);
                onCallbackAddedConvert(ref p14);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise<TConvert> p15 = default(Promise<TConvert>);
                p15 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(() => { onResolve(); return promiseToPromiseConvert(p15); }, () => { onUnknownRejection(); return convertValue; }, cancelationToken)
                    .CatchCancelation(() => { onCancel(); return convertValue; });
                onAdoptCallbackAddedConvert(ref p15, AdoptLocation.Resolve);
                onCallbackAddedConvert(ref p15);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise<TConvert> p16 = default(Promise<TConvert>);
                p16 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(() => { onResolve(); return promiseToPromiseConvert(p16); }, (TReject failValue) => { onReject(failValue); return convertValue; }, cancelationToken)
                    .CatchCancelation(() => { onCancel(); return convertValue; });
                onAdoptCallbackAddedConvert(ref p16, AdoptLocation.Resolve);
                onCallbackAddedConvert(ref p16);
            }


            foreach (var p in GetTestablePromises(promise))
            {
                Promise p17 = default(Promise);
                p17 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Catch(() => { onUnknownRejection(); }, cancelationToken)
                    .CatchCancelation(onCancel);
                onDirectCallbackAddedCatch(ref p17);
                onCallbackAdded(ref p17);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise p18 = default(Promise);
                p18 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Catch((TReject failValue) => { onReject(failValue); }, cancelationToken)
                    .CatchCancelation(onCancel);
                onDirectCallbackAddedCatch(ref p18);
                onCallbackAdded(ref p18);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise p19 = default(Promise);
                p19 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Catch(() => { onUnknownRejection(); return promiseToPromise(p19); }, cancelationToken)
                    .CatchCancelation(onCancel);
                onAdoptCallbackAddedCatch(ref p19);
                onCallbackAdded(ref p19);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise p20 = default(Promise);
                p20 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Catch((TReject failValue) => { onReject(failValue); return promiseToPromise(p20); }, cancelationToken)
                    .CatchCancelation(onCancel);
                onAdoptCallbackAddedCatch(ref p20);
                onCallbackAdded(ref p20);
            }


            foreach (var p in GetTestablePromises(promise))
            {
                Promise p21 = default(Promise);
                p21 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(captureValue, cv => { onResolveCapture(cv); onResolve(); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); }, cancelationToken)
                    .CatchCancelation(onCancel);
                onDirectCallbackAdded(ref p21);
                onCallbackAdded(ref p21);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise p22 = default(Promise);
                p22 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(captureValue, cv => { onResolveCapture(cv); onResolve(); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); }, cancelationToken)
                    .CatchCancelation(onCancel);
                onDirectCallbackAdded(ref p22);
                onCallbackAdded(ref p22);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise p23 = default(Promise);
                p23 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(captureValue, cv => { onResolveCapture(cv); onResolve(); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return promiseToPromise(p23); }, cancelationToken)
                    .CatchCancelation(onCancel);
                onAdoptCallbackAdded(ref p23, AdoptLocation.Reject);
                onCallbackAdded(ref p23);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise p24 = default(Promise);
                p24 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(captureValue, cv => { onResolveCapture(cv); onResolve(); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return promiseToPromise(p24); }, cancelationToken)
                    .CatchCancelation(onCancel);
                onAdoptCallbackAdded(ref p24, AdoptLocation.Reject);
                onCallbackAdded(ref p24);
            }

            foreach (var p in GetTestablePromises(promise))
            {
                Promise<TConvert> p25 = default(Promise<TConvert>);
                p25 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return convertValue; }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return convertValue; }, cancelationToken)
                    .CatchCancelation(() => { onCancel(); return convertValue; });
                onDirectCallbackAddedConvert(ref p25);
                onCallbackAddedConvert(ref p25);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise<TConvert> p26 = default(Promise<TConvert>);
                p26 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return convertValue; }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return convertValue; }, cancelationToken)
                    .CatchCancelation(() => { onCancel(); return convertValue; });
                onDirectCallbackAddedConvert(ref p26);
                onCallbackAddedConvert(ref p26);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise<TConvert> p27 = default(Promise<TConvert>);
                p27 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return convertValue; }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return promiseToPromiseConvert(p27); }, cancelationToken)
                    .CatchCancelation(() => { onCancel(); return convertValue; });
                onAdoptCallbackAddedConvert(ref p27, AdoptLocation.Reject);
                onCallbackAddedConvert(ref p27);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise<TConvert> p28 = default(Promise<TConvert>);
                p28 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return convertValue; }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return promiseToPromiseConvert(p28); }, cancelationToken)
                    .CatchCancelation(() => { onCancel(); return convertValue; });
                onAdoptCallbackAddedConvert(ref p28, AdoptLocation.Reject);
                onCallbackAddedConvert(ref p28);
            }

            foreach (var p in GetTestablePromises(promise))
            {
                Promise p29 = default(Promise);
                p29 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return promiseToPromise(p29); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return promiseToPromise(p29); }, cancelationToken)
                    .CatchCancelation(onCancel);
                onAdoptCallbackAdded(ref p29, AdoptLocation.Both);
                onCallbackAdded(ref p29);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise p30 = default(Promise);
                p30 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return promiseToPromise(p30); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return promiseToPromise(p30); }, cancelationToken)
                    .CatchCancelation(onCancel);
                onAdoptCallbackAdded(ref p30, AdoptLocation.Both);
                onCallbackAdded(ref p30);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise p31 = default(Promise);
                p31 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return promiseToPromise(p31); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); }, cancelationToken)
                    .CatchCancelation(onCancel);
                onAdoptCallbackAdded(ref p31, AdoptLocation.Resolve);
                onCallbackAdded(ref p31);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise p32 = default(Promise);
                p32 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return promiseToPromise(p32); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); }, cancelationToken)
                    .CatchCancelation(onCancel);
                onAdoptCallbackAdded(ref p32, AdoptLocation.Resolve);
                onCallbackAdded(ref p32);
            }

            foreach (var p in GetTestablePromises(promise))
            {
                Promise<TConvert> p33 = default(Promise<TConvert>);
                p33 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return promiseToPromiseConvert(p33); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return promiseToPromiseConvert(p33); }, cancelationToken)
                    .CatchCancelation(() => { onCancel(); return convertValue; });
                onAdoptCallbackAddedConvert(ref p33, AdoptLocation.Both);
                onCallbackAddedConvert(ref p33);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise<TConvert> p34 = default(Promise<TConvert>);
                p34 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return promiseToPromiseConvert(p34); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return promiseToPromiseConvert(p34); }, cancelationToken)
                    .CatchCancelation(() => { onCancel(); return convertValue; });
                onAdoptCallbackAddedConvert(ref p34, AdoptLocation.Both);
                onCallbackAddedConvert(ref p34);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise<TConvert> p35 = default(Promise<TConvert>);
                p35 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return promiseToPromiseConvert(p35); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return convertValue; }, cancelationToken)
                    .CatchCancelation(() => { onCancel(); return convertValue; });
                onAdoptCallbackAddedConvert(ref p35, AdoptLocation.Resolve);
                onCallbackAddedConvert(ref p35);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise<TConvert> p36 = default(Promise<TConvert>);
                p36 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return promiseToPromiseConvert(p36); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return convertValue; }, cancelationToken)
                    .CatchCancelation(() => { onCancel(); return convertValue; });
                onAdoptCallbackAddedConvert(ref p36, AdoptLocation.Resolve);
                onCallbackAddedConvert(ref p36);
            }


            foreach (var p in GetTestablePromises(promise))
            {
                Promise p37 = default(Promise);
                p37 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Catch(captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); }, cancelationToken)
                    .CatchCancelation(onCancel);
                onDirectCallbackAddedCatch(ref p37);
                onCallbackAdded(ref p37);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise p38 = default(Promise);
                p38 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Catch(captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); }, cancelationToken)
                    .CatchCancelation(onCancel);
                onDirectCallbackAddedCatch(ref p38);
                onCallbackAdded(ref p38);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise p39 = default(Promise);
                p39 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Catch(captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return promiseToPromise(p39); }, cancelationToken)
                    .CatchCancelation(onCancel);
                onAdoptCallbackAddedCatch(ref p39);
                onCallbackAdded(ref p39);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise p40 = default(Promise);
                p40 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Catch(captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return promiseToPromise(p40); }, cancelationToken)
                    .CatchCancelation(onCancel);
                onAdoptCallbackAddedCatch(ref p40);
                onCallbackAdded(ref p40);
            }


            foreach (var p in GetTestablePromises(promise))
            {
                Promise p41 = default(Promise);
                p41 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(captureValue, cv => { onResolveCapture(cv); onResolve(); }, () => { onUnknownRejection(); }, cancelationToken)
                    .CatchCancelation(onCancel);
                onDirectCallbackAdded(ref p41);
                onCallbackAdded(ref p41);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise p42 = default(Promise);
                p42 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(captureValue, cv => { onResolveCapture(cv); onResolve(); }, (TReject failValue) => { onReject(failValue); }, cancelationToken)
                    .CatchCancelation(onCancel);
                onDirectCallbackAdded(ref p42);
                onCallbackAdded(ref p42);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise p43 = default(Promise);
                p43 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(captureValue, cv => { onResolveCapture(cv); onResolve(); }, () => { onUnknownRejection(); return promiseToPromise(p43); }, cancelationToken)
                    .CatchCancelation(onCancel);
                onAdoptCallbackAdded(ref p43, AdoptLocation.Reject);
                onCallbackAdded(ref p43);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise p44 = default(Promise);
                p44 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(captureValue, cv => { onResolveCapture(cv); onResolve(); }, (TReject failValue) => { onReject(failValue); return promiseToPromise(p44); }, cancelationToken)
                    .CatchCancelation(onCancel);
                onAdoptCallbackAdded(ref p44, AdoptLocation.Reject);
                onCallbackAdded(ref p44);
            }

            foreach (var p in GetTestablePromises(promise))
            {
                Promise<TConvert> p45 = default(Promise<TConvert>);
                p45 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return convertValue; }, () => { onUnknownRejection(); return convertValue; }, cancelationToken)
                    .CatchCancelation(() => { onCancel(); return convertValue; });
                onDirectCallbackAddedConvert(ref p45);
                onCallbackAddedConvert(ref p45);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise<TConvert> p46 = default(Promise<TConvert>);
                p46 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return convertValue; }, (TReject failValue) => { onReject(failValue); return convertValue; }, cancelationToken)
                    .CatchCancelation(() => { onCancel(); return convertValue; });
                onDirectCallbackAddedConvert(ref p46);
                onCallbackAddedConvert(ref p46);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise<TConvert> p47 = default(Promise<TConvert>);
                p47 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return convertValue; }, () => { onUnknownRejection(); return promiseToPromiseConvert(p47); }, cancelationToken)
                    .CatchCancelation(() => { onCancel(); return convertValue; });
                onAdoptCallbackAddedConvert(ref p47, AdoptLocation.Reject);
                onCallbackAddedConvert(ref p47);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise<TConvert> p48 = default(Promise<TConvert>);
                p48 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return convertValue; }, (TReject failValue) => { onReject(failValue); return promiseToPromiseConvert(p48); }, cancelationToken)
                    .CatchCancelation(() => { onCancel(); return convertValue; });
                onAdoptCallbackAddedConvert(ref p48, AdoptLocation.Reject);
                onCallbackAddedConvert(ref p48);
            }

            foreach (var p in GetTestablePromises(promise))
            {
                Promise p49 = default(Promise);
                p49 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return promiseToPromise(p49); }, () => { onUnknownRejection(); return promiseToPromise(p49); }, cancelationToken)
                    .CatchCancelation(onCancel);
                onAdoptCallbackAdded(ref p49, AdoptLocation.Both);
                onCallbackAdded(ref p49);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise p50 = default(Promise);
                p50 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return promiseToPromise(p50); }, (TReject failValue) => { onReject(failValue); return promiseToPromise(p50); }, cancelationToken)
                    .CatchCancelation(onCancel);
                onAdoptCallbackAdded(ref p50, AdoptLocation.Both);
                onCallbackAdded(ref p50);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise p51 = default(Promise);
                p51 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return promiseToPromise(p51); }, () => { onUnknownRejection(); }, cancelationToken)
                    .CatchCancelation(onCancel);
                onAdoptCallbackAdded(ref p51, AdoptLocation.Resolve);
                onCallbackAdded(ref p51);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise p52 = default(Promise);
                p52 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return promiseToPromise(p52); }, (TReject failValue) => { onReject(failValue); }, cancelationToken)
                    .CatchCancelation(onCancel);
                onAdoptCallbackAdded(ref p52, AdoptLocation.Resolve);
                onCallbackAdded(ref p52);
            }

            foreach (var p in GetTestablePromises(promise))
            {
                Promise<TConvert> p53 = default(Promise<TConvert>);
                p53 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return promiseToPromiseConvert(p53); }, () => { onUnknownRejection(); return promiseToPromiseConvert(p53); }, cancelationToken)
                    .CatchCancelation(() => { onCancel(); return convertValue; });
                onAdoptCallbackAddedConvert(ref p53, AdoptLocation.Both);
                onCallbackAddedConvert(ref p53);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise<TConvert> p54 = default(Promise<TConvert>);
                p54 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return promiseToPromiseConvert(p54); }, (TReject failValue) => { onReject(failValue); return promiseToPromiseConvert(p54); }, cancelationToken)
                    .CatchCancelation(() => { onCancel(); return convertValue; });
                onAdoptCallbackAddedConvert(ref p54, AdoptLocation.Both);
                onCallbackAddedConvert(ref p54);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise<TConvert> p55 = default(Promise<TConvert>);
                p55 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return promiseToPromiseConvert(p55); }, () => { onUnknownRejection(); return convertValue; }, cancelationToken)
                    .CatchCancelation(() => { onCancel(); return convertValue; });
                onAdoptCallbackAddedConvert(ref p55, AdoptLocation.Resolve);
                onCallbackAddedConvert(ref p55);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise<TConvert> p56 = default(Promise<TConvert>);
                p56 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return promiseToPromiseConvert(p56); }, (TReject failValue) => { onReject(failValue); return convertValue; }, cancelationToken)
                    .CatchCancelation(() => { onCancel(); return convertValue; });
                onAdoptCallbackAddedConvert(ref p56, AdoptLocation.Resolve);
                onCallbackAddedConvert(ref p56);
            }


            foreach (var p in GetTestablePromises(promise))
            {
                Promise p57 = default(Promise);
                p57 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(() => { onResolve(); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); }, cancelationToken)
                    .CatchCancelation(onCancel);
                onDirectCallbackAdded(ref p57);
                onCallbackAdded(ref p57);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise p58 = default(Promise);
                p58 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(() => { onResolve(); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); }, cancelationToken)
                    .CatchCancelation(onCancel);
                onDirectCallbackAdded(ref p58);
                onCallbackAdded(ref p58);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise p59 = default(Promise);
                p59 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(() => { onResolve(); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return promiseToPromise(p59); }, cancelationToken)
                    .CatchCancelation(onCancel);
                onAdoptCallbackAdded(ref p59, AdoptLocation.Reject);
                onCallbackAdded(ref p59);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise p60 = default(Promise);
                p60 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(() => { onResolve(); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return promiseToPromise(p60); }, cancelationToken)
                    .CatchCancelation(onCancel);
                onAdoptCallbackAdded(ref p60, AdoptLocation.Reject);
                onCallbackAdded(ref p60);
            }

            foreach (var p in GetTestablePromises(promise))
            {
                Promise<TConvert> p61 = default(Promise<TConvert>);
                p61 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(() => { onResolve(); return convertValue; }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return convertValue; }, cancelationToken)
                    .CatchCancelation(() => { onCancel(); return convertValue; });
                onDirectCallbackAddedConvert(ref p61);
                onCallbackAddedConvert(ref p61);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise<TConvert> p62 = default(Promise<TConvert>);
                p62 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(() => { onResolve(); return convertValue; }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return convertValue; }, cancelationToken)
                    .CatchCancelation(() => { onCancel(); return convertValue; });
                onDirectCallbackAddedConvert(ref p62);
                onCallbackAddedConvert(ref p62);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise<TConvert> p63 = default(Promise<TConvert>);
                p63 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(() => { onResolve(); return convertValue; }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return promiseToPromiseConvert(p63); }, cancelationToken)
                    .CatchCancelation(() => { onCancel(); return convertValue; });
                onAdoptCallbackAddedConvert(ref p63, AdoptLocation.Reject);
                onCallbackAddedConvert(ref p63);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise<TConvert> p64 = default(Promise<TConvert>);
                p64 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(() => { onResolve(); return convertValue; }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return promiseToPromiseConvert(p64); }, cancelationToken)
                    .CatchCancelation(() => { onCancel(); return convertValue; });
                onAdoptCallbackAddedConvert(ref p64, AdoptLocation.Reject);
                onCallbackAddedConvert(ref p64);
            }

            foreach (var p in GetTestablePromises(promise))
            {
                Promise p65 = default(Promise);
                p65 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(() => { onResolve(); return promiseToPromise(p65); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return promiseToPromise(p65); }, cancelationToken)
                    .CatchCancelation(onCancel);
                onAdoptCallbackAdded(ref p65, AdoptLocation.Both);
                onCallbackAdded(ref p65);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise p66 = default(Promise);
                p66 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(() => { onResolve(); return promiseToPromise(p66); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return promiseToPromise(p66); }, cancelationToken)
                    .CatchCancelation(onCancel);
                onAdoptCallbackAdded(ref p66, AdoptLocation.Both);
                onCallbackAdded(ref p66);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise p67 = default(Promise);
                p67 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(() => { onResolve(); return promiseToPromise(p67); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); }, cancelationToken)
                    .CatchCancelation(onCancel);
                onAdoptCallbackAdded(ref p67, AdoptLocation.Resolve);
                onCallbackAdded(ref p67);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise p68 = default(Promise);
                p68 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(() => { onResolve(); return promiseToPromise(p68); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); }, cancelationToken)
                    .CatchCancelation(onCancel);
                onAdoptCallbackAdded(ref p68, AdoptLocation.Resolve);
                onCallbackAdded(ref p68);
            }

            foreach (var p in GetTestablePromises(promise))
            {
                Promise<TConvert> p69 = default(Promise<TConvert>);
                p69 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(() => { onResolve(); return promiseToPromiseConvert(p69); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return promiseToPromiseConvert(p69); }, cancelationToken)
                    .CatchCancelation(() => { onCancel(); return convertValue; });
                onAdoptCallbackAddedConvert(ref p69, AdoptLocation.Both);
                onCallbackAddedConvert(ref p69);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise<TConvert> p70 = default(Promise<TConvert>);
                p70 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(() => { onResolve(); return promiseToPromiseConvert(p70); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return promiseToPromiseConvert(p70); }, cancelationToken)
                    .CatchCancelation(() => { onCancel(); return convertValue; });
                onAdoptCallbackAddedConvert(ref p70, AdoptLocation.Both);
                onCallbackAddedConvert(ref p70);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise<TConvert> p71 = default(Promise<TConvert>);
                p71 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(() => { onResolve(); return promiseToPromiseConvert(p71); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return convertValue; }, cancelationToken)
                    .CatchCancelation(() => { onCancel(); return convertValue; });
                onAdoptCallbackAddedConvert(ref p71, AdoptLocation.Resolve);
                onCallbackAddedConvert(ref p71);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise<TConvert> p72 = default(Promise<TConvert>);
                p72 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(() => { onResolve(); return promiseToPromiseConvert(p72); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return convertValue; }, cancelationToken)
                    .CatchCancelation(() => { onCancel(); return convertValue; });
                onAdoptCallbackAddedConvert(ref p72, AdoptLocation.Resolve);
                onCallbackAddedConvert(ref p72);
            }

            promise.Forget();
        }

        public static void AddCallbacks<T, TConvert, TReject, TCapture>(Promise<T> promise,
            Action<T> onResolve = null, Action<TReject> onReject = null, Action onUnknownRejection = null, TConvert convertValue = default(TConvert), T TValue = default(T),
            Action<TCapture> onResolveCapture = null, Action<TCapture> onRejectCapture = null, Action<TCapture> onUnknownRejectionCapture = null, TCapture captureValue = default(TCapture),
            Func<Promise, Promise> promiseToPromise = null, Func<Promise<TConvert>, Promise<TConvert>> promiseToPromiseConvert = null, Func<Promise<T>, Promise<T>> promiseToPromiseT = null,
            TestAction<Promise> onCallbackAdded = null, TestAction<Promise<TConvert>> onCallbackAddedConvert = null, TestAction<Promise<T>> onCallbackAddedT = null,
            Action onCancel = null,
            TestAction<Promise> onDirectCallbackAdded = null, TestAction<Promise<TConvert>> onDirectCallbackAddedConvert = null, TestAction<Promise<T>> onDirectCallbackAddedT = null,
            TestAction<Promise, AdoptLocation> onAdoptCallbackAdded = null, TestAction<Promise<TConvert>, AdoptLocation> onAdoptCallbackAddedConvert = null, TestAction<Promise<T>> onAdoptCallbackAddedT = null,
            ConfigureAwaitType configureAwaitType = ConfigureAwaitType.None, bool configureAwaitForceAsync = false)
        {
            promise = promise.Preserve();
            promise.Catch(() => { }).Forget(); // Suppress any rejections from the preserved promise.

            AddCallbacksWithCancelation(
                promise,
                onResolve, onReject, onUnknownRejection, convertValue, TValue,
                onResolveCapture, onRejectCapture, onUnknownRejectionCapture, captureValue,
                promiseToPromise, promiseToPromiseConvert, promiseToPromiseT,
                onCallbackAdded, onCallbackAddedConvert, onCallbackAddedT,
                default(CancelationToken), default(CancelationToken),
                onCancel,
                onDirectCallbackAdded, onDirectCallbackAddedConvert, onDirectCallbackAddedT,
                onAdoptCallbackAdded, onAdoptCallbackAddedConvert, onAdoptCallbackAddedT,
                configureAwaitType
            );

            CancelationSource cancelationSource = CancelationSource.New();
            AddCallbacksWithCancelation(
                promise,
                onResolve, onReject, onUnknownRejection, convertValue, TValue,
                onResolveCapture, onRejectCapture, onUnknownRejectionCapture, captureValue,
                promiseToPromise, promiseToPromiseConvert, promiseToPromiseT,
                onCallbackAdded, onCallbackAddedConvert, onCallbackAddedT,
                cancelationSource.Token, cancelationSource.Token,
                onCancel,
                onDirectCallbackAdded, onDirectCallbackAddedConvert, onDirectCallbackAddedT,
                onAdoptCallbackAdded, onAdoptCallbackAddedConvert, onAdoptCallbackAddedT,
                configureAwaitType
            );
            cancelationSource.Dispose();

            promise.Forget();
        }

        public static void AddCallbacksWithCancelation<T, TConvert, TReject, TCapture>(Promise<T> promise,
            Action<T> onResolve = null, Action<TReject> onReject = null, Action onUnknownRejection = null, TConvert convertValue = default(TConvert), T TValue = default(T),
            Action<TCapture> onResolveCapture = null, Action<TCapture> onRejectCapture = null, Action<TCapture> onUnknownRejectionCapture = null, TCapture captureValue = default(TCapture),
            Func<Promise, Promise> promiseToPromise = null, Func<Promise<TConvert>, Promise<TConvert>> promiseToPromiseConvert = null, Func<Promise<T>, Promise<T>> promiseToPromiseT = null,
            TestAction<Promise> onCallbackAdded = null, TestAction<Promise<TConvert>> onCallbackAddedConvert = null, TestAction<Promise<T>> onCallbackAddedT = null,
            CancelationToken cancelationToken = default(CancelationToken), CancelationToken waitAsyncCancelationToken = default(CancelationToken),
            Action onCancel = null,
            TestAction<Promise> onDirectCallbackAdded = null, TestAction<Promise<TConvert>> onDirectCallbackAddedConvert = null, TestAction<Promise<T>> onDirectCallbackAddedT = null,
            TestAction<Promise, AdoptLocation> onAdoptCallbackAdded = null, TestAction<Promise<TConvert>, AdoptLocation> onAdoptCallbackAddedConvert = null, TestAction<Promise<T>> onAdoptCallbackAddedT = null,
            ConfigureAwaitType configureAwaitType = ConfigureAwaitType.None, bool configureAwaitForceAsync = false)
        {
            promise = promise.Preserve();
            promise.Catch(() => { }).Forget(); // Suppress any rejections from the preserved promise.

            // Add empty delegates so no need for null check.
            onResolve += x => { };
            onReject += s => { };
            onUnknownRejection += () => { };
            onResolveCapture += _ => { };
            onRejectCapture += _ => { };
            onUnknownRejectionCapture += _ => { };
            if (promiseToPromiseT == null)
            {
                promiseToPromiseT = _ => Promise.Resolved(TValue);
            }
            if (promiseToPromise == null)
            {
                promiseToPromise = _ => Promise.Resolved();
            }
            if (promiseToPromiseConvert == null)
            {
                promiseToPromiseConvert = _ => Promise.Resolved(convertValue);
            }
            if (onCallbackAdded == null)
            {
                onCallbackAdded = (ref Promise p) => p.Forget();
            }
            if (onCallbackAddedConvert == null)
            {
                onCallbackAddedConvert = (ref Promise<TConvert> p) => p.Forget();
            }
            if (onCallbackAddedT == null)
            {
                onCallbackAddedT = (ref Promise<T> p) => p.Forget();
            }
            if (onDirectCallbackAdded == null)
            {
                onDirectCallbackAdded = (ref Promise p) => { };
            }
            if (onDirectCallbackAddedConvert == null)
            {
                onDirectCallbackAddedConvert = (ref Promise<TConvert> p) => { };
            }
            if (onDirectCallbackAddedT == null)
            {
                onDirectCallbackAddedT = (ref Promise<T> p) => { };
            }
            if (onAdoptCallbackAdded == null)
            {
                onAdoptCallbackAdded = (ref Promise p, AdoptLocation _) => { };
            }
            if (onAdoptCallbackAddedConvert == null)
            {
                onAdoptCallbackAddedConvert = (ref Promise<TConvert> p, AdoptLocation _) => { };
            }
            if (onAdoptCallbackAddedT == null)
            {
                onAdoptCallbackAddedT = (ref Promise<T> p) => { };
            }
            onCancel += () => { };

            foreach (var p in GetTestablePromises(promise))
            {
                Promise p1 = default(Promise);
                p1 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(x => { onResolve(x); }, () => { onUnknownRejection(); }, cancelationToken)
                    .CatchCancelation(onCancel);
                onDirectCallbackAdded(ref p1);
                onCallbackAdded(ref p1);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise p2 = default(Promise);
                p2 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(x => { onResolve(x); }, (TReject failValue) => { onReject(failValue); }, cancelationToken)
                    .CatchCancelation(onCancel);
                onDirectCallbackAdded(ref p2);
                onCallbackAdded(ref p2);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise p3 = default(Promise);
                p3 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(x => { onResolve(x); }, () => { onUnknownRejection(); return promiseToPromise(p3); }, cancelationToken)
                    .CatchCancelation(onCancel);
                onAdoptCallbackAdded(ref p3, AdoptLocation.Reject);
                onCallbackAdded(ref p3);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise p4 = default(Promise);
                p4 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(x => { onResolve(x); }, (TReject failValue) => { onReject(failValue); return promiseToPromise(p4); }, cancelationToken)
                    .CatchCancelation(onCancel);
                onAdoptCallbackAdded(ref p4, AdoptLocation.Reject);
                onCallbackAdded(ref p4);
            }

            foreach (var p in GetTestablePromises(promise))
            {
                Promise<TConvert> p5 = default(Promise<TConvert>);
                p5 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(x => { onResolve(x); return convertValue; }, () => { onUnknownRejection(); return convertValue; }, cancelationToken)
                    .CatchCancelation(() => { onCancel(); return convertValue; });
                onDirectCallbackAddedConvert(ref p5);
                onCallbackAddedConvert(ref p5);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise<TConvert> p6 = default(Promise<TConvert>);
                p6 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(x => { onResolve(x); return convertValue; }, (TReject failValue) => { onReject(failValue); return convertValue; }, cancelationToken)
                    .CatchCancelation(() => { onCancel(); return convertValue; });
                onDirectCallbackAddedConvert(ref p6);
                onCallbackAddedConvert(ref p6);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise<TConvert> p7 = default(Promise<TConvert>);
                p7 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(x => { onResolve(x); return convertValue; }, () => { onUnknownRejection(); return promiseToPromiseConvert(p7); }, cancelationToken)
                    .CatchCancelation(() => { onCancel(); return convertValue; });
                onAdoptCallbackAddedConvert(ref p7, AdoptLocation.Reject);
                onCallbackAddedConvert(ref p7);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise<TConvert> p8 = default(Promise<TConvert>);
                p8 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(x => { onResolve(x); return convertValue; }, (TReject failValue) => { onReject(failValue); return promiseToPromiseConvert(p8); }, cancelationToken)
                    .CatchCancelation(() => { onCancel(); return convertValue; });
                onAdoptCallbackAddedConvert(ref p8, AdoptLocation.Reject);
                onCallbackAddedConvert(ref p8);
            }

            foreach (var p in GetTestablePromises(promise))
            {
                Promise p9 = default(Promise);
                p9 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(x => { onResolve(x); return promiseToPromise(p9); }, () => { onUnknownRejection(); return promiseToPromise(p9); }, cancelationToken)
                    .CatchCancelation(onCancel);
                onAdoptCallbackAdded(ref p9, AdoptLocation.Both);
                onCallbackAdded(ref p9);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise p10 = default(Promise);
                p10 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(x => { onResolve(x); return promiseToPromise(p10); }, (TReject failValue) => { onReject(failValue); return promiseToPromise(p10); }, cancelationToken)
                    .CatchCancelation(onCancel);
                onAdoptCallbackAdded(ref p10, AdoptLocation.Both);
                onCallbackAdded(ref p10);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise p11 = default(Promise);
                p11 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(x => { onResolve(x); return promiseToPromise(p11); }, () => { onUnknownRejection(); }, cancelationToken)
                    .CatchCancelation(onCancel);
                onAdoptCallbackAdded(ref p11, AdoptLocation.Resolve);
                onCallbackAdded(ref p11);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise p12 = default(Promise);
                p12 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(x => { onResolve(x); return promiseToPromise(p12); }, (TReject failValue) => { onReject(failValue); }, cancelationToken)
                    .CatchCancelation(onCancel);
                onAdoptCallbackAdded(ref p12, AdoptLocation.Resolve);
                onCallbackAdded(ref p12);
            }

            foreach (var p in GetTestablePromises(promise))
            {
                Promise<TConvert> p13 = default(Promise<TConvert>);
                p13 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(x => { onResolve(x); return promiseToPromiseConvert(p13); }, () => { onUnknownRejection(); return promiseToPromiseConvert(p13); }, cancelationToken)
                    .CatchCancelation(() => { onCancel(); return convertValue; });
                onAdoptCallbackAddedConvert(ref p13, AdoptLocation.Both);
                onCallbackAddedConvert(ref p13);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise<TConvert> p14 = default(Promise<TConvert>);
                p14 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(x => { onResolve(x); return promiseToPromiseConvert(p14); }, (TReject failValue) => { onReject(failValue); return promiseToPromiseConvert(p14); }, cancelationToken)
                    .CatchCancelation(() => { onCancel(); return convertValue; });
                onAdoptCallbackAddedConvert(ref p14, AdoptLocation.Both);
                onCallbackAddedConvert(ref p14);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise<TConvert> p15 = default(Promise<TConvert>);
                p15 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(x => { onResolve(x); return promiseToPromiseConvert(p15); }, () => { onUnknownRejection(); return convertValue; }, cancelationToken)
                    .CatchCancelation(() => { onCancel(); return convertValue; });
                onAdoptCallbackAddedConvert(ref p15, AdoptLocation.Resolve);
                onCallbackAddedConvert(ref p15);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise<TConvert> p16 = default(Promise<TConvert>);
                p16 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(x => { onResolve(x); return promiseToPromiseConvert(p16); }, (TReject failValue) => { onReject(failValue); return convertValue; }, cancelationToken)
                    .CatchCancelation(() => { onCancel(); return convertValue; });
                onAdoptCallbackAddedConvert(ref p16, AdoptLocation.Resolve);
                onCallbackAddedConvert(ref p16);
            }


            foreach (var p in GetTestablePromises(promise))
            {
                Promise<T> p17 = default(Promise<T>);
                p17 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Catch(() => { onUnknownRejection(); return TValue; }, cancelationToken)
                    .CatchCancelation(() => { onCancel(); return TValue; });
                onDirectCallbackAddedT(ref p17);
                onCallbackAddedT(ref p17);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise<T> p18 = default(Promise<T>);
                p18 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Catch((TReject failValue) => { onReject(failValue); return TValue; }, cancelationToken)
                    .CatchCancelation(() => { onCancel(); return TValue; });
                onDirectCallbackAddedT(ref p18);
                onCallbackAddedT(ref p18);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise<T> p19 = default(Promise<T>);
                p19 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Catch(() => { onUnknownRejection(); return promiseToPromiseT(p19); }, cancelationToken)
                    .CatchCancelation(() => { onCancel(); return TValue; });
                onAdoptCallbackAddedT(ref p19);
                onCallbackAddedT(ref p19);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise<T> p20 = default(Promise<T>);
                p20 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Catch((TReject failValue) => { onReject(failValue); return promiseToPromiseT(p20); }, cancelationToken)
                    .CatchCancelation(() => { onCancel(); return TValue; });
                onAdoptCallbackAddedT(ref p20);
                onCallbackAddedT(ref p20);
            }


            foreach (var p in GetTestablePromises(promise))
            {
                Promise p21 = default(Promise);
                p21 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); }, cancelationToken)
                    .CatchCancelation(onCancel);
                onDirectCallbackAdded(ref p21);
                onCallbackAdded(ref p21);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise p22 = default(Promise);
                p22 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); }, cancelationToken)
                    .CatchCancelation(onCancel);
                onDirectCallbackAdded(ref p22);
                onCallbackAdded(ref p22);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise p23 = default(Promise);
                p23 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return promiseToPromise(p23); }, cancelationToken)
                    .CatchCancelation(onCancel);
                onAdoptCallbackAdded(ref p23, AdoptLocation.Reject);
                onCallbackAdded(ref p23);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise p24 = default(Promise);
                p24 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return promiseToPromise(p24); }, cancelationToken)
                    .CatchCancelation(onCancel);
                onAdoptCallbackAdded(ref p24, AdoptLocation.Reject);
                onCallbackAdded(ref p24);
            }

            foreach (var p in GetTestablePromises(promise))
            {
                Promise<TConvert> p25 = default(Promise<TConvert>);
                p25 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return convertValue; }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return convertValue; }, cancelationToken)
                    .CatchCancelation(() => { onCancel(); return convertValue; });
                onDirectCallbackAddedConvert(ref p25);
                onCallbackAddedConvert(ref p25);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise<TConvert> p26 = default(Promise<TConvert>);
                p26 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return convertValue; }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return convertValue; }, cancelationToken)
                    .CatchCancelation(() => { onCancel(); return convertValue; });
                onDirectCallbackAddedConvert(ref p26);
                onCallbackAddedConvert(ref p26);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise<TConvert> p27 = default(Promise<TConvert>);
                p27 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return convertValue; }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return promiseToPromiseConvert(p27); }, cancelationToken)
                    .CatchCancelation(() => { onCancel(); return convertValue; });
                onAdoptCallbackAddedConvert(ref p27, AdoptLocation.Reject);
                onCallbackAddedConvert(ref p27);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise<TConvert> p28 = default(Promise<TConvert>);
                p28 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return convertValue; }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return promiseToPromiseConvert(p28); }, cancelationToken)
                    .CatchCancelation(() => { onCancel(); return convertValue; });
                onAdoptCallbackAddedConvert(ref p28, AdoptLocation.Reject);
                onCallbackAddedConvert(ref p28);
            }

            foreach (var p in GetTestablePromises(promise))
            {
                Promise p29 = default(Promise);
                p29 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return promiseToPromise(p29); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return promiseToPromise(p29); }, cancelationToken)
                    .CatchCancelation(onCancel);
                onAdoptCallbackAdded(ref p29, AdoptLocation.Both);
                onCallbackAdded(ref p29);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise p30 = default(Promise);
                p30 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return promiseToPromise(p30); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return promiseToPromise(p30); }, cancelationToken)
                    .CatchCancelation(onCancel);
                onAdoptCallbackAdded(ref p30, AdoptLocation.Both);
                onCallbackAdded(ref p30);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise p31 = default(Promise);
                p31 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return promiseToPromise(p31); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); }, cancelationToken)
                    .CatchCancelation(onCancel);
                onAdoptCallbackAdded(ref p31, AdoptLocation.Resolve);
                onCallbackAdded(ref p31);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise p32 = default(Promise);
                p32 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return promiseToPromise(p32); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); }, cancelationToken)
                    .CatchCancelation(onCancel);
                onAdoptCallbackAdded(ref p32, AdoptLocation.Resolve);
                onCallbackAdded(ref p32);
            }


            foreach (var p in GetTestablePromises(promise))
            {
                Promise<TConvert> p33 = default(Promise<TConvert>);
                p33 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return promiseToPromiseConvert(p33); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return promiseToPromiseConvert(p33); }, cancelationToken)
                    .CatchCancelation(() => { onCancel(); return convertValue; });
                onAdoptCallbackAddedConvert(ref p33, AdoptLocation.Both);
                onCallbackAddedConvert(ref p33);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise<TConvert> p34 = default(Promise<TConvert>);
                p34 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return promiseToPromiseConvert(p34); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return promiseToPromiseConvert(p34); }, cancelationToken)
                    .CatchCancelation(() => { onCancel(); return convertValue; });
                onAdoptCallbackAddedConvert(ref p34, AdoptLocation.Both);
                onCallbackAddedConvert(ref p34);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise<TConvert> p35 = default(Promise<TConvert>);
                p35 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return promiseToPromiseConvert(p35); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return convertValue; }, cancelationToken)
                    .CatchCancelation(() => { onCancel(); return convertValue; });
                onAdoptCallbackAddedConvert(ref p35, AdoptLocation.Resolve);
                onCallbackAddedConvert(ref p35);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise<TConvert> p36 = default(Promise<TConvert>);
                p36 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return promiseToPromiseConvert(p36); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return convertValue; }, cancelationToken)
                    .CatchCancelation(() => { onCancel(); return convertValue; });
                onAdoptCallbackAddedConvert(ref p36, AdoptLocation.Resolve);
                onCallbackAddedConvert(ref p36);
            }


            foreach (var p in GetTestablePromises(promise))
            {
                Promise<T> p37 = default(Promise<T>);
                p37 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Catch(captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return TValue; }, cancelationToken)
                    .CatchCancelation(() => { onCancel(); return TValue; });
                onDirectCallbackAddedT(ref p37);
                onCallbackAddedT(ref p37);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise<T> p38 = default(Promise<T>);
                p38 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Catch(captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return TValue; }, cancelationToken)
                    .CatchCancelation(() => { onCancel(); return TValue; });
                onDirectCallbackAddedT(ref p38);
                onCallbackAddedT(ref p38);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise<T> p39 = default(Promise<T>);
                p39 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Catch(captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return promiseToPromiseT(p39); }, cancelationToken)
                    .CatchCancelation(() => { onCancel(); return TValue; });
                onAdoptCallbackAddedT(ref p39);
                onCallbackAddedT(ref p39);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise<T> p40 = default(Promise<T>);
                p40 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Catch(captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return promiseToPromiseT(p40); }, cancelationToken)
                    .CatchCancelation(() => { onCancel(); return TValue; });
                onAdoptCallbackAddedT(ref p40);
                onCallbackAddedT(ref p40);
            }


            foreach (var p in GetTestablePromises(promise))
            {
                Promise p41 = default(Promise);
                p41 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); }, () => { onUnknownRejection(); }, cancelationToken)
                    .CatchCancelation(onCancel);
                onDirectCallbackAdded(ref p41);
                onCallbackAdded(ref p41);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise p42 = default(Promise);
                p42 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); }, (TReject failValue) => { onReject(failValue); }, cancelationToken)
                    .CatchCancelation(onCancel);
                onDirectCallbackAdded(ref p42);
                onCallbackAdded(ref p42);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise p43 = default(Promise);
                p43 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); }, () => { onUnknownRejection(); return promiseToPromise(p43); }, cancelationToken)
                    .CatchCancelation(onCancel);
                onAdoptCallbackAdded(ref p43, AdoptLocation.Reject);
                onCallbackAdded(ref p43);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise p44 = default(Promise);
                p44 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); }, (TReject failValue) => { onReject(failValue); return promiseToPromise(p44); }, cancelationToken)
                    .CatchCancelation(onCancel);
                onAdoptCallbackAdded(ref p44, AdoptLocation.Reject);
                onCallbackAdded(ref p44);
            }

            foreach (var p in GetTestablePromises(promise))
            {
                Promise<TConvert> p45 = default(Promise<TConvert>);
                p45 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return convertValue; }, () => { onUnknownRejection(); return convertValue; }, cancelationToken)
                    .CatchCancelation(() => { onCancel(); return convertValue; });
                onDirectCallbackAddedConvert(ref p45);
                onCallbackAddedConvert(ref p45);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise<TConvert> p46 = default(Promise<TConvert>);
                p46 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return convertValue; }, (TReject failValue) => { onReject(failValue); return convertValue; }, cancelationToken)
                    .CatchCancelation(() => { onCancel(); return convertValue; });
                onDirectCallbackAddedConvert(ref p46);
                onCallbackAddedConvert(ref p46);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise<TConvert> p47 = default(Promise<TConvert>);
                p47 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return convertValue; }, () => { onUnknownRejection(); return promiseToPromiseConvert(p47); }, cancelationToken)
                    .CatchCancelation(() => { onCancel(); return convertValue; });
                onAdoptCallbackAddedConvert(ref p47, AdoptLocation.Reject);
                onCallbackAddedConvert(ref p47);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise<TConvert> p48 = default(Promise<TConvert>);
                p48 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return convertValue; }, (TReject failValue) => { onReject(failValue); return promiseToPromiseConvert(p48); }, cancelationToken)
                    .CatchCancelation(() => { onCancel(); return convertValue; });
                onAdoptCallbackAddedConvert(ref p48, AdoptLocation.Reject);
                onCallbackAddedConvert(ref p48);
            }

            foreach (var p in GetTestablePromises(promise))
            {
                Promise p49 = default(Promise);
                p49 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return promiseToPromise(p49); }, () => { onUnknownRejection(); return promiseToPromise(p49); }, cancelationToken)
                    .CatchCancelation(onCancel);
                onAdoptCallbackAdded(ref p49, AdoptLocation.Both);
                onCallbackAdded(ref p49);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise p50 = default(Promise);
                p50 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return promiseToPromise(p50); }, (TReject failValue) => { onReject(failValue); return promiseToPromise(p50); }, cancelationToken)
                    .CatchCancelation(onCancel);
                onAdoptCallbackAdded(ref p50, AdoptLocation.Both);
                onCallbackAdded(ref p50);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise p51 = default(Promise);
                p51 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return promiseToPromise(p51); }, () => { onUnknownRejection(); }, cancelationToken)
                    .CatchCancelation(onCancel);
                onAdoptCallbackAdded(ref p51, AdoptLocation.Resolve);
                onCallbackAdded(ref p51);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise p52 = default(Promise);
                p52 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return promiseToPromise(p52); }, (TReject failValue) => { onReject(failValue); }, cancelationToken)
                    .CatchCancelation(onCancel);
                onAdoptCallbackAdded(ref p52, AdoptLocation.Resolve);
                onCallbackAdded(ref p52);
            }

            foreach (var p in GetTestablePromises(promise))
            {
                Promise<TConvert> p53 = default(Promise<TConvert>);
                p53 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return promiseToPromiseConvert(p53); }, () => { onUnknownRejection(); return promiseToPromiseConvert(p53); }, cancelationToken)
                    .CatchCancelation(() => { onCancel(); return convertValue; });
                onAdoptCallbackAddedConvert(ref p53, AdoptLocation.Both);
                onCallbackAddedConvert(ref p53);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise<TConvert> p54 = default(Promise<TConvert>);
                p54 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return promiseToPromiseConvert(p54); }, (TReject failValue) => { onReject(failValue); return promiseToPromiseConvert(p54); }, cancelationToken)
                    .CatchCancelation(() => { onCancel(); return convertValue; });
                onAdoptCallbackAddedConvert(ref p54, AdoptLocation.Both);
                onCallbackAddedConvert(ref p54);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise<TConvert> p55 = default(Promise<TConvert>);
                p55 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return promiseToPromiseConvert(p55); }, () => { onUnknownRejection(); return convertValue; }, cancelationToken)
                    .CatchCancelation(() => { onCancel(); return convertValue; });
                onAdoptCallbackAddedConvert(ref p55, AdoptLocation.Resolve);
                onCallbackAddedConvert(ref p55);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise<TConvert> p56 = default(Promise<TConvert>);
                p56 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return promiseToPromiseConvert(p56); }, (TReject failValue) => { onReject(failValue); return convertValue; }, cancelationToken)
                    .CatchCancelation(() => { onCancel(); return convertValue; });
                onAdoptCallbackAddedConvert(ref p56, AdoptLocation.Resolve);
                onCallbackAddedConvert(ref p56);
            }


            foreach (var p in GetTestablePromises(promise))
            {
                Promise p57 = default(Promise);
                p57 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(x => { onResolve(x); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); }, cancelationToken)
                    .CatchCancelation(onCancel);
                onDirectCallbackAdded(ref p57);
                onCallbackAdded(ref p57);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise p58 = default(Promise);
                p58 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(x => { onResolve(x); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); }, cancelationToken)
                    .CatchCancelation(onCancel);
                onDirectCallbackAdded(ref p58);
                onCallbackAdded(ref p58);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise p59 = default(Promise);
                p59 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(x => { onResolve(x); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return promiseToPromise(p59); }, cancelationToken)
                    .CatchCancelation(onCancel);
                onAdoptCallbackAdded(ref p59, AdoptLocation.Reject);
                onCallbackAdded(ref p59);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise p60 = default(Promise);
                p60 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(x => { onResolve(x); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return promiseToPromise(p60); }, cancelationToken)
                    .CatchCancelation(onCancel);
                onAdoptCallbackAdded(ref p60, AdoptLocation.Reject);
                onCallbackAdded(ref p60);
            }

            foreach (var p in GetTestablePromises(promise))
            {
                Promise<TConvert> p61 = default(Promise<TConvert>);
                p61 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(x => { onResolve(x); return convertValue; }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return convertValue; }, cancelationToken)
                    .CatchCancelation(() => { onCancel(); return convertValue; });
                onDirectCallbackAddedConvert(ref p61);
                onCallbackAddedConvert(ref p61);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise<TConvert> p62 = default(Promise<TConvert>);
                p62 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(x => { onResolve(x); return convertValue; }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return convertValue; }, cancelationToken)
                    .CatchCancelation(() => { onCancel(); return convertValue; });
                onDirectCallbackAddedConvert(ref p62);
                onCallbackAddedConvert(ref p62);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise<TConvert> p63 = default(Promise<TConvert>);
                p63 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(x => { onResolve(x); return convertValue; }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return promiseToPromiseConvert(p63); }, cancelationToken)
                    .CatchCancelation(() => { onCancel(); return convertValue; });
                onAdoptCallbackAddedConvert(ref p63, AdoptLocation.Reject);
                onCallbackAddedConvert(ref p63);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise<TConvert> p64 = default(Promise<TConvert>);
                p64 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(x => { onResolve(x); return convertValue; }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return promiseToPromiseConvert(p64); }, cancelationToken)
                    .CatchCancelation(() => { onCancel(); return convertValue; });
                onAdoptCallbackAddedConvert(ref p64, AdoptLocation.Reject);
                onCallbackAddedConvert(ref p64);
            }

            foreach (var p in GetTestablePromises(promise))
            {
                Promise p65 = default(Promise);
                p65 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(x => { onResolve(x); return promiseToPromise(p65); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return promiseToPromise(p65); }, cancelationToken)
                    .CatchCancelation(onCancel);
                onAdoptCallbackAdded(ref p65, AdoptLocation.Both);
                onCallbackAdded(ref p65);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise p66 = default(Promise);
                p66 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(x => { onResolve(x); return promiseToPromise(p66); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return promiseToPromise(p66); }, cancelationToken)
                    .CatchCancelation(onCancel);
                onAdoptCallbackAdded(ref p66, AdoptLocation.Both);
                onCallbackAdded(ref p66);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise p67 = default(Promise);
                p67 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(x => { onResolve(x); return promiseToPromise(p67); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); }, cancelationToken)
                    .CatchCancelation(onCancel);
                onAdoptCallbackAdded(ref p67, AdoptLocation.Resolve);
                onCallbackAdded(ref p67);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise p68 = default(Promise);
                p68 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(x => { onResolve(x); return promiseToPromise(p68); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); }, cancelationToken)
                    .CatchCancelation(onCancel);
                onAdoptCallbackAdded(ref p68, AdoptLocation.Resolve);
                onCallbackAdded(ref p68);
            }

            foreach (var p in GetTestablePromises(promise))
            {
                Promise<TConvert> p69 = default(Promise<TConvert>);
                p69 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(x => { onResolve(x); return promiseToPromiseConvert(p69); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return promiseToPromiseConvert(p69); }, cancelationToken)
                    .CatchCancelation(() => { onCancel(); return convertValue; });
                onAdoptCallbackAddedConvert(ref p69, AdoptLocation.Both);
                onCallbackAddedConvert(ref p69);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise<TConvert> p70 = default(Promise<TConvert>);
                p70 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(x => { onResolve(x); return promiseToPromiseConvert(p70); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return promiseToPromiseConvert(p70); }, cancelationToken)
                    .CatchCancelation(() => { onCancel(); return convertValue; });
                onAdoptCallbackAddedConvert(ref p70, AdoptLocation.Both);
                onCallbackAddedConvert(ref p70);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise<TConvert> p71 = default(Promise<TConvert>);
                p71 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(x => { onResolve(x); return promiseToPromiseConvert(p71); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return convertValue; }, cancelationToken)
                    .CatchCancelation(() => { onCancel(); return convertValue; });
                onAdoptCallbackAddedConvert(ref p71, AdoptLocation.Resolve);
                onCallbackAddedConvert(ref p71);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise<TConvert> p72 = default(Promise<TConvert>);
                p72 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .Then(x => { onResolve(x); return promiseToPromiseConvert(p72); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return convertValue; }, cancelationToken)
                    .CatchCancelation(() => { onCancel(); return convertValue; });
                onAdoptCallbackAddedConvert(ref p72, AdoptLocation.Resolve);
                onCallbackAddedConvert(ref p72);
            }

            promise.Forget();
        }

        public static void AddContinueCallbacks<TConvert, TCapture>(Promise promise,
            Promise.ContinueAction onContinue = null, TConvert convertValue = default(TConvert),
            Promise.ContinueAction<TCapture> onContinueCapture = null, TCapture captureValue = default(TCapture),
            Func<Promise, Promise> promiseToPromise = null, Func<Promise<TConvert>, Promise<TConvert>> promiseToPromiseConvert = null,
            TestAction<Promise> onCallbackAdded = null, TestAction<Promise<TConvert>> onCallbackAddedConvert = null,
            Action onCancel = null,
            TestAction<Promise> onAdoptCallbackAdded = null, TestAction<Promise<TConvert>> onAdoptCallbackAddedConvert = null,
            ConfigureAwaitType configureAwaitType = ConfigureAwaitType.None, bool configureAwaitForceAsync = false)
        {
            promise = promise.Preserve();
            promise.Catch(() => { }).Forget(); // Suppress any rejections from the preserved promise.

            AddContinueCallbacksWithCancelation(
                promise,
                onContinue, convertValue,
                onContinueCapture, captureValue,
                promiseToPromise, promiseToPromiseConvert,
                onCallbackAdded, onCallbackAddedConvert,
                default(CancelationToken), default(CancelationToken),
                onCancel,
                onAdoptCallbackAdded, onAdoptCallbackAddedConvert,
                configureAwaitType
            );

            CancelationSource cancelationSource = CancelationSource.New();
            AddContinueCallbacksWithCancelation(
                promise,
                onContinue, convertValue,
                onContinueCapture, captureValue,
                promiseToPromise, promiseToPromiseConvert,
                onCallbackAdded, onCallbackAddedConvert,
                cancelationSource.Token, cancelationSource.Token,
                onCancel,
                onAdoptCallbackAdded, onAdoptCallbackAddedConvert,
                configureAwaitType
            );
            cancelationSource.Dispose();

            promise.Forget();
        }

        public static void AddContinueCallbacksWithCancelation<TConvert, TCapture>(Promise promise,
            Promise.ContinueAction onContinue = null, TConvert convertValue = default(TConvert),
            Promise.ContinueAction<TCapture> onContinueCapture = null, TCapture captureValue = default(TCapture),
            Func<Promise, Promise> promiseToPromise = null, Func<Promise<TConvert>, Promise<TConvert>> promiseToPromiseConvert = null,
            TestAction<Promise> onCallbackAdded = null, TestAction<Promise<TConvert>> onCallbackAddedConvert = null,
            CancelationToken cancelationToken = default(CancelationToken), CancelationToken waitAsyncCancelationToken = default(CancelationToken),
            Action onCancel = null,
            TestAction<Promise> onAdoptCallbackAdded = null, TestAction<Promise<TConvert>> onAdoptCallbackAddedConvert = null,
            ConfigureAwaitType configureAwaitType = ConfigureAwaitType.None, bool configureAwaitForceAsync = false)
        {
            promise = promise.Preserve();
            promise.Catch(() => { }).Forget(); // Suppress any rejections from the preserved promise.

            // Add empty delegate so no need for null check.
            onContinue += _ => { };
            onContinueCapture += (_, __) => { };
            if (promiseToPromise == null)
            {
                promiseToPromise = _ => Promise.Resolved();
            }
            if (promiseToPromiseConvert == null)
            {
                promiseToPromiseConvert = _ => Promise.Resolved(convertValue);
            }
            if (onCallbackAdded == null)
            {
                onCallbackAdded = (ref Promise p) => p.Forget();
            }
            if (onCallbackAddedConvert == null)
            {
                onCallbackAddedConvert = (ref Promise<TConvert> p) => p.Forget();
            }
            if (onAdoptCallbackAdded == null)
            {
                onAdoptCallbackAdded = (ref Promise p) => { };
            }
            if (onAdoptCallbackAddedConvert == null)
            {
                onAdoptCallbackAddedConvert = (ref Promise<TConvert> p) => { };
            }
            onCancel += () => { };

            foreach (var p in GetTestablePromises(promise))
            {
                Promise p1 = default(Promise);
                p1 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .ContinueWith(r => { onContinue(r); }, cancelationToken)
                    .CatchCancelation(onCancel);
                onCallbackAdded(ref p1);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise<TConvert> p2 = default(Promise<TConvert>);
                p2 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .ContinueWith(r => { onContinue(r); return convertValue; }, cancelationToken)
                    .CatchCancelation(() => { onCancel(); return convertValue; });
                onCallbackAddedConvert(ref p2);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise p3 = default(Promise);
                p3 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .ContinueWith(r => { onContinue(r); return promiseToPromise(p3); }, cancelationToken)
                    .CatchCancelation(onCancel);
                onAdoptCallbackAdded(ref p3);
                onCallbackAdded(ref p3);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise<TConvert> p4 = default(Promise<TConvert>);
                p4 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .ContinueWith(r => { onContinue(r); return promiseToPromiseConvert(p4); }, cancelationToken)
                    .CatchCancelation(() => { onCancel(); return convertValue; });
                onAdoptCallbackAddedConvert(ref p4);
                onCallbackAddedConvert(ref p4);
            }

            foreach (var p in GetTestablePromises(promise))
            {
                Promise p5 = default(Promise);
                p5 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .ContinueWith(captureValue, (cv, r) => { onContinueCapture(cv, r); onContinue(r); }, cancelationToken)
                    .CatchCancelation(onCancel);
                onCallbackAdded(ref p5);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise<TConvert> p6 = default(Promise<TConvert>);
                p6 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .ContinueWith(captureValue, (cv, r) => { onContinueCapture(cv, r); onContinue(r); return convertValue; }, cancelationToken)
                    .CatchCancelation(() => { onCancel(); return convertValue; });
                onCallbackAddedConvert(ref p6);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise p7 = default(Promise);
                p7 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .ContinueWith(captureValue, (cv, r) => { onContinueCapture(cv, r); onContinue(r); return promiseToPromise(p7); }, cancelationToken)
                    .CatchCancelation(onCancel);
                onAdoptCallbackAdded(ref p7);
                onCallbackAdded(ref p7);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise<TConvert> p8 = default(Promise<TConvert>);
                p8 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .ContinueWith(captureValue, (cv, r) => { onContinueCapture(cv, r); onContinue(r); return promiseToPromiseConvert(p8); }, cancelationToken)
                    .CatchCancelation(() => { onCancel(); return convertValue; });
                onAdoptCallbackAddedConvert(ref p8);
                onCallbackAddedConvert(ref p8);
            }

            promise.Forget();
        }

        public static void AddContinueCallbacks<T, TConvert, TCapture>(Promise<T> promise,
            Promise<T>.ContinueAction onContinue = null, TConvert convertValue = default(TConvert),
            Promise<T>.ContinueAction<TCapture> onContinueCapture = null, TCapture captureValue = default(TCapture),
            Func<Promise, Promise> promiseToPromise = null, Func<Promise<TConvert>, Promise<TConvert>> promiseToPromiseConvert = null,
            TestAction<Promise> onCallbackAdded = null, TestAction<Promise<TConvert>> onCallbackAddedConvert = null,
            Action onCancel = null,
            TestAction<Promise> onAdoptCallbackAdded = null, TestAction<Promise<TConvert>> onAdoptCallbackAddedConvert = null,
            ConfigureAwaitType configureAwaitType = ConfigureAwaitType.None, bool configureAwaitForceAsync = false)
        {
            promise = promise.Preserve();
            promise.Catch(() => { }).Forget(); // Suppress any rejections from the preserved promise.

            AddContinueCallbacksWithCancelation(
                promise,
                onContinue, convertValue,
                onContinueCapture, captureValue,
                promiseToPromise, promiseToPromiseConvert,
                onCallbackAdded, onCallbackAddedConvert,
                default(CancelationToken), default(CancelationToken),
                onCancel,
                onAdoptCallbackAdded, onAdoptCallbackAddedConvert,
                configureAwaitType
            );

            CancelationSource cancelationSource = CancelationSource.New();
            AddContinueCallbacksWithCancelation(
                promise,
                onContinue, convertValue,
                onContinueCapture, captureValue,
                promiseToPromise, promiseToPromiseConvert,
                onCallbackAdded, onCallbackAddedConvert,
                cancelationSource.Token, cancelationSource.Token,
                onCancel,
                onAdoptCallbackAdded, onAdoptCallbackAddedConvert,
                configureAwaitType
            );
            cancelationSource.Dispose();

            promise.Forget();
        }

        public static void AddContinueCallbacksWithCancelation<T, TConvert, TCapture>(Promise<T> promise,
            Promise<T>.ContinueAction onContinue = null, TConvert convertValue = default(TConvert),
            Promise<T>.ContinueAction<TCapture> onContinueCapture = null, TCapture captureValue = default(TCapture),
            Func<Promise, Promise> promiseToPromise = null, Func<Promise<TConvert>, Promise<TConvert>> promiseToPromiseConvert = null,
            TestAction<Promise> onCallbackAdded = null, TestAction<Promise<TConvert>> onCallbackAddedConvert = null,
            CancelationToken cancelationToken = default(CancelationToken), CancelationToken waitAsyncCancelationToken = default(CancelationToken),
            Action onCancel = null,
            TestAction<Promise> onAdoptCallbackAdded = null, TestAction<Promise<TConvert>> onAdoptCallbackAddedConvert = null,
            ConfigureAwaitType configureAwaitType = ConfigureAwaitType.None, bool configureAwaitForceAsync = false)
        {
            promise = promise.Preserve();
            promise.Catch(() => { }).Forget(); // Suppress any rejections from the preserved promise.

            // Add empty delegate so no need for null check.
            onContinue += _ => { };
            onContinueCapture += (_, __) => { };
            if (promiseToPromise == null)
            {
                promiseToPromise = _ => Promise.Resolved();
            }
            if (promiseToPromiseConvert == null)
            {
                promiseToPromiseConvert = _ => Promise.Resolved(convertValue);
            }
            if (onCallbackAdded == null)
            {
                onCallbackAdded = (ref Promise p) => p.Forget();
            }
            if (onCallbackAddedConvert == null)
            {
                onCallbackAddedConvert = (ref Promise<TConvert> p) => p.Forget();
            }
            if (onAdoptCallbackAdded == null)
            {
                onAdoptCallbackAdded = (ref Promise p) => { };
            }
            if (onAdoptCallbackAddedConvert == null)
            {
                onAdoptCallbackAddedConvert = (ref Promise<TConvert> p) => { };
            }
            onCancel += () => { };

            foreach (var p in GetTestablePromises(promise))
            {
                Promise p1 = default(Promise);
                p1 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .ContinueWith(new Promise<T>.ContinueAction(r => { onContinue(r); }), cancelationToken)
                    .CatchCancelation(onCancel);
                onCallbackAdded(ref p1);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise<TConvert> p2 = default(Promise<TConvert>);
                p2 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .ContinueWith(new Promise<T>.ContinueFunc<TConvert>(r => { onContinue(r); return convertValue; }), cancelationToken)
                    .CatchCancelation(() => { onCancel(); return convertValue; });
                onCallbackAddedConvert(ref p2);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise p3 = default(Promise);
                p3 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .ContinueWith(new Promise<T>.ContinueFunc<Promise>(r => { onContinue(r); return promiseToPromise(p3); }), cancelationToken)
                    .CatchCancelation(onCancel);
                onAdoptCallbackAdded(ref p3);
                onCallbackAdded(ref p3);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise<TConvert> p4 = default(Promise<TConvert>);
                p4 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .ContinueWith(new Promise<T>.ContinueFunc<Promise<TConvert>>(r => { onContinue(r); return promiseToPromiseConvert(p4); }), cancelationToken)
                    .CatchCancelation(() => { onCancel(); return convertValue; });
                onAdoptCallbackAddedConvert(ref p4);
                onCallbackAddedConvert(ref p4);
            }

            foreach (var p in GetTestablePromises(promise))
            {
                Promise p5 = default(Promise);
                p5 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .ContinueWith(captureValue, new Promise<T>.ContinueAction<TCapture>((cv, r) => { onContinueCapture(cv, r); onContinue(r); }), cancelationToken)
                    .CatchCancelation(onCancel);
                onCallbackAdded(ref p5);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise<TConvert> p6 = default(Promise<TConvert>);
                p6 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .ContinueWith(captureValue, new Promise<T>.ContinueFunc<TCapture, TConvert>((cv, r) => { onContinueCapture(cv, r); onContinue(r); return convertValue; }), cancelationToken)
                    .CatchCancelation(() => { onCancel(); return convertValue; });
                onCallbackAddedConvert(ref p6);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise p7 = default(Promise);
                p7 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .ContinueWith(captureValue, new Promise<T>.ContinueFunc<TCapture, Promise>((cv, r) => { onContinueCapture(cv, r); onContinue(r); return promiseToPromise(p7); }), cancelationToken)
                    .CatchCancelation(onCancel);
                onAdoptCallbackAdded(ref p7);
                onCallbackAdded(ref p7);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise<TConvert> p8 = default(Promise<TConvert>);
                p8 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .ContinueWith(captureValue, new Promise<T>.ContinueFunc<TCapture, Promise<TConvert>>((cv, r) => { onContinueCapture(cv, r); onContinue(r); return promiseToPromiseConvert(p8); }), cancelationToken)
                    .CatchCancelation(() => { onCancel(); return convertValue; });
                onAdoptCallbackAddedConvert(ref p8);
                onCallbackAddedConvert(ref p8);
            }

            promise.Forget();
        }

        public static void AddCancelCallbacks<TCapture>(Promise promise,
            Action onCancel = null, Action<TCapture> onCancelCapture = null,
            TCapture captureValue = default(TCapture),
            Func<Promise, Promise> promiseToPromise = null,
            TestAction<Promise> onCallbackAdded = null,
            TestAction<Promise> onAdoptCallbackAdded = null,
            ConfigureAwaitType configureAwaitType = ConfigureAwaitType.None, bool configureAwaitForceAsync = false)
        {
            promise = promise.Preserve();
            promise.Catch(() => { }).Forget(); // Suppress any rejections from the preserved promise.

            AddCancelCallbacks(
                promise,
                default(CancelationToken),
                onCancel, onCancelCapture,
                captureValue,
                promiseToPromise,
                onCallbackAdded,
                onAdoptCallbackAdded,
                default(CancelationToken),
                configureAwaitType,
                configureAwaitForceAsync
            );

            CancelationSource cancelationSource = CancelationSource.New();
            AddCancelCallbacks(
                promise,
                cancelationSource.Token,
                onCancel, onCancelCapture,
                captureValue,
                promiseToPromise,
                onCallbackAdded,
                onAdoptCallbackAdded,
                cancelationSource.Token,
                configureAwaitType,
                configureAwaitForceAsync
            );
            cancelationSource.Dispose();

            promise.Forget();
        }

        public static void AddCancelCallbacks<TCapture>(Promise promise,
            CancelationToken cancelationToken,
            Action onCancel = null, Action<TCapture> onCancelCapture = null,
            TCapture captureValue = default(TCapture),
            Func<Promise, Promise> promiseToPromise = null,
            TestAction<Promise> onCallbackAdded = null,
            TestAction<Promise> onAdoptCallbackAdded = null,
            CancelationToken waitAsyncCancelationToken = default(CancelationToken),
            ConfigureAwaitType configureAwaitType = ConfigureAwaitType.None, bool configureAwaitForceAsync = false)
        {
            promise = promise.Preserve();
            promise.Catch(() => { }).Forget(); // Suppress any rejections from the preserved promise.

            // Add empty delegate so no need for null check.
            if (promiseToPromise == null)
            {
                promiseToPromise = _ => Promise.Resolved();
            }
            if (onCallbackAdded == null)
            {
                onCallbackAdded = (ref Promise p) => p.Forget();
            }
            if (onAdoptCallbackAdded == null)
            {
                onAdoptCallbackAdded = (ref Promise p) => { };
            }
            onCancel += () => { };
            onCancelCapture += cv => { };

            foreach (var p in GetTestablePromises(promise))
            {
                Promise p1 = default(Promise);
                p1 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, cancelationToken)
                    .CatchCancelation(() => { onCancel(); }, cancelationToken);
                onCallbackAdded(ref p1);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise p2 = default(Promise);
                p2 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .CatchCancelation(captureValue, cv => { onCancelCapture(cv); }, cancelationToken);
                onCallbackAdded(ref p2);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise p3 = default(Promise);
                p3 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .CatchCancelation(() => { onCancel(); return promiseToPromise(p3); }, cancelationToken);
                onAdoptCallbackAdded(ref p3);
                onCallbackAdded(ref p3);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise p3 = default(Promise);
                p3 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .CatchCancelation(captureValue, cv => { onCancelCapture(cv); return promiseToPromise(p3); }, cancelationToken);
                onAdoptCallbackAdded(ref p3);
                onCallbackAdded(ref p3);
            }

            promise.Forget();
        }

        public static void AddCancelCallbacks<T, TCapture>(Promise<T> promise, T TValue = default(T),
            Action onCancel = null, Action<TCapture> onCancelCapture = null,
            TCapture captureValue = default(TCapture),
            Func<Promise<T>, Promise<T>> promiseToPromise = null,
            TestAction<Promise<T>> onCallbackAdded = null,
            TestAction<Promise<T>> onAdoptCallbackAdded = null,
            ConfigureAwaitType configureAwaitType = ConfigureAwaitType.None, bool configureAwaitForceAsync = false)
        {
            promise = promise.Preserve();
            promise.Catch(() => { }).Forget(); // Suppress any rejections from the preserved promise.

            AddCancelCallbacks(
                promise,
                default(CancelationToken), TValue,
                onCancel, onCancelCapture,
                captureValue,
                promiseToPromise,
                onCallbackAdded,
                onAdoptCallbackAdded,
                default(CancelationToken),
                configureAwaitType,
                configureAwaitForceAsync
            );

            CancelationSource cancelationSource = CancelationSource.New();
            AddCancelCallbacks(
                promise,
                cancelationSource.Token, TValue,
                onCancel, onCancelCapture,
                captureValue,
                promiseToPromise,
                onCallbackAdded,
                onAdoptCallbackAdded,
                cancelationSource.Token,
                configureAwaitType,
                configureAwaitForceAsync
            );
            cancelationSource.Dispose();

            promise.Forget();
        }

        public static void AddCancelCallbacks<T, TCapture>(Promise<T> promise,
            CancelationToken cancelationToken, T TValue = default(T),
            Action onCancel = null, Action<TCapture> onCancelCapture = null,
            TCapture captureValue = default(TCapture),
            Func<Promise<T>, Promise<T>> promiseToPromise = null,
            TestAction<Promise<T>> onCallbackAdded = null,
            TestAction<Promise<T>> onAdoptCallbackAdded = null,
            CancelationToken waitAsyncCancelationToken = default(CancelationToken),
            ConfigureAwaitType configureAwaitType = ConfigureAwaitType.None, bool configureAwaitForceAsync = false)
        {
            promise = promise.Preserve();
            promise.Catch(() => { }).Forget(); // Suppress any rejections from the preserved promise.

            // Add empty delegate so no need for null check.
            if (promiseToPromise == null)
            {
                promiseToPromise = _ => Promise.Resolved(TValue);
            }
            if (onCallbackAdded == null)
            {
                onCallbackAdded = (ref Promise<T> p) => p.Forget();
            }
            if (onAdoptCallbackAdded == null)
            {
                onAdoptCallbackAdded = (ref Promise<T> p) => { };
            }
            onCancel += () => { };
            onCancelCapture += cv => { };

            foreach (var p in GetTestablePromises(promise))
            {
                Promise<T> p1 = default(Promise<T>);
                p1 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .CatchCancelation(() => { onCancel(); return TValue; }, cancelationToken);
                onCallbackAdded(ref p1);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise<T> p2 = default(Promise<T>);
                p2 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .CatchCancelation(captureValue, cv => { onCancelCapture(cv); return TValue; }, cancelationToken);
                onCallbackAdded(ref p2);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise<T> p3 = default(Promise<T>);
                p3 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .CatchCancelation(() => { onCancel(); return promiseToPromise(p3); }, cancelationToken);
                onAdoptCallbackAdded(ref p3);
                onCallbackAdded(ref p3);
            }
            foreach (var p in GetTestablePromises(promise))
            {
                Promise<T> p3 = default(Promise<T>);
                p3 = p.ConfigureAwait(configureAwaitType, configureAwaitForceAsync, waitAsyncCancelationToken)
                    .CatchCancelation(captureValue, cv => { onCancelCapture(cv); return promiseToPromise(p3); }, cancelationToken);
                onAdoptCallbackAdded(ref p3);
                onCallbackAdded(ref p3);
            }

            promise.Forget();
        }

        public static Action<Promise>[] ResolveActionsVoid(Action onResolved = null)
        {
            onResolved += () => { };
            return new Action<Promise>[8]
            {
                promise => promise.Then(() => onResolved()).Forget(),
                promise => promise.Then(() => { onResolved(); return Promise.Resolved(); }).Forget(),
                promise => promise.Then(() => { onResolved(); return 1; }).Forget(),
                promise => promise.Then(() => { onResolved(); return Promise.Resolved(1); }).Forget(),

                promise => promise.Then(1, cv => onResolved()).Forget(),
                promise => promise.Then(1, cv => { onResolved(); return Promise.Resolved(); }).Forget(),
                promise => promise.Then(1, cv => { onResolved(); return 1; }).Forget(),
                promise => promise.Then(1, cv => { onResolved(); return Promise.Resolved(1); }).Forget()
            };
        }

        public static Action<Promise<T>>[] ResolveActions<T>(Action<T> onResolved = null)
        {
            onResolved += v => { };
            return new Action<Promise<T>>[8]
            {
                promise => promise.Then(v => onResolved(v)).Forget(),
                promise => promise.Then(v => { onResolved(v); return Promise.Resolved(); }).Forget(),
                promise => promise.Then(v => { onResolved(v); return 1; }).Forget(),
                promise => promise.Then(v => { onResolved(v); return Promise.Resolved(1); }).Forget(),

                promise => promise.Then(1, (cv, v) => onResolved(v)).Forget(),
                promise => promise.Then(1, (cv, v) => { onResolved(v); return Promise.Resolved(); }).Forget(),
                promise => promise.Then(1, (cv, v) => { onResolved(v); return 1; }).Forget(),
                promise => promise.Then(1, (cv, v) => { onResolved(v); return Promise.Resolved(1); }).Forget(),
            };
        }

        public static Action<Promise>[] ThenActionsVoid(Action onResolved = null, Action onRejected = null)
        {
            onResolved += () => { };
            onRejected += () => { };
            return new Action<Promise>[64]
            {
                promise => promise.Then(() => onResolved(), () => onRejected()).Forget(),
                promise => promise.Then(() => onResolved(), (object o) => onRejected()).Forget(),
                promise => promise.Then(() => { onResolved(); return Promise.Resolved(); }, () => onRejected()).Forget(),
                promise => promise.Then(() => { onResolved(); return Promise.Resolved(); }, (object o) => onRejected()).Forget(),
                promise => promise.Then(() => { onResolved(); return 1; }, () => { onRejected(); return 1; }).Forget(),
                promise => promise.Then(() => { onResolved(); return 1; }, (object o) => { onRejected(); return 1; }).Forget(),
                promise => promise.Then(() => { onResolved(); return Promise.Resolved(1); }, () => { onRejected(); return 1; }).Forget(),
                promise => promise.Then(() => { onResolved(); return Promise.Resolved(1); }, (object o) => { onRejected(); return 1; }).Forget(),

                promise => promise.Then(() => onResolved(), () => { onRejected(); return Promise.Resolved(); }).Forget(),
                promise => promise.Then(() => onResolved(), (object o) => { onRejected(); return Promise.Resolved(); }).Forget(),
                promise => promise.Then(() => { onResolved(); return Promise.Resolved(); }, () => { onRejected(); return Promise.Resolved(); }).Forget(),
                promise => promise.Then(() => { onResolved(); return Promise.Resolved(); }, (object o) => { onRejected(); return Promise.Resolved(); }).Forget(),
                promise => promise.Then(() => { onResolved(); return 1; }, () => { onRejected(); return Promise.Resolved(1); }).Forget(),
                promise => promise.Then(() => { onResolved(); return 1; }, (object o) => { onRejected(); return Promise.Resolved(1); }).Forget(),
                promise => promise.Then(() => { onResolved(); return Promise.Resolved(1); }, () => { onRejected(); return Promise.Resolved(1); }).Forget(),
                promise => promise.Then(() => { onResolved(); return Promise.Resolved(1); }, (object o) => { onRejected(); return Promise.Resolved(1); }).Forget(),

                promise => promise.Then(1, cv => onResolved(), () => onRejected()).Forget(),
                promise => promise.Then(1, cv => onResolved(), (object o) => onRejected()).Forget(),
                promise => promise.Then(1, cv => { onResolved(); return Promise.Resolved(); }, () => onRejected()).Forget(),
                promise => promise.Then(1, cv => { onResolved(); return Promise.Resolved(); }, (object o) => onRejected()).Forget(),
                promise => promise.Then(1, cv => { onResolved(); return 1; }, () => { onRejected(); return 1; }).Forget(),
                promise => promise.Then(1, cv => { onResolved(); return 1; }, (object o) => { onRejected(); return 1; }).Forget(),
                promise => promise.Then(1, cv => { onResolved(); return Promise.Resolved(1); }, () => { onRejected(); return 1; }).Forget(),
                promise => promise.Then(1, cv => { onResolved(); return Promise.Resolved(1); }, (object o) => { onRejected(); return 1; }).Forget(),

                promise => promise.Then(1, cv => onResolved(), () => { onRejected(); return Promise.Resolved(); }).Forget(),
                promise => promise.Then(1, cv => onResolved(), (object o) => { onRejected(); return Promise.Resolved(); }).Forget(),
                promise => promise.Then(1, cv => { onResolved(); return Promise.Resolved(); }, () => { onRejected(); return Promise.Resolved(); }).Forget(),
                promise => promise.Then(1, cv => { onResolved(); return Promise.Resolved(); }, (object o) => { onRejected(); return Promise.Resolved(); }).Forget(),
                promise => promise.Then(1, cv => { onResolved(); return 1; }, () => { onRejected(); return Promise.Resolved(1); }).Forget(),
                promise => promise.Then(1, cv => { onResolved(); return 1; }, (object o) => { onRejected(); return Promise.Resolved(1); }).Forget(),
                promise => promise.Then(1, cv => { onResolved(); return Promise.Resolved(1); }, () => { onRejected(); return Promise.Resolved(1); }).Forget(),
                promise => promise.Then(1, cv => { onResolved(); return Promise.Resolved(1); }, (object o) => { onRejected(); return Promise.Resolved(1); }).Forget(),

                promise => promise.Then(() => onResolved(), 1, cv => onRejected()).Forget(),
                promise => promise.Then(() => onResolved(), 1, (int cv, object o) => onRejected()).Forget(),
                promise => promise.Then(() => { onResolved(); return Promise.Resolved(); }, 1, cv => onRejected()).Forget(),
                promise => promise.Then(() => { onResolved(); return Promise.Resolved(); }, 1, (int cv, object o) => onRejected()).Forget(),
                promise => promise.Then(() => { onResolved(); return 1; }, 1, cv => { onRejected(); return 1; }).Forget(),
                promise => promise.Then(() => { onResolved(); return 1; }, 1, (int cv, object o) => { onRejected(); return 1; }).Forget(),
                promise => promise.Then(() => { onResolved(); return Promise.Resolved(1); }, 1, cv => { onRejected(); return 1; }).Forget(),
                promise => promise.Then(() => { onResolved(); return Promise.Resolved(1); }, 1, (int cv, object o) => { onRejected(); return 1; }).Forget(),

                promise => promise.Then(() => onResolved(), 1, cv => { onRejected(); return Promise.Resolved(); }).Forget(),
                promise => promise.Then(() => onResolved(), 1, (int cv, object o) => { onRejected(); return Promise.Resolved(); }).Forget(),
                promise => promise.Then(() => { onResolved(); return Promise.Resolved(); }, 1, cv => { onRejected(); return Promise.Resolved(); }).Forget(),
                promise => promise.Then(() => { onResolved(); return Promise.Resolved(); }, 1, (int cv, object o) => { onRejected(); return Promise.Resolved(); }).Forget(),
                promise => promise.Then(() => { onResolved(); return 1; }, 1, cv => { onRejected(); return Promise.Resolved(1); }).Forget(),
                promise => promise.Then(() => { onResolved(); return 1; }, 1, (int cv, object o) => { onRejected(); return Promise.Resolved(1); }).Forget(),
                promise => promise.Then(() => { onResolved(); return Promise.Resolved(1); }, 1, cv => { onRejected(); return Promise.Resolved(1); }).Forget(),
                promise => promise.Then(() => { onResolved(); return Promise.Resolved(1); }, 1, (int cv, object o) => { onRejected(); return Promise.Resolved(1); }).Forget(),

                promise => promise.Then(1, cv => onResolved(), 1, cv => onRejected()).Forget(),
                promise => promise.Then(1, cv => onResolved(), 1, (int cv, object o) => onRejected()).Forget(),
                promise => promise.Then(1, cv => { onResolved(); return Promise.Resolved(); }, 1, cv => onRejected()).Forget(),
                promise => promise.Then(1, cv => { onResolved(); return Promise.Resolved(); }, 1, (int cv, object o) => onRejected()).Forget(),
                promise => promise.Then(1, cv => { onResolved(); return 1; }, 1, cv => { onRejected(); return 1; }).Forget(),
                promise => promise.Then(1, cv => { onResolved(); return 1; }, 1, (int cv, object o) => { onRejected(); return 1; }).Forget(),
                promise => promise.Then(1, cv => { onResolved(); return Promise.Resolved(1); }, 1, cv => { onRejected(); return 1; }).Forget(),
                promise => promise.Then(1, cv => { onResolved(); return Promise.Resolved(1); }, 1, (int cv, object o) => { onRejected(); return 1; }).Forget(),

                promise => promise.Then(1, cv => onResolved(), 1, cv => { onRejected(); return Promise.Resolved(); }).Forget(),
                promise => promise.Then(1, cv => onResolved(), 1, (int cv, object o) => { onRejected(); return Promise.Resolved(); }).Forget(),
                promise => promise.Then(1, cv => { onResolved(); return Promise.Resolved(); }, 1, cv => { onRejected(); return Promise.Resolved(); }).Forget(),
                promise => promise.Then(1, cv => { onResolved(); return Promise.Resolved(); }, 1, (int cv, object o) => { onRejected(); return Promise.Resolved(); }).Forget(),
                promise => promise.Then(1, cv => { onResolved(); return 1; }, 1, cv => { onRejected(); return Promise.Resolved(1); }).Forget(),
                promise => promise.Then(1, cv => { onResolved(); return 1; }, 1, (int cv, object o) => { onRejected(); return Promise.Resolved(1); }).Forget(),
                promise => promise.Then(1, cv => { onResolved(); return Promise.Resolved(1); }, 1, cv => { onRejected(); return Promise.Resolved(1); }).Forget(),
                promise => promise.Then(1, cv => { onResolved(); return Promise.Resolved(1); }, 1, (int cv, object o) => { onRejected(); return Promise.Resolved(1); }).Forget(),
            };
        }

        public static Action<Promise<T>>[] ThenActions<T>(Action<T> onResolved = null, Action onRejected = null)
        {
            onResolved += v => { };
            onRejected += () => { };
            return new Action<Promise<T>>[64]
            {
                promise => promise.Then(v => onResolved(v), () => onRejected()).Forget(),
                promise => promise.Then(v => onResolved(v), (object o) => onRejected()).Forget(),
                promise => promise.Then(v => { onResolved(v); return Promise.Resolved(); }, () => onRejected()).Forget(),
                promise => promise.Then(v => { onResolved(v); return Promise.Resolved(); }, (object o) => onRejected()).Forget(),
                promise => promise.Then(v => { onResolved(v); return 1; }, () => { onRejected(); return 1; }).Forget(),
                promise => promise.Then(v => { onResolved(v); return 1; }, (object o) => { onRejected(); return 1; }).Forget(),
                promise => promise.Then(v => { onResolved(v); return Promise.Resolved(1); }, () => { onRejected(); return 1; }).Forget(),
                promise => promise.Then(v => { onResolved(v); return Promise.Resolved(1); }, (object o) => { onRejected(); return 1; }).Forget(),

                promise => promise.Then(v => onResolved(v), () => { onRejected(); return Promise.Resolved(); }).Forget(),
                promise => promise.Then(v => onResolved(v), (object o) => { onRejected(); return Promise.Resolved(); }).Forget(),
                promise => promise.Then(v => { onResolved(v); return Promise.Resolved(); }, () => { onRejected(); return Promise.Resolved(); }).Forget(),
                promise => promise.Then(v => { onResolved(v); return Promise.Resolved(); }, (object o) => { onRejected(); return Promise.Resolved(); }).Forget(),
                promise => promise.Then(v => { onResolved(v); return 1; }, () => { onRejected(); return Promise.Resolved(1); }).Forget(),
                promise => promise.Then(v => { onResolved(v); return 1; }, (object o) => { onRejected(); return Promise.Resolved(1); }).Forget(),
                promise => promise.Then(v => { onResolved(v); return Promise.Resolved(1); }, () => { onRejected(); return Promise.Resolved(1); }).Forget(),
                promise => promise.Then(v => { onResolved(v); return Promise.Resolved(1); }, (object o) => { onRejected(); return Promise.Resolved(1); }).Forget(),

                promise => promise.Then(1, (cv, v) => onResolved(v), () => onRejected()).Forget(),
                promise => promise.Then(1, (cv, v) => onResolved(v), (object o) => onRejected()).Forget(),
                promise => promise.Then(1, (cv, v) => { onResolved(v); return Promise.Resolved(); }, () => onRejected()).Forget(),
                promise => promise.Then(1, (cv, v) => { onResolved(v); return Promise.Resolved(); }, (object o) => onRejected()).Forget(),
                promise => promise.Then(1, (cv, v) => { onResolved(v); return 1; }, () => { onRejected(); return 1; }).Forget(),
                promise => promise.Then(1, (cv, v) => { onResolved(v); return 1; }, (object o) => { onRejected(); return 1; }).Forget(),
                promise => promise.Then(1, (cv, v) => { onResolved(v); return Promise.Resolved(1); }, () => { onRejected(); return 1; }).Forget(),
                promise => promise.Then(1, (cv, v) => { onResolved(v); return Promise.Resolved(1); }, (object o) => { onRejected(); return 1; }).Forget(),

                promise => promise.Then(1, (cv, v) => onResolved(v), () => { onRejected(); return Promise.Resolved(); }).Forget(),
                promise => promise.Then(1, (cv, v) => onResolved(v), (object o) => { onRejected(); return Promise.Resolved(); }).Forget(),
                promise => promise.Then(1, (cv, v) => { onResolved(v); return Promise.Resolved(); }, () => { onRejected(); return Promise.Resolved(); }).Forget(),
                promise => promise.Then(1, (cv, v) => { onResolved(v); return Promise.Resolved(); }, (object o) => { onRejected(); return Promise.Resolved(); }).Forget(),
                promise => promise.Then(1, (cv, v) => { onResolved(v); return 1; }, () => { onRejected(); return Promise.Resolved(1); }).Forget(),
                promise => promise.Then(1, (cv, v) => { onResolved(v); return 1; }, (object o) => { onRejected(); return Promise.Resolved(1); }).Forget(),
                promise => promise.Then(1, (cv, v) => { onResolved(v); return Promise.Resolved(1); }, () => { onRejected(); return Promise.Resolved(1); }).Forget(),
                promise => promise.Then(1, (cv, v) => { onResolved(v); return Promise.Resolved(1); }, (object o) => { onRejected(); return Promise.Resolved(1); }).Forget(),

                promise => promise.Then(v => onResolved(v), 1, cv => onRejected()).Forget(),
                promise => promise.Then(v => onResolved(v), 1, (int cv, object o) => onRejected()).Forget(),
                promise => promise.Then(v => { onResolved(v); return Promise.Resolved(); }, 1, cv => onRejected()).Forget(),
                promise => promise.Then(v => { onResolved(v); return Promise.Resolved(); }, 1, (int cv, object o) => onRejected()).Forget(),
                promise => promise.Then(v => { onResolved(v); return 1; }, 1, cv => { onRejected(); return 1; }).Forget(),
                promise => promise.Then(v => { onResolved(v); return 1; }, 1, (int cv, object o) => { onRejected(); return 1; }).Forget(),
                promise => promise.Then(v => { onResolved(v); return Promise.Resolved(1); }, 1, cv => { onRejected(); return 1; }).Forget(),
                promise => promise.Then(v => { onResolved(v); return Promise.Resolved(1); }, 1, (int cv, object o) => { onRejected(); return 1; }).Forget(),

                promise => promise.Then(v => onResolved(v), 1, cv => { onRejected(); return Promise.Resolved(); }).Forget(),
                promise => promise.Then(v => onResolved(v), 1, (int cv, object o) => { onRejected(); return Promise.Resolved(); }).Forget(),
                promise => promise.Then(v => { onResolved(v); return Promise.Resolved(); }, 1, cv => { onRejected(); return Promise.Resolved(); }).Forget(),
                promise => promise.Then(v => { onResolved(v); return Promise.Resolved(); }, 1, (int cv, object o) => { onRejected(); return Promise.Resolved(); }).Forget(),
                promise => promise.Then(v => { onResolved(v); return 1; }, 1, cv => { onRejected(); return Promise.Resolved(1); }).Forget(),
                promise => promise.Then(v => { onResolved(v); return 1; }, 1, (int cv, object o) => { onRejected(); return Promise.Resolved(1); }).Forget(),
                promise => promise.Then(v => { onResolved(v); return Promise.Resolved(1); }, 1, cv => { onRejected(); return Promise.Resolved(1); }).Forget(),
                promise => promise.Then(v => { onResolved(v); return Promise.Resolved(1); }, 1, (int cv, object o) => { onRejected(); return Promise.Resolved(1); }).Forget(),

                promise => promise.Then(1, (cv, v) => onResolved(v), 1, cv => onRejected()).Forget(),
                promise => promise.Then(1, (cv, v) => onResolved(v), 1, (int cv, object o) => onRejected()).Forget(),
                promise => promise.Then(1, (cv, v) => { onResolved(v); return Promise.Resolved(); }, 1, cv => onRejected()).Forget(),
                promise => promise.Then(1, (cv, v) => { onResolved(v); return Promise.Resolved(); }, 1, (int cv, object o) => onRejected()).Forget(),
                promise => promise.Then(1, (cv, v) => { onResolved(v); return 1; }, 1, cv => { onRejected(); return 1; }).Forget(),
                promise => promise.Then(1, (cv, v) => { onResolved(v); return 1; }, 1, (int cv, object o) => { onRejected(); return 1; }).Forget(),
                promise => promise.Then(1, (cv, v) => { onResolved(v); return Promise.Resolved(1); }, 1, cv => { onRejected(); return 1; }).Forget(),
                promise => promise.Then(1, (cv, v) => { onResolved(v); return Promise.Resolved(1); }, 1, (int cv, object o) => { onRejected(); return 1; }).Forget(),

                promise => promise.Then(1, (cv, v) => onResolved(v), 1, cv => { onRejected(); return Promise.Resolved(); }).Forget(),
                promise => promise.Then(1, (cv, v) => onResolved(v), 1, (int cv, object o) => { onRejected(); return Promise.Resolved(); }).Forget(),
                promise => promise.Then(1, (cv, v) => { onResolved(v); return Promise.Resolved(); }, 1, cv => { onRejected(); return Promise.Resolved(); }).Forget(),
                promise => promise.Then(1, (cv, v) => { onResolved(v); return Promise.Resolved(); }, 1, (int cv, object o) => { onRejected(); return Promise.Resolved(); }).Forget(),
                promise => promise.Then(1, (cv, v) => { onResolved(v); return 1; }, 1, cv => { onRejected(); return Promise.Resolved(1); }).Forget(),
                promise => promise.Then(1, (cv, v) => { onResolved(v); return 1; }, 1, (int cv, object o) => { onRejected(); return Promise.Resolved(1); }).Forget(),
                promise => promise.Then(1, (cv, v) => { onResolved(v); return Promise.Resolved(1); }, 1, cv => { onRejected(); return Promise.Resolved(1); }).Forget(),
                promise => promise.Then(1, (cv, v) => { onResolved(v); return Promise.Resolved(1); }, 1, (int cv, object o) => { onRejected(); return Promise.Resolved(1); }).Forget()
            };
        }

        public static Action<Promise>[] CatchActionsVoid(Action onRejected = null)
        {
            onRejected += () => { };
            return new Action<Promise>[8]
            {
                promise => promise.Catch(() => onRejected()).Forget(),
                promise => promise.Catch(() => { onRejected(); return Promise.Resolved(); }).Forget(),
                promise => promise.Catch((object o) => onRejected()).Forget(),
                promise => promise.Catch((object o) => { onRejected(); return Promise.Resolved(); }).Forget(),

                promise => promise.Catch(1, cv => onRejected()).Forget(),
                promise => promise.Catch(1, cv => { onRejected(); return Promise.Resolved(); }).Forget(),
                promise => promise.Catch(1, (int cv, object o) => onRejected()).Forget(),
                promise => promise.Catch(1, (int cv, object o) => { onRejected(); return Promise.Resolved(); }).Forget()
            };
        }

        public static Action<Promise<T>>[] CatchActions<T>(Action onRejected = null)
        {
            onRejected += () => { };
            return new Action<Promise<T>>[8]
            {
                promise => promise.Catch(() => { onRejected(); return default(T); }).Forget(),
                promise => promise.Catch(() => { onRejected(); return Promise.Resolved(default(T)); }).Forget(),
                promise => promise.Catch((object o) => { onRejected(); return default(T); }).Forget(),
                promise => promise.Catch((object o) => { onRejected(); return Promise.Resolved(default(T)); }).Forget(),

                promise => promise.Catch(1, cv => { onRejected(); return default(T); }).Forget(),
                promise => promise.Catch(1, cv => { onRejected(); return Promise.Resolved(default(T)); }).Forget(),
                promise => promise.Catch(1, (int cv, object o) => { onRejected(); return default(T); }).Forget(),
                promise => promise.Catch(1, (int cv, object o) => { onRejected(); return Promise.Resolved(default(T)); }).Forget(),
            };
        }

        public static Action<Promise>[] ContinueWithActionsVoid(Action onContinue = null)
        {
            onContinue += () => { };
            return new Action<Promise>[8]
            {
                promise => promise.ContinueWith(_ => onContinue()).Forget(),
                promise => promise.ContinueWith(_ => { onContinue(); return Promise.Resolved(); }).Forget(),
                promise => promise.ContinueWith(_ => { onContinue(); return 1; }).Forget(),
                promise => promise.ContinueWith(_ => { onContinue(); return Promise.Resolved(1); }).Forget(),

                promise => promise.ContinueWith(1, (int cv, Promise.ResultContainer _) => onContinue()).Forget(),
                promise => promise.ContinueWith(1, (int cv, Promise.ResultContainer _) => { onContinue(); return Promise.Resolved(); }).Forget(),
                promise => promise.ContinueWith(1, (Promise.ContinueFunc<int, int>)((cv, _) => { onContinue(); return 1; })).Forget(),
                promise => promise.ContinueWith(1, (Promise.ContinueFunc<int, Promise<int>>)((cv, _) => { onContinue(); return Promise.Resolved(1); })).Forget()
            };
        }

        public static Action<Promise<T>>[] ContinueWithActions<T>(Action onContinue = null)
        {
            onContinue += () => { };
            return new Action<Promise<T>>[8]
            {
                promise => promise.ContinueWith(_ => onContinue()).Forget(),
                promise => promise.ContinueWith(_ => { onContinue(); return Promise.Resolved(); }).Forget(),
                promise => promise.ContinueWith(_ => { onContinue(); return 1; }).Forget(),
                promise => promise.ContinueWith(_ => { onContinue(); return Promise.Resolved(1); }).Forget(),

                promise => promise.ContinueWith(1, (int cv, Promise<T>.ResultContainer _) => onContinue()).Forget(),
                promise => promise.ContinueWith(1, (int cv, Promise<T>.ResultContainer _) => { onContinue(); return Promise.Resolved(); }).Forget(),
                promise => promise.ContinueWith(1, (Promise<T>.ContinueFunc<int, int>)((cv, _) => { onContinue(); return 1; })).Forget(),
                promise => promise.ContinueWith(1, (Promise<T>.ContinueFunc<int, Promise<int>>)((cv, _) => { onContinue(); return Promise.Resolved(1); })).Forget()
            };
        }
        public static Func<Promise, CancelationToken, Promise>[] ResolveActionsVoidWithCancelation(Action onResolved = null)
        {
            onResolved += () => { };
            return new Func<Promise, CancelationToken, Promise>[8]
            {
                (promise, cancelationToken) => promise.Then(() => onResolved(), cancelationToken),
                (promise, cancelationToken) => promise.Then(() => { onResolved(); return Promise.Resolved(); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(() => { onResolved(); return 1; }, cancelationToken),
                (promise, cancelationToken) => promise.Then(() => { onResolved(); return Promise.Resolved(1); }, cancelationToken),

                (promise, cancelationToken) => promise.Then(1, cv => onResolved(), cancelationToken),
                (promise, cancelationToken) => promise.Then(1, cv => { onResolved(); return Promise.Resolved(); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(1, cv => { onResolved(); return 1; }, cancelationToken),
                (promise, cancelationToken) => promise.Then(1, cv => { onResolved(); return Promise.Resolved(1); }, cancelationToken)
            };
        }

        public static Func<Promise<T>, CancelationToken, Promise>[] ResolveActionsWithCancelation<T>(Action<T> onResolved = null)
        {
            onResolved += v => { };
            return new Func<Promise<T>, CancelationToken, Promise>[8]
            {
                (promise, cancelationToken) => promise.Then(v => onResolved(v), cancelationToken),
                (promise, cancelationToken) => promise.Then(v => { onResolved(v); return Promise.Resolved(); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(v => { onResolved(v); return 1; }, cancelationToken),
                (promise, cancelationToken) => promise.Then(v => { onResolved(v); return Promise.Resolved(1); }, cancelationToken),

                (promise, cancelationToken) => promise.Then(1, (cv, v) => onResolved(v), cancelationToken),
                (promise, cancelationToken) => promise.Then(1, (cv, v) => { onResolved(v); return Promise.Resolved(); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(1, (cv, v) => { onResolved(v); return 1; }, cancelationToken),
                (promise, cancelationToken) => promise.Then(1, (cv, v) => { onResolved(v); return Promise.Resolved(1); }, cancelationToken),
            };
        }

        public static Func<Promise, CancelationToken, Promise>[] ThenActionsVoidWithCancelation(Action onResolved = null, Action onRejected = null)
        {
            onResolved += () => { };
            onRejected += () => { };
            return new Func<Promise, CancelationToken, Promise>[64]
            {
                (promise, cancelationToken) => promise.Then(() => onResolved(), () => onRejected(), cancelationToken),
                (promise, cancelationToken) => promise.Then(() => onResolved(), (object o) => onRejected(), cancelationToken),
                (promise, cancelationToken) => promise.Then(() => { onResolved(); return Promise.Resolved(); }, () => onRejected(), cancelationToken),
                (promise, cancelationToken) => promise.Then(() => { onResolved(); return Promise.Resolved(); }, (object o) => onRejected(), cancelationToken),
                (promise, cancelationToken) => promise.Then(() => { onResolved(); return 1; }, () => { onRejected(); return 1; }, cancelationToken),
                (promise, cancelationToken) => promise.Then(() => { onResolved(); return 1; }, (object o) => { onRejected(); return 1; }, cancelationToken),
                (promise, cancelationToken) => promise.Then(() => { onResolved(); return Promise.Resolved(1); }, () => { onRejected(); return 1; }, cancelationToken),
                (promise, cancelationToken) => promise.Then(() => { onResolved(); return Promise.Resolved(1); }, (object o) => { onRejected(); return 1; }, cancelationToken),

                (promise, cancelationToken) => promise.Then(() => onResolved(), () => { onRejected(); return Promise.Resolved(); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(() => onResolved(), (object o) => { onRejected(); return Promise.Resolved(); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(() => { onResolved(); return Promise.Resolved(); }, () => { onRejected(); return Promise.Resolved(); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(() => { onResolved(); return Promise.Resolved(); }, (object o) => { onRejected(); return Promise.Resolved(); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(() => { onResolved(); return 1; }, () => { onRejected(); return Promise.Resolved(1); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(() => { onResolved(); return 1; }, (object o) => { onRejected(); return Promise.Resolved(1); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(() => { onResolved(); return Promise.Resolved(1); }, () => { onRejected(); return Promise.Resolved(1); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(() => { onResolved(); return Promise.Resolved(1); }, (object o) => { onRejected(); return Promise.Resolved(1); }, cancelationToken),

                (promise, cancelationToken) => promise.Then(1, cv => onResolved(), () => onRejected(), cancelationToken),
                (promise, cancelationToken) => promise.Then(1, cv => onResolved(), (object o) => onRejected(), cancelationToken),
                (promise, cancelationToken) => promise.Then(1, cv => { onResolved(); return Promise.Resolved(); }, () => onRejected(), cancelationToken),
                (promise, cancelationToken) => promise.Then(1, cv => { onResolved(); return Promise.Resolved(); }, (object o) => onRejected(), cancelationToken),
                (promise, cancelationToken) => promise.Then(1, cv => { onResolved(); return 1; }, () => { onRejected(); return 1; }, cancelationToken),
                (promise, cancelationToken) => promise.Then(1, cv => { onResolved(); return 1; }, (object o) => { onRejected(); return 1; }, cancelationToken),
                (promise, cancelationToken) => promise.Then(1, cv => { onResolved(); return Promise.Resolved(1); }, () => { onRejected(); return 1; }, cancelationToken),
                (promise, cancelationToken) => promise.Then(1, cv => { onResolved(); return Promise.Resolved(1); }, (object o) => { onRejected(); return 1; }, cancelationToken),

                (promise, cancelationToken) => promise.Then(1, cv => onResolved(), () => { onRejected(); return Promise.Resolved(); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(1, cv => onResolved(), (object o) => { onRejected(); return Promise.Resolved(); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(1, cv => { onResolved(); return Promise.Resolved(); }, () => { onRejected(); return Promise.Resolved(); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(1, cv => { onResolved(); return Promise.Resolved(); }, (object o) => { onRejected(); return Promise.Resolved(); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(1, cv => { onResolved(); return 1; }, () => { onRejected(); return Promise.Resolved(1); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(1, cv => { onResolved(); return 1; }, (object o) => { onRejected(); return Promise.Resolved(1); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(1, cv => { onResolved(); return Promise.Resolved(1); }, () => { onRejected(); return Promise.Resolved(1); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(1, cv => { onResolved(); return Promise.Resolved(1); }, (object o) => { onRejected(); return Promise.Resolved(1); }, cancelationToken),

                (promise, cancelationToken) => promise.Then(() => onResolved(), 1, cv => onRejected(), cancelationToken),
                (promise, cancelationToken) => promise.Then(() => onResolved(), 1, (int cv, object o) => onRejected(), cancelationToken),
                (promise, cancelationToken) => promise.Then(() => { onResolved(); return Promise.Resolved(); }, 1, cv => onRejected(), cancelationToken),
                (promise, cancelationToken) => promise.Then(() => { onResolved(); return Promise.Resolved(); }, 1, (int cv, object o) => onRejected(), cancelationToken),
                (promise, cancelationToken) => promise.Then(() => { onResolved(); return 1; }, 1, cv => { onRejected(); return 1; }, cancelationToken),
                (promise, cancelationToken) => promise.Then(() => { onResolved(); return 1; }, 1, (int cv, object o) => { onRejected(); return 1; }, cancelationToken),
                (promise, cancelationToken) => promise.Then(() => { onResolved(); return Promise.Resolved(1); }, 1, cv => { onRejected(); return 1; }, cancelationToken),
                (promise, cancelationToken) => promise.Then(() => { onResolved(); return Promise.Resolved(1); }, 1, (int cv, object o) => { onRejected(); return 1; }, cancelationToken),

                (promise, cancelationToken) => promise.Then(() => onResolved(), 1, cv => { onRejected(); return Promise.Resolved(); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(() => onResolved(), 1, (int cv, object o) => { onRejected(); return Promise.Resolved(); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(() => { onResolved(); return Promise.Resolved(); }, 1, cv => { onRejected(); return Promise.Resolved(); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(() => { onResolved(); return Promise.Resolved(); }, 1, (int cv, object o) => { onRejected(); return Promise.Resolved(); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(() => { onResolved(); return 1; }, 1, cv => { onRejected(); return Promise.Resolved(1); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(() => { onResolved(); return 1; }, 1, (int cv, object o) => { onRejected(); return Promise.Resolved(1); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(() => { onResolved(); return Promise.Resolved(1); }, 1, cv => { onRejected(); return Promise.Resolved(1); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(() => { onResolved(); return Promise.Resolved(1); }, 1, (int cv, object o) => { onRejected(); return Promise.Resolved(1); }, cancelationToken),

                (promise, cancelationToken) => promise.Then(1, cv => onResolved(), 1, cv => onRejected(), cancelationToken),
                (promise, cancelationToken) => promise.Then(1, cv => onResolved(), 1, (int cv, object o) => onRejected(), cancelationToken),
                (promise, cancelationToken) => promise.Then(1, cv => { onResolved(); return Promise.Resolved(); }, 1, cv => onRejected(), cancelationToken),
                (promise, cancelationToken) => promise.Then(1, cv => { onResolved(); return Promise.Resolved(); }, 1, (int cv, object o) => onRejected(), cancelationToken),
                (promise, cancelationToken) => promise.Then(1, cv => { onResolved(); return 1; }, 1, cv => { onRejected(); return 1; }, cancelationToken),
                (promise, cancelationToken) => promise.Then(1, cv => { onResolved(); return 1; }, 1, (int cv, object o) => { onRejected(); return 1; }, cancelationToken),
                (promise, cancelationToken) => promise.Then(1, cv => { onResolved(); return Promise.Resolved(1); }, 1, cv => { onRejected(); return 1; }, cancelationToken),
                (promise, cancelationToken) => promise.Then(1, cv => { onResolved(); return Promise.Resolved(1); }, 1, (int cv, object o) => { onRejected(); return 1; }, cancelationToken),

                (promise, cancelationToken) => promise.Then(1, cv => onResolved(), 1, cv => { onRejected(); return Promise.Resolved(); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(1, cv => onResolved(), 1, (int cv, object o) => { onRejected(); return Promise.Resolved(); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(1, cv => { onResolved(); return Promise.Resolved(); }, 1, cv => { onRejected(); return Promise.Resolved(); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(1, cv => { onResolved(); return Promise.Resolved(); }, 1, (int cv, object o) => { onRejected(); return Promise.Resolved(); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(1, cv => { onResolved(); return 1; }, 1, cv => { onRejected(); return Promise.Resolved(1); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(1, cv => { onResolved(); return 1; }, 1, (int cv, object o) => { onRejected(); return Promise.Resolved(1); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(1, cv => { onResolved(); return Promise.Resolved(1); }, 1, cv => { onRejected(); return Promise.Resolved(1); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(1, cv => { onResolved(); return Promise.Resolved(1); }, 1, (int cv, object o) => { onRejected(); return Promise.Resolved(1); }, cancelationToken),
            };
        }

        public static Func<Promise<T>, CancelationToken, Promise>[] ThenActionsWithCancelation<T>(Action<T> onResolved = null, Action onRejected = null)
        {
            onResolved += v => { };
            onRejected += () => { };
            return new Func<Promise<T>, CancelationToken, Promise>[64]
            {
                (promise, cancelationToken) => promise.Then(v => onResolved(v), () => onRejected(), cancelationToken),
                (promise, cancelationToken) => promise.Then(v => onResolved(v), (object o) => onRejected(), cancelationToken),
                (promise, cancelationToken) => promise.Then(v => { onResolved(v); return Promise.Resolved(); }, () => onRejected(), cancelationToken),
                (promise, cancelationToken) => promise.Then(v => { onResolved(v); return Promise.Resolved(); }, (object o) => onRejected(), cancelationToken),
                (promise, cancelationToken) => promise.Then(v => { onResolved(v); return 1; }, () => { onRejected(); return 1; }, cancelationToken),
                (promise, cancelationToken) => promise.Then(v => { onResolved(v); return 1; }, (object o) => { onRejected(); return 1; }, cancelationToken),
                (promise, cancelationToken) => promise.Then(v => { onResolved(v); return Promise.Resolved(1); }, () => { onRejected(); return 1; }, cancelationToken),
                (promise, cancelationToken) => promise.Then(v => { onResolved(v); return Promise.Resolved(1); }, (object o) => { onRejected(); return 1; }, cancelationToken),

                (promise, cancelationToken) => promise.Then(v => onResolved(v), () => { onRejected(); return Promise.Resolved(); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(v => onResolved(v), (object o) => { onRejected(); return Promise.Resolved(); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(v => { onResolved(v); return Promise.Resolved(); }, () => { onRejected(); return Promise.Resolved(); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(v => { onResolved(v); return Promise.Resolved(); }, (object o) => { onRejected(); return Promise.Resolved(); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(v => { onResolved(v); return 1; }, () => { onRejected(); return Promise.Resolved(1); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(v => { onResolved(v); return 1; }, (object o) => { onRejected(); return Promise.Resolved(1); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(v => { onResolved(v); return Promise.Resolved(1); }, () => { onRejected(); return Promise.Resolved(1); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(v => { onResolved(v); return Promise.Resolved(1); }, (object o) => { onRejected(); return Promise.Resolved(1); }, cancelationToken),

                (promise, cancelationToken) => promise.Then(1, (cv, v) => onResolved(v), () => onRejected(), cancelationToken),
                (promise, cancelationToken) => promise.Then(1, (cv, v) => onResolved(v), (object o) => onRejected(), cancelationToken),
                (promise, cancelationToken) => promise.Then(1, (cv, v) => { onResolved(v); return Promise.Resolved(); }, () => onRejected(), cancelationToken),
                (promise, cancelationToken) => promise.Then(1, (cv, v) => { onResolved(v); return Promise.Resolved(); }, (object o) => onRejected(), cancelationToken),
                (promise, cancelationToken) => promise.Then(1, (cv, v) => { onResolved(v); return 1; }, () => { onRejected(); return 1; }, cancelationToken),
                (promise, cancelationToken) => promise.Then(1, (cv, v) => { onResolved(v); return 1; }, (object o) => { onRejected(); return 1; }, cancelationToken),
                (promise, cancelationToken) => promise.Then(1, (cv, v) => { onResolved(v); return Promise.Resolved(1); }, () => { onRejected(); return 1; }, cancelationToken),
                (promise, cancelationToken) => promise.Then(1, (cv, v) => { onResolved(v); return Promise.Resolved(1); }, (object o) => { onRejected(); return 1; }, cancelationToken),

                (promise, cancelationToken) => promise.Then(1, (cv, v) => onResolved(v), () => { onRejected(); return Promise.Resolved(); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(1, (cv, v) => onResolved(v), (object o) => { onRejected(); return Promise.Resolved(); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(1, (cv, v) => { onResolved(v); return Promise.Resolved(); }, () => { onRejected(); return Promise.Resolved(); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(1, (cv, v) => { onResolved(v); return Promise.Resolved(); }, (object o) => { onRejected(); return Promise.Resolved(); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(1, (cv, v) => { onResolved(v); return 1; }, () => { onRejected(); return Promise.Resolved(1); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(1, (cv, v) => { onResolved(v); return 1; }, (object o) => { onRejected(); return Promise.Resolved(1); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(1, (cv, v) => { onResolved(v); return Promise.Resolved(1); }, () => { onRejected(); return Promise.Resolved(1); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(1, (cv, v) => { onResolved(v); return Promise.Resolved(1); }, (object o) => { onRejected(); return Promise.Resolved(1); }, cancelationToken),

                (promise, cancelationToken) => promise.Then(v => onResolved(v), 1, cv => onRejected(), cancelationToken),
                (promise, cancelationToken) => promise.Then(v => onResolved(v), 1, (int cv, object o) => onRejected(), cancelationToken),
                (promise, cancelationToken) => promise.Then(v => { onResolved(v); return Promise.Resolved(); }, 1, cv => onRejected(), cancelationToken),
                (promise, cancelationToken) => promise.Then(v => { onResolved(v); return Promise.Resolved(); }, 1, (int cv, object o) => onRejected(), cancelationToken),
                (promise, cancelationToken) => promise.Then(v => { onResolved(v); return 1; }, 1, cv => { onRejected(); return 1; }, cancelationToken),
                (promise, cancelationToken) => promise.Then(v => { onResolved(v); return 1; }, 1, (int cv, object o) => { onRejected(); return 1; }, cancelationToken),
                (promise, cancelationToken) => promise.Then(v => { onResolved(v); return Promise.Resolved(1); }, 1, cv => { onRejected(); return 1; }, cancelationToken),
                (promise, cancelationToken) => promise.Then(v => { onResolved(v); return Promise.Resolved(1); }, 1, (int cv, object o) => { onRejected(); return 1; }, cancelationToken),

                (promise, cancelationToken) => promise.Then(v => onResolved(v), 1, cv => { onRejected(); return Promise.Resolved(); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(v => onResolved(v), 1, (int cv, object o) => { onRejected(); return Promise.Resolved(); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(v => { onResolved(v); return Promise.Resolved(); }, 1, cv => { onRejected(); return Promise.Resolved(); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(v => { onResolved(v); return Promise.Resolved(); }, 1, (int cv, object o) => { onRejected(); return Promise.Resolved(); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(v => { onResolved(v); return 1; }, 1, cv => { onRejected(); return Promise.Resolved(1); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(v => { onResolved(v); return 1; }, 1, (int cv, object o) => { onRejected(); return Promise.Resolved(1); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(v => { onResolved(v); return Promise.Resolved(1); }, 1, cv => { onRejected(); return Promise.Resolved(1); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(v => { onResolved(v); return Promise.Resolved(1); }, 1, (int cv, object o) => { onRejected(); return Promise.Resolved(1); }, cancelationToken),

                (promise, cancelationToken) => promise.Then(1, (cv, v) => onResolved(v), 1, cv => onRejected(), cancelationToken),
                (promise, cancelationToken) => promise.Then(1, (cv, v) => onResolved(v), 1, (int cv, object o) => onRejected(), cancelationToken),
                (promise, cancelationToken) => promise.Then(1, (cv, v) => { onResolved(v); return Promise.Resolved(); }, 1, cv => onRejected(), cancelationToken),
                (promise, cancelationToken) => promise.Then(1, (cv, v) => { onResolved(v); return Promise.Resolved(); }, 1, (int cv, object o) => onRejected(), cancelationToken),
                (promise, cancelationToken) => promise.Then(1, (cv, v) => { onResolved(v); return 1; }, 1, cv => { onRejected(); return 1; }, cancelationToken),
                (promise, cancelationToken) => promise.Then(1, (cv, v) => { onResolved(v); return 1; }, 1, (int cv, object o) => { onRejected(); return 1; }, cancelationToken),
                (promise, cancelationToken) => promise.Then(1, (cv, v) => { onResolved(v); return Promise.Resolved(1); }, 1, cv => { onRejected(); return 1; }, cancelationToken),
                (promise, cancelationToken) => promise.Then(1, (cv, v) => { onResolved(v); return Promise.Resolved(1); }, 1, (int cv, object o) => { onRejected(); return 1; }, cancelationToken),

                (promise, cancelationToken) => promise.Then(1, (cv, v) => onResolved(v), 1, cv => { onRejected(); return Promise.Resolved(); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(1, (cv, v) => onResolved(v), 1, (int cv, object o) => { onRejected(); return Promise.Resolved(); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(1, (cv, v) => { onResolved(v); return Promise.Resolved(); }, 1, cv => { onRejected(); return Promise.Resolved(); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(1, (cv, v) => { onResolved(v); return Promise.Resolved(); }, 1, (int cv, object o) => { onRejected(); return Promise.Resolved(); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(1, (cv, v) => { onResolved(v); return 1; }, 1, cv => { onRejected(); return Promise.Resolved(1); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(1, (cv, v) => { onResolved(v); return 1; }, 1, (int cv, object o) => { onRejected(); return Promise.Resolved(1); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(1, (cv, v) => { onResolved(v); return Promise.Resolved(1); }, 1, cv => { onRejected(); return Promise.Resolved(1); }, cancelationToken),
                (promise, cancelationToken) => promise.Then(1, (cv, v) => { onResolved(v); return Promise.Resolved(1); }, 1, (int cv, object o) => { onRejected(); return Promise.Resolved(1); }, cancelationToken)
            };
        }

        public static Func<Promise, CancelationToken, Promise>[] CatchActionsVoidWithCancelation(Action onRejected = null)
        {
            onRejected += () => { };
            return new Func<Promise, CancelationToken, Promise>[8]
            {
                (promise, cancelationToken) => promise.Catch(() => onRejected(), cancelationToken),
                (promise, cancelationToken) => promise.Catch(() => { onRejected(); return Promise.Resolved(); }, cancelationToken),
                (promise, cancelationToken) => promise.Catch((object o) => onRejected(), cancelationToken),
                (promise, cancelationToken) => promise.Catch((object o) => { onRejected(); return Promise.Resolved(); }, cancelationToken),

                (promise, cancelationToken) => promise.Catch(1, cv => onRejected(), cancelationToken),
                (promise, cancelationToken) => promise.Catch(1, cv => { onRejected(); return Promise.Resolved(); }, cancelationToken),
                (promise, cancelationToken) => promise.Catch(1, (int cv, object o) => onRejected(), cancelationToken),
                (promise, cancelationToken) => promise.Catch(1, (int cv, object o) => { onRejected(); return Promise.Resolved(); }, cancelationToken)
            };
        }

        public static Func<Promise<T>, CancelationToken, Promise>[] CatchActionsWithCancelation<T>(Action onRejected = null)
        {
            onRejected += () => { };
            return new Func<Promise<T>, CancelationToken, Promise>[8]
            {
                (promise, cancelationToken) => promise.Catch(() => { onRejected(); return default(T); }, cancelationToken),
                (promise, cancelationToken) => promise.Catch(() => { onRejected(); return Promise.Resolved(default(T)); }, cancelationToken),
                (promise, cancelationToken) => promise.Catch((object o) => { onRejected(); return default(T); }, cancelationToken),
                (promise, cancelationToken) => promise.Catch((object o) => { onRejected(); return Promise.Resolved(default(T)); }, cancelationToken),

                (promise, cancelationToken) => promise.Catch(1, cv => { onRejected(); return default(T); }, cancelationToken),
                (promise, cancelationToken) => promise.Catch(1, cv => { onRejected(); return Promise.Resolved(default(T)); }, cancelationToken),
                (promise, cancelationToken) => promise.Catch(1, (int cv, object o) => { onRejected(); return default(T); }, cancelationToken),
                (promise, cancelationToken) => promise.Catch(1, (int cv, object o) => { onRejected(); return Promise.Resolved(default(T)); }, cancelationToken),
            };
        }

        public static Func<Promise, CancelationToken, Promise>[] ContinueWithActionsVoidWithCancelation(Action onContinue = null)
        {
            onContinue += () => { };
            return new Func<Promise, CancelationToken, Promise>[8]
            {
                (promise, cancelationToken) => promise.ContinueWith(_ => onContinue(), cancelationToken),
                (promise, cancelationToken) => promise.ContinueWith(_ => { onContinue(); return Promise.Resolved(); }, cancelationToken),
                (promise, cancelationToken) => promise.ContinueWith(_ => { onContinue(); return 1; }, cancelationToken),
                (promise, cancelationToken) => promise.ContinueWith(_ => { onContinue(); return Promise.Resolved(1); }, cancelationToken),

                (promise, cancelationToken) => promise.ContinueWith(1, (int cv, Promise.ResultContainer _) => onContinue(), cancelationToken),
                (promise, cancelationToken) => promise.ContinueWith(1, (int cv, Promise.ResultContainer _) => { onContinue(); return Promise.Resolved(); }, cancelationToken),
                (promise, cancelationToken) => promise.ContinueWith(1, new Promise.ContinueFunc<int, int>((cv, _) => { onContinue(); return 1; }), cancelationToken),
                (promise, cancelationToken) => promise.ContinueWith(1, new Promise.ContinueFunc<int, Promise<int>>((cv, _) => { onContinue(); return Promise.Resolved(1); }), cancelationToken)
            };
        }

        public static Func<Promise<T>, CancelationToken, Promise>[] ContinueWithActionsWithCancelation<T>(Action onContinue = null)
        {
            onContinue += () => { };
            return new Func<Promise<T>, CancelationToken, Promise>[8]
            {
                (promise, cancelationToken) => promise.ContinueWith(_ => onContinue(), cancelationToken),
                (promise, cancelationToken) => promise.ContinueWith(_ => { onContinue(); return Promise.Resolved(); }, cancelationToken),
                (promise, cancelationToken) => promise.ContinueWith(_ => { onContinue(); return 1; }, cancelationToken),
                (promise, cancelationToken) => promise.ContinueWith(_ => { onContinue(); return Promise.Resolved(1); }, cancelationToken),

                (promise, cancelationToken) => promise.ContinueWith(1, (int cv, Promise<T>.ResultContainer _) => onContinue(), cancelationToken),
                (promise, cancelationToken) => promise.ContinueWith(1, (int cv, Promise<T>.ResultContainer _) => { onContinue(); return Promise.Resolved(); }, cancelationToken),
                (promise, cancelationToken) => promise.ContinueWith(1, new Promise<T>.ContinueFunc<int, int>((cv, _) => { onContinue(); return 1; }), cancelationToken),
                (promise, cancelationToken) => promise.ContinueWith(1, new Promise<T>.ContinueFunc<int, Promise<int>>((cv, _) => { onContinue(); return Promise.Resolved(1); }), cancelationToken)
            };
        }

        public static Func<Promise>[] ActionsReturningPromiseVoid(Func<Promise> returnProvider)
        {
            return new Func<Promise>[]
            {
                () => Promise.Resolved().Then(() => returnProvider()),
                () => Promise.Resolved().Then(1, cv => returnProvider()),
                () => Promise.Resolved(1).Then(v => returnProvider()),
                () => Promise.Resolved(1).Then(1, (cv, v) => returnProvider()),


                () => Promise.Resolved().Then(() => returnProvider(), () => { }),
                () => Promise.Resolved().Then(() => returnProvider(), (int r) => { }),
                () => Promise.Resolved().Then(() => returnProvider(), () => returnProvider()),
                () => Promise.Resolved().Then(() => returnProvider(), (int r) => returnProvider()),
                () => Promise.Resolved(1).Then(v => returnProvider(), () => { }),
                () => Promise.Resolved(1).Then(v => returnProvider(), (int r) => { }),
                () => Promise.Resolved(1).Then(v => returnProvider(), () => returnProvider()),
                () => Promise.Resolved(1).Then(v => returnProvider(), (int r) => returnProvider()),

                () => Promise.Rejected(2).Then(() => { }, () => returnProvider()),
                () => Promise.Rejected(2).Then(() => { }, (int r) => returnProvider()),
                () => Promise.Rejected(2).Then(() => returnProvider(), () => returnProvider()),
                () => Promise.Rejected(2).Then(() => returnProvider(), (int r) => returnProvider()),

                () => Promise.Resolved().Then(1, cv => returnProvider(), () => { }),
                () => Promise.Resolved().Then(1, cv => returnProvider(), (int r) => { }),
                () => Promise.Resolved().Then(1, cv => returnProvider(), () => returnProvider()),
                () => Promise.Resolved().Then(1, cv => returnProvider(), (int r) => returnProvider()),
                () => Promise.Resolved(1).Then(1, (cv, v) => returnProvider(), () => { }),
                () => Promise.Resolved(1).Then(1, (cv, v) => returnProvider(), (int r) => { }),
                () => Promise.Resolved(1).Then(1, (cv, v) => returnProvider(), () => returnProvider()),
                () => Promise.Resolved(1).Then(1, (cv, v) => returnProvider(), (int r) => returnProvider()),

                () => Promise.Rejected(2).Then(1, cv => { }, () => returnProvider()),
                () => Promise.Rejected(2).Then(1, cv => { }, (int r) => returnProvider()),
                () => Promise.Rejected(2).Then(1, cv => returnProvider(), () => returnProvider()),
                () => Promise.Rejected(2).Then(1, cv => returnProvider(), (int r) => returnProvider()),

                () => Promise.Resolved().Then(() => returnProvider(), 1, cv => { }),
                () => Promise.Resolved().Then(() => returnProvider(), 1, (int cv, int r) => { }),
                () => Promise.Resolved().Then(() => returnProvider(), 1, cv => returnProvider()),
                () => Promise.Resolved().Then(() => returnProvider(), 1, (int cv, int r) => returnProvider()),
                () => Promise.Resolved(1).Then(v => returnProvider(), 1, cv => { }),
                () => Promise.Resolved(1).Then(v => returnProvider(), 1, (int cv, int r) => { }),
                () => Promise.Resolved(1).Then(v => returnProvider(), 1, cv => returnProvider()),
                () => Promise.Resolved(1).Then(v => returnProvider(), 1, (int cv, int r) => returnProvider()),

                () => Promise.Rejected(2).Then(() => { }, 1, cv => returnProvider()),
                () => Promise.Rejected(2).Then(() => { }, 1, (int cv, int r) => returnProvider()),
                () => Promise.Rejected(2).Then(() => returnProvider(), 1, cv => returnProvider()),
                () => Promise.Rejected(2).Then(() => returnProvider(), 1, (int cv, int r) => returnProvider()),

                () => Promise.Resolved().Then(1, cv => returnProvider(), 1, cv => { }),
                () => Promise.Resolved().Then(1, cv => returnProvider(), 1, (int cv, int r) => { }),
                () => Promise.Resolved().Then(1, cv => returnProvider(), 1, cv => returnProvider()),
                () => Promise.Resolved().Then(1, cv => returnProvider(), 1, (int cv, int r) => returnProvider()),
                () => Promise.Resolved(1).Then(1, (cv, v) => returnProvider(), 1, cv => { }),
                () => Promise.Resolved(1).Then(1, (cv, v) => returnProvider(), 1, (int cv, int r) => { }),
                () => Promise.Resolved(1).Then(1, (cv, v) => returnProvider(), 1, cv => returnProvider()),
                () => Promise.Resolved(1).Then(1, (cv, v) => returnProvider(), 1, (int cv, int r) => returnProvider()),

                () => Promise.Rejected(2).Then(1, cv => { }, 1, cv => returnProvider()),
                () => Promise.Rejected(2).Then(1, cv => { }, 1, (int cv, int r) => returnProvider()),
                () => Promise.Rejected(2).Then(1, cv => returnProvider(), 1, cv => returnProvider()),
                () => Promise.Rejected(2).Then(1, cv => returnProvider(), 1, (int cv, int r) => returnProvider()),


                () => Promise.Rejected(2).Catch(() => returnProvider()),
                () => Promise.Rejected(2).Catch((int r) => returnProvider()),
                () => Promise.Rejected(2).Catch(1, cv => returnProvider()),
                () => Promise.Rejected(2).Catch(1, (int cv, int r) => returnProvider()),


                () => Promise.Resolved().ContinueWith(_ => returnProvider()),
                () => Promise.Resolved().ContinueWith(1, (int cv, Promise.ResultContainer _) => returnProvider()),
                () => Promise.Resolved(1).ContinueWith(_ => returnProvider()),
                () => Promise.Resolved(1).ContinueWith(1, (int cv, Promise<int>.ResultContainer _) => returnProvider()),
            };
        }

        public static Func<Promise<T>>[] ActionsReturningPromiseT<T>(Func<Promise<T>> returnProvider)
        {
            return new Func<Promise<T>>[]
            {
                () => Promise.Resolved().Then(() => returnProvider()),
                () => Promise.Resolved().Then(1, cv => returnProvider()),
                () => Promise.Resolved(1).Then(v => returnProvider()),
                () => Promise.Resolved(1).Then(1, (cv, v) => returnProvider()),


                () => Promise.Resolved().Then(() => returnProvider(), () => default(T)),
                () => Promise.Resolved().Then(() => returnProvider(), (int r) => default(T)),
                () => Promise.Resolved().Then(() => returnProvider(), () => returnProvider()),
                () => Promise.Resolved().Then(() => returnProvider(), (int r) => returnProvider()),
                () => Promise.Resolved(1).Then(v => returnProvider(), () => default(T)),
                () => Promise.Resolved(1).Then(v => returnProvider(), (int r) => default(T)),
                () => Promise.Resolved(1).Then(v => returnProvider(), () => returnProvider()),
                () => Promise.Resolved(1).Then(v => returnProvider(), (int r) => returnProvider()),

                () => Promise.Rejected(2).Then(() => default(T), () => returnProvider()),
                () => Promise.Rejected(2).Then(() => default(T), (int r) => returnProvider()),
                () => Promise.Rejected(2).Then(() => returnProvider(), () => returnProvider()),
                () => Promise.Rejected(2).Then(() => returnProvider(), (int r) => returnProvider()),

                () => Promise.Resolved().Then(1, cv => returnProvider(), () => default(T)),
                () => Promise.Resolved().Then(1, cv => returnProvider(), (int r) => default(T)),
                () => Promise.Resolved().Then(1, cv => returnProvider(), () => returnProvider()),
                () => Promise.Resolved().Then(1, cv => returnProvider(), (int r) => returnProvider()),
                () => Promise.Resolved(1).Then(1, (cv, v) => returnProvider(), () => default(T)),
                () => Promise.Resolved(1).Then(1, (cv, v) => returnProvider(), (int r) => default(T)),
                () => Promise.Resolved(1).Then(1, (cv, v) => returnProvider(), () => returnProvider()),
                () => Promise.Resolved(1).Then(1, (cv, v) => returnProvider(), (int r) => returnProvider()),

                () => Promise.Rejected(2).Then(1, cv => default(T), () => returnProvider()),
                () => Promise.Rejected(2).Then(1, cv => default(T), (int r) => returnProvider()),
                () => Promise.Rejected(2).Then(1, cv => returnProvider(), () => returnProvider()),
                () => Promise.Rejected(2).Then(1, cv => returnProvider(), (int r) => returnProvider()),

                () => Promise.Resolved().Then(() => returnProvider(), 1, cv => default(T)),
                () => Promise.Resolved().Then(() => returnProvider(), 1, (int cv, int r) => default(T)),
                () => Promise.Resolved().Then(() => returnProvider(), 1, cv => returnProvider()),
                () => Promise.Resolved().Then(() => returnProvider(), 1, (int cv, int r) => returnProvider()),
                () => Promise.Resolved(1).Then(v => returnProvider(), 1, cv => default(T)),
                () => Promise.Resolved(1).Then(v => returnProvider(), 1, (int cv, int r) => default(T)),
                () => Promise.Resolved(1).Then(v => returnProvider(), 1, cv => returnProvider()),
                () => Promise.Resolved(1).Then(v => returnProvider(), 1, (int cv, int r) => returnProvider()),

                () => Promise.Rejected(2).Then(() => default(T), 1, cv => returnProvider()),
                () => Promise.Rejected(2).Then(() => default(T), 1, (int cv, int r) => returnProvider()),
                () => Promise.Rejected(2).Then(() => returnProvider(), 1, cv => returnProvider()),
                () => Promise.Rejected(2).Then(() => returnProvider(), 1, (int cv, int r) => returnProvider()),

                () => Promise.Resolved().Then(1, cv => returnProvider(), 1, cv => default(T)),
                () => Promise.Resolved().Then(1, cv => returnProvider(), 1, (int cv, int r) => default(T)),
                () => Promise.Resolved().Then(1, cv => returnProvider(), 1, cv => returnProvider()),
                () => Promise.Resolved().Then(1, cv => returnProvider(), 1, (int cv, int r) => returnProvider()),
                () => Promise.Resolved(1).Then(1, (cv, v) => returnProvider(), 1, cv => default(T)),
                () => Promise.Resolved(1).Then(1, (cv, v) => returnProvider(), 1, (int cv, int r) => default(T)),
                () => Promise.Resolved(1).Then(1, (cv, v) => returnProvider(), 1, cv => returnProvider()),
                () => Promise.Resolved(1).Then(1, (cv, v) => returnProvider(), 1, (int cv, int r) => returnProvider()),

                () => Promise.Rejected(2).Then(1, cv => default(T), 1, cv => returnProvider()),
                () => Promise.Rejected(2).Then(1, cv => default(T), 1, (int cv, int r) => returnProvider()),
                () => Promise.Rejected(2).Then(1, cv => returnProvider(), 1, cv => returnProvider()),
                () => Promise.Rejected(2).Then(1, cv => returnProvider(), 1, (int cv, int r) => returnProvider()),


                () => Promise<T>.Rejected(2).Catch(() => returnProvider()),
                () => Promise<T>.Rejected(2).Catch((int r) => returnProvider()),
                () => Promise<T>.Rejected(2).Catch(1, cv => returnProvider()),
                () => Promise<T>.Rejected(2).Catch(1, (int cv, int r) => returnProvider()),


                () => Promise.Resolved().ContinueWith(_ => returnProvider()),
                () => Promise.Resolved().ContinueWith(1, (Promise.ContinueFunc<int, Promise<T>>)((cv, _) => returnProvider())),
                () => Promise.Resolved(1).ContinueWith(_ => returnProvider()),
                () => Promise.Resolved(1).ContinueWith(1, (Promise<int>.ContinueFunc<int, Promise<T>>)((cv, _) => returnProvider())),
            };
        }
    }
}