#if !PROTO_PROMISE_CANCEL_DISABLE
#define PROMISE_CANCEL
#else
#undef PROMISE_CANCEL
#endif
#if !PROTO_PROMISE_PROGRESS_DISABLE
#define PROMISE_PROGRESS
#else
#undef PROMISE_PROGRESS
#endif

using System;

namespace Proto.Promises.Tests
{
    // These help test all Then/Catch functions at once.
    public static class TestHelper
    {
#if PROMISE_PROGRESS
        public static readonly double progressEpsilon = 1d / Math.Pow(2d, Promise.Config.ProgressDecimalBits);
#endif

        public const int resolveVoidCallbacks = 20;
        public const int resolveTCallbacks = 20;
        public const int rejectVoidCallbacks = 20;
        public const int rejectTCallbacks = 20;

        public const int resolveVoidVoidCallbacks = 5;
        public const int resolveVoidConvertCallbacks = 5;
        public const int resolveTVoidCallbacks = 5;
        public const int resolveTConvertCallbacks = 5;
        public const int rejectVoidVoidCallbacks = 6;
        public const int rejectVoidConvertCallbacks = 4;
        public const int rejectTVoidCallbacks = 4;
        public const int rejectTConvertCallbacks = 4;
        public const int rejectTTCallbacks = 2;

        public const int resolveVoidPromiseVoidCallbacks = 5;
        public const int resolveVoidPromiseConvertCallbacks = 5;
        public const int resolveTPromiseVoidCallbacks = 5;
        public const int resolveTPromiseConvertCallbacks = 5;
        public const int rejectVoidPromiseVoidCallbacks = 6;
        public const int rejectVoidPromiseConvertCallbacks = 4;
        public const int rejectTPromiseVoidCallbacks = 4;
        public const int rejectTPromiseConvertCallbacks = 4;
        public const int rejectTPromiseTCallbacks = 2;

        public const int rejectVoidKnownCallbacks = 10;
        public const int rejectTKnownCallbacks = 10;

        public const int completeCallbacks = 4;
        public const int completeVoidCallbacks = 1;
        public const int completeConvertCallbacks = 1;
        public const int completePromiseVoidCallbacks = 1;
        public const int completePromiseConvertCallbacks = 1;

        public static void AddResolveCallbacks<TConvert>(Promise promise,
            Action onResolve = null, TConvert convertValue = default(TConvert),
            Action<Promise> promiseToVoid = null, Func<Promise<TConvert>, TConvert> promiseToConvert = null,
            Func<Promise, Promise> promiseToPromise = null, Func<Promise<TConvert>, Promise<TConvert>> promiseToPromiseConvert = null)
        {
            // Add empty delegates so no need for null check.
            onResolve += () => { };
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

            Promise p1 = null;
            p1 = promise.Then(() => { onResolve(); promiseToVoid(p1); });
            Promise<TConvert> p2 = null;
            p2 = promise.Then(() => { onResolve(); return promiseToConvert(p2); });
            Promise p3 = null;
            p3 = promise.Then(() => { onResolve(); return promiseToPromise(p3); });
            Promise<TConvert> p4 = null;
            p4 = promise.Then(() => { onResolve(); return promiseToPromiseConvert(p4); });
        }

        public static void AddResolveCallbacks<T, TConvert>(Promise<T> promise,
            Action<T> onResolve = null, TConvert convertValue = default(TConvert),
            Action<Promise> promiseToVoid = null, Func<Promise<TConvert>, TConvert> promiseToConvert = null,
            Func<Promise, Promise> promiseToPromise = null, Func<Promise<TConvert>, Promise<TConvert>> promiseToPromiseConvert = null)
        {
            // Add empty delegates so no need for null check.
            onResolve += x => { };
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

            Promise p1 = null;
            p1 = promise.Then(x => { onResolve(x); promiseToVoid(p1); });
            Promise<TConvert> p2 = null;
            p2 = promise.Then(x => { onResolve(x); return promiseToConvert(p2); });
            Promise p3 = null;
            p3 = promise.Then(x => { onResolve(x); return promiseToPromise(p3); });
            Promise<TConvert> p4 = null;
            p4 = promise.Then(x => { onResolve(x); return promiseToPromiseConvert(p4); });
        }

