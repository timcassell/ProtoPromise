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
        public const int completePromiseVoidCallbacks = 1;
        public const int completePromiseConvertCallbacks = 1;

        public static void AddResolveCallbacks<TConvert>(Promise promise,
            Action onResolve = null, TConvert convertValue = default(TConvert),
            Action<Promise> onCallbackAddedVoid = null, Action<Promise<TConvert>> onCallbackAddedConvert = null,
            Func<Promise, Promise> promiseToPromise = null, Func<Promise<TConvert>, Promise<TConvert>> promiseToPromiseConvert = null)
        {
            // Add empty delegates so no need for null check.
            onResolve += () => { };
            onCallbackAddedVoid += _ => { };
            onCallbackAddedConvert += _ => { };
            if (promiseToPromise == null)
            {
                promiseToPromise = _ => Promise.Resolved();
            }
            if (promiseToPromiseConvert == null)
            {
                promiseToPromiseConvert = _ => Promise.Resolved(convertValue);
            }

            onCallbackAddedVoid(promise.Then(() => onResolve()));
            onCallbackAddedConvert(promise.Then(() => { onResolve(); return convertValue; }));
            Promise p1 = null;
            p1 = promise.Then(() => { onResolve(); return promiseToPromise(p1); });
            onCallbackAddedVoid(p1);
            Promise<TConvert> p2 = null;
            p2 = promise.Then(() => { onResolve(); return promiseToPromiseConvert(p2); });
            onCallbackAddedConvert(p2);
        }

        public static void AddResolveCallbacks<T, TConvert>(Promise<T> promise,
            Action<T> onResolve = null, TConvert convertValue = default(TConvert),
            Action<Promise> onCallbackAddedVoid = null, Action<Promise<TConvert>> onCallbackAddedConvert = null,
            Func<Promise, Promise> promiseToPromise = null, Func<Promise<TConvert>, Promise<TConvert>> promiseToPromiseConvert = null)
        {
            // Add empty delegates so no need for null check.
            onResolve += x => { };
            onCallbackAddedVoid += _ => { };
            onCallbackAddedConvert += _ => { };
            if (promiseToPromise == null)
            {
                promiseToPromise = _ => Promise.Resolved();
            }
            if (promiseToPromiseConvert == null)
            {
                promiseToPromiseConvert = _ => Promise.Resolved(convertValue);
            }

            onCallbackAddedVoid(promise.Then(x => onResolve(x)));
            onCallbackAddedConvert(promise.Then(x => { onResolve(x); return convertValue; }));
            Promise p1 = null;
            p1 = promise.Then(x => { onResolve(x); return promiseToPromise(p1); });
            onCallbackAddedVoid(p1);
            Promise<TConvert> p2 = null;
            p2 = promise.Then(x => { onResolve(x); return promiseToPromiseConvert(p2); });
            onCallbackAddedConvert(p2);
        }

        public static void AddCallbacks<TConvert, TReject>(Promise promise,
            Action onResolve = null, Action<TReject> onReject = null, Action onUnknownRejection = null, TConvert convertValue = default(TConvert),
            Action<Promise> onCallbackAddedVoid = null, Action<Promise<TConvert>> onCallbackAddedConvert = null,
            Func<Promise, Promise> promiseToPromise = null, Func<Promise<TConvert>, Promise<TConvert>> promiseToPromiseConvert = null)
        {
            // Add empty delegates so no need for null check.
            onResolve += () => { };
            onReject += s => { };
            onUnknownRejection += () => { };
            onCallbackAddedVoid += _ => { };
            onCallbackAddedConvert += _ => { };
            if (promiseToPromise == null)
            {
                promiseToPromise = _ => Promise.Resolved();
            }
            if (promiseToPromiseConvert == null)
            {
                promiseToPromiseConvert = _ => Promise.Resolved(convertValue);
            }

            onCallbackAddedVoid(promise.Then(() => onResolve(), () => onUnknownRejection()));
            onCallbackAddedVoid(promise.Then(() => onResolve(), (TReject failValue) => onReject(failValue)));
            Promise p3 = null;
            p3 = promise.Then(() => onResolve(), () => { onUnknownRejection(); return promiseToPromise(p3); });
            onCallbackAddedVoid(p3);
            Promise p4 = null;
            p4 = promise.Then(() => onResolve(), (TReject failValue) => { onReject(failValue); return promiseToPromise(p4); });
            onCallbackAddedVoid(p4);

            onCallbackAddedConvert(promise.Then(() => { onResolve(); return convertValue; }, () => { onUnknownRejection(); return convertValue; }));
            onCallbackAddedConvert(promise.Then(() => { onResolve(); return convertValue; }, (TReject failValue) => { onReject(failValue); return convertValue; }));
            Promise<TConvert> p5 = null;
            p5 = promise.Then(() => { onResolve(); return convertValue; }, () => { onUnknownRejection(); return promiseToPromiseConvert(p5); });
            onCallbackAddedConvert(p5);
            Promise<TConvert> p6 = null;
            p6 = promise.Then(() => { onResolve(); return convertValue; }, (TReject failValue) => { onReject(failValue); return promiseToPromiseConvert(p6); });
            onCallbackAddedConvert(p6);

            Promise p7 = null;
            p7 = promise.Then(() => { onResolve(); return promiseToPromise(p7); }, () => { onUnknownRejection(); return promiseToPromise(p7); });
            onCallbackAddedVoid(p7);
            Promise p8 = null;
            p8 = promise.Then(() => { onResolve(); return promiseToPromise(p8); }, (TReject failValue) => { onReject(failValue); return promiseToPromise(p8); });
            onCallbackAddedVoid(p8);
            Promise p9 = null;
            p9 = promise.Then(() => { onResolve(); return promiseToPromise(p9); }, () => onUnknownRejection());
            onCallbackAddedVoid(p9);
            Promise p10 = null;
            p10 = promise.Then(() => { onResolve(); return promiseToPromise(p10); }, (TReject failValue) => onReject(failValue));
            onCallbackAddedVoid(p10);

            Promise<TConvert> p11 = null;
            p11 = promise.Then(() => { onResolve(); return promiseToPromiseConvert(p11); }, () => { onUnknownRejection(); return promiseToPromiseConvert(p11); });
            onCallbackAddedConvert(p11);
            Promise<TConvert> p12 = null;
            p12 = promise.Then(() => { onResolve(); return promiseToPromiseConvert(p12); }, (TReject failValue) => { onReject(failValue); return promiseToPromiseConvert(p12); });
            onCallbackAddedConvert(p12);
            Promise<TConvert> p13 = null;
            p13 = promise.Then(() => { onResolve(); return promiseToPromiseConvert(p13); }, () => { onUnknownRejection(); return convertValue; });
            onCallbackAddedConvert(p13);
            Promise<TConvert> p14 = null;
            p14 = promise.Then(() => { onResolve(); return promiseToPromiseConvert(p14); }, (TReject failValue) => { onReject(failValue); return convertValue; });
            onCallbackAddedConvert(p14);

            onCallbackAddedVoid(promise.Catch(() => onUnknownRejection()));
            onCallbackAddedVoid(promise.Catch((TReject failValue) => onReject(failValue)));

            Promise p15 = null;
            p15 = promise.Catch(() => { onUnknownRejection(); return promiseToPromise(p15); });
            onCallbackAddedVoid(p15);
            Promise p16 = null;
            p16 = promise.Catch((TReject failValue) => { onReject(failValue); return promiseToPromise(p16); });
            onCallbackAddedVoid(p16);
        }

        public static void AddCallbacks<T, TConvert, TReject>(Promise<T> promise,
            Action<T> onResolve = null, Action<TReject> onReject = null, Action onUnknownRejection = null, TConvert convertValue = default(TConvert),
            Action<Promise> onCallbackAddedVoid = null, Action<Promise<TConvert>> onCallbackAddedConvert = null, Action<Promise<T>> onCallbackAddedT = null,
            Func<Promise, Promise> promiseToPromise = null, Func<Promise<TConvert>, Promise<TConvert>> promiseToPromiseConvert = null, Func<Promise<T>, Promise<T>> promiseToPromiseT = null)
        {
            // Add empty delegates so no need for null check.
            onResolve += x => { };
            onReject += s => { };
            onUnknownRejection += () => { };
            onCallbackAddedVoid += _ => { };
            onCallbackAddedConvert += _ => { };
            onCallbackAddedT += _ => { };
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

            onCallbackAddedVoid(promise.Then(x => onResolve(x), () => onUnknownRejection()));
            onCallbackAddedVoid(promise.Then(x => onResolve(x), (TReject failValue) => onReject(failValue)));
            Promise p3 = null;
            p3 = promise.Then(x => onResolve(x), () => { onUnknownRejection(); return promiseToPromise(p3); });
            onCallbackAddedVoid(p3);
            Promise p4 = null;
            p4 = promise.Then(x => onResolve(x), (TReject failValue) => { onReject(failValue); return promiseToPromise(p4); });
            onCallbackAddedVoid(p4);

            onCallbackAddedConvert(promise.Then(x => { onResolve(x); return convertValue; }, () => { onUnknownRejection(); return convertValue; }));
            onCallbackAddedConvert(promise.Then(x => { onResolve(x); return convertValue; }, (TReject failValue) => { onReject(failValue); return convertValue; }));
            Promise<TConvert> p5 = null;
            p5 = promise.Then(x => { onResolve(x); return convertValue; }, () => { onUnknownRejection(); return promiseToPromiseConvert(p5); });
            onCallbackAddedConvert(p5);
            Promise<TConvert> p6 = null;
            p6 = promise.Then(x => { onResolve(x); return convertValue; }, (TReject failValue) => { onReject(failValue); return promiseToPromiseConvert(p6); });
            onCallbackAddedConvert(p6);

            Promise p7 = null;
            p7 = promise.Then(x => { onResolve(x); return promiseToPromise(p7); }, () => { onUnknownRejection(); return promiseToPromise(p7); });
            onCallbackAddedVoid(p7);
            Promise p8 = null;
            p8 = promise.Then(x => { onResolve(x); return promiseToPromise(p8); }, (TReject failValue) => { onReject(failValue); return promiseToPromise(p8); });
            onCallbackAddedVoid(p8);
            Promise p9 = null;
            p9 = promise.Then(x => { onResolve(x); return promiseToPromise(p9); }, () => onUnknownRejection());
            onCallbackAddedVoid(p9);
            Promise p10 = null;
            p10 = promise.Then(x => { onResolve(x); return promiseToPromise(p10); }, (TReject failValue) => onReject(failValue));
            onCallbackAddedVoid(p10);

            Promise<TConvert> p11 = null;
            p11 = promise.Then(x => { onResolve(x); return promiseToPromiseConvert(p11); }, () => { onUnknownRejection(); return promiseToPromiseConvert(p11); });
            onCallbackAddedConvert(p11);
            Promise<TConvert> p12 = null;
            p12 = promise.Then(x => { onResolve(x); return promiseToPromiseConvert(p12); }, (TReject failValue) => { onReject(failValue); return promiseToPromiseConvert(p12); });
            onCallbackAddedConvert(p12);
            Promise<TConvert> p13 = null;
            p13 = promise.Then(x => { onResolve(x); return promiseToPromiseConvert(p13); }, () => { onUnknownRejection(); return convertValue; });
            onCallbackAddedConvert(p13);
            Promise<TConvert> p14 = null;
            p14 = promise.Then(x => { onResolve(x); return promiseToPromiseConvert(p14); }, (TReject failValue) => { onReject(failValue); return convertValue; });
            onCallbackAddedConvert(p14);

            onCallbackAddedT(promise.Catch(() => { onUnknownRejection(); return default(T); }));
            onCallbackAddedT(promise.Catch((TReject failValue) => { onReject(failValue); return default(T); }));

            Promise<T> p15 = null;
            p15 = promise.Catch(() => { onUnknownRejection(); return promiseToPromiseT(p15); });
            onCallbackAddedT(p15);
            Promise<T> p16 = null;
            p16 = promise.Catch((TReject failValue) => { onReject(failValue); return promiseToPromiseT(p16); });
            onCallbackAddedT(p16);
        }

        public static void AddCompleteCallbacks<TConvert>(Promise promise, Action onComplete = null, TConvert convertValue = default(TConvert),
            Action<Promise> onCallbackAddedVoid = null, Action<Promise<TConvert>> onCallbackAddedConvert = null,
            Func<Promise, Promise> promiseToPromise = null, Func<Promise<TConvert>, Promise<TConvert>> promiseToPromiseConvert = null)
        {
            // Add empty delegate so no need for null check.
            onComplete += () => { };
            onCallbackAddedVoid += _ => { };
            onCallbackAddedConvert += _ => { };
            if (promiseToPromise == null)
            {
                promiseToPromise = _ => Promise.Resolved();
            }
            if (promiseToPromiseConvert == null)
            {
                promiseToPromiseConvert = _ => Promise.Resolved(convertValue);
            }

            onCallbackAddedVoid(promise.Complete(() => onComplete()));
            onCallbackAddedConvert(promise.Complete(() => { onComplete(); return convertValue; }));
            Promise p1 = null;
            p1 = promise.Complete(() => { onComplete(); return promiseToPromise(p1); });
            onCallbackAddedVoid(p1);
            Promise<TConvert> p2 = null;
            p2 = promise.Complete(() => { onComplete(); return promiseToPromiseConvert(p2); });
            onCallbackAddedConvert(p2);
        }
    }
}