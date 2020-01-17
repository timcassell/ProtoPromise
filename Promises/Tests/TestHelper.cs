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
using NUnit.Framework;

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

        public const int catchRejectVoidCallbacks = 24;
        public const int catchRejectTCallbacks = 24;

        public const int totalVoidCallbacks = 25;
        public const int totalTCallbacks = 25;

        public const int secondCatchVoidCallbacks = 10;
        public const int secondCatchTCallbacks = 10;

        public static void AddCallbacks<TReject>(Promise promise, Action onResolve, Action<TReject> onReject, Action onUnknownRejection = null, Action<Exception> onError = null)
        {
            // Add empty delegates so no need for null check.
            onResolve += () => { };
            onReject += s => { };
            onUnknownRejection += () => { };
            if (onError == null)
            {
                onError = e => { throw Promise.Rethrow; };
            }

            promise.Then(() => onResolve())
                .Catch((object e) => { if (e is Exception) throw Promise.Rethrow; })
                .Catch(onError);
            promise.Then(() => { onResolve(); return 0; })
                .Catch((object e) => { if (e is Exception) throw Promise.Rethrow; })
                .Catch(onError);
            promise.Then(() => { onResolve(); return Promise.Resolved(); })
                .Catch((object e) => { if (e is Exception) throw Promise.Rethrow; })
                .Catch(onError);
            promise.Then(() => { onResolve(); return Promise.Resolved(0); })
                .Catch((object e) => { if (e is Exception) throw Promise.Rethrow; })
                .Catch(onError);

            promise.Then(() => onResolve(), () => onUnknownRejection())
                .Catch(onError);
            promise.Then(() => onResolve(), (TReject failValue) => onReject(failValue))
                .Catch(onError);
            promise.Then(() => onResolve(), () => { onUnknownRejection(); return Promise.Resolved(); })
                .Catch(onError);
            promise.Then(() => onResolve(), (TReject failValue) => { onReject(failValue); return Promise.Resolved(); })
                .Catch(onError);

            promise.Then(() => { onResolve(); return 0; }, () => { onUnknownRejection(); return 0; })
                .Catch(onError);
            promise.Then(() => { onResolve(); return 0; }, (TReject failValue) => { onReject(failValue); return 0; })
                .Catch(onError);
            promise.Then(() => { onResolve(); return 0; }, () => { onUnknownRejection(); return Promise.Resolved(0); })
                .Catch(onError);
            promise.Then(() => { onResolve(); return 0; }, (TReject failValue) => { onReject(failValue); return Promise.Resolved(0); })
                .Catch(onError);

            promise.Then(() => { onResolve(); return Promise.Resolved(); }, () => { onUnknownRejection(); return Promise.Resolved(); })
                .Catch(onError);
            promise.Then(() => { onResolve(); return Promise.Resolved(); }, (TReject failValue) => { onReject(failValue); return Promise.Resolved(); })
                .Catch(onError);
            promise.Then(() => { onResolve(); return Promise.Resolved(); }, () => onUnknownRejection())
                .Catch(onError);
            promise.Then(() => { onResolve(); return Promise.Resolved(); }, (TReject failValue) => onReject(failValue))
                .Catch(onError);

            promise.Then(() => { onResolve(); return Promise.Resolved(0); }, () => { onUnknownRejection(); return Promise.Resolved(0); })
                .Catch(onError);
            promise.Then(() => { onResolve(); return Promise.Resolved(0); }, (TReject failValue) => { onReject(failValue); return Promise.Resolved(0); })
                .Catch(onError);
            promise.Then(() => { onResolve(); return Promise.Resolved(0); }, () => { onUnknownRejection(); return 0; })
                .Catch(onError);
            promise.Then(() => { onResolve(); return Promise.Resolved(0); }, (TReject failValue) => { onReject(failValue); return 0; })
                .Catch(onError);

            promise.Catch(() => onUnknownRejection())
                .Catch(onError);
            promise.Catch((TReject failValue) => onReject(failValue))
                .Catch(onError);

            promise.Catch(() => { onUnknownRejection(); return Promise.Resolved(); })
                .Catch(onError);
            promise.Catch((TReject failValue) => { onReject(failValue); return Promise.Resolved(); })
                .Catch(onError);
        }

        public static void AddCallbacks<T, TReject>(Promise<T> promise, Action<T> onResolve, Action<TReject> onReject, Action onUnknownRejection = null, Action<Exception> onError = null)
        {
            // Add empty delegates so no need for null check.
            onResolve += x => { };
            onReject += s => { };
            onUnknownRejection += () => { };
            if (onError == null)
            {
                onError = e => { throw Promise.Rethrow; };
            }

            promise.Then(x => onResolve(x))
                .Catch((object e) => { if (e is Exception) throw Promise.Rethrow; })
                .Catch(onError);
            promise.Then(x => { onResolve(x); return 0; })
                .Catch((object e) => { if (e is Exception) throw Promise.Rethrow; })
                .Catch(onError);
            promise.Then(x => { onResolve(x); return Promise.Resolved(); })
                .Catch((object e) => { if (e is Exception) throw Promise.Rethrow; })
                .Catch(onError);
            promise.Then(x => { onResolve(x); return Promise.Resolved(0); })
                .Catch((object e) => { if (e is Exception) throw Promise.Rethrow; })
                .Catch(onError);

            promise.Then(x => onResolve(x), () => onUnknownRejection())
                .Catch(onError);
            promise.Then(x => onResolve(x), (TReject failValue) => onReject(failValue))
                .Catch(onError);
            promise.Then(x => onResolve(x), () => { onUnknownRejection(); return Promise.Resolved(); })
                .Catch(onError);
            promise.Then(x => onResolve(x), (TReject failValue) => { onReject(failValue); return Promise.Resolved(); })
                .Catch(onError);

            promise.Then(x => { onResolve(x); return 0; }, () => { onUnknownRejection(); return 0; })
                .Catch(onError);
            promise.Then(x => { onResolve(x); return 0; }, (TReject failValue) => { onReject(failValue); return 0; })
                .Catch(onError);
            promise.Then(x => { onResolve(x); return 0; }, () => { onUnknownRejection(); return Promise.Resolved(0); })
                .Catch(onError);
            promise.Then(x => { onResolve(x); return 0; }, (TReject failValue) => { onReject(failValue); return Promise.Resolved(0); })
                .Catch(onError);

            promise.Then(x => { onResolve(x); return Promise.Resolved(); }, () => { onUnknownRejection(); return Promise.Resolved(); })
                .Catch(onError);
            promise.Then(x => { onResolve(x); return Promise.Resolved(); }, (TReject failValue) => { onReject(failValue); return Promise.Resolved(); })
                .Catch(onError);
            promise.Then(x => { onResolve(x); return Promise.Resolved(); }, () => onUnknownRejection())
                .Catch(onError);
            promise.Then(x => { onResolve(x); return Promise.Resolved(); }, (TReject failValue) => onReject(failValue))
                .Catch(onError);

            promise.Then(x => { onResolve(x); return Promise.Resolved(0); }, () => { onUnknownRejection(); return Promise.Resolved(0); })
                .Catch(onError);
            promise.Then(x => { onResolve(x); return Promise.Resolved(0); }, (TReject failValue) => { onReject(failValue); return Promise.Resolved(0); })
                .Catch(onError);
            promise.Then(x => { onResolve(x); return Promise.Resolved(0); }, () => { onUnknownRejection(); return 0; })
                .Catch(onError);
            promise.Then(x => { onResolve(x); return Promise.Resolved(0); }, (TReject failValue) => { onReject(failValue); return 0; })
                .Catch(onError);

            promise.Catch(() => { onUnknownRejection(); return default(T); })
                .Catch(onError);
            promise.Catch((TReject failValue) => { onReject(failValue); return default(T); })
                .Catch(onError);

            promise.Catch(() => { onUnknownRejection(); return Promise.Resolved(default(T)); })
                .Catch(onError);
            promise.Catch((TReject failValue) => { onReject(failValue); return Promise.Resolved(default(T)); })
                .Catch(onError);
        }

        public static void AddCatchCallbacks<TReject0, TReject1>(Promise promise, Action<TReject0> onReject0, Action<TReject1> onReject1)
        {
            // Add empty delegates so no need for null check.
            onReject0 += v => { };
            onReject1 += v => { };
            Action onResolve = () => { };

            promise.Then(onResolve, (TReject0 failValue) => onReject0(failValue))
                .Catch(onReject1);
            promise.Then(() => { onResolve(); return 0; }, (TReject0 failValue) => { onReject0(failValue); return 0; })
                .Catch(onReject1);
            promise.Then(() => { onResolve(); return Promise.Resolved(); }, (TReject0 failValue) => { onReject0(failValue); return Promise.Resolved(); })
                .Catch(onReject1);
            promise.Then(() => { onResolve(); return Promise.Resolved(0); }, (TReject0 failValue) => { onReject0(failValue); return Promise.Resolved(0); })
                .Catch(onReject1);

            promise.Then(onResolve, (TReject0 failValue) => { onReject0(failValue); return Promise.Resolved(); })
                .Catch(onReject1);
            promise.Then(() => { onResolve(); return 0; }, (TReject0 failValue) => { onReject0(failValue); return Promise.Resolved(0); })
                .Catch(onReject1);
            promise.Then(() => { onResolve(); return Promise.Resolved(); }, (TReject0 failValue) => onReject0(failValue))
                .Catch(onReject1);
            promise.Then(() => { onResolve(); return Promise.Resolved(0); }, (TReject0 failValue) => { onReject0(failValue); return 0; })
                .Catch(onReject1);

            promise.Catch((TReject0 failValue) => onReject0(failValue))
                .Catch(onReject1);
            promise.Catch((TReject0 failValue) => { onReject0(failValue); return Promise.Resolved(); })
                .Catch(onReject1);
        }

        public static void AddCatchCallbacks<T, TReject0, TReject1>(Promise<T> promise, Action<TReject0> onReject0, Action<TReject1> onReject1)
        {
            // Add empty delegates so no need for null check.
            onReject0 += v => { };
            onReject1 += v => { };
            Action<T> onResolve = v => { };

            promise.Then(x => onResolve(x), (TReject0 failValue) => onReject0(failValue))
                .Catch(onReject1);
            promise.Then(x => { onResolve(x); return 0; }, (TReject0 failValue) => { onReject0(failValue); return 0; })
                .Catch(onReject1);
            promise.Then(x => { onResolve(x); return Promise.Resolved(); }, (TReject0 failValue) => { onReject0(failValue); return Promise.Resolved(); })
                .Catch(onReject1);
            promise.Then(x => { onResolve(x); return Promise.Resolved(0); }, (TReject0 failValue) => { onReject0(failValue); return Promise.Resolved(0); })
                .Catch(onReject1);

            promise.Then(x => onResolve(x), (TReject0 failValue) => { onReject0(failValue); return Promise.Resolved(); })
                .Catch(onReject1);
            promise.Then(x => { onResolve(x); return 0; }, (TReject0 failValue) => { onReject0(failValue); return Promise.Resolved(0); })
                .Catch(onReject1);
            promise.Then(x => { onResolve(x); return Promise.Resolved(); }, (TReject0 failValue) => onReject0(failValue))
                .Catch(onReject1);
            promise.Then(x => { onResolve(x); return Promise.Resolved(0); }, (TReject0 failValue) => { onReject0(failValue); return 0; })
                .Catch(onReject1);

            promise.Catch((TReject0 failValue) => { onReject0(failValue); return default(T); })
                .Catch(onReject1);
            promise.Catch((TReject0 failValue) => { onReject0(failValue); return Promise.Resolved(default(T)); })
                .Catch(onReject1);
        }

        public static void AddCompleteCallbacks(Promise promise, Action onComplete)
        {
            // Add empty delegate so no need for null check.
            onComplete += () => { };

            promise.Complete(() => onComplete());
            promise.Complete(() => { onComplete(); return 0; });
            promise.Complete(() => { onComplete(); return Promise.Resolved(); });
            promise.Complete(() => { onComplete(); return Promise.Resolved(0); });
        }

        public static void AssertRejectType<TReject>(Promise promise)
        {
            int rejectCounter = 0;
            AddCallbacks(promise,
                () => Assert.Fail("Promise was resolved when it should be rejected with " + typeof(TReject)),
                (object e) => { Assert.IsInstanceOf<TReject>(e); ++rejectCounter; },
                () => ++rejectCounter,
                e => Assert.IsInstanceOf<TReject>(e)
                );
            Promise.Manager.HandleCompletes();

            Assert.AreEqual(rejectVoidCallbacks, rejectCounter);
        }

        public static void AssertRejectType<T, TReject>(Promise<T> promise)
        {
            int rejectCounter = 0;
            AddCallbacks(promise,
                v => Assert.Fail("Promise was resolved when it should be rejected with " + typeof(TReject)),
                (object e) => { Assert.IsInstanceOf<TReject>(e); ++rejectCounter; },
                () => ++rejectCounter,
                e => Assert.IsInstanceOf<TReject>(e)
                );
            Promise.Manager.HandleCompletes();

            Assert.AreEqual(rejectTCallbacks, rejectCounter);
        }