        public static void AddCallbacks<TConvert, TReject>(Promise promise,
            Action onResolve = null, Action<TReject> onReject = null, Action onUnknownRejection = null, TConvert convertValue = default(TConvert),
            Action<Promise> promiseToVoid = null, Func<Promise<TConvert>, TConvert> promiseToConvert = null,
            Func<Promise, Promise> promiseToPromise = null, Func<Promise<TConvert>, Promise<TConvert>> promiseToPromiseConvert = null,
            Action<Promise> onCallbackAdded = null, Action<Promise<TConvert>> onCallbackAddedConvert = null)
        {
            // Add empty delegates so no need for null check.
            onResolve += () => { };
            onReject += s => { };
            onUnknownRejection += () => { };
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

            Promise p1 = null;
            p1 = promise.Then(() => { onResolve(); promiseToVoid(p1); }, () => { onUnknownRejection(); promiseToVoid(p1); });
            onCallbackAdded(p1);
            Promise p2 = null;
            p2 = promise.Then(() => { onResolve(); promiseToVoid(p2); }, (TReject failValue) => { onReject(failValue); promiseToVoid(p2); });
            onCallbackAdded(p2);
            Promise p3 = null;
            p3 = promise.Then(() => { onResolve(); promiseToVoid(p3); }, () => { onUnknownRejection(); return promiseToPromise(p3); });
            onCallbackAdded(p3);
            Promise p4 = null;
            p4 = promise.Then(() => { onResolve(); promiseToVoid(p4); }, (TReject failValue) => { onReject(failValue); return promiseToPromise(p4); });
            onCallbackAdded(p4);

            Promise<TConvert> p5 = null;
            p5 = promise.Then(() => { onResolve(); return promiseToConvert(p5); }, () => { onUnknownRejection(); return promiseToConvert(p5); });
            onCallbackAddedConvert(p5);
            Promise<TConvert> p6 = null;
            p6 = promise.Then(() => { onResolve(); return promiseToConvert(p6); }, (TReject failValue) => { onReject(failValue); return promiseToConvert(p6); });
            onCallbackAddedConvert(p6);
            Promise<TConvert> p7 = null;
            p7 = promise.Then(() => { onResolve(); return promiseToConvert(p7); }, () => { onUnknownRejection(); return promiseToPromiseConvert(p7); });
            onCallbackAddedConvert(p7);
            Promise<TConvert> p8 = null;
            p8 = promise.Then(() => { onResolve(); return promiseToConvert(p8); }, (TReject failValue) => { onReject(failValue); return promiseToPromiseConvert(p8); });
            onCallbackAddedConvert(p8);

            Promise p9 = null;
            p9 = promise.Then(() => { onResolve(); return promiseToPromise(p9); }, () => { onUnknownRejection(); return promiseToPromise(p9); });
            onCallbackAdded(p9);
            Promise p10 = null;
            p10 = promise.Then(() => { onResolve(); return promiseToPromise(p10); }, (TReject failValue) => { onReject(failValue); return promiseToPromise(p10); });
            onCallbackAdded(p10);
            Promise p11 = null;
            p11 = promise.Then(() => { onResolve(); return promiseToPromise(p11); }, () => { onUnknownRejection(); promiseToVoid(p11); });
            onCallbackAdded(p11);
            Promise p12 = null;
            p12 = promise.Then(() => { onResolve(); return promiseToPromise(p12); }, (TReject failValue) => { onReject(failValue); promiseToVoid(p12); });
            onCallbackAdded(p12);

            Promise<TConvert> p13 = null;
            p13 = promise.Then(() => { onResolve(); return promiseToPromiseConvert(p13); }, () => { onUnknownRejection(); return promiseToPromiseConvert(p13); });
            onCallbackAddedConvert(p13);
            Promise<TConvert> p14 = null;
            p14 = promise.Then(() => { onResolve(); return promiseToPromiseConvert(p14); }, (TReject failValue) => { onReject(failValue); return promiseToPromiseConvert(p14); });
            onCallbackAddedConvert(p14);
            Promise<TConvert> p15 = null;
            p15 = promise.Then(() => { onResolve(); return promiseToPromiseConvert(p15); }, () => { onUnknownRejection(); return promiseToConvert(p15); });
            onCallbackAddedConvert(p15);
            Promise<TConvert> p16 = null;
            p16 = promise.Then(() => { onResolve(); return promiseToPromiseConvert(p16); }, (TReject failValue) => { onReject(failValue); return promiseToConvert(p16); });
            onCallbackAddedConvert(p16);

            Promise p17 = null;
            p17 = promise.Catch(() => { onUnknownRejection(); promiseToVoid(p17); });
            onCallbackAdded(p17);
            Promise p18 = null;
            p18 = promise.Catch((TReject failValue) => { onReject(failValue); promiseToVoid(p18); });
            onCallbackAdded(p18);

            Promise p19 = null;
            p19 = promise.Catch(() => { onUnknownRejection(); return promiseToPromise(p19); });
            onCallbackAdded(p19);
            Promise p20 = null;
            p20 = promise.Catch((TReject failValue) => { onReject(failValue); return promiseToPromise(p20); });
            onCallbackAdded(p20);
        }

