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

        public const int resolveVoidCallbacks = 24;
        public const int resolveTCallbacks = 24;
        public const int rejectVoidCallbacks = 27;
        public const int rejectTCallbacks = 27;

        public const int catchRejectVoidCallbacks = 33;
        public const int catchRejectTCallbacks = 33;

        public const int completeCallbacks = 6;

        public const int totalVoidCallbacks = 33;
        public const int totalTCallbacks = 33;

        public const int secondCatchVoidCallbacks = 18;
        public const int secondCatchTCallbacks = 18;

        static Action<Promise.Deferred> resolveDeferredAction = deferred => deferred.Resolve();
        static Action<Promise<int>.Deferred> resolveDeferredActionInt = deferred => deferred.Resolve(0);

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
            promise.ThenDefer(() => { onResolve(); return resolveDeferredAction; })
                .Catch(onError)
                .Catch((string s) => { });
            promise.ThenDefer<int>(() => { onResolve(); return resolveDeferredActionInt; })
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

            promise.ThenDefer(() => { onResolve(); return resolveDeferredAction; }, () => { onReject(unknownRejectValue); return resolveDeferredAction; })
                .Catch(onError);
            promise.ThenDefer(() => { onResolve(); return resolveDeferredAction; }, (string failValue) => { onReject(failValue); return resolveDeferredAction; })
                .Catch(onError);
            promise.ThenDefer<string>(() => { onResolve(); return resolveDeferredAction; }, () => { onReject(unknownRejectValue); return resolveDeferredAction; })
                .Catch(onError);

            promise.ThenDefer<int>(() => { onResolve(); return resolveDeferredActionInt; }, () => { onReject(unknownRejectValue); return resolveDeferredActionInt; })
                .Catch(onError);
            promise.ThenDefer<int, string>(() => { onResolve(); return resolveDeferredActionInt; }, (string failValue) => { onReject(failValue); return resolveDeferredActionInt; })
                .Catch(onError);
            promise.ThenDefer<int, string>(() => { onResolve(); return resolveDeferredActionInt; }, () => { onReject(unknownRejectValue); return resolveDeferredActionInt; })
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

            promise.CatchDefer(() => { onReject(unknownRejectValue); return resolveDeferredAction; })
                .Catch(onError);
            promise.CatchDefer((string failValue) => { onReject(failValue); return resolveDeferredAction; })
                .Catch(onError);
            promise.CatchDefer<string>(() => { onReject(unknownRejectValue); return resolveDeferredAction; })
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

            promise.Then(x => onResolve(x))
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
            promise.ThenDefer(x => { onResolve(x); return resolveDeferredAction; })
                .Catch(onError)
                .Catch((string s) => { });
            promise.ThenDefer<int>(x => { onResolve(x); return resolveDeferredActionInt; })
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

            promise.ThenDefer(x => { onResolve(x); return resolveDeferredAction; }, () => { onReject(unknownRejectValue); return resolveDeferredAction; })
                .Catch(onError);
            promise.ThenDefer(x => { onResolve(x); return resolveDeferredAction; }, (string failValue) => { onReject(failValue); return resolveDeferredAction; })
                .Catch(onError);
            promise.ThenDefer<string>(x => { onResolve(x); return resolveDeferredAction; }, () => { onReject(unknownRejectValue); return resolveDeferredAction; })
                .Catch(onError);

            promise.ThenDefer<int>(x => { onResolve(x); return resolveDeferredActionInt; }, () => { onReject(unknownRejectValue); return resolveDeferredActionInt; })
                .Catch(onError);
            promise.ThenDefer<int, string>(x => { onResolve(x); return resolveDeferredActionInt; }, (string failValue) => { onReject(failValue); return resolveDeferredActionInt; })
                .Catch(onError);
            promise.ThenDefer<int, string>(x => { onResolve(x); return resolveDeferredActionInt; }, () => { onReject(unknownRejectValue); return resolveDeferredActionInt; })
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

            Action<Promise<T>.Deferred> resolveDeferredActionT = d => d.Resolve(default(T));

            promise.CatchDefer(() => { onReject(unknownRejectValue); return resolveDeferredActionT; })
                .Catch(onError);
            promise.CatchDefer((string failValue) => { onReject(failValue); return resolveDeferredActionT; })
                .Catch(onError);
            promise.CatchDefer<string>(() => { onReject(unknownRejectValue); return resolveDeferredActionT; })
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
            promise.ThenDefer(() => { onResolve(); return resolveDeferredAction; })
                .Catch((object e) => { if (e is Exception) throw Promise.Rethrow; })
                .Catch(onError);
            promise.ThenDefer<int>(() => { onResolve(); return resolveDeferredActionInt; })
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

            promise.ThenDefer(() => { onResolve(); return resolveDeferredAction; }, () => { onUnknownRejection(); return resolveDeferredAction; })
                .Catch(onError);
            promise.ThenDefer(() => { onResolve(); return resolveDeferredAction; }, (TReject failValue) => { onReject(failValue); return resolveDeferredAction; })
                .Catch(onError);
            promise.ThenDefer<TReject>(() => { onResolve(); return resolveDeferredAction; }, () => { onUnknownRejection(); return resolveDeferredAction; })
                .Catch(onError);

            promise.ThenDefer<int>(() => { onResolve(); return resolveDeferredActionInt; }, () => { onUnknownRejection(); return resolveDeferredActionInt; })
                .Catch(onError);
            promise.ThenDefer<int, TReject>(() => { onResolve(); return resolveDeferredActionInt; }, (TReject failValue) => { onReject(failValue); return resolveDeferredActionInt; })
                .Catch(onError);
            promise.ThenDefer<int, TReject>(() => { onResolve(); return resolveDeferredActionInt; }, () => { onUnknownRejection(); return resolveDeferredActionInt; })
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

            promise.CatchDefer(() => { onUnknownRejection(); return resolveDeferredAction; })
                .Catch(onError);
            promise.CatchDefer((TReject failValue) => { onReject(failValue); return resolveDeferredAction; })
                .Catch(onError);
            promise.CatchDefer<TReject>(() => { onUnknownRejection(); return resolveDeferredAction; })
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
            promise.ThenDefer(x => { onResolve(x); return resolveDeferredAction; })
                .Catch((object e) => { if (e is Exception) throw Promise.Rethrow; })
                .Catch(onError);
            promise.ThenDefer<int>(x => { onResolve(x); return resolveDeferredActionInt; })
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

            promise.ThenDefer(x => { onResolve(x); return resolveDeferredAction; }, () => { onUnknownRejection(); return resolveDeferredAction; })
                .Catch(onError);
            promise.ThenDefer(x => { onResolve(x); return resolveDeferredAction; }, (TReject failValue) => { onReject(failValue); return resolveDeferredAction; })
                .Catch(onError);
            promise.ThenDefer<TReject>(x => { onResolve(x); return resolveDeferredAction; }, () => { onUnknownRejection(); return resolveDeferredAction; })
                .Catch(onError);

            promise.ThenDefer<int>(x => { onResolve(x); return resolveDeferredActionInt; }, () => { onUnknownRejection(); return resolveDeferredActionInt; })
                .Catch(onError);
            promise.ThenDefer<int, TReject>(x => { onResolve(x); return resolveDeferredActionInt; }, (TReject failValue) => { onReject(failValue); return resolveDeferredActionInt; })
                .Catch(onError);
            promise.ThenDefer<int, TReject>(x => { onResolve(x); return resolveDeferredActionInt; }, () => { onUnknownRejection(); return resolveDeferredActionInt; })
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

            Action<Promise<T>.Deferred> resolveDeferredActionT = d => d.Resolve(default(T));

            promise.CatchDefer(() => { onUnknownRejection(); return resolveDeferredActionT; })
                .Catch(onError);
            promise.CatchDefer((TReject failValue) => { onReject(failValue); return resolveDeferredActionT; })
                .Catch(onError);
            promise.CatchDefer<TReject>(() => { onUnknownRejection(); return resolveDeferredActionT; })
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

            promise.ThenDefer(() => { onResolve(); return resolveDeferredAction; }, (TReject0 failValue) => { onReject0(failValue); return resolveDeferredAction; })
                .Catch(onReject1);
            promise.ThenDefer<TReject0>(() => { onResolve(); return resolveDeferredAction; }, () => { onUnknownRejection(); return resolveDeferredAction; })
                .Catch(onReject1);

            promise.ThenDefer<int, TReject0>(() => { onResolve(); return resolveDeferredActionInt; }, (TReject0 failValue) => { onReject0(failValue); return resolveDeferredActionInt; })
                .Catch(onReject1);
            promise.ThenDefer<int, TReject0>(() => { onResolve(); return resolveDeferredActionInt; }, () => { onUnknownRejection(); return resolveDeferredActionInt; })
                .Catch(onReject1);

            promise.Catch((TReject0 failValue) => onReject0(failValue))
                .Catch(onReject1);
            promise.Catch<TReject0>(() => onUnknownRejection())
                .Catch(onReject1);

            promise.Catch((TReject0 failValue) => { onReject0(failValue); return Promise.Resolved(); })
                .Catch(onReject1);
            promise.Catch<TReject0>(() => { onUnknownRejection(); return Promise.Resolved(); })
                .Catch(onReject1);

            promise.CatchDefer((TReject0 failValue) => { onReject0(failValue); return resolveDeferredAction; })
                .Catch(onReject1);
            promise.CatchDefer<TReject0>(() => { onUnknownRejection(); return resolveDeferredAction; })
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

            promise.ThenDefer(x => { onResolve(x); return resolveDeferredAction; }, (TReject0 failValue) => { onReject0(failValue); return resolveDeferredAction; })
                .Catch(onReject1);
            promise.ThenDefer<TReject0>(x => { onResolve(x); return resolveDeferredAction; }, () => { onUnknownRejection(); return resolveDeferredAction; })
                .Catch(onReject1);

            promise.ThenDefer<int, TReject0>(x => { onResolve(x); return resolveDeferredActionInt; }, (TReject0 failValue) => { onReject0(failValue); return resolveDeferredActionInt; })
                .Catch(onReject1);
            promise.ThenDefer<int, TReject0>(x => { onResolve(x); return resolveDeferredActionInt; }, () => { onUnknownRejection(); return resolveDeferredActionInt; })
                .Catch(onReject1);

            promise.Catch((TReject0 failValue) => { onReject0(failValue); return default(T); })
                .Catch(onReject1);
            promise.Catch<TReject0>(() => { onUnknownRejection(); return default(T); })
                .Catch(onReject1);

            promise.Catch((TReject0 failValue) => { onReject0(failValue); return Promise.Resolved(default(T)); })
                .Catch(onReject1);
            promise.Catch<TReject0>(() => { onUnknownRejection(); return Promise.Resolved(default(T)); })
                .Catch(onReject1);

            Action<Promise<T>.Deferred> resolveDeferredActionT = d => d.Resolve(default(T));

            promise.CatchDefer((TReject0 failValue) => { onReject0(failValue); return resolveDeferredActionT; })
                .Catch(onReject1);
            promise.CatchDefer<TReject0>(() => { onUnknownRejection(); return resolveDeferredActionT; })
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
            promise.CompleteDefer(() => { onComplete(); return deferred => deferred.Resolve(); });
            promise.CompleteDefer<int>(() => { onComplete(); return deferred => deferred.Resolve(0); });
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
            promise.ThenDefer(() => { onResolve(); return resolveDeferredAction; })
                .Catch(() => { })
                .CatchCancelation(onCancel);
            promise.ThenDefer<int>(() => { onResolve(); return resolveDeferredActionInt; })
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

            promise.ThenDefer(() => { onResolve(); return resolveDeferredAction; }, () => { onUnknownRejection(); return resolveDeferredAction; })
                .Catch(() => { })
                .CatchCancelation(onCancel);
            promise.ThenDefer(() => { onResolve(); return resolveDeferredAction; }, (TReject failValue) => { onReject(failValue); return resolveDeferredAction; })
                .Catch(() => { })
                .CatchCancelation(onCancel);
            promise.ThenDefer<TReject>(() => { onResolve(); return resolveDeferredAction; }, () => { onUnknownRejection(); return resolveDeferredAction; })
                .Catch(() => { })
                .CatchCancelation(onCancel);

            promise.ThenDefer<int>(() => { onResolve(); return resolveDeferredActionInt; }, () => { onUnknownRejection(); return resolveDeferredActionInt; })
                .Catch(() => { })
                .CatchCancelation(onCancel);
            promise.ThenDefer<int, TReject>(() => { onResolve(); return resolveDeferredActionInt; }, (TReject failValue) => { onReject(failValue); return resolveDeferredActionInt; })
                .Catch(() => { })
                .CatchCancelation(onCancel);
            promise.ThenDefer<int, TReject>(() => { onResolve(); return resolveDeferredActionInt; }, () => { onUnknownRejection(); return resolveDeferredActionInt; })
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

            promise.CatchDefer(() => { onUnknownRejection(); return resolveDeferredAction; })
                .Catch(() => { })
                .CatchCancelation(onCancel);
            promise.CatchDefer((TReject failValue) => { onReject(failValue); return resolveDeferredAction; })
                .Catch(() => { })
                .CatchCancelation(onCancel);
            promise.CatchDefer<TReject>(() => { onUnknownRejection(); return resolveDeferredAction; })
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
            promise.ThenDefer(x => { onResolve(x); return resolveDeferredAction; })
                .Catch(() => { })
                .CatchCancelation(onCancel);
            promise.ThenDefer<int>(x => { onResolve(x); return resolveDeferredActionInt; })
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

            promise.ThenDefer(x => { onResolve(x); return resolveDeferredAction; }, () => { onUnknownRejection(); return resolveDeferredAction; })
                .Catch(() => { })
                .CatchCancelation(onCancel);
            promise.ThenDefer(x => { onResolve(x); return resolveDeferredAction; }, (TReject failValue) => { onReject(failValue); return resolveDeferredAction; })
                .Catch(() => { })
                .CatchCancelation(onCancel);
            promise.ThenDefer<TReject>(x => { onResolve(x); return resolveDeferredAction; }, () => { onUnknownRejection(); return resolveDeferredAction; })
                .Catch(() => { })
                .CatchCancelation(onCancel);

            promise.ThenDefer<int>(x => { onResolve(x); return resolveDeferredActionInt; }, () => { onUnknownRejection(); return resolveDeferredActionInt; })
                .Catch(() => { })
                .CatchCancelation(onCancel);
            promise.ThenDefer<int, TReject>(x => { onResolve(x); return resolveDeferredActionInt; }, (TReject failValue) => { onReject(failValue); return resolveDeferredActionInt; })
                .Catch(() => { })
                .CatchCancelation(onCancel);
            promise.ThenDefer<int, TReject>(x => { onResolve(x); return resolveDeferredActionInt; }, () => { onUnknownRejection(); return resolveDeferredActionInt; })
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

            Action<Promise<T>.Deferred> resolveDeferredActionT = d => d.Resolve(default(T));

            promise.CatchDefer(() => { onUnknownRejection(); return resolveDeferredActionT; })
                .Catch(() => { })
                .CatchCancelation(onCancel);
            promise.CatchDefer((TReject failValue) => { onReject(failValue); return resolveDeferredActionT; })
                .Catch(() => { })
                .CatchCancelation(onCancel);
            promise.CatchDefer<TReject>(() => { onUnknownRejection(); return resolveDeferredActionT; })
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
            promise.ThenDefer(() => { onResolve(); return resolveDeferredAction; })
                .Catch(onUnknownRejection)
                .Catch(onThrown);
            promise.ThenDefer<int>(() => { onResolve(); return resolveDeferredActionInt; })
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

            promise.ThenDefer(() => { onResolve(); return resolveDeferredAction; }, () => { onUnknownRejection(); return resolveDeferredAction; })
                .Catch(onThrown);
            promise.ThenDefer(() => { onResolve(); return resolveDeferredAction; }, (TReject failValue) => { onReject(failValue); return resolveDeferredAction; })
                .Catch(onThrown);
            promise.ThenDefer<TReject>(() => { onResolve(); return resolveDeferredAction; }, () => { onUnknownRejection(); return resolveDeferredAction; })
                .Catch(onThrown);

            promise.ThenDefer<int>(() => { onResolve(); return resolveDeferredActionInt; }, () => { onUnknownRejection(); return resolveDeferredActionInt; })
                .Catch(onThrown);
            promise.ThenDefer<int, TReject>(() => { onResolve(); return resolveDeferredActionInt; }, (TReject failValue) => { onReject(failValue); return resolveDeferredActionInt; })
                .Catch(onThrown);
            promise.ThenDefer<int, TReject>(() => { onResolve(); return resolveDeferredActionInt; }, () => { onUnknownRejection(); return resolveDeferredActionInt; })
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

            promise.CatchDefer(() => { onUnknownRejection(); return resolveDeferredAction; })
                .Catch(onThrown);
            promise.CatchDefer((TReject failValue) => { onReject(failValue); return resolveDeferredAction; })
                .Catch(onThrown);
            promise.CatchDefer<TReject>(() => { onUnknownRejection(); return resolveDeferredAction; })
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
            promise.ThenDefer(x => { onResolve(x); return resolveDeferredAction; })
                .Catch(onUnknownRejection)
                .Catch(onThrown);
            promise.ThenDefer<int>(x => { onResolve(x); return resolveDeferredActionInt; })
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

            promise.ThenDefer(x => { onResolve(x); return resolveDeferredAction; }, () => { onUnknownRejection(); return resolveDeferredAction; })
                .Catch(onThrown);
            promise.ThenDefer(x => { onResolve(x); return resolveDeferredAction; }, (TReject failValue) => { onReject(failValue); return resolveDeferredAction; })
                .Catch(onThrown);
            promise.ThenDefer<TReject>(x => { onResolve(x); return resolveDeferredAction; }, () => { onUnknownRejection(); return resolveDeferredAction; })
                .Catch(onThrown);

            promise.ThenDefer<int>(x => { onResolve(x); return resolveDeferredActionInt; }, () => { onUnknownRejection(); return resolveDeferredActionInt; })
                .Catch(onThrown);
            promise.ThenDefer<int, TReject>(x => { onResolve(x); return resolveDeferredActionInt; }, (TReject failValue) => { onReject(failValue); return resolveDeferredActionInt; })
                .Catch(onThrown);
            promise.ThenDefer<int, TReject>(x => { onResolve(x); return resolveDeferredActionInt; }, () => { onUnknownRejection(); return resolveDeferredActionInt; })
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

            Action<Promise<T>.Deferred> resolveDeferredActionT = d => d.Resolve(default(T));

            promise.CatchDefer(() => { onUnknownRejection(); return resolveDeferredActionT; })
                .Catch(onThrown);
            promise.CatchDefer((TReject failValue) => { onReject(failValue); return resolveDeferredActionT; })
                .Catch(onThrown);
            promise.CatchDefer<TReject>(() => { onUnknownRejection(); return resolveDeferredActionT; })
                .Catch(onThrown);
        }
    }
}