#if PROMISE_CANCEL
        public static void AddCatchCancelCallbacks<TReject, TCancel>(Promise promise, Action onResolve, Action<TReject> onReject, Action onUnknownRejection, Action<TCancel> onCancel)
        {
            // Add empty delegates so no need for null check.
            onResolve += () => { };
            onReject += s => { };
            onUnknownRejection += () => { };
            onCancel += x => { };

            promise.Then(() => onResolve())
                .Catch(() => { })
                .CatchCancelation(onCancel);
            promise.Then(() => { onResolve(); return 0; })
                .Catch(() => { })
                .CatchCancelation(onCancel);
            promise.Then(() => { onResolve(); return Promise.Resolved(); })
                .Catch(() => { })
                .CatchCancelation(onCancel);
            promise.Then(() => { onResolve(); return Promise.Resolved(0); })
                .Catch(() => { })
                .CatchCancelation(onCancel);

            promise.Then(() => onResolve(), () => onUnknownRejection())
                .Catch(() => { })
                .CatchCancelation(onCancel);
            promise.Then(() => onResolve(), (TReject failValue) => onReject(failValue))
                .Catch(() => { })
                .CatchCancelation(onCancel);
            promise.Then(() => onResolve(), () => { onUnknownRejection(); return Promise.Resolved(); })
                .Catch(() => { })
                .CatchCancelation(onCancel);
            promise.Then(() => onResolve(), (TReject failValue) => { onReject(failValue); return Promise.Resolved(); })
                .Catch(() => { })
                .CatchCancelation(onCancel);

            promise.Then(() => { onResolve(); return 0; }, () => { onUnknownRejection(); return 0; })
                .Catch(() => { })
                .CatchCancelation(onCancel);
            promise.Then(() => { onResolve(); return 0; }, (TReject failValue) => { onReject(failValue); return 0; })
                .Catch(() => { })
                .CatchCancelation(onCancel);
            promise.Then(() => { onResolve(); return 0; }, () => { onUnknownRejection(); return Promise.Resolved(0); })
                .Catch(() => { })
                .CatchCancelation(onCancel);
            promise.Then(() => { onResolve(); return 0; }, (TReject failValue) => { onReject(failValue); return Promise.Resolved(0); })
                .Catch(() => { })
                .CatchCancelation(onCancel);

            promise.Then(() => { onResolve(); return Promise.Resolved(); }, () => { onUnknownRejection(); return Promise.Resolved(); })
                .Catch(() => { })
                .CatchCancelation(onCancel);
            promise.Then(() => { onResolve(); return Promise.Resolved(); }, (TReject failValue) => { onReject(failValue); return Promise.Resolved(); })
                .Catch(() => { })
                .CatchCancelation(onCancel);
            promise.Then(() => { onResolve(); return Promise.Resolved(); }, () => onUnknownRejection())
                .Catch(() => { })
                .CatchCancelation(onCancel);
            promise.Then(() => { onResolve(); return Promise.Resolved(); }, (TReject failValue) => onReject(failValue))
                .Catch(() => { })
                .CatchCancelation(onCancel);

            promise.Then(() => { onResolve(); return Promise.Resolved(0); }, () => { onUnknownRejection(); return Promise.Resolved(0); })
                .Catch(() => { })
                .CatchCancelation(onCancel);
            promise.Then(() => { onResolve(); return Promise.Resolved(0); }, (TReject failValue) => { onReject(failValue); return Promise.Resolved(0); })
                .Catch(() => { })
                .CatchCancelation(onCancel);
            promise.Then(() => { onResolve(); return Promise.Resolved(0); }, () => { onUnknownRejection(); return 0; })
                .Catch(() => { })
                .CatchCancelation(onCancel);
            promise.Then(() => { onResolve(); return Promise.Resolved(0); }, (TReject failValue) => { onReject(failValue); return 0; })
                .Catch(() => { })
                .CatchCancelation(onCancel);

            promise.Catch(() => onUnknownRejection())
                .Catch(() => { })
                .CatchCancelation(onCancel);
            promise.Catch((TReject failValue) => onReject(failValue))
                .Catch(() => { })
                .CatchCancelation(onCancel);

            promise.Catch(() => { onUnknownRejection(); return Promise.Resolved(); })
                .Catch(() => { })
                .CatchCancelation(onCancel);
            promise.Catch((TReject failValue) => { onReject(failValue); return Promise.Resolved(); })
                .Catch(() => { })
                .CatchCancelation(onCancel);
        }

        //public static void AddCatchCancelCallbacks<T, TReject, TCancel>(Promise<T> promise, Action<T> onResolve, Action<TReject> onReject, Action onUnknownRejection, Action<TCancel> onCancel)
        public static void AddCatchCancelCallbacks<T, TReject, TCancel>(Promise<T> promise, Action<T> onResolve, Action<TReject> onReject, Action onUnknownRejection, Action<TCancel> onCancel)
        {
            // Add empty delegates so no need for null check.
            onResolve += x => { };
            onReject += s => { };
            onUnknownRejection += () => { };
            onCancel += x => { };

            promise.Then(x => onResolve(x))
                .Catch(() => { })
                .CatchCancelation(onCancel);
            promise.Then(x => { onResolve(x); return 0; })
                .Catch(() => { })
                .CatchCancelation(onCancel);
            promise.Then(x => { onResolve(x); return Promise.Resolved(); })
                .Catch(() => { })
                .CatchCancelation(onCancel);
            promise.Then(x => { onResolve(x); return Promise.Resolved(0); })
                .Catch(() => { })
                .CatchCancelation(onCancel);

            promise.Then(x => onResolve(x), () => onUnknownRejection())
                .Catch(() => { })
                .CatchCancelation(onCancel);
            promise.Then(x => onResolve(x), (TReject failValue) => onReject(failValue))
                .Catch(() => { })
                .CatchCancelation(onCancel);
            promise.Then(x => onResolve(x), () => { onUnknownRejection(); return Promise.Resolved(); })
                .Catch(() => { })
                .CatchCancelation(onCancel);
            promise.Then(x => onResolve(x), (TReject failValue) => { onReject(failValue); return Promise.Resolved(); })
                .Catch(() => { })
                .CatchCancelation(onCancel);

            promise.Then(x => { onResolve(x); return 0; }, () => { onUnknownRejection(); return 0; })
                .Catch(() => { })
                .CatchCancelation(onCancel);
            promise.Then(x => { onResolve(x); return 0; }, (TReject failValue) => { onReject(failValue); return 0; })
                .Catch(() => { })
                .CatchCancelation(onCancel);
            promise.Then(x => { onResolve(x); return 0; }, () => { onUnknownRejection(); return Promise.Resolved(0); })
                .Catch(() => { })
                .CatchCancelation(onCancel);
            promise.Then(x => { onResolve(x); return 0; }, (TReject failValue) => { onReject(failValue); return Promise.Resolved(0); })
                .Catch(() => { })
                .CatchCancelation(onCancel);

            promise.Then(x => { onResolve(x); return Promise.Resolved(); }, () => { onUnknownRejection(); return Promise.Resolved(); })
                .Catch(() => { })
                .CatchCancelation(onCancel);
            promise.Then(x => { onResolve(x); return Promise.Resolved(); }, (TReject failValue) => { onReject(failValue); return Promise.Resolved(); })
                .Catch(() => { })
                .CatchCancelation(onCancel);
            promise.Then(x => { onResolve(x); return Promise.Resolved(); }, () => onUnknownRejection())
                .Catch(() => { })
                .CatchCancelation(onCancel);
            promise.Then(x => { onResolve(x); return Promise.Resolved(); }, (TReject failValue) => onReject(failValue))
                .Catch(() => { })
                .CatchCancelation(onCancel);

            promise.Then(x => { onResolve(x); return Promise.Resolved(0); }, () => { onUnknownRejection(); return Promise.Resolved(0); })
                .Catch(() => { })
                .CatchCancelation(onCancel);
            promise.Then(x => { onResolve(x); return Promise.Resolved(0); }, (TReject failValue) => { onReject(failValue); return Promise.Resolved(0); })
                .Catch(() => { })
                .CatchCancelation(onCancel);
            promise.Then(x => { onResolve(x); return Promise.Resolved(0); }, () => { onUnknownRejection(); return 0; })
                .Catch(() => { })
                .CatchCancelation(onCancel);
            promise.Then(x => { onResolve(x); return Promise.Resolved(0); }, (TReject failValue) => { onReject(failValue); return 0; })
                .Catch(() => { })
                .CatchCancelation(onCancel);

            promise.Catch(() => { onUnknownRejection(); return default(T); })
                .Catch(() => { })
                .CatchCancelation(onCancel);
            promise.Catch((TReject failValue) => { onReject(failValue); return default(T); })
                .Catch(() => { })
                .CatchCancelation(onCancel);

            promise.Catch(() => { onUnknownRejection(); return Promise.Resolved(default(T)); })
                .Catch(() => { })
                .CatchCancelation(onCancel);
            promise.Catch((TReject failValue) => { onReject(failValue); return Promise.Resolved(default(T)); })
                .Catch(() => { })
                .CatchCancelation(onCancel);
        }
