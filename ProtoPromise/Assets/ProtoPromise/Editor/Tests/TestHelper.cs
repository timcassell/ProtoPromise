using System;

namespace Proto.Promises.Tests
{
    // These help test all Then/Catch functions at once.
    public class TestHelper
    {
        public const int resolveVoidCallbacks = 24;
        public const int resolveTCallbacks = 24;
        public const int rejectVoidCallbacks = 27;
        public const int rejectTCallbacks = 27;

        static Action<Promise.Deferred> resolveDeferredAction = deferred => deferred.Resolve();
        static Action<Promise<int>.Deferred> resolveDeferredActionInt = deferred => deferred.Resolve(0);

        public static void AddCallbacks(Promise promise, Action onResolve, Action<string> onReject, string unknownRejectValue = "Unknown fail", Action<Exception> onError = null)
        {
            Action suppressor = () => { };

            // Add empty delegates so no need for null check.
            onResolve += suppressor;
            onReject += s => { };
            onError += e => { };

            promise.Then(() => onResolve())
                .Catch(onError)
                .Catch(suppressor);
            promise.Then(() => { onResolve(); return 0; })
                .Catch(onError)
                .Catch(suppressor);
            promise.Then(() => { onResolve(); return Promise.Resolved(); })
                .Catch(onError)
                .Catch(suppressor);
            promise.Then(() => { onResolve(); return Promise.Resolved(0); })
                .Catch(onError)
                .Catch(suppressor);
            promise.Then(() => { onResolve(); return resolveDeferredAction; })
                .Catch(onError)
                .Catch(suppressor);
            promise.Then(() => { onResolve(); return resolveDeferredActionInt; })
                .Catch(onError)
                .Catch(suppressor);

            promise.Then(() => onResolve(), () => onReject(unknownRejectValue))
                .Catch(onError)
                .Catch(suppressor);
            promise.Then(() => onResolve(), (string failValue) => onReject(failValue))
                .Catch(onError)
                .Catch(suppressor);
            promise.Then<string>(() => onResolve(), () => onReject(unknownRejectValue))
                .Catch(onError)
                .Catch(suppressor);

            promise.Then(() => { onResolve(); return 0; }, () => { onReject(unknownRejectValue); return 0; })
                .Catch(onError)
                .Catch(suppressor);
            promise.Then(() => { onResolve(); return 0; }, (string failValue) => { onReject(failValue); return 0; })
                .Catch(onError)
                .Catch(suppressor);
            promise.Then<int, string>(() => { onResolve(); return 0; }, () => { onReject(unknownRejectValue); return 0; })
                .Catch(onError)
                .Catch(suppressor);

            promise.Then(() => { onResolve(); return Promise.Resolved(); }, () => { onReject(unknownRejectValue); return Promise.Resolved(); })
                .Catch(onError)
                .Catch(suppressor);
            promise.Then(() => { onResolve(); return Promise.Resolved(); }, (string failValue) => { onReject(failValue); return Promise.Resolved(); })
                .Catch(onError)
                .Catch(suppressor);
            promise.Then<string>(() => { onResolve(); return Promise.Resolved(); }, () => { onReject(unknownRejectValue); return Promise.Resolved(); })
                .Catch(onError)
                .Catch(suppressor);

            promise.Then(() => { onResolve(); return Promise.Resolved(0); }, () => { onReject(unknownRejectValue); return Promise.Resolved(0); })
                .Catch(onError)
                .Catch(suppressor);
            promise.Then(() => { onResolve(); return Promise.Resolved(0); }, (string failValue) => { onReject(failValue); return Promise.Resolved(0); })
                .Catch(onError)
                .Catch(suppressor);
            promise.Then<int, string>(() => { onResolve(); return Promise.Resolved(0); }, () => { onReject(unknownRejectValue); return Promise.Resolved(0); })
                .Catch(onError)
                .Catch(suppressor);

            promise.Then(() => { onResolve(); return resolveDeferredAction; }, () => { onReject(unknownRejectValue); return resolveDeferredAction; })
                .Catch(onError)
                .Catch(suppressor);
            promise.Then(() => { onResolve(); return resolveDeferredAction; }, (string failValue) => { onReject(failValue); return resolveDeferredAction; })
                .Catch(onError)
                .Catch(suppressor);
            promise.Then<string>(() => { onResolve(); return resolveDeferredAction; }, () => { onReject(unknownRejectValue); return resolveDeferredAction; })
                .Catch(onError)
                .Catch(suppressor);

            promise.Then(() => { onResolve(); return resolveDeferredActionInt; }, () => { onReject(unknownRejectValue); return resolveDeferredActionInt; })
                .Catch(onError)
                .Catch(suppressor);
            promise.Then(() => { onResolve(); return resolveDeferredActionInt; }, (string failValue) => { onReject(failValue); return resolveDeferredActionInt; })
                .Catch(onError)
                .Catch(suppressor);
            promise.Then<int, string>(() => { onResolve(); return resolveDeferredActionInt; }, () => { onReject(unknownRejectValue); return resolveDeferredActionInt; })
                .Catch(onError)
                .Catch(suppressor);

            promise.Catch(() => onReject(unknownRejectValue))
                .Catch(onError)
                .Catch(suppressor);
            promise.Catch((string failValue) => onReject(failValue))
                .Catch(onError)
                .Catch(suppressor);
            promise.Catch<string>(() => onReject(unknownRejectValue))
                .Catch(onError)
                .Catch(suppressor);

            promise.Catch(() => { onReject(unknownRejectValue); return Promise.Resolved(); })
                .Catch(onError)
                .Catch(suppressor);
            promise.Catch((string failValue) => { onReject(failValue); return Promise.Resolved(); })
                .Catch(onError)
                .Catch(suppressor);
            promise.Catch<string>(() => { onReject(unknownRejectValue); return Promise.Resolved(); })
                .Catch(onError)
                .Catch(suppressor);

            promise.Catch(() => { onReject(unknownRejectValue); return resolveDeferredAction; })
                .Catch(onError)
                .Catch(suppressor);
            promise.Catch((string failValue) => { onReject(failValue); return resolveDeferredAction; })
                .Catch(onError)
                .Catch(suppressor);
            promise.Catch<string>(() => { onReject(unknownRejectValue); return resolveDeferredAction; })
                .Catch(onError)
                .Catch(suppressor);
        }

