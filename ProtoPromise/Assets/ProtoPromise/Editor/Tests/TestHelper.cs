using System;
using NUnit.Framework;

namespace Proto.Promises.Tests
{
    // These help test all Then/Catch functions at once.
    public class TestHelper
    {
        public const int resolveVoidCallbacks = 24;
        public const int resolveTCallbacks = 24;
        public const int rejectVoidCallbacks = 27;
        public const int rejectTCallbacks = 27;

        public const int rejectVoidKnownCallbacks = 18;
        public const int rejectVoidUnknownCallbacks = 9;
        public const int rejectTKnownCallbacks = 18;
        public const int rejectTUnknownCallbacks = 9;

        static Action<Promise.Deferred> resolveDeferredAction = deferred => deferred.Resolve();
        static Action<Promise<int>.Deferred> resolveDeferredActionInt = deferred => deferred.Resolve(0);

        public static void AddCallbacks(Promise promise, Action onResolve, Action<string> onReject, string unknownRejectValue = "Unknown fail", Action<Exception> onError = null)
        {
            // Add empty delegates so no need for null check.
            onResolve += () => { };
            onReject += s => { };
            onError += e => { };

            promise.Then(() => onResolve())
                .Catch(onError);
            promise.Then(() => { onResolve(); return 0; })
                .Catch(onError);
            promise.Then(() => { onResolve(); return Promise.Resolved(); })
                .Catch(onError);
            promise.Then(() => { onResolve(); return Promise.Resolved(0); })
                .Catch(onError);
            promise.Then(() => { onResolve(); return resolveDeferredAction; })
                .Catch(onError);
            promise.Then(() => { onResolve(); return resolveDeferredActionInt; })
                .Catch(onError);

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

            promise.Then(() => { onResolve(); return resolveDeferredAction; }, () => { onReject(unknownRejectValue); return resolveDeferredAction; })
                .Catch(onError);
            promise.Then(() => { onResolve(); return resolveDeferredAction; }, (string failValue) => { onReject(failValue); return resolveDeferredAction; })
                .Catch(onError);
            promise.Then<string>(() => { onResolve(); return resolveDeferredAction; }, () => { onReject(unknownRejectValue); return resolveDeferredAction; })
                .Catch(onError);

            promise.Then(() => { onResolve(); return resolveDeferredActionInt; }, () => { onReject(unknownRejectValue); return resolveDeferredActionInt; })
                .Catch(onError);
            promise.Then(() => { onResolve(); return resolveDeferredActionInt; }, (string failValue) => { onReject(failValue); return resolveDeferredActionInt; })
                .Catch(onError);
            promise.Then<int, string>(() => { onResolve(); return resolveDeferredActionInt; }, () => { onReject(unknownRejectValue); return resolveDeferredActionInt; })
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

            promise.Catch(() => { onReject(unknownRejectValue); return resolveDeferredAction; })
                .Catch(onError);
            promise.Catch((string failValue) => { onReject(failValue); return resolveDeferredAction; })
                .Catch(onError);
            promise.Catch<string>(() => { onReject(unknownRejectValue); return resolveDeferredAction; })
                .Catch(onError);
        }

        public static void AddCallbacks<T>(Promise<T> promise, Action<T> onResolve, Action<string> onReject, string unknownRejectValue = "Unknown fail", Action<Exception> onError = null)
        {
            // Add empty delegates so no need for null check.
            onResolve += x => { };
            onReject += s => { };
            onError += e => { };

            promise.Then(x => onResolve(x))
                .Catch(onError);
            promise.Then(x => { onResolve(x); return 0; })
                .Catch(onError);
            promise.Then(x => { onResolve(x); return Promise.Resolved(); })
                .Catch(onError);
            promise.Then(x => { onResolve(x); return Promise.Resolved(0); })
                .Catch(onError);
            promise.Then(x => { onResolve(x); return resolveDeferredAction; })
                .Catch(onError);
            promise.Then(x => { onResolve(x); return resolveDeferredActionInt; })
                .Catch(onError);

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

            promise.Then(x => { onResolve(x); return resolveDeferredAction; }, () => { onReject(unknownRejectValue); return resolveDeferredAction; })
                .Catch(onError);
            promise.Then(x => { onResolve(x); return resolveDeferredAction; }, (string failValue) => { onReject(failValue); return resolveDeferredAction; })
                .Catch(onError);
            promise.Then<string>(x => { onResolve(x); return resolveDeferredAction; }, () => { onReject(unknownRejectValue); return resolveDeferredAction; })
                .Catch(onError);

            promise.Then(x => { onResolve(x); return resolveDeferredActionInt; }, () => { onReject(unknownRejectValue); return resolveDeferredActionInt; })
                .Catch(onError);
            promise.Then(x => { onResolve(x); return resolveDeferredActionInt; }, (string failValue) => { onReject(failValue); return resolveDeferredActionInt; })
                .Catch(onError);
            promise.Then<int, string>(x => { onResolve(x); return resolveDeferredActionInt; }, () => { onReject(unknownRejectValue); return resolveDeferredActionInt; })
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

            promise.Catch(() => { onReject(unknownRejectValue); return resolveDeferredActionT; })
                .Catch(onError);
            promise.Catch((string failValue) => { onReject(failValue); return resolveDeferredActionT; })
                .Catch(onError);
            promise.Catch<string>(() => { onReject(unknownRejectValue); return resolveDeferredActionT; })
                .Catch(onError);
        }

