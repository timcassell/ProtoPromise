﻿#if !PROTO_PROMISE_PROGRESS_DISABLE
#define PROMISE_PROGRESS
#else
#undef PROMISE_PROGRESS
#endif

#pragma warning disable IDE0034 // Simplify 'default' expression

using System;
#if UNITY_8_4_OR_NEWER
using UnityEngine.TestTools;
#endif

namespace Proto.Promises.Tests
{
    // These help test all Then/Catch functions at once.
    public static class TestHelper
    {
        public static void Cleanup()
        {
            Promise.Manager.HandleCompletesAndProgress();
            GC.Collect();
            Promise.Manager.HandleCompletesAndProgress();
#if UNITY_8_4_OR_NEWER
            LogAssert.NoUnexpectedReceived();
#endif
        }

        public static void ExpectWarning(string message)
        {
#if UNITY_8_4_OR_NEWER
            LogAssert.Expect(UnityEngine.LogType.Warning, message);
#endif
        }

        public static Promise ThenDuplicate(this Promise promise)
        {
            return promise.Then(() => { });
        }

        public static Promise<T> ThenDuplicate<T>(this Promise<T> promise)
        {
            return promise.Then(v => v);
        }

        public static Action<UnhandledException> cachedRejectionHandler;

#if PROMISE_PROGRESS
        public static readonly double progressEpsilon = 1d / Math.Pow(2d, Promise.Config.ProgressDecimalBits);
#endif

        public const int resolveVoidCallbacks = 72;
        public const int resolveTCallbacks = 72;
        public const int rejectVoidCallbacks = 72;
        public const int rejectTCallbacks = 72;

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
            Action<Promise> promiseToVoid = null, Func<Promise<TConvert>, TConvert> promiseToConvert = null,
            Func<Promise, Promise> promiseToPromise = null, Func<Promise<TConvert>, Promise<TConvert>> promiseToPromiseConvert = null)
        {
            AddResolveCallbacksWithCancelation(
                promise,
                onResolve, convertValue,
                onResolveCapture, captureValue,
                promiseToVoid, promiseToConvert,
                promiseToPromise, promiseToPromiseConvert
            );

            CancelationSource cancelationSource = CancelationSource.New();
            AddResolveCallbacksWithCancelation(
                promise,
                onResolve, convertValue,
                onResolveCapture, captureValue,
                promiseToVoid, promiseToConvert,
                promiseToPromise, promiseToPromiseConvert,
                cancelationSource.Token
            );
            cancelationSource.Dispose();
        }

