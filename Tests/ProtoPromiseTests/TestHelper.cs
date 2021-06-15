﻿#if !PROTO_PROMISE_PROGRESS_DISABLE
#define PROMISE_PROGRESS
#else
#undef PROMISE_PROGRESS
#endif

#pragma warning disable IDE0034 // Simplify 'default' expression

using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Proto.Promises.Tests
{
    public delegate void TestAction<T>(ref T value);

    // These help test all Then/Catch/ContinueWith methods at once.
    public static partial class TestHelper
    {
        static TestHelper()
        {
            // Capture first exception from finalizers.
            // Only throw one exception instead of aggregate to try to avoid overloading the test error output.
            Promise.Config.UncaughtRejectionHandler = e => { throw e; };
            Promise.Config.ObjectPoolingEnabled = true; // Make sure to test object pool.
            Promise.Config.DebugCausalityTracer = Promise.TraceLevel.None; // Disabled because it makes the tests slow.
        }

        // Just to make sure static constructor is ran.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Setup() { }

        public static void Cleanup()
        {
            Promise.Manager.HandleCompletesAndProgress();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            Promise.Manager.HandleCompletesAndProgress();
        }

        public static Promise ThenDuplicate(this Promise promise, CancelationToken cancelationToken = default(CancelationToken))
        {
            return promise.Then(() => { }, cancelationToken);
        }

        public static Promise<T> ThenDuplicate<T>(this Promise<T> promise, CancelationToken cancelationToken = default(CancelationToken))
        {
            return promise.Then(v => v, cancelationToken);
        }

#if PROMISE_PROGRESS
        public static readonly double progressEpsilon = 1d / Math.Pow(2d, Promise.Config.ProgressDecimalBits);
#endif

        public const int resolveVoidCallbacks = 72;
        public const int resolveTCallbacks = 72;
        public const int rejectVoidCallbacks = 72;
        public const int rejectTCallbacks = 72;

        public const int resolveOnlyVoidCallbacks = 8;
        public const int resolveOnlyTCallbacks = 8;

        public const int resolveVoidVoidCallbacks = 18;
        public const int resolveVoidConvertCallbacks = 18;
        public const int resolveTVoidCallbacks = 18;
        public const int resolveTConvertCallbacks = 18;
        public const int rejectVoidVoidCallbacks = 20;
        public const int rejectVoidConvertCallbacks = 16;
        public const int rejectTVoidCallbacks = 16;
        public const int rejectTConvertCallbacks = 16;
        public const int rejectTTCallbacks = 4;

        public const int resolveVoidPromiseVoidCallbacks = 18;
        public const int resolveVoidPromiseConvertCallbacks = 18;
        public const int resolveTPromiseVoidCallbacks = 18;
        public const int resolveTPromiseConvertCallbacks = 18;
        public const int rejectVoidPromiseVoidCallbacks = 20;
        public const int rejectVoidPromiseConvertCallbacks = 16;
        public const int rejectTPromiseVoidCallbacks = 16;
        public const int rejectTPromiseConvertCallbacks = 16;
        public const int rejectTPromiseTCallbacks = 4;

        public const int rejectVoidKnownCallbacks = 36;
        public const int rejectTKnownCallbacks = 36;

        public const int continueVoidCallbacks = 8;
        public const int continueVoidVoidCallbacks = 2;
        public const int continueVoidConvertCallbacks = 2;
        public const int continueVoidPromiseVoidCallbacks = 2;
        public const int continueVoidPromiseConvertCallbacks = 2;

        public const int continueTCallbacks = 8;
        public const int continueTVoidCallbacks = 2;
        public const int continueTConvertCallbacks = 2;
        public const int continueTPromiseVoidCallbacks = 2;
        public const int continueTPromiseConvertCallbacks = 2;

        public const int cancelVoidCallbacks = (72 + 8 + 8) * 2;
        public const int cancelTCallbacks = (72 + 8 + 8) * 2;

        public static void AddResolveCallbacks<TConvert, TCapture>(Promise promise,
            Action onResolve = null, TConvert convertValue = default(TConvert),
            Action<TCapture> onResolveCapture = null, TCapture captureValue = default(TCapture),
            Func<Promise, Promise> promiseToPromise = null, Func<Promise<TConvert>, Promise<TConvert>> promiseToPromiseConvert = null,
            TestAction<Promise> onCallbackAdded = null, TestAction<Promise<TConvert>> onCallbackAddedConvert = null,
            Promise.CanceledAction onCancel = null, Promise.CanceledAction<TCapture> onCancelCapture = null)
        {
            promise = promise.Preserve();
            promise.Catch(() => { }).Forget(); // Suppress any rejections from the preserved promise.

            AddResolveCallbacksWithCancelation(
                promise,
                onResolve, convertValue,
                onResolveCapture, captureValue,
                promiseToPromise, promiseToPromiseConvert,
                onCallbackAdded, onCallbackAddedConvert,
                onCancel: onCancel, onCancelCapture: onCancelCapture
            );

            CancelationSource cancelationSource = CancelationSource.New();
            AddResolveCallbacksWithCancelation(
                promise,
                onResolve, convertValue,
                onResolveCapture, captureValue,
                promiseToPromise, promiseToPromiseConvert,
                onCallbackAdded, onCallbackAddedConvert,
                cancelationSource.Token,
                onCancel: onCancel, onCancelCapture: onCancelCapture
            );
            cancelationSource.Dispose();

            promise.Forget();
        }

        public static void AddResolveCallbacksWithCancelation<TConvert, TCapture>(Promise promise,
            Action onResolve = null, TConvert convertValue = default(TConvert),
            Action<TCapture> onResolveCapture = null, TCapture captureValue = default(TCapture),
            Func<Promise, Promise> promiseToPromise = null, Func<Promise<TConvert>, Promise<TConvert>> promiseToPromiseConvert = null,
            TestAction<Promise> onCallbackAdded = null, TestAction<Promise<TConvert>> onCallbackAddedConvert = null,
            CancelationToken cancelationToken = default(CancelationToken),
            Promise.CanceledAction onCancel = null, Promise.CanceledAction<TCapture> onCancelCapture = null)
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
            onCancel += _ => { };
            onCancelCapture += (_, __) => { };

            Promise p1 = default(Promise);
            p1 = promise.Then(() => { onResolve(); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(ref p1);
            Promise<TConvert> p2 = default(Promise<TConvert>);
            p2 = promise.Then(() => { onResolve(); return convertValue; }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(ref p2);
            Promise p3 = default(Promise);
            p3 = promise.Then(() => { onResolve(); return promiseToPromise(p3); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(ref p3);
            Promise<TConvert> p4 = default(Promise<TConvert>);
            p4 = promise.Then(() => { onResolve(); return promiseToPromiseConvert(p4); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(ref p4);

            Promise p5 = default(Promise);
            p5 = promise.Then(captureValue, cv => { onResolveCapture(cv); onResolve(); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(ref p5);
            Promise<TConvert> p6 = default(Promise<TConvert>);
            p6 = promise.Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return convertValue; }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(ref p6);
            Promise p7 = default(Promise);
            p7 = promise.Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return promiseToPromise(p7); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(ref p7);
            Promise<TConvert> p8 = default(Promise<TConvert>);
            p8 = promise.Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return promiseToPromiseConvert(p8); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(ref p8);

            promise.Forget();
        }

        public static void AddResolveCallbacks<T, TConvert, TCapture>(Promise<T> promise,
            Action<T> onResolve = null, TConvert convertValue = default(TConvert),
            Action<TCapture> onResolveCapture = null, TCapture captureValue = default(TCapture),
            Func<Promise, Promise> promiseToPromise = null, Func<Promise<TConvert>, Promise<TConvert>> promiseToPromiseConvert = null,
            TestAction<Promise> onCallbackAdded = null, TestAction<Promise<TConvert>> onCallbackAddedConvert = null,
            Promise.CanceledAction onCancel = null, Promise.CanceledAction<TCapture> onCancelCapture = null)
        {
            promise = promise.Preserve();
            promise.Catch(() => { }).Forget(); // Suppress any rejections from the preserved promise.

            AddResolveCallbacksWithCancelation(
                promise,
                onResolve, convertValue,
                onResolveCapture, captureValue,
                promiseToPromise, promiseToPromiseConvert,
                onCallbackAdded, onCallbackAddedConvert,
                onCancel: onCancel, onCancelCapture: onCancelCapture
            );

            CancelationSource cancelationSource = CancelationSource.New();
            AddResolveCallbacksWithCancelation(
                promise,
                onResolve, convertValue,
                onResolveCapture, captureValue,
                promiseToPromise, promiseToPromiseConvert,
                onCallbackAdded, onCallbackAddedConvert,
                cancelationSource.Token,
                onCancel: onCancel, onCancelCapture: onCancelCapture
            );
            cancelationSource.Dispose();

            promise.Forget();
        }

        public static void AddResolveCallbacksWithCancelation<T, TConvert, TCapture>(Promise<T> promise,
            Action<T> onResolve = null, TConvert convertValue = default(TConvert),
            Action<TCapture> onResolveCapture = null, TCapture captureValue = default(TCapture),
            Func<Promise, Promise> promiseToPromise = null, Func<Promise<TConvert>, Promise<TConvert>> promiseToPromiseConvert = null,
            TestAction<Promise> onCallbackAdded = null, TestAction<Promise<TConvert>> onCallbackAddedConvert = null,
            CancelationToken cancelationToken = default(CancelationToken),
            Promise.CanceledAction onCancel = null, Promise.CanceledAction<TCapture> onCancelCapture = null)
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
            onCancel += _ => { };
            onCancelCapture += (_, __) => { };

            Promise p1 = default(Promise);
            p1 = promise.Then(x => { onResolve(x); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(ref p1);
            Promise<TConvert> p2 = default(Promise<TConvert>);
            p2 = promise.Then(x => { onResolve(x); return convertValue; }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(ref p2);
            Promise p3 = default(Promise);
            p3 = promise.Then(x => { onResolve(x); return promiseToPromise(p3); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(ref p3);
            Promise<TConvert> p4 = default(Promise<TConvert>);
            p4 = promise.Then(x => { onResolve(x); return promiseToPromiseConvert(p4); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(ref p4);

            Promise p5 = default(Promise);
            p5 = promise.Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(ref p5);
            Promise<TConvert> p6 = default(Promise<TConvert>);
            p6 = promise.Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return convertValue; }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(ref p6);
            Promise p7 = default(Promise);
            p7 = promise.Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return promiseToPromise(p7); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(ref p7);
            Promise<TConvert> p8 = default(Promise<TConvert>);
            p8 = promise.Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return promiseToPromiseConvert(p8); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(ref p8);

            promise.Forget();
        }

        public static void AddCallbacks<TConvert, TReject, TCapture>(Promise promise,
            Action onResolve = null, Action<TReject> onReject = null, Action onUnknownRejection = null, TConvert convertValue = default(TConvert),
            Action<TCapture> onResolveCapture = null, Action<TCapture> onRejectCapture = null, Action<TCapture> onUnknownRejectionCapture = null, TCapture captureValue = default(TCapture),
            Func<Promise, Promise> promiseToPromise = null, Func<Promise<TConvert>, Promise<TConvert>> promiseToPromiseConvert = null,
            TestAction<Promise> onCallbackAdded = null, TestAction<Promise<TConvert>> onCallbackAddedConvert = null,
            Promise.CanceledAction onCancel = null, Promise.CanceledAction<TCapture> onCancelCapture = null)
        {
            promise = promise.Preserve();
            promise.Catch(() => { }).Forget(); // Suppress any rejections from the preserved promise.

            AddCallbacksWithCancelation(
                promise,
                onResolve, onReject, onUnknownRejection, convertValue,
                onResolveCapture, onRejectCapture, onUnknownRejectionCapture, captureValue,
                promiseToPromise, promiseToPromiseConvert,
                onCallbackAdded, onCallbackAddedConvert,
                onCancel: onCancel, onCancelCapture: onCancelCapture
            );

            CancelationSource cancelationSource = CancelationSource.New();
            AddCallbacksWithCancelation(
                promise,
                onResolve, onReject, onUnknownRejection, convertValue,
                onResolveCapture, onRejectCapture, onUnknownRejectionCapture, captureValue,
                promiseToPromise, promiseToPromiseConvert,
                onCallbackAdded, onCallbackAddedConvert,
                cancelationSource.Token,
                onCancel: onCancel, onCancelCapture: onCancelCapture
            );
            cancelationSource.Dispose();

            promise.Forget();
        }

        public static void AddCallbacksWithCancelation<TConvert, TReject, TCapture>(Promise promise,
            Action onResolve = null, Action<TReject> onReject = null, Action onUnknownRejection = null, TConvert convertValue = default(TConvert),
            Action<TCapture> onResolveCapture = null, Action<TCapture> onRejectCapture = null, Action<TCapture> onUnknownRejectionCapture = null, TCapture captureValue = default(TCapture),
            Func<Promise, Promise> promiseToPromise = null, Func<Promise<TConvert>, Promise<TConvert>> promiseToPromiseConvert = null,
            TestAction<Promise> onCallbackAdded = null, TestAction<Promise<TConvert>> onCallbackAddedConvert = null,
            CancelationToken cancelationToken = default(CancelationToken),
            Promise.CanceledAction onCancel = null, Promise.CanceledAction<TCapture> onCancelCapture = null)
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
            onCancel += _ => { };
            onCancelCapture += (_, __) => { };

            Promise p1 = default(Promise);
            p1 = promise.Then(() => { onResolve(); }, () => { onUnknownRejection(); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(ref p1);
            Promise p2 = default(Promise);
            p2 = promise.Then(() => { onResolve(); }, (TReject failValue) => { onReject(failValue); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(ref p2);
            Promise p3 = default(Promise);
            p3 = promise.Then(() => { onResolve(); }, () => { onUnknownRejection(); return promiseToPromise(p3); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(ref p3);
            Promise p4 = default(Promise);
            p4 = promise.Then(() => { onResolve(); }, (TReject failValue) => { onReject(failValue); return promiseToPromise(p4); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(ref p4);

            Promise<TConvert> p5 = default(Promise<TConvert>);
            p5 = promise.Then(() => { onResolve(); return convertValue; }, () => { onUnknownRejection(); return convertValue; }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(ref p5);
            Promise<TConvert> p6 = default(Promise<TConvert>);
            p6 = promise.Then(() => { onResolve(); return convertValue; }, (TReject failValue) => { onReject(failValue); return convertValue; }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(ref p6);
            Promise<TConvert> p7 = default(Promise<TConvert>);
            p7 = promise.Then(() => { onResolve(); return convertValue; }, () => { onUnknownRejection(); return promiseToPromiseConvert(p7); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(ref p7);
            Promise<TConvert> p8 = default(Promise<TConvert>);
            p8 = promise.Then(() => { onResolve(); return convertValue; }, (TReject failValue) => { onReject(failValue); return promiseToPromiseConvert(p8); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(ref p8);

            Promise p9 = default(Promise);
            p9 = promise.Then(() => { onResolve(); return promiseToPromise(p9); }, () => { onUnknownRejection(); return promiseToPromise(p9); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(ref p9);
            Promise p10 = default(Promise);
            p10 = promise.Then(() => { onResolve(); return promiseToPromise(p10); }, (TReject failValue) => { onReject(failValue); return promiseToPromise(p10); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(ref p10);
            Promise p11 = default(Promise);
            p11 = promise.Then(() => { onResolve(); return promiseToPromise(p11); }, () => { onUnknownRejection(); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(ref p11);
            Promise p12 = default(Promise);
            p12 = promise.Then(() => { onResolve(); return promiseToPromise(p12); }, (TReject failValue) => { onReject(failValue); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(ref p12);

            Promise<TConvert> p13 = default(Promise<TConvert>);
            p13 = promise.Then(() => { onResolve(); return promiseToPromiseConvert(p13); }, () => { onUnknownRejection(); return promiseToPromiseConvert(p13); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(ref p13);
            Promise<TConvert> p14 = default(Promise<TConvert>);
            p14 = promise.Then(() => { onResolve(); return promiseToPromiseConvert(p14); }, (TReject failValue) => { onReject(failValue); return promiseToPromiseConvert(p14); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(ref p14);
            Promise<TConvert> p15 = default(Promise<TConvert>);
            p15 = promise.Then(() => { onResolve(); return promiseToPromiseConvert(p15); }, () => { onUnknownRejection(); return convertValue; }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(ref p15);
            Promise<TConvert> p16 = default(Promise<TConvert>);
            p16 = promise.Then(() => { onResolve(); return promiseToPromiseConvert(p16); }, (TReject failValue) => { onReject(failValue); return convertValue; }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(ref p16);

            Promise p17 = default(Promise);
            p17 = promise.Catch(() => { onUnknownRejection(); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(ref p17);
            Promise p18 = default(Promise);
            p18 = promise.Catch((TReject failValue) => { onReject(failValue); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(ref p18);

            Promise p19 = default(Promise);
            p19 = promise.Catch(() => { onUnknownRejection(); return promiseToPromise(p19); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(ref p19);
            Promise p20 = default(Promise);
            p20 = promise.Catch((TReject failValue) => { onReject(failValue); return promiseToPromise(p20); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(ref p20);


            Promise p21 = default(Promise);
            p21 = promise.Then(captureValue, cv => { onResolveCapture(cv); onResolve(); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(ref p21);
            Promise p22 = default(Promise);
            p22 = promise.Then(captureValue, cv => { onResolveCapture(cv); onResolve(); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(ref p22);
            Promise p23 = default(Promise);
            p23 = promise.Then(captureValue, cv => { onResolveCapture(cv); onResolve(); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return promiseToPromise(p23); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(ref p23);
            Promise p24 = default(Promise);
            p24 = promise.Then(captureValue, cv => { onResolveCapture(cv); onResolve(); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return promiseToPromise(p24); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(ref p24);

            Promise<TConvert> p25 = default(Promise<TConvert>);
            p25 = promise.Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return convertValue; }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return convertValue; }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(ref p25);
            Promise<TConvert> p26 = default(Promise<TConvert>);
            p26 = promise.Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return convertValue; }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return convertValue; }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(ref p26);
            Promise<TConvert> p27 = default(Promise<TConvert>);
            p27 = promise.Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return convertValue; }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return promiseToPromiseConvert(p27); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(ref p27);
            Promise<TConvert> p28 = default(Promise<TConvert>);
            p28 = promise.Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return convertValue; }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return promiseToPromiseConvert(p28); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(ref p28);

            Promise p29 = default(Promise);
            p29 = promise.Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return promiseToPromise(p29); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return promiseToPromise(p29); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(ref p29);
            Promise p30 = default(Promise);
            p30 = promise.Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return promiseToPromise(p30); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return promiseToPromise(p30); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(ref p30);
            Promise p31 = default(Promise);
            p31 = promise.Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return promiseToPromise(p31); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(ref p31);
            Promise p32 = default(Promise);
            p32 = promise.Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return promiseToPromise(p32); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(ref p32);

            Promise<TConvert> p33 = default(Promise<TConvert>);
            p33 = promise.Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return promiseToPromiseConvert(p33); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return promiseToPromiseConvert(p33); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(ref p33);
            Promise<TConvert> p34 = default(Promise<TConvert>);
            p34 = promise.Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return promiseToPromiseConvert(p34); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return promiseToPromiseConvert(p34); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(ref p34);
            Promise<TConvert> p35 = default(Promise<TConvert>);
            p35 = promise.Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return promiseToPromiseConvert(p35); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return convertValue; }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(ref p35);
            Promise<TConvert> p36 = default(Promise<TConvert>);
            p36 = promise.Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return promiseToPromiseConvert(p36); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return convertValue; }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(ref p36);

            Promise p37 = default(Promise);
            p37 = promise.Catch(captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(ref p37);
            Promise p38 = default(Promise);
            p38 = promise.Catch(captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(ref p38);

            Promise p39 = default(Promise);
            p39 = promise.Catch(captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return promiseToPromise(p39); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(ref p39);
            Promise p40 = default(Promise);
            p40 = promise.Catch(captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return promiseToPromise(p40); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(ref p40);


            Promise p41 = default(Promise);
            p41 = promise.Then(captureValue, cv => { onResolveCapture(cv); onResolve(); }, () => { onUnknownRejection(); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(ref p41);
            Promise p42 = default(Promise);
            p42 = promise.Then(captureValue, cv => { onResolveCapture(cv); onResolve(); }, (TReject failValue) => { onReject(failValue); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(ref p42);
            Promise p43 = default(Promise);
            p43 = promise.Then(captureValue, cv => { onResolveCapture(cv); onResolve(); }, () => { onUnknownRejection(); return promiseToPromise(p43); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(ref p43);
            Promise p44 = default(Promise);
            p44 = promise.Then(captureValue, cv => { onResolveCapture(cv); onResolve(); }, (TReject failValue) => { onReject(failValue); return promiseToPromise(p44); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(ref p44);

            Promise<TConvert> p45 = default(Promise<TConvert>);
            p45 = promise.Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return convertValue; }, () => { onUnknownRejection(); return convertValue; }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(ref p45);
            Promise<TConvert> p46 = default(Promise<TConvert>);
            p46 = promise.Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return convertValue; }, (TReject failValue) => { onReject(failValue); return convertValue; }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(ref p46);
            Promise<TConvert> p47 = default(Promise<TConvert>);
            p47 = promise.Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return convertValue; }, () => { onUnknownRejection(); return promiseToPromiseConvert(p47); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(ref p47);
            Promise<TConvert> p48 = default(Promise<TConvert>);
            p48 = promise.Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return convertValue; }, (TReject failValue) => { onReject(failValue); return promiseToPromiseConvert(p48); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(ref p48);

            Promise p49 = default(Promise);
            p49 = promise.Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return promiseToPromise(p49); }, () => { onUnknownRejection(); return promiseToPromise(p49); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(ref p49);
            Promise p50 = default(Promise);
            p50 = promise.Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return promiseToPromise(p50); }, (TReject failValue) => { onReject(failValue); return promiseToPromise(p50); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(ref p50);
            Promise p51 = default(Promise);
            p51 = promise.Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return promiseToPromise(p51); }, () => { onUnknownRejection(); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(ref p51);
            Promise p52 = default(Promise);
            p52 = promise.Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return promiseToPromise(p52); }, (TReject failValue) => { onReject(failValue); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(ref p52);

            Promise<TConvert> p53 = default(Promise<TConvert>);
            p53 = promise.Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return promiseToPromiseConvert(p53); }, () => { onUnknownRejection(); return promiseToPromiseConvert(p53); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(ref p53);
            Promise<TConvert> p54 = default(Promise<TConvert>);
            p54 = promise.Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return promiseToPromiseConvert(p54); }, (TReject failValue) => { onReject(failValue); return promiseToPromiseConvert(p54); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(ref p54);
            Promise<TConvert> p55 = default(Promise<TConvert>);
            p55 = promise.Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return promiseToPromiseConvert(p55); }, () => { onUnknownRejection(); return convertValue; }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(ref p55);
            Promise<TConvert> p56 = default(Promise<TConvert>);
            p56 = promise.Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return promiseToPromiseConvert(p56); }, (TReject failValue) => { onReject(failValue); return convertValue; }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(ref p56);


            Promise p57 = default(Promise);
            p57 = promise.Then(() => { onResolve(); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(ref p57);
            Promise p58 = default(Promise);
            p58 = promise.Then(() => { onResolve(); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(ref p58);
            Promise p59 = default(Promise);
            p59 = promise.Then(() => { onResolve(); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return promiseToPromise(p59); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(ref p59);
            Promise p60 = default(Promise);
            p60 = promise.Then(() => { onResolve(); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return promiseToPromise(p60); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(ref p60);

            Promise<TConvert> p61 = default(Promise<TConvert>);
            p61 = promise.Then(() => { onResolve(); return convertValue; }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return convertValue; }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(ref p61);
            Promise<TConvert> p62 = default(Promise<TConvert>);
            p62 = promise.Then(() => { onResolve(); return convertValue; }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return convertValue; }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(ref p62);
            Promise<TConvert> p63 = default(Promise<TConvert>);
            p63 = promise.Then(() => { onResolve(); return convertValue; }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return promiseToPromiseConvert(p63); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(ref p63);
            Promise<TConvert> p64 = default(Promise<TConvert>);
            p64 = promise.Then(() => { onResolve(); return convertValue; }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return promiseToPromiseConvert(p64); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(ref p64);

            Promise p65 = default(Promise);
            p65 = promise.Then(() => { onResolve(); return promiseToPromise(p65); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return promiseToPromise(p65); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(ref p65);
            Promise p66 = default(Promise);
            p66 = promise.Then(() => { onResolve(); return promiseToPromise(p66); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return promiseToPromise(p66); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(ref p66);
            Promise p67 = default(Promise);
            p67 = promise.Then(() => { onResolve(); return promiseToPromise(p67); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(ref p67);
            Promise p68 = default(Promise);
            p68 = promise.Then(() => { onResolve(); return promiseToPromise(p68); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(ref p68);

            Promise<TConvert> p69 = default(Promise<TConvert>);
            p69 = promise.Then(() => { onResolve(); return promiseToPromiseConvert(p69); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return promiseToPromiseConvert(p69); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(ref p69);
            Promise<TConvert> p70 = default(Promise<TConvert>);
            p70 = promise.Then(() => { onResolve(); return promiseToPromiseConvert(p70); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return promiseToPromiseConvert(p70); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(ref p70);
            Promise<TConvert> p71 = default(Promise<TConvert>);
            p71 = promise.Then(() => { onResolve(); return promiseToPromiseConvert(p71); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return convertValue; }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(ref p71);
            Promise<TConvert> p72 = default(Promise<TConvert>);
            p72 = promise.Then(() => { onResolve(); return promiseToPromiseConvert(p72); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return convertValue; }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(ref p72);

            promise.Forget();
        }

        public static void AddCallbacks<T, TConvert, TReject, TCapture>(Promise<T> promise,
            Action<T> onResolve = null, Action<TReject> onReject = null, Action onUnknownRejection = null, TConvert convertValue = default(TConvert), T TValue = default(T),
            Action<TCapture> onResolveCapture = null, Action<TCapture> onRejectCapture = null, Action<TCapture> onUnknownRejectionCapture = null, TCapture captureValue = default(TCapture),
            Func<Promise, Promise> promiseToPromise = null, Func<Promise<TConvert>, Promise<TConvert>> promiseToPromiseConvert = null, Func<Promise<T>, Promise<T>> promiseToPromiseT = null,
            TestAction<Promise> onCallbackAdded = null, TestAction<Promise<TConvert>> onCallbackAddedConvert = null, TestAction<Promise<T>> onCallbackAddedT = null,
            Promise.CanceledAction onCancel = null, Promise.CanceledAction<TCapture> onCancelCapture = null)
        {
            promise = promise.Preserve();
            promise.Catch(() => { }).Forget(); // Suppress any rejections from the preserved promise.

            AddCallbacksWithCancelation(
                promise,
                onResolve, onReject, onUnknownRejection, convertValue, TValue,
                onResolveCapture, onRejectCapture, onUnknownRejectionCapture, captureValue,
                promiseToPromise, promiseToPromiseConvert, promiseToPromiseT,
                onCallbackAdded, onCallbackAddedConvert, onCallbackAddedT,
                onCancel: onCancel, onCancelCapture: onCancelCapture
            );

            CancelationSource cancelationSource = CancelationSource.New();
            AddCallbacksWithCancelation(
                promise,
                onResolve, onReject, onUnknownRejection, convertValue, TValue,
                onResolveCapture, onRejectCapture, onUnknownRejectionCapture, captureValue,
                promiseToPromise, promiseToPromiseConvert, promiseToPromiseT,
                onCallbackAdded, onCallbackAddedConvert, onCallbackAddedT,
                cancelationSource.Token,
                onCancel: onCancel, onCancelCapture: onCancelCapture
            );
            cancelationSource.Dispose();

            promise.Forget();
        }

        public static void AddCallbacksWithCancelation<T, TConvert, TReject, TCapture>(Promise<T> promise,
            Action<T> onResolve = null, Action<TReject> onReject = null, Action onUnknownRejection = null, TConvert convertValue = default(TConvert), T TValue = default(T),
            Action<TCapture> onResolveCapture = null, Action<TCapture> onRejectCapture = null, Action<TCapture> onUnknownRejectionCapture = null, TCapture captureValue = default(TCapture),
            Func<Promise, Promise> promiseToPromise = null, Func<Promise<TConvert>, Promise<TConvert>> promiseToPromiseConvert = null, Func<Promise<T>, Promise<T>> promiseToPromiseT = null,
            TestAction<Promise> onCallbackAdded = null, TestAction<Promise<TConvert>> onCallbackAddedConvert = null, TestAction<Promise<T>> onCallbackAddedT = null,
            CancelationToken cancelationToken = default(CancelationToken),
            Promise.CanceledAction onCancel = null, Promise.CanceledAction<TCapture> onCancelCapture = null)
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
            onCancel += _ => { };
            onCancelCapture += (_, __) => { };

            Promise p1 = default(Promise);
            p1 = promise.Then(x => { onResolve(x); }, () => { onUnknownRejection(); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(ref p1);
            Promise p2 = default(Promise);
            p2 = promise.Then(x => { onResolve(x); }, (TReject failValue) => { onReject(failValue); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(ref p2);
            Promise p3 = default(Promise);
            p3 = promise.Then(x => { onResolve(x); }, () => { onUnknownRejection(); return promiseToPromise(p3); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(ref p3);
            Promise p4 = default(Promise);
            p4 = promise.Then(x => { onResolve(x); }, (TReject failValue) => { onReject(failValue); return promiseToPromise(p4); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(ref p4);

            Promise<TConvert> p5 = default(Promise<TConvert>);
            p5 = promise.Then(x => { onResolve(x); return convertValue; }, () => { onUnknownRejection(); return convertValue; }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(ref p5);
            Promise<TConvert> p6 = default(Promise<TConvert>);
            p6 = promise.Then(x => { onResolve(x); return convertValue; }, (TReject failValue) => { onReject(failValue); return convertValue; }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(ref p6);
            Promise<TConvert> p7 = default(Promise<TConvert>);
            p7 = promise.Then(x => { onResolve(x); return convertValue; }, () => { onUnknownRejection(); return promiseToPromiseConvert(p7); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(ref p7);
            Promise<TConvert> p8 = default(Promise<TConvert>);
            p8 = promise.Then(x => { onResolve(x); return convertValue; }, (TReject failValue) => { onReject(failValue); return promiseToPromiseConvert(p8); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(ref p8);

            Promise p9 = default(Promise);
            p9 = promise.Then(x => { onResolve(x); return promiseToPromise(p9); }, () => { onUnknownRejection(); return promiseToPromise(p9); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(ref p9);
            Promise p10 = default(Promise);
            p10 = promise.Then(x => { onResolve(x); return promiseToPromise(p10); }, (TReject failValue) => { onReject(failValue); return promiseToPromise(p10); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(ref p10);
            Promise p11 = default(Promise);
            p11 = promise.Then(x => { onResolve(x); return promiseToPromise(p11); }, () => { onUnknownRejection(); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(ref p11);
            Promise p12 = default(Promise);
            p12 = promise.Then(x => { onResolve(x); return promiseToPromise(p12); }, (TReject failValue) => { onReject(failValue); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(ref p12);

            Promise<TConvert> p13 = default(Promise<TConvert>);
            p13 = promise.Then(x => { onResolve(x); return promiseToPromiseConvert(p13); }, () => { onUnknownRejection(); return promiseToPromiseConvert(p13); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(ref p13);
            Promise<TConvert> p14 = default(Promise<TConvert>);
            p14 = promise.Then(x => { onResolve(x); return promiseToPromiseConvert(p14); }, (TReject failValue) => { onReject(failValue); return promiseToPromiseConvert(p14); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(ref p14);
            Promise<TConvert> p15 = default(Promise<TConvert>);
            p15 = promise.Then(x => { onResolve(x); return promiseToPromiseConvert(p15); }, () => { onUnknownRejection(); return convertValue; }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(ref p15);
            Promise<TConvert> p16 = default(Promise<TConvert>);
            p16 = promise.Then(x => { onResolve(x); return promiseToPromiseConvert(p16); }, (TReject failValue) => { onReject(failValue); return convertValue; }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(ref p16);

            Promise<T> p17 = default(Promise<T>);
            p17 = promise.Catch(() => { onUnknownRejection(); return TValue; }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedT(ref p17);
            Promise<T> p18 = default(Promise<T>);
            p18 = promise.Catch((TReject failValue) => { onReject(failValue); return TValue; }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedT(ref p18);

            Promise<T> p19 = default(Promise<T>);
            p19 = promise.Catch(() => { onUnknownRejection(); return promiseToPromiseT(p19); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedT(ref p19);
            Promise<T> p20 = default(Promise<T>);
            p20 = promise.Catch((TReject failValue) => { onReject(failValue); return promiseToPromiseT(p20); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedT(ref p20);


            Promise p21 = default(Promise);
            p21 = promise.Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(ref p21);
            Promise p22 = default(Promise);
            p22 = promise.Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(ref p22);
            Promise p23 = default(Promise);
            p23 = promise.Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return promiseToPromise(p23); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(ref p23);
            Promise p24 = default(Promise);
            p24 = promise.Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return promiseToPromise(p24); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(ref p24);

            Promise<TConvert> p25 = default(Promise<TConvert>);
            p25 = promise.Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return convertValue; }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return convertValue; }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(ref p25);
            Promise<TConvert> p26 = default(Promise<TConvert>);
            p26 = promise.Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return convertValue; }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return convertValue; }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(ref p26);
            Promise<TConvert> p27 = default(Promise<TConvert>);
            p27 = promise.Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return convertValue; }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return promiseToPromiseConvert(p27); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(ref p27);
            Promise<TConvert> p28 = default(Promise<TConvert>);
            p28 = promise.Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return convertValue; }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return promiseToPromiseConvert(p28); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(ref p28);

            Promise p29 = default(Promise);
            p29 = promise.Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return promiseToPromise(p29); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return promiseToPromise(p29); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(ref p29);
            Promise p30 = default(Promise);
            p30 = promise.Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return promiseToPromise(p30); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return promiseToPromise(p30); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(ref p30);
            Promise p31 = default(Promise);
            p31 = promise.Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return promiseToPromise(p31); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(ref p31);
            Promise p32 = default(Promise);
            p32 = promise.Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return promiseToPromise(p32); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(ref p32);

            Promise<TConvert> p33 = default(Promise<TConvert>);
            p33 = promise.Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return promiseToPromiseConvert(p33); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return promiseToPromiseConvert(p33); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(ref p33);
            Promise<TConvert> p34 = default(Promise<TConvert>);
            p34 = promise.Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return promiseToPromiseConvert(p34); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return promiseToPromiseConvert(p34); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(ref p34);
            Promise<TConvert> p35 = default(Promise<TConvert>);
            p35 = promise.Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return promiseToPromiseConvert(p35); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return convertValue; }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(ref p35);
            Promise<TConvert> p36 = default(Promise<TConvert>);
            p36 = promise.Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return promiseToPromiseConvert(p36); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return convertValue; }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(ref p36);

            Promise<T> p37 = default(Promise<T>);
            p37 = promise.Catch(captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return TValue; }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedT(ref p37);
            Promise<T> p38 = default(Promise<T>);
            p38 = promise.Catch(captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return TValue; }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedT(ref p38);

            Promise<T> p39 = default(Promise<T>);
            p39 = promise.Catch(captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return promiseToPromiseT(p39); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedT(ref p39);
            Promise<T> p40 = default(Promise<T>);
            p40 = promise.Catch(captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return promiseToPromiseT(p40); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedT(ref p40);


            Promise p41 = default(Promise);
            p41 = promise.Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); }, () => { onUnknownRejection(); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(ref p41);
            Promise p42 = default(Promise);
            p42 = promise.Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); }, (TReject failValue) => { onReject(failValue); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(ref p42);
            Promise p43 = default(Promise);
            p43 = promise.Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); }, () => { onUnknownRejection(); return promiseToPromise(p43); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(ref p43);
            Promise p44 = default(Promise);
            p44 = promise.Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); }, (TReject failValue) => { onReject(failValue); return promiseToPromise(p44); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(ref p44);

            Promise<TConvert> p45 = default(Promise<TConvert>);
            p45 = promise.Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return convertValue; }, () => { onUnknownRejection(); return convertValue; }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(ref p45);
            Promise<TConvert> p46 = default(Promise<TConvert>);
            p46 = promise.Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return convertValue; }, (TReject failValue) => { onReject(failValue); return convertValue; }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(ref p46);
            Promise<TConvert> p47 = default(Promise<TConvert>);
            p47 = promise.Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return convertValue; }, () => { onUnknownRejection(); return promiseToPromiseConvert(p47); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(ref p47);
            Promise<TConvert> p48 = default(Promise<TConvert>);
            p48 = promise.Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return convertValue; }, (TReject failValue) => { onReject(failValue); return promiseToPromiseConvert(p48); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(ref p48);

            Promise p49 = default(Promise);
            p49 = promise.Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return promiseToPromise(p49); }, () => { onUnknownRejection(); return promiseToPromise(p49); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(ref p49);
            Promise p50 = default(Promise);
            p50 = promise.Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return promiseToPromise(p50); }, (TReject failValue) => { onReject(failValue); return promiseToPromise(p50); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(ref p50);
            Promise p51 = default(Promise);
            p51 = promise.Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return promiseToPromise(p51); }, () => { onUnknownRejection(); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(ref p51);
            Promise p52 = default(Promise);
            p52 = promise.Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return promiseToPromise(p52); }, (TReject failValue) => { onReject(failValue); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(ref p52);

            Promise<TConvert> p53 = default(Promise<TConvert>);
            p53 = promise.Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return promiseToPromiseConvert(p53); }, () => { onUnknownRejection(); return promiseToPromiseConvert(p53); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(ref p53);
            Promise<TConvert> p54 = default(Promise<TConvert>);
            p54 = promise.Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return promiseToPromiseConvert(p54); }, (TReject failValue) => { onReject(failValue); return promiseToPromiseConvert(p54); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(ref p54);
            Promise<TConvert> p55 = default(Promise<TConvert>);
            p55 = promise.Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return promiseToPromiseConvert(p55); }, () => { onUnknownRejection(); return convertValue; }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(ref p55);
            Promise<TConvert> p56 = default(Promise<TConvert>);
            p56 = promise.Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return promiseToPromiseConvert(p56); }, (TReject failValue) => { onReject(failValue); return convertValue; }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(ref p56);


            Promise p57 = default(Promise);
            p57 = promise.Then(x => { onResolve(x); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(ref p57);
            Promise p58 = default(Promise);
            p58 = promise.Then(x => { onResolve(x); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(ref p58);
            Promise p59 = default(Promise);
            p59 = promise.Then(x => { onResolve(x); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return promiseToPromise(p59); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(ref p59);
            Promise p60 = default(Promise);
            p60 = promise.Then(x => { onResolve(x); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return promiseToPromise(p60); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(ref p60);

            Promise<TConvert> p61 = default(Promise<TConvert>);
            p61 = promise.Then(x => { onResolve(x); return convertValue; }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return convertValue; }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(ref p61);
            Promise<TConvert> p62 = default(Promise<TConvert>);
            p62 = promise.Then(x => { onResolve(x); return convertValue; }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return convertValue; }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(ref p62);
            Promise<TConvert> p63 = default(Promise<TConvert>);
            p63 = promise.Then(x => { onResolve(x); return convertValue; }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return promiseToPromiseConvert(p63); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(ref p63);
            Promise<TConvert> p64 = default(Promise<TConvert>);
            p64 = promise.Then(x => { onResolve(x); return convertValue; }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return promiseToPromiseConvert(p64); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(ref p64);

            Promise p65 = default(Promise);
            p65 = promise.Then(x => { onResolve(x); return promiseToPromise(p65); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return promiseToPromise(p65); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(ref p65);
            Promise p66 = default(Promise);
            p66 = promise.Then(x => { onResolve(x); return promiseToPromise(p66); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return promiseToPromise(p66); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(ref p66);
            Promise p67 = default(Promise);
            p67 = promise.Then(x => { onResolve(x); return promiseToPromise(p67); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(ref p67);
            Promise p68 = default(Promise);
            p68 = promise.Then(x => { onResolve(x); return promiseToPromise(p68); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(ref p68);

            Promise<TConvert> p69 = default(Promise<TConvert>);
            p69 = promise.Then(x => { onResolve(x); return promiseToPromiseConvert(p69); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return promiseToPromiseConvert(p69); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(ref p69);
            Promise<TConvert> p70 = default(Promise<TConvert>);
            p70 = promise.Then(x => { onResolve(x); return promiseToPromiseConvert(p70); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return promiseToPromiseConvert(p70); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(ref p70);
            Promise<TConvert> p71 = default(Promise<TConvert>);
            p71 = promise.Then(x => { onResolve(x); return promiseToPromiseConvert(p71); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return convertValue; }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(ref p71);
            Promise<TConvert> p72 = default(Promise<TConvert>);
            p72 = promise.Then(x => { onResolve(x); return promiseToPromiseConvert(p72); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return convertValue; }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(ref p72);

            promise.Forget();
        }

        public static void AddContinueCallbacks<TConvert, TCapture>(Promise promise,
            Promise.ContinueAction onContinue = null, TConvert convertValue = default(TConvert),
            Promise.ContinueAction<TCapture> onContinueCapture = null, TCapture captureValue = default(TCapture),
            Func<Promise, Promise> promiseToPromise = null, Func<Promise<TConvert>, Promise<TConvert>> promiseToPromiseConvert = null,
            TestAction<Promise> onCallbackAdded = null, TestAction<Promise<TConvert>> onCallbackAddedConvert = null,
            Promise.CanceledAction onCancel = null, Promise.CanceledAction<TCapture> onCancelCapture = null)
        {
            promise = promise.Preserve();
            promise.Catch(() => { }).Forget(); // Suppress any rejections from the preserved promise.

            AddContinueCallbacksWithCancelation(
                promise,
                onContinue, convertValue,
                onContinueCapture, captureValue,
                promiseToPromise, promiseToPromiseConvert,
                onCallbackAdded, onCallbackAddedConvert,
                onCancel: onCancel, onCancelCapture: onCancelCapture
            );

            CancelationSource cancelationSource = CancelationSource.New();
            AddContinueCallbacksWithCancelation(
                promise,
                onContinue, convertValue,
                onContinueCapture, captureValue,
                promiseToPromise, promiseToPromiseConvert,
                onCallbackAdded, onCallbackAddedConvert,
                cancelationSource.Token,
                onCancel: onCancel, onCancelCapture: onCancelCapture
            );
            cancelationSource.Dispose();

            promise.Forget();
        }

        public static void AddContinueCallbacksWithCancelation<TConvert, TCapture>(Promise promise,
            Promise.ContinueAction onContinue = null, TConvert convertValue = default(TConvert),
            Promise.ContinueAction<TCapture> onContinueCapture = null, TCapture captureValue = default(TCapture),
            Func<Promise, Promise> promiseToPromise = null, Func<Promise<TConvert>, Promise<TConvert>> promiseToPromiseConvert = null,
            TestAction<Promise> onCallbackAdded = null, TestAction<Promise<TConvert>> onCallbackAddedConvert = null,
            CancelationToken cancelationToken = default(CancelationToken),
            Promise.CanceledAction onCancel = null, Promise.CanceledAction<TCapture> onCancelCapture = null)
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
            onCancel += _ => { };
            onCancelCapture += (_, __) => { };

            Promise p1 = default(Promise);
            p1 = promise.ContinueWith(r => { onContinue(r); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(ref p1);
            Promise<TConvert> p2 = default(Promise<TConvert>);
            p2 = promise.ContinueWith(r => { onContinue(r); return convertValue; }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(ref p2);
            Promise p3 = default(Promise);
            p3 = promise.ContinueWith(r => { onContinue(r); return promiseToPromise(p3); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(ref p3);
            Promise<TConvert> p4 = default(Promise<TConvert>);
            p4 = promise.ContinueWith(r => { onContinue(r); return promiseToPromiseConvert(p4); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(ref p4);

            Promise p5 = default(Promise);
            p5 = promise.ContinueWith(captureValue, (cv, r) => { onContinueCapture(cv, r); onContinue(r); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(ref p5);
            Promise<TConvert> p6 = default(Promise<TConvert>);
            p6 = promise.ContinueWith(captureValue, (cv, r) => { onContinueCapture(cv, r); onContinue(r); return convertValue; }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(ref p6);
            Promise p7 = default(Promise);
            p7 = promise.ContinueWith(captureValue, (cv, r) => { onContinueCapture(cv, r); onContinue(r); return promiseToPromise(p7); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(ref p7);
            Promise<TConvert> p8 = default(Promise<TConvert>);
            p8 = promise.ContinueWith(captureValue, (cv, r) => { onContinueCapture(cv, r); onContinue(r); return promiseToPromiseConvert(p8); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(ref p8);

            promise.Forget();
        }

        public static void AddContinueCallbacks<T, TConvert, TCapture>(Promise<T> promise, Promise<T>.ContinueAction onContinue = null, TConvert convertValue = default(TConvert),
            Promise<T>.ContinueAction<TCapture> onContinueCapture = null, TCapture captureValue = default(TCapture),
            Func<Promise, Promise> promiseToPromise = null, Func<Promise<TConvert>, Promise<TConvert>> promiseToPromiseConvert = null,
            TestAction<Promise> onCallbackAdded = null, TestAction<Promise<TConvert>> onCallbackAddedConvert = null,
            Promise.CanceledAction onCancel = null, Promise.CanceledAction<TCapture> onCancelCapture = null)
        {
            promise = promise.Preserve();
            promise.Catch(() => { }).Forget(); // Suppress any rejections from the preserved promise.

            AddContinueCallbacksWithCancelation(
                promise,
                onContinue, convertValue,
                onContinueCapture, captureValue,
                promiseToPromise, promiseToPromiseConvert,
                onCallbackAdded, onCallbackAddedConvert,
                onCancel: onCancel, onCancelCapture: onCancelCapture
            );

            CancelationSource cancelationSource = CancelationSource.New();
            AddContinueCallbacksWithCancelation(
                promise,
                onContinue, convertValue,
                onContinueCapture, captureValue,
                promiseToPromise, promiseToPromiseConvert,
                onCallbackAdded, onCallbackAddedConvert,
                cancelationSource.Token,
                onCancel: onCancel, onCancelCapture: onCancelCapture
            );
            cancelationSource.Dispose();

            promise.Forget();
        }

        public static void AddContinueCallbacksWithCancelation<T, TConvert, TCapture>(Promise<T> promise, Promise<T>.ContinueAction onContinue = null, TConvert convertValue = default(TConvert),
            Promise<T>.ContinueAction<TCapture> onContinueCapture = null, TCapture captureValue = default(TCapture),
            Func<Promise, Promise> promiseToPromise = null, Func<Promise<TConvert>, Promise<TConvert>> promiseToPromiseConvert = null,
            TestAction<Promise> onCallbackAdded = null, TestAction<Promise<TConvert>> onCallbackAddedConvert = null,
            CancelationToken cancelationToken = default(CancelationToken),
            Promise.CanceledAction onCancel = null, Promise.CanceledAction<TCapture> onCancelCapture = null)
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
            onCancel += _ => { };
            onCancelCapture += (_, __) => { };

            Promise p1 = default(Promise);
            p1 = promise.ContinueWith(new Promise<T>.ContinueAction(r => { onContinue(r); }), cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(ref p1);
            Promise<TConvert> p2 = default(Promise<TConvert>);
            p2 = promise.ContinueWith(new Promise<T>.ContinueFunc<TConvert>(r => { onContinue(r); return convertValue; }), cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(ref p2);
            Promise p3 = default(Promise);
            p3 = promise.ContinueWith(new Promise<T>.ContinueFunc<Promise>(r => { onContinue(r); return promiseToPromise(p3); }), cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(ref p3);
            Promise<TConvert> p4 = default(Promise<TConvert>);
            p4 = promise.ContinueWith(new Promise<T>.ContinueFunc<Promise<TConvert>>(r => { onContinue(r); return promiseToPromiseConvert(p4); }), cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(ref p4);

            Promise p5 = default(Promise);
            p5 = promise.ContinueWith(captureValue, new Promise<T>.ContinueAction<TCapture>((cv, r) => { onContinueCapture(cv, r); onContinue(r); }), cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(ref p5);
            Promise<TConvert> p6 = default(Promise<TConvert>);
            p6 = promise.ContinueWith(captureValue, new Promise<T>.ContinueFunc<TCapture, TConvert>((cv, r) => { onContinueCapture(cv, r); onContinue(r); return convertValue; }), cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(ref p6);
            Promise p7 = default(Promise);
            p7 = promise.ContinueWith(captureValue, new Promise<T>.ContinueFunc<TCapture, Promise>((cv, r) => { onContinueCapture(cv, r); onContinue(r); return promiseToPromise(p7); }), cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(ref p7);
            Promise<TConvert> p8 = default(Promise<TConvert>);
            p8 = promise.ContinueWith(captureValue, new Promise<T>.ContinueFunc<TCapture, Promise<TConvert>>((cv, r) => { onContinueCapture(cv, r); onContinue(r); return promiseToPromiseConvert(p8); }), cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(ref p8);

            promise.Forget();
        }
    }
}