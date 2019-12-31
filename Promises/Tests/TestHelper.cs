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

        public const int resolveVoidCallbacks = 16;
        public const int resolveTCallbacks = 16;
        public const int rejectVoidCallbacks = 18;
        public const int rejectTCallbacks = 18;

        public const int catchRejectVoidCallbacks = 22;
        public const int catchRejectTCallbacks = 22;

        public const int completeCallbacks = 4;

        public const int totalVoidCallbacks = 25;
        public const int totalTCallbacks = 25;

        public const int secondCatchVoidCallbacks = 12;
        public const int secondCatchTCallbacks = 12;

        public static void AddCallbacks(Promise promise, Action onResolve, Action<string> onReject, string unknownRejectValue = "Unknown fail", Action<Exception> onError = null)
        {
            // Add empty delegates so no need for null check.
            onResolve += () => { };
            onReject += s => { };
            if (onError == null)
            {
                onError = e => { throw Promise.Rethrow; };
            }

            promise.Then(() => onResolve())
                .Catch(onError)
                .Catch((string s) => { });
            promise.Then(() => { onResolve(); return 0; })
                .Catch(onError)
                .Catch((string s) => { });
            promise.Then(() => { onResolve(); return Promise.Resolved(); })
                .Catch(onError)
                .Catch((string s) => { });
            promise.Then(() => { onResolve(); return Promise.Resolved(0); })
                .Catch(onError)
                .Catch((string s) => { });

            promise.Then(() => onResolve(), () => onReject(unknownRejectValue))
                .Catch(onError);
            promise.Then(() => onResolve(), (string failValue) => onReject(failValue))
                .Catch(onError);
            promise.Then<string>(() => onResolve(), () => onReject(unknownRejectValue))
                .Catch(onError);

            promise.Then(() => { onResolve(); return 0; }, () => { onReject(unknownRejectValue); return 0; })
                .Catch(onError);
            promise.Then(() => { onResolve(); return 0; }, (string failValue) => { onReject(failValue); return 0; })
                .Catch(onError);
            promise.Then<int, string>(() => { onResolve(); return 0; }, () => { onReject(unknownRejectValue); return 0; })
                .Catch(onError);

            promise.Then(() => { onResolve(); return Promise.Resolved(); }, () => { onReject(unknownRejectValue); return Promise.Resolved(); })
                .Catch(onError);
            promise.Then(() => { onResolve(); return Promise.Resolved(); }, (string failValue) => { onReject(failValue); return Promise.Resolved(); })
                .Catch(onError);
            promise.Then<string>(() => { onResolve(); return Promise.Resolved(); }, () => { onReject(unknownRejectValue); return Promise.Resolved(); })
                .Catch(onError);

            promise.Then(() => { onResolve(); return Promise.Resolved(0); }, () => { onReject(unknownRejectValue); return Promise.Resolved(0); })
                .Catch(onError);
            promise.Then(() => { onResolve(); return Promise.Resolved(0); }, (string failValue) => { onReject(failValue); return Promise.Resolved(0); })
                .Catch(onError);
            promise.Then<int, string>(() => { onResolve(); return Promise.Resolved(0); }, () => { onReject(unknownRejectValue); return Promise.Resolved(0); })
                .Catch(onError);

            promise.Catch(() => onReject(unknownRejectValue))
                .Catch(onError);
            promise.Catch((string failValue) => onReject(failValue))
                .Catch(onError);
            promise.Catch<string>(() => onReject(unknownRejectValue))
                .Catch(onError);

            promise.Catch(() => { onReject(unknownRejectValue); return Promise.Resolved(); })
                .Catch(onError);
            promise.Catch((string failValue) => { onReject(failValue); return Promise.Resolved(); })
                .Catch(onError);
            promise.Catch<string>(() => { onReject(unknownRejectValue); return Promise.Resolved(); })
                .Catch(onError);
        }

        public static void AddCallbacks<T>(Promise<T> promise, Action<T> onResolve, Action<string> onReject, string unknownRejectValue = "Unknown fail", Action<Exception> onError = null)
        {
            // Add empty delegates so no need for null check.
            onResolve += x => { };
            onReject += s => { };
            if (onError == null)
            {
                onError = e => { throw Promise.Rethrow; };
            }

            promise.Then(x => { onResolve(x); })
                .Catch(onError)
                .Catch((string s) => { });
            promise.Then(x => { onResolve(x); return 0; })
                .Catch(onError)
                .Catch((string s) => { });
            promise.Then(x => { onResolve(x); return Promise.Resolved(); })
                .Catch(onError)
                .Catch((string s) => { });
            promise.Then(x => { onResolve(x); return Promise.Resolved(0); })
                .Catch(onError)
                .Catch((string s) => { });

            promise.Then(x => onResolve(x), () => onReject(unknownRejectValue))
                .Catch(onError);
            promise.Then(x => onResolve(x), (string failValue) => onReject(failValue))
                .Catch(onError);
            promise.Then<string>(x => onResolve(x), () => onReject(unknownRejectValue))
                .Catch(onError);

            promise.Then(x => { onResolve(x); return 0; }, () => { onReject(unknownRejectValue); return 0; })
                .Catch(onError);
            promise.Then(x => { onResolve(x); return 0; }, (string failValue) => { onReject(failValue); return 0; })
                .Catch(onError);
            promise.Then<int, string>(x => { onResolve(x); return 0; }, () => { onReject(unknownRejectValue); return 0; })
                .Catch(onError);

            promise.Then(x => { onResolve(x); return Promise.Resolved(); }, () => { onReject(unknownRejectValue); return Promise.Resolved(); })
                .Catch(onError);
            promise.Then(x => { onResolve(x); return Promise.Resolved(); }, (string failValue) => { onReject(failValue); return Promise.Resolved(); })
                .Catch(onError);
            promise.Then<string>(x => { onResolve(x); return Promise.Resolved(); }, () => { onReject(unknownRejectValue); return Promise.Resolved(); })
                .Catch(onError);

            promise.Then(x => { onResolve(x); return Promise.Resolved(0); }, () => { onReject(unknownRejectValue); return Promise.Resolved(0); })
                .Catch(onError);
            promise.Then(x => { onResolve(x); return Promise.Resolved(0); }, (string failValue) => { onReject(failValue); return Promise.Resolved(0); })
                .Catch(onError);
            promise.Then<int, string>(x => { onResolve(x); return Promise.Resolved(0); }, () => { onReject(unknownRejectValue); return Promise.Resolved(0); })
                .Catch(onError);

            promise.Catch(() => { onReject(unknownRejectValue); return default(T); })
                .Catch(onError);
            promise.Catch((string failValue) => { onReject(failValue); return default(T); })
                .Catch(onError);
            promise.Catch<string>(() => { onReject(unknownRejectValue); return default(T); })
                .Catch(onError);

            promise.Catch(() => { onReject(unknownRejectValue); return Promise.Resolved(default(T)); })
                .Catch(onError);
            promise.Catch((string failValue) => { onReject(failValue); return Promise.Resolved(default(T)); })
                .Catch(onError);
            promise.Catch<string>(() => { onReject(unknownRejectValue); return Promise.Resolved(default(T)); })
                .Catch(onError);
        }

        public static void AddCallbacks<TReject>(Promise promise, Action onResolve, Action<TReject> onReject, Action onUnknownRejection, Action<Exception> onError = null)
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
            promise.Then<TReject>(() => onResolve(), () => onUnknownRejection())
                .Catch(onError);

            promise.Then(() => { onResolve(); return 0; }, () => { onUnknownRejection(); return 0; })
                .Catch(onError);
            promise.Then(() => { onResolve(); return 0; }, (TReject failValue) => { onReject(failValue); return 0; })
                .Catch(onError);
            promise.Then<int, TReject>(() => { onResolve(); return 0; }, () => { onUnknownRejection(); return 0; })
                .Catch(onError);

            promise.Then(() => { onResolve(); return Promise.Resolved(); }, () => { onUnknownRejection(); return Promise.Resolved(); })
                .Catch(onError);
            promise.Then(() => { onResolve(); return Promise.Resolved(); }, (TReject failValue) => { onReject(failValue); return Promise.Resolved(); })
                .Catch(onError);
            promise.Then<TReject>(() => { onResolve(); return Promise.Resolved(); }, () => { onUnknownRejection(); return Promise.Resolved(); })
                .Catch(onError);

            promise.Then(() => { onResolve(); return Promise.Resolved(0); }, () => { onUnknownRejection(); return Promise.Resolved(0); })
                .Catch(onError);
            promise.Then(() => { onResolve(); return Promise.Resolved(0); }, (TReject failValue) => { onReject(failValue); return Promise.Resolved(0); })
                .Catch(onError);
            promise.Then<int, TReject>(() => { onResolve(); return Promise.Resolved(0); }, () => { onUnknownRejection(); return Promise.Resolved(0); })
                .Catch(onError);

            promise.Catch(() => onUnknownRejection())
                .Catch(onError);
            promise.Catch((TReject failValue) => onReject(failValue))
                .Catch(onError);
            promise.Catch<TReject>(() => onUnknownRejection())
                .Catch(onError);

            promise.Catch(() => { onUnknownRejection(); return Promise.Resolved(); })
                .Catch(onError);
            promise.Catch((TReject failValue) => { onReject(failValue); return Promise.Resolved(); })
                .Catch(onError);
            promise.Catch<TReject>(() => { onUnknownRejection(); return Promise.Resolved(); })
                .Catch(onError);
        }

        public static void AddCallbacks<T, TReject>(Promise<T> promise, Action<T> onResolve, Action<TReject> onReject, Action onUnknownRejection, Action<Exception> onError = null)
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
            promise.Then<TReject>(x => onResolve(x), () => onUnknownRejection())
                .Catch(onError);

            promise.Then(x => { onResolve(x); return 0; }, () => { onUnknownRejection(); return 0; })
                .Catch(onError);
            promise.Then(x => { onResolve(x); return 0; }, (TReject failValue) => { onReject(failValue); return 0; })
                .Catch(onError);
            promise.Then<int, TReject>(x => { onResolve(x); return 0; }, () => { onUnknownRejection(); return 0; })
                .Catch(onError);

            promise.Then(x => { onResolve(x); return Promise.Resolved(); }, () => { onUnknownRejection(); return Promise.Resolved(); })
                .Catch(onError);
            promise.Then(x => { onResolve(x); return Promise.Resolved(); }, (TReject failValue) => { onReject(failValue); return Promise.Resolved(); })
                .Catch(onError);
            promise.Then<TReject>(x => { onResolve(x); return Promise.Resolved(); }, () => { onUnknownRejection(); return Promise.Resolved(); })
                .Catch(onError);

            promise.Then(x => { onResolve(x); return Promise.Resolved(0); }, () => { onUnknownRejection(); return Promise.Resolved(0); })
                .Catch(onError);
            promise.Then(x => { onResolve(x); return Promise.Resolved(0); }, (TReject failValue) => { onReject(failValue); return Promise.Resolved(0); })
                .Catch(onError);
            promise.Then<int, TReject>(x => { onResolve(x); return Promise.Resolved(0); }, () => { onUnknownRejection(); return Promise.Resolved(0); })
                .Catch(onError);

            promise.Catch(() => { onUnknownRejection(); return default(T); })
                .Catch(onError);
            promise.Catch((TReject failValue) => { onReject(failValue); return default(T); })
                .Catch(onError);
            promise.Catch<TReject>(() => { onUnknownRejection(); return default(T); })
                .Catch(onError);

            promise.Catch(() => { onUnknownRejection(); return Promise.Resolved(default(T)); })
                .Catch(onError);
            promise.Catch((TReject failValue) => { onReject(failValue); return Promise.Resolved(default(T)); })
                .Catch(onError);
            promise.Catch<TReject>(() => { onUnknownRejection(); return Promise.Resolved(default(T)); })
                .Catch(onError);
        }

        public static void AddCatchCallbacks<TReject0, TReject1>(Promise promise, Action<TReject0> onReject0, Action<TReject1> onReject1)
        {
            // Add empty delegates so no need for null check.
            onReject0 += v => { };
            onReject1 += v => { };
            Action onResolve = () => { };
            Action onUnknownRejection = () => onReject0(default(TReject0));

            promise.Then(onResolve, (TReject0 failValue) => onReject0(failValue))
                .Catch(onReject1);
            promise.Then<TReject0>(onResolve, onUnknownRejection)
                .Catch(onReject1);

            promise.Then(() => { onResolve(); return 0; }, (TReject0 failValue) => { onReject0(failValue); return 0; })
                .Catch(onReject1);
            promise.Then<int, TReject0>(() => { onResolve(); return 0; }, () => { onUnknownRejection(); return 0; })
                .Catch(onReject1);

            promise.Then(() => { onResolve(); return Promise.Resolved(); }, (TReject0 failValue) => { onReject0(failValue); return Promise.Resolved(); })
                .Catch(onReject1);
            promise.Then<TReject0>(() => { onResolve(); return Promise.Resolved(); }, () => { onUnknownRejection(); return Promise.Resolved(); })
                .Catch(onReject1);

            promise.Then(() => { onResolve(); return Promise.Resolved(0); }, (TReject0 failValue) => { onReject0(failValue); return Promise.Resolved(0); })
                .Catch(onReject1);
            promise.Then<int, TReject0>(() => { onResolve(); return Promise.Resolved(0); }, () => { onUnknownRejection(); return Promise.Resolved(0); })
                .Catch(onReject1);

            promise.Catch((TReject0 failValue) => onReject0(failValue))
                .Catch(onReject1);
            promise.Catch<TReject0>(() => onUnknownRejection())
                .Catch(onReject1);

            promise.Catch((TReject0 failValue) => { onReject0(failValue); return Promise.Resolved(); })
                .Catch(onReject1);
            promise.Catch<TReject0>(() => { onUnknownRejection(); return Promise.Resolved(); })
                .Catch(onReject1);
        }

        public static void AddCatchCallbacks<T, TReject0, TReject1>(Promise<T> promise, Action<TReject0> onReject0, Action<TReject1> onReject1)
        {
            // Add empty delegates so no need for null check.
            onReject0 += v => { };
            onReject1 += v => { };
            Action<T> onResolve = v => { };
            Action onUnknownRejection = () => onReject0(default(TReject0));

            promise.Then(x => onResolve(x), (TReject0 failValue) => onReject0(failValue))
                .Catch(onReject1);
            promise.Then<TReject0>(x => onResolve(x), () => onUnknownRejection())
                .Catch(onReject1);

            promise.Then(x => { onResolve(x); return 0; }, (TReject0 failValue) => { onReject0(failValue); return 0; })
                .Catch(onReject1);
            promise.Then<int, TReject0>(x => { onResolve(x); return 0; }, () => { onUnknownRejection(); return 0; })
                .Catch(onReject1);

            promise.Then(x => { onResolve(x); return Promise.Resolved(); }, (TReject0 failValue) => { onReject0(failValue); return Promise.Resolved(); })
                .Catch(onReject1);
            promise.Then<TReject0>(x => { onResolve(x); return Promise.Resolved(); }, () => { onUnknownRejection(); return Promise.Resolved(); })
                .Catch(onReject1);

            promise.Then(x => { onResolve(x); return Promise.Resolved(0); }, (TReject0 failValue) => { onReject0(failValue); return Promise.Resolved(0); })
                .Catch(onReject1);
            promise.Then<int, TReject0>(x => { onResolve(x); return Promise.Resolved(0); }, () => { onUnknownRejection(); return Promise.Resolved(0); })
                .Catch(onReject1);

            promise.Catch((TReject0 failValue) => { onReject0(failValue); return default(T); })
                .Catch(onReject1);
            promise.Catch<TReject0>(() => { onUnknownRejection(); return default(T); })
                .Catch(onReject1);

            promise.Catch((TReject0 failValue) => { onReject0(failValue); return Promise.Resolved(default(T)); })
                .Catch(onReject1);
            promise.Catch<TReject0>(() => { onUnknownRejection(); return Promise.Resolved(default(T)); })
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

        public static void AssertIgnore(Promise promise, int expectedResolveCount, int expectedRejectCount, params Action[] ignoreActions)
        {
            foreach (var action in ignoreActions)
            {
                int resolveCounter = 0;
                int rejectCounter = 0;
                action.Invoke();
                Assert.AreEqual(0, resolveCounter);
                Assert.AreEqual(0, rejectCounter);
                AddCallbacks(promise, () => ++resolveCounter, s => ++rejectCounter);
                Promise.Manager.HandleCompletes();
                Assert.AreEqual(expectedResolveCount, resolveCounter);
                Assert.AreEqual(expectedRejectCount, rejectCounter);
            }
        }

        public static void AssertIgnore<T>(Promise<T> promise, int expectedResolveCount, int expectedRejectCount, params Action[] ignoreActions)
        {
            foreach (var action in ignoreActions)
            {
                int resolveCounter = 0;
                int rejectCounter = 0;
                action.Invoke();
                Assert.AreEqual(0, resolveCounter);
                Assert.AreEqual(0, rejectCounter);
                AddCallbacks(promise, v => ++resolveCounter, s => ++rejectCounter);
                Promise.Manager.HandleCompletes();
                Assert.AreEqual(expectedResolveCount, resolveCounter);
                Assert.AreEqual(expectedRejectCount, rejectCounter);
            }
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
            promise.Then<TReject>(() => onResolve(), () => onUnknownRejection())
                .Catch(() => { })
                .CatchCancelation(onCancel);

            promise.Then(() => { onResolve(); return 0; }, () => { onUnknownRejection(); return 0; })
                .Catch(() => { })
                .CatchCancelation(onCancel);
            promise.Then(() => { onResolve(); return 0; }, (TReject failValue) => { onReject(failValue); return 0; })
                .Catch(() => { })
                .CatchCancelation(onCancel);
            promise.Then<int, TReject>(() => { onResolve(); return 0; }, () => { onUnknownRejection(); return 0; })
                .Catch(() => { })
                .CatchCancelation(onCancel);

            promise.Then(() => { onResolve(); return Promise.Resolved(); }, () => { onUnknownRejection(); return Promise.Resolved(); })
                .Catch(() => { })
                .CatchCancelation(onCancel);
            promise.Then(() => { onResolve(); return Promise.Resolved(); }, (TReject failValue) => { onReject(failValue); return Promise.Resolved(); })
                .Catch(() => { })
                .CatchCancelation(onCancel);
            promise.Then<TReject>(() => { onResolve(); return Promise.Resolved(); }, () => { onUnknownRejection(); return Promise.Resolved(); })
                .Catch(() => { })
                .CatchCancelation(onCancel);

            promise.Then(() => { onResolve(); return Promise.Resolved(0); }, () => { onUnknownRejection(); return Promise.Resolved(0); })
                .Catch(() => { })
                .CatchCancelation(onCancel);
            promise.Then(() => { onResolve(); return Promise.Resolved(0); }, (TReject failValue) => { onReject(failValue); return Promise.Resolved(0); })
                .Catch(() => { })
                .CatchCancelation(onCancel);
            promise.Then<int, TReject>(() => { onResolve(); return Promise.Resolved(0); }, () => { onUnknownRejection(); return Promise.Resolved(0); })
                .Catch(() => { })
                .CatchCancelation(onCancel);

            promise.Catch(() => onUnknownRejection())
                .Catch(() => { })
                .CatchCancelation(onCancel);
            promise.Catch((TReject failValue) => onReject(failValue))
                .Catch(() => { })
                .CatchCancelation(onCancel);
            promise.Catch<TReject>(() => onUnknownRejection())
                .Catch(() => { })
                .CatchCancelation(onCancel);

            promise.Catch(() => { onUnknownRejection(); return Promise.Resolved(); })
                .Catch(() => { })
                .CatchCancelation(onCancel);
            promise.Catch((TReject failValue) => { onReject(failValue); return Promise.Resolved(); })
                .Catch(() => { })
                .CatchCancelation(onCancel);
            promise.Catch<TReject>(() => { onUnknownRejection(); return Promise.Resolved(); })
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
            promise.Then<TReject>(x => onResolve(x), () => onUnknownRejection())
                .Catch(() => { })
                .CatchCancelation(onCancel);

            promise.Then(x => { onResolve(x); return 0; }, () => { onUnknownRejection(); return 0; })
                .Catch(() => { })
                .CatchCancelation(onCancel);
            promise.Then(x => { onResolve(x); return 0; }, (TReject failValue) => { onReject(failValue); return 0; })
                .Catch(() => { })
                .CatchCancelation(onCancel);
            promise.Then<int, TReject>(x => { onResolve(x); return 0; }, () => { onUnknownRejection(); return 0; })
                .Catch(() => { })
                .CatchCancelation(onCancel);

            promise.Then(x => { onResolve(x); return Promise.Resolved(); }, () => { onUnknownRejection(); return Promise.Resolved(); })
                .Catch(() => { })
                .CatchCancelation(onCancel);
            promise.Then(x => { onResolve(x); return Promise.Resolved(); }, (TReject failValue) => { onReject(failValue); return Promise.Resolved(); })
                .Catch(() => { })
                .CatchCancelation(onCancel);
            promise.Then<TReject>(x => { onResolve(x); return Promise.Resolved(); }, () => { onUnknownRejection(); return Promise.Resolved(); })
                .Catch(() => { })
                .CatchCancelation(onCancel);

            promise.Then(x => { onResolve(x); return Promise.Resolved(0); }, () => { onUnknownRejection(); return Promise.Resolved(0); })
                .Catch(() => { })
                .CatchCancelation(onCancel);
            promise.Then(x => { onResolve(x); return Promise.Resolved(0); }, (TReject failValue) => { onReject(failValue); return Promise.Resolved(0); })
                .Catch(() => { })
                .CatchCancelation(onCancel);
            promise.Then<int, TReject>(x => { onResolve(x); return Promise.Resolved(0); }, () => { onUnknownRejection(); return Promise.Resolved(0); })
                .Catch(() => { })
                .CatchCancelation(onCancel);

            promise.Catch(() => { onUnknownRejection(); return default(T); })
                .Catch(() => { })
                .CatchCancelation(onCancel);
            promise.Catch((TReject failValue) => { onReject(failValue); return default(T); })
                .Catch(() => { })
                .CatchCancelation(onCancel);
            promise.Catch<TReject>(() => { onUnknownRejection(); return default(T); })
                .Catch(() => { })
                .CatchCancelation(onCancel);

            promise.Catch(() => { onUnknownRejection(); return Promise.Resolved(default(T)); })
                .Catch(() => { })
                .CatchCancelation(onCancel);
            promise.Catch((TReject failValue) => { onReject(failValue); return Promise.Resolved(default(T)); })
                .Catch(() => { })
                .CatchCancelation(onCancel);
            promise.Catch<TReject>(() => { onUnknownRejection(); return Promise.Resolved(default(T)); })
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
            promise.Then<TReject>(() => onResolve(), () => onUnknownRejection())
                .Catch(onThrown);

            promise.Then(() => { onResolve(); return 0; }, () => { onUnknownRejection(); return 0; })
                .Catch(onThrown);
            promise.Then(() => { onResolve(); return 0; }, (TReject failValue) => { onReject(failValue); return 0; })
                .Catch(onThrown);
            promise.Then<int, TReject>(() => { onResolve(); return 0; }, () => { onUnknownRejection(); return 0; })
                .Catch(onThrown);

            promise.Then(() => { onResolve(); return Promise.Resolved(); }, () => { onUnknownRejection(); return Promise.Resolved(); })
                .Catch(onThrown);
            promise.Then(() => { onResolve(); return Promise.Resolved(); }, (TReject failValue) => { onReject(failValue); return Promise.Resolved(); })
                .Catch(onThrown);
            promise.Then<TReject>(() => { onResolve(); return Promise.Resolved(); }, () => { onUnknownRejection(); return Promise.Resolved(); })
                .Catch(onThrown);

            promise.Then(() => { onResolve(); return Promise.Resolved(0); }, () => { onUnknownRejection(); return Promise.Resolved(0); })
                .Catch(onThrown);
            promise.Then(() => { onResolve(); return Promise.Resolved(0); }, (TReject failValue) => { onReject(failValue); return Promise.Resolved(0); })
                .Catch(onThrown);
            promise.Then<int, TReject>(() => { onResolve(); return Promise.Resolved(0); }, () => { onUnknownRejection(); return Promise.Resolved(0); })
                .Catch(onThrown);

            promise.Catch(() => onUnknownRejection())
                .Catch(onThrown);
            promise.Catch((TReject failValue) => onReject(failValue))
                .Catch(onThrown);
            promise.Catch<TReject>(() => onUnknownRejection())
                .Catch(onThrown);

            promise.Catch(() => { onUnknownRejection(); return Promise.Resolved(); })
                .Catch(onThrown);
            promise.Catch((TReject failValue) => { onReject(failValue); return Promise.Resolved(); })
                .Catch(onThrown);
            promise.Catch<TReject>(() => { onUnknownRejection(); return Promise.Resolved(); })
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
            promise.Then<TReject>(x => onResolve(x), () => onUnknownRejection())
                .Catch(onThrown);

            promise.Then(x => { onResolve(x); return 0; }, () => { onUnknownRejection(); return 0; })
                .Catch(onThrown);
            promise.Then(x => { onResolve(x); return 0; }, (TReject failValue) => { onReject(failValue); return 0; })
                .Catch(onThrown);
            promise.Then<int, TReject>(x => { onResolve(x); return 0; }, () => { onUnknownRejection(); return 0; })
                .Catch(onThrown);

            promise.Then(x => { onResolve(x); return Promise.Resolved(); }, () => { onUnknownRejection(); return Promise.Resolved(); })
                .Catch(onThrown);
            promise.Then(x => { onResolve(x); return Promise.Resolved(); }, (TReject failValue) => { onReject(failValue); return Promise.Resolved(); })
                .Catch(onThrown);
            promise.Then<TReject>(x => { onResolve(x); return Promise.Resolved(); }, () => { onUnknownRejection(); return Promise.Resolved(); })
                .Catch(onThrown);

            promise.Then(x => { onResolve(x); return Promise.Resolved(0); }, () => { onUnknownRejection(); return Promise.Resolved(0); })
                .Catch(onThrown);
            promise.Then(x => { onResolve(x); return Promise.Resolved(0); }, (TReject failValue) => { onReject(failValue); return Promise.Resolved(0); })
                .Catch(onThrown);
            promise.Then<int, TReject>(x => { onResolve(x); return Promise.Resolved(0); }, () => { onUnknownRejection(); return Promise.Resolved(0); })
                .Catch(onThrown);

            promise.Catch(() => { onUnknownRejection(); return default(T); })
                .Catch(onThrown);
            promise.Catch((TReject failValue) => { onReject(failValue); return default(T); })
                .Catch(onThrown);
            promise.Catch<TReject>(() => { onUnknownRejection(); return default(T); })
                .Catch(onThrown);

            promise.Catch(() => { onUnknownRejection(); return Promise.Resolved(default(T)); })
                .Catch(onThrown);
            promise.Catch((TReject failValue) => { onReject(failValue); return Promise.Resolved(default(T)); })
                .Catch(onThrown);
            promise.Catch<TReject>(() => { onUnknownRejection(); return Promise.Resolved(default(T)); })
                .Catch(onThrown);
        }
    }
}