        public static void AddResolveCallbacksWithCancelation<TConvert, TCapture>(Promise promise,
            Action onResolve = null, TConvert convertValue = default(TConvert),
            Action<TCapture> onResolveCapture = null, TCapture captureValue = default(TCapture),
            Action<Promise> promiseToVoid = null, Func<Promise<TConvert>, TConvert> promiseToConvert = null,
            Func<Promise, Promise> promiseToPromise = null, Func<Promise<TConvert>, Promise<TConvert>> promiseToPromiseConvert = null,
            CancelationToken cancelationToken = default(CancelationToken),
            Action<ReasonContainer> onCancel = null, Action<TCapture, ReasonContainer> onCancelCapture = null)
        {
            // Add empty delegates so no need for null check.
            onResolve += () => { };
            onResolveCapture += _ => { };
            promiseToVoid += _ => { };
            if (promiseToConvert == null)
            {
                promiseToConvert += _ => convertValue;
            }
            if (promiseToPromise == null)
            {
                promiseToPromise = _ => Promise.Resolved();
            }
            if (promiseToPromiseConvert == null)
            {
                promiseToPromiseConvert = _ => Promise.Resolved(convertValue);
            }
            onCancel += _ => { };
            onCancelCapture += (_, __) => { };

            Promise p1 = null;
            p1 = promise.Then(() => { onResolve(); promiseToVoid(p1); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            Promise<TConvert> p2 = null;
            p2 = promise.Then(() => { onResolve(); return promiseToConvert(p2); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            Promise p3 = null;
            p3 = promise.Then(() => { onResolve(); return promiseToPromise(p3); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            Promise<TConvert> p4 = null;
            p4 = promise.Then(() => { onResolve(); return promiseToPromiseConvert(p4); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);

            Promise p5 = null;
            p5 = promise.Then(captureValue, cv => { onResolveCapture(cv); onResolve(); promiseToVoid(p5); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            Promise<TConvert> p6 = null;
            p6 = promise.Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return promiseToConvert(p6); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            Promise p7 = null;
            p7 = promise.Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return promiseToPromise(p7); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            Promise<TConvert> p8 = null;
            p8 = promise.Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return promiseToPromiseConvert(p8); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
        }

        public static void AddResolveCallbacks<T, TConvert, TCapture>(Promise<T> promise,
            Action<T> onResolve = null, TConvert convertValue = default(TConvert),
            Action<TCapture> onResolveCapture = null, TCapture captureValue = default(TCapture),
            Action<Promise> promiseToVoid = null, Func<Promise<TConvert>, TConvert> promiseToConvert = null,
            Func<Promise, Promise> promiseToPromise = null, Func<Promise<TConvert>, Promise<TConvert>> promiseToPromiseConvert = null)
        {
            AddResolveCallbacksWithCancelation(
                promise,
                onResolve, convertValue,
                onResolveCapture, captureValue,
                promiseToVoid, promiseToConvert,
                promiseToPromise, promiseToPromiseConvert
            );

            CancelationSource cancelationSource = CancelationSource.New();
            AddResolveCallbacksWithCancelation(
                promise,
                onResolve, convertValue,
                onResolveCapture, captureValue,
                promiseToVoid, promiseToConvert,
                promiseToPromise, promiseToPromiseConvert,
                cancelationSource.Token
            );
            cancelationSource.Dispose();
        }

        public static void AddResolveCallbacksWithCancelation<T, TConvert, TCapture>(Promise<T> promise,
            Action<T> onResolve = null, TConvert convertValue = default(TConvert),
            Action<TCapture> onResolveCapture = null, TCapture captureValue = default(TCapture),
            Action<Promise> promiseToVoid = null, Func<Promise<TConvert>, TConvert> promiseToConvert = null,
            Func<Promise, Promise> promiseToPromise = null, Func<Promise<TConvert>, Promise<TConvert>> promiseToPromiseConvert = null,
            CancelationToken cancelationToken = default(CancelationToken),
            Action<ReasonContainer> onCancel = null, Action<TCapture, ReasonContainer> onCancelCapture = null)
        {
            // Add empty delegates so no need for null check.
            onResolve += _ => { };
            onResolveCapture += _ => { };
            promiseToVoid += _ => { };
            if (promiseToConvert == null)
            {
                promiseToConvert += _ => convertValue;
            }
            if (promiseToPromise == null)
            {
                promiseToPromise = _ => Promise.Resolved();
            }
            if (promiseToPromiseConvert == null)
            {
                promiseToPromiseConvert = _ => Promise.Resolved(convertValue);
            }
            onCancel += _ => { };
            onCancelCapture += (_, __) => { };

            Promise p1 = null;
            p1 = promise.Then(x => { onResolve(x); promiseToVoid(p1); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            Promise<TConvert> p2 = null;
            p2 = promise.Then(x => { onResolve(x); return promiseToConvert(p2); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            Promise p3 = null;
            p3 = promise.Then(x => { onResolve(x); return promiseToPromise(p3); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            Promise<TConvert> p4 = null;
            p4 = promise.Then(x => { onResolve(x); return promiseToPromiseConvert(p4); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);

            Promise p5 = null;
            p5 = promise.Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); promiseToVoid(p5); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            Promise<TConvert> p6 = null;
            p6 = promise.Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return promiseToConvert(p6); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            Promise p7 = null;
            p7 = promise.Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return promiseToPromise(p7); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            Promise<TConvert> p8 = null;
            p8 = promise.Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return promiseToPromiseConvert(p8); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
        }

        public static void AddCallbacks<TConvert, TReject, TCapture>(Promise promise,
            Action onResolve = null, Action<TReject> onReject = null, Action onUnknownRejection = null, TConvert convertValue = default(TConvert),
            Action<TCapture> onResolveCapture = null, Action<TCapture> onRejectCapture = null, Action<TCapture> onUnknownRejectionCapture = null, TCapture captureValue = default(TCapture),
            Action<Promise> promiseToVoid = null, Func<Promise<TConvert>, TConvert> promiseToConvert = null,
            Func<Promise, Promise> promiseToPromise = null, Func<Promise<TConvert>, Promise<TConvert>> promiseToPromiseConvert = null,
            Action<Promise> onCallbackAdded = null, Action<Promise<TConvert>> onCallbackAddedConvert = null)
        {
            AddCallbacksWithCancelation(
                promise,
                onResolve, onReject, onUnknownRejection, convertValue,
                onResolveCapture, onRejectCapture, onUnknownRejectionCapture, captureValue,
                promiseToVoid, promiseToConvert,
                promiseToPromise, promiseToPromiseConvert,
                onCallbackAdded, onCallbackAddedConvert
            );

            CancelationSource cancelationSource = CancelationSource.New();
            AddCallbacksWithCancelation(
                promise,
                onResolve, onReject, onUnknownRejection, convertValue,
                onResolveCapture, onRejectCapture, onUnknownRejectionCapture, captureValue,
                promiseToVoid, promiseToConvert,
                promiseToPromise, promiseToPromiseConvert,
                onCallbackAdded, onCallbackAddedConvert,
                cancelationSource.Token
            );
            cancelationSource.Dispose();
        }

        public static void AddCallbacksWithCancelation<TConvert, TReject, TCapture>(Promise promise,
            Action onResolve = null, Action<TReject> onReject = null, Action onUnknownRejection = null, TConvert convertValue = default(TConvert),
            Action<TCapture> onResolveCapture = null, Action<TCapture> onRejectCapture = null, Action<TCapture> onUnknownRejectionCapture = null, TCapture captureValue = default(TCapture),
            Action<Promise> promiseToVoid = null, Func<Promise<TConvert>, TConvert> promiseToConvert = null,
            Func<Promise, Promise> promiseToPromise = null, Func<Promise<TConvert>, Promise<TConvert>> promiseToPromiseConvert = null,
            Action<Promise> onCallbackAdded = null, Action<Promise<TConvert>> onCallbackAddedConvert = null,
            CancelationToken cancelationToken = default(CancelationToken),
            Action<ReasonContainer> onCancel = null, Action<TCapture, ReasonContainer> onCancelCapture = null)
        {
            // Add empty delegates so no need for null check.
            onResolve += () => { };
            onReject += s => { };
            onUnknownRejection += () => { };
            onResolveCapture += _ => { };
            onRejectCapture += _ => { };
            onUnknownRejectionCapture += _ => { };
            promiseToVoid += _ => { };
            onCallbackAdded += _ => { };
            onCallbackAddedConvert += _ => { };
            if (promiseToConvert == null)
            {
                promiseToConvert += _ => convertValue;
            }
            if (promiseToPromise == null)
            {
                promiseToPromise = _ => Promise.Resolved();
            }
            if (promiseToPromiseConvert == null)
            {
                promiseToPromiseConvert = _ => Promise.Resolved(convertValue);
            }
            onCancel += _ => { };
            onCancelCapture += (_, __) => { };

            Promise p1 = null;
            p1 = promise.Then(() => { onResolve(); promiseToVoid(p1); }, () => { onUnknownRejection(); promiseToVoid(p1); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(p1);
            Promise p2 = null;
            p2 = promise.Then(() => { onResolve(); promiseToVoid(p2); }, (TReject failValue) => { onReject(failValue); promiseToVoid(p2); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(p2);
            Promise p3 = null;
            p3 = promise.Then(() => { onResolve(); promiseToVoid(p3); }, () => { onUnknownRejection(); return promiseToPromise(p3); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(p3);
            Promise p4 = null;
            p4 = promise.Then(() => { onResolve(); promiseToVoid(p4); }, (TReject failValue) => { onReject(failValue); return promiseToPromise(p4); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(p4);

            Promise<TConvert> p5 = null;
            p5 = promise.Then(() => { onResolve(); return promiseToConvert(p5); }, () => { onUnknownRejection(); return promiseToConvert(p5); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(p5);
            Promise<TConvert> p6 = null;
            p6 = promise.Then(() => { onResolve(); return promiseToConvert(p6); }, (TReject failValue) => { onReject(failValue); return promiseToConvert(p6); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(p6);
            Promise<TConvert> p7 = null;
            p7 = promise.Then(() => { onResolve(); return promiseToConvert(p7); }, () => { onUnknownRejection(); return promiseToPromiseConvert(p7); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(p7);
            Promise<TConvert> p8 = null;
            p8 = promise.Then(() => { onResolve(); return promiseToConvert(p8); }, (TReject failValue) => { onReject(failValue); return promiseToPromiseConvert(p8); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(p8);

            Promise p9 = null;
            p9 = promise.Then(() => { onResolve(); return promiseToPromise(p9); }, () => { onUnknownRejection(); return promiseToPromise(p9); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(p9);
            Promise p10 = null;
            p10 = promise.Then(() => { onResolve(); return promiseToPromise(p10); }, (TReject failValue) => { onReject(failValue); return promiseToPromise(p10); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(p10);
            Promise p11 = null;
            p11 = promise.Then(() => { onResolve(); return promiseToPromise(p11); }, () => { onUnknownRejection(); promiseToVoid(p11); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(p11);
            Promise p12 = null;
            p12 = promise.Then(() => { onResolve(); return promiseToPromise(p12); }, (TReject failValue) => { onReject(failValue); promiseToVoid(p12); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(p12);

            Promise<TConvert> p13 = null;
            p13 = promise.Then(() => { onResolve(); return promiseToPromiseConvert(p13); }, () => { onUnknownRejection(); return promiseToPromiseConvert(p13); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(p13);
            Promise<TConvert> p14 = null;
            p14 = promise.Then(() => { onResolve(); return promiseToPromiseConvert(p14); }, (TReject failValue) => { onReject(failValue); return promiseToPromiseConvert(p14); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(p14);
            Promise<TConvert> p15 = null;
            p15 = promise.Then(() => { onResolve(); return promiseToPromiseConvert(p15); }, () => { onUnknownRejection(); return promiseToConvert(p15); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(p15);
            Promise<TConvert> p16 = null;
            p16 = promise.Then(() => { onResolve(); return promiseToPromiseConvert(p16); }, (TReject failValue) => { onReject(failValue); return promiseToConvert(p16); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(p16);

            Promise p17 = null;
            p17 = promise.Catch(() => { onUnknownRejection(); promiseToVoid(p17); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(p17);
            Promise p18 = null;
            p18 = promise.Catch((TReject failValue) => { onReject(failValue); promiseToVoid(p18); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(p18);

            Promise p19 = null;
            p19 = promise.Catch(() => { onUnknownRejection(); return promiseToPromise(p19); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(p19);
            Promise p20 = null;
            p20 = promise.Catch((TReject failValue) => { onReject(failValue); return promiseToPromise(p20); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(p20);


            Promise p21 = null;
            p21 = promise.Then(captureValue, cv => { onResolveCapture(cv); onResolve(); promiseToVoid(p21); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); promiseToVoid(p21); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(p21);
            Promise p22 = null;
            p22 = promise.Then(captureValue, cv => { onResolveCapture(cv); onResolve(); promiseToVoid(p22); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); promiseToVoid(p22); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(p22);
            Promise p23 = null;
            p23 = promise.Then(captureValue, cv => { onResolveCapture(cv); onResolve(); promiseToVoid(p23); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return promiseToPromise(p23); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(p23);
            Promise p24 = null;
            p24 = promise.Then(captureValue, cv => { onResolveCapture(cv); onResolve(); promiseToVoid(p24); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return promiseToPromise(p24); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(p24);

            Promise<TConvert> p25 = null;
            p25 = promise.Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return promiseToConvert(p25); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return promiseToConvert(p25); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(p25);
            Promise<TConvert> p26 = null;
            p26 = promise.Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return promiseToConvert(p26); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return promiseToConvert(p26); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(p26);
            Promise<TConvert> p27 = null;
            p27 = promise.Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return promiseToConvert(p27); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return promiseToPromiseConvert(p27); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(p7);
            Promise<TConvert> p28 = null;
            p28 = promise.Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return promiseToConvert(p28); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return promiseToPromiseConvert(p28); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(p28);

            Promise p29 = null;
            p29 = promise.Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return promiseToPromise(p29); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return promiseToPromise(p29); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(p29);
            Promise p30 = null;
            p30 = promise.Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return promiseToPromise(p30); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return promiseToPromise(p30); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(p30);
            Promise p31 = null;
            p31 = promise.Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return promiseToPromise(p31); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); promiseToVoid(p31); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(p31);
            Promise p32 = null;
            p32 = promise.Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return promiseToPromise(p32); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); promiseToVoid(p32); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(p32);

            Promise<TConvert> p33 = null;
            p33 = promise.Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return promiseToPromiseConvert(p33); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return promiseToPromiseConvert(p33); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(p33);
            Promise<TConvert> p34 = null;
            p34 = promise.Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return promiseToPromiseConvert(p34); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return promiseToPromiseConvert(p34); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(p34);
            Promise<TConvert> p35 = null;
            p35 = promise.Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return promiseToPromiseConvert(p35); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return promiseToConvert(p35); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(p35);
            Promise<TConvert> p36 = null;
            p36 = promise.Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return promiseToPromiseConvert(p36); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return promiseToConvert(p36); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(p36);

            Promise p37 = null;
            p37 = promise.Catch(captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); promiseToVoid(p37); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(p37);
            Promise p38 = null;
            p38 = promise.Catch(captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); promiseToVoid(p38); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(p38);

            Promise p39 = null;
            p39 = promise.Catch(captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return promiseToPromise(p39); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(p39);
            Promise p40 = null;
            p40 = promise.Catch(captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return promiseToPromise(p40); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(p40);


            Promise p41 = null;
            p41 = promise.Then(captureValue, cv => { onResolveCapture(cv); onResolve(); promiseToVoid(p41); }, () => { onUnknownRejection(); promiseToVoid(p41); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(p41);
            Promise p42 = null;
            p42 = promise.Then(captureValue, cv => { onResolveCapture(cv); onResolve(); promiseToVoid(p42); }, (TReject failValue) => { onReject(failValue); promiseToVoid(p42); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(p42);
            Promise p43 = null;
            p43 = promise.Then(captureValue, cv => { onResolveCapture(cv); onResolve(); promiseToVoid(p43); }, () => { onUnknownRejection(); return promiseToPromise(p43); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(p43);
            Promise p44 = null;
            p44 = promise.Then(captureValue, cv => { onResolveCapture(cv); onResolve(); promiseToVoid(p44); }, (TReject failValue) => { onReject(failValue); return promiseToPromise(p44); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(p44);

            Promise<TConvert> p45 = null;
            p45 = promise.Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return promiseToConvert(p45); }, () => { onUnknownRejection(); return promiseToConvert(p45); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(p45);
            Promise<TConvert> p46 = null;
            p46 = promise.Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return promiseToConvert(p46); }, (TReject failValue) => { onReject(failValue); return promiseToConvert(p46); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(p46);
            Promise<TConvert> p47 = null;
            p47 = promise.Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return promiseToConvert(p47); }, () => { onUnknownRejection(); return promiseToPromiseConvert(p47); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(p47);
            Promise<TConvert> p48 = null;
            p48 = promise.Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return promiseToConvert(p48); }, (TReject failValue) => { onReject(failValue); return promiseToPromiseConvert(p48); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(p48);

            Promise p49 = null;
            p49 = promise.Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return promiseToPromise(p49); }, () => { onUnknownRejection(); return promiseToPromise(p49); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(p49);
            Promise p50 = null;
            p50 = promise.Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return promiseToPromise(p50); }, (TReject failValue) => { onReject(failValue); return promiseToPromise(p50); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(p50);
            Promise p51 = null;
            p51 = promise.Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return promiseToPromise(p51); }, () => { onUnknownRejection(); promiseToVoid(p51); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(p51);
            Promise p52 = null;
            p52 = promise.Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return promiseToPromise(p52); }, (TReject failValue) => { onReject(failValue); promiseToVoid(p52); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(p52);

            Promise<TConvert> p53 = null;
            p53 = promise.Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return promiseToPromiseConvert(p53); }, () => { onUnknownRejection(); return promiseToPromiseConvert(p53); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(p53);
            Promise<TConvert> p54 = null;
            p54 = promise.Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return promiseToPromiseConvert(p54); }, (TReject failValue) => { onReject(failValue); return promiseToPromiseConvert(p54); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(p54);
            Promise<TConvert> p55 = null;
            p55 = promise.Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return promiseToPromiseConvert(p55); }, () => { onUnknownRejection(); return promiseToConvert(p55); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(p55);
            Promise<TConvert> p56 = null;
            p56 = promise.Then(captureValue, cv => { onResolveCapture(cv); onResolve(); return promiseToPromiseConvert(p56); }, (TReject failValue) => { onReject(failValue); return promiseToConvert(p56); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(p56);


            Promise p57 = null;
            p57 = promise.Then(() => { onResolve(); promiseToVoid(p57); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); promiseToVoid(p57); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(p57);
            Promise p58 = null;
            p58 = promise.Then(() => { onResolve(); promiseToVoid(p58); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); promiseToVoid(p58); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(p58);
            Promise p59 = null;
            p59 = promise.Then(() => { onResolve(); promiseToVoid(p59); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return promiseToPromise(p59); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(p59);
            Promise p60 = null;
            p60 = promise.Then(() => { onResolve(); promiseToVoid(p60); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return promiseToPromise(p60); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(p60);

            Promise<TConvert> p61 = null;
            p61 = promise.Then(() => { onResolve(); return promiseToConvert(p61); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return promiseToConvert(p61); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(p61);
            Promise<TConvert> p62 = null;
            p62 = promise.Then(() => { onResolve(); return promiseToConvert(p62); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return promiseToConvert(p62); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(p62);
            Promise<TConvert> p63 = null;
            p63 = promise.Then(() => { onResolve(); return promiseToConvert(p63); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return promiseToPromiseConvert(p63); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(p63);
            Promise<TConvert> p64 = null;
            p64 = promise.Then(() => { onResolve(); return promiseToConvert(p64); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return promiseToPromiseConvert(p64); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(p64);

            Promise p65 = null;
            p65 = promise.Then(() => { onResolve(); return promiseToPromise(p65); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return promiseToPromise(p65); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(p65);
            Promise p66 = null;
            p66 = promise.Then(() => { onResolve(); return promiseToPromise(p66); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return promiseToPromise(p66); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(p66);
            Promise p67 = null;
            p67 = promise.Then(() => { onResolve(); return promiseToPromise(p67); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); promiseToVoid(p67); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(p67);
            Promise p68 = null;
            p68 = promise.Then(() => { onResolve(); return promiseToPromise(p68); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); promiseToVoid(p68); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(p68);

            Promise<TConvert> p69 = null;
            p69 = promise.Then(() => { onResolve(); return promiseToPromiseConvert(p69); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return promiseToPromiseConvert(p69); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(p69);
            Promise<TConvert> p70 = null;
            p70 = promise.Then(() => { onResolve(); return promiseToPromiseConvert(p70); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return promiseToPromiseConvert(p70); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(p70);
            Promise<TConvert> p71 = null;
            p71 = promise.Then(() => { onResolve(); return promiseToPromiseConvert(p71); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return promiseToConvert(p71); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(p71);
            Promise<TConvert> p72 = null;
            p72 = promise.Then(() => { onResolve(); return promiseToPromiseConvert(p72); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return promiseToConvert(p72); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(p72);
        }

        public static void AddCallbacks<T, TConvert, TReject, TCapture>(Promise<T> promise,
            Action<T> onResolve = null, Action<TReject> onReject = null, Action onUnknownRejection = null, TConvert convertValue = default(TConvert),
            Action<TCapture> onResolveCapture = null, Action<TCapture> onRejectCapture = null, Action<TCapture> onUnknownRejectionCapture = null, TCapture captureValue = default(TCapture),
            Action<Promise> promiseToVoid = null, Func<Promise<TConvert>, TConvert> promiseToConvert = null, Func<Promise<T>, T> promiseToT = null,
            Func<Promise, Promise> promiseToPromise = null, Func<Promise<TConvert>, Promise<TConvert>> promiseToPromiseConvert = null, Func<Promise<T>, Promise<T>> promiseToPromiseT = null,
            Action<Promise> onCallbackAdded = null, Action<Promise<TConvert>> onCallbackAddedConvert = null, Action<Promise<T>> onCallbackAddedT = null)
        {
            AddCallbacksWithCancelation(
                promise,
                onResolve, onReject, onUnknownRejection, convertValue,
                onResolveCapture, onRejectCapture, onUnknownRejectionCapture, captureValue,
                promiseToVoid, promiseToConvert,
                promiseToT, promiseToPromise,
                promiseToPromiseConvert, promiseToPromiseT, onCallbackAdded,
                onCallbackAddedConvert, onCallbackAddedT
            );

            CancelationSource cancelationSource = CancelationSource.New();
            AddCallbacksWithCancelation(
                promise,
                onResolve, onReject, onUnknownRejection, convertValue,
                onResolveCapture, onRejectCapture, onUnknownRejectionCapture, captureValue,
                promiseToVoid, promiseToConvert,
                promiseToT, promiseToPromise,
                promiseToPromiseConvert, promiseToPromiseT, onCallbackAdded,
                onCallbackAddedConvert, onCallbackAddedT,
                cancelationSource.Token
            );
            cancelationSource.Dispose();
        }

        public static void AddCallbacksWithCancelation<T, TConvert, TReject, TCapture>(Promise<T> promise,
            Action<T> onResolve = null, Action<TReject> onReject = null, Action onUnknownRejection = null, TConvert convertValue = default(TConvert),
            Action<TCapture> onResolveCapture = null, Action<TCapture> onRejectCapture = null, Action<TCapture> onUnknownRejectionCapture = null, TCapture captureValue = default(TCapture),
            Action<Promise> promiseToVoid = null, Func<Promise<TConvert>, TConvert> promiseToConvert = null, Func<Promise<T>, T> promiseToT = null,
            Func<Promise, Promise> promiseToPromise = null, Func<Promise<TConvert>, Promise<TConvert>> promiseToPromiseConvert = null, Func<Promise<T>, Promise<T>> promiseToPromiseT = null,
            Action<Promise> onCallbackAdded = null, Action<Promise<TConvert>> onCallbackAddedConvert = null, Action<Promise<T>> onCallbackAddedT = null,
            CancelationToken cancelationToken = default(CancelationToken),
            Action<ReasonContainer> onCancel = null, Action<TCapture, ReasonContainer> onCancelCapture = null)
        {
            // Add empty delegates so no need for null check.
            onResolve += x => { };
            onReject += s => { };
            onUnknownRejection += () => { };
            onResolveCapture += _ => { };
            onRejectCapture += _ => { };
            onUnknownRejectionCapture += _ => { };
            promiseToVoid += _ => { };
            onCallbackAdded += _ => { };
            onCallbackAddedConvert += _ => { };
            onCallbackAddedT += _ => { };
            if (promiseToConvert == null)
            {
                promiseToConvert += _ => convertValue;
            }
            if (promiseToT == null)
            {
                promiseToT += _ => default(T);
            }
            if (promiseToPromise == null)
            {
                promiseToPromise = _ => Promise.Resolved();
            }
            if (promiseToPromiseConvert == null)
            {
                promiseToPromiseConvert = _ => Promise.Resolved(convertValue);
            }
            if (promiseToPromiseT == null)
            {
                promiseToPromiseT = _ => Promise.Resolved(default(T));
            }
            onCancel += _ => { };
            onCancelCapture += (_, __) => { };

            Promise p1 = null;
            p1 = promise.Then(x => { onResolve(x); promiseToVoid(p1); }, () => { onUnknownRejection(); promiseToVoid(p1); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(p1);
            Promise p2 = null;
            p2 = promise.Then(x => { onResolve(x); promiseToVoid(p2); }, (TReject failValue) => { onReject(failValue); promiseToVoid(p2); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(p2);
            Promise p3 = null;
            p3 = promise.Then(x => { onResolve(x); promiseToVoid(p3); }, () => { onUnknownRejection(); return promiseToPromise(p3); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(p3);
            Promise p4 = null;
            p4 = promise.Then(x => { onResolve(x); promiseToVoid(p4); }, (TReject failValue) => { onReject(failValue); return promiseToPromise(p4); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(p4);

            Promise<TConvert> p5 = null;
            p5 = promise.Then(x => { onResolve(x); return promiseToConvert(p5); }, () => { onUnknownRejection(); return promiseToConvert(p5); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(p5);
            Promise<TConvert> p6 = null;
            p6 = promise.Then(x => { onResolve(x); return promiseToConvert(p6); }, (TReject failValue) => { onReject(failValue); return promiseToConvert(p6); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(p6);
            Promise<TConvert> p7 = null;
            p7 = promise.Then(x => { onResolve(x); return promiseToConvert(p7); }, () => { onUnknownRejection(); return promiseToPromiseConvert(p7); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(p7);
            Promise<TConvert> p8 = null;
            p8 = promise.Then(x => { onResolve(x); return promiseToConvert(p8); }, (TReject failValue) => { onReject(failValue); return promiseToPromiseConvert(p8); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(p8);

            Promise p9 = null;
            p9 = promise.Then(x => { onResolve(x); return promiseToPromise(p9); }, () => { onUnknownRejection(); return promiseToPromise(p9); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(p9);
            Promise p10 = null;
            p10 = promise.Then(x => { onResolve(x); return promiseToPromise(p10); }, (TReject failValue) => { onReject(failValue); return promiseToPromise(p10); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(p10);
            Promise p11 = null;
            p11 = promise.Then(x => { onResolve(x); return promiseToPromise(p11); }, () => { onUnknownRejection(); promiseToVoid(p11); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(p11);
            Promise p12 = null;
            p12 = promise.Then(x => { onResolve(x); return promiseToPromise(p12); }, (TReject failValue) => { onReject(failValue); promiseToVoid(p12); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(p12);

            Promise<TConvert> p13 = null;
            p13 = promise.Then(x => { onResolve(x); return promiseToPromiseConvert(p13); }, () => { onUnknownRejection(); return promiseToPromiseConvert(p13); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(p13);
            Promise<TConvert> p14 = null;
            p14 = promise.Then(x => { onResolve(x); return promiseToPromiseConvert(p14); }, (TReject failValue) => { onReject(failValue); return promiseToPromiseConvert(p14); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(p14);
            Promise<TConvert> p15 = null;
            p15 = promise.Then(x => { onResolve(x); return promiseToPromiseConvert(p15); }, () => { onUnknownRejection(); return promiseToConvert(p15); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(p15);
            Promise<TConvert> p16 = null;
            p16 = promise.Then(x => { onResolve(x); return promiseToPromiseConvert(p16); }, (TReject failValue) => { onReject(failValue); return promiseToConvert(p16); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(p16);

            Promise<T> p17 = null;
            p17 = promise.Catch(() => { onUnknownRejection(); return promiseToT(p17); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedT(p17);
            Promise<T> p18 = null;
            p18 = promise.Catch((TReject failValue) => { onReject(failValue); return promiseToT(p18); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedT(p18);

            Promise<T> p19 = null;
            p19 = promise.Catch(() => { onUnknownRejection(); return promiseToPromiseT(p19); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedT(p19);
            Promise<T> p20 = null;
            p20 = promise.Catch((TReject failValue) => { onReject(failValue); return promiseToPromiseT(p20); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedT(p20);


            Promise p21 = null;
            p21 = promise.Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); promiseToVoid(p21); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); promiseToVoid(p21); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(p21);
            Promise p22 = null;
            p22 = promise.Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); promiseToVoid(p22); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); promiseToVoid(p22); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(p22);
            Promise p23 = null;
            p23 = promise.Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); promiseToVoid(p23); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return promiseToPromise(p23); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(p23);
            Promise p24 = null;
            p24 = promise.Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); promiseToVoid(p24); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return promiseToPromise(p24); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(p24);

            Promise<TConvert> p25 = null;
            p25 = promise.Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return promiseToConvert(p25); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return promiseToConvert(p25); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(p25);
            Promise<TConvert> p26 = null;
            p26 = promise.Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return promiseToConvert(p26); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return promiseToConvert(p26); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(p26);
            Promise<TConvert> p27 = null;
            p27 = promise.Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return promiseToConvert(p27); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return promiseToPromiseConvert(p27); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(p7);
            Promise<TConvert> p28 = null;
            p28 = promise.Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return promiseToConvert(p28); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return promiseToPromiseConvert(p28); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(p28);

            Promise p29 = null;
            p29 = promise.Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return promiseToPromise(p29); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return promiseToPromise(p29); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(p29);
            Promise p30 = null;
            p30 = promise.Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return promiseToPromise(p30); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return promiseToPromise(p30); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(p30);
            Promise p31 = null;
            p31 = promise.Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return promiseToPromise(p31); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); promiseToVoid(p31); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(p31);
            Promise p32 = null;
            p32 = promise.Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return promiseToPromise(p32); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); promiseToVoid(p32); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(p32);

            Promise<TConvert> p33 = null;
            p33 = promise.Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return promiseToPromiseConvert(p33); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return promiseToPromiseConvert(p33); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(p33);
            Promise<TConvert> p34 = null;
            p34 = promise.Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return promiseToPromiseConvert(p34); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return promiseToPromiseConvert(p34); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(p34);
            Promise<TConvert> p35 = null;
            p35 = promise.Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return promiseToPromiseConvert(p35); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return promiseToConvert(p35); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(p35);
            Promise<TConvert> p36 = null;
            p36 = promise.Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return promiseToPromiseConvert(p36); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return promiseToConvert(p36); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(p36);

            Promise<T> p37 = null;
            p37 = promise.Catch(captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return promiseToT(p37); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedT(p37);
            Promise<T> p38 = null;
            p38 = promise.Catch(captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return promiseToT(p38); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedT(p38);

            Promise<T> p39 = null;
            p39 = promise.Catch(captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return promiseToPromiseT(p39); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedT(p39);
            Promise<T> p40 = null;
            p40 = promise.Catch(captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return promiseToPromiseT(p40); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedT(p40);


            Promise p41 = null;
            p41 = promise.Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); promiseToVoid(p41); }, () => { onUnknownRejection(); promiseToVoid(p41); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(p41);
            Promise p42 = null;
            p42 = promise.Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); promiseToVoid(p42); }, (TReject failValue) => { onReject(failValue); promiseToVoid(p42); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(p42);
            Promise p43 = null;
            p43 = promise.Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); promiseToVoid(p43); }, () => { onUnknownRejection(); return promiseToPromise(p43); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(p43);
            Promise p44 = null;
            p44 = promise.Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); promiseToVoid(p44); }, (TReject failValue) => { onReject(failValue); return promiseToPromise(p44); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(p44);

            Promise<TConvert> p45 = null;
            p45 = promise.Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return promiseToConvert(p45); }, () => { onUnknownRejection(); return promiseToConvert(p45); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(p45);
            Promise<TConvert> p46 = null;
            p46 = promise.Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return promiseToConvert(p46); }, (TReject failValue) => { onReject(failValue); return promiseToConvert(p46); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(p46);
            Promise<TConvert> p47 = null;
            p47 = promise.Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return promiseToConvert(p47); }, () => { onUnknownRejection(); return promiseToPromiseConvert(p47); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(p47);
            Promise<TConvert> p48 = null;
            p48 = promise.Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return promiseToConvert(p48); }, (TReject failValue) => { onReject(failValue); return promiseToPromiseConvert(p48); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(p48);

            Promise p49 = null;
            p49 = promise.Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return promiseToPromise(p49); }, () => { onUnknownRejection(); return promiseToPromise(p49); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(p49);
            Promise p50 = null;
            p50 = promise.Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return promiseToPromise(p50); }, (TReject failValue) => { onReject(failValue); return promiseToPromise(p50); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(p50);
            Promise p51 = null;
            p51 = promise.Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return promiseToPromise(p51); }, () => { onUnknownRejection(); promiseToVoid(p51); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(p51);
            Promise p52 = null;
            p52 = promise.Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return promiseToPromise(p52); }, (TReject failValue) => { onReject(failValue); promiseToVoid(p52); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(p52);

            Promise<TConvert> p53 = null;
            p53 = promise.Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return promiseToPromiseConvert(p53); }, () => { onUnknownRejection(); return promiseToPromiseConvert(p53); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(p53);
            Promise<TConvert> p54 = null;
            p54 = promise.Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return promiseToPromiseConvert(p54); }, (TReject failValue) => { onReject(failValue); return promiseToPromiseConvert(p54); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(p54);
            Promise<TConvert> p55 = null;
            p55 = promise.Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return promiseToPromiseConvert(p55); }, () => { onUnknownRejection(); return promiseToConvert(p55); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(p55);
            Promise<TConvert> p56 = null;
            p56 = promise.Then(captureValue, (cv, x) => { onResolveCapture(cv); onResolve(x); return promiseToPromiseConvert(p56); }, (TReject failValue) => { onReject(failValue); return promiseToConvert(p56); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(p56);


            Promise p57 = null;
            p57 = promise.Then(x => { onResolve(x); promiseToVoid(p57); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); promiseToVoid(p57); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(p57);
            Promise p58 = null;
            p58 = promise.Then(x => { onResolve(x); promiseToVoid(p58); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); promiseToVoid(p58); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(p58);
            Promise p59 = null;
            p59 = promise.Then(x => { onResolve(x); promiseToVoid(p59); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return promiseToPromise(p59); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(p59);
            Promise p60 = null;
            p60 = promise.Then(x => { onResolve(x); promiseToVoid(p60); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return promiseToPromise(p60); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(p60);

            Promise<TConvert> p61 = null;
            p61 = promise.Then(x => { onResolve(x); return promiseToConvert(p61); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return promiseToConvert(p61); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(p61);
            Promise<TConvert> p62 = null;
            p62 = promise.Then(x => { onResolve(x); return promiseToConvert(p62); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return promiseToConvert(p62); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(p62);
            Promise<TConvert> p63 = null;
            p63 = promise.Then(x => { onResolve(x); return promiseToConvert(p63); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return promiseToPromiseConvert(p63); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(p63);
            Promise<TConvert> p64 = null;
            p64 = promise.Then(x => { onResolve(x); return promiseToConvert(p64); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return promiseToPromiseConvert(p64); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(p64);

            Promise p65 = null;
            p65 = promise.Then(x => { onResolve(x); return promiseToPromise(p65); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return promiseToPromise(p65); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(p65);
            Promise p66 = null;
            p66 = promise.Then(x => { onResolve(x); return promiseToPromise(p66); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return promiseToPromise(p66); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(p66);
            Promise p67 = null;
            p67 = promise.Then(x => { onResolve(x); return promiseToPromise(p67); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); promiseToVoid(p67); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(p67);
            Promise p68 = null;
            p68 = promise.Then(x => { onResolve(x); return promiseToPromise(p68); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); promiseToVoid(p68); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAdded(p68);

            Promise<TConvert> p69 = null;
            p69 = promise.Then(x => { onResolve(x); return promiseToPromiseConvert(p69); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return promiseToPromiseConvert(p69); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(p69);
            Promise<TConvert> p70 = null;
            p70 = promise.Then(x => { onResolve(x); return promiseToPromiseConvert(p70); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return promiseToPromiseConvert(p70); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(p70);
            Promise<TConvert> p71 = null;
            p71 = promise.Then(x => { onResolve(x); return promiseToPromiseConvert(p71); }, captureValue, cv => { onUnknownRejectionCapture(cv); onUnknownRejection(); return promiseToConvert(p71); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(p71);
            Promise<TConvert> p72 = null;
            p72 = promise.Then(x => { onResolve(x); return promiseToPromiseConvert(p72); }, captureValue, (TCapture cv, TReject failValue) => { onRejectCapture(cv); onReject(failValue); return promiseToConvert(p72); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            onCallbackAddedConvert(p72);
        }

        public static void AddContinueCallbacks<TConvert, TCapture>(Promise promise,
            Action<Promise.ResultContainer> onContinue = null, TConvert convertValue = default(TConvert),
            Action<TCapture, Promise.ResultContainer> onContinueCapture = null, TCapture captureValue = default(TCapture),
            Action<Promise> promiseToVoid = null, Func<Promise<TConvert>, TConvert> promiseToConvert = null,
            Func<Promise, Promise> promiseToPromise = null, Func<Promise<TConvert>, Promise<TConvert>> promiseToPromiseConvert = null)
        {
            AddContinueCallbacksWithCancelation(
                promise,
                onContinue, convertValue,
                onContinueCapture, captureValue,
                promiseToVoid, promiseToConvert,
                promiseToPromise, promiseToPromiseConvert
            );

            CancelationSource cancelationSource = CancelationSource.New();
            AddContinueCallbacksWithCancelation(
                promise,
                onContinue, convertValue,
                onContinueCapture, captureValue,
                promiseToVoid, promiseToConvert,
                promiseToPromise, promiseToPromiseConvert,
                cancelationSource.Token
            );
            cancelationSource.Dispose();
        }

        public static void AddContinueCallbacksWithCancelation<TConvert, TCapture>(Promise promise,
            Action<Promise.ResultContainer> onContinue = null, TConvert convertValue = default(TConvert),
            Action<TCapture, Promise.ResultContainer> onContinueCapture = null, TCapture captureValue = default(TCapture),
            Action<Promise> promiseToVoid = null, Func<Promise<TConvert>, TConvert> promiseToConvert = null,
            Func<Promise, Promise> promiseToPromise = null, Func<Promise<TConvert>, Promise<TConvert>> promiseToPromiseConvert = null,
            CancelationToken cancelationToken = default(CancelationToken),
            Action<ReasonContainer> onCancel = null, Action<TCapture, ReasonContainer> onCancelCapture = null)
        {
            // Add empty delegate so no need for null check.
            onContinue += _ => { };
            onContinueCapture += (_, __) => { };
            promiseToVoid += _ => { };
            if (promiseToConvert == null)
            {
                promiseToConvert += _ => convertValue;
            }
            if (promiseToPromise == null)
            {
                promiseToPromise = _ => Promise.Resolved();
            }
            if (promiseToPromiseConvert == null)
            {
                promiseToPromiseConvert = _ => Promise.Resolved(convertValue);
            }
            onCancel += _ => { };
            onCancelCapture += (_, __) => { };

            Promise p1 = null;
            p1 = promise.ContinueWith(r => { onContinue(r); promiseToVoid(p1); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            Promise<TConvert> p2 = null;
            p2 = promise.ContinueWith(r => { onContinue(r); return promiseToConvert(p2); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            Promise p3 = null;
            p3 = promise.ContinueWith(r => { onContinue(r); return promiseToPromise(p3); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            Promise<TConvert> p4 = null;
            p4 = promise.ContinueWith(r => { onContinue(r); return promiseToPromiseConvert(p4); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);

            Promise p5 = null;
            p5 = promise.ContinueWith(captureValue, (cv, r) => { onContinueCapture(cv, r); onContinue(r); promiseToVoid(p5); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            Promise<TConvert> p6 = null;
            p6 = promise.ContinueWith(captureValue, (cv, r) => { onContinueCapture(cv, r); onContinue(r); return promiseToConvert(p6); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            Promise p7 = null;
            p7 = promise.ContinueWith(captureValue, (cv, r) => { onContinueCapture(cv, r); onContinue(r); return promiseToPromise(p7); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            Promise<TConvert> p8 = null;
            p8 = promise.ContinueWith(captureValue, (cv, r) => { onContinueCapture(cv, r); onContinue(r); return promiseToPromiseConvert(p8); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
        }

        public static void AddContinueCallbacks<T, TConvert, TCapture>(Promise<T> promise, Action<Promise<T>.ResultContainer> onContinue = null, TConvert convertValue = default(TConvert),
            Action<TCapture, Promise<T>.ResultContainer> onContinueCapture = null, TCapture captureValue = default(TCapture),
            Action<Promise> promiseToVoid = null, Func<Promise<TConvert>, TConvert> promiseToConvert = null,
            Func<Promise, Promise> promiseToPromise = null, Func<Promise<TConvert>, Promise<TConvert>> promiseToPromiseConvert = null)
        {
            AddContinueCallbacksWithCancelation(
                promise,
                onContinue, convertValue,
                onContinueCapture, captureValue,
                promiseToVoid, promiseToConvert,
                promiseToPromise, promiseToPromiseConvert
            );

            CancelationSource cancelationSource = CancelationSource.New();
            AddContinueCallbacksWithCancelation(
                promise,
                onContinue, convertValue,
                onContinueCapture, captureValue,
                promiseToVoid, promiseToConvert,
                promiseToPromise, promiseToPromiseConvert,
                cancelationSource.Token
            );
            cancelationSource.Dispose();
        }

        public static void AddContinueCallbacksWithCancelation<T, TConvert, TCapture>(Promise<T> promise, Action<Promise<T>.ResultContainer> onContinue = null, TConvert convertValue = default(TConvert),
            Action<TCapture, Promise<T>.ResultContainer> onContinueCapture = null, TCapture captureValue = default(TCapture),
            Action<Promise> promiseToVoid = null, Func<Promise<TConvert>, TConvert> promiseToConvert = null,
            Func<Promise, Promise> promiseToPromise = null, Func<Promise<TConvert>, Promise<TConvert>> promiseToPromiseConvert = null,
            CancelationToken cancelationToken = default(CancelationToken),
            Action<ReasonContainer> onCancel = null, Action<TCapture, ReasonContainer> onCancelCapture = null)
        {
            // Add empty delegate so no need for null check.
            onContinue += _ => { };
            onContinueCapture += (_, __) => { };
            promiseToVoid += _ => { };
            if (promiseToConvert == null)
            {
                promiseToConvert += _ => convertValue;
            }
            if (promiseToPromise == null)
            {
                promiseToPromise = _ => Promise.Resolved();
            }
            if (promiseToPromiseConvert == null)
            {
                promiseToPromiseConvert = _ => Promise.Resolved(convertValue);
            }
            onCancel += _ => { };
            onCancelCapture += (_, __) => { };

            Promise p1 = null;
            p1 = promise.ContinueWith(r => { onContinue(r); promiseToVoid(p1); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            Promise<TConvert> p2 = null;
            p2 = promise.ContinueWith((Func<Promise<T>.ResultContainer, TConvert>) (r => { onContinue(r); return promiseToConvert(p2); }), cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            Promise p3 = null;
            p3 = promise.ContinueWith((Func<Promise<T>.ResultContainer, Promise>) (r => { onContinue(r); return promiseToPromise(p3); }), cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            Promise<TConvert> p4 = null;
            p4 = promise.ContinueWith((Func<Promise<T>.ResultContainer, Promise<TConvert>>) (r => { onContinue(r); return promiseToPromiseConvert(p4); }), cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);

            Promise p5 = null;
            p5 = promise.ContinueWith(captureValue, (cv, r) => { onContinueCapture(cv, r); onContinue(r); promiseToVoid(p5); }, cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            Promise<TConvert> p6 = null;
            p6 = promise.ContinueWith(captureValue, (Func<TCapture, Promise<T>.ResultContainer, TConvert>) ((cv, r) => { onContinueCapture(cv, r); onContinue(r); return promiseToConvert(p6); }), cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            Promise p7 = null;
            p7 = promise.ContinueWith(captureValue, (Func<TCapture, Promise<T>.ResultContainer, Promise>) ((cv, r) => { onContinueCapture(cv, r); onContinue(r); return promiseToPromise(p7); }), cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
            Promise<TConvert> p8 = null;
            p8 = promise.ContinueWith(captureValue, (Func<TCapture, Promise<T>.ResultContainer, Promise<TConvert>>) ((cv, r) => { onContinueCapture(cv, r); onContinue(r); return promiseToPromiseConvert(p8); }), cancelationToken)
                .CatchCancelation(onCancel)
                .CatchCancelation(captureValue, onCancelCapture);
        }
    }
}