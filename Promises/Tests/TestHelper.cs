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

        public const int completeCallbacks = 4;

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
    }
}