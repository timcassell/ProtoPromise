#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0034 // Simplify 'default' expression

using NUnit.Framework;
using Proto.Promises;
using Proto.Promises.Threading;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace ProtoPromise.Tests
{
    public enum CompleteType : byte
    {
        // Explicit numbers for easy comparison with Promise.State
        Resolve = 1,
        Reject = 2,
        Cancel = 3
    }

    public enum SynchronizationType
    {
        Synchronous = 0,
        Foreground = 1,
#if !UNITY_WEBGL // WebGL doesn't support threads.
        Background = 2,
#endif
        CapturedContext = 3,
        Explicit = 4
    }

    public enum AdoptLocation
    {
        Resolve,
        Reject,
        Both
    }

    public enum CancelationType
    {
        None,
        Deferred,
        Immediate
    }

    public delegate void TestAction<T>(ref T value);
    public delegate void TestAction<T1, T2>(ref T1 value1, T2 value2);

    // These help test all Then/Catch/ContinueWith methods at once.
    public static class TestHelper
    {
        public const SynchronizationType backgroundType = (SynchronizationType) SynchronizationOption.Background;

        private static Thread _foregroundContextThread;
        public static PromiseSynchronizationContext _foregroundContext;
        public static BackgroundSynchronizationContext _backgroundContext;
        private static readonly List<Exception> _uncaughtExceptions = new List<Exception>();
        public static object s_expectedUncaughtRejectValue;

        private static Stopwatch _stopwatch;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Setup()
        {
            if (_foregroundContext == null)
            {
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                Internal.TrackObjectsForRelease();
#endif

                // Set the foreground context to execute foreground promise callbacks.
                _foregroundContextThread = Thread.CurrentThread;
                Promise.Config.ForegroundContext = _foregroundContext = new PromiseSynchronizationContext(_foregroundContextThread);
                // Used instead of ThreadPool, because ThreadPool has issues in old runtime, causing tests to fail.
                // This also allows us to wait for all background threads to complete for validation purposes.
                Promise.Config.BackgroundContext = _backgroundContext = new BackgroundSynchronizationContext();
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

            SynchronizationContext.SetSynchronizationContext(_foregroundContext);
            Promise.Manager.ThreadStaticSynchronizationContext = _foregroundContext;
            LogProgress("Begin");
        }

        public static void AssertRejection(object expected, object actual)
        {
#if ENABLE_IL2CPP && !UNITY_2022_1_OR_NEWER
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

#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
            Internal.AssertAllObjectsReleased();
#endif

            s_expectedUncaughtRejectValue = null;
            LogProgress("Success");
        }

        private static void LogProgress(string beginOrSuccess)
        {
            var message = $"{beginOrSuccess} time: {_stopwatch.Elapsed}, test: {TestContext.CurrentContext.Test.FullName}";
            // TestContext.Progress is not logged when running Unity tests, so we use Debug.LogFormat instead.
            // Debug.Log captures stacktrace which is too expensive for CI, and the option to log without capturing stacktrace was added in 2019.
#if UNITY_2019_1_OR_NEWER
            UnityEngine.Debug.LogFormat(UnityEngine.LogType.Log, UnityEngine.LogOption.NoStacktrace, null, message);
#else
            TestContext.Progress.WriteLine(message);
#endif
        }

        private static void WaitForAllThreadsToCompleteAndGcCollect()
        {
            _backgroundContext.WaitForAllThreadsToComplete();

            // Some tests execute in a separate thread (like tests with a [Timeout]), so we can't execute the context on that thread.
            if (_foregroundContextThread == Thread.CurrentThread)
            {
                ExecuteForegroundCallbacks();
            }
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

        public static Promise AssertThrowsAsync<TException>(Func<Promise> asyncAction, TException expected = default(TException)) where TException : Exception
        {
            return Promise.Run(asyncAction, SynchronizationOption.Synchronous)
                .ContinueWith(resultContainer =>
                {
                    Assert.AreEqual(Promise.State.Rejected, resultContainer.State);
                    if (expected != null)
                    {
                        AssertRejection(expected, resultContainer.Reason);
                    }
                    else
                    {
                        Assert.IsInstanceOf<TException>(resultContainer.Reason);
                    }
                });
        }

        public static Promise AssertCanceledAsync(Func<Promise> asyncAction)
        {
            return Promise.Run(asyncAction, SynchronizationOption.Synchronous)
                .ContinueWith(resultContainer =>
                {
                    resultContainer.RethrowIfRejected();
                    Assert.AreEqual(Promise.State.Canceled, resultContainer.State);
                });
        }

        public static float Lerp(float a, float b, float t)
        {
            return a + (b - a) * t;
        }

        public static Action<Promise.Deferred> GetCompleterVoid<TReject>(CompleteType completeType, TReject rejectValue)
        {
            switch (completeType)
            {
                case CompleteType.Resolve:
                    return deferred => deferred.Resolve();
                case CompleteType.Reject:
                    return deferred => deferred.Reject(rejectValue);
                case CompleteType.Cancel:
                    return deferred => deferred.Cancel();
            }
            throw new Exception();
        }

        public static Action<Promise<T>.Deferred> GetCompleterT<T, TReject>(CompleteType completeType, T value, TReject rejectValue)
        {
            switch (completeType)
            {
                case CompleteType.Resolve:
                    return deferred => deferred.Resolve(value);
                case CompleteType.Reject:
                    return deferred => deferred.Reject(rejectValue);
                case CompleteType.Cancel:
                    return deferred => deferred.Cancel();
            }
            throw new Exception();
        }

        public static Promise BuildPromise<TReject>(CompleteType completeType, bool isAlreadyComplete, TReject reason, out Action tryCompleter)
        {
            if (isAlreadyComplete)
            {
                tryCompleter = () => { };
                switch (completeType)
                {
                    case CompleteType.Resolve:
                        return Promise.Resolved();
                    case CompleteType.Reject:
                        return Promise.Rejected(reason);
                    case CompleteType.Cancel:
                        return Promise.Canceled();
                }
                throw new Exception();
            }

            var deferred = Promise.NewDeferred();
            Action completer;
            switch (completeType)
            {
                case CompleteType.Resolve:
                    completer = () => deferred.Resolve();
                    break;
                case CompleteType.Reject:
                    completer = () => deferred.Reject(reason);
                    break;
                case CompleteType.Cancel:
                    completer = () => deferred.Cancel();
                    break;
                default:
                    throw new Exception();
            }
            tryCompleter = () => Interlocked.Exchange(ref completer, null)?.Invoke();
            return deferred.Promise;
        }

        public static Promise<T> BuildPromise<T, TReject>(CompleteType completeType, bool isAlreadyComplete, T value, TReject reason, out Action tryCompleter)
        {
            if (isAlreadyComplete)
            {
                tryCompleter = () => { };
                switch (completeType)
                {
                    case CompleteType.Resolve:
                        return Promise.Resolved(value);
                    case CompleteType.Reject:
                        return Promise<T>.Rejected(reason);
                    case CompleteType.Cancel:
                        return Promise<T>.Canceled();
                }
                throw new Exception();
            }

            var deferred = Promise<T>.NewDeferred();
            Action completer;
            switch (completeType)
            {
                case CompleteType.Resolve:
                    completer = () => deferred.Resolve(value);
                    break;
                case CompleteType.Reject:
                    completer = () => deferred.Reject(reason);
                    break;
                case CompleteType.Cancel:
                    completer = () => deferred.Cancel();
                    break;
                default:
                    throw new Exception();
            }
            tryCompleter = () => Interlocked.Exchange(ref completer, null)?.Invoke();
            return deferred.Promise;
        }

        public static Promise BuildPromise<TReject>(CompleteType completeType, bool isAlreadyComplete, TReject reason, CancelationToken cancelationToken, out Action tryCompleter)
        {
            if (cancelationToken.IsCancelationRequested)
            {
                tryCompleter = () => { };
                return Promise.Canceled();
            }

            if (isAlreadyComplete)
            {
                tryCompleter = () => { };
                switch (completeType)
                {
                    case CompleteType.Resolve:
                        return Promise.Resolved();
                    case CompleteType.Reject:
                        return Promise.Rejected(reason);
                    case CompleteType.Cancel:
                        return Promise.Canceled();
                }
                throw new Exception();
            }

            var deferred = Promise.NewDeferred();
            Action completer;
            switch (completeType)
            {
                case CompleteType.Resolve:
                    completer = () => deferred.Resolve();
                    break;
                case CompleteType.Reject:
                    completer = () => deferred.Reject(reason);
                    break;
                case CompleteType.Cancel:
                    completer = () => deferred.Cancel();
                    break;
                default:
                    throw new Exception();
            }
            var registration = cancelationToken.Register(() =>
            {
                if (Interlocked.Exchange(ref completer, null) != null)
                {
                    deferred.Cancel();
                }
            });
            tryCompleter = () => Interlocked.Exchange(ref completer, null)?.Invoke();
            return deferred.Promise
                .Finally(registration.Dispose);
        }

        public static Promise<T> BuildPromise<T, TReject>(CompleteType completeType, bool isAlreadyComplete, T value, TReject reason, CancelationToken cancelationToken, out Action tryCompleter)
        {
            if (cancelationToken.IsCancelationRequested)
            {
                tryCompleter = () => { };
                return Promise<T>.Canceled();
            }

            if (isAlreadyComplete)
            {
                tryCompleter = () => { };
                switch (completeType)
                {
                    case CompleteType.Resolve:
                        return Promise.Resolved(value);
                    case CompleteType.Reject:
                        return Promise<T>.Rejected(reason);
                    case CompleteType.Cancel:
                        return Promise<T>.Canceled();
                }
                throw new Exception();
            }

            var deferred = Promise<T>.NewDeferred();
            Action completer;
            switch (completeType)
            {
                case CompleteType.Resolve:
                    completer = () => deferred.Resolve(value);
                    break;
                case CompleteType.Reject:
                    completer = () => deferred.Reject(reason);
                    break;
                case CompleteType.Cancel:
                    completer = () => deferred.Cancel();
                    break;
                default:
                    throw new Exception();
            }
            var registration = cancelationToken.Register(() =>
            {
                if (Interlocked.Exchange(ref completer, null) != null)
                {
                    deferred.Cancel();
                }
            });
            tryCompleter = () => Interlocked.Exchange(ref completer, null)?.Invoke();
            return deferred.Promise
                .Finally(registration.Dispose);
        }

        public static Promise ThenDuplicate(this Promise promise, CancelationToken cancelationToken = default(CancelationToken))
            => promise
                .WaitAsync(cancelationToken)
                .Then(() => { });

        public static Promise<T> ThenDuplicate<T>(this Promise<T> promise, CancelationToken cancelationToken = default(CancelationToken))
            => promise
                .WaitAsync(cancelationToken)
                .Then(v => v);

        public static ContinuationOptions GetContinuationOptions(SynchronizationType continuationContext, CompletedContinuationBehavior completedBehavior)
            => continuationContext == SynchronizationType.Explicit
                ? new ContinuationOptions(_foregroundContext, completedBehavior)
                : new ContinuationOptions((SynchronizationOption) continuationContext, completedBehavior);

        public static Promise ConfigureContinuation(this Promise promise, ContinuationOptions? continuationOptions)
            => continuationOptions == null ? promise : promise.ConfigureContinuation(continuationOptions.Value);

        public static Promise<T> ConfigureContinuation<T>(this Promise<T> promise, ContinuationOptions? continuationOptions)
            => continuationOptions == null ? promise : promise.ConfigureContinuation(continuationOptions.Value);

        public static void WaitWithTimeout(this Promise promise, TimeSpan timeout)
        {
            if (!promise.TryWait(timeout))
            {
                throw new TimeoutException("Promise.TryWait timed out after " + timeout);
            }
        }

        public static T WaitWithTimeout<T>(this Promise<T> promise, TimeSpan timeout)
        {
            T result;
            if (!promise.TryWaitForResult(timeout, out result))
            {
                throw new TimeoutException("Promise.TryWaitForResult timed out after " + timeout);
            }
            return result;
        }

        public static void WaitWithTimeoutWhileExecutingForegroundContext(this Promise promise, TimeSpan timeout)
        {
            bool isPending = true;
            promise = promise.Finally(() => isPending = false);

            SpinUntilWhileExecutingForegroundContext(() => !isPending, timeout);
            promise.Wait();
        }

        public static T WaitWithTimeoutWhileExecutingForegroundContext<T>(this Promise<T> promise, TimeSpan timeout)
        {
            bool isPending = true;
            promise = promise.Finally(() => isPending = false);

            SpinUntilWhileExecutingForegroundContext(() => !isPending, timeout);
            return promise.WaitForResult();
        }

        public static void SpinUntil(Func<bool> condition, TimeSpan timeout, string message = null)
        {
            if (!SpinWait.SpinUntil(condition, timeout))
            {
                var msg = $"SpinUntil timed out after {timeout}";
                if (message != null)
                {
                    msg += $"; {message}";
                }
                throw new TimeoutException(msg);
            }
        }

        public static void SpinUntilWhileExecutingForegroundContext(Func<bool> condition, TimeSpan timeout, string message = null)
        {
            if (!SpinWait.SpinUntil(() =>
            {
                ExecuteForegroundCallbacks();
                return condition();
            }, timeout))
            {
                var msg = $"SpinUntilWhileExecutingForegroundContext timed out after {timeout}";
                if (message != null)
                {
                    msg += $"; {message}";
                }
                throw new TimeoutException(msg);
            }
        }

        public static void AssertCallbackContext(SynchronizationType expectedContext, SynchronizationType invokeContext, Thread foregroundThread)
        {
            switch (expectedContext)
            {
                case SynchronizationType.CapturedContext:
                case SynchronizationType.Foreground:
                case SynchronizationType.Explicit:
                {
                    Assert.AreEqual(foregroundThread, Thread.CurrentThread);
                    return;
                }
                case backgroundType:
                {
                    if (foregroundThread == Thread.CurrentThread)
                    {

                    }
                    Assert.AreNotEqual(foregroundThread, Thread.CurrentThread);
                    Assert.IsTrue(Thread.CurrentThread.IsBackground);
                    return;
                }
                case SynchronizationType.Synchronous:
                {
                    if (invokeContext == SynchronizationType.CapturedContext
                        || invokeContext == SynchronizationType.Foreground
                        || invokeContext == SynchronizationType.Explicit)
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

        // The distance between 1 and the largest value smaller than 1.
        public static readonly float progressEpsilon = 1f - 0.99999994f;

        public const int callbacksMultiplier = 5;

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
        public const int onCancelPromiseCallbacks = 2 * callbacksMultiplier;

        public static IEnumerable<Promise> GetTestablePromises(Promise.Retainer promiseRetainer, bool includePreserved = true)
        {
            // This helps to test several different kinds of promises to make sure they all work with the same API.
            if (includePreserved)
            {
#pragma warning disable CS0618 // Type or member is obsolete
                var preservedPromise = promiseRetainer.WaitAsync().Preserve();
                yield return preservedPromise;
                yield return preservedPromise.Duplicate();
                preservedPromise.Forget();
#pragma warning restore CS0618 // Type or member is obsolete
            }
            yield return promiseRetainer.WaitAsync();
            var deferred = Promise.NewDeferred();
            promiseRetainer.WaitAsync()
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
                        d.Reject(result.Reason);
                    }
                })
                .Forget();
            yield return deferred.Promise;
            yield return Await(promiseRetainer.WaitAsync());
        }

        public static IEnumerable<Promise<T>> GetTestablePromises<T>(Promise<T>.Retainer promiseRetainer, bool includePreserved = true)
        {
            // This helps to test several different kinds of promises to make sure they all work with the same API.
            if (includePreserved)
            {
#pragma warning disable CS0618 // Type or member is obsolete
                var preservedPromise = promiseRetainer.WaitAsync().Preserve();
                yield return preservedPromise;
                yield return preservedPromise.Duplicate();
                preservedPromise.Forget();
#pragma warning restore CS0618 // Type or member is obsolete
            }
            yield return promiseRetainer.WaitAsync();
            var deferred = Promise.NewDeferred<T>();
            promiseRetainer.WaitAsync()
                .ContinueWith(deferred, (d, result) =>
                {
                    if (result.State == Promise.State.Resolved)
                    {
                        d.Resolve(result.Value);
                    }
                    else if (result.State == Promise.State.Canceled)
                    {
                        d.Cancel();
                    }
                    else
                    {
                        d.Reject(result.Reason);
                    }
                })
                .Forget();
            yield return deferred.Promise;
            yield return Await(promiseRetainer.WaitAsync());
        }

        private static async Promise Await(Promise promise)
        {
            await promise;
        }

        private static async Promise<T> Await<T>(Promise<T> promise)
        {
            return await promise;
        }

        public static void AddResolveCallbacks<TConvert, TCapture>(Promise promise,
            Action onResolve = null, TConvert convertValue = default(TConvert),
            Action<TCapture> onResolveCapture = null, TCapture captureValue = default(TCapture),
            Func<Promise, Promise> promiseToPromise = null, Func<Promise<TConvert>, Promise<TConvert>> promiseToPromiseConvert = null,
            TestAction<Promise> onCallbackAdded = null, TestAction<Promise<TConvert>> onCallbackAddedConvert = null,
            Action onCancel = null,
            TestAction<Promise> onAdoptCallbackAdded = null, TestAction<Promise<TConvert>> onAdoptCallbackAddedConvert = null,
            ContinuationOptions? continuationOptions = null)
        {
            using (var retainer = promise.GetRetainer())
            {
                retainer.WaitAsync().Catch(() => { }).Forget(); // Suppress any rejections from the retained promise.

                AddResolveCallbacksWithCancelation(
                    retainer.WaitAsync(),
                    onResolve, convertValue,
                    onResolveCapture, captureValue,
                    promiseToPromise, promiseToPromiseConvert,
                    onCallbackAdded, onCallbackAddedConvert,
                    default(CancelationToken),
                    onCancel,
                    onAdoptCallbackAdded, onAdoptCallbackAddedConvert,
                    continuationOptions
                );

                CancelationSource cancelationSource = CancelationSource.New();
                AddResolveCallbacksWithCancelation(
                    retainer.WaitAsync(),
                    onResolve, convertValue,
                    onResolveCapture, captureValue,
                    promiseToPromise, promiseToPromiseConvert,
                    onCallbackAdded, onCallbackAddedConvert,
                    cancelationSource.Token,
                    onCancel,
                    onAdoptCallbackAdded, onAdoptCallbackAddedConvert,
                    continuationOptions
                );
                cancelationSource.Dispose();
            }
        }

        public static void AddResolveCallbacksWithCancelation<TConvert, TCapture>(Promise promise,
            Action onResolve = null, TConvert convertValue = default(TConvert),
            Action<TCapture> onResolveCapture = null, TCapture captureValue = default(TCapture),
            Func<Promise, Promise> promiseToPromise = null, Func<Promise<TConvert>, Promise<TConvert>> promiseToPromiseConvert = null,
            TestAction<Promise> onCallbackAdded = null, TestAction<Promise<TConvert>> onCallbackAddedConvert = null,
            CancelationToken cancelationToken = default(CancelationToken),
            Action onCancel = null,
            TestAction<Promise> onAdoptCallbackAdded = null, TestAction<Promise<TConvert>> onAdoptCallbackAddedConvert = null,
            ContinuationOptions? continuationOptions = null)
        {
            using (var retainer = promise.GetRetainer())
            {
                retainer.WaitAsync().Catch(() => { }).Forget(); // Suppress any rejections from the retained promise.

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

                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise p1 = default(Promise);
                    p1 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(() => { onResolve(); })
                        .CatchCancelation(onCancel);
                    onCallbackAdded(ref p1);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise<TConvert> p2 = default(Promise<TConvert>);
                    p2 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(() => { onResolve(); return convertValue; })
                        .CatchCancelation(() => { onCancel(); return convertValue; });
                    onCallbackAddedConvert(ref p2);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise p3 = default(Promise);
                    p3 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(() => { onResolve(); return promiseToPromise(p3); })
                        .CatchCancelation(onCancel);
                    onAdoptCallbackAdded(ref p3);
                    onCallbackAdded(ref p3);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise<TConvert> p4 = default(Promise<TConvert>);
                    p4 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(() => { onResolve(); return promiseToPromiseConvert(p4); })
                        .CatchCancelation(() => { onCancel(); return convertValue; });
                    onAdoptCallbackAddedConvert(ref p4);
                    onCallbackAddedConvert(ref p4);
                }

                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise p5 = default(Promise);
                    p5 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(captureValue, cv => { onResolveCapture(cv); onResolve(); })
                        .CatchCancelation(onCancel);
                    onCallbackAdded(ref p5);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise<TConvert> p6 = default(Promise<TConvert>);
                    p6 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return convertValue; })
                        .CatchCancelation(() => { onCancel(); return convertValue; });
                    onCallbackAddedConvert(ref p6);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise p7 = default(Promise);
                    p7 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return promiseToPromise(p7); })
                        .CatchCancelation(onCancel);
                    onAdoptCallbackAdded(ref p7);
                    onCallbackAdded(ref p7);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise<TConvert> p8 = default(Promise<TConvert>);
                    p8 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return promiseToPromiseConvert(p8); })
                        .CatchCancelation(() => { onCancel(); return convertValue; });
                    onAdoptCallbackAddedConvert(ref p8);
                    onCallbackAddedConvert(ref p8);
                }
            }
        }

        public static void AddResolveCallbacks<T, TConvert, TCapture>(Promise<T> promise,
            Action<T> onResolve = null, TConvert convertValue = default(TConvert),
            Action<TCapture> onResolveCapture = null, TCapture captureValue = default(TCapture),
            Func<Promise, Promise> promiseToPromise = null, Func<Promise<TConvert>, Promise<TConvert>> promiseToPromiseConvert = null,
            TestAction<Promise> onCallbackAdded = null, TestAction<Promise<TConvert>> onCallbackAddedConvert = null,
            Action onCancel = null,
            TestAction<Promise> onAdoptCallbackAdded = null, TestAction<Promise<TConvert>> onAdoptCallbackAddedConvert = null,
            ContinuationOptions? continuationOptions = null)
        {
            using (var retainer = promise.GetRetainer())
            {
                retainer.WaitAsync().Catch(() => { }).Forget(); // Suppress any rejections from the retained promise.

                AddResolveCallbacksWithCancelation(
                retainer.WaitAsync(),
                onResolve, convertValue,
                onResolveCapture, captureValue,
                promiseToPromise, promiseToPromiseConvert,
                onCallbackAdded, onCallbackAddedConvert,
                default(CancelationToken),
                onCancel,
                onAdoptCallbackAdded, onAdoptCallbackAddedConvert,
                continuationOptions
            );

                CancelationSource cancelationSource = CancelationSource.New();
                AddResolveCallbacksWithCancelation(
                    retainer.WaitAsync(),
                    onResolve, convertValue,
                    onResolveCapture, captureValue,
                    promiseToPromise, promiseToPromiseConvert,
                    onCallbackAdded, onCallbackAddedConvert,
                    cancelationSource.Token,
                    onCancel,
                    onAdoptCallbackAdded, onAdoptCallbackAddedConvert,
                    continuationOptions
                );
                cancelationSource.Dispose();
            }
        }

        public static void AddResolveCallbacksWithCancelation<T, TConvert, TCapture>(Promise<T> promise,
            Action<T> onResolve = null, TConvert convertValue = default(TConvert),
            Action<TCapture> onResolveCapture = null, TCapture captureValue = default(TCapture),
            Func<Promise, Promise> promiseToPromise = null, Func<Promise<TConvert>, Promise<TConvert>> promiseToPromiseConvert = null,
            TestAction<Promise> onCallbackAdded = null, TestAction<Promise<TConvert>> onCallbackAddedConvert = null,
            CancelationToken cancelationToken = default(CancelationToken),
            Action onCancel = null,
            TestAction<Promise> onAdoptCallbackAdded = null, TestAction<Promise<TConvert>> onAdoptCallbackAddedConvert = null,
            ContinuationOptions? continuationOptions = null)
        {
            using (var retainer = promise.GetRetainer())
            {
                retainer.WaitAsync().Catch(() => { }).Forget(); // Suppress any rejections from the retained promise.

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

                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise p1 = default(Promise);
                    p1 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(x => { onResolve(x); })
                        .CatchCancelation(onCancel);
                    onCallbackAdded(ref p1);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise<TConvert> p2 = default(Promise<TConvert>);
                    p2 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(x => { onResolve(x); return convertValue; })
                        .CatchCancelation(() => { onCancel(); return convertValue; });
                    onCallbackAddedConvert(ref p2);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise p3 = default(Promise);
                    p3 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(x => { onResolve(x); return promiseToPromise(p3); })
                        .CatchCancelation(onCancel);
                    onAdoptCallbackAdded(ref p3);
                    onCallbackAdded(ref p3);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise<TConvert> p4 = default(Promise<TConvert>);
                    p4 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(x => { onResolve(x); return promiseToPromiseConvert(p4); })
                        .CatchCancelation(() => { onCancel(); return convertValue; });
                    onAdoptCallbackAddedConvert(ref p4);
                    onCallbackAddedConvert(ref p4);
                }

                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise p5 = default(Promise);
                    p5 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); })
                        .CatchCancelation(onCancel);
                    onCallbackAdded(ref p5);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise<TConvert> p6 = default(Promise<TConvert>);
                    p6 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return convertValue; })
                        .CatchCancelation(() => { onCancel(); return convertValue; });
                    onCallbackAddedConvert(ref p6);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise p7 = default(Promise);
                    p7 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return promiseToPromise(p7); })
                        .CatchCancelation(onCancel);
                    onAdoptCallbackAdded(ref p7);
                    onCallbackAdded(ref p7);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise<TConvert> p8 = default(Promise<TConvert>);
                    p8 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return promiseToPromiseConvert(p8); })
                        .CatchCancelation(() => { onCancel(); return convertValue; });
                    onAdoptCallbackAddedConvert(ref p8);
                    onCallbackAddedConvert(ref p8);
                }
            }
        }

        public static void AddCallbacks<TConvert, TReject, TCapture>(Promise promise,
            Action onResolve = null, Action<TReject> onReject = null, Action onUnknownRejection = null, TConvert convertValue = default(TConvert),
            Action<TCapture> onResolveCapture = null, Action<TCapture> onRejectCapture = null, Action<TCapture> onUnknownRejectionCapture = null, TCapture captureValue = default(TCapture),
            Func<Promise, Promise> promiseToPromise = null, Func<Promise<TConvert>, Promise<TConvert>> promiseToPromiseConvert = null,
            TestAction<Promise> onCallbackAdded = null, TestAction<Promise<TConvert>> onCallbackAddedConvert = null,
            Action onCancel = null,
            TestAction<Promise> onDirectCallbackAdded = null, TestAction<Promise<TConvert>> onDirectCallbackAddedConvert = null, TestAction<Promise> onDirectCallbackAddedCatch = null,
            TestAction<Promise, AdoptLocation> onAdoptCallbackAdded = null, TestAction<Promise<TConvert>, AdoptLocation> onAdoptCallbackAddedConvert = null, TestAction<Promise> onAdoptCallbackAddedCatch = null,
            ContinuationOptions? continuationOptions = null)
        {
            using (var retainer = promise.GetRetainer())
            {
                retainer.WaitAsync().Catch(() => { }).Forget(); // Suppress any rejections from the retained promise.

                AddCallbacksWithCancelation(
                    retainer.WaitAsync(),
                    onResolve, onReject, onUnknownRejection, convertValue,
                    onResolveCapture, onRejectCapture, onUnknownRejectionCapture, captureValue,
                    promiseToPromise, promiseToPromiseConvert,
                    onCallbackAdded, onCallbackAddedConvert,
                    default(CancelationToken),
                    onCancel,
                    onDirectCallbackAdded, onDirectCallbackAddedConvert, onDirectCallbackAddedCatch,
                    onAdoptCallbackAdded, onAdoptCallbackAddedConvert, onAdoptCallbackAddedCatch,
                    continuationOptions
                );

                CancelationSource cancelationSource = CancelationSource.New();
                AddCallbacksWithCancelation(
                    retainer.WaitAsync(),
                    onResolve, onReject, onUnknownRejection, convertValue,
                    onResolveCapture, onRejectCapture, onUnknownRejectionCapture, captureValue,
                    promiseToPromise, promiseToPromiseConvert,
                    onCallbackAdded, onCallbackAddedConvert,
                    cancelationSource.Token,
                    onCancel,
                    onDirectCallbackAdded, onDirectCallbackAddedConvert, onDirectCallbackAddedCatch,
                    onAdoptCallbackAdded, onAdoptCallbackAddedConvert, onAdoptCallbackAddedCatch,
                    continuationOptions
                );
                cancelationSource.Dispose();
            }
        }

        public static void AddCallbacksWithCancelation<TConvert, TReject, TCapture>(Promise promise,
            Action onResolve = null, Action<TReject> onReject = null, Action onUnknownRejection = null, TConvert convertValue = default(TConvert),
            Action<TCapture> onResolveCapture = null, Action<TCapture> onRejectCapture = null, Action<TCapture> onUnknownRejectionCapture = null, TCapture captureValue = default(TCapture),
            Func<Promise, Promise> promiseToPromise = null, Func<Promise<TConvert>, Promise<TConvert>> promiseToPromiseConvert = null,
            TestAction<Promise> onCallbackAdded = null, TestAction<Promise<TConvert>> onCallbackAddedConvert = null,
            CancelationToken cancelationToken = default(CancelationToken),
            Action onCancel = null,
            TestAction<Promise> onDirectCallbackAdded = null, TestAction<Promise<TConvert>> onDirectCallbackAddedConvert = null, TestAction<Promise> onDirectCallbackAddedCatch = null,
            TestAction<Promise, AdoptLocation> onAdoptCallbackAdded = null, TestAction<Promise<TConvert>, AdoptLocation> onAdoptCallbackAddedConvert = null, TestAction<Promise> onAdoptCallbackAddedCatch = null,
            ContinuationOptions? continuationOptions = null)
        {
            using (var retainer = promise.GetRetainer())
            {
                retainer.WaitAsync().Catch(() => { }).Forget(); // Suppress any rejections from the retained promise.

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

                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise p1 = default(Promise);
                    p1 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(() => { onResolve(); }, () => { onUnknownRejection(); })
                        .CatchCancelation(onCancel);
                    onDirectCallbackAdded(ref p1);
                    onCallbackAdded(ref p1);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise p2 = default(Promise);
                    p2 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(() => { onResolve(); }, (TReject failValue) => { onReject(failValue); })
                        .CatchCancelation(onCancel);
                    onDirectCallbackAdded(ref p2);
                    onCallbackAdded(ref p2);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise p3 = default(Promise);
                    p3 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(() => { onResolve(); }, () => { onUnknownRejection(); return promiseToPromise(p3); })
                        .CatchCancelation(onCancel);
                    onAdoptCallbackAdded(ref p3, AdoptLocation.Reject);
                    onCallbackAdded(ref p3);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise p4 = default(Promise);
                    p4 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(() => { onResolve(); }, (TReject failValue) => { onReject(failValue); return promiseToPromise(p4); })
                        .CatchCancelation(onCancel);
                    onAdoptCallbackAdded(ref p4, AdoptLocation.Reject);
                    onCallbackAdded(ref p4);
                }

                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise<TConvert> p5 = default(Promise<TConvert>);
                    p5 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(() => { onResolve(); return convertValue; }, () => { onUnknownRejection(); return convertValue; })
                        .CatchCancelation(() => { onCancel(); return convertValue; });
                    onDirectCallbackAddedConvert(ref p5);
                    onCallbackAddedConvert(ref p5);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise<TConvert> p6 = default(Promise<TConvert>);
                    p6 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(() => { onResolve(); return convertValue; }, (TReject failValue) => { onReject(failValue); return convertValue; })
                        .CatchCancelation(() => { onCancel(); return convertValue; });
                    onDirectCallbackAddedConvert(ref p6);
                    onCallbackAddedConvert(ref p6);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise<TConvert> p7 = default(Promise<TConvert>);
                    p7 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(() => { onResolve(); return convertValue; }, () => { onUnknownRejection(); return promiseToPromiseConvert(p7); })
                        .CatchCancelation(() => { onCancel(); return convertValue; });
                    onAdoptCallbackAddedConvert(ref p7, AdoptLocation.Reject);
                    onCallbackAddedConvert(ref p7);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise<TConvert> p8 = default(Promise<TConvert>);
                    p8 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(() => { onResolve(); return convertValue; }, (TReject failValue) => { onReject(failValue); return promiseToPromiseConvert(p8); })
                        .CatchCancelation(() => { onCancel(); return convertValue; });
                    onAdoptCallbackAddedConvert(ref p8, AdoptLocation.Reject);
                    onCallbackAddedConvert(ref p8);
                }

                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise p9 = default(Promise);
                    p9 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(() => { onResolve(); return promiseToPromise(p9); }, () => { onUnknownRejection(); return promiseToPromise(p9); })
                        .CatchCancelation(onCancel);
                    onAdoptCallbackAdded(ref p9, AdoptLocation.Both);
                    onCallbackAdded(ref p9);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise p10 = default(Promise);
                    p10 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(() => { onResolve(); return promiseToPromise(p10); }, (TReject failValue) => { onReject(failValue); return promiseToPromise(p10); })
                        .CatchCancelation(onCancel);
                    onAdoptCallbackAdded(ref p10, AdoptLocation.Both);
                    onCallbackAdded(ref p10);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise p11 = default(Promise);
                    p11 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(() => { onResolve(); return promiseToPromise(p11); }, () => { onUnknownRejection(); })
                        .CatchCancelation(onCancel);
                    onAdoptCallbackAdded(ref p11, AdoptLocation.Resolve);
                    onCallbackAdded(ref p11);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise p12 = default(Promise);
                    p12 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(() => { onResolve(); return promiseToPromise(p12); }, (TReject failValue) => { onReject(failValue); })
                        .CatchCancelation(onCancel);
                    onAdoptCallbackAdded(ref p12, AdoptLocation.Resolve);
                    onCallbackAdded(ref p12);
                }

                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise<TConvert> p13 = default(Promise<TConvert>);
                    p13 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(() => { onResolve(); return promiseToPromiseConvert(p13); }, () => { onUnknownRejection(); return promiseToPromiseConvert(p13); })
                        .CatchCancelation(() => { onCancel(); return convertValue; });
                    onAdoptCallbackAddedConvert(ref p13, AdoptLocation.Both);
                    onCallbackAddedConvert(ref p13);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise<TConvert> p14 = default(Promise<TConvert>);
                    p14 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(() => { onResolve(); return promiseToPromiseConvert(p14); }, (TReject failValue) => { onReject(failValue); return promiseToPromiseConvert(p14); })
                        .CatchCancelation(() => { onCancel(); return convertValue; });
                    onAdoptCallbackAddedConvert(ref p14, AdoptLocation.Both);
                    onCallbackAddedConvert(ref p14);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise<TConvert> p15 = default(Promise<TConvert>);
                    p15 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(() => { onResolve(); return promiseToPromiseConvert(p15); }, () => { onUnknownRejection(); return convertValue; })
                        .CatchCancelation(() => { onCancel(); return convertValue; });
                    onAdoptCallbackAddedConvert(ref p15, AdoptLocation.Resolve);
                    onCallbackAddedConvert(ref p15);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise<TConvert> p16 = default(Promise<TConvert>);
                    p16 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(() => { onResolve(); return promiseToPromiseConvert(p16); }, (TReject failValue) => { onReject(failValue); return convertValue; })
                        .CatchCancelation(() => { onCancel(); return convertValue; });
                    onAdoptCallbackAddedConvert(ref p16, AdoptLocation.Resolve);
                    onCallbackAddedConvert(ref p16);
                }


                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise p17 = default(Promise);
                    p17 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Catch(() => { onUnknownRejection(); })
                        .CatchCancelation(onCancel);
                    onDirectCallbackAddedCatch(ref p17);
                    onCallbackAdded(ref p17);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise p18 = default(Promise);
                    p18 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Catch((TReject failValue) => { onReject(failValue); })
                        .CatchCancelation(onCancel);
                    onDirectCallbackAddedCatch(ref p18);
                    onCallbackAdded(ref p18);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise p19 = default(Promise);
                    p19 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Catch(() => { onUnknownRejection(); return promiseToPromise(p19); })
                        .CatchCancelation(onCancel);
                    onAdoptCallbackAddedCatch(ref p19);
                    onCallbackAdded(ref p19);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise p20 = default(Promise);
                    p20 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Catch((TReject failValue) => { onReject(failValue); return promiseToPromise(p20); })
                        .CatchCancelation(onCancel);
                    onAdoptCallbackAddedCatch(ref p20);
                    onCallbackAdded(ref p20);
                }


                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise p21 = default(Promise);
                    p21 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(captureValue, cv => { onResolveCapture(cv); onResolve(); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); })
                        .CatchCancelation(onCancel);
                    onDirectCallbackAdded(ref p21);
                    onCallbackAdded(ref p21);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise p22 = default(Promise);
                    p22 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(captureValue, cv => { onResolveCapture(cv); onResolve(); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); })
                        .CatchCancelation(onCancel);
                    onDirectCallbackAdded(ref p22);
                    onCallbackAdded(ref p22);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise p23 = default(Promise);
                    p23 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(captureValue, cv => { onResolveCapture(cv); onResolve(); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return promiseToPromise(p23); })
                        .CatchCancelation(onCancel);
                    onAdoptCallbackAdded(ref p23, AdoptLocation.Reject);
                    onCallbackAdded(ref p23);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise p24 = default(Promise);
                    p24 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(captureValue, cv => { onResolveCapture(cv); onResolve(); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return promiseToPromise(p24); })
                        .CatchCancelation(onCancel);
                    onAdoptCallbackAdded(ref p24, AdoptLocation.Reject);
                    onCallbackAdded(ref p24);
                }

                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise<TConvert> p25 = default(Promise<TConvert>);
                    p25 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return convertValue; }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return convertValue; })
                        .CatchCancelation(() => { onCancel(); return convertValue; });
                    onDirectCallbackAddedConvert(ref p25);
                    onCallbackAddedConvert(ref p25);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise<TConvert> p26 = default(Promise<TConvert>);
                    p26 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return convertValue; }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return convertValue; })
                        .CatchCancelation(() => { onCancel(); return convertValue; });
                    onDirectCallbackAddedConvert(ref p26);
                    onCallbackAddedConvert(ref p26);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise<TConvert> p27 = default(Promise<TConvert>);
                    p27 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return convertValue; }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return promiseToPromiseConvert(p27); })
                        .CatchCancelation(() => { onCancel(); return convertValue; });
                    onAdoptCallbackAddedConvert(ref p27, AdoptLocation.Reject);
                    onCallbackAddedConvert(ref p27);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise<TConvert> p28 = default(Promise<TConvert>);
                    p28 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return convertValue; }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return promiseToPromiseConvert(p28); })
                        .CatchCancelation(() => { onCancel(); return convertValue; });
                    onAdoptCallbackAddedConvert(ref p28, AdoptLocation.Reject);
                    onCallbackAddedConvert(ref p28);
                }

                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise p29 = default(Promise);
                    p29 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return promiseToPromise(p29); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return promiseToPromise(p29); })
                        .CatchCancelation(onCancel);
                    onAdoptCallbackAdded(ref p29, AdoptLocation.Both);
                    onCallbackAdded(ref p29);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise p30 = default(Promise);
                    p30 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return promiseToPromise(p30); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return promiseToPromise(p30); })
                        .CatchCancelation(onCancel);
                    onAdoptCallbackAdded(ref p30, AdoptLocation.Both);
                    onCallbackAdded(ref p30);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise p31 = default(Promise);
                    p31 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return promiseToPromise(p31); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); })
                        .CatchCancelation(onCancel);
                    onAdoptCallbackAdded(ref p31, AdoptLocation.Resolve);
                    onCallbackAdded(ref p31);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise p32 = default(Promise);
                    p32 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return promiseToPromise(p32); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); })
                        .CatchCancelation(onCancel);
                    onAdoptCallbackAdded(ref p32, AdoptLocation.Resolve);
                    onCallbackAdded(ref p32);
                }

                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise<TConvert> p33 = default(Promise<TConvert>);
                    p33 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return promiseToPromiseConvert(p33); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return promiseToPromiseConvert(p33); })
                        .CatchCancelation(() => { onCancel(); return convertValue; });
                    onAdoptCallbackAddedConvert(ref p33, AdoptLocation.Both);
                    onCallbackAddedConvert(ref p33);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise<TConvert> p34 = default(Promise<TConvert>);
                    p34 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return promiseToPromiseConvert(p34); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return promiseToPromiseConvert(p34); })
                        .CatchCancelation(() => { onCancel(); return convertValue; });
                    onAdoptCallbackAddedConvert(ref p34, AdoptLocation.Both);
                    onCallbackAddedConvert(ref p34);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise<TConvert> p35 = default(Promise<TConvert>);
                    p35 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return promiseToPromiseConvert(p35); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return convertValue; })
                        .CatchCancelation(() => { onCancel(); return convertValue; });
                    onAdoptCallbackAddedConvert(ref p35, AdoptLocation.Resolve);
                    onCallbackAddedConvert(ref p35);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise<TConvert> p36 = default(Promise<TConvert>);
                    p36 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return promiseToPromiseConvert(p36); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return convertValue; })
                        .CatchCancelation(() => { onCancel(); return convertValue; });
                    onAdoptCallbackAddedConvert(ref p36, AdoptLocation.Resolve);
                    onCallbackAddedConvert(ref p36);
                }


                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise p37 = default(Promise);
                    p37 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Catch(captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); })
                        .CatchCancelation(onCancel);
                    onDirectCallbackAddedCatch(ref p37);
                    onCallbackAdded(ref p37);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise p38 = default(Promise);
                    p38 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Catch(captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); })
                        .CatchCancelation(onCancel);
                    onDirectCallbackAddedCatch(ref p38);
                    onCallbackAdded(ref p38);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise p39 = default(Promise);
                    p39 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Catch(captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return promiseToPromise(p39); })
                        .CatchCancelation(onCancel);
                    onAdoptCallbackAddedCatch(ref p39);
                    onCallbackAdded(ref p39);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise p40 = default(Promise);
                    p40 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Catch(captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return promiseToPromise(p40); })
                        .CatchCancelation(onCancel);
                    onAdoptCallbackAddedCatch(ref p40);
                    onCallbackAdded(ref p40);
                }


                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise p41 = default(Promise);
                    p41 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(captureValue, cv => { onResolveCapture(cv); onResolve(); }, () => { onUnknownRejection(); })
                        .CatchCancelation(onCancel);
                    onDirectCallbackAdded(ref p41);
                    onCallbackAdded(ref p41);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise p42 = default(Promise);
                    p42 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(captureValue, cv => { onResolveCapture(cv); onResolve(); }, (TReject failValue) => { onReject(failValue); })
                        .CatchCancelation(onCancel);
                    onDirectCallbackAdded(ref p42);
                    onCallbackAdded(ref p42);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise p43 = default(Promise);
                    p43 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(captureValue, cv => { onResolveCapture(cv); onResolve(); }, () => { onUnknownRejection(); return promiseToPromise(p43); })
                        .CatchCancelation(onCancel);
                    onAdoptCallbackAdded(ref p43, AdoptLocation.Reject);
                    onCallbackAdded(ref p43);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise p44 = default(Promise);
                    p44 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(captureValue, cv => { onResolveCapture(cv); onResolve(); }, (TReject failValue) => { onReject(failValue); return promiseToPromise(p44); })
                        .CatchCancelation(onCancel);
                    onAdoptCallbackAdded(ref p44, AdoptLocation.Reject);
                    onCallbackAdded(ref p44);
                }

                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise<TConvert> p45 = default(Promise<TConvert>);
                    p45 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return convertValue; }, () => { onUnknownRejection(); return convertValue; })
                        .CatchCancelation(() => { onCancel(); return convertValue; });
                    onDirectCallbackAddedConvert(ref p45);
                    onCallbackAddedConvert(ref p45);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise<TConvert> p46 = default(Promise<TConvert>);
                    p46 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return convertValue; }, (TReject failValue) => { onReject(failValue); return convertValue; })
                        .CatchCancelation(() => { onCancel(); return convertValue; });
                    onDirectCallbackAddedConvert(ref p46);
                    onCallbackAddedConvert(ref p46);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise<TConvert> p47 = default(Promise<TConvert>);
                    p47 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return convertValue; }, () => { onUnknownRejection(); return promiseToPromiseConvert(p47); })
                        .CatchCancelation(() => { onCancel(); return convertValue; });
                    onAdoptCallbackAddedConvert(ref p47, AdoptLocation.Reject);
                    onCallbackAddedConvert(ref p47);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise<TConvert> p48 = default(Promise<TConvert>);
                    p48 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return convertValue; }, (TReject failValue) => { onReject(failValue); return promiseToPromiseConvert(p48); })
                        .CatchCancelation(() => { onCancel(); return convertValue; });
                    onAdoptCallbackAddedConvert(ref p48, AdoptLocation.Reject);
                    onCallbackAddedConvert(ref p48);
                }

                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise p49 = default(Promise);
                    p49 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return promiseToPromise(p49); }, () => { onUnknownRejection(); return promiseToPromise(p49); })
                        .CatchCancelation(onCancel);
                    onAdoptCallbackAdded(ref p49, AdoptLocation.Both);
                    onCallbackAdded(ref p49);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise p50 = default(Promise);
                    p50 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return promiseToPromise(p50); }, (TReject failValue) => { onReject(failValue); return promiseToPromise(p50); })
                        .CatchCancelation(onCancel);
                    onAdoptCallbackAdded(ref p50, AdoptLocation.Both);
                    onCallbackAdded(ref p50);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise p51 = default(Promise);
                    p51 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return promiseToPromise(p51); }, () => { onUnknownRejection(); })
                        .CatchCancelation(onCancel);
                    onAdoptCallbackAdded(ref p51, AdoptLocation.Resolve);
                    onCallbackAdded(ref p51);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise p52 = default(Promise);
                    p52 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return promiseToPromise(p52); }, (TReject failValue) => { onReject(failValue); })
                        .CatchCancelation(onCancel);
                    onAdoptCallbackAdded(ref p52, AdoptLocation.Resolve);
                    onCallbackAdded(ref p52);
                }

                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise<TConvert> p53 = default(Promise<TConvert>);
                    p53 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return promiseToPromiseConvert(p53); }, () => { onUnknownRejection(); return promiseToPromiseConvert(p53); })
                        .CatchCancelation(() => { onCancel(); return convertValue; });
                    onAdoptCallbackAddedConvert(ref p53, AdoptLocation.Both);
                    onCallbackAddedConvert(ref p53);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise<TConvert> p54 = default(Promise<TConvert>);
                    p54 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return promiseToPromiseConvert(p54); }, (TReject failValue) => { onReject(failValue); return promiseToPromiseConvert(p54); })
                        .CatchCancelation(() => { onCancel(); return convertValue; });
                    onAdoptCallbackAddedConvert(ref p54, AdoptLocation.Both);
                    onCallbackAddedConvert(ref p54);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise<TConvert> p55 = default(Promise<TConvert>);
                    p55 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return promiseToPromiseConvert(p55); }, () => { onUnknownRejection(); return convertValue; })
                        .CatchCancelation(() => { onCancel(); return convertValue; });
                    onAdoptCallbackAddedConvert(ref p55, AdoptLocation.Resolve);
                    onCallbackAddedConvert(ref p55);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise<TConvert> p56 = default(Promise<TConvert>);
                    p56 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return promiseToPromiseConvert(p56); }, (TReject failValue) => { onReject(failValue); return convertValue; })
                        .CatchCancelation(() => { onCancel(); return convertValue; });
                    onAdoptCallbackAddedConvert(ref p56, AdoptLocation.Resolve);
                    onCallbackAddedConvert(ref p56);
                }


                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise p57 = default(Promise);
                    p57 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(() => { onResolve(); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); })
                        .CatchCancelation(onCancel);
                    onDirectCallbackAdded(ref p57);
                    onCallbackAdded(ref p57);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise p58 = default(Promise);
                    p58 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(() => { onResolve(); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); })
                        .CatchCancelation(onCancel);
                    onDirectCallbackAdded(ref p58);
                    onCallbackAdded(ref p58);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise p59 = default(Promise);
                    p59 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(() => { onResolve(); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return promiseToPromise(p59); })
                        .CatchCancelation(onCancel);
                    onAdoptCallbackAdded(ref p59, AdoptLocation.Reject);
                    onCallbackAdded(ref p59);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise p60 = default(Promise);
                    p60 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(() => { onResolve(); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return promiseToPromise(p60); })
                        .CatchCancelation(onCancel);
                    onAdoptCallbackAdded(ref p60, AdoptLocation.Reject);
                    onCallbackAdded(ref p60);
                }

                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise<TConvert> p61 = default(Promise<TConvert>);
                    p61 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(() => { onResolve(); return convertValue; }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return convertValue; })
                        .CatchCancelation(() => { onCancel(); return convertValue; });
                    onDirectCallbackAddedConvert(ref p61);
                    onCallbackAddedConvert(ref p61);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise<TConvert> p62 = default(Promise<TConvert>);
                    p62 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(() => { onResolve(); return convertValue; }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return convertValue; })
                        .CatchCancelation(() => { onCancel(); return convertValue; });
                    onDirectCallbackAddedConvert(ref p62);
                    onCallbackAddedConvert(ref p62);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise<TConvert> p63 = default(Promise<TConvert>);
                    p63 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(() => { onResolve(); return convertValue; }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return promiseToPromiseConvert(p63); })
                        .CatchCancelation(() => { onCancel(); return convertValue; });
                    onAdoptCallbackAddedConvert(ref p63, AdoptLocation.Reject);
                    onCallbackAddedConvert(ref p63);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise<TConvert> p64 = default(Promise<TConvert>);
                    p64 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(() => { onResolve(); return convertValue; }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return promiseToPromiseConvert(p64); })
                        .CatchCancelation(() => { onCancel(); return convertValue; });
                    onAdoptCallbackAddedConvert(ref p64, AdoptLocation.Reject);
                    onCallbackAddedConvert(ref p64);
                }

                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise p65 = default(Promise);
                    p65 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(() => { onResolve(); return promiseToPromise(p65); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return promiseToPromise(p65); })
                        .CatchCancelation(onCancel);
                    onAdoptCallbackAdded(ref p65, AdoptLocation.Both);
                    onCallbackAdded(ref p65);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise p66 = default(Promise);
                    p66 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(() => { onResolve(); return promiseToPromise(p66); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return promiseToPromise(p66); })
                        .CatchCancelation(onCancel);
                    onAdoptCallbackAdded(ref p66, AdoptLocation.Both);
                    onCallbackAdded(ref p66);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise p67 = default(Promise);
                    p67 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(() => { onResolve(); return promiseToPromise(p67); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); })
                        .CatchCancelation(onCancel);
                    onAdoptCallbackAdded(ref p67, AdoptLocation.Resolve);
                    onCallbackAdded(ref p67);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise p68 = default(Promise);
                    p68 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(() => { onResolve(); return promiseToPromise(p68); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); })
                        .CatchCancelation(onCancel);
                    onAdoptCallbackAdded(ref p68, AdoptLocation.Resolve);
                    onCallbackAdded(ref p68);
                }

                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise<TConvert> p69 = default(Promise<TConvert>);
                    p69 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(() => { onResolve(); return promiseToPromiseConvert(p69); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return promiseToPromiseConvert(p69); })
                        .CatchCancelation(() => { onCancel(); return convertValue; });
                    onAdoptCallbackAddedConvert(ref p69, AdoptLocation.Both);
                    onCallbackAddedConvert(ref p69);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise<TConvert> p70 = default(Promise<TConvert>);
                    p70 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(() => { onResolve(); return promiseToPromiseConvert(p70); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return promiseToPromiseConvert(p70); })
                        .CatchCancelation(() => { onCancel(); return convertValue; });
                    onAdoptCallbackAddedConvert(ref p70, AdoptLocation.Both);
                    onCallbackAddedConvert(ref p70);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise<TConvert> p71 = default(Promise<TConvert>);
                    p71 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(() => { onResolve(); return promiseToPromiseConvert(p71); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return convertValue; })
                        .CatchCancelation(() => { onCancel(); return convertValue; });
                    onAdoptCallbackAddedConvert(ref p71, AdoptLocation.Resolve);
                    onCallbackAddedConvert(ref p71);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise<TConvert> p72 = default(Promise<TConvert>);
                    p72 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(() => { onResolve(); return promiseToPromiseConvert(p72); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return convertValue; })
                        .CatchCancelation(() => { onCancel(); return convertValue; });
                    onAdoptCallbackAddedConvert(ref p72, AdoptLocation.Resolve);
                    onCallbackAddedConvert(ref p72);
                }
            }
        }

        public static void AddCallbacks<T, TConvert, TReject, TCapture>(Promise<T> promise,
            Action<T> onResolve = null, Action<TReject> onReject = null, Action onUnknownRejection = null, TConvert convertValue = default(TConvert), T TValue = default(T),
            Action<TCapture> onResolveCapture = null, Action<TCapture> onRejectCapture = null, Action<TCapture> onUnknownRejectionCapture = null, TCapture captureValue = default(TCapture),
            Func<Promise, Promise> promiseToPromise = null, Func<Promise<TConvert>, Promise<TConvert>> promiseToPromiseConvert = null, Func<Promise<T>, Promise<T>> promiseToPromiseT = null,
            TestAction<Promise> onCallbackAdded = null, TestAction<Promise<TConvert>> onCallbackAddedConvert = null, TestAction<Promise<T>> onCallbackAddedT = null,
            Action onCancel = null,
            TestAction<Promise> onDirectCallbackAdded = null, TestAction<Promise<TConvert>> onDirectCallbackAddedConvert = null, TestAction<Promise<T>> onDirectCallbackAddedT = null,
            TestAction<Promise, AdoptLocation> onAdoptCallbackAdded = null, TestAction<Promise<TConvert>, AdoptLocation> onAdoptCallbackAddedConvert = null, TestAction<Promise<T>> onAdoptCallbackAddedCatch = null,
            ContinuationOptions? continuationOptions = null)
        {
            using (var retainer = promise.GetRetainer())
            {
                retainer.WaitAsync().Catch(() => { }).Forget(); // Suppress any rejections from the retained promise.

                AddCallbacksWithCancelation(
                    retainer.WaitAsync(),
                    onResolve, onReject, onUnknownRejection, convertValue, TValue,
                    onResolveCapture, onRejectCapture, onUnknownRejectionCapture, captureValue,
                    promiseToPromise, promiseToPromiseConvert, promiseToPromiseT,
                    onCallbackAdded, onCallbackAddedConvert, onCallbackAddedT,
                    default(CancelationToken),
                    onCancel,
                    onDirectCallbackAdded, onDirectCallbackAddedConvert, onDirectCallbackAddedT,
                    onAdoptCallbackAdded, onAdoptCallbackAddedConvert, onAdoptCallbackAddedCatch,
                    continuationOptions
                );

                CancelationSource cancelationSource = CancelationSource.New();
                AddCallbacksWithCancelation(
                    retainer.WaitAsync(),
                    onResolve, onReject, onUnknownRejection, convertValue, TValue,
                    onResolveCapture, onRejectCapture, onUnknownRejectionCapture, captureValue,
                    promiseToPromise, promiseToPromiseConvert, promiseToPromiseT,
                    onCallbackAdded, onCallbackAddedConvert, onCallbackAddedT,
                    cancelationSource.Token,
                    onCancel,
                    onDirectCallbackAdded, onDirectCallbackAddedConvert, onDirectCallbackAddedT,
                    onAdoptCallbackAdded, onAdoptCallbackAddedConvert, onAdoptCallbackAddedCatch,
                    continuationOptions
                );
                cancelationSource.Dispose();
            }
        }

        public static void AddCallbacksWithCancelation<T, TConvert, TReject, TCapture>(Promise<T> promise,
            Action<T> onResolve = null, Action<TReject> onReject = null, Action onUnknownRejection = null, TConvert convertValue = default(TConvert), T TValue = default(T),
            Action<TCapture> onResolveCapture = null, Action<TCapture> onRejectCapture = null, Action<TCapture> onUnknownRejectionCapture = null, TCapture captureValue = default(TCapture),
            Func<Promise, Promise> promiseToPromise = null, Func<Promise<TConvert>, Promise<TConvert>> promiseToPromiseConvert = null, Func<Promise<T>, Promise<T>> promiseToPromiseT = null,
            TestAction<Promise> onCallbackAdded = null, TestAction<Promise<TConvert>> onCallbackAddedConvert = null, TestAction<Promise<T>> onAdoptCallbackAddedCatch = null,
            CancelationToken cancelationToken = default(CancelationToken),
            Action onCancel = null,
            TestAction<Promise> onDirectCallbackAdded = null, TestAction<Promise<TConvert>> onDirectCallbackAddedConvert = null, TestAction<Promise<T>> onDirectCallbackAddedT = null,
            TestAction<Promise, AdoptLocation> onAdoptCallbackAdded = null, TestAction<Promise<TConvert>, AdoptLocation> onAdoptCallbackAddedConvert = null, TestAction<Promise<T>> onAdoptCallbackAddedT = null,
            ContinuationOptions? continuationOptions = null)
        {
            using (var retainer = promise.GetRetainer())
            {
                retainer.WaitAsync().Catch(() => { }).Forget(); // Suppress any rejections from the retained promise.

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
                if (onAdoptCallbackAddedCatch == null)
                {
                    onAdoptCallbackAddedCatch = (ref Promise<T> p) => p.Forget();
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

                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise p1 = default(Promise);
                    p1 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(x => { onResolve(x); }, () => { onUnknownRejection(); })
                        .CatchCancelation(onCancel);
                    onDirectCallbackAdded(ref p1);
                    onCallbackAdded(ref p1);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise p2 = default(Promise);
                    p2 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(x => { onResolve(x); }, (TReject failValue) => { onReject(failValue); })
                        .CatchCancelation(onCancel);
                    onDirectCallbackAdded(ref p2);
                    onCallbackAdded(ref p2);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise p3 = default(Promise);
                    p3 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(x => { onResolve(x); }, () => { onUnknownRejection(); return promiseToPromise(p3); })
                        .CatchCancelation(onCancel);
                    onAdoptCallbackAdded(ref p3, AdoptLocation.Reject);
                    onCallbackAdded(ref p3);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise p4 = default(Promise);
                    p4 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(x => { onResolve(x); }, (TReject failValue) => { onReject(failValue); return promiseToPromise(p4); })
                        .CatchCancelation(onCancel);
                    onAdoptCallbackAdded(ref p4, AdoptLocation.Reject);
                    onCallbackAdded(ref p4);
                }

                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise<TConvert> p5 = default(Promise<TConvert>);
                    p5 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(x => { onResolve(x); return convertValue; }, () => { onUnknownRejection(); return convertValue; })
                        .CatchCancelation(() => { onCancel(); return convertValue; });
                    onDirectCallbackAddedConvert(ref p5);
                    onCallbackAddedConvert(ref p5);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise<TConvert> p6 = default(Promise<TConvert>);
                    p6 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(x => { onResolve(x); return convertValue; }, (TReject failValue) => { onReject(failValue); return convertValue; })
                        .CatchCancelation(() => { onCancel(); return convertValue; });
                    onDirectCallbackAddedConvert(ref p6);
                    onCallbackAddedConvert(ref p6);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise<TConvert> p7 = default(Promise<TConvert>);
                    p7 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(x => { onResolve(x); return convertValue; }, () => { onUnknownRejection(); return promiseToPromiseConvert(p7); })
                        .CatchCancelation(() => { onCancel(); return convertValue; });
                    onAdoptCallbackAddedConvert(ref p7, AdoptLocation.Reject);
                    onCallbackAddedConvert(ref p7);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise<TConvert> p8 = default(Promise<TConvert>);
                    p8 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(x => { onResolve(x); return convertValue; }, (TReject failValue) => { onReject(failValue); return promiseToPromiseConvert(p8); })
                        .CatchCancelation(() => { onCancel(); return convertValue; });
                    onAdoptCallbackAddedConvert(ref p8, AdoptLocation.Reject);
                    onCallbackAddedConvert(ref p8);
                }

                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise p9 = default(Promise);
                    p9 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(x => { onResolve(x); return promiseToPromise(p9); }, () => { onUnknownRejection(); return promiseToPromise(p9); })
                        .CatchCancelation(onCancel);
                    onAdoptCallbackAdded(ref p9, AdoptLocation.Both);
                    onCallbackAdded(ref p9);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise p10 = default(Promise);
                    p10 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(x => { onResolve(x); return promiseToPromise(p10); }, (TReject failValue) => { onReject(failValue); return promiseToPromise(p10); })
                        .CatchCancelation(onCancel);
                    onAdoptCallbackAdded(ref p10, AdoptLocation.Both);
                    onCallbackAdded(ref p10);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise p11 = default(Promise);
                    p11 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(x => { onResolve(x); return promiseToPromise(p11); }, () => { onUnknownRejection(); })
                        .CatchCancelation(onCancel);
                    onAdoptCallbackAdded(ref p11, AdoptLocation.Resolve);
                    onCallbackAdded(ref p11);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise p12 = default(Promise);
                    p12 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(x => { onResolve(x); return promiseToPromise(p12); }, (TReject failValue) => { onReject(failValue); })
                        .CatchCancelation(onCancel);
                    onAdoptCallbackAdded(ref p12, AdoptLocation.Resolve);
                    onCallbackAdded(ref p12);
                }

                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise<TConvert> p13 = default(Promise<TConvert>);
                    p13 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(x => { onResolve(x); return promiseToPromiseConvert(p13); }, () => { onUnknownRejection(); return promiseToPromiseConvert(p13); })
                        .CatchCancelation(() => { onCancel(); return convertValue; });
                    onAdoptCallbackAddedConvert(ref p13, AdoptLocation.Both);
                    onCallbackAddedConvert(ref p13);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise<TConvert> p14 = default(Promise<TConvert>);
                    p14 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(x => { onResolve(x); return promiseToPromiseConvert(p14); }, (TReject failValue) => { onReject(failValue); return promiseToPromiseConvert(p14); })
                        .CatchCancelation(() => { onCancel(); return convertValue; });
                    onAdoptCallbackAddedConvert(ref p14, AdoptLocation.Both);
                    onCallbackAddedConvert(ref p14);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise<TConvert> p15 = default(Promise<TConvert>);
                    p15 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(x => { onResolve(x); return promiseToPromiseConvert(p15); }, () => { onUnknownRejection(); return convertValue; })
                        .CatchCancelation(() => { onCancel(); return convertValue; });
                    onAdoptCallbackAddedConvert(ref p15, AdoptLocation.Resolve);
                    onCallbackAddedConvert(ref p15);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise<TConvert> p16 = default(Promise<TConvert>);
                    p16 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(x => { onResolve(x); return promiseToPromiseConvert(p16); }, (TReject failValue) => { onReject(failValue); return convertValue; })
                        .CatchCancelation(() => { onCancel(); return convertValue; });
                    onAdoptCallbackAddedConvert(ref p16, AdoptLocation.Resolve);
                    onCallbackAddedConvert(ref p16);
                }


                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise<T> p17 = default(Promise<T>);
                    p17 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Catch(() => { onUnknownRejection(); return TValue; })
                        .CatchCancelation(() => { onCancel(); return TValue; });
                    onDirectCallbackAddedT(ref p17);
                    onAdoptCallbackAddedCatch(ref p17);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise<T> p18 = default(Promise<T>);
                    p18 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Catch((TReject failValue) => { onReject(failValue); return TValue; })
                        .CatchCancelation(() => { onCancel(); return TValue; });
                    onDirectCallbackAddedT(ref p18);
                    onAdoptCallbackAddedCatch(ref p18);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise<T> p19 = default(Promise<T>);
                    p19 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Catch(() => { onUnknownRejection(); return promiseToPromiseT(p19); })
                        .CatchCancelation(() => { onCancel(); return TValue; });
                    onAdoptCallbackAddedT(ref p19);
                    onAdoptCallbackAddedCatch(ref p19);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise<T> p20 = default(Promise<T>);
                    p20 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Catch((TReject failValue) => { onReject(failValue); return promiseToPromiseT(p20); })
                        .CatchCancelation(() => { onCancel(); return TValue; });
                    onAdoptCallbackAddedT(ref p20);
                    onAdoptCallbackAddedCatch(ref p20);
                }


                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise p21 = default(Promise);
                    p21 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); })
                        .CatchCancelation(onCancel);
                    onDirectCallbackAdded(ref p21);
                    onCallbackAdded(ref p21);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise p22 = default(Promise);
                    p22 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); })
                        .CatchCancelation(onCancel);
                    onDirectCallbackAdded(ref p22);
                    onCallbackAdded(ref p22);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise p23 = default(Promise);
                    p23 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return promiseToPromise(p23); })
                        .CatchCancelation(onCancel);
                    onAdoptCallbackAdded(ref p23, AdoptLocation.Reject);
                    onCallbackAdded(ref p23);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise p24 = default(Promise);
                    p24 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return promiseToPromise(p24); })
                        .CatchCancelation(onCancel);
                    onAdoptCallbackAdded(ref p24, AdoptLocation.Reject);
                    onCallbackAdded(ref p24);
                }

                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise<TConvert> p25 = default(Promise<TConvert>);
                    p25 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return convertValue; }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return convertValue; })
                        .CatchCancelation(() => { onCancel(); return convertValue; });
                    onDirectCallbackAddedConvert(ref p25);
                    onCallbackAddedConvert(ref p25);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise<TConvert> p26 = default(Promise<TConvert>);
                    p26 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return convertValue; }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return convertValue; })
                        .CatchCancelation(() => { onCancel(); return convertValue; });
                    onDirectCallbackAddedConvert(ref p26);
                    onCallbackAddedConvert(ref p26);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise<TConvert> p27 = default(Promise<TConvert>);
                    p27 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return convertValue; }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return promiseToPromiseConvert(p27); })
                        .CatchCancelation(() => { onCancel(); return convertValue; });
                    onAdoptCallbackAddedConvert(ref p27, AdoptLocation.Reject);
                    onCallbackAddedConvert(ref p27);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise<TConvert> p28 = default(Promise<TConvert>);
                    p28 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return convertValue; }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return promiseToPromiseConvert(p28); })
                        .CatchCancelation(() => { onCancel(); return convertValue; });
                    onAdoptCallbackAddedConvert(ref p28, AdoptLocation.Reject);
                    onCallbackAddedConvert(ref p28);
                }

                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise p29 = default(Promise);
                    p29 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return promiseToPromise(p29); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return promiseToPromise(p29); })
                        .CatchCancelation(onCancel);
                    onAdoptCallbackAdded(ref p29, AdoptLocation.Both);
                    onCallbackAdded(ref p29);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise p30 = default(Promise);
                    p30 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return promiseToPromise(p30); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return promiseToPromise(p30); })
                        .CatchCancelation(onCancel);
                    onAdoptCallbackAdded(ref p30, AdoptLocation.Both);
                    onCallbackAdded(ref p30);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise p31 = default(Promise);
                    p31 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return promiseToPromise(p31); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); })
                        .CatchCancelation(onCancel);
                    onAdoptCallbackAdded(ref p31, AdoptLocation.Resolve);
                    onCallbackAdded(ref p31);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise p32 = default(Promise);
                    p32 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return promiseToPromise(p32); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); })
                        .CatchCancelation(onCancel);
                    onAdoptCallbackAdded(ref p32, AdoptLocation.Resolve);
                    onCallbackAdded(ref p32);
                }


                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise<TConvert> p33 = default(Promise<TConvert>);
                    p33 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return promiseToPromiseConvert(p33); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return promiseToPromiseConvert(p33); })
                        .CatchCancelation(() => { onCancel(); return convertValue; });
                    onAdoptCallbackAddedConvert(ref p33, AdoptLocation.Both);
                    onCallbackAddedConvert(ref p33);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise<TConvert> p34 = default(Promise<TConvert>);
                    p34 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return promiseToPromiseConvert(p34); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return promiseToPromiseConvert(p34); })
                        .CatchCancelation(() => { onCancel(); return convertValue; });
                    onAdoptCallbackAddedConvert(ref p34, AdoptLocation.Both);
                    onCallbackAddedConvert(ref p34);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise<TConvert> p35 = default(Promise<TConvert>);
                    p35 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return promiseToPromiseConvert(p35); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return convertValue; })
                        .CatchCancelation(() => { onCancel(); return convertValue; });
                    onAdoptCallbackAddedConvert(ref p35, AdoptLocation.Resolve);
                    onCallbackAddedConvert(ref p35);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise<TConvert> p36 = default(Promise<TConvert>);
                    p36 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return promiseToPromiseConvert(p36); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return convertValue; })
                        .CatchCancelation(() => { onCancel(); return convertValue; });
                    onAdoptCallbackAddedConvert(ref p36, AdoptLocation.Resolve);
                    onCallbackAddedConvert(ref p36);
                }


                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise<T> p37 = default(Promise<T>);
                    p37 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Catch(captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return TValue; })
                        .CatchCancelation(() => { onCancel(); return TValue; });
                    onDirectCallbackAddedT(ref p37);
                    onAdoptCallbackAddedCatch(ref p37);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise<T> p38 = default(Promise<T>);
                    p38 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Catch(captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return TValue; })
                        .CatchCancelation(() => { onCancel(); return TValue; });
                    onDirectCallbackAddedT(ref p38);
                    onAdoptCallbackAddedCatch(ref p38);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise<T> p39 = default(Promise<T>);
                    p39 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Catch(captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return promiseToPromiseT(p39); })
                        .CatchCancelation(() => { onCancel(); return TValue; });
                    onAdoptCallbackAddedT(ref p39);
                    onAdoptCallbackAddedCatch(ref p39);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise<T> p40 = default(Promise<T>);
                    p40 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Catch(captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return promiseToPromiseT(p40); })
                        .CatchCancelation(() => { onCancel(); return TValue; });
                    onAdoptCallbackAddedT(ref p40);
                    onAdoptCallbackAddedCatch(ref p40);
                }


                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise p41 = default(Promise);
                    p41 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); }, () => { onUnknownRejection(); })
                        .CatchCancelation(onCancel);
                    onDirectCallbackAdded(ref p41);
                    onCallbackAdded(ref p41);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise p42 = default(Promise);
                    p42 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); }, (TReject failValue) => { onReject(failValue); })
                        .CatchCancelation(onCancel);
                    onDirectCallbackAdded(ref p42);
                    onCallbackAdded(ref p42);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise p43 = default(Promise);
                    p43 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); }, () => { onUnknownRejection(); return promiseToPromise(p43); })
                        .CatchCancelation(onCancel);
                    onAdoptCallbackAdded(ref p43, AdoptLocation.Reject);
                    onCallbackAdded(ref p43);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise p44 = default(Promise);
                    p44 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); }, (TReject failValue) => { onReject(failValue); return promiseToPromise(p44); })
                        .CatchCancelation(onCancel);
                    onAdoptCallbackAdded(ref p44, AdoptLocation.Reject);
                    onCallbackAdded(ref p44);
                }

                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise<TConvert> p45 = default(Promise<TConvert>);
                    p45 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return convertValue; }, () => { onUnknownRejection(); return convertValue; })
                        .CatchCancelation(() => { onCancel(); return convertValue; });
                    onDirectCallbackAddedConvert(ref p45);
                    onCallbackAddedConvert(ref p45);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise<TConvert> p46 = default(Promise<TConvert>);
                    p46 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return convertValue; }, (TReject failValue) => { onReject(failValue); return convertValue; })
                        .CatchCancelation(() => { onCancel(); return convertValue; });
                    onDirectCallbackAddedConvert(ref p46);
                    onCallbackAddedConvert(ref p46);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise<TConvert> p47 = default(Promise<TConvert>);
                    p47 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return convertValue; }, () => { onUnknownRejection(); return promiseToPromiseConvert(p47); })
                        .CatchCancelation(() => { onCancel(); return convertValue; });
                    onAdoptCallbackAddedConvert(ref p47, AdoptLocation.Reject);
                    onCallbackAddedConvert(ref p47);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise<TConvert> p48 = default(Promise<TConvert>);
                    p48 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return convertValue; }, (TReject failValue) => { onReject(failValue); return promiseToPromiseConvert(p48); })
                        .CatchCancelation(() => { onCancel(); return convertValue; });
                    onAdoptCallbackAddedConvert(ref p48, AdoptLocation.Reject);
                    onCallbackAddedConvert(ref p48);
                }

                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise p49 = default(Promise);
                    p49 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return promiseToPromise(p49); }, () => { onUnknownRejection(); return promiseToPromise(p49); })
                        .CatchCancelation(onCancel);
                    onAdoptCallbackAdded(ref p49, AdoptLocation.Both);
                    onCallbackAdded(ref p49);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise p50 = default(Promise);
                    p50 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return promiseToPromise(p50); }, (TReject failValue) => { onReject(failValue); return promiseToPromise(p50); })
                        .CatchCancelation(onCancel);
                    onAdoptCallbackAdded(ref p50, AdoptLocation.Both);
                    onCallbackAdded(ref p50);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise p51 = default(Promise);
                    p51 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return promiseToPromise(p51); }, () => { onUnknownRejection(); })
                        .CatchCancelation(onCancel);
                    onAdoptCallbackAdded(ref p51, AdoptLocation.Resolve);
                    onCallbackAdded(ref p51);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise p52 = default(Promise);
                    p52 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return promiseToPromise(p52); }, (TReject failValue) => { onReject(failValue); })
                        .CatchCancelation(onCancel);
                    onAdoptCallbackAdded(ref p52, AdoptLocation.Resolve);
                    onCallbackAdded(ref p52);
                }

                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise<TConvert> p53 = default(Promise<TConvert>);
                    p53 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return promiseToPromiseConvert(p53); }, () => { onUnknownRejection(); return promiseToPromiseConvert(p53); })
                        .CatchCancelation(() => { onCancel(); return convertValue; });
                    onAdoptCallbackAddedConvert(ref p53, AdoptLocation.Both);
                    onCallbackAddedConvert(ref p53);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise<TConvert> p54 = default(Promise<TConvert>);
                    p54 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return promiseToPromiseConvert(p54); }, (TReject failValue) => { onReject(failValue); return promiseToPromiseConvert(p54); })
                        .CatchCancelation(() => { onCancel(); return convertValue; });
                    onAdoptCallbackAddedConvert(ref p54, AdoptLocation.Both);
                    onCallbackAddedConvert(ref p54);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise<TConvert> p55 = default(Promise<TConvert>);
                    p55 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return promiseToPromiseConvert(p55); }, () => { onUnknownRejection(); return convertValue; })
                        .CatchCancelation(() => { onCancel(); return convertValue; });
                    onAdoptCallbackAddedConvert(ref p55, AdoptLocation.Resolve);
                    onCallbackAddedConvert(ref p55);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise<TConvert> p56 = default(Promise<TConvert>);
                    p56 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return promiseToPromiseConvert(p56); }, (TReject failValue) => { onReject(failValue); return convertValue; })
                        .CatchCancelation(() => { onCancel(); return convertValue; });
                    onAdoptCallbackAddedConvert(ref p56, AdoptLocation.Resolve);
                    onCallbackAddedConvert(ref p56);
                }


                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise p57 = default(Promise);
                    p57 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(x => { onResolve(x); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); })
                        .CatchCancelation(onCancel);
                    onDirectCallbackAdded(ref p57);
                    onCallbackAdded(ref p57);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise p58 = default(Promise);
                    p58 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(x => { onResolve(x); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); })
                        .CatchCancelation(onCancel);
                    onDirectCallbackAdded(ref p58);
                    onCallbackAdded(ref p58);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise p59 = default(Promise);
                    p59 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(x => { onResolve(x); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return promiseToPromise(p59); })
                        .CatchCancelation(onCancel);
                    onAdoptCallbackAdded(ref p59, AdoptLocation.Reject);
                    onCallbackAdded(ref p59);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise p60 = default(Promise);
                    p60 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(x => { onResolve(x); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return promiseToPromise(p60); })
                        .CatchCancelation(onCancel);
                    onAdoptCallbackAdded(ref p60, AdoptLocation.Reject);
                    onCallbackAdded(ref p60);
                }

                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise<TConvert> p61 = default(Promise<TConvert>);
                    p61 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(x => { onResolve(x); return convertValue; }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return convertValue; })
                        .CatchCancelation(() => { onCancel(); return convertValue; });
                    onDirectCallbackAddedConvert(ref p61);
                    onCallbackAddedConvert(ref p61);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise<TConvert> p62 = default(Promise<TConvert>);
                    p62 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(x => { onResolve(x); return convertValue; }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return convertValue; })
                        .CatchCancelation(() => { onCancel(); return convertValue; });
                    onDirectCallbackAddedConvert(ref p62);
                    onCallbackAddedConvert(ref p62);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise<TConvert> p63 = default(Promise<TConvert>);
                    p63 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(x => { onResolve(x); return convertValue; }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return promiseToPromiseConvert(p63); })
                        .CatchCancelation(() => { onCancel(); return convertValue; });
                    onAdoptCallbackAddedConvert(ref p63, AdoptLocation.Reject);
                    onCallbackAddedConvert(ref p63);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise<TConvert> p64 = default(Promise<TConvert>);
                    p64 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(x => { onResolve(x); return convertValue; }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return promiseToPromiseConvert(p64); })
                        .CatchCancelation(() => { onCancel(); return convertValue; });
                    onAdoptCallbackAddedConvert(ref p64, AdoptLocation.Reject);
                    onCallbackAddedConvert(ref p64);
                }

                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise p65 = default(Promise);
                    p65 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(x => { onResolve(x); return promiseToPromise(p65); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return promiseToPromise(p65); })
                        .CatchCancelation(onCancel);
                    onAdoptCallbackAdded(ref p65, AdoptLocation.Both);
                    onCallbackAdded(ref p65);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise p66 = default(Promise);
                    p66 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(x => { onResolve(x); return promiseToPromise(p66); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return promiseToPromise(p66); })
                        .CatchCancelation(onCancel);
                    onAdoptCallbackAdded(ref p66, AdoptLocation.Both);
                    onCallbackAdded(ref p66);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise p67 = default(Promise);
                    p67 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(x => { onResolve(x); return promiseToPromise(p67); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); })
                        .CatchCancelation(onCancel);
                    onAdoptCallbackAdded(ref p67, AdoptLocation.Resolve);
                    onCallbackAdded(ref p67);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise p68 = default(Promise);
                    p68 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(x => { onResolve(x); return promiseToPromise(p68); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); })
                        .CatchCancelation(onCancel);
                    onAdoptCallbackAdded(ref p68, AdoptLocation.Resolve);
                    onCallbackAdded(ref p68);
                }

                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise<TConvert> p69 = default(Promise<TConvert>);
                    p69 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(x => { onResolve(x); return promiseToPromiseConvert(p69); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return promiseToPromiseConvert(p69); })
                        .CatchCancelation(() => { onCancel(); return convertValue; });
                    onAdoptCallbackAddedConvert(ref p69, AdoptLocation.Both);
                    onCallbackAddedConvert(ref p69);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise<TConvert> p70 = default(Promise<TConvert>);
                    p70 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(x => { onResolve(x); return promiseToPromiseConvert(p70); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return promiseToPromiseConvert(p70); })
                        .CatchCancelation(() => { onCancel(); return convertValue; });
                    onAdoptCallbackAddedConvert(ref p70, AdoptLocation.Both);
                    onCallbackAddedConvert(ref p70);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise<TConvert> p71 = default(Promise<TConvert>);
                    p71 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(x => { onResolve(x); return promiseToPromiseConvert(p71); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return convertValue; })
                        .CatchCancelation(() => { onCancel(); return convertValue; });
                    onAdoptCallbackAddedConvert(ref p71, AdoptLocation.Resolve);
                    onCallbackAddedConvert(ref p71);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise<TConvert> p72 = default(Promise<TConvert>);
                    p72 = p
                        .WaitAsync(cancelationToken)
                        .ConfigureContinuation(continuationOptions)
                        .Then(x => { onResolve(x); return promiseToPromiseConvert(p72); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return convertValue; })
                        .CatchCancelation(() => { onCancel(); return convertValue; });
                    onAdoptCallbackAddedConvert(ref p72, AdoptLocation.Resolve);
                    onCallbackAddedConvert(ref p72);
                }
            }
        }

        public static void AddContinueCallbacks<TConvert, TCapture>(Promise promise,
            Action<Promise.ResultContainer> onContinue = null, TConvert convertValue = default(TConvert),
            Action<TCapture, Promise.ResultContainer> onContinueCapture = null, TCapture captureValue = default(TCapture),
            Func<Promise, Promise> promiseToPromise = null, Func<Promise<TConvert>, Promise<TConvert>> promiseToPromiseConvert = null,
            TestAction<Promise> onCallbackAdded = null, TestAction<Promise<TConvert>> onCallbackAddedConvert = null,
            Action onCancel = null,
            TestAction<Promise> onAdoptCallbackAdded = null, TestAction<Promise<TConvert>> onAdoptCallbackAddedConvert = null,
            ContinuationOptions? continuationOptions = null)
        {
            using (var retainer = promise.GetRetainer())
            {
                retainer.WaitAsync().Catch(() => { }).Forget(); // Suppress any rejections from the retained promise.

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

                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise p1 = default(Promise);
                    p1 = p
                        .ConfigureContinuation(continuationOptions)
                        .ContinueWith(r => { onContinue(r); })
                        .CatchCancelation(onCancel);
                    onCallbackAdded(ref p1);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise<TConvert> p2 = default(Promise<TConvert>);
                    p2 = p
                        .ConfigureContinuation(continuationOptions)
                        .ContinueWith(r => { onContinue(r); return convertValue; })
                        .CatchCancelation(() => { onCancel(); return convertValue; });
                    onCallbackAddedConvert(ref p2);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise p3 = default(Promise);
                    p3 = p
                        .ConfigureContinuation(continuationOptions)
                        .ContinueWith(r => { onContinue(r); return promiseToPromise(p3); })
                        .CatchCancelation(onCancel);
                    onAdoptCallbackAdded(ref p3);
                    onCallbackAdded(ref p3);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise<TConvert> p4 = default(Promise<TConvert>);
                    p4 = p
                        .ConfigureContinuation(continuationOptions)
                        .ContinueWith(r => { onContinue(r); return promiseToPromiseConvert(p4); })
                        .CatchCancelation(() => { onCancel(); return convertValue; });
                    onAdoptCallbackAddedConvert(ref p4);
                    onCallbackAddedConvert(ref p4);
                }

                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise p5 = default(Promise);
                    p5 = p
                        .ConfigureContinuation(continuationOptions)
                        .ContinueWith(captureValue, (cv, r) => { onContinueCapture(cv, r); onContinue(r); })
                        .CatchCancelation(onCancel);
                    onCallbackAdded(ref p5);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise<TConvert> p6 = default(Promise<TConvert>);
                    p6 = p
                        .ConfigureContinuation(continuationOptions)
                        .ContinueWith(captureValue, (cv, r) => { onContinueCapture(cv, r); onContinue(r); return convertValue; })
                        .CatchCancelation(() => { onCancel(); return convertValue; });
                    onCallbackAddedConvert(ref p6);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise p7 = default(Promise);
                    p7 = p
                        .ConfigureContinuation(continuationOptions)
                        .ContinueWith(captureValue, (cv, r) => { onContinueCapture(cv, r); onContinue(r); return promiseToPromise(p7); })
                        .CatchCancelation(onCancel);
                    onAdoptCallbackAdded(ref p7);
                    onCallbackAdded(ref p7);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise<TConvert> p8 = default(Promise<TConvert>);
                    p8 = p
                        .ConfigureContinuation(continuationOptions)
                        .ContinueWith(captureValue, (cv, r) => { onContinueCapture(cv, r); onContinue(r); return promiseToPromiseConvert(p8); })
                        .CatchCancelation(() => { onCancel(); return convertValue; });
                    onAdoptCallbackAddedConvert(ref p8);
                    onCallbackAddedConvert(ref p8);
                }

                // Also call overloads accepting CancelationToken
                CancelationSource cancelationSource = CancelationSource.New();
                AddContinueCallbacksWithCancelation(
                    retainer.WaitAsync(),
                    onContinue, convertValue,
                    onContinueCapture, captureValue,
                    promiseToPromise, promiseToPromiseConvert,
                    onCallbackAdded, onCallbackAddedConvert,
                    cancelationSource.Token,
                    onCancel,
                    onAdoptCallbackAdded, onAdoptCallbackAddedConvert,
                    continuationOptions
                );
                cancelationSource.Dispose();
            }
        }

        public static void AddContinueCallbacksWithCancelation<TConvert, TCapture>(Promise promise,
            Action<Promise.ResultContainer> onContinue = null, TConvert convertValue = default(TConvert),
            Action<TCapture, Promise.ResultContainer> onContinueCapture = null, TCapture captureValue = default(TCapture),
            Func<Promise, Promise> promiseToPromise = null, Func<Promise<TConvert>, Promise<TConvert>> promiseToPromiseConvert = null,
            TestAction<Promise> onCallbackAdded = null, TestAction<Promise<TConvert>> onCallbackAddedConvert = null,
            CancelationToken cancelationToken = default(CancelationToken),
            Action onCancel = null,
            TestAction<Promise> onAdoptCallbackAdded = null, TestAction<Promise<TConvert>> onAdoptCallbackAddedConvert = null,
            ContinuationOptions? continuationOptions = null)
        {
            using (var retainer = promise.GetRetainer())
            {
                retainer.WaitAsync().Catch(() => { }).Forget(); // Suppress any rejections from the retained promise.

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

                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise p1 = default(Promise);
                    p1 = p
                        .ConfigureContinuation(continuationOptions)
                        .ContinueWith(r => { onContinue(r); }, cancelationToken)
                        .CatchCancelation(onCancel);
                    onCallbackAdded(ref p1);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise<TConvert> p2 = default(Promise<TConvert>);
                    p2 = p
                        .ConfigureContinuation(continuationOptions)
                        .ContinueWith(r => { onContinue(r); return convertValue; }, cancelationToken)
                        .CatchCancelation(() => { onCancel(); return convertValue; });
                    onCallbackAddedConvert(ref p2);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise p3 = default(Promise);
                    p3 = p
                        .ConfigureContinuation(continuationOptions)
                        .ContinueWith(r => { onContinue(r); return promiseToPromise(p3); }, cancelationToken)
                        .CatchCancelation(onCancel);
                    onAdoptCallbackAdded(ref p3);
                    onCallbackAdded(ref p3);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise<TConvert> p4 = default(Promise<TConvert>);
                    p4 = p
                        .ConfigureContinuation(continuationOptions)
                        .ContinueWith(r => { onContinue(r); return promiseToPromiseConvert(p4); }, cancelationToken)
                        .CatchCancelation(() => { onCancel(); return convertValue; });
                    onAdoptCallbackAddedConvert(ref p4);
                    onCallbackAddedConvert(ref p4);
                }

                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise p5 = default(Promise);
                    p5 = p
                        .ConfigureContinuation(continuationOptions)
                        .ContinueWith(captureValue, (cv, r) => { onContinueCapture(cv, r); onContinue(r); }, cancelationToken)
                        .CatchCancelation(onCancel);
                    onCallbackAdded(ref p5);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise<TConvert> p6 = default(Promise<TConvert>);
                    p6 = p
                        .ConfigureContinuation(continuationOptions)
                        .ContinueWith(captureValue, (cv, r) => { onContinueCapture(cv, r); onContinue(r); return convertValue; }, cancelationToken)
                        .CatchCancelation(() => { onCancel(); return convertValue; });
                    onCallbackAddedConvert(ref p6);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise p7 = default(Promise);
                    p7 = p
                        .ConfigureContinuation(continuationOptions)
                        .ContinueWith(captureValue, (cv, r) => { onContinueCapture(cv, r); onContinue(r); return promiseToPromise(p7); }, cancelationToken)
                        .CatchCancelation(onCancel);
                    onAdoptCallbackAdded(ref p7);
                    onCallbackAdded(ref p7);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise<TConvert> p8 = default(Promise<TConvert>);
                    p8 = p
                        .ConfigureContinuation(continuationOptions)
                        .ContinueWith(captureValue, (cv, r) => { onContinueCapture(cv, r); onContinue(r); return promiseToPromiseConvert(p8); }, cancelationToken)
                        .CatchCancelation(() => { onCancel(); return convertValue; });
                    onAdoptCallbackAddedConvert(ref p8);
                    onCallbackAddedConvert(ref p8);
                }
            }
        }

        public static void AddContinueCallbacks<T, TConvert, TCapture>(Promise<T> promise,
            Action<Promise<T>.ResultContainer> onContinue = null, TConvert convertValue = default(TConvert),
            Action<TCapture, Promise<T>.ResultContainer> onContinueCapture = null, TCapture captureValue = default(TCapture),
            Func<Promise, Promise> promiseToPromise = null, Func<Promise<TConvert>, Promise<TConvert>> promiseToPromiseConvert = null,
            TestAction<Promise> onCallbackAdded = null, TestAction<Promise<TConvert>> onCallbackAddedConvert = null,
            Action onCancel = null,
            TestAction<Promise> onAdoptCallbackAdded = null, TestAction<Promise<TConvert>> onAdoptCallbackAddedConvert = null,
            ContinuationOptions? continuationOptions = null)
        {
            using (var retainer = promise.GetRetainer())
            {
                retainer.WaitAsync().Catch(() => { }).Forget(); // Suppress any rejections from the retained promise.

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

                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise p1 = default(Promise);
                    p1 = p
                        .ConfigureContinuation(continuationOptions)
                        .ContinueWith(r => { onContinue(r); })
                        .CatchCancelation(onCancel);
                    onCallbackAdded(ref p1);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise<TConvert> p2 = default(Promise<TConvert>);
                    p2 = p
                        .ConfigureContinuation(continuationOptions)
                        .ContinueWith(r => { onContinue(r); return convertValue; })
                        .CatchCancelation(() => { onCancel(); return convertValue; });
                    onCallbackAddedConvert(ref p2);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise p3 = default(Promise);
                    p3 = p
                        .ConfigureContinuation(continuationOptions)
                        .ContinueWith(r => { onContinue(r); return promiseToPromise(p3); })
                        .CatchCancelation(onCancel);
                    onAdoptCallbackAdded(ref p3);
                    onCallbackAdded(ref p3);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise<TConvert> p4 = default(Promise<TConvert>);
                    p4 = p
                        .ConfigureContinuation(continuationOptions)
                        .ContinueWith(r => { onContinue(r); return promiseToPromiseConvert(p4); })
                        .CatchCancelation(() => { onCancel(); return convertValue; });
                    onAdoptCallbackAddedConvert(ref p4);
                    onCallbackAddedConvert(ref p4);
                }

                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise p5 = default(Promise);
                    p5 = p
                        .ConfigureContinuation(continuationOptions)
                        .ContinueWith(captureValue, (cv, r) => { onContinueCapture(cv, r); onContinue(r); })
                        .CatchCancelation(onCancel);
                    onCallbackAdded(ref p5);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise<TConvert> p6 = default(Promise<TConvert>);
                    p6 = p
                        .ConfigureContinuation(continuationOptions)
                        .ContinueWith(captureValue, (cv, r) => { onContinueCapture(cv, r); onContinue(r); return convertValue; })
                        .CatchCancelation(() => { onCancel(); return convertValue; });
                    onCallbackAddedConvert(ref p6);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise p7 = default(Promise);
                    p7 = p
                        .ConfigureContinuation(continuationOptions)
                        .ContinueWith(captureValue, (cv, r) => { onContinueCapture(cv, r); onContinue(r); return promiseToPromise(p7); })
                        .CatchCancelation(onCancel);
                    onAdoptCallbackAdded(ref p7);
                    onCallbackAdded(ref p7);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise<TConvert> p8 = default(Promise<TConvert>);
                    p8 = p
                        .ConfigureContinuation(continuationOptions)
                        .ContinueWith(captureValue, (cv, r) => { onContinueCapture(cv, r); onContinue(r); return promiseToPromiseConvert(p8); })
                        .CatchCancelation(() => { onCancel(); return convertValue; });
                    onAdoptCallbackAddedConvert(ref p8);
                    onCallbackAddedConvert(ref p8);
                }

                // Also call overloads accepting CancelationToken
                CancelationSource cancelationSource = CancelationSource.New();
                AddContinueCallbacksWithCancelation(
                    retainer.WaitAsync(),
                    onContinue, convertValue,
                    onContinueCapture, captureValue,
                    promiseToPromise, promiseToPromiseConvert,
                    onCallbackAdded, onCallbackAddedConvert,
                    cancelationSource.Token,
                    onCancel,
                    onAdoptCallbackAdded, onAdoptCallbackAddedConvert,
                    continuationOptions
                );
                cancelationSource.Dispose();
            }
        }

        public static void AddContinueCallbacksWithCancelation<T, TConvert, TCapture>(Promise<T> promise,
            Action<Promise<T>.ResultContainer> onContinue = null, TConvert convertValue = default(TConvert),
            Action<TCapture, Promise<T>.ResultContainer> onContinueCapture = null, TCapture captureValue = default(TCapture),
            Func<Promise, Promise> promiseToPromise = null, Func<Promise<TConvert>, Promise<TConvert>> promiseToPromiseConvert = null,
            TestAction<Promise> onCallbackAdded = null, TestAction<Promise<TConvert>> onCallbackAddedConvert = null,
            CancelationToken cancelationToken = default(CancelationToken),
            Action onCancel = null,
            TestAction<Promise> onAdoptCallbackAdded = null, TestAction<Promise<TConvert>> onAdoptCallbackAddedConvert = null,
            ContinuationOptions? continuationOptions = null)
        {
            using (var retainer = promise.GetRetainer())
            {
                retainer.WaitAsync().Catch(() => { }).Forget(); // Suppress any rejections from the retained promise.

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

                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise p1 = default(Promise);
                    p1 = p
                        .ConfigureContinuation(continuationOptions)
                        .ContinueWith(r => { onContinue(r); }, cancelationToken)
                        .CatchCancelation(onCancel);
                    onCallbackAdded(ref p1);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise<TConvert> p2 = default(Promise<TConvert>);
                    p2 = p
                        .ConfigureContinuation(continuationOptions)
                        .ContinueWith(r => { onContinue(r); return convertValue; }, cancelationToken)
                        .CatchCancelation(() => { onCancel(); return convertValue; });
                    onCallbackAddedConvert(ref p2);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise p3 = default(Promise);
                    p3 = p
                        .ConfigureContinuation(continuationOptions)
                        .ContinueWith(r => { onContinue(r); return promiseToPromise(p3); }, cancelationToken)
                        .CatchCancelation(onCancel);
                    onAdoptCallbackAdded(ref p3);
                    onCallbackAdded(ref p3);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise<TConvert> p4 = default(Promise<TConvert>);
                    p4 = p
                        .ConfigureContinuation(continuationOptions)
                        .ContinueWith(r => { onContinue(r); return promiseToPromiseConvert(p4); }, cancelationToken)
                        .CatchCancelation(() => { onCancel(); return convertValue; });
                    onAdoptCallbackAddedConvert(ref p4);
                    onCallbackAddedConvert(ref p4);
                }

                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise p5 = default(Promise);
                    p5 = p
                        .ConfigureContinuation(continuationOptions)
                        .ContinueWith(captureValue, (cv, r) => { onContinueCapture(cv, r); onContinue(r); }, cancelationToken)
                        .CatchCancelation(onCancel);
                    onCallbackAdded(ref p5);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise<TConvert> p6 = default(Promise<TConvert>);
                    p6 = p
                        .ConfigureContinuation(continuationOptions)
                        .ContinueWith(captureValue, (cv, r) => { onContinueCapture(cv, r); onContinue(r); return convertValue; }, cancelationToken)
                        .CatchCancelation(() => { onCancel(); return convertValue; });
                    onCallbackAddedConvert(ref p6);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise p7 = default(Promise);
                    p7 = p
                        .ConfigureContinuation(continuationOptions)
                        .ContinueWith(captureValue, (cv, r) => { onContinueCapture(cv, r); onContinue(r); return promiseToPromise(p7); }, cancelationToken)
                        .CatchCancelation(onCancel);
                    onAdoptCallbackAdded(ref p7);
                    onCallbackAdded(ref p7);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise<TConvert> p8 = default(Promise<TConvert>);
                    p8 = p
                        .ConfigureContinuation(continuationOptions)
                        .ContinueWith(captureValue, (cv, r) => { onContinueCapture(cv, r); onContinue(r); return promiseToPromiseConvert(p8); }, cancelationToken)
                        .CatchCancelation(() => { onCancel(); return convertValue; });
                    onAdoptCallbackAddedConvert(ref p8);
                    onCallbackAddedConvert(ref p8);
                }
            }
        }

        public static void AddCancelCallbacks<TCapture>(Promise promise,
            Action onCancel = null, Action<TCapture> onCancelCapture = null,
            TCapture captureValue = default(TCapture),
            Func<Promise, Promise> promiseToPromise = null,
            TestAction<Promise> onCallbackAdded = null,
            TestAction<Promise> onAdoptCallbackAdded = null,
            ContinuationOptions? continuationOptions = null)
        {
            using (var retainer = promise.GetRetainer())
            {
                retainer.WaitAsync().Catch(() => { }).Forget(); // Suppress any rejections from the retained promise.

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

                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise p1 = default(Promise);
                    p1 = p
                        .ConfigureContinuation(continuationOptions)
                        .CatchCancelation(() => { onCancel(); });
                    onCallbackAdded(ref p1);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise p2 = default(Promise);
                    p2 = p
                        .ConfigureContinuation(continuationOptions)
                        .CatchCancelation(captureValue, cv => { onCancelCapture(cv); });
                    onCallbackAdded(ref p2);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise p3 = default(Promise);
                    p3 = p
                        .ConfigureContinuation(continuationOptions)
                        .CatchCancelation(() => { onCancel(); return promiseToPromise(p3); });
                    onAdoptCallbackAdded(ref p3);
                    onCallbackAdded(ref p3);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise p3 = default(Promise);
                    p3 = p
                        .ConfigureContinuation(continuationOptions)
                        .CatchCancelation(captureValue, cv => { onCancelCapture(cv); return promiseToPromise(p3); });
                    onAdoptCallbackAdded(ref p3);
                    onCallbackAdded(ref p3);
                }
            }
        }

        public static void AddCancelCallbacks<T, TCapture>(Promise<T> promise, T TValue = default(T),
            Action onCancel = null, Action<TCapture> onCancelCapture = null,
            TCapture captureValue = default(TCapture),
            Func<Promise<T>, Promise<T>> promiseToPromise = null,
            TestAction<Promise<T>> onCallbackAdded = null,
            TestAction<Promise<T>> onAdoptCallbackAdded = null,
            ContinuationOptions? continuationOptions = null)
        {
            using (var retainer = promise.GetRetainer())
            {
                retainer.WaitAsync().Catch(() => { }).Forget(); // Suppress any rejections from the retained promise.

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

                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise<T> p1 = default(Promise<T>);
                    p1 = p
                        .ConfigureContinuation(continuationOptions)
                        .CatchCancelation(() => { onCancel(); return TValue; });
                    onCallbackAdded(ref p1);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise<T> p2 = default(Promise<T>);
                    p2 = p
                        .ConfigureContinuation(continuationOptions)
                        .CatchCancelation(captureValue, cv => { onCancelCapture(cv); return TValue; });
                    onCallbackAdded(ref p2);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise<T> p3 = default(Promise<T>);
                    p3 = p
                        .ConfigureContinuation(continuationOptions)
                        .CatchCancelation(() => { onCancel(); return promiseToPromise(p3); });
                    onAdoptCallbackAdded(ref p3);
                    onCallbackAdded(ref p3);
                }
                foreach (var p in GetTestablePromises(retainer))
                {
                    Promise<T> p3 = default(Promise<T>);
                    p3 = p
                        .ConfigureContinuation(continuationOptions)
                        .CatchCancelation(captureValue, cv => { onCancelCapture(cv); return promiseToPromise(p3); });
                    onAdoptCallbackAdded(ref p3);
                    onCallbackAdded(ref p3);
                }
            }
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
                promise => promise.ContinueWith(1, (cv, _) => { onContinue(); return 1; }).Forget(),
                promise => promise.ContinueWith(1, (cv, _) => { onContinue(); return Promise.Resolved(1); }).Forget()
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
                promise => promise.ContinueWith(1, (cv, _) => { onContinue(); return 1; }).Forget(),
                promise => promise.ContinueWith(1, (cv, _) => { onContinue(); return Promise.Resolved(1); }).Forget()
            };
        }
        public static Func<Promise, CancelationToken, Promise>[] ResolveActionsVoidWithCancelation(Action onResolved = null)
        {
            onResolved += () => { };
            return new Func<Promise, CancelationToken, Promise>[8]
            {
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(() => onResolved()),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(() => { onResolved(); return Promise.Resolved(); }),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(() => { onResolved(); return 1; }),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(() => { onResolved(); return Promise.Resolved(1); }),

                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(1, cv => onResolved()),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(1, cv => { onResolved(); return Promise.Resolved(); }),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(1, cv => { onResolved(); return 1; }),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(1, cv => { onResolved(); return Promise.Resolved(1); })
            };
        }

        public static Func<Promise<T>, CancelationToken, Promise>[] ResolveActionsWithCancelation<T>(Action<T> onResolved = null)
        {
            onResolved += v => { };
            return new Func<Promise<T>, CancelationToken, Promise>[8]
            {
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(v => onResolved(v)),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(v => { onResolved(v); return Promise.Resolved(); }),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(v => { onResolved(v); return 1; }),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(v => { onResolved(v); return Promise.Resolved(1); }),

                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(1, (cv, v) => onResolved(v)),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(1, (cv, v) => { onResolved(v); return Promise.Resolved(); }),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(1, (cv, v) => { onResolved(v); return 1; }),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(1, (cv, v) => { onResolved(v); return Promise.Resolved(1); }),
            };
        }

        public static Func<Promise, CancelationToken, Promise>[] ThenActionsVoidWithCancelation(Action onResolved = null, Action onRejected = null)
        {
            onResolved += () => { };
            onRejected += () => { };
            return new Func<Promise, CancelationToken, Promise>[64]
            {
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(() => onResolved(), () => onRejected()),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(() => onResolved(), (object o) => onRejected()),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(() => { onResolved(); return Promise.Resolved(); }, () => onRejected()),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(() => { onResolved(); return Promise.Resolved(); }, (object o) => onRejected()),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(() => { onResolved(); return 1; }, () => { onRejected(); return 1; }),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(() => { onResolved(); return 1; }, (object o) => { onRejected(); return 1; }),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(() => { onResolved(); return Promise.Resolved(1); }, () => { onRejected(); return 1; }),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(() => { onResolved(); return Promise.Resolved(1); }, (object o) => { onRejected(); return 1; }),

                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(() => onResolved(), () => { onRejected(); return Promise.Resolved(); }),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(() => onResolved(), (object o) => { onRejected(); return Promise.Resolved(); }),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(() => { onResolved(); return Promise.Resolved(); }, () => { onRejected(); return Promise.Resolved(); }),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(() => { onResolved(); return Promise.Resolved(); }, (object o) => { onRejected(); return Promise.Resolved(); }),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(() => { onResolved(); return 1; }, () => { onRejected(); return Promise.Resolved(1); }),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(() => { onResolved(); return 1; }, (object o) => { onRejected(); return Promise.Resolved(1); }),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(() => { onResolved(); return Promise.Resolved(1); }, () => { onRejected(); return Promise.Resolved(1); }),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(() => { onResolved(); return Promise.Resolved(1); }, (object o) => { onRejected(); return Promise.Resolved(1); }),

                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(1, cv => onResolved(), () => onRejected()),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(1, cv => onResolved(), (object o) => onRejected()),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(1, cv => { onResolved(); return Promise.Resolved(); }, () => onRejected()),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(1, cv => { onResolved(); return Promise.Resolved(); }, (object o) => onRejected()),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(1, cv => { onResolved(); return 1; }, () => { onRejected(); return 1; }),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(1, cv => { onResolved(); return 1; }, (object o) => { onRejected(); return 1; }),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(1, cv => { onResolved(); return Promise.Resolved(1); }, () => { onRejected(); return 1; }),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(1, cv => { onResolved(); return Promise.Resolved(1); }, (object o) => { onRejected(); return 1; }),

                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(1, cv => onResolved(), () => { onRejected(); return Promise.Resolved(); }),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(1, cv => onResolved(), (object o) => { onRejected(); return Promise.Resolved(); }),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(1, cv => { onResolved(); return Promise.Resolved(); }, () => { onRejected(); return Promise.Resolved(); }),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(1, cv => { onResolved(); return Promise.Resolved(); }, (object o) => { onRejected(); return Promise.Resolved(); }),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(1, cv => { onResolved(); return 1; }, () => { onRejected(); return Promise.Resolved(1); }),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(1, cv => { onResolved(); return 1; }, (object o) => { onRejected(); return Promise.Resolved(1); }),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(1, cv => { onResolved(); return Promise.Resolved(1); }, () => { onRejected(); return Promise.Resolved(1); }),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(1, cv => { onResolved(); return Promise.Resolved(1); }, (object o) => { onRejected(); return Promise.Resolved(1); }),

                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(() => onResolved(), 1, cv => onRejected()),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(() => onResolved(), 1, (int cv, object o) => onRejected()),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(() => { onResolved(); return Promise.Resolved(); }, 1, cv => onRejected()),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(() => { onResolved(); return Promise.Resolved(); }, 1, (int cv, object o) => onRejected()),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(() => { onResolved(); return 1; }, 1, cv => { onRejected(); return 1; }),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(() => { onResolved(); return 1; }, 1, (int cv, object o) => { onRejected(); return 1; }),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(() => { onResolved(); return Promise.Resolved(1); }, 1, cv => { onRejected(); return 1; }),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(() => { onResolved(); return Promise.Resolved(1); }, 1, (int cv, object o) => { onRejected(); return 1; }),

                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(() => onResolved(), 1, cv => { onRejected(); return Promise.Resolved(); }),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(() => onResolved(), 1, (int cv, object o) => { onRejected(); return Promise.Resolved(); }),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(() => { onResolved(); return Promise.Resolved(); }, 1, cv => { onRejected(); return Promise.Resolved(); }),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(() => { onResolved(); return Promise.Resolved(); }, 1, (int cv, object o) => { onRejected(); return Promise.Resolved(); }),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(() => { onResolved(); return 1; }, 1, cv => { onRejected(); return Promise.Resolved(1); }),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(() => { onResolved(); return 1; }, 1, (int cv, object o) => { onRejected(); return Promise.Resolved(1); }),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(() => { onResolved(); return Promise.Resolved(1); }, 1, cv => { onRejected(); return Promise.Resolved(1); }),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(() => { onResolved(); return Promise.Resolved(1); }, 1, (int cv, object o) => { onRejected(); return Promise.Resolved(1); }),

                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(1, cv => onResolved(), 1, cv => onRejected()),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(1, cv => onResolved(), 1, (int cv, object o) => onRejected()),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(1, cv => { onResolved(); return Promise.Resolved(); }, 1, cv => onRejected()),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(1, cv => { onResolved(); return Promise.Resolved(); }, 1, (int cv, object o) => onRejected()),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(1, cv => { onResolved(); return 1; }, 1, cv => { onRejected(); return 1; }),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(1, cv => { onResolved(); return 1; }, 1, (int cv, object o) => { onRejected(); return 1; }),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(1, cv => { onResolved(); return Promise.Resolved(1); }, 1, cv => { onRejected(); return 1; }),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(1, cv => { onResolved(); return Promise.Resolved(1); }, 1, (int cv, object o) => { onRejected(); return 1; }),

                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(1, cv => onResolved(), 1, cv => { onRejected(); return Promise.Resolved(); }),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(1, cv => onResolved(), 1, (int cv, object o) => { onRejected(); return Promise.Resolved(); }),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(1, cv => { onResolved(); return Promise.Resolved(); }, 1, cv => { onRejected(); return Promise.Resolved(); }),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(1, cv => { onResolved(); return Promise.Resolved(); }, 1, (int cv, object o) => { onRejected(); return Promise.Resolved(); }),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(1, cv => { onResolved(); return 1; }, 1, cv => { onRejected(); return Promise.Resolved(1); }),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(1, cv => { onResolved(); return 1; }, 1, (int cv, object o) => { onRejected(); return Promise.Resolved(1); }),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(1, cv => { onResolved(); return Promise.Resolved(1); }, 1, cv => { onRejected(); return Promise.Resolved(1); }),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(1, cv => { onResolved(); return Promise.Resolved(1); }, 1, (int cv, object o) => { onRejected(); return Promise.Resolved(1); }),
            };
        }

        public static Func<Promise<T>, CancelationToken, Promise>[] ThenActionsWithCancelation<T>(Action<T> onResolved = null, Action onRejected = null)
        {
            onResolved += v => { };
            onRejected += () => { };
            return new Func<Promise<T>, CancelationToken, Promise>[64]
            {
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(v => onResolved(v), () => onRejected()),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(v => onResolved(v), (object o) => onRejected()),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(v => { onResolved(v); return Promise.Resolved(); }, () => onRejected()),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(v => { onResolved(v); return Promise.Resolved(); }, (object o) => onRejected()),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(v => { onResolved(v); return 1; }, () => { onRejected(); return 1; }),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(v => { onResolved(v); return 1; }, (object o) => { onRejected(); return 1; }),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(v => { onResolved(v); return Promise.Resolved(1); }, () => { onRejected(); return 1; }),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(v => { onResolved(v); return Promise.Resolved(1); }, (object o) => { onRejected(); return 1; }),

                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(v => onResolved(v), () => { onRejected(); return Promise.Resolved(); }),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(v => onResolved(v), (object o) => { onRejected(); return Promise.Resolved(); }),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(v => { onResolved(v); return Promise.Resolved(); }, () => { onRejected(); return Promise.Resolved(); }),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(v => { onResolved(v); return Promise.Resolved(); }, (object o) => { onRejected(); return Promise.Resolved(); }),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(v => { onResolved(v); return 1; }, () => { onRejected(); return Promise.Resolved(1); }),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(v => { onResolved(v); return 1; }, (object o) => { onRejected(); return Promise.Resolved(1); }),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(v => { onResolved(v); return Promise.Resolved(1); }, () => { onRejected(); return Promise.Resolved(1); }),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(v => { onResolved(v); return Promise.Resolved(1); }, (object o) => { onRejected(); return Promise.Resolved(1); }),

                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(1, (cv, v) => onResolved(v), () => onRejected()),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(1, (cv, v) => onResolved(v), (object o) => onRejected()),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(1, (cv, v) => { onResolved(v); return Promise.Resolved(); }, () => onRejected()),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(1, (cv, v) => { onResolved(v); return Promise.Resolved(); }, (object o) => onRejected()),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(1, (cv, v) => { onResolved(v); return 1; }, () => { onRejected(); return 1; }),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(1, (cv, v) => { onResolved(v); return 1; }, (object o) => { onRejected(); return 1; }),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(1, (cv, v) => { onResolved(v); return Promise.Resolved(1); }, () => { onRejected(); return 1; }),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(1, (cv, v) => { onResolved(v); return Promise.Resolved(1); }, (object o) => { onRejected(); return 1; }),

                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(1, (cv, v) => onResolved(v), () => { onRejected(); return Promise.Resolved(); }),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(1, (cv, v) => onResolved(v), (object o) => { onRejected(); return Promise.Resolved(); }),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(1, (cv, v) => { onResolved(v); return Promise.Resolved(); }, () => { onRejected(); return Promise.Resolved(); }),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(1, (cv, v) => { onResolved(v); return Promise.Resolved(); }, (object o) => { onRejected(); return Promise.Resolved(); }),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(1, (cv, v) => { onResolved(v); return 1; }, () => { onRejected(); return Promise.Resolved(1); }),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(1, (cv, v) => { onResolved(v); return 1; }, (object o) => { onRejected(); return Promise.Resolved(1); }),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(1, (cv, v) => { onResolved(v); return Promise.Resolved(1); }, () => { onRejected(); return Promise.Resolved(1); }),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(1, (cv, v) => { onResolved(v); return Promise.Resolved(1); }, (object o) => { onRejected(); return Promise.Resolved(1); }),

                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(v => onResolved(v), 1, cv => onRejected()),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(v => onResolved(v), 1, (int cv, object o) => onRejected()),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(v => { onResolved(v); return Promise.Resolved(); }, 1, cv => onRejected()),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(v => { onResolved(v); return Promise.Resolved(); }, 1, (int cv, object o) => onRejected()),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(v => { onResolved(v); return 1; }, 1, cv => { onRejected(); return 1; }),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(v => { onResolved(v); return 1; }, 1, (int cv, object o) => { onRejected(); return 1; }),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(v => { onResolved(v); return Promise.Resolved(1); }, 1, cv => { onRejected(); return 1; }),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(v => { onResolved(v); return Promise.Resolved(1); }, 1, (int cv, object o) => { onRejected(); return 1; }),

                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(v => onResolved(v), 1, cv => { onRejected(); return Promise.Resolved(); }),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(v => onResolved(v), 1, (int cv, object o) => { onRejected(); return Promise.Resolved(); }),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(v => { onResolved(v); return Promise.Resolved(); }, 1, cv => { onRejected(); return Promise.Resolved(); }),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(v => { onResolved(v); return Promise.Resolved(); }, 1, (int cv, object o) => { onRejected(); return Promise.Resolved(); }),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(v => { onResolved(v); return 1; }, 1, cv => { onRejected(); return Promise.Resolved(1); }),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(v => { onResolved(v); return 1; }, 1, (int cv, object o) => { onRejected(); return Promise.Resolved(1); }),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(v => { onResolved(v); return Promise.Resolved(1); }, 1, cv => { onRejected(); return Promise.Resolved(1); }),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(v => { onResolved(v); return Promise.Resolved(1); }, 1, (int cv, object o) => { onRejected(); return Promise.Resolved(1); }),

                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(1, (cv, v) => onResolved(v), 1, cv => onRejected()),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(1, (cv, v) => onResolved(v), 1, (int cv, object o) => onRejected()),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(1, (cv, v) => { onResolved(v); return Promise.Resolved(); }, 1, cv => onRejected()),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(1, (cv, v) => { onResolved(v); return Promise.Resolved(); }, 1, (int cv, object o) => onRejected()),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(1, (cv, v) => { onResolved(v); return 1; }, 1, cv => { onRejected(); return 1; }),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(1, (cv, v) => { onResolved(v); return 1; }, 1, (int cv, object o) => { onRejected(); return 1; }),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(1, (cv, v) => { onResolved(v); return Promise.Resolved(1); }, 1, cv => { onRejected(); return 1; }),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(1, (cv, v) => { onResolved(v); return Promise.Resolved(1); }, 1, (int cv, object o) => { onRejected(); return 1; }),

                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(1, (cv, v) => onResolved(v), 1, cv => { onRejected(); return Promise.Resolved(); }),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(1, (cv, v) => onResolved(v), 1, (int cv, object o) => { onRejected(); return Promise.Resolved(); }),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(1, (cv, v) => { onResolved(v); return Promise.Resolved(); }, 1, cv => { onRejected(); return Promise.Resolved(); }),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(1, (cv, v) => { onResolved(v); return Promise.Resolved(); }, 1, (int cv, object o) => { onRejected(); return Promise.Resolved(); }),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(1, (cv, v) => { onResolved(v); return 1; }, 1, cv => { onRejected(); return Promise.Resolved(1); }),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(1, (cv, v) => { onResolved(v); return 1; }, 1, (int cv, object o) => { onRejected(); return Promise.Resolved(1); }),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(1, (cv, v) => { onResolved(v); return Promise.Resolved(1); }, 1, cv => { onRejected(); return Promise.Resolved(1); }),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Then(1, (cv, v) => { onResolved(v); return Promise.Resolved(1); }, 1, (int cv, object o) => { onRejected(); return Promise.Resolved(1); })
            };
        }

        public static Func<Promise, CancelationToken, Promise>[] CatchActionsVoidWithCancelation(Action onRejected = null)
        {
            onRejected += () => { };
            return new Func<Promise, CancelationToken, Promise>[8]
            {
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Catch(() => onRejected()),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Catch(() => { onRejected(); return Promise.Resolved(); }),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Catch((object o) => onRejected()),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Catch((object o) => { onRejected(); return Promise.Resolved(); }),

                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Catch(1, cv => onRejected()),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Catch(1, cv => { onRejected(); return Promise.Resolved(); }),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Catch(1, (int cv, object o) => onRejected()),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Catch(1, (int cv, object o) => { onRejected(); return Promise.Resolved(); })
            };
        }

        public static Func<Promise<T>, CancelationToken, Promise>[] CatchActionsWithCancelation<T>(Action onRejected = null)
        {
            onRejected += () => { };
            return new Func<Promise<T>, CancelationToken, Promise>[8]
            {
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Catch(() => { onRejected(); return default(T); }),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Catch(() => { onRejected(); return Promise.Resolved(default(T)); }),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Catch((object o) => { onRejected(); return default(T); }),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Catch((object o) => { onRejected(); return Promise.Resolved(default(T)); }),

                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Catch(1, cv => { onRejected(); return default(T); }),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Catch(1, cv => { onRejected(); return Promise.Resolved(default(T)); }),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Catch(1, (int cv, object o) => { onRejected(); return default(T); }),
                (promise, cancelationToken) => promise.WaitAsync(cancelationToken).Catch(1, (int cv, object o) => { onRejected(); return Promise.Resolved(default(T)); }),
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
                (promise, cancelationToken) => promise.ContinueWith(1, (cv, _) => { onContinue(); return 1; }, cancelationToken),
                (promise, cancelationToken) => promise.ContinueWith(1, (cv, _) => { onContinue(); return Promise.Resolved(1); }, cancelationToken)
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
                (promise, cancelationToken) => promise.ContinueWith(1, (cv, _) => { onContinue(); return 1; }, cancelationToken),
                (promise, cancelationToken) => promise.ContinueWith(1, (cv, _) => { onContinue(); return Promise.Resolved(1); }, cancelationToken)
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
                () => Promise.Resolved().ContinueWith(1, (cv, _) => returnProvider()),
                () => Promise.Resolved(1).ContinueWith(_ => returnProvider()),
                () => Promise.Resolved(1).ContinueWith(1, (cv, _) => returnProvider()),
            };
        }

        public static (Func<Promise, Promise> func, AdoptLocation adoptLocation)[] GetFunctionsAdoptingPromiseVoidToVoid<TCatch>(Func<Promise> returnProvider)
        {
            return new (Func<Promise, Promise>, AdoptLocation)[]
            {
                (promise => promise.Then(() => returnProvider()), AdoptLocation.Resolve),
                (promise => promise.Then(1, cv => returnProvider()), AdoptLocation.Resolve),


                (promise => promise.Then(() => returnProvider(), () => { }), AdoptLocation.Resolve),
                (promise => promise.Then(() => returnProvider(), (TCatch r) => { }), AdoptLocation.Resolve),
                (promise => promise.Then(() => returnProvider(), () => returnProvider()), AdoptLocation.Resolve),
                (promise => promise.Then(() => returnProvider(), (TCatch r) => returnProvider()), AdoptLocation.Resolve),

                (promise => promise.Then(() => { }, () => returnProvider()), AdoptLocation.Reject),
                (promise => promise.Then(() => { }, (TCatch r) => returnProvider()), AdoptLocation.Reject),
                (promise => promise.Then(() => returnProvider(), () => returnProvider()), AdoptLocation.Both),
                (promise => promise.Then(() => returnProvider(), (TCatch r) => returnProvider()), AdoptLocation.Both),

                (promise => promise.Then(1, cv => returnProvider(), () => { }), AdoptLocation.Resolve),
                (promise => promise.Then(1, cv => returnProvider(), (TCatch r) => { }), AdoptLocation.Resolve),
                (promise => promise.Then(1, cv => returnProvider(), () => returnProvider()), AdoptLocation.Both),
                (promise => promise.Then(1, cv => returnProvider(), (TCatch r) => returnProvider()), AdoptLocation.Both),

                (promise => promise.Then(1, cv => { }, () => returnProvider()), AdoptLocation.Reject),
                (promise => promise.Then(1, cv => { }, (TCatch r) => returnProvider()), AdoptLocation.Reject),
                (promise => promise.Then(1, cv => returnProvider(), () => returnProvider()), AdoptLocation.Both),
                (promise => promise.Then(1, cv => returnProvider(), (TCatch r) => returnProvider()), AdoptLocation.Both),

                (promise => promise.Then(() => returnProvider(), 1, cv => { }), AdoptLocation.Resolve),
                (promise => promise.Then(() => returnProvider(), 1, (int cv, TCatch r) => { }), AdoptLocation.Resolve),
                (promise => promise.Then(() => returnProvider(), 1, cv => returnProvider()), AdoptLocation.Both),
                (promise => promise.Then(() => returnProvider(), 1, (int cv, TCatch r) => returnProvider()), AdoptLocation.Both),

                (promise => promise.Then(() => { }, 1, cv => returnProvider()), AdoptLocation.Reject),
                (promise => promise.Then(() => { }, 1, (int cv, TCatch r) => returnProvider()), AdoptLocation.Reject),
                (promise => promise.Then(() => returnProvider(), 1, cv => returnProvider()), AdoptLocation.Both),
                (promise => promise.Then(() => returnProvider(), 1, (int cv, TCatch r) => returnProvider()), AdoptLocation.Both),

                (promise => promise.Then(1, cv => returnProvider(), 1, cv => { }), AdoptLocation.Resolve),
                (promise => promise.Then(1, cv => returnProvider(), 1, (int cv, TCatch r) => { }), AdoptLocation.Resolve),
                (promise => promise.Then(1, cv => returnProvider(), 1, cv => returnProvider()), AdoptLocation.Both),
                (promise => promise.Then(1, cv => returnProvider(), 1, (int cv, TCatch r) => returnProvider()), AdoptLocation.Both),

                (promise => promise.Then(1, cv => { }, 1, cv => returnProvider()), AdoptLocation.Reject),
                (promise => promise.Then(1, cv => { }, 1, (int cv, TCatch r) => returnProvider()), AdoptLocation.Reject),
                (promise => promise.Then(1, cv => returnProvider(), 1, cv => returnProvider()), AdoptLocation.Both),
                (promise => promise.Then(1, cv => returnProvider(), 1, (int cv, TCatch r) => returnProvider()), AdoptLocation.Both),


                (promise => promise.Catch(() => returnProvider()), AdoptLocation.Reject),
                (promise => promise.Catch((TCatch r) => returnProvider()), AdoptLocation.Reject),
                (promise => promise.Catch(1, cv => returnProvider()), AdoptLocation.Reject),
                (promise => promise.Catch(1, (int cv, TCatch r) => returnProvider()), AdoptLocation.Reject),


                (promise => promise.ContinueWith(_ => returnProvider()), AdoptLocation.Both),
                (promise => promise.ContinueWith(1, (cv, _) => returnProvider()), AdoptLocation.Both),
            };
        }

        public static (Func<Promise<T>, Promise> func, AdoptLocation adoptLocation)[] GetFunctionsAdoptingPromiseTToVoid<T, TCatch>(Func<Promise> returnProvider)
        {
            return new (Func<Promise<T>, Promise> func, AdoptLocation)[]
            {
                (promise => promise.Then(v => returnProvider()), AdoptLocation.Resolve),
                (promise => promise.Then(1, (cv, v) => returnProvider()), AdoptLocation.Resolve),

                (promise => promise.Then(v => returnProvider(), () => { }), AdoptLocation.Resolve),
                (promise => promise.Then(v => returnProvider(), (TCatch r) => { }), AdoptLocation.Resolve),
                (promise => promise.Then(v => returnProvider(), () => returnProvider()), AdoptLocation.Resolve),
                (promise => promise.Then(v => returnProvider(), (TCatch r) => returnProvider()), AdoptLocation.Resolve),

                (promise => promise.Then(1, (cv, v) => returnProvider(), () => { }), AdoptLocation.Resolve),
                (promise => promise.Then(1, (cv, v) => returnProvider(), (TCatch r) => { }), AdoptLocation.Resolve),
                (promise => promise.Then(1, (cv, v) => returnProvider(), () => returnProvider()), AdoptLocation.Both),
                (promise => promise.Then(1, (cv, v) => returnProvider(), (TCatch r) => returnProvider()), AdoptLocation.Both),

                (promise => promise.Then(v => returnProvider(), 1, cv => { }), AdoptLocation.Resolve),
                (promise => promise.Then(v => returnProvider(), 1, (int cv, TCatch r) => { }), AdoptLocation.Resolve),
                (promise => promise.Then(v => returnProvider(), 1, cv => returnProvider()), AdoptLocation.Both),
                (promise => promise.Then(v => returnProvider(), 1, (int cv, TCatch r) => returnProvider()), AdoptLocation.Both),

                (promise => promise.Then(1, (cv, v) => returnProvider(), 1, cv => { }), AdoptLocation.Resolve),
                (promise => promise.Then(1, (cv, v) => returnProvider(), 1, (int cv, TCatch r) => { }), AdoptLocation.Resolve),
                (promise => promise.Then(1, (cv, v) => returnProvider(), 1, cv => returnProvider()), AdoptLocation.Both),
                (promise => promise.Then(1, (cv, v) => returnProvider(), 1, (int cv, TCatch r) => returnProvider()), AdoptLocation.Both),

                (promise => promise.ContinueWith(_ => returnProvider()), AdoptLocation.Both),
                (promise => promise.ContinueWith(1, (cv, _) => returnProvider()), AdoptLocation.Both),

                (promise => promise.Then(() => { }, () => returnProvider()), AdoptLocation.Reject),
                (promise => promise.Then(() => { }, (TCatch r) => returnProvider()), AdoptLocation.Reject),
                (promise => promise.Then(() => returnProvider(), () => returnProvider()), AdoptLocation.Both),
                (promise => promise.Then(() => returnProvider(), (TCatch r) => returnProvider()), AdoptLocation.Both),
                                   
                (promise => promise.Then(1, cv => { }, () => returnProvider()), AdoptLocation.Reject),
                (promise => promise.Then(1, cv => { }, (TCatch r) => returnProvider()), AdoptLocation.Reject),
                (promise => promise.Then(1, cv => returnProvider(), () => returnProvider()), AdoptLocation.Both),
                (promise => promise.Then(1, cv => returnProvider(), (TCatch r) => returnProvider()), AdoptLocation.Both),
                                   
                (promise => promise.Then(() => { }, 1, cv => returnProvider()), AdoptLocation.Reject),
                (promise => promise.Then(() => { }, 1, (int cv, TCatch r) => returnProvider()), AdoptLocation.Reject),
                (promise => promise.Then(() => returnProvider(), 1, cv => returnProvider()), AdoptLocation.Both),
                (promise => promise.Then(() => returnProvider(), 1, (int cv, TCatch r) => returnProvider()), AdoptLocation.Both),
                                   
                (promise => promise.Then(1, cv => { }, 1, cv => returnProvider()), AdoptLocation.Reject),
                (promise => promise.Then(1, cv => { }, 1, (int cv, TCatch r) => returnProvider()), AdoptLocation.Reject),
                (promise => promise.Then(1, cv => returnProvider(), 1, cv => returnProvider()), AdoptLocation.Both),
                (promise => promise.Then(1, cv => returnProvider(), 1, (int cv, TCatch r) => returnProvider()), AdoptLocation.Both),
                                   
                                   
                (promise => promise.Catch(() => returnProvider()), AdoptLocation.Reject),
                (promise => promise.Catch((TCatch r) => returnProvider()), AdoptLocation.Reject),
                (promise => promise.Catch(1, cv => returnProvider()), AdoptLocation.Reject),
                (promise => promise.Catch(1, (int cv, TCatch r) => returnProvider()), AdoptLocation.Reject),
            };
        }

        public static (Func<Promise, Promise<T>> func, AdoptLocation adoptLocation)[] GetFunctionsAdoptingPromiseVoidToT<T, TCatch>(Func<Promise<T>> returnProvider)
        {
            return new (Func<Promise, Promise<T>> func, AdoptLocation adoptLocation)[]
            {
                (promise => promise.Then(() => returnProvider()), AdoptLocation.Resolve),
                (promise => promise.Then(1, cv => returnProvider()), AdoptLocation.Resolve),


                (promise => promise.Then(() => returnProvider(), () => default(T)), AdoptLocation.Resolve),
                (promise => promise.Then(() => returnProvider(), (TCatch r) => default(T)), AdoptLocation.Resolve),
                (promise => promise.Then(() => returnProvider(), () => returnProvider()), AdoptLocation.Resolve),
                (promise => promise.Then(() => returnProvider(), (TCatch r) => returnProvider()), AdoptLocation.Resolve),

                (promise => promise.Then(() => default(T), () => returnProvider()), AdoptLocation.Reject),
                (promise => promise.Then(() => default(T), (TCatch r) => returnProvider()), AdoptLocation.Reject),
                (promise => promise.Then(() => returnProvider(), () => returnProvider()), AdoptLocation.Both),
                (promise => promise.Then(() => returnProvider(), (TCatch r) => returnProvider()), AdoptLocation.Both),

                (promise => promise.Then(1, cv => returnProvider(), () => default(T)), AdoptLocation.Resolve),
                (promise => promise.Then(1, cv => returnProvider(), (TCatch r) => default(T)), AdoptLocation.Resolve),
                (promise => promise.Then(1, cv => returnProvider(), () => returnProvider()), AdoptLocation.Both),
                (promise => promise.Then(1, cv => returnProvider(), (TCatch r) => returnProvider()), AdoptLocation.Both),

                (promise => promise.Then(1, cv => default(T), () => returnProvider()), AdoptLocation.Reject),
                (promise => promise.Then(1, cv => default(T), (TCatch r) => returnProvider()), AdoptLocation.Reject),
                (promise => promise.Then(1, cv => returnProvider(), () => returnProvider()), AdoptLocation.Both),
                (promise => promise.Then(1, cv => returnProvider(), (TCatch r) => returnProvider()), AdoptLocation.Both),

                (promise => promise.Then(() => returnProvider(), 1, cv => default(T)), AdoptLocation.Resolve),
                (promise => promise.Then(() => returnProvider(), 1, (int cv, TCatch r) => default(T)), AdoptLocation.Resolve),
                (promise => promise.Then(() => returnProvider(), 1, cv => returnProvider()), AdoptLocation.Both),
                (promise => promise.Then(() => returnProvider(), 1, (int cv, TCatch r) => returnProvider()), AdoptLocation.Both),

                (promise => promise.Then(() => default(T), 1, cv => returnProvider()), AdoptLocation.Reject),
                (promise => promise.Then(() => default(T), 1, (int cv, TCatch r) => returnProvider()), AdoptLocation.Reject),
                (promise => promise.Then(() => returnProvider(), 1, cv => returnProvider()), AdoptLocation.Both),
                (promise => promise.Then(() => returnProvider(), 1, (int cv, TCatch r) => returnProvider()), AdoptLocation.Both),

                (promise => promise.Then(1, cv => returnProvider(), 1, cv => default(T)), AdoptLocation.Resolve),
                (promise => promise.Then(1, cv => returnProvider(), 1, (int cv, TCatch r) => default(T)), AdoptLocation.Resolve),
                (promise => promise.Then(1, cv => returnProvider(), 1, cv => returnProvider()), AdoptLocation.Both),
                (promise => promise.Then(1, cv => returnProvider(), 1, (int cv, TCatch r) => returnProvider()), AdoptLocation.Both),

                (promise => promise.Then(1, cv => default(T), 1, cv => returnProvider()), AdoptLocation.Reject),
                (promise => promise.Then(1, cv => default(T), 1, (int cv, TCatch r) => returnProvider()), AdoptLocation.Reject),
                (promise => promise.Then(1, cv => returnProvider(), 1, cv => returnProvider()), AdoptLocation.Both),
                (promise => promise.Then(1, cv => returnProvider(), 1, (int cv, TCatch r) => returnProvider()), AdoptLocation.Both),


                (promise => promise.ContinueWith(_ => returnProvider()), AdoptLocation.Both),
                (promise => promise.ContinueWith(1, (cv, _) => returnProvider()), AdoptLocation.Both),
            };
        }

        public static (Func<Promise<T>, Promise<T>> func, AdoptLocation adoptLocation)[] GetFunctionsAdoptingPromiseTToT<T, TCatch>(Func<Promise<T>> returnProvider)
        {
            return new (Func<Promise<T>, Promise<T>> func, AdoptLocation adoptLocation)[]
            {
                (promise => promise.Then(v => returnProvider()), AdoptLocation.Resolve),
                (promise => promise.Then(1, (cv, v) => returnProvider()), AdoptLocation.Resolve),

                (promise => promise.Then(v => returnProvider(), () => default(T)), AdoptLocation.Resolve),
                (promise => promise.Then(v => returnProvider(), (TCatch r) => default(T)), AdoptLocation.Resolve),
                (promise => promise.Then(v => returnProvider(), () => returnProvider()), AdoptLocation.Resolve),
                (promise => promise.Then(v => returnProvider(), (TCatch r) => returnProvider()), AdoptLocation.Resolve),

                (promise => promise.Then(1, (cv, v) => returnProvider(), () => default(T)), AdoptLocation.Resolve),
                (promise => promise.Then(1, (cv, v) => returnProvider(), (TCatch r) => default(T)), AdoptLocation.Resolve),
                (promise => promise.Then(1, (cv, v) => returnProvider(), () => returnProvider()), AdoptLocation.Both),
                (promise => promise.Then(1, (cv, v) => returnProvider(), (TCatch r) => returnProvider()), AdoptLocation.Both),

                (promise => promise.Then(v => returnProvider(), 1, cv => default(T)), AdoptLocation.Resolve),
                (promise => promise.Then(v => returnProvider(), 1, (int cv, TCatch r) => default(T)), AdoptLocation.Resolve),
                (promise => promise.Then(v => returnProvider(), 1, cv => returnProvider()), AdoptLocation.Both),
                (promise => promise.Then(v => returnProvider(), 1, (int cv, TCatch r) => returnProvider()), AdoptLocation.Both),

                (promise => promise.Then(1, (cv, v) => returnProvider(), 1, cv => default(T)), AdoptLocation.Resolve),
                (promise => promise.Then(1, (cv, v) => returnProvider(), 1, (int cv, TCatch r) => default(T)), AdoptLocation.Resolve),
                (promise => promise.Then(1, (cv, v) => returnProvider(), 1, cv => returnProvider()), AdoptLocation.Both),
                (promise => promise.Then(1, (cv, v) => returnProvider(), 1, (int cv, TCatch r) => returnProvider()), AdoptLocation.Both),

                (promise => promise.ContinueWith(_ => returnProvider()), AdoptLocation.Both),
                (promise => promise.ContinueWith(1, (cv, _) => returnProvider()), AdoptLocation.Both),

                (promise => promise.Then(() => default(T), () => returnProvider()), AdoptLocation.Reject),
                (promise => promise.Then(() => default(T), (TCatch r) => returnProvider()), AdoptLocation.Reject),
                (promise => promise.Then(() => returnProvider(), () => returnProvider()), AdoptLocation.Both),
                (promise => promise.Then(() => returnProvider(), (TCatch r) => returnProvider()), AdoptLocation.Both),

                (promise => promise.Then(1, cv => default(T), () => returnProvider()), AdoptLocation.Reject),
                (promise => promise.Then(1, cv => default(T), (TCatch r) => returnProvider()), AdoptLocation.Reject),
                (promise => promise.Then(1, cv => returnProvider(), () => returnProvider()), AdoptLocation.Both),
                (promise => promise.Then(1, cv => returnProvider(), (TCatch r) => returnProvider()), AdoptLocation.Both),

                (promise => promise.Then(() => default(T), 1, cv => returnProvider()), AdoptLocation.Reject),
                (promise => promise.Then(() => default(T), 1, (int cv, TCatch r) => returnProvider()), AdoptLocation.Reject),
                (promise => promise.Then(() => returnProvider(), 1, cv => returnProvider()), AdoptLocation.Both),
                (promise => promise.Then(() => returnProvider(), 1, (int cv, TCatch r) => returnProvider()), AdoptLocation.Both),

                (promise => promise.Then(1, cv => default(T), 1, cv => returnProvider()), AdoptLocation.Reject),
                (promise => promise.Then(1, cv => default(T), 1, (int cv, TCatch r) => returnProvider()), AdoptLocation.Reject),
                (promise => promise.Then(1, cv => returnProvider(), 1, cv => returnProvider()), AdoptLocation.Both),
                (promise => promise.Then(1, cv => returnProvider(), 1, (int cv, TCatch r) => returnProvider()), AdoptLocation.Both),


                (promise => promise.Catch(() => returnProvider()), AdoptLocation.Reject),
                (promise => promise.Catch((TCatch r) => returnProvider()), AdoptLocation.Reject),
                (promise => promise.Catch(1, cv => returnProvider()), AdoptLocation.Reject),
                (promise => promise.Catch(1, (int cv, TCatch r) => returnProvider()), AdoptLocation.Reject),
            };
        }
    }
}