        public static void AddCallbacks<T, TConvert, TReject>(Promise<T> promise,
            Action<T> onResolve = null, Action<TReject> onReject = null, Action onUnknownRejection = null, TConvert convertValue = default(TConvert),
            Action<Promise> promiseToVoid = null, Func<Promise<TConvert>, TConvert> promiseToConvert = null, Func<Promise<T>, T> promiseToT = null,
            Func<Promise, Promise> promiseToPromise = null, Func<Promise<TConvert>, Promise<TConvert>> promiseToPromiseConvert = null, Func<Promise<T>, Promise<T>> promiseToPromiseT = null,
            Action<Promise> onCallbackAdded = null, Action<Promise<TConvert>> onCallbackAddedConvert = null, Action<Promise<T>> onCallbackAddedT = null)
        {
            // Add empty delegates so no need for null check.
            onResolve += x => { };
            onReject += s => { };
            onUnknownRejection += () => { };
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

            Promise p1 = null;
            p1 = promise.Then(x => { onResolve(x); promiseToVoid(p1); }, () => { onUnknownRejection(); promiseToVoid(p1); });
            onCallbackAdded(p1);
            Promise p2 = null;
            p2 = promise.Then(x => { onResolve(x); promiseToVoid(p2); }, (TReject failValue) => { onReject(failValue); promiseToVoid(p2); });
            onCallbackAdded(p2);
            Promise p3 = null;
            p3 = promise.Then(x => { onResolve(x); promiseToVoid(p3); }, () => { onUnknownRejection(); return promiseToPromise(p3); });
            onCallbackAdded(p3);
            Promise p4 = null;
            p4 = promise.Then(x => { onResolve(x); promiseToVoid(p4); }, (TReject failValue) => { onReject(failValue); return promiseToPromise(p4); });
            onCallbackAdded(p4);

            Promise<TConvert> p5 = null;
            p5 = promise.Then(x => { onResolve(x); return promiseToConvert(p5); }, () => { onUnknownRejection(); return promiseToConvert(p5); });
            onCallbackAddedConvert(p5);
            Promise<TConvert> p6 = null;
            p6 = promise.Then(x => { onResolve(x); return promiseToConvert(p6); }, (TReject failValue) => { onReject(failValue); return promiseToConvert(p6); });
            onCallbackAddedConvert(p6);
            Promise<TConvert> p7 = null;
            p7 = promise.Then(x => { onResolve(x); return promiseToConvert(p7); }, () => { onUnknownRejection(); return promiseToPromiseConvert(p7); });
            onCallbackAddedConvert(p7);
            Promise<TConvert> p8 = null;
            p8 = promise.Then(x => { onResolve(x); return promiseToConvert(p8); }, (TReject failValue) => { onReject(failValue); return promiseToPromiseConvert(p8); });
            onCallbackAddedConvert(p8);

            Promise p9 = null;
            p9 = promise.Then(x => { onResolve(x); return promiseToPromise(p9); }, () => { onUnknownRejection(); return promiseToPromise(p9); });
            onCallbackAdded(p9);
            Promise p10 = null;
            p10 = promise.Then(x => { onResolve(x); return promiseToPromise(p10); }, (TReject failValue) => { onReject(failValue); return promiseToPromise(p10); });
            onCallbackAdded(p10);
            Promise p11 = null;
            p11 = promise.Then(x => { onResolve(x); return promiseToPromise(p11); }, () => { onUnknownRejection(); promiseToVoid(p11); });
            onCallbackAdded(p11);
            Promise p12 = null;
            p12 = promise.Then(x => { onResolve(x); return promiseToPromise(p12); }, (TReject failValue) => { onReject(failValue); promiseToVoid(p12); });
            onCallbackAdded(p12);

            Promise<TConvert> p13 = null;
            p13 = promise.Then(x => { onResolve(x); return promiseToPromiseConvert(p13); }, () => { onUnknownRejection(); return promiseToPromiseConvert(p13); });
            onCallbackAddedConvert(p13);
            Promise<TConvert> p14 = null;
            p14 = promise.Then(x => { onResolve(x); return promiseToPromiseConvert(p14); }, (TReject failValue) => { onReject(failValue); return promiseToPromiseConvert(p14); });
            onCallbackAddedConvert(p14);
            Promise<TConvert> p15 = null;
            p15 = promise.Then(x => { onResolve(x); return promiseToPromiseConvert(p15); }, () => { onUnknownRejection(); return promiseToConvert(p15); });
            onCallbackAddedConvert(p15);
            Promise<TConvert> p16 = null;
            p16 = promise.Then(x => { onResolve(x); return promiseToPromiseConvert(p16); }, (TReject failValue) => { onReject(failValue); return promiseToConvert(p16); });
            onCallbackAddedConvert(p16);

            Promise<T> p17 = null;
            p17 = promise.Catch(() => { onUnknownRejection(); return promiseToT(p17); });
            onCallbackAddedT(p17);
            Promise<T> p18 = null;
            p18 = promise.Catch((TReject failValue) => { onReject(failValue); return promiseToT(p18); });
            onCallbackAddedT(p18);

            Promise<T> p19 = null;
            p19 = promise.Catch(() => { onUnknownRejection(); return promiseToPromiseT(p19); });
            onCallbackAddedT(p19);
            Promise<T> p20 = null;
            p20 = promise.Catch((TReject failValue) => { onReject(failValue); return promiseToPromiseT(p20); });
            onCallbackAddedT(p20);
        }

        public static void AddCompleteCallbacks<TConvert>(Promise promise, Action onComplete = null, TConvert convertValue = default(TConvert),
            Action<Promise> promiseToVoid = null, Func<Promise<TConvert>, TConvert> promiseToConvert = null,
            Func<Promise, Promise> promiseToPromise = null, Func<Promise<TConvert>, Promise<TConvert>> promiseToPromiseConvert = null)
        {
            // Add empty delegate so no need for null check.
            onComplete += () => { };
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

            Promise p1 = null;
            p1 = promise.Complete(() => { onComplete(); promiseToVoid(p1); });
            Promise<TConvert> p2 = null;
            p2 = promise.Complete(() => { onComplete(); return promiseToConvert(p2); });
            Promise p3 = null;
            p3 = promise.Complete(() => { onComplete(); return promiseToPromise(p3); });
            Promise<TConvert> p4 = null;
            p4 = promise.Complete(() => { onComplete(); return promiseToPromiseConvert(p4); });
        }
    }
}