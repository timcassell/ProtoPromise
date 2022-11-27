﻿#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif
#if !PROTO_PROMISE_PROGRESS_DISABLE
#define PROMISE_PROGRESS
#else
#undef PROMISE_PROGRESS
# endif

using NUnit.Framework;
using Proto.Promises;
using ProtoPromiseTests.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ProtoPromiseTests.APIs
{
    public class MiscellaneousTests
    {
        [SetUp]
        public void Setup()
        {
            TestHelper.Setup();
        }

        [TearDown]
        public void Teardown()
        {
            TestHelper.Cleanup();

            TestHelper.s_expectedUncaughtRejectValue = null;
        }

        [Test]
        public void PromiseIsInvalidAfterAwaited_void()
        {
            var deferred = Promise.NewDeferred();
            var promise = deferred.Promise;

            Assert.IsTrue(promise.IsValid);

            promise.Then(() => { }).Forget();

            Assert.IsFalse(promise.IsValid);

#if CSHARP_7_3_OR_NEWER && PROMISE_DEBUG
            Assert.Throws<InvalidOperationException>(() => promise.GetAwaiter());
#endif
            Assert.Throws<InvalidOperationException>(() => promise.Preserve());
            Assert.Throws<InvalidOperationException>(() => promise.Forget());
            Assert.Throws<InvalidOperationException>(() => promise.Duplicate());

#if PROMISE_PROGRESS
            Assert.Throws<InvalidOperationException>(() => promise.Progress(v => { }));
            Assert.Throws<InvalidOperationException>(() => promise.Progress(1, (cv, v) => { }));
#endif
            Assert.Throws<InvalidOperationException>(() => promise.CatchCancelation(() => { }));
            Assert.Throws<InvalidOperationException>(() => promise.CatchCancelation(1, cv => { }));

            Assert.Throws<InvalidOperationException>(() => promise.Finally(() => { }));
            Assert.Throws<InvalidOperationException>(() => promise.Finally(1, cv => { }));

            Assert.Throws<InvalidOperationException>(() => promise.ContinueWith(r => { }));
            Assert.Throws<InvalidOperationException>(() => promise.ContinueWith(r => 1));
            Assert.Throws<InvalidOperationException>(() => promise.ContinueWith(r => Promise.Resolved()));
            Assert.Throws<InvalidOperationException>(() => promise.ContinueWith(r => Promise.Resolved(1)));
            Assert.Throws<InvalidOperationException>(() => promise.ContinueWith(1, (cv, r) => { }));
            Assert.Throws<InvalidOperationException>(() => promise.ContinueWith(1, (cv, r) => 1));
            Assert.Throws<InvalidOperationException>(() => promise.ContinueWith(1, (cv, r) => Promise.Resolved()));
            Assert.Throws<InvalidOperationException>(() => promise.ContinueWith(1, (cv, r) => Promise.Resolved(1)));

            Assert.Throws<InvalidOperationException>(() => promise.Catch(() => { }));
            Assert.Throws<InvalidOperationException>(() => promise.Catch(() => Promise.Resolved()));
            Assert.Throws<InvalidOperationException>(() => promise.Catch((object r) => { }));
            Assert.Throws<InvalidOperationException>(() => promise.Catch((object r) => Promise.Resolved()));
            Assert.Throws<InvalidOperationException>(() => promise.Catch(1, cv => { }));
            Assert.Throws<InvalidOperationException>(() => promise.Catch(1, cv => Promise.Resolved()));
            Assert.Throws<InvalidOperationException>(() => promise.Catch(1, (int cv, object r) => { }));
            Assert.Throws<InvalidOperationException>(() => promise.Catch(1, (int cv, object r) => Promise.Resolved()));

            Assert.Throws<InvalidOperationException>(() => promise.Then(() => { }));
            Assert.Throws<InvalidOperationException>(() => promise.Then(() => 1));
            Assert.Throws<InvalidOperationException>(() => promise.Then(() => Promise.Resolved()));
            Assert.Throws<InvalidOperationException>(() => promise.Then(() => Promise.Resolved(1)));
            Assert.Throws<InvalidOperationException>(() => promise.Then(1, cv => { }));
            Assert.Throws<InvalidOperationException>(() => promise.Then(1, cv => 1));
            Assert.Throws<InvalidOperationException>(() => promise.Then(1, cv => Promise.Resolved()));
            Assert.Throws<InvalidOperationException>(() => promise.Then(1, cv => Promise.Resolved(1)));

            Assert.Throws<InvalidOperationException>(() => promise.Then(() => { }, () => { }));
            Assert.Throws<InvalidOperationException>(() => promise.Then(() => 1, () => 1));
            Assert.Throws<InvalidOperationException>(() => promise.Then(() => Promise.Resolved(), () => { }));
            Assert.Throws<InvalidOperationException>(() => promise.Then(() => Promise.Resolved(1), () => 1));
            Assert.Throws<InvalidOperationException>(() => promise.Then(1, cv => { }, () => { }));
            Assert.Throws<InvalidOperationException>(() => promise.Then(1, cv => 1, () => 1));
            Assert.Throws<InvalidOperationException>(() => promise.Then(1, cv => Promise.Resolved(), () => { }));
            Assert.Throws<InvalidOperationException>(() => promise.Then(1, cv => Promise.Resolved(1), () => 1));

            Assert.Throws<InvalidOperationException>(() => promise.Then(() => { }, 1, cv => { }));
            Assert.Throws<InvalidOperationException>(() => promise.Then(() => 1, 1, cv => 1));
            Assert.Throws<InvalidOperationException>(() => promise.Then(() => Promise.Resolved(), 1, cv => { }));
            Assert.Throws<InvalidOperationException>(() => promise.Then(() => Promise.Resolved(1), 1, cv => 1));
            Assert.Throws<InvalidOperationException>(() => promise.Then(1, cv => { }, 1, cv => { }));
            Assert.Throws<InvalidOperationException>(() => promise.Then(1, cv => 1, 1, cv => 1));
            Assert.Throws<InvalidOperationException>(() => promise.Then(1, cv => Promise.Resolved(), 1, cv => { }));
            Assert.Throws<InvalidOperationException>(() => promise.Then(1, cv => Promise.Resolved(1), 1, cv => 1));

            Assert.Throws<InvalidOperationException>(() => promise.Then(() => { }, (object r) => { }));
            Assert.Throws<InvalidOperationException>(() => promise.Then(() => 1, (object r) => 1));
            Assert.Throws<InvalidOperationException>(() => promise.Then(() => Promise.Resolved(), (object r) => { }));
            Assert.Throws<InvalidOperationException>(() => promise.Then(() => Promise.Resolved(1), (object r) => 1));
            Assert.Throws<InvalidOperationException>(() => promise.Then(1, cv => { }, (object r) => { }));
            Assert.Throws<InvalidOperationException>(() => promise.Then(1, cv => 1, (object r) => 1));
            Assert.Throws<InvalidOperationException>(() => promise.Then(1, cv => Promise.Resolved(), (object r) => { }));
            Assert.Throws<InvalidOperationException>(() => promise.Then(1, cv => Promise.Resolved(1), (object r) => 1));

            Assert.Throws<InvalidOperationException>(() => promise.Then(() => { }, 1, (int cv, object r) => { }));
            Assert.Throws<InvalidOperationException>(() => promise.Then(() => 1, 1, (int cv, object r) => 1));
            Assert.Throws<InvalidOperationException>(() => promise.Then(() => Promise.Resolved(), 1, (int cv, object r) => { }));
            Assert.Throws<InvalidOperationException>(() => promise.Then(() => Promise.Resolved(1), 1, (int cv, object r) => 1));
            Assert.Throws<InvalidOperationException>(() => promise.Then(1, cv => { }, 1, (int cv, object r) => { }));
            Assert.Throws<InvalidOperationException>(() => promise.Then(1, cv => 1, 1, (int cv, object r) => 1));
            Assert.Throws<InvalidOperationException>(() => promise.Then(1, cv => Promise.Resolved(), 1, (int cv, object r) => { }));
            Assert.Throws<InvalidOperationException>(() => promise.Then(1, cv => Promise.Resolved(1), 1, (int cv, object r) => 1));

            Assert.Throws<InvalidOperationException>(() => promise.Then(() => { }, () => Promise.Resolved()));
            Assert.Throws<InvalidOperationException>(() => promise.Then(() => 1, () => Promise.Resolved(1)));
            Assert.Throws<InvalidOperationException>(() => promise.Then(() => Promise.Resolved(), () => Promise.Resolved()));
            Assert.Throws<InvalidOperationException>(() => promise.Then(() => Promise.Resolved(1), () => Promise.Resolved(1)));
            Assert.Throws<InvalidOperationException>(() => promise.Then(1, cv => { }, () => Promise.Resolved()));
            Assert.Throws<InvalidOperationException>(() => promise.Then(1, cv => 1, () => Promise.Resolved(1)));
            Assert.Throws<InvalidOperationException>(() => promise.Then(1, cv => Promise.Resolved(), () => Promise.Resolved()));
            Assert.Throws<InvalidOperationException>(() => promise.Then(1, cv => Promise.Resolved(1), () => Promise.Resolved(1)));

            Assert.Throws<InvalidOperationException>(() => promise.Then(() => { }, 1, cv => Promise.Resolved()));
            Assert.Throws<InvalidOperationException>(() => promise.Then(() => 1, 1, cv => Promise.Resolved(1)));
            Assert.Throws<InvalidOperationException>(() => promise.Then(() => Promise.Resolved(), 1, cv => Promise.Resolved()));
            Assert.Throws<InvalidOperationException>(() => promise.Then(() => Promise.Resolved(1), 1, cv => Promise.Resolved(1)));
            Assert.Throws<InvalidOperationException>(() => promise.Then(1, cv => { }, 1, cv => Promise.Resolved()));
            Assert.Throws<InvalidOperationException>(() => promise.Then(1, cv => 1, 1, cv => Promise.Resolved(1)));
            Assert.Throws<InvalidOperationException>(() => promise.Then(1, cv => Promise.Resolved(), 1, cv => Promise.Resolved()));
            Assert.Throws<InvalidOperationException>(() => promise.Then(1, cv => Promise.Resolved(1), 1, cv => Promise.Resolved(1)));

            Assert.Throws<InvalidOperationException>(() => promise.Then(() => { }, (object r) => Promise.Resolved()));
            Assert.Throws<InvalidOperationException>(() => promise.Then(() => 1, (object r) => Promise.Resolved(1)));
            Assert.Throws<InvalidOperationException>(() => promise.Then(() => Promise.Resolved(), (object r) => Promise.Resolved()));
            Assert.Throws<InvalidOperationException>(() => promise.Then(() => Promise.Resolved(1), (object r) => Promise.Resolved(1)));
            Assert.Throws<InvalidOperationException>(() => promise.Then(1, cv => { }, (object r) => Promise.Resolved()));
            Assert.Throws<InvalidOperationException>(() => promise.Then(1, cv => 1, (object r) => Promise.Resolved(1)));
            Assert.Throws<InvalidOperationException>(() => promise.Then(1, cv => Promise.Resolved(), (object r) => Promise.Resolved()));
            Assert.Throws<InvalidOperationException>(() => promise.Then(1, cv => Promise.Resolved(1), (object r) => Promise.Resolved(1)));

            Assert.Throws<InvalidOperationException>(() => promise.Then(() => { }, 1, (int cv, object r) => Promise.Resolved()));
            Assert.Throws<InvalidOperationException>(() => promise.Then(() => 1, 1, (int cv, object r) => Promise.Resolved(1)));
            Assert.Throws<InvalidOperationException>(() => promise.Then(() => Promise.Resolved(), 1, (int cv, object r) => Promise.Resolved()));
            Assert.Throws<InvalidOperationException>(() => promise.Then(() => Promise.Resolved(1), 1, (int cv, object r) => Promise.Resolved(1)));
            Assert.Throws<InvalidOperationException>(() => promise.Then(1, cv => { }, 1, (int cv, object r) => Promise.Resolved()));
            Assert.Throws<InvalidOperationException>(() => promise.Then(1, cv => 1, 1, (int cv, object r) => Promise.Resolved(1)));
            Assert.Throws<InvalidOperationException>(() => promise.Then(1, cv => Promise.Resolved(), 1, (int cv, object r) => Promise.Resolved()));
            Assert.Throws<InvalidOperationException>(() => promise.Then(1, cv => Promise.Resolved(1), 1, (int cv, object r) => Promise.Resolved(1)));

            deferred.Resolve();
        }

        [Test]
        public void PromiseIsInvalidAfterAwaited_T()
        {
            var deferred = Promise.NewDeferred<int>();
            var promise = deferred.Promise;

            Assert.IsTrue(promise.IsValid);

            promise.Then(v => { }).Forget();

            Assert.IsFalse(promise.IsValid);

#if CSHARP_7_3_OR_NEWER && PROMISE_DEBUG
            Assert.Throws<InvalidOperationException>(() => promise.GetAwaiter());
#endif
            Assert.Throws<InvalidOperationException>(() => promise.Preserve());
            Assert.Throws<InvalidOperationException>(() => promise.Forget());
            Assert.Throws<InvalidOperationException>(() => promise.Duplicate());

#if PROMISE_PROGRESS
            Assert.Throws<InvalidOperationException>(() => promise.Progress(v => { }));
            Assert.Throws<InvalidOperationException>(() => promise.Progress(1, (cv, v) => { }));
#endif
            Assert.Throws<InvalidOperationException>(() => promise.CatchCancelation(() => { }));
            Assert.Throws<InvalidOperationException>(() => promise.CatchCancelation(1, cv => { }));

            Assert.Throws<InvalidOperationException>(() => promise.Finally(() => { }));
            Assert.Throws<InvalidOperationException>(() => promise.Finally(1, cv => { }));

            Assert.Throws<InvalidOperationException>(() => promise.ContinueWith(r => { }));
            Assert.Throws<InvalidOperationException>(() => promise.ContinueWith(r => 1));
            Assert.Throws<InvalidOperationException>(() => promise.ContinueWith(r => Promise.Resolved()));
            Assert.Throws<InvalidOperationException>(() => promise.ContinueWith(r => Promise.Resolved(1)));
            Assert.Throws<InvalidOperationException>(() => promise.ContinueWith(1, (cv, r) => { }));
            Assert.Throws<InvalidOperationException>(() => promise.ContinueWith(1, (cv, r) => 1));
            Assert.Throws<InvalidOperationException>(() => promise.ContinueWith(1, (cv, r) => Promise.Resolved()));
            Assert.Throws<InvalidOperationException>(() => promise.ContinueWith(1, (cv, r) => Promise.Resolved(1)));

            Assert.Throws<InvalidOperationException>(() => promise.Catch(() => 1));
            Assert.Throws<InvalidOperationException>(() => promise.Catch(() => Promise.Resolved(1)));
            Assert.Throws<InvalidOperationException>(() => promise.Catch((object r) => 1));
            Assert.Throws<InvalidOperationException>(() => promise.Catch((object r) => Promise.Resolved(1)));
            Assert.Throws<InvalidOperationException>(() => promise.Catch(1, cv => 1));
            Assert.Throws<InvalidOperationException>(() => promise.Catch(1, cv => Promise.Resolved(1)));
            Assert.Throws<InvalidOperationException>(() => promise.Catch(1, (int cv, object r) => 1));
            Assert.Throws<InvalidOperationException>(() => promise.Catch(1, (int cv, object r) => Promise.Resolved(1)));

            Assert.Throws<InvalidOperationException>(() => promise.Then(v => { }));
            Assert.Throws<InvalidOperationException>(() => promise.Then(v => 1));
            Assert.Throws<InvalidOperationException>(() => promise.Then(v => Promise.Resolved()));
            Assert.Throws<InvalidOperationException>(() => promise.Then(v => Promise.Resolved(1)));
            Assert.Throws<InvalidOperationException>(() => promise.Then(1, (cv, v) => { }));
            Assert.Throws<InvalidOperationException>(() => promise.Then(1, (cv, v) => 1));
            Assert.Throws<InvalidOperationException>(() => promise.Then(1, (cv, v) => Promise.Resolved()));
            Assert.Throws<InvalidOperationException>(() => promise.Then(1, (cv, v) => Promise.Resolved(1)));

            Assert.Throws<InvalidOperationException>(() => promise.Then(v => { }, () => { }));
            Assert.Throws<InvalidOperationException>(() => promise.Then(v => 1, () => 1));
            Assert.Throws<InvalidOperationException>(() => promise.Then(v => Promise.Resolved(), () => { }));
            Assert.Throws<InvalidOperationException>(() => promise.Then(v => Promise.Resolved(1), () => 1));
            Assert.Throws<InvalidOperationException>(() => promise.Then(1, (cv, v) => { }, () => { }));
            Assert.Throws<InvalidOperationException>(() => promise.Then(1, (cv, v) => 1, () => 1));
            Assert.Throws<InvalidOperationException>(() => promise.Then(1, (cv, v) => Promise.Resolved(), () => { }));
            Assert.Throws<InvalidOperationException>(() => promise.Then(1, (cv, v) => Promise.Resolved(1), () => 1));

            Assert.Throws<InvalidOperationException>(() => promise.Then(v => { }, 1, cv => { }));
            Assert.Throws<InvalidOperationException>(() => promise.Then(v => 1, 1, cv => 1));
            Assert.Throws<InvalidOperationException>(() => promise.Then(v => Promise.Resolved(), 1, cv => { }));
            Assert.Throws<InvalidOperationException>(() => promise.Then(v => Promise.Resolved(1), 1, cv => 1));
            Assert.Throws<InvalidOperationException>(() => promise.Then(1, (cv, v) => { }, 1, cv => { }));
            Assert.Throws<InvalidOperationException>(() => promise.Then(1, (cv, v) => 1, 1, cv => 1));
            Assert.Throws<InvalidOperationException>(() => promise.Then(1, (cv, v) => Promise.Resolved(), 1, cv => { }));
            Assert.Throws<InvalidOperationException>(() => promise.Then(1, (cv, v) => Promise.Resolved(1), 1, cv => 1));

            Assert.Throws<InvalidOperationException>(() => promise.Then(v => { }, (object r) => { }));
            Assert.Throws<InvalidOperationException>(() => promise.Then(v => 1, (object r) => 1));
            Assert.Throws<InvalidOperationException>(() => promise.Then(v => Promise.Resolved(), (object r) => { }));
            Assert.Throws<InvalidOperationException>(() => promise.Then(v => Promise.Resolved(1), (object r) => 1));
            Assert.Throws<InvalidOperationException>(() => promise.Then(1, (cv, v) => { }, (object r) => { }));
            Assert.Throws<InvalidOperationException>(() => promise.Then(1, (cv, v) => 1, (object r) => 1));
            Assert.Throws<InvalidOperationException>(() => promise.Then(1, (cv, v) => Promise.Resolved(), (object r) => { }));
            Assert.Throws<InvalidOperationException>(() => promise.Then(1, (cv, v) => Promise.Resolved(1), (object r) => 1));

            Assert.Throws<InvalidOperationException>(() => promise.Then(v => { }, 1, (int cv, object r) => { }));
            Assert.Throws<InvalidOperationException>(() => promise.Then(v => 1, 1, (int cv, object r) => 1));
            Assert.Throws<InvalidOperationException>(() => promise.Then(v => Promise.Resolved(), 1, (int cv, object r) => { }));
            Assert.Throws<InvalidOperationException>(() => promise.Then(v => Promise.Resolved(1), 1, (int cv, object r) => 1));
            Assert.Throws<InvalidOperationException>(() => promise.Then(1, (cv, v) => { }, 1, (int cv, object r) => { }));
            Assert.Throws<InvalidOperationException>(() => promise.Then(1, (cv, v) => 1, 1, (int cv, object r) => 1));
            Assert.Throws<InvalidOperationException>(() => promise.Then(1, (cv, v) => Promise.Resolved(), 1, (int cv, object r) => { }));
            Assert.Throws<InvalidOperationException>(() => promise.Then(1, (cv, v) => Promise.Resolved(1), 1, (int cv, object r) => 1));

            Assert.Throws<InvalidOperationException>(() => promise.Then(v => { }, () => Promise.Resolved()));
            Assert.Throws<InvalidOperationException>(() => promise.Then(v => 1, () => Promise.Resolved(1)));
            Assert.Throws<InvalidOperationException>(() => promise.Then(v => Promise.Resolved(), () => Promise.Resolved()));
            Assert.Throws<InvalidOperationException>(() => promise.Then(v => Promise.Resolved(1), () => Promise.Resolved(1)));
            Assert.Throws<InvalidOperationException>(() => promise.Then(1, (cv, v) => { }, () => Promise.Resolved()));
            Assert.Throws<InvalidOperationException>(() => promise.Then(1, (cv, v) => 1, () => Promise.Resolved(1)));
            Assert.Throws<InvalidOperationException>(() => promise.Then(1, (cv, v) => Promise.Resolved(), () => Promise.Resolved()));
            Assert.Throws<InvalidOperationException>(() => promise.Then(1, (cv, v) => Promise.Resolved(1), () => Promise.Resolved(1)));

            Assert.Throws<InvalidOperationException>(() => promise.Then(v => { }, 1, cv => Promise.Resolved()));
            Assert.Throws<InvalidOperationException>(() => promise.Then(v => 1, 1, cv => Promise.Resolved(1)));
            Assert.Throws<InvalidOperationException>(() => promise.Then(v => Promise.Resolved(), 1, cv => Promise.Resolved()));
            Assert.Throws<InvalidOperationException>(() => promise.Then(v => Promise.Resolved(1), 1, cv => Promise.Resolved(1)));
            Assert.Throws<InvalidOperationException>(() => promise.Then(1, (cv, v) => { }, 1, cv => Promise.Resolved()));
            Assert.Throws<InvalidOperationException>(() => promise.Then(1, (cv, v) => 1, 1, cv => Promise.Resolved(1)));
            Assert.Throws<InvalidOperationException>(() => promise.Then(1, (cv, v) => Promise.Resolved(), 1, cv => Promise.Resolved()));
            Assert.Throws<InvalidOperationException>(() => promise.Then(1, (cv, v) => Promise.Resolved(1), 1, cv => Promise.Resolved(1)));

            Assert.Throws<InvalidOperationException>(() => promise.Then(v => { }, (object r) => Promise.Resolved()));
            Assert.Throws<InvalidOperationException>(() => promise.Then(v => 1, (object r) => Promise.Resolved(1)));
            Assert.Throws<InvalidOperationException>(() => promise.Then(v => Promise.Resolved(), (object r) => Promise.Resolved()));
            Assert.Throws<InvalidOperationException>(() => promise.Then(v => Promise.Resolved(1), (object r) => Promise.Resolved(1)));
            Assert.Throws<InvalidOperationException>(() => promise.Then(1, (cv, v) => { }, (object r) => Promise.Resolved()));
            Assert.Throws<InvalidOperationException>(() => promise.Then(1, (cv, v) => 1, (object r) => Promise.Resolved(1)));
            Assert.Throws<InvalidOperationException>(() => promise.Then(1, (cv, v) => Promise.Resolved(), (object r) => Promise.Resolved()));
            Assert.Throws<InvalidOperationException>(() => promise.Then(1, (cv, v) => Promise.Resolved(1), (object r) => Promise.Resolved(1)));

            Assert.Throws<InvalidOperationException>(() => promise.Then(v => { }, 1, (int cv, object r) => Promise.Resolved()));
            Assert.Throws<InvalidOperationException>(() => promise.Then(v => 1, 1, (int cv, object r) => Promise.Resolved(1)));
            Assert.Throws<InvalidOperationException>(() => promise.Then(v => Promise.Resolved(), 1, (int cv, object r) => Promise.Resolved()));
            Assert.Throws<InvalidOperationException>(() => promise.Then(v => Promise.Resolved(1), 1, (int cv, object r) => Promise.Resolved(1)));
            Assert.Throws<InvalidOperationException>(() => promise.Then(1, (cv, v) => { }, 1, (int cv, object r) => Promise.Resolved()));
            Assert.Throws<InvalidOperationException>(() => promise.Then(1, (cv, v) => 1, 1, (int cv, object r) => Promise.Resolved(1)));
            Assert.Throws<InvalidOperationException>(() => promise.Then(1, (cv, v) => Promise.Resolved(), 1, (int cv, object r) => Promise.Resolved()));
            Assert.Throws<InvalidOperationException>(() => promise.Then(1, (cv, v) => Promise.Resolved(1), 1, (int cv, object r) => Promise.Resolved(1)));

            deferred.Resolve(1);
        }

        [Test]
        public void ThrowingRejectExceptionInOnResolvedRejectsThePromiseWithTheGivenValue_void()
        {
            var promise = Promise.Resolved().Preserve();

            int rejectCount = 0;
            string expected = "Reject!";

            TestAction<Promise> onCallbackAdded = (ref Promise p) =>
                p.Catch((string e) =>
                {
                    Assert.AreEqual(expected, e);
                    ++rejectCount;
                }).Forget();
            TestAction<Promise<int>> onCallbackAddedConvert = (ref Promise<int> p) =>
            {
                p.Catch((string e) =>
                {
                    Assert.AreEqual(expected, e);
                    ++rejectCount;
                }).Forget();
            };

            TestHelper.AddResolveCallbacks<int, string>(promise,
                onResolve: () => { throw Promise.RejectException(expected); },
                onCallbackAdded: onCallbackAdded,
                onCallbackAddedConvert: onCallbackAddedConvert
            );
            TestHelper.AddCallbacks<int, object, string>(promise,
                onResolve: () => { throw Promise.RejectException(expected); },
                onCallbackAdded: onCallbackAdded,
                onCallbackAddedConvert: onCallbackAddedConvert
            );
            TestHelper.AddContinueCallbacks<int, string>(promise,
                onContinue: _ => { throw Promise.RejectException(expected); },
                onCallbackAdded: onCallbackAdded,
                onCallbackAddedConvert: onCallbackAddedConvert
            );

            Assert.AreEqual(
                (TestHelper.resolveVoidCallbacks + TestHelper.continueVoidCallbacks) * 2,
                rejectCount
            );

            promise.Forget();
        }

        [Test]
        public void ThrowingRejectExceptionInOnResolvedRejectsThePromiseWithTheGivenValue_T()
        {
            var promise = Promise.Resolved(100).Preserve();

            int rejectCount = 0;
            string expected = "Reject!";

            TestAction<Promise> onCallbackAdded = (ref Promise p) =>
                p.Catch((string e) =>
                {
                    Assert.AreEqual(expected, e);
                    ++rejectCount;
                }).Forget();
            TestAction<Promise<int>> onCallbackAddedConvert = (ref Promise<int> p) =>
                p.Catch((string e) =>
                {
                    Assert.AreEqual(expected, e);
                    ++rejectCount;
                }).Forget();

            TestHelper.AddResolveCallbacks<int, int, string>(promise,
                onResolve: v => { throw Promise.RejectException(expected); },
                onCallbackAdded: onCallbackAdded,
                onCallbackAddedConvert: onCallbackAddedConvert
            );
            TestHelper.AddCallbacks<int, int, object, string>(promise,
                onResolve: v => { throw Promise.RejectException(expected); },
                onCallbackAdded: onCallbackAdded,
                onCallbackAddedConvert: onCallbackAddedConvert
            );
            TestHelper.AddContinueCallbacks<int, int, string>(promise,
                onContinue: _ => { throw Promise.RejectException(expected); },
                onCallbackAdded: onCallbackAdded,
                onCallbackAddedConvert: onCallbackAddedConvert
            );

            Assert.AreEqual(
                (TestHelper.resolveTCallbacks + TestHelper.continueTCallbacks) * 2,
                rejectCount
            );

            promise.Forget();
        }

        [Test]
        public void ThrowingRejectExceptionInOnRejectedRejectsThePromiseWithTheGivenValue_void()
        {
            var promise = Promise.Rejected("A different reject value.").Preserve();

            int rejectCount = 0;
            string expected = "Reject!";

            TestAction<Promise> onCallbackAdded = (ref Promise p) =>
                p.Catch((string e) =>
                {
                    Assert.AreEqual(expected, e);
                    ++rejectCount;
                }).Forget();
            TestAction<Promise<int>> onCallbackAddedConvert = (ref Promise<int> p) =>
                p.Catch((string e) =>
                {
                    Assert.AreEqual(expected, e);
                    ++rejectCount;
                }).Forget();

            TestHelper.AddCallbacks<int, object, string>(promise,
                onReject: v => { throw Promise.RejectException(expected); },
                onUnknownRejection: () => { throw Promise.RejectException(expected); },
                onCallbackAdded: onCallbackAdded,
                onCallbackAddedConvert: onCallbackAddedConvert
            );
            TestHelper.AddContinueCallbacks<int, string>(promise,
                onContinue: _ => { throw Promise.RejectException(expected); },
                onCallbackAdded: onCallbackAdded,
                onCallbackAddedConvert: onCallbackAddedConvert
            );

            Assert.AreEqual(
                (TestHelper.rejectVoidCallbacks + TestHelper.continueVoidCallbacks) * 2,
                rejectCount
            );

            promise.Forget();
        }

        [Test]
        public void ThrowingRejectExceptionInOnRejectedRejectsThePromiseWithTheGivenValue_T()
        {
            var promise = Promise<int>.Rejected("A different reject value.").Preserve();

            int rejectCount = 0;
            string expected = "Reject!";

            TestAction<Promise> onCallbackAdded = (ref Promise p) =>
                p.Catch((string e) =>
                {
                    Assert.AreEqual(expected, e);
                    ++rejectCount;
                }).Forget();
            TestAction<Promise<int>> onCallbackAddedConvert = (ref Promise<int> p) =>
                p.Catch((string e) =>
                {
                    Assert.AreEqual(expected, e);
                    ++rejectCount;
                }).Forget();

            TestHelper.AddCallbacks<int, int, object, string>(promise,
                onReject: v => { throw Promise.RejectException(expected); },
                onUnknownRejection: () => { throw Promise.RejectException(expected); },
                onCallbackAdded: onCallbackAdded,
                onCallbackAddedConvert: onCallbackAddedConvert,
                onCallbackAddedT: onCallbackAddedConvert
            );
            TestHelper.AddContinueCallbacks<int, int, string>(promise,
                onContinue: _ => { throw Promise.RejectException(expected); },
                onCallbackAdded: onCallbackAdded,
                onCallbackAddedConvert: onCallbackAddedConvert
            );

            Assert.AreEqual(
                (TestHelper.rejectTCallbacks + TestHelper.continueTCallbacks) * 2,
                rejectCount
            );

            promise.Forget();
        }

        [Test]
        public void ThrowingCancelExceptionInOnResolvedCancelsThePromiseWithTheGivenValue_void()
        {
            var promise = Promise.Resolved().Preserve();

            int cancelCount = 0;

            System.Action onCancel = () =>
            {
                ++cancelCount;
            };

            TestHelper.AddResolveCallbacks<int, string>(promise,
                onResolve: () => { throw Promise.CancelException(); },
                onCancel: onCancel
            );
            TestHelper.AddCallbacks<int, object, string>(promise,
                onResolve: () => { throw Promise.CancelException(); },
                onCancel: onCancel
            );
            TestHelper.AddContinueCallbacks<int, string>(promise,
                onContinue: _ => { throw Promise.CancelException(); },
                onCancel: onCancel
            );

            Assert.AreEqual(
                (TestHelper.resolveVoidCallbacks + TestHelper.continueVoidCallbacks) * 2,
                cancelCount
            );

            promise.Forget();
        }

        [Test]
        public void ThrowingCancelExceptionInOnResolvedCancelsThePromiseWithTheGivenValue_T()
        {
            var promise = Promise.Resolved(100).Preserve();

            int cancelCount = 0;

            System.Action onCancel = () =>
            {
                ++cancelCount;
            };

            TestHelper.AddResolveCallbacks<int, bool, string>(promise,
                onResolve: v => { throw Promise.CancelException(); },
                onCancel: onCancel
            );
            TestHelper.AddCallbacks<int, bool, object, string>(promise,
                onResolve: v => { throw Promise.CancelException(); },
                onCancel: onCancel
            );
            TestHelper.AddContinueCallbacks<int, bool, string>(promise,
                onContinue: _ => { throw Promise.CancelException(); },
                onCancel: onCancel
            );

            Assert.AreEqual(
                (TestHelper.resolveTCallbacks + TestHelper.continueTCallbacks) * 2,
                cancelCount
            );

            promise.Forget();
        }

        [Test]
        public void ThrowingCancelExceptionInOnRejectedCancelsThePromiseWithTheGivenValue_void()
        {
            var promise = Promise.Rejected("Rejected").Preserve();

            int cancelCount = 0;

            System.Action onCancel = () =>
            {
                ++cancelCount;
            };

            TestHelper.AddCallbacks<int, object, string>(promise,
                onReject: v => { throw Promise.CancelException(); },
                onUnknownRejection: () => { throw Promise.CancelException(); },
                onCancel: onCancel
            );
            TestHelper.AddContinueCallbacks<int, string>(promise,
                onContinue: _ => { throw Promise.CancelException(); },
                onCancel: onCancel
            );

            Assert.AreEqual(
                (TestHelper.rejectVoidCallbacks + TestHelper.continueVoidCallbacks) * 2,
                cancelCount
            );

            promise.Forget();
        }

        [Test]
        public void ThrowingCancelExceptionInOnRejectedCancelsThePromiseWithTheGivenValue_T()
        {
            var promise = Promise<int>.Rejected("Rejected").Preserve();

            int cancelCount = 0;

            System.Action onCancel = () =>
            {
                ++cancelCount;
            };

            TestHelper.AddCallbacks<int, bool, object, string>(promise,
                onReject: v => { throw Promise.CancelException(); },
                onUnknownRejection: () => { throw Promise.CancelException(); },
                onCancel: onCancel
            );
            TestHelper.AddContinueCallbacks<int, int, string>(promise,
                onContinue: _ => { throw Promise.CancelException(); },
                onCancel: onCancel
            );

            Assert.AreEqual(
                (TestHelper.rejectTCallbacks + TestHelper.continueTCallbacks) * 2,
                cancelCount
            );

            promise.Forget();
        }

        [Test]
        public void ThrowingRethrowInOnResolvedRejectsThePromiseWithInvalidOperationException_void()
        {
            var promise = Promise.Resolved().Preserve();

            int errorCount = 0;

            TestAction<Promise> onCallbackAdded = (ref Promise p) =>
                p.Catch((object e) =>
                {
                    Assert.IsInstanceOf(typeof(InvalidOperationException), e);
                    ++errorCount;
                }).Forget();
            TestAction<Promise<int>> onCallbackAddedConvert = (ref Promise<int> p) =>
                p.Catch((object e) =>
                {
                    Assert.IsInstanceOf(typeof(InvalidOperationException), e);
                    ++errorCount;
                }).Forget();

            TestHelper.AddResolveCallbacks<int, string>(promise,
                onResolve: () => { throw Promise.Rethrow; },
                onCallbackAdded: onCallbackAdded,
                onCallbackAddedConvert: onCallbackAddedConvert
            );
            TestHelper.AddCallbacks<int, object, string>(promise,
                onResolve: () => { throw Promise.Rethrow; },
                onCallbackAdded: onCallbackAdded,
                onCallbackAddedConvert: onCallbackAddedConvert
            );
            TestHelper.AddContinueCallbacks<int, string>(promise,
                onContinue: _ => { throw Promise.Rethrow; },
                onCallbackAdded: onCallbackAdded,
                onCallbackAddedConvert: onCallbackAddedConvert
            );

            Assert.AreEqual(
                (TestHelper.resolveVoidCallbacks + TestHelper.continueVoidCallbacks) * 2,
                errorCount
            );

            promise.Forget();
        }

        [Test]
        public void ThrowingRethrowInOnResolvedRejectsThePromiseWithInvalidOperationException_T()
        {
            var promise = Promise.Resolved(100).Preserve();

            int errorCount = 0;

            TestAction<Promise> onCallbackAdded = (ref Promise p) =>
                p.Catch((object e) =>
                {
                    Assert.IsInstanceOf(typeof(InvalidOperationException), e);
                    ++errorCount;
                }).Forget();
            TestAction<Promise<int>> onCallbackAddedConvert = (ref Promise<int> p) =>
                p.Catch((object e) =>
                {
                    Assert.IsInstanceOf(typeof(InvalidOperationException), e);
                    ++errorCount;
                }).Forget();

            TestHelper.AddResolveCallbacks<int, int, string>(promise,
                onResolve: v => { throw Promise.Rethrow; },
                onCallbackAdded: onCallbackAdded,
                onCallbackAddedConvert: onCallbackAddedConvert
            );
            TestHelper.AddCallbacks<int, int, object, string>(promise,
                onResolve: v => { throw Promise.Rethrow; },
                onCallbackAdded: onCallbackAdded,
                onCallbackAddedConvert: onCallbackAddedConvert
            );
            TestHelper.AddContinueCallbacks<int, int, string>(promise,
                onContinue: _ => { throw Promise.Rethrow; },
                onCallbackAdded: onCallbackAdded,
                onCallbackAddedConvert: onCallbackAddedConvert
            );

            Assert.AreEqual(
                (TestHelper.resolveTCallbacks + TestHelper.continueTCallbacks) * 2,
                errorCount
            );

            promise.Forget();
        }

        [Test]
        public void ThrowingRethrowInOnRejectedRejectsThePromiseWithTheSameReason_void()
        {
            string expected = "Reject!";

            var promise = Promise.Rejected(expected).Preserve();

            int rejectCount = 0;

            TestAction<Promise> onCallbackAdded = (ref Promise p) =>
                p.Catch((string e) =>
                {
                    Assert.AreEqual(expected, e);
                    ++rejectCount;
                }).Forget();
            TestAction<Promise<int>> onCallbackAddedConvert = (ref Promise<int> p) =>
                p.Catch((string e) =>
                {
                    Assert.AreEqual(expected, e);
                    ++rejectCount;
                }).Forget();

            TestHelper.AddCallbacks<int, object, string>(promise,
                onReject: _ => { throw Promise.Rethrow; },
                onUnknownRejection: () => { throw Promise.Rethrow; },
                onCallbackAdded: onCallbackAdded,
                onCallbackAddedConvert: onCallbackAddedConvert
            );

            Assert.AreEqual(
                TestHelper.rejectVoidCallbacks * 2,
                rejectCount
            );

            promise.Forget();
        }

        [Test]
        public void ThrowingRethrowInOnRejectedRejectsThePromiseWithTheSameReason_T()
        {
            string expected = "Reject!";

            var promise = Promise<int>.Rejected(expected).Preserve();

            int rejectCount = 0;

            TestAction<Promise> onCallbackAdded = (ref Promise p) =>
                p.Catch((string e) =>
                {
                    Assert.AreEqual(expected, e);
                    ++rejectCount;
                }).Forget();
            TestAction<Promise<int>> onCallbackAddedConvert = (ref Promise<int> p) =>
                p.Catch((string e) =>
                {
                    Assert.AreEqual(expected, e);
                    ++rejectCount;
                }).Forget();

            TestHelper.AddCallbacks<int, int, object, string>(promise,
                onReject: _ => { throw Promise.Rethrow; },
                onUnknownRejection: () => { throw Promise.Rethrow; },
                onCallbackAdded: onCallbackAdded,
                onCallbackAddedConvert: onCallbackAddedConvert,
                onCallbackAddedT: onCallbackAddedConvert
            );

            Assert.AreEqual(
                TestHelper.rejectTCallbacks * 2,
                rejectCount
            );

            promise.Forget();
        }

        [Test]
        public void PromiseResolvedIsResolved()
        {
            bool resolved = false;

            Promise.Resolved()
                .CatchCancelation(() => Assert.Fail("Promise was canceled when it should have been resolved"))
                .Then(() => resolved = true, () => Assert.Fail("Promise was rejected when it should have been resolved"))
                .Forget();

            Assert.IsTrue(resolved);
        }

        [Test]
        public void PromiseResolvedIsResolvedWithTheGivenValue()
        {
            int expected = 100;
            bool resolved = false;

            Promise.Resolved(expected)
                .Then(val =>
                {
                    Assert.AreEqual(expected, val);
                    resolved = true;
                }, () => Assert.Fail("Promise was rejected when it should have been resolved"))
                .CatchCancelation(() => Assert.Fail("Promise was canceled when it should have been resolved"))
                .Forget();

            Assert.IsTrue(resolved);
        }

        [Test]
        public void PromiseRejectedIsRejectedWithTheGivenReason_void()
        {
            string expected = "Reject";
            bool rejected = false;

            Promise.Rejected(expected)
                .CatchCancelation(() => Assert.Fail("Promise was canceled when it should have been rejected"))
                .Then(() => Assert.Fail("Promise was resolved when it should have been rejected"),
                (object reason) =>
                {
                    Assert.AreEqual(expected, reason);
                    rejected = true;
                })
                .Forget();

            Assert.IsTrue(rejected);
        }

        [Test]
        public void PromiseRejectedIsRejectedWithTheGivenReason_T()
        {
            string expected = "Reject";
            bool rejected = false;

            Promise<int>.Rejected(expected)
                .CatchCancelation(() => Assert.Fail("Promise was canceled when it should have been rejected"))
                .Then(() => Assert.Fail("Promise was resolved when it should have been rejected"),
                (object reason) =>
                {
                    Assert.AreEqual(expected, reason);
                    rejected = true;
                })
                .Forget();

            Assert.IsTrue(rejected);
        }

        [Test]
        public void PromiseCanceledIsCanceled_void()
        {
            bool canceled = false;

            Promise.Canceled()
                .CatchCancelation(() =>
                {
                    canceled = true;
                })
                .Forget();

            Assert.IsTrue(canceled);
        }

        [Test]
        public void PromiseCanceledIsCanceled_T()
        {
            bool canceled = false;

            Promise<int>.Canceled()
                .CatchCancelation(() =>
                {
                    canceled = true;
                })
                .Forget();

            Assert.IsTrue(canceled);
        }

        [Test]
        public void PromiseSwitchToContextWorksProperly_Then(
            [Values(SynchronizationType.Foreground,
#if !UNITY_WEBGL
                SynchronizationType.Background,
#endif
                SynchronizationType.Explicit)] SynchronizationType synchronizationType,
            [Values(SynchronizationType.Foreground
#if !UNITY_WEBGL
                , SynchronizationType.Background
#endif
                )] SynchronizationType invokeContext)
        {
            Thread foregroundThread = Thread.CurrentThread;
            bool invoked = false;

            new ThreadHelper().ExecuteSynchronousOrOnThread(() =>
            {
                Promise promise = synchronizationType == SynchronizationType.Foreground
                    ? Promise.SwitchToForeground()
                    : synchronizationType == TestHelper.backgroundType
                    ? Promise.SwitchToBackground()
                    : Promise.SwitchToContext(TestHelper._foregroundContext);

                promise
                    .Then(() =>
                    {
                        TestHelper.AssertCallbackContext(synchronizationType, invokeContext, foregroundThread);
                        invoked = true;
                    })
                    .Forget();
            }, invokeContext == SynchronizationType.Foreground);

            TestHelper.ExecuteForegroundCallbacks();
            if (synchronizationType != TestHelper.backgroundType)
            {
                Assert.True(invoked);
            }
            else
            {
                if (!SpinWait.SpinUntil(() => invoked, System.TimeSpan.FromSeconds(1)))
                {
                    throw new System.TimeoutException();
                }
            }
        }

        private static IEnumerable<TestCaseData> GetExpectedRejections()
        {
            yield return new TestCaseData(null);
            yield return new TestCaseData(new object());
            yield return new TestCaseData(new System.InvalidOperationException());
            yield return new TestCaseData(42);
        }

        [Test, TestCaseSource("GetExpectedRejections")]
        public void UncaughtRejectionIsSentToUncaughtRejectionHandler_void(object expectedRejectionValue)
        {
            var currentRejectionHandler = Promise.Config.UncaughtRejectionHandler;
            try
            {
                int expectedCount = 0;
                int uncaughtCount = 0;
                Promise.Config.UncaughtRejectionHandler = unhandledException =>
                {
                    if (expectedRejectionValue == null)
                    {
                        Assert.IsInstanceOf<System.NullReferenceException>(unhandledException.Value);
                    }
                    else
                    {
                        TestHelper.AssertRejection(expectedRejectionValue, unhandledException.Value);
                    }
                    ++uncaughtCount;
                };

                System.Action throwExpected = () => { throw Promise.RejectException(expectedRejectionValue); };

                var cancelationSource = CancelationSource.New();
                var promiseToAwait = default(Promise);

                var actions = new System.Action<Promise>[][]
                {
                    TestHelper.ResolveActionsVoid(),
                    TestHelper.ThenActionsVoid(onRejected: throwExpected),
                    TestHelper.CatchActionsVoid(onRejected: throwExpected),
                    TestHelper.ContinueWithActionsVoid(throwExpected)
                }
                .SelectMany(x => x)
                .Concat(
                    new System.Func<Promise, CancelationToken, Promise>[][]
                    {
                        TestHelper.ResolveActionsVoidWithCancelation(),
                        TestHelper.ThenActionsVoidWithCancelation(onRejected: throwExpected),
                        TestHelper.CatchActionsVoidWithCancelation(onRejected: throwExpected),
                        TestHelper.ContinueWithActionsVoidWithCancelation(throwExpected)
                    }
                    .SelectMany(funcs =>
                        funcs.Select(func => (System.Action<Promise>) (promise => func.Invoke(promise, cancelationSource.Token).Forget()))
                    )
                )
                .Concat(
                    TestHelper.ActionsReturningPromiseVoid(() => promiseToAwait)
                    .Select(func =>
                        (System.Action<Promise>) (promise =>
                        {
                            promiseToAwait = promise;
                            func.Invoke().Forget();
                        })
                    )
                );

                var deferred = Promise.NewDeferred();
                var preservedPromise = deferred.Promise.Preserve();

                foreach (var callback in actions)
                {
                    foreach (var promise in TestHelper.GetTestablePromises(preservedPromise))
                    {
                        ++expectedCount;
                        callback.Invoke(promise);
                    }
                }

                preservedPromise.Forget();
                deferred.Reject(expectedRejectionValue);
                cancelationSource.Dispose();
                Assert.AreEqual(expectedCount, uncaughtCount);
            }
            finally
            {
                Promise.Config.UncaughtRejectionHandler = currentRejectionHandler;
            }
        }

        [Test, TestCaseSource("GetExpectedRejections")]
        public void UncaughtRejectionIsSentToUncaughtRejectionHandler_T(object expectedRejectionValue)
        {
            var currentRejectionHandler = Promise.Config.UncaughtRejectionHandler;
            try
            {
                int expectedCount = 0;
                int uncaughtCount = 0;
                Promise.Config.UncaughtRejectionHandler = unhandledException =>
                {
                    if (expectedRejectionValue == null)
                    {
                        Assert.IsInstanceOf<System.NullReferenceException>(unhandledException.Value);
                    }
                    else
                    {
                        TestHelper.AssertRejection(expectedRejectionValue, unhandledException.Value);
                    }
                    ++uncaughtCount;
                };

                System.Action throwExpected = () => { throw Promise.RejectException(expectedRejectionValue); };

                var cancelationSource = CancelationSource.New();
                var promiseToAwait = default(Promise<int>);

                var actions = new System.Action<Promise<int>>[][]
                {
                    TestHelper.ResolveActions<int>(),
                    TestHelper.ThenActions<int>(onRejected: throwExpected),
                    TestHelper.CatchActions<int>(onRejected: throwExpected),
                    TestHelper.ContinueWithActions<int>(throwExpected)
                }
                .SelectMany(x => x)
                .Concat(
                    new System.Func<Promise<int>, CancelationToken, Promise>[][]
                    {
                        TestHelper.ResolveActionsWithCancelation<int>(),
                        TestHelper.ThenActionsWithCancelation<int>(onRejected: throwExpected),
                        TestHelper.CatchActionsWithCancelation<int>(onRejected: throwExpected),
                        TestHelper.ContinueWithActionsWithCancelation<int>(throwExpected)
                    }
                    .SelectMany(funcs =>
                        funcs.Select(func => (System.Action<Promise<int>>) (promise => func.Invoke(promise, cancelationSource.Token).Forget()))
                    )
                )
                .Concat(
                    TestHelper.ActionsReturningPromiseT<int>(() => promiseToAwait)
                    .Select(func =>
                        (System.Action<Promise<int>>) (promise =>
                        {
                            promiseToAwait = promise;
                            func.Invoke().Forget();
                        })
                    )
                );

                var deferred = Promise.NewDeferred<int>();
                var preservedPromise = deferred.Promise.Preserve();

                foreach (var callback in actions)
                {
                    foreach (var promise in TestHelper.GetTestablePromises(preservedPromise))
                    {
                        ++expectedCount;
                        callback.Invoke(promise);
                    }
                }

                preservedPromise.Forget();
                deferred.Reject(expectedRejectionValue);
                cancelationSource.Dispose();
                Assert.AreEqual(expectedCount, uncaughtCount);
            }
            finally
            {
                Promise.Config.UncaughtRejectionHandler = currentRejectionHandler;
            }
        }

        [Test, TestCaseSource("GetExpectedRejections")]
        public void WhenPromiseIsCanceled_UncaughtRejectionIsSentToUncaughtRejectionHandler_void(object expectedRejectionValue)
        {
            // Testing an implementation detail - when a promise is canceled and the previous promise is rejected, it counts as an uncaught rejection.
            // This behavior is subject to change.

            var currentRejectionHandler = Promise.Config.UncaughtRejectionHandler;
            try
            {
                int expectedCount = 0;
                int uncaughtCount = 0;
                Promise.Config.UncaughtRejectionHandler = unhandledException =>
                {
                    if (expectedRejectionValue == null)
                    {
                        Assert.IsInstanceOf<System.NullReferenceException>(unhandledException.Value);
                    }
                    else
                    {
                        TestHelper.AssertRejection(expectedRejectionValue, unhandledException.Value);
                    }
                    ++uncaughtCount;
                };

                var cancelationSource = CancelationSource.New();

                var actions = new System.Func<Promise, CancelationToken, Promise>[][]
                {
                    TestHelper.ResolveActionsVoidWithCancelation(),
                    TestHelper.ThenActionsVoidWithCancelation(onRejected: () => { }),
                    TestHelper.CatchActionsVoidWithCancelation(onRejected: () => { }),
                    TestHelper.ContinueWithActionsVoidWithCancelation(() => { }),
                    new System.Func<Promise, CancelationToken, Promise>[]
                    {
                        (promise, token) => promise.WaitAsync(token),
                        (promise, token) => promise.WaitAsync(SynchronizationOption.Foreground, cancelationToken: token)
                    }
                }
                .SelectMany(x => x);

                var deferred = Promise.NewDeferred();
                var preservedPromise = deferred.Promise.Preserve();

                foreach (var callback in actions)
                {
                    // We subtract 1 because the preserved promise will only report its unhandled rejection if none of the waiters suppress it. (In this case, the .Duplicate() does suppress it.)
                    --expectedCount;
                    foreach (var promise in TestHelper.GetTestablePromises(preservedPromise))
                    {
                        ++expectedCount;
                        callback.Invoke(promise, cancelationSource.Token).Forget();
                    }
                }

                preservedPromise.Forget();
                cancelationSource.Cancel();
                cancelationSource.Dispose();
                deferred.Reject(expectedRejectionValue);
                TestHelper.ExecuteForegroundCallbacks();
                Assert.AreEqual(expectedCount, uncaughtCount);
            }
            finally
            {
                Promise.Config.UncaughtRejectionHandler = currentRejectionHandler;
            }
        }

        [Test, TestCaseSource("GetExpectedRejections")]
        public void WhenPromiseIsCanceled_UncaughtRejectionIsSentToUncaughtRejectionHandler_T(object expectedRejectionValue)
        {
            // Testing an implementation detail - when a promise is canceled and the previous promise is rejected, it counts as an uncaught rejection.
            // This behavior is subject to change.

            var currentRejectionHandler = Promise.Config.UncaughtRejectionHandler;
            try
            {
                int expectedCount = 0;
                int uncaughtCount = 0;
                Promise.Config.UncaughtRejectionHandler = unhandledException =>
                {
                    if (expectedRejectionValue == null)
                    {
                        Assert.IsInstanceOf<System.NullReferenceException>(unhandledException.Value);
                    }
                    else
                    {
                        TestHelper.AssertRejection(expectedRejectionValue, unhandledException.Value);
                    }
                    ++uncaughtCount;
                };

                var cancelationSource = CancelationSource.New();

                var actions = new System.Func<Promise<int>, CancelationToken, Promise>[][]
                {
                    TestHelper.ResolveActionsWithCancelation<int>(),
                    TestHelper.ThenActionsWithCancelation<int>(onRejected: () => { }),
                    TestHelper.CatchActionsWithCancelation<int>(onRejected: () => { }),
                    TestHelper.ContinueWithActionsWithCancelation<int>(() => { }),
                    new System.Func<Promise<int>, CancelationToken, Promise>[]
                    {
                        (promise, token) => promise.WaitAsync(token),
                        (promise, token) => promise.WaitAsync(SynchronizationOption.Foreground, cancelationToken: token)
                    }
                }
                .SelectMany(x => x);

                var deferred = Promise.NewDeferred<int>();
                var preservedPromise = deferred.Promise.Preserve();

                foreach (var callback in actions)
                {
                    // We subtract 1 because the preserved promise will only report its unhandled rejection if none of the waiters suppress it. (In this case, the .Duplicate() does suppress it.)
                    --expectedCount;
                    foreach (var promise in TestHelper.GetTestablePromises(preservedPromise))
                    {
                        ++expectedCount;
                        callback.Invoke(promise, cancelationSource.Token).Forget();
                    }
                }

                preservedPromise.Forget();
                cancelationSource.Cancel();
                cancelationSource.Dispose();
                deferred.Reject(expectedRejectionValue);
                TestHelper.ExecuteForegroundCallbacks();
                Assert.AreEqual(expectedCount, uncaughtCount);
            }
            finally
            {
                Promise.Config.UncaughtRejectionHandler = currentRejectionHandler;
            }
        }

#if CSHARP_7_3_OR_NEWER
        [Test]
        public void PromiseSwitchToContextWorksProperly_Await(
            [Values(SynchronizationType.Foreground,
#if !UNITY_WEBGL
                SynchronizationType.Background,
#endif
                SynchronizationType.Explicit)] SynchronizationType synchronizationType,
            [Values(SynchronizationType.Foreground
#if !UNITY_WEBGL
                , SynchronizationType.Background
#endif
                )] SynchronizationType invokeContext)
        {
            Thread foregroundThread = Thread.CurrentThread;
            bool invoked = false;

            new ThreadHelper().ExecuteSynchronousOrOnThread(() =>
            {
                Await().Forget();

                async Promise Await()
                {
                    Promise promise = synchronizationType == SynchronizationType.Foreground
                        ? Promise.SwitchToForeground()
                        : synchronizationType == TestHelper.backgroundType
                        ? Promise.SwitchToBackground()
                        : Promise.SwitchToContext(TestHelper._foregroundContext);
                    
                    await promise;

                    TestHelper.AssertCallbackContext(synchronizationType, invokeContext, foregroundThread);
                    invoked = true;
                }
            }, invokeContext == SynchronizationType.Foreground);

            TestHelper.ExecuteForegroundCallbacks();
            if (synchronizationType != TestHelper.backgroundType)
            {
                Assert.True(invoked);
            }
            else
            {
                if (!SpinWait.SpinUntil(() => invoked, System.TimeSpan.FromSeconds(1)))
                {
                    throw new System.TimeoutException();
                }
            }
        }
#endif // CSHARP_7_3_OR_NEWER

        [Test]
        public void PromiseMayBeResolvedWithNullable(
            [Values(10, 0, null)] int? expected)
        {
            var deferred = Promise.NewDeferred<int?>();
            deferred.Promise
                .Then(v => Assert.AreEqual(expected, v))
                .Forget();
            deferred.Resolve(expected);
        }

#if !UNITY_WEBGL
        private static readonly System.TimeSpan sleepTime = System.TimeSpan.FromSeconds(0.5);

        private static bool Wait(Promise promise, bool withTimeout)
        {
            if (withTimeout)
            {
                return promise.Wait(System.TimeSpan.Zero);
            }
            promise.Wait();
            return true;
        }

        private static bool Wait<T>(Promise<T> promise, out T result, bool withTimeout)
        {
            if (withTimeout)
            {
                return promise.WaitForResult(System.TimeSpan.Zero, out result);
            }
            result = promise.WaitForResult();
            return true;
        }

        [Test]
        public void PromiseWait_AlreadyCompleted_ReturnsSuccessfullyOrThrowsCorrectException(
            [Values(CompleteType.Resolve, CompleteType.Reject, CompleteType.Cancel)] CompleteType completeType,
            [Values] bool withTimeout)
        {
            var expectedException = completeType == CompleteType.Reject
                ? new InvalidOperationException("Test")
                : (System.Exception) Promise.CancelException();
            var promise = completeType == CompleteType.Resolve ? Promise.Resolved()
                : completeType == CompleteType.Reject ? Promise.Rejected(expectedException)
                : Promise.Canceled();
            bool didCatch = false;
            bool didNotTimeout = false;
            try
            {
                didNotTimeout = Wait(promise, withTimeout);
            }
            // The original exception is thrown in .Net 4.5+, but UnhandledException is thrown in old runtimes in order to preserve stack traces.
            catch (UnhandledException e)
            {
                didCatch = e.Value == expectedException;
            }
            catch (System.Exception e)
            {
                didCatch = completeType == CompleteType.Reject
                    ? e == expectedException
                    : e is CanceledException;
            }
            if (completeType == CompleteType.Resolve)
            {
                Assert.IsTrue(didNotTimeout);
            }
            Assert.AreNotEqual(completeType == CompleteType.Resolve, didCatch);
        }

        [Test]
        public void PromiseWait_DoesNotReturnUntilOperationIsComplete(
            [Values] bool alreadyComplete,
            [Values] bool withTimeout)
        {
            bool isExecuting = false;
            bool isComplete = false;

            var promise = Promise.Run(() =>
            {
                isExecuting = true;

                Thread.Sleep(sleepTime);

                isComplete = true;
            });

            SpinWait.SpinUntil(() => isExecuting);
            if (alreadyComplete)
            {
                Thread.Sleep(sleepTime.Add(sleepTime));
            }
            bool didNotTimeout = Wait(promise, withTimeout);
            bool expectedTimeout = withTimeout && !alreadyComplete;
            Assert.AreNotEqual(expectedTimeout, didNotTimeout);
            Assert.AreNotEqual(expectedTimeout, isComplete);
        }

        [Test]
        public void PromiseWait_DoesNotReturnUntilOperationIsComplete_AndThrowsCorrectException(
            [Values(CompleteType.Reject, CompleteType.Cancel)] CompleteType throwType,
            [Values] bool alreadyComplete,
            [Values] bool withTimeout)
        {
            bool isExecuting = false;
            bool isComplete = false;
            var expectedException = throwType == CompleteType.Reject
                ? new InvalidOperationException("Test")
                : (System.Exception) Promise.CancelException();

            TestHelper.s_expectedUncaughtRejectValue = expectedException;

            var promise = Promise.Run(() =>
            {
                isExecuting = true;

                Thread.Sleep(sleepTime);

                isComplete = true;
                throw expectedException;
            });

            SpinWait.SpinUntil(() => isExecuting);
            if (alreadyComplete)
            {
                Thread.Sleep(sleepTime.Add(sleepTime));
            }
            bool didCatch = false;
            bool didNotTimeout = false;
            try
            {
                didNotTimeout = Wait(promise, withTimeout);
            }
            // The original exception is thrown in .Net 4.5+, but UnhandledException is thrown in old runtimes in order to preserve stack traces.
            catch (UnhandledException e)
            {
                didCatch = e.Value == expectedException;
            }
            catch (System.Exception e)
            {
                didCatch = throwType == CompleteType.Reject
                    ? e == expectedException
                    : e is CanceledException;
            }
            bool expectedTimeout = withTimeout && !alreadyComplete;
            Assert.IsFalse(didNotTimeout);
            Assert.AreNotEqual(expectedTimeout, isComplete);
            Assert.AreNotEqual(expectedTimeout, didCatch);
        }

        [Test]
        public void PromiseWaitForResult_AlreadyCompleted_ReturnsSuccessfullyOrThrowsCorrectException(
            [Values(CompleteType.Resolve, CompleteType.Reject, CompleteType.Cancel)] CompleteType completeType,
            [Values] bool withTimeout)
        {
            var expectedException = completeType == CompleteType.Reject
                ? new InvalidOperationException("Test")
                : (System.Exception) Promise.CancelException();
            int expectedResult = 42;
            var promise = completeType == CompleteType.Resolve ? Promise<int>.Resolved(expectedResult)
                : completeType == CompleteType.Reject ? Promise<int>.Rejected(expectedException)
                : Promise<int>.Canceled();
            bool didCatch = false;
            int result = -1;
            bool didNotTimeout = false;
            try
            {
                didNotTimeout = Wait(promise, out result, withTimeout);
            }
            // The original exception is thrown in .Net 4.5+, but UnhandledException is thrown in old runtimes in order to preserve stack traces.
            catch (UnhandledException e)
            {
                didCatch = e.Value == expectedException;
            }
            catch (System.Exception e)
            {
                didCatch = completeType == CompleteType.Reject
                    ? e == expectedException
                    : e is CanceledException;
            }
            if (completeType == CompleteType.Resolve)
            {
                Assert.IsTrue(didNotTimeout);
                Assert.AreEqual(expectedResult, result);
            }
            else
            {
                Assert.IsTrue(didCatch);
            }
        }

        [Test]
        public void PromiseWaitForResult_DoesNotReturnUntilOperationIsComplete_AndReturnsWithCorrectResult(
            [Values] bool alreadyComplete,
            [Values] bool withTimeout)
        {
            bool isExecuting = false;
            bool isComplete = false;
            int expected = 42;

            var promise = Promise.Run(() =>
            {
                isExecuting = true;

                Thread.Sleep(sleepTime);

                isComplete = true;
                return expected;
            });

            SpinWait.SpinUntil(() => isExecuting);
            if (alreadyComplete)
            {
                Thread.Sleep(sleepTime.Add(sleepTime));
            }
            int result = -1;
            bool didNotTimeout = Wait(promise, out result, withTimeout);
            bool expectedTimeout = withTimeout && !alreadyComplete;
            Assert.AreNotEqual(expectedTimeout, didNotTimeout);
            Assert.AreNotEqual(expectedTimeout, isComplete);
            if (!expectedTimeout)
            {
                Assert.AreEqual(expected, result);
            }
        }

        [Test]
        public void PromiseWaitForResult_DoesNotReturnUntilOperationIsComplete_AndThrowsCorrectException(
            [Values(CompleteType.Reject, CompleteType.Cancel)] CompleteType throwType,
            [Values] bool alreadyComplete,
            [Values] bool withTimeout)
        {
            bool isExecuting = false;
            bool isComplete = false;
            var expectedException = throwType == CompleteType.Reject
                ? new InvalidOperationException("Test")
                : (System.Exception) Promise.CancelException();

            TestHelper.s_expectedUncaughtRejectValue = expectedException;

            var promise = Promise.Run(() =>
            {
                isExecuting = true;

                Thread.Sleep(sleepTime);

                isComplete = true;
                throw expectedException;
                return 42;
            });

            SpinWait.SpinUntil(() => isExecuting);
            if (alreadyComplete)
            {
                Thread.Sleep(sleepTime.Add(sleepTime));
            }
            bool didCatch = false;
            int result = -1;
            bool didNotTimeout = false;
            try
            {
                didNotTimeout = Wait(promise, out result, withTimeout);
            }
            // The original exception is thrown in .Net 4.5+, but UnhandledException is thrown in old runtimes in order to preserve stack traces.
            catch (UnhandledException e)
            {
                didCatch = e.Value == expectedException;
            }
            catch (System.Exception e)
            {
                didCatch = throwType == CompleteType.Reject
                    ? e == expectedException
                    : e is CanceledException;
            }
            bool expectedTimeout = withTimeout && !alreadyComplete;
            Assert.IsFalse(didNotTimeout);
            Assert.AreNotEqual(expectedTimeout, isComplete);
            Assert.AreNotEqual(expectedTimeout, didCatch);
        }

        private static IEnumerable<TestCaseData> GetExpectedRejectionsAndTimeout()
        {
            object[] rejections = new object[4]
            {
                null,
                new object(),
                new System.InvalidOperationException(),
                42
            };
            int[] timeouts = new int[2] { 0, 1 };
            
            foreach (var rejection in rejections)
            foreach (var timeout in timeouts)
            {
                yield return new TestCaseData(rejection, timeout);
            }
        }

        [Test, TestCaseSource("GetExpectedRejectionsAndTimeout")]
        public void PromiseWait_UncaughtRejectionIsSentToUncaughtRejectionHandler(object expectedRejectionValue, int timeout)
        {
            var currentRejectionHandler = Promise.Config.UncaughtRejectionHandler;
            try
            {
                int expectedCount = 0;
                int uncaughtCount = 0;
                Promise.Config.UncaughtRejectionHandler = unhandledException =>
                {
                    if (expectedRejectionValue == null)
                    {
                        Assert.IsInstanceOf<System.NullReferenceException>(unhandledException.Value);
                    }
                    else
                    {
                        TestHelper.AssertRejection(expectedRejectionValue, unhandledException.Value);
                    }
                    ++uncaughtCount;
                };

                var deferred = Promise.NewDeferred();
                var preservedPromise = deferred.Promise.Preserve();

                foreach (var promise in TestHelper.GetTestablePromises(preservedPromise))
                {
                    ++expectedCount;
                    Assert.IsFalse(promise.Wait(System.TimeSpan.FromMilliseconds(timeout)));
                }

                // Run it again with a freshly preserved promise, because the initial promise will have had its rejection suppressed by the other promises.
                var secondPreservedPromise = preservedPromise.Preserve();
                preservedPromise.Forget();
                Assert.IsFalse(secondPreservedPromise.Wait(System.TimeSpan.FromMilliseconds(timeout)));

                secondPreservedPromise.Forget();
                deferred.Reject(expectedRejectionValue);
                Assert.AreEqual(expectedCount, uncaughtCount);
            }
            finally
            {
                Promise.Config.UncaughtRejectionHandler = currentRejectionHandler;
            }
        }

        [Test, TestCaseSource("GetExpectedRejectionsAndTimeout")]
        public void PromiseWaitForResult_UncaughtRejectionIsSentToUncaughtRejectionHandler(object expectedRejectionValue, int timeout)
        {
            var currentRejectionHandler = Promise.Config.UncaughtRejectionHandler;
            try
            {
                int expectedCount = 0;
                int uncaughtCount = 0;
                Promise.Config.UncaughtRejectionHandler = unhandledException =>
                {
                    if (expectedRejectionValue == null)
                    {
                        Assert.IsInstanceOf<System.NullReferenceException>(unhandledException.Value);
                    }
                    else
                    {
                        TestHelper.AssertRejection(expectedRejectionValue, unhandledException.Value);
                    }
                    ++uncaughtCount;
                };

                var deferred = Promise.NewDeferred<int>();
                var preservedPromise = deferred.Promise.Preserve();

                int outResult;
                foreach (var promise in TestHelper.GetTestablePromises(preservedPromise))
                {
                    ++expectedCount;
                    Assert.IsFalse(promise.WaitForResult(System.TimeSpan.FromMilliseconds(timeout), out outResult));
                }

                // Run it again with a freshly preserved promise, because the initial promise will have had its rejection suppressed by the other promises.
                var secondPreservedPromise = preservedPromise.Preserve();
                preservedPromise.Forget();
                Assert.IsFalse(secondPreservedPromise.WaitForResult(System.TimeSpan.FromMilliseconds(timeout), out outResult));

                secondPreservedPromise.Forget();
                deferred.Reject(expectedRejectionValue);
                Assert.AreEqual(expectedCount, uncaughtCount);
            }
            finally
            {
                Promise.Config.UncaughtRejectionHandler = currentRejectionHandler;
            }
        }
#endif // !UNITY_WEBGL
    }
}