        public static void AddCallbacks<TReject>(Promise promise, Action onResolve, Action<TReject> onReject, Action onUnknownRejection, Action<Exception> onError = null)
        {
            // Add empty delegates so no need for null check.
            onResolve += () => { };
            onReject += s => { };
            onUnknownRejection += () => { };
            onError += e => { };

            promise.Then(() => onResolve())
                .Catch(onError);
            promise.Then(() => { onResolve(); return 0; })
                .Catch(onError);
            promise.Then(() => { onResolve(); return Promise.Resolved(); })
                .Catch(onError);
            promise.Then(() => { onResolve(); return Promise.Resolved(0); })
                .Catch(onError);
            promise.Then(() => { onResolve(); return resolveDeferredAction; })
                .Catch(onError);
            promise.Then(() => { onResolve(); return resolveDeferredActionInt; })
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

            promise.Then(() => { onResolve(); return resolveDeferredAction; }, () => { onUnknownRejection(); return resolveDeferredAction; })
                .Catch(onError);
            promise.Then(() => { onResolve(); return resolveDeferredAction; }, (TReject failValue) => { onReject(failValue); return resolveDeferredAction; })
                .Catch(onError);
            promise.Then<TReject>(() => { onResolve(); return resolveDeferredAction; }, () => { onUnknownRejection(); return resolveDeferredAction; })
                .Catch(onError);

            promise.Then(() => { onResolve(); return resolveDeferredActionInt; }, () => { onUnknownRejection(); return resolveDeferredActionInt; })
                .Catch(onError);
            promise.Then(() => { onResolve(); return resolveDeferredActionInt; }, (TReject failValue) => { onReject(failValue); return resolveDeferredActionInt; })
                .Catch(onError);
            promise.Then<int, TReject>(() => { onResolve(); return resolveDeferredActionInt; }, () => { onUnknownRejection(); return resolveDeferredActionInt; })
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

            promise.Catch(() => { onUnknownRejection(); return resolveDeferredAction; })
                .Catch(onError);
            promise.Catch((TReject failValue) => { onReject(failValue); return resolveDeferredAction; })
                .Catch(onError);
            promise.Catch<TReject>(() => { onUnknownRejection(); return resolveDeferredAction; })
                .Catch(onError);
        }

        public static void AddCallbacks<T, TReject>(Promise<T> promise, Action<T> onResolve, Action<TReject> onReject, Action onUnknownRejection, Action<Exception> onError = null)
        {
            // Add empty delegates so no need for null check.
            onResolve += x => { };
            onReject += s => { };
            onUnknownRejection += () => { };
            onError += e => { };

            promise.Then(x => onResolve(x))
                .Catch(onError);
            promise.Then(x => { onResolve(x); return 0; })
                .Catch(onError);
            promise.Then(x => { onResolve(x); return Promise.Resolved(); })
                .Catch(onError);
            promise.Then(x => { onResolve(x); return Promise.Resolved(0); })
                .Catch(onError);
            promise.Then(x => { onResolve(x); return resolveDeferredAction; })
                .Catch(onError);
            promise.Then(x => { onResolve(x); return resolveDeferredActionInt; })
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

            promise.Then(x => { onResolve(x); return resolveDeferredAction; }, () => { onUnknownRejection(); return resolveDeferredAction; })
                .Catch(onError);
            promise.Then(x => { onResolve(x); return resolveDeferredAction; }, (TReject failValue) => { onReject(failValue); return resolveDeferredAction; })
                .Catch(onError);
            promise.Then<TReject>(x => { onResolve(x); return resolveDeferredAction; }, () => { onUnknownRejection(); return resolveDeferredAction; })
                .Catch(onError);

            promise.Then(x => { onResolve(x); return resolveDeferredActionInt; }, () => { onUnknownRejection(); return resolveDeferredActionInt; })
                .Catch(onError);
            promise.Then(x => { onResolve(x); return resolveDeferredActionInt; }, (TReject failValue) => { onReject(failValue); return resolveDeferredActionInt; })
                .Catch(onError);
            promise.Then<int, TReject>(x => { onResolve(x); return resolveDeferredActionInt; }, () => { onUnknownRejection(); return resolveDeferredActionInt; })
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

            promise.Catch(() => { onUnknownRejection(); return resolveDeferredActionT; })
                .Catch(onError);
            promise.Catch((TReject failValue) => { onReject(failValue); return resolveDeferredActionT; })
                .Catch(onError);
            promise.Catch<TReject>(() => { onUnknownRejection(); return resolveDeferredActionT; })
                .Catch(onError);
        }

        public static void AssertRejectType<TReject>(Promise promise)
        {
            int rejectCounter = 0;
            AddCallbacks(promise,
                () => Assert.Fail("Promise was resolved when it should be rejected with InvalidReturnException."),
                (object e) => { Assert.IsInstanceOf<TReject>(e); ++rejectCounter; },
                () => ++rejectCounter
                );
            Promise.Manager.HandleCompletes();

            Assert.AreEqual(rejectVoidCallbacks, rejectCounter);
        }

        public static void AssertRejectType<T, TReject>(Promise<T> promise)
        {
            int rejectCounter = 0;
            AddCallbacks(promise,
                v => Assert.Fail("Promise was resolved when it should be rejected with InvalidReturnException."),
                (object e) => { Assert.IsInstanceOf<TReject>(e); ++rejectCounter; },
                () => ++rejectCounter
                );
            Promise.Manager.HandleCompletes();

            Assert.AreEqual(rejectVoidCallbacks, rejectCounter);
        }
    }
}