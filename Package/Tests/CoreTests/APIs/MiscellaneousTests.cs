#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using NUnit.Framework;
using Proto.Diagnostics;
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

        private static int GetCausalityTraceDepth(CausalityTrace trace, Dictionary<CausalityTrace, int> depths = null)
        {
            depths = depths ?? new Dictionary<CausalityTrace, int>();
            if (depths.TryGetValue(trace, out int depth))
            {
                return depth;
            }
            int maxPreviousDepth = 0;
            foreach (var previousTrace in trace.PreviousTraces)
            {
                if (previousTrace != null)
                {
                    maxPreviousDepth = System.Math.Max(maxPreviousDepth, GetCausalityTraceDepth(previousTrace, depths));
                }
            }
            depth = 1 + maxPreviousDepth;
            depths.Add(trace, depth);
            return depth;
        }

        [Test]
        public void CausalityTraceCapture_ReturnsNullWhenDisabled()
        {
            CausalityTrace.IsEnabled = false;
            Assert.IsNull(CausalityTrace.Capture());
            Assert.IsNull(CausalityTrace.Capture(0));
        }

        [Test]
        public void CausalityTraceCapture_ReturnsTraceStartingAtCallerWhenEnabled()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var trace = CausalityTrace.Capture();
                Assert.IsNotNull(trace);
#if !PROTO_PROMISE_DEVELOPER_MODE // Developer mode doesn't skip frames.
                Assert.AreEqual(nameof(CausalityTraceCapture_ReturnsTraceStartingAtCallerWhenEnabled), trace.StackTrace.GetFrame(0).GetMethod().Name);
#endif
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTraceToString_PrintsTheMainChainFirst_WithReferencesToOuterScopes()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var traceA = CausalityTrace.Capture();
                var scopeA = CausalityTrace.EnterScope(traceA);
                var traceB = CausalityTrace.Capture();
                // B's callback runs synchronously inside A's callback.
                var scopeB = CausalityTrace.EnterScope(traceB);
                var traceX = CausalityTrace.Capture();
                scopeB.Dispose();
                scopeA.Dispose();

                CollectionAssert.AreEqual(new[] { traceB, traceA }, traceX.PreviousTraces);

                var text = traceX.ToString();
                var lines = text.Split(new[] { System.Environment.NewLine }, System.StringSplitOptions.None);
                // X is printed first, with a reference to its outer scope A, then its async cause B.
                int referenceIndex = System.Array.IndexOf(lines, "   --- [Outer causality scope A, see below] ---");
                int causeIndex = System.Array.IndexOf(lines, "--- End of stack trace from the previous location ---");
                Assert.Greater(referenceIndex, 0);
                Assert.Greater(causeIndex, referenceIndex);
                // A is also B's async cause, so it's printed in the main chain, and again in its own section below.
                Assert.Greater(System.Array.IndexOf(lines, "--- [Outer causality scope A] ---"), causeIndex);
                // The main chain is X -> B -> A, and A has no history.
                Assert.AreEqual(2, lines.Count(l => l == "--- End of stack trace from the previous location ---"));
                Assert.IsFalse(lines.Any(l => l.StartsWith("    ")), text);
                AssertCausalityTraceReferencesAreLabeled(text);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTraceToString_IsMemoized()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var trace = CausalityTrace.Capture();
                Assert.AreSame(trace.ToString(), trace.ToString());
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE // Developer mode doesn't hide internal frames.
        [Test]
        public void CausalityTraceGetFrames_ExcludesInternalFrames()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                CausalityTrace trace = null;
                var deferred = Promise.NewDeferred();
                deferred.Promise
                    .Then(() => trace = CausalityTrace.Capture())
                    .Forget();
                deferred.Resolve();

                var rawFrames = trace.StackTrace.GetFrames();
                var visibleFrames = trace.GetFrames();
                System.Func<System.Diagnostics.StackFrame, bool> isInternal = f => f.GetMethod().DeclaringType?.Namespace?.StartsWith("Proto.") == true;
                Assert.IsTrue(rawFrames.Any(isInternal), "The raw stack trace was expected to contain internal frames.");
                Assert.IsFalse(visibleFrames.Any(isInternal), string.Join(System.Environment.NewLine, visibleFrames.Where(isInternal).Select(f => f.GetMethod().DeclaringType + "." + f.GetMethod().Name)));
                Assert.Less(visibleFrames.Length, rawFrames.Length);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }
#endif

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void RejectForReportTraceTest(Promise.Deferred deferred, System.Exception exception)
        {
            deferred.Reject(exception);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void DisposeForReportTraceTest(Promise.Retainer retainer)
        {
            retainer.Dispose();
        }

        private static UnhandledException CaptureUnhandledException(System.Exception exception, System.Action reportUnhandledRejection)
        {
            UnhandledException unhandledException = null;
            var originalHandler = Promise.Config.UncaughtRejectionHandler;
            Promise.Config.UncaughtRejectionHandler = e => unhandledException = e;
            try
            {
                reportUnhandledRejection();
            }
            finally
            {
                Promise.Config.UncaughtRejectionHandler = originalHandler;
            }
            Assert.IsNotNull(unhandledException);
#if PROTO_PROMISE_DEVELOPER_MODE
            // Developer mode wraps the reported exception in another one with the location it was reported.
            unhandledException = (UnhandledException) unhandledException.InnerException;
#endif
            Assert.AreSame(exception, unhandledException.Value);
            return unhandledException;
        }

        private static UnhandledException RejectAndReportLater(System.Exception exception)
            => CaptureUnhandledException(exception, () =>
            {
                // The rejection is reported when the retainer is disposed, which is far removed from where it was rejected.
                var deferred = Promise.NewDeferred();
                var retainer = deferred.Promise.GetRetainer();
                RejectForReportTraceTest(deferred, exception);
                DisposeForReportTraceTest(retainer);
            });

        [Test]
        public void DeferredReject_RejectionTraceHasTheDeferredTraceAsItsCause()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var exception = new System.InvalidOperationException("Bang!");
                CausalityTrace deferredTrace = null;
                var trace = CaptureUnhandledException(exception, () =>
                {
                    var deferred = Promise.NewDeferred();
                    deferredTrace = deferred.Promise._ref.Trace;
                    deferred.Promise.Forget();
                    RejectForReportTraceTest(deferred, exception);
                }).CausalityTrace;

                Assert.IsNotNull(deferredTrace);
                StringAssert.Contains(nameof(RejectForReportTraceTest), trace.StackTrace.ToString());
                CollectionAssert.AreEqual(new[] { deferredTrace }, trace.PreviousTraces);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void DeferredResolve_ContinuationsInvokedSynchronouslyRunInsideTheDeferredScope()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var deferred = Promise.NewDeferred();
                CausalityTrace callbackTrace = null;
                var thenPromise = deferred.Promise.Then(() => { callbackTrace = CausalityTrace.Capture(); });
                var deferredTrace = deferred.Promise._ref.Trace;
                var thenTrace = thenPromise._ref.Trace;
                thenPromise.Forget();

                deferred.Resolve();

                Assert.IsNotNull(deferredTrace);
                Assert.IsNotNull(thenTrace);
                CollectionAssert.AreEqual(new[] { thenTrace, deferredTrace }, callbackTrace.PreviousTraces);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void DeferredResolve_CreatedWhileTracingIsDisabled_ContinuationsRunWithoutTheDeferredScope()
        {
            var deferred = Promise.NewDeferred();
            CausalityTrace.IsEnabled = true;
            try
            {
                CausalityTrace callbackTrace = null;
                var thenPromise = deferred.Promise.Then(() => { callbackTrace = CausalityTrace.Capture(); });
                var thenTrace = thenPromise._ref.Trace;
                thenPromise.Forget();

                deferred.Resolve();

                CollectionAssert.AreEqual(new[] { thenTrace }, callbackTrace.PreviousTraces);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void DeferredResolveAndReject_CreatedWhileTracingIsEnabled_CompleteNormallyAfterTracingIsDisabled(
            [Values] bool reject)
        {
            CausalityTrace.IsEnabled = true;
            var deferred = Promise.NewDeferred();
            CausalityTrace.IsEnabled = false;

            bool completed = false;
            deferred.Promise
                .ContinueWith(result =>
                {
                    Assert.AreEqual(reject ? Promise.State.Rejected : Promise.State.Resolved, result.State);
                    completed = true;
                })
                .Forget();

            if (reject)
            {
                deferred.Reject("Rejected");
            }
            else
            {
                deferred.Resolve();
            }
            Assert.IsTrue(completed);
        }

        [Test]
        public void Finally_CallbackRunsInsideItsScope(
            [Values] bool reject,
            [Values] bool callbackReturnsPromise)
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var deferred = Promise.NewDeferred();
                CausalityTrace callbackTrace = null;
                var finallyPromise = callbackReturnsPromise
                    ? deferred.Promise.Finally(() =>
                    {
                        callbackTrace = CausalityTrace.Capture();
                        return Promise.Resolved();
                    })
                    : deferred.Promise.Finally(() => { callbackTrace = CausalityTrace.Capture(); });
                var finallyTrace = finallyPromise._ref.Trace;
                finallyPromise
                    .Catch(() => { })
                    .Forget();

                if (reject)
                {
                    deferred.Reject("Rejected");
                }
                else
                {
                    deferred.Resolve();
                }

                Assert.IsNotNull(finallyTrace);
                Assert.AreSame(finallyTrace, callbackTrace.PreviousTraces[0]);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void Catch_CallbackRunsInsideItsScope(
            [Values] bool callbackReturnsPromise)
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var deferred = Promise.NewDeferred();
                CausalityTrace callbackTrace = null;
                var catchPromise = callbackReturnsPromise
                    ? deferred.Promise.Catch(() =>
                    {
                        callbackTrace = CausalityTrace.Capture();
                        return Promise.Resolved();
                    })
                    : deferred.Promise.Catch(() => { callbackTrace = CausalityTrace.Capture(); });
                var catchTrace = catchPromise._ref.Trace;
                catchPromise.Forget();

                deferred.Reject("Rejected");

                Assert.IsNotNull(catchTrace);
                Assert.AreSame(catchTrace, callbackTrace.PreviousTraces[0]);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void ThrowRejectExceptionForTraceTest(System.Exception exception)
        {
            throw Promise.RejectException(exception);
        }

        [Test]
        public void RejectException_RejectionTraceIsWhereItWasThrown()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var exception = new System.InvalidOperationException("Bang!");
                CausalityTrace thenTrace = null;
                var trace = CaptureUnhandledException(exception, () =>
                {
                    var deferred = Promise.NewDeferred();
                    var thenPromise = deferred.Promise.Then(() => ThrowRejectExceptionForTraceTest(exception));
                    thenTrace = thenPromise._ref.Trace;
                    thenPromise.Forget();
                    deferred.Resolve();
                }).CausalityTrace;

                Assert.IsNotNull(thenTrace);
                StringAssert.Contains(nameof(ThrowRejectExceptionForTraceTest), trace.StackTrace.ToString());
                Assert.AreSame(thenTrace, trace.PreviousTraces[0]);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        public enum TracedPromiseKind
        {
            Deferred,
            Async,
            Then,
            CatchPassThrough,
            CancelableContinueWith
        }

        private static async Promise TracedAsyncForSentinelTest(Promise promise, bool cancel)
        {
            await promise;
            if (cancel)
            {
                throw Promise.CancelException();
            }
        }

        // Creates a traced promise of the kind that stores a sentinel in the field that's shared with the RejectContainer,
        // and returns an action that completes it, resolved or canceled.
        private static Promise CreateTracedPromiseForSentinelTest(TracedPromiseKind kind, bool cancel, out System.Action complete)
        {
            var deferred = Promise.NewDeferred();
            switch (kind)
            {
                case TracedPromiseKind.Deferred:
                    complete = cancel ? (System.Action) deferred.Cancel : deferred.Resolve;
                    return deferred.Promise;
                case TracedPromiseKind.Async:
                    complete = deferred.Resolve;
                    return TracedAsyncForSentinelTest(deferred.Promise, cancel);
                case TracedPromiseKind.Then:
                    complete = deferred.Resolve;
                    return deferred.Promise.Then(() =>
                    {
                        if (cancel)
                        {
                            throw Promise.CancelException();
                        }
                    });
                case TracedPromiseKind.CatchPassThrough:
                    complete = cancel ? (System.Action) deferred.Cancel : deferred.Resolve;
                    return deferred.Promise.Catch(() => { });
                default:
                    var cancelationSource = CancelationSource.New();
                    var promise = deferred.Promise.ContinueWith(_ => { }, cancelationSource.Token);
                    complete = () =>
                    {
                        if (cancel)
                        {
                            cancelationSource.Cancel();
                        }
                        deferred.Resolve();
                        cancelationSource.Dispose();
                    };
                    return promise;
            }
        }

        [Test]
        public void PromiseEachGroup_DisposedWithTracedResultsRemaining_DoesNotTreatThemAsRejected(
            [Values] TracedPromiseKind kind,
            [Values] bool cancel)
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var enumerator = PromiseEachGroup.New(out _)
                    .Add(CreateTracedPromiseForSentinelTest(kind, cancel, out var complete1))
                    .Add(CreateTracedPromiseForSentinelTest(kind, cancel, out var complete2))
                    .GetAsyncEnumerable()
                    .GetAsyncEnumerator();
                complete1();
                complete2();

                // Disposing with results remaining checks them for rejections.
                bool completed = false;
                enumerator.DisposeAsync()
                    .ContinueWith(result =>
                    {
                        Assert.AreEqual(Promise.State.Resolved, result.State);
                        completed = true;
                    })
                    .Forget();
                Assert.IsTrue(completed);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void PromiseNew_RejectInsideTheCallback_DoesNotEnterTheSameScopeTwice()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var exception = new System.InvalidOperationException("Bang!");
                var trace = CaptureUnhandledException(exception, () =>
                {
                    Promise.New(deferred => RejectForReportTraceTest(deferred, exception))
                        .Forget();
                }).CausalityTrace;

                Assert.AreEqual(1, trace.PreviousTraces.Count);
                Assert.IsNotNull(trace.PreviousTraces[0]);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void AsyncPromise_ContinuationsRunInsideTheAsyncPromiseScope(
            [Values] bool configureFirstAwaitToForeground,
            [Values] bool flowExecutionContext)
        {
            if (flowExecutionContext)
            {
                // This covers the ExecutionContext and ConfiguredAwaitDualContext paths.
                // It can't be disabled after it's enabled, the same as AsyncLocalTests.
                Promise.Config.AsyncFlowExecutionContextEnabled = true;
            }
            CausalityTrace.IsEnabled = true;
            try
            {
                var deferred1 = Promise.NewDeferred();
                var deferred2 = Promise.NewDeferred();
                CausalityTrace traceAfterFirstAwait = null;
                CausalityTrace traceAfterSecondAwait = null;

                async Promise Func()
                {
                    if (configureFirstAwaitToForeground)
                    {
                        await deferred1.Promise.ConfigureAwait(ContinuationOptions.Foreground);
                    }
                    else
                    {
                        await deferred1.Promise;
                    }
                    traceAfterFirstAwait = CausalityTrace.Capture();
                    // If the first await resumed on a context, this checks that the async promise is still traced afterwards.
                    await deferred2.Promise;
                    traceAfterSecondAwait = CausalityTrace.Capture();
                }

                var promise = Func();
                var asyncTrace = promise._ref.Trace;
                promise.Forget();

                deferred1.Resolve();
                TestHelper.ExecuteForegroundCallbacks();
                deferred2.Resolve();

                Assert.IsNotNull(asyncTrace);
                Assert.AreSame(asyncTrace, traceAfterFirstAwait.PreviousTraces[0]);
                Assert.AreSame(asyncTrace, traceAfterSecondAwait.PreviousTraces[0]);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void AsyncPromise_CreatedWhileTracingIsDisabled_ContinuationsRunWithoutAScope()
        {
            var deferred = Promise.NewDeferred();
            CausalityTrace traceAfterAwait = null;

            async Promise Func()
            {
                await deferred.Promise;
                traceAfterAwait = CausalityTrace.Capture();
            }

            var promise = Func();
            promise.Forget();

            CausalityTrace.IsEnabled = true;
            try
            {
                deferred.Resolve();

                Assert.IsNotNull(traceAfterAwait);
                // Neither the async promise nor the deferred was traced, so there are no scopes.
                Assert.AreEqual(0, traceAfterAwait.PreviousTraces.Count);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void RejectChannelForTraceTest(Proto.Promises.Channels.Channel<int> channel, System.Exception exception)
        {
            channel.Writer.TryReject(exception);
        }

        [Test]
        public void ChannelReject_RejectionTraceHasTheChannelTraceAsItsCause()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var exception = new System.InvalidOperationException("Bang!");
                var channel = Proto.Promises.Channels.Channel<int>.NewUnbounded();
                try
                {
                    var channelTrace = Internal.GetTrace(channel._ref);
                    RejectChannelForTraceTest(channel, exception);
                    // Reading from a rejected channel resolves with the reason rather than rejecting, so report the channel's rejection directly.
                    var trace = CaptureUnhandledException(exception, () => ((Internal.IRejectContainer) channel._ref._closedReason).ReportUnhandled())
                        .CausalityTrace;

                    Assert.IsNotNull(channelTrace);
                    StringAssert.Contains(nameof(RejectChannelForTraceTest), trace.StackTrace.ToString());
                    CollectionAssert.AreEqual(new[] { channelTrace }, trace.PreviousTraces);
                }
                finally
                {
                    channel.Dispose();
                }
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void UnhandledRejection_CausalityTraceIsTheRejectionTrace_EvenWhenReportedElsewhere()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var trace = RejectAndReportLater(new System.InvalidOperationException("Bang!")).CausalityTrace;

                Assert.IsNotNull(trace);
                StringAssert.Contains(nameof(RejectForReportTraceTest), trace.StackTrace.ToString());
                StringAssert.DoesNotContain(nameof(DisposeForReportTraceTest), trace.StackTrace.ToString());
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void UnhandledRejection_CausalityTraceIsNullWhenDisabled()
        {
            CausalityTrace.IsEnabled = false;
            Assert.IsNull(RejectAndReportLater(new System.InvalidOperationException("Bang!")).CausalityTrace);
        }

        [Test]
        public void CausalityTraceMaxDepth_MustBeGreaterThanZero()
        {
            Assert.Throws<Proto.Promises.ArgumentOutOfRangeException>(() => CausalityTrace.MaxDepth = 0);
        }

        [Test]
        public void CausalityTrace_SequentialScopes_DepthIsTruncatedToHalfWhenItExceedsMax()
        {
            const int maxDepth = 10;
            int originalMaxDepth = CausalityTrace.MaxDepth;
            CausalityTrace.MaxDepth = maxDepth;
            CausalityTrace.IsEnabled = true;
            try
            {
                CausalityTrace trace = null;
                int expectedDepth = 0;
                bool expectTruncated = false;
                for (int i = 0; i < 100; ++i)
                {
                    var scope = CausalityTrace.EnterScope(trace);
                    trace = CausalityTrace.Capture();
                    scope.Dispose();

                    ++expectedDepth;
                    if (expectedDepth > maxDepth)
                    {
                        expectedDepth = maxDepth / 2;
                        expectTruncated = true;
                    }
                    Assert.AreEqual(expectedDepth, GetCausalityTraceDepth(trace));
                    Assert.AreEqual(expectTruncated, trace.ToString().Contains("Older traces truncated"));
                }
            }
            finally
            {
                CausalityTrace.MaxDepth = originalMaxDepth;
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTrace_NestedScopes_DepthDoesNotExceedMax()
        {
            const int maxDepth = 10;
            int originalMaxDepth = CausalityTrace.MaxDepth;
            CausalityTrace.MaxDepth = maxDepth;
            CausalityTrace.IsEnabled = true;
            try
            {
                // Each trace is captured while all previous traces are still in scope, so traces share their previous traces.
                CaptureNestedCausalityTraces(null, 30, maxDepth);
            }
            finally
            {
                CausalityTrace.MaxDepth = originalMaxDepth;
                CausalityTrace.IsEnabled = false;
            }
        }

        private static void CaptureNestedCausalityTraces(CausalityTrace trace, int remaining, int maxDepth)
        {
            var scope = CausalityTrace.EnterScope(trace);
            try
            {
                var nextTrace = CausalityTrace.Capture();
                Assert.LessOrEqual(GetCausalityTraceDepth(nextTrace), maxDepth);
                if (remaining > 0)
                {
                    CaptureNestedCausalityTraces(nextTrace, remaining - 1, maxDepth);
                }
            }
            finally
            {
                scope.Dispose();
            }
        }

        [Test]
        public void CausalityTrace_RecursivelyScheduledContinuations_DepthDoesNotExceedMax()
        {
            const int maxDepth = 10;
            int originalMaxDepth = CausalityTrace.MaxDepth;
            CausalityTrace.MaxDepth = maxDepth;
            CausalityTrace.IsEnabled = true;
            try
            {
                // Each continuation schedules the next continuation, like a polling loop.
                Promise.Deferred deferred = default;
                CausalityTrace callbackTrace = null;
                System.Action schedule = null;
                schedule = () =>
                {
                    deferred = Promise.NewDeferred();
                    deferred.Promise
                        .Then(() =>
                        {
                            callbackTrace = CausalityTrace.Capture();
                            schedule();
                        })
                        .Forget();
                };
                schedule();

                for (int i = 0; i < 100; ++i)
                {
                    deferred.Resolve();
                    Assert.LessOrEqual(GetCausalityTraceDepth(callbackTrace), maxDepth);
                }
                Assert.GreaterOrEqual(GetCausalityTraceDepth(callbackTrace), maxDepth / 2);
                StringAssert.Contains("Older traces truncated", callbackTrace.ToString());

                // Complete the last scheduled continuation without scheduling another.
                schedule = () => { };
                deferred.Resolve();
            }
            finally
            {
                CausalityTrace.MaxDepth = originalMaxDepth;
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTraceToString_RecursivelyScheduledContinuations_RepeatsArePrintedOnce()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                // Each continuation schedules the next continuation, like a polling loop.
                // Each level's traces refer to the previous level twice (the callback and the deferred), so printing every path would grow exponentially.
                Promise.Deferred deferred = default;
                CausalityTrace callbackTrace = null;
                System.Action schedule = null;
                schedule = () =>
                {
                    deferred = Promise.NewDeferred();
                    deferred.Promise
                        .Then(() =>
                        {
                            callbackTrace = CausalityTrace.Capture();
                            schedule();
                        })
                        .Forget();
                };
                schedule();
                for (int i = 0; i < 200; ++i)
                {
                    deferred.Resolve();
                }

                var text = callbackTrace.ToString();
                StringAssert.IsMatch(@"--- \[#\d+ repeated \d+ times?\] ---", text);
                Assert.Less(text.Length, 50000);
                AssertCausalityTraceReferencesAreLabeled(text);

                // Complete the last scheduled continuation without scheduling another.
                schedule = () => { };
                deferred.Resolve();
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTraceToString_AsyncLoop_RepeatsArePrintedOnce()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var text16 = GetTraceFromAsyncLoop(16);
                var text64 = GetTraceFromAsyncLoop(64);
                StringAssert.IsMatch(@"--- \[#\d+ repeated \d+ times?\] ---", text64);
                // The output doesn't grow with the number of iterations (only the repeat counts change).
                Assert.Less(text64.Length, text16.Length + 100);
                AssertCausalityTraceReferencesAreLabeled(text64);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTraceToString_RepeatsThatRanInDifferentOperations_AreAllPrinted()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                // Each recursion has the same stack trace, but it ran inside a different operation.
                var pending = new Queue<Promise.Deferred>();
                var trace = new CausalityTrace[1];
                var responses = new[] { LoadPlayer(pending), LoadInventory(pending), LoadSettings(pending) };
                ProcessAll(responses, 0, trace).Forget();
                while (pending.Count > 0)
                {
                    pending.Dequeue().Resolve();
                }

                var text = trace[0].ToString();
                StringAssert.Contains(nameof(LoadPlayer), text);
                StringAssert.Contains(nameof(LoadInventory), text);
                StringAssert.Contains(nameof(LoadSettings), text);
                AssertCausalityTraceReferencesAreLabeled(text);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTraceToString_RepeatsWithDifferentHistoriesFurtherBack_AreAllPrinted()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                // Each recursion has the same stack trace, and ran inside operations with the same stack traces,
                // but those operations were started by different loaders further back.
                var pending = new Queue<Promise.Deferred>();
                var trace = new CausalityTrace[1];
                var responses = new List<Promise>();
                foreach (var load in new System.Func<Queue<Promise.Deferred>, Promise>[] { LoadPlayer, LoadInventory, LoadSettings })
                {
                    responses.Add(WaitAndRequest(WaitAndRequest(load(pending), pending), pending));
                }
                ProcessAll(responses.ToArray(), 0, trace).Forget();
                while (pending.Count > 0)
                {
                    pending.Dequeue().Resolve();
                }

                var text = trace[0].ToString();
                StringAssert.Contains(nameof(LoadPlayer), text);
                StringAssert.Contains(nameof(LoadInventory), text);
                StringAssert.Contains(nameof(LoadSettings), text);
                AssertCausalityTraceReferencesAreLabeled(text);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        private static async Promise WaitAndRequest(Promise trigger, Queue<Promise.Deferred> pending)
        {
            await trigger;
            await GetPendingPromise(pending);
        }

        [Test]
        public void CausalityTraceToString_IdenticalTraces_ArePrintedOnce()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                // Two different traces with the same stack trace and the same history, which are both outer scopes of X.
                var traces = new CausalityTrace[2];
                for (int i = 0; i < traces.Length; ++i)
                {
                    traces[i] = CaptureFromHelper();
                }
                var traceB = CausalityTrace.Capture();
                var scope0 = CausalityTrace.EnterScope(traces[0]);
                var scope1 = CausalityTrace.EnterScope(traces[1]);
                var scopeB = CausalityTrace.EnterScope(traceB);
                var traceX = CausalityTrace.Capture();
                scopeB.Dispose();
                scope1.Dispose();
                scope0.Dispose();
                Assert.AreNotSame(traces[0], traces[1]);

                var text = traceX.ToString();
                // They are printed as one outer scope, which is referred to once.
                Assert.AreEqual(1, System.Text.RegularExpressions.Regex.Matches(text, nameof(CaptureFromHelper)).Count, text);
                Assert.AreEqual(1, System.Text.RegularExpressions.Regex.Matches(text, "Outer causality scope A, see below").Count, text);
                StringAssert.DoesNotContain("Outer causality scope B", text);
                AssertCausalityTraceReferencesAreLabeled(text);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static CausalityTrace CaptureFromHelper()
            => CausalityTrace.Capture();

        private static Promise GetPendingPromise(Queue<Promise.Deferred> pending)
        {
            var deferred = Promise.NewDeferred();
            pending.Enqueue(deferred);
            return deferred.Promise;
        }

        private static async Promise CaptureAfterPending(Queue<Promise.Deferred> pending, CausalityTrace[] trace)
        {
            await GetPendingPromise(pending);
            trace[0] = CausalityTrace.Capture();
        }

        private static async Promise LoopCallingAsyncMethod(Queue<Promise.Deferred> pending, CausalityTrace[] trace, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                await CaptureAfterPending(pending, trace);
            }
        }

        private static string GetTraceFromAsyncLoop(int iterations)
        {
            var pending = new Queue<Promise.Deferred>();
            var trace = new CausalityTrace[1];
            LoopCallingAsyncMethod(pending, trace, iterations).Forget();
            while (pending.Count > 0)
            {
                pending.Dequeue().Resolve();
            }
            return trace[0].ToString();
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static Promise LoadPlayer(Queue<Promise.Deferred> pending)
            => GetPendingPromise(pending);

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static Promise LoadInventory(Queue<Promise.Deferred> pending)
            => GetPendingPromise(pending);

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static Promise LoadSettings(Queue<Promise.Deferred> pending)
            => GetPendingPromise(pending);

        private static async Promise ProcessAll(Promise[] responses, int index, CausalityTrace[] trace)
        {
            await responses[index];
            trace[0] = CausalityTrace.Capture();
            if (index + 1 < responses.Length)
            {
                await ProcessAll(responses, index + 1, trace);
            }
        }

        // Every outer scope that is referred to must be labeled, in the direction that the reference says,
        // and repeated blocks must be printed before they are repeated.
        private static void AssertCausalityTraceReferencesAreLabeled(string text)
        {
            Assert.IsFalse(text.Any(c => c < ' ' && c != '\r' && c != '\n'), "The text contains an unresolved placeholder: " + text);
            foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(text, @"Outer causality scope ([A-Z]+(?:/[A-Z]+)*), see (above|below)"))
            {
                int labelIndex = text.IndexOf("--- [Outer causality scope " + match.Groups[1].Value + "] ---");
                Assert.GreaterOrEqual(labelIndex, 0, "Missing label for " + match.Value + ": " + text);
                Assert.AreEqual(match.Groups[2].Value == "below", labelIndex > match.Index, match.Value + ": " + text);
            }
            foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(text, @"\[#(\d+) repeated \d+ times?\]"))
            {
                var number = match.Groups[1].Value;
                int beginIndex = text.IndexOf("--- [#" + number + " Begin] ---");
                int endIndex = text.IndexOf("--- [#" + number + " End] ---");
                Assert.GreaterOrEqual(beginIndex, 0, "Missing begin label for " + match.Value + ": " + text);
                Assert.Greater(endIndex, beginIndex, "Missing end label for " + match.Value + ": " + text);
                Assert.Less(endIndex, match.Index, match.Value + " must be after its block: " + text);
            }
        }
    }
}