        public static void AddCallbacks<T>(Promise<T> promise, Action<T> onResolve, Action<string> onReject, string unknownRejectValue = "Unknown fail", Action<Exception> onError = null)
        {
            Action suppressor = () => { };

            // Add empty delegates so no need for null check.
            onResolve += x => { };
            onReject += s => { };
            onError += e => { };

            promise.Then(x => onResolve(x))
                .Catch(onError)
                .Catch(suppressor);
            promise.Then(x => { onResolve(x); return 0; })
                .Catch(onError)
                .Catch(suppressor);
            promise.Then(x => { onResolve(x); return Promise.Resolved(); })
                .Catch(onError)
                .Catch(suppressor);
            promise.Then(x => { onResolve(x); return Promise.Resolved(0); })
                .Catch(onError)
                .Catch(suppressor);
            promise.Then(x => { onResolve(x); return resolveDeferredAction; })
                .Catch(onError)
                .Catch(suppressor);
            promise.Then(x => { onResolve(x); return resolveDeferredActionInt; })
                .Catch(onError)
                .Catch(suppressor);

            promise.Then(x => onResolve(x), () => onReject(unknownRejectValue))
                .Catch(onError)
                .Catch(suppressor);
            promise.Then(x => onResolve(x), (string failValue) => onReject(failValue))
                .Catch(onError)
                .Catch(suppressor);
            promise.Then<string>(x => onResolve(x), () => onReject(unknownRejectValue))
                .Catch(onError)
                .Catch(suppressor);

            promise.Then(x => { onResolve(x); return 0; }, () => { onReject(unknownRejectValue); return 0; })
                .Catch(onError)
                .Catch(suppressor);
            promise.Then(x => { onResolve(x); return 0; }, (string failValue) => { onReject(failValue); return 0; })
                .Catch(onError)
                .Catch(suppressor);
            promise.Then<int, string>(x => { onResolve(x); return 0; }, () => { onReject(unknownRejectValue); return 0; })
                .Catch(onError)
                .Catch(suppressor);

            promise.Then(x => { onResolve(x); return Promise.Resolved(); }, () => { onReject(unknownRejectValue); return Promise.Resolved(); })
                .Catch(onError)
                .Catch(suppressor);
            promise.Then(x => { onResolve(x); return Promise.Resolved(); }, (string failValue) => { onReject(failValue); return Promise.Resolved(); })
                .Catch(onError)
                .Catch(suppressor);
            promise.Then<string>(x => { onResolve(x); return Promise.Resolved(); }, () => { onReject(unknownRejectValue); return Promise.Resolved(); })
                .Catch(onError)
                .Catch(suppressor);

            promise.Then(x => { onResolve(x); return Promise.Resolved(0); }, () => { onReject(unknownRejectValue); return Promise.Resolved(0); })
                .Catch(onError)
                .Catch(suppressor);
            promise.Then(x => { onResolve(x); return Promise.Resolved(0); }, (string failValue) => { onReject(failValue); return Promise.Resolved(0); })
                .Catch(onError)
                .Catch(suppressor);
            promise.Then<int, string>(x => { onResolve(x); return Promise.Resolved(0); }, () => { onReject(unknownRejectValue); return Promise.Resolved(0); })
                .Catch(onError)
                .Catch(suppressor);

            promise.Then(x => { onResolve(x); return resolveDeferredAction; }, () => { onReject(unknownRejectValue); return resolveDeferredAction; })
                .Catch(onError)
                .Catch(suppressor);
            promise.Then(x => { onResolve(x); return resolveDeferredAction; }, (string failValue) => { onReject(failValue); return resolveDeferredAction; })
                .Catch(onError)
                .Catch(suppressor);
            promise.Then<string>(x => { onResolve(x); return resolveDeferredAction; }, () => { onReject(unknownRejectValue); return resolveDeferredAction; })
                .Catch(onError)
                .Catch(suppressor);

            promise.Then(x => { onResolve(x); return resolveDeferredActionInt; }, () => { onReject(unknownRejectValue); return resolveDeferredActionInt; })
                .Catch(onError)
                .Catch(suppressor);
            promise.Then(x => { onResolve(x); return resolveDeferredActionInt; }, (string failValue) => { onReject(failValue); return resolveDeferredActionInt; })
                .Catch(onError)
                .Catch(suppressor);
            promise.Then<int, string>(x => { onResolve(x); return resolveDeferredActionInt; }, () => { onReject(unknownRejectValue); return resolveDeferredActionInt; })
                .Catch(onError)
                .Catch(suppressor);

            promise.Catch(() => { onReject(unknownRejectValue); return default(T); })
                .Catch(onError)
                .Catch(suppressor);
            promise.Catch((string failValue) => { onReject(failValue); return default(T); })
                .Catch(onError)
                .Catch(suppressor);
            promise.Catch<string>(() => { onReject(unknownRejectValue); return default(T); })
                .Catch(onError)
                .Catch(suppressor);

            promise.Catch(() => { onReject(unknownRejectValue); return Promise.Resolved(default(T)); })
                .Catch(onError)
                .Catch(suppressor);
            promise.Catch((string failValue) => { onReject(failValue); return Promise.Resolved(default(T)); })
                .Catch(onError)
                .Catch(suppressor);
            promise.Catch<string>(() => { onReject(unknownRejectValue); return Promise.Resolved(default(T)); })
                .Catch(onError)
                .Catch(suppressor);

            Action<Promise<T>.Deferred> resolveDeferredActionT = d => d.Resolve(default(T));

            promise.Catch(() => { onReject(unknownRejectValue); return resolveDeferredActionT; })
                .Catch(onError)
                .Catch(suppressor);
            promise.Catch((string failValue) => { onReject(failValue); return resolveDeferredActionT; })
                .Catch(onError)
                .Catch(suppressor);
            promise.Catch<string>(() => { onReject(unknownRejectValue); return resolveDeferredActionT; })
                .Catch(onError)
                .Catch(suppressor);
        }

    }
}