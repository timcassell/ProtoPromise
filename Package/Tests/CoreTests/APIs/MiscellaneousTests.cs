﻿#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using NUnit.Framework;
using Proto.Promises;
using ProtoPromise.Tests.Concurrency;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ProtoPromise.Tests.APIs
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
        public void PromiseIsValidBeforeAwaited_void()
        {
            var promise = Promise.Resolved();
            promise.Forget();
        }

        [Test]
        public void PromiseIsValidBeforeAwaited_T()
        {
            var promise = Promise.Resolved(42);
            promise.Forget();
        }

        [Test]
        public void PromiseIsInvalidAfterAwaited_void()
        {
            var deferred = Promise.NewDeferred();
            var promise = deferred.Promise;
            promise.Then(() => { }).Forget();

#if PROMISE_DEBUG
            Assert.Throws<InvalidOperationException>(() => promise.GetAwaiter());
#endif
#pragma warning disable CS0618 // Type or member is obsolete
            Assert.Throws<InvalidOperationException>(() => promise.Preserve());
            Assert.Throws<InvalidOperationException>(() => promise.Duplicate());
#pragma warning restore CS0618 // Type or member is obsolete
            Assert.Throws<InvalidOperationException>(() => promise.GetRetainer());
            Assert.Throws<InvalidOperationException>(() => promise.Forget());

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
            promise.Then(v => { }).Forget();

#if PROMISE_DEBUG
            Assert.Throws<InvalidOperationException>(() => promise.GetAwaiter());
#endif
#pragma warning disable CS0618 // Type or member is obsolete
            Assert.Throws<InvalidOperationException>(() => promise.Preserve());
            Assert.Throws<InvalidOperationException>(() => promise.Duplicate());
#pragma warning restore CS0618 // Type or member is obsolete
            Assert.Throws<InvalidOperationException>(() => promise.GetRetainer());
            Assert.Throws<InvalidOperationException>(() => promise.Forget());

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
            using (var promiseRetainer = Promise.Resolved().GetRetainer())
            {
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

                TestHelper.AddResolveCallbacks<int, string>(promiseRetainer.WaitAsync(),
                    onResolve: () => { throw Promise.RejectException(expected); },
                    onCallbackAdded: onCallbackAdded,
                    onCallbackAddedConvert: onCallbackAddedConvert
                );
                TestHelper.AddCallbacks<int, object, string>(promiseRetainer.WaitAsync(),
                    onResolve: () => { throw Promise.RejectException(expected); },
                    onCallbackAdded: onCallbackAdded,
                    onCallbackAddedConvert: onCallbackAddedConvert
                );
                TestHelper.AddContinueCallbacks<int, string>(promiseRetainer.WaitAsync(),
                    onContinue: _ => { throw Promise.RejectException(expected); },
                    onCallbackAdded: onCallbackAdded,
                    onCallbackAddedConvert: onCallbackAddedConvert
                );

                Assert.AreEqual(
                    (TestHelper.resolveVoidCallbacks + TestHelper.continueVoidCallbacks) * 2,
                    rejectCount
                );
            }
        }

        [Test]
        public void ThrowingRejectExceptionInOnResolvedRejectsThePromiseWithTheGivenValue_T()
        {
            using (var promiseRetainer = Promise.Resolved(100).GetRetainer())
            {
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

                TestHelper.AddResolveCallbacks<int, int, string>(promiseRetainer.WaitAsync(),
                    onResolve: v => { throw Promise.RejectException(expected); },
                    onCallbackAdded: onCallbackAdded,
                    onCallbackAddedConvert: onCallbackAddedConvert
                );
                TestHelper.AddCallbacks<int, int, object, string>(promiseRetainer.WaitAsync(),
                    onResolve: v => { throw Promise.RejectException(expected); },
                    onCallbackAdded: onCallbackAdded,
                    onCallbackAddedConvert: onCallbackAddedConvert
                );
                TestHelper.AddContinueCallbacks<int, int, string>(promiseRetainer.WaitAsync(),
                    onContinue: _ => { throw Promise.RejectException(expected); },
                    onCallbackAdded: onCallbackAdded,
                    onCallbackAddedConvert: onCallbackAddedConvert
                );

                Assert.AreEqual(
                    (TestHelper.resolveTCallbacks + TestHelper.continueTCallbacks) * 2,
                    rejectCount
                );
            }
        }

        [Test]
        public void ThrowingRejectExceptionInOnRejectedRejectsThePromiseWithTheGivenValue_void()
        {
            using (var promiseRetainer = Promise.Rejected("A different reject value.").GetRetainer())
            {
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

                TestHelper.AddCallbacks<int, object, string>(promiseRetainer.WaitAsync(),
                    onReject: v => { throw Promise.RejectException(expected); },
                    onUnknownRejection: () => { throw Promise.RejectException(expected); },
                    onCallbackAdded: onCallbackAdded,
                    onCallbackAddedConvert: onCallbackAddedConvert
                );
                TestHelper.AddContinueCallbacks<int, string>(promiseRetainer.WaitAsync(),
                    onContinue: _ => { throw Promise.RejectException(expected); },
                    onCallbackAdded: onCallbackAdded,
                    onCallbackAddedConvert: onCallbackAddedConvert
                );

                Assert.AreEqual(
                    (TestHelper.rejectVoidCallbacks + TestHelper.continueVoidCallbacks) * 2,
                    rejectCount
                );
            }
        }

        [Test]
        public void ThrowingRejectExceptionInOnRejectedRejectsThePromiseWithTheGivenValue_T()
        {
            using (var promiseRetainer = Promise<int>.Rejected("A different reject value.").GetRetainer())
            {
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

                TestHelper.AddCallbacks<int, int, object, string>(promiseRetainer.WaitAsync(),
                    onReject: v => { throw Promise.RejectException(expected); },
                    onUnknownRejection: () => { throw Promise.RejectException(expected); },
                    onCallbackAdded: onCallbackAdded,
                    onCallbackAddedConvert: onCallbackAddedConvert,
                    onCallbackAddedT: onCallbackAddedConvert
                );
                TestHelper.AddContinueCallbacks<int, int, string>(promiseRetainer.WaitAsync(),
                    onContinue: _ => { throw Promise.RejectException(expected); },
                    onCallbackAdded: onCallbackAdded,
                    onCallbackAddedConvert: onCallbackAddedConvert
                );

                Assert.AreEqual(
                    (TestHelper.rejectTCallbacks + TestHelper.continueTCallbacks) * 2,
                    rejectCount
                );
            }
        }

        [Test]
        public void ThrowingCancelExceptionInOnResolvedCancelsThePromiseWithTheGivenValue_void()
        {
            using (var promiseRetainer = Promise.Resolved().GetRetainer())
            {
                int cancelCount = 0;

                System.Action onCancel = () =>
                {
                    ++cancelCount;
                };

                TestHelper.AddResolveCallbacks<int, string>(promiseRetainer.WaitAsync(),
                    onResolve: () => { throw Promise.CancelException(); },
                    onCancel: onCancel
                );
                TestHelper.AddCallbacks<int, object, string>(promiseRetainer.WaitAsync(),
                    onResolve: () => { throw Promise.CancelException(); },
                    onCancel: onCancel
                );
                TestHelper.AddContinueCallbacks<int, string>(promiseRetainer.WaitAsync(),
                    onContinue: _ => { throw Promise.CancelException(); },
                    onCancel: onCancel
                );

                Assert.AreEqual(
                    (TestHelper.resolveVoidCallbacks + TestHelper.continueVoidCallbacks) * 2,
                    cancelCount
                );
            }
        }

        [Test]
        public void ThrowingCancelExceptionInOnResolvedCancelsThePromiseWithTheGivenValue_T()
        {
            using (var promiseRetainer = Promise.Resolved(100).GetRetainer())
            {
                int cancelCount = 0;

                System.Action onCancel = () =>
                {
                    ++cancelCount;
                };

                TestHelper.AddResolveCallbacks<int, bool, string>(promiseRetainer.WaitAsync(),
                    onResolve: v => { throw Promise.CancelException(); },
                    onCancel: onCancel
                );
                TestHelper.AddCallbacks<int, bool, object, string>(promiseRetainer.WaitAsync(),
                    onResolve: v => { throw Promise.CancelException(); },
                    onCancel: onCancel
                );
                TestHelper.AddContinueCallbacks<int, bool, string>(promiseRetainer.WaitAsync(),
                    onContinue: _ => { throw Promise.CancelException(); },
                    onCancel: onCancel
                );

                Assert.AreEqual(
                    (TestHelper.resolveTCallbacks + TestHelper.continueTCallbacks) * 2,
                    cancelCount
                );
            }
        }

        [Test]
        public void ThrowingCancelExceptionInOnRejectedCancelsThePromiseWithTheGivenValue_void()
        {
            using (var promiseRetainer = Promise.Rejected("Rejected").GetRetainer())
            {
                int cancelCount = 0;

                System.Action onCancel = () =>
                {
                    ++cancelCount;
                };

                TestHelper.AddCallbacks<int, object, string>(promiseRetainer.WaitAsync(),
                    onReject: v => { throw Promise.CancelException(); },
                    onUnknownRejection: () => { throw Promise.CancelException(); },
                    onCancel: onCancel
                );
                TestHelper.AddContinueCallbacks<int, string>(promiseRetainer.WaitAsync(),
                    onContinue: _ => { throw Promise.CancelException(); },
                    onCancel: onCancel
                );

                Assert.AreEqual(
                    (TestHelper.rejectVoidCallbacks + TestHelper.continueVoidCallbacks) * 2,
                    cancelCount
                );
            }
        }

        [Test]
        public void ThrowingCancelExceptionInOnRejectedCancelsThePromiseWithTheGivenValue_T()
        {
            using (var promiseRetainer = Promise<int>.Rejected("Rejected").GetRetainer())
            {
                int cancelCount = 0;

                System.Action onCancel = () =>
                {
                    ++cancelCount;
                };

                TestHelper.AddCallbacks<int, bool, object, string>(promiseRetainer.WaitAsync(),
                    onReject: v => { throw Promise.CancelException(); },
                    onUnknownRejection: () => { throw Promise.CancelException(); },
                    onCancel: onCancel
                );
                TestHelper.AddContinueCallbacks<int, int, string>(promiseRetainer.WaitAsync(),
                    onContinue: _ => { throw Promise.CancelException(); },
                    onCancel: onCancel
                );

                Assert.AreEqual(
                    (TestHelper.rejectTCallbacks + TestHelper.continueTCallbacks) * 2,
                    cancelCount
                );
            }
        }

        [Test]
        public void ThrowingRethrowInOnResolvedRejectsThePromiseWithInvalidOperationException_void()
        {
            using (var promiseRetainer = Promise.Resolved().GetRetainer())
            {
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

                TestHelper.AddResolveCallbacks<int, string>(promiseRetainer.WaitAsync(),
                    onResolve: () => { throw Promise.Rethrow; },
                    onCallbackAdded: onCallbackAdded,
                    onCallbackAddedConvert: onCallbackAddedConvert
                );
                TestHelper.AddCallbacks<int, object, string>(promiseRetainer.WaitAsync(),
                    onResolve: () => { throw Promise.Rethrow; },
                    onCallbackAdded: onCallbackAdded,
                    onCallbackAddedConvert: onCallbackAddedConvert
                );
                TestHelper.AddContinueCallbacks<int, string>(promiseRetainer.WaitAsync(),
                    onContinue: _ => { throw Promise.Rethrow; },
                    onCallbackAdded: onCallbackAdded,
                    onCallbackAddedConvert: onCallbackAddedConvert
                );

                Assert.AreEqual(
                    (TestHelper.resolveVoidCallbacks + TestHelper.continueVoidCallbacks) * 2,
                    errorCount
                );
            }
        }

        [Test]
        public void ThrowingRethrowInOnResolvedRejectsThePromiseWithInvalidOperationException_T()
        {
            using (var promiseRetainer = Promise.Resolved(100).GetRetainer())
            {
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

                TestHelper.AddResolveCallbacks<int, int, string>(promiseRetainer.WaitAsync(),
                    onResolve: v => { throw Promise.Rethrow; },
                    onCallbackAdded: onCallbackAdded,
                    onCallbackAddedConvert: onCallbackAddedConvert
                );
                TestHelper.AddCallbacks<int, int, object, string>(promiseRetainer.WaitAsync(),
                    onResolve: v => { throw Promise.Rethrow; },
                    onCallbackAdded: onCallbackAdded,
                    onCallbackAddedConvert: onCallbackAddedConvert
                );
                TestHelper.AddContinueCallbacks<int, int, string>(promiseRetainer.WaitAsync(),
                    onContinue: _ => { throw Promise.Rethrow; },
                    onCallbackAdded: onCallbackAdded,
                    onCallbackAddedConvert: onCallbackAddedConvert
                );

                Assert.AreEqual(
                    (TestHelper.resolveTCallbacks + TestHelper.continueTCallbacks) * 2,
                    errorCount
                );
            }
        }

        [Test]
        public void ThrowingRethrowInOnRejectedRejectsThePromiseWithTheSameReason_void(
            [Values] bool alreadyComplete)
        {
            string expected = "Reject!";

            using (var promiseRetainer = TestHelper.BuildPromise(CompleteType.Reject, alreadyComplete, expected, out var tryCompleter)
                .GetRetainer())
            {
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

                TestHelper.AddCallbacks<int, object, string>(promiseRetainer.WaitAsync(),
                    onReject: _ => { throw Promise.Rethrow; },
                    onUnknownRejection: () => { throw Promise.Rethrow; },
                    onCallbackAdded: onCallbackAdded,
                    onCallbackAddedConvert: onCallbackAddedConvert
                );

                tryCompleter();

                Assert.AreEqual(
                    TestHelper.rejectVoidCallbacks * 2,
                    rejectCount
                );
            }
        }

        [Test]
        public void ThrowingRethrowInOnRejectedRejectsThePromiseWithTheSameReason_T(
            [Values] bool alreadyComplete)
        {
            string expected = "Reject!";

            using (var promiseRetainer = TestHelper.BuildPromise(CompleteType.Reject, alreadyComplete, 0, expected, out var tryCompleter)
                .GetRetainer())
            {
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

                TestHelper.AddCallbacks<int, int, object, string>(promiseRetainer.WaitAsync(),
                    onReject: _ => { throw Promise.Rethrow; },
                    onUnknownRejection: () => { throw Promise.Rethrow; },
                    onCallbackAdded: onCallbackAdded,
                    onCallbackAddedConvert: onCallbackAddedConvert,
                    onCallbackAddedT: onCallbackAddedConvert
                );

                tryCompleter();

                Assert.AreEqual(
                    TestHelper.rejectTCallbacks * 2,
                    rejectCount
                );
            }
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
                )] SynchronizationType invokeContext,
            [Values] bool forceAsync)
        {
            Thread foregroundThread = Thread.CurrentThread;
            bool invoked = false;

            new ThreadHelper().ExecuteSynchronousOrOnThread(() =>
            {
                Promise promise = synchronizationType == SynchronizationType.Foreground
                    ? Promise.SwitchToForeground(forceAsync)
                    : synchronizationType == TestHelper.backgroundType
                    ? Promise.SwitchToBackground(forceAsync)
                    : Promise.SwitchToContext(TestHelper._foregroundContext, forceAsync);

                promise
                    .Then(() =>
                    {
                        TestHelper.AssertCallbackContext(synchronizationType, invokeContext, foregroundThread);
                        invoked = true;
                    })
                    .Forget();
            }, invokeContext == SynchronizationType.Foreground);

            TestHelper.ExecuteForegroundCallbacksAndWaitForThreadsToComplete();
            Assert.True(invoked);
        }

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
                )] SynchronizationType invokeContext,
            [Values] bool forceAsync)
        {
            Thread foregroundThread = Thread.CurrentThread;
            bool invoked = false;

            new ThreadHelper().ExecuteSynchronousOrOnThread(() =>
            {
                Await().Forget();

                async Promise Await()
                {
                    var contextSwitcher = synchronizationType == SynchronizationType.Foreground
                        ? Promise.SwitchToForegroundAwait(forceAsync)
                        : synchronizationType == TestHelper.backgroundType
                        ? Promise.SwitchToBackgroundAwait(forceAsync)
                        : Promise.SwitchToContextAwait(TestHelper._foregroundContext, forceAsync);

                    await contextSwitcher;

                    TestHelper.AssertCallbackContext(synchronizationType, invokeContext, foregroundThread);
                    invoked = true;
                }
            }, invokeContext == SynchronizationType.Foreground);

            TestHelper.ExecuteForegroundCallbacksAndWaitForThreadsToComplete();
            Assert.True(invoked);
        }

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
                return promise.TryWait(System.TimeSpan.Zero);
            }
            promise.Wait();
            return true;
        }

        private static bool WaitNoThrow(Promise promise, out Promise.ResultContainer resultContainer, bool withTimeout)
        {
            if (withTimeout)
            {
                return promise.TryWaitNoThrow(System.TimeSpan.Zero, out resultContainer);
            }
            resultContainer = promise.WaitNoThrow();
            return true;
        }

        private static bool Wait<T>(Promise<T> promise, out T result, bool withTimeout)
        {
            if (withTimeout)
            {
                return promise.TryWaitForResult(System.TimeSpan.Zero, out result);
            }
            result = promise.WaitForResult();
            return true;
        }

        private static bool WaitNoThrow<T>(Promise<T> promise, out Promise<T>.ResultContainer resultContainer, bool withTimeout)
        {
            if (withTimeout)
            {
                return promise.TryWaitForResultNoThrow(System.TimeSpan.Zero, out resultContainer);
            }
            resultContainer = promise.WaitForResultNoThrow();
            return true;
        }

        [Test]
        public void PromiseWait_AlreadyCompleted_ReturnsSuccessfullyOrThrowsCorrectException(
            [Values] CompleteType completeType,
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
        public void PromiseWaitNoThrow_AlreadyCompleted_ReturnsSuccessfullyWithProperResult(
            [Values] CompleteType completeType,
            [Values] bool withTimeout)
        {
            string expectedRejection = "Rejected";
            var promise = completeType == CompleteType.Resolve ? Promise.Resolved()
                : completeType == CompleteType.Reject ? Promise.Rejected(expectedRejection)
                : Promise.Canceled();
            Promise.ResultContainer resultContainer;
            bool didNotTimeout = WaitNoThrow(promise, out resultContainer, withTimeout);
            Assert.True(didNotTimeout);
            Assert.AreEqual(completeType, (CompleteType) resultContainer.State);
            if (completeType == CompleteType.Reject)
            {
                Assert.AreEqual(expectedRejection, resultContainer.Reason);
            }
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

            TestHelper.SpinUntil(() => isExecuting, System.TimeSpan.FromSeconds(1));
            if (alreadyComplete)
            {
                Thread.Sleep(sleepTime.Add(sleepTime));
            }
            bool didNotTimeout = Wait(promise, withTimeout);
            bool expectedTimeout = withTimeout && !alreadyComplete;
            Assert.AreNotEqual(expectedTimeout, isComplete);
            Assert.AreNotEqual(expectedTimeout, didNotTimeout);
        }

        [Test]
        public void PromiseWaitNoThrow_DoesNotReturnUntilOperationIsComplete(
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

            TestHelper.SpinUntil(() => isExecuting, System.TimeSpan.FromSeconds(1));
            if (alreadyComplete)
            {
                Thread.Sleep(sleepTime.Add(sleepTime));
            }
            Promise.ResultContainer resultContainer;
            bool didNotTimeout = WaitNoThrow(promise, out resultContainer, withTimeout);
            bool expectedTimeout = withTimeout && !alreadyComplete;
            Assert.AreNotEqual(expectedTimeout, isComplete);
            Assert.AreNotEqual(expectedTimeout, didNotTimeout);
            if (!expectedTimeout)
            {
                Assert.AreEqual(Promise.State.Resolved, resultContainer.State);
            }
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

            TestHelper.SpinUntil(() => isExecuting, System.TimeSpan.FromSeconds(1));
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
        public void PromiseWaitNoThrow_DoesNotReturnUntilOperationIsComplete_WithCorrectReason(
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

            TestHelper.SpinUntil(() => isExecuting, System.TimeSpan.FromSeconds(1));
            if (alreadyComplete)
            {
                Thread.Sleep(sleepTime.Add(sleepTime));
            }
            Promise.ResultContainer resultContainer;
            bool didNotTimeout = WaitNoThrow(promise, out resultContainer, withTimeout);
            bool expectedTimeout = withTimeout && !alreadyComplete;
            Assert.AreNotEqual(expectedTimeout, isComplete);
            Assert.AreNotEqual(expectedTimeout, didNotTimeout);
            if (!expectedTimeout)
            {
                Assert.AreEqual(throwType, (CompleteType) resultContainer.State);
                if (throwType == CompleteType.Reject)
                {
                    Assert.AreEqual(expectedException, resultContainer.Reason);
                }
            }
        }

        [Test]
        public void PromiseWaitForResult_AlreadyCompleted_ReturnsSuccessfullyOrThrowsCorrectException(
            [Values] CompleteType completeType,
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
        public void PromiseWaitForResultNoThrow_AlreadyCompleted_ReturnsSuccessfullyWithProperResult(
            [Values] CompleteType completeType,
            [Values] bool withTimeout)
        {
            var expectedException = completeType == CompleteType.Reject
                ? new InvalidOperationException("Test")
                : (System.Exception) Promise.CancelException();
            int expectedResult = 42;
            var promise = completeType == CompleteType.Resolve ? Promise<int>.Resolved(expectedResult)
                : completeType == CompleteType.Reject ? Promise<int>.Rejected(expectedException)
                : Promise<int>.Canceled();
            Promise<int>.ResultContainer resultContainer;
            bool didNotTimeout = WaitNoThrow(promise, out resultContainer, withTimeout);
            Assert.IsTrue(didNotTimeout);
            Assert.AreEqual(completeType, (CompleteType) resultContainer.State);
            if (completeType == CompleteType.Resolve)
            {
                Assert.AreEqual(expectedResult, resultContainer.Value);
            }
            else if (completeType == CompleteType.Reject)
            {
                Assert.AreEqual(expectedException, resultContainer.Reason);
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

            TestHelper.SpinUntil(() => isExecuting, System.TimeSpan.FromSeconds(1));
            if (alreadyComplete)
            {
                Thread.Sleep(sleepTime.Add(sleepTime));
            }
            int result = -1;
            bool didNotTimeout = Wait(promise, out result, withTimeout);
            bool expectedTimeout = withTimeout && !alreadyComplete;
            Assert.AreNotEqual(expectedTimeout, isComplete);
            Assert.AreNotEqual(expectedTimeout, didNotTimeout);
            if (!expectedTimeout)
            {
                Assert.AreEqual(expected, result);
            }
        }

        [Test]
        public void PromiseWaitForResultNoThrow_DoesNotReturnUntilOperationIsComplete_AndReturnsWithCorrectResult(
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

            TestHelper.SpinUntil(() => isExecuting, System.TimeSpan.FromSeconds(1));
            if (alreadyComplete)
            {
                Thread.Sleep(sleepTime.Add(sleepTime));
            }
            Promise<int>.ResultContainer resultContainer;
            bool didNotTimeout = WaitNoThrow(promise, out resultContainer, withTimeout);
            bool expectedTimeout = withTimeout && !alreadyComplete;
            Assert.AreNotEqual(expectedTimeout, isComplete);
            Assert.AreNotEqual(expectedTimeout, didNotTimeout);
            if (!expectedTimeout)
            {
                Assert.AreEqual(Promise.State.Resolved, resultContainer.State);
                Assert.AreEqual(expected, resultContainer.Value);
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
#pragma warning disable CS0162 // Unreachable code detected
                return 42;
#pragma warning restore CS0162 // Unreachable code detected
            });

            TestHelper.SpinUntil(() => isExecuting, System.TimeSpan.FromSeconds(1));
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

        [Test]
        public void PromiseWaitForResultNoThrow_DoesNotReturnUntilOperationIsComplete_WithCorrectReason(
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
#pragma warning disable CS0162 // Unreachable code detected
                return 42;
#pragma warning restore CS0162 // Unreachable code detected
            });

            TestHelper.SpinUntil(() => isExecuting, System.TimeSpan.FromSeconds(1));
            if (alreadyComplete)
            {
                Thread.Sleep(sleepTime.Add(sleepTime));
            }
            Promise<int>.ResultContainer resultContainer;
            bool didNotTimeout = WaitNoThrow(promise, out resultContainer, withTimeout);
            bool expectedTimeout = withTimeout && !alreadyComplete;
            Assert.AreNotEqual(expectedTimeout, isComplete);
            Assert.AreNotEqual(expectedTimeout, didNotTimeout);
            if (!expectedTimeout)
            {
                Assert.AreEqual(throwType, (CompleteType) resultContainer.State);
                if (throwType == CompleteType.Reject)
                {
                    Assert.AreEqual(expectedException, resultContainer.Reason);
                }
            }
        }
#endif // !UNITY_WEBGL

        [Test]
        public void ClearObjectPool_NoErrors()
        {
            var deferred = Promise.NewDeferred();
#pragma warning disable CS0618 // Type or member is obsolete
            var promise = deferred.Promise.Preserve();
#pragma warning restore CS0618 // Type or member is obsolete

            promise
                .Then(() => { })
                .Forget();

            deferred.Resolve();
            promise.Forget();

            deferred = Promise.NewDeferred();
            using (var promiseRetainer = deferred.Promise.GetRetainer())
            {
                promiseRetainer.WaitAsync()
                    .Then(() => { })
                    .Forget();
            }

            deferred.Resolve();

            Promise.Manager.ClearObjectPool();
        }

        [Test]
        public void PromiseRetainer_CanAddCallbacksToWaitAsyncPromiseAfterDisposed_void()
        {
            var deferred = Promise.NewDeferred();
            var promiseRetainer = deferred.Promise.GetRetainer();
            var promises = new Queue<Promise>();
            var actions = TestHelper.ResolveActionsVoid()
                .Concat(TestHelper.ThenActionsVoid())
                .Concat(TestHelper.CatchActionsVoid())
                .Concat(TestHelper.ContinueWithActionsVoid());
            foreach (var action in actions)
            {
                promises.Enqueue(promiseRetainer.WaitAsync());
            }
            promiseRetainer.Dispose();
            foreach (var action in actions)
            {
                action.Invoke(promises.Dequeue());
            }

            deferred.Resolve();
        }

        [Test]
        public void PromiseRetainer_CanAddCallbacksToWaitAsyncPromiseAfterDisposed_T()
        {
            var deferred = Promise.NewDeferred<int>();
            var promiseRetainer = deferred.Promise.GetRetainer();
            var promises = new Queue<Promise<int>>();
            var actions = TestHelper.ResolveActions<int>()
                .Concat(TestHelper.ThenActions<int>())
                .Concat(TestHelper.CatchActions<int>())
                .Concat(TestHelper.ContinueWithActions<int>());
            foreach (var action in actions)
            {
                promises.Enqueue(promiseRetainer.WaitAsync());
            }
            promiseRetainer.Dispose();
            foreach (var action in actions)
            {
                action.Invoke(promises.Dequeue());
            }

            deferred.Resolve(1);
        }

        [Test]
        public void PromiseAppendResult_void(
            [Values] CompleteType completeType,
            [Values] bool alreadyComplete)
        {
            const int appendValue = 42;
            var expectedException = new System.Exception("Bang!");
            int expectedCompletedCount = 0;
            int completedCount = 0;

            var promiseRetainer = TestHelper.BuildPromise(completeType, alreadyComplete, expectedException, out var tryCompleter).GetRetainer();
            foreach (var promise in TestHelper.GetTestablePromises(promiseRetainer))
            {
                ++expectedCompletedCount;
                promise
                    .AppendResult(appendValue)
                    .ContinueWith(result =>
                    {
                        Assert.AreEqual((Promise.State) completeType, result.State);
                        if (completeType == CompleteType.Resolve)
                        {
                            Assert.AreEqual(appendValue, result.Value);
                        }
                        else if (completeType == CompleteType.Reject)
                        {
                            Assert.AreEqual(expectedException, result.Reason);
                        }
                        ++completedCount;
                    })
                    .Forget();
            }

            tryCompleter.Invoke();
            Assert.AreEqual(expectedCompletedCount, completedCount);
            promiseRetainer.Dispose();
        }

        [Test]
        public void PromiseAppendResult_T(
            [Values] CompleteType completeType,
            [Values] bool alreadyComplete)
        {
            const int expectedResult = 1;
            const int appendValue = 42;
            var expectedException = new System.Exception("Bang!");
            int expectedCompletedCount = 0;
            int completedCount = 0;

            var promiseRetainer = TestHelper.BuildPromise(completeType, alreadyComplete, expectedResult, expectedException, out var tryCompleter).GetRetainer();
            foreach (var promise in TestHelper.GetTestablePromises(promiseRetainer))
            {
                ++expectedCompletedCount;
                promise
                    .AppendResult(appendValue)
                    .ContinueWith(result =>
                    {
                        Assert.AreEqual((Promise.State) completeType, result.State);
                        if (completeType == CompleteType.Resolve)
                        {
                            Assert.AreEqual((expectedResult, appendValue), result.Value);
                        }
                        else if (completeType == CompleteType.Reject)
                        {
                            Assert.AreEqual(expectedException, result.Reason);
                        }
                        ++completedCount;
                    })
                    .Forget();
            }

            tryCompleter.Invoke();
            Assert.AreEqual(expectedCompletedCount, completedCount);
            promiseRetainer.Dispose();
        }
    }
}