#endif

        public static void AddCatchRejectCallbacks<TReject, TThrown>(Promise promise, Action onResolve, Action<TReject> onReject, Action onUnknownRejection, Action<TThrown> onThrown)
        {
            // Add empty delegates so no need for null check.
            onResolve += () => { };
            onReject += s => { };
            onUnknownRejection += () => { };
            onThrown += x => { };

            promise.Then(() => onResolve())
                .Catch(onUnknownRejection)
                .Catch(onThrown);
            promise.Then(() => { onResolve(); return 0; })
                .Catch(onUnknownRejection)
                .Catch(onThrown);
            promise.Then(() => { onResolve(); return Promise.Resolved(); })
                .Catch(onUnknownRejection)
                .Catch(onThrown);
            promise.Then(() => { onResolve(); return Promise.Resolved(0); })
                .Catch(onUnknownRejection)
                .Catch(onThrown);

            promise.Then(() => onResolve(), () => onUnknownRejection())
                .Catch(onThrown);
            promise.Then(() => onResolve(), (TReject failValue) => onReject(failValue))
                .Catch(onThrown);
            promise.Then(() => onResolve(), () => { onUnknownRejection(); return Promise.Resolved(); })
                .Catch(onThrown);
            promise.Then(() => onResolve(), (TReject failValue) => { onReject(failValue); return Promise.Resolved(); })
                .Catch(onThrown);

            promise.Then(() => { onResolve(); return 0; }, () => { onUnknownRejection(); return 0; })
                .Catch(onThrown);
            promise.Then(() => { onResolve(); return 0; }, (TReject failValue) => { onReject(failValue); return 0; })
                .Catch(onThrown);
            promise.Then(() => { onResolve(); return 0; }, () => { onUnknownRejection(); return Promise.Resolved(0); })
                .Catch(onThrown);
            promise.Then(() => { onResolve(); return 0; }, (TReject failValue) => { onReject(failValue); return Promise.Resolved(0); })
                .Catch(onThrown);

            promise.Then(() => { onResolve(); return Promise.Resolved(); }, () => { onUnknownRejection(); return Promise.Resolved(); })
                .Catch(onThrown);
            promise.Then(() => { onResolve(); return Promise.Resolved(); }, (TReject failValue) => { onReject(failValue); return Promise.Resolved(); })
                .Catch(onThrown);
            promise.Then(() => { onResolve(); return Promise.Resolved(); }, () => onUnknownRejection())
                .Catch(onThrown);
            promise.Then(() => { onResolve(); return Promise.Resolved(); }, (TReject failValue) => onReject(failValue))
                .Catch(onThrown);

            promise.Then(() => { onResolve(); return Promise.Resolved(0); }, () => { onUnknownRejection(); return Promise.Resolved(0); })
                .Catch(onThrown);
            promise.Then(() => { onResolve(); return Promise.Resolved(0); }, (TReject failValue) => { onReject(failValue); return Promise.Resolved(0); })
                .Catch(onThrown);
            promise.Then(() => { onResolve(); return Promise.Resolved(0); }, () => { onUnknownRejection(); return 0; })
                .Catch(onThrown);
            promise.Then(() => { onResolve(); return Promise.Resolved(0); }, (TReject failValue) => { onReject(failValue); return 0; })
                .Catch(onThrown);

            promise.Catch(() => onUnknownRejection())
                .Catch(onThrown);
            promise.Catch((TReject failValue) => onReject(failValue))
                .Catch(onThrown);

            promise.Catch(() => { onUnknownRejection(); return Promise.Resolved(); })
                .Catch(onThrown);
            promise.Catch((TReject failValue) => { onReject(failValue); return Promise.Resolved(); })
                .Catch(onThrown);
        }

        //public static void AddCatchCancelCallbacks<T, TReject, TCancel>(Promise<T> promise, Action<T> onResolve, Action<TReject> onReject, Action onUnknownRejection, Action<TCancel> onCancel)
        public static void AddCatchRejectCallbacks<T, TReject, TThrown>(Promise<T> promise, Action<T> onResolve, Action<TReject> onReject, Action onUnknownRejection, Action<TThrown> onThrown)
        {
            // Add empty delegates so no need for null check.
            onResolve += x => { };
            onReject += s => { };
            onUnknownRejection += () => { };
            onThrown += x => { };

            promise.Then(x => onResolve(x))
                .Catch(onUnknownRejection)
                .Catch(onThrown);
            promise.Then(x => { onResolve(x); return 0; })
                .Catch(onUnknownRejection)
                .Catch(onThrown);
            promise.Then(x => { onResolve(x); return Promise.Resolved(); })
                .Catch(onUnknownRejection)
                .Catch(onThrown);
            promise.Then(x => { onResolve(x); return Promise.Resolved(0); })
                .Catch(onUnknownRejection)
                .Catch(onThrown);

            promise.Then(x => onResolve(x), () => onUnknownRejection())
                .Catch(onThrown);
            promise.Then(x => onResolve(x), (TReject failValue) => onReject(failValue))
                .Catch(onThrown);
            promise.Then(x => onResolve(x), () => { onUnknownRejection(); return Promise.Resolved(); })
                .Catch(onThrown);
            promise.Then(x => onResolve(x), (TReject failValue) => { onReject(failValue); return Promise.Resolved(); })
                .Catch(onThrown);

            promise.Then(x => { onResolve(x); return 0; }, () => { onUnknownRejection(); return 0; })
                .Catch(onThrown);
            promise.Then(x => { onResolve(x); return 0; }, (TReject failValue) => { onReject(failValue); return 0; })
                .Catch(onThrown);
            promise.Then(x => { onResolve(x); return 0; }, () => { onUnknownRejection(); return Promise.Resolved(0); })
                .Catch(onThrown);
            promise.Then(x => { onResolve(x); return 0; }, (TReject failValue) => { onReject(failValue); return Promise.Resolved(0); })
                .Catch(onThrown);

            promise.Then(x => { onResolve(x); return Promise.Resolved(); }, () => { onUnknownRejection(); return Promise.Resolved(); })
                .Catch(onThrown);
            promise.Then(x => { onResolve(x); return Promise.Resolved(); }, (TReject failValue) => { onReject(failValue); return Promise.Resolved(); })
                .Catch(onThrown);
            promise.Then(x => { onResolve(x); return Promise.Resolved(); }, () => onUnknownRejection())
                .Catch(onThrown);
            promise.Then(x => { onResolve(x); return Promise.Resolved(); }, (TReject failValue) => onReject(failValue))
                .Catch(onThrown);

            promise.Then(x => { onResolve(x); return Promise.Resolved(0); }, () => { onUnknownRejection(); return Promise.Resolved(0); })
                .Catch(onThrown);
            promise.Then(x => { onResolve(x); return Promise.Resolved(0); }, (TReject failValue) => { onReject(failValue); return Promise.Resolved(0); })
                .Catch(onThrown);
            promise.Then(x => { onResolve(x); return Promise.Resolved(0); }, () => { onUnknownRejection(); return 0; })
                .Catch(onThrown);
            promise.Then(x => { onResolve(x); return Promise.Resolved(0); }, (TReject failValue) => { onReject(failValue); return 0; })
                .Catch(onThrown);

            promise.Catch(() => { onUnknownRejection(); return default(T); })
                .Catch(onThrown);
            promise.Catch((TReject failValue) => { onReject(failValue); return default(T); })
                .Catch(onThrown);

            promise.Catch(() => { onUnknownRejection(); return Promise.Resolved(default(T)); })
                .Catch(onThrown);
            promise.Catch((TReject failValue) => { onReject(failValue); return Promise.Resolved(default(T)); })
                .Catch(onThrown);
        }

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
            Func<Promise, Promise> promiseToPromise = null, Func<Promise<TConvert>, Promise<TConvert>> promiseToPromiseConvert = null)
        {
            // Add empty delegates so no need for null check.
            onResolve += () => { };
            onReject += s => { };
            onUnknownRejection += () => { };
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
            p1 = promise.Then(() => { onResolve(); promiseToVoid(p1); }, () => { onUnknownRejection(); promiseToVoid(p1); });
            Promise p2 = null;
            p2 = promise.Then(() => { onResolve(); promiseToVoid(p2); }, (TReject failValue) => { onReject(failValue); promiseToVoid(p2); });
            Promise p3 = null;
            p3 = promise.Then(() => { onResolve(); promiseToVoid(p3); }, () => { onUnknownRejection(); return promiseToPromise(p3); });
            Promise p4 = null;
            p4 = promise.Then(() => { onResolve(); promiseToVoid(p4); }, (TReject failValue) => { onReject(failValue); return promiseToPromise(p4); });

            Promise<TConvert> p5 = null;
            p5 = promise.Then(() => { onResolve(); return promiseToConvert(p5); }, () => { onUnknownRejection(); return promiseToConvert(p5); });
            Promise<TConvert> p6 = null;
            p6 = promise.Then(() => { onResolve(); return promiseToConvert(p6); }, (TReject failValue) => { onReject(failValue); return promiseToConvert(p6); });
            Promise<TConvert> p7 = null;
            p7 = promise.Then(() => { onResolve(); return promiseToConvert(p7); }, () => { onUnknownRejection(); return promiseToPromiseConvert(p7); });
            Promise<TConvert> p8 = null;
            p8 = promise.Then(() => { onResolve(); return promiseToConvert(p8); }, (TReject failValue) => { onReject(failValue); return promiseToPromiseConvert(p8); });

            Promise p9 = null;
            p9 = promise.Then(() => { onResolve(); return promiseToPromise(p9); }, () => { onUnknownRejection(); return promiseToPromise(p9); });
            Promise p10 = null;
            p10 = promise.Then(() => { onResolve(); return promiseToPromise(p10); }, (TReject failValue) => { onReject(failValue); return promiseToPromise(p10); });
            Promise p11 = null;
            p11 = promise.Then(() => { onResolve(); return promiseToPromise(p11); }, () => { onUnknownRejection(); promiseToVoid(p11); });
            Promise p12 = null;
            p12 = promise.Then(() => { onResolve(); return promiseToPromise(p12); }, (TReject failValue) => { onReject(failValue); promiseToVoid(p12); });

            Promise<TConvert> p13 = null;
            p13 = promise.Then(() => { onResolve(); return promiseToPromiseConvert(p13); }, () => { onUnknownRejection(); return promiseToPromiseConvert(p13); });
            Promise<TConvert> p14 = null;
            p14 = promise.Then(() => { onResolve(); return promiseToPromiseConvert(p14); }, (TReject failValue) => { onReject(failValue); return promiseToPromiseConvert(p14); });
            Promise<TConvert> p15 = null;
            p15 = promise.Then(() => { onResolve(); return promiseToPromiseConvert(p15); }, () => { onUnknownRejection(); return promiseToConvert(p15); });
            Promise<TConvert> p16 = null;
            p16 = promise.Then(() => { onResolve(); return promiseToPromiseConvert(p16); }, (TReject failValue) => { onReject(failValue); return promiseToConvert(p16); });

            Promise p17 = null;
            p17 = promise.Catch(() => { onUnknownRejection(); promiseToVoid(p17); });
            Promise p18 = null;
            p18 = promise.Catch((TReject failValue) => { onReject(failValue); promiseToVoid(p18); });

            Promise p19 = null;
            p19 = promise.Catch(() => { onUnknownRejection(); return promiseToPromise(p19); });
            Promise p20 = null;
            p20 = promise.Catch((TReject failValue) => { onReject(failValue); return promiseToPromise(p20); });
        }

        public static void AddCallbacks<T, TConvert, TReject>(Promise<T> promise,
            Action<T> onResolve = null, Action<TReject> onReject = null, Action onUnknownRejection = null, TConvert convertValue = default(TConvert),
            Action<Promise> promiseToVoid = null, Func<Promise<TConvert>, TConvert> promiseToConvert = null, Func<Promise<T>, T> promiseToT = null,
            Func<Promise, Promise> promiseToPromise = null, Func<Promise<TConvert>, Promise<TConvert>> promiseToPromiseConvert = null, Func<Promise<T>, Promise<T>> promiseToPromiseT = null)
        {
            // Add empty delegates so no need for null check.
            onResolve += x => { };
            onReject += s => { };
            onUnknownRejection += () => { };
            promiseToVoid += _ => { };
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
            Promise p2 = null;
            p2 = promise.Then(x => { onResolve(x); promiseToVoid(p2); }, (TReject failValue) => { onReject(failValue); promiseToVoid(p2); });
            Promise p3 = null;
            p3 = promise.Then(x => { onResolve(x); promiseToVoid(p3); }, () => { onUnknownRejection(); return promiseToPromise(p3); });
            Promise p4 = null;
            p4 = promise.Then(x => { onResolve(x); promiseToVoid(p4); }, (TReject failValue) => { onReject(failValue); return promiseToPromise(p4); });

            Promise<TConvert> p5 = null;
            p5 = promise.Then(x => { onResolve(x); return promiseToConvert(p5); }, () => { onUnknownRejection(); return promiseToConvert(p5); });
            Promise<TConvert> p6 = null;
            p6 = promise.Then(x => { onResolve(x); return promiseToConvert(p6); }, (TReject failValue) => { onReject(failValue); return promiseToConvert(p6); });
            Promise<TConvert> p7 = null;
            p7 = promise.Then(x => { onResolve(x); return promiseToConvert(p7); }, () => { onUnknownRejection(); return promiseToPromiseConvert(p7); });
            Promise<TConvert> p8 = null;
            p8 = promise.Then(x => { onResolve(x); return promiseToConvert(p8); }, (TReject failValue) => { onReject(failValue); return promiseToPromiseConvert(p8); });

            Promise p9 = null;
            p9 = promise.Then(x => { onResolve(x); return promiseToPromise(p9); }, () => { onUnknownRejection(); return promiseToPromise(p9); });
            Promise p10 = null;
            p10 = promise.Then(x => { onResolve(x); return promiseToPromise(p10); }, (TReject failValue) => { onReject(failValue); return promiseToPromise(p10); });
            Promise p11 = null;
            p11 = promise.Then(x => { onResolve(x); return promiseToPromise(p11); }, () => { onUnknownRejection(); promiseToVoid(p11); });
            Promise p12 = null;
            p12 = promise.Then(x => { onResolve(x); return promiseToPromise(p12); }, (TReject failValue) => { onReject(failValue); promiseToVoid(p12); });

            Promise<TConvert> p13 = null;
            p13 = promise.Then(x => { onResolve(x); return promiseToPromiseConvert(p13); }, () => { onUnknownRejection(); return promiseToPromiseConvert(p13); });
            Promise<TConvert> p14 = null;
            p14 = promise.Then(x => { onResolve(x); return promiseToPromiseConvert(p14); }, (TReject failValue) => { onReject(failValue); return promiseToPromiseConvert(p14); });
            Promise<TConvert> p15 = null;
            p15 = promise.Then(x => { onResolve(x); return promiseToPromiseConvert(p15); }, () => { onUnknownRejection(); return promiseToConvert(p15); });
            Promise<TConvert> p16 = null;
            p16 = promise.Then(x => { onResolve(x); return promiseToPromiseConvert(p16); }, (TReject failValue) => { onReject(failValue); return promiseToConvert(p16); });

            Promise<T> p17 = null;
            p17 = promise.Catch(() => { onUnknownRejection(); return promiseToT(p17); });
            Promise<T> p18 = null;
            p18 = promise.Catch((TReject failValue) => { onReject(failValue); return promiseToT(p18); });

            Promise<T> p19 = null;
            p19 = promise.Catch(() => { onUnknownRejection(); return promiseToPromiseT(p19); });
            Promise<T> p20 = null;
            p20 = promise.Catch((TReject failValue) => { onReject(failValue); return promiseToPromiseT(